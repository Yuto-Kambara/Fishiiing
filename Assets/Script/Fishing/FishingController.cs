using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FishingController（入力・状態遷移・餌/ルアー/リール消費＆適用）
/// ・成功：魚生成 → 餌のみ 1 個消費（ルアーは消費しない）
/// ・失敗：餌またはルアーを 1 個失う
/// ・装備：餌/ルアーのレア重み・リール速度乗数を RodStats に反映
/// </summary>
public class FishingController : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Transform breakwater;
    public FishSpawner spawner;
    public RodStats rod;

    [Header("Hook / Cast")]
    public GameObject hookPrefab;
    public float castSpeed = 6f;
    public Vector2 castOffset = new(0f, -0.5f);

    [Header("Catch Point (local)")]
    public Vector2 catchOffset = Vector2.zero;
    public float catchTolerance = 0.08f;

    [Header("Judge Distances")]
    [Tooltip("堤防右端からこの距離以上で Space を離すと成功")]
    public float releaseDistance = 0.06f;

    [Header("Fail Sinking")]
    public float sinkSpeed = 3f;
    public float sinkDestroyDelay = 2f;

    [Header("Inventory Link")]
    [Tooltip("プレイヤーの InventoryBinder（PlayerPanel）")]
    public InventoryBinder playerInventory;
    [Tooltip("『餌/ルアー』スロットのインデックス")]
    public int baitOrLureSlotIndex = 6;
    [Tooltip("『リール』スロットのインデックス")]
    public int reelSlotIndex = 5;

    private enum State { Idle, Casting, Fishing }
    private State state = State.Idle;

    private GameObject currentHook;
    private Hook hookComp;
    private Rigidbody2D hookRb;

    private float rightEdgeX;
    private float lastEdgeDist;
    private bool fullReel = false, isSinking = false;
    private float sinkTimer;
    private float currentFullReelSpeed = 4f;

    /*--------------------------------------------------------*/
    private void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerController>();
        if (!rod) rod = FindFirstObjectByType<RodStats>();
        if (!spawner) spawner = FindFirstObjectByType<FishSpawner>();

        if (!breakwater)
        {
            Debug.LogError("[FishingController] Breakwater 未設定");
            enabled = false; return;
        }

        var col = breakwater.GetComponent<BoxCollider2D>();
        if (!col)
        {
            Debug.LogError("[FishingController] Breakwater に BoxCollider2D が必要");
            enabled = false; return;
        }
        float half = col.size.x * breakwater.lossyScale.x * 0.5f;
        rightEdgeX = breakwater.position.x + half;

        if (!playerInventory) playerInventory = FindFirstObjectByType<InventoryBinder>();
    }

    private void Update()
    {
        switch (state)
        {
            case State.Idle:
                if (Input.GetKeyDown(KeyCode.Space)) StartCast();
                break;

            case State.Fishing:
                if (isSinking) UpdateSinking();
                else HandleFishing();
                break;
        }
    }

    /* ======================= CAST ======================= */
    private void StartCast()
    {
        // 右端でのみ開始可能（少しの許容）
        if (player && !player.IsAtRightEdge(0.15f)) return;

        // 釣り中は完全停止（UI ゲートより優先）
        player?.SetMovementEnabled(false);

        state = State.Casting;
        fullReel = false; isSinking = false;

        // 装備（餌/ルアー重み、リール乗数）を Rod へ反映
        ApplyEquippedToRod();

        // ルアー生成＆射出
        Vector3 origin = transform.TransformPoint(castOffset);
        currentHook = Instantiate(hookPrefab, origin, Quaternion.identity);
        hookRb = currentHook.GetComponent<Rigidbody2D>();
        hookComp = currentHook.GetComponent<Hook>();
        hookComp.Init(this);

        hookRb.linearVelocity = Vector2.right * castSpeed;
    }

    public void OnHookHitWater() => state = State.Fishing;

    /* ======================= FISHING ======================= */
    private void HandleFishing()
    {
        if (!currentHook) return;

        Vector2 hookPos = currentHook.transform.position;
        Vector2 rodTipPos = transform.position;
        float edgeDist = Mathf.Max(0f, hookPos.x - rightEdgeX);

        if (!fullReel)
        {
            bool held = Input.GetKey(KeyCode.Space);
            bool up = Input.GetKeyUp(KeyCode.Space);

            if (held)
            {
                float dx = rodTipPos.x - hookPos.x;
                float hSpeed = rod ? rod.GetHorizontalReelSpeed() : 4f;
                hookRb.linearVelocity = Mathf.Abs(dx) < 0.05f
                    ? Vector2.zero
                    : new Vector2(Mathf.Sign(dx) * hSpeed, 0f);

                if (edgeDist < releaseDistance)
                {
                    TriggerFailSink();
                    return;
                }
            }
            else
            {
                hookRb.linearVelocity = Vector2.zero;
            }

            if (up)
            {
                if (edgeDist >= releaseDistance)
                {
                    lastEdgeDist = edgeDist;

                    // ★ 釣れた瞬間：魚生成 → 餌の消費
                    var res = spawner
                        ? spawner.SpawnFromHook(hookPos, lastEdgeDist, releaseDistance)
                        : new FishSpawnResult();

                    currentFullReelSpeed = rod ? rod.GetFullReelSpeed(res.maxRarity) : 4f;

                    ConsumeOnSuccess();                 // 餌のみ 1 個消費
                    hookComp.ReleaseAnchor();
                    fullReel = true;
                }
                else
                {
                    TriggerFailSink();
                }
            }
        }
        else
        {
            Vector2 catchWorld = (Vector2)transform.position + catchOffset;
            Vector2 dir = catchWorld - hookPos;

            if (dir.magnitude < catchTolerance)
            {
                FinishReel();
            }
            else
            {
                hookRb.linearVelocity = dir.normalized * Mathf.Max(0.1f, currentFullReelSpeed);
            }
        }
    }

    /* ======================= SUCCESS / FAIL ======================= */
    private void FinishReel()
    {
        if (currentHook)
        {
            hookRb.linearVelocity = Vector2.zero;
            Destroy(currentHook);
        }
        player?.SetMovementEnabled(true);
        state = State.Idle;
    }

    private void TriggerFailSink()
    {
        if (isSinking) return;

        // ★ 失敗時：餌でもルアーでも 1 個失う
        ConsumeOnFail();

        if (hookComp && hookComp.IsAnchored) hookComp.ReleaseAnchor();

        fullReel = false; isSinking = true; sinkTimer = 0f;
        hookRb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        hookRb.gravityScale = 1f;
        hookRb.linearVelocity = Vector2.down * sinkSpeed;
    }

    private void UpdateSinking()
    {
        sinkTimer += Time.deltaTime;
        if (sinkTimer >= sinkDestroyDelay)
        {
            if (currentHook) Destroy(currentHook);
            player?.SetMovementEnabled(true);
            state = State.Idle;
        }
    }

    /* ======================= 装備の適用 & 消費 ======================= */

    /// <summary>餌/ルアー枠 & リール枠の装備を RodStats に適用</summary>
    private void ApplyEquippedToRod()
    {
        if (!rod) return;

        // --- 餌/ルアーの抽選重みオーバーライド ---
        Dictionary<FishRarity, int> weightsOverride = null;
        var bl = GetEquippedBaitOrLure();

        if (bl == null)
        {
            // ★装備なし：Common / Uncommon のみ出るように制限
            weightsOverride = new Dictionary<FishRarity, int>
        {
            { FishRarity.Common,    2 },
            { FishRarity.Uncommon,  1 },
            { FishRarity.Rare,      0 },
            { FishRarity.Epic,      0 },
            { FishRarity.Legendary, 0 },
        };
            rod.SetBait(RodStats.BaitType.Lure); // 種別自体はルアー相当でOK
        }
        else
        {
            // 装備あり：定義の重みを使用
            if (bl.def is BaitDefinition bd)
            {
                weightsOverride = bd.rarityWeights.IsAllZero() ? null : bd.rarityWeights.ToDict();
                rod.SetBait(RodStats.BaitType.FishBait);
            }
            else if (bl.def is LureDefinition ld)
            {
                weightsOverride = ld.rarityWeights.IsAllZero() ? null : ld.rarityWeights.ToDict();
                rod.SetBait(RodStats.BaitType.Lure);
            }
            else
            {
                // 想定外タグはルアー扱い + ノーエquip制限にしてもよいが、ここでは上書きなし
                rod.SetBait(RodStats.BaitType.Lure);
            }
        }

        rod.SetRarityWeightsOverride(weightsOverride);

        // --- リール速度オーバーライド ---
        ReelDefinition reelDef = null;
        var reelItem = GetEquippedReel();
        if (reelItem != null && reelItem.def is ReelDefinition rd) reelDef = rd;
        rod.SetReelOverride(reelDef);
    }


    private ItemInstance GetEquippedBaitOrLure()
    {
        if (!playerInventory || playerInventory.Model == null) return null;
        if (!playerInventory.Model.InRange(baitOrLureSlotIndex)) return null;
        return playerInventory.Model.Get(baitOrLureSlotIndex);
    }

    private ItemInstance GetEquippedReel()
    {
        if (!playerInventory || playerInventory.Model == null) return null;
        if (!playerInventory.Model.InRange(reelSlotIndex)) return null;
        return playerInventory.Model.Get(reelSlotIndex);
    }

    /// <summary>成功時：餌は 1 個だけ消費、ルアーは消費しない</summary>
    private void ConsumeOnSuccess()
    {
        var model = playerInventory ? playerInventory.Model : null;
        if (model == null || !model.InRange(baitOrLureSlotIndex)) return;

        var it = model.Get(baitOrLureSlotIndex);
        if (it == null) return;

        if ((it.Tags & ItemTags.Bait) != 0)
        {
            it.count = Mathf.Max(0, it.count - 1);
            model.Set(baitOrLureSlotIndex, it.count > 0 ? it : null);
        }
        // ルアーは消費なし
    }

    /// <summary>失敗時：餌/ルアーのいずれでも 1 個失う</summary>
    private void ConsumeOnFail()
    {
        var model = playerInventory ? playerInventory.Model : null;
        if (model == null || !model.InRange(baitOrLureSlotIndex)) return;

        var it = model.Get(baitOrLureSlotIndex);
        if (it == null) return;

        it.count = Mathf.Max(0, it.count - 1);
        model.Set(baitOrLureSlotIndex, it.count > 0 ? it : null);
    }
}

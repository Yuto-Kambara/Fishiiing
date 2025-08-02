using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;   // Toast画像用

/// <summary>
/// FishingController
/// ・一対一対応：マルチキャッチ時、i匹目 ←→ i番目の餌/ルアースロットの重みで抽選（枠を周回）
/// ・成功：魚生成 → 使用した餌スロットのみ1個消費（ルアーは消費しない）
/// ・失敗：装備枠から1個消費。通常失敗=自動巻き上げ / 糸が堤防接触=沈降
/// ・着水後：ランダム遅延で食い付き（biteReady）
/// ・食い付いた瞬間：効果音＆右上に画像トースト
/// ・成功時の巻き上げ速度：公開変数で上書き or 乗算で調整可能
/// ・落下先の最終決定は FishSpawner 側
/// ・糸は白色のたゆみ付き LineRenderer（ベジェ）で表示、セグメント Linecast で接触判定
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

    [Header("Success Reel")]
    [Tooltip("true なら成功時の巻き上げ速度を固定値 successReelSpeed で上書き")]
    public bool overrideSuccessReelSpeed = false;
    [Tooltip("成功時の巻き上げ速度（上書き用）。単位: units/sec")]
    public float successReelSpeed = 4f;
    [Tooltip("RodStats の計算結果に掛ける係数（override=false のとき有効）")]
    public float successReelSpeedMultiplier = 1f;

    [Header("Fail Auto Reel")]
    [Tooltip("通常失敗の自動巻き上げスピード")]
    public float failReelSpeed = 3.5f;

    [Header("Fail Sinking")]
    [Tooltip("沈降時の落下速度（糸が堤防に触れた失敗など）")]
    public float sinkSpeed = 3f;
    public float sinkDestroyDelay = 2f;

    [Header("Multi Catch Spawn (Controller Offset)")]
    [Tooltip("2匹目以降の出現を1匹ごとに下げる量（>0で下方向）")]
    public float multiSpawnYOffset = 0.5f;
    [Tooltip("左右の微オフセット量（奇数/偶数で左右に振る）")]
    public float multiSpawnXJitter = 0.5f;

    [Header("Multi Catch Landing Offset (along drop mapping)")]
    [Tooltip("同時生成時、i匹目ごとに edgeDist をずらして着地点を分散（単位: ワールド距離）")]
    public float multiLandingEdgeDistStep = 0.08f;
    [Tooltip("オフセット方向を±交互に広げる（true 推奨）")]
    public bool alternateLandingOffset = true;

    [Header("Inventory Link")]
    public InventoryBinder playerInventory;
    public int reelSlotIndex = 5;

    [Header("Bite Window (Timeout)")]
    [Tooltip("魚がかかってから離れるまでの猶予時間(秒)。0以下で無効。")]
    public float biteTimeoutSeconds = 2.5f;
    [Tooltip("タイムアウト時の効果音（任意）")]
    public AudioClip escapeSfx;
    [Range(0f, 1f)] public float escapeSfxVolume = 1f;

    [Header("Early Reel Penalty")]
    [Tooltip("バイト前に巻き始めたら、このキャストの成功を無効化する")]
    public bool enableEarlyReelPenalty = true;

    [Header("Line Break (on sink)")]
    [Tooltip("沈降開始時に糸を切って可視ラインを消す")]
    public bool cutLineOnSink = true;
    public AudioClip lineBreakSfx;
    [Range(0f, 1f)] public float lineBreakVolume = 1f;

    // 内部
    private float biteExpireAt = -1f;
    private bool isReelingAfterBite = false;

    // 早巻き関連
    private bool startedReelBeforeBite = false;
    private bool biteInvalidDueToEarlyReel = false;

    // 複数枠対応
    public int baitOrLureBaseIndex = 6;
    public int baitOrLureSlots = 1;

    [Header("Bite Timing (Random Delay)")]
    public Vector2 biteDelayRange = new(0.8f, 2.5f);

    [Header("Bite Notify (SFX & Image)")]
    public AudioClip biteSfx;
    [Range(0f, 1f)] public float biteSfxVolume = 1f;
    public Sprite biteSprite;
    public Vector2 biteImageSize = new(160, 160);
    public float biteImageDuration = 1.5f;
    public float biteImageFadeOut = 0.25f;
    public Vector2 biteImageMargin = new(20, -20); // 右上からの相対

    [Header("Drop Assist (for distance to cooler)")]
    [Tooltip("クーラー入口（距離計算だけに使用）。落下先の決定は FishSpawner 側。")]
    public Transform coolerMouth;

    [Header("Fishing Line (visual & collision)")]
    [Tooltip("糸の LineRenderer（未指定なら自動生成）")]
    public LineRenderer lineRenderer;
    [Tooltip("糸の幅")]
    public float lineWidth = 0.02f;
    [Tooltip("糸の色（既定：白）")]
    public Color lineColor = Color.white;
    [Tooltip("ロッド先端のローカルオフセット（糸の起点）")]
    public Vector2 lineRodLocalOffset = Vector2.zero;
    [Tooltip("糸のたゆみ（距離に対する割合 0..1）")]
    [Range(0f, 0.5f)] public float lineSagFactor = 0.12f;
    [Tooltip("糸の分割数（曲線の滑らかさ）")]
    [Min(2)] public int lineSegments = 10;
    [Tooltip("Linecast の障害物マスク（未設定なら全レイヤ）")]
    public LayerMask lineObstacleMask = ~0;
    [Tooltip("糸のZ（表示順調整用）")]
    public float lineZ = 0f;

    private enum State { Idle, Casting, Fishing }
    private State state = State.Idle;

    private GameObject currentHook;
    private Hook hookComp;
    private Rigidbody2D hookRb;
    private float maxEdgeDistSeenThisCast = 0f;

    private float rightEdgeX;
    private float leftEdgeX;
    private float lastEdgeDist;
    private bool fullReel = false;

    // 失敗状態
    private bool isSinking = false;     // 糸接触などの沈降モード
    private float sinkTimer;
    private bool autoReelBack = false;  // 通常失敗の自動巻き上げ

    private float currentFullReelSpeed = 4f;

    // バイト（食い付き）管理
    private bool biteReady = false;
    private float biteReadyAt = -1f;

    // SFX / Toast
    private AudioSource sfxSource;
    private GameObject biteToast;
    private CanvasGroup biteToastCg;
    private float biteToastHideAt = -1f;

    // 作業用バッファ（割り当て抑制）
    private Vector3[] _linePts;

    // 糸切れフラグ
    private bool lineBroken = false;

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
        leftEdgeX = breakwater.position.x - half;

        if (!playerInventory) playerInventory = FindFirstObjectByType<InventoryBinder>();

        var pil = FindFirstObjectByType<PlayerInventoryLayout>();
        if (pil)
        {
            baitOrLureBaseIndex = pil.baitOrLureBaseIndex;
            baitOrLureSlots = Mathf.Max(1, pil.baitOrLureSlots);
        }

        if (!TryGetComponent(out sfxSource)) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false; sfxSource.loop = false;
        sfxSource.spatialBlend = 0f; sfxSource.volume = biteSfxVolume;

        EnsureLineRenderer();
        SetLineActive(false);
    }

    private void Update()
    {
        switch (state)
        {
            case State.Idle:
                if (Input.GetKeyDown(KeyCode.Space)) StartCast();
                break;

            case State.Fishing:
                if (isSinking) { UpdateSinking(); }
                else { HandleFishing(); }
                break;
        }

        UpdateBiteToast();
        UpdateLineAndCollision();
    }

    /* ======================= CAST ======================= */
    private void StartCast()
    {
        if (player && !player.IsAtRightEdge(0.15f)) return;

        player?.SetMovementEnabled(false);

        state = State.Casting;
        fullReel = false;
        isSinking = false;
        autoReelBack = false;

        // バイト状態リセット
        biteReady = false; biteReadyAt = -1f;
        biteExpireAt = -1f;

        // 早巻きフラグ初期化
        startedReelBeforeBite = false;
        biteInvalidDueToEarlyReel = false;
        isReelingAfterBite = false;

        // 糸切れフラグ初期化
        lineBroken = false;

        // 待機時見かけ用
        ApplyEquippedPreviewToRod();

        // フック生成＆射出
        Vector3 origin = transform.TransformPoint(castOffset);
        currentHook = Instantiate(hookPrefab, origin, Quaternion.identity);
        hookRb = currentHook.GetComponent<Rigidbody2D>();
        hookComp = currentHook.GetComponent<Hook>();
        hookComp.Init(this);

        hookRb.linearVelocity = Vector2.right * castSpeed;

        SetLineActive(true);
    }

    public void OnHookHitWater()
    {
        state = State.Fishing;
        StartBiteCountdown();
    }

    private void StartBiteCountdown()
    {
        float min = Mathf.Min(biteDelayRange.x, biteDelayRange.y);
        float max = Mathf.Max(biteDelayRange.x, biteDelayRange.y);
        biteReadyAt = Time.time + Random.Range(min, max);
        biteReady = false;
    }

    private void UpdateBiteTimer()
    {
        if (!biteReady && biteReadyAt > 0f && Time.time >= biteReadyAt)
        {
            if (enableEarlyReelPenalty && startedReelBeforeBite)
            {
                biteInvalidDueToEarlyReel = true; // 早巻きペナルティ
            }

            biteReady = true;
            biteExpireAt = (biteTimeoutSeconds > 0f) ? Time.time + biteTimeoutSeconds : -1f;

            if (biteSfx) { sfxSource.volume = biteSfxVolume; sfxSource.PlayOneShot(biteSfx); }
            if (biteSprite) ShowBiteToast();
        }
    }

    /* ======================= FISHING ======================= */
    private void HandleFishing()
    {
        if (!currentHook) return;

        UpdateBiteTimer();
        CheckBiteTimeout();

        Vector2 hookPos = currentHook.transform.position;
        Vector2 rodTipPos = GetRodTipWorld();
        float edgeDist = Mathf.Max(0f, hookPos.x - rightEdgeX);

        if (edgeDist > maxEdgeDistSeenThisCast)
            maxEdgeDistSeenThisCast = edgeDist;

        // 失敗自動巻き上げ中
        if (autoReelBack)
        {
            Vector2 catchWorld = (Vector2)transform.position + catchOffset;
            Vector2 dir = catchWorld - hookPos;

            if (dir.magnitude < catchTolerance)
            {
                FinishReel();
                return;
            }

            if (hookRb)
            {
                hookRb.gravityScale = 0f;
                hookRb.constraints = RigidbodyConstraints2D.FreezeRotation;
                hookRb.linearVelocity = dir.normalized * Mathf.Max(0.1f, failReelSpeed);
            }
            return;
        }

        if (!fullReel)
        {
            bool held = Input.GetKey(KeyCode.Space);
            bool up = Input.GetKeyUp(KeyCode.Space);

            // バイト前に巻き始めたかを記録
            if (held && !biteReady) startedReelBeforeBite = true;

            // バイト後に巻いている場合のみタイムアウト停止
            isReelingAfterBite = biteReady && held && !biteInvalidDueToEarlyReel;

            if (held)
            {
                // 手動巻き（早巻き中でも自動回収しない）
                float dx = rodTipPos.x - hookPos.x;
                float hSpeed = rod ? rod.GetHorizontalReelSpeed() : 4f;
                hookRb.linearVelocity = Mathf.Abs(dx) < 0.05f
                    ? Vector2.zero
                    : new Vector2(Mathf.Sign(dx) * hSpeed, 0f);

                // （削除済）edgeDist 小で自動回収していた早期失敗判定
            }
            else
            {
                hookRb.linearVelocity = Vector2.zero;
            }

            if (up)
            {
                if (edgeDist >= releaseDistance)
                {
                    // バイト無し or 早巻きペナルティ中 → 失敗（自動回収）
                    if (!biteReady || biteInvalidDueToEarlyReel)
                    {
                        TriggerFailAutoReel();
                        return;
                    }

                    // --- 成功処理 ---
                    lastEdgeDist = edgeDist;

                    var equipped = GetEquippedBaitOrLures();
                    int multi = GetMultiCatchCountSafe();
                    if (multi <= 0) multi = 1;

                    if (spawner)
                    {
                        int kmax = Mathf.CeilToInt((multi - 1) / 2f);
                        float neededMax = maxEdgeDistSeenThisCast + kmax * Mathf.Abs(multiLandingEdgeDistStep);
                        spawner.maxDropDistance = Mathf.Max(spawner.releaseDistance + 0.01f, neededMax);
                    }

                    FishRarity maxRarity = FishRarity.Common;
                    var consumedIndexes = new HashSet<int>();

                    for (int i = 0; i < multi; i++)
                    {
                        int slotIdx = -1;
                        ItemInstance it = null;

                        if (equipped.Count > 0)
                        {
                            var pair = equipped[i % equipped.Count];
                            slotIdx = pair.index;
                            it = pair.item;
                        }

                        bool isBait = it != null && (it.Tags & ItemTags.Bait) != 0;
                        var weights = BuildWeightsForItem(it);

                        rod.SetRarityWeightsOverride(weights);
                        rod.SetBait(isBait ? RodStats.BaitType.FishBait : RodStats.BaitType.Lure);

                        if (spawner)
                            spawner.maxDropDistance = Mathf.Max(
                                spawner.releaseDistance + 0.01f,
                                maxEdgeDistSeenThisCast
                            );

                        Vector2 hookPos_i = hookPos;
                        if (i > 0)
                        {
                            float xJitter = ((i & 1) == 0 ? -1f : 1f) * Mathf.Abs(multiSpawnXJitter);
                            float yStagger = -Mathf.Abs(multiSpawnYOffset) * i;
                            hookPos_i += new Vector2(xJitter, yStagger);
                        }

                        // 落下位置の分散
                        float edgeDist_i = edgeDist;
                        if (spawner && i > 0)
                        {
                            float step = Mathf.Abs(multiLandingEdgeDistStep);
                            float delta;
                            if (alternateLandingOffset)
                            {
                                int m = Mathf.CeilToInt(i / 2f);
                                float sign = (i % 2 == 1) ? -1f : 1f;
                                delta = sign * m * step;
                            }
                            else
                            {
                                delta = i * step;
                            }

                            float minD = spawner.releaseDistance;
                            float maxD = Mathf.Max(minD + 0.001f, spawner.maxDropDistance);
                            edgeDist_i = Mathf.Clamp(edgeDist + delta, minD, maxD);
                        }

                        var res = spawner
                            ? spawner.SpawnFromHook(hookPos_i, edgeDist_i)
                            : new FishSpawnResult();

                        if (res.maxRarity > maxRarity) maxRarity = res.maxRarity;
                        if (isBait && slotIdx >= 0) consumedIndexes.Add(slotIdx);
                    }

                    var baseSpeed = rod ? rod.GetFullReelSpeed(maxRarity) : 4f;
                    currentFullReelSpeed = overrideSuccessReelSpeed
                        ? successReelSpeed
                        : baseSpeed * Mathf.Max(0f, successReelSpeedMultiplier);
                    currentFullReelSpeed = Mathf.Max(0.1f, currentFullReelSpeed);

                    ConsumeOnSuccessMulti(consumedIndexes);

                    // 状態クリア
                    biteReady = false;
                    biteReadyAt = -1f;
                    biteExpireAt = -1f;
                    biteInvalidDueToEarlyReel = false;
                    startedReelBeforeBite = false;

                    hookComp.ReleaseAnchor();
                    fullReel = true;

                    rod.SetRarityWeightsOverride(null);
                }
                else
                {
                    // しきい値未満で離したら通常失敗（自動回収）
                    TriggerFailAutoReel();
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

    /* ======================= 糸の見た目＆接触判定 ======================= */
    private void UpdateLineAndCollision()
    {
        if (!lineRenderer) return;

        // 糸切れ中は描画も接触判定もしない
        if (lineBroken)
        {
            SetLineActive(false);
            return;
        }

        if (!currentHook)
        {
            SetLineActive(false);
            return;
        }

        // たゆみ付きのライン形状（2次ベジェ）
        Vector3 a = GetRodTipWorld();
        Vector3 b = currentHook.transform.position;
        a.z = lineZ; b.z = lineZ;

        BuildBezierPoints(a, b, ref _linePts);

        lineRenderer.positionCount = _linePts.Length;
        lineRenderer.SetPositions(_linePts);
        if (!lineRenderer.enabled) lineRenderer.enabled = true;

        // セグメントごとの Linecast で接触判定（堤防に触れたら沈降失敗）
        if (state != State.Idle && !isSinking)
        {
            var bwCol = breakwater ? breakwater.GetComponent<Collider2D>() : null;

            for (int i = 0; i < _linePts.Length - 1; i++)
            {
                RaycastHit2D hit = (lineObstacleMask.value != 0)
                    ? Physics2D.Linecast(_linePts[i], _linePts[i + 1], lineObstacleMask)
                    : Physics2D.Linecast(_linePts[i], _linePts[i + 1]);

                if (hit.collider != null)
                {
                    bool hitBreakwater = (bwCol && hit.collider == bwCol) || bwCol == null;
                    if (hitBreakwater)
                    {
                        TriggerFailSinkByLine();
                        break;
                    }
                }
            }
        }
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
        {
            var go = new GameObject("FishingLine", typeof(LineRenderer));
            lineRenderer = go.GetComponent<LineRenderer>();
            lineRenderer.transform.SetParent(transform, false);
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }
        lineRenderer.widthMultiplier = Mathf.Max(0.001f, lineWidth);
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.enabled = false;
    }

    private void SetLineActive(bool on)
    {
        if (!lineRenderer) return;
        lineRenderer.enabled = on;
        if (!on) lineRenderer.positionCount = 0;
    }

    private Vector2 GetRodTipWorld()
    {
        // ロッド位置＋任意の先端オフセット（catchOffset と別に微調整可能）
        return (Vector2)transform.position + catchOffset + lineRodLocalOffset;
    }

    /* ======================= ベジェ生成 & 最短距離 ======================= */

    private void BuildBezierPoints(Vector3 a, Vector3 b, ref Vector3[] buffer)
    {
        int n = Mathf.Max(2, lineSegments);
        if (buffer == null || buffer.Length != n + 1) buffer = new Vector3[n + 1];

        float length = Vector2.Distance(a, b);
        float sag = Mathf.Max(0f, lineSagFactor) * length;
        Vector3 c = (a + b) * 0.5f; c.y -= sag; c.z = lineZ;

        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n;
            float u = 1f - t;
            buffer[i] = (u * u) * a + (2f * u * t) * c + (t * t) * b;
        }
    }

    private float ComputeMinDistanceLineTo(Vector2 target)
    {
        if (!currentHook) return float.PositiveInfinity;

        Vector3 a = GetRodTipWorld(); a.z = lineZ;
        Vector3 b = currentHook.transform.position; b.z = lineZ;
        Vector3[] pts = null;
        BuildBezierPoints(a, b, ref pts);

        float min = float.PositiveInfinity;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            float d = DistancePointToSegment(target, pts[i], pts[i + 1]);
            if (d < min) min = d;
        }
        return min;
    }

    private float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        Vector2 proj = a + t * ab;
        return Vector2.Distance(p, proj);
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

        // 状態リセット
        fullReel = false;
        autoReelBack = false;
        isSinking = false;
        biteReady = false; biteReadyAt = -1f;
        biteExpireAt = -1f;

        // 早巻き・糸切れ関連もリセット
        startedReelBeforeBite = false;
        biteInvalidDueToEarlyReel = false;
        isReelingAfterBite = false;
        lineBroken = false;

        // プレビュー重み解除
        rod?.SetRarityWeightsOverride(null);
        SetLineActive(false);
    }

    // 通常失敗：自動巻き上げ
    private void TriggerFailAutoReel()
    {
        if (autoReelBack || isSinking) return;

        biteReady = false;
        biteReadyAt = -1f;
        biteExpireAt = -1f;

        // 早巻き・糸切れ関連もクリア
        startedReelBeforeBite = false;
        biteInvalidDueToEarlyReel = false;
        isReelingAfterBite = false;
        lineBroken = false; // 自動回収では糸は切れない

        ConsumeOnFailMulti();

        if (hookComp && hookComp.IsAnchored) hookComp.ReleaseAnchor();

        fullReel = false;
        autoReelBack = true;
        isSinking = false;

        if (hookRb)
        {
            hookRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            hookRb.gravityScale = 0f;
            hookRb.linearVelocity = Vector2.zero;
        }
    }

    // 糸を切る
    private void BreakLine()
    {
        if (lineBroken) return;
        lineBroken = true;

        // 見た目のラインを消す
        SetLineActive(false);

        // SFX
        if (lineBreakSfx)
        {
            sfxSource.volume = lineBreakVolume;
            sfxSource.PlayOneShot(lineBreakSfx);
        }
    }

    // 糸接触などの失敗：沈降モード
    private void TriggerFailSinkByLine()
    {
        if (isSinking) return;

        biteReady = false;
        biteReadyAt = -1f;
        biteExpireAt = -1f;

        ConsumeOnFailMulti();

        if (hookComp && hookComp.IsAnchored) hookComp.ReleaseAnchor();

        fullReel = false;
        autoReelBack = false;
        isSinking = true;
        sinkTimer = 0f;

        // 沈降開始の瞬間に糸を切る
        if (cutLineOnSink) BreakLine();

        if (hookRb)
        {
            hookRb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            hookRb.gravityScale = 1f;
            hookRb.linearVelocity = Vector2.down * Mathf.Max(0.1f, sinkSpeed);
        }

        // 早巻きフラグは明示的に無効化
        startedReelBeforeBite = false;
        biteInvalidDueToEarlyReel = false;
        isReelingAfterBite = false;
    }

    private void UpdateSinking()
    {
        sinkTimer += Time.deltaTime;
        if (sinkTimer >= sinkDestroyDelay)
        {
            if (currentHook) Destroy(currentHook);
            player?.SetMovementEnabled(true);
            state = State.Idle;

            biteReady = false; biteReadyAt = -1f;
            isSinking = false;
            autoReelBack = false;

            // 糸切れフラグはここでは復帰（次回キャストで描画できるように）
            lineBroken = false;

            SetLineActive(false);
        }
    }

    private void CheckBiteTimeout()
    {
        if (!biteReady) return;
        if (biteTimeoutSeconds <= 0f) return;
        if (biteExpireAt < 0f) return;

        // バイト後に巻いている間はタイムアウトしない（操作猶予）
        if (isReelingAfterBite) return;

        if (Time.time <= biteExpireAt) return;

        // タイムアウト：失敗→自動回収
        biteReady = false; biteReadyAt = -1f; biteExpireAt = -1f;
        if (escapeSfx) { sfxSource.volume = escapeSfxVolume; sfxSource.PlayOneShot(escapeSfx); }
        TriggerFailAutoReel();
    }

    /* ======================= Bite Toast（右上画像） ======================= */
    private void ShowBiteToast()
    {
        if (biteToast) Destroy(biteToast);

        var canvas = FindFirstObjectByType<Canvas>();
        if (!canvas) return;

        biteToast = new GameObject("BiteToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var rt = biteToast.GetComponent<RectTransform>();
        var img = biteToast.GetComponent<Image>();
        biteToastCg = biteToast.GetComponent<CanvasGroup>();

        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); // 右上
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = biteImageSize;
        rt.anchoredPosition = biteImageMargin;

        img.raycastTarget = false;
        img.sprite = biteSprite;
        img.preserveAspect = true;

        biteToastCg.alpha = 1f;
        biteToastHideAt = Time.time + biteImageDuration;
    }

    private void UpdateBiteToast()
    {
        if (!biteToast) return;

        if (Time.time >= biteToastHideAt)
        {
            Destroy(biteToast);
            biteToast = null; biteToastCg = null;
            return;
        }

        if (biteImageFadeOut > 0f && biteToastCg)
        {
            float tEnd = biteToastHideAt;
            float tFade = tEnd - biteImageFadeOut;
            if (Time.time >= tFade)
            {
                float k = Mathf.InverseLerp(tEnd, tFade, Time.time); // 1→0
                biteToastCg.alpha = Mathf.Clamp01(k);
            }
        }
    }

    /* ======================= 装備の適用（待機中の見かけ用） ======================= */
    private void ApplyEquippedPreviewToRod()
    {
        if (!rod) return;

        // 待機時は「何も装備なし」相当の緩いテーブルを設定（見かけ用）
        rod.SetBait(RodStats.BaitType.Lure);
        rod.SetRarityWeightsOverride(new Dictionary<FishRarity, int>{
            { FishRarity.Common,2 }, { FishRarity.Uncommon,1 },
            { FishRarity.Rare,0 }, { FishRarity.Epic,0 }, { FishRarity.Legendary,0 }
        });
    }

    /* ======================= 複数枠ヘルパ ======================= */

    private struct SlotItem { public int index; public ItemInstance item; }

    private List<SlotItem> GetEquippedBaitOrLures()
    {
        var list = new List<SlotItem>();
        var model = playerInventory ? playerInventory.Model : null;
        if (model == null) return list;

        for (int i = 0; i < Mathf.Max(1, baitOrLureSlots); i++)
        {
            int idx = baitOrLureBaseIndex + i;
            if (!model.InRange(idx)) break;
            var it = model.Get(idx);
            if (it != null) list.Add(new SlotItem { index = idx, item = it });
        }
        return list;
    }

    private Dictionary<FishRarity, int> BuildWeightsForItem(ItemInstance it)
    {
        if (it == null || it.def == null) return DefaultNoEquipWeights();

        if (it.def is BaitDefinition bd)
        {
            return bd.rarityWeights.IsAllZero() ? null : bd.rarityWeights.ToDict();
        }
        if (it.def is LureDefinition ld)
        {
            return ld.rarityWeights.IsAllZero() ? null : ld.rarityWeights.ToDict();
        }
        // 不明な定義はデフォルト
        return DefaultNoEquipWeights();
    }

    private Dictionary<FishRarity, int> DefaultNoEquipWeights()
    {
        // 何も装備なし：Common/Uncommon のみ
        return new Dictionary<FishRarity, int> {
            { FishRarity.Common,    2 },
            { FishRarity.Uncommon,  1 },
            { FishRarity.Rare,      0 },
            { FishRarity.Epic,      0 },
            { FishRarity.Legendary, 0 },
        };
    }

    private int GetMultiCatchCountSafe()
    {
        if (rod == null) return 1;

        // 1) メソッド GetMultiCatchCount()
        var m = rod.GetType().GetMethod("GetMultiCatchCount",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (m != null && m.ReturnType == typeof(int))
        {
            try { return Mathf.Max(1, (int)m.Invoke(rod, null)); } catch { }
        }

        // 2) プロパティ MultiCatchCount
        var p = rod.GetType().GetProperty("MultiCatchCount",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null && p.PropertyType == typeof(int))
        {
            try { return Mathf.Max(1, (int)p.GetValue(rod)); } catch { }
        }

        // 3) フィールド multiCatchCount
        var f = rod.GetType().GetField("multiCatchCount",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(int))
        {
            try { return Mathf.Max(1, (int)f.GetValue(rod)); } catch { }
        }

        return 1;
    }

    /* ============== 消費（成功/失敗） ============== */

    // 成功：各スロットの餌のみ1個ずつ
    private void ConsumeOnSuccessMulti(HashSet<int> baitSlotIndexes)
    {
        if (baitSlotIndexes == null || baitSlotIndexes.Count == 0) return;
        var model = playerInventory ? playerInventory.Model : null;
        if (model == null) return;

        foreach (var idx in baitSlotIndexes)
        {
            if (!model.InRange(idx)) continue;
            var it = model.Get(idx);
            if (it == null) continue;
            if ((it.Tags & ItemTags.Bait) == 0) continue;

            it.count = Mathf.Max(0, it.count - 1);
            model.Set(idx, it.count > 0 ? it : null);
        }
    }

    // 失敗：最初に見つかった装備枠から1個（餌 or ルアー）
    private void ConsumeOnFailMulti()
    {
        var model = playerInventory ? playerInventory.Model : null;
        if (model == null) return;

        for (int i = 0; i < Mathf.Max(1, baitOrLureSlots); i++)
        {
            int idx = baitOrLureBaseIndex + i;
            if (!model.InRange(idx)) break;
            var it = model.Get(idx);
            if (it == null) continue;

            it.count = Mathf.Max(0, it.count - 1);
            model.Set(idx, it.count > 0 ? it : null);
            break;
        }
    }

    /* ======================= リール/装備（従来） ======================= */

    private ItemInstance GetEquippedReel()
    {
        if (!playerInventory || playerInventory.Model == null) return null;
        if (!playerInventory.Model.InRange(reelSlotIndex)) return null;
        return playerInventory.Model.Get(reelSlotIndex);
    }
}

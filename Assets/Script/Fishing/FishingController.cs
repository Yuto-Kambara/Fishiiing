using UnityEngine;

/// <summary>
/// FishingController (Rod‑based 2025‑07)
///  ──────────────────────────────────────────
///  ✔ 出る位置 … Cast Offset (local)
///  ✔ 戻る速さ … Reel Speeds (水平 / 全方向)
///  ✔ 戻る位置 … Catch Offset (local) + Catch Tolerance
/// </summary>
public class FishingController : MonoBehaviour
{
    /*===== References =====*/
    [Header("References")]
    public PlayerController player;
    public Transform breakwater;

    /*===== Prefabs & Cast =====*/
    [Header("Prefabs & Cast")]
    public GameObject hookPrefab;
    public GameObject fishPrefab;
    public float castSpeed = 6f;

    [Header("Cast Offset (local)")]           // ★釣り竿ローカル
    public Vector2 castOffset = new(0f, -0.5f);   // 🆕Inspector で調整

    /*===== Reel Speeds =====*/
    [Header("Reel Speeds")]
    public float reelSpeedHorizontal = 4f;    // 🆕水平リール(アンカー状態)
    public float reelSpeedFull = 4f;          // 🆕XY 巻取り

    /*===== Catch Point =====*/
    [Header("Catch Offset (local)")]
    public Vector2 catchOffset = Vector2.zero;  // 🆕戻ってくる最終位置(ロッドローカル)

    [Tooltip("Catch Offset 付近に到達したとみなす許容距離")]
    public float catchTolerance = 0.08f;        // 🆕

    /*===== 判定距離 (変わらず) =====*/
    [Header("判定距離 (m)")]
    public float releaseDistance = 0.06f;
    public float maxDropDistance = 3f;

    /*===== 飛翔アーク設定 (変わらず) =====*/
    [Header("Arc Settings")]
    public float peakHeight = 1.5f;
    public float groundYOffset = 0.2f;
    public float fishSpawnYOff = 0.2f;

    /*===== Fail 沈没 =====*/
    [Header("Sinking")]
    public float sinkSpeed = 3f;
    public float sinkDestroyDelay = 2f;

    /*===== 内部 =====*/
    enum State { Idle, Casting, Fishing }
    State state = State.Idle;

    GameObject currentHook;
    Hook hookComp;
    Rigidbody2D hookRb;

    float rightEdgeX, leftEdgeX;
    float lastEdgeDist;
    bool fullReel = false, isSinking = false;
    float sinkTimer;

    /*================ Awake =================*/
    void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerController>();
        if (!breakwater) { Debug.LogError("Breakwater 未設定"); enabled = false; return; }

        var col = breakwater.GetComponent<BoxCollider2D>();
        float half = col.size.x * breakwater.lossyScale.x * 0.5f;
        rightEdgeX = breakwater.position.x + half;
        leftEdgeX = breakwater.position.x - half;
    }

    /*================ Update =================*/
    void Update()
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

    /*================ CAST =================*/
    void StartCast()
    {
        if (player && !player.IsAtRightEdge(0.15f)) return;

        player?.SetMovementEnabled(false);    // 移動ロック

        state = State.Casting;
        fullReel = false; isSinking = false;

        Vector3 origin = transform.TransformPoint(castOffset);          // 🆕
        currentHook = Instantiate(hookPrefab, origin, Quaternion.identity);
        hookRb = currentHook.GetComponent<Rigidbody2D>();
        hookComp = currentHook.GetComponent<Hook>();
        hookComp.Init(this);

        hookRb.linearVelocity = Vector2.right * castSpeed;
    }

    public void OnHookHitWater() => state = State.Fishing;

    /*================ FISHING =================*/
    void HandleFishing()
    {
        if (!currentHook) return;

        Vector2 hookPos = currentHook.transform.position;
        Vector2 rodTipPos = transform.position;
        float edgeDist = Mathf.Max(0f, hookPos.x - rightEdgeX);

        /*---- 水平リール中 ----*/
        if (!fullReel)
        {
            bool held = Input.GetKey(KeyCode.Space);
            bool up = Input.GetKeyUp(KeyCode.Space);

            if (held)
            {
                float dx = rodTipPos.x - hookPos.x;
                hookRb.linearVelocity = Mathf.Abs(dx) < 0.05f
                    ? Vector2.zero
                    : new Vector2(Mathf.Sign(dx) * reelSpeedHorizontal, 0f);   // 🆕

                if (edgeDist < releaseDistance) { TriggerFailSink(); return; }
            }
            else hookRb.linearVelocity = Vector2.zero;

            if (up)
            {
                if (edgeDist >= releaseDistance)
                {
                    lastEdgeDist = edgeDist;
                    SpawnAndThrowFish(hookPos);
                    hookComp.ReleaseAnchor();
                    fullReel = true;
                }
                else TriggerFailSink();
            }
        }
        /*---- XY 巻取り中 ----*/
        else
        {
            Vector2 catchWorld = rodTipPos + (Vector2)catchOffset;              // 🆕
            Vector2 dir = catchWorld - hookPos;

            if (dir.magnitude < catchTolerance) FinishReel();                   // 🆕
            else hookRb.linearVelocity = dir.normalized * reelSpeedFull;        // 🆕
        }
    }

    /*================ SUCCESS / FAIL =================*/
    void FinishReel()
    {
        if (currentHook)
        {
            hookRb.linearVelocity = Vector2.zero;
            Destroy(currentHook);
        }
        player?.SetMovementEnabled(true);
        state = State.Idle;
    }

    void TriggerFailSink()
    {
        if (isSinking) return;
        if (hookComp.IsAnchored) hookComp.ReleaseAnchor();

        fullReel = false; isSinking = true; sinkTimer = 0f;
        hookRb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        hookRb.gravityScale = 1f;
        hookRb.linearVelocity = Vector2.down * sinkSpeed;
    }

    void UpdateSinking()
    {
        sinkTimer += Time.deltaTime;
        if (sinkTimer >= sinkDestroyDelay)
        {
            Destroy(currentHook);
            player?.SetMovementEnabled(true);
            state = State.Idle;
        }
    }

    /*================ 魚投射 =================*/
    void SpawnAndThrowFish(Vector3 spawnPos)
    {
        if (!fishPrefab) return;

        float t = Mathf.InverseLerp(releaseDistance, maxDropDistance,
                                    Mathf.Clamp(lastEdgeDist, releaseDistance, maxDropDistance));
        float targetX = Mathf.Lerp(player.transform.position.x, leftEdgeX, t);
        float targetY = breakwater.position.y + groundYOffset;

        float g = Mathf.Abs(Physics2D.gravity.y);
        float peakY = Mathf.Max(spawnPos.y, targetY) + peakHeight;
        float vyUp = Mathf.Sqrt(2f * g * (peakY - spawnPos.y));
        float tUp = vyUp / g;
        float vyDown = Mathf.Sqrt(2f * g * (peakY - targetY));
        float tDown = vyDown / g;
        float totalT = tUp + tDown;
        float vx = (targetX - spawnPos.x) / totalT;

        Vector3 spawn = spawnPos + Vector3.up * fishSpawnYOff;
        var fish = Instantiate(fishPrefab, spawn, Quaternion.identity);
        if (fish.TryGetComponent(out Rigidbody2D rb))
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = new Vector2(vx, vyUp);
            rb.gravityScale = 1f;
        }
    }
}

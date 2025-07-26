using UnityEngine;

/// <summary>
/// FishingController（軽量：入力と状態遷移のみ）
/// ・生成／レア抽選／複数同時は FishSpawner に委譲
/// ・速度や同時数は RodStats から取得
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
    public float releaseDistance = 0.06f;

    [Header("Fail Sinking")]
    public float sinkSpeed = 3f;
    public float sinkDestroyDelay = 2f;

    enum State { Idle, Casting, Fishing }
    State state = State.Idle;

    GameObject currentHook;
    Hook hookComp;
    Rigidbody2D hookRb;

    float rightEdgeX;
    float lastEdgeDist;
    bool fullReel = false, isSinking = false;
    float sinkTimer;

    // 巻取り中の動的速度（レアリティによって変動）
    float currentFullReelSpeed;

    void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerController>();
        if (!rod) rod = FindFirstObjectByType<RodStats>();
        if (!spawner) spawner = FindFirstObjectByType<FishSpawner>();
        if (!breakwater) { Debug.LogError("[FishingController] Breakwater 未設定"); enabled = false; return; }

        var col = breakwater.GetComponent<BoxCollider2D>();
        float half = col.size.x * breakwater.lossyScale.x * 0.5f;
        rightEdgeX = breakwater.position.x + half;
    }

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

    void StartCast()
    {
        if (player && !player.IsAtRightEdge(0.15f)) return;

        player?.SetMovementEnabled(false);
        state = State.Casting;
        fullReel = false; isSinking = false;

        Vector3 origin = transform.TransformPoint(castOffset);
        currentHook = Instantiate(hookPrefab, origin, Quaternion.identity);
        hookRb = currentHook.GetComponent<Rigidbody2D>();
        hookComp = currentHook.GetComponent<Hook>();
        hookComp.Init(this);

        hookRb.linearVelocity = Vector2.right * castSpeed;
    }

    public void OnHookHitWater() => state = State.Fishing;

    void HandleFishing()
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

                if (edgeDist < releaseDistance) { TriggerFailSink(); return; }
            }
            else hookRb.linearVelocity = Vector2.zero;

            if (up)
            {
                if (edgeDist >= releaseDistance)
                {
                    lastEdgeDist = edgeDist;

                    // ★ 生成は Spawner に任せる（複数対応）
                    // 生成直後の箇所（HandleFishing 内の up 判定成功時）
                    var res = spawner
                        ? spawner.SpawnFromHook(hookPos, lastEdgeDist, releaseDistance)
                        : new FishSpawnResult(); // ★ default ではなく new で非 null に
                    currentFullReelSpeed = rod ? rod.GetFullReelSpeed(res.maxRarity) : 4f;

                    hookComp.ReleaseAnchor();
                    fullReel = true;
                }
                else TriggerFailSink();
            }
        }
        else
        {
            Vector2 catchWorld = (Vector2)transform.position + catchOffset;
            Vector2 dir = catchWorld - (Vector2)hookPos;

            if (dir.magnitude < catchTolerance) FinishReel();
            else hookRb.linearVelocity = dir.normalized * Mathf.Max(0.1f, currentFullReelSpeed);
        }
    }

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
}

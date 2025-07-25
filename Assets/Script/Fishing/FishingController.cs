using UnityEngine;

/// <summary>
/// FishingController (Rod‑based)
/// ・釣り竿オブジェクトにアタッチ
/// ・キャスト開始〜終了まで Player の移動をロック
/// </summary>
public class FishingController : MonoBehaviour
{
    /* ===== References ===== */
    [Header("References")]
    public PlayerController player;        // 必須
    public Transform breakwater;

    /* ===== Prefabs & Params ===== */
    [Header("Prefabs & Speeds")]
    public GameObject hookPrefab;
    public GameObject fishPrefab;
    public float castSpeed = 6f;
    public float reelSpeed = 4f;

    [Header("判定距離 (m)")]
    public float releaseDistance = 0.06f;
    public float maxDropDistance = 3f;
    public float catchDistance = 0.10f;
    public float castStartTolerance = 0.15f;

    [Header("Arc Settings")]
    public float peakHeight = 1.5f;
    public float groundYOffset = 0.2f;
    public float fishSpawnYOff = 0.2f;

    [Header("Sinking")]
    public float sinkSpeed = 3f;
    public float sinkDestroyDelay = 2f;

    [Header("Cast Offset (local)")]
    public Vector2 castOffset = new Vector2(0f, -0.5f);

    /* ===== Internal ===== */
    enum State { Idle, Casting, Fishing }
    State state = State.Idle;

    GameObject currentHook;
    Hook hookComp;
    Rigidbody2D hookRb;

    float rightEdgeX, leftEdgeX;
    float lastEdgeDist;
    bool fullReel = false, isSinking = false;
    float sinkTimer;

    /* ------------------------------------------------------------ */
    private void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerController>();
        if (!breakwater) { Debug.LogError("Breakwater 未設定"); enabled = false; return; }

        var col = breakwater.GetComponent<BoxCollider2D>();
        float half = col.size.x * breakwater.lossyScale.x * 0.5f;
        rightEdgeX = breakwater.position.x + half;
        leftEdgeX = breakwater.position.x - half;
    }

    /* ------------------------------------------------------------ */
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
        if (player && !player.IsAtRightEdge(castStartTolerance)) return;

        /* ---- 移動ロック ---- */
        player?.SetMovementEnabled(false);

        state = State.Casting;
        fullReel = false;
        isSinking = false;

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
                hookRb.linearVelocity =
                    Mathf.Abs(dx) < 0.05f ? Vector2.zero
                                          : new Vector2(Mathf.Sign(dx) * reelSpeed, 0f);

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
        else
        {
            Vector2 dir = rodTipPos - hookPos;
            if (dir.magnitude < catchDistance) FinishReel();
            else hookRb.linearVelocity = dir.normalized * reelSpeed;
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
        player?.SetMovementEnabled(true);     // ★移動アンロック
        state = State.Idle;
    }

    private void TriggerFailSink()
    {
        if (isSinking) return;
        if (hookComp.IsAnchored) hookComp.ReleaseAnchor();

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
            Destroy(currentHook);
            player?.SetMovementEnabled(true); // ★移動アンロック
            state = State.Idle;
        }
    }

    /* ======================= FISH SPAWN ======================= */
    private void SpawnAndThrowFish(Vector3 spawnPos)
    {
        if (!fishPrefab) return;

        float t = Mathf.InverseLerp(releaseDistance, maxDropDistance,
                                    Mathf.Clamp(lastEdgeDist, releaseDistance, maxDropDistance));
        float targetX = Mathf.Lerp(player.transform.position.x, leftEdgeX, t);
        float targetY = breakwater.position.y + groundYOffset;

        float peakY = Mathf.Max(spawnPos.y, targetY) + peakHeight;
        float g = Mathf.Abs(Physics2D.gravity.y);
        float vy_up = Mathf.Sqrt(2f * g * (peakY - spawnPos.y));
        float t_up = vy_up / g;
        float vy_down = Mathf.Sqrt(2f * g * (peakY - targetY));
        float t_down = vy_down / g;
        float totalT = t_up + t_down;
        float vx = (targetX - spawnPos.x) / totalT;

        Vector3 spawn = spawnPos + Vector3.up * fishSpawnYOff;
        var fish = Instantiate(fishPrefab, spawn, Quaternion.identity);
        if (fish.TryGetComponent(out Rigidbody2D rb))
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = new Vector2(vx, vy_up);
            rb.gravityScale = 1f;
        }
    }
}

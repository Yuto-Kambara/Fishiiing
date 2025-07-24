using UnityEngine;

/// <summary>
/// FishingController  (Unity 2023.2+ ― linearVelocity API)
/// ───────────────────────────────────────────────────────────
/// 右側の堤防から釣ることを前提に、
///   ▸ Space 長押し   : ルアーを水平 (X) に寄せる
///   ▸ Space を離す   : 「堤防端から releaseDistance 以上」なら成功 → XY 巻取り開始  
///                      それ未満で離した、または離さず突入  → 失敗 (その場で沈没)
///   ▸ XY 巻取り完了 : プレイヤー直下 (catchDistance) で回収
/// -----------------------------------------------------------
/// * 堤防端 = 〈堤防中心 X〉 + 〈堤防幅 * 0.5〉
///   （Transform と BoxCollider2D の size.x を組み合わせて算出）
/// * 釣りは堤防 **右側のみ** で行う設計
/// </summary>
public class FishingController : MonoBehaviour
{
    /* === インスペクタ設定 === */
    [Header("Prefabs & Speeds")]
    public GameObject hookPrefab;
    public float castSpeed = 6f;   // 投げ初速
    public float reelSpeed = 4f;   // 巻取り速度 (X / XY 共通)

    [Header("判定距離 (m)")]
    public float releaseDistance = 0.06f;   // ★堤防端からココ以上離れていれば成功
    public float catchDistance = 0.10f;   // プレイヤー直下で回収

    [Header("失敗時の沈没挙動")]
    public float sinkSpeed = 3f;     // 下へ落ちる速度
    public float sinkDestroyDelay = 2f;     // 何秒後に消滅

    [Header("Scene Reference")]
    public Transform breakwater;            // 堤防の Transform
                                            // (BoxCollider2D 必須)

    [Header("Cast Offset (local)")]
    public Vector2 castOffset = new Vector2(0f, -0.5f);

    /* === 内部状態 === */
    enum State { Idle, Casting, Fishing }
    State state = State.Idle;

    GameObject currentHook;
    Hook hookComp;
    Rigidbody2D hookRb;

    float rightEdgeX;        // 堤防“右端”のワールド座標
    bool fullReel = false; // XY 巻取り中?
    bool isSinking = false; // 失敗沈没中?
    float sinkTimer;

    /* === 初期化 === */
    void Awake()
    {
        // 堤防の半幅 = BoxCollider2D.size.x * lossyScale.x * 0.5
        BoxCollider2D col = breakwater.GetComponent<BoxCollider2D>();
        if (!col)
        {
            Debug.LogError("Breakwater に BoxCollider2D が必要です。");
            enabled = false;
            return;
        }
        float halfWidth = col.size.x * breakwater.lossyScale.x * 0.5f;
        rightEdgeX = breakwater.position.x + halfWidth;   // 右端のみ使用
    }

    /* === 更新ループ === */
    void Update()
    {
        switch (state)
        {
            case State.Idle:
                if (Input.GetKeyDown(KeyCode.Space))
                    StartCast();
                break;

            case State.Casting:
                // 必要ならタイムアウト処理など
                break;

            case State.Fishing:
                if (isSinking) UpdateSinking();
                else HandleFishing();
                break;
        }
    }

    /* === キャスト === */
    void StartCast()
    {
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

    /* 水面ヒットコールバック */
    public void OnHookHitWater()
    {
        state = State.Fishing;   // アンカー状態
    }

    /* === 釣りフェーズ === */
    void HandleFishing()
    {
        if (!currentHook) return;

        Vector2 hookPos = currentHook.transform.position;
        Vector2 playerPos = transform.position;
        float edgeDist = Mathf.Max(0f, hookPos.x - rightEdgeX); // 右端からの水平距離

        /* --- アンカー中：Space 長押しで水平リール --- */
        if (!fullReel)
        {
            bool spaceHeld = Input.GetKey(KeyCode.Space);
            bool spaceUpThis = Input.GetKeyUp(KeyCode.Space);

            if (spaceHeld)
            {
                float dx = playerPos.x - hookPos.x;
                hookRb.linearVelocity = Mathf.Abs(dx) < 0.05f
                                      ? Vector2.zero
                                      : new Vector2(Mathf.Sign(dx) * reelSpeed, 0f);

                // releaseDistance 未満に突入しても離さなかった → 失敗
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

            /* Space を離した瞬間の判定 */
            if (spaceUpThis)
            {
                if (edgeDist >= releaseDistance)   // ★成功条件
                {
                    hookComp.ReleaseAnchor();      // Y 凍結解除
                    fullReel = true;               // XY 巻取りへ
                }
                else                               // 近すぎる位置で離した → 失敗
                {
                    TriggerFailSink();
                }
            }
        }
        /* --- XY 巻取り中 --- */
        else
        {
            Vector2 dir = playerPos - hookPos;
            if (dir.magnitude < catchDistance)      // プレイヤー直下
                FinishReel();
            else
                hookRb.linearVelocity = dir.normalized * reelSpeed;
        }
    }

    /* === SUCCESS === */
    void FinishReel()
    {
        hookRb.linearVelocity = Vector2.zero;
        Destroy(currentHook);
        state = State.Idle;
        Debug.Log("Reel SUCCESS");
    }

    /* === FAIL：その場で沈没 === */
    void TriggerFailSink()
    {
        if (isSinking) return;

        if (hookComp.IsAnchored) hookComp.ReleaseAnchor();

        fullReel = false;
        isSinking = true;
        sinkTimer = 0f;

        hookRb.constraints = RigidbodyConstraints2D.FreezePositionX
                              | RigidbodyConstraints2D.FreezeRotation;
        hookRb.gravityScale = 1f;
        hookRb.linearVelocity = Vector2.down * sinkSpeed;

        Debug.Log("Reel FAIL → sinking…");
    }

    void UpdateSinking()
    {
        sinkTimer += Time.deltaTime;
        if (sinkTimer >= sinkDestroyDelay)
        {
            Destroy(currentHook);
            state = State.Idle;
            Debug.Log("Sink complete – インスタンス破棄");
        }
    }
}

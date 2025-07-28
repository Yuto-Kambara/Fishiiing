using UnityEngine;

/// <summary>
/// PlayerController
///  ・←→ / A‑D で堤防上を移動 (左右端は Padding 制限)
///  ・魚 (Landed) に触れるとピックアップ（収納は魚自身の Trigger に任せる）
///  ・FishingController から移動ロック / アンロック可能
///  ・InventoryUIManager から UI 開閉に応じた移動モードを指示（横のみ許可/通常）
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    /* ===== Inspector ===== */
    [Header("Movement")]
    public float moveSpeed = 3f;
    public Transform breakwater;
    public float leftPadding = 0.05f;
    public float rightPadding = 0.30f;

    [Header("Carry")]
    public Transform carryAnchor;

    /* ===== UI Movement Gate ===== */
    public enum MovementUIMode { Closed, HorizontalOnly, Disabled } // Disabled は完全停止
    private MovementUIMode uiMovementMode = MovementUIMode.Closed;

    /* ===== Internal ===== */
    private float leftEdgeX, rightEdgeX;
    private FishProjectile heldFish;
    private bool movementEnabled = true; // 釣りなどのゲーム側ロック

    /*--------------------------------------------------------*/
    private void Awake()
    {
        // Rigidbody を Kinematic にして Trigger イベントを受ける
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 堤防端を算出
        if (!breakwater)
        {
            Debug.LogError("[PlayerController] Breakwater 未設定です。");
        }
        else
        {
            var col = breakwater.GetComponent<BoxCollider2D>();
            if (!col)
            {
                Debug.LogError("[PlayerController] Breakwater に BoxCollider2D が必要です。");
            }
            else
            {
                float half = col.size.x * breakwater.lossyScale.x * 0.5f;
                leftEdgeX = breakwater.position.x - half + leftPadding;
                rightEdgeX = breakwater.position.x + half - rightPadding;
            }
        }

        // 所持位置
        if (!carryAnchor)
        {
            carryAnchor = new GameObject("CarryAnchor").transform;
            carryAnchor.SetParent(transform);
            carryAnchor.localPosition = Vector3.zero;
        }
    }

    /*--------------------------------------------------------*/
    private void Update()
    {
        // 実効モードを取得（釣り等の完全ロックが最優先）
        var mode = GetEffectiveUIMode();

        if (mode != MovementUIMode.Disabled)
        {
            // このコントローラは元々「横移動のみ」なので、
            // Closed/HorizontalOnly いずれでも HandleMovement は同じ（横だけ適用）
            HandleMovement();
        }

        // 魚がクーラーボックスで Destroy されたら参照は自然に null になる
        if (heldFish == null)
        {
            heldFish = null;
        }
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(h) < 0.01f) return;

        Vector3 pos = transform.position;
        pos.x += h * moveSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, leftEdgeX, rightEdgeX);
        transform.position = pos;
    }

    /*--------------------------------------------------------*/
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 魚ピックアップのみ担当（収納は魚自身が行う）
        if (heldFish == null && other.TryGetComponent(out FishProjectile fish) && fish.Landed)
        {
            heldFish = fish;

            fish.PrepareForCarry();                // 物理→Trigger化
            fish.transform.SetParent(carryAnchor); // 手元に保持
            fish.transform.localPosition = Vector3.zero;
        }
    }

    /* ===== API ===== */
    /// <summary>釣りなどゲーム側の完全ロック / アンロック</summary>
    public void SetMovementEnabled(bool enable) => movementEnabled = enable;

    /// <summary>UI 開閉側からの移動モード指示（横のみ許可 / 通常）</summary>
    public void SetUIMovementMode(MovementUIMode mode) => uiMovementMode = mode;

    /// <summary>右端にいるか？（釣り開始の判定用）</summary>
    public bool IsAtRightEdge(float thr = 0.1f)
        => Mathf.Abs(transform.position.x - rightEdgeX) <= thr;

    /// <summary>釣り等のロックを優先した実効モード</summary>
    private MovementUIMode GetEffectiveUIMode()
    {
        if (!movementEnabled) return MovementUIMode.Disabled; // 釣り中などは完全停止
        return uiMovementMode;                                 // それ以外はUIの指示に従う
    }
}

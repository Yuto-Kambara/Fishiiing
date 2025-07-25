using UnityEngine;

/// <summary>
/// PlayerController
///  ・←→ / A‑D で堤防上を移動 (左右端は Padding 制限)
///  ・魚 (Landed) に触れるとピックアップ
///  ・クーラーボックス収納は「魚 Trigger が触れた瞬間」に任せる
///  ・FishingController から移動ロック / アンロック可能
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

    /* ===== Internal ===== */
    private float leftEdgeX, rightEdgeX;
    private FishProjectile heldFish;
    private bool movementEnabled = true;

    /*--------------------------------------------------------*/
    private void Awake()
    {
        /* Rigidbody を Kinematic にして Trigger イベントを受ける */
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var col = breakwater.GetComponent<BoxCollider2D>();
        float half = col.size.x * breakwater.lossyScale.x * 0.5f;
        leftEdgeX = breakwater.position.x - half + leftPadding;
        rightEdgeX = breakwater.position.x + half - rightPadding;

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
        if (movementEnabled) HandleMovement();

        /* 魚がクーラーボックスで Destroy されたら参照をクリア */
        if (heldFish == null) heldFish = null;
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
    public void SetMovementEnabled(bool enable) => movementEnabled = enable;
    public bool IsAtRightEdge(float thr = 0.1f)
        => Mathf.Abs(transform.position.x - rightEdgeX) <= thr;
}

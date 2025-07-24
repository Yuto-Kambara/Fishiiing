using UnityEngine;

public class Hook : MonoBehaviour
{
    [Tooltip("水面よりどれだけ下で止めるか（単位: Unity単位 = メートル）")]
    public float stopDepth = 0.25f;

    Rigidbody2D rb;
    FishingController controller;

    bool anchored = false;
    float cachedGravity;
    RigidbodyConstraints2D cachedConstraints;

    public bool IsAnchored => anchored;

    public void Init(FishingController ctrl)
    {
        controller = ctrl;
        rb = GetComponent<Rigidbody2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (anchored) return;

        if (other.CompareTag("WaterSurface"))
        {
            AnchorBelowWater(other);
            controller?.OnHookHitWater();   // モード遷移は前回と同じ
        }
    }

    // ------------------------------------------------------------
    // 停止／解除ロジック
    // ------------------------------------------------------------
    void AnchorBelowWater(Collider2D water)
    {
        anchored = true;

        // 位置を水面から stopDepth 分だけ下げた所へ
        Vector3 p = transform.position;
        // water.bounds.max.y が水面の「上端」。そこから stopDepth 分下へ
        p.y = water.bounds.max.y - stopDepth;
        transform.position = p;

        // 慣性を残したい場合は velocity = Vector2.zero のみでも可
        rb.linearVelocity = Vector2.zero;

        // 後で戻せるように値をキャッシュ
        cachedGravity = rb.gravityScale;
        cachedConstraints = rb.constraints;

        rb.gravityScale = 0f;  // ぷかぷか浮くイメージ（重力ゼロ）
        rb.constraints =
            RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>プレイヤー操作などで再び動けるようにする</summary>
    public void ReleaseAnchor()
    {
        if (!anchored) return;

        anchored = false;
        rb.gravityScale = cachedGravity;
        rb.constraints = cachedConstraints;
    }
}

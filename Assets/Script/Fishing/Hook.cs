using UnityEngine;

public class Hook : MonoBehaviour
{
    [Tooltip("���ʂ��ǂꂾ�����Ŏ~�߂邩�i�P��: Unity�P�� = ���[�g���j")]
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
            controller?.OnHookHitWater();   // ���[�h�J�ڂ͑O��Ɠ���
        }
    }

    // ------------------------------------------------------------
    // ��~�^�������W�b�N
    // ------------------------------------------------------------
    void AnchorBelowWater(Collider2D water)
    {
        anchored = true;

        // �ʒu�𐅖ʂ��� stopDepth ����������������
        Vector3 p = transform.position;
        // water.bounds.max.y �����ʂ́u��[�v�B�������� stopDepth ������
        p.y = water.bounds.max.y - stopDepth;
        transform.position = p;

        // �������c�������ꍇ�� velocity = Vector2.zero �݂̂ł���
        rb.linearVelocity = Vector2.zero;

        // ��Ŗ߂���悤�ɒl���L���b�V��
        cachedGravity = rb.gravityScale;
        cachedConstraints = rb.constraints;

        rb.gravityScale = 0f;  // �Ղ��Ղ������C���[�W�i�d�̓[���j
        rb.constraints =
            RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>�v���C���[����ȂǂōĂѓ�����悤�ɂ���</summary>
    public void ReleaseAnchor()
    {
        if (!anchored) return;

        anchored = false;
        rb.gravityScale = cachedGravity;
        rb.constraints = cachedConstraints;
    }
}

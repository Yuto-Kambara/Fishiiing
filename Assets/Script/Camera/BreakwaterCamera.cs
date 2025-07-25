using UnityEngine;

/// <summary>
/// BreakwaterCamera
/// ------------------------------------------------------------
/// �t���[�F
///   ����������������������    ����������������������    ����������������������
///   �� Fixed    �� �� �� Follow  �� �� �� Clamped ��
///   ����������������������    ����������������������    ����������������������
/// �u�Œ� �� �Ǐ] �� �����E�Œ�~�v
/// 
/// 1.  �E�[�ł̒ނ�V�[������Ɍ����邽��
///     Player �� followStartX �ȏ�Ȃ�J���� X = fixedX
/// 2.  Player �� followStartX ��荶�ɓ�������Ǐ]�J�n
/// 3.  Player �� followStopX ��肳��ɍ��֍s������
///     �J������ followStopX �Ŏ~�܂�A����ȏ�͓����Ȃ�
/// </summary>
public class BreakwaterCamera : MonoBehaviour
{
    [Header("References")]
    public Transform player;              // PlayerController �� Transform

    [Header("Horizontal Settings")]
    [Tooltip("�v���C���[������ X ���E�ɂ���Ԃ̓J�����͌Œ�")]
    public float fixedX = 5.0f;

    [Tooltip("�v���C���[������ X ��荶�ɓ�������Ǐ]�J�n")]
    public float followStartX = 4.0f;

    [Tooltip("�J����������ȏ㍶�֓����Ȃ� X ���W")]
    public float followStopX = 0.0f;

    [Header("Vertical Offset (m)")]
    public float yOffset = 1.5f;          // �v���C���[��菭������f��

    [Header("Smoothing")]
    [Tooltip("�ړ��X���[�Y�� (�b)�B0 �ŃX�i�b�v�Ǐ]")]
    [Min(0f)]
    public float smoothTime = 0.15f;

    private Vector3 _velocity;            // SmoothDamp �p

    /*--------------------------------------------------------*/
    private void LateUpdate()
    {
        if (!player) return;

        Vector3 camPos = transform.position;
        float targetX;

        /*======= ��Ԕ��� =======*/
        if (player.position.x >= followStartX)
        {
            // ���� �ނ�G���A�F�J�����Œ�
            targetX = fixedX;
        }
        else
        {
            // ���� �Ǐ] or ���[�X�g�b�v
            targetX = Mathf.Max(followStopX, player.position.x);
        }

        /*======= �����ʒu�̓v���C���[ + yOffset =======*/
        float targetY = player.position.y + yOffset;

        /*======= ���f (�X���[�X or ����) =======*/
        Vector3 target = new Vector3(targetX, targetY, camPos.z);

        if (smoothTime > 0f)
            transform.position =
                Vector3.SmoothDamp(camPos, target, ref _velocity, smoothTime);
        else
            transform.position = target;
    }

#if UNITY_EDITOR
    /* Scene �r���[�ŃK�C�h��`��i�C�Ӂj */
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(fixedX, transform.position.y - 100, 0),
                        new Vector3(fixedX, transform.position.y + 100, 0));

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(followStartX, transform.position.y - 100, 0),
                        new Vector3(followStartX, transform.position.y + 100, 0));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(followStopX, transform.position.y - 100, 0),
                        new Vector3(followStopX, transform.position.y + 100, 0));
    }
#endif
}

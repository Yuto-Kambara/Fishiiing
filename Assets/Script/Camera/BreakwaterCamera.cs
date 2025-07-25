using UnityEngine;

/// <summary>
/// BreakwaterCamera
/// ------------------------------------------------------------
/// フロー：
///   ┌─────────┐    ┌─────────┐    ┌─────────┐
///   │ Fixed    │ → │ Follow  │ → │ Clamped │
///   └─────────┘    └─────────┘    └─────────┘
/// 「固定 → 追従 → 左限界で停止」
/// 
/// 1.  右端での釣りシーンを常に見せるため
///     Player が followStartX 以上ならカメラ X = fixedX
/// 2.  Player が followStartX より左に入ったら追従開始
/// 3.  Player が followStopX よりさらに左へ行ったら
///     カメラは followStopX で止まり、それ以上は動かない
/// </summary>
public class BreakwaterCamera : MonoBehaviour
{
    [Header("References")]
    public Transform player;              // PlayerController の Transform

    [Header("Horizontal Settings")]
    [Tooltip("プレイヤーがこの X より右にいる間はカメラは固定")]
    public float fixedX = 5.0f;

    [Tooltip("プレイヤーがこの X より左に入ったら追従開始")]
    public float followStartX = 4.0f;

    [Tooltip("カメラがこれ以上左へ動かない X 座標")]
    public float followStopX = 0.0f;

    [Header("Vertical Offset (m)")]
    public float yOffset = 1.5f;          // プレイヤーより少し上を映す

    [Header("Smoothing")]
    [Tooltip("移動スムーズさ (秒)。0 でスナップ追従")]
    [Min(0f)]
    public float smoothTime = 0.15f;

    private Vector3 _velocity;            // SmoothDamp 用

    /*--------------------------------------------------------*/
    private void LateUpdate()
    {
        if (!player) return;

        Vector3 camPos = transform.position;
        float targetX;

        /*======= 状態判定 =======*/
        if (player.position.x >= followStartX)
        {
            // ── 釣りエリア：カメラ固定
            targetX = fixedX;
        }
        else
        {
            // ── 追従 or 左端ストップ
            targetX = Mathf.Max(followStopX, player.position.x);
        }

        /*======= 垂直位置はプレイヤー + yOffset =======*/
        float targetY = player.position.y + yOffset;

        /*======= 反映 (スムース or 即時) =======*/
        Vector3 target = new Vector3(targetX, targetY, camPos.z);

        if (smoothTime > 0f)
            transform.position =
                Vector3.SmoothDamp(camPos, target, ref _velocity, smoothTime);
        else
            transform.position = target;
    }

#if UNITY_EDITOR
    /* Scene ビューでガイドを描画（任意） */
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

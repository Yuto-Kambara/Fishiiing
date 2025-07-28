using UnityEngine;

/// <summary>
/// インベントリ UI の開閉管理
/// ・開始時は「プレイヤー／クーラー」両パネルを必ず非表示にする
/// ・クーラー近接時：P でも C でも「プレイヤー＆クーラー」を同時に開閉（両方開く/両方閉じる）
/// ・通常時：P=プレイヤー / C=クーラー を個別トグル（相互排他）
/// ・UI が開いている間はプレイヤーの移動を「横移動のみ」に制限
/// </summary>
public class InventoryUIManager : MonoBehaviour
{
    [Header("Targets")]
    public GameObject playerPanel;   // プレイヤーインベントリの UI ルート
    public GameObject coolerPanel;   // クーラーボックスインベントリの UI ルート

    [Header("Hotkeys")]
    public KeyCode playerToggleKey = KeyCode.P;
    public KeyCode coolerToggleKey = KeyCode.C;

    [Header("Proximity (Cooler Near)")]
    [Tooltip("この距離以内なら P でも C でも両方のインベントリを同時に開閉")]
    public float coolerOpenRadius = 2.5f;

    [Header("References (Auto-fill if empty)")]
    public PlayerController playerController;
    public Transform playerTransform;
    public Transform coolerTransform;

    private void Awake()
    {
        // 参照の自動補完
        if (!playerController) playerController = FindFirstObjectByType<PlayerController>();
        if (!playerTransform && playerController) playerTransform = playerController.transform;
        if (!coolerTransform)
        {
            var coolerBox = FindFirstObjectByType<CoolerBox>();
            if (coolerBox) coolerTransform = coolerBox.transform;
        }

        // ★ 開始時は必ずパネルを閉じる（シーン上でアクティブでも強制的に非表示へ）
        SetActiveSafe(playerPanel, false);
        SetActiveSafe(coolerPanel, false);

        // 開閉状態に応じた移動モード適用（この時点では Closed になる）
        ApplyPlayerGate();
    }

    private void Update()
    {
        bool nearCooler = IsNearCooler();

        if (Input.GetKeyDown(playerToggleKey) || Input.GetKeyDown(coolerToggleKey))
        {
            if (nearCooler)
            {
                // 近接時は P でも C でも両方同時トグル
                ToggleBothPanelsTogether();
            }
            else
            {
                // 通常時：P/C は個別トグル（相互排他）
                if (Input.GetKeyDown(playerToggleKey))
                {
                    TogglePanel(playerPanel, otherToClose: coolerPanel);
                }
                else if (Input.GetKeyDown(coolerToggleKey))
                {
                    TogglePanel(coolerPanel, otherToClose: playerPanel);
                }
            }

            ApplyPlayerGate();
        }
    }

    /* ---------- 内部 ---------- */

    private bool IsNearCooler()
    {
        if (!playerTransform || !coolerTransform) return false;
        float sq = (playerTransform.position - coolerTransform.position).sqrMagnitude;
        return sq <= coolerOpenRadius * coolerOpenRadius;
    }

    /// <summary>
    /// 近接時：両方のインベントリを同じ状態にトグル
    ///  - どちらかが閉じていれば → 両方「開く」
    ///  - 両方開いていれば      → 両方「閉じる」
    /// </summary>
    private void ToggleBothPanelsTogether()
    {
        bool playerOpen = playerPanel && playerPanel.activeSelf;
        bool coolerOpen = coolerPanel && coolerPanel.activeSelf;

        bool next = !(playerOpen && coolerOpen); // どちらか閉じているなら開く、両方開なら閉じる

        SetActiveSafe(playerPanel, next);
        SetActiveSafe(coolerPanel, next);
    }

    /// <summary>
    /// 個別トグル（通常時）：target をトグルし、必要なら other を閉じる
    /// </summary>
    private void TogglePanel(GameObject target, GameObject otherToClose)
    {
        if (!target) return;
        bool next = !target.activeSelf;
        SetActiveSafe(target, next);

        // 相互排他：片方を開いたら他方は閉じる
        if (next && otherToClose) SetActiveSafe(otherToClose, false);
    }

    private void SetActiveSafe(GameObject go, bool active)
    {
        if (!go) return;
        if (go.activeSelf != active) go.SetActive(active);
    }

    /// <summary>
    /// UI 開閉状態に応じて、プレイヤーの移動モードを切り替える（横のみ or 通常）
    /// </summary>
    private void ApplyPlayerGate()
    {
        if (!playerController) return;

        bool anyOpen = (playerPanel && playerPanel.activeSelf) ||
                       (coolerPanel && coolerPanel.activeSelf);

        playerController.SetUIMovementMode(
            anyOpen ? PlayerController.MovementUIMode.HorizontalOnly
                    : PlayerController.MovementUIMode.Closed
        );
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!coolerTransform) return;
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.25f);
        Gizmos.DrawWireSphere(coolerTransform.position, coolerOpenRadius);
    }
#endif
}

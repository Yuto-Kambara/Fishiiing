using UnityEngine;

/// <summary>
/// �C���x���g�� UI �̊J�Ǘ�
/// �E�J�n���́u�v���C���[�^�N�[���[�v���p�l����K����\���ɂ���
/// �E�N�[���[�ߐڎ��FP �ł� C �ł��u�v���C���[���N�[���[�v�𓯎��ɊJ�i�����J��/��������j
/// �E�ʏ펞�FP=�v���C���[ / C=�N�[���[ ���ʃg�O���i���ݔr���j
/// �EUI ���J���Ă���Ԃ̓v���C���[�̈ړ����u���ړ��̂݁v�ɐ���
/// </summary>
public class InventoryUIManager : MonoBehaviour
{
    [Header("Targets")]
    public GameObject playerPanel;   // �v���C���[�C���x���g���� UI ���[�g
    public GameObject coolerPanel;   // �N�[���[�{�b�N�X�C���x���g���� UI ���[�g

    [Header("Hotkeys")]
    public KeyCode playerToggleKey = KeyCode.P;
    public KeyCode coolerToggleKey = KeyCode.C;

    [Header("Proximity (Cooler Near)")]
    [Tooltip("���̋����ȓ��Ȃ� P �ł� C �ł������̃C���x���g���𓯎��ɊJ��")]
    public float coolerOpenRadius = 2.5f;

    [Header("References (Auto-fill if empty)")]
    public PlayerController playerController;
    public Transform playerTransform;
    public Transform coolerTransform;

    private void Awake()
    {
        // �Q�Ƃ̎����⊮
        if (!playerController) playerController = FindFirstObjectByType<PlayerController>();
        if (!playerTransform && playerController) playerTransform = playerController.transform;
        if (!coolerTransform)
        {
            var coolerBox = FindFirstObjectByType<CoolerBox>();
            if (coolerBox) coolerTransform = coolerBox.transform;
        }

        // �� �J�n���͕K���p�l�������i�V�[����ŃA�N�e�B�u�ł������I�ɔ�\���ցj
        SetActiveSafe(playerPanel, false);
        SetActiveSafe(coolerPanel, false);

        // �J��Ԃɉ������ړ����[�h�K�p�i���̎��_�ł� Closed �ɂȂ�j
        ApplyPlayerGate();
    }

    private void Update()
    {
        bool nearCooler = IsNearCooler();

        if (Input.GetKeyDown(playerToggleKey) || Input.GetKeyDown(coolerToggleKey))
        {
            if (nearCooler)
            {
                // �ߐڎ��� P �ł� C �ł����������g�O��
                ToggleBothPanelsTogether();
            }
            else
            {
                // �ʏ펞�FP/C �͌ʃg�O���i���ݔr���j
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

    /* ---------- ���� ---------- */

    private bool IsNearCooler()
    {
        if (!playerTransform || !coolerTransform) return false;
        float sq = (playerTransform.position - coolerTransform.position).sqrMagnitude;
        return sq <= coolerOpenRadius * coolerOpenRadius;
    }

    /// <summary>
    /// �ߐڎ��F�����̃C���x���g���𓯂���ԂɃg�O��
    ///  - �ǂ��炩�����Ă���� �� �����u�J���v
    ///  - �����J���Ă����      �� �����u����v
    /// </summary>
    private void ToggleBothPanelsTogether()
    {
        bool playerOpen = playerPanel && playerPanel.activeSelf;
        bool coolerOpen = coolerPanel && coolerPanel.activeSelf;

        bool next = !(playerOpen && coolerOpen); // �ǂ��炩���Ă���Ȃ�J���A�����J�Ȃ����

        SetActiveSafe(playerPanel, next);
        SetActiveSafe(coolerPanel, next);
    }

    /// <summary>
    /// �ʃg�O���i�ʏ펞�j�Ftarget ���g�O�����A�K�v�Ȃ� other �����
    /// </summary>
    private void TogglePanel(GameObject target, GameObject otherToClose)
    {
        if (!target) return;
        bool next = !target.activeSelf;
        SetActiveSafe(target, next);

        // ���ݔr���F�Е����J�����瑼���͕���
        if (next && otherToClose) SetActiveSafe(otherToClose, false);
    }

    private void SetActiveSafe(GameObject go, bool active)
    {
        if (!go) return;
        if (go.activeSelf != active) go.SetActive(active);
    }

    /// <summary>
    /// UI �J��Ԃɉ����āA�v���C���[�̈ړ����[�h��؂�ւ���i���̂� or �ʏ�j
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

using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ��h���[�̃V���b�v�S��NPC
/// �ETrigger���� E �ŃV���b�v�J��
/// �E�I�v�V����: Breakwater �̍��[�֎����X�i�b�v
/// �E�V���b�v�J�ɍ��킹�ăv���C���[�̈ړ����u���̂݁v�ɐ���/����
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ShopNPC : MonoBehaviour
{
    [Header("References")]
    public GridShopUI shopUI;                 // �V���b�vUI�i�p�l���̃��[�g�j
    public PlayerController player;             // �v���C���[�i�ړ��Q�[�g�p�j
    public Transform breakwater;                // ��h�iBoxCollider2D �K�{�j

    [Header("Interact")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Trigger ����o���玩���ŃV���b�v�����")]
    public bool autoCloseOnExit = false;

    [Header("Snap to Left Edge")]
    public bool snapToLeftEdgeOnStart = true;
    [Tooltip("���[����̃I�t�Z�b�g(+�ŉE��)")]
    public float edgeOffsetX = 0.5f;
    [Tooltip("NPC��Y���W��n�ʂɍ��킹��I�t�Z�b�g")]
    public float yOffset = 0f;

    [Header("Panels (optional)")]
    public GameObject playerInventoryPanel; // �v���C���[�̃C���x���g�� UI ���[�g�i���ׂĕ\������j

    [Header("Prompt UI (optional)")]
    [Tooltip("�ڋߎ������\�������������o��/�L�[�ē��Ȃ�")]
    public GameObject talkPrompt; // �C�ӁB�w�莞�̂ݎ����\��/��\��

    private bool playerInside = false;

    private void Reset()
    {
        // Trigger�ݒ��Rigidbody2D��Kinematic��
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerController>();
        if (!shopUI) shopUI = FindFirstObjectByType<GridShopUI>();
        if (!breakwater)
        {
            var bw = GameObject.FindFirstObjectByType<BoxCollider2D>();
            if (bw) breakwater = bw.transform;
        }

        if (talkPrompt) talkPrompt.SetActive(false);

        if (snapToLeftEdgeOnStart && breakwater)
            SnapToLeftEdge();
    }

    private void Update()
    {
        if (!playerInside) return;
        if (!shopUI) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (shopUI.IsOpen) CloseShop();
            else OpenShop();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            if (talkPrompt) talkPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            if (talkPrompt) talkPrompt.SetActive(false);

            if (autoCloseOnExit && shopUI && shopUI.IsOpen)
                CloseShop();
        }
    }

    private void OpenShop()
    {
        if (!shopUI) return;
        // ���ׂĕ\���i����Open�j
        if (playerInventoryPanel) playerInventoryPanel.SetActive(true);

        shopUI.Open();
        if (player) player.SetUIMovementMode(PlayerController.MovementUIMode.HorizontalOnly);
    }

    private void CloseShop()
    {
        if (!shopUI) return;
        shopUI.Close();

        // �v���C���[�C���x���g���͕��Ȃ��^�p�����邪�A�K�v�Ȃ����
        if (playerInventoryPanel) playerInventoryPanel.SetActive(false);

        if (player) player.SetUIMovementMode(PlayerController.MovementUIMode.Closed);
    }

    private void SnapToLeftEdge()
    {
        var col = breakwater.GetComponent<BoxCollider2D>();
        if (!col) { Debug.LogWarning("[ShopNPC] Breakwater �� BoxCollider2D ���K�v�ł�"); return; }

        float half = col.size.x * breakwater.lossyScale.x * 0.5f;
        float leftX = breakwater.position.x - half; // ��h���[
        Vector3 pos = transform.position;
        pos.x = leftX + edgeOffsetX;
        pos.y = breakwater.position.y + yOffset;
        transform.position = pos;
    }
}

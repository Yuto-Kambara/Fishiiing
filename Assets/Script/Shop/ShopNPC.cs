using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 堤防左端のショップ担当NPC
/// ・Trigger内で E でショップ開閉
/// ・オプション: Breakwater の左端へ自動スナップ
/// ・ショップ開閉に合わせてプレイヤーの移動を「横のみ」に制限/解除
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ShopNPC : MonoBehaviour
{
    [Header("References")]
    public GridShopUI shopUI;                 // ショップUI（パネルのルート）
    public PlayerController player;             // プレイヤー（移動ゲート用）
    public Transform breakwater;                // 堤防（BoxCollider2D 必須）

    [Header("Interact")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Trigger から出たら自動でショップを閉じる")]
    public bool autoCloseOnExit = false;

    [Header("Snap to Left Edge")]
    public bool snapToLeftEdgeOnStart = true;
    [Tooltip("左端からのオフセット(+で右へ)")]
    public float edgeOffsetX = 0.5f;
    [Tooltip("NPCのY座標を地面に合わせるオフセット")]
    public float yOffset = 0f;

    [Header("Panels (optional)")]
    public GameObject playerInventoryPanel; // プレイヤーのインベントリ UI ルート（並べて表示する）

    [Header("Prompt UI (optional)")]
    [Tooltip("接近時だけ表示したい吹き出し/キー案内など")]
    public GameObject talkPrompt; // 任意。指定時のみ自動表示/非表示

    private bool playerInside = false;

    private void Reset()
    {
        // Trigger設定とRigidbody2DをKinematicに
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
        // 並べて表示（両方Open）
        if (playerInventoryPanel) playerInventoryPanel.SetActive(true);

        shopUI.Open();
        if (player) player.SetUIMovementMode(PlayerController.MovementUIMode.HorizontalOnly);
    }

    private void CloseShop()
    {
        if (!shopUI) return;
        shopUI.Close();

        // プレイヤーインベントリは閉じない運用もあるが、必要なら閉じる
        if (playerInventoryPanel) playerInventoryPanel.SetActive(false);

        if (player) player.SetUIMovementMode(PlayerController.MovementUIMode.Closed);
    }

    private void SnapToLeftEdge()
    {
        var col = breakwater.GetComponent<BoxCollider2D>();
        if (!col) { Debug.LogWarning("[ShopNPC] Breakwater に BoxCollider2D が必要です"); return; }

        float half = col.size.x * breakwater.lossyScale.x * 0.5f;
        float leftX = breakwater.position.x - half; // 堤防左端
        Vector3 pos = transform.position;
        pos.x = leftX + edgeOffsetX;
        pos.y = breakwater.position.y + yOffset;
        transform.position = pos;
    }
}

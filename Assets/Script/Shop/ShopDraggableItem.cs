using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ショップの1行（商品）をドラッグできるようにする
/// ・ドラッグ中はゴーストを生成して追従
/// ・ドロップ先（InventorySlotView）が購入処理を実行
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ShopDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ShopOffer offer;
    public SimpleShopUI shopUI;

    [Header("Drag Ghost")]
    public Canvas dragCanvas;               // 未設定なら最上位 Canvas を自動検出
    public float ghostAlpha = 0.85f;

    private GameObject ghost;
    private RectTransform ghostRt;

    private void Awake()
    {
        if (!dragCanvas)
        {
            // 最上位の Screen Space Canvas を拾う
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.isRootCanvas && (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera))
                { dragCanvas = c; break; }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!offer || !dragCanvas) return;

        // ゴースト生成（見た目はシンプルにアイコン＋名前＋価格）
        ghost = new GameObject("ShopGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        ghostRt = ghost.GetComponent<RectTransform>();
        ghostRt.SetParent(dragCanvas.transform, false);
        ghostRt.sizeDelta = new Vector2(260, 64);
        ghostRt.position = eventData.position;

        var bg = ghost.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);

        var cg = ghost.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false; // ドロップ先のレイを妨げない

        if (offer.Icon)
        {
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var rt = icon.GetComponent<RectTransform>();
            rt.SetParent(ghostRt, false);
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(8, 0);
            rt.sizeDelta = new Vector2(48, 48);
            icon.GetComponent<Image>().sprite = offer.Icon;
        }

        var txt = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var trt = txt.GetComponent<RectTransform>();
        trt.SetParent(ghostRt, false);
        trt.anchorMin = new Vector2(0, 0.5f);
        trt.anchorMax = new Vector2(0, 0.5f);
        trt.pivot = new Vector2(0, 0.5f);
        trt.anchoredPosition = new Vector2(64, 0);
        trt.sizeDelta = new Vector2(180, 48);
        var t = txt.GetComponent<Text>();
        t.text = $"{offer.DisplayName} x{offer.count}  ¥{offer.price}";
        t.color = new Color(1, 1, 1, ghostAlpha);
        t.alignment = TextAnchor.MiddleLeft;
        t.fontSize = 18;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostRt) ghostRt.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghost) Destroy(ghost);
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ShopGridItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Parts")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Image starTemplate;          // ★雛形（非表示で保持）

    [Header("Star Layout")]
    [Range(0.2f, 1.2f)]
    public float starOverlapFactor = 0.6f; // 1.0で隣接、0.6で40%重なり
    public float starExtraSpacing = 0f;    // 追加スペーシング（負でさらに重なる）

    [Header("Drag Ghost (Cursor Icon)")]
    public Canvas dragCanvas;
    public float ghostAlpha = .85f;
    public bool ghostMatchSourceSize = true;
    public Vector2 ghostIconSize = new Vector2(96, 96);
    public bool cursorGrabsCenter = true;   // true: カーソルが常にアイコン中心を掴む
    public Vector2 ghostOffset = Vector2.zero; // 追加オフセット（必要なら使用）

    /* 運搬用 */
    [HideInInspector] public ShopOffer Offer;     // GridShopUI が設定
    [HideInInspector] public GridShopUI OwnerUI;  // GridShopUI が設定

    Transform starsRoot; // 右下に置く星コンテナ

    #region visual setup
    public void Setup(ShopOffer offer, GridShopUI owner)
    {
        Offer = offer;
        OwnerUI = owner;

        if (iconImage) iconImage.sprite = offer.Icon;
        if (nameText) nameText.text = offer.DisplayName;
        if (priceText) priceText.text = $"¥{offer.price:N0}";

        // ★ ランク表示（右下・重ね配置）
        if (starTemplate)
        {
            if (!starsRoot)
            {
                var go = new GameObject("Stars", typeof(RectTransform));
                starsRoot = go.transform;
                var rt = (RectTransform)starsRoot;
                rt.SetParent(starTemplate.transform.parent, false);
                var tmplRt = starTemplate.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(1, 0); // 右下基準
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = tmplRt.anchoredPosition;
            }

            starTemplate.gameObject.SetActive(false);

            for (int i = starsRoot.childCount - 1; i >= 0; i--)
                Destroy(starsRoot.GetChild(i).gameObject);

            int rank = Mathf.Max(0, offer.item ? offer.item.rank : 0);
            if (rank > 0)
            {
                float baseWidth = starTemplate.rectTransform.sizeDelta.x;
                float stride = baseWidth * Mathf.Max(0f, starOverlapFactor) + starExtraSpacing;

                for (int i = 0; i < rank; i++)
                {
                    var star = Instantiate(starTemplate, starsRoot);
                    star.gameObject.SetActive(true);
                    star.enabled = true;

                    var srt = star.rectTransform;
                    srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(1, 0); // 右下基準
                    srt.anchoredPosition = new Vector2(-i * stride, 0f);
                    star.transform.SetAsLastSibling(); // 左側が手前
                }
            }
        }
    }
    #endregion

    #region drag purchase
    GameObject ghost;
    RectTransform ghostRt;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Offer == null) return;
        if (!dragCanvas)
            dragCanvas = OwnerUI ? OwnerUI.dragCanvas : GetComponentInParent<Canvas>();

        // 親キャンバス配下にゴースト作成（アイコンのみ）
        ghost = new GameObject("Ghost", typeof(RectTransform), typeof(CanvasGroup));
        ghostRt = ghost.GetComponent<RectTransform>();
        ghostRt.SetParent(dragCanvas.transform, false);

        // 中心を基準に配置
        ghostRt.anchorMin = ghostRt.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRt.pivot = new Vector2(0.5f, 0.5f);

        var cg = ghost.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false; // ドロップ先の判定を邪魔しない
        cg.alpha = ghostAlpha;

        // アイコン
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.SetParent(ghostRt, false);
        iconRt.pivot = new Vector2(0.5f, 0.5f);

        var icon = iconGO.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.sprite = (iconImage && iconImage.sprite) ? iconImage.sprite : Offer.Icon;

        // サイズ決定
        if (ghostMatchSourceSize && iconImage)
        {
            var r = iconImage.rectTransform.rect;
            iconRt.sizeDelta = new Vector2(Mathf.Max(1, r.width), Mathf.Max(1, r.height));
        }
        else
        {
            iconRt.sizeDelta = (ghostIconSize.sqrMagnitude > 0f) ? ghostIconSize : new Vector2(96, 96);
        }

        // 初期位置：カーソル中心（＋任意オフセット）
        SetGhostToPointer(eventData);
        if (!cursorGrabsCenter && ghostOffset != Vector2.zero)
            ghostRt.anchoredPosition += ghostOffset;

        ghost.transform.SetAsLastSibling(); // 最前面
    }

    public void OnDrag(PointerEventData eventData)
    {
        SetGhostToPointer(eventData);
        if (!cursorGrabsCenter && ghostOffset != Vector2.zero)
            ghostRt.anchoredPosition += ghostOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghost) Destroy(ghost);
    }

    // キャンバス種別に合わせてローカル座標に変換し、中心に吸着
    void SetGhostToPointer(PointerEventData eventData)
    {
        if (!ghostRt || !dragCanvas) return;

        var parentRt = dragCanvas.transform as RectTransform;
        Camera cam = null;
        if (dragCanvas.renderMode == RenderMode.ScreenSpaceCamera ||
            dragCanvas.renderMode == RenderMode.WorldSpace)
        {
            cam = dragCanvas.worldCamera;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRt, eventData.position, cam, out var localPos))
        {
            // アンカー(0.5,0.5)のため、anchoredPosition にそのまま入れると中心一致
            ghostRt.anchoredPosition = localPos;
        }
    }
    #endregion
}

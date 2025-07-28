using UnityEngine;
using UnityEngine.UI;
using System;

public class SimpleShopUI : MonoBehaviour
{
    [Header("Catalog & Target")]
    public ShopCatalog catalog;
    public InventoryBinder targetBinder;     // 参照は保持するが、購入はドロップ先スロットが決める

    [Header("UI Root")]
    public RectTransform contentParent;
    public GameObject panelRoot;

    [Header("Hotkeys")]
    public KeyCode closeKey = KeyCode.Escape;

    [Header("Style")]
    public Vector2 itemSize = new Vector2(560, 80);
    public int fontSize = 20;
    public Color priceColor = new Color(1f, 0.95f, 0.2f, 1f);

    public bool IsOpen => panelRoot ? panelRoot.activeSelf : gameObject.activeSelf;

    public event Action OnOpened;
    public event Action OnClosed;

    private void Awake()
    {
        if (!contentParent) contentParent = GetComponent<RectTransform>();
        BuildUI();
        Close();
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(closeKey)) Close();
    }

    public void Open()
    {
        if (panelRoot) panelRoot.SetActive(true); else gameObject.SetActive(true);
        OnOpened?.Invoke();
    }

    public void Close()
    {
        if (panelRoot) panelRoot.SetActive(false); else gameObject.SetActive(false);
        OnClosed?.Invoke();
    }

    private void BuildUI()
    {
        if (!catalog || catalog.offers == null || contentParent == null) return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        foreach (var offer in catalog.offers)
        {
            if (!offer || !offer.item) continue;
            CreateDraggableRow(offer);
        }
    }

    private void CreateDraggableRow(ShopOffer offer)
    {
        var row = new GameObject("Offer_" + offer.DisplayName, typeof(RectTransform), typeof(Image));
        var rt = row.GetComponent<RectTransform>();
        rt.SetParent(contentParent, false);
        rt.sizeDelta = itemSize;
        row.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.20f);

        // ドラッグ可能
        var drag = row.AddComponent<ShopDraggableItem>();
        drag.offer = offer;
        drag.shopUI = this;

        // アイコン
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.SetParent(rt, false);
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(10, 0);
        iconRt.sizeDelta = new Vector2(itemSize.y - 16, itemSize.y - 16);
        iconGO.GetComponent<Image>().sprite = offer.Icon;

        // 名前
        var nameGO = new GameObject("Name", typeof(RectTransform), typeof(Text));
        var nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.SetParent(rt, false);
        nameRt.anchorMin = new Vector2(0, 0.5f);
        nameRt.anchorMax = new Vector2(0, 0.5f);
        nameRt.pivot = new Vector2(0, 0.5f);
        nameRt.anchoredPosition = new Vector2(10 + (itemSize.y - 16) + 10, 0);
        var nameText = nameGO.GetComponent<Text>();
        nameText.text = offer.DisplayName;
        nameText.fontSize = fontSize;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.color = Color.white;
        nameRt.sizeDelta = new Vector2(itemSize.x * 0.45f, itemSize.y - 16);

        // 数量・価格
        var priceGO = new GameObject("Price", typeof(RectTransform), typeof(Text));
        var priceRt = priceGO.GetComponent<RectTransform>();
        priceRt.SetParent(rt, false);
        priceRt.anchorMin = new Vector2(1, 0.5f);
        priceRt.anchorMax = new Vector2(1, 0.5f);
        priceRt.pivot = new Vector2(1, 0.5f);
        priceRt.anchoredPosition = new Vector2(-12, 0);
        var priceText = priceGO.GetComponent<Text>();
        priceText.text = $"x{offer.count}   ¥{offer.price:N0}";
        priceText.fontSize = fontSize;
        priceText.alignment = TextAnchor.MiddleRight;
        priceText.color = priceColor;
        priceRt.sizeDelta = new Vector2(itemSize.x * 0.4f, itemSize.y - 16);
    }
}

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class GridShopUI : MonoBehaviour
{
    [Header("Catalog & Inventory")]
    public ShopCatalog catalog;
    public InventoryBinder playerBinder;

    [Header("Prefabs & Sprites")]
    public ShopGridItemView itemPrefab;
    public Sprite cellBackground;
    public Canvas dragCanvas;

    [Header("Layout")]
    public Vector2 shopSize = new(620, 900);
    public Vector2 cellSize = new(120, 120);
    public Vector2 cellSpacing = new(12, 16);

    [Header("Options")]
    public bool createBackground = false;   // 既定は背景なし

    RectTransform contentRt;
    Button upgradeButton;                   // ★ 追加：参照保持
    public bool IsOpen => gameObject.activeSelf;

    void Awake()
    {
        if (!playerBinder) playerBinder = FindFirstObjectByType<InventoryBinder>();
        BuildLayout();
        Close();
    }

    #region Build
    void BuildLayout()
    {
        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = shopSize;

        if (createBackground)
        {
            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, .96f);
            if (cellBackground) { bg.sprite = cellBackground; bg.type = Image.Type.Sliced; }
        }

        MakeText("Title", "ショップ", 48, new Vector2(.5f, 1), new Vector2(0, -40), transform);

        // --- 赤ボタン（マルチキャッチ強化） ---
        var btnGO = new GameObject("UpgradeButton",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(MultiCatchUpgradeButton));
        var btnRt = btnGO.GetComponent<RectTransform>();
        btnRt.SetParent(rt, false);
        btnRt.sizeDelta = new Vector2(shopSize.x - 80, 60);
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(.5f, 1);
        btnRt.pivot = new Vector2(.5f, 1);
        btnRt.anchoredPosition = new Vector2(0, -120);
        btnGO.GetComponent<Image>().color = new Color(.9f, .3f, .3f, 1);
        MakeText("BtnLabel", "一回で釣れる魚の数を\n増やすボタン", 22, new Vector2(.5f, .5f), Vector2.zero, btnRt);

        // MultiCatchUpgradeButton 設定
        var upBtn = btnGO.GetComponent<MultiCatchUpgradeButton>();
        upBtn.priceStep = 300;
        upBtn.playerBinder = playerBinder;

        // Button 参照保持
        upgradeButton = btnGO.GetComponent<Button>();
        upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);

        // --- 縦積みコンテンツ（左上揃え） ---
        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentRt = contentGO.GetComponent<RectTransform>();
        contentRt.SetParent(rt, false);
        contentRt.anchorMin = contentRt.anchorMax = new Vector2(.5f, 1);
        contentRt.pivot = new Vector2(.5f, 1);
        contentRt.anchoredPosition = new Vector2(0, -220);
        contentRt.sizeDelta = new Vector2(shopSize.x - 40, shopSize.y - 260);

        var v = contentGO.GetComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperLeft;
        v.spacing = 18;
        v.padding = new RectOffset(0, 0, 0, 0);

        CreateSection("リール", ItemTags.Reel);
        CreateSection("ルアー", ItemTags.Lure);
        CreateSection("餌", ItemTags.Bait);
    }

    // セクション生成（見出し＋左上揃え Grid）
    void CreateSection(string header, ItemTags tag)
    {
        if (catalog == null || catalog.offers == null) return;

        var targets = catalog.offers
            .Where(o => o && o.item && HasTag(o, tag))
            .ToList();
        if (targets.Count == 0) return;

        MakeText($"{header}Header", $"値段   {header}", 24,
                 new Vector2(0f, 1f), Vector2.zero, contentRt);

        var gridGO = new GameObject($"{header}Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        var gridRt = gridGO.GetComponent<RectTransform>();
        gridRt.SetParent(contentRt, false);
        gridRt.anchorMin = gridRt.anchorMax = new Vector2(0f, 1f);
        gridRt.pivot = new Vector2(0f, 1f);
        gridRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, 1);

        var grid = gridGO.GetComponent<GridLayoutGroup>();
        grid.cellSize = cellSize;
        grid.spacing = cellSpacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        foreach (var offer in targets)
        {
            var cell = Instantiate(itemPrefab, gridRt);
            var img = cell.GetComponent<Image>();
            if (img && cellBackground) img.sprite = cellBackground;
            cell.Setup(offer, this);
        }
    }
    #endregion

    /* ==================== Upgrade Button Logic ==================== */

    void OnUpgradeButtonClicked()
    {
        var pil = FindFirstObjectByType<PlayerInventoryLayout>();
        if (!pil)
        {
            Debug.LogWarning("[GridShopUI] PlayerInventoryLayout が見つかりません。");
            return;
        }

        var binder = pil.binder ? pil.binder : playerBinder;
        if (!binder)
        {
            Debug.LogWarning("[GridShopUI] InventoryBinder が見つかりません。");
            return;
        }

        // 追加後（+1）の必要枠が Capacity を超えるなら、このクリックでボタンを消す
        if (!HasRoomForAnotherBaitLureSlot(pil, binder, 1))
        {
            HideUpgradeButton();
            Debug.Log("[GridShopUI] スロット容量上限に達したため、アップグレードボタンを非表示にしました。");
            return; // スロット増設は行わない（容量オーバー）
        }

        // まだ余裕があるので +1 して再構築
        pil.IncreaseBaitLureSlots(1);

        // 増設後、次の+1がもう不可能ならボタンを消す
        UpdateUpgradeButtonVisibility();
    }

    // 次の +delta で容量超過になるか判定
    bool HasRoomForAnotherBaitLureSlot(PlayerInventoryLayout pil, InventoryBinder binder, int delta)
    {
        if (!pil || !binder) return true;

        int requiredAfter = pil.bottomCount            // 下段
                          + 1                          // リール枠
                          + (pil.baitOrLureSlots + delta); // 餌/ルアー枠（+delta）
        return requiredAfter <= binder.capacity;
    }

    // 現在の状態で「次の+1」が可能かを見てボタン表示を更新
    void UpdateUpgradeButtonVisibility()
    {
        if (!upgradeButton) return;
        var pil = FindFirstObjectByType<PlayerInventoryLayout>();
        var binder = pil && pil.binder ? pil.binder : playerBinder;

        bool canAddNext = pil && binder && HasRoomForAnotherBaitLureSlot(pil, binder, 1);
        upgradeButton.gameObject.SetActive(canAddNext);

    }

    void HideUpgradeButton()
    {
        if (upgradeButton) upgradeButton.gameObject.SetActive(false);
    }

    /* ==================== Helpers ==================== */

    bool HasTag(ShopOffer o, ItemTags tag)
    {
        var def = o.item as ItemDefinition;
        if (def == null) return false;
        return (def.tags & tag) != 0;
    }

    Text MakeText(string name, string txt, int size, Vector2 anchor, Vector2 pos, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent ? parent : transform, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(shopSize.x - 40, 60);
        var t = go.GetComponent<Text>();
        t.text = txt; t.fontSize = size; t.alignment = TextAnchor.MiddleLeft; t.color = Color.black;
        return t;
    }

    public void Open() => gameObject.SetActive(true);
    public void Close() => gameObject.SetActive(false);
}

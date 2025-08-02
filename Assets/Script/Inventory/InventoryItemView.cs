using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryItemView : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Binding")]
    public InventorySlotView ParentSlot;
    public ItemInstance Item;

    [Header("UI")]
    public Image icon;
    public TMP_Text countText;

    [Header("Star (Rank / Rarity)")]
    public Image starTemplate;                // 右下の★雛形（非表示で保持）
    [Range(0.2f, 1.2f)]
    public float starOverlapFactor = 0.6f;    // 1.0で隣接、0.6で40%重なり
    public float starExtraSpacing = 0f;       // 追加スペーシング（負でさらに重なる）

    private CanvasGroup _cg;
    private Transform _dragLayer;
    private Transform _originalParent;

    // internal
    Transform _starsRoot;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        _dragLayer = FindFirstObjectByType<Canvas>().transform;
    }

    // 優先順位：魚個体 > 定義アイコン
    public void Setup(InventorySlotView slot, ItemInstance item)
    {
        ParentSlot = slot;
        Item = item;

        Sprite s = null;
        if (item != null && item.fish.def != null && slot != null && slot.allowed.HasFlag(ItemTags.Fish))
            s = item.fish.def.sprite;  // 魚種の見た目を優先
        if (!s && item?.def) s = item.def.icon;
        if (icon) icon.sprite = s;

        if (countText) countText.text = (item != null && item.count > 1) ? item.count.ToString() : "";

        // ★数を算出：魚→レアリティ、その他→ItemDefinition.rank
        int starCount = GetStarCountForItem(item);
        UpdateStars(starCount);
    }

    int GetStarCountForItem(ItemInstance item)
    {
        if (item == null) return 0;

        // 魚：レアリティ→★数
        if ((item.Tags & ItemTags.Fish) != 0)
        {
            // 1) item.fish.rarity を試す
            int stars = TryExtractStarsFromRarityBoxed((object)item.fish);
            if (stars > 0) return stars;

            // 2) item.fish.def.rarity を試す
            var fishDef = item.fish.def;
            if (fishDef)
            {
                stars = TryExtractStarsFromRarityBoxed((object)fishDef);
                if (stars > 0) return stars;
            }

            // 3) 取れなければ定義 rank にフォールバック
            if (item.def) return Mathf.Clamp(item.def.rank, 0, 5);
            return 1; // 最低1つ表示したい場合は 1（不要なら 0）
        }

        // 非魚：ItemDefinition.rank
        if (item.def) return Mathf.Clamp(item.def.rank, 0, 5);
        return 0;
    }

    // obj（fish struct や fishDef ScriptableObject）の中から rarity を探して★数に変換
    int TryExtractStarsFromRarityBoxed(object obj)
    {
        if (obj == null) return 0;
        var t = obj.GetType();

        // rarity / Rarity を Property → Field の順で探索
        object rarityVal = null;

        var p = t.GetProperty("rarity", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             ?? t.GetProperty("Rarity", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null) rarityVal = p.GetValue(obj);

        if (rarityVal == null)
        {
            var f = t.GetField("rarity", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                 ?? t.GetField("Rarity", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) rarityVal = f.GetValue(obj);
        }

        if (rarityVal == null) return 0;

        // enum / int / string いずれでも対応
        // Common=1, Uncommon=2, Rare=3, Epic=4, Legendary=5 にマップ
        if (rarityVal is System.Enum)
        {
            return StarsFromRarityName(rarityVal.ToString());
        }
        if (rarityVal is int iVal)
        {
            // 0..4 を 1..5 にマップ（プロジェクト定義に合わせて調整可）
            return Mathf.Clamp(iVal + 1, 1, 5);
        }
        if (rarityVal is string sVal)
        {
            return StarsFromRarityName(sVal);
        }
        return 0;
    }

    int StarsFromRarityName(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        switch (name.ToLowerInvariant())
        {
            case "common": return 1;
            case "uncommon": return 2;
            case "rare": return 3;
            case "epic": return 4;
            case "legendary": return 5;
            default: return 0;
        }
    }

    void UpdateStars(int rankOrStars)
    {
        if (!starTemplate) return;

        // Stars コンテナ（右下アンカー）を用意
        if (!_starsRoot)
        {
            var go = new GameObject("Stars", typeof(RectTransform));
            _starsRoot = go.transform;
            var rt = (RectTransform)_starsRoot;
            rt.SetParent(starTemplate.transform.parent, false);

            var tmplRt = starTemplate.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0); // 右下基準
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = tmplRt.anchoredPosition;
        }

        // 雛形は非表示（複製元としてのみ使用）
        starTemplate.gameObject.SetActive(false);

        // 既存の星をクリア
        for (int i = _starsRoot.childCount - 1; i >= 0; i--)
            Destroy(_starsRoot.GetChild(i).gameObject);

        int rank = Mathf.Clamp(rankOrStars, 0, 5);
        if (rank <= 0) return;

        float baseWidth = starTemplate.rectTransform.sizeDelta.x;
        float stride = baseWidth * Mathf.Max(0f, starOverlapFactor) + starExtraSpacing;

        // rank 個だけ生成（右→左に重ねる：左が手前）
        for (int i = 0; i < rank; i++)
        {
            var star = Instantiate(starTemplate, _starsRoot);
            star.gameObject.SetActive(true);
            star.enabled = true;

            var srt = star.rectTransform;
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(1, 0); // 右下基準
            srt.anchoredPosition = new Vector2(-i * stride, 0f);
            star.transform.SetAsLastSibling();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;
        transform.SetParent(_dragLayer, true);
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _cg.blocksRaycasts = true;
        transform.SetParent(_originalParent, false);
        transform.localPosition = Vector3.zero;
    }
}

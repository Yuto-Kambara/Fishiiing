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
    public Image starTemplate;                // �E���́����`�i��\���ŕێ��j
    [Range(0.2f, 1.2f)]
    public float starOverlapFactor = 0.6f;    // 1.0�ŗאځA0.6��40%�d�Ȃ�
    public float starExtraSpacing = 0f;       // �ǉ��X�y�[�V���O�i���ł���ɏd�Ȃ�j

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

    // �D�揇�ʁF���� > ��`�A�C�R��
    public void Setup(InventorySlotView slot, ItemInstance item)
    {
        ParentSlot = slot;
        Item = item;

        Sprite s = null;
        if (item != null && item.fish.def != null && slot != null && slot.allowed.HasFlag(ItemTags.Fish))
            s = item.fish.def.sprite;  // ����̌����ڂ�D��
        if (!s && item?.def) s = item.def.icon;
        if (icon) icon.sprite = s;

        if (countText) countText.text = (item != null && item.count > 1) ? item.count.ToString() : "";

        // �������Z�o�F�������A���e�B�A���̑���ItemDefinition.rank
        int starCount = GetStarCountForItem(item);
        UpdateStars(starCount);
    }

    int GetStarCountForItem(ItemInstance item)
    {
        if (item == null) return 0;

        // ���F���A���e�B������
        if ((item.Tags & ItemTags.Fish) != 0)
        {
            // 1) item.fish.rarity ������
            int stars = TryExtractStarsFromRarityBoxed((object)item.fish);
            if (stars > 0) return stars;

            // 2) item.fish.def.rarity ������
            var fishDef = item.fish.def;
            if (fishDef)
            {
                stars = TryExtractStarsFromRarityBoxed((object)fishDef);
                if (stars > 0) return stars;
            }

            // 3) ���Ȃ���Β�` rank �Ƀt�H�[���o�b�N
            if (item.def) return Mathf.Clamp(item.def.rank, 0, 5);
            return 1; // �Œ�1�\���������ꍇ�� 1�i�s�v�Ȃ� 0�j
        }

        // �񋛁FItemDefinition.rank
        if (item.def) return Mathf.Clamp(item.def.rank, 0, 5);
        return 0;
    }

    // obj�ifish struct �� fishDef ScriptableObject�j�̒����� rarity ��T���ā����ɕϊ�
    int TryExtractStarsFromRarityBoxed(object obj)
    {
        if (obj == null) return 0;
        var t = obj.GetType();

        // rarity / Rarity �� Property �� Field �̏��ŒT��
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

        // enum / int / string ������ł��Ή�
        // Common=1, Uncommon=2, Rare=3, Epic=4, Legendary=5 �Ƀ}�b�v
        if (rarityVal is System.Enum)
        {
            return StarsFromRarityName(rarityVal.ToString());
        }
        if (rarityVal is int iVal)
        {
            // 0..4 �� 1..5 �Ƀ}�b�v�i�v���W�F�N�g��`�ɍ��킹�Ē����j
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

        // Stars �R���e�i�i�E���A���J�[�j��p��
        if (!_starsRoot)
        {
            var go = new GameObject("Stars", typeof(RectTransform));
            _starsRoot = go.transform;
            var rt = (RectTransform)_starsRoot;
            rt.SetParent(starTemplate.transform.parent, false);

            var tmplRt = starTemplate.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0); // �E���
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = tmplRt.anchoredPosition;
        }

        // ���`�͔�\���i�������Ƃ��Ă̂ݎg�p�j
        starTemplate.gameObject.SetActive(false);

        // �����̐����N���A
        for (int i = _starsRoot.childCount - 1; i >= 0; i--)
            Destroy(_starsRoot.GetChild(i).gameObject);

        int rank = Mathf.Clamp(rankOrStars, 0, 5);
        if (rank <= 0) return;

        float baseWidth = starTemplate.rectTransform.sizeDelta.x;
        float stride = baseWidth * Mathf.Max(0f, starOverlapFactor) + starExtraSpacing;

        // rank ���������i�E�����ɏd�˂�F������O�j
        for (int i = 0; i < rank; i++)
        {
            var star = Instantiate(starTemplate, _starsRoot);
            star.gameObject.SetActive(true);
            star.enabled = true;

            var srt = star.rectTransform;
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(1, 0); // �E���
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

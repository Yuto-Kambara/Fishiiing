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

    private CanvasGroup _cg;
    private Transform _dragLayer;
    private Transform _originalParent;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        // ドラッグ中に前面に出すレイヤ（Canvas 直下に空の RectTransform を用意して指定しておくのが楽）
        _dragLayer = FindFirstObjectByType<Canvas>().transform;
    }

    // InventoryItemView.Setup に追記（優先順位：魚個体 > 定義アイコン）
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

        // IDropHandler で成功しなかった場合は元に戻す
        transform.SetParent(_originalParent, false);
        transform.localPosition = Vector3.zero;
        // Binder 側がモデル更新イベントで再配置するので、ここではこれでOK
    }
}

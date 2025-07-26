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
        // �h���b�O���ɑO�ʂɏo�����C���iCanvas �����ɋ�� RectTransform ��p�ӂ��Ďw�肵�Ă����̂��y�j
        _dragLayer = FindFirstObjectByType<Canvas>().transform;
    }

    // InventoryItemView.Setup �ɒǋL�i�D�揇�ʁF���� > ��`�A�C�R���j
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

        // IDropHandler �Ő������Ȃ������ꍇ�͌��ɖ߂�
        transform.SetParent(_originalParent, false);
        transform.localPosition = Vector3.zero;
        // Binder �������f���X�V�C�x���g�ōĔz�u����̂ŁA�����ł͂����OK
    }
}

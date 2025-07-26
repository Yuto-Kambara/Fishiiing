using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, IDropHandler
{
    [Header("Binding")]
    public InventoryBinder binder;  // �����C���x���g��
    public int slotIndex;

    [Header("Accept Rule")]
    public ItemTags allowed = ItemTags.All; // �X���b�g���Ƃ̎󂯓������

    [Header("Visual")]
    public Image highlight;

    public InventoryModel Model => binder ? binder.Model : null;

    private void OnEnable()
    {
        if (binder) binder.RegisterSlot(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        var itemView = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<InventoryItemView>() : null;
        if (itemView == null) return;

        // �ړ����s
        bool ok = InventoryService.TryMove(
            itemView.ParentSlot.Model, itemView.ParentSlot.slotIndex, itemView.Item?.Tags ?? ItemTags.None,
            this.Model, this.slotIndex, this.allowed
        );

        // ��������UI���C�x���g�w�ǂōX�V����܂��iInventoryBinder �����f�j
        if (!ok)
        {
            // ���s �� ���̈ʒu�֖߂��̂� InventoryItemView ���ɔC����
        }
    }
}

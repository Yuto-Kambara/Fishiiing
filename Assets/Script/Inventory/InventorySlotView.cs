using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, IDropHandler
{
    [Header("Binding")]
    public InventoryBinder binder;  // 所属インベントリ
    public int slotIndex;

    [Header("Accept Rule")]
    public ItemTags allowed = ItemTags.All; // スロットごとの受け入れ条件

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

        // 移動試行
        bool ok = InventoryService.TryMove(
            itemView.ParentSlot.Model, itemView.ParentSlot.slotIndex, itemView.Item?.Tags ?? ItemTags.None,
            this.Model, this.slotIndex, this.allowed
        );

        // 成功時はUIがイベント購読で更新されます（InventoryBinder が反映）
        if (!ok)
        {
            // 失敗 → 元の位置へ戻すのは InventoryItemView 側に任せる
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, IDropHandler
{
    [Header("Binding")]
    public InventoryBinder binder;
    public int slotIndex;

    [Header("Accept Rule")]
    public ItemTags allowed = ItemTags.All;

    [Header("Visual")]
    public Image highlight;

    public InventoryModel Model => binder ? binder.Model : null;

    public void Initialize(InventoryBinder owner, int index, ItemTags allowedMask, bool ignoreLayout = false)
    {
        binder = owner;
        slotIndex = index;
        allowed = allowedMask;

        if (ignoreLayout)
        {
            var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        if (binder) binder.RegisterSlot(this);
    }

    private void OnEnable()
    {
        if (!binder) binder = GetComponentInParent<InventoryBinder>();
        if (binder) binder.RegisterSlot(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!binder) return;

        // 1) 既存：インベントリ間の移動
        var itemView = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<InventoryItemView>() : null;
        if (itemView)
        {
            InventoryService.TryMove(
                itemView.ParentSlot.Model, itemView.ParentSlot.slotIndex, itemView.Item?.Tags ?? ItemTags.None,
                this.Model, this.slotIndex, this.allowed
            );
            return;
        }

        // 2) 新規：ショップからの購入ドロップ
        var shopDrag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<ShopGridItemView>() : null;
        if (shopDrag && shopDrag.Offer != null)
        {
            bool ok = ShopPurchaseService.TryPurchaseToSlot(binder, slotIndex, allowed, shopDrag.Offer);
            if (!ok)
            {
                // 失敗：スロットに入らずショップ側に残る（視覚的には元の行は動かない）
                Debug.Log("Purchase failed: not enough money or slot mismatch/overflow.");
            }
        }
    }
}

using UnityEngine;

/// <summary>
/// プレイヤーのインベントリUIに「フリー」「餌/ルアー」「リール」スロットの許容タグを自動設定する補助
/// ・Hierarchy 上で Slot(InventorySlotView) を配置済みとし、その配列を渡す想定
/// </summary>
public class PlayerInventoryBootstrap : MonoBehaviour
{
    public InventoryBinder binder;

    [Header("Slots (任意の並び)")]
    public InventorySlotView[] freeSlots;
    public InventorySlotView baitOrLureSlot;
    public InventorySlotView reelSlot;

    private void Start()
    {
        // 受け入れ条件をタグで設定
        foreach (var s in freeSlots) if (s) s.allowed = ItemTags.All;
        if (baitOrLureSlot) baitOrLureSlot.allowed = ItemTags.Bait | ItemTags.Lure;
        if (reelSlot) reelSlot.allowed = ItemTags.Reel;

        // binder.RegisterSlot は SlotView.OnEnable で呼ばれる前提
    }
}

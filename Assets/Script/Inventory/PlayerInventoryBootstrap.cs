using UnityEngine;

/// <summary>
/// �v���C���[�̃C���x���g��UI�Ɂu�t���[�v�u�a/���A�[�v�u���[���v�X���b�g�̋��e�^�O�������ݒ肷��⏕
/// �EHierarchy ��� Slot(InventorySlotView) ��z�u�ς݂Ƃ��A���̔z���n���z��
/// </summary>
public class PlayerInventoryBootstrap : MonoBehaviour
{
    public InventoryBinder binder;

    [Header("Slots (�C�ӂ̕���)")]
    public InventorySlotView[] freeSlots;
    public InventorySlotView baitOrLureSlot;
    public InventorySlotView reelSlot;

    private void Start()
    {
        // �󂯓���������^�O�Őݒ�
        foreach (var s in freeSlots) if (s) s.allowed = ItemTags.All;
        if (baitOrLureSlot) baitOrLureSlot.allowed = ItemTags.Bait | ItemTags.Lure;
        if (reelSlot) reelSlot.allowed = ItemTags.Reel;

        // binder.RegisterSlot �� SlotView.OnEnable �ŌĂ΂��O��
    }
}

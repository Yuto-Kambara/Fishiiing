using UnityEngine;

public static class InventoryService
{
    /// <summary>
    /// �X���b�g�̎󂯓�������iallowedTags�j�𖞂����Ȃ� move/swap�B
    /// ���� ItemDefinition ���� stackable �Ȃ�X�^�b�N���̂��\�B
    /// </summary>
    public static bool TryMove(
        InventoryModel fromInv, int fromIdx, ItemTags fromTags,
        InventoryModel toInv, int toIdx, ItemTags toAllowed)
    {
        if (fromInv == null || toInv == null) return false;
        if (!fromInv.InRange(fromIdx) || !toInv.InRange(toIdx)) return false;

        var item = fromInv.Get(fromIdx);
        if (item == null) return false;

        // �󂯓������
        if ((toAllowed & item.Tags) == 0)
        {
            Debug.Log("Slot rejects this item type.");
            return false;
        }

        var dst = toInv.Get(toIdx);

        // �X�^�b�N����
        if (dst != null && item.IsStackable && dst.IsStackable && dst.def == item.def)
        {
            int space = dst.def.maxStack - dst.count;
            if (space <= 0) return false;
            int move = Mathf.Min(space, item.count);
            dst.count += move;
            item.count -= move;

            if (item.count <= 0) fromInv.Set(fromIdx, null);
            else fromInv.Set(fromIdx, item);

            toInv.Set(toIdx, dst);
            return true;
        }

        // ����ȊO�͒P���X���b�v
        fromInv.Set(fromIdx, dst);
        toInv.Set(toIdx, item);
        return true;
    }
}

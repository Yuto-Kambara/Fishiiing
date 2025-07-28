using UnityEngine;

/// <summary>
/// �V���b�v�w���̃��[�e�B���e�B
/// �E�x�����ۃ`�F�b�N �� �X���b�g�K���`�F�b�N �� ���f
/// �E�x�������s/�K���s�Ȃ疳�ύX�i�V���b�v���Ɏc��j
/// </summary>
public static class ShopPurchaseService
{
    /// <summary>
    /// �w��X���b�g�֍w�����ē����i������ true�j
    /// </summary>
    public static bool TryPurchaseToSlot(InventoryBinder binder, int slotIndex, ItemTags slotAllowed, ShopOffer offer)
    {
        if (binder == null || binder.Model == null || offer == null || offer.item == null) return false;

        // �^�O����`�F�b�N
        var itemTags = offer.item.tags;
        if ((slotAllowed & itemTags) == 0) return false;

        // �x�����O�Ɂu���邩�v�����O�`�F�b�N
        if (!WillFitAt(binder, slotIndex, offer)) return false;

        // �x�������s
        var wallet = CurrencyManager.Instance;
        if (wallet == null) { Debug.LogWarning("[ShopPurchaseService] CurrencyManager.Instance ������܂���B"); return false; }
        if (!wallet.TrySpend(offer.price)) return false;

        // ���f�i���s���͕ԋ��j
        var inst = new ItemInstance(offer.item, offer.count);
        bool placed = binder.TryAddAt(slotIndex, inst, respectSlotRule: true);
        if (!placed)
        {
            wallet.AddMoney(offer.price); // �ԋ�
            return false;
        }
        return true;
    }

    private static bool WillFitAt(InventoryBinder binder, int index, ShopOffer offer)
    {
        var model = binder.Model;
        if (!model.InRange(index)) return false;

        var cur = model.Get(index);
        if (cur == null) return true;

        // ����X�^�b�N�\��
        if (!cur.IsStackable || !offer.item.stackable) return false;
        if (cur.def != offer.item) return false;

        int space = cur.def.maxStack - cur.count;
        return space >= offer.count;
    }
}

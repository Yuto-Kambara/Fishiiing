using UnityEngine;

/// <summary>
/// �V���b�v UI ��̏��i�r���[�i�h���b�O�\�j
/// �Eprice ��ێ����AInventorySlotView.OnDrop ���ŎQ�Ƃ��Č���
/// </summary>
public class ShopItemView : InventoryItemView
{
    [HideInInspector] public int price;     // �P�� (offer.price)
    [HideInInspector] public ShopOffer offerRef; // ���� Offer (�݌ɍX�V���������p)

    /// <summary>�V���b�v�������ɌĂ�</summary>
    public void SetupShop(InventorySlotView parent, ItemInstance item, int price, ShopOffer offer)
    {
        this.price = price;
        this.offerRef = offer;
        base.Setup(parent, item);
    }
}

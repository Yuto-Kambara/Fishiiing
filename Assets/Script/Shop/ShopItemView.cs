using UnityEngine;

/// <summary>
/// ショップ UI 上の商品ビュー（ドラッグ可能）
/// ・price を保持し、InventorySlotView.OnDrop 側で参照して決済
/// </summary>
public class ShopItemView : InventoryItemView
{
    [HideInInspector] public int price;     // 単価 (offer.price)
    [HideInInspector] public ShopOffer offerRef; // 元の Offer (在庫更新したい時用)

    /// <summary>ショップ生成時に呼ぶ</summary>
    public void SetupShop(InventorySlotView parent, ItemInstance item, int price, ShopOffer offer)
    {
        this.price = price;
        this.offerRef = offer;
        base.Setup(parent, item);
    }
}

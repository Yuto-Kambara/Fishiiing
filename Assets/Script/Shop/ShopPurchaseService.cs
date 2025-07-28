using UnityEngine;

/// <summary>
/// ショップ購入のユーティリティ
/// ・支払い可否チェック → スロット適合チェック → 反映
/// ・支払い失敗/適合不可なら無変更（ショップ側に残る）
/// </summary>
public static class ShopPurchaseService
{
    /// <summary>
    /// 指定スロットへ購入して入れる（成功で true）
    /// </summary>
    public static bool TryPurchaseToSlot(InventoryBinder binder, int slotIndex, ItemTags slotAllowed, ShopOffer offer)
    {
        if (binder == null || binder.Model == null || offer == null || offer.item == null) return false;

        // タグ制約チェック
        var itemTags = offer.item.tags;
        if ((slotAllowed & itemTags) == 0) return false;

        // 支払い前に「入るか」を事前チェック
        if (!WillFitAt(binder, slotIndex, offer)) return false;

        // 支払い実行
        var wallet = CurrencyManager.Instance;
        if (wallet == null) { Debug.LogWarning("[ShopPurchaseService] CurrencyManager.Instance がありません。"); return false; }
        if (!wallet.TrySpend(offer.price)) return false;

        // 反映（失敗時は返金）
        var inst = new ItemInstance(offer.item, offer.count);
        bool placed = binder.TryAddAt(slotIndex, inst, respectSlotRule: true);
        if (!placed)
        {
            wallet.AddMoney(offer.price); // 返金
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

        // 同種スタック可能か
        if (!cur.IsStackable || !offer.item.stackable) return false;
        if (cur.def != offer.item) return false;

        int space = cur.def.maxStack - cur.count;
        return space >= offer.count;
    }
}

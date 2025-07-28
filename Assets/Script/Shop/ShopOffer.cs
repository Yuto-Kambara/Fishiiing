using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Offer", fileName = "Offer_")]
public class ShopOffer : ScriptableObject
{
    [Header("What")]
    public ItemDefinition item;         // 販売するアイテム定義（Bait/Lure/Reelなど）
    [Min(1)] public int count = 1;      // 1回の購入で手に入る個数（餌は複数推奨）

    [Header("Price")]
    [Min(0)] public int price = 100;

    [Header("UI")]
    public string displayNameOverride;  // 空なら item.displayName
    public Sprite iconOverride;         // 空なら item.icon

    public string DisplayName => string.IsNullOrEmpty(displayNameOverride)
        ? (item ? item.displayName : "(null)")
        : displayNameOverride;

    public Sprite Icon => iconOverride ? iconOverride : (item ? item.icon : null);
}

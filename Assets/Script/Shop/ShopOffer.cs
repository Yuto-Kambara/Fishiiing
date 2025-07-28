using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Offer", fileName = "Offer_")]
public class ShopOffer : ScriptableObject
{
    [Header("What")]
    public ItemDefinition item;         // �̔�����A�C�e����`�iBait/Lure/Reel�Ȃǁj
    [Min(1)] public int count = 1;      // 1��̍w���Ŏ�ɓ�����i�a�͕��������j

    [Header("Price")]
    [Min(0)] public int price = 100;

    [Header("UI")]
    public string displayNameOverride;  // ��Ȃ� item.displayName
    public Sprite iconOverride;         // ��Ȃ� item.icon

    public string DisplayName => string.IsNullOrEmpty(displayNameOverride)
        ? (item ? item.displayName : "(null)")
        : displayNameOverride;

    public Sprite Icon => iconOverride ? iconOverride : (item ? item.icon : null);
}

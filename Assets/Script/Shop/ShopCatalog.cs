using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Catalog", fileName = "ShopCatalog")]
public class ShopCatalog : ScriptableObject
{
    public ShopOffer[] offers;
}

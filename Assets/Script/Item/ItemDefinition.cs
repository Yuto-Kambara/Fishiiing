using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Definition", fileName = "ItemDef_")]
public class ItemDefinition : ScriptableObject
{
    [Header("Basic")]
    public string displayName = "Item";
    public Sprite icon;
    public ItemTags tags = ItemTags.Misc;

    [Header("Stacking")]
    public bool stackable = true;
    [Min(1)] public int maxStack = 99;

    [Header("Fish (for itemizing a caught fish)")]
    public bool isFishItem = false;  // 魚アイテム用の共通定義か？
}

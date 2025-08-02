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

    [Header("Shop/Rank (UI 表示用)")]
    [Range(1, 5)] public int rank = 1;   // ★表示に使う（1〜5）

    [Header("Fish (for itemizing a caught fish)")]
    public bool isFishItem = false;  // 魚アイテム用の共通定義か？

    [Header("Economy")]
    [Tooltip("このアイテムを売れるかどうか")]
    public bool canSell = true;

    [Min(0)]
    [Tooltip("売値（通貨単位）。0 の場合は売れない扱いにするのが一般的です。")]
    public int sellPrice = 0;

    public bool IsSellable => canSell && sellPrice > 0;

    private void OnValidate()
    {
        if (maxStack < 1) maxStack = 1;
        rank = Mathf.Clamp(rank, 0, 5);
        if (sellPrice < 0) sellPrice = 0;
    }
}

using System;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    public ItemDefinition def;
    public int count = 1;

    // 魚の個体データ（Fish のときだけ使用）
    public FishInstance fish; // FishDefinition+長さ+売値など

    public ItemTags Tags => def ? def.tags : ItemTags.None;
    public bool IsStackable => def && def.stackable && fish.def == null; // 魚は基本非スタック

    public ItemInstance(ItemDefinition d, int c = 1)
    {
        def = d; count = Mathf.Max(1, c);
        fish = new FishInstance(); // 空
    }

    public static ItemInstance FromFish(FishInstance fishInst, ItemDefinition fishItemDef)
    {
        var item = new ItemInstance(fishItemDef, 1);
        item.fish = fishInst; // 個体情報を保持
        return item;
    }
}

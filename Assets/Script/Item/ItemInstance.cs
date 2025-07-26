using System;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    public ItemDefinition def;
    public int count = 1;

    // ���̌̃f�[�^�iFish �̂Ƃ������g�p�j
    public FishInstance fish; // FishDefinition+����+���l�Ȃ�

    public ItemTags Tags => def ? def.tags : ItemTags.None;
    public bool IsStackable => def && def.stackable && fish.def == null; // ���͊�{��X�^�b�N

    public ItemInstance(ItemDefinition d, int c = 1)
    {
        def = d; count = Mathf.Max(1, c);
        fish = new FishInstance(); // ��
    }

    public static ItemInstance FromFish(FishInstance fishInst, ItemDefinition fishItemDef)
    {
        var item = new ItemInstance(fishItemDef, 1);
        item.fish = fishInst; // �̏���ێ�
        return item;
    }
}

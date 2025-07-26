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

    [Header("Optional Fish Link (���A�C�e�����Ɏg��)")]
    public FishDefinition fishDef; // Fish ��p��`�������������ꍇ�i�C�Ӂj
}

using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Bait Definition", fileName = "ItemDef_Bait_")]
public class BaitDefinition : ItemDefinition
{
    [Header("Rarity Weights (���̉a�Œނ�₷�����A�x�̏d��)")]
    public RarityWeights rarityWeights = RarityWeights.Ones();

    private void OnValidate()
    {
        // �^�O�������� Bait ��
        if ((tags & ItemTags.Bait) == 0) tags |= ItemTags.Bait;
    }
}

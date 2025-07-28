using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Lure Definition", fileName = "ItemDef_Lure_")]
public class LureDefinition : ItemDefinition
{
    [Header("Rarity Weights (���̃��A�[�Œނ�₷�����A�x�̏d��)")]
    public RarityWeights rarityWeights = RarityWeights.Ones();

    private void OnValidate()
    {
        // �^�O�������� Lure ��
        if ((tags & ItemTags.Lure) == 0) tags |= ItemTags.Lure;
    }
}

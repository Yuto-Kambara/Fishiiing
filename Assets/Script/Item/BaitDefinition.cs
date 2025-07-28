using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Bait Definition", fileName = "ItemDef_Bait_")]
public class BaitDefinition : ItemDefinition
{
    [Header("Rarity Weights (この餌で釣れやすいレア度の重み)")]
    public RarityWeights rarityWeights = RarityWeights.Ones();

    private void OnValidate()
    {
        // タグを自動で Bait に
        if ((tags & ItemTags.Bait) == 0) tags |= ItemTags.Bait;
    }
}

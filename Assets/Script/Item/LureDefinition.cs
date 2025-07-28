using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Lure Definition", fileName = "ItemDef_Lure_")]
public class LureDefinition : ItemDefinition
{
    [Header("Rarity Weights (このルアーで釣れやすいレア度の重み)")]
    public RarityWeights rarityWeights = RarityWeights.Ones();

    private void OnValidate()
    {
        // タグを自動で Lure に
        if ((tags & ItemTags.Lure) == 0) tags |= ItemTags.Lure;
    }
}

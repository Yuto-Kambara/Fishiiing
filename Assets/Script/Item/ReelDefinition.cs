using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Reel Definition", fileName = "ItemDef_Reel_")]
public class ReelDefinition : ItemDefinition
{
    [Header("Reel Speed Multipliers (�搔)")]
    [Tooltip("�A���J�[���̐������[�����x�Ɋ|����搔")]
    public float horizontalSpeedMul = 1f;

    [Tooltip("XY ����葬�x�Ɋ|����x�[�X�搔")]
    public float fullReelSpeedMul = 1f;

    [Header("Per-Rarity Multipliers (�C��)")]
    [Tooltip("���A�x���Ƃɒǉ��Ŋ|����搔�i���g�p�Ȃ�1�̂܂܂�OK�j")]
    public float mulCommon = 1f;
    public float mulUncommon = 1f;
    public float mulRare = 1f;
    public float mulEpic = 1f;
    public float mulLegendary = 1f;

    private void OnValidate()
    {
        if ((tags & ItemTags.Reel) == 0) tags |= ItemTags.Reel;
        horizontalSpeedMul = Mathf.Max(0.01f, horizontalSpeedMul);
        fullReelSpeedMul = Mathf.Max(0.01f, fullReelSpeedMul);
        mulCommon = Mathf.Max(0.01f, mulCommon);
        mulUncommon = Mathf.Max(0.01f, mulUncommon);
        mulRare = Mathf.Max(0.01f, mulRare);
        mulEpic = Mathf.Max(0.01f, mulEpic);
        mulLegendary = Mathf.Max(0.01f, mulLegendary);
    }

    public float GetRarityMul(FishRarity r)
    {
        switch (r)
        {
            case FishRarity.Common: return mulCommon;
            case FishRarity.Uncommon: return mulUncommon;
            case FishRarity.Rare: return mulRare;
            case FishRarity.Epic: return mulEpic;
            case FishRarity.Legendary: return mulLegendary;
            default: return 1f;
        }
    }
}

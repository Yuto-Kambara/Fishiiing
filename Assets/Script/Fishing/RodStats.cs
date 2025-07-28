using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RodStats
/// ・巻取り速度（水平/XY）… 基礎値 × レベル補正 × レア補正 × リール乗数
/// ・抽選テーブル … bait(Lure/FishBait) 既定 or 装備(Bait/Lure)の重みオーバーライド
/// ・UI/ショップから Upgrade / SetBait / SetReelOverride / SetRarityWeightsOverride を呼ぶ
/// </summary>
public class RodStats : MonoBehaviour
{
    public enum BaitType { Lure, FishBait }

    [Header("Base Speeds")]
    [Tooltip("アンカー中の水平リール速度の基礎値")]
    public float baseReelHorizontal = 4f;
    [Tooltip("XY 巻取り速度の基礎値")]
    public float baseReelFull = 4f;

    [Header("Leveling")]
    [Tooltip("リール強化レベル")]
    public int reelLevel = 0;
    [Tooltip("レベルごとに掛ける成長率（0.1=+10%/Lv）")]
    public float reelLevelStepMul = 0.10f;

    [Header("Per-Rarity Speed Multipliers")]
    [Tooltip("Common 釣り上げ時に掛ける係数（1=等倍）")]
    public float speedMulCommon = 1f;
    public float speedMulUncommon = 1f;
    public float speedMulRare = 1f;
    public float speedMulEpic = 1f;
    public float speedMulLegendary = 1f;

    [Header("Default Rarity Weights (BaitType に応じて使用)")]
    public BaitType bait = BaitType.Lure;
    public RarityWeights lureWeights = RarityWeights.Ones();
    public RarityWeights fishBaitWeights = RarityWeights.Ones();

    [Header("Multi Catch")]
    [Min(1)] public int multiCatchCount = 1;

    /* ===== 外部(装備)オーバーライド ===== */
    // 抽選重み：Bait/Lure の定義から流し込む（nullなら既定テーブルを使う）
    private Dictionary<FishRarity, int> overrideRarityWeights = null;

    // リール乗数
    private float overrideHorizMul = 1f;
    private float overrideFullMul = 1f;
    private readonly Dictionary<FishRarity, float> overridePerRarityMul
        = new Dictionary<FishRarity, float>()
        {
            { FishRarity.Common, 1f },
            { FishRarity.Uncommon, 1f },
            { FishRarity.Rare, 1f },
            { FishRarity.Epic, 1f },
            { FishRarity.Legendary, 1f },
        };

    /* ====== API：UI/ショップ/コントローラが使用 ====== */

    public void UpgradeReel() => reelLevel = Mathf.Max(0, reelLevel + 1);

    public void UpgradeMultiCatch() => multiCatchCount = Mathf.Max(1, multiCatchCount + 1);

    public int GetMultiCatchCount() => Mathf.Max(1, multiCatchCount);

    public void SetBait(BaitType type) => bait = type;

    /// <summary>餌/ルアーによるレア重みを上書き（null で解除）</summary>
    public void SetRarityWeightsOverride(Dictionary<FishRarity, int> weightsOrNull)
    {
        overrideRarityWeights = weightsOrNull;
    }

    /// <summary>リール（装備）から速度乗数を上書き（null で解除）</summary>
    public void SetReelOverride(ReelDefinition reelOrNull)
    {
        if (reelOrNull == null)
        {
            overrideHorizMul = 1f;
            overrideFullMul = 1f;
            overridePerRarityMul[FishRarity.Common] = 1f;
            overridePerRarityMul[FishRarity.Uncommon] = 1f;
            overridePerRarityMul[FishRarity.Rare] = 1f;
            overridePerRarityMul[FishRarity.Epic] = 1f;
            overridePerRarityMul[FishRarity.Legendary] = 1f;
            return;
        }

        overrideHorizMul = Mathf.Max(0.01f, reelOrNull.horizontalSpeedMul);
        overrideFullMul = Mathf.Max(0.01f, reelOrNull.fullReelSpeedMul);
        overridePerRarityMul[FishRarity.Common] = reelOrNull.mulCommon;
        overridePerRarityMul[FishRarity.Uncommon] = reelOrNull.mulUncommon;
        overridePerRarityMul[FishRarity.Rare] = reelOrNull.mulRare;
        overridePerRarityMul[FishRarity.Epic] = reelOrNull.mulEpic;
        overridePerRarityMul[FishRarity.Legendary] = reelOrNull.mulLegendary;
    }

    /* ====== 速度計算 ====== */

    public float GetHorizontalReelSpeed()
    {
        float baseSpeed = baseReelHorizontal * (1f + reelLevel * reelLevelStepMul);
        return baseSpeed * overrideHorizMul;
    }

    public float GetFullReelSpeed(FishRarity r)
    {
        float baseSpeed = baseReelFull * (1f + reelLevel * reelLevelStepMul);
        float rarityBaseMul = GetRaritySpeedMul(r);
        float fromReel = overrideFullMul * overridePerRarityMul[r];
        return baseSpeed * rarityBaseMul * fromReel;
    }

    private float GetRaritySpeedMul(FishRarity r)
    {
        switch (r)
        {
            case FishRarity.Common: return speedMulCommon;
            case FishRarity.Uncommon: return speedMulUncommon;
            case FishRarity.Rare: return speedMulRare;
            case FishRarity.Epic: return speedMulEpic;
            case FishRarity.Legendary: return speedMulLegendary;
            default: return 1f;
        }
    }

    /* ====== 抽選テーブル ====== */

    public Dictionary<FishRarity, int> GetRarityWeights()
    {
        if (overrideRarityWeights != null) return overrideRarityWeights;

        // 既定テーブル（BaitType に応じて）
        return (bait == BaitType.FishBait)
            ? fishBaitWeights.ToDict()
            : lureWeights.ToDict();
    }
}

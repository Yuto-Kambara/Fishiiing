using System;
using System.Collections.Generic;
using UnityEngine;

public enum BaitType { Lure, FishBait }   // ルアー／さばき餌

/// <summary>
/// 釣り竿のパラメータ・強化状態を一元管理。
/// ・リール速度（レベル×レアリティ係数）
/// ・同時釣獲数（レベル）
/// ・エサ種別（レアリティ重み）
/// </summary>
public class RodStats : MonoBehaviour
{
    [Header("Base Reel Speeds (m/s)")]
    public float baseReelHorizontal = 4f;   // 水平リールの基礎
    public float baseReelFull = 4f;         // XY 巻取りの基礎

    [Header("Reel Level")]
    [Tooltip("リール強化レベル (0〜)")]
    public int reelLevel = 0;

    [Tooltip("レベル毎の乗数 (例: 0.1 → +10%/Lv)")]
    [Range(0f, 1f)] public float reelLevelStepMul = 0.1f;

    [Header("Rarity Speed Multipliers")]
    [Tooltip("レアリティ別の速度係数（難しいほど <1 に）")]
    public float speedMulCommon = 1.00f;
    public float speedMulUncommon = 0.95f;
    public float speedMulRare = 0.90f;
    public float speedMulEpic = 0.85f;
    public float speedMulLegendary = 0.80f;

    [Header("Multi Catch")]
    [Tooltip("一度に釣れる数（最低1）")]
    [Min(1)] public int multiCatchCount = 1;

    [Header("Bait")]
    public BaitType bait = BaitType.Lure;

    [Tooltip("ルアー時のレアリティ重み（大きいほど出やすい）")]
    public Vector5 lureWeights = Vector5.DefaultLure();
    [Tooltip("さばき餌時のレアリティ重み（大きいほど出やすい）")]
    public Vector5 fishBaitWeights = Vector5.DefaultFishBait();

    /*================= API =================*/

    /// <summary>水平リール速度（レベルのみ反映。レアリティ非依存）</summary>
    public float GetHorizontalReelSpeed()
        => baseReelHorizontal * (1f + reelLevel * reelLevelStepMul);

    /// <summary>XY 巻取り速度（レベル×レアリティ係数）</summary>
    public float GetFullReelSpeed(FishRarity rarity)
        => baseReelFull * (1f + reelLevel * reelLevelStepMul) * GetRaritySpeedMul(rarity);

    /// <summary>現在の同時釣獲数</summary>
    public int GetMultiCatchCount() => Mathf.Max(1, multiCatchCount);

    /// <summary>現在のエサに対応するレアリティ重み（正規化済み）</summary>
    public Dictionary<FishRarity, int> GetRarityWeights()
    {
        var src = bait == BaitType.Lure ? lureWeights : fishBaitWeights;
        return src.ToDict();
    }

    public float GetRaritySpeedMul(FishRarity r)
    {
        return r switch
        {
            FishRarity.Common => speedMulCommon,
            FishRarity.Uncommon => speedMulUncommon,
            FishRarity.Rare => speedMulRare,
            FishRarity.Epic => speedMulEpic,
            FishRarity.Legendary => speedMulLegendary,
            _ => 1f
        };
    }

    /*================= 強化（仮API） =================*/
    // UI 実装前提：通貨消費はここでは行わず、呼び出し側で CurrencyManager.TrySpend を使ってください
    public void UpgradeReel() => reelLevel++;
    public void UpgradeMultiCatch() => multiCatchCount++;
    public void SetBait(BaitType t) => bait = t;
}

/// <summary>レア5段の重みを Inspector で扱いやすくするヘルパ</summary>
[Serializable]
public struct Vector5
{
    public int Common, Uncommon, Rare, Epic, Legendary;

    public Dictionary<FishRarity, int> ToDict()
        => new()
        {
            { FishRarity.Common, Common },
            { FishRarity.Uncommon, Uncommon },
            { FishRarity.Rare, Rare },
            { FishRarity.Epic, Epic },
            { FishRarity.Legendary, Legendary },
        };

    public static Vector5 DefaultLure() => new() { Common = 50, Uncommon = 35, Rare = 12, Epic = 3, Legendary = 0 };
    public static Vector5 DefaultFishBait() => new() { Common = 35, Uncommon = 35, Rare = 20, Epic = 8, Legendary = 2 };
}

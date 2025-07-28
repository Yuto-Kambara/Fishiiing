using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct RarityWeights
{
    [Min(0)] public int common;
    [Min(0)] public int uncommon;
    [Min(0)] public int rare;
    [Min(0)] public int epic;
    [Min(0)] public int legendary;

    public static RarityWeights Ones()
    {
        return new RarityWeights { common = 1, uncommon = 1, rare = 1, epic = 1, legendary = 1 };
    }

    public Dictionary<FishRarity, int> ToDict()
    {
        var d = new Dictionary<FishRarity, int>(5);
        d[FishRarity.Common] = Mathf.Max(0, common);
        d[FishRarity.Uncommon] = Mathf.Max(0, uncommon);
        d[FishRarity.Rare] = Mathf.Max(0, rare);
        d[FishRarity.Epic] = Mathf.Max(0, epic);
        d[FishRarity.Legendary] = Mathf.Max(0, legendary);
        return d;
    }

    public bool IsAllZero()
    {
        return common == 0 && uncommon == 0 && rare == 0 && epic == 0 && legendary == 0;
    }
}

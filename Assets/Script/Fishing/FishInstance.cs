using UnityEngine;

/// <summary>実体の魚 1 匹が保持するデータ</summary>
[System.Serializable]
public struct FishInstance
{
    public FishDefinition def;   // 種データ
    public float lengthCm;       // 個体ごと
    public int value;          // 売却額

    public FishInstance(FishDefinition d, float len)
    {
        def = d;
        lengthCm = len;
        value = d.CalcPrice(len);
    }
}

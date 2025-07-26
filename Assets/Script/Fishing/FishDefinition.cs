using UnityEngine;

/// <summary>
/// ScriptableObject 1 ファイル = 魚種 1 つ
/// インスペクタで値を入力するだけで “新種” が追加できる
/// </summary>
[CreateAssetMenu(fileName = "FishDef_", menuName = "Fishing/Fish Definition", order = 0)]
public class FishDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string speciesName = "Salmon";
    public FishRarity rarity = FishRarity.Common;

    [Header("Economy")]
    public int basePrice = 100;          // ショップ売却の基準値
    public float pricePerCm = 2f;        // 長さ 1cm ごとの加算

    [Header("Size Range (cm)")]
    public float minLength = 20f;
    public float maxLength = 60f;

    [Header("Visuals")]
    public Sprite sprite;                // 魚の見た目
    public GameObject prefabOverride;    // プレハブを個別に変えたい場合 (任意)

    /*------------ 価格計算 ------------*/
    public int CalcPrice(float length)
        => basePrice + Mathf.RoundToInt(length * pricePerCm) + (int)rarity * 50;
}

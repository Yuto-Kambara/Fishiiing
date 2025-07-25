using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クーラーボックス
/// ・BoxCollider2D（Is Trigger = true）必須
/// ・魚オブジェクトが Trigger に触れた瞬間に自動収納
///   （Player が持っている場合も、魚自身の Trigger が発火します）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CoolerBox : MonoBehaviour
{
    [Header("Capacity  (0 = Unlimited)")]
    public int capacity = 0;

    /* 内部：保持している魚データだけ保存 */
    private readonly List<FishProjectile.FishData> stored = new();

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    /*------------------------------------------------------------
     * 魚オブジェクトが触れた瞬間に収納処理
     *-----------------------------------------------------------*/
    private void OnTriggerEnter2D(Collider2D other)
    {
        var fish = other.GetComponent<FishProjectile>();
        if (fish) TryStoreFish(fish);
    }

    /*------------------------------------------------------------*/
    /// <summary>
    /// 魚を収納するメイン処理
    /// </summary>
    public bool TryStoreFish(FishProjectile fish)
    {
        if (!fish) return false;

        if (capacity > 0 && stored.Count >= capacity)
        {
            Debug.Log("Cooler full!");
            return false;
        }

        /* ------ データ保存 & オブジェクト破棄 ------ */
        stored.Add(fish.data);          // 情報だけ保持
        Destroy(fish.gameObject);       // 物体は破棄
        Debug.Log($"▶ Stored {fish.data.species}  (Total: {stored.Count})");
        return true;
    }

    /// <summary>クーラーボックス内の魚をすべて売却し、得た金額を返す</summary>
    public int SellAllFish()
    {
        if (stored.Count == 0) return 0;

        int total = 0;
        foreach (var fd in stored)
            total += fd.basePrice + Mathf.RoundToInt(fd.lengthCm * 2f) + fd.rarity * 50;

        stored.Clear();                     // 中身を空に
        return total;
    }

    /* 公開 API */
    public int FishCount => stored.Count;
    public IReadOnlyList<FishProjectile.FishData> GetInventory() => stored;
}

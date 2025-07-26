using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クーラーボックス
/// ・BoxCollider2D（IsTrigger = true）必須
/// ・魚（FishProjectile）が触れた瞬間に自動収納
/// ・中身は FishInstance（ScriptableObjectベースの定義 + 個体値）で保持
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CoolerBox : MonoBehaviour
{
    [Header("Capacity  (0 = Unlimited)")]
    public int capacity = 0;

    // ★ 旧: List<FishProjectile.FishData> → 新: List<FishInstance>
    private readonly List<FishInstance> stored = new List<FishInstance>();

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    // 魚が触れたら収納（プレイヤーが持っている魚も魚自身のTriggerで入ります）
    private void OnTriggerEnter2D(Collider2D other)
    {
        var fish = other.GetComponent<FishProjectile>();
        if (fish) TryStoreFish(fish);
    }

    /// <summary>魚を収納（プレイヤーから渡す場合にも呼べる）</summary>
    public bool TryStoreFish(FishProjectile fish)
    {
        if (!fish) return false;

        if (capacity > 0 && stored.Count >= capacity)
        {
            Debug.Log("Cooler full!");
            return false;
        }

        // ★ FishInstance を格納（旧: fish.data を FishData として扱っていた）
        stored.Add(fish.data);

        // 物体は破棄（情報だけ残す）
        Destroy(fish.gameObject);

        // ★ ログも新構造に合わせて speciesName 参照
        Debug.Log($"▶ Stored {fish.data.def.speciesName}  (Total: {stored.Count})");
        return true;
    }

    /// <summary>中の魚をすべて売却して金額を返す</summary>
    public int SellAllFish()
    {
        if (stored.Count == 0) return 0;

        int total = 0;
        // ★ FishInstance は value に確定済みの売値を持っています
        foreach (var fi in stored)
            total += fi.value;

        stored.Clear();
        return total;
    }

    /* 公開 API */
    public int FishCount => stored.Count;
    public IReadOnlyList<FishInstance> GetInventory() => stored.AsReadOnly();
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 魚の生成／放物線初速付与／エサに応じたレアリティ抽選／複数同時生成
/// </summary>
public class FishSpawner : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Transform breakwater;
    public RodStats rod;                         // 竿パラメータ参照

    [Header("Prefabs / Definitions")]
    public GameObject defaultFishPrefab;
    public FishDefinition[] fishTable;           // 種定義（レア込み）

    [Header("Arc Settings")]
    public float peakHeight = 1.5f;
    public float groundYOffset = 0.2f;
    public float fishSpawnYOff = 0.2f;

    [Header("Drop Mapping")]
    public float maxDropDistance = 3f;

    [Header("Multi Spawn")]
    [Tooltip("複数同時の際、落下Xに与えるランダム幅")]
    public float multiSpawnSpreadX = 0.25f;

    /* === 内部 === */
    float leftEdgeX, rightEdgeX;

    void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerController>();
        if (!rod) rod = FindFirstObjectByType<RodStats>();
        if (!breakwater) { Debug.LogError("[FishSpawner] Breakwater 未設定"); enabled = false; return; }
        if (!defaultFishPrefab)
            Debug.LogWarning("[FishSpawner] defaultFishPrefab が未割り当てです。");

        var col = breakwater.GetComponent<BoxCollider2D>();
        if (!col) { Debug.LogError("[FishSpawner] Breakwater に BoxCollider2D が必要"); enabled = false; return; }
        float half = col.size.x * breakwater.lossyScale.x * 0.5f;
        rightEdgeX = breakwater.position.x + half;
        leftEdgeX = breakwater.position.x - half;
    }

    public FishSpawnResult SpawnFromHook(Vector3 hookPos, float edgeDist, float releaseDistance)
    {
        var result = new FishSpawnResult(); // ★ class なので常にインスタンス化

        int count = rod ? rod.GetMultiCatchCount() : 1;
        if (!defaultFishPrefab || count <= 0) return result;

        // 目標Xの基点を計算
        float clamped = Mathf.Clamp(edgeDist, releaseDistance, maxDropDistance);
        float t = Mathf.InverseLerp(releaseDistance, maxDropDistance, clamped);
        float baseTargetX = Mathf.Lerp(player.transform.position.x, leftEdgeX, t);
        float targetY = breakwater.position.y + groundYOffset;

        for (int i = 0; i < count; i++)
        {
            // 1) エサ/ルアー・竿設定に応じてレアリティを重み抽選
            var rarity = PickRarity();
            if ((int)rarity > (int)result.maxRarity) result.maxRarity = rarity;

            // 2) 同レア内から FishDefinition を選択
            var defs = fishTable?.Where(d => d && d.rarity == rarity).ToList();
            FishDefinition def = (defs != null && defs.Count > 0)
                ? defs[Random.Range(0, defs.Count)]
                : PickAnyDefinition(); // レア内不在なら全体から

            // 3) 落下目標X を少し散らす
            float targetX = baseTargetX + Random.Range(-multiSpawnSpreadX, multiSpawnSpreadX);

            // 4) 放物線初速計算
            Vector2 v0 = ComputeBallisticVelocity(hookPos, new Vector2(targetX, targetY), peakHeight);

            // 5) 生成＆初速付与
            Vector3 spawn = hookPos + Vector3.up * fishSpawnYOff;
            GameObject fishGO = FishFactory.SpawnFish(def, spawn, Quaternion.identity, defaultFishPrefab);
            if (fishGO && fishGO.TryGetComponent(out Rigidbody2D rb))
            {
                rb.linearVelocity = v0;
                rb.gravityScale = 1f;
            }
            if (fishGO) result.spawned.Add(fishGO);
        }

        return result;
    }

    Vector2 ComputeBallisticVelocity(Vector2 start, Vector2 target, float peak)
    {
        float g = Mathf.Abs(Physics2D.gravity.y);
        float peakY = Mathf.Max(start.y, target.y) + peak;
        float vyUp = Mathf.Sqrt(2f * g * (peakY - start.y));
        float tUp = vyUp / g;
        float vyDn = Mathf.Sqrt(2f * g * (peakY - target.y));
        float tDn = vyDn / g;
        float T = tUp + tDn;
        float vx = (target.x - start.x) / T;
        return new Vector2(vx, vyUp);
    }

    FishDefinition PickAnyDefinition()
    {
        if (fishTable == null || fishTable.Length == 0) return null;
        return fishTable[Random.Range(0, fishTable.Length)];
    }

    FishRarity PickRarity()
    {
        // ★ 修正：Vector5 は廃止。rod が無い/重みが全ゼロの時は Common=1 のフォールバックを使う
        var weights = (rod != null) ? rod.GetRarityWeights() : FallbackWeights();

        int sum = 0; foreach (var kv in weights) sum += Mathf.Max(0, kv.Value);
        if (sum <= 0)
        {
            // すべて 0 なら Common を返す
            return FishRarity.Common;
        }

        int r = Random.Range(0, sum);
        foreach (var kv in weights)
        {
            int w = Mathf.Max(0, kv.Value);
            if (r < w) return kv.Key;
            r -= w;
        }
        return FishRarity.Common;
    }

    // Common=1 のみを持つ安全なフォールバック重み
    Dictionary<FishRarity, int> FallbackWeights()
    {
        return new Dictionary<FishRarity, int>
        {
            { FishRarity.Common, 1 },
            { FishRarity.Uncommon, 0 },
            { FishRarity.Rare, 0 },
            { FishRarity.Epic, 0 },
            { FishRarity.Legendary, 0 },
        };
    }

    public float RightEdgeX => rightEdgeX;
}

/// <summary>スポーン結果：生成した魚と、その中で最も高いレアリティ</summary>
public class FishSpawnResult
{
    public List<GameObject> spawned = new List<GameObject>();
    public FishRarity maxRarity = FishRarity.Common;
}

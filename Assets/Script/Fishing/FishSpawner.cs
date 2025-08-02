using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Transform breakwater;
    public RodStats rod;
    public Transform coolerMouth;

    [Header("Prefabs / Definitions")]
    public GameObject defaultFishPrefab;
    public FishDefinition[] fishTable;

    [Header("Arc Settings")]
    public float peakHeightNear = 1.2f;
    public float peakHeightFar = 2.5f;
    public float leftDropHeight = 0.2f;

    [Header("Drop Mapping")]
    [Tooltip("クーラー寄りとみなす開始距離（右端から）")]
    public float releaseDistance = 0.06f;
    [Tooltip("t=1（最遠）とみなす距離上限（自動補正で上書き可）")]
    public float maxDropDistance = 3f;
    [Tooltip("左端のわずかな内寄せ")]
    public float leftDropPadding = 0.3f;

    [Header("Multi Spawn")]
    [Tooltip("クーラー寄りほど自動で縮むXばらつき量の上限")]
    public float multiSpawnSpreadX = 0.25f;
    [Tooltip("2匹目以降の出現段差（>0で下方向）")]
    public float multiSpawnStaggerDownY = 0.12f;
    [Tooltip("生成直後、同一キャスト内の魚同士の衝突を無効化する秒数")]
    public float launchNoCollideTime = 0.4f;

    /* === 内部 === */
    float leftEdgeX, rightEdgeX;

    // 同一キャスト内で最近生成した魚のコライダー群（短時間で衝突無効にする）
    readonly List<Collider2D> _launchGroup = new List<Collider2D>();
    float _lastSpawnTime = -999f;
    const float GROUP_RESET_GAP = 1.0f;   // この時間空くと新キャスト扱いでグループを初期化

    void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerController>();
        if (!rod) rod = FindFirstObjectByType<RodStats>();
        if (!breakwater || !coolerMouth)
        { Debug.LogError("[FishSpawner] Breakwater または CoolerMouth 未設定"); enabled = false; return; }

        var col = breakwater.GetComponent<BoxCollider2D>();
        if (!col)
        { Debug.LogError("[FishSpawner] Breakwater に BoxCollider2D が必要"); enabled = false; return; }

        float half = col.size.x * breakwater.lossyScale.x * 0.5f;
        rightEdgeX = breakwater.position.x + half;
        leftEdgeX = breakwater.position.x - half;
    }

    /// <summary>
    /// 1回の呼び出しで「必ず1匹だけ」生成します。
    /// edgeDist: 右端からの距離（FishingController 側で計算した値）
    /// </summary>
    public FishSpawnResult SpawnFromHook(Vector3 hookPos, float edgeDist)
    {
        var res = new FishSpawnResult();
        if (!defaultFishPrefab) return res;

        // --- キャスト切り替え検出（間が空いたらグループ初期化） ---
        if (Time.time - _lastSpawnTime > GROUP_RESET_GAP) _launchGroup.Clear();
        _lastSpawnTime = Time.time;

        // 0..1 正規化：近い→0 / 遠い→1
        float t = Mathf.InverseLerp(releaseDistance, maxDropDistance,
                    Mathf.Clamp(edgeDist, releaseDistance, maxDropDistance));

        // 落下2点
        float earlyX = leftEdgeX + Mathf.Max(0f, leftDropPadding);
        float earlyY = breakwater.position.y + leftDropHeight;
        Vector2 targetEarly = new Vector2(earlyX, earlyY);
        Vector2 targetCooler = new Vector2(coolerMouth.position.x, coolerMouth.position.y);

        // 近い→クーラー / 遠い→左端（補間の向きに注意）
        Vector2 targetBase = Vector2.Lerp(targetCooler, targetEarly, t);

        // 放物線ピーク：遠いほど高い
        float peak = Mathf.Lerp(peakHeightNear, peakHeightFar, t);

        // クーラー寄りほどばらつき縮小
        float spread = multiSpawnSpreadX * t;

        // レアリティ抽選（Rod の重みを利用）
        var rarity = PickRarity();
        res.maxRarity = rarity;

        var defs = fishTable?.Where(d => d && d.rarity == rarity).ToList();
        FishDefinition def = (defs != null && defs.Count > 0)
            ? defs[Random.Range(0, defs.Count)]
            : PickAnyDefinition();

        // 目標を少し散らす
        Vector2 target = targetBase;
        if (spread > 0f) target.x += Random.Range(-spread, spread);

        // === 出現位置：2匹目以降は段差 ===
        int idxInGroup = Mathf.Max(0, _launchGroup.Count); // 0=最初
        float yStagger = -multiSpawnStaggerDownY * idxInGroup;
        Vector3 spawn = hookPos + new Vector3(0f, 0.2f + yStagger, 0f);

        // 初速計算は「spawn」基準
        Vector2 v0 = ComputeBallisticVelocity(spawn, target, peak);

        // 生成
        GameObject fishGO = FishFactory.SpawnFish(def, spawn, Quaternion.identity, defaultFishPrefab);
        if (fishGO && fishGO.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = v0;
            rb.gravityScale = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 衝突安定化
        }

        // --- 魚同士の一時的な衝突無効化 ---
        if (fishGO && fishGO.TryGetComponent(out Collider2D myCol))
        {
            // 既存グループと相互衝突を無効化
            for (int i = 0; i < _launchGroup.Count; i++)
            {
                var other = _launchGroup[i];
                if (other && myCol) Physics2D.IgnoreCollision(myCol, other, true);
            }
            _launchGroup.Add(myCol);
            StartCoroutine(ReenableCollisionsAfterDelay(myCol, launchNoCollideTime));
        }

        if (fishGO) res.spawned.Add(fishGO);
        return res;
    }

    IEnumerator ReenableCollisionsAfterDelay(Collider2D mine, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 生存している相手との衝突を再有効化
        for (int i = _launchGroup.Count - 1; i >= 0; i--)
        {
            var other = _launchGroup[i];
            if (!other) { _launchGroup.RemoveAt(i); continue; }
            if (mine) Physics2D.IgnoreCollision(mine, other, false);
        }
        // 自分をグループから外す
        _launchGroup.Remove(mine);
    }

    /* === 放物線初速計算（頂点指定） === */
    Vector2 ComputeBallisticVelocity(Vector2 start, Vector2 target, float peak)
    {
        float g = Mathf.Abs(Physics2D.gravity.y);

        float baseY = Mathf.Max(start.y, target.y);
        float peakY = baseY + Mathf.Max(0.1f, peak);

        float vyUp = Mathf.Sqrt(2f * g * Mathf.Max(0.01f, peakY - start.y));
        float tUp = vyUp / g;
        float vyDn = Mathf.Sqrt(2f * g * Mathf.Max(0.01f, peakY - target.y));
        float tDn = vyDn / g;

        float T = tUp + tDn;
        float vx = (target.x - start.x) / Mathf.Max(0.01f, T);
        return new Vector2(vx, vyUp);
    }

    FishDefinition PickAnyDefinition()
    {
        if (fishTable == null || fishTable.Length == 0) return null;
        return fishTable[Random.Range(0, fishTable.Length)];
    }

    FishRarity PickRarity()
    {
        var weights = (rod != null) ? rod.GetRarityWeights() : FallbackWeights();
        int sum = 0; foreach (var kv in weights) sum += Mathf.Max(0, kv.Value);
        if (sum <= 0) return FishRarity.Common;

        int r = Random.Range(0, sum);
        foreach (var kv in weights)
        {
            int w = Mathf.Max(0, kv.Value);
            if (r < w) return kv.Key;
            r -= w;
        }
        return FishRarity.Common;
    }

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
}

public class FishSpawnResult
{
    public List<GameObject> spawned = new List<GameObject>();
    public FishRarity maxRarity = FishRarity.Common;
}

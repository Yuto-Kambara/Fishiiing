using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ���̐����^�����������t�^�^�G�T�ɉ��������A���e�B���I�^������������
/// </summary>
public class FishSpawner : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Transform breakwater;
    public RodStats rod;                         // �ƃp�����[�^�Q��

    [Header("Prefabs / Definitions")]
    public GameObject defaultFishPrefab;
    public FishDefinition[] fishTable;           // ���`�i���A���݁j

    [Header("Arc Settings")]
    public float peakHeight = 1.5f;
    public float groundYOffset = 0.2f;
    public float fishSpawnYOff = 0.2f;

    [Header("Drop Mapping")]
    public float maxDropDistance = 3f;

    [Header("Multi Spawn")]
    [Tooltip("���������̍ہA����X�ɗ^���郉���_����")]
    public float multiSpawnSpreadX = 0.25f;

    /* === ���� === */
    float leftEdgeX, rightEdgeX;

    void Awake()
    {
        if (!player) player = FindFirstObjectByType<PlayerController>();
        if (!rod) rod = FindFirstObjectByType<RodStats>();
        if (!breakwater) { Debug.LogError("[FishSpawner] Breakwater ���ݒ�"); enabled = false; return; }
        if (!defaultFishPrefab)
            Debug.LogWarning("[FishSpawner] defaultFishPrefab �������蓖�Ăł��B");

        var col = breakwater.GetComponent<BoxCollider2D>();
        if (!col) { Debug.LogError("[FishSpawner] Breakwater �� BoxCollider2D ���K�v"); enabled = false; return; }
        float half = col.size.x * breakwater.lossyScale.x * 0.5f;
        rightEdgeX = breakwater.position.x + half;
        leftEdgeX = breakwater.position.x - half;
    }

    public FishSpawnResult SpawnFromHook(Vector3 hookPos, float edgeDist, float releaseDistance)
    {
        var result = new FishSpawnResult(); // �� class �Ȃ̂ŏ�ɃC���X�^���X��

        int count = rod ? rod.GetMultiCatchCount() : 1;
        if (!defaultFishPrefab || count <= 0) return result;

        // �ڕWX�̊�_���v�Z
        float clamped = Mathf.Clamp(edgeDist, releaseDistance, maxDropDistance);
        float t = Mathf.InverseLerp(releaseDistance, maxDropDistance, clamped);
        float baseTargetX = Mathf.Lerp(player.transform.position.x, leftEdgeX, t);
        float targetY = breakwater.position.y + groundYOffset;

        for (int i = 0; i < count; i++)
        {
            // 1) �G�T�ɉ����ă��A���e�B���d�ݒ��I
            var rarity = PickRarity();
            if ((int)rarity > (int)result.maxRarity) result.maxRarity = rarity;

            // 2) �����A������ FishDefinition ��I��
            var defs = fishTable?.Where(d => d && d.rarity == rarity).ToList();
            FishDefinition def = (defs != null && defs.Count > 0)
                ? defs[Random.Range(0, defs.Count)]
                : PickAnyDefinition(); // ���A���s�݂Ȃ�S�̂���

            // 3) �����ڕWX �������U�炷
            float targetX = baseTargetX + Random.Range(-multiSpawnSpreadX, multiSpawnSpreadX);

            // 4) �����������v�Z
            Vector2 v0 = ComputeBallisticVelocity(hookPos, new Vector2(targetX, targetY), peakHeight);

            // 5) �����������t�^
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
        var weights = rod ? rod.GetRarityWeights()
                          : new Vector5().ToDict(); // ���ׂ�0�Ȃ� Common �Ƀt�H�[���o�b�N
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

    public float RightEdgeX => rightEdgeX;
}

/// <summary>�X�|�[�����ʁF�����������ƁA���̒��ōł��������A���e�B</summary>
public class FishSpawnResult
{
    public List<GameObject> spawned = new List<GameObject>();
    public FishRarity maxRarity = FishRarity.Common;
}

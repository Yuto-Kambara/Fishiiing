using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 30 秒放置の魚を監視し、邪魔者をスポーン
/// ※ Breakwater 参照を使って「堤防左端」を算出
/// </summary>
public class NuisanceManager : MonoBehaviour
{
    public static NuisanceManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /*========== Inspector ==========*/
    [Header("References")]
    public Transform breakwater;                // BoxCollider2D 必須

    [Header("Thief Settings")]
    public GameObject thiefPrefab;
    public float seaSpawnX = -12f;          // 左下から登場
    public float seaReturnX = -14f;          // 左下へ消える
    public float seaDepthOffset = -1f;          // 海面より下

    /*========== Internal ==========*/
    private readonly Queue<FishProjectile> waiting = new();

    /*-- 魚から呼ばれる --*/
    public void RegisterFish(FishProjectile fish)
    {
        if (!fish || !fish.Landed || fish.IsHeld) return;  
        waiting.Enqueue(fish);
        TrySpawnThief();
    }

    /*========== スポーン処理 ==========*/
    private void TrySpawnThief()
    {
        if (waiting.Count == 0 || !thiefPrefab || !breakwater) return;

        /* 取りに行く魚 (最大 2) を確定 */
        List<FishProjectile> targets = new(2);
        for (int i = 0; i < 2 && waiting.Count > 0; i++)
            targets.Add(waiting.Dequeue());

        /* 堤防左端 (edgeX) を算出 */
        var bwCol = breakwater.GetComponent<BoxCollider2D>();
        float halfW = bwCol.size.x * breakwater.lossyScale.x * 0.5f;
        float edgeX = breakwater.position.x - halfW;        // 左端 X

        /* 堤防上面 Y を 1 匹目の魚高さで代表させる */
        float surfaceY = targets[0].transform.position.y;

        /* 座標セット */
        Vector3 spawnPos = new(seaSpawnX, surfaceY + seaDepthOffset, 0);
        Vector3 edgePos = new(edgeX, surfaceY, 0);
        Vector3 returnPos = new(seaReturnX, surfaceY + seaDepthOffset, 0);

        /* スポーン & 初期化 */
        var obj = Instantiate(thiefPrefab, spawnPos, Quaternion.identity);
        obj.GetComponent<NuisanceThief>()
           .Initialize(targets, edgePos, returnPos);
    }


}

using UnityEngine;

public static class FishFactory
{
    /// <summary>
    /// FishDefinition から魚を生成
    /// ・def.prefabOverride があればそれ
    /// ・無ければ fallbackPrefab（Inspector 渡し）
    /// 生成後に FishProjectile.Init(def) で個体値等を確定
    /// </summary>
    public static GameObject SpawnFish(
        FishDefinition def,
        Vector3 pos,
        Quaternion rot,
        GameObject fallbackPrefab)
    {
        GameObject prefab = (def && def.prefabOverride) ? def.prefabOverride : fallbackPrefab;
        if (!prefab)
        {
            Debug.LogError("[FishFactory] Prefab not provided.");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, pos, rot);

        if (!go.TryGetComponent(out FishProjectile proj))
        {
            Debug.LogError("[FishFactory] FishProjectile が見つかりません。");
            return go;
        }

        if (def) proj.Init(def);

        if (go.TryGetComponent(out Rigidbody2D rb))
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        return go;
    }
}

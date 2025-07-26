using UnityEngine;

public static class FishFactory
{
    /// <summary>
    /// FishDefinition ���狛�𐶐�
    /// �Edef.prefabOverride ������΂���
    /// �E������� fallbackPrefab�iInspector �n���j
    /// ������� FishProjectile.Init(def) �Ō̒l�����m��
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
            Debug.LogError("[FishFactory] FishProjectile ��������܂���B");
            return go;
        }

        if (def) proj.Init(def);

        if (go.TryGetComponent(out Rigidbody2D rb))
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        return go;
    }
}

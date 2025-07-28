using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// デバッグ用インベントリ投入ツール
/// ・Inspector から定義したアイテムを、キー1～9で投入
/// ・右クリックメニュー（ContextMenu）や起動時自動投入も可能
/// ・InventoryBinder.TryAddFirst を用いるため、スロットの allowed も尊重される
/// </summary>
public class DebugInventorySpawner : MonoBehaviour
{
    [Serializable]
    public class SpawnEntry
    {
        public string label = "Bait x10";
        public ItemDefinition item;
        [Min(1)] public int count = 1;
        [Tooltip("このエントリーを投入するキー（未指定=自動でAlpha1..9）")]
        public KeyCode hotkey = KeyCode.None;
    }

    [Header("Target")]
    public InventoryBinder targetBinder;     // プレイヤーの InventoryBinder を割り当て
    [Header("Entries (Max 9 推奨)")]
    public List<SpawnEntry> entries = new();

    [Header("Options")]
    public bool spawnOnStart = false;        // 起動時に entries を順に投入
    public bool logOnSpawn = true;

    private void Awake()
    {
        if (!targetBinder)
        {
            targetBinder = FindFirstObjectByType<InventoryBinder>();
            if (!targetBinder) Debug.LogWarning("[DebugInventorySpawner] InventoryBinder が見つかりません。");
        }

        // 未指定のホットキーに 1..9 を自動割当
        KeyCode[] defaults = {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
            KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
            KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
        };
        for (int i = 0; i < entries.Count && i < defaults.Length; i++)
        {
            if (entries[i].hotkey == KeyCode.None) entries[i].hotkey = defaults[i];
        }
    }

    private void Start()
    {
        if (spawnOnStart) SpawnAll();
    }

    private void Update()
    {
        if (!targetBinder) return;
        foreach (var e in entries)
        {
            if (e.item == null || e.count <= 0) continue;
            if (e.hotkey != KeyCode.None && Input.GetKeyDown(e.hotkey))
            {
                Spawn(e);
            }
        }
    }

    [ContextMenu("Spawn All Now")]
    public void SpawnAll()
    {
        if (!targetBinder) return;
        foreach (var e in entries) Spawn(e);
    }

    private void Spawn(SpawnEntry e)
    {
        if (!targetBinder || e.item == null || e.count <= 0) return;
        var inst = new ItemInstance(e.item, e.count);
        bool ok = targetBinder.TryAddFirst(inst, ItemTags.All);
        if (logOnSpawn)
        {
            Debug.Log(ok
                ? $"[DebugInventorySpawner] Added {e.label} ({e.item.displayName} x{e.count})"
                : $"[DebugInventorySpawner] Failed to add {e.label} (inventory full or slot rule mismatch)");
        }
    }
}

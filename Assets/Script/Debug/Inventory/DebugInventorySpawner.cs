using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �f�o�b�O�p�C���x���g�������c�[��
/// �EInspector �����`�����A�C�e�����A�L�[1�`9�œ���
/// �E�E�N���b�N���j���[�iContextMenu�j��N���������������\
/// �EInventoryBinder.TryAddFirst ��p���邽�߁A�X���b�g�� allowed �����d�����
/// </summary>
public class DebugInventorySpawner : MonoBehaviour
{
    [Serializable]
    public class SpawnEntry
    {
        public string label = "Bait x10";
        public ItemDefinition item;
        [Min(1)] public int count = 1;
        [Tooltip("���̃G���g���[�𓊓�����L�[�i���w��=������Alpha1..9�j")]
        public KeyCode hotkey = KeyCode.None;
    }

    [Header("Target")]
    public InventoryBinder targetBinder;     // �v���C���[�� InventoryBinder �����蓖��
    [Header("Entries (Max 9 ����)")]
    public List<SpawnEntry> entries = new();

    [Header("Options")]
    public bool spawnOnStart = false;        // �N������ entries �����ɓ���
    public bool logOnSpawn = true;

    private void Awake()
    {
        if (!targetBinder)
        {
            targetBinder = FindFirstObjectByType<InventoryBinder>();
            if (!targetBinder) Debug.LogWarning("[DebugInventorySpawner] InventoryBinder ��������܂���B");
        }

        // ���w��̃z�b�g�L�[�� 1..9 ����������
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

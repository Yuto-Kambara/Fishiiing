using System.Collections.Generic;
using UnityEngine;

public class InventoryBinder : MonoBehaviour
{
    [Header("Model")]
    public int capacity = 12;
    public InventoryModel Model { get; private set; }

    [Header("Prefabs")]
    public InventoryItemView itemViewPrefab;

    private readonly Dictionary<int, InventorySlotView> _slots = new();
    private readonly Dictionary<int, InventoryItemView> _items = new();

    private bool _initialized = false;

    private void Awake()
    {
        EnsureInitialized();
    }

    /// <summary>Model �̐����ƃC�x���g�w�ǂ�ۏ�</summary>
    private void EnsureInitialized()
    {
        if (_initialized) return;

        if (Model == null)
        {
            Model = new InventoryModel(capacity);
        }

        // ��d�w�ǖh�~�̂��߈�x�O���Ă���w��
        Model.OnSlotChanged -= OnSlotChanged;
        Model.OnSlotChanged += OnSlotChanged;

        _initialized = true;
    }

    /// <summary>�X���b�g��o�^���A����UI�𔽉f</summary>
    public void RegisterSlot(InventorySlotView slot)
    {
        if (!slot) return;

        EnsureInitialized();

        if (!_slots.ContainsKey(slot.slotIndex))
        {
            _slots.Add(slot.slotIndex, slot);
        }
        else
        {
            _slots[slot.slotIndex] = slot; // �� index �ɍēo�^�������ꍇ�͏㏑��
        }

        // �������f�iModel �Ȃ��EPrefab �Ȃ��ł����S�Ɂj
        OnSlotChanged(slot.slotIndex);
    }

    private void OnDestroy()
    {
        if (Model != null)
            Model.OnSlotChanged -= OnSlotChanged;
    }

    /// <summary>�w��X���b�g��UI���Đ���</summary>
    private void OnSlotChanged(int index)
    {
        // �����\���̔j��
        if (_items.TryGetValue(index, out var iv) && iv)
        {
            Destroy(iv.gameObject);
            _items.Remove(index);
        }

        // ���S�K�[�h
        if (Model == null) return;
        if (!_slots.TryGetValue(index, out var slot) || !slot) return;

        var item = Model.Get(index);
        if (item == null) return;
        if (!itemViewPrefab) { Debug.LogWarning("[InventoryBinder] itemViewPrefab ���ݒ�"); return; }

        // �V�K�\��
        var ivNew = Instantiate(itemViewPrefab, slot.transform);
        ivNew.transform.localPosition = Vector3.zero;
        ivNew.Setup(slot, item);
        _items[index] = ivNew;
    }

    /// <summary>�S�X���b�g����ɂ��� UI �ɒʒm</summary>
    public void Clear()
    {
        EnsureInitialized();
        for (int i = 0; i < Model.Capacity; i++)
        {
            Model.Set(i, null);
        }
    }
    // === �֗�API: �ǉ� ===
    #region Add Helpers

    /// <summary>
    /// �ŏ��ɓ������ꏊ�փA�C�e����ǉ�����B
    /// 1) �o�^�ς݃X���b�g�� allowed �𑸏d���āu����X�^�b�N���́v���u�󂫘g�v
    /// 2) �X���b�g���o�^�ȂǂŌ�����Ȃ��ꍇ�� allowedMask ��p�����t�H�[���o�b�N��
    ///    Model �𒼐ڑ������āu����X�^�b�N���́v���u�󂫘g�v
    /// </summary>
    /// <param name="item">�ǉ��������A�C�e���inull�s�j</param>
    /// <param name="allowedMask">�t�H�[���o�b�N���ɋ�����^�O�i�ʏ�� ItemTags.All�j</param>
    public bool TryAddFirst(ItemInstance item, ItemTags allowedMask = ItemTags.All)
    {
        EnsureInitialized();
        if (item == null) return false;
        var tag = item.Tags;

        // --- 1) �o�^�ς݃X���b�g��D��iallowed �������Ƀ`�F�b�N�j ---
        if (_slots.Count > 0)
        {
            // ����X�^�b�N����
            foreach (var kv in _slots)
            {
                int i = kv.Key;
                var slot = kv.Value;
                if (slot == null) continue;
                if ((slot.allowed & tag) == 0) continue;

                var cur = Model.Get(i);
                if (cur != null && cur.IsStackable && item.IsStackable && cur.def == item.def)
                {
                    int space = cur.def.maxStack - cur.count;
                    if (space > 0)
                    {
                        int move = Mathf.Min(space, item.count);
                        cur.count += move;
                        item.count -= move;
                        Model.Set(i, cur);
                        if (item.count <= 0) return true;
                    }
                }
            }
            // �󂫘g��
            foreach (var kv in _slots)
            {
                int i = kv.Key;
                var slot = kv.Value;
                if (slot == null) continue;
                if ((slot.allowed & tag) == 0) continue;

                if (Model.Get(i) == null)
                {
                    Model.Set(i, item);
                    return true;
                }
            }
        }

        // --- 2) �t�H�[���o�b�N�FallowedMask �� Model �𒼐ڑ��� ---
        if ((allowedMask & tag) == 0) return false;

        // ����X�^�b�N����
        for (int i = 0; i < Model.Capacity; i++)
        {
            var cur = Model.Get(i);
            if (cur != null && cur.IsStackable && item.IsStackable && cur.def == item.def)
            {
                int space = cur.def.maxStack - cur.count;
                if (space > 0)
                {
                    int move = Mathf.Min(space, item.count);
                    cur.count += move;
                    item.count -= move;
                    Model.Set(i, cur);
                    if (item.count <= 0) return true;
                }
            }
        }
        // �󂫘g��
        for (int i = 0; i < Model.Capacity; i++)
        {
            if (Model.Get(i) == null)
            {
                Model.Set(i, item);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// �w��C���f�b�N�X�ɒǉ��i�X���b�g�� allowed �𑸏d�j�B
    /// ���ɓ��킪����X�^�b�N�\�Ȃ獇�́B�󂫂Ȃ�z�u�B���̑��� false�B
    /// </summary>
    public bool TryAddAt(int index, ItemInstance item, bool respectSlotRule = true)
    {
        EnsureInitialized();
        if (item == null) return false;
        if (!Model.InRange(index)) return false;

        if (respectSlotRule && _slots.TryGetValue(index, out var slot) && slot)
        {
            if ((slot.allowed & item.Tags) == 0) return false;
        }

        var cur = Model.Get(index);
        if (cur == null)
        {
            Model.Set(index, item);
            return true;
        }
        if (cur.IsStackable && item.IsStackable && cur.def == item.def)
        {
            int space = cur.def.maxStack - cur.count;
            if (space <= 0) return false;
            int move = Mathf.Min(space, item.count);
            cur.count += move;
            item.count -= move;
            Model.Set(index, cur);
            return true;
        }
        return false;
    }

    #endregion

}


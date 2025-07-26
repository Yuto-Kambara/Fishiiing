using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InventoryModel �ƕ����� InventorySlotView �����т��A
/// �X���b�g���e���ς�����Ƃ��ɃA�C�e��UI���o�����ꂵ�܂��B
/// </summary>
public class InventoryBinder : MonoBehaviour
{
    [Header("Model")]
    public int capacity = 12;
    public InventoryModel Model { get; private set; }

    [Header("Prefabs")]
    public InventoryItemView itemViewPrefab;

    private readonly Dictionary<int, InventorySlotView> _slots = new();
    private readonly Dictionary<int, InventoryItemView> _items = new();

    private void Awake()
    {
        Model = new InventoryModel(capacity);
        Model.OnSlotChanged += OnSlotChanged;
    }

    public void RegisterSlot(InventorySlotView slot)
    {
        if (!_slots.ContainsKey(slot.slotIndex))
            _slots.Add(slot.slotIndex, slot);

        // �������f
        OnSlotChanged(slot.slotIndex);
    }

    private void OnSlotChanged(int index)
    {
        // ����������
        if (_items.TryGetValue(index, out var iv) && iv)
        {
            Destroy(iv.gameObject);
            _items.Remove(index);
        }

        // ���g������Ȃ琶��
        var item = Model.Get(index);
        if (item != null && _slots.TryGetValue(index, out var slot))
        {
            var ivNew = Instantiate(itemViewPrefab, slot.transform);
            ivNew.transform.localPosition = Vector3.zero;
            ivNew.Setup(slot, item);
            _items[index] = ivNew;
        }
    }

    #region �֗�API�i�X�N���v�g��������j
    public bool TryAddFirst(ItemInstance item, ItemTags allowedMask = ItemTags.All)
    {
        // �X�^�b�N�D��
        for (int i = 0; i < Model.Capacity; i++)
        {
            var cur = Model.Get(i);
            if (cur != null && cur.IsStackable && item.IsStackable && cur.def == item.def &&
                (allowedMask & item.Tags) != 0)
            {
                int space = cur.def.maxStack - cur.count;
                if (space <= 0) continue;

                int move = Mathf.Min(space, item.count);
                cur.count += move;
                item.count -= move;

                Model.Set(i, cur);
                if (item.count <= 0) return true;
            }
        }
        // �󂫃X���b�g��
        for (int i = 0; i < Model.Capacity; i++)
        {
            if (Model.Get(i) == null && (allowedMask & item.Tags) != 0)
            {
                Model.Set(i, item);
                return true;
            }
        }
        return false;
    }
    #endregion
}

using System;
using System.Collections.Generic;

public class InventoryModel
{
    public readonly int Capacity;
    private readonly List<ItemInstance> _slots;
    public event Action<int> OnSlotChanged; // index

    public InventoryModel(int capacity)
    {
        Capacity = capacity;
        _slots = new List<ItemInstance>(capacity);
        for (int i = 0; i < capacity; i++) _slots.Add(null);
    }

    public ItemInstance Get(int index) => InRange(index) ? _slots[index] : null;

    public void Set(int index, ItemInstance item)
    {
        if (!InRange(index)) return;
        _slots[index] = item;
        OnSlotChanged?.Invoke(index);
    }

    public void Swap(int a, int b)
    {
        if (!InRange(a) || !InRange(b)) return;
        (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
        OnSlotChanged?.Invoke(a);
        OnSlotChanged?.Invoke(b);
    }

    /// <summary>全スロットを空にして UI に通知</summary>
    public void Clear()
    {
        for (int i = 0; i<Capacity; i++)
        {
            _slots[i] = null;
            OnSlotChanged?.Invoke(i);
        }
    }

    public bool InRange(int i) => i >= 0 && i < Capacity;
}

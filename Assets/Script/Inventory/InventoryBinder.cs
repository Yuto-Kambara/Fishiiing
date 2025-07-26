using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InventoryModel と複数の InventorySlotView を結びつけ、
/// スロット内容が変わったときにアイテムUIを出し入れします。
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

        // 初期反映
        OnSlotChanged(slot.slotIndex);
    }

    private void OnSlotChanged(int index)
    {
        // 既存を消す
        if (_items.TryGetValue(index, out var iv) && iv)
        {
            Destroy(iv.gameObject);
            _items.Remove(index);
        }

        // 中身があるなら生成
        var item = Model.Get(index);
        if (item != null && _slots.TryGetValue(index, out var slot))
        {
            var ivNew = Instantiate(itemViewPrefab, slot.transform);
            ivNew.transform.localPosition = Vector3.zero;
            ivNew.Setup(slot, item);
            _items[index] = ivNew;
        }
    }

    #region 便利API（スクリプトから入れる）
    public bool TryAddFirst(ItemInstance item, ItemTags allowedMask = ItemTags.All)
    {
        // スタック優先
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
        // 空きスロットへ
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

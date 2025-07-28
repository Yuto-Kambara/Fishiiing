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

    /// <summary>Model の生成とイベント購読を保証</summary>
    private void EnsureInitialized()
    {
        if (_initialized) return;

        if (Model == null)
        {
            Model = new InventoryModel(capacity);
        }

        // 二重購読防止のため一度外してから購読
        Model.OnSlotChanged -= OnSlotChanged;
        Model.OnSlotChanged += OnSlotChanged;

        _initialized = true;
    }

    /// <summary>スロットを登録し、初期UIを反映</summary>
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
            _slots[slot.slotIndex] = slot; // 同 index に再登録が来た場合は上書き
        }

        // 初期反映（Model なし・Prefab なしでも安全に）
        OnSlotChanged(slot.slotIndex);
    }

    private void OnDestroy()
    {
        if (Model != null)
            Model.OnSlotChanged -= OnSlotChanged;
    }

    /// <summary>指定スロットのUIを再生成</summary>
    private void OnSlotChanged(int index)
    {
        // 既存表示の破棄
        if (_items.TryGetValue(index, out var iv) && iv)
        {
            Destroy(iv.gameObject);
            _items.Remove(index);
        }

        // 安全ガード
        if (Model == null) return;
        if (!_slots.TryGetValue(index, out var slot) || !slot) return;

        var item = Model.Get(index);
        if (item == null) return;
        if (!itemViewPrefab) { Debug.LogWarning("[InventoryBinder] itemViewPrefab 未設定"); return; }

        // 新規表示
        var ivNew = Instantiate(itemViewPrefab, slot.transform);
        ivNew.transform.localPosition = Vector3.zero;
        ivNew.Setup(slot, item);
        _items[index] = ivNew;
    }

    /// <summary>全スロットを空にして UI に通知</summary>
    public void Clear()
    {
        EnsureInitialized();
        for (int i = 0; i < Model.Capacity; i++)
        {
            Model.Set(i, null);
        }
    }
    // === 便利API: 追加 ===
    #region Add Helpers

    /// <summary>
    /// 最初に入れられる場所へアイテムを追加する。
    /// 1) 登録済みスロットの allowed を尊重して「同種スタック合体」→「空き枠」
    /// 2) スロット未登録などで見つからない場合は allowedMask を用いたフォールバックで
    ///    Model を直接走査して「同種スタック合体」→「空き枠」
    /// </summary>
    /// <param name="item">追加したいアイテム（null不可）</param>
    /// <param name="allowedMask">フォールバック時に許可するタグ（通常は ItemTags.All）</param>
    public bool TryAddFirst(ItemInstance item, ItemTags allowedMask = ItemTags.All)
    {
        EnsureInitialized();
        if (item == null) return false;
        var tag = item.Tags;

        // --- 1) 登録済みスロットを優先（allowed を厳密にチェック） ---
        if (_slots.Count > 0)
        {
            // 同種スタック合体
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
            // 空き枠へ
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

        // --- 2) フォールバック：allowedMask で Model を直接走査 ---
        if ((allowedMask & tag) == 0) return false;

        // 同種スタック合体
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
        // 空き枠へ
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
    /// 指定インデックスに追加（スロットの allowed を尊重）。
    /// 既に同種がありスタック可能なら合体。空きなら配置。その他は false。
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


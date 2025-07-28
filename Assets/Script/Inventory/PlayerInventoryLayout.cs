using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーインベントリのレイアウトを構築：
/// ・下段：規則配置（GridLayoutGroup）
/// ・上段：自由配置（任意のRectTransformアンカー）
///
/// 必要に応じて PlayerPanel 上の見出し（「リール」「餌やルアー」）画像の位置に
/// アンカーを置いて、そこにスロットを生成します。
/// </summary>
public class PlayerInventoryLayout : MonoBehaviour
{
    [Header("Inventory Binder (Player側)")]
    public InventoryBinder binder;          // PlayerPanel に付いているBinder

    [Header("Prefabs")]
    public InventorySlotView slotPrefab;    // Slotプレハブ

    [Header("Bottom (regular grid)")]
    public RectTransform bottomGridParent;  // GridLayoutGroupを持つ親
    [Min(1)] public int bottomCount = 5;    // フリー5枠
    public ItemTags bottomAllowed = ItemTags.All;

    [Header("Top (free placement)")]
    public RectTransform reelAnchor;        // リール用アンカー（自由配置）
    public RectTransform baitOrLureAnchor;  // 餌/ルアー用アンカー（自由配置）

    public ItemTags reelAllowed = ItemTags.Reel;
    public ItemTags baitOrLureAllowed = ItemTags.Bait | ItemTags.Lure;

    [Header("Indices (Model順)")]
    [Tooltip("下段を0..(bottomCount-1)、上段2枠のインデックス")]
    public int reelIndex = 5;  // 例：5
    public int baitOrLureIndex = 6; // 例：6

    [Header("Validate")]
    public bool clearExistingSlotsOnBuild = true; // 再生成時に子スロットを掃除

    void Reset()
    {
        // よくある構成の仮値をセット
        bottomCount = 5; reelIndex = 5; baitOrLureIndex = 6;
    }

    void Awake()
    {
        if (!binder) binder = GetComponentInChildren<InventoryBinder>(true);
        if (!slotPrefab) Debug.LogError("[PlayerInventoryLayout] slotPrefab 未設定");
        if (!binder) Debug.LogError("[PlayerInventoryLayout] binder 未設定");
        if (!bottomGridParent) Debug.LogError("[PlayerInventoryLayout] bottomGridParent 未設定");

        // Binderのcapacityが一致していないと破綻するため注意
        int expected = bottomCount + 2;
        if (binder && binder.capacity != expected)
            Debug.LogWarning($"[PlayerInventoryLayout] binder.capacity={binder.capacity} と想定{expected}が不一致です。Inspectorで合わせてください。");
    }

    void Start()
    {
        BuildLayout();
    }

    public void BuildLayout()
    {
        if (!binder || !slotPrefab) return;

        if (clearExistingSlotsOnBuild)
        {
            // 既存スロットを掃除（UIだけ。モデルは保持）
            CleanupChildren(bottomGridParent);
            if (reelAnchor) CleanupChildren(reelAnchor);
            if (baitOrLureAnchor) CleanupChildren(baitOrLureAnchor);
        }

        // === 下段（0..bottomCount-1） ===
        for (int i = 0; i < bottomCount; i++)
        {
            var slot = Instantiate(slotPrefab, bottomGridParent);
            slot.name = $"Slot_Free_{i}";
            slot.Initialize(binder, i, bottomAllowed, ignoreLayout: false); // Gridの子なのでignoreLayout=false
        }

        // === 上段（自由配置） ===
        if (reelAnchor)
        {
            var slot = Instantiate(slotPrefab, reelAnchor);
            slot.name = "Slot_Reel";
            // 自由配置なので ignoreLayout=true（親がGridでなくても安全）
            slot.Initialize(binder, reelIndex, reelAllowed, ignoreLayout: true);
            // アンカー側のRectTransformの位置をScene上で自由に動かしてください
        }

        if (baitOrLureAnchor)
        {
            var slot = Instantiate(slotPrefab, baitOrLureAnchor);
            slot.name = "Slot_BaitOrLure";
            slot.Initialize(binder, baitOrLureIndex, baitOrLureAllowed, ignoreLayout: true);
        }
    }

    void CleanupChildren(Transform parent)
    {
        if (!parent) return;
        var tmp = new List<Transform>();
        foreach (Transform t in parent) tmp.Add(t);
        foreach (var t in tmp) Destroy(t.gameObject);
    }
}

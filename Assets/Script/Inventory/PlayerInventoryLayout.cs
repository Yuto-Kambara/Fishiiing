using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryLayout : MonoBehaviour
{
    [Header("Inventory Binder (Player側)")]
    public InventoryBinder binder;

    [Header("Prefabs")]
    public InventorySlotView slotPrefab;

    [Header("Bottom (regular grid)")]
    public RectTransform bottomGridParent;
    [Min(1)] public int bottomCount = 5;
    public ItemTags bottomAllowed = ItemTags.All;

    [Header("Top (free placement)")]
    public RectTransform reelAnchor;            // リール用1枠
    public RectTransform baitOrLureAnchor;      // 互換: 単一アンカー
    public RectTransform baitOrLureContainer;   // 複数枠コンテナ（未指定なら baitOrLureAnchor を使用）

    public ItemTags reelAllowed = ItemTags.Reel;
    public ItemTags baitOrLureAllowed = ItemTags.Bait | ItemTags.Lure;

    [Header("Indices (Model順)")]
    [Tooltip("下段を0..(bottomCount-1)、上段のインデックス。bait/lure は baseIndex..baseIndex+slots-1")]
    public int reelIndex = 5;              // 例：5
    public int baitOrLureBaseIndex = 6;    // 例：6

    [Header("Bait/Lure Slots")]
    [Min(1)] public int baitOrLureSlots = 1;   // 可変枠

    [Header("Bait/Lure Vertical Zigzag Layout")]
    [Tooltip("スロットの縦方向の間隔(px)。正値で下へ等間隔に積む")]
    public float baitLureSpacingY = 72f;
    [Tooltip("互い違い時の横方向のズレ量(px)。奇数番のみ右へズラす")]
    public float baitLureStaggerX = 14f;

    [Header("Validate")]
    public bool clearExistingSlotsOnBuild = true;

    void Reset()
    {
        bottomCount = 5; reelIndex = 5; baitOrLureBaseIndex = 6; baitOrLureSlots = 1;
        baitLureSpacingY = 72f; baitLureStaggerX = 14f;
    }

    void Awake()
    {
        if (!binder) binder = GetComponentInChildren<InventoryBinder>(true);
        if (!slotPrefab) Debug.LogError("[PlayerInventoryLayout] slotPrefab 未設定");
        if (!binder) Debug.LogError("[PlayerInventoryLayout] binder 未設定");
        if (!bottomGridParent) Debug.LogError("[PlayerInventoryLayout] bottomGridParent 未設定");

        int requiredMin = bottomCount + 1 /*reel*/ + baitOrLureSlots;
        if (binder && binder.capacity < requiredMin)
            Debug.LogWarning($"[PlayerInventoryLayout] binder.capacity={binder.capacity} < 必要枠{requiredMin}。容量に合わせて生成数をクランプします。");
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
            CleanupChildren(bottomGridParent);
            if (reelAnchor) CleanupChildren(reelAnchor);
            var container = GetBaitLureContainer();
            if (container) CleanupChildren(container);
        }

        // === 下段（0..bottomCount-1） ===
        for (int i = 0; i < bottomCount; i++)
        {
            var slot = Instantiate(slotPrefab, bottomGridParent);
            slot.name = $"Slot_Free_{i}";
            slot.Initialize(binder, i, bottomAllowed, ignoreLayout: false);
        }

        // === 上段：リール ===
        if (reelAnchor)
        {
            var slot = Instantiate(slotPrefab, reelAnchor);
            slot.name = "Slot_Reel";
            slot.Initialize(binder, reelIndex, reelAllowed, ignoreLayout: true);
        }

        // === 上段：餌/ルアー（縦積み＋左右ジグザグ） ===
        var parent = GetBaitLureContainer();
        if (parent)
        {
            // 自動レイアウトは無効化（手動配置）
            RemoveLayoutComponents(parent);

            int maxUsable = Mathf.Max(0, binder.capacity - (bottomCount + 1)); // 残り枠
            int slots = Mathf.Clamp(baitOrLureSlots, 0, maxUsable);

            for (int i = 0; i < slots; i++)
            {
                int modelIndex = baitOrLureBaseIndex + i;
                var slot = Instantiate(slotPrefab, parent);
                slot.name = $"Slot_BaitOrLure_{i}";
                slot.Initialize(binder, modelIndex, baitOrLureAllowed, ignoreLayout: true);

                // 縦に等間隔で積み、奇数番(i=1,3,5,...)だけ右へズラす
                var rt = slot.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // 親の左上基準
                rt.pivot = new Vector2(0f, 1f);

                float x = (i % 2 == 0) ? 0f : baitLureStaggerX; // 偶数=基準、奇数=右へ
                float y = -i * baitLureSpacingY;                // 下方向へ積む（UIは下がマイナス）
                rt.anchoredPosition = new Vector2(x, y);
            }

            if (slots < baitOrLureSlots)
            {
                Debug.LogWarning($"[PlayerInventoryLayout] binder.capacity不足のため、餌/ルアースロットを {baitOrLureSlots} → {slots} にクランプしました。");
            }
        }
    }

    /// <summary>ボタンから呼ぶ：餌/ルアー枠を増やして再レイアウト</summary>
    public void IncreaseBaitLureSlots(int delta = 1)
    {
        baitOrLureSlots = Mathf.Max(1, baitOrLureSlots + Mathf.Max(1, delta));
        BuildLayout();
    }

    RectTransform GetBaitLureContainer()
    {
        if (baitOrLureContainer) return baitOrLureContainer;
        return baitOrLureAnchor ? baitOrLureAnchor : null;
    }

    void RemoveLayoutComponents(Transform t)
    {
        if (!t) return;
        var hl = t.GetComponent<HorizontalLayoutGroup>(); if (hl) DestroyImmediate(hl);
        var vl = t.GetComponent<VerticalLayoutGroup>(); if (vl) DestroyImmediate(vl);
        var gl = t.GetComponent<GridLayoutGroup>(); if (gl) DestroyImmediate(gl);
        var fitter = t.GetComponent<ContentSizeFitter>(); if (fitter) DestroyImmediate(fitter);
    }

    void CleanupChildren(Transform parent)
    {
        if (!parent) return;
        var tmp = new List<Transform>();
        foreach (Transform t in parent) tmp.Add(t);
        foreach (var t in tmp) Destroy(t.gameObject);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryLayout : MonoBehaviour
{
    [Header("Inventory Binder (Player��)")]
    public InventoryBinder binder;

    [Header("Prefabs")]
    public InventorySlotView slotPrefab;

    [Header("Bottom (regular grid)")]
    public RectTransform bottomGridParent;
    [Min(1)] public int bottomCount = 5;
    public ItemTags bottomAllowed = ItemTags.All;

    [Header("Top (free placement)")]
    public RectTransform reelAnchor;            // ���[���p1�g
    public RectTransform baitOrLureAnchor;      // �݊�: �P��A���J�[
    public RectTransform baitOrLureContainer;   // �����g�R���e�i�i���w��Ȃ� baitOrLureAnchor ���g�p�j

    public ItemTags reelAllowed = ItemTags.Reel;
    public ItemTags baitOrLureAllowed = ItemTags.Bait | ItemTags.Lure;

    [Header("Indices (Model��)")]
    [Tooltip("���i��0..(bottomCount-1)�A��i�̃C���f�b�N�X�Bbait/lure �� baseIndex..baseIndex+slots-1")]
    public int reelIndex = 5;              // ��F5
    public int baitOrLureBaseIndex = 6;    // ��F6

    [Header("Bait/Lure Slots")]
    [Min(1)] public int baitOrLureSlots = 1;   // �Ϙg

    [Header("Bait/Lure Vertical Zigzag Layout")]
    [Tooltip("�X���b�g�̏c�����̊Ԋu(px)�B���l�ŉ��֓��Ԋu�ɐς�")]
    public float baitLureSpacingY = 72f;
    [Tooltip("�݂��Ⴂ���̉������̃Y����(px)�B��Ԃ̂݉E�փY����")]
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
        if (!slotPrefab) Debug.LogError("[PlayerInventoryLayout] slotPrefab ���ݒ�");
        if (!binder) Debug.LogError("[PlayerInventoryLayout] binder ���ݒ�");
        if (!bottomGridParent) Debug.LogError("[PlayerInventoryLayout] bottomGridParent ���ݒ�");

        int requiredMin = bottomCount + 1 /*reel*/ + baitOrLureSlots;
        if (binder && binder.capacity < requiredMin)
            Debug.LogWarning($"[PlayerInventoryLayout] binder.capacity={binder.capacity} < �K�v�g{requiredMin}�B�e�ʂɍ��킹�Đ��������N�����v���܂��B");
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

        // === ���i�i0..bottomCount-1�j ===
        for (int i = 0; i < bottomCount; i++)
        {
            var slot = Instantiate(slotPrefab, bottomGridParent);
            slot.name = $"Slot_Free_{i}";
            slot.Initialize(binder, i, bottomAllowed, ignoreLayout: false);
        }

        // === ��i�F���[�� ===
        if (reelAnchor)
        {
            var slot = Instantiate(slotPrefab, reelAnchor);
            slot.name = "Slot_Reel";
            slot.Initialize(binder, reelIndex, reelAllowed, ignoreLayout: true);
        }

        // === ��i�F�a/���A�[�i�c�ς݁{���E�W�O�U�O�j ===
        var parent = GetBaitLureContainer();
        if (parent)
        {
            // �������C�A�E�g�͖������i�蓮�z�u�j
            RemoveLayoutComponents(parent);

            int maxUsable = Mathf.Max(0, binder.capacity - (bottomCount + 1)); // �c��g
            int slots = Mathf.Clamp(baitOrLureSlots, 0, maxUsable);

            for (int i = 0; i < slots; i++)
            {
                int modelIndex = baitOrLureBaseIndex + i;
                var slot = Instantiate(slotPrefab, parent);
                slot.name = $"Slot_BaitOrLure_{i}";
                slot.Initialize(binder, modelIndex, baitOrLureAllowed, ignoreLayout: true);

                // �c�ɓ��Ԋu�Őς݁A���(i=1,3,5,...)�����E�փY����
                var rt = slot.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // �e�̍���
                rt.pivot = new Vector2(0f, 1f);

                float x = (i % 2 == 0) ? 0f : baitLureStaggerX; // ����=��A�=�E��
                float y = -i * baitLureSpacingY;                // �������֐ςށiUI�͉����}�C�i�X�j
                rt.anchoredPosition = new Vector2(x, y);
            }

            if (slots < baitOrLureSlots)
            {
                Debug.LogWarning($"[PlayerInventoryLayout] binder.capacity�s���̂��߁A�a/���A�[�X���b�g�� {baitOrLureSlots} �� {slots} �ɃN�����v���܂����B");
            }
        }
    }

    /// <summary>�{�^������ĂԁF�a/���A�[�g�𑝂₵�čă��C�A�E�g</summary>
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

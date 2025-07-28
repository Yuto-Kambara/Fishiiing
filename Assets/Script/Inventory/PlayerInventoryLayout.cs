using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �v���C���[�C���x���g���̃��C�A�E�g���\�z�F
/// �E���i�F�K���z�u�iGridLayoutGroup�j
/// �E��i�F���R�z�u�i�C�ӂ�RectTransform�A���J�[�j
///
/// �K�v�ɉ����� PlayerPanel ��̌��o���i�u���[���v�u�a�⃋�A�[�v�j�摜�̈ʒu��
/// �A���J�[��u���āA�����ɃX���b�g�𐶐����܂��B
/// </summary>
public class PlayerInventoryLayout : MonoBehaviour
{
    [Header("Inventory Binder (Player��)")]
    public InventoryBinder binder;          // PlayerPanel �ɕt���Ă���Binder

    [Header("Prefabs")]
    public InventorySlotView slotPrefab;    // Slot�v���n�u

    [Header("Bottom (regular grid)")]
    public RectTransform bottomGridParent;  // GridLayoutGroup�����e
    [Min(1)] public int bottomCount = 5;    // �t���[5�g
    public ItemTags bottomAllowed = ItemTags.All;

    [Header("Top (free placement)")]
    public RectTransform reelAnchor;        // ���[���p�A���J�[�i���R�z�u�j
    public RectTransform baitOrLureAnchor;  // �a/���A�[�p�A���J�[�i���R�z�u�j

    public ItemTags reelAllowed = ItemTags.Reel;
    public ItemTags baitOrLureAllowed = ItemTags.Bait | ItemTags.Lure;

    [Header("Indices (Model��)")]
    [Tooltip("���i��0..(bottomCount-1)�A��i2�g�̃C���f�b�N�X")]
    public int reelIndex = 5;  // ��F5
    public int baitOrLureIndex = 6; // ��F6

    [Header("Validate")]
    public bool clearExistingSlotsOnBuild = true; // �Đ������Ɏq�X���b�g��|��

    void Reset()
    {
        // �悭����\���̉��l���Z�b�g
        bottomCount = 5; reelIndex = 5; baitOrLureIndex = 6;
    }

    void Awake()
    {
        if (!binder) binder = GetComponentInChildren<InventoryBinder>(true);
        if (!slotPrefab) Debug.LogError("[PlayerInventoryLayout] slotPrefab ���ݒ�");
        if (!binder) Debug.LogError("[PlayerInventoryLayout] binder ���ݒ�");
        if (!bottomGridParent) Debug.LogError("[PlayerInventoryLayout] bottomGridParent ���ݒ�");

        // Binder��capacity����v���Ă��Ȃ��Ɣj�]���邽�ߒ���
        int expected = bottomCount + 2;
        if (binder && binder.capacity != expected)
            Debug.LogWarning($"[PlayerInventoryLayout] binder.capacity={binder.capacity} �Ƒz��{expected}���s��v�ł��BInspector�ō��킹�Ă��������B");
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
            // �����X���b�g��|���iUI�����B���f���͕ێ��j
            CleanupChildren(bottomGridParent);
            if (reelAnchor) CleanupChildren(reelAnchor);
            if (baitOrLureAnchor) CleanupChildren(baitOrLureAnchor);
        }

        // === ���i�i0..bottomCount-1�j ===
        for (int i = 0; i < bottomCount; i++)
        {
            var slot = Instantiate(slotPrefab, bottomGridParent);
            slot.name = $"Slot_Free_{i}";
            slot.Initialize(binder, i, bottomAllowed, ignoreLayout: false); // Grid�̎q�Ȃ̂�ignoreLayout=false
        }

        // === ��i�i���R�z�u�j ===
        if (reelAnchor)
        {
            var slot = Instantiate(slotPrefab, reelAnchor);
            slot.name = "Slot_Reel";
            // ���R�z�u�Ȃ̂� ignoreLayout=true�i�e��Grid�łȂ��Ă����S�j
            slot.Initialize(binder, reelIndex, reelAllowed, ignoreLayout: true);
            // �A���J�[����RectTransform�̈ʒu��Scene��Ŏ��R�ɓ������Ă�������
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

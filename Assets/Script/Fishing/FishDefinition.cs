using UnityEngine;

/// <summary>
/// ScriptableObject 1 �t�@�C�� = ���� 1 ��
/// �C���X�y�N�^�Œl����͂��邾���� �g�V��h ���ǉ��ł���
/// </summary>
[CreateAssetMenu(fileName = "FishDef_", menuName = "Fishing/Fish Definition", order = 0)]
public class FishDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string speciesName = "Salmon";
    public FishRarity rarity = FishRarity.Common;

    [Header("Economy")]
    public int basePrice = 100;          // �V���b�v���p�̊�l
    public float pricePerCm = 2f;        // ���� 1cm ���Ƃ̉��Z

    [Header("Size Range (cm)")]
    public float minLength = 20f;
    public float maxLength = 60f;

    [Header("Visuals")]
    public Sprite sprite;                // ���̌�����
    public GameObject prefabOverride;    // �v���n�u���ʂɕς������ꍇ (�C��)

    /*------------ ���i�v�Z ------------*/
    public int CalcPrice(float length)
        => basePrice + Mathf.RoundToInt(length * pricePerCm) + (int)rarity * 50;
}

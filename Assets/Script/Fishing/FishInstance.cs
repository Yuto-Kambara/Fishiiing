using UnityEngine;

/// <summary>���̂̋� 1 �C���ێ�����f�[�^</summary>
[System.Serializable]
public struct FishInstance
{
    public FishDefinition def;   // ��f�[�^
    public float lengthCm;       // �̂���
    public int value;          // ���p�z

    public FishInstance(FishDefinition d, float len)
    {
        def = d;
        lengthCm = len;
        value = d.CalcPrice(len);
    }
}

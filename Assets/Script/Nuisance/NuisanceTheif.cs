using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �C�i�����j�� �΂߂Œ�h���[�֏㗤 �� �����ŋ���D�� ��
/// ��h���[�֐����A�� �� �΂߂ŊC�֖߂�
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NuisanceThief : MonoBehaviour
{
    [Header("Move Speeds (m/s)")]
    public float diagonalSpeed = 2.8f;   // �㗤 & ����
    public float horizontalSpeed = 2.0f;   // ��h�㐅��

    /*=== �^�[�Q�b�g & ���W ===*/
    private List<FishProjectile> targets;   // �ő� 2 �C
    private int tIndex = 0;
    private int collected = 0;

    private Vector3 edgePos;                // ��h���[ (���)
    private Vector3 seaReturnPos;           // �C�֖߂���W

    /*=== �X�e�[�g ===*/
    private enum Phase { ApproachDiag, WalkFish, WalkEdgeBack, RetreatDiag }
    private Phase phase = Phase.ApproachDiag;

    private SpriteRenderer sr;

    /*--------------------------------------------------*/
    public void Initialize(List<FishProjectile> fishTargets,
                           Vector3 edgePosition, Vector3 returnPosition)
    {
        targets = fishTargets;
        edgePos = edgePosition;
        seaReturnPos = returnPosition;
        sr = GetComponent<SpriteRenderer>();
        FaceRight(true);                    // �E�����œo��
    }

    private void Update()
    {
        switch (phase)
        {
            case Phase.ApproachDiag: ApproachDiag(); break;
            case Phase.WalkFish: WalkFish(); break;
            case Phase.WalkEdgeBack: WalkEdgeBack(); break;
            case Phase.RetreatDiag: RetreatDiag(); break;
        }
    }

    /*========== �΂߂ɒ�h�[�֏オ�� ==========*/
    private void ApproachDiag()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, edgePos, diagonalSpeed * Time.deltaTime);

        if (Vector3.SqrMagnitude(transform.position - edgePos) < 0.01f)
            phase = Phase.WalkFish;          // �㗤���� �� ����
    }

    /*========== �����ŋ���D�� ==========*/
    private void WalkFish()
    {
        if (tIndex >= targets.Count || collected >= 2)
        {
            FaceRight(false);
            phase = Phase.WalkEdgeBack;
            return;
        }

        FishProjectile fish = targets[tIndex];
        if (!fish || fish.IsHeld) { tIndex++; return; }

        Vector3 pos = transform.position;
        Vector3 tgt = fish.transform.position;
        tgt.y = pos.y;                       // �����Œ�

        pos.x = Mathf.MoveTowards(pos.x, tgt.x, horizontalSpeed * Time.deltaTime);
        transform.position = pos;

        if (Mathf.Abs(pos.x - tgt.x) < 0.08f)
        {   // �D��
            Destroy(fish.gameObject);
            collected++; tIndex++;

            if (collected >= 2) { FaceRight(false); phase = Phase.WalkEdgeBack; }
        }
    }

    /*========== ��h�[�֐����Ŗ߂� ==========*/
    private void WalkEdgeBack()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.MoveTowards(pos.x, edgePos.x, horizontalSpeed * Time.deltaTime);
        transform.position = pos;

        if (Mathf.Abs(pos.x - edgePos.x) < 0.05f)
            phase = Phase.RetreatDiag;       // �[�ɒ�������C��
    }

    /*========== �΂߂ŊC�։��� ==========*/
    private void RetreatDiag()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, seaReturnPos, diagonalSpeed * Time.deltaTime);

        if (Vector3.SqrMagnitude(transform.position - seaReturnPos) < 0.02f)
            Destroy(gameObject);             // �C�֏�����
    }

    private void FaceRight(bool right)
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        sr.flipX = !right;
    }
}

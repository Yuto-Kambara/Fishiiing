using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// äCÅiç∂â∫ÅjÅ® éŒÇﬂÇ≈íÁñhç∂í[Ç÷è„ó§ Å® êÖïΩÇ≈ãõÇíDÇ§ Å®
/// íÁñhç∂í[Ç÷êÖïΩãAä“ Å® éŒÇﬂÇ≈äCÇ÷ñﬂÇÈ
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NuisanceThief : MonoBehaviour
{
    [Header("Move Speeds (m/s)")]
    public float diagonalSpeed = 2.8f;   // è„ó§ & â∫ÇË
    public float horizontalSpeed = 2.0f;   // íÁñhè„êÖïΩ

    /*=== É^Å[ÉQÉbÉg & ç¿ïW ===*/
    private List<FishProjectile> targets;   // ç≈ëÂ 2 ïC
    private int tIndex = 0;
    private int collected = 0;

    private Vector3 edgePos;                // íÁñhç∂í[ (è„ñ )
    private Vector3 seaReturnPos;           // äCÇ÷ñﬂÇÈç¿ïW

    /*=== ÉXÉeÅ[Ég ===*/
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
        FaceRight(true);                    // âEå¸Ç´Ç≈ìoèÍ
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

    /*========== éŒÇﬂÇ…íÁñhí[Ç÷è„Ç™ÇÈ ==========*/
    private void ApproachDiag()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, edgePos, diagonalSpeed * Time.deltaTime);

        if (Vector3.SqrMagnitude(transform.position - edgePos) < 0.01f)
            phase = Phase.WalkFish;          // è„ó§äÆóπ Å® ãõÇ÷
    }

    /*========== êÖïΩÇ≈ãõÇíDÇ§ ==========*/
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
        tgt.y = pos.y;                       // çÇÇ≥å≈íË

        pos.x = Mathf.MoveTowards(pos.x, tgt.x, horizontalSpeed * Time.deltaTime);
        transform.position = pos;

        if (Mathf.Abs(pos.x - tgt.x) < 0.08f)
        {   // íDéÊ
            Destroy(fish.gameObject);
            collected++; tIndex++;

            if (collected >= 2) { FaceRight(false); phase = Phase.WalkEdgeBack; }
        }
    }

    /*========== íÁñhí[Ç÷êÖïΩÇ≈ñﬂÇÈ ==========*/
    private void WalkEdgeBack()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.MoveTowards(pos.x, edgePos.x, horizontalSpeed * Time.deltaTime);
        transform.position = pos;

        if (Mathf.Abs(pos.x - edgePos.x) < 0.05f)
            phase = Phase.RetreatDiag;       // í[Ç…íÖÇ¢ÇΩÇÁäCÇ÷
    }

    /*========== éŒÇﬂÇ≈äCÇ÷â∫ÇÈ ==========*/
    private void RetreatDiag()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, seaReturnPos, diagonalSpeed * Time.deltaTime);

        if (Vector3.SqrMagnitude(transform.position - seaReturnPos) < 0.02f)
            Destroy(gameObject);             // äCÇ÷è¡Ç¶ÇÈ
    }

    private void FaceRight(bool right)
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        sr.flipX = !right;
    }
}

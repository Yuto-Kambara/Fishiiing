using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GridLayoutGroup �̊ȈՊg����:
/// �E��/�Z���T�C�Y/�Ԋu/�]��
/// �EFit(�e�T�C�Y�ɍ��킹�ăZ�������v�Z)
/// �E�q�� GridNudge ������ΌʃI�t�Z�b�g
/// </summary>
[AddComponentMenu("UI/Flexible Grid Layout Group")]
public class FlexibleGridLayoutGroup : LayoutGroup
{
    [Min(1)] public int columns = 5;
    public Vector2 cellSize = new Vector2(96, 96);
    public Vector2 spacing = new Vector2(8, 8);

    [Header("Auto Fit (optional)")]
    public bool fitCellWidth = false;
    public bool fitCellHeight = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        MarkDirty();
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        if (columns < 1) columns = 1;

        if (fitCellWidth)
        {
            var rect = rectTransform.rect;
            float totalSpacing = spacing.x * Mathf.Max(0, columns - 1);
            float totalPadding = padding.left + padding.right;
            float w = (rect.width - totalPadding - totalSpacing) / columns;
            cellSize.x = Mathf.Max(1f, w);
        }

        MarkDirty();
    }

    public override void CalculateLayoutInputVertical()
    {
        int activeChildCount = 0;
        for (int i = 0; i < rectChildren.Count; i++)
            if (rectChildren[i].gameObject.activeSelf) activeChildCount++;

        int rows = Mathf.CeilToInt(activeChildCount / (float)columns);

        if (fitCellHeight && rows > 0)
        {
            var rect = rectTransform.rect;
            float totalSpacing = spacing.y * Mathf.Max(0, rows - 1);
            float totalPadding = padding.top + padding.bottom;
            float h = (rect.height - totalPadding - totalSpacing) / rows;
            cellSize.y = Mathf.Max(1f, h);
        }
    }

    public override void SetLayoutHorizontal()
    {
        PlaceChildren();
    }

    public override void SetLayoutVertical()
    {
        PlaceChildren();
    }

    void PlaceChildren()
    {
        for (int i = 0; i < rectChildren.Count; i++)
        {
            var child = rectChildren[i];
            if (!child.gameObject.activeSelf) continue;

            int col = i % columns;
            int row = i / columns;

            float x = padding.left + (cellSize.x + spacing.x) * col;
            float y = padding.top + (cellSize.y + spacing.y) * row;

            var nudge = child.GetComponent<GridNudge>();
            if (nudge) { x += nudge.offset.x; y += nudge.offset.y; }

            // axis 0: X, axis 1: Y�iUI�͏㌴�_�Ȃ̂�Y�͂��̂܂܎w���OK�j
            SetChildAlongAxis(child, 0, x, cellSize.x);
            SetChildAlongAxis(child, 1, y, cellSize.y);
        }
    }

    // �� �x������̂��߁A���Ɠ����� SetDirty �͍�炸�A�ʖ��ɕύX
    void MarkDirty()
    {
        if (!IsActive()) return;
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    // �e�̃T�C�Y�ύX���ɂ��ă��C�A�E�g�i�C�Ӂj
    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        MarkDirty();
    }
}

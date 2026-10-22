using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class GridAutoSize : MonoBehaviour
{
    public GridLayoutGroup grid;
    public int columns = 3;

    void Update()
    {
        if (!grid) return;

        RectTransform rt = grid.GetComponent<RectTransform>();

        float width = rt.rect.width;

        float padding = grid.padding.left + grid.padding.right;
        float spacing = grid.spacing.x * (columns - 1);

        float cellSize = (width - padding - spacing) / columns;

        grid.cellSize = new Vector2(cellSize, cellSize); // квадрат
    }
}
using System.Collections.Generic;
using UnityEngine;

public class MenuBackgroundGrid : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform container;
    [SerializeField] private RectTransform prefab;

    [Header("Grid")]
    [SerializeField] private int rows = 6;
    [SerializeField] private int columns = 10;
    [SerializeField] private float spacingX = 150f;
    [SerializeField] private float spacingY = 150f;
    [SerializeField] private bool hexOffset = true; // как соты

    [Header("Move")]
    [SerializeField] private float speed = 50f;

    private List<RectTransform> items = new List<RectTransform>();

    private float leftBound;
    private float rightBound;

    void Start()
    {
        CalculateBounds();
        CreateGrid();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        foreach (var item in items)
        {
            Vector2 pos = item.anchoredPosition;
            pos.x += speed * dt;

            // если вышел за экран то переносим влево
            if (pos.x > rightBound + spacingX)
            {
                pos.x -= (columns * spacingX);
            }

            item.anchoredPosition = pos;
        }
    }

    void CreateGrid()
    {
        float startX = leftBound;
        float startY = (rows - 1) * spacingY * 0.5f;

        for (int y = 0; y < rows; y++)
        {
            float offsetX = 0f;

            // смещение
            if (hexOffset && y % 2 == 1)
                offsetX = spacingX * 0.5f;

            for (int x = 0; x < columns; x++)
            {
                RectTransform obj = Instantiate(prefab, container);

                float posX = startX + x * spacingX + offsetX;
                float posY = startY - y * spacingY;

                obj.anchoredPosition = new Vector2(posX, posY);

                items.Add(obj);
            }
        }
    }

    void CalculateBounds()
    {
        float width = container.rect.width;

        leftBound = -width / 2f;
        rightBound = width / 2f;
    }
}
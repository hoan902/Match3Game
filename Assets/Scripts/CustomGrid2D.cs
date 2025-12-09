using TMPro;
using UnityEngine;
using Sirenix;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using System;

public class CustomGrid2D<TCellObject> : MonoBehaviour
{
    private int m_width;
    private int m_height;
    private float m_cellSize;
    private Transform m_grid2DContainer;
    private TCellObject[,] m_cellArray;
    private CellData[,] m_cellsDataArray;

    [Title("-- Debug Attributes")]
    [SerializeField] private TextMeshProUGUI m_gridPosTxt;
    [SerializeField] private int m_textSize = 20;

    public CustomGrid2D(int width, int height, float cellSize, bool isDebug, Transform grid2DContainer, Func<CustomGrid2D<TCellObject>, int, int, TCellObject> cellObject)
    {
        this.m_width = width;
        this.m_height = height;
        this.m_cellSize = cellSize;

        m_cellArray = new TCellObject[width, height];
        m_cellsDataArray = new CellData[width, height];

        m_grid2DContainer = grid2DContainer;
        m_grid2DContainer.name = "Grid2D";
        m_grid2DContainer.position = Vector3.zero;
        for (int x = 0; x < m_cellArray.GetLength(0); x++)
        {
            for (int y = 0; y < m_cellArray.GetLength(1); y++)
            {
                //Setup Cell Data and Cell Objects
                CellData cellData = new CellData();
                cellData.x = x;
                cellData.y = y;
                m_cellsDataArray[x,y] = cellData;
                m_cellArray[x, y] = cellObject(this, x, y);

                //Debugs text for cells
                if (isDebug)
                {
                    var gameObj = new GameObject($"cell: {x} - {y}");
                    gameObj.transform.parent = m_grid2DContainer.transform;
                    gameObj.AddComponent<TextMesh>();
                    var text = gameObj.GetComponent<TextMesh>();
                    text.text = $"{x}-{y}";
                    text.anchor = TextAnchor.MiddleCenter;
                    text.alignment = TextAlignment.Center;
                    text.color = Color.white;
                    text.fontSize = (int)cellSize * 3;
                    text.transform.position = GetWorldPos(x, y) + new Vector3(cellSize, cellSize) * .5f;
                }
                //Draw cell grid 1
                Debug.DrawLine(GetWorldPos(x, y), GetWorldPos(x, y + 1), Color.white, 200f);
                Debug.DrawLine(GetWorldPos(x, y), GetWorldPos(x + 1, y), Color.white, 200f);
            }
        }
        //Draw cell grid 1
        Debug.DrawLine(GetWorldPos(0, height), GetWorldPos(width, height), Color.white, 200f);
        Debug.DrawLine(GetWorldPos(width, 0), GetWorldPos(width, height), Color.white, 200f);

        //Setup Camera to center
        UpdateCamera();
    }

    public CellData GetCellData(int x, int y)
    {
        return m_cellsDataArray[x, y];
    }

    public int GetWidth()
    {
        return m_width;
    }
    public int GetHeight()
    {
        return m_height;
    }
    public TCellObject[,] GetCellArr()
    {
        return m_cellArray;
    }
    public TCellObject GetCellObject(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < m_width && y < m_height)
        {
            return m_cellArray[x, y];
        }
        else
        {
            return default;
        }
    }

    public void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt(worldPosition.x / m_cellSize);
        y = Mathf.FloorToInt(worldPosition.y / m_cellSize);
    }

    public void SetupCellData(int x, int y, CellScriptableObj cellType, GemScriptableObj containedGem)
    {
        if (m_cellsDataArray[x, y] == null)
        {
            Debug.LogError("ERROR-customGrid2D: Cannot setup this Cell Data, Cell Data is null!!");
            return;
        }
        var cellData = m_cellsDataArray[x, y];
        cellData.cellTypeInfo = cellType; 
        if (cellData.cellTypeInfo.cellType != CellType.Blocked)
            cellData.currentContainedGem = containedGem;
    }

    public void UpdateCamera(Transform cameraTransform = null)
    {
        float gridWorldWidth = m_width * m_cellSize;
        float gridWorldHeight = m_height * m_cellSize;
        Vector2 gridCenter = new Vector2(gridWorldWidth / 2f, gridWorldHeight / 2f);
        Camera camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : Camera.main;
        camera.transform.position = new Vector3(gridCenter.x, gridCenter.y, -10);

        float extraSize = 5f;
        float sizeByHeight = gridWorldHeight / 2f + extraSize;
        float sizeByWidth = gridWorldWidth / (2f * camera.aspect) + extraSize;
        camera.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }


    public Vector3 GetWorldPos(int x, int y)
    {
        return new Vector3(x, y) * m_cellSize;
    }

    public float GetCellSize()
    {
        return m_cellSize;
    }

    public Transform GetGridContainer()
    {
        return m_grid2DContainer;
    }
}

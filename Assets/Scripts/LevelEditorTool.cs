using CodeMonkey.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/*
 * Editor Controllers: 
 *   Number Keys 1-5 = Set Gem Type
 *   Number 1 (numpad) = swaping cell type
 *   Right Click = Change Cell types
 *   -- Self note: Might put these instruction on gamescene for Game Designer in future if necessary
 * */

public class LevelEditorTool : MonoBehaviour
{
    [SerializeField] private LevelScriptableObj m_levelSO;
    [SerializeField] private Transform m_gemPrefab;
    [SerializeField] private Transform m_gridBackgroundPrefab;
    [SerializeField] private Transform m_cameraTransform;
    [SerializeField] private Transform m_gridContainer;
    [SerializeField] private TextMeshProUGUI m_levelText;
    [SerializeField] private TextMeshProUGUI m_currentCellType;

    private CustomGrid2D<EditorVisualCell> m_grid2D;
    private int m_typeCount = 0;
    private CellScriptableObj m_selectedCellType;

    private void Awake()
    {
        //Creating blank grid
        m_grid2D = new CustomGrid2D<EditorVisualCell>(m_levelSO.width, m_levelSO.height, 5f, true, m_gridContainer, (CustomGrid2D<EditorVisualCell> g, int x, int y) => new EditorVisualCell(m_levelSO, g, x, y));
        //Set name of level
        m_levelText.text = m_levelSO.levelName;
        if (m_levelSO.levelName == "")
            m_levelText.text = m_levelSO.name;

        //Creating random level if not custom nor template level
        if (m_levelSO.levelCellList.Count == 0)
        {
            // Generate random gem level
            Debug.Log("LOG-LevelEditorTool: Generating new level...");
            m_levelSO.levelCellList = new List<CellData>();
            for (int x = 0; x < m_grid2D.GetWidth(); x++)
            {
                for (int y = 0; y < m_grid2D.GetHeight(); y++)
                {
                    GemScriptableObj gem = GetRandomValidGem(x,y);
                    CellScriptableObj cellType = m_levelSO.availableCellTypeList[0]; //Default should always be the normal cell
                    CellData levelCellData = new CellData { x = x, y = y, cellTypeInfo = cellType, currentContainedGem = gem };
                    m_levelSO.levelCellList.Add(levelCellData);
                    m_grid2D.GetCellObject(x, y).cellData = levelCellData;
                    CreateVisualCell(m_grid2D.GetCellObject(x,y), levelCellData);
                }
            }
        }
        else
        {
            // Loading level
            Debug.Log("LOG-LevelEditorTool: Loading custom level...");
            for (int x = 0; x < m_grid2D.GetWidth(); x++)
            {
                for (int y = 0; y < m_grid2D.GetHeight(); y++)
                {
                    CellData levelCellData = null;
                    foreach (CellData loadedCellData in m_levelSO.levelCellList)
                    {
                        if (loadedCellData.x == x && loadedCellData.y == y)
                        {
                            levelCellData = loadedCellData;
                            break;
                        }
                    }

                    if (levelCellData == null)
                    {
                        GemScriptableObj gem = m_levelSO.availableGemList[Random.Range(0, m_levelSO.availableGemList.Count)];
                        levelCellData = new CellData { x = x, y = y, currentContainedGem = gem };
                        m_levelSO.levelCellList.Add(levelCellData);
                    }
                    m_grid2D.GetCellObject(x, y).cellData = levelCellData;
                    CreateVisualCell(m_grid2D.GetCellObject(x, y), levelCellData);
                }
            }
        }
        m_grid2D.UpdateCamera(m_cameraTransform);
        SwapCellTypeEditor();
    }

    private void Update()
    {
        Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();

        m_grid2D.GetXY(mouseWorldPosition, out int x, out int y);
        //Set Gem and Cell by Mouse position and key input
        if (IsValidPosition(x, y))
        {
            if (Input.GetKey(KeyCode.Alpha1))
                m_grid2D.GetCellObject(x, y).SetGem(m_levelSO.availableGemList[0], true);
            if (Input.GetKey(KeyCode.Alpha2))
                m_grid2D.GetCellObject(x, y).SetGem(m_levelSO.availableGemList[1], true);
            if (Input.GetKey(KeyCode.Alpha3))
                m_grid2D.GetCellObject(x, y).SetGem(m_levelSO.availableGemList[2], true);
            if (Input.GetKey(KeyCode.Alpha4))
                m_grid2D.GetCellObject(x, y).SetGem(m_levelSO.availableGemList[3], true);
            if (Input.GetKey(KeyCode.Alpha5))
                m_grid2D.GetCellObject(x, y).SetGem(m_levelSO.availableGemList[4], true);
            if (Input.GetKey(KeyCode.Alpha6))
                m_grid2D.GetCellObject(x, y).SetGem(m_levelSO.ramdomGenerateGem, true);

            if (Input.GetKeyDown(KeyCode.Keypad1))
                SwapCellTypeEditor();

            if (Input.GetMouseButton(1))
            {
                if (m_selectedCellType == null)
                    return;
                var cellObj = m_grid2D.GetCellObject(x, y);
                cellObj.SetBlockedCell(m_selectedCellType);
            }
        }
    }

    //
    private GemScriptableObj GetRandomValidGem(int x, int y)
    {
        if (m_levelSO.availableGemList.Count == 0)
            return null;
        List<GemScriptableObj> validGemPool = new List<GemScriptableObj>(m_levelSO.availableGemList);

        // Check left 2:
        if (x >= 2)
        {
            CellData left1 = GetLevelCellDataByPos(x - 1, y);
            CellData left2 = GetLevelCellDataByPos(x - 2, y);
            if (left1 != null && left2 != null && left1.currentContainedGem != null && left2.currentContainedGem != null)
            {
                if (GlobalFunction.CompareGemType(left1.currentContainedGem, left2.currentContainedGem))
                    GlobalFunction.RemoveGemTypeFromList(validGemPool, left1.currentContainedGem);
            }
        }

        // Check down 2:
        if (y >= 2)
        {
            CellData down1 = GetLevelCellDataByPos(x, y - 1);
            CellData down2 = GetLevelCellDataByPos(x, y - 2);
            if (down1 != null && down2 != null && down1.currentContainedGem != null && down2.currentContainedGem != null)
            {
                if (GlobalFunction.CompareGemType(down1.currentContainedGem, down2.currentContainedGem))
                    GlobalFunction.RemoveGemTypeFromList(validGemPool, down1.currentContainedGem);
            }
        }

        // If after removing we still have at least one type, pick one; otherwise fallback to random from all candidates
        if (validGemPool.Count == 0)
            return m_levelSO.availableGemList[Random.Range(0, m_levelSO.availableGemList.Count)];

        return validGemPool[Random.Range(0, validGemPool.Count)];
    }
    CellData GetLevelCellDataByPos(int x, int y)
    {
        foreach (CellData cellData in m_levelSO.levelCellList)
            if (cellData.x == x && cellData.y == y)
                return cellData;
        return null;
    }
    
    //

    private void SwapCellTypeEditor()
    {
        int availableCellTypeCount = m_levelSO.availableCellTypeList.Count;
        if (availableCellTypeCount > 0)
        {
            m_selectedCellType = m_levelSO.availableCellTypeList[m_typeCount];
            m_typeCount++;
            if (m_typeCount == availableCellTypeCount)
                m_typeCount = 0;
        }
        if (m_selectedCellType != null)
            m_currentCellType.text = $"Current Cell brush: {m_selectedCellType.cellName}";
        else
            m_currentCellType.text = "ERROR: No Available Cell Types (Level cant be edit)";
    }

    private void CreateVisualCell(EditorVisualCell visualCellPos, CellData cellData)
    {
        //Setup Cell Data
        visualCellPos.cellData = cellData;

        bool isBlockedCell = cellData.cellTypeInfo.cellType == CellType.Blocked;

        //Setup Background cell
        Transform cellSlotVisualTransform = Instantiate(m_gridBackgroundPrefab, visualCellPos.GetWorldPosition(cellData.x, cellData.y), Quaternion.identity);
        cellSlotVisualTransform.name = $"cell Bg: {cellData.x} - {cellData.y}";
        cellSlotVisualTransform.transform.SetParent(visualCellPos.GetGridContainer());
        cellSlotVisualTransform.transform.localScale = Vector3.one * visualCellPos.GetCellSize();
        visualCellPos.bgSpriteRenderer = cellSlotVisualTransform.Find("sprite").GetComponent<SpriteRenderer>();

        //Setup Gem visual
        Transform gemVisualTransform = Instantiate(m_gemPrefab, visualCellPos.GetWorldPosition(cellData.x, cellData.y), Quaternion.identity);
        gemVisualTransform.name = isBlockedCell ? "Gem: Null" : $"Gem: {cellData.currentContainedGem.gemName}";
        gemVisualTransform.transform.SetParent(cellSlotVisualTransform);
        gemVisualTransform.transform.localScale = Vector3.one;
        visualCellPos.gemSpriteRenderer = gemVisualTransform.Find("sprite").GetComponent<SpriteRenderer>();
        visualCellPos.gemSpriteRenderer.sortingOrder = visualCellPos.bgSpriteRenderer.sortingOrder + 1;

        //Remove gem if cell is blocked
        //Start Spawning Gem and set Cell Status
        if (isBlockedCell)
            visualCellPos.gemSpriteRenderer.sprite = null;
        else
            visualCellPos.SetGem(cellData.currentContainedGem);
        visualCellPos.SetBlockedCell(cellData.cellTypeInfo);
    }

    private bool IsValidPosition(int x, int y)
    {
        if (x < 0 || y < 0 ||
            x >= m_grid2D.GetWidth() || y >= m_grid2D.GetHeight())
        {
            // Invalid position
            return false;
        }
        else
        {
            return true;
        }
    }

    //Visual cell wrapper to show board editor
    private class EditorVisualCell
    {
        public SpriteRenderer gemSpriteRenderer;
        public CellData cellData;
        public SpriteRenderer bgSpriteRenderer;

        private LevelScriptableObj levelSO;
        private CustomGrid2D<EditorVisualCell> grid2D;
        private int x;
        private int y;

        public EditorVisualCell(LevelScriptableObj levelSO, CustomGrid2D<EditorVisualCell> grid, int x, int y)
        {
            this.levelSO = levelSO;
            this.grid2D = grid;
            this.x = x;
            this.y = y;
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            return grid2D.GetWorldPos(x, y);
        }

        public float GetCellSize()
        {
            return grid2D.GetCellSize();
        }

        public Transform GetGridContainer()
        {
            return grid2D.GetGridContainer();
        }

        public void SetGem(GemScriptableObj gemSO, bool isMouseClick = false)
        {
            //Set this to only check if using editor not when generate level!!
            if (cellData.cellTypeInfo.cellType  == CellType.Blocked)
            {
                if(isMouseClick)
                    Debug.LogError("ERROR-LevelEditorTool: Cannot place Gem in blocked slot!");
                return;
            }
            gemSpriteRenderer.sprite = gemSO.sprite;
            cellData.currentContainedGem = gemSO;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(levelSO);
#endif
        }

        public void SetBlockedCell(CellScriptableObj cellType)
        {
            cellData.cellTypeInfo = cellType;
            bgSpriteRenderer.sprite = cellType.sprite;
            if (cellType.cellType == CellType.Blocked)
                gemSpriteRenderer.sprite = null;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(levelSO);
#endif
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [Header("Board Configs")]
    [SerializeField] float m_cellSize = 5f;        // should match your CustomGrid2D cell size
    [SerializeField] int m_maxGenerateTries = 30;  // max attempts to regenerate when no available move found
    [SerializeField] private Transform m_gridContainer;

    private GameMaster m_gameMaster;
    private CustomGrid2D<RuntimeCell> m_runtimeGrid;
    private List<GemScriptableObj> m_gemPool = new List<GemScriptableObj>();
    private LevelScriptableObj m_runtimeOnlyLevel;
    private LevelScriptableObj m_selectedLevel;

    //small runtime wrapper to ref grid
    private class RuntimeCell
    {
        public CustomGrid2D<RuntimeCell> grid;
        public int x;
        public int y;
        public RuntimeCell(CustomGrid2D<RuntimeCell> g, int x, int y)
        {
            this.grid = g; this.x = x; this.y = y;
        }
    }

    #region ------------ Public Initial Func
    public void Init(GameMaster gameMaster, LevelScriptableObj selectedLevel)
    {
        m_gameMaster = gameMaster;
        m_selectedLevel = selectedLevel;
        GenerateLevel();
    }
    #endregion

    #region ------------ Generating logic (no visual)
    //----------------------------------------
    // Generating level
    //----------------------------------------
    private void GenerateLevel()
    {
        //Check level selected
        if (m_selectedLevel == null)
        {
            Debug.LogError("ERROR-BoardGenerator: Level not selected!");
            return;
        }

        //Set game state while generating level
        m_gameMaster.SetGameState(GameState.Busy);

        //*** Clone the Level Scriptable Object so it doesn't directly change the level data
        m_runtimeOnlyLevel = ScriptableObject.CreateInstance<LevelScriptableObj>();
        CloneLevel(m_selectedLevel, m_runtimeOnlyLevel);

        //Clean and Start Init
        m_gemPool.Clear();
        if (m_runtimeOnlyLevel.availableGemList != null && m_runtimeOnlyLevel.availableGemList.Count > 0)
            m_gemPool.AddRange(m_runtimeOnlyLevel.availableGemList);
        else
            Debug.LogError("ERROR-BoardGenerator: list gem is empty. Check if there gem in the list or if the list is null (Check Level Scriptable Object)!.");
        InitGrid();
        GenerateValidBoardLoop();
    }
    //----------------------------------------
    // Set Clone Level (To be the temp data for runtime only)
    //----------------------------------------
    void CloneLevel(LevelScriptableObj sourceLevel, LevelScriptableObj cloneLevel)
    {
        cloneLevel.levelName = sourceLevel.levelName;
        cloneLevel.width = sourceLevel.width;
        cloneLevel.height = sourceLevel.height;
        cloneLevel.moveAmount = sourceLevel.moveAmount;
        cloneLevel.goalType = sourceLevel.goalType;
        cloneLevel.targetScore = sourceLevel.targetScore;
        cloneLevel.availableGemList = new List<GemScriptableObj>(sourceLevel.availableGemList);
        cloneLevel.ramdomGenerateGem = sourceLevel.ramdomGenerateGem;
        cloneLevel.availableCellTypeList = new List<CellScriptableObj>(sourceLevel.availableCellTypeList);
        cloneLevel.levelCellList = new List<CellData>();

        foreach (var cell in sourceLevel.levelCellList)
        {
            var newCell = new CellData();
            newCell.x = cell.x;
            newCell.y = cell.y;
            newCell.cellTypeInfo = cell.cellTypeInfo;
            newCell.currentContainedGem = cell.currentContainedGem;
            cloneLevel.levelCellList.Add(newCell);
        }
    }

    //----------------------------------------
    // Initial Base Grid
    //----------------------------------------
    void InitGrid()
    {
        if (m_gridContainer.childCount > 0)
        {
            foreach (Transform cell in m_gridContainer)
                Destroy(cell.gameObject);
        }
        m_runtimeGrid = new CustomGrid2D<RuntimeCell>(m_runtimeOnlyLevel.width, m_runtimeOnlyLevel.height, m_cellSize, false, m_gridContainer,
            (CustomGrid2D<RuntimeCell> g, int x, int y) => new RuntimeCell(g, x, y));
    }

    //----------------------------------------
    // Loop Generating level until it valid
    //----------------------------------------
    void GenerateValidBoardLoop()
    {
        int tries = 0;
        do
        {
            GenerateBoardPreventMatches();
            tries++;
            if (tries >= m_maxGenerateTries)
            {
                Debug.LogWarning($"BoardGenerator: reached {m_maxGenerateTries} tries. Stopping with current board.");
                break;
            }
        } while (!IsTherePossibleMove());
        Debug.Log("BoardGenerator: finished generating board (no initial matches, at least one move).");
    }

    //----------------------------------------
    // Generate board while avoid having match-3 at initial case
    //----------------------------------------
    void GenerateBoardPreventMatches()
    {
        //Ensure levelCellList exists (if not, generate new)
        EnsureLevelCellListExists();

        //Fill cells left-to-right, bottom-to-top (same as your CustomGrid2D)
        for (int x = 0; x < m_runtimeGrid.GetWidth(); x++)
        {
            for (int y = 0; y < m_runtimeGrid.GetHeight(); y++)
            {
                CellData levelCellData = GetLevelCellDataByPos(x, y);
                if (levelCellData == null)
                {
                    Debug.LogError($"ERROR-BoardGenerator: no CellData at {x},{y}");
                    continue;
                }

                //If this cell is blocked, clear any gem
                if (levelCellData.cellTypeInfo != null && levelCellData.cellTypeInfo.cellType == CellType.Blocked)
                {
                    levelCellData.currentContainedGem = null;
                    continue;
                }

                //If the level file specifies a concrete gem that is NOT the random placeholder, keep it.
                //Otherwise we choose a random gem from the available pool, but we ensure it doesn't create a 3-in-row.
                GemScriptableObj currentGem = levelCellData.currentContainedGem;
                bool isRandom = (currentGem == null) || IsRandomGeneratedGem(currentGem);
                if (isRandom)
                {
                    //pick a random valid gem that will not create immediate match
                    levelCellData.currentContainedGem = GetRandomValidGem(x, y);
                }

                //Write back to the grid's internal CellData (CustomGrid2D made one on initialization)
                m_runtimeGrid.SetupCellData(x, y, levelCellData.cellTypeInfo, levelCellData.currentContainedGem);
            }
        }
    }

    //----------------------------------------
    // Ensure levelSO.levelCellList not empty
    //----------------------------------------
    void EnsureLevelCellListExists()
    {
        if (m_runtimeOnlyLevel.levelCellList == null)
        {
            Debug.LogWarning("WARNING-BoardGenerator: The list is Null, Generate new list of gem randomly");
            m_runtimeOnlyLevel.levelCellList = new List<CellData>();
        }

        for (int x = 0; x < m_runtimeGrid.GetWidth(); x++)
        {
            for (int y = 0; y < m_runtimeGrid.GetHeight(); y++)
            {
                bool found = false;
                foreach (CellData cellData in m_runtimeOnlyLevel.levelCellList)
                {
                    if (cellData.x == x && cellData.y == y)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    var newCell = new CellData { x = x, y = y, cellTypeInfo = m_runtimeOnlyLevel.availableCellTypeList[0], currentContainedGem = null };
                    m_runtimeOnlyLevel.levelCellList.Add(newCell);
                }
            }
        }
    }

    //----------------------------------------
    // Return a CellData from the levelSO list by x,y
    //----------------------------------------
    CellData GetLevelCellDataByPos(int x, int y)
    {
        foreach (CellData cellData in m_runtimeOnlyLevel.levelCellList)
            if (cellData.x == x && cellData.y == y) 
                return cellData;
        return null;
    }

    //----------------------------------------
    // Check if current gem is random generate type
    //----------------------------------------
    bool IsRandomGeneratedGem(GemScriptableObj gemSO)
    {
        if (gemSO == null)
        {
            Debug.LogError("-- No Gem Found! --");
            return true;
        }
        var gemType = gemSO.gemType;
        if (gemType.Equals(GemType.RandomGenerate))
            return true;
        return false;
    }

    //----------------------------------------
    // Pick a random gem that valid (not create match-3 case at (x,y))
    //----------------------------------------
    GemScriptableObj GetRandomValidGem(int x, int y)
    {
        if (m_gemPool.Count == 0)
            return null;

        //Build candidate list and remove types which would create a 3-in-a-row horizontally or vertically
        List<GemScriptableObj> validGemPool = new List<GemScriptableObj>(m_gemPool);

        //Check cell left 2:
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

        //Check cell down 2:
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

        //In case there no valid gem, just put random
        if (validGemPool.Count == 0)
        {
            Debug.LogError($"RANDOM GEM NO FILTER AT {x}-{y}");
            return m_gemPool[Random.Range(0, m_gemPool.Count)];
        }

        return validGemPool[Random.Range(0, validGemPool.Count)];
    }

    //----------------------------------------
    // Validate if the swap is match 3
    //----------------------------------------
    bool IsSwapMoveClearGem(int x1, int y1, int x2, int y2)
    {
        CellData cell1 = GetLevelCellDataByPos(x1, y1);
        CellData cell2 = GetLevelCellDataByPos(x2, y2);
        if (cell1 == null || cell2 == null) return false;

        GemScriptableObj g1 = cell1.currentContainedGem;
        GemScriptableObj g2 = cell2.currentContainedGem;

        // swap
        cell1.currentContainedGem = g2;
        cell2.currentContainedGem = g1;

        bool result = IsThereMatchAt(x1, y1) || IsThereMatchAt(x2, y2);

        // swap back
        cell1.currentContainedGem = g1;
        cell2.currentContainedGem = g2;

        return result;
    }
    #endregion

    #region ------------ Public Functions (for others scripts)
    public int GetWidth() => m_runtimeGrid.GetWidth();
    public int GetHeight() => m_runtimeGrid.GetHeight();
    public float GetCellSize() => m_cellSize;
    public LevelScriptableObj GetLevelDataSO() => m_selectedLevel;
    public Vector3 GetCellWorldPos(Vector2 pos)
    {
        m_runtimeGrid.GetXY(pos, out int x, out int y);
        return m_runtimeGrid.GetWorldPos(x, y);
    }
    public GemScriptableObj GetRandomGem()
    {
        return m_gemPool[Random.Range(0, m_gemPool.Count)];
    }
    public CellData GetCellDataFromWorldPos(Vector2 pos)
    {
        m_runtimeGrid.GetXY(pos, out int x, out int y);
        if (x < 0 || y < 0 || x >= m_runtimeGrid.GetWidth() || y >= m_runtimeGrid.GetHeight())
            return null;
        return m_runtimeGrid.GetCellData(x, y);
    }
    public CellData GetCellData(int x, int y)
    {
        return m_runtimeOnlyLevel.levelCellList.Find(c => c.x == x && c.y == y);
    }
    public bool IsTherePossibleMove()
    {
        for (int x = 0; x < m_runtimeGrid.GetWidth(); x++)
        {
            for (int y = 0; y < m_runtimeGrid.GetHeight(); y++)
            {
                CellData unavailiableCell = GetLevelCellDataByPos(x, y);
                if (unavailiableCell == null)
                    continue;
                if (unavailiableCell.cellTypeInfo != null && unavailiableCell.cellTypeInfo.cellType == CellType.Blocked)
                    continue;

                // Try swap with gem on the right
                if (x < m_runtimeGrid.GetWidth() - 1)
                {
                    CellData testSwapGem = GetLevelCellDataByPos(x + 1, y);
                    if (testSwapGem != null && !(testSwapGem.cellTypeInfo != null && testSwapGem.cellTypeInfo.cellType == CellType.Blocked))
                        if (IsSwapMoveClearGem(x, y, x + 1, y)) return true;
                }

                // Try swap with upper gem
                if (y < m_runtimeGrid.GetHeight() - 1)
                {
                    CellData testSwapGem = GetLevelCellDataByPos(x, y + 1);
                    if (testSwapGem != null && !(testSwapGem.cellTypeInfo != null && testSwapGem.cellTypeInfo.cellType == CellType.Blocked))
                        if (IsSwapMoveClearGem(x, y, x, y + 1)) return true;
                }
            }
        }
        return false;
    }
    public bool IsThereMatchAt(int x, int y)
    {
        CellData checkGem = GetLevelCellDataByPos(x, y);
        if (checkGem == null || checkGem.currentContainedGem == null)
            return false;

        GemScriptableObj checkGemData = checkGem.currentContainedGem;

        //--- Horizontal check
        int count = 1;
        //---- Start checking to the left
        int i = x - 1;
        while (i >= 0)
        {
            CellData cellData = GetLevelCellDataByPos(i, y);
            if (cellData == null || cellData.currentContainedGem == null)
                break;
            if (GlobalFunction.CompareGemType(cellData.currentContainedGem, checkGemData))
            {
                count++;
                i--;
            }
            else
                break;
        }
        //---- Start checking to the right
        i = x + 1;
        while (i < m_runtimeGrid.GetWidth())
        {
            CellData cd = GetLevelCellDataByPos(i, y);
            if (cd == null || cd.currentContainedGem == null)
                break;
            if (GlobalFunction.CompareGemType(cd.currentContainedGem, checkGemData))
            {
                count++;
                i++;
            }
            else
                break;
        }
        if (count >= 3)
            return true;

        //--- Vertical check
        count = 1;
        //---- Start checking downard
        int j = y - 1;
        while (j >= 0)
        {
            CellData cd = GetLevelCellDataByPos(x, j);
            if (cd == null || cd.currentContainedGem == null)
                break;
            if (GlobalFunction.CompareGemType(cd.currentContainedGem, checkGemData))
            {
                count++;
                j--;
            }
            else
                break;
        }
        //---- Start checking upward
        j = y + 1;
        while (j < m_runtimeGrid.GetHeight())
        {
            CellData cd = GetLevelCellDataByPos(x, j);
            if (cd == null || cd.currentContainedGem == null)
                break;
            if (GlobalFunction.CompareGemType(cd.currentContainedGem, checkGemData))
            {
                count++;
                j++;
            }
            else
                break;
        }
        if (count >= 3)
            return true;

        return false;
    }
    #endregion
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeMonkey.Utils;

public class BoardGameplay : MonoBehaviour
{
    private GameMaster m_gameMaster;
    private BoardGenerator m_boardGenerator;
    private BoardVisual m_boardVisual;

    private bool m_isInputEnabled = true;

    private Vector2 m_onDownMousePos;
    private Vector2 m_onUpMousePos;

    private RuntimeCellSelected m_selectedCell = null;

    private float m_minDragDistance = 0.35f;   // world units

    public void Init(GameMaster gameMaster, BoardGenerator boardGenerator, BoardVisual boardVisual)
    {
        m_gameMaster = gameMaster;
        m_boardGenerator = boardGenerator;
        m_boardVisual = boardVisual;
    }

    public void UpdateGameplayState(GameState curState)
    {
        m_isInputEnabled = curState == GameState.PlayerMove;
    }

    private void Update()
    {
        if (!m_gameMaster)
            return;
        if (m_gameMaster.GetCurrentGameState() != GameState.PlayerMove) 
            return;
        InputHandling();
    }

    //--------------------------------------------------------------------
    // Drag gem movement logic
    //--------------------------------------------------------------------
    void InputHandling()
    {
        if (!m_isInputEnabled) 
            return;

        if (Input.GetMouseButtonDown(0))
        {
            m_onDownMousePos = UtilsClass.GetMouseWorldPosition();
            CellData selectedCellData = GetCellFromWorld(m_onDownMousePos);
            if (selectedCellData == null) 
                return;
            if (selectedCellData.currentContainedGem == null) 
                return;

            SelectCell(selectedCellData);
        }

        if (Input.GetMouseButtonUp(0) && m_selectedCell != null)
        {
            m_onUpMousePos = UtilsClass.GetMouseWorldPosition();
            Vector2 dragDistance = m_onUpMousePos - m_onDownMousePos;

            if (dragDistance.magnitude < m_minDragDistance)
            {
                //Not enough drag distance -> deselect gem
                Deselect();
                return;
            }
            Vector2 dir = DetectDragDirection(dragDistance);
            TrySwapInDirection(dir);
        }
    }

    //--------------------------------------------------------------------
    // Select and Deselect Gem
    //--------------------------------------------------------------------
    void SelectCell(CellData cell)
    {
        m_selectedCell = new RuntimeCellSelected()
        {
            x = cell.x,
            y = cell.y,
            data = cell
        };
        m_boardVisual.SelectVisualCell(cell.x, cell.y);
    }

    void Deselect()
    {
        if (m_selectedCell != null)
        {
            m_boardVisual.DeselectVisualCell(m_selectedCell.x, m_selectedCell.y);
            m_selectedCell = null;
        }
    }

    //--------------------------------------------------------------------
    // Check Drag direction
    //--------------------------------------------------------------------
    Vector2 DetectDragDirection(Vector2 dragDistance)
    {
        //Horizontal or vertical
        if (Mathf.Abs(dragDistance.x) > Mathf.Abs(dragDistance.y))
            return dragDistance.x > 0 ? Vector2.right : Vector2.left;
        else
            return dragDistance.y > 0 ? Vector2.up : Vector2.down;
    }

    //--------------------------------------------------------------------
    // Try swap gem on drag
    //--------------------------------------------------------------------
    void TrySwapInDirection(Vector2 dir)
    {
        if (m_selectedCell == null) 
            return;

        int targetX = m_selectedCell.x + (int)dir.x;
        int targetY = m_selectedCell.y + (int)dir.y;

        if (!IsTargetCellInsideBoard(targetX, targetY))
        {
            Deselect();
            return;
        }

        CellData cellA = m_selectedCell.data;
        CellData cellB = m_boardGenerator.GetCellData(targetX, targetY);

        if (cellB == null || cellB.currentContainedGem == null || cellB.cellTypeInfo.cellType == CellType.Blocked)
        {
            Deselect();
            return;
        }

        StartCoroutine(IESwapAndCheck(cellA, cellB));
    }

    //--------------------------------------------------------------------
    // Swap and check match logic (IENumerator)
    //--------------------------------------------------------------------
    IEnumerator IESwapAndCheck(CellData cell1, CellData cell2)
    {
        m_gameMaster.SetGameState(GameState.Busy);

        //Swap visually
        yield return StartCoroutine(m_boardVisual.IESwapAnimation(cell1, cell2));

        //Swap logically
        CellData cellA = m_boardGenerator.GetCellData(cell1.x, cell1.y);
        CellData cellB = m_boardGenerator.GetCellData(cell2.x, cell2.y);
        SwapCellGems(cellA, cellB);

        //Check if match exists
        List<CellData> matchList = ScanAllMatches();
        if (matchList.Count == 0)
        {
            // No match → swap back
            SwapCellGems(cellA, cellB);
            yield return StartCoroutine(m_boardVisual.IESwapAnimation(cell1, cell2));
            m_gameMaster.SetGameState(GameState.PlayerMove);
            Deselect();
            yield break;
        }

        //If match found -> Start loop check
        m_gameMaster.OnMoveSpend();
        yield return StartCoroutine(IELoopCheckMatches());

        //After loop check ends -> check if board have available
        if (!IsTherePossibleMove())
            yield return StartCoroutine(ShuffleBoard());

        m_gameMaster.SetGameState(GameState.PlayerMove);

        Deselect();
    }

    //--------------------------------------------------------------------
    // loop check untill no more matches chained (IENumerator)
    //--------------------------------------------------------------------
    IEnumerator IELoopCheckMatches()
    {
        bool continueLoop = true;

        while (continueLoop)
        {
            List<CellData> matches = ScanAllMatches();
            if (matches.Count == 0)
                break;

            // Send score info
            m_gameMaster.AddScore(matches.Count);

            // Clear
            yield return StartCoroutine(m_boardVisual.IEClearGemAnimation(matches));
            ClearCells(matches);

            // Gravity
            yield return StartCoroutine(IEStartFallingGemLogic());

            // Refill
            yield return StartCoroutine(RefillBoard());

            // Check again
            continueLoop = (ScanAllMatches().Count > 0);
        }
    }

    //--------------------------------------------------------------------
    // Scan all board for matches
    //--------------------------------------------------------------------
    List<CellData> ScanAllMatches()
    {
        List<CellData> result = new List<CellData>();

        int width = m_boardGenerator.GetWidth();
        int height = m_boardGenerator.GetHeight();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var cell = m_boardGenerator.GetCellData(x, y);
                if (cell.currentContainedGem == null) 
                    continue;

                List<CellData> match = GetListOfMatchesAt(x, y);
                foreach (var cellData in match)
                    if (!result.Contains(cellData))
                        result.Add(cellData);
            }

        return result;
    }

    //--------------------------------------------------------------------
    // Shuffle board when there no possible move
    //--------------------------------------------------------------------
    IEnumerator ShuffleBoard()
    {
        m_gameMaster.SetGameState(GameState.Busy);
        do
        {
            // simple shuffle logic
            ShuffleBoardData();
            yield return StartCoroutine(m_boardVisual.IEShuffleBoardVisual());
        } while (!IsTherePossibleMove() || HasAnyInitialMatch());
        m_gameMaster.SetGameState(GameState.PlayerMove);
    }

    #region +++++++++++ Extra Logics Helper (to support other logics)
    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // Extra Logics Helper (to support other logics)
    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    bool IsTargetCellInsideBoard(int x, int y)
    {
        return x >= 0 && y >= 0 && x < m_boardGenerator.GetWidth() && y < m_boardGenerator.GetHeight();
    }
    bool IsTherePossibleMove()
    {
        return m_boardGenerator.IsTherePossibleMove();
    }
    bool HasAnyInitialMatch()
    {
        int width = m_boardGenerator.GetWidth();
        int height = m_boardGenerator.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = m_boardGenerator.GetCellData(x, y);
                if (cell == null) 
                    continue;
                if (cell.cellTypeInfo.cellType == CellType.Blocked) 
                    continue;
                if (cell.currentContainedGem == null) 
                    continue;

                // if ANY position has a match → board has initial match
                if (m_boardGenerator.IsThereMatchAt(x, y))
                    return true;
            }
        }

        return false; // no matches anywhere
    }
    void ShuffleBoardData()
    {
        int width = m_boardGenerator.GetWidth();
        int height = m_boardGenerator.GetHeight();

        //collect all movable gems
        List<GemScriptableObj> gemList = new List<GemScriptableObj>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = m_boardGenerator.GetCellData(x, y);
                if (cell.cellTypeInfo.cellType != CellType.Blocked &&
                    cell.currentContainedGem != null)
                {
                    gemList.Add(cell.currentContainedGem);
                }
            }
        }

        //shuffle gem list
        for (int i = 0; i < gemList.Count; i++)
        {
            var temp = gemList[i];
            int rand = Random.Range(i, gemList.Count);
            gemList[i] = gemList[rand];
            gemList[rand] = temp;
        }

        //Set shuffled gems back to grid
        int index = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = m_boardGenerator.GetCellData(x, y);
                if (cell.cellTypeInfo.cellType != CellType.Blocked)
                {
                    cell.currentContainedGem = gemList[index];
                    index++;
                }
            }
        }
    }
    void SwapCellGems(CellData a, CellData b)
    {
        (b.currentContainedGem, a.currentContainedGem) = (a.currentContainedGem, b.currentContainedGem);
    }
    void ClearCells(List<CellData> cellDataList)
    {
        foreach (var cellData in cellDataList)
            cellData.currentContainedGem = null;
    }
    List<CellData> GetListOfMatchesAt(int x, int y)
    {
        List<CellData> list = new List<CellData>();
        CellData checkedGem = m_boardGenerator.GetCellData(x, y);
        if (checkedGem.currentContainedGem == null)
            return list;

        // horizontal
        List<CellData> horizontalCellList = new List<CellData>() { checkedGem };
        for (int i = x - 1; i >= 0; i--)
        {
            CellData comparedGem = m_boardGenerator.GetCellData(i, y);
            if (GlobalFunction.CompareGemType(comparedGem.currentContainedGem, checkedGem.currentContainedGem))
                horizontalCellList.Add(comparedGem);
            else
                break;
        }
        for (int i = x + 1; i < m_boardGenerator.GetWidth(); i++)
        {
            var comparedGem = m_boardGenerator.GetCellData(i, y);
            if (GlobalFunction.CompareGemType(comparedGem.currentContainedGem, checkedGem.currentContainedGem))
                horizontalCellList.Add(comparedGem);
            else
                break;
        }
        if (horizontalCellList.Count >= 3)
            list.AddRange(horizontalCellList);

        // vertical
        List<CellData> verticalCellList = new List<CellData>() { checkedGem };
        for (int j = y - 1; j >= 0; j--)
        {
            var comparedGem = m_boardGenerator.GetCellData(x, j);
            if (GlobalFunction.CompareGemType(comparedGem.currentContainedGem, checkedGem.currentContainedGem))
                verticalCellList.Add(comparedGem);
            else
                break;
        }
        for (int j = y + 1; j < m_boardGenerator.GetHeight(); j++)
        {
            var comparedGem = m_boardGenerator.GetCellData(x, j);
            if (GlobalFunction.CompareGemType(comparedGem.currentContainedGem, checkedGem.currentContainedGem))
                verticalCellList.Add(comparedGem);
            else
                break;
        }
        if (verticalCellList.Count >= 3)
            list.AddRange(verticalCellList);

        return list;
    }
    CellData GetCellFromWorld(Vector2 worldPos)
    {
        return m_boardGenerator.GetCellDataFromWorldPos(worldPos);
    }
    IEnumerator IEStartFallingGemLogic()
    {
        int width = m_boardGenerator.GetWidth();
        int height = m_boardGenerator.GetHeight();
        List<Coroutine> coroutineList = new List<Coroutine>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cellData = m_boardGenerator.GetCellData(x, y);
                if (cellData.currentContainedGem == null && cellData.cellTypeInfo.cellType != CellType.Blocked)
                {
                    // find first gem above
                    for (int yPlus1 = y + 1; yPlus1 < height; yPlus1++)
                    {
                        CellData above = m_boardGenerator.GetCellData(x, yPlus1);
                        if (above.currentContainedGem != null)
                        {
                            // drop down
                            m_boardGenerator.GetCellData(x, y).currentContainedGem = above.currentContainedGem;
                            above.currentContainedGem = null;
                            CellData startCell = m_boardGenerator.GetCellData(x, yPlus1);
                            CellData targetCell = m_boardGenerator.GetCellData(x, y);
                            coroutineList.Add(StartCoroutine(m_boardVisual.IEFallGemAnimation(startCell, targetCell)));
                            break;
                        }
                    }
                }
            }
        }
        foreach (var coroutine in coroutineList)
            yield return coroutine;
    }
    IEnumerator RefillBoard()
    {
        int width = m_boardGenerator.GetWidth();
        int height = m_boardGenerator.GetHeight();
        List<Coroutine> coroutineList = new List<Coroutine>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = m_boardGenerator.GetCellData(x, y);
                if (cell.currentContainedGem == null && cell.cellTypeInfo.cellType != CellType.Blocked)
                {
                    cell.currentContainedGem = m_boardGenerator.GetRandomGem();
                    coroutineList.Add(StartCoroutine(m_boardVisual.IESpawnDropGemAnimation(cell)));
                }
            }
        }
        foreach (var coroutine in coroutineList) 
            yield return coroutine;
    }
    #endregion

    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // Grid Object Class
    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    private class RuntimeCellSelected
    {
        public int x;
        public int y;
        public CellData data;
    }

}

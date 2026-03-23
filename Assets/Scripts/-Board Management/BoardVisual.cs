using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BoardVisual : MonoBehaviour
{
    [Header("--- References")]
    [SerializeField] private Transform m_gemPrefab;
    [SerializeField] private Transform m_cellBackgroundPrefab;
    [SerializeField] private Transform m_cameraTransform;
    [SerializeField] private Transform m_gridContainer;

    [Header("--- Sound Effects")]
    [SerializeField] private AudioClip m_popupBoardSoundEff;
    [SerializeField] private AudioClip m_match3SoundEff;
    [SerializeField] private AudioClip m_selectSoundEff;
    
    [Header("--- Sound Settings")]
    [SerializeField] private float m_spawnSoundVolume = 0.3f;
    [SerializeField] private float m_matchSoundVolume = 0.6f;
    [SerializeField] private float m_selectSoundVolume = 0.5f;

    private CustomGrid2D<RuntimeVisualCell> m_grid2D;
    private GameMaster m_gameMaster;
    private RuntimeVisualCell[,] m_visualCellArr;
    private BoardGenerator m_generatedBoard;
    private Sequence m_startAnimationSequence;

    private Transform m_gemContainer;
    private Transform m_cellContainer;

    public void Init(GameMaster gameMaster, BoardGenerator boardGenerator)
    {
        m_generatedBoard = boardGenerator;
        m_gameMaster = gameMaster;
        GeneratingBoardVisual();
    }

    #region +++++ Private Visual Logic
    private void GeneratingBoardVisual()
    {
        //Set "State" to "busy" to generate board visual and animation
        m_gameMaster.SetGameState(GameState.Busy);

        //Init data
        m_startAnimationSequence = DOTween.Sequence();
        int width = m_generatedBoard.GetWidth();
        int height = m_generatedBoard.GetHeight();
        m_grid2D = new CustomGrid2D<RuntimeVisualCell>(width, height, m_generatedBoard.GetCellSize(), false, m_gridContainer,
            (grid, x, y) => new RuntimeVisualCell(m_generatedBoard.GetLevelDataSO(), grid, x, y));
        m_visualCellArr = m_grid2D.GetCellArr();
        List<SpriteRenderer> backgroundSpriteRenderereList = new List<SpriteRenderer>();
        List<SpriteRenderer> gemSpriteRenderereList = new List<SpriteRenderer>();

        //Create Cell Container Transform
        Transform cellContainer = new GameObject("CellContainer").transform;
        cellContainer.SetParent(m_grid2D.GetGridContainer());
        cellContainer.transform.position = Vector3.zero;
        m_cellContainer = cellContainer;

        //Create Gem Container Transform
        Transform gemContainer = new GameObject("GemContainer").transform;
        gemContainer.SetParent(m_grid2D.GetGridContainer());
        gemContainer.transform.position = Vector3.zero;
        m_gemContainer = gemContainer;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cellData = m_generatedBoard.GetCellData(x, y);
                var runtimeCell = m_grid2D.GetCellObject(x, y);
                runtimeCell.cellData = cellData;

                //Setup Background cell
                var cellBackground = Instantiate(m_cellBackgroundPrefab, m_grid2D.GetWorldPos(x, y), Quaternion.identity);
                cellBackground.name = $"cell Bg: {cellData.x} - {cellData.y}";
                cellBackground.transform.SetParent(m_cellContainer);
                cellBackground.transform.localScale = Vector3.one * m_grid2D.GetCellSize();
                runtimeCell.bgSpriteRenderer = cellBackground.Find("sprite").GetComponent<SpriteRenderer>();
                if (cellData.cellTypeInfo.cellType == CellType.Blocked)
                    runtimeCell.bgSpriteRenderer.sprite = cellData.cellTypeInfo.sprite;
                backgroundSpriteRenderereList.Add(runtimeCell.bgSpriteRenderer);

                //Setup Gem visual if exist
                if (cellData.currentContainedGem != null)
                {
                    var visualGem = Instantiate(m_gemPrefab, m_grid2D.GetWorldPos(x, y), Quaternion.identity, m_grid2D.GetGridContainer());
                    visualGem.name = $"Gem: {cellData.currentContainedGem.gemName}";
                    visualGem.transform.SetParent(m_gemContainer);
                    visualGem.transform.localScale = Vector3.one * m_grid2D.GetCellSize();
                    runtimeCell.gemSpriteRenderer = visualGem.Find("sprite").GetComponent<SpriteRenderer>();
                    runtimeCell.gemSpriteRenderer.sprite = cellData.currentContainedGem.sprite;
                    gemSpriteRenderereList.Add(runtimeCell.gemSpriteRenderer);
                }
            }
        }
        
        float timePosPerCell = 1f / backgroundSpriteRenderereList.Count;
        float timePosPerGem = 1f / gemSpriteRenderereList.Count;
        float speedTime = 1.25f;
        SpawningAnimation(backgroundSpriteRenderereList, timePosPerCell / speedTime, isSilent: true);
        SpawningAnimation(gemSpriteRenderereList, timePosPerGem / speedTime, isSilent: false);
        m_startAnimationSequence.Play().OnComplete(() =>
        {
            m_gameMaster.SetGameState(GameState.PlayerMove);
        });
    }

    private void SpawningAnimation(List<SpriteRenderer> spriteRenderList, float animSpeedRate, bool isSilent = false)
    {
        float i = 0;
        int index = 0;
        int soundInterval = 4; // Play sound every 4 gems instead of every 3
        
        foreach (var spriteRender in spriteRenderList)
        {
            i += animSpeedRate;
            spriteRender.color = new Color(1, 1, 1, 0);
            spriteRender.transform.localScale = Vector3.zero;
            m_startAnimationSequence.Insert(i, spriteRender.DOFade(1, 0.3f));
            m_startAnimationSequence.Join(spriteRender.transform.DOScale(1f, 0.3f).OnComplete(() =>
            {
                index++;
                // Play sound less frequently and at lower volume to prevent overlap
                if (!isSilent && index % soundInterval == 0 && index < 80)
                {
                    SoundManager.PlaySound(m_popupBoardSoundEff, false, volume: m_spawnSoundVolume);
                }
            }));
        }
    }

    private void HidingAnimation(List<SpriteRenderer> spriteRenderList, float animSpeedRate)
    {
        float i = 0;
        int index = 0;
        int soundInterval = 4;
        
        foreach (var spriteRender in spriteRenderList)
        {
            if (spriteRender == null)
                continue;
            i += animSpeedRate;
            spriteRender.color = Color.white;
            spriteRender.transform.localScale = Vector3.one;
            m_startAnimationSequence.Insert(i, spriteRender.DOFade(0, 0.3f));
            m_startAnimationSequence.Join(spriteRender.transform.DOScale(0, 0.3f).OnComplete(() =>
            {
                index++;
                if (index % soundInterval == 0 && index < 80)
                {
                    SoundManager.PlaySound(m_popupBoardSoundEff, false, volume: m_spawnSoundVolume);
                }
            }));
        }
    }

    private bool IsValidLengthOfGrid(CellData cellData)
    {
        if (cellData.x >= m_grid2D.GetWidth() || cellData.y >= m_grid2D.GetHeight())
            return false;
        return true;
    }
    #endregion

    #region +++++ Public visual logics (to be called by others *mostly gameplay script)
    //--
    public IEnumerator IESwapAnimation(CellData cell1, CellData cell2)
    {
        if (!IsValidLengthOfGrid(cell1) || !IsValidLengthOfGrid(cell2))
        {
            Debug.LogError("ERROR-BoardVisual: Invalid cell swap, check your input from BoardGameplay script");
            yield break;
        }
        //Set Visual Cell
        RuntimeVisualCell cellA = m_visualCellArr[cell1.x, cell1.y];
        RuntimeVisualCell cellB = m_visualCellArr[cell2.x, cell2.y];

        //Set gem1 parent
        Transform gemA = cellA.gemSpriteRenderer.transform.parent;
        string gemAName = gemA.name;
        SpriteRenderer spriteRenderGemA = cellA.gemSpriteRenderer;

        //Set gem2 parent
        Transform gemB = cellB.gemSpriteRenderer.transform.parent;
        string gemBName = gemB.name;
        SpriteRenderer spriteRenderGemB = cellB.gemSpriteRenderer;

        //Get both gem old position
        Vector3 posA = gemA.position;
        Vector3 posB = gemB.position;

        //Kill all current animation (just to make sure there no tween active while doing animation)
        spriteRenderGemA.transform.DOKill();
        spriteRenderGemA.transform.localScale = Vector3.one;
        spriteRenderGemB.transform.DOKill();
        spriteRenderGemB.transform.localScale = Vector3.one;

        //Start Animation (swap position) 
        Sequence swapAnimationSequence = DOTween.Sequence();
        swapAnimationSequence
            .Join(gemA.DOMove(posB, 0.2f))
            .Join(gemB.DOMove(posA, 0.2f))
            .OnComplete(() =>
            {
                //Re-reference both visual data 
                cellA.gemSpriteRenderer = spriteRenderGemB;
                gemA.name = gemBName;

                cellB.gemSpriteRenderer = spriteRenderGemA;
                gemB.name = gemAName;
            });
        swapAnimationSequence.Play();
        yield return swapAnimationSequence.WaitForCompletion();
    }
    //--
    public IEnumerator IEClearGemAnimation(List<CellData> listMatchesCell)
    {
        bool isExplodePlayed = false;
        //Create DOTween Sequence
        Sequence clearAnimationSequence = DOTween.Sequence();

        //Get all matches gem cell data (to animation clear them)
        foreach (CellData cellData in listMatchesCell)
        {
            RuntimeVisualCell cell = m_visualCellArr[cellData.x, cellData.y];
            Transform gemTranformParent = cell.gemSpriteRenderer.transform.parent;
            clearAnimationSequence.Join(cell.gemSpriteRenderer.transform.DOScale(0f, 0.3f).OnComplete(() =>
            {
                GameObject particle = gemTranformParent.GetChild(0).gameObject;
                particle.transform.SetParent(gemTranformParent.transform.parent, true);
                particle.SetActive(true);
                
                // Play sound only once per clear animation
                if (!isExplodePlayed)
                {
                    SoundManager.PlaySound(m_match3SoundEff, false, volume: m_matchSoundVolume);
                    isExplodePlayed = true;
                }
            }).SetAutoKill());
        }

        //Start sequence animation
        clearAnimationSequence.Play().SetAutoKill();
        yield return clearAnimationSequence.WaitForCompletion();
        foreach (CellData cellData in listMatchesCell)
        {
            RuntimeVisualCell cell = m_visualCellArr[cellData.x, cellData.y];
            Transform gemTranformParent = cell.gemSpriteRenderer.transform.parent;
            Destroy(gemTranformParent.gameObject);
            cell.gemSpriteRenderer = null;
        }
    }
    //--
    public IEnumerator IESpawnDropGemAnimation(CellData cellData)
    {
        if (!IsValidLengthOfGrid(cellData))
        {
            Debug.LogError($"ERROR-BoardVisual: Invalid coordinate ({cellData.x},{cellData.y}) in spawn animation!");
            yield break;
        }

        //Get cell reference
        RuntimeVisualCell cell = m_visualCellArr[cellData.x, cellData.y];
        Vector3 targetPos = m_grid2D.GetWorldPos(cellData.x, cellData.y);

        //Create gem visual object
        Transform visualGem = Instantiate(m_gemPrefab, targetPos, Quaternion.identity, m_grid2D.GetGridContainer());
        visualGem.name = $"Gem: {cellData.currentContainedGem.gemName}";
        visualGem.transform.localScale = Vector3.one * m_grid2D.GetCellSize();
        visualGem.transform.SetParent(m_gemContainer, true);

        //Create new spriteRenderer and set sprite
        SpriteRenderer gemRenderer = visualGem.Find("sprite").GetComponent<SpriteRenderer>();
        gemRenderer.sprite = cellData.currentContainedGem.sprite;

        //Assign new spriteRenderer to visual cell 
        cell.gemSpriteRenderer = gemRenderer;

        //Set start position (above the board)
        float fallOffset = m_grid2D.GetCellSize() * 2.5f;
        Vector3 startPos = new Vector3(targetPos.x, targetPos.y + fallOffset, targetPos.z);
        visualGem.position = startPos;
        gemRenderer.color = new Color(1, 1, 1, 0);

        //Animation sequence
        Sequence fallingSequence = DOTween.Sequence();
        fallingSequence.Append(gemRenderer.DOFade(1f, 0.15f));
        fallingSequence.Join(visualGem.DOMove(targetPos, 0.25f).SetEase(Ease.OutQuad));
        yield return fallingSequence.WaitForCompletion();
    }
    //--
    public IEnumerator IEFallGemAnimation(CellData startCell, CellData targetCell, float fallDuration = 0.25f)
    {
        if (!IsValidLengthOfGrid(startCell) || !IsValidLengthOfGrid(targetCell))
            yield break;

        //Set visual cell
        var visualStartCell = m_visualCellArr[startCell.x, startCell.y];
        if (startCell == null || visualStartCell.gemSpriteRenderer == null)
            yield break;

        //Get end position for gem to fall in
        Transform gemTransform = visualStartCell.gemSpriteRenderer.transform.parent;
        Vector3 endPos = m_grid2D.GetWorldPos(targetCell.x, targetCell.y);

        //Start animation
        Tween fallTweener = gemTransform.DOMove(endPos, fallDuration).SetEase(Ease.InQuad);
        yield return fallTweener.WaitForCompletion();

        //After animation, update visual references.
        RuntimeVisualCell visualEndCell = m_visualCellArr[targetCell.x, targetCell.y];
        visualEndCell.gemSpriteRenderer = visualStartCell.gemSpriteRenderer;
        visualStartCell.gemSpriteRenderer = null;
    }
    //--
    public IEnumerator IEShuffleBoardVisual()
    {
        List<SpriteRenderer> gemSpriteRenderereList = new List<SpriteRenderer>();

        int width = m_generatedBoard.GetWidth();
        int height = m_generatedBoard.GetHeight();

        //Setup to animation hide current gem list
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cell = m_generatedBoard.GetCellData(x, y);
                RuntimeVisualCell visualCell = m_visualCellArr[x, y];
                if (cell.currentContainedGem != null)
                    gemSpriteRenderereList.Add(visualCell.gemSpriteRenderer);
            }
        }
        
        m_startAnimationSequence = DOTween.Sequence();
        float timePosPerCell = 1f / gemSpriteRenderereList.Count;
        float speedTime = 1.25f;
        HidingAnimation(gemSpriteRenderereList, timePosPerCell / speedTime);
        m_startAnimationSequence.Play();
        yield return m_startAnimationSequence.WaitForCompletion();

        //Setup to animation spawn new possible move gem list
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cell = m_generatedBoard.GetCellData(x, y);
                RuntimeVisualCell visualCell = m_visualCellArr[x, y];
                if (cell.currentContainedGem != null)
                    visualCell.gemSpriteRenderer.sprite = cell.currentContainedGem.sprite;
            }
        }
        
        m_startAnimationSequence = DOTween.Sequence();
        SpawningAnimation(gemSpriteRenderereList, timePosPerCell / speedTime, isSilent: false);
        m_startAnimationSequence.Play();
        yield return m_startAnimationSequence.WaitForCompletion();
    }
    //--
    public IEnumerator IECleanAllVisual()
    {
        DOTween.Clear();
        List<SpriteRenderer> gemSpriteRenderereList = new List<SpriteRenderer>();
        List<SpriteRenderer> bgSpriteRenderereList = new List<SpriteRenderer>();
        foreach (var visualCell in m_visualCellArr)
        {
            gemSpriteRenderereList.Add(visualCell.gemSpriteRenderer);
            bgSpriteRenderereList.Add(visualCell.bgSpriteRenderer);
        }

        m_startAnimationSequence = DOTween.Sequence();
        float timePosPerCell = 1f / gemSpriteRenderereList.Count;
        float timePosPerCellbg = 1f / bgSpriteRenderereList.Count;
        float speedTime = 1.25f;
        HidingAnimation(gemSpriteRenderereList, timePosPerCell / speedTime);
        HidingAnimation(bgSpriteRenderereList, timePosPerCellbg / speedTime);
        m_startAnimationSequence.Play().OnComplete(() =>
        {
            Destroy(m_gemContainer.gameObject);
            Destroy(m_cellContainer.gameObject);
        });
        yield return m_startAnimationSequence.WaitForCompletion();
        m_gameMaster.SetGameState(GameState.Busy);
    }

    public void SelectVisualCell(int x, int y)
    {
        SoundManager.PlaySound(m_selectSoundEff, false, volume: m_selectSoundVolume);
        var selectedCell = m_visualCellArr[x, y];
        if (selectedCell != null && selectedCell.gemSpriteRenderer != null)
        {
            selectedCell.gemSpriteRenderer.DOKill();
            selectedCell.gemSpriteRenderer
                .DOColor(new Color(1.6f, 1.6f, 1.6f, 1f), 0.25f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutQuad);
            selectedCell.gemSpriteRenderer.transform.DOScale(1.1f, 0.3f).SetLoops(-1, LoopType.Yoyo);
        }
    }
    //--
    public void DeselectVisualCell(int x, int y)
    {
        var selectedCell = m_visualCellArr[x, y];
        if (selectedCell != null && selectedCell.gemSpriteRenderer != null)
        {
            selectedCell.gemSpriteRenderer.DOKill();
            selectedCell.gemSpriteRenderer.transform.DOKill();
            selectedCell.gemSpriteRenderer.DOColor(Color.white, 0.25f);
            selectedCell.gemSpriteRenderer.transform.localScale = Vector3.one;
        }
    }
    #endregion

    #region +++++ RuntimeVisualCell (Grid2D Object Type)
    //------------------------------------------------------------------------------------------------------------
    private class RuntimeVisualCell
    {
        public CellData cellData;
        public SpriteRenderer gemSpriteRenderer;
        public SpriteRenderer bgSpriteRenderer;

        private LevelScriptableObj levelSO;
        private CustomGrid2D<RuntimeVisualCell> grid2D;
        private int x;
        private int y;

        public RuntimeVisualCell(LevelScriptableObj levelSO, CustomGrid2D<RuntimeVisualCell> grid, int x, int y)
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
    }
    #endregion
}

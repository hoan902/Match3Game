using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    [Header("--- References")]
    [SerializeField] BoardGenerator m_boardGenerator;
    [SerializeField] BoardVisual m_boardVisual;
    [SerializeField] BoardGameplay m_boardGameplay;
    [SerializeField] GameUI m_gameUI;

    [Header("--- Level Config")]
    [SerializeField] LevelScriptableObj m_selectedLevel; // assign the same SO you used in LevelEditorTool

    [Header("--- Sound Effects")]
    [SerializeField] private AudioClip m_musicLoops;

    private int m_baseComboMultiplier = 1;
    private int m_baseScore = 600;
    private GameState m_gameState;
    private int m_currentMoveLeft;
    private int m_currentScore;
    private int m_currentObjectiveScore;

    void Start()
    {
        m_gameState = GameState.Busy;
        SoundManager.PlaySound(m_musicLoops, true, true);
        SoundManager.AdjustVolumeMusic(0.5f);
        StartCoroutine(InitiateGame());
    }

    IEnumerator InitiateGame()
    {
        yield return new WaitUntil(() => m_gameState == GameState.Busy);
        m_boardGenerator.Init(this, m_selectedLevel);
        m_boardVisual.Init(this, m_boardGenerator);
        m_boardGameplay.Init(this, m_boardGenerator, m_boardVisual);
        m_currentScore = 0;
        m_currentMoveLeft = m_selectedLevel.moveAmount;
        m_gameUI.UpdateLevelName(m_selectedLevel.levelName);

        //Init UI
        //More objective can be added in the future
        switch (m_selectedLevel.goalType)
        {
            case GoalType.Score:
                m_currentObjectiveScore = m_selectedLevel.targetScore;
                m_gameUI.UpdateScoreObjective(m_currentObjectiveScore);
                break;
            default:
                break;
        }
        m_gameUI.UpdateMoveLeft(m_currentMoveLeft);
        m_gameUI.UpdateScore(m_currentScore);
    }
    private void CheckWinLoseCondition()
    {
        StartCoroutine(IEDelayPopup());
    }
    private void CleanAll()
    {
        //HoanDN Reset all here
        m_gameState = GameState.GameOver;
        StartCoroutine(m_boardVisual.IECleanAllVisual());
        m_gameUI.ResetAll();
    }

    IEnumerator IEDelayPopup()
    {
        yield return new WaitUntil(()=> m_gameState == GameState.PlayerMove);
        if (m_currentMoveLeft >= 0 && m_currentScore >= m_currentObjectiveScore)
        {
            m_gameState = GameState.GameOver;
            m_gameUI.OnWinPopup();
        }
        if (m_currentMoveLeft == 0)
        {
            m_gameState = GameState.GameOver;
            if (m_currentScore >= m_currentObjectiveScore)
                m_gameUI.OnWinPopup();
            else
                m_gameUI.OnLosePopup();
        }
    }

    public void OnButtonRetry()
    {
        CleanAll();
        StartCoroutine(InitiateGame());
    }

    public void OnMoveSpend()
    {
        if (m_currentMoveLeft > 0)
            m_currentMoveLeft--;
        m_gameUI.UpdateMoveLeft(m_currentMoveLeft);
    }

    public void SetGameState(GameState newState)
    {
        m_gameState = newState;
        m_boardGameplay.UpdateGameplayState(newState);
    }
    public GameState GetCurrentGameState()
    {
        return m_gameState;
    }
    public void AddScore(int clearedCount)
    {
        int comboMultiplier = m_baseComboMultiplier;
        if (clearedCount == 4)
            comboMultiplier += 1;
        else if (clearedCount > 4)
            comboMultiplier += 2;
        int lengthBonus = clearedCount * m_baseScore;
        int totalScore = lengthBonus * m_baseComboMultiplier;

        m_currentScore += totalScore;
        m_gameUI.UpdateScore(m_currentScore);
        CheckWinLoseCondition();
    }
}

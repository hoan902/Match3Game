using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [Header("--- References")]
    [SerializeField] private TextMeshProUGUI m_scoreTxt;
    [SerializeField] private TextMeshProUGUI m_moveLeftTxt;
    [SerializeField] private TextMeshProUGUI m_LevelNameTxt;
    [SerializeField] private TextMeshProUGUI m_objectiveTxt;
    [SerializeField] private Transform m_winPopup;
    [SerializeField] private Transform m_losePopup;

    [Header("--- Sound configs")]
    [SerializeField] private AudioClip m_clickSoundEff;
    [SerializeField] private AudioClip m_winSoundEff;
    [SerializeField] private AudioClip m_loseSoundEff;

    public void UpdateLevelName(string textLevelName)
    {
        m_LevelNameTxt.text = textLevelName;
    }
    public void UpdateMoveLeft(int moveLeft)
    {
        m_moveLeftTxt.text = moveLeft.ToString();
    }

    public void UpdateScoreObjective(int scoreGoal)
    {
        m_objectiveTxt.text = $"Target Score: {scoreGoal}";
    }

    public void UpdateScore(int score)
    {
        m_scoreTxt.text = $"Target Score: {score}";
    }

    public void ResetAll()
    {
        m_scoreTxt.text = "";
        m_moveLeftTxt.text = "";
        m_objectiveTxt.text = "";
        m_winPopup.gameObject.SetActive(false);
        m_losePopup.gameObject.SetActive(false);
    }

    public void OnWinPopup()
    {
        SoundManager.PlaySound(m_winSoundEff, false);
        m_winPopup.gameObject.SetActive(true);
        m_losePopup.gameObject.SetActive(false);
    }

    public void OnLosePopup()
    {
        SoundManager.PlaySound(m_loseSoundEff, false);
        m_losePopup.gameObject.SetActive(true);
        m_winPopup.gameObject.SetActive(false);
    }
}

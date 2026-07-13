using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject startChoicePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject tutorialPanel;

    [Header("이동할 게임 씬 이름")]
    [SerializeField] private string gameSceneName = "Map";

    public void OpenStartChoice()
    {
        startChoicePanel.SetActive(true);
    }

    public void CloseStartChoice()
    {
        startChoicePanel.SetActive(false);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void OpenTutorial()
    {
        tutorialPanel.SetActive(true);
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
    }

    // 튜토리얼을 보면서 시작
    public void StartWithTutorial()
    {
        GameStartData.ShowTutorial = true;
        SceneManager.LoadScene(gameSceneName);
    }

    // 튜토리얼 없이 바로 시작
    public void StartDirectly()
    {
        GameStartData.ShowTutorial = false;
        SceneManager.LoadScene(gameSceneName);
    }
}

public static class GameStartData
{
    public static bool ShowTutorial;
}
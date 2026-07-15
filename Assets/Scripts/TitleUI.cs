using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    public GameObject titlePanel;
    public GameObject settingPanel;
    public Image Tutorial;
    public Sprite[] TutorialImages;

    private int tutorialIndex = -1;

    private void Start()
    {
        Tutorial.enabled = false;
        titlePanel.SetActive(true);
        settingPanel.SetActive(false);
    }

    private void Update()
    {
        if (tutorialIndex >= 0 && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ShowNextTutorial();
        }
    }

    public void OpenSetting()
    {
        titlePanel.SetActive(false);
        settingPanel.SetActive(true);
    }

    public void CloseSetting()
    {
        settingPanel.SetActive(false);
        titlePanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quit!!!!!!!!!!!!!!!!!!!!!!!!");
        Application.Quit();
    }

    public void StartGame()
    {
        AudioManager.instance.ChangeBgm(AudioManager.instance.dungeonBgm);
        SceneManager.LoadScene("MapFloor1Game");
    }

    public void TutorialSystem()
    {
        tutorialIndex = 0;
        Tutorial.sprite = TutorialImages[tutorialIndex];
        Tutorial.enabled = true;
        Tutorial.transform.SetAsLastSibling();
    }

    private void ShowNextTutorial()
    {
        tutorialIndex++;

        if (tutorialIndex < TutorialImages.Length)
        {
            Tutorial.sprite = TutorialImages[tutorialIndex];
            return;
        }

        Tutorial.enabled = false;
        tutorialIndex = -1;
    }
}

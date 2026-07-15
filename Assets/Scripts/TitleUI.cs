using UnityEngine.SceneManagement;
using UnityEngine;

public class TitleUI : MonoBehaviour
{
    public GameObject titlePanel;
    public GameObject settingPanel;

    void Start()
    {
        titlePanel.SetActive(true);
        settingPanel.SetActive(false);
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
        SceneManager.LoadScene("Map1Game");
    }
}
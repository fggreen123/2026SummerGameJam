using UnityEngine;

public class SettingEsc : MonoBehaviour
{
    [Header("Settings Panel")]
    [Tooltip("켜고 깔 PopUpSettingPanel 오브젝트를 할당해주세요")]
    [SerializeField]
    private GameObject settingPanel;

    private bool isPaused = false;

    private void Start()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        Time.timeScale = 1.0f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ResumeGame()
    {
        if (settingPanel == null) return;

        settingPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }
    public void PauseGame()
    {
        if (settingPanel == null) return;

        settingPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

}

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void OnClickRestart()
    {
        SceneManager.LoadScene("Map 1");
        Debug.Log("Restart");
    }

    public void OnClickMainScene()
    {
        SceneManager.LoadScene("TitleScene");
        Debug.Log("Main");
    }
}

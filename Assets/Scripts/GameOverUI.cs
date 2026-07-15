using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void OnClickRestart()
    {
        SceneManager.LoadScene("MapFloor1Game");
        Debug.Log("Restart");
    }

    public void OnClickMainScene()
    {
        SceneManager.LoadScene("TitleScene");
        Debug.Log("Main");
    }
}

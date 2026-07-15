using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    public string nextSceneName;

    public string enemyTag = "Enemy";

    public int enemyCount;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            enemyCount = GameObject.FindGameObjectsWithTag(enemyTag).Length;

            if(enemyCount == 0)
            {
                Debug.Log("모든 적 처치 완료. 다음 맵 이동");
                FindFirstObjectByType<CardDistribution>().PreserveHandForNextScene();
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.Log($"아직 적이 {enemyCount}마리 있어 문이 열리지 않습니다!");
            }
        }
    }
}

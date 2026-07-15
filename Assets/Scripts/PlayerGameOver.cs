using System.Globalization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerGameOver : MonoBehaviour
{
    [UnitHeaderInspectable("플레이어 데이터 베이스")]
    public PlayerData PlayerData;

    [HideInInspector]
    public float maxHP;
    private float curHP;

    [Header("GameOverUI")]
    public string gameOverSceneName = "GOScene";

    public System.Action Died;

    //private void Start()
    //{
    //    if (PlayerData != null)
    //    {
    //        maxHP = PlayerData.healthPoint;
    //        curHP = maxHP;

    //        Debug.Log($"load Success 현재 체력 : {maxHP}");
    //    }
    //    else
    //    {
    //        maxHP = 100f;
    //        curHP = maxHP;
    //        Debug.LogWarning("PlayerData 연결 실패 체력 100");
    //    }
    //}

    public void TakeDamage(float damage)
    {
        curHP -= damage;
        Debug.Log($"currentHP : {curHP}");

        //if (curHP <= 0)
        //{
        //    GOver();
        //}
    }

    //    private void GOver()
    //{
    //    Debug.Log("GameOverUI print");

    //    if(!SceneManager.GetSceneByName(gameOverSceneName).isLoaded) {
    //        SceneManager.LoadScene(gameOverSceneName, LoadSceneMode.Additive);
    //    }
    //}

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TakeDamage(10f);
        }
    }
}

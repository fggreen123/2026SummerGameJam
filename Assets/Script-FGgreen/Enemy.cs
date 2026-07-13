using UnityEngine;

public class Enemy : MonoBehaviour
{
    public EnemyData data;

    private int currentHp;

    void Start()
    {
        currentHp = data.hp;

        Debug.Log(data.enemyName);
        Debug.Log(data.hp);
        Debug.Log(data.damage);
        Debug.Log(data.attackSpeed);
    }
}
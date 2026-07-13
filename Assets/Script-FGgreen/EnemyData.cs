using UnityEngine;

[CreateAssetMenu(menuName = "DB/Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int hp;
    public int damage;
    public float attackSpeed;
}
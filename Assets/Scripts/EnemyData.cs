using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "Game Data/Enemy Data"
)]
public sealed class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;

    [Header("Stats")]
    [Min(1)]
    public int hp = 1;

    [Min(0)]
    public int damage = 1;

    [Tooltip("초당 공격 횟수")]
    [Min(0f)]
    public float attackSpeed = 1f;

    [Min(0f)]
    public float attackRange = 2f;

    [Min(0f)]
    public float moveSpeed = 2f;
}
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerData",
    menuName = "Game Data/Player Data"
)]
public sealed class PlayerData : ScriptableObject
{
    [Header("Identity")]
    public string playerName;

    [Tooltip("사용할 스킬")]
    public string skillNum;

    [Header("Stats")]
    [Min(1)]
    public int healthPoint = 50;

    [Min(0)]
    public int damage = 15;

    [Tooltip("초당 공격 횟수입니다.")]
    [Min(0f)]
    public float attackSpeed = 1f;

    [Min(0f)]
    public float attackRange = 2f;

    [Min(0f)]
    public float moveSpeed = 2f;
}
using UnityEngine;

[CreateAssetMenu(
    fileName = "BossSkillData",
    menuName = "Game/Boss/Boss Skill Data"
)]
public sealed class BossSkillData : ScriptableObject
{
    [Header("공 던지기")]
    [Tooltip("공이 플레이어에게 주는 피해입니다.")]
    [Min(0)]
    public int ballDamage = 5;

    [Tooltip("공을 한 번 생성한 뒤 다음 공을 생성하기까지의 시간입니다.")]
    [Min(0f)]
    public float ballAttackInterval = 1f;

    [Tooltip("공의 이동 속도입니다.")]
    [Min(0f)]
    public float ballSpeed = 5f;

    [Tooltip("공의 회전 속도 배율입니다.")]
    [Min(0f)]
    public float ballRotationMultiplier = 180f;

    [Tooltip("공이 충돌하지 않았을 때 자동으로 제거되는 시간입니다.")]
    [Min(0f)]
    public float ballLifetime = 5f;

    [Header("훌라후프 굴리기")]
    [Min(0)]
    public int hoopDamage = 15;

    [Min(0f)]
    public float hoopAttackInterval = 0.25f;

    [Min(0f)]
    public float hoopSpeed = 5f;

    [Header("파이어링")]
    [Min(0)]
    public int fireRingDamage = 7;

    [Min(0f)]
    public float fireRingAttackInterval = 0.5f;

    [Min(1)]
    public int fireRingCount = 3;

    [Min(0f)]
    public float fireRingMoveSpeed = 3f;

    [Header("점프 이동")]
    [Min(0)]
    public int jumpDamage = 5;

    [Min(0f)]
    public float jumpDuration = 0.75f;

    [Min(0f)]
    public float jumpDistanceFromPlayer = 3f;

    [Header("패턴")]
    [Min(0f)]
    public float patternStartDelay = 1f;

    [Min(0f)]
    public float patternEndDelay = 1f;
}
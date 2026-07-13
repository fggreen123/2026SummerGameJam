using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
[DisallowMultipleComponent]
public sealed class EnemyAttack : MonoBehaviour
{
    public enum EffectBaseDirection
    {
        Right,
        Up,
        Left,
        Down
    }

    [Header("References")]
    [SerializeField]
    private Enemy enemy;

    [Tooltip("공격 시작 위치입니다. 비어 있으면 적 오브젝트 위치를 사용합니다.")]
    [SerializeField]
    private Transform attackOrigin;

    [Tooltip("플레이어 방향으로 생성되는 공격 이펙트 프리팹입니다.")]
    [SerializeField]
    private GameObject attackEffectPrefab;

    [Header("Attack Shape")]
    [Tooltip("방향성 공격 판정의 폭입니다.")]
    [SerializeField, Min(0.01f)]
    private float attackWidth = 1f;

    [Tooltip("적 중심에서 이펙트를 생성할 거리입니다.")]
    [SerializeField, Min(0f)]
    private float effectSpawnDistance = 1f;

    [Tooltip("공격 이펙트가 자동으로 제거되는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float effectLifetime = 0.35f;

    [Tooltip("플레이어 레이어를 선택합니다.")]
    [SerializeField]
    private LayerMask targetMask;

    [Header("Effect Direction")]
    [Tooltip("이펙트 이미지가 원본 상태에서 바라보는 방향입니다.")]
    [SerializeField]
    private EffectBaseDirection effectBaseDirection =
        EffectBaseDirection.Right;

    private float nextAttackTime;

    private readonly HashSet<IDamageable> damagedTargets =
        new HashSet<IDamageable>();

    public bool IsReady =>
        enemy != null &&
        enemy.AttackSpeed > 0f &&
        Time.time >= nextAttackTime;

    public float AttackRange =>
        enemy != null
            ? enemy.AttackRange
            : 0f;

    private void Reset()
    {
        enemy = GetComponent<Enemy>();
        attackOrigin = transform;
    }

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (attackOrigin == null)
            attackOrigin = transform;
    }

    public bool IsInAttackRange(Transform target)
    {
        if (target == null || enemy == null)
            return false;

        Vector2 originPosition = attackOrigin.position;
        Vector2 targetPosition = target.position;

        float distanceSqr =
            (targetPosition - originPosition).sqrMagnitude;

        float range = enemy.AttackRange;

        return distanceSqr <= range * range;
    }

    public bool TryAttack(Transform target)
    {
        if (target == null)
            return false;

        if (enemy == null ||
            enemy.Data == null ||
            enemy.IsDead)
        {
            return false;
        }

        if (!IsInAttackRange(target))
            return false;

        if (!IsReady)
            return false;

        Vector2 direction =
            (Vector2)target.position -
            (Vector2)attackOrigin.position;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        direction.Normalize();

        float cooldown =
            1f / enemy.AttackSpeed;

        nextAttackTime =
            Time.time + cooldown;

        PerformAttack(direction);

        return true;
    }

    private void PerformAttack(Vector2 direction)
    {
        Vector2 originPosition =
            attackOrigin.position;

        float attackAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        SpawnAttackEffect(
            originPosition,
            direction,
            attackAngle
        );

        ApplyDamage(
            originPosition,
            direction,
            attackAngle
        );
    }

    private void SpawnAttackEffect(
        Vector2 originPosition,
        Vector2 direction,
        float attackAngle
    )
    {
        if (attackEffectPrefab == null)
            return;

        Vector2 spawnPosition =
            originPosition +
            direction * effectSpawnDistance;

        float rotationOffset =
            GetEffectRotationOffset();

        Quaternion rotation =
            Quaternion.Euler(
                0f,
                0f,
                attackAngle + rotationOffset
            );

        GameObject effectInstance =
            Instantiate(
                attackEffectPrefab,
                spawnPosition,
                rotation
            );

        if (effectLifetime > 0f)
        {
            Destroy(
                effectInstance,
                effectLifetime
            );
        }
    }

    private void ApplyDamage(
        Vector2 originPosition,
        Vector2 direction,
        float attackAngle
    )
    {
        float range = enemy.AttackRange;

        Vector2 hitboxCenter =
            originPosition +
            direction * (range * 0.5f);

        Vector2 hitboxSize =
            new Vector2(
                range,
                attackWidth
            );

        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                hitboxCenter,
                hitboxSize,
                attackAngle,
                targetMask
            );

        damagedTargets.Clear();

        foreach (Collider2D hit in hits)
        {
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            if (!damagedTargets.Add(damageable))
                continue;

            damageable.TakeDamage(enemy.Damage);
        }
    }

    private float GetEffectRotationOffset()
    {
        switch (effectBaseDirection)
        {
            case EffectBaseDirection.Up:
                return -90f;

            case EffectBaseDirection.Left:
                return 180f;

            case EffectBaseDirection.Down:
                return 90f;

            default:
                return 0f;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        attackWidth =
            Mathf.Max(
                0.01f,
                attackWidth
            );

        effectSpawnDistance =
            Mathf.Max(
                0f,
                effectSpawnDistance
            );

        effectLifetime =
            Mathf.Max(
                0f,
                effectLifetime
            );
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin =
            attackOrigin != null
                ? attackOrigin
                : transform;

        Enemy currentEnemy =
            enemy != null
                ? enemy
                : GetComponent<Enemy>();

        float range =
            currentEnemy != null
                ? currentEnemy.AttackRange
                : 2f;

        Vector2 direction = origin.right;

        Vector2 center =
            (Vector2)origin.position +
            direction * (range * 0.5f);

        Matrix4x4 previousMatrix =
            Gizmos.matrix;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        Gizmos.matrix =
            Matrix4x4.TRS(
                center,
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                ),
                Vector3.one
            );

        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                range,
                attackWidth,
                0f
            )
        );

        Gizmos.matrix =
            previousMatrix;
    }
#endif
}
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

    [Tooltip("공격 이펙트 프리팹입니다. 비어 있으면 기본 사각형 이펙트를 생성합니다.")]
    [SerializeField]
    private GameObject attackEffectPrefab;

    [Header("Attack Shape")]
    [Tooltip("기본 공격 이펙트의 폭입니다.")]
    [SerializeField, Min(0.01f)]
    private float attackWidth = 1f;

    [Tooltip("커스텀 공격 이펙트를 적 중심에서 생성할 거리입니다.")]
    [SerializeField, Min(0f)]
    private float effectSpawnDistance = 1f;

    [Tooltip("공격 이펙트가 자동으로 제거되는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float effectLifetime = 0.35f;

    [Header("Effect Direction")]
    [Tooltip("커스텀 이펙트 이미지가 원본 상태에서 바라보는 방향입니다.")]
    [SerializeField]
    private EffectBaseDirection effectBaseDirection =
        EffectBaseDirection.Right;

    private float nextAttackTime;

    private static Sprite defaultEffectSprite;

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

        Vector2 originPosition =
            attackOrigin != null
                ? attackOrigin.position
                : transform.position;

        Vector2 targetPosition =
            target.position;

        float distanceSqr =
            (targetPosition - originPosition).sqrMagnitude;

        float range =
            enemy.AttackRange;

        return distanceSqr <=
               range * range;
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

        Player player =
            target.GetComponent<Player>();

        if (player == null)
        {
            player =
                target.GetComponentInParent<Player>();
        }

        if (player == null ||
            player.IsDead)
        {
            return false;
        }

        Vector2 originPosition =
            attackOrigin != null
                ? attackOrigin.position
                : transform.position;

        Vector2 direction =
            (Vector2)target.position -
            originPosition;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        direction.Normalize();

        float cooldown =
            1f / enemy.AttackSpeed;

        nextAttackTime =
            Time.time + cooldown;

        SpawnAttackEffect(
            originPosition,
            direction
        );

        player.TakeDamage(
            enemy.Damage
        );

        return true;
    }

    private void SpawnAttackEffect(
        Vector2 originPosition,
        Vector2 direction
    )
    {
        float attackAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        GameObject effectInstance;

        if (attackEffectPrefab != null)
        {
            Vector2 spawnPosition =
                originPosition +
                direction * effectSpawnDistance;

            float rotationOffset =
                GetEffectRotationOffset();

            Quaternion rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    attackAngle +
                    rotationOffset
                );

            effectInstance =
                Instantiate(
                    attackEffectPrefab,
                    spawnPosition,
                    rotation
                );
        }
        else
        {
            effectInstance =
                CreateDefaultAttackEffect(
                    originPosition,
                    direction,
                    attackAngle
                );
        }

        float lifetime =
            effectLifetime > 0f
                ? effectLifetime
                : 0.1f;

        Destroy(
            effectInstance,
            lifetime
        );
    }

    private GameObject CreateDefaultAttackEffect(
        Vector2 originPosition,
        Vector2 direction,
        float attackAngle
    )
    {
        float range =
            Mathf.Max(
                0.01f,
                enemy.AttackRange
            );

        Vector2 center =
            originPosition +
            direction *
            (range * 0.5f);

        GameObject effectObject =
            new GameObject(
                "Enemy Attack Effect"
            );

        effectObject.transform.position =
            center;

        effectObject.transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                attackAngle
            );

        effectObject.transform.localScale =
            new Vector3(
                range,
                attackWidth,
                1f
            );

        SpriteRenderer renderer =
            effectObject.AddComponent<SpriteRenderer>();

        renderer.sprite =
            GetDefaultEffectSprite();

        renderer.color =
            new Color(
                1f,
                0.2f,
                0.2f,
                0.45f
            );

        renderer.sortingOrder =
            1000;

        return effectObject;
    }

    private static Sprite GetDefaultEffectSprite()
    {
        if (defaultEffectSprite != null)
            return defaultEffectSprite;

        Texture2D texture =
            Texture2D.whiteTexture;

        defaultEffectSprite =
            Sprite.Create(
                texture,
                new Rect(
                    0f,
                    0f,
                    texture.width,
                    texture.height
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                texture.width
            );

        defaultEffectSprite.name =
            "Runtime Default Attack Effect";

        return defaultEffectSprite;
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

        Gizmos.color =
            Color.red;

        Gizmos.DrawWireSphere(
            origin.position,
            range
        );
    }
#endif
}
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallThrowSkill : BossSkill
{
    [Header("References")]
    [Tooltip("BossController입니다.")]
    [SerializeField]
    private BossController bossController;

    [Tooltip("Rigidbody2D와 Collider2D가 포함된 공 프리팹입니다.")]
    [SerializeField]
    private GameObject ballPrefab;

    [Tooltip("공이 생성될 위치입니다. 비어 있으면 현재 오브젝트 위치를 사용합니다.")]
    [SerializeField]
    private Transform spawnPoint;

    [Header("Ball Settings")]
    [Tooltip("공이 충돌하지 않았을 때 자동으로 제거되는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float ballLifetime = 5f;

    [Tooltip("공의 회전 속도 배율입니다.")]
    [SerializeField, Min(0f)]
    private float rotationMultiplier = 180f;

    private void Reset()
    {
        bossController = GetComponentInParent<BossController>();
    }

    private void Awake()
    {
        if (bossController == null)
            bossController = GetComponentInParent<BossController>();
    }

    protected override IEnumerator ExecuteSkill()
    {
        if (bossController == null)
        {
            Debug.LogWarning(
                $"{name}: BossController가 연결되지 않았습니다.",
                this
            );

            yield break;
        }

        if (ballPrefab == null)
        {
            Debug.LogWarning(
                $"{name}: Ball Prefab이 연결되지 않았습니다.",
                this
            );

            yield break;
        }

        Transform player = bossController.Player;
        BossSkillData skillData = bossController.SkillData;

        if (player == null)
        {
            Debug.LogWarning(
                $"{name}: BossController의 Player가 연결되지 않았습니다.",
                this
            );

            yield break;
        }

        if (skillData == null)
        {
            Debug.LogWarning(
                $"{name}: BossSkillData가 연결되지 않았습니다.",
                this
            );

            yield break;
        }

        Vector3 spawnPosition =
            spawnPoint != null
                ? spawnPoint.position
                : transform.position;

        Vector2 moveDirection =
            ((Vector2)player.position - (Vector2)spawnPosition).normalized;

        if (moveDirection.sqrMagnitude <= 0.0001f)
            moveDirection = Vector2.right;

        GameObject ballObject = Instantiate(
            ballPrefab,
            spawnPosition,
            Quaternion.identity
        );

        BallProjectile projectile =
            ballObject.GetComponent<BallProjectile>();

        if (projectile == null)
            projectile = ballObject.AddComponent<BallProjectile>();

        projectile.Initialize(
            bossController.gameObject,
            moveDirection,
            skillData.ballDamage,
            skillData.ballSpeed,
            rotationMultiplier,
            ballLifetime
        );

        if (skillData.ballAttackInterval > 0f)
            yield return new WaitForSeconds(
                skillData.ballAttackInterval
            );
    }

    private sealed class BallProjectile : MonoBehaviour
    {
        private Rigidbody2D rb;
        private GameObject owner;
        private int damage;

        private bool initialized;
        private bool hasHit;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void Initialize(
            GameObject ownerObject,
            Vector2 direction,
            int ballDamage,
            float speed,
            float rotationSpeedMultiplier,
            float lifetime
        )
        {
            owner = ownerObject;
            damage = Mathf.Max(0, ballDamage);

            if (rb == null)
                rb = GetComponent<Rigidbody2D>();

            if (rb == null)
            {
                Debug.LogWarning(
                    $"{name}: 공 프리팹에 Rigidbody2D가 없습니다.",
                    this
                );

                Destroy(gameObject);
                return;
            }

            Vector2 safeDirection = direction.normalized;

            if (safeDirection.sqrMagnitude <= 0.0001f)
                safeDirection = Vector2.right;

            float safeSpeed = Mathf.Max(0f, speed);

            rb.linearVelocity = safeDirection * safeSpeed;

            float rotateDirection =
                safeDirection.x >= 0f
                    ? 1f
                    : -1f;

            rb.angularVelocity =
                -safeSpeed
                * rotationSpeedMultiplier
                * rotateDirection;

            initialized = true;

            if (lifetime > 0f)
                Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProcessHit(other);
        }

        private void OnCollisionEnter2D(
            Collision2D collision
        )
        {
            ProcessHit(collision.collider);
        }
        private void ProcessHit(Collider2D other)
        {
            if (!initialized || hasHit || other == null)
                return;

            if (IsOwner(other))
                return;

            Player player =
                other.GetComponentInParent<Player>();

            if (player != null)
            {
                IDamageable damageable =
                    player.GetComponent<IDamageable>();

                if (damageable != null)
                {
                    hasHit = true;
                    damageable.TakeDamage(damage);
                    Destroy(gameObject);
                }

                return;
            }

            hasHit = true;
            Destroy(gameObject);
        }
        private bool IsOwner(Collider2D other)
        {
            if (owner == null)
                return false;

            return other.transform.root ==
                   owner.transform.root;
        }
    }
}

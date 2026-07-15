using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HoopSkill : BossSkill
{
    [Header("References")]
    [Tooltip("BossController입니다.")]
    [SerializeField]
    private BossController bossController;

    [Tooltip("Rigidbody2D와 Collider2D가 포함된 훌라후프 프리팹입니다.")]
    [SerializeField]
    private GameObject hoopPrefab;

    [Tooltip("훌라후프가 생성되는 위치입니다.")]
    [SerializeField]
    private Transform spawnPoint;

    [Header("Attack Settings")]
    [Tooltip("한 번의 공격에서 생성하는 훌라후프 수입니다.")]
    [SerializeField, Min(1)]
    private int hoopCount = 5;

    [Tooltip("첫 발부터 마지막 발까지 걸리는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float attackDuration = 2f;

    [Tooltip("훌라후프의 피해량입니다.")]
    [SerializeField, Min(0)]
    private int damage = 5;

    [Tooltip("훌라후프의 느린 이동 속도입니다.")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 2f;

    [Tooltip("훌라후프가 자동으로 제거되는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float lifetime = 8f;

    [Tooltip("훌라후프의 회전 속도 배율입니다.")]
    [SerializeField, Min(0f)]
    private float rotationMultiplier = 180f;

    [Tooltip("첫 번째 훌라후프가 나가는 기준 각도입니다.")]
    [SerializeField]
    private float startAngle = 0f;

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

        if (hoopPrefab == null)
        {
            Debug.LogWarning(
                $"{name}: Hoop Prefab이 연결되지 않았습니다.",
                this
            );

            yield break;
        }

        int safeCount = Mathf.Max(1, hoopCount);

        float angleInterval = 360f / safeCount;

        float fireInterval =
            safeCount > 1
                ? attackDuration / (safeCount - 1)
                : 0f;

        for (int i = 0; i < safeCount; i++)
        {
            Vector3 spawnPosition =
                spawnPoint != null
                    ? spawnPoint.position
                    : bossController.transform.position;

            float angle =
                startAngle + angleInterval * i;

            Vector2 moveDirection = AngleToDirection(angle);

            SpawnHoop(
                spawnPosition,
                moveDirection
            );

            if (i < safeCount - 1 && fireInterval > 0f)
                yield return new WaitForSeconds(fireInterval);
        }
    }

    private void SpawnHoop(
        Vector3 spawnPosition,
        Vector2 moveDirection
    )
    {
        GameObject hoopObject = Instantiate(
            hoopPrefab,
            spawnPosition,
            Quaternion.identity
        );

        HoopProjectile projectile =
            hoopObject.GetComponent<HoopProjectile>();

        if (projectile == null)
            projectile = hoopObject.AddComponent<HoopProjectile>();

        projectile.Initialize(
            ownerObject: bossController.gameObject,
            direction: moveDirection,
            hoopDamage: damage,
            speed: moveSpeed,
            rotationSpeedMultiplier: rotationMultiplier,
            destroyTime: lifetime
        );
    }

    private static Vector2 AngleToDirection(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        ).normalized;
    }

    private sealed class HoopProjectile : MonoBehaviour
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
            int hoopDamage,
            float speed,
            float rotationSpeedMultiplier,
            float destroyTime
        )
        {
            owner = ownerObject;
            damage = Mathf.Max(0, hoopDamage);

            if (rb == null)
                rb = GetComponent<Rigidbody2D>();

            if (rb == null)
            {
                Debug.LogWarning(
                    $"{name}: 훌라후프 프리팹에 Rigidbody2D가 없습니다.",
                    this
                );

                Destroy(gameObject);
                return;
            }

            Vector2 safeDirection = direction.normalized;

            if (safeDirection.sqrMagnitude <= 0.0001f)
                safeDirection = Vector2.right;

            float safeSpeed = Mathf.Max(0f, speed);

            rb.linearVelocity =
                safeDirection * safeSpeed;

            float rotateDirection =
                safeDirection.x >= 0f
                    ? 1f
                    : -1f;

            rb.angularVelocity =
                -safeSpeed
                * Mathf.Max(0f, rotationSpeedMultiplier)
                * rotateDirection;

            initialized = true;

            if (destroyTime > 0f)
                Destroy(gameObject, destroyTime);
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

            // 보스 본체와 보스의 자식 콜라이더는 무시합니다.
            if (IsOwner(other))
                return;

            Player player =
                other.GetComponentInParent<Player>();

            // 플레이어가 아니면 아무 처리도 하지 않고 계속 이동합니다.
            if (player == null)
                return;

            IDamageable damageable =
                player.GetComponent<IDamageable>();

            if (damageable == null)
                return;

            hasHit = true;

            damageable.TakeDamage(damage);

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

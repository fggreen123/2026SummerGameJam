using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(EnemyAttack))]
[DisallowMultipleComponent]
public sealed class EnemyAI : MonoBehaviour
{
    public enum EnemyAIState
    {
        Patrol,
        Chase
    }

    private enum PatrolPhase
    {
        Moving,
        Idle
    }

    [Header("References")]
    [SerializeField]
    private Rigidbody2D rb;

    [SerializeField]
    private Enemy enemy;

    [SerializeField]
    private EnemyAttack enemyAttack;

    [Tooltip("적 애니메이션을 재생하는 Animator입니다.")]
    [SerializeField]
    private Animator animator;

    [Tooltip("좌우 반전할 적의 SpriteRenderer입니다.")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("Animation")]
    [Tooltip("Animator에 만든 이동 Bool 파라미터 이름입니다.")]
    [SerializeField]
    private string movingParameterName = "isMoving";

    [Tooltip("Animator에 만든 공격 Trigger 파라미터 이름입니다.")]
    [SerializeField]
    private string attackTriggerName = "Attack";

    [Tooltip("공격 애니메이션을 다시 실행할 수 있는 간격입니다.")]
    [SerializeField, Min(0.01f)]
    private float attackAnimationInterval = 1f;

    [Tooltip("이 속도보다 빠르게 움직일 때 이동 애니메이션을 재생합니다.")]
    [SerializeField, Min(0f)]
    private float movementAnimationThreshold = 0.05f;

    [Header("Player Detection")]
    [SerializeField]
    private string playerTag = "Player";

    [Tooltip("플레이어를 발견하는 거리입니다.")]
    [SerializeField, Min(0f)]
    private float detectionRadius = 6f;

    [Tooltip("플레이어 추적을 포기하는 거리입니다.")]
    [SerializeField, Min(0f)]
    private float loseTargetRadius = 8f;

    [Tooltip("플레이어를 다시 검색하는 간격입니다.")]
    [SerializeField, Min(0.05f)]
    private float playerSearchInterval = 0.5f;

    [Header("Patrol")]
    [Tooltip("한 번 이동하는 시간 범위입니다.")]
    [SerializeField]
    private Vector2 patrolMoveDuration =
        new Vector2(1.2f, 3f);

    [Tooltip("멈춰 있는 시간 범위입니다.")]
    [SerializeField]
    private Vector2 patrolIdleDuration =
        new Vector2(0.35f, 1.2f);

    [Tooltip("한 이동 구간이 끝났을 때 멈출 확률입니다.")]
    [SerializeField, Range(0f, 1f)]
    private float patrolStopChance = 0.4f;

    [Tooltip("배회 중 한 번에 방향을 꺾는 최대 각도입니다.")]
    [SerializeField, Range(0f, 180f)]
    private float patrolTurnAngleRange = 100f;

    [Tooltip("배회 중 방향 전환 속도입니다.")]
    [SerializeField, Min(0f)]
    private float patrolTurnSpeed = 140f;

    [Tooltip("추적 중 방향 전환 속도입니다.")]
    [SerializeField, Min(0f)]
    private float chaseTurnSpeed = 300f;

    [Header("Movement Feel")]
    [Tooltip("목표 속도까지 도달하는 속도입니다.")]
    [SerializeField, Min(0f)]
    private float acceleration = 18f;

    [Tooltip("멈출 때 속도가 줄어드는 정도입니다.")]
    [SerializeField, Min(0f)]
    private float deceleration = 28f;

    [Tooltip("반대 방향으로 전환할 때 가속 배율입니다.")]
    [SerializeField, Min(1f)]
    private float reverseMultiplier = 1.3f;

    [Tooltip("배회 중 이동속도 배율입니다.")]
    [SerializeField, Range(0f, 1f)]
    private float patrolSpeedMultiplier = 0.75f;

    [Tooltip("물리 충돌로 밀렸을 때 허용할 최대 속도 배율입니다.")]
    [SerializeField, Min(1f)]
    private float maximumVelocityMultiplier = 1.5f;

    public EnemyAIState CurrentState { get; private set; }

    public Transform Target => player;

    public Vector2 CurrentDirection =>
        AngleToDirection(currentAngle);

    public Vector2 DesiredMoveDirection =>
        desiredMoveDirection;

    public bool IsMoving =>
        rb != null &&
        rb.linearVelocity.sqrMagnitude >
        movementAnimationThreshold *
        movementAnimationThreshold;

    private Transform player;

    private PatrolPhase patrolPhase;

    private Vector2 desiredMoveDirection;

    private float currentAngle;
    private float targetAngle;

    private float patrolTimer;
    private float playerSearchTimer;
    private float nextAttackAnimationTime;

    private int movingParameterHash;
    private int attackTriggerHash;

    private bool wantsToMove;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        enemy = GetComponent<Enemy>();
        enemyAttack = GetComponent<EnemyAttack>();

        animator =
            GetComponentInChildren<Animator>();

        spriteRenderer =
            GetComponentInChildren<SpriteRenderer>();

        ConfigureRigidbody();
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (enemyAttack == null)
            enemyAttack = GetComponent<EnemyAttack>();

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        CacheAnimatorParameters();
        ConfigureRigidbody();
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        currentAngle =
            Random.Range(0f, 360f);

        targetAngle =
            currentAngle;

        desiredMoveDirection =
            CurrentDirection;

        playerSearchTimer = 0f;
        nextAttackAnimationTime = 0f;

        EnterPatrolMoving(true);
        SearchPlayer();
    }

    private void Update()
    {
        if (enemy == null ||
            enemy.Data == null ||
            enemy.IsDead)
        {
            wantsToMove = false;
            desiredMoveDirection = Vector2.zero;

            UpdateMovementAnimation();
            return;
        }

        float deltaTime = Time.deltaTime;

        UpdatePlayerReference(deltaTime);
        UpdateAIState();

        switch (CurrentState)
        {
            case EnemyAIState.Patrol:
                UpdatePatrol(deltaTime);
                break;

            case EnemyAIState.Chase:
                UpdateChase(deltaTime);
                break;
        }

        UpdateMovementAnimation();
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (enemy == null ||
            enemy.Data == null ||
            enemy.IsDead)
        {
            rb.linearVelocity =
                Vector2.MoveTowards(
                    rb.linearVelocity,
                    Vector2.zero,
                    deceleration *
                    Time.fixedDeltaTime
                );

            return;
        }

        ApplyMovement();
        LimitVelocity();
        UpdateSpriteFlip();
    }

    private void CacheAnimatorParameters()
    {
        movingParameterHash =
            Animator.StringToHash(
                movingParameterName
            );

        attackTriggerHash =
            Animator.StringToHash(
                attackTriggerName
            );
    }

    private void ConfigureRigidbody()
    {
        if (rb == null)
            return;

        rb.bodyType =
            RigidbodyType2D.Dynamic;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private bool ValidateReferences()
    {
        if (rb == null)
        {
            Debug.LogError(
                $"{name}: Rigidbody2D가 없습니다.",
                this
            );

            return false;
        }

        if (enemy == null)
        {
            Debug.LogError(
                $"{name}: Enemy 컴포넌트가 없습니다.",
                this
            );

            return false;
        }

        if (enemyAttack == null)
        {
            Debug.LogError(
                $"{name}: EnemyAttack 컴포넌트가 없습니다.",
                this
            );

            return false;
        }

        if (enemy.Data == null)
        {
            Debug.LogError(
                $"{name}: EnemyData가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (animator == null)
        {
            Debug.LogWarning(
                $"{name}: Animator가 연결되지 않았습니다.",
                this
            );
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning(
                $"{name}: SpriteRenderer가 연결되지 않았습니다.",
                this
            );
        }

        return true;
    }

    private void UpdatePlayerReference(
        float deltaTime
    )
    {
        if (player != null &&
            player.gameObject.activeInHierarchy)
        {
            return;
        }

        player = null;

        playerSearchTimer -= deltaTime;

        if (playerSearchTimer > 0f)
            return;

        playerSearchTimer =
            playerSearchInterval;

        SearchPlayer();
    }

    private void SearchPlayer()
    {
        GameObject playerObject;

        try
        {
            playerObject =
                GameObject.FindGameObjectWithTag(
                    playerTag
                );
        }
        catch (UnityException)
        {
            Debug.LogError(
                $"{name}: '{playerTag}' 태그가 없습니다.",
                this
            );

            return;
        }

        if (playerObject != null)
            player = playerObject.transform;
    }

    private void UpdateAIState()
    {
        if (player == null)
        {
            if (CurrentState ==
                EnemyAIState.Chase)
            {
                EnterPatrolIdle();
            }

            return;
        }

        Vector2 toPlayer =
            (Vector2)player.position -
            rb.position;

        float distanceSqr =
            toPlayer.sqrMagnitude;

        if (CurrentState ==
            EnemyAIState.Patrol)
        {
            float detectionRadiusSqr =
                detectionRadius *
                detectionRadius;

            if (distanceSqr <=
                detectionRadiusSqr)
            {
                EnterChase();
            }

            return;
        }

        float loseTargetRadiusSqr =
            loseTargetRadius *
            loseTargetRadius;

        if (distanceSqr >
            loseTargetRadiusSqr)
        {
            EnterPatrolIdle();
        }
    }

    private void UpdatePatrol(
        float deltaTime
    )
    {
        patrolTimer -= deltaTime;

        switch (patrolPhase)
        {
            case PatrolPhase.Idle:
                wantsToMove = false;

                desiredMoveDirection =
                    Vector2.zero;

                if (patrolTimer <= 0f)
                {
                    EnterPatrolMoving(
                        false
                    );
                }

                break;

            case PatrolPhase.Moving:
                currentAngle =
                    Mathf.MoveTowardsAngle(
                        currentAngle,
                        targetAngle,
                        patrolTurnSpeed *
                        deltaTime
                    );

                desiredMoveDirection =
                    CurrentDirection;

                wantsToMove = true;

                if (patrolTimer <= 0f)
                    FinishPatrolMove();

                break;
        }
    }

    private void FinishPatrolMove()
    {
        if (Random.value <=
            patrolStopChance)
        {
            EnterPatrolIdle();
            return;
        }

        ChooseNextPatrolDirection();

        patrolTimer =
            GetRandomDuration(
                patrolMoveDuration
            );
    }

    private void EnterPatrolMoving(
        bool keepCurrentDirection
    )
    {
        CurrentState =
            EnemyAIState.Patrol;

        patrolPhase =
            PatrolPhase.Moving;

        wantsToMove = true;

        if (!keepCurrentDirection)
            ChooseNextPatrolDirection();

        desiredMoveDirection =
            CurrentDirection;

        patrolTimer =
            GetRandomDuration(
                patrolMoveDuration
            );
    }

    private void EnterPatrolIdle()
    {
        CurrentState =
            EnemyAIState.Patrol;

        patrolPhase =
            PatrolPhase.Idle;

        wantsToMove = false;

        desiredMoveDirection =
            Vector2.zero;

        patrolTimer =
            GetRandomDuration(
                patrolIdleDuration
            );
    }

    private void ChooseNextPatrolDirection()
    {
        float angleOffset =
            Random.Range(
                -patrolTurnAngleRange,
                patrolTurnAngleRange
            );

        targetAngle =
            currentAngle +
            angleOffset;
    }

    private void EnterChase()
    {
        CurrentState =
            EnemyAIState.Chase;

        wantsToMove = true;
    }

    private void UpdateChase(
        float deltaTime
    )
    {
        if (player == null)
        {
            wantsToMove = false;

            desiredMoveDirection =
                Vector2.zero;

            return;
        }

        Vector2 toPlayer =
            (Vector2)player.position -
            rb.position;

        float distance =
            toPlayer.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            wantsToMove = false;

            desiredMoveDirection =
                Vector2.zero;

            TryAttackPlayer();

            return;
        }

        Vector2 directionToPlayer =
            toPlayer / distance;

        targetAngle =
            DirectionToAngle(
                directionToPlayer
            );

        currentAngle =
            Mathf.MoveTowardsAngle(
                currentAngle,
                targetAngle,
                chaseTurnSpeed *
                deltaTime
            );

        UpdateFacingDirection(
            directionToPlayer
        );

        if (enemyAttack.IsInAttackRange(player))
        {
            wantsToMove = false;

            desiredMoveDirection =
                Vector2.zero;

            TryAttackPlayer();

            return;
        }

        wantsToMove = true;

        desiredMoveDirection =
            CurrentDirection;
    }

    private void TryAttackPlayer()
    {
        if (player == null ||
            enemyAttack == null)
        {
            return;
        }

        enemyAttack.TryAttack(player);

        if (Time.time <
            nextAttackAnimationTime)
        {
            return;
        }

        nextAttackAnimationTime =
            Time.time +
            attackAnimationInterval;

        PlayAttackAnimation();
    }

    private void PlayAttackAnimation()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(
            attackTriggerHash
        );

        animator.SetTrigger(
            attackTriggerHash
        );
    }

    private void UpdateMovementAnimation()
    {
        if (animator == null ||
            rb == null)
        {
            return;
        }

        bool isMoving =
            wantsToMove &&
            rb.linearVelocity.sqrMagnitude >
            movementAnimationThreshold *
            movementAnimationThreshold;

        animator.SetBool(
            movingParameterHash,
            isMoving
        );
    }

    private void ApplyMovement()
    {
        float speedMultiplier =
            CurrentState ==
            EnemyAIState.Patrol
                ? patrolSpeedMultiplier
                : 1f;

        Vector2 targetVelocity =
            wantsToMove
                ? desiredMoveDirection.normalized *
                  enemy.MoveSpeed *
                  speedMultiplier
                : Vector2.zero;

        float changeSpeed;

        if (!wantsToMove ||
            targetVelocity.sqrMagnitude <= 0.001f)
        {
            changeSpeed =
                deceleration;
        }
        else
        {
            changeSpeed =
                acceleration;

            bool isReversing =
                rb.linearVelocity.sqrMagnitude >
                0.001f &&
                Vector2.Dot(
                    rb.linearVelocity,
                    targetVelocity
                ) < 0f;

            if (isReversing)
            {
                changeSpeed *=
                    reverseMultiplier;
            }
        }

        rb.linearVelocity =
            Vector2.MoveTowards(
                rb.linearVelocity,
                targetVelocity,
                changeSpeed *
                Time.fixedDeltaTime
            );
    }

    private void LimitVelocity()
    {
        float maximumSpeed =
            enemy.MoveSpeed *
            maximumVelocityMultiplier;

        if (maximumSpeed <= 0f)
        {
            rb.linearVelocity =
                Vector2.zero;

            return;
        }

        if (rb.linearVelocity.sqrMagnitude >
            maximumSpeed * maximumSpeed)
        {
            rb.linearVelocity =
                rb.linearVelocity.normalized *
                maximumSpeed;
        }
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer == null ||
            rb == null)
        {
            return;
        }

        float horizontalVelocity =
            rb.linearVelocity.x;

        if (horizontalVelocity > 0.01f)
        {
            spriteRenderer.flipX = true;
        }
        else if (horizontalVelocity < -0.01f)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void UpdateFacingDirection(
        Vector2 direction
    )
    {
        if (spriteRenderer == null)
            return;

        if (direction.x > 0.01f)
        {
            spriteRenderer.flipX = true;
        }
        else if (direction.x < -0.01f)
        {
            spriteRenderer.flipX = false;
        }
    }

    private static float GetRandomDuration(
        Vector2 range
    )
    {
        float minimum =
            Mathf.Min(
                range.x,
                range.y
            );

        float maximum =
            Mathf.Max(
                range.x,
                range.y
            );

        return Random.Range(
            minimum,
            maximum
        );
    }

    private static Vector2 AngleToDirection(
        float angle
    )
    {
        float radians =
            angle *
            Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        );
    }

    private static float DirectionToAngle(
        Vector2 direction
    )
    {
        return Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;
    }

    private void OnDisable()
    {
        wantsToMove = false;

        desiredMoveDirection =
            Vector2.zero;

        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;
        }

        if (animator != null)
        {
            animator.SetBool(
                movingParameterHash,
                false
            );

            animator.ResetTrigger(
                attackTriggerHash
            );
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        detectionRadius =
            Mathf.Max(
                0f,
                detectionRadius
            );

        loseTargetRadius =
            Mathf.Max(
                detectionRadius,
                loseTargetRadius
            );

        playerSearchInterval =
            Mathf.Max(
                0.05f,
                playerSearchInterval
            );

        patrolMoveDuration.x =
            Mathf.Max(
                0.01f,
                patrolMoveDuration.x
            );

        patrolMoveDuration.y =
            Mathf.Max(
                0.01f,
                patrolMoveDuration.y
            );

        patrolIdleDuration.x =
            Mathf.Max(
                0f,
                patrolIdleDuration.x
            );

        patrolIdleDuration.y =
            Mathf.Max(
                0f,
                patrolIdleDuration.y
            );

        acceleration =
            Mathf.Max(
                0f,
                acceleration
            );

        deceleration =
            Mathf.Max(
                0f,
                deceleration
            );

        reverseMultiplier =
            Mathf.Max(
                1f,
                reverseMultiplier
            );

        maximumVelocityMultiplier =
            Mathf.Max(
                1f,
                maximumVelocityMultiplier
            );

        attackAnimationInterval =
            Mathf.Max(
                0.01f,
                attackAnimationInterval
            );

        movementAnimationThreshold =
            Mathf.Max(
                0f,
                movementAnimationThreshold
            );

        if (string.IsNullOrWhiteSpace(
            movingParameterName))
        {
            movingParameterName =
                "isMoving";
        }

        if (string.IsNullOrWhiteSpace(
            attackTriggerName))
        {
            attackTriggerName =
                "Attack";
        }

        CacheAnimatorParameters();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );

        Gizmos.color =
            Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            loseTargetRadius
        );

        Gizmos.color =
            Color.green;

        Vector2 direction =
            Application.isPlaying
                ? CurrentDirection
                : Vector2.right;

        Gizmos.DrawLine(
            transform.position,
            (Vector2)transform.position +
            direction
        );
    }
#endif
}
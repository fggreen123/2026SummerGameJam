using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class JumpSkill : BossSkill
{
    [Header("References")]
    [Tooltip("BossController입니다.")]
    [SerializeField]
    private BossController bossController;

    [Tooltip("보스의 Rigidbody2D입니다.")]
    [SerializeField]
    private Rigidbody2D rb;

    [Tooltip("보스의 충돌 판정용 Collider2D입니다.")]
    [SerializeField]
    private Collider2D bodyCollider;

    [Tooltip("게임 화면을 표시하는 카메라입니다.")]
    [SerializeField]
    private Camera targetCamera;

    [Header("Jump Out")]
    [Tooltip("화면 밖으로 튀어 오르는 데 걸리는 시간입니다.")]
    [SerializeField, Min(0.01f)]
    private float jumpOutDuration = 0.65f;

    [Tooltip("화면 밖으로 나갈 때 좌우로 이동하는 거리입니다.")]
    [SerializeField, Min(0f)]
    private float jumpSideDistance = 2f;

    [Tooltip("점프 중 추가로 올라가는 포물선 높이입니다.")]
    [SerializeField, Min(0f)]
    private float jumpArcHeight = 2f;

    [Tooltip("화면 상단에서 이만큼 더 벗어난 위치까지 이동합니다.")]
    [SerializeField, Min(0f)]
    private float offscreenViewportMargin = 0.25f;

    [Tooltip("화면 밖으로 튀어 오를 때 회전하는 각도입니다.")]
    [SerializeField]
    private float jumpRotation = 360f;

    [Header("Air Time")]
    [Tooltip("화면 밖에서 대기하는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float airTime = 3f;

    [Header("Landing")]
    [Tooltip("플레이어 위치 위에서 낙하를 시작하는 높이입니다.")]
    [SerializeField, Min(0f)]
    private float landingStartHeight = 5f;

    [Tooltip("플레이어 위치까지 떨어지는 데 걸리는 시간입니다.")]
    [SerializeField, Min(0.01f)]
    private float landingDuration = 0.25f;

    [Tooltip("낙하 충돌 피해입니다.")]
    [SerializeField, Min(0)]
    private int landingDamage = 5;

    [Tooltip("낙하 중 플레이어의 현재 위치를 계속 추적합니다.")]
    [SerializeField]
    private bool targetLatestPlayerPosition = true;

    [Tooltip("낙하 피해를 줄 플레이어 레이어입니다.")]
    [SerializeField]
    private LayerMask damageLayerMask;

    [Header("Landing Damage Area")]
    [Tooltip("보스 본체 콜라이더 크기를 착지 피해 범위로 사용합니다.")]
    [SerializeField]
    private bool useBodyColliderSize = true;

    [Tooltip("본체 콜라이더 크기를 사용하지 않을 때 적용할 피해 범위입니다.")]
    [SerializeField]
    private Vector2 manualDamageSize = new Vector2(1f, 1f);

    [Tooltip("착지 피해 범위 위치 보정값입니다.")]
    [SerializeField]
    private Vector2 damageAreaOffset = Vector2.zero;

    [Header("Safe Landing")]
    [Tooltip("착지 공격 후 플레이어와 떨어질 최소 거리입니다.")]
    [SerializeField, Min(0.1f)]
    private float postLandingDistance = 1.2f;

    [Tooltip("보스가 배치되면 안 되는 타일맵 벽 레이어입니다.")]
    [SerializeField]
    private LayerMask wallLayerMask;

    [Tooltip("플레이어 주변에서 안전한 위치를 검사할 방향 개수입니다.")]
    [SerializeField, Range(4, 32)]
    private int safePositionCheckCount = 16;

    [Tooltip("첫 번째 거리에서 자리를 못 찾았을 때 추가로 검사할 횟수입니다.")]
    [SerializeField, Range(1, 8)]
    private int safePositionDistanceSteps = 3;

    [Tooltip("안전한 위치를 다시 검사할 때 늘어나는 거리입니다.")]
    [SerializeField, Min(0.05f)]
    private float safePositionDistanceStep = 0.4f;

    [Tooltip("벽 검사 시 보스 콜라이더 크기를 약간 줄이는 비율입니다.")]
    [SerializeField, Range(0.5f, 1f)]
    private float safeCheckSizeMultiplier = 0.9f;

    [Header("Landing Feel")]
    [Tooltip("낙하가 점점 빨라지는 정도입니다.")]
    [SerializeField]
    private AnimationCurve landingCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.65f, 0.15f),
            new Keyframe(1f, 1f)
        );

    private readonly Collider2D[] damageOverlapResults =
        new Collider2D[16];

    private readonly Collider2D[] placementOverlapResults =
        new Collider2D[32];

    private readonly HashSet<IDamageable> damagedTargets =
        new HashSet<IDamageable>();

    private bool landingActive;
    private bool physicsStateStored;

    private RigidbodyType2D storedBodyType;
    private Vector2 storedVelocity;
    private float storedAngularVelocity;
    private RigidbodyConstraints2D storedConstraints;
    private bool storedColliderEnabled;

    private Vector2 cachedDamageSize;
    private Vector2 cachedColliderCenterOffset;

    private Vector3 jumpStartPosition;
    private Vector3 originalScale;
    private float originalRotation;

    private void Reset()
    {
        bossController =
            GetComponentInParent<BossController>();

        rb =
            GetComponentInParent<Rigidbody2D>();

        bodyCollider =
            GetComponentInParent<Collider2D>();

        targetCamera =
            Camera.main;
    }

    private void Awake()
    {
        if (bossController == null)
        {
            bossController =
                GetComponentInParent<BossController>();
        }

        if (rb == null)
        {
            rb =
                GetComponentInParent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider =
                GetComponentInParent<Collider2D>();
        }

        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }
    }

    protected override IEnumerator ExecuteSkill()
    {
        if (!ValidateReferences())
            yield break;

        Transform bossTransform =
            bossController.transform;

        Transform player =
            bossController.Player;

        damagedTargets.Clear();
        landingActive = false;

        jumpStartPosition =
            bossTransform.position;

        originalScale =
            bossTransform.localScale;

        originalRotation =
            bossTransform.eulerAngles.z;

        StorePhysicsState();
        CacheDamageArea(bossTransform);

        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;

        rb.bodyType =
            RigidbodyType2D.Kinematic;

        /*
         * 점프를 시작하기 전에 본체 콜라이더를 끕니다.
         * 화면 밖으로 올라갈 때와 내려올 때 타일맵 벽을 무시합니다.
         */
        bodyCollider.enabled =
            false;

        yield return JumpOutsideScreen(
            bossTransform
        );

        if (airTime > 0f)
        {
            yield return new WaitForSeconds(
                airTime
            );
        }

        player =
            bossController.Player;

        if (player == null)
        {
            MoveBossImmediately(
                bossTransform,
                jumpStartPosition
            );

            RestorePhysics();
            yield break;
        }

        Vector3 landingTarget =
            player.position;

        Vector3 landingStart =
            landingTarget +
            Vector3.up * landingStartHeight;

        MoveBossImmediately(
            bossTransform,
            landingStart
        );

        bossTransform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                originalRotation
            );

        bossTransform.localScale =
            originalScale;

        /*
         * 보스 본체 콜라이더는 계속 꺼져 있습니다.
         * 피해는 별도의 OverlapBox로 검사합니다.
         */
        landingActive =
            true;

        yield return DropToPlayer(
            bossTransform,
            player,
            landingStart,
            landingTarget
        );

        /*
         * 최종 착지 위치에서도 피해를 검사합니다.
         */
        CheckLandingDamage();

        landingActive =
            false;

        /*
         * 플레이어와 겹친 상태에서 콜라이더를 켜면
         * 물리 엔진이 플레이어를 강제로 밀어냅니다.
         *
         * 따라서 먼저 플레이어 옆의 안전한 위치로
         * 보스를 이동시킨 다음 콜라이더를 켭니다.
         */
        Vector2 safePosition;

        bool foundSafePosition =
            TryFindSafeLandingPosition(
                player,
                out safePosition
            );

        if (!foundSafePosition)
        {
            /*
             * 주변에 안전한 위치를 찾지 못하면
             * 점프 전 위치로 돌아갑니다.
             */
            safePosition =
                jumpStartPosition;
        }

        MoveBossImmediately(
            bossTransform,
            safePosition
        );

        /*
         * 위치 변경을 물리 엔진에 확실히 반영한 뒤
         * 한 프레임 기다립니다.
         */
        Physics2D.SyncTransforms();
        yield return null;

        RestorePhysics();
    }

    private IEnumerator JumpOutsideScreen(
        Transform bossTransform
    )
    {
        Vector3 startPosition =
            bossTransform.position;

        Vector3 viewportPosition =
            targetCamera.WorldToViewportPoint(
                startPosition
            );

        float worldDepth =
            Mathf.Abs(
                targetCamera.transform.position.z -
                startPosition.z
            );

        Vector3 offscreenViewportPosition =
            new Vector3(
                viewportPosition.x,
                1f + offscreenViewportMargin,
                worldDepth
            );

        Vector3 offscreenPosition =
            targetCamera.ViewportToWorldPoint(
                offscreenViewportPosition
            );

        offscreenPosition.z =
            startPosition.z;

        float horizontalDirection =
            Random.value < 0.5f
                ? -1f
                : 1f;

        offscreenPosition.x +=
            jumpSideDistance *
            horizontalDirection;

        float elapsed =
            0f;

        while (elapsed < jumpOutDuration)
        {
            elapsed +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    jumpOutDuration
                );

            Vector3 linearPosition =
                Vector3.Lerp(
                    startPosition,
                    offscreenPosition,
                    progress
                );

            float arc =
                Mathf.Sin(
                    progress *
                    Mathf.PI
                ) *
                jumpArcHeight;

            linearPosition.y +=
                arc;

            MoveBossImmediately(
                bossTransform,
                linearPosition,
                false
            );

            float rotation =
                originalRotation +
                jumpRotation *
                horizontalDirection *
                progress;

            bossTransform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    rotation
                );

            float scaleBounce =
                Mathf.Sin(
                    progress *
                    Mathf.PI
                );

            bossTransform.localScale =
                new Vector3(
                    originalScale.x *
                    (1f - scaleBounce * 0.12f),

                    originalScale.y *
                    (1f + scaleBounce * 0.18f),

                    originalScale.z
                );

            yield return null;
        }

        MoveBossImmediately(
            bossTransform,
            offscreenPosition
        );

        bossTransform.localScale =
            originalScale;
    }

    private IEnumerator DropToPlayer(
        Transform bossTransform,
        Transform player,
        Vector3 startPosition,
        Vector3 initialTarget
    )
    {
        float elapsed =
            0f;

        Vector3 targetPosition =
            initialTarget;

        while (elapsed < landingDuration)
        {
            if (player == null)
                break;

            elapsed +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    landingDuration
                );

            if (targetLatestPlayerPosition)
            {
                targetPosition =
                    player.position;
            }

            float curvedProgress =
                landingCurve != null
                    ? landingCurve.Evaluate(
                        progress
                    )
                    : progress;

            Vector3 currentPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    curvedProgress
                );

            MoveBossImmediately(
                bossTransform,
                currentPosition,
                false
            );

            CheckLandingDamage();

            yield return null;
        }

        MoveBossImmediately(
            bossTransform,
            targetPosition
        );
    }

    private void CheckLandingDamage()
    {
        if (!landingActive)
            return;

        if (bossController == null)
            return;

        Vector2 damageCenter =
            (Vector2)bossController.transform.position +
            cachedColliderCenterOffset +
            damageAreaOffset;

        int hitCount =
            Physics2D.OverlapBoxNonAlloc(
                damageCenter,
                cachedDamageSize,
                0f,
                damageOverlapResults,
                damageLayerMask
            );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit =
                damageOverlapResults[i];

            damageOverlapResults[i] =
                null;

            if (hit == null)
                continue;

            if (hit.transform.root ==
                bossController.transform.root)
            {
                continue;
            }

            Player player =
                hit.GetComponentInParent<Player>();

            if (player == null)
                continue;

            IDamageable damageable =
                player.GetComponent<IDamageable>();

            if (damageable == null)
            {
                damageable =
                    player.GetComponentInParent<IDamageable>();
            }

            if (damageable == null)
                continue;

            if (!damagedTargets.Add(damageable))
                continue;

            damageable.TakeDamage(
                landingDamage
            );
        }
    }

    private bool TryFindSafeLandingPosition(
        Transform player,
        out Vector2 safePosition
    )
    {
        safePosition =
            jumpStartPosition;

        if (player == null)
            return false;

        Vector2 playerPosition =
            player.position;

        Vector2 preferredDirection =
            (Vector2)jumpStartPosition -
            playerPosition;

        if (preferredDirection.sqrMagnitude <= 0.0001f)
        {
            preferredDirection =
                Random.value < 0.5f
                    ? Vector2.left
                    : Vector2.right;
        }

        preferredDirection.Normalize();

        int directionCount =
            Mathf.Max(
                4,
                safePositionCheckCount
            );

        int distanceSteps =
            Mathf.Max(
                1,
                safePositionDistanceSteps
            );

        float preferredAngle =
            Mathf.Atan2(
                preferredDirection.y,
                preferredDirection.x
            ) *
            Mathf.Rad2Deg;

        float angleStep =
            360f /
            directionCount;

        /*
         * 가까운 거리부터 검사하고,
         * 자리가 없으면 점점 더 먼 위치를 검사합니다.
         */
        for (int distanceIndex = 0;
             distanceIndex < distanceSteps;
             distanceIndex++)
        {
            float checkDistance =
                postLandingDistance +
                safePositionDistanceStep *
                distanceIndex;

            for (int directionIndex = 0;
                 directionIndex < directionCount;
                 directionIndex++)
            {
                int alternatingIndex =
                    GetAlternatingIndex(
                        directionIndex
                    );

                float angle =
                    preferredAngle +
                    alternatingIndex *
                    angleStep;

                Vector2 direction =
                    new Vector2(
                        Mathf.Cos(
                            angle *
                            Mathf.Deg2Rad
                        ),
                        Mathf.Sin(
                            angle *
                            Mathf.Deg2Rad
                        )
                    );

                Vector2 candidate =
                    playerPosition +
                    direction *
                    checkDistance;

                if (!IsSafeBossPosition(
                        candidate,
                        player
                    ))
                {
                    continue;
                }

                safePosition =
                    candidate;

                return true;
            }
        }

        /*
         * 원래 위치가 안전하면 원래 위치를 사용합니다.
         */
        if (IsSafeBossPosition(
                jumpStartPosition,
                player
            ))
        {
            safePosition =
                jumpStartPosition;

            return true;
        }

        return false;
    }

    private int GetAlternatingIndex(
        int index
    )
    {
        if (index == 0)
            return 0;

        int step =
            (index + 1) / 2;

        return index % 2 == 1
            ? step
            : -step;
    }

    private bool IsSafeBossPosition(
        Vector2 bossPosition,
        Transform player
    )
    {
        Vector2 checkSize =
            cachedDamageSize *
            safeCheckSizeMultiplier;

        checkSize.x =
            Mathf.Max(
                0.05f,
                checkSize.x
            );

        checkSize.y =
            Mathf.Max(
                0.05f,
                checkSize.y
            );

        Vector2 checkCenter =
            bossPosition +
            cachedColliderCenterOffset;

        int hitCount =
            Physics2D.OverlapBoxNonAlloc(
                checkCenter,
                checkSize,
                0f,
                placementOverlapResults
            );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit =
                placementOverlapResults[i];

            placementOverlapResults[i] =
                null;

            if (hit == null)
                continue;

            /*
             * 보스 자신의 콜라이더와 트리거는 무시합니다.
             */
            if (hit.transform.root ==
                bossController.transform.root)
            {
                continue;
            }

            /*
             * 플레이어와 겹치는 위치는 사용하지 않습니다.
             */
            if (player != null &&
                hit.transform.root ==
                player.root)
            {
                return false;
            }

            /*
             * 타일맵 벽과 겹치는 위치는 사용하지 않습니다.
             */
            if (IsLayerInMask(
                    hit.gameObject.layer,
                    wallLayerMask
                ))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsLayerInMask(
        int layer,
        LayerMask layerMask
    )
    {
        return
            (layerMask.value &
             (1 << layer)) != 0;
    }

    private void CacheDamageArea(
        Transform bossTransform
    )
    {
        if (bodyCollider == null)
        {
            cachedDamageSize =
                manualDamageSize;

            cachedColliderCenterOffset =
                Vector2.zero;

            return;
        }

        Bounds bounds =
            bodyCollider.bounds;

        cachedDamageSize =
            useBodyColliderSize
                ? new Vector2(
                    bounds.size.x,
                    bounds.size.y
                )
                : manualDamageSize;

        cachedDamageSize.x =
            Mathf.Max(
                0.01f,
                cachedDamageSize.x
            );

        cachedDamageSize.y =
            Mathf.Max(
                0.01f,
                cachedDamageSize.y
            );

        cachedColliderCenterOffset =
            (Vector2)(
                bounds.center -
                bossTransform.position
            );
    }

    private void StorePhysicsState()
    {
        storedBodyType =
            rb.bodyType;

        storedVelocity =
            rb.linearVelocity;

        storedAngularVelocity =
            rb.angularVelocity;

        storedConstraints =
            rb.constraints;

        storedColliderEnabled =
            bodyCollider.enabled;

        physicsStateStored =
            true;
    }

    private void RestorePhysics()
    {
        landingActive =
            false;

        if (!physicsStateStored)
            return;

        rb.position =
            bossController.transform.position;

        rb.bodyType =
            storedBodyType;

        rb.constraints =
            storedConstraints;

        /*
         * 보스는 평소 이동하지 않으므로
         * 점프 이전 속도를 되살리지 않고 정지시킵니다.
         */
        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;

        Physics2D.SyncTransforms();

        /*
         * 보스와 플레이어가 겹치지 않는 안전한 위치에서만
         * 본체 콜라이더를 다시 켭니다.
         */
        bodyCollider.enabled =
            storedColliderEnabled;

        physicsStateStored =
            false;
    }

    private void MoveBossImmediately(
        Transform bossTransform,
        Vector3 position,
        bool syncTransforms = true
    )
    {
        bossTransform.position =
            position;

        rb.position =
            position;

        if (syncTransforms)
        {
            Physics2D.SyncTransforms();
        }
    }

    private bool ValidateReferences()
    {
        if (bossController == null)
        {
            Debug.LogWarning(
                $"{name}: BossController가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (bossController.Player == null)
        {
            Debug.LogWarning(
                $"{name}: BossController의 Player가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (rb == null)
        {
            Debug.LogWarning(
                $"{name}: 보스의 Rigidbody2D를 찾지 못했습니다.",
                this
            );

            return false;
        }

        if (bodyCollider == null)
        {
            Debug.LogWarning(
                $"{name}: 보스의 Collider2D를 찾지 못했습니다.",
                this
            );

            return false;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning(
                $"{name}: Target Camera가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        return true;
    }

    private void OnDisable()
    {
        landingActive =
            false;

        if (!physicsStateStored)
            return;

        /*
         * 스킬 도중 오브젝트가 비활성화되면
         * 콜라이더가 계속 꺼져 있지 않도록 복구합니다.
         */
        if (bossController != null &&
            rb != null &&
            bodyCollider != null)
        {
            RestorePhysics();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform bossTransform =
            bossController != null
                ? bossController.transform
                : transform;

        Vector2 size;

        Vector2 centerOffset =
            damageAreaOffset;

        if (Application.isPlaying)
        {
            size =
                cachedDamageSize;

            centerOffset +=
                cachedColliderCenterOffset;
        }
        else if (useBodyColliderSize &&
                 bodyCollider != null)
        {
            size =
                bodyCollider.bounds.size;

            centerOffset +=
                (Vector2)(
                    bodyCollider.bounds.center -
                    bossTransform.position
                );
        }
        else
        {
            size =
                manualDamageSize;
        }

        Gizmos.DrawWireCube(
            (Vector2)bossTransform.position +
            centerOffset,
            size
        );
    }
}
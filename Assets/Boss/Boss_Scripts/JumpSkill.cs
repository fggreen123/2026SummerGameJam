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

    [Tooltip("낙하 직전 플레이어 위치를 다시 추적할지 설정합니다.")]
    [SerializeField]
    private bool targetLatestPlayerPosition = true;

    [Tooltip("낙하 중 피해를 줄 수 있는 레이어입니다.")]
    [SerializeField]
    private LayerMask damageLayerMask = ~0;

    [Header("Landing Feel")]
    [Tooltip("낙하가 점점 빨라지는 정도입니다.")]
    [SerializeField]
    private AnimationCurve landingCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.65f, 0.15f),
            new Keyframe(1f, 1f)
        );

    private readonly Collider2D[] overlapResults =
        new Collider2D[16];

    private readonly HashSet<IDamageable> damagedTargets =
        new HashSet<IDamageable>();

    private bool landingActive;

    private void Reset()
    {
        bossController =
            GetComponentInParent<BossController>();

        rb = GetComponentInParent<Rigidbody2D>();
        bodyCollider =
            GetComponentInParent<Collider2D>();

        targetCamera = Camera.main;
    }

    private void Awake()
    {
        if (bossController == null)
        {
            bossController =
                GetComponentInParent<BossController>();
        }

        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();

        if (bodyCollider == null)
        {
            bodyCollider =
                GetComponentInParent<Collider2D>();
        }

        if (targetCamera == null)
            targetCamera = Camera.main;
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

        RigidbodyType2D previousBodyType =
            rb.bodyType;

        Vector2 previousVelocity =
            rb.linearVelocity;

        float previousAngularVelocity =
            rb.angularVelocity;

        RigidbodyConstraints2D previousConstraints =
            rb.constraints;

        bool previousColliderEnabled =
            bodyCollider.enabled;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Vector3 originalScale =
            bossTransform.localScale;

        float originalRotation =
            bossTransform.eulerAngles.z;

        yield return JumpOutsideScreen(
            bossTransform,
            originalScale,
            originalRotation
        );

        bodyCollider.enabled = false;

        if (airTime > 0f)
            yield return new WaitForSeconds(airTime);

        if (player == null)
        {
            RestorePhysics(
                previousBodyType,
                previousVelocity,
                previousAngularVelocity,
                previousConstraints,
                previousColliderEnabled
            );

            yield break;
        }

        Vector3 landingTarget =
            player.position;

        Vector3 landingStart =
            landingTarget +
            Vector3.up * landingStartHeight;

        bossTransform.position = landingStart;
        bossTransform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                originalRotation
            );

        bossTransform.localScale =
            originalScale;

        bodyCollider.enabled =
            previousColliderEnabled;

        landingActive = true;

        yield return DropToPlayer(
            bossTransform,
            player,
            landingStart,
            landingTarget
        );

        landingActive = false;

        CheckLandingDamage();

        RestorePhysics(
            previousBodyType,
            Vector2.zero,
            0f,
            previousConstraints,
            previousColliderEnabled
        );
    }

    private IEnumerator JumpOutsideScreen(
        Transform bossTransform,
        Vector3 originalScale,
        float originalRotation
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

        float elapsed = 0f;

        while (elapsed < jumpOutDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / jumpOutDuration
                );

            Vector3 linearPosition =
                Vector3.Lerp(
                    startPosition,
                    offscreenPosition,
                    progress
                );

            float arc =
                Mathf.Sin(progress * Mathf.PI) *
                jumpArcHeight;

            linearPosition.y += arc;

            bossTransform.position =
                linearPosition;

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
                Mathf.Sin(progress * Mathf.PI);

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

        bossTransform.position =
            offscreenPosition;

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
        float elapsed = 0f;

        Vector3 targetPosition =
            initialTarget;

        while (elapsed < landingDuration)
        {
            if (player == null)
                break;

            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / landingDuration
                );

            if (targetLatestPlayerPosition)
            {
                targetPosition =
                    player.position;
            }

            float curvedProgress =
                landingCurve != null
                    ? landingCurve.Evaluate(progress)
                    : progress;

            bossTransform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    curvedProgress
                );

            CheckLandingDamage();

            yield return null;
        }

        bossTransform.position =
            targetPosition;
    }

    private void CheckLandingDamage()
    {
        if (!landingActive ||
            bodyCollider == null ||
            !bodyCollider.enabled)
        {
            return;
        }

        ContactFilter2D filter =
            new ContactFilter2D();

        filter.SetLayerMask(
            damageLayerMask
        );

        filter.useTriggers = true;

        int hitCount =
            bodyCollider.Overlap(
                filter,
                overlapResults
            );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit =
                overlapResults[i];

            overlapResults[i] = null;

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
                continue;

            if (!damagedTargets.Add(damageable))
                continue;

            damageable.TakeDamage(landingDamage);
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

    private void RestorePhysics(
        RigidbodyType2D previousBodyType,
        Vector2 previousVelocity,
        float previousAngularVelocity,
        RigidbodyConstraints2D previousConstraints,
        bool previousColliderEnabled
    )
    {
        landingActive = false;

        bodyCollider.enabled =
            previousColliderEnabled;

        rb.bodyType =
            previousBodyType;

        rb.constraints =
            previousConstraints;

        rb.linearVelocity =
            previousVelocity;

        rb.angularVelocity =
            previousAngularVelocity;
    }
}

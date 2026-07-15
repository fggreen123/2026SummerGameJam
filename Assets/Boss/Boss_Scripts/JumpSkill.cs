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

    [Tooltip("보스 루트의 Rigidbody2D입니다.")]
    [SerializeField]
    private Rigidbody2D rb;

    [Tooltip("보스 몸체의 충돌 판정용 Collider2D입니다.")]
    [SerializeField]
    private Collider2D bodyCollider;

    [Tooltip("보스 스프라이트가 들어 있는 Visual 자식입니다.")]
    [SerializeField]
    private Transform visualTransform;

    [Header("Jump Out")]
    [Tooltip("화면 밖으로 튀어 오르는 연출 시간입니다.")]
    [SerializeField, Min(0.01f)]
    private float jumpOutDuration = 0.65f;

    [Tooltip("점프할 때 옆으로 이동하는 시각적 거리입니다.")]
    [SerializeField, Min(0f)]
    private float jumpSideDistance = 2f;

    [Tooltip("점프할 때 위로 올라가는 시각적 높이입니다.")]
    [SerializeField, Min(0f)]
    private float jumpHeight = 3f;

    [Tooltip("점프 중 회전하는 각도입니다.")]
    [SerializeField]
    private float jumpRotation = 360f;

    [Header("Air Time")]
    [Tooltip("화면 밖에서 대기하는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float airTime = 3f;

    [Header("Landing")]
    [Tooltip("플레이어 위에서 낙하를 시작하는 시각적 높이입니다.")]
    [SerializeField, Min(0f)]
    private float landingStartHeight = 5f;

    [Tooltip("착지하는 데 걸리는 시간입니다.")]
    [SerializeField, Min(0.01f)]
    private float landingDuration = 0.25f;

    [Tooltip("착지 피해입니다.")]
    [SerializeField, Min(0)]
    private int landingDamage = 5;

    [Tooltip("낙하 중에도 플레이어 위치를 계속 추적합니다.")]
    [SerializeField]
    private bool targetLatestPlayerPosition = true;

    [Tooltip("Player 레이어만 선택하십시오.")]
    [SerializeField]
    private LayerMask damageLayerMask;

    [Header("Landing Feel")]
    [Tooltip("낙하 속도 변화 곡선입니다.")]
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

    private SpriteRenderer[] visualRenderers;

    private Vector3 originalVisualLocalPosition;
    private Vector3 originalVisualScale;
    private Quaternion originalVisualRotation;

    private bool landingActive;

    private void Reset()
    {
        bossController =
            GetComponentInParent<BossController>();

        rb =
            GetComponentInParent<Rigidbody2D>();

        bodyCollider =
            GetComponentInParent<Collider2D>();

        Transform visual =
            transform.Find("Visual");

        if (visual != null)
        {
            visualTransform =
                visual;
        }
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

        if (visualTransform != null)
        {
            visualRenderers =
                visualTransform.GetComponentsInChildren<SpriteRenderer>(
                    true
                );

            originalVisualLocalPosition =
                visualTransform.localPosition;

            originalVisualScale =
                visualTransform.localScale;

            originalVisualRotation =
                visualTransform.localRotation;
        }
    }

    protected override IEnumerator ExecuteSkill()
    {
        if (!ValidateReferences())
        {
            yield break;
        }

        Player playerComponent =
            FindActualPlayer();

        if (playerComponent == null)
        {
            Debug.LogWarning(
                $"{name}: 활성화된 Player를 찾지 못했습니다.",
                this
            );

            yield break;
        }

        Transform playerTransform =
            playerComponent.transform;

        damagedTargets.Clear();
        landingActive = false;

        RigidbodyType2D previousBodyType =
            rb.bodyType;

        RigidbodyConstraints2D previousConstraints =
            rb.constraints;

        bool previousColliderEnabled =
            bodyCollider.enabled;

        bool previousColliderIsTrigger =
            bodyCollider.isTrigger;

        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;

        rb.bodyType =
            RigidbodyType2D.Kinematic;

        bodyCollider.enabled =
            false;

        RestoreVisualTransform();
        SetVisualVisible(true);

        yield return PlayJumpOut();

        SetVisualVisible(false);

        if (airTime > 0f)
        {
            yield return new WaitForSeconds(
                airTime
            );
        }

        if (playerComponent == null)
        {
            RestoreVisualTransform();
            SetVisualVisible(true);

            RestorePhysics(
                previousBodyType,
                previousConstraints,
                previousColliderEnabled,
                previousColliderIsTrigger
            );

            yield break;
        }

        Vector2 landingTarget =
            playerTransform.position;

        SetBossGroundPosition(
            landingTarget
        );

        RestoreVisualTransform();

        visualTransform.localPosition =
            originalVisualLocalPosition +
            Vector3.up *
            landingStartHeight;

        SetVisualVisible(true);

        // 착지 중 플레이어를 밀지 않도록 Trigger로 사용합니다.
        bodyCollider.isTrigger =
            true;

        bodyCollider.enabled =
            previousColliderEnabled;

        Physics2D.SyncTransforms();

        landingActive =
            true;

        yield return PlayLanding(
            playerComponent,
            landingTarget
        );

        landingActive =
            false;

        RestoreVisualTransform();

        Physics2D.SyncTransforms();

        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;

        yield return new WaitForFixedUpdate();

        RestorePhysics(
            previousBodyType,
            previousConstraints,
            previousColliderEnabled,
            previousColliderIsTrigger
        );
    }

    private Player FindActualPlayer()
    {
        Player foundPlayer =
            FindFirstObjectByType<Player>();

        if (foundPlayer != null &&
            foundPlayer.gameObject.activeInHierarchy)
        {
            return foundPlayer;
        }

        GameObject taggedPlayer = null;

        try
        {
            taggedPlayer =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );
        }
        catch (UnityException)
        {
            Debug.LogWarning(
                $"{name}: Player 태그가 등록되어 있지 않습니다.",
                this
            );
        }

        if (taggedPlayer == null)
        {
            return null;
        }

        foundPlayer =
            taggedPlayer.GetComponent<Player>();

        if (foundPlayer == null)
        {
            foundPlayer =
                taggedPlayer.GetComponentInParent<Player>();
        }

        if (foundPlayer == null)
        {
            foundPlayer =
                taggedPlayer.GetComponentInChildren<Player>();
        }

        return foundPlayer;
    }

    private IEnumerator PlayJumpOut()
    {
        Vector3 startLocalPosition =
            originalVisualLocalPosition;

        float horizontalDirection =
            Random.value < 0.5f
                ? -1f
                : 1f;

        float elapsed = 0f;

        while (elapsed < jumpOutDuration)
        {
            elapsed +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    jumpOutDuration
                );

            float heightProgress =
                Mathf.Sin(
                    progress *
                    Mathf.PI *
                    0.5f
                );

            float sideOffset =
                jumpSideDistance *
                horizontalDirection *
                progress;

            float heightOffset =
                jumpHeight *
                heightProgress;

            visualTransform.localPosition =
                startLocalPosition +
                new Vector3(
                    sideOffset,
                    heightOffset,
                    0f
                );

            visualTransform.localRotation =
                originalVisualRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    jumpRotation *
                    horizontalDirection *
                    progress
                );

            float stretch =
                Mathf.Sin(
                    progress *
                    Mathf.PI
                );

            visualTransform.localScale =
                new Vector3(
                    originalVisualScale.x *
                    (1f - stretch * 0.12f),

                    originalVisualScale.y *
                    (1f + stretch * 0.18f),

                    originalVisualScale.z
                );

            yield return null;
        }
    }

    private IEnumerator PlayLanding(
        Player playerComponent,
        Vector2 initialTarget
    )
    {
        float elapsed = 0f;

        Vector2 targetPosition =
            initialTarget;

        Vector3 landingVisualStart =
            originalVisualLocalPosition +
            Vector3.up *
            landingStartHeight;

        while (elapsed < landingDuration)
        {
            if (playerComponent == null)
            {
                break;
            }

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
                    playerComponent.transform.position;

                SetBossGroundPosition(
                    targetPosition
                );
            }

            float curvedProgress =
                landingCurve != null
                    ? landingCurve.Evaluate(
                        progress
                    )
                    : progress;

            visualTransform.localPosition =
                Vector3.Lerp(
                    landingVisualStart,
                    originalVisualLocalPosition,
                    curvedProgress
                );

            Physics2D.SyncTransforms();

            CheckLandingDamage();

            yield return null;
        }

        SetBossGroundPosition(
            targetPosition
        );

        visualTransform.localPosition =
            originalVisualLocalPosition;

        Physics2D.SyncTransforms();

        CheckLandingDamage();
    }

    private void SetBossGroundPosition(
        Vector2 position
    )
    {
        rb.position =
            position;

        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;
    }

    private void CheckLandingDamage()
    {
        if (!landingActive)
        {
            return;
        }

        if (bodyCollider == null ||
            !bodyCollider.enabled)
        {
            return;
        }

        ContactFilter2D filter =
            new ContactFilter2D();

        filter.useLayerMask =
            true;

        filter.SetLayerMask(
            damageLayerMask
        );

        filter.useTriggers =
            true;

        int hitCount =
            bodyCollider.Overlap(
                filter,
                overlapResults
            );

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hit =
                overlapResults[i];

            overlapResults[i] =
                null;

            if (hit == null)
            {
                continue;
            }

            if (hit.transform.root ==
                bossController.transform.root)
            {
                continue;
            }

            Player player =
                hit.GetComponentInParent<Player>();

            if (player == null)
            {
                continue;
            }

            IDamageable damageable =
                player.GetComponent<IDamageable>();

            if (damageable == null)
            {
                continue;
            }

            if (!damagedTargets.Add(
                    damageable
                ))
            {
                continue;
            }

            damageable.TakeDamage(
                landingDamage
            );
        }
    }

    private void RestoreVisualTransform()
    {
        if (visualTransform == null)
        {
            return;
        }

        visualTransform.localPosition =
            originalVisualLocalPosition;

        visualTransform.localScale =
            originalVisualScale;

        visualTransform.localRotation =
            originalVisualRotation;
    }

    private void SetVisualVisible(
        bool visible
    )
    {
        if (visualRenderers == null)
        {
            return;
        }

        for (int i = 0;
             i < visualRenderers.Length;
             i++)
        {
            if (visualRenderers[i] == null)
            {
                continue;
            }

            visualRenderers[i].enabled =
                visible;
        }
    }

    private void RestorePhysics(
        RigidbodyType2D previousBodyType,
        RigidbodyConstraints2D previousConstraints,
        bool previousColliderEnabled,
        bool previousColliderIsTrigger
    )
    {
        landingActive =
            false;

        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity =
            0f;

        rb.constraints =
            previousConstraints;

        rb.bodyType =
            previousBodyType;

        bodyCollider.enabled =
            previousColliderEnabled;

        bodyCollider.isTrigger =
            previousColliderIsTrigger;

        Physics2D.SyncTransforms();
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

        if (rb == null)
        {
            Debug.LogWarning(
                $"{name}: Rigidbody2D가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (bodyCollider == null)
        {
            Debug.LogWarning(
                $"{name}: Body Collider가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (visualTransform == null)
        {
            Debug.LogWarning(
                $"{name}: Visual Transform이 연결되지 않았습니다.",
                this
            );

            return false;
        }

        return true;
    }
}
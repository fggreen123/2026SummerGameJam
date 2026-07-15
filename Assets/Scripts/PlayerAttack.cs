using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player))]
[DisallowMultipleComponent]
public sealed class PlayerAttack : MonoBehaviour
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
    private Player player;

    [Tooltip("자식 Visual 오브젝트에 있는 Animator입니다.")]
    [SerializeField]
    private Animator animator;

    [Tooltip("공격 시작 위치입니다. 비어 있으면 플레이어 오브젝트 위치를 사용합니다.")]
    [SerializeField]
    private Transform attackOrigin;

    [Tooltip("공격 이펙트 프리팹입니다. 비어 있으면 기본 사각형 이펙트를 생성합니다.")]
    [SerializeField]
    private GameObject attackEffectPrefab;

    [Header("Attack Shape")]
    [Tooltip("공격 판정의 폭입니다.")]
    [SerializeField, Min(0.01f)]
    private float attackWidth = 1f;

    [Tooltip("커스텀 공격 이펙트가 생성되는 거리입니다.")]
    [SerializeField, Min(0f)]
    private float effectSpawnDistance = 1f;

    [Tooltip("공격 이펙트가 제거되는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float effectLifetime = 0.25f;

    [Tooltip("적 레이어를 선택합니다. Nothing이면 전체 레이어에서 Enemy만 판정합니다.")]
    [SerializeField]
    private LayerMask targetMask;

    [Header("Animator Parameters")]
    [Tooltip("공격 애니메이션을 실행하는 Trigger 파라미터입니다.")]
    [SerializeField]
    private string attackParameter = "Attack";

    [Tooltip("공격 방향의 X값을 전달하는 Float 파라미터입니다.")]
    [SerializeField]
    private string attackXParameter = "AttackX";

    [Tooltip("공격 방향의 Y값을 전달하는 Float 파라미터입니다.")]
    [SerializeField]
    private string attackYParameter = "AttackY";

    [Header("Effect Direction")]
    [Tooltip("공격 이펙트 원본 이미지가 바라보는 방향입니다.")]
    [SerializeField]
    private EffectBaseDirection effectBaseDirection =
        EffectBaseDirection.Right;

    private InputAction attackAction;
    private Camera mainCamera;

    private float nextAttackTime;

    private Vector2 lastAttackDirection =
        Vector2.down;

    private int attackHash;
    private int attackXHash;
    private int attackYHash;

    private readonly HashSet<Enemy> damagedTargets =
        new HashSet<Enemy>();

    private static Sprite defaultEffectSprite;

    public bool IsReady =>
        player != null &&
        player.AttackSpeed > 0f &&
        Time.time >= nextAttackTime;
    public CardDistribution hct;

    public Vector2 LastAttackDirection =>
        lastAttackDirection;

    private void Reset()
    {
        player =
            GetComponent<Player>();

        animator =
            GetComponentInChildren<Animator>();

        attackOrigin =
            transform;
    }

    private void Awake()
    {
        if (player == null)
        {
            player =
                GetComponent<Player>();
        }

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (attackOrigin == null)
        {
            attackOrigin =
                transform;
        }

        mainCamera =
            Camera.main;

        CacheAnimatorHashes();
        CreateAttackAction();
    }

    private void OnEnable()
    {
        attackAction?.Enable();
    }

    private void OnDisable()
    {
        attackAction?.Disable();

        if (animator != null)
        {
            animator.ResetTrigger(
                attackHash
            );
        }
    }

    private void OnDestroy()
    {
        attackAction?.Dispose();
    }

    private void CacheAnimatorHashes()
    {
        attackHash =
            Animator.StringToHash(
                attackParameter
            );

        attackXHash =
            Animator.StringToHash(
                attackXParameter
            );

        attackYHash =
            Animator.StringToHash(
                attackYParameter
            );
    }

    private void CreateAttackAction()
    {
        attackAction =
            new InputAction(
                name: "Attack",
                type: InputActionType.Button
            );

        attackAction.AddBinding(
            "<Keyboard>/space"
        );

        attackAction.AddBinding(
            "<Mouse>/leftButton"
        );
    }

    private void Update()
    {
        if (player == null ||
            player.Data == null ||
            player.IsDead)
        {
            return;
        }

        if (attackAction != null &&
            attackAction.WasPressedThisFrame())
        {
            if(!hct.HandCenterToggle) TryAttack();
        }
    }

    public bool TryAttack()
    {
        if (!IsReady)
            return false;

        if (Mouse.current == null)
        {
            Debug.LogWarning(
                $"{name}: 마우스 입력 장치를 찾을 수 없습니다.",
                this
            );

            return false;
        }

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning(
                $"{name}: MainCamera 태그가 설정된 카메라를 찾을 수 없습니다.",
                this
            );

            return false;
        }

        Vector2 originPosition =
            attackOrigin != null
                ? attackOrigin.position
                : transform.position;

        Vector2 mouseScreenPosition =
            Mouse.current.position.ReadValue();

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    mouseScreenPosition.x,
                    mouseScreenPosition.y,
                    Mathf.Abs(
                        mainCamera.transform.position.z -
                        transform.position.z
                    )
                )
            );

        Vector2 direction =
            (Vector2)mouseWorldPosition -
            originPosition;

        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        direction.Normalize();

        lastAttackDirection =
            direction;

        float cooldown =
            1f / player.AttackSpeed;

        nextAttackTime =
            Time.time + cooldown;

        PlayAttackAnimation(
            direction
        );

        //AudioManager.instance.PlaySfx(AudioManager.Sfx.PlayerSwordSound);

        PerformAttack(
            direction
        );

        return true;
    }

    private void PlayAttackAnimation(
        Vector2 direction
    )
    {
        if (animator == null)
            return;

        animator.SetFloat(
            attackXHash,
            direction.x
        );

        animator.SetFloat(
            attackYHash,
            direction.y
        );

        animator.ResetTrigger(
            attackHash
        );

        animator.SetTrigger(
            attackHash
        );
    }

    private void PerformAttack(
        Vector2 direction
    )
    {
        Vector2 originPosition =
            attackOrigin != null
                ? attackOrigin.position
                : transform.position;

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
        GameObject effectInstance;

        if (attackEffectPrefab != null)
        {
            Vector2 spawnPosition =
                originPosition +
                direction *
                effectSpawnDistance;

            float rotationOffset =
                GetEffectRotationOffset();

            effectInstance =
                Instantiate(
                    attackEffectPrefab,
                    spawnPosition,
                    Quaternion.Euler(
                        0f,
                        0f,
                        attackAngle +
                        rotationOffset
                    )
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
                player.AttackRange
            );

        Vector2 center = GetAttackCenter(
            originPosition,
            direction,
            range
        );

        GameObject effectObject =
            new GameObject(
                "Player Attack Effect"
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

        SpriteRenderer effectRenderer =
            effectObject.AddComponent<SpriteRenderer>();

        effectRenderer.sprite =
            GetDefaultEffectSprite();

        effectRenderer.color =
            new Color(
                0.25f,
                0.75f,
                1f,
                0.45f
            );

        effectRenderer.sortingOrder =
            1000;

        return effectObject;
    }

    private void ApplyDamage(
        Vector2 originPosition,
        Vector2 direction,
        float attackAngle
    )
    {
        float range =
            Mathf.Max(
                0.1f,
                player.AttackRange
            );

        Vector2 hitboxCenter = GetAttackCenter(
            originPosition,
            direction,
            range
        );

        Vector2 hitboxSize =
            new Vector2(
                range,
                Mathf.Max(
                    0.1f,
                    attackWidth
                )
            );

        int mask =
            targetMask.value == 0
                ? Physics2D.AllLayers
                : targetMask.value;

        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                hitboxCenter,
                hitboxSize,
                attackAngle,
                mask
            );

        damagedTargets.Clear();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            Enemy targetEnemy =
                hit.GetComponentInParent<Enemy>();

            if (targetEnemy == null ||
                targetEnemy.IsDead)
            {
                continue;
            }

            if (!damagedTargets.Add(
                targetEnemy))
            {
                continue;
            }

            targetEnemy.TakeDamage(
                player.Damage
            );
        }
    }

    private static Vector2 GetAttackCenter(
        Vector2 originPosition,
        Vector2 direction,
        float range
    )
    {
        return originPosition +
               direction * (range * 0.5f);
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
            "Runtime Default Player Attack Effect";

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

        if (string.IsNullOrWhiteSpace(
            attackParameter))
        {
            attackParameter =
                "Attack";
        }

        if (string.IsNullOrWhiteSpace(
            attackXParameter))
        {
            attackXParameter =
                "AttackX";
        }

        if (string.IsNullOrWhiteSpace(
            attackYParameter))
        {
            attackYParameter =
                "AttackY";
        }

        CacheAnimatorHashes();
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin =
            attackOrigin != null
                ? attackOrigin
                : transform;

        Player currentPlayer =
            player != null
                ? player
                : GetComponent<Player>();

        float range =
            currentPlayer != null
                ? currentPlayer.AttackRange
                : 2f;

        Vector2 direction =
            Application.isPlaying
                ? lastAttackDirection
                : Vector2.right;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction =
                Vector2.right;
        }

        direction.Normalize();

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        Vector2 center = GetAttackCenter(
            origin.position,
            direction,
            range
        );

        Matrix4x4 previousMatrix =
            Gizmos.matrix;

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

        Gizmos.color =
            Color.cyan;

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
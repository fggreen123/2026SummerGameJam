using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Player))]
[DisallowMultipleComponent]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Rigidbody2D rb;

    [SerializeField]
    private Player player;

    [Tooltip("좌우 반전과 통통 튀는 연출을 적용할 자식 스프라이트")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Tooltip("자식 이미지 오브젝트에 있는 Animator")]
    [SerializeField]
    private Animator animator;

    [Header("Movement Feel")]
    [Tooltip("목표 속도에 도달하는 속도입니다.")]
    [SerializeField, Min(0f)]
    private float acceleration = 35f;

    [Tooltip("입력을 놓았을 때 멈추는 속도입니다.")]
    [SerializeField, Min(0f)]
    private float deceleration = 45f;

    [Tooltip("반대 방향 입력 시 방향 전환 속도 배율입니다.")]
    [SerializeField, Min(1f)]
    private float reverseMultiplier = 1.25f;

    [Header("Bounce Motion")]
    [Tooltip("이동 중 위로 튀어 오르는 높이")]
    [SerializeField, Min(0f)]
    private float bounceHeight = 0.08f;

    [Tooltip("통통 튀는 속도")]
    [SerializeField, Min(0f)]
    private float bounceSpeed = 10f;

    [Header("Animator Parameters")]
    [SerializeField]
    private string isMovingParameter = "IsMoving";

    [SerializeField]
    private string moveXParameter = "MoveX";

    [SerializeField]
    private string moveYParameter = "MoveY";

    public Vector2 MoveInput { get; private set; }

    public Vector2 LastMoveDirection { get; private set; } =
        Vector2.down;

    public bool Moveable = true;
    public bool IsMoving =>
        rb != null &&
        rb.linearVelocity.sqrMagnitude > 0.01f;

    private InputAction moveAction;

    private Transform spriteTransform;
    private Vector3 spriteStartLocalPosition;

    private float bounceTime;
    private bool canBounce;

    private int isMovingHash;
    private int moveXHash;
    private int moveYHash;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (player == null)
            player = GetComponent<Player>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer != null)
        {
            spriteTransform =
                spriteRenderer.transform;

            canBounce =
                spriteTransform != transform &&
                spriteTransform.IsChildOf(transform);

            if (canBounce)
            {
                spriteStartLocalPosition =
                    spriteTransform.localPosition;
            }
        }

        CacheAnimatorHashes();
        CreateMoveAction();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();

        MoveInput = Vector2.zero;
        bounceTime = 0f;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        ResetBouncePosition();
        ResetAnimation();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
    }

    private void CreateMoveAction()
    {
        moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );

        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
    }

    private void CacheAnimatorHashes()
    {
        isMovingHash =
            Animator.StringToHash(
                isMovingParameter
            );

        moveXHash =
            Animator.StringToHash(
                moveXParameter
            );

        moveYHash =
            Animator.StringToHash(
                moveYParameter
            );
    }

    private void Update()
    {
        if (player == null ||
            player.Data == null ||
            player.IsDead ||
            !Moveable
            )
        {
            MoveInput = Vector2.zero;
            UpdateAnimation();
            return;
        }

        MoveInput =
            moveAction.ReadValue<Vector2>();

        if (MoveInput.sqrMagnitude > 1f)
            MoveInput = MoveInput.normalized;

        if (MoveInput.sqrMagnitude > 0.001f)
        {
            LastMoveDirection =
                MoveInput.normalized;
        }

        UpdateSpriteFlip();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (player == null ||
            player.Data == null ||
            player.IsDead)
        {
            rb.linearVelocity =
                Vector2.zero;

            return;
        }

        Vector2 targetVelocity =
            MoveInput *
            player.MoveSpeed;

        float changeSpeed;

        if (MoveInput.sqrMagnitude <= 0.001f)
        {
            changeSpeed =
                deceleration;
        }
        else
        {
            changeSpeed =
                acceleration;

            bool isReversing =
                rb.linearVelocity.sqrMagnitude > 0.001f &&
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

    private void LateUpdate()
    {
        UpdateBounceMotion();
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer == null)
            return;

        if (MoveInput.x > 0.001f)
        {
            spriteRenderer.flipX =
                true;
        }
        else if (MoveInput.x < -0.001f)
        {
            spriteRenderer.flipX =
                false;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        bool moving =
            MoveInput.sqrMagnitude >
            0.001f;

        animator.SetBool(
            isMovingHash,
            moving
        );

        if (!moving)
            return;

        animator.SetFloat(
            moveXHash,
            MoveInput.x
        );

        animator.SetFloat(
            moveYHash,
            MoveInput.y
        );
    }

    private void ResetAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool(
            isMovingHash,
            false
        );

        animator.SetFloat(
            moveXHash,
            0f
        );

        animator.SetFloat(
            moveYHash,
            0f
        );
    }

    private void UpdateBounceMotion()
    {
        if (!canBounce ||
            spriteTransform == null)
        {
            return;
        }

        bool moving =
            MoveInput.sqrMagnitude > 0.001f &&
            rb != null &&
            rb.linearVelocity.sqrMagnitude > 0.01f;

        if (!moving)
        {
            bounceTime = 0f;
            ResetBouncePosition();
            return;
        }

        bounceTime +=
            Time.deltaTime *
            bounceSpeed;

        float height =
            Mathf.Abs(
                Mathf.Sin(bounceTime)
            ) * bounceHeight;

        spriteTransform.localPosition =
            spriteStartLocalPosition +
            Vector3.up * height;
    }

    private void ResetBouncePosition()
    {
        if (!canBounce ||
            spriteTransform == null)
        {
            return;
        }

        spriteTransform.localPosition =
            spriteStartLocalPosition;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
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

        bounceHeight =
            Mathf.Max(
                0f,
                bounceHeight
            );

        bounceSpeed =
            Mathf.Max(
                0f,
                bounceSpeed
            );

        if (string.IsNullOrWhiteSpace(
            isMovingParameter))
        {
            isMovingParameter =
                "IsMoving";
        }

        if (string.IsNullOrWhiteSpace(
            moveXParameter))
        {
            moveXParameter =
                "MoveX";
        }

        if (string.IsNullOrWhiteSpace(
            moveYParameter))
        {
            moveYParameter =
                "MoveY";
        }

        CacheAnimatorHashes();
    }
#endif
}
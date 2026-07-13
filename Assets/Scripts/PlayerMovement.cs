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

    [Tooltip("좌우 반전할 스프라이트 렌더러입니다.")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

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

    public Vector2 MoveInput { get; private set; }

    public Vector2 LastMoveDirection { get; private set; } =
        Vector2.down;

    public bool IsMoving =>
        rb != null &&
        rb.linearVelocity.sqrMagnitude > 0.01f;

    private InputAction moveAction;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (player == null)
            player = GetComponent<Player>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

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

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
    }

    private void CreateMoveAction()
    {
        moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value
        );

        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
    }

    private void Update()
    {
        if (player == null ||
            player.Data == null ||
            player.IsDead)
        {
            MoveInput = Vector2.zero;
            return;
        }

        MoveInput = moveAction.ReadValue<Vector2>();

        if (MoveInput.sqrMagnitude > 1f)
            MoveInput = MoveInput.normalized;

        if (MoveInput.sqrMagnitude > 0.001f)
            LastMoveDirection = MoveInput.normalized;

        UpdateSpriteFlip();
    }
    private void UpdateSpriteFlip()
        {
            if (spriteRenderer == null)
                return;

            if (MoveInput.x > 0.001f)
            {
                // D 입력: 좌우 반전
                spriteRenderer.flipX = true;
            }
            else if (MoveInput.x < -0.001f)
            {
                // A 입력: 원래 방향
                spriteRenderer.flipX = false;
            }
        }


    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (player == null ||
            player.Data == null ||
            player.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetVelocity =
            MoveInput * player.MoveSpeed;

        float changeSpeed;

        if (MoveInput.sqrMagnitude <= 0.001f)
        {
            changeSpeed = deceleration;
        }
        else
        {
            changeSpeed = acceleration;

            bool isReversing =
                rb.linearVelocity.sqrMagnitude > 0.001f &&
                Vector2.Dot(
                    rb.linearVelocity,
                    targetVelocity
                ) < 0f;

            if (isReversing)
                changeSpeed *= reverseMultiplier;
        }

        rb.linearVelocity =
            Vector2.MoveTowards(
                rb.linearVelocity,
                targetVelocity,
                changeSpeed * Time.fixedDeltaTime
            );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        acceleration = Mathf.Max(0f, acceleration);
        deceleration = Mathf.Max(0f, deceleration);
        reverseMultiplier = Mathf.Max(1f, reverseMultiplier);
    }
#endif
}
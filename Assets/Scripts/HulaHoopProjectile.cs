using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[DisallowMultipleComponent]
public sealed class HulaHoopProjectile : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("훌라후프가 회전하는 속도입니다.")]
    [SerializeField]
    private float spinSpeed = 720f;

    private Rigidbody2D rb;
    private CircleCollider2D hitCollider;

    private Vector2 startPosition;
    private float maxDistance;
    private int damage;

    private LayerMask targetMask;
    private LayerMask obstacleMask;

    private Transform ownerRoot;
    private bool initialized;
    private bool consumed;

    private void Reset()
    {
        CacheComponents();
        ConfigureComponents();
    }

    private void Awake()
    {
        CacheComponents();
        ConfigureComponents();
    }

    private void CacheComponents()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (hitCollider == null)
            hitCollider = GetComponent<CircleCollider2D>();
    }

    private void ConfigureComponents()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;
            rb.interpolation =
                RigidbodyInterpolation2D.Interpolate;
        }

        if (hitCollider != null)
            hitCollider.isTrigger = true;
    }

    public void Initialize(
        Vector2 direction,
        float speed,
        float maxDistance,
        int damage,
        LayerMask targetMask,
        LayerMask obstacleMask,
        Transform owner
    )
    {
        CacheComponents();
        ConfigureComponents();

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        direction.Normalize();

        this.maxDistance =
            Mathf.Max(0.01f, maxDistance);

        this.damage =
            Mathf.Max(0, damage);

        this.targetMask = targetMask;
        this.obstacleMask = obstacleMask;

        ownerRoot =
            owner != null
                ? owner.root
                : null;

        startPosition = rb.position;
        consumed = false;
        initialized = true;

        rb.linearVelocity =
            direction * Mathf.Max(0.1f, speed);

        rb.angularVelocity = spinSpeed;
    }

    private void FixedUpdate()
    {
        if (!initialized || consumed)
            return;

        float travelledDistanceSqr =
            (rb.position - startPosition).sqrMagnitude;

        if (travelledDistanceSqr >=
            maxDistance * maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized ||
            consumed ||
            other == null)
        {
            return;
        }

        if (ownerRoot != null &&
            other.transform.root == ownerRoot)
        {
            return;
        }

        int otherLayerMask =
            1 << other.gameObject.layer;

        bool isTarget =
            (targetMask.value & otherLayerMask) != 0;

        bool isObstacle =
            (obstacleMask.value & otherLayerMask) != 0;

        if (!isTarget && !isObstacle)
            return;

        if (isTarget)
        {
            IDamageable damageable =
                other.GetComponentInParent<IDamageable>();

            if (damageable != null)
                damageable.TakeDamage(damage);
        }

        consumed = true;
        rb.linearVelocity = Vector2.zero;
        Destroy(gameObject);
    }
}

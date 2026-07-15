using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallAttack : MonoBehaviour
{
    [UnitHeaderInspectable("Movement Settings")]
    Vector3 target = new Vector3(8, 0, 0);
    public float speed = 10f;
    public float destroyTime = 5.0f;

    private Rigidbody2D rb;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Vector3 straightTarget = new Vector3(target.x,transform.position.y,target.z);
        Vector2 moveDirection = ((Vector2)straightTarget - (Vector2)transform.position).normalized;
        rb.linearVelocity = moveDirection * speed;

        float rotateDir = moveDirection.x > 0 ? 1f : -1f;

        rb.angularVelocity = -speed * rotateDir * 180f;

        Destroy(gameObject, destroyTime);
    }
}

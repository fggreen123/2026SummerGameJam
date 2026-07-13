using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private Vector2 MoveInput;
    private Rigidbody2D rb;
    public float MoveSpeed = 5f;
    public bool Moveable = true;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        MoveInput.x = Input.GetAxisRaw("Horizontal");
        MoveInput.y = Input.GetAxisRaw("Vertical");
        MoveInput.Normalize();
    }
    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(MoveInput.x * MoveSpeed, MoveInput.y*MoveSpeed);
    }
}

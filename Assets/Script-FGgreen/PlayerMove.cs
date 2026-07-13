using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private Vector2 MoveInput;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    public float MoveSpeed = 5f;
    public bool Moveable = true;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        MoveInput.x = Input.GetAxisRaw("Horizontal");
        MoveInput.y = Input.GetAxisRaw("Vertical");
        MoveInput.Normalize();
    }
    private void FixedUpdate()
    {
        if (Keyboard.current.aKey.IsPressed() || Keyboard.current.rightArrowKey.IsPressed()) sr.flipX = false;
        else if (Keyboard.current.dKey.IsPressed()|| Keyboard.current.leftArrowKey.IsPressed()) sr.flipX = true;
        rb.linearVelocity = new Vector2(MoveInput.x * MoveSpeed, MoveInput.y * MoveSpeed);
    }
}

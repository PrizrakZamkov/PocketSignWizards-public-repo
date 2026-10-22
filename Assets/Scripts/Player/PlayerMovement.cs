using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    public float speed;
    public VariableJoystick variableJoystick;
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Keyboard")]
    [SerializeField] private bool useKeyboard = true;

    public Vector2 LastDirection { get; private set; } = Vector2.right;
    public bool FacingRight { get; private set; } = true;

    void Awake()
    {
        Instance = this;
    }

    public void FixedUpdate()
    {
        Vector2 joystickDirection = Vector2.zero;

        if (variableJoystick != null)
        {
            joystickDirection = Vector2.up * variableJoystick.Vertical
                              + Vector2.right * variableJoystick.Horizontal;
        }

        Vector2 keyboardDirection = Vector2.zero;

        if (useKeyboard)
        {
            keyboardDirection = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
        }

        Vector2 direction = joystickDirection != Vector2.zero
            ? joystickDirection
            : keyboardDirection;

        rb.linearVelocity = direction.normalized * speed;

        if (animator != null)
            animator.SetBool("IsRunning", direction != Vector2.zero);

        if (direction.x != 0)
        {
            if (spriteRenderer != null)
                spriteRenderer.flipX = direction.x < 0;

            FacingRight = direction.x >= 0;
            LastDirection = direction.normalized;
        }
    }

    public void StopMovement()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}
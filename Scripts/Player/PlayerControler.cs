using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControler : MonoBehaviour
{
    public float moveSpeed = 6f; 
    private PlayerStatsManager stats;
    private PlayerInput playerInput;
    private Rigidbody2D rb;

    void Start()
    {
    playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody2D>();
    stats = GetComponent<PlayerStatsManager>();
    }

    void FixedUpdate()
    {
        Vector2 input = playerInput.MoveInput;
        // flip player 
        if (input.x > 0) transform.localScale = new Vector3(-1, 1, 1);
        else if (input.x < 0) transform.localScale = new Vector3(1, 1, 1);

    float speed = stats ? stats.moveSpd : moveSpeed;
        Vector2 velocity = new Vector2(input.x, input.y) * speed;
        rb.linearVelocity = velocity;
    }
}

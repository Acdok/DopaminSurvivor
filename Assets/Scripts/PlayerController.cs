using UnityEngine;

/// <summary>
/// Moves the Player with keyboard input using only local state, Health, and the optional GameManager.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    [Header("State References")]
    [SerializeField]
    private Health playerHealth;

    [SerializeField]
    private GameManager gameManager;

    private Rigidbody2D body;
    private Vector2 movementInput;

    /// <summary>
    /// Runtime-adjustable movement speed in units per second.
    /// </summary>
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Last normalized keyboard direction read for physics movement.
    /// </summary>
    public Vector2 MovementInput => movementInput;

    /// <summary>
    /// True while movement input and physics movement are allowed.
    /// </summary>
    public bool CanMove => !IsMovementBlocked();

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ConfigureBodyForTopDownMovement();
        ResolveOptionalReferences();
    }

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
        ConfigureBodyForTopDownMovement();
        ResolveOptionalReferences();
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }

    private void Update()
    {
        // Input is intentionally cached here and consumed from FixedUpdate by the physics body.
        if (IsMovementBlocked())
        {
            movementInput = Vector2.zero;
            return;
        }

        movementInput = ReadMovementInput();
    }

    private void FixedUpdate()
    {
        // Stop immediately when player death or game over disables control.
        if (IsMovementBlocked())
        {
            StopMovement();
            return;
        }

        body.linearVelocity = movementInput * moveSpeed;
    }

    private void OnDisable()
    {
        StopMovement();
    }

    private void ResolveOptionalReferences()
    {
        if (playerHealth == null)
        {
            TryGetComponent(out playerHealth);
        }

        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    private void ConfigureBodyForTopDownMovement()
    {
        if (body == null)
        {
            return;
        }

        // Top-view movement should not drift under gravity between physics velocity updates.
        body.gravityScale = 0f;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }

    private bool IsMovementBlocked()
    {
        bool playerIsDead = playerHealth != null && !playerHealth.IsAlive;
        bool gameIsOver = gameManager != null && gameManager.IsGameOver;

        return playerIsDead || gameIsOver;
    }

    private void StopMovement()
    {
        movementInput = Vector2.zero;

        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector2.zero;
    }

    private static Vector2 ReadMovementInput()
    {
        float x = 0f;
        float y = 0f;

        // 프로젝트 규칙에 따라 이동 입력은 Old Input의 KeyCode만 사용한다.
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            x -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            x += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            y -= 1f;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            y += 1f;
        }

        return NormalizeInput(new Vector2(x, y));
    }

    private static Vector2 NormalizeInput(Vector2 input)
    {
        if (input.sqrMagnitude > 1f)
        {
            return input.normalized;
        }

        return input;
    }
}

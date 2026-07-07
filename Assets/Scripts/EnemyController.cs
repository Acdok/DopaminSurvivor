using UnityEngine;

/// <summary>
/// Moves an enemy toward an injected Transform target without depending on player-specific components.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    private const float DefaultWobbleAngle = 8f;
    private const float DefaultWobbleFrequency = 6f;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 2.5f;

    [Header("Visual Wobble")]
    [SerializeField, Min(0f)]
    [Tooltip("이동 중 좌우로 흔들리듯 회전하는 최대 각도다.")]
    private float wobbleAngle = DefaultWobbleAngle;

    [SerializeField, Min(0f)]
    [Tooltip("이동 중 좌우 흔들림이 반복되는 속도다.")]
    private float wobbleFrequency = DefaultWobbleFrequency;

    [Header("State References")]
    [SerializeField]
    private Transform target;

    [SerializeField]
    private Health enemyHealth;

    private Rigidbody2D body;
    private float wobblePhase;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    /// <summary>
    /// Allows spawners or setup code to inject the target after this enemy is created.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        wobblePhase = Random.Range(0f, Mathf.PI * 2f);
        ConfigureBodyForTopDownMovement();
        ResolveOptionalReferences();
    }

    private void OnEnable()
    {
        ResolveOptionalReferences();
        SubscribeToHealth();
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
        wobbleAngle = Mathf.Max(0f, wobbleAngle);
        wobbleFrequency = Mathf.Max(0f, wobbleFrequency);
    }

    private void FixedUpdate()
    {
        // Dead enemies or enemies without a target should stop cleanly and wait.
        if (ShouldStopMovement())
        {
            StopMovement();
            return;
        }

        MoveTowardTarget();
    }

    private void LateUpdate()
    {
        if (ShouldStopMovement() || body.linearVelocity.sqrMagnitude <= Mathf.Epsilon)
        {
            ResetVisualWobble();
            return;
        }

        UpdateVisualWobble();
    }

    private void OnDisable()
    {
        UnsubscribeFromHealth();
        StopMovement();
    }

    private void ResolveOptionalReferences()
    {
        if (enemyHealth == null)
        {
            TryGetComponent(out enemyHealth);
        }
    }

    private void SubscribeToHealth()
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Died -= HandleDied;
        enemyHealth.Died += HandleDied;
    }

    private void UnsubscribeFromHealth()
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Died -= HandleDied;
    }

    private void HandleDied(Health health)
    {
        StopMovement();
        Destroy(gameObject);
    }

    private void ConfigureBodyForTopDownMovement()
    {
        if (body == null)
        {
            return;
        }

        body.gravityScale = 0f;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }

    private bool ShouldStopMovement()
    {
        return body == null || target == null || IsDead();
    }

    private bool IsDead()
    {
        return enemyHealth != null && !enemyHealth.IsAlive;
    }

    private void MoveTowardTarget()
    {
        Vector2 currentPosition = body.position;
        Vector2 targetPosition = target.position;
        Vector2 toTarget = targetPosition - currentPosition;

        if (toTarget.sqrMagnitude <= Mathf.Epsilon)
        {
            StopMovement();
            return;
        }

        // Recalculate direction every physics tick so the enemy follows moving targets.
        body.linearVelocity = toTarget.normalized * moveSpeed;
    }

    private void UpdateVisualWobble()
    {
        if (wobbleAngle <= 0f || wobbleFrequency <= 0f)
        {
            ResetVisualWobble();
            return;
        }

        // 물리 이동은 그대로 두고, 적 스프라이트가 좌우로 비틀리며 다가오는 느낌만 더한다.
        float zRotation = Mathf.Sin((Time.time * wobbleFrequency) + wobblePhase) * wobbleAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    private void ResetVisualWobble()
    {
        transform.rotation = Quaternion.identity;
    }

    private void StopMovement()
    {
        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector2.zero;
    }
}

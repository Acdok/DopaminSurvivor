using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic straight-moving projectile that damages Health targets without depending on enemy controllers.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    private const float DefaultSpeed = 10f;
    private const float DefaultDamage = 10f;
    private const float DefaultLifetime = 3f;
    private const float DefaultHomingTurnSpeed = 240f;
    private const float DefaultHomingRetargetInterval = 0.2f;
    private const float DefaultHomingRadius = 12f;
    private const int DefaultSplitProjectileCount = 2;
    private const int MaximumSplitProjectileCount = 6;
    private const float DefaultSplitSpreadAngle = 70f;
    private const float DefaultSplitDamageMultiplier = 0.45f;
    private const float DefaultSplitSpeedMultiplier = 0.85f;
    private const float DefaultSplitScaleMultiplier = 0.5f;
    private const float DefaultSplitLifetime = 1.2f;
    private const float MinimumLifetime = 0.01f;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float speed = DefaultSpeed;

    [Header("Damage")]
    [SerializeField, Min(0f)]
    private float damage = DefaultDamage;

    [SerializeField, Min(MinimumLifetime)]
    private float lifetime = DefaultLifetime;

    [Header("Piercing")]
    [SerializeField]
    private bool pierceEnemies;

    [SerializeField, Min(1)]
    private int maxPierceHits = 8;

    [Header("Homing")]
    [SerializeField]
    private bool homingEnabled;

    [Tooltip("초당 회전 각도다. 값을 낮출수록 유도 궤적이 더 완만해진다.")]
    [SerializeField, Min(0f)]
    private float homingTurnSpeed = DefaultHomingTurnSpeed;

    [SerializeField, Min(0.01f)]
    private float homingRetargetInterval = DefaultHomingRetargetInterval;

    [SerializeField, Min(0f)]
    private float homingRadius = DefaultHomingRadius;

    [Header("Enemy Detection")]
    [Tooltip("Optional. Leave empty to allow any non-player Health as a target.")]
    [SerializeField]
    private string enemyTag;

    [SerializeField]
    private LayerMask enemyLayers;

    [Header("Player Safety")]
    [SerializeField]
    private string playerTag = "Player";

    [SerializeField]
    private LayerMask playerLayers;

    [Tooltip("Optional owner to ignore, usually the player that fired this projectile.")]
    [SerializeField]
    private GameObject owner;

    private Rigidbody2D body;
    private readonly List<Health> damagedTargets = new List<Health>();
    private readonly List<Health> ignoredTargets = new List<Health>();
    private Vector2 moveDirection = Vector2.right;
    private float remainingLifetime;
    private float homingRetargetTimer;
    private Health homingTarget;
    private int pierceHitCount;
    private bool hasHit;
    private bool splitEnabled;
    private GameObject splitProjectilePrefab;
    private GameObject splitOwner;
    private int splitProjectileCount = DefaultSplitProjectileCount;
    private float splitSpreadAngle = DefaultSplitSpreadAngle;
    private float splitDamageMultiplier = DefaultSplitDamageMultiplier;
    private float splitSpeedMultiplier = DefaultSplitSpeedMultiplier;
    private float splitScaleMultiplier = DefaultSplitScaleMultiplier;
    private float splitLifetime = DefaultSplitLifetime;
    private bool splitHomingEnabled;
    private float splitHomingTurnSpeed = DefaultHomingTurnSpeed;
    private float splitHomingRetargetInterval = DefaultHomingRetargetInterval;
    private float splitHomingRadius = DefaultHomingRadius;
    private int splitGeneration;

    public float Speed
    {
        get => speed;
        set => speed = SanitizeNonNegative(value, DefaultSpeed);
    }

    public float Damage
    {
        get => damage;
        set => damage = SanitizeNonNegative(value, DefaultDamage);
    }

    public float Lifetime
    {
        get => lifetime;
        set => lifetime = SanitizeLifetime(value, DefaultLifetime);
    }

    public Vector2 Direction => moveDirection;

    public GameObject Owner
    {
        get => owner;
        set => owner = value;
    }

    public bool PierceEnemies
    {
        get => pierceEnemies;
        set => pierceEnemies = value;
    }

    public int MaxPierceHits
    {
        get => maxPierceHits;
        set => maxPierceHits = Mathf.Max(1, value);
    }

    /// <summary>
    /// Uses Inspector speed, damage, and lifetime while setting the spawn direction.
    /// </summary>
    public void Initialize(Vector2 direction)
    {
        Configure(direction, speed, damage, lifetime);
    }

    /// <summary>
    /// Uses Inspector lifetime while overriding direction, speed, and damage.
    /// </summary>
    public void Initialize(Vector2 direction, float speed, float damage)
    {
        Configure(direction, speed, damage, lifetime);
    }

    /// <summary>
    /// Fully configures this projectile from spawner or weapon code.
    /// </summary>
    public void Initialize(Vector2 direction, float speed, float damage, float lifetime)
    {
        Configure(direction, speed, damage, lifetime);
    }

    public void ConfigurePiercing(bool shouldPierce, int hitLimit)
    {
        pierceEnemies = shouldPierce;
        maxPierceHits = Mathf.Max(1, hitLimit);
    }

    public void ConfigureHoming(bool enabled, float turnSpeed, float retargetInterval, float radius, Health initialTarget)
    {
        homingEnabled = enabled;
        homingTurnSpeed = SanitizeNonNegative(turnSpeed, DefaultHomingTurnSpeed);
        homingRetargetInterval = Mathf.Max(0.01f, SanitizeNonNegative(retargetInterval, DefaultHomingRetargetInterval));
        homingRadius = SanitizeNonNegative(radius, DefaultHomingRadius);
        homingTarget = initialTarget;
        homingRetargetTimer = 0f;
    }

    public void ConfigureSplitAttack(
        bool enabled,
        GameObject prefab,
        GameObject newOwner,
        int projectileCount,
        float spreadAngle,
        float damageMultiplier,
        float speedMultiplier,
        float newLifetime,
        float scaleMultiplier,
        bool homingEnabled,
        float homingTurnSpeed,
        float homingRetargetInterval,
        float homingRadius,
        int generation)
    {
        splitEnabled = enabled;
        splitProjectilePrefab = prefab;
        splitOwner = newOwner;
        splitProjectileCount = Mathf.Clamp(projectileCount, 1, MaximumSplitProjectileCount);
        splitSpreadAngle = SanitizeNonNegative(spreadAngle, DefaultSplitSpreadAngle);
        splitDamageMultiplier = SanitizeNonNegative(damageMultiplier, DefaultSplitDamageMultiplier);
        splitSpeedMultiplier = SanitizeNonNegative(speedMultiplier, DefaultSplitSpeedMultiplier);
        splitLifetime = SanitizeLifetime(newLifetime, DefaultSplitLifetime);
        splitScaleMultiplier = SanitizePositiveWithFallback(scaleMultiplier, DefaultSplitScaleMultiplier);
        splitHomingEnabled = homingEnabled;
        splitHomingTurnSpeed = SanitizeNonNegative(homingTurnSpeed, DefaultHomingTurnSpeed);
        splitHomingRetargetInterval = Mathf.Max(0.01f, SanitizeNonNegative(homingRetargetInterval, DefaultHomingRetargetInterval));
        splitHomingRadius = SanitizeNonNegative(homingRadius, DefaultHomingRadius);
        splitGeneration = Mathf.Max(0, generation);
    }

    public void IgnoreTarget(Health target)
    {
        if (target != null && !ignoredTargets.Contains(target))
        {
            ignoredTargets.Add(target);
        }
    }

    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
    }

    public void SetOwner(Component newOwner)
    {
        owner = newOwner != null ? newOwner.gameObject : null;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ConfigureBodyForProjectileMovement();
        remainingLifetime = SanitizeLifetime(lifetime, DefaultLifetime);
    }

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
        ConfigureBodyForProjectileMovement();
        moveDirection = NormalizeDirection(transform.right, Vector2.right);
    }

    private void OnEnable()
    {
        hasHit = false;
        damagedTargets.Clear();
        ignoredTargets.Clear();
        pierceHitCount = 0;
        homingTarget = null;
        homingRetargetTimer = 0f;
        remainingLifetime = SanitizeLifetime(lifetime, DefaultLifetime);
        ApplyVelocity();
    }

    private void OnValidate()
    {
        speed = SanitizeNonNegative(speed, DefaultSpeed);
        damage = SanitizeNonNegative(damage, DefaultDamage);
        lifetime = SanitizeLifetime(lifetime, DefaultLifetime);
        maxPierceHits = Mathf.Max(1, maxPierceHits);
        homingTurnSpeed = SanitizeNonNegative(homingTurnSpeed, DefaultHomingTurnSpeed);
        homingRetargetInterval = Mathf.Max(0.01f, SanitizeNonNegative(homingRetargetInterval, DefaultHomingRetargetInterval));
        homingRadius = SanitizeNonNegative(homingRadius, DefaultHomingRadius);
        splitProjectileCount = Mathf.Clamp(splitProjectileCount, 1, MaximumSplitProjectileCount);
        splitSpreadAngle = SanitizeNonNegative(splitSpreadAngle, DefaultSplitSpreadAngle);
        splitDamageMultiplier = SanitizeNonNegative(splitDamageMultiplier, DefaultSplitDamageMultiplier);
        splitSpeedMultiplier = SanitizeNonNegative(splitSpeedMultiplier, DefaultSplitSpeedMultiplier);
        splitLifetime = SanitizeLifetime(splitLifetime, DefaultSplitLifetime);
        splitScaleMultiplier = SanitizePositiveWithFallback(splitScaleMultiplier, DefaultSplitScaleMultiplier);
    }

    private void Update()
    {
        if (hasHit)
        {
            return;
        }

        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            Despawn();
        }
    }

    private void FixedUpdate()
    {
        if (hasHit)
        {
            StopMovement();
            return;
        }

        if (CanUseRigidbody())
        {
            UpdateHomingDirection();
            ApplyVelocity();
            return;
        }

        UpdateHomingDirection();
        // Rigidbody2D가 없거나 비활성화되어도 테스트 중 발사체가 멈추지 않게 이동을 보정한다.
        transform.position += (Vector3)(moveDirection * speed * Time.fixedDeltaTime);
    }

    private void OnDisable()
    {
        StopMovement();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    private void Configure(Vector2 direction, float newSpeed, float newDamage, float newLifetime)
    {
        moveDirection = NormalizeDirection(direction, GetFallbackDirection());
        speed = SanitizeNonNegative(newSpeed, DefaultSpeed);
        damage = SanitizeNonNegative(newDamage, DefaultDamage);
        lifetime = SanitizeLifetime(newLifetime, DefaultLifetime);
        remainingLifetime = lifetime;
        hasHit = false;
        damagedTargets.Clear();
        ignoredTargets.Clear();
        pierceHitCount = 0;
        homingRetargetTimer = 0f;
        ApplyVelocity();
    }

    private void ConfigureBodyForProjectileMovement()
    {
        if (body == null)
        {
            return;
        }

        body.gravityScale = 0f;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }

    private void HandleCollision(Collision2D collision)
    {
        if (collision == null || hasHit)
        {
            return;
        }

        if (TryHit(collision.collider))
        {
            return;
        }

        TryHit(collision.otherCollider);
    }

    private bool TryHit(Collider2D other)
    {
        if (hasHit || !TryGetEnemyHealth(other, out Health targetHealth))
        {
            return false;
        }

        if (damagedTargets.Contains(targetHealth))
        {
            return false;
        }

        // 관통 공격은 같은 적에게 중복 피해를 주지 않고 다음 적을 계속 노린다.
        damagedTargets.Add(targetHealth);
        targetHealth.TakeDamage(damage);
        SpawnSplitProjectiles(targetHealth);

        if (!pierceEnemies)
        {
            Despawn();
            return true;
        }

        pierceHitCount++;

        if (pierceHitCount >= maxPierceHits)
        {
            Despawn();
        }

        return true;
    }

    private void UpdateHomingDirection()
    {
        if (!homingEnabled || homingRadius <= 0f || homingTurnSpeed <= 0f)
        {
            return;
        }

        homingRetargetTimer -= Time.fixedDeltaTime;

        if (!IsValidHomingTarget(homingTarget) || homingRetargetTimer <= 0f)
        {
            homingTarget = FindNearestHomingTarget();
            homingRetargetTimer = homingRetargetInterval;
        }

        if (!IsValidHomingTarget(homingTarget))
        {
            return;
        }

        Vector2 desiredDirection = homingTarget.transform.position - transform.position;

        if (!IsUsableDirection(desiredDirection))
        {
            return;
        }

        float maxRadiansDelta = homingTurnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 rotatedDirection = Vector3.RotateTowards(moveDirection, desiredDirection.normalized, maxRadiansDelta, 0f);
        moveDirection = NormalizeDirection((Vector2)rotatedDirection, moveDirection);
    }

    private Health FindNearestHomingTarget()
    {
        Health[] candidates = FindObjectsByType<Health>(FindObjectsInactive.Exclude);
        Health nearestHealth = null;
        float nearestSqrDistance = homingRadius * homingRadius;
        Vector3 origin = transform.position;

        foreach (Health candidate in candidates)
        {
            if (!IsValidHomingTarget(candidate))
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - origin).sqrMagnitude;

            if (sqrDistance >= nearestSqrDistance)
            {
                continue;
            }

            nearestSqrDistance = sqrDistance;
            nearestHealth = candidate;
        }

        return nearestHealth;
    }

    private bool IsValidHomingTarget(Health candidate)
    {
        if (candidate == null
            || !candidate.isActiveAndEnabled
            || !candidate.IsAlive
            || damagedTargets.Contains(candidate)
            || ignoredTargets.Contains(candidate))
        {
            return false;
        }

        if (owner != null && MatchesObjectOrChild(candidate.gameObject, owner))
        {
            return false;
        }

        if (MatchesMarker(candidate.gameObject, playerTag, playerLayers))
        {
            return false;
        }

        return IsEnemyContact(candidate);
    }

    private bool TryGetEnemyHealth(Collider2D other, out Health targetHealth)
    {
        targetHealth = null;

        if (other == null)
        {
            return false;
        }

        targetHealth = ResolveHealth(other);

        if (targetHealth == null
            || ignoredTargets.Contains(targetHealth)
            || IsSelfContact(other)
            || IsOwnerContact(other, targetHealth))
        {
            return false;
        }

        if (IsPlayerContact(other, targetHealth))
        {
            return false;
        }

        // Enemy filters are optional; Health plus player/owner exclusion is enough for early prefabs.
        return IsEnemyContact(other, targetHealth);
    }

    private Health ResolveHealth(Collider2D other)
    {
        if (other.TryGetComponent(out Health health))
        {
            return health;
        }

        Rigidbody2D attachedBody = other.attachedRigidbody;

        if (attachedBody != null && attachedBody.TryGetComponent(out health))
        {
            return health;
        }

        return other.GetComponentInParent<Health>();
    }

    private void SpawnSplitProjectiles(Health hitTarget)
    {
        if (!splitEnabled || splitProjectilePrefab == null || splitGeneration > 0)
        {
            return;
        }

        Vector3 splitOrigin = hitTarget != null ? hitTarget.transform.position : transform.position;

        for (int i = 0; i < splitProjectileCount; i++)
        {
            Vector2 splitDirection = ResolveSplitDirection(i, splitProjectileCount);
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(splitDirection.y, splitDirection.x) * Mathf.Rad2Deg);
            Vector3 spawnPosition = splitOrigin + (Vector3)(splitDirection * 0.18f);
            GameObject splitObject = Instantiate(splitProjectilePrefab, spawnPosition, rotation);
            ApplySplitScale(splitObject);

            if (!splitObject.TryGetComponent(out Projectile splitProjectile))
            {
                continue;
            }

            // 분열 투사체는 위력과 사거리를 낮추고, 같은 적을 즉시 다시 맞히지 않게 제외한다.
            splitProjectile.SetOwner(splitOwner != null ? splitOwner : owner);
            splitProjectile.Initialize(
                splitDirection,
                speed * splitSpeedMultiplier,
                damage * splitDamageMultiplier,
                splitLifetime);
            splitProjectile.IgnoreTarget(hitTarget);
            splitProjectile.ConfigureHoming(
                splitHomingEnabled,
                splitHomingTurnSpeed,
                splitHomingRetargetInterval,
                splitHomingRadius,
                null);
            splitProjectile.ConfigureSplitAttack(
                false,
                null,
                splitOwner,
                splitProjectileCount,
                splitSpreadAngle,
                splitDamageMultiplier,
                splitSpeedMultiplier,
                splitLifetime,
                splitScaleMultiplier,
                false,
                splitHomingTurnSpeed,
                splitHomingRetargetInterval,
                splitHomingRadius,
                splitGeneration + 1);
        }
    }

    private void ApplySplitScale(GameObject splitObject)
    {
        if (splitObject == null)
        {
            return;
        }

        // 분열 투사체는 현재 날아가던 투사체의 크기를 기준으로 줄여 무기별 외형을 유지한다.
        Vector3 sourceScale = IsUsableScale(transform.localScale) ? transform.localScale : splitObject.transform.localScale;
        splitObject.transform.localScale = sourceScale * splitScaleMultiplier;
    }

    private Vector2 ResolveSplitDirection(int index, int count)
    {
        if (count <= 1)
        {
            return moveDirection;
        }

        float normalizedOffset = index / (float)(count - 1) - 0.5f;
        return RotateDirection(moveDirection, normalizedOffset * splitSpreadAngle);
    }

    private static Vector2 RotateDirection(Vector2 direction, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos).normalized;
    }

    private bool IsSelfContact(Collider2D other)
    {
        return MatchesObjectOrChild(other.gameObject, gameObject)
            || (other.attachedRigidbody != null && MatchesObjectOrChild(other.attachedRigidbody.gameObject, gameObject));
    }

    private bool IsOwnerContact(Collider2D other, Health targetHealth)
    {
        return owner != null
            && (MatchesObjectOrChild(other.gameObject, owner)
                || (other.attachedRigidbody != null && MatchesObjectOrChild(other.attachedRigidbody.gameObject, owner))
                || MatchesObjectOrChild(targetHealth.gameObject, owner));
    }

    private bool IsPlayerContact(Collider2D other, Health targetHealth)
    {
        bool usesTagFilter = !string.IsNullOrEmpty(playerTag);
        bool usesLayerFilter = playerLayers.value != 0;

        if (!usesTagFilter && !usesLayerFilter)
        {
            return false;
        }

        return MatchesMarker(other.gameObject, playerTag, playerLayers)
            || (other.attachedRigidbody != null && MatchesMarker(other.attachedRigidbody.gameObject, playerTag, playerLayers))
            || MatchesMarker(targetHealth.gameObject, playerTag, playerLayers);
    }

    private bool IsEnemyContact(Collider2D other, Health targetHealth)
    {
        bool usesTagFilter = !string.IsNullOrEmpty(enemyTag);
        bool usesLayerFilter = enemyLayers.value != 0;

        if (!usesTagFilter && !usesLayerFilter)
        {
            return true;
        }

        return MatchesMarker(other.gameObject, enemyTag, enemyLayers)
            || (other.attachedRigidbody != null && MatchesMarker(other.attachedRigidbody.gameObject, enemyTag, enemyLayers))
            || MatchesMarker(targetHealth.gameObject, enemyTag, enemyLayers);
    }

    private bool IsEnemyContact(Health targetHealth)
    {
        bool usesTagFilter = !string.IsNullOrEmpty(enemyTag);
        bool usesLayerFilter = enemyLayers.value != 0;

        if (!usesTagFilter && !usesLayerFilter)
        {
            return true;
        }

        return targetHealth != null && MatchesMarker(targetHealth.gameObject, enemyTag, enemyLayers);
    }

    private bool MatchesMarker(GameObject candidate, string tagName, LayerMask layers)
    {
        if (candidate == null)
        {
            return false;
        }

        bool tagMatches = !string.IsNullOrEmpty(tagName) && candidate.tag == tagName;
        bool layerMatches = layers.value != 0 && (layers.value & (1 << candidate.layer)) != 0;

        return tagMatches || layerMatches;
    }

    private bool MatchesObjectOrChild(GameObject candidate, GameObject root)
    {
        return candidate != null
            && root != null
            && (candidate == root || candidate.transform.IsChildOf(root.transform));
    }

    private bool CanUseRigidbody()
    {
        return body != null && body.simulated;
    }

    private void ApplyVelocity()
    {
        if (!CanUseRigidbody())
        {
            return;
        }

        body.linearVelocity = moveDirection * speed;
    }

    private void StopMovement()
    {
        if (!CanUseRigidbody())
        {
            return;
        }

        body.linearVelocity = Vector2.zero;
    }

    private void Despawn()
    {
        hasHit = true;
        StopMovement();
        Destroy(gameObject);
    }

    private Vector2 GetFallbackDirection()
    {
        if (IsUsableDirection(moveDirection))
        {
            return moveDirection;
        }

        return NormalizeDirection(transform.right, Vector2.right);
    }

    private Vector2 NormalizeDirection(Vector2 direction, Vector2 fallback)
    {
        if (!IsUsableDirection(direction))
        {
            return IsUsableDirection(fallback) ? fallback.normalized : Vector2.right;
        }

        return direction.normalized;
    }

    private bool IsUsableDirection(Vector2 direction)
    {
        return IsFinite(direction.x)
            && IsFinite(direction.y)
            && direction.sqrMagnitude > Mathf.Epsilon;
    }

    private static float SanitizeNonNegative(float value, float fallback)
    {
        return IsFinite(value) ? Mathf.Max(0f, value) : fallback;
    }

    private static float SanitizeLifetime(float value, float fallback)
    {
        return IsFinite(value) ? Mathf.Max(MinimumLifetime, value) : fallback;
    }

    private static float SanitizePositiveWithFallback(float value, float fallback)
    {
        return IsFinite(value) && value > 0f ? value : fallback;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsUsableScale(Vector3 scale)
    {
        return IsFinite(scale.x)
            && IsFinite(scale.y)
            && IsFinite(scale.z)
            && scale.sqrMagnitude > Mathf.Epsilon;
    }
}

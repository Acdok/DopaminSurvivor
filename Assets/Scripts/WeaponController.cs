using UnityEngine;

/// <summary>
/// 플레이어의 자동 조준 기본 공격과 선택형 혈사포 레이저를 관리한다.
/// </summary>
[DisallowMultipleComponent]
public class WeaponController : MonoBehaviour
{
    private const float DefaultAttackInterval = 0.35f;
    private const float DefaultBrimstoneAttackInterval = 1.5f;
    private const float DefaultProjectileDamage = 10f;
    private const float DefaultProjectileSpeed = 10f;
    private const float MinimumAttackInterval = 0.01f;
    private const int MinimumAttackCount = 1;
    private const int MaximumAttackCount = 4;
    private const float DefaultMultishotSpreadAngle = 30f;
    private const float DefaultDoubleShotSpacing = 0.35f;
    private const int DefaultSplitProjectileCount = 2;
    private const int MaximumSplitProjectileCount = 6;
    private const float DefaultSplitSpreadAngle = 70f;
    private const float DefaultSplitDamageMultiplier = 0.45f;
    private const float DefaultSplitSpeedMultiplier = 0.85f;
    private const float DefaultSplitScaleMultiplier = 0.5f;
    private const float DefaultSplitProjectileLifetime = 1.2f;
    private const float DefaultLaserLength = 14f;
    private const float DefaultLaserWidth = 0.45f;
    private const float DefaultLaserDuration = 0.9f;
    private const float DefaultDamageMultiplier = 4f;
    private const float DefaultProjectileHomingTurnSpeed = 240f;
    private const float DefaultProjectileHomingRetargetInterval = 0.2f;
    private const float DefaultHomingCurveStrength = 1.2f;
    private const int DefaultHomingCurveSamplesPerSegment = 10;
    private const int MaximumHomingCurveSamplesPerSegment = 24;

    [Header("Attack")]
    [Tooltip("일반 공격의 발사 간격이다. 혈사포 옵션이 켜지면 충전 시간으로 사용한다.")]
    [SerializeField, Min(MinimumAttackInterval)]
    private float attackInterval = DefaultAttackInterval;

    [SerializeField]
    private bool startCharged;

    [SerializeField]
    private Transform firePoint;

    [SerializeField, Range(MinimumAttackCount, MaximumAttackCount)]
    [Tooltip("한 번 공격할 때 생성하는 공격 수다. 2는 평행, 3 이상은 부채꼴로 발사한다.")]
    private int attackCount = MinimumAttackCount;

    [SerializeField, Min(0f)]
    [Tooltip("트리플샷 이상에서 전체 부채꼴이 벌어지는 각도다.")]
    private float multishotSpreadAngle = DefaultMultishotSpreadAngle;

    [SerializeField, Min(0f)]
    [Tooltip("더블샷에서 두 공격 사이를 벌리는 거리다.")]
    private float doubleShotSpacing = DefaultDoubleShotSpacing;

    [SerializeField, Min(MinimumAttackInterval)]
    [Tooltip("혈사포 옵션이 켜졌을 때 사용하는 충전 간격이다.")]
    private float brimstoneAttackInterval = DefaultBrimstoneAttackInterval;

    [SerializeField, Min(0f)]
    private float projectileDamage = DefaultProjectileDamage;

    [Tooltip("일반 투사체 속도다. 혈사포 옵션에서는 레이저 맥동 속도에 사용한다.")]
    [SerializeField, Min(0f)]
    private float projectileSpeed = DefaultProjectileSpeed;

    [SerializeField]
    [Tooltip("끄면 ProjectilePrefab을 사용하는 기존 일반 공격을 발사한다.")]
    private bool useBrimstoneLaser;

    [Header("Projectile Attack")]
    [SerializeField]
    private GameObject projectilePrefab;

    [Header("Homing Attack")]
    [SerializeField]
    [Tooltip("일반 투사체와 혈사포에 공통으로 적용되는 유도 옵션이다.")]
    private bool useHomingAttack;

    [SerializeField, Min(0f)]
    [Tooltip("일반 투사체가 목표 방향으로 초당 회전할 수 있는 각도다.")]
    private float projectileHomingTurnSpeed = DefaultProjectileHomingTurnSpeed;

    [SerializeField, Min(0.01f)]
    [Tooltip("일반 투사체가 새 유도 목표를 다시 찾는 간격이다.")]
    private float projectileHomingRetargetInterval = DefaultProjectileHomingRetargetInterval;

    [SerializeField, Min(0f)]
    private float homingRadius = 14f;

    [Header("Split Attack")]
    [SerializeField]
    [Tooltip("적중 시 현재 무기 형태를 따른 약한 분열 공격을 생성한다.")]
    private bool useSplitAttack;

    [SerializeField, Range(1, MaximumSplitProjectileCount)]
    private int splitProjectileCount = DefaultSplitProjectileCount;

    [SerializeField, Min(0f)]
    [Tooltip("분열 공격들이 벌어지는 전체 각도다.")]
    private float splitSpreadAngle = DefaultSplitSpreadAngle;

    [SerializeField, Min(0f)]
    private float splitDamageMultiplier = DefaultSplitDamageMultiplier;

    [SerializeField, Min(0f)]
    private float splitSpeedMultiplier = DefaultSplitSpeedMultiplier;

    [SerializeField, Range(0.01f, 1f)]
    [Tooltip("분열로 생성되는 공격의 시각/충돌 크기 배율이다.")]
    private float splitScaleMultiplier = DefaultSplitScaleMultiplier;

    [SerializeField, Min(0.01f)]
    [Tooltip("분열 공격의 수명이다. 짧을수록 사거리가 줄어든다.")]
    private float splitProjectileLifetime = DefaultSplitProjectileLifetime;

    [Header("Brimstone Laser")]
    [SerializeField, Min(0.1f)]
    private float laserLength = DefaultLaserLength;

    [SerializeField, Min(0.01f)]
    private float laserWidth = DefaultLaserWidth;

    [SerializeField, Min(0.01f)]
    private float laserDuration = DefaultLaserDuration;

    [SerializeField, Min(0f)]
    private float laserDamageMultiplier = DefaultDamageMultiplier;

    [SerializeField]
    private Color laserColor = new Color(1f, 0.04f, 0.02f, 0.95f);

    [Header("Homing Synergy")]
    [SerializeField]
    [Tooltip("혈사포 전용 세트 시너지 유도 옵션이다. 범용 유도와 별도로 관리한다.")]
    private bool useHomingLaser;

    [SerializeField, Min(1)]
    private int homingTargetLimit = 8;

    [SerializeField, Min(0f)]
    [Tooltip("유도 타겟 사이의 방향 변화를 부드럽게 이어 주는 곡선 핸들 길이다. 낮을수록 직선에 가깝다.")]
    private float homingCurveStrength = DefaultHomingCurveStrength;

    [SerializeField, Range(2, MaximumHomingCurveSamplesPerSegment)]
    private int homingCurveSamplesPerSegment = DefaultHomingCurveSamplesPerSegment;

    [Header("Feedback")]
    [SerializeField]
    private CameraFollow cameraFollow;

    [SerializeField, Min(0f)]
    private float screenShakeDuration = 0.08f;

    [SerializeField, Min(0f)]
    private float screenShakeStrength = 0.08f;

    [Header("State References")]
    [SerializeField]
    private AutoTargeting autoTargeting;

    [SerializeField]
    private Health playerHealth;

    [Tooltip("없어도 동작한다. 비어 있으면 플레이어 Health만 확인한다.")]
    [SerializeField]
    private GameManager gameManager;

    private float chargeElapsed;
    private bool warnedMissingProjectilePrefab;

    public float AttackInterval
    {
        get => attackInterval;
        set => attackInterval = SanitizeAttackInterval(value);
    }

    public float BrimstoneAttackInterval
    {
        get => brimstoneAttackInterval;
        set => brimstoneAttackInterval = SanitizeAttackInterval(value);
    }

    public GameObject ProjectilePrefab
    {
        get => projectilePrefab;
        set => projectilePrefab = value;
    }

    public Transform FirePoint
    {
        get => firePoint;
        set => firePoint = value;
    }

    public int AttackCount
    {
        get => attackCount;
        set => attackCount = Mathf.Clamp(value, MinimumAttackCount, MaximumAttackCount);
    }

    public float ProjectileDamage
    {
        get => projectileDamage;
        set => projectileDamage = SanitizeNonNegative(value, DefaultProjectileDamage);
    }

    public float ProjectileSpeed
    {
        get => projectileSpeed;
        set => projectileSpeed = SanitizeNonNegative(value, DefaultProjectileSpeed);
    }

    public AutoTargeting Targeting
    {
        get => autoTargeting;
        set => autoTargeting = value;
    }

    public bool UseHomingLaser
    {
        get => useHomingLaser;
        set => useHomingLaser = value;
    }

    public bool UseHomingAttack
    {
        get => useHomingAttack;
        set => useHomingAttack = value;
    }

    public bool UseBrimstoneLaser
    {
        get => useBrimstoneLaser;
        set => useBrimstoneLaser = value;
    }

    public bool UseSplitAttack
    {
        get => useSplitAttack;
        set => useSplitAttack = value;
    }

    public float ChargeProgress01
    {
        get
        {
            float activeAttackInterval = ResolveActiveAttackInterval();
            return activeAttackInterval <= 0f ? 1f : Mathf.Clamp01(chargeElapsed / activeAttackInterval);
        }
    }

    public float LaserDamage => projectileDamage * laserDamageMultiplier;
    public bool CanAttack => !IsAttackBlocked();

    private void Awake()
    {
        ResolveOptionalReferences();
        chargeElapsed = startCharged ? ResolveActiveAttackInterval() : 0f;
    }

    private void Reset()
    {
        ResolveOptionalReferences();
    }

    private void OnValidate()
    {
        attackInterval = SanitizeAttackInterval(attackInterval);
        brimstoneAttackInterval = SanitizeAttackInterval(brimstoneAttackInterval);
        attackCount = Mathf.Clamp(attackCount, MinimumAttackCount, MaximumAttackCount);
        multishotSpreadAngle = SanitizeNonNegative(multishotSpreadAngle, DefaultMultishotSpreadAngle);
        doubleShotSpacing = SanitizeNonNegative(doubleShotSpacing, DefaultDoubleShotSpacing);
        projectileDamage = SanitizeNonNegative(projectileDamage, DefaultProjectileDamage);
        projectileSpeed = SanitizeNonNegative(projectileSpeed, DefaultProjectileSpeed);
        projectileHomingTurnSpeed = SanitizeNonNegative(projectileHomingTurnSpeed, DefaultProjectileHomingTurnSpeed);
        projectileHomingRetargetInterval = Mathf.Max(0.01f, SanitizeNonNegative(projectileHomingRetargetInterval, DefaultProjectileHomingRetargetInterval));
        splitProjectileCount = Mathf.Clamp(splitProjectileCount, 1, MaximumSplitProjectileCount);
        splitSpreadAngle = SanitizeNonNegative(splitSpreadAngle, DefaultSplitSpreadAngle);
        splitDamageMultiplier = SanitizeNonNegative(splitDamageMultiplier, DefaultSplitDamageMultiplier);
        splitSpeedMultiplier = SanitizeNonNegative(splitSpeedMultiplier, DefaultSplitSpeedMultiplier);
        splitScaleMultiplier = Mathf.Max(0.01f, SanitizeNonNegative(splitScaleMultiplier, DefaultSplitScaleMultiplier));
        splitProjectileLifetime = Mathf.Max(0.01f, SanitizeNonNegative(splitProjectileLifetime, DefaultSplitProjectileLifetime));
        laserLength = Mathf.Max(0.1f, SanitizeNonNegative(laserLength, DefaultLaserLength));
        laserWidth = Mathf.Max(0.01f, SanitizeNonNegative(laserWidth, DefaultLaserWidth));
        laserDuration = Mathf.Max(0.01f, SanitizeNonNegative(laserDuration, DefaultLaserDuration));
        laserDamageMultiplier = SanitizeNonNegative(laserDamageMultiplier, DefaultDamageMultiplier);
        homingTargetLimit = Mathf.Max(1, homingTargetLimit);
        homingRadius = SanitizeNonNegative(homingRadius, 14f);
        homingCurveStrength = SanitizeNonNegative(homingCurveStrength, DefaultHomingCurveStrength);
        homingCurveSamplesPerSegment = Mathf.Clamp(
            homingCurveSamplesPerSegment,
            2,
            MaximumHomingCurveSamplesPerSegment);
        screenShakeDuration = SanitizeNonNegative(screenShakeDuration, 0.08f);
        screenShakeStrength = SanitizeNonNegative(screenShakeStrength, 0.08f);
    }

    private void Update()
    {
        if (IsAttackBlocked())
        {
            return;
        }

        TickCharge();

        if (!IsChargeReady())
        {
            return;
        }

        if (!TryGetCurrentTarget(out Transform target, out Health targetHealth))
        {
            return;
        }

        if (FireAt(target, targetHealth))
        {
            RestartCharge();
        }
    }

    private void ResolveOptionalReferences()
    {
        if (autoTargeting == null)
        {
            TryGetComponent(out autoTargeting);
        }

        if (playerHealth == null)
        {
            TryGetComponent(out playerHealth);
        }

        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        if (cameraFollow == null)
        {
            cameraFollow = FindAnyObjectByType<CameraFollow>();
        }
    }

    private void TickCharge()
    {
        float activeAttackInterval = ResolveActiveAttackInterval();

        // 타겟이 없는 동안에도 충전은 완료 상태로 대기해 다음 적에게 즉시 반응한다.
        if (chargeElapsed >= activeAttackInterval)
        {
            return;
        }

        chargeElapsed = Mathf.Min(activeAttackInterval, chargeElapsed + Time.deltaTime);
    }

    private bool IsChargeReady()
    {
        return chargeElapsed >= ResolveActiveAttackInterval();
    }

    private void RestartCharge()
    {
        chargeElapsed = 0f;
    }

    private bool TryGetCurrentTarget(out Transform target, out Health targetHealth)
    {
        target = null;
        targetHealth = null;

        return autoTargeting != null
            && autoTargeting.TryGetNearestTarget(out target, out targetHealth)
            && target != null;
    }

    private bool FireAt(Transform target, Health targetHealth)
    {
        Transform origin = ResolveFirePoint();
        Vector2 baseDirection = target.position - origin.position;
        baseDirection = NormalizeDirection(baseDirection, origin.right);

        if (!useBrimstoneLaser && projectilePrefab == null)
        {
            WarnMissingProjectilePrefabOnce();
            return false;
        }

        int resolvedAttackCount = Mathf.Clamp(attackCount, MinimumAttackCount, MaximumAttackCount);

        for (int i = 0; i < resolvedAttackCount; i++)
        {
            Vector2 shotDirection = ResolveShotDirection(baseDirection, i, resolvedAttackCount);
            Vector3 shotOrigin = ResolveShotOrigin(origin.position, baseDirection, i, resolvedAttackCount);

            if (useBrimstoneLaser)
            {
                FireBrimstoneLaser(origin, shotOrigin - origin.position, shotDirection, targetHealth);
            }
            else
            {
                FireProjectile(shotOrigin, shotDirection, targetHealth);
            }
        }

        TriggerFeedback();
        return true;
    }

    private void FireProjectile(Vector3 originPosition, Vector2 direction, Health initialTarget)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        GameObject projectileObject = Instantiate(projectilePrefab, originPosition, rotation);

        if (projectileObject.TryGetComponent(out Projectile projectile))
        {
            // 일반 공격은 프리팹의 충돌 설정을 유지하고, 공격력/속도만 현재 무기 수치로 덮어쓴다.
            projectile.SetOwner(gameObject);
            projectile.Initialize(direction, projectileSpeed, projectileDamage);
            projectile.ConfigureHoming(
                useHomingAttack,
                projectileHomingTurnSpeed,
                projectileHomingRetargetInterval,
                homingRadius,
                initialTarget);
            projectile.ConfigureSplitAttack(
                useSplitAttack,
                projectilePrefab,
                gameObject,
                splitProjectileCount,
                splitSpreadAngle,
                splitDamageMultiplier,
                splitSpeedMultiplier,
                splitProjectileLifetime,
                splitScaleMultiplier,
                useHomingAttack,
                projectileHomingTurnSpeed,
                projectileHomingRetargetInterval,
                homingRadius,
                0);
        }
    }

    private void FireBrimstoneLaser(Transform origin, Vector3 originOffset, Vector2 direction, Health targetHealth)
    {
        GameObject laserObject = new GameObject("JIN_BrimstoneLaser");
        JIN_BrimstoneLaser laser = laserObject.AddComponent<JIN_BrimstoneLaser>();

        // 혈사포 판정과 연출은 별도 컴포넌트로 넘겨 이후 시너지 확장을 분리한다.
        laser.Initialize(new JIN_BrimstoneLaser.Configuration
        {
            Owner = gameObject,
            Origin = origin,
            OriginOffset = originOffset,
            InitialTarget = targetHealth,
            Direction = direction,
            DamagePerSecond = LaserDamage,
            Length = laserLength,
            Width = laserWidth,
            Duration = laserDuration,
            LaserColor = laserColor,
            EnemyTag = autoTargeting != null ? autoTargeting.EnemyTag : string.Empty,
            EnemyLayers = autoTargeting != null ? autoTargeting.EnemyLayers : default,
            UseHoming = useHomingAttack || useHomingLaser,
            HomingRadius = homingRadius,
            HomingTargetLimit = homingTargetLimit,
            HomingCurveStrength = homingCurveStrength,
            CurveSamplesPerSegment = homingCurveSamplesPerSegment,
            PulseSpeed = projectileSpeed,
            UseSplitAttack = useSplitAttack,
            SplitProjectileCount = splitProjectileCount,
            SplitSpreadAngle = splitSpreadAngle,
            SplitDamage = projectileDamage * splitDamageMultiplier,
            SplitLength = laserLength * splitSpeedMultiplier,
            SplitLifetime = splitProjectileLifetime,
            SplitUseHoming = useHomingAttack,
            SplitHomingTurnSpeed = projectileHomingTurnSpeed,
            SplitHomingRetargetInterval = projectileHomingRetargetInterval,
            SplitHomingRadius = homingRadius,
            SplitScaleMultiplier = splitScaleMultiplier
        });
    }

    private Transform ResolveFirePoint()
    {
        return firePoint != null ? firePoint : transform;
    }

    private float ResolveActiveAttackInterval()
    {
        return useBrimstoneLaser ? brimstoneAttackInterval : attackInterval;
    }

    private Vector2 ResolveShotDirection(Vector2 baseDirection, int shotIndex, int resolvedAttackCount)
    {
        if (resolvedAttackCount <= 1 || resolvedAttackCount == 2)
        {
            return baseDirection;
        }

        float normalizedOffset = shotIndex / (float)(resolvedAttackCount - 1) - 0.5f;
        return RotateDirection(baseDirection, normalizedOffset * multishotSpreadAngle);
    }

    private Vector3 ResolveShotOrigin(Vector3 originPosition, Vector2 baseDirection, int shotIndex, int resolvedAttackCount)
    {
        if (resolvedAttackCount != 2 || doubleShotSpacing <= 0f)
        {
            return originPosition;
        }

        Vector2 perpendicular = new Vector2(-baseDirection.y, baseDirection.x).normalized;
        float offset = shotIndex == 0 ? -0.5f : 0.5f;
        return originPosition + (Vector3)(perpendicular * doubleShotSpacing * offset);
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

    private void TriggerFeedback()
    {
        if (cameraFollow != null)
        {
            cameraFollow.Shake(screenShakeDuration, screenShakeStrength);
        }
    }

    private void WarnMissingProjectilePrefabOnce()
    {
        if (warnedMissingProjectilePrefab)
        {
            return;
        }

        warnedMissingProjectilePrefab = true;
        Debug.LogWarning($"{nameof(WeaponController)} on {name} cannot fire normal attack because ProjectilePrefab is missing.", this);
    }

    private bool IsAttackBlocked()
    {
        bool playerIsDead = playerHealth != null && !playerHealth.IsAlive;
        bool gameIsOver = gameManager != null && gameManager.IsGameOver;

        return playerIsDead || gameIsOver;
    }

    private static float SanitizeAttackInterval(float value)
    {
        return IsFinite(value) ? Mathf.Max(MinimumAttackInterval, value) : DefaultAttackInterval;
    }

    private static float SanitizeNonNegative(float value, float fallback)
    {
        return IsFinite(value) ? Mathf.Max(0f, value) : fallback;
    }

    private static Vector2 NormalizeDirection(Vector2 direction, Vector2 fallback)
    {
        if (!IsUsableDirection(direction))
        {
            return IsUsableDirection(fallback) ? fallback.normalized : Vector2.right;
        }

        return direction.normalized;
    }

    private static bool IsUsableDirection(Vector2 direction)
    {
        return IsFinite(direction.x)
            && IsFinite(direction.y)
            && direction.sqrMagnitude > Mathf.Epsilon;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

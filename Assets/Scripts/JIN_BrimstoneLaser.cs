using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 혈사포 레이저의 관통 판정과 프로토타입 렌더링을 담당한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public class JIN_BrimstoneLaser : MonoBehaviour
{
    public struct Configuration
    {
        public GameObject Owner;
        public Transform Origin;
        public Vector3 OriginOffset;
        public Health InitialTarget;
        public Vector2 Direction;
        public float DamagePerSecond;
        public float Length;
        public float Width;
        public float Duration;
        public Color LaserColor;
        public string EnemyTag;
        public LayerMask EnemyLayers;
        public bool UseHoming;
        public float HomingRadius;
        public int HomingTargetLimit;
        public float HomingCurveStrength;
        public int CurveSamplesPerSegment;
        public float PulseSpeed;
        public bool UseSplitAttack;
        public int SplitProjectileCount;
        public float SplitSpreadAngle;
        public float SplitDamage;
        public float SplitLength;
        public float SplitLifetime;
        public bool SplitUseHoming;
        public float SplitHomingTurnSpeed;
        public float SplitHomingRetargetInterval;
        public float SplitHomingRadius;
        public float SplitScaleMultiplier;
        public Health IgnoredTarget;
    }

    private const float MinimumLength = 0.1f;
    private const float MinimumWidth = 0.01f;
    private const float MinimumDuration = 0.01f;
    private const float MinimumHitRadius = 0.05f;
    private const int MinimumCurveSamplesPerSegment = 2;
    private const int MaximumCurveSamplesPerSegment = 24;
    private const float MaximumCurveHandleRatio = 0.32f;
    private const float ForwardConeDotThreshold = 0f;
    private const float ForwardTailReserveRatio = 0.2f;
    private const int DefaultSplitProjectileCount = 2;
    private const int MaximumSplitProjectileCount = 6;
    private const float DefaultSplitSpreadAngle = 70f;
    private const float DefaultSplitLifetime = 1.2f;
    private const float DefaultSplitHomingTurnSpeed = 240f;
    private const float DefaultSplitHomingRetargetInterval = 0.2f;
    private const float DefaultSplitHomingRadius = 12f;
    private const float DefaultSplitScaleMultiplier = 0.5f;

    private static Material sharedLaserMaterial;

    private readonly List<Health> homingTargets = new List<Health>();
    private readonly List<Vector3> anchors = new List<Vector3>();
    private readonly List<Vector3> renderPoints = new List<Vector3>();
    private readonly List<Health> splitSpawnedTargets = new List<Health>();

    private LineRenderer lineRenderer;
    private GameObject owner;
    private Transform origin;
    private Vector3 originOffset;
    private Scene ownerScene;
    private Vector2 direction = Vector2.right;
    private float damagePerSecond;
    private float length;
    private float width;
    private float duration;
    private Color laserColor;
    private string enemyTag;
    private LayerMask enemyLayers;
    private bool useHoming;
    private float homingRadius;
    private int homingTargetLimit;
    private float homingCurveStrength;
    private int curveSamplesPerSegment;
    private float pulseSpeed;
    private bool useSplitAttack;
    private int splitProjectileCount;
    private float splitSpreadAngle;
    private float splitDamage;
    private float splitLength;
    private float splitLifetime;
    private bool splitUseHoming;
    private float splitHomingTurnSpeed;
    private float splitHomingRetargetInterval;
    private float splitHomingRadius;
    private float splitScaleMultiplier;
    private Health ignoredTarget;
    private float elapsed;

    public void Initialize(Configuration configuration)
    {
        owner = configuration.Owner;
        origin = configuration.Origin;
        originOffset = configuration.OriginOffset;
        ownerScene = owner != null ? owner.scene : gameObject.scene;
        direction = NormalizeDirection(configuration.Direction, ResolveOriginRight());
        damagePerSecond = SanitizeNonNegative(configuration.DamagePerSecond);
        length = Mathf.Max(MinimumLength, SanitizeNonNegative(configuration.Length));
        width = Mathf.Max(MinimumWidth, SanitizeNonNegative(configuration.Width));
        duration = Mathf.Max(MinimumDuration, SanitizeNonNegative(configuration.Duration));
        laserColor = SanitizeColor(configuration.LaserColor);
        enemyTag = NormalizeTag(configuration.EnemyTag);
        enemyLayers = configuration.EnemyLayers;
        useHoming = configuration.UseHoming;
        homingRadius = SanitizeNonNegative(configuration.HomingRadius);
        homingTargetLimit = Mathf.Max(1, configuration.HomingTargetLimit);
        homingCurveStrength = SanitizeNonNegative(configuration.HomingCurveStrength);
        curveSamplesPerSegment = Mathf.Clamp(
            configuration.CurveSamplesPerSegment,
            MinimumCurveSamplesPerSegment,
            MaximumCurveSamplesPerSegment);
        pulseSpeed = SanitizeNonNegative(configuration.PulseSpeed);
        useSplitAttack = configuration.UseSplitAttack;
        splitProjectileCount = Mathf.Clamp(configuration.SplitProjectileCount, 1, MaximumSplitProjectileCount);
        splitSpreadAngle = SanitizeNonNegativeWithFallback(configuration.SplitSpreadAngle, DefaultSplitSpreadAngle);
        splitDamage = SanitizeNonNegative(configuration.SplitDamage);
        splitLength = SanitizeNonNegative(configuration.SplitLength);
        splitLifetime = Mathf.Max(MinimumDuration, SanitizePositive(configuration.SplitLifetime, DefaultSplitLifetime));
        splitUseHoming = configuration.SplitUseHoming;
        splitHomingTurnSpeed = SanitizeNonNegativeWithFallback(configuration.SplitHomingTurnSpeed, DefaultSplitHomingTurnSpeed);
        splitHomingRetargetInterval = Mathf.Max(0.01f, SanitizePositive(configuration.SplitHomingRetargetInterval, DefaultSplitHomingRetargetInterval));
        splitHomingRadius = SanitizeNonNegativeWithFallback(configuration.SplitHomingRadius, DefaultSplitHomingRadius);
        splitScaleMultiplier = SanitizePositiveWithFallback(configuration.SplitScaleMultiplier, DefaultSplitScaleMultiplier);
        ignoredTarget = configuration.IgnoredTarget;
        splitSpawnedTargets.Clear();
        elapsed = 0f;

        EnsureLineRenderer();
        ConfigureLineRenderer();

        if (useHoming)
        {
            SelectHomingTargets(configuration.InitialTarget);
        }

        BuildPath();
        UpdateVisual(0f);
    }

    private void Awake()
    {
        EnsureLineRenderer();
    }

    private void Update()
    {
        float frameDelta = Mathf.Min(Time.deltaTime, Mathf.Max(0f, duration - elapsed));
        elapsed += frameDelta;

        if (frameDelta <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float life01 = Mathf.Clamp01(elapsed / duration);

        BuildPath();
        ApplyDamageAlongPath(frameDelta);
        UpdateVisual(life01);

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer != null)
        {
            return;
        }

        lineRenderer = GetComponent<LineRenderer>();
    }

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 6;
        lineRenderer.numCornerVertices = 8;
        lineRenderer.sortingOrder = 20;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.sharedMaterial = ResolveLaserMaterial();
    }

    private void SelectHomingTargets(Health initialTarget)
    {
        homingTargets.Clear();

        Vector3 currentPoint = ResolveOriginPosition();
        float remainingLength = ResolveHomingPathLengthLimit();

        TryAppendHomingTarget(initialTarget, ref currentPoint, ref remainingLength, true);

        while (homingTargets.Count < homingTargetLimit)
        {
            Health nextTarget = FindNextHomingTarget(currentPoint, remainingLength);

            if (nextTarget == null)
            {
                return;
            }

            if (!TryAppendHomingTarget(nextTarget, ref currentPoint, ref remainingLength, false))
            {
                return;
            }
        }
    }

    private bool TryAppendHomingTarget(
        Health target,
        ref Vector3 currentPoint,
        ref float remainingLength,
        bool allowOriginalAttackRangeTarget)
    {
        if (!IsValidDamageTarget(target) || homingTargets.Contains(target))
        {
            return false;
        }

        if (!allowOriginalAttackRangeTarget && IsInsideOriginalStraightAttackRange(target.transform.position))
        {
            return false;
        }

        float distance = Vector3.Distance(currentPoint, target.transform.position);
        float stepRange = ResolveHomingStepRange(remainingLength);

        if (distance > stepRange || !IsForwardHomingStep(currentPoint, target.transform.position))
        {
            return false;
        }

        homingTargets.Add(target);
        currentPoint = target.transform.position;
        remainingLength = Mathf.Max(0f, remainingLength - distance);
        return true;
    }

    private Health FindNextHomingTarget(Vector3 currentPoint, float remainingLength)
    {
        float stepRange = ResolveHomingStepRange(remainingLength);

        if (stepRange <= 0f)
        {
            return null;
        }

        Health[] candidates = FindObjectsByType<Health>(FindObjectsInactive.Exclude);
        Health bestTarget = null;
        int bestHitCount = -1;
        float bestSqrDistance = stepRange * stepRange;

        foreach (Health candidate in candidates)
        {
            if (!IsValidDamageTarget(candidate) || homingTargets.Contains(candidate))
            {
                continue;
            }

            if (!IsForwardHomingStep(currentPoint, candidate.transform.position))
            {
                continue;
            }

            if (IsInsideOriginalStraightAttackRange(candidate.transform.position))
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - currentPoint).sqrMagnitude;

            if (sqrDistance > stepRange * stepRange)
            {
                continue;
            }

            int hitCount = CountPredictedHits(currentPoint, candidate.transform.position, candidates);

            if (hitCount < bestHitCount)
            {
                continue;
            }

            if (hitCount == bestHitCount && sqrDistance >= bestSqrDistance)
            {
                continue;
            }

            bestHitCount = hitCount;
            bestSqrDistance = sqrDistance;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    private int CountPredictedHits(
        Vector3 currentPoint,
        Vector3 candidatePoint,
        Health[] candidates)
    {
        Vector3 straightEndPoint = ResolveStraightEndPoint();
        int hitCount = 0;

        foreach (Health target in candidates)
        {
            if (!IsCountablePredictedTarget(target))
            {
                continue;
            }

            Vector3 targetPosition = target.transform.position;

            if (IsInsideLaserSegment(targetPosition, currentPoint, candidatePoint)
                || IsInsideLaserSegment(targetPosition, candidatePoint, straightEndPoint))
            {
                hitCount++;
            }
        }

        return hitCount;
    }

    private bool IsCountablePredictedTarget(Health target)
    {
        if (!IsValidDamageTarget(target) || homingTargets.Contains(target))
        {
            return false;
        }

        return !IsInsideOriginalStraightAttackRange(target.transform.position);
    }

    private float ResolveHomingStepRange(float remainingLength)
    {
        if (remainingLength <= 0f)
        {
            return 0f;
        }

        if (homingRadius <= 0f)
        {
            return remainingLength;
        }

        return Mathf.Min(homingRadius, remainingLength);
    }

    private void BuildPath()
    {
        renderPoints.Clear();
        anchors.Clear();

        Vector3 originPosition = ResolveOriginPosition();
        anchors.Add(originPosition);

        if (useHoming)
        {
            AppendLiveHomingAnchors();
        }

        if (anchors.Count <= 1)
        {
            renderPoints.Add(originPosition);
            renderPoints.Add(ResolveStraightEndPoint());
            return;
        }

        AppendForwardExtension();
        BuildCurvedRenderPoints();
    }

    private void AppendLiveHomingAnchors()
    {
        Vector3 currentPoint = anchors[0];
        float remainingLength = ResolveHomingPathLengthLimit();

        for (int i = 0; i < homingTargets.Count; i++)
        {
            Health target = homingTargets[i];

            if (!IsValidDamageTarget(target))
            {
                continue;
            }

            Vector3 targetPosition = target.transform.position;
            float distance = Vector3.Distance(currentPoint, targetPosition);
            float stepRange = ResolveHomingStepRange(remainingLength);

            if ((i > 0 && IsInsideOriginalStraightAttackRange(targetPosition))
                || distance > stepRange
                || !IsForwardHomingStep(currentPoint, targetPosition))
            {
                continue;
            }

            anchors.Add(targetPosition);
            currentPoint = targetPosition;
            remainingLength = Mathf.Max(0f, remainingLength - distance);
        }
    }

    private void AppendForwardExtension()
    {
        Vector3 straightEndPoint = ResolveStraightEndPoint();
        Vector3 finalSegment = straightEndPoint - anchors[anchors.Count - 1];

        if (!IsUsableDirection(finalSegment))
        {
            return;
        }

        // 유도 후에도 레이저 끝점은 일반 혈사포의 끝점과 정확히 같게 유지한다.
        anchors.Add(straightEndPoint);
    }

    private void BuildCurvedRenderPoints()
    {
        renderPoints.Add(anchors[0]);

        for (int i = 0; i < anchors.Count - 1; i++)
        {
            Vector3 from = anchors[i];
            Vector3 to = anchors[i + 1];

            if (!IsUsableDirection(to - from))
            {
                continue;
            }

            CalculateBezierControls(i, from, to, out Vector3 startControl, out Vector3 endControl);

            for (int sample = 1; sample <= curveSamplesPerSegment; sample++)
            {
                float t = sample / (float)curveSamplesPerSegment;
                renderPoints.Add(CubicBezier(from, startControl, endControl, to, t));
            }
        }
    }

    private void CalculateBezierControls(
        int fromIndex,
        Vector3 from,
        Vector3 to,
        out Vector3 startControl,
        out Vector3 endControl)
    {
        float handleLength = CalculateCurveHandleLength(from, to);

        if (handleLength <= Mathf.Epsilon)
        {
            startControl = from;
            endControl = to;
            return;
        }

        Vector3 startTangent = ResolveAnchorTangent(fromIndex);
        Vector3 endTangent = ResolveAnchorTangent(fromIndex + 1);

        // 인접 구간의 진행 방향을 공유해 타겟 지점에서 갑자기 꺾이지 않는 유도 곡선을 만든다.
        startControl = from + startTangent * handleLength;
        endControl = to - endTangent * handleLength;
    }

    private float CalculateCurveHandleLength(Vector3 from, Vector3 to)
    {
        Vector3 segment = to - from;
        float segmentLength = segment.magnitude;
        float forwardDistance = Vector3.Dot(segment, ResolveForwardVector());

        if (homingCurveStrength <= 0f || segmentLength <= Mathf.Epsilon || forwardDistance <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Mathf.Min(
            homingCurveStrength,
            Mathf.Min(segmentLength * MaximumCurveHandleRatio, forwardDistance * MaximumCurveHandleRatio));
    }

    private Vector3 ResolveAnchorTangent(int anchorIndex)
    {
        Vector3 previousDirection = Vector3.zero;
        Vector3 nextDirection = Vector3.zero;
        bool hasPrevious = anchorIndex > 0
            && TryNormalizeDirection(anchors[anchorIndex] - anchors[anchorIndex - 1], out previousDirection);
        bool hasNext = anchorIndex < anchors.Count - 1
            && TryNormalizeDirection(anchors[anchorIndex + 1] - anchors[anchorIndex], out nextDirection);

        if (hasPrevious && hasNext)
        {
            Vector3 blendedDirection = previousDirection + nextDirection;

            if (TryNormalizeDirection(blendedDirection, out Vector3 tangent))
            {
                return tangent;
            }

            return nextDirection;
        }

        if (hasNext)
        {
            return nextDirection;
        }

        if (hasPrevious)
        {
            return previousDirection;
        }

        return new Vector3(direction.x, direction.y, 0f);
    }

    private float ResolveHomingPathLengthLimit()
    {
        return Mathf.Max(0f, length - ResolveForwardTailReserveLength());
    }

    private float ResolveForwardTailReserveLength()
    {
        return Mathf.Min(length * ForwardTailReserveRatio, Mathf.Max(0f, length - MinimumLength));
    }

    private bool IsForwardHomingStep(Vector3 from, Vector3 to)
    {
        Vector3 forward = ResolveForwardVector();

        // 원점 기준과 현재 타겟 기준을 모두 검사해 뒤쪽으로 꺾이는 유도 연결을 막는다.
        return IsInsideForwardCone(to - ResolveOriginPosition(), forward)
            && IsInsideForwardCone(to - from, forward);
    }

    private bool IsInsideForwardCone(Vector3 offset, Vector3 forward)
    {
        if (!TryNormalizeDirection(offset, out Vector3 normalizedOffset))
        {
            return false;
        }

        return Vector3.Dot(normalizedOffset, forward) > ForwardConeDotThreshold;
    }

    private bool IsInsideOriginalStraightAttackRange(Vector3 point)
    {
        Vector2 from = ResolveOriginPosition();
        Vector2 to = ResolveStraightEndPoint();

        // 직선 혈사포만으로 맞는 추가 대상은 유도 경유점으로 삼지 않아 불필요한 꺾임을 줄인다.
        return IsInsideLaserSegment(point, from, to);
    }

    private bool IsInsideLaserSegment(Vector3 point, Vector3 from, Vector3 to)
    {
        float hitRadius = Mathf.Max(MinimumHitRadius, width * 0.5f);
        return DistanceToSegmentSqr(point, from, to) <= hitRadius * hitRadius;
    }

    private Vector3 ResolveForwardVector()
    {
        return new Vector3(direction.x, direction.y, 0f).normalized;
    }

    private Vector3 ResolveStraightEndPoint()
    {
        return ResolveOriginPosition() + (Vector3)(direction * length);
    }

    private void ApplyDamageAlongPath(float deltaTime)
    {
        if (damagePerSecond <= 0f || deltaTime <= 0f || renderPoints.Count < 2)
        {
            return;
        }

        Health[] candidates = FindObjectsByType<Health>(FindObjectsInactive.Exclude);

        foreach (Health candidate in candidates)
        {
            if (!IsValidDamageTarget(candidate))
            {
                continue;
            }

            if (!IsPointOnLaserPath(candidate.transform.position))
            {
                continue;
            }

            // 레이저 위에 머문 시간만큼 프레임 독립적인 지속 피해를 준다.
            candidate.TakeDamage(damagePerSecond * deltaTime);
            SpawnSplitAttacks(candidate);
        }
    }

    private void SpawnSplitAttacks(Health hitTarget)
    {
        if (!useSplitAttack
            || hitTarget == null
            || splitDamage <= 0f
            || splitLength <= 0f
            || splitSpawnedTargets.Contains(hitTarget))
        {
            return;
        }

        splitSpawnedTargets.Add(hitTarget);
        Vector2 baseDirection = ResolvePathDirectionNearPoint(hitTarget.transform.position);
        Vector3 splitOrigin = hitTarget.transform.position;

        for (int i = 0; i < splitProjectileCount; i++)
        {
            Vector2 splitDirection = ResolveSplitDirection(baseDirection, i, splitProjectileCount);
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(splitDirection.y, splitDirection.x) * Mathf.Rad2Deg);
            Vector3 spawnPosition = splitOrigin + (Vector3)(splitDirection * 0.18f);
            SpawnSplitLaser(spawnPosition, rotation, splitDirection, hitTarget);
        }
    }

    private void SpawnSplitLaser(Vector3 spawnPosition, Quaternion rotation, Vector2 splitDirection, Health hitTarget)
    {
        GameObject splitObject = new GameObject("JIN_BrimstoneLaser_Split");
        splitObject.transform.SetPositionAndRotation(spawnPosition, rotation);
        JIN_BrimstoneLaser splitLaser = splitObject.AddComponent<JIN_BrimstoneLaser>();

        // 혈사포 분열은 총알 프리팹 대신 같은 혈사포 판정을 절반 두께로 생성한다.
        splitLaser.Initialize(new Configuration
        {
            Owner = owner,
            Origin = null,
            OriginOffset = Vector3.zero,
            InitialTarget = null,
            Direction = splitDirection,
            DamagePerSecond = splitDamage,
            Length = splitLength,
            Width = width * splitScaleMultiplier,
            Duration = splitLifetime,
            LaserColor = laserColor,
            EnemyTag = enemyTag,
            EnemyLayers = enemyLayers,
            UseHoming = splitUseHoming,
            HomingRadius = splitHomingRadius,
            HomingTargetLimit = homingTargetLimit,
            HomingCurveStrength = homingCurveStrength,
            CurveSamplesPerSegment = curveSamplesPerSegment,
            PulseSpeed = pulseSpeed,
            UseSplitAttack = false,
            SplitProjectileCount = splitProjectileCount,
            SplitSpreadAngle = splitSpreadAngle,
            SplitDamage = 0f,
            SplitLength = splitLength,
            SplitLifetime = splitLifetime,
            SplitUseHoming = false,
            SplitHomingTurnSpeed = splitHomingTurnSpeed,
            SplitHomingRetargetInterval = splitHomingRetargetInterval,
            SplitHomingRadius = splitHomingRadius,
            SplitScaleMultiplier = splitScaleMultiplier,
            IgnoredTarget = hitTarget
        });
    }

    private Vector2 ResolvePathDirectionNearPoint(Vector3 point)
    {
        Vector2 fallbackDirection = direction;
        float nearestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < renderPoints.Count - 1; i++)
        {
            Vector2 from = renderPoints[i];
            Vector2 to = renderPoints[i + 1];
            Vector2 segment = to - from;

            if (!IsUsableDirection(segment))
            {
                continue;
            }

            float sqrDistance = DistanceToSegmentSqr(point, from, to);

            if (sqrDistance >= nearestSqrDistance)
            {
                continue;
            }

            nearestSqrDistance = sqrDistance;
            fallbackDirection = segment.normalized;
        }

        return fallbackDirection;
    }

    private Vector2 ResolveSplitDirection(Vector2 baseDirection, int index, int count)
    {
        if (count <= 1)
        {
            return baseDirection;
        }

        float normalizedOffset = index / (float)(count - 1) - 0.5f;
        return RotateDirection(baseDirection, normalizedOffset * splitSpreadAngle);
    }

    private bool IsPointOnLaserPath(Vector3 point)
    {
        float hitRadius = Mathf.Max(MinimumHitRadius, width * 0.5f);
        float hitRadiusSqr = hitRadius * hitRadius;

        for (int i = 0; i < renderPoints.Count - 1; i++)
        {
            Vector2 from = renderPoints[i];
            Vector2 to = renderPoints[i + 1];

            if (DistanceToSegmentSqr(point, from, to) <= hitRadiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateVisual(float life01)
    {
        if (lineRenderer == null || renderPoints.Count == 0)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(elapsed * Mathf.Max(1f, pulseSpeed) * Mathf.PI * 2f) * 0.08f;
        float widthScale = Mathf.Lerp(1.25f, 0.35f, life01) * pulse;
        float alphaScale = 1f - Mathf.SmoothStep(0.65f, 1f, life01);
        Color startColor = laserColor;
        Color endColor = new Color(1f, 0f, 0f, laserColor.a * 0.65f);

        startColor.a *= alphaScale;
        endColor.a *= alphaScale;

        lineRenderer.startWidth = width * widthScale;
        lineRenderer.endWidth = width * widthScale * 0.82f;
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;
        lineRenderer.positionCount = renderPoints.Count;

        for (int i = 0; i < renderPoints.Count; i++)
        {
            lineRenderer.SetPosition(i, renderPoints[i]);
        }
    }

    private bool IsValidDamageTarget(Health candidate)
    {
        if (candidate == null || !candidate.isActiveAndEnabled || !candidate.IsAlive)
        {
            return false;
        }

        if (candidate == ignoredTarget)
        {
            return false;
        }

        if (!IsInOwnerScene(candidate))
        {
            return false;
        }

        if (owner != null && MatchesObjectOrChild(candidate.gameObject, owner))
        {
            return false;
        }

        return MatchesEnemyFilters(candidate);
    }

    private bool IsInOwnerScene(Health candidate)
    {
        return !ownerScene.IsValid() || candidate.gameObject.scene == ownerScene;
    }

    private bool MatchesEnemyFilters(Health candidate)
    {
        bool usesTagFilter = !string.IsNullOrEmpty(enemyTag);
        bool usesLayerFilter = enemyLayers.value != 0;

        if (!usesTagFilter && !usesLayerFilter)
        {
            return true;
        }

        bool tagMatches = usesTagFilter && candidate.gameObject.tag == enemyTag;
        bool layerMatches = usesLayerFilter && (enemyLayers.value & (1 << candidate.gameObject.layer)) != 0;

        return tagMatches || layerMatches;
    }

    private Vector3 ResolveOriginPosition()
    {
        Vector3 basePosition = origin != null ? origin.position : transform.position;
        return basePosition + originOffset;
    }

    private Vector2 ResolveOriginRight()
    {
        return origin != null ? origin.right : transform.right;
    }

    private static Vector3 CubicBezier(Vector3 from, Vector3 startControl, Vector3 endControl, Vector3 to, float t)
    {
        float inverseT = 1f - t;
        return inverseT * inverseT * inverseT * from
            + 3f * inverseT * inverseT * t * startControl
            + 3f * inverseT * t * t * endControl
            + t * t * t * to;
    }

    private static Vector2 RotateDirection(Vector2 value, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            value.x * cos - value.y * sin,
            value.x * sin + value.y * cos).normalized;
    }

    private static bool TryNormalizeDirection(Vector3 value, out Vector3 normalized)
    {
        if (!IsUsableDirection(value))
        {
            normalized = Vector3.zero;
            return false;
        }

        normalized = value.normalized;
        return true;
    }

    private static float DistanceToSegmentSqr(Vector2 point, Vector2 from, Vector2 to)
    {
        Vector2 segment = to - from;
        float segmentLengthSqr = segment.sqrMagnitude;

        if (segmentLengthSqr <= Mathf.Epsilon)
        {
            return (point - from).sqrMagnitude;
        }

        float t = Vector2.Dot(point - from, segment) / segmentLengthSqr;
        t = Mathf.Clamp01(t);
        Vector2 closestPoint = from + segment * t;
        return (point - closestPoint).sqrMagnitude;
    }

    private static bool MatchesObjectOrChild(GameObject candidate, GameObject root)
    {
        return candidate != null
            && root != null
            && (candidate == root || candidate.transform.IsChildOf(root.transform));
    }

    private static Material ResolveLaserMaterial()
    {
        if (sharedLaserMaterial != null)
        {
            return sharedLaserMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            return null;
        }

        sharedLaserMaterial = new Material(shader)
        {
            name = "JIN_BrimstoneLaser_Runtime"
        };

        return sharedLaserMaterial;
    }

    private static Color SanitizeColor(Color color)
    {
        if (!IsFinite(color.r) || !IsFinite(color.g) || !IsFinite(color.b) || !IsFinite(color.a))
        {
            return new Color(1f, 0.04f, 0.02f, 0.95f);
        }

        return color;
    }

    private static string NormalizeTag(string tagName)
    {
        return string.IsNullOrWhiteSpace(tagName) ? string.Empty : tagName.Trim();
    }

    private static float SanitizeNonNegative(float value)
    {
        return IsFinite(value) ? Mathf.Max(0f, value) : 0f;
    }

    private static float SanitizeNonNegativeWithFallback(float value, float fallback)
    {
        return IsFinite(value) ? Mathf.Max(0f, value) : fallback;
    }

    private static float SanitizePositive(float value, float fallback)
    {
        return IsFinite(value) ? Mathf.Max(0.01f, value) : fallback;
    }

    private static float SanitizePositiveWithFallback(float value, float fallback)
    {
        return IsFinite(value) && value > 0f ? value : fallback;
    }

    private static Vector2 NormalizeDirection(Vector2 value, Vector2 fallback)
    {
        if (!IsUsableDirection(value))
        {
            return IsUsableDirection(fallback) ? fallback.normalized : Vector2.right;
        }

        return value.normalized;
    }

    private static bool IsUsableDirection(Vector2 value)
    {
        return IsFinite(value.x)
            && IsFinite(value.y)
            && value.sqrMagnitude > Mathf.Epsilon;
    }

    private static bool IsUsableDirection(Vector3 value)
    {
        return IsFinite(value.x)
            && IsFinite(value.y)
            && value.sqrMagnitude > Mathf.Epsilon;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

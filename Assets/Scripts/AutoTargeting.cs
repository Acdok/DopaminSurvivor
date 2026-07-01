using UnityEngine;

/// <summary>
/// Finds the nearest alive enemy-like Health target from this object's position.
/// </summary>
[DisallowMultipleComponent]
public class AutoTargeting : MonoBehaviour
{
    [Header("Enemy Filters")]
    [Tooltip("Optional. Leave empty to allow any non-player Health as a target.")]
    [SerializeField]
    private string enemyTag;

    [Tooltip("Optional. Leave empty to allow any non-player Health as a target.")]
    [SerializeField]
    private LayerMask enemyLayers;

    [Header("Self")]
    [Tooltip("Optional. Auto-resolved from this object or its parents when empty.")]
    [SerializeField]
    private Health selfHealth;

    public string EnemyTag
    {
        get => enemyTag;
        set => enemyTag = NormalizeTag(value);
    }

    public LayerMask EnemyLayers
    {
        get => enemyLayers;
        set => enemyLayers = value;
    }

    public Health SelfHealth
    {
        get => selfHealth;
        set => selfHealth = value;
    }

    public Transform GetNearestTarget()
    {
        return TryGetNearestTarget(out Transform target) ? target : null;
    }

    public Health GetNearestTargetHealth()
    {
        return TryGetNearestTargetHealth(out Health targetHealth) ? targetHealth : null;
    }

    public bool TryGetNearestTarget(out Transform target)
    {
        return TryGetNearestTarget(out target, out _);
    }

    public bool TryGetNearestTarget(out Transform target, out Health targetHealth)
    {
        if (TryGetNearestTargetHealth(out targetHealth))
        {
            target = targetHealth.transform;
            return true;
        }

        target = null;
        return false;
    }

    public bool TryGetNearestTargetHealth(out Health targetHealth)
    {
        Health[] candidates = CollectCandidates();
        return TrySelectNearestValidTarget(candidates, out targetHealth);
    }

    private void Awake()
    {
        ResolveSelfHealth();
    }

    private void Reset()
    {
        ResolveSelfHealth();
    }

    private void OnValidate()
    {
        enemyTag = NormalizeTag(enemyTag);
    }

    private void ResolveSelfHealth()
    {
        if (selfHealth != null)
        {
            return;
        }

        if (TryGetComponent(out selfHealth))
        {
            return;
        }

        selfHealth = GetComponentInParent<Health>();
    }

    private Health[] CollectCandidates()
    {
        // Prototype-friendly full scene scan; this boundary can later become cached or physics-based.
        return Object.FindObjectsByType<Health>(FindObjectsInactive.Exclude);
    }

    private bool TrySelectNearestValidTarget(Health[] candidates, out Health nearestHealth)
    {
        nearestHealth = null;
        float nearestSqrDistance = float.PositiveInfinity;
        Vector3 origin = transform.position;

        foreach (Health candidate in candidates)
        {
            if (!IsValidCandidate(candidate))
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

        return nearestHealth != null;
    }

    private bool IsValidCandidate(Health candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        // Dead, disabled, inactive, or self Health components are never valid attack targets.
        return candidate.isActiveAndEnabled
            && candidate.IsAlive
            && IsInCurrentScene(candidate)
            && !IsSelfHealth(candidate)
            && MatchesEnemyFilters(candidate);
    }

    private bool IsInCurrentScene(Health candidate)
    {
        return !gameObject.scene.IsValid() || candidate.gameObject.scene == gameObject.scene;
    }

    private bool IsSelfHealth(Health candidate)
    {
        if (candidate == selfHealth)
        {
            return true;
        }

        return candidate.transform == transform || candidate.gameObject == gameObject;
    }

    private bool MatchesEnemyFilters(Health candidate)
    {
        bool usesTagFilter = !string.IsNullOrEmpty(enemyTag);
        bool usesLayerFilter = enemyLayers.value != 0;

        // With no filters, any alive Health except the player is a useful early prototype target.
        if (!usesTagFilter && !usesLayerFilter)
        {
            return true;
        }

        return MatchesMarker(candidate.gameObject, usesTagFilter, usesLayerFilter);
    }

    private bool MatchesMarker(GameObject candidate, bool usesTagFilter, bool usesLayerFilter)
    {
        if (candidate == null)
        {
            return false;
        }

        bool tagMatches = usesTagFilter && candidate.tag == enemyTag;
        bool layerMatches = usesLayerFilter && (enemyLayers.value & (1 << candidate.layer)) != 0;

        return tagMatches || layerMatches;
    }

    private static string NormalizeTag(string tagName)
    {
        return string.IsNullOrWhiteSpace(tagName) ? string.Empty : tagName.Trim();
    }
}

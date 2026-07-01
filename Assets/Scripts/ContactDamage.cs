using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies periodic damage to a player Health while this enemy remains in contact.
/// </summary>
[DisallowMultipleComponent]
public class ContactDamage : MonoBehaviour
{
    private const float MinimumDamageInterval = 0.01f;

    [Header("Damage")]
    [SerializeField, Min(0f)]
    private float contactDamage = 10f;

    [SerializeField, Min(MinimumDamageInterval)]
    private float damageInterval = 1f;

    [Header("Player Detection")]
    [SerializeField]
    private string playerTag = "Player";

    [SerializeField]
    private LayerMask playerLayers;

    [Header("State References")]
    [SerializeField]
    private Health enemyHealth;

    // Track colliders separately so multi-collider players do not lose contact when only one collider exits.
    private readonly Dictionary<Collider2D, Health> contactHealthByCollider = new Dictionary<Collider2D, Health>();
    private readonly Dictionary<Health, int> contactCountsByHealth = new Dictionary<Health, int>();

    // Cooldowns stay keyed by Health after exit, so re-entering does not reset the damage interval.
    private readonly Dictionary<Health, float> nextDamageTimeByHealth = new Dictionary<Health, float>();
    private readonly List<Health> contactSnapshot = new List<Health>();

    public float ContactDamageAmount
    {
        get => contactDamage;
        set => contactDamage = Mathf.Max(0f, value);
    }

    public float DamageInterval
    {
        get => damageInterval;
        set => damageInterval = Mathf.Max(MinimumDamageInterval, value);
    }

    private void Awake()
    {
        ResolveOptionalReferences();
    }

    private void Reset()
    {
        ResolveOptionalReferences();
    }

    private void OnValidate()
    {
        contactDamage = Mathf.Max(0f, contactDamage);
        damageInterval = Mathf.Max(MinimumDamageInterval, damageInterval);
        ResolveOptionalReferences();
    }

    private void Update()
    {
        if (!IsEnemyAlive())
        {
            return;
        }

        contactSnapshot.Clear();

        foreach (Health playerHealth in contactCountsByHealth.Keys)
        {
            contactSnapshot.Add(playerHealth);
        }

        foreach (Health playerHealth in contactSnapshot)
        {
            TryApplyDamage(playerHealth);
        }
    }

    private void OnDisable()
    {
        contactHealthByCollider.Clear();
        contactCountsByHealth.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleContact(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        HandleContactEnded(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollisionContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollisionContact(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        HandleCollisionContactEnded(collision);
    }

    private void ResolveOptionalReferences()
    {
        if (enemyHealth == null)
        {
            TryGetComponent(out enemyHealth);
        }
    }

    private void HandleCollisionContact(Collision2D collision)
    {
        if (collision == null)
        {
            return;
        }

        HandleContact(collision.collider);
        HandleContact(collision.otherCollider);
    }

    private void HandleCollisionContactEnded(Collision2D collision)
    {
        if (collision == null)
        {
            return;
        }

        HandleContactEnded(collision.collider);
        HandleContactEnded(collision.otherCollider);
    }

    private void HandleContact(Collider2D other)
    {
        if (!TryGetPlayerHealth(other, out Health playerHealth))
        {
            return;
        }

        TrackContact(other, playerHealth);
        TryApplyDamage(playerHealth);
    }

    private void HandleContactEnded(Collider2D other)
    {
        if (other == null || !contactHealthByCollider.TryGetValue(other, out Health playerHealth))
        {
            return;
        }

        contactHealthByCollider.Remove(other);

        if (!contactCountsByHealth.TryGetValue(playerHealth, out int contactCount))
        {
            return;
        }

        contactCount--;

        if (contactCount <= 0)
        {
            contactCountsByHealth.Remove(playerHealth);
            return;
        }

        contactCountsByHealth[playerHealth] = contactCount;
    }

    private void TrackContact(Collider2D other, Health playerHealth)
    {
        if (other == null || playerHealth == null || contactHealthByCollider.ContainsKey(other))
        {
            return;
        }

        contactHealthByCollider.Add(other, playerHealth);

        contactCountsByHealth.TryGetValue(playerHealth, out int contactCount);
        contactCountsByHealth[playerHealth] = contactCount + 1;
    }

    private bool TryGetPlayerHealth(Collider2D other, out Health playerHealth)
    {
        playerHealth = null;

        if (other == null)
        {
            return false;
        }

        playerHealth = ResolveHealth(other);

        if (playerHealth == null || playerHealth == enemyHealth)
        {
            return false;
        }

        return IsPlayerContact(other, playerHealth);
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

    private bool IsPlayerContact(Collider2D other, Health playerHealth)
    {
        bool usesTagFilter = !string.IsNullOrEmpty(playerTag);
        bool usesLayerFilter = playerLayers.value != 0;

        if (!usesTagFilter && !usesLayerFilter)
        {
            return true;
        }

        return MatchesPlayerMarker(other.gameObject)
            || (other.attachedRigidbody != null && MatchesPlayerMarker(other.attachedRigidbody.gameObject))
            || MatchesPlayerMarker(playerHealth.gameObject);
    }

    private bool MatchesPlayerMarker(GameObject candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        bool tagMatches = !string.IsNullOrEmpty(playerTag) && candidate.tag == playerTag;
        bool layerMatches = playerLayers.value != 0 && (playerLayers.value & (1 << candidate.layer)) != 0;

        return tagMatches || layerMatches;
    }

    private void TryApplyDamage(Health playerHealth)
    {
        // Death checks are defensive because either actor can die while contacts are still being reported.
        if (!IsEnemyAlive() || playerHealth == null || !playerHealth.IsAlive || contactDamage <= 0f)
        {
            return;
        }

        float currentTime = Time.time;

        if (nextDamageTimeByHealth.TryGetValue(playerHealth, out float nextDamageTime)
            && currentTime < nextDamageTime)
        {
            return;
        }

        playerHealth.TakeDamage(contactDamage);
        nextDamageTimeByHealth[playerHealth] = currentTime + damageInterval;
    }

    private bool IsEnemyAlive()
    {
        return enemyHealth == null || enemyHealth.IsAlive;
    }
}

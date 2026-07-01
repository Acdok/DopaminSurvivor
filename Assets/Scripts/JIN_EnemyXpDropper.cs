using UnityEngine;

[DisallowMultipleComponent]
public class JIN_EnemyXpDropper : MonoBehaviour
{
    [SerializeField]
    private Health enemyHealth;

    [SerializeField, Min(1)]
    private int experienceAmount = 1;

    [SerializeField]
    private GameObject experiencePickupPrefab;

    private bool hasDropped;

    public int ExperienceAmount
    {
        get => experienceAmount;
        set => experienceAmount = Mathf.Max(1, value);
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        hasDropped = false;
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        experienceAmount = Mathf.Max(1, experienceAmount);
    }

    private void ResolveReferences()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<Health>();
        }
    }

    private void Subscribe()
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Died -= HandleEnemyDied;
        enemyHealth.Died += HandleEnemyDied;
    }

    private void Unsubscribe()
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Died -= HandleEnemyDied;
    }

    private void HandleEnemyDied(Health health)
    {
        if (hasDropped)
        {
            return;
        }

        hasDropped = true;
        SpawnExperiencePickup();
    }

    private void SpawnExperiencePickup()
    {
        // XP 프리팹이 없으면 프로토타입용 구슬을 런타임에 만들어 즉시 테스트 가능하게 한다.
        if (experiencePickupPrefab == null)
        {
            JIN_XpPickup.CreateRuntimePickup(transform.position, experienceAmount);
            return;
        }

        GameObject pickupObject = Instantiate(experiencePickupPrefab, transform.position, Quaternion.identity);

        if (pickupObject.TryGetComponent(out JIN_XpPickup xpPickup))
        {
            xpPickup.SetExperienceValue(experienceAmount);
        }
    }
}

using UnityEngine;

[DisallowMultipleComponent]
public class JIN_BossChestDropper : MonoBehaviour
{
    [SerializeField]
    private Health bossHealth;

    [SerializeField]
    private GameObject chestPrefab;

    [SerializeField, Range(1, 3)]
    private int rewardChoiceCount = 3;

    private bool hasDropped;

    /// <summary>
    /// 런타임에 생성된 보스가 사망 이벤트와 상자 보상 설정을 받을 수 있게 연결한다.
    /// </summary>
    public void Configure(Health newBossHealth, GameObject newChestPrefab, int newRewardChoiceCount)
    {
        Unsubscribe();

        if (newBossHealth != null)
        {
            bossHealth = newBossHealth;
        }

        if (newChestPrefab != null)
        {
            chestPrefab = newChestPrefab;
        }

        rewardChoiceCount = Mathf.Clamp(newRewardChoiceCount, 1, 3);
        hasDropped = false;
        ResolveReferences();
        Subscribe();
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

    private void ResolveReferences()
    {
        if (bossHealth == null)
        {
            bossHealth = GetComponent<Health>();
        }
    }

    private void OnValidate()
    {
        rewardChoiceCount = Mathf.Clamp(rewardChoiceCount, 1, 3);
    }

    private void Subscribe()
    {
        if (bossHealth == null)
        {
            return;
        }

        bossHealth.Died -= HandleBossDied;
        bossHealth.Died += HandleBossDied;
    }

    private void Unsubscribe()
    {
        if (bossHealth == null)
        {
            return;
        }

        bossHealth.Died -= HandleBossDied;
    }

    private void HandleBossDied(Health health)
    {
        if (hasDropped)
        {
            return;
        }

        hasDropped = true;
        SpawnChest();
    }

    private void SpawnChest()
    {
        // 보스 프리팹이 따로 없을 때도 규칙 검증이 가능하도록 런타임 상자를 만든다.
        if (chestPrefab == null)
        {
            JIN_BossRewardChest.CreateRuntimeChest(transform.position, rewardChoiceCount);
            return;
        }

        GameObject chestObject = Instantiate(chestPrefab, transform.position, Quaternion.identity);

        if (chestObject.TryGetComponent(out JIN_BossRewardChest chest))
        {
            chest.SetRewardChoiceCount(rewardChoiceCount);
        }
    }
}

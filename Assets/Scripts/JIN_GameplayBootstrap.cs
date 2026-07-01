using UnityEngine;

public static class JIN_GameplayBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapActiveScene()
    {
        JIN_UIUtility.EnsureLegacyEventSystem();

        GameObject playerObject = ResolvePlayerObject();

        if (playerObject == null)
        {
            return;
        }

        JIN_PlayerExperience experience = EnsureComponent<JIN_PlayerExperience>(playerObject);
        JIN_PlayerItemInventory inventory = EnsureComponent<JIN_PlayerItemInventory>(playerObject);
        JIN_SetSynergyController synergy = EnsureComponent<JIN_SetSynergyController>(playerObject);
        synergy.Configure(inventory);

        JIN_ItemEffectApplier effectApplier = EnsureComponent<JIN_ItemEffectApplier>(playerObject);
        effectApplier.Configure(inventory, experience, synergy);

        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        GameObject controllerHost = gameManager != null ? gameManager.gameObject : playerObject;

        JIN_LevelUpRewardController rewardController = EnsureComponent<JIN_LevelUpRewardController>(controllerHost);
        rewardController.Configure(experience, inventory, gameManager);

        Canvas canvas = JIN_UIUtility.ResolveOrCreateCanvas();

        JIN_GrowthUIController growthUI = EnsureComponent<JIN_GrowthUIController>(canvas.gameObject);
        growthUI.Configure(experience);

        JIN_SetSynergyUIController setUI = EnsureComponent<JIN_SetSynergyUIController>(canvas.gameObject);
        setUI.Configure(inventory, synergy);

        EnsureExistingEnemiesHaveDropper();
    }

    public static void EnsureEnemyDropper(GameObject enemyObject)
    {
        if (enemyObject == null)
        {
            return;
        }

        if (enemyObject.GetComponentInChildren<JIN_EnemyXpDropper>() != null)
        {
            return;
        }

        if (enemyObject.GetComponentInChildren<Health>() == null)
        {
            return;
        }

        // 스폰된 적은 별도 컴포넌트로 XP 드롭 책임만 가진다.
        enemyObject.AddComponent<JIN_EnemyXpDropper>();
    }

    private static GameObject ResolvePlayerObject()
    {
        GameObject taggedPlayer = null;

        try
        {
            taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            taggedPlayer = null;
        }

        if (taggedPlayer != null)
        {
            return taggedPlayer;
        }

        PlayerController playerController = Object.FindAnyObjectByType<PlayerController>();
        return playerController != null ? playerController.gameObject : null;
    }

    private static void EnsureExistingEnemiesHaveDropper()
    {
        EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);

        foreach (EnemyController enemy in enemies)
        {
            if (enemy != null)
            {
                EnsureEnemyDropper(enemy.gameObject);
            }
        }
    }

    private static T EnsureComponent<T>(GameObject host) where T : Component
    {
        if (host.TryGetComponent(out T component))
        {
            return component;
        }

        return host.AddComponent<T>();
    }
}

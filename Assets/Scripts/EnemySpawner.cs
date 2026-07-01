using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemy prefabs around the player at a steady prototype-friendly cadence.
/// </summary>
[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    private const float DefaultSpawnInterval = 2f;
    private const float MinimumSpawnInterval = 0.01f;
    private const float DefaultMinSpawnDistance = 8f;
    private const float DefaultMaxSpawnDistance = 12f;
    private const float DefaultBossSpawnInterval = 60f;
    private const float DefaultBossHealthMultiplier = 8f;
    private const float DefaultBossScaleMultiplier = 2.2f;
    private const float DefaultBossMoveSpeedMultiplier = 0.75f;
    private const float DefaultBossContactDamageMultiplier = 1.5f;

    private enum SpawnArea
    {
        AroundPlayer,
        OutsideCameraView
    }

    [Header("Required References")]
    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private Transform playerTransform;

    [Header("Spawn Timing")]
    [SerializeField, Min(MinimumSpawnInterval)]
    private float spawnInterval = DefaultSpawnInterval;

    [SerializeField, Min(1)]
    private int spawnCount = 1;

    [SerializeField, Min(0)]
    private int maxActiveEnemies = 30;

    [Header("Boss Spawn")]
    [SerializeField]
    [Tooltip("켜면 생존 시간 기준으로 일정 주기마다 보스를 생성한다.")]
    private bool spawnBosses = true;

    [SerializeField]
    [Tooltip("비어 있으면 Enemy Prefab을 보스 원형으로 사용한다.")]
    private GameObject bossPrefab;

    [SerializeField, Min(MinimumSpawnInterval)]
    [Tooltip("보스 생성 주기다. 기본값은 60초다.")]
    private float bossSpawnInterval = DefaultBossSpawnInterval;

    [SerializeField, Min(1f)]
    [Tooltip("기본 적 프리팹을 보스로 사용할 때 적용하는 체력 배율이다.")]
    private float bossHealthMultiplier = DefaultBossHealthMultiplier;

    [SerializeField, Min(0.01f)]
    [Tooltip("보스를 일반 적보다 크게 보이게 하는 크기 배율이다.")]
    private float bossScaleMultiplier = DefaultBossScaleMultiplier;

    [SerializeField, Min(0f)]
    [Tooltip("보스 이동 속도 배율이다.")]
    private float bossMoveSpeedMultiplier = DefaultBossMoveSpeedMultiplier;

    [SerializeField, Min(0f)]
    [Tooltip("보스 접촉 피해 배율이다.")]
    private float bossContactDamageMultiplier = DefaultBossContactDamageMultiplier;

    [SerializeField]
    [Tooltip("비어 있으면 런타임 보상 상자를 생성한다.")]
    private GameObject bossChestPrefab;

    [SerializeField, Range(1, 3)]
    private int bossRewardChoiceCount = 3;

    [Header("Spawn Area")]
    [SerializeField]
    private SpawnArea spawnArea = SpawnArea.AroundPlayer;

    [SerializeField, Min(0f)]
    private float minSpawnDistance = DefaultMinSpawnDistance;

    [SerializeField, Min(0f)]
    private float maxSpawnDistance = DefaultMaxSpawnDistance;

    [Tooltip("Optional. Used only when spawning outside an orthographic camera view.")]
    [SerializeField]
    private Camera referenceCamera;

    [SerializeField, Min(0f)]
    private float cameraEdgePadding = 1f;

    [Header("State References")]
    [Tooltip("Optional. When absent, spawning ignores game-over state.")]
    [SerializeField]
    private GameManager gameManager;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    private float spawnTimer;
    private float bossSpawnTimer;
    private bool warnedMissingEnemyPrefab;
    private bool warnedMissingPlayerTransform;
    private bool warnedMissingEnemyController;
    private bool warnedMissingCamera;

    private void Awake()
    {
        ResolveOptionalReferences();
    }

    private void OnEnable()
    {
        spawnTimer = spawnInterval;
        bossSpawnTimer = bossSpawnInterval;
    }

    private void Reset()
    {
        ResolveOptionalReferences();
    }

    private void OnValidate()
    {
        spawnInterval = SanitizeMinimum(spawnInterval, MinimumSpawnInterval, DefaultSpawnInterval);
        spawnCount = Mathf.Max(1, spawnCount);
        maxActiveEnemies = Mathf.Max(0, maxActiveEnemies);
        bossSpawnInterval = SanitizeMinimum(bossSpawnInterval, MinimumSpawnInterval, DefaultBossSpawnInterval);
        bossHealthMultiplier = SanitizeMinimum(bossHealthMultiplier, 1f, DefaultBossHealthMultiplier);
        bossScaleMultiplier = SanitizeMinimum(bossScaleMultiplier, 0.01f, DefaultBossScaleMultiplier);
        bossMoveSpeedMultiplier = SanitizeMinimum(bossMoveSpeedMultiplier, 0f, DefaultBossMoveSpeedMultiplier);
        bossContactDamageMultiplier = SanitizeMinimum(bossContactDamageMultiplier, 0f, DefaultBossContactDamageMultiplier);
        bossRewardChoiceCount = Mathf.Clamp(bossRewardChoiceCount, 1, 3);
        minSpawnDistance = SanitizeMinimum(minSpawnDistance, 0f, DefaultMinSpawnDistance);
        maxSpawnDistance = SanitizeMinimum(
            maxSpawnDistance,
            minSpawnDistance,
            Mathf.Max(DefaultMaxSpawnDistance, minSpawnDistance));
        cameraEdgePadding = SanitizeMinimum(cameraEdgePadding, 0f, 1f);
    }

    private void Update()
    {
        // Game-over state stops future spawns, while scenes without a GameManager keep running.
        if (IsSpawningBlockedByGameState())
        {
            return;
        }

        if (!HasRequiredReferences())
        {
            return;
        }

        PruneInactiveEnemies();
        TickSpawnTimer();
        TickBossSpawnTimer();

        if (IsBossSpawnReady())
        {
            SpawnBoss();
            RestartBossSpawnTimer();
        }

        if (spawnTimer > 0f)
        {
            return;
        }

        TrySpawnBatch();
        RestartSpawnTimer();
    }

    private void ResolveOptionalReferences()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        if (referenceCamera == null)
        {
            referenceCamera = Camera.main;
        }
    }

    private bool IsSpawningBlockedByGameState()
    {
        return gameManager != null && gameManager.IsGameOver;
    }

    private bool HasRequiredReferences()
    {
        bool hasRequiredReferences = true;

        if (enemyPrefab == null)
        {
            WarnMissingEnemyPrefabOnce();
            hasRequiredReferences = false;
        }

        if (playerTransform == null)
        {
            WarnMissingPlayerTransformOnce();
            hasRequiredReferences = false;
        }

        return hasRequiredReferences;
    }

    private void TickSpawnTimer()
    {
        // Timer logic stays separate so blocked spawns do not consume a spawn cycle.
        spawnTimer -= Time.deltaTime;
    }

    private void RestartSpawnTimer()
    {
        spawnTimer = spawnInterval;
    }

    private void TickBossSpawnTimer()
    {
        if (!spawnBosses)
        {
            return;
        }

        bossSpawnTimer -= Time.deltaTime;
    }

    private bool IsBossSpawnReady()
    {
        return spawnBosses && bossSpawnTimer <= 0f;
    }

    private void RestartBossSpawnTimer()
    {
        bossSpawnTimer = bossSpawnInterval;
    }

    private void TrySpawnBatch()
    {
        int spawnSlotsAvailable = maxActiveEnemies - spawnedEnemies.Count;

        if (spawnSlotsAvailable <= 0)
        {
            return;
        }

        int enemiesToSpawn = Mathf.Min(spawnCount, spawnSlotsAvailable);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        Vector3 spawnPosition = CalculateSpawnPosition();
        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, enemyPrefab.transform.rotation);

        spawnedEnemies.Add(enemyObject);
        InjectTarget(enemyObject);
        JIN_GameplayBootstrap.EnsureEnemyDropper(enemyObject);
    }

    private void SpawnBoss()
    {
        GameObject resolvedBossPrefab = ResolveBossPrefab();

        if (resolvedBossPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = CalculateSpawnPosition();
        GameObject bossObject = Instantiate(resolvedBossPrefab, spawnPosition, resolvedBossPrefab.transform.rotation);
        bossObject.name = $"{resolvedBossPrefab.name}_Boss";

        spawnedEnemies.Add(bossObject);
        InjectTarget(bossObject);
        ConfigureBossStats(bossObject);
        JIN_GameplayBootstrap.EnsureEnemyDropper(bossObject);
        EnsureBossChestDropper(bossObject);
    }

    private GameObject ResolveBossPrefab()
    {
        return bossPrefab != null ? bossPrefab : enemyPrefab;
    }

    private void ConfigureBossStats(GameObject bossObject)
    {
        if (bossObject == null)
        {
            return;
        }

        // 별도 보스 프리팹이 없어도 1분 보스 루프를 검증할 수 있게 기본 적을 강화한다.
        bossObject.transform.localScale *= bossScaleMultiplier;

        Health bossHealth = ResolveEnemyHealth(bossObject);

        if (bossHealth != null)
        {
            float bonusHealth = bossHealth.MaxHealth * (bossHealthMultiplier - 1f);
            bossHealth.SetMaxHealthBonus(bonusHealth, true);
        }

        EnemyController bossController = ResolveEnemyController(bossObject);

        if (bossController != null)
        {
            bossController.MoveSpeed *= bossMoveSpeedMultiplier;
        }

        ContactDamage contactDamage = ResolveContactDamage(bossObject);

        if (contactDamage != null)
        {
            contactDamage.ContactDamageAmount *= bossContactDamageMultiplier;
        }
    }

    private void EnsureBossChestDropper(GameObject bossObject)
    {
        if (bossObject == null)
        {
            return;
        }

        Health bossHealth = ResolveEnemyHealth(bossObject);
        JIN_BossChestDropper dropper = ResolveBossChestDropper(bossObject);

        if (dropper == null)
        {
            dropper = bossObject.AddComponent<JIN_BossChestDropper>();
        }

        dropper.Configure(bossHealth, bossChestPrefab, bossRewardChoiceCount);
    }

    private void PruneInactiveEnemies()
    {
        // Keep the list as the active-count source; null, disabled, and dead enemies stop counting.
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (IsActiveEnemy(spawnedEnemies[i]))
            {
                continue;
            }

            spawnedEnemies.RemoveAt(i);
        }
    }

    private bool IsActiveEnemy(GameObject enemyObject)
    {
        if (enemyObject == null || !enemyObject.activeInHierarchy)
        {
            return false;
        }

        Health enemyHealth = ResolveEnemyHealth(enemyObject);
        return enemyHealth == null || enemyHealth.IsAlive;
    }

    private Health ResolveEnemyHealth(GameObject enemyObject)
    {
        if (enemyObject.TryGetComponent(out Health health))
        {
            return health;
        }

        return enemyObject.GetComponentInChildren<Health>();
    }

    private Vector3 CalculateSpawnPosition()
    {
        // Position choice is intentionally simple: an annulus around the player or just outside the camera.
        if (spawnArea == SpawnArea.OutsideCameraView
            && TryGetCameraEdgeSpawnPosition(out Vector3 cameraEdgePosition))
        {
            return cameraEdgePosition;
        }

        return GetPlayerRadiusSpawnPosition();
    }

    private Vector3 GetPlayerRadiusSpawnPosition()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        Vector3 playerPosition = playerTransform.position;

        return new Vector3(playerPosition.x + offset.x, playerPosition.y + offset.y, playerPosition.z);
    }

    private bool TryGetCameraEdgeSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        Camera spawnCamera = ResolveSpawnCamera();

        if (spawnCamera == null || !spawnCamera.orthographic)
        {
            WarnMissingCameraOnce();
            return false;
        }

        Vector3 cameraPosition = spawnCamera.transform.position;
        float verticalExtent = spawnCamera.orthographicSize + cameraEdgePadding;
        float horizontalExtent = (spawnCamera.orthographicSize * spawnCamera.aspect) + cameraEdgePadding;

        float x = Random.Range(-horizontalExtent, horizontalExtent);
        float y = Random.Range(-verticalExtent, verticalExtent);
        int edge = Random.Range(0, 4);

        if (edge == 0)
        {
            x = -horizontalExtent;
        }
        else if (edge == 1)
        {
            x = horizontalExtent;
        }
        else if (edge == 2)
        {
            y = verticalExtent;
        }
        else
        {
            y = -verticalExtent;
        }

        spawnPosition = new Vector3(cameraPosition.x + x, cameraPosition.y + y, playerTransform.position.z);
        return true;
    }

    private Camera ResolveSpawnCamera()
    {
        if (referenceCamera != null)
        {
            return referenceCamera;
        }

        referenceCamera = Camera.main;
        return referenceCamera;
    }

    private void InjectTarget(GameObject enemyObject)
    {
        if (enemyObject == null || playerTransform == null)
        {
            return;
        }

        // Target injection lets EnemyController chase the player without adding spawner-side dependencies.
        EnemyController enemyController = ResolveEnemyController(enemyObject);

        if (enemyController == null)
        {
            WarnMissingEnemyControllerOnce();
            return;
        }

        enemyController.SetTarget(playerTransform);
    }

    private EnemyController ResolveEnemyController(GameObject enemyObject)
    {
        if (enemyObject.TryGetComponent(out EnemyController enemyController))
        {
            return enemyController;
        }

        return enemyObject.GetComponentInChildren<EnemyController>();
    }

    private ContactDamage ResolveContactDamage(GameObject enemyObject)
    {
        if (enemyObject.TryGetComponent(out ContactDamage contactDamage))
        {
            return contactDamage;
        }

        return enemyObject.GetComponentInChildren<ContactDamage>();
    }

    private JIN_BossChestDropper ResolveBossChestDropper(GameObject bossObject)
    {
        if (bossObject.TryGetComponent(out JIN_BossChestDropper dropper))
        {
            return dropper;
        }

        return bossObject.GetComponentInChildren<JIN_BossChestDropper>();
    }

    private void WarnMissingEnemyPrefabOnce()
    {
        if (warnedMissingEnemyPrefab)
        {
            return;
        }

        warnedMissingEnemyPrefab = true;
        Debug.LogWarning($"{nameof(EnemySpawner)} on {name} has no Enemy Prefab assigned.", this);
    }

    private void WarnMissingPlayerTransformOnce()
    {
        if (warnedMissingPlayerTransform)
        {
            return;
        }

        warnedMissingPlayerTransform = true;
        Debug.LogWarning($"{nameof(EnemySpawner)} on {name} has no Player Transform assigned.", this);
    }

    private void WarnMissingEnemyControllerOnce()
    {
        if (warnedMissingEnemyController)
        {
            return;
        }

        warnedMissingEnemyController = true;
        Debug.LogWarning(
            $"{nameof(EnemySpawner)} on {name} spawned an enemy without an {nameof(EnemyController)} component.",
            this);
    }

    private void WarnMissingCameraOnce()
    {
        if (warnedMissingCamera)
        {
            return;
        }

        warnedMissingCamera = true;
        Debug.LogWarning(
            $"{nameof(EnemySpawner)} on {name} needs an orthographic camera for camera-edge spawning. Falling back to player-radius spawning.",
            this);
    }

    private static float SanitizeMinimum(float value, float minimum, float fallback)
    {
        return IsFinite(value) ? Mathf.Max(minimum, value) : fallback;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

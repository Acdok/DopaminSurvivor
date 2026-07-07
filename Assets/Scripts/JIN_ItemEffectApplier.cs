using UnityEngine;

[DisallowMultipleComponent]
public class JIN_ItemEffectApplier : MonoBehaviour
{
    private const string BrimstoneItemId = "blood_cannon";
    private const string GenericHomingItemId = "guidance_chip";
    private const string SplitAttackItemId = "split_round";
    private const string DoubleShotItemId = "double_shot";
    private const string TripleShotItemId = "triple_shot";
    private const string QuadShotItemId = "quad_shot";
    private const string BrimstoneSetId = "overcharge";

    [SerializeField]
    private JIN_PlayerItemInventory inventory;

    [SerializeField]
    private JIN_PlayerExperience playerExperience;

    [SerializeField]
    private JIN_SetSynergyController setSynergyController;

    [SerializeField]
    private WeaponController weaponController;

    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private Health playerHealth;

    [Header("Test Keyboard Toggles")]
    [SerializeField]
    [Tooltip("테스트용 숫자키 아이템 토글을 사용할지 여부다.")]
    private bool useTestKeyboardToggles = true;

    [SerializeField]
    [Tooltip("혈사포 활성화 중 공격속도를 50% 낮추기 위해 공격 간격에 곱하는 값이다.")]
    private float brimstoneAttackIntervalMultiplier = 2f;

    private bool testDoubleShotEnabled;
    private bool testTripleShotEnabled;
    private bool testQuadShotEnabled;
    private bool testSplitAttackEnabled;
    private bool testHomingAttackEnabled;
    private bool testBrimstoneEnabled;
    private bool hasTestDoubleShotOverride;
    private bool hasTestTripleShotOverride;
    private bool hasTestQuadShotOverride;
    private bool hasTestSplitAttackOverride;
    private bool hasTestHomingAttackOverride;
    private bool hasTestBrimstoneOverride;

    private bool hasBaseStats;
    private float baseAttackInterval;
    private float baseBrimstoneAttackInterval;
    private float baseProjectileDamage;
    private float baseProjectileSpeed;
    private float baseMoveSpeed;

    private void Awake()
    {
        ResolveReferences();
        CacheBaseStats();
    }

    private void OnEnable()
    {
        Subscribe();
        ApplyCurrentEffects();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        HandleTestKeyboardToggles();
    }

    public void Configure(
        JIN_PlayerItemInventory newInventory,
        JIN_PlayerExperience newExperience,
        JIN_SetSynergyController newSetSynergyController)
    {
        Unsubscribe();
        inventory = newInventory;
        playerExperience = newExperience;
        setSynergyController = newSetSynergyController;
        ResolveReferences();
        CacheBaseStats();
        Subscribe();
        ApplyCurrentEffects();
    }

    private void ResolveReferences()
    {
        if (inventory == null)
        {
            inventory = GetComponent<JIN_PlayerItemInventory>();
        }

        if (playerExperience == null)
        {
            playerExperience = GetComponent<JIN_PlayerExperience>();
        }

        if (setSynergyController == null)
        {
            setSynergyController = GetComponent<JIN_SetSynergyController>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<WeaponController>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<Health>();
        }
    }

    private void Subscribe()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= HandleInventoryChanged;
            inventory.InventoryChanged += HandleInventoryChanged;
        }

        if (setSynergyController != null)
        {
            setSynergyController.SynergyChanged -= HandleSynergyChanged;
            setSynergyController.SynergyChanged += HandleSynergyChanged;
        }
    }

    private void Unsubscribe()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= HandleInventoryChanged;
        }

        if (setSynergyController != null)
        {
            setSynergyController.SynergyChanged -= HandleSynergyChanged;
        }
    }

    private void CacheBaseStats()
    {
        if (hasBaseStats)
        {
            return;
        }

        baseAttackInterval = weaponController != null ? weaponController.AttackInterval : 1f;
        baseBrimstoneAttackInterval = weaponController != null ? weaponController.BrimstoneAttackInterval : 1.5f;
        baseProjectileDamage = weaponController != null ? weaponController.ProjectileDamage : 10f;
        baseProjectileSpeed = weaponController != null ? weaponController.ProjectileSpeed : 10f;
        baseMoveSpeed = playerController != null ? playerController.MoveSpeed : 5f;
        hasBaseStats = true;
    }

    private void HandleInventoryChanged(JIN_PlayerItemInventory changedInventory)
    {
        ApplyCurrentEffects();
    }

    private void HandleSynergyChanged(JIN_SetSynergyController changedSynergy)
    {
        ApplyCurrentEffects();
    }

    private void ApplyCurrentEffects()
    {
        if (!hasBaseStats)
        {
            CacheBaseStats();
        }

        float attackSpeedBonus = 0f;
        float damageBonus = 0f;
        float projectileSpeedBonus = 0f;
        float moveSpeedBonus = 0f;
        float maxHealthBonus = 0f;
        float experienceGainBonus = 0f;
        bool shouldUseBrimstoneLaser = HasBrimstoneItem();
        bool shouldUseGenericHoming = HasGenericHomingItem();
        bool shouldUseSplitAttack = HasSplitAttackItem();
        bool shouldUseBrimstoneSetHoming = shouldUseBrimstoneLaser && IsBrimstoneSetSynergyActive();
        int attackCount = ResolveAttackCount();

        ApplyItemBonuses(
            ref attackSpeedBonus,
            ref damageBonus,
            ref projectileSpeedBonus,
            ref moveSpeedBonus,
            ref maxHealthBonus,
            ref experienceGainBonus);

        ApplySetBonuses(
            ref attackSpeedBonus,
            ref damageBonus,
            ref projectileSpeedBonus,
            ref moveSpeedBonus,
            ref maxHealthBonus,
            ref experienceGainBonus);

        // 전투 수치는 원본 값을 기준으로 다시 계산해 중복 적용을 방지한다.
        if (weaponController != null)
        {
            float attackInterval = Mathf.Max(0.08f, baseAttackInterval / (1f + attackSpeedBonus));
            float brimstoneAttackInterval = Mathf.Max(0.08f, baseBrimstoneAttackInterval / (1f + attackSpeedBonus));

            if (shouldUseBrimstoneLaser)
            {
                // 혈사포는 활성화된 동안만 공격 간격을 늘려 실제 공격속도를 50% 감소시킨다.
                brimstoneAttackInterval *= Mathf.Max(1f, brimstoneAttackIntervalMultiplier);
            }

            weaponController.AttackInterval = attackInterval;
            weaponController.BrimstoneAttackInterval = brimstoneAttackInterval;
            weaponController.ProjectileDamage = Mathf.Max(0f, baseProjectileDamage + damageBonus);
            weaponController.ProjectileSpeed = Mathf.Max(0f, baseProjectileSpeed + projectileSpeedBonus);

            // 혈사포 세트 유도와 범용 유도 아이템은 별도 플래그로 관리해 다른 공격 확장에도 재사용한다.
            weaponController.UseBrimstoneLaser = shouldUseBrimstoneLaser;
            weaponController.UseHomingAttack = shouldUseGenericHoming;
            weaponController.UseSplitAttack = shouldUseSplitAttack;
            weaponController.UseHomingLaser = shouldUseBrimstoneSetHoming;
            weaponController.AttackCount = attackCount;
        }

        if (playerController != null)
        {
            playerController.MoveSpeed = Mathf.Max(0f, baseMoveSpeed + moveSpeedBonus);
        }

        if (playerHealth != null)
        {
            playerHealth.SetMaxHealthBonus(maxHealthBonus, true);
        }

        if (playerExperience != null)
        {
            playerExperience.ExperienceGainMultiplier = 1f + experienceGainBonus;
        }
    }

    private void HandleTestKeyboardToggles()
    {
        if (!useTestKeyboardToggles)
        {
            return;
        }

        bool changed = false;

        // 테스트용 숫자키 토글은 Old Input만 사용한다.
        if (WasTestTogglePressed(KeyCode.Alpha1, KeyCode.Keypad1))
        {
            ToggleTestOverride(ref hasTestDoubleShotOverride, ref testDoubleShotEnabled, HasInventoryItem(DoubleShotItemId));
            changed = true;
        }

        if (WasTestTogglePressed(KeyCode.Alpha2, KeyCode.Keypad2))
        {
            ToggleTestOverride(ref hasTestTripleShotOverride, ref testTripleShotEnabled, HasInventoryItem(TripleShotItemId));
            changed = true;
        }

        if (WasTestTogglePressed(KeyCode.Alpha3, KeyCode.Keypad3))
        {
            ToggleTestOverride(ref hasTestQuadShotOverride, ref testQuadShotEnabled, HasInventoryItem(QuadShotItemId));
            changed = true;
        }

        if (WasTestTogglePressed(KeyCode.Alpha4, KeyCode.Keypad4))
        {
            ToggleTestOverride(ref hasTestSplitAttackOverride, ref testSplitAttackEnabled, HasInventoryItem(SplitAttackItemId));
            changed = true;
        }

        if (WasTestTogglePressed(KeyCode.Alpha5, KeyCode.Keypad5))
        {
            ToggleTestOverride(ref hasTestHomingAttackOverride, ref testHomingAttackEnabled, HasInventoryItem(GenericHomingItemId));
            changed = true;
        }

        if (WasTestTogglePressed(KeyCode.Alpha6, KeyCode.Keypad6))
        {
            ToggleTestOverride(ref hasTestBrimstoneOverride, ref testBrimstoneEnabled, HasInventoryItem(BrimstoneItemId));
            changed = true;
        }

        if (changed)
        {
            ApplyCurrentEffects();
        }
    }

    private void ApplyItemBonuses(
        ref float attackSpeedBonus,
        ref float damageBonus,
        ref float projectileSpeedBonus,
        ref float moveSpeedBonus,
        ref float maxHealthBonus,
        ref float experienceGainBonus)
    {
        if (inventory == null)
        {
            return;
        }

        foreach (JIN_OwnedItemInfo ownedItem in inventory.OwnedItems)
        {
            float totalValue = ownedItem.Definition.StatPerLevel * ownedItem.Level;
            ApplyStatBonus(
                ownedItem.Definition.StatType,
                totalValue,
                ref attackSpeedBonus,
                ref damageBonus,
                ref projectileSpeedBonus,
                ref moveSpeedBonus,
                ref maxHealthBonus,
                ref experienceGainBonus);
        }
    }

    private bool HasBrimstoneItem()
    {
        return ResolveTestableItemState(hasTestBrimstoneOverride, testBrimstoneEnabled, BrimstoneItemId);
    }

    private bool HasGenericHomingItem()
    {
        return ResolveTestableItemState(hasTestHomingAttackOverride, testHomingAttackEnabled, GenericHomingItemId);
    }

    private bool HasSplitAttackItem()
    {
        return ResolveTestableItemState(hasTestSplitAttackOverride, testSplitAttackEnabled, SplitAttackItemId);
    }

    private int ResolveAttackCount()
    {
        if (ResolveTestableItemState(hasTestQuadShotOverride, testQuadShotEnabled, QuadShotItemId))
        {
            return 4;
        }

        if (ResolveTestableItemState(hasTestTripleShotOverride, testTripleShotEnabled, TripleShotItemId))
        {
            return 3;
        }

        if (ResolveTestableItemState(hasTestDoubleShotOverride, testDoubleShotEnabled, DoubleShotItemId))
        {
            return 2;
        }

        return 1;
    }

    private bool HasInventoryItem(string itemId)
    {
        return inventory != null && inventory.GetItemLevel(itemId) > 0;
    }

    private bool ResolveTestableItemState(bool hasOverride, bool testEnabled, string itemId)
    {
        return hasOverride ? testEnabled : HasInventoryItem(itemId);
    }

    private static void ToggleTestOverride(ref bool hasOverride, ref bool testEnabled, bool inventoryEnabled)
    {
        // 첫 테스트 입력은 현재 실제 보유 상태의 반대로 시작해 획득 아이템도 즉시 끌 수 있게 한다.
        testEnabled = hasOverride ? !testEnabled : !inventoryEnabled;
        hasOverride = true;
    }

    private static bool WasTestTogglePressed(KeyCode mainKey, KeyCode keypadKey)
    {
        return Input.GetKeyDown(mainKey) || Input.GetKeyDown(keypadKey);
    }

    private bool IsBrimstoneSetSynergyActive()
    {
        if (setSynergyController == null)
        {
            return false;
        }

        JIN_SetProgress progress = setSynergyController.GetProgress(BrimstoneSetId);
        return progress.HasWarmup;
    }

    private void ApplySetBonuses(
        ref float attackSpeedBonus,
        ref float damageBonus,
        ref float projectileSpeedBonus,
        ref float moveSpeedBonus,
        ref float maxHealthBonus,
        ref float experienceGainBonus)
    {
        if (setSynergyController == null)
        {
            return;
        }

        foreach (JIN_SetProgress progress in setSynergyController.Progress)
        {
            int count = progress.OwnedUniqueItemCount;

            if (count < 2)
            {
                continue;
            }

            float tierScale = count >= 4 ? 1.75f : count >= 3 ? 1.25f : 0.75f;

            switch (progress.Definition.Id)
            {
                case "overcharge":
                    attackSpeedBonus += 0.08f * tierScale;
                    damageBonus += 1.5f * tierScale;
                    break;
                case "survival":
                    maxHealthBonus += 10f * tierScale;
                    damageBonus += 0.8f * tierScale;
                    break;
                case "dash":
                    moveSpeedBonus += 0.25f * tierScale;
                    projectileSpeedBonus += 0.8f * tierScale;
                    break;
                case "greed":
                    experienceGainBonus += 0.08f * tierScale;
                    attackSpeedBonus += 0.04f * tierScale;
                    break;
                case "burn":
                    damageBonus += 2f * tierScale;
                    break;
                case "control":
                    damageBonus += 1.2f * tierScale;
                    maxHealthBonus += 6f * tierScale;
                    break;
            }
        }
    }

    private void ApplyStatBonus(
        JIN_ItemStatType statType,
        float value,
        ref float attackSpeedBonus,
        ref float damageBonus,
        ref float projectileSpeedBonus,
        ref float moveSpeedBonus,
        ref float maxHealthBonus,
        ref float experienceGainBonus)
    {
        switch (statType)
        {
            case JIN_ItemStatType.ProjectileDamage:
                damageBonus += value;
                break;
            case JIN_ItemStatType.AttackSpeed:
                attackSpeedBonus += value;
                break;
            case JIN_ItemStatType.ProjectileSpeed:
                projectileSpeedBonus += value;
                break;
            case JIN_ItemStatType.MoveSpeed:
                moveSpeedBonus += value;
                break;
            case JIN_ItemStatType.MaxHealth:
                maxHealthBonus += value;
                break;
            case JIN_ItemStatType.ExperienceGain:
                experienceGainBonus += value;
                break;
        }
    }
}

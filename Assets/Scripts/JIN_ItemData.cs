using System.Collections.Generic;
using UnityEngine;

public enum JIN_ItemStatType
{
    ProjectileDamage,
    AttackSpeed,
    ProjectileSpeed,
    MoveSpeed,
    MaxHealth,
    ExperienceGain
}

public enum JIN_ItemEffectKind
{
    StatIncrease,
    WeaponAdd,
    Passive,
    RandomOption
}

public enum JIN_ItemRewardSource
{
    LevelUp,
    BossDrop
}

public sealed class JIN_ItemDefinition
{
    public JIN_ItemDefinition(
        string id,
        string displayName,
        string setId,
        int tier,
        JIN_ItemEffectKind effectKind,
        string iconGlyph,
        string flavorText,
        string description,
        JIN_ItemStatType statType,
        float statPerLevel,
        int maxLevel = 5)
    {
        Id = id;
        DisplayName = displayName;
        SetId = setId;
        Tier = Mathf.Clamp(tier, 1, 5);
        EffectKind = effectKind;
        IconGlyph = string.IsNullOrEmpty(iconGlyph) ? "?" : iconGlyph;
        FlavorText = flavorText;
        Description = description;
        StatType = statType;
        StatPerLevel = statPerLevel;
        MaxLevel = Mathf.Clamp(maxLevel, 1, 5);
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string SetId { get; }
    public int Tier { get; }
    public JIN_ItemEffectKind EffectKind { get; }
    public string IconGlyph { get; }
    public string FlavorText { get; }
    public string Description { get; }
    public JIN_ItemStatType StatType { get; }
    public float StatPerLevel { get; }
    public int MaxLevel { get; }
    public string TierLabel => $"{Tier}티어";

    public string EffectKindLabel
    {
        get
        {
            switch (EffectKind)
            {
                case JIN_ItemEffectKind.StatIncrease:
                    return "스탯 증가";
                case JIN_ItemEffectKind.WeaponAdd:
                    return "무기 추가";
                case JIN_ItemEffectKind.Passive:
                    return "패시브";
                case JIN_ItemEffectKind.RandomOption:
                    return "랜덤 옵션";
                default:
                    return "효과";
            }
        }
    }
}

public sealed class JIN_SetDefinition
{
    public JIN_SetDefinition(string id, string displayName, string shortDescription, Color color)
    {
        Id = id;
        DisplayName = displayName;
        ShortDescription = shortDescription;
        Color = color;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string ShortDescription { get; }
    public Color Color { get; }
}

public readonly struct JIN_SetProgress
{
    public JIN_SetProgress(JIN_SetDefinition definition, int ownedUniqueItemCount)
    {
        Definition = definition;
        OwnedUniqueItemCount = ownedUniqueItemCount;
    }

    public JIN_SetDefinition Definition { get; }
    public int OwnedUniqueItemCount { get; }
    public bool HasAnyItem => OwnedUniqueItemCount > 0;
    public bool HasWarmup => OwnedUniqueItemCount >= 2;
    public bool HasCore => OwnedUniqueItemCount >= 3;
    public bool HasOverload => OwnedUniqueItemCount >= 4;

    public string TierLabel
    {
        get
        {
            if (HasOverload)
            {
                return "과충전";
            }

            if (HasCore)
            {
                return "핵심";
            }

            if (HasWarmup)
            {
                return "예열";
            }

            return "미발동";
        }
    }
}

public static class JIN_ItemCatalog
{
    private const int MinTier = 1;
    private const int MaxTier = 5;

    private static readonly int[] LevelUpTierWeights = { 20, 30, 30, 15, 5 };
    private static readonly int[] BossDropTierWeights = { 0, 10, 30, 40, 20 };

    // 아이템규칙.md의 등급/효과 방식과 기존 세트 구조를 함께 쓰는 프로토타입 데이터다.
    private static readonly JIN_SetDefinition[] Sets =
    {
        new JIN_SetDefinition("overcharge", "과충전 사격", "탄환 계열 발사 속도와 연쇄 화력을 강화한다.", new Color(0.18f, 0.72f, 1f)),
        new JIN_SetDefinition("survival", "생존 반격", "체력과 방어 기반 반격 능력을 강화한다.", new Color(0.24f, 0.86f, 0.42f)),
        new JIN_SetDefinition("dash", "질주 절단", "이동 속도와 기동형 공격을 강화한다.", new Color(1f, 0.83f, 0.22f)),
        new JIN_SetDefinition("greed", "탐욕 증폭", "경험치 획득과 성장 효율을 공격력으로 전환한다.", new Color(1f, 0.62f, 0.2f)),
        new JIN_SetDefinition("burn", "연소 확산", "장판, 폭발, 지속 피해 계열 화력을 강화한다.", new Color(1f, 0.32f, 0.24f)),
        new JIN_SetDefinition("control", "안정 제어", "둔화와 제어 대상 추가 피해를 강화한다.", new Color(0.58f, 0.64f, 1f))
    };

    private static readonly JIN_ItemDefinition[] Items =
    {
        new JIN_ItemDefinition("dopamine_bullet", "도파민 탄환", "overcharge", 1, JIN_ItemEffectKind.WeaponAdd, "D", "손끝이 먼저 방아쇠를 당긴다.", "공격 속도가 레벨마다 증가한다. 과충전 사격 세트의 기본 탄환 축이다.", JIN_ItemStatType.AttackSpeed, 0.08f),
        new JIN_ItemDefinition("double_shot", "더블샷", "overcharge", 2, JIN_ItemEffectKind.WeaponAdd, "2", "한 줄로는 부족하다.", "기본 공격을 같은 방향의 평행한 2개 공격으로 바꾼다.", JIN_ItemStatType.ProjectileDamage, 0f, 1),
        new JIN_ItemDefinition("chain_spark", "연쇄 스파크", "overcharge", 3, JIN_ItemEffectKind.WeaponAdd, "S", "한 번 맞으면 생각보다 오래 번진다.", "투사체 피해가 증가한다. 연쇄 전이 무기 컨셉으로 세트 완성 시 연계 화력이 커진다.", JIN_ItemStatType.ProjectileDamage, 2f),
        new JIN_ItemDefinition("split_round", "분열 탄환", "overcharge", 3, JIN_ItemEffectKind.WeaponAdd, "V", "맞은 자리가 다시 벌어진다.", "공격 적중 시 현재 무기 형태를 따른 약한 분열 공격이 일정 각도로 갈라져 다른 적을 노린다.", JIN_ItemStatType.ProjectileDamage, 0f, 1),
        new JIN_ItemDefinition("triple_shot", "트리플샷", "overcharge", 3, JIN_ItemEffectKind.WeaponAdd, "3", "정면이 조금 넓어진다.", "기본 공격을 부채꼴로 퍼지는 3개 공격으로 바꾼다.", JIN_ItemStatType.ProjectileDamage, 0f, 1),
        new JIN_ItemDefinition("focus_lens", "집중 렌즈", "overcharge", 4, JIN_ItemEffectKind.WeaponAdd, "L", "빛은 줄을 서지 않는다.", "투사체 속도가 증가한다. 관통 광선형 보조 무기로 빠른 정리력을 담당한다.", JIN_ItemStatType.ProjectileSpeed, 1.2f),
        new JIN_ItemDefinition("quad_shot", "쿼드샷", "overcharge", 4, JIN_ItemEffectKind.WeaponAdd, "4", "시야가 네 갈래로 찢어진다.", "기본 공격을 부채꼴로 퍼지는 4개 공격으로 바꾼다.", JIN_ItemStatType.ProjectileDamage, 0f, 1),
        new JIN_ItemDefinition("blood_cannon", "혈사포", "overcharge", 4, JIN_ItemEffectKind.WeaponAdd, "H", "눈물이 붉은 줄로 바뀐다.", "기본 공격을 충전형 관통 혈사포로 바꾼다. 과충전 사격 세트가 발동하면 유도 혈사포가 된다.", JIN_ItemStatType.ProjectileDamage, 0f, 1),
        new JIN_ItemDefinition("impact_shield", "충격 방패", "survival", 3, JIN_ItemEffectKind.Passive, "I", "맞는 순간에도 되갚을 시간이 있다.", "최대 체력이 크게 증가한다. 보호막과 반격 충격파 컨셉의 생존 장비다.", JIN_ItemStatType.MaxHealth, 12f),
        new JIN_ItemDefinition("recovery_syringe", "회복 주사기", "survival", 2, JIN_ItemEffectKind.StatIncrease, "R", "심장은 아직 협상 중이다.", "최대 체력이 증가한다. 회복과 과치유 보호막으로 확장되는 방어형 아이템이다.", JIN_ItemStatType.MaxHealth, 10f),
        new JIN_ItemDefinition("thorn_core", "가시 코어", "survival", 3, JIN_ItemEffectKind.WeaponAdd, "T", "가까이 온 쪽이 먼저 후회한다.", "투사체 피해가 증가한다. 주변을 도는 방어 무기 컨셉으로 근접 압박에 대응한다.", JIN_ItemStatType.ProjectileDamage, 1.5f),
        new JIN_ItemDefinition("jet_booster", "제트 부스터", "dash", 1, JIN_ItemEffectKind.StatIncrease, "J", "발밑이 늦게 따라온다.", "이동 속도가 레벨마다 증가한다. 질주 절단 세트의 진입과 이탈을 돕는다.", JIN_ItemStatType.MoveSpeed, 0.35f),
        new JIN_ItemDefinition("evade_blade", "회피 칼날", "dash", 2, JIN_ItemEffectKind.WeaponAdd, "B", "피하는 김에 베어낸다.", "투사체 속도가 증가한다. 이동 방향 기반 칼날 투사체 컨셉의 기동 공격 무기다.", JIN_ItemStatType.ProjectileSpeed, 0.9f),
        new JIN_ItemDefinition("afterimage_generator", "잔상 발생기", "dash", 4, JIN_ItemEffectKind.Passive, "A", "남겨둔 그림자가 대신 시선을 끈다.", "이동 속도가 증가한다. 잔상 유도와 폭발로 이어지는 패시브 기동 장치다.", JIN_ItemStatType.MoveSpeed, 0.25f),
        new JIN_ItemDefinition("xp_magnet", "경험치 자석", "greed", 1, JIN_ItemEffectKind.StatIncrease, "X", "바닥의 작은 빛까지 욕심낸다.", "경험치 획득 효율이 증가한다. 탐욕 증폭 세트의 성장 속도를 끌어올린다.", JIN_ItemStatType.ExperienceGain, 0.1f),
        new JIN_ItemDefinition("coin_drone", "코인 드론", "greed", 3, JIN_ItemEffectKind.WeaponAdd, "C", "돈 냄새가 나는 방향으로 총구가 돈다.", "투사체 피해가 증가한다. 자원 획득량을 화력으로 바꾸는 드론 무기다.", JIN_ItemStatType.ProjectileDamage, 1.8f),
        new JIN_ItemDefinition("lucky_battery", "행운 배터리", "greed", 5, JIN_ItemEffectKind.RandomOption, "K", "운이 과충전되면 가끔 과열된다.", "공격 속도가 증가한다. 보상 품질과 무작위 옵션 확장에 연결되는 고티어 아이템이다.", JIN_ItemStatType.AttackSpeed, 0.05f),
        new JIN_ItemDefinition("fire_bottle", "화염 병", "burn", 2, JIN_ItemEffectKind.WeaponAdd, "F", "던진 자리에는 오래 남는 대답이 있다.", "투사체 피해가 증가한다. 적 위치에 화염 장판을 만드는 범위 무기다.", JIN_ItemStatType.ProjectileDamage, 2.2f),
        new JIN_ItemDefinition("explosive_capsule", "폭발 캡슐", "burn", 4, JIN_ItemEffectKind.WeaponAdd, "E", "작은 껍질 안에 큰 마침표가 있다.", "투사체 피해가 크게 증가한다. 처치 흐름을 폭발로 전환하는 고화력 장비다.", JIN_ItemStatType.ProjectileDamage, 2.4f),
        new JIN_ItemDefinition("toxic_filter", "독성 필터", "burn", 3, JIN_ItemEffectKind.Passive, "P", "숨을 고를수록 적이 더 느리게 무너진다.", "투사체 피해가 증가한다. 독 중첩과 약화로 지속 피해를 보조한다.", JIN_ItemStatType.ProjectileDamage, 1.7f),
        new JIN_ItemDefinition("cooling_ring", "냉각 링", "control", 2, JIN_ItemEffectKind.Passive, "N", "뜨거운 전장은 차갑게 접힌다.", "최대 체력이 증가한다. 주변 냉기 파동으로 접근을 늦추는 제어 장비다.", JIN_ItemStatType.MaxHealth, 8f),
        new JIN_ItemDefinition("guidance_chip", "유도 칩", "control", 3, JIN_ItemEffectKind.Passive, "Y", "빗나간 길도 다시 적을 찾는다.", "모든 기본 공격에 유도 보정을 추가한다. 혈사포 세트 유도와 별개로 일반 투사체와 혈사포 모두에 적용된다.", JIN_ItemStatType.ProjectileSpeed, 0f, 1),
        new JIN_ItemDefinition("gravity_nail", "중력 못", "control", 5, JIN_ItemEffectKind.WeaponAdd, "G", "못 하나가 방 전체의 방향을 바꾼다.", "투사체 피해가 증가한다. 적을 끌어당기는 중력장 무기 컨셉의 고티어 장비다.", JIN_ItemStatType.ProjectileDamage, 1.9f),
        new JIN_ItemDefinition("discharge_coil", "방전 코일", "control", 4, JIN_ItemEffectKind.Passive, "O", "사람 많은 곳에서는 늘 번개가 친다.", "공격 속도가 증가한다. 적이 모였을 때 원형 충격을 방출하는 반격 장치다.", JIN_ItemStatType.AttackSpeed, 0.06f)
    };

    private static readonly Dictionary<string, JIN_ItemDefinition> ItemById = BuildItemMap();
    private static readonly Dictionary<string, JIN_SetDefinition> SetById = BuildSetMap();

    public static IReadOnlyList<JIN_ItemDefinition> AllItems => Items;
    public static IReadOnlyList<JIN_SetDefinition> AllSets => Sets;
    public static IReadOnlyList<int> LevelUpRewardTierWeights => LevelUpTierWeights;
    public static IReadOnlyList<int> BossRewardTierWeights => BossDropTierWeights;

    public static bool TryGetItem(string itemId, out JIN_ItemDefinition definition)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            definition = null;
            return false;
        }

        return ItemById.TryGetValue(itemId, out definition);
    }

    public static bool TryGetSet(string setId, out JIN_SetDefinition definition)
    {
        if (string.IsNullOrEmpty(setId))
        {
            definition = null;
            return false;
        }

        return SetById.TryGetValue(setId, out definition);
    }

    public static JIN_SetDefinition GetSetOrNull(string setId)
    {
        return TryGetSet(setId, out JIN_SetDefinition definition) ? definition : null;
    }

    public static Color GetTierColor(int tier)
    {
        switch (Mathf.Clamp(tier, MinTier, MaxTier))
        {
            case 1:
                return new Color(0.78f, 0.82f, 0.86f);
            case 2:
                return new Color(0.38f, 0.9f, 0.52f);
            case 3:
                return new Color(0.36f, 0.68f, 1f);
            case 4:
                return new Color(0.82f, 0.48f, 1f);
            case 5:
                return new Color(1f, 0.78f, 0.24f);
            default:
                return Color.white;
        }
    }

    public static int GetTierWeight(int tier, JIN_ItemRewardSource rewardSource)
    {
        int[] weights = ResolveTierWeights(rewardSource);
        int clampedTier = Mathf.Clamp(tier, MinTier, MaxTier);
        return weights[clampedTier - 1];
    }

    public static string GetRewardSourceLabel(JIN_ItemRewardSource rewardSource)
    {
        switch (rewardSource)
        {
            case JIN_ItemRewardSource.BossDrop:
                return "보스 상자";
            default:
                return "레벨업";
        }
    }

    public static List<JIN_ItemDefinition> GetRewardChoices(JIN_PlayerItemInventory inventory, int count)
    {
        return GetRewardChoices(inventory, count, JIN_ItemRewardSource.LevelUp);
    }

    public static List<JIN_ItemDefinition> GetBossRewardChoices(JIN_PlayerItemInventory inventory, int count)
    {
        return GetRewardChoices(inventory, count, JIN_ItemRewardSource.BossDrop);
    }

    public static List<JIN_ItemDefinition> GetRewardChoices(
        JIN_PlayerItemInventory inventory,
        int count,
        JIN_ItemRewardSource rewardSource)
    {
        List<JIN_ItemDefinition> pool = BuildAvailableRewardPool(inventory);
        List<JIN_ItemDefinition> choices = new List<JIN_ItemDefinition>();
        int desiredCount = Mathf.Clamp(count, 1, Items.Length);
        int[] tierWeights = ResolveTierWeights(rewardSource);

        while (choices.Count < desiredCount && pool.Count > 0)
        {
            JIN_ItemDefinition selectedItem = DrawWeightedItem(pool, tierWeights);
            choices.Add(selectedItem);
            pool.Remove(selectedItem);
        }

        return choices;
    }

    private static List<JIN_ItemDefinition> BuildAvailableRewardPool(JIN_PlayerItemInventory inventory)
    {
        List<JIN_ItemDefinition> pool = new List<JIN_ItemDefinition>();

        // 최대 레벨 아이템은 보상 후보에서 제외해 선택지가 낭비되지 않게 한다.
        foreach (JIN_ItemDefinition item in Items)
        {
            int ownedLevel = inventory != null ? inventory.GetItemLevel(item.Id) : 0;

            if (ownedLevel < item.MaxLevel)
            {
                pool.Add(item);
            }
        }

        if (pool.Count == 0)
        {
            pool.AddRange(Items);
        }

        return pool;
    }

    private static JIN_ItemDefinition DrawWeightedItem(List<JIN_ItemDefinition> pool, int[] tierWeights)
    {
        int totalWeight = 0;

        for (int tier = MinTier; tier <= MaxTier; tier++)
        {
            int tierWeight = tierWeights[tier - 1];

            if (tierWeight <= 0 || !HasTierCandidate(pool, tier))
            {
                continue;
            }

            totalWeight += tierWeight;
        }

        if (totalWeight <= 0)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        int roll = Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        // 먼저 등급을 확률로 뽑고, 같은 등급 안에서는 균등 추첨한다.
        for (int tier = MinTier; tier <= MaxTier; tier++)
        {
            int tierWeight = tierWeights[tier - 1];

            if (tierWeight <= 0 || !HasTierCandidate(pool, tier))
            {
                continue;
            }

            cumulativeWeight += tierWeight;

            if (roll < cumulativeWeight)
            {
                return GetRandomItemInTier(pool, tier);
            }
        }

        return pool[Random.Range(0, pool.Count)];
    }

    private static bool HasTierCandidate(List<JIN_ItemDefinition> pool, int tier)
    {
        foreach (JIN_ItemDefinition item in pool)
        {
            if (item.Tier == tier)
            {
                return true;
            }
        }

        return false;
    }

    private static JIN_ItemDefinition GetRandomItemInTier(List<JIN_ItemDefinition> pool, int tier)
    {
        List<JIN_ItemDefinition> tierItems = new List<JIN_ItemDefinition>();

        foreach (JIN_ItemDefinition item in pool)
        {
            if (item.Tier == tier)
            {
                tierItems.Add(item);
            }
        }

        if (tierItems.Count == 0)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        return tierItems[Random.Range(0, tierItems.Count)];
    }

    private static int[] ResolveTierWeights(JIN_ItemRewardSource rewardSource)
    {
        return rewardSource == JIN_ItemRewardSource.BossDrop
            ? BossDropTierWeights
            : LevelUpTierWeights;
    }

    private static Dictionary<string, JIN_ItemDefinition> BuildItemMap()
    {
        Dictionary<string, JIN_ItemDefinition> map = new Dictionary<string, JIN_ItemDefinition>();

        foreach (JIN_ItemDefinition item in Items)
        {
            map[item.Id] = item;
        }

        return map;
    }

    private static Dictionary<string, JIN_SetDefinition> BuildSetMap()
    {
        Dictionary<string, JIN_SetDefinition> map = new Dictionary<string, JIN_SetDefinition>();

        foreach (JIN_SetDefinition set in Sets)
        {
            map[set.Id] = set;
        }

        return map;
    }
}

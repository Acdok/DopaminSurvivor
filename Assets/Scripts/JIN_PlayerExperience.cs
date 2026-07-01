using System;
using UnityEngine;

[DisallowMultipleComponent]
public class JIN_PlayerExperience : MonoBehaviour
{
    [Header("Level")]
    [SerializeField, Min(1)]
    private int startingLevel = 1;

    [Header("Experience Curve")]
    [SerializeField, Min(1)]
    private int baseExperienceToLevel = 4;

    [SerializeField, Min(1f)]
    private float requirementGrowth = 1.45f;

    [SerializeField, Min(1)]
    private int requirementAddPerLevel = 2;

    private int level;
    private int currentExperience;
    private int experienceToNextLevel;
    private int pendingRewardCount;
    private float experienceGainMultiplier = 1f;

    public event Action<JIN_PlayerExperience> ExperienceChanged;
    public event Action<JIN_PlayerExperience> LevelChanged;
    public event Action<JIN_PlayerExperience> RewardQueued;

    public int Level => level;
    public int CurrentExperience => currentExperience;
    public int ExperienceToNextLevel => experienceToNextLevel;
    public int PendingRewardCount => pendingRewardCount;
    public float Progress => experienceToNextLevel <= 0 ? 0f : Mathf.Clamp01((float)currentExperience / experienceToNextLevel);

    public float ExperienceGainMultiplier
    {
        get => experienceGainMultiplier;
        set => experienceGainMultiplier = Mathf.Max(0.1f, IsFinite(value) ? value : 1f);
    }

    private void Awake()
    {
        level = Mathf.Max(1, startingLevel);
        currentExperience = 0;
        experienceToNextLevel = CalculateRequirement(level);
        pendingRewardCount = 0;
    }

    private void OnValidate()
    {
        startingLevel = Mathf.Max(1, startingLevel);
        baseExperienceToLevel = Mathf.Max(1, baseExperienceToLevel);
        requirementGrowth = Mathf.Max(1f, requirementGrowth);
        requirementAddPerLevel = Mathf.Max(1, requirementAddPerLevel);
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int gainedExperience = Mathf.Max(1, Mathf.RoundToInt(amount * experienceGainMultiplier));
        currentExperience += gainedExperience;

        // 한 번에 많이 먹었을 때도 레벨업 보상이 밀리지 않도록 큐에 쌓는다.
        while (currentExperience >= experienceToNextLevel)
        {
            currentExperience -= experienceToNextLevel;
            level++;
            pendingRewardCount++;
            experienceToNextLevel = CalculateRequirement(level);
            LevelChanged?.Invoke(this);
            RewardQueued?.Invoke(this);
        }

        ExperienceChanged?.Invoke(this);
    }

    public bool ConsumePendingReward()
    {
        if (pendingRewardCount <= 0)
        {
            return false;
        }

        pendingRewardCount--;
        ExperienceChanged?.Invoke(this);
        return true;
    }

    private int CalculateRequirement(int targetLevel)
    {
        int levelIndex = Mathf.Max(0, targetLevel - 1);
        float scaledRequirement = baseExperienceToLevel * Mathf.Pow(requirementGrowth, levelIndex);
        return Mathf.Max(1, Mathf.RoundToInt(scaledRequirement) + (levelIndex * requirementAddPerLevel));
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

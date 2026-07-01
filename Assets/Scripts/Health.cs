using System;
using UnityEngine;

/// <summary>
/// Shared HP component for actors such as the Player and Enemies.
/// </summary>
[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [SerializeField, Min(1f)]
    private float maxHealth = 100f;

    private float currentHealth;
    private float maxHealthBonus;
    private bool isDead;

    /// <summary>
    /// Raised whenever current or maximum health changes while this component is alive.
    /// </summary>
    public event Action<Health> HealthChanged;

    /// <summary>
    /// Raised once when this component transitions from alive to dead.
    /// </summary>
    public event Action<Health> Died;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => Mathf.Max(1f, maxHealth + maxHealthBonus);
    public bool IsAlive => !isDead;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = MaxHealth;
        isDead = false;
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);

        if (currentHealth > MaxHealth)
        {
            currentHealth = MaxHealth;
        }
    }

    /// <summary>
    /// Applies finite positive damage. Invalid or non-positive values are ignored.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
        {
            return;
        }

        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        if (!Mathf.Approximately(previousHealth, currentHealth))
        {
            HealthChanged?.Invoke(this);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Restores finite positive health without exceeding the current maximum health.
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
        {
            return;
        }

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, MaxHealth);

        if (!Mathf.Approximately(previousHealth, currentHealth))
        {
            HealthChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// Applies runtime maximum-health bonuses from item or buff systems without changing the authored base value.
    /// </summary>
    public void SetMaxHealthBonus(float bonus, bool healAddedCapacity)
    {
        float previousMaxHealth = MaxHealth;
        maxHealthBonus = Mathf.Max(0f, IsFinite(bonus) ? bonus : 0f);
        float newMaxHealth = MaxHealth;

        if (healAddedCapacity && newMaxHealth > previousMaxHealth)
        {
            currentHealth += newMaxHealth - previousMaxHealth;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0f, newMaxHealth);
        HealthChanged?.Invoke(this);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentHealth = 0f;
        HealthChanged?.Invoke(this);
        Died?.Invoke(this);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

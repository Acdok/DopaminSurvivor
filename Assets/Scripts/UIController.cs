using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small HUD controller for the prototype combat scene.
/// </summary>
[DisallowMultipleComponent]
public class UIController : MonoBehaviour
{
    private const string DefaultHealthFormat = "HP {0:0}/{1:0}";
    private const string DefaultSurvivalTimeFormat = "Time {0:00}:{1:00}";

    [Header("Required References")]
    [SerializeField]
    private Health playerHealth;

    [Header("Optional Game State")]
    [SerializeField]
    private GameManager gameManager;

    [Header("Health UI")]
    [SerializeField]
    private TextTarget healthText = new TextTarget();

    [SerializeField]
    private string healthFormat = DefaultHealthFormat;

    [Header("Survival Time UI")]
    [SerializeField]
    private bool showSurvivalTime = true;

    [SerializeField]
    private TextTarget survivalTimeText = new TextTarget();

    [SerializeField]
    private string survivalTimeFormat = DefaultSurvivalTimeFormat;

    [Header("Game Over UI")]
    [SerializeField]
    private GameObject gameOverPanel;

    [SerializeField]
    private TextTarget gameOverText = new TextTarget();

    [SerializeField]
    private string gameOverMessage = "GAME OVER";

    [SerializeField]
    private TextTarget restartHintText = new TextTarget();

    [SerializeField]
    private string restartHintMessage = "Press R to Restart";

    private bool hasCachedHealthValues;
    private float cachedCurrentHealth;
    private float cachedMaxHealth;
    private int cachedSurvivalSeconds = -1;
    private bool isGameOverVisible;
    private bool survivalTimeStopped;
    private float survivalStartTime;
    private float finalSurvivalSeconds;

    private bool warnedMissingPlayerHealth;
    private bool warnedMissingHealthText;
    private bool warnedMissingGameOverUi;
    private bool warnedInvalidHealthFormat;
    private bool warnedInvalidSurvivalTimeFormat;
    private bool warnedInvalidTextTarget;

    private void Awake()
    {
        survivalStartTime = Time.time;
    }

    private void OnEnable()
    {
        // Pair event subscriptions with OnDisable so disabled UI objects do not keep stale handlers.
        SubscribeToGameManager();
        SubscribeToPlayerHealth();
    }

    private void Start()
    {
        WarnIfPlayerHealthMissing();
        WarnIfHealthTextMissing();

        RefreshHealthUI(true);
        RefreshSurvivalTimeUI(true);
        SetGameOverVisible(IsGameOverState(), true);
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
        UnsubscribeFromPlayerHealth();
    }

    private void Update()
    {
        RefreshHealthUI(false);
        SetGameOverVisible(IsGameOverState());
        RefreshSurvivalTimeUI(false);
    }

    private void SubscribeToGameManager()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.GameOver += HandleGameOver;
    }

    private void UnsubscribeFromGameManager()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.GameOver -= HandleGameOver;
    }

    private void SubscribeToPlayerHealth()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.Died += HandlePlayerDied;
    }

    private void UnsubscribeFromPlayerHealth()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.Died -= HandlePlayerDied;
    }

    private void HandleGameOver()
    {
        SetGameOverVisible(true);
        RefreshSurvivalTimeUI(true);
    }

    private void HandlePlayerDied(Health health)
    {
        SetGameOverVisible(true);
        RefreshHealthUI(true);
        RefreshSurvivalTimeUI(true);
    }

    private void RefreshHealthUI(bool force)
    {
        if (playerHealth == null)
        {
            WarnIfPlayerHealthMissing();
            return;
        }

        float currentHealth = playerHealth.CurrentHealth;
        float maxHealth = playerHealth.MaxHealth;

        // Health currently exposes only a death event, so HP text watches values without modifying Health.cs.
        if (!force
            && hasCachedHealthValues
            && Mathf.Approximately(currentHealth, cachedCurrentHealth)
            && Mathf.Approximately(maxHealth, cachedMaxHealth))
        {
            return;
        }

        hasCachedHealthValues = true;
        cachedCurrentHealth = currentHealth;
        cachedMaxHealth = maxHealth;

        if (!healthText.HasTarget)
        {
            WarnIfHealthTextMissing();
            return;
        }

        healthText.SetText(FormatHealth(currentHealth, maxHealth), this, ref warnedInvalidTextTarget);
    }

    private void RefreshSurvivalTimeUI(bool force)
    {
        if (!showSurvivalTime)
        {
            survivalTimeText.SetVisible(false);
            return;
        }

        if (!survivalTimeText.HasTarget)
        {
            return;
        }

        survivalTimeText.SetVisible(true);

        // Survival time is optional and freezes at the first observed game-over state.
        float totalSeconds = survivalTimeStopped ? finalSurvivalSeconds : Time.time - survivalStartTime;
        int wholeSeconds = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));

        if (!force && wholeSeconds == cachedSurvivalSeconds)
        {
            return;
        }

        cachedSurvivalSeconds = wholeSeconds;
        survivalTimeText.SetText(FormatSurvivalTime(wholeSeconds, totalSeconds), this, ref warnedInvalidTextTarget);
    }

    private void SetGameOverVisible(bool visible, bool force = false)
    {
        if (!force && isGameOverVisible == visible)
        {
            return;
        }

        isGameOverVisible = visible;

        if (visible && !survivalTimeStopped)
        {
            finalSurvivalSeconds = Mathf.Max(0f, Time.time - survivalStartTime);
            survivalTimeStopped = true;
        }
        else if (!visible)
        {
            survivalTimeStopped = false;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(visible);
        }

        gameOverText.SetText(gameOverMessage, this, ref warnedInvalidTextTarget);
        restartHintText.SetText(restartHintMessage, this, ref warnedInvalidTextTarget);
        gameOverText.SetVisible(visible);
        restartHintText.SetVisible(visible);

        if (visible)
        {
            WarnIfGameOverUiMissing();
        }
    }

    private bool IsGameOverState()
    {
        if (gameManager != null && gameManager.IsGameOver)
        {
            return true;
        }

        return playerHealth != null && !playerHealth.IsAlive;
    }

    private string FormatHealth(float currentHealth, float maxHealth)
    {
        string format = string.IsNullOrEmpty(healthFormat) ? DefaultHealthFormat : healthFormat;

        try
        {
            return string.Format(format, currentHealth, maxHealth);
        }
        catch (FormatException)
        {
            WarnIfInvalidFormat(nameof(healthFormat), ref warnedInvalidHealthFormat);
            return string.Format(DefaultHealthFormat, currentHealth, maxHealth);
        }
    }

    private string FormatSurvivalTime(int wholeSeconds, float totalSeconds)
    {
        string format = string.IsNullOrEmpty(survivalTimeFormat)
            ? DefaultSurvivalTimeFormat
            : survivalTimeFormat;
        int minutes = wholeSeconds / 60;
        int seconds = wholeSeconds % 60;

        try
        {
            return string.Format(format, minutes, seconds, totalSeconds);
        }
        catch (FormatException)
        {
            WarnIfInvalidFormat(nameof(survivalTimeFormat), ref warnedInvalidSurvivalTimeFormat);
            return string.Format(DefaultSurvivalTimeFormat, minutes, seconds);
        }
    }

    private void WarnIfPlayerHealthMissing()
    {
        if (playerHealth != null || warnedMissingPlayerHealth)
        {
            return;
        }

        warnedMissingPlayerHealth = true;
        Debug.LogWarning($"{nameof(UIController)} on {name} has no Player Health assigned.", this);
    }

    private void WarnIfHealthTextMissing()
    {
        if (healthText.HasTarget || warnedMissingHealthText)
        {
            return;
        }

        warnedMissingHealthText = true;
        Debug.LogWarning($"{nameof(UIController)} on {name} has no Health Text assigned.", this);
    }

    private void WarnIfGameOverUiMissing()
    {
        if (gameOverPanel != null
            || gameOverText.HasTarget
            || restartHintText.HasTarget
            || warnedMissingGameOverUi)
        {
            return;
        }

        warnedMissingGameOverUi = true;
        Debug.LogWarning($"{nameof(UIController)} on {name} has no Game Over UI assigned.", this);
    }

    private void WarnIfInvalidFormat(string fieldName, ref bool warned)
    {
        if (warned)
        {
            return;
        }

        warned = true;
        Debug.LogWarning($"{nameof(UIController)} on {name} has an invalid {fieldName}; using default text.", this);
    }

    [Serializable]
    private sealed class TextTarget
    {
        [SerializeField, Tooltip("Optional TextMeshPro TMP_Text reference. Stored as Component to avoid a hard package dependency.")]
        private Component tmpText;

        [SerializeField, Tooltip("Optional legacy UnityEngine.UI.Text fallback.")]
        private Text uiText;

        [NonSerialized]
        private Type cachedTextType;

        [NonSerialized]
        private PropertyInfo cachedTextProperty;

        public bool HasTarget => tmpText != null || uiText != null;

        public void SetText(string value, UnityEngine.Object context, ref bool warnedInvalidTextTarget)
        {
            // TMP_Text is preferred when assigned, while legacy Text keeps older UI prefabs usable.
            if (tmpText != null)
            {
                TrySetComponentText(tmpText, value, context, ref warnedInvalidTextTarget);
            }

            if (uiText != null)
            {
                uiText.text = value;
            }
        }

        public void SetVisible(bool visible)
        {
            if (tmpText != null)
            {
                tmpText.gameObject.SetActive(visible);
            }

            if (uiText != null)
            {
                uiText.gameObject.SetActive(visible);
            }
        }

        private bool TrySetComponentText(
            Component textComponent,
            string value,
            UnityEngine.Object context,
            ref bool warnedInvalidTextTarget)
        {
            PropertyInfo textProperty = GetTextProperty(textComponent.GetType());

            if (textProperty == null)
            {
                WarnInvalidTextTarget(textComponent, context, ref warnedInvalidTextTarget);
                return false;
            }

            try
            {
                textProperty.SetValue(textComponent, value, null);
                return true;
            }
            catch (Exception)
            {
                WarnInvalidTextTarget(textComponent, context, ref warnedInvalidTextTarget);
                return false;
            }
        }

        private PropertyInfo GetTextProperty(Type textType)
        {
            if (cachedTextType == textType)
            {
                return cachedTextProperty;
            }

            cachedTextType = textType;
            cachedTextProperty = textType.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);

            if (cachedTextProperty == null
                || !cachedTextProperty.CanWrite
                || cachedTextProperty.PropertyType != typeof(string)
                || cachedTextProperty.GetIndexParameters().Length > 0)
            {
                cachedTextProperty = null;
            }

            return cachedTextProperty;
        }

        private void WarnInvalidTextTarget(
            Component textComponent,
            UnityEngine.Object context,
            ref bool warnedInvalidTextTarget)
        {
            if (warnedInvalidTextTarget)
            {
                return;
            }

            warnedInvalidTextTarget = true;
            Debug.LogWarning(
                $"{nameof(UIController)} on {context.name} has {textComponent.GetType().Name} assigned where a text component was expected.",
                context);
        }
    }
}

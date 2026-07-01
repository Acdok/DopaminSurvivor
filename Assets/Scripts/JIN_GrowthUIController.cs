using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class JIN_GrowthUIController : MonoBehaviour
{
    [SerializeField]
    private JIN_PlayerExperience playerExperience;

    private GameObject rootObject;
    private Image experienceFill;
    private Text levelText;
    private Text experienceText;

    private void Awake()
    {
        ResolveReferences();
        EnsureUI();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Configure(JIN_PlayerExperience newPlayerExperience)
    {
        Unsubscribe();
        playerExperience = newPlayerExperience;
        Subscribe();
        Refresh();
    }

    private void ResolveReferences()
    {
        if (playerExperience == null)
        {
            playerExperience = FindAnyObjectByType<JIN_PlayerExperience>();
        }
    }

    private void Subscribe()
    {
        if (playerExperience == null)
        {
            return;
        }

        playerExperience.ExperienceChanged -= HandleExperienceChanged;
        playerExperience.LevelChanged -= HandleExperienceChanged;
        playerExperience.ExperienceChanged += HandleExperienceChanged;
        playerExperience.LevelChanged += HandleExperienceChanged;
    }

    private void Unsubscribe()
    {
        if (playerExperience == null)
        {
            return;
        }

        playerExperience.ExperienceChanged -= HandleExperienceChanged;
        playerExperience.LevelChanged -= HandleExperienceChanged;
    }

    private void HandleExperienceChanged(JIN_PlayerExperience changedExperience)
    {
        Refresh();
    }

    private void EnsureUI()
    {
        if (rootObject != null)
        {
            return;
        }

        // 기존 HUD가 없어도 XP 바를 바로 확인할 수 있게 런타임 UI를 생성한다.
        Canvas canvas = JIN_UIUtility.ResolveOrCreateCanvas();
        rootObject = new GameObject("JIN_ExperienceHud");
        rootObject.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = rootObject.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 24f);
        rootRect.sizeDelta = new Vector2(520f, 54f);

        Image background = rootObject.AddComponent<Image>();
        background.sprite = JIN_RuntimeSpriteUtility.WhiteSprite;
        background.color = new Color(0.03f, 0.04f, 0.06f, 0.86f);

        Image fillBackground = JIN_UIUtility.CreateImage(rootObject.transform, "XP Fill", new Color(0.18f, 0.86f, 0.96f, 0.95f));
        experienceFill = fillBackground;
        experienceFill.type = Image.Type.Filled;
        experienceFill.fillMethod = Image.FillMethod.Horizontal;
        experienceFill.fillOrigin = 0;

        RectTransform fillRect = experienceFill.GetComponent<RectTransform>();
        JIN_UIUtility.Stretch(fillRect);
        fillRect.offsetMin = new Vector2(8f, 8f);
        fillRect.offsetMax = new Vector2(-8f, -8f);

        levelText = JIN_UIUtility.CreateText(rootObject.transform, "LevelText", "LV 1", 22, Color.white, TextAnchor.MiddleLeft);
        RectTransform levelRect = levelText.GetComponent<RectTransform>();
        JIN_UIUtility.Stretch(levelRect);
        levelRect.offsetMin = new Vector2(18f, 0f);
        levelRect.offsetMax = new Vector2(-360f, 0f);

        experienceText = JIN_UIUtility.CreateText(rootObject.transform, "ExperienceText", "0 / 4", 20, Color.white, TextAnchor.MiddleRight);
        RectTransform experienceRect = experienceText.GetComponent<RectTransform>();
        JIN_UIUtility.Stretch(experienceRect);
        experienceRect.offsetMin = new Vector2(150f, 0f);
        experienceRect.offsetMax = new Vector2(-18f, 0f);
    }

    private void Refresh()
    {
        EnsureUI();

        if (playerExperience == null)
        {
            levelText.text = "LV -";
            experienceText.text = "0 / 0";
            experienceFill.fillAmount = 0f;
            return;
        }

        levelText.text = $"LV {playerExperience.Level}";
        experienceText.text = $"{playerExperience.CurrentExperience} / {playerExperience.ExperienceToNextLevel}";
        experienceFill.fillAmount = playerExperience.Progress;
    }
}

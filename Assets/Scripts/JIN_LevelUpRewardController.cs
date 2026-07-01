using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class JIN_LevelUpRewardController : MonoBehaviour
{
    [SerializeField]
    private JIN_PlayerExperience playerExperience;

    [SerializeField]
    private JIN_PlayerItemInventory inventory;

    [SerializeField]
    private GameManager gameManager;

    [SerializeField, Range(1, 3)]
    private int choiceCount = 3;

    [SerializeField]
    private bool pauseWhileChoosing = true;

    private readonly List<JIN_ItemDefinition> currentChoices = new List<JIN_ItemDefinition>();
    private readonly List<Button> choiceButtons = new List<Button>();
    private readonly List<Text> choiceTexts = new List<Text>();
    private readonly List<RectTransform> choiceRects = new List<RectTransform>();

    private Canvas canvas;
    private GameObject panelRoot;
    private float previousTimeScale = 1f;
    private bool isChoosing;
    private bool consumePendingRewardOnSelect;
    private JIN_ItemRewardSource currentRewardSource = JIN_ItemRewardSource.LevelUp;

    private void Awake()
    {
        ResolveReferences();
        EnsureUI();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        TryOpenNextReward();
    }

    private void Update()
    {
        if (!isChoosing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SelectChoice(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SelectChoice(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SelectChoice(2);
        }

        TrySelectChoiceByMouse();
    }

    private void OnDisable()
    {
        Unsubscribe();
        CloseRewardPanel();
    }

    public void Configure(
        JIN_PlayerExperience newPlayerExperience,
        JIN_PlayerItemInventory newInventory,
        GameManager newGameManager)
    {
        Unsubscribe();
        playerExperience = newPlayerExperience;
        inventory = newInventory;
        gameManager = newGameManager;
        ResolveReferences();
        Subscribe();
        TryOpenNextReward();
    }

    public void OpenBossChestReward()
    {
        TryOpenBossChestReward(choiceCount);
    }

    public bool TryOpenBossChestReward(int rewardChoiceCount)
    {
        ResolveReferences();

        if (isChoosing || inventory == null)
        {
            return false;
        }

        if (gameManager != null && gameManager.IsGameOver)
        {
            return false;
        }

        // 실제 보스 시스템이 붙기 전에는 외부에서 이 메서드만 호출해 상자 보상을 검증한다.
        OpenRewardPanel(JIN_ItemRewardSource.BossDrop, rewardChoiceCount, false);
        return true;
    }

    private void ResolveReferences()
    {
        if (playerExperience == null)
        {
            playerExperience = FindAnyObjectByType<JIN_PlayerExperience>();
        }

        if (inventory == null)
        {
            inventory = FindAnyObjectByType<JIN_PlayerItemInventory>();
        }

        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    private void Subscribe()
    {
        if (playerExperience != null)
        {
            playerExperience.RewardQueued -= HandleRewardQueued;
            playerExperience.RewardQueued += HandleRewardQueued;
        }

        if (gameManager != null)
        {
            gameManager.GameOver -= HandleGameOver;
            gameManager.GameOver += HandleGameOver;
        }
    }

    private void Unsubscribe()
    {
        if (playerExperience != null)
        {
            playerExperience.RewardQueued -= HandleRewardQueued;
        }

        if (gameManager != null)
        {
            gameManager.GameOver -= HandleGameOver;
        }
    }

    private void HandleRewardQueued(JIN_PlayerExperience changedExperience)
    {
        TryOpenNextReward();
    }

    private void HandleGameOver()
    {
        CloseRewardPanel();
    }

    private void EnsureUI()
    {
        if (panelRoot != null)
        {
            return;
        }

        JIN_UIUtility.EnsureLegacyEventSystem();
        canvas = JIN_UIUtility.ResolveOrCreateCanvas();

        panelRoot = new GameObject("JIN_LevelUpRewardPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = panelRoot.AddComponent<RectTransform>();
        JIN_UIUtility.Stretch(rootRect);

        Image dimImage = panelRoot.AddComponent<Image>();
        dimImage.sprite = JIN_RuntimeSpriteUtility.WhiteSprite;
        dimImage.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(panelRoot.transform, false);
        RectTransform contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(900f, 360f);

        HorizontalLayoutGroup layoutGroup = contentObject.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 18f;
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = true;

        for (int i = 0; i < 3; i++)
        {
            CreateChoiceButton(contentObject.transform, i);
        }

        panelRoot.SetActive(false);
    }

    private void CreateChoiceButton(Transform parent, int index)
    {
        GameObject buttonObject = new GameObject($"Choice_{index + 1}");
        buttonObject.transform.SetParent(parent, false);

        Image background = buttonObject.AddComponent<Image>();
        background.sprite = JIN_RuntimeSpriteUtility.WhiteSprite;
        background.color = new Color(0.08f, 0.09f, 0.12f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        int choiceIndex = index;
        button.onClick.AddListener(() => SelectChoice(choiceIndex));

        Text text = JIN_UIUtility.CreateText(
            buttonObject.transform,
            "Text",
            string.Empty,
            25,
            Color.white,
            TextAnchor.MiddleCenter);

        RectTransform textRect = text.GetComponent<RectTransform>();
        JIN_UIUtility.Stretch(textRect);
        textRect.offsetMin = new Vector2(18f, 18f);
        textRect.offsetMax = new Vector2(-18f, -18f);

        choiceButtons.Add(button);
        choiceTexts.Add(text);
        choiceRects.Add(buttonObject.GetComponent<RectTransform>());
    }

    private void TrySelectChoiceByMouse()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Camera eventCamera = ResolveCanvasCamera();
        Vector2 mousePosition = Input.mousePosition;

        // EventSystem이 씬 설정 문제로 막혀도 선택창에서 빠져나올 수 있게 직접 클릭 영역을 검사한다.
        for (int i = 0; i < currentChoices.Count && i < choiceRects.Count; i++)
        {
            RectTransform choiceRect = choiceRects[i];

            if (choiceRect != null
                && choiceRect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(choiceRect, mousePosition, eventCamera))
            {
                SelectChoice(i);
                return;
            }
        }
    }

    private Camera ResolveCanvasCamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private void TryOpenNextReward()
    {
        if (isChoosing || playerExperience == null || inventory == null)
        {
            return;
        }

        if (gameManager != null && gameManager.IsGameOver)
        {
            return;
        }

        if (playerExperience.PendingRewardCount <= 0)
        {
            return;
        }

        OpenRewardPanel(JIN_ItemRewardSource.LevelUp, choiceCount, true);
    }

    private void OpenRewardPanel(
        JIN_ItemRewardSource rewardSource,
        int requestedChoiceCount,
        bool shouldConsumePendingReward)
    {
        EnsureUI();
        currentRewardSource = rewardSource;
        consumePendingRewardOnSelect = shouldConsumePendingReward;
        currentChoices.Clear();
        currentChoices.AddRange(JIN_ItemCatalog.GetRewardChoices(
            inventory,
            Mathf.Clamp(requestedChoiceCount, 1, choiceButtons.Count),
            rewardSource));

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            bool hasChoice = i < currentChoices.Count;
            choiceButtons[i].gameObject.SetActive(hasChoice);

            if (hasChoice)
            {
                ApplyChoiceTierColor(choiceButtons[i], currentChoices[i]);
                choiceTexts[i].text = BuildChoiceText(i, currentChoices[i]);
            }
        }

        isChoosing = true;
        panelRoot.SetActive(true);

        if (pauseWhileChoosing)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    private void ApplyChoiceTierColor(Button button, JIN_ItemDefinition item)
    {
        if (button == null || button.targetGraphic == null || item == null)
        {
            return;
        }

        Color tierColor = JIN_ItemCatalog.GetTierColor(item.Tier);
        Color cardColor = Color.Lerp(new Color(0.08f, 0.09f, 0.12f), tierColor, 0.22f);
        cardColor.a = 0.96f;
        button.targetGraphic.color = cardColor;
    }

    private string BuildChoiceText(int index, JIN_ItemDefinition item)
    {
        int currentLevel = inventory != null ? inventory.GetItemLevel(item.Id) : 0;
        string levelText = currentLevel > 0 ? $"Lv.{currentLevel} > Lv.{currentLevel + 1}" : "NEW";
        string setName = JIN_ItemCatalog.TryGetSet(item.SetId, out JIN_SetDefinition set)
            ? set.DisplayName
            : "세트 없음";
        string sourceLabel = JIN_ItemCatalog.GetRewardSourceLabel(currentRewardSource);

        return $"{index + 1}  <size=17>{sourceLabel}</size>\n\n{item.IconGlyph} {item.DisplayName}\n<size=17>{item.TierLabel} · {item.EffectKindLabel} · {setName}</size>\n<size=15><i>{item.FlavorText}</i></size>\n\n<size=16>{item.Description}</size>\n\n<size=20>{levelText}</size>";
    }

    private void SelectChoice(int index)
    {
        if (!isChoosing || index < 0 || index >= currentChoices.Count)
        {
            return;
        }

        JIN_ItemDefinition selectedItem = currentChoices[index];

        if (inventory != null)
        {
            inventory.AddItem(selectedItem.Id);
        }

        if (consumePendingRewardOnSelect && playerExperience != null)
        {
            playerExperience.ConsumePendingReward();
        }

        CloseRewardPanel();
        TryOpenNextReward();
    }

    private void CloseRewardPanel()
    {
        bool wasChoosing = isChoosing;

        if (!wasChoosing && panelRoot == null)
        {
            return;
        }

        if (pauseWhileChoosing && wasChoosing)
        {
            Time.timeScale = Mathf.Max(0f, previousTimeScale);
        }

        isChoosing = false;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }
}

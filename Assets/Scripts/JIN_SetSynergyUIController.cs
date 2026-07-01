using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class JIN_SetSynergyUIController : MonoBehaviour
{
    [SerializeField]
    private JIN_PlayerItemInventory inventory;

    [SerializeField]
    private JIN_SetSynergyController setSynergyController;

    private readonly Dictionary<string, Text> rowTextsBySetId = new Dictionary<string, Text>();
    private readonly Dictionary<RectTransform, string> setIdByRowRect = new Dictionary<RectTransform, string>();
    private readonly List<GameObject> itemSlotObjects = new List<GameObject>();
    private readonly List<Image> itemSlotImages = new List<Image>();
    private readonly List<Text> itemSlotGlyphTexts = new List<Text>();
    private readonly List<Text> itemSlotLevelTexts = new List<Text>();
    private readonly List<RectTransform> itemSlotRects = new List<RectTransform>();
    private readonly List<string> itemIdsBySlot = new List<string>();
    private Canvas canvas;
    private GameObject rootObject;
    private GameObject itemSlotContainerObject;
    private Text detailsText;
    private GameObject tooltipRoot;
    private RectTransform tooltipRect;
    private Text tooltipText;
    private string selectedSetId;

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

    private void Update()
    {
        TrySelectSetByMouse();
        UpdateItemTooltipByMouse();
    }

    private void OnDisable()
    {
        Unsubscribe();
        HideItemTooltip();
    }

    public void Configure(JIN_PlayerItemInventory newInventory, JIN_SetSynergyController newSetSynergyController)
    {
        Unsubscribe();
        inventory = newInventory;
        setSynergyController = newSetSynergyController;
        Subscribe();
        Refresh();
    }

    private void ResolveReferences()
    {
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<JIN_PlayerItemInventory>();
        }

        if (setSynergyController == null)
        {
            setSynergyController = FindAnyObjectByType<JIN_SetSynergyController>();
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

    private void HandleInventoryChanged(JIN_PlayerItemInventory changedInventory)
    {
        Refresh();
    }

    private void HandleSynergyChanged(JIN_SetSynergyController changedSynergy)
    {
        Refresh();
    }

    private void EnsureUI()
    {
        if (rootObject != null)
        {
            return;
        }

        JIN_UIUtility.EnsureLegacyEventSystem();
        canvas = JIN_UIUtility.ResolveOrCreateCanvas();
        rootObject = new GameObject("JIN_SetSynergyPanel");
        rootObject.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = rootObject.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0.5f);
        rootRect.anchorMax = new Vector2(0f, 0.5f);
        rootRect.pivot = new Vector2(0f, 0.5f);
        rootRect.anchoredPosition = new Vector2(18f, -20f);
        rootRect.sizeDelta = new Vector2(340f, 660f);

        Image background = rootObject.AddComponent<Image>();
        background.sprite = JIN_RuntimeSpriteUtility.WhiteSprite;
        background.color = new Color(0.025f, 0.03f, 0.04f, 0.82f);

        VerticalLayoutGroup layoutGroup = rootObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(12, 12, 12, 12);
        layoutGroup.spacing = 8f;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        Text headerText = JIN_UIUtility.CreateText(rootObject.transform, "Header", "세트 시너지", 22, Color.white, TextAnchor.MiddleLeft);
        LayoutElement headerLayout = headerText.gameObject.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 34f;

        foreach (JIN_SetDefinition set in JIN_ItemCatalog.AllSets)
        {
            CreateSetRow(set);
        }

        CreateItemSlotContainer();

        detailsText = JIN_UIUtility.CreateText(rootObject.transform, "Details", "아이템을 획득하면 세트 정보가 표시됩니다.", 17, new Color(0.88f, 0.9f, 0.95f), TextAnchor.UpperLeft);
        LayoutElement detailsLayout = detailsText.gameObject.AddComponent<LayoutElement>();
        detailsLayout.preferredHeight = 155f;
        detailsLayout.flexibleHeight = 1f;

        CreateTooltip();
    }

    private void CreateSetRow(JIN_SetDefinition set)
    {
        GameObject rowObject = new GameObject(set.Id);
        rowObject.transform.SetParent(rootObject.transform, false);

        Image rowImage = rowObject.AddComponent<Image>();
        rowImage.sprite = JIN_RuntimeSpriteUtility.WhiteSprite;
        rowImage.color = new Color(set.Color.r, set.Color.g, set.Color.b, 0.18f);

        Button rowButton = rowObject.AddComponent<Button>();
        rowButton.targetGraphic = rowImage;
        string capturedSetId = set.Id;
        rowButton.onClick.AddListener(() => SelectSet(capturedSetId));

        LayoutElement rowLayout = rowObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 56f;

        Text rowText = JIN_UIUtility.CreateText(rowObject.transform, "Text", string.Empty, 16, Color.white, TextAnchor.MiddleLeft);
        RectTransform rowTextRect = rowText.GetComponent<RectTransform>();
        JIN_UIUtility.Stretch(rowTextRect);
        rowTextRect.offsetMin = new Vector2(12f, 0f);
        rowTextRect.offsetMax = new Vector2(-8f, 0f);

        rowTextsBySetId[set.Id] = rowText;
        setIdByRowRect[rowObject.GetComponent<RectTransform>()] = set.Id;
    }

    private void CreateItemSlotContainer()
    {
        itemSlotContainerObject = new GameObject("OwnedItemSlots");
        itemSlotContainerObject.transform.SetParent(rootObject.transform, false);
        itemSlotContainerObject.AddComponent<RectTransform>();

        HorizontalLayoutGroup layoutGroup = itemSlotContainerObject.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 8f;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        LayoutElement layoutElement = itemSlotContainerObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 64f;
        itemSlotContainerObject.SetActive(false);
    }

    private void EnsureItemSlotCount(int count)
    {
        while (itemSlotObjects.Count < count)
        {
            CreateItemSlot(itemSlotObjects.Count);
        }
    }

    private void CreateItemSlot(int index)
    {
        GameObject slotObject = new GameObject($"ItemSlot_{index + 1}");
        slotObject.transform.SetParent(itemSlotContainerObject.transform, false);
        slotObject.AddComponent<RectTransform>();

        Image background = slotObject.AddComponent<Image>();
        background.sprite = JIN_RuntimeSpriteUtility.WhiteSprite;
        background.color = new Color(0.12f, 0.13f, 0.16f, 0.95f);

        LayoutElement layoutElement = slotObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 58f;
        layoutElement.preferredHeight = 58f;

        Text glyphText = JIN_UIUtility.CreateText(slotObject.transform, "Glyph", string.Empty, 25, Color.white, TextAnchor.MiddleCenter);
        glyphText.resizeTextForBestFit = true;
        glyphText.resizeTextMinSize = 15;
        glyphText.resizeTextMaxSize = 25;
        RectTransform glyphRect = glyphText.GetComponent<RectTransform>();
        JIN_UIUtility.Stretch(glyphRect);
        glyphRect.offsetMin = new Vector2(6f, 2f);
        glyphRect.offsetMax = new Vector2(-6f, -8f);

        Text levelText = JIN_UIUtility.CreateText(slotObject.transform, "Level", string.Empty, 12, Color.white, TextAnchor.LowerRight);
        RectTransform levelRect = levelText.GetComponent<RectTransform>();
        JIN_UIUtility.Stretch(levelRect);
        levelRect.offsetMin = new Vector2(3f, 3f);
        levelRect.offsetMax = new Vector2(-5f, -4f);

        itemSlotObjects.Add(slotObject);
        itemSlotImages.Add(background);
        itemSlotGlyphTexts.Add(glyphText);
        itemSlotLevelTexts.Add(levelText);
        itemSlotRects.Add(slotObject.GetComponent<RectTransform>());
        itemIdsBySlot.Add(string.Empty);
    }

    private void CreateTooltip()
    {
        tooltipRoot = new GameObject("JIN_ItemTooltip");
        tooltipRoot.transform.SetParent(canvas.transform, false);

        tooltipRect = tooltipRoot.AddComponent<RectTransform>();
        tooltipRect.anchorMin = Vector2.zero;
        tooltipRect.anchorMax = Vector2.zero;
        tooltipRect.pivot = new Vector2(0f, 1f);
        tooltipRect.sizeDelta = new Vector2(315f, 180f);

        Image background = tooltipRoot.AddComponent<Image>();
        background.sprite = JIN_RuntimeSpriteUtility.WhiteSprite;
        background.color = new Color(0.025f, 0.03f, 0.04f, 0.96f);

        tooltipText = JIN_UIUtility.CreateText(tooltipRoot.transform, "Text", string.Empty, 15, Color.white, TextAnchor.UpperLeft);
        RectTransform textRect = tooltipText.GetComponent<RectTransform>();
        JIN_UIUtility.Stretch(textRect);
        textRect.offsetMin = new Vector2(12f, 10f);
        textRect.offsetMax = new Vector2(-12f, -10f);

        tooltipRoot.SetActive(false);
    }

    private void TrySelectSetByMouse()
    {
        if (rootObject == null || !rootObject.activeInHierarchy || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        Camera eventCamera = ResolveCanvasCamera();
        Vector2 mousePosition = Input.mousePosition;

        // 버튼 입력 모듈이 비활성화된 상황에서도 세트 상세 확인이 가능하게 직접 클릭 영역을 검사한다.
        foreach (KeyValuePair<RectTransform, string> row in setIdByRowRect)
        {
            RectTransform rowRect = row.Key;

            if (rowRect != null
                && rowRect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(rowRect, mousePosition, eventCamera))
            {
                SelectSet(row.Value);
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

    private void SelectSet(string setId)
    {
        selectedSetId = setId;
        RefreshDetails();
    }

    private void Refresh()
    {
        EnsureUI();

        if (setSynergyController == null)
        {
            ResolveReferences();
        }

        string firstActiveSetId = string.Empty;

        foreach (JIN_SetDefinition set in JIN_ItemCatalog.AllSets)
        {
            JIN_SetProgress progress = setSynergyController != null
                ? setSynergyController.GetProgress(set.Id)
                : new JIN_SetProgress(set, 0);
            bool isActive = progress.HasAnyItem;

            if (isActive && string.IsNullOrEmpty(firstActiveSetId))
            {
                firstActiveSetId = set.Id;
            }

            if (rowTextsBySetId.TryGetValue(set.Id, out Text rowText))
            {
                rowText.transform.parent.gameObject.SetActive(isActive);
                rowText.text = $"{set.DisplayName}  {progress.OwnedUniqueItemCount}/3\n<size=13>{progress.TierLabel} · {set.ShortDescription}</size>";
            }
        }

        if (string.IsNullOrEmpty(selectedSetId) || !HasOwnedItemInSet(selectedSetId))
        {
            selectedSetId = firstActiveSetId;
        }

        RefreshDetails();
    }

    private void RefreshDetails()
    {
        if (detailsText == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(selectedSetId)
            || !JIN_ItemCatalog.TryGetSet(selectedSetId, out JIN_SetDefinition selectedSet))
        {
            ClearItemSlots();
            detailsText.text = "아이템을 획득하면 세트 정보가 표시됩니다.";
            return;
        }

        List<JIN_OwnedItemInfo> ownedItems = inventory != null
            ? inventory.GetOwnedItemsInSet(selectedSetId)
            : new List<JIN_OwnedItemInfo>();
        RefreshItemSlots(ownedItems);

        if (ownedItems.Count == 0)
        {
            detailsText.text = "아이템을 획득하면 세트 정보가 표시됩니다.";
            return;
        }

        string itemLines = string.Empty;

        foreach (JIN_OwnedItemInfo ownedItem in ownedItems)
        {
            itemLines += $"\n{ownedItem.Definition.IconGlyph} {ownedItem.Definition.DisplayName} Lv.{ownedItem.Level} · {ownedItem.Definition.TierLabel} · {ownedItem.Definition.EffectKindLabel}";
        }

        // 클릭한 세트에 속한 보유 아이템을 바로 확인할 수 있게 상세 영역을 갱신한다.
        detailsText.text = $"{selectedSet.DisplayName}\n<size=14>{selectedSet.ShortDescription}</size>\n{itemLines}";
    }

    private void RefreshItemSlots(List<JIN_OwnedItemInfo> ownedItems)
    {
        if (itemSlotContainerObject == null)
        {
            return;
        }

        EnsureItemSlotCount(ownedItems.Count);
        itemSlotContainerObject.SetActive(ownedItems.Count > 0);

        for (int i = 0; i < itemSlotObjects.Count; i++)
        {
            bool hasItem = i < ownedItems.Count;
            itemSlotObjects[i].SetActive(hasItem);
            itemIdsBySlot[i] = hasItem ? ownedItems[i].Definition.Id : string.Empty;

            if (!hasItem)
            {
                continue;
            }

            JIN_ItemDefinition definition = ownedItems[i].Definition;
            Color tierColor = JIN_ItemCatalog.GetTierColor(definition.Tier);
            Color backgroundColor = Color.Lerp(new Color(0.08f, 0.09f, 0.12f), tierColor, 0.34f);
            backgroundColor.a = 0.96f;

            itemSlotImages[i].color = backgroundColor;
            itemSlotGlyphTexts[i].text = definition.IconGlyph;
            itemSlotLevelTexts[i].text = $"Lv.{ownedItems[i].Level}";
        }
    }

    private void ClearItemSlots()
    {
        if (itemSlotContainerObject != null)
        {
            itemSlotContainerObject.SetActive(false);
        }

        for (int i = 0; i < itemSlotObjects.Count; i++)
        {
            itemSlotObjects[i].SetActive(false);
            itemIdsBySlot[i] = string.Empty;
        }

        HideItemTooltip();
    }

    private void UpdateItemTooltipByMouse()
    {
        if (rootObject == null || !rootObject.activeInHierarchy || tooltipRoot == null)
        {
            return;
        }

        Camera eventCamera = ResolveCanvasCamera();
        Vector2 mousePosition = Input.mousePosition;

        // EventSystem hover가 없어도 Old Input 좌표만으로 아이템 설명을 띄운다.
        for (int i = 0; i < itemSlotRects.Count; i++)
        {
            RectTransform slotRect = itemSlotRects[i];

            if (slotRect != null
                && slotRect.gameObject.activeInHierarchy
                && !string.IsNullOrEmpty(itemIdsBySlot[i])
                && RectTransformUtility.RectangleContainsScreenPoint(slotRect, mousePosition, eventCamera))
            {
                ShowItemTooltip(itemIdsBySlot[i], mousePosition);
                return;
            }
        }

        HideItemTooltip();
    }

    private void ShowItemTooltip(string itemId, Vector2 mousePosition)
    {
        if (tooltipRoot == null
            || tooltipText == null
            || !JIN_ItemCatalog.TryGetItem(itemId, out JIN_ItemDefinition item))
        {
            HideItemTooltip();
            return;
        }

        int currentLevel = inventory != null ? inventory.GetItemLevel(item.Id) : 0;
        string setName = JIN_ItemCatalog.TryGetSet(item.SetId, out JIN_SetDefinition set)
            ? set.DisplayName
            : "세트 없음";

        tooltipText.text = $"{item.IconGlyph} {item.DisplayName}\n<size=13>{item.TierLabel} · {item.EffectKindLabel} · {setName}</size>\n<size=14><i>{item.FlavorText}</i></size>\n\n<size=14>{item.Description}</size>\n<size=13>Lv.{currentLevel}/{item.MaxLevel}</size>";
        PositionTooltip(mousePosition);
        tooltipRoot.transform.SetAsLastSibling();
        tooltipRoot.SetActive(true);
    }

    private void HideItemTooltip()
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(false);
        }
    }

    private void PositionTooltip(Vector2 mousePosition)
    {
        if (tooltipRect == null)
        {
            return;
        }

        Vector2 position = mousePosition + new Vector2(18f, -18f);
        float maxX = Mathf.Max(8f, Screen.width - tooltipRect.sizeDelta.x - 8f);
        float minY = tooltipRect.sizeDelta.y + 8f;
        float maxY = Mathf.Max(minY, Screen.height - 8f);

        position.x = Mathf.Clamp(position.x, 8f, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        tooltipRect.position = position;
    }

    private bool HasOwnedItemInSet(string setId)
    {
        return inventory != null && inventory.GetUniqueItemCountInSet(setId) > 0;
    }
}

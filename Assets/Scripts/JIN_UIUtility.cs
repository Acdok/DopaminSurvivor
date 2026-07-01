using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class JIN_UIUtility
{
    private static Font defaultFont;

    public static Font DefaultFont
    {
        get
        {
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                if (defaultFont == null)
                {
                    defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }

            return defaultFont;
        }
    }

    public static Canvas ResolveOrCreateCanvas()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();

        if (canvas != null)
        {
            EnsureGraphicRaycaster(canvas);
            return canvas;
        }

        GameObject canvasObject = new GameObject("HUD Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static void EnsureLegacyEventSystem()
    {
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            EventSystem.current = eventSystem;
            return;
        }

        eventSystem.enabled = true;
        EventSystem.current = eventSystem;

        StandaloneInputModule legacyInputModule = eventSystem.GetComponent<StandaloneInputModule>();

        if (legacyInputModule == null)
        {
            legacyInputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        legacyInputModule.enabled = true;

        // UI 클릭도 Old Input으로 받기 위해 새 입력 모듈이 있으면 런타임에서 비활성화한다.
        BaseInputModule[] inputModules = eventSystem.GetComponents<BaseInputModule>();

        foreach (BaseInputModule inputModule in inputModules)
        {
            if (inputModule != null && inputModule != legacyInputModule)
            {
                inputModule.enabled = false;
            }
        }
    }

    public static Text CreateText(
        Transform parent,
        string name,
        string initialText,
        int fontSize,
        Color color,
        TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = DefaultFont;
        text.text = initialText;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        return text;
    }

    public static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = JIN_RuntimeSpriteUtility.WhiteSprite;
        image.color = color;

        return image;
    }

    private static void EnsureGraphicRaycaster(Canvas canvas)
    {
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    public static RectTransform Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        return rectTransform;
    }
}

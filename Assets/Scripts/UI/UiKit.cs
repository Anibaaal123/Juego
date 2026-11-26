using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Utilidades para construir interfaz por código (paneles del resumen y de la tienda, HUD de habilidades).
/// Todo se dibuja en una resolución de referencia de 1920×1080.
/// </summary>
public static class UiKit
{
    public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private static Font font;

    public static Font DefaultFont
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    /// <summary>Crea un Canvas de pantalla completa por encima de la interfaz de la arena.</summary>
    public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        if (parent != null) go.transform.SetParent(parent, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        return canvas;
    }

    /// <summary>Los botones necesitan un EventSystem; las arenas ya traen uno, esto es solo un seguro.</summary>
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    public static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    public static Image MakeImage(string name, Transform parent, Color color, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rt = MakeRect(name, parent, anchor, position, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static Text MakeText(Transform parent, string name, string content, int fontSize, Color color,
                                Vector2 anchor, Vector2 position, Vector2 size,
                                TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal,
                                bool wrap = false)
    {
        RectTransform rt = MakeRect(name, parent, anchor, position, size);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = DefaultFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    public static Button MakeButton(Transform parent, string name, string label, int fontSize, Vector2 anchor,
                                    Vector2 position, Vector2 size, Color baseColor, UnityAction onClick)
    {
        Image img = MakeImage(name, parent, Color.white, anchor, position, size);
        img.raycastTarget = true; // necesario para recibir clics del mouse

        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.3f);
        colors.selectedColor = Color.Lerp(baseColor, Color.white, 0.3f);
        colors.pressedColor = new Color(1f, 0.78f, 0.25f);
        colors.disabledColor = new Color(0.22f, 0.22f, 0.26f, 0.95f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        if (onClick != null) button.onClick.AddListener(onClick);

        Text text = MakeText(img.transform, "Texto", label, fontSize, Color.white, Center, Vector2.zero, size,
                             TextAnchor.MiddleCenter, FontStyle.Bold);
        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return button;
    }

    public static void SetButtonLabel(Button button, string label)
    {
        Text text = button.GetComponentInChildren<Text>();
        if (text != null) text.text = label;
    }

    public static void SelectDefault(Button button)
    {
        if (button != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}

using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Genera la escena del menú de inicio (Assets/Scenes/MainMenu.unity).
/// Menú: Colossus → Construir Menú Principal. Al ejecutarlo de nuevo se regenera completa
/// (los cambios hechos a mano dentro de la escena se pierden).
/// </summary>
public static class MenuBuilder
{
    public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    private const string BackgroundPath = "Assets/Environment/background.png";
    private const string ButtonSpritePath = "Assets/Environment/Common/tile_plate.png";
    private const string HeroSpritePath = "Assets/Characters/Hero/Sprites/hero-idle/hero-idle-1.png";
    private const string DemonSpritePath = "Assets/Characters/Boss/demon-Files/Sprites/Idle/idle1.png";

    private static readonly Color Gold = new Color(1f, 0.78f, 0.25f);
    private static readonly Color ButtonColor = new Color(0.28f, 0.12f, 0.38f, 0.95f);

    [MenuItem("Colossus/Construir Menú Principal")]
    public static void BuildMainMenuFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildMainMenu();
        ArenaBuilder.UpdateBuildSettings();
        EditorSceneManager.OpenScene(MainMenuScenePath);
    }

    public static void BuildMainMenu()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Cámara
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.02f, 0.1f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();

        // Canvas (Screen Space - Camera)
        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.layer = 5;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5f;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Fondo y decoración
        var bg = CreateImage(canvasGo.transform, "Fondo", LoadSprite(BackgroundPath), new Color(0.75f, 0.75f, 0.75f));
        Stretch(bg.rectTransform);
        var shade = CreateImage(canvasGo.transform, "Sombra", null, new Color(0f, 0f, 0f, 0.35f));
        Stretch(shade.rectTransform);

        var demon = CreateImage(canvasGo.transform, "Demonio", LoadSprite(DemonSpritePath), Color.white);
        demon.preserveAspect = true;
        Place(demon.rectTransform, new Vector2(1f, 0f), new Vector2(-330f, 330f), new Vector2(620f, 620f));
        demon.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // mira hacia el héroe

        var hero = CreateImage(canvasGo.transform, "Heroe", LoadSprite(HeroSpritePath), Color.white);
        hero.preserveAspect = true;
        Place(hero.rectTransform, new Vector2(0f, 0f), new Vector2(330f, 250f), new Vector2(560f, 315f));

        // Título
        var title = CreateText(canvasGo.transform, "Titulo", "COLOSSUS", font, 150, Gold, FontStyle.Bold);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(1400f, 200f));
        var subtitle = CreateText(canvasGo.transform, "Subtitulo", "B O S S   R U S H", font, 52, Color.white, FontStyle.Bold);
        Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(1000f, 80f));

        // Panel principal con botones
        var mainPanel = new GameObject("PanelPrincipal", typeof(RectTransform));
        mainPanel.transform.SetParent(canvasGo.transform, false);
        Place((RectTransform)mainPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(520f, 420f));

        Button playButton = CreateButton(mainPanel.transform, "Jugar", "JUGAR", font, new Vector2(0f, 120f));
        Button controlsButton = CreateButton(mainPanel.transform, "Controles", "CONTROLES", font, new Vector2(0f, 0f));
        Button quitButton = CreateButton(mainPanel.transform, "Salir", "SALIR", font, new Vector2(0f, -120f));

        // Panel de controles
        var controlsPanel = CreateImage(canvasGo.transform, "PanelControles", LoadSprite(ButtonSpritePath), new Color(0.06f, 0.02f, 0.1f, 0.92f));
        controlsPanel.type = Image.Type.Sliced;
        Place(controlsPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(900f, 560f));

        var controlsTitle = CreateText(controlsPanel.transform, "Titulo", "CONTROLES", font, 56, Gold, FontStyle.Bold);
        Place(controlsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(800f, 80f));
        var controlsText = CreateText(controlsPanel.transform, "Lista",
            "Moverse:  A / D\n" +
            "Saltar:  Espacio\n" +
            "Golpe:  Clic izquierdo\n" +
            "Golpe cargado:  mantener clic 0.6 s y soltar\n" +
            "Esquiva (dash):  Shift izquierdo\n" +
            "Reiniciar partida:  R      Volver al menú:  Esc",
            font, 34, Color.white, FontStyle.Normal);
        controlsText.alignment = TextAnchor.MiddleCenter;
        controlsText.lineSpacing = 1.25f;
        Place(controlsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(820f, 320f));
        Button backButton = CreateButton(controlsPanel.transform, "Volver", "VOLVER", font, Vector2.zero);
        Place((RectTransform)backButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 75f), new Vector2(360f, 90f));
        controlsPanel.gameObject.SetActive(false);

        // Lógica del menú y botones
        var menu = canvasGo.AddComponent<MainMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        so.FindProperty("controlsPanel").objectReferenceValue = controlsPanel.gameObject;
        so.FindProperty("firstButton").objectReferenceValue = playButton;
        so.FindProperty("controlsBackButton").objectReferenceValue = backButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(playButton.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(controlsButton.onClick, menu.ShowControls);
        UnityEventTools.AddPersistentListener(quitButton.onClick, menu.Quit);
        UnityEventTools.AddPersistentListener(backButton.onClick, menu.ShowMain);

        // EventSystem (mismo módulo de entrada que las arenas)
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>().firstSelectedGameObject = playButton.gameObject;
        es.AddComponent<InputSystemUIInputModule>();

        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        Debug.Log("[MenuBuilder] Menú principal construido en " + MainMenuScenePath);
    }

    // ---------------- Utilidades de UI ----------------

    private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 position)
    {
        var img = CreateImage(parent, name, LoadSprite(ButtonSpritePath), Color.white);
        img.type = Image.Type.Sliced;
        img.raycastTarget = true; // necesario para que el botón reciba clics del mouse
        Place(img.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(460f, 96f));

        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color(0.55f, 0.25f, 0.7f, 1f);
        colors.selectedColor = new Color(0.55f, 0.25f, 0.7f, 1f);
        colors.pressedColor = Gold;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var text = CreateText(img.transform, "Texto", label, font, 44, Color.white, FontStyle.Bold);
        Stretch(text.rectTransform);
        return button;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Text CreateText(Transform parent, string name, string content, Font font, int size, Color color, FontStyle style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite == null) Debug.LogWarning("[MenuBuilder] No se encontró sprite en " + path);
        return sprite;
    }
}

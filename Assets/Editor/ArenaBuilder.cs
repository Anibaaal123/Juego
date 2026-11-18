using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Construye el Boss Rush en el editor:
///   • Arena 1 (Assets/Scenes/Arena1_Demonio.unity): el jefe demonio original + pedestales de mejora + puerta.
///   • Arena 2 (Assets/Scenes/Arena2_Centinela.unity): se genera desde la Arena 1 con El Centinela y el piso eléctrico.
/// Menú: Colossus → Construir Boss Rush. Se puede volver a ejecutar: la Arena 2 se regenera completa
/// (los cambios hechos a mano dentro de ella se pierden); en la Arena 1 solo se rehacen los objetos del Boss Rush.
/// </summary>
public static class ArenaBuilder
{
    private const string BeastDir = "Assets/Characters/Boss/HellBeast";
    private const string AnimDir = BeastDir + "/Animations";
    private const string TileSpritePath = "Assets/Environment/Common/tile_plate.png";
    private const string OrbSpritePath = "Assets/Environment/Common/orb.png";
    private const string FireballPrefabPath = "Assets/Prefabs/Fireball.prefab";
    private const string OldArena1Path = "Assets/Scenes/SampleScene.unity";
    public const string Arena1ScenePath = "Assets/Scenes/Arena1_Demonio.unity";
    public const string Arena2ScenePath = "Assets/Scenes/Arena2_Centinela.unity";

    private const int PixelsPerUnit = 16;
    private const float BossScale = 1.5f;
    private const int EnemyLayer = 7; // "Enemigo"

    // Nombres de los objetos que crea el constructor (para poder rehacerlos)
    private const string ManagerName = "BossArenaManager";
    private const string DoorName = "ExitDoor";
    private const string MessageName = "ArenaMessage";

    [MenuItem("Colossus/Construir Boss Rush (Arena 1 + Arena 2)")]
    public static void BuildBossRush()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // SampleScene pasa a llamarse Arena1_Demonio (conserva su GUID y referencias)
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Arena1ScenePath) == null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(OldArena1Path) != null)
        {
            string error = AssetDatabase.MoveAsset(OldArena1Path, Arena1ScenePath);
            if (!string.IsNullOrEmpty(error)) { Debug.LogError("[ArenaBuilder] " + error); return; }
        }

        ConfigureSprites(BeastDir + "/Idle", SpriteAlignment.BottomCenter);
        ConfigureSprites(BeastDir + "/Breath", SpriteAlignment.BottomCenter);
        ConfigureSprites(BeastDir + "/Burn", SpriteAlignment.BottomCenter);
        ConfigureSprites(BeastDir + "/Fireball", SpriteAlignment.Center);
        ConfigureSingleSprite(TileSpritePath, new Vector4(2, 2, 2, 2));
        ConfigureSingleSprite(OrbSpritePath, Vector4.zero);

        BuildFireballPrefab(LoadSprites(BeastDir + "/Fireball"));

        BuildArena1();
        BuildArena2();

        // El menú se crea solo si no existe (se regenera con Colossus → Construir Menú Principal)
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuBuilder.MainMenuScenePath) == null) MenuBuilder.BuildMainMenu();

        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuBuilder.MainMenuScenePath);
        Debug.Log("[ArenaBuilder] Boss Rush construido: MainMenu -> Arena1_Demonio -> Arena2_Centinela");
    }

    /// <summary>Orden de las escenas en el juego: menú (0), Arena 1 (1), Arena 2 (2).</summary>
    public static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuBuilder.MainMenuScenePath, true),
            new EditorBuildSettingsScene(Arena1ScenePath, true),
            new EditorBuildSettingsScene(Arena2ScenePath, true),
        };
    }

    // ---------------- Arena 1 ----------------

    private static void BuildArena1()
    {
        var scene = EditorSceneManager.OpenScene(Arena1ScenePath, OpenSceneMode.Single);

        // Las monedas daban daño extra fuera del sistema de mejoras: se quitan (las reemplazan los pedestales)
        DestroyIfExists("Coin", "Coin (1)", "Coin (2)");

        GameObject boss = GameObject.Find("Enemy");
        EnemyHealth bossHealth = boss != null ? boss.GetComponent<EnemyHealth>() : null;
        if (bossHealth == null) Debug.LogError("[ArenaBuilder] La Arena 1 no tiene el objeto 'Enemy' con EnemyHealth");

        if (boss != null) SetupDemonBoss(boss);

        SetupBossRushObjects(scene, bossHealth, nextScene: "Arena2_Centinela", spawnPedestals: true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Reemplaza EnemyAttack (prototipo) por DemonBoss conservando los valores ajustados en la escena.
    /// Si ya tiene DemonBoss, solo actualiza referencias y límites.
    /// </summary>
    private static void SetupDemonBoss(GameObject boss)
    {
        var demon = boss.GetComponent<DemonBoss>();
        var old = boss.GetComponent<EnemyAttack>();

        if (demon == null)
        {
            demon = boss.AddComponent<DemonBoss>();
            if (old != null)
            {
                var from = new SerializedObject(old);
                var to = new SerializedObject(demon);
                foreach (string field in new[] { "flightSpeed", "stoppingDistance", "preferredHeightOffset", "floatFrequency",
                                                 "floatAmplitude", "attackDamage", "attackPoint", "fireAreaSize", "playerLayer",
                                                 "fireStartDelay", "flameDuration", "recoveryDelay", "animator", "spriteRenderer",
                                                 "rb", "audioSource", "attackSound", "contactDamage", "contactDamageCooldown" })
                {
                    SerializedProperty src = from.FindProperty(field);
                    if (src != null) to.CopyFromSerializedProperty(src);
                }
                to.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        if (old != null) Object.DestroyImmediate(old, true);

        float arenaMin = GetColliderEdge("Wall_Left", right: true, fallback: -32f);
        float arenaMax = GetColliderEdge("Wall_Right", left: true, fallback: 32f);
        demon.SetArenaBounds(arenaMin + 3f, arenaMax - 3f);

        var so = new SerializedObject(demon);
        so.FindProperty("projectilePrefab").objectReferenceValue = LoadFireballPrefab();
        so.FindProperty("warningSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(demon);
    }

    private static BossProjectile LoadFireballPrefab()
    {
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(FireballPrefabPath);
        return go != null ? go.GetComponent<BossProjectile>() : null;
    }

    // ---------------- Arena 2 ----------------

    private static void BuildArena2()
    {
        Sprite[] idle = LoadSprites(BeastDir + "/Idle");
        Sprite[] breath = LoadSprites(BeastDir + "/Breath");
        Sprite[] burn = LoadSprites(BeastDir + "/Burn");

        AnimatorController controller = BuildAnimator(idle, breath, burn);
        BossProjectile fireballPrefab = LoadFireballPrefab();

        AssetDatabase.DeleteAsset(Arena2ScenePath);
        if (!AssetDatabase.CopyAsset(Arena1ScenePath, Arena2ScenePath))
        {
            Debug.LogError("[ArenaBuilder] No se pudo copiar " + Arena1ScenePath);
            return;
        }
        var scene = EditorSceneManager.OpenScene(Arena2ScenePath, OpenSceneMode.Single);

        // Rescata los sonidos del jefe anterior antes de borrarlo
        GameObject oldBoss = GameObject.Find("Enemy");
        AudioClip deathSound = null, fireSound = null;
        if (oldBoss != null)
        {
            var oldHealth = oldBoss.GetComponent<EnemyHealth>();
            if (oldHealth != null) deathSound = new SerializedObject(oldHealth).FindProperty("bossDeathSound").objectReferenceValue as AudioClip;
            MonoBehaviour oldAttack = oldBoss.GetComponent<DemonBoss>();
            if (oldAttack == null) oldAttack = oldBoss.GetComponent<EnemyAttack>();
            if (oldAttack != null) fireSound = new SerializedObject(oldAttack).FindProperty("attackSound").objectReferenceValue as AudioClip;
        }

        // Elementos de la Arena 1 que no corresponden a la Arena 2
        DestroyIfExists("Enemy", "HazardSkullGroup", "FireWall_Left");

        float groundTop = GetColliderEdge("Suelo", top: true, fallback: -2.5f);
        float arenaMin = GetColliderEdge("Wall_Left", right: true, fallback: -32f);
        float arenaMax = GetColliderEdge("Wall_Right", left: true, fallback: 32f);
        int baseOrder = PlayerSortingOrder();

        Slider bossSlider = Resources.FindObjectsOfTypeAll<Slider>()
                                     .FirstOrDefault(s => s.name == "BossHealthSlider" && s.gameObject.scene == scene);

        GameObject boss = BuildBoss(idle[0], controller, fireballPrefab, groundTop, arenaMin + 2.5f, arenaMax - 2.5f,
                                    baseOrder, bossSlider, deathSound, fireSound);

        BuildElectricFloor(groundTop, arenaMin, arenaMax, baseOrder - 1, boss.GetComponent<EnemyHealth>());

        // Por ahora es la última arena: sin pedestales ni puerta (se activan cuando exista la Arena 3)
        SetupBossRushObjects(scene, boss.GetComponent<EnemyHealth>(), nextScene: "", spawnPedestals: false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ---------------- Objetos comunes del Boss Rush ----------------

    private static void SetupBossRushObjects(UnityEngine.SceneManagement.Scene scene, EnemyHealth boss,
                                             string nextScene, bool spawnPedestals)
    {
        DestroyIfExists(ManagerName, DoorName, MessageName);

        float groundTop = GetColliderEdge("Suelo", top: true, fallback: -2.5f);
        float arenaMax = GetColliderEdge("Wall_Right", left: true, fallback: 32f);
        int baseOrder = PlayerSortingOrder();

        // El jugador conserva mejoras y vida entre arenas
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<PlayerRunStats>() == null) player.AddComponent<PlayerRunStats>();

        // El jefe ya no reinicia la escena al morir: ahora sigue el flujo del Boss Rush
        if (boss != null)
        {
            var hso = new SerializedObject(boss);
            hso.FindProperty("restartSceneOnDeath").boolValue = false;
            hso.ApplyModifiedPropertiesWithoutUndo();
        }

        Sprite plate = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);
        Sprite orb = AssetDatabase.LoadAssetAtPath<Sprite>(OrbSpritePath);

        ExitDoor door = string.IsNullOrEmpty(nextScene) ? null : BuildDoor(plate, arenaMax - 2.5f, groundTop, baseOrder - 1, nextScene);
        Text message = BuildMessageText(scene);

        var managerGo = new GameObject(ManagerName);
        var manager = managerGo.AddComponent<BossArenaManager>();
        var so = new SerializedObject(manager);
        so.FindProperty("boss").objectReferenceValue = boss;
        so.FindProperty("spawnPedestals").boolValue = spawnPedestals;
        SerializedProperty positions = so.FindProperty("pedestalPositions");
        positions.arraySize = 2;
        // Separados para que sus textos no choquen con la plataforma central
        positions.GetArrayElementAtIndex(0).vector2Value = new Vector2(-8f, groundTop);
        positions.GetArrayElementAtIndex(1).vector2Value = new Vector2(6f, groundTop);
        so.FindProperty("pedestalSprite").objectReferenceValue = plate;
        so.FindProperty("orbSprite").objectReferenceValue = orb;
        so.FindProperty("sortingOrder").intValue = baseOrder - 1;
        so.FindProperty("exitDoor").objectReferenceValue = door;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ExitDoor BuildDoor(Sprite plate, float x, float groundTop, int sortingOrder, string nextScene)
    {
        var door = new GameObject(DoorName);
        door.transform.position = new Vector3(x, groundTop, 0f);

        var frame = new GameObject("Marco");
        frame.transform.SetParent(door.transform, false);
        frame.transform.localPosition = new Vector3(0f, 2.25f, 0f);
        var sr = frame.AddComponent<SpriteRenderer>();
        sr.sprite = plate;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(2.5f, 4.5f);
        sr.color = new Color(0.6f, 0.3f, 1f);
        sr.sortingOrder = sortingOrder;

        var exit = door.AddComponent<ExitDoor>();
        exit.NextSceneName = nextScene;
        EditorUtility.SetDirty(exit);
        return exit;
    }

    private static Text BuildMessageText(UnityEngine.SceneManagement.Scene scene)
    {
        Canvas canvas = Resources.FindObjectsOfTypeAll<Canvas>()
                                 .FirstOrDefault(c => c.gameObject.scene == scene && c.isRootCanvas && c.renderMode != RenderMode.WorldSpace);
        if (canvas == null) { Debug.LogWarning("[ArenaBuilder] No hay Canvas para los mensajes"); return null; }

        var go = new GameObject(MessageName, typeof(RectTransform));
        go.layer = 5; // UI
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -110f);
        rt.sizeDelta = new Vector2(1200f, 160f);

        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 44;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.UpperCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = "";
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    private static void DestroyIfExists(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject go;
            while ((go = GameObject.Find(name)) != null) Object.DestroyImmediate(go);
        }
    }

    private static int PlayerSortingOrder()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        SpriteRenderer sr = player != null ? player.GetComponent<SpriteRenderer>() : null;
        return sr != null ? sr.sortingOrder : 2;
    }

    private static void ConfigureSingleSprite(string path, Vector4 border)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti == null) { Debug.LogError("[ArenaBuilder] No existe " + path); return; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = PixelsPerUnit;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.spriteBorder = border;

        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(settings);
        ti.SaveAndReimport();
    }

    // ---------------- Sprites ----------------

    private static void ConfigureSprites(string folder, SpriteAlignment alignment)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PixelsPerUnit;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)alignment;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }
    }

    private static Sprite[] LoadSprites(string folder)
    {
        return AssetDatabase.FindAssets("t:Sprite", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .OrderBy(p => NaturalKey(p))
            .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
            .Where(s => s != null)
            .ToArray();
    }

    // Ordena "frame2" antes que "frame10"
    private static string NaturalKey(string path)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(path);
        string digits = new string(name.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        string prefix = name.Substring(0, name.Length - digits.Length);
        return prefix + (digits.Length > 0 ? int.Parse(digits).ToString("D4") : "");
    }

    // ---------------- Animator ----------------

    private static AnimatorController BuildAnimator(Sprite[] idle, Sprite[] breath, Sprite[] burn)
    {
        if (!AssetDatabase.IsValidFolder(AnimDir)) AssetDatabase.CreateFolder(BeastDir, "Animations");

        var clips = new List<AnimationClip>
        {
            MakeClip("Idle", idle, 0.12f, loop: true),
            MakeClip("Breath", breath, 0.12f, loop: false),
            MakeClip("BurnStart", burn.Take(4).ToArray(), 0.15f, loop: false),
            MakeClip("Pillar", burn.Skip(4).ToArray(), 0.10f, loop: true),
            MakeClip("Emerge", burn.Take(4).Reverse().ToArray(), 0.10f, loop: false),
        };

        string path = AnimDir + "/Centinela.controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        foreach (AnimationClip clip in clips)
        {
            AnimatorState st = sm.AddState(clip.name);
            st.motion = clip;
            if (clip.name == "Idle") sm.defaultState = st;
        }
        return controller;
    }

    private static AnimationClip MakeClip(string name, Sprite[] frames, float frameTime, bool loop)
    {
        string path = $"{AnimDir}/Centinela_{name}.anim";
        AssetDatabase.DeleteAsset(path);

        var clip = new AnimationClip { name = name, frameRate = 60f };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i * frameTime, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length * frameTime, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ---------------- Prefab bola de fuego ----------------

    private static BossProjectile BuildFireballPrefab(Sprite[] frames)
    {
        var go = new GameObject("Fireball");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = 20;
        go.transform.localScale = Vector3.one * 1.6f;

        var anim = go.AddComponent<SimpleSpriteAnimation>();
        var so = new SerializedObject(anim);
        SerializedProperty arr = so.FindProperty("frames");
        arr.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        so.FindProperty("framesPerSecond").floatValue = 12f;
        so.ApplyModifiedPropertiesWithoutUndo();

        go.AddComponent<BossProjectile>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, FireballPrefabPath);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<BossProjectile>();
    }

    // ---------------- Jefe ----------------

    private static GameObject BuildBoss(Sprite firstFrame, AnimatorController controller, BossProjectile fireballPrefab,
                                        float groundTop, float minX, float maxX, int sortingOrder, Slider slider,
                                        AudioClip deathSound, AudioClip fireSound)
    {
        var boss = new GameObject("ElCentinela") { layer = EnemyLayer };
        boss.transform.position = new Vector3(Mathf.Min(18f, maxX), groundTop, 0f);
        boss.transform.localScale = Vector3.one * BossScale;

        var sr = boss.AddComponent<SpriteRenderer>();
        sr.sprite = firstFrame;
        sr.sortingOrder = sortingOrder;

        boss.AddComponent<Animator>().runtimeAnimatorController = controller;

        var rb = boss.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Cuerpo (en unidades locales: 44x60 px a 16 PPU)
        var col = boss.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.6f, 3.6f);
        col.offset = new Vector2(0f, 1.8f);

        var audio = boss.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        var health = boss.AddComponent<EnemyHealth>();
        var hso = new SerializedObject(health);
        hso.FindProperty("maxHealth").intValue = 220;
        hso.FindProperty("bossHealthSlider").objectReferenceValue = slider;
        hso.FindProperty("bossDeathSound").objectReferenceValue = deathSound;
        hso.FindProperty("deathFadeDuration").floatValue = 1.0f;
        hso.ApplyModifiedPropertiesWithoutUndo();

        // Recarga el prefab desde disco: la referencia en memoria puede quedar inválida tras abrir la escena
        GameObject fireballAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FireballPrefabPath);
        if (fireballAsset != null) fireballPrefab = fireballAsset.GetComponent<BossProjectile>();
        if (fireballPrefab == null) Debug.LogError("[ArenaBuilder] No se encontró el prefab " + FireballPrefabPath);

        var ai = boss.AddComponent<CentinelaBoss>();
        ai.SetArenaBounds(minX, maxX);
        var aso = new SerializedObject(ai);
        aso.FindProperty("projectilePrefab").objectReferenceValue = fireballPrefab;
        aso.FindProperty("fireballSound").objectReferenceValue = fireSound;
        aso.FindProperty("pillarSound").objectReferenceValue = fireSound;
        aso.ApplyModifiedPropertiesWithoutUndo();

        if (slider != null) slider.gameObject.SetActive(true);
        return boss;
    }

    // ---------------- Piso eléctrico ----------------

    private static void BuildElectricFloor(float groundTop, float arenaMin, float arenaMax, int sortingOrder, EnemyHealth boss)
    {
        Sprite plate = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);

        var root = new GameObject("ElectricFloor");
        var controller = root.AddComponent<ElectricFloorController>();

        const int tileCount = 4;
        const float tileWidth = 7f;
        const float tileHeight = 0.5f;
        float span = arenaMax - arenaMin;
        var tiles = new ElectricTile[tileCount];

        for (int i = 0; i < tileCount; i++)
        {
            // 4 baldosas repartidas en partes iguales a lo ancho de la arena
            float x = arenaMin + span * (i + 0.5f) / tileCount;
            var tile = new GameObject($"ElectricTile_{i + 1}");
            tile.transform.SetParent(root.transform);
            tile.transform.position = new Vector3(x, groundTop + 0.05f - tileHeight * 0.5f, 0f);

            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = plate;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(tileWidth, tileHeight);
            sr.sortingOrder = sortingOrder;

            tiles[i] = tile.AddComponent<ElectricTile>();
        }

        var so = new SerializedObject(controller);
        SerializedProperty arr = so.FindProperty("tiles");
        arr.arraySize = tileCount;
        for (int i = 0; i < tileCount; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = tiles[i];
        so.FindProperty("boss").objectReferenceValue = boss;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------- Utilidades ----------------

    private static float GetColliderEdge(string objectName, bool top = false, bool left = false, bool right = false, float fallback = 0f)
    {
        GameObject go = GameObject.Find(objectName);
        BoxCollider2D box = go != null ? go.GetComponent<BoxCollider2D>() : null;
        if (box == null)
        {
            Debug.LogWarning($"[ArenaBuilder] No se encontró BoxCollider2D en '{objectName}', se usa {fallback}");
            return fallback;
        }

        Vector2 half = box.size * 0.5f;
        Vector2 local = box.offset + new Vector2(left ? -half.x : right ? half.x : 0f, top ? half.y : 0f);
        Vector3 world = go.transform.TransformPoint(local);
        return top ? world.y : world.x;
    }
}

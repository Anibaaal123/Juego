using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arena 3 — El Guardián. Se genera desde la Arena 1 (igual que la Arena 2), pero:
///   • es estrecha (14 u de ancho): paredes más cerca, columnas de piedra en los límites y sombra fuera de la arena;
///   • no tiene plataformas;
///   • los dos bordes (2 u cada uno) se encienden con fuego durante 8 s cada 20 s (15 HP/s) y el jefe es inmune;
///   • la cámara se acerca para encuadrar la arena.
/// </summary>
public static partial class ArenaBuilder
{
    public const string Arena3ScenePath = "Assets/Scenes/Arena3_Guardian.unity";

    private const string OgreDir = "Assets/Characters/Boss/Ogre";
    private const string PillarSpritePath = "Assets/Environment/Terrain/pillar.png";
    private const string BlackSpritePath = "Assets/Environment/Terrain/black.png";

    private const float OgreScale = 1.8f;
    /// <summary>Mitad del ancho de la arena (GDD: 14 u de ancho).</summary>
    private const float ArenaHalfWidth = 7f;

    private static void ConfigureArena3Sprites()
    {
        foreach (string folder in new[] { "Idle", "Walk", "Attack", "IdleUnarmed", "WalkUnarmed" })
            ConfigureSprites(OgreDir + "/" + folder, SpriteAlignment.BottomCenter);

        ConfigureSingleSprite(PillarSpritePath, Vector4.zero, SpriteAlignment.BottomCenter);
        ConfigureSingleSprite(BlackSpritePath, Vector4.zero, SpriteAlignment.Center);
    }

    private static void BuildArena3()
    {
        AnimatorController controller = EnsureOgreAnimator();
        Sprite[] idle = LoadSprites(OgreDir + "/Idle");
        Sprite[] flames = LoadSprites(BeastDir + "/Burn").Skip(4).ToArray();   // columna de fuego del Centinela

        // La Arena 3 parte de una copia de la Arena 1; si ya existe se sobrescribe conservando su GUID
        var arena1 = EditorSceneManager.OpenScene(Arena1ScenePath, OpenSceneMode.Single);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Arena3ScenePath) != null)
        {
            if (!EditorSceneManager.SaveScene(arena1, Arena3ScenePath, true))
            {
                Debug.LogError("[ArenaBuilder] No se pudo sobrescribir " + Arena3ScenePath);
                return;
            }
            AssetDatabase.ImportAsset(Arena3ScenePath, ImportAssetOptions.ForceSynchronousImport);
        }
        else if (!AssetDatabase.CopyAsset(Arena1ScenePath, Arena3ScenePath))
        {
            Debug.LogError("[ArenaBuilder] No se pudo copiar " + Arena1ScenePath);
            return;
        }
        var scene = EditorSceneManager.OpenScene(Arena3ScenePath, OpenSceneMode.Single);

        // Rescata el sonido de muerte y el gruñido del demonio antes de borrarlo
        GameObject oldBoss = GameObject.Find("Enemy");
        AudioClip deathSound = null, growlSound = null;
        if (oldBoss != null)
        {
            var oldHealth = oldBoss.GetComponent<EnemyHealth>();
            if (oldHealth != null) deathSound = new SerializedObject(oldHealth).FindProperty("bossDeathSound").objectReferenceValue as AudioClip;
            var demon = oldBoss.GetComponent<DemonBoss>();
            if (demon != null) growlSound = new SerializedObject(demon).FindProperty("attackSound").objectReferenceValue as AudioClip;
        }

        // Lo que no va en una arena estrecha y sin plataformas
        DestroyIfExists("Enemy", "HazardSkullGroup", "FireWall_Left", "Platforms");

        float groundTop = GetColliderEdge("Suelo", top: true, fallback: -2.5f);
        int baseOrder = PlayerSortingOrder();

        NarrowArena(groundTop, baseOrder);

        Slider bossSlider = Resources.FindObjectsOfTypeAll<Slider>()
                                     .FirstOrDefault(s => s.name == "BossHealthSlider" && s.gameObject.scene == scene);

        GameObject boss = BuildOgre(idle[0], controller, groundTop, baseOrder, bossSlider, deathSound, growlSound);
        EnemyHealth bossHealth = boss.GetComponent<EnemyHealth>();

        BuildEdgeFire(groundTop, baseOrder - 1, baseOrder + 2, bossHealth, flames);

        // Es la última arena: muestra el resumen y la victoria, sin tienda ni puerta
        SetupBossRushObjects(scene, bossHealth, nextScene: "", offerUpgrades: false, showStartShop: false,
                             bossName: "El Guardián", fastTime: 75f, slowTime: 180f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ---------------- Arena estrecha ----------------

    /// <summary>Acerca las paredes, pone columnas y sombra en los límites, acerca la cámara y reubica al jugador.</summary>
    private static void NarrowArena(float groundTop, int baseOrder)
    {
        foreach (string wallName in new[] { "Wall_Left", "Wall_Right" })
        {
            GameObject wall = GameObject.Find(wallName);
            if (wall == null) continue;

            BoxCollider2D col = NormalizeScale(wall);
            if (col == null) continue;

            float side = wallName == "Wall_Left" ? -1f : 1f;
            // La cara interior de la pared queda exactamente en ±ArenaHalfWidth
            wall.transform.position = new Vector3(side * (ArenaHalfWidth + col.size.x * 0.5f), wall.transform.position.y, 0f);
        }

        Sprite pillar = AssetDatabase.LoadAssetAtPath<Sprite>(PillarSpritePath);
        Sprite black = AssetDatabase.LoadAssetAtPath<Sprite>(BlackSpritePath);

        foreach (float side in new[] { -1f, 1f })
        {
            string suffix = side < 0f ? "L" : "R";

            // Columna de piedra que marca el límite (2 u de ancho, de ArenaHalfWidth a ArenaHalfWidth + 2)
            var column = new GameObject("Columna_" + suffix);
            column.transform.position = new Vector3(side * (ArenaHalfWidth + 1f), groundTop - 1.2f, 0f);
            var csr = column.AddComponent<SpriteRenderer>();
            csr.sprite = pillar;
            csr.sortingOrder = baseOrder - 2;

            // Sombra: oscurece todo lo que queda fuera de la arena
            var shade = new GameObject("Sombra_" + suffix);
            shade.transform.position = new Vector3(side * (ArenaHalfWidth + 2f + 30f), 10f, 0f);
            var ssr = shade.AddComponent<SpriteRenderer>();
            ssr.sprite = black;
            ssr.drawMode = SpriteDrawMode.Tiled;
            ssr.tileMode = SpriteTileMode.Continuous;
            ssr.size = new Vector2(60f, 60f);
            ssr.sortingOrder = 40;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographicSize = 9.5f;
            cam.transform.position = new Vector3(0f, 4f, -10f);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = new Vector3(-4.5f, player.transform.position.y, player.transform.position.z);
    }

    // ---------------- El Guardián ----------------

    private static AnimatorController EnsureOgreAnimator()
    {
        string dir = OgreDir + "/Animations";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(OgreDir, "Animations");

        // Si ya existe se reutiliza: así no cambian los identificadores en cada ejecución
        string path = dir + "/Ogre.controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;

        var clips = new[]
        {
            MakeOgreClip(dir, "Idle", LoadSprites(OgreDir + "/Idle"), 0.18f, true),
            MakeOgreClip(dir, "Walk", LoadSprites(OgreDir + "/Walk"), 0.11f, true),
            MakeOgreClip(dir, "Attack", LoadSprites(OgreDir + "/Attack"), 0.09f, false),
            MakeOgreClip(dir, "IdleUnarmed", LoadSprites(OgreDir + "/IdleUnarmed"), 0.18f, true),
            MakeOgreClip(dir, "WalkUnarmed", LoadSprites(OgreDir + "/WalkUnarmed"), 0.11f, true),
        };

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        foreach (AnimationClip clip in clips)
        {
            AnimatorState state = sm.AddState(clip.name);
            state.motion = clip;
            if (clip.name == "Idle") sm.defaultState = state;
        }
        return controller;
    }

    private static AnimationClip MakeOgreClip(string dir, string name, Sprite[] frames, float frameTime, bool loop)
    {
        string path = $"{dir}/Ogre_{name}.anim";
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

    private static GameObject BuildOgre(Sprite firstFrame, AnimatorController controller, float groundTop, int sortingOrder,
                                        Slider slider, AudioClip deathSound, AudioClip growlSound)
    {
        var boss = new GameObject("ElGuardian") { layer = EnemyLayer };
        boss.transform.position = new Vector3(4.5f, groundTop, 0f);
        boss.transform.localScale = Vector3.one * OgreScale;

        var sr = boss.AddComponent<SpriteRenderer>();
        sr.sprite = firstFrame;
        sr.sortingOrder = sortingOrder;

        boss.AddComponent<Animator>().runtimeAnimatorController = controller;

        var rb = boss.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Cuerpo: el ogro mide unos 2.8 × 3.6 u sin escala (45 × 58 px a 16 PPU) y el pivote está al centro de sus pies
        var col = boss.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.8f, 3.4f);
        col.offset = new Vector2(0f, 1.8f);

        var audio = boss.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        var health = boss.AddComponent<EnemyHealth>();
        var hso = new SerializedObject(health);
        hso.FindProperty("maxHealth").intValue = 450;
        hso.FindProperty("bossHealthSlider").objectReferenceValue = slider;
        hso.FindProperty("bossDeathSound").objectReferenceValue = deathSound;
        hso.FindProperty("deathFadeDuration").floatValue = 1.2f;
        hso.FindProperty("damageNumberHeight").floatValue = 6f;
        hso.ApplyModifiedPropertiesWithoutUndo();

        var ai = boss.AddComponent<OgreBoss>();
        ai.SetArenaBounds(-ArenaHalfWidth + 1.5f, ArenaHalfWidth - 1.5f);
        var aso = new SerializedObject(ai);
        aso.FindProperty("attackSound").objectReferenceValue = growlSound;
        aso.FindProperty("warningSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);
        aso.ApplyModifiedPropertiesWithoutUndo();

        if (slider != null) slider.gameObject.SetActive(true);
        return boss;
    }

    // ---------------- Bordes de fuego ----------------

    /// <summary>
    /// Dos géiseres de fuego, uno en cada borde de la arena (2 u de ancho). Cada 20 s se encienden juntos
    /// durante 8 s (1 s de aviso antes) y queman 15 HP por segundo; el jefe es inmune.
    /// </summary>
    private static void BuildEdgeFire(float groundTop, int plateOrder, int flameOrder, EnemyHealth boss, Sprite[] flameSprites)
    {
        Sprite plate = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);

        var root = new GameObject("EdgeFire");
        var controller = root.AddComponent<FireFloorController>();

        const float ventWidth = 2f;
        const float ventHeight = 0.5f;
        var vents = new FireVent[2];

        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            var vent = new GameObject(i == 0 ? "EdgeVent_L" : "EdgeVent_R");
            vent.transform.SetParent(root.transform);
            vent.transform.position = new Vector3(side * (ArenaHalfWidth - ventWidth * 0.5f), groundTop + 0.05f - ventHeight * 0.5f, 0f);

            var sr = vent.AddComponent<SpriteRenderer>();
            sr.sprite = plate;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(ventWidth, ventHeight);
            sr.sortingOrder = plateOrder;

            vents[i] = vent.AddComponent<FireVent>();
            var vso = new SerializedObject(vents[i]);
            SerializedProperty sprites = vso.FindProperty("flameSprites");
            sprites.arraySize = flameSprites.Length;
            for (int k = 0; k < flameSprites.Length; k++) sprites.GetArrayElementAtIndex(k).objectReferenceValue = flameSprites[k];
            vso.FindProperty("flameSortingOrder").intValue = flameOrder;
            vso.FindProperty("flameWidth").floatValue = ventWidth - 0.1f;
            vso.FindProperty("flameHeight").floatValue = 9f;
            vso.FindProperty("damagePerTick").intValue = 15;
            vso.FindProperty("tickInterval").floatValue = 1f;     // 15 HP por segundo
            vso.ApplyModifiedPropertiesWithoutUndo();
        }

        var so = new SerializedObject(controller);
        SerializedProperty arr = so.FindProperty("vents");
        arr.arraySize = 2;
        for (int i = 0; i < 2; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = vents[i];
        so.FindProperty("boss").objectReferenceValue = boss;
        // Ciclo de 20 s: 11 s de calma + 1 s de aviso + 8 s de fuego
        so.FindProperty("initialDelay").floatValue = 11f;
        so.FindProperty("idleTime").floatValue = 11f;
        so.FindProperty("warningTime").floatValue = 1f;
        so.FindProperty("activeTime").floatValue = 8f;
        so.FindProperty("ventsPerCycle").intValue = 2;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}

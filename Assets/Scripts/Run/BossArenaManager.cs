using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla una arena del Boss Rush:
///   Jefe derrotado → (1.0 s) aparecen 2 pedestales con mejoras distintas → el jugador pisa uno →
///   el otro se destruye → (1.5 s) se abre la puerta a la siguiente arena.
/// Si es la última arena (sin escena siguiente), muestra la victoria y reinicia la partida.
/// </summary>
public class BossArenaManager : MonoBehaviour
{
    [Header("Jefe de esta arena")]
    [SerializeField] private EnemyHealth boss;

    [Header("Recompensa")]
    [Tooltip("Al derrotar al jefe, el jugador recupera toda su vida")]
    [SerializeField] private bool healPlayerOnVictory = true;

    [Header("Pedestales")]
    [SerializeField] private bool spawnPedestals = true;
    [SerializeField] private float pedestalSpawnDelay = 1.0f;
    [Tooltip("Posiciones (base) de los 2 pedestales")]
    [SerializeField] private Vector2[] pedestalPositions = { new Vector2(-3f, -2.5f), new Vector2(3f, -2.5f) };
    [SerializeField] private Sprite pedestalSprite;
    [SerializeField] private Sprite orbSprite;
    [SerializeField] private int sortingOrder = 4;

    [Header("Salida")]
    [SerializeField] private ExitDoor exitDoor;
    [SerializeField] private float doorOpenDelay = 1.5f;
    [Tooltip("Si es la última arena: segundos antes de volver a la Arena 1")]
    [SerializeField] private float finalVictoryDelay = 5f;

    [Header("Mensajes en pantalla")]
    [SerializeField] private Text messageText;

    private UpgradePedestal[] pedestals;
    private bool upgradeTaken;

    private void Start()
    {
        ShowMessage("");
        if (boss != null) boss.Died += _ => StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        bool isLastArena = exitDoor == null || string.IsNullOrEmpty(exitDoor.NextSceneName);

        if (healPlayerOnVictory && !isLastArena) HealPlayer();
        ShowMessage(healPlayerOnVictory && !isLastArena ? "¡Jefe derrotado!\nVida restaurada" : "¡Jefe derrotado!");
        yield return new WaitForSeconds(pedestalSpawnDelay);

        if (spawnPedestals)
        {
            SpawnPedestals();
            ShowMessage("Elige una mejora");
            yield return new WaitUntil(() => upgradeTaken);
        }

        if (isLastArena)
        {
            ShowMessage("¡VICTORIA!\nDerrotaste a todos los jefes");
            yield return new WaitForSeconds(finalVictoryDelay);
            RunState.Reset();
            SceneManager.LoadScene(0);
            yield break;
        }

        yield return new WaitForSeconds(doorOpenDelay);
        exitDoor.Open();
        ShowMessage("La puerta se abrió →");
    }

    private void HealPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth health = playerObj != null ? playerObj.GetComponent<PlayerHealth>() : null;
        if (health != null && !health.IsDead) health.SetHealth(health.MaxHealth, health.MaxHealth);
    }

    private void SpawnPedestals()
    {
        // Dos tipos distintos al azar entre Poder, Agilidad y Vital; cada uno tira su rareza
        UpgradeType first = (UpgradeType)Random.Range(0, 3);
        UpgradeType second = (UpgradeType)(((int)first + Random.Range(1, 3)) % 3);
        UpgradeType[] types = { first, second };

        pedestals = new UpgradePedestal[pedestalPositions.Length];
        for (int i = 0; i < pedestalPositions.Length; i++)
        {
            var upgrade = new Upgrade(types[i % types.Length], Upgrade.RollRarity());
            pedestals[i] = CreatePedestal(pedestalPositions[i], upgrade);
        }
    }

    private void OnPedestalTaken(UpgradePedestal taken)
    {
        upgradeTaken = true;
        foreach (UpgradePedestal p in pedestals)
        {
            if (p != null && p != taken) Destroy(p.gameObject);
        }
        ShowMessage($"{taken.Upgrade.Title} ({taken.Upgrade.RarityName}): {taken.Upgrade.EffectText}");
    }

    private UpgradePedestal CreatePedestal(Vector2 basePosition, Upgrade upgrade)
    {
        var root = new GameObject($"Pedestal_{upgrade.Type}");
        root.transform.position = basePosition;

        // Base de piedra
        var baseGo = new GameObject("Base");
        baseGo.transform.SetParent(root.transform, false);
        baseGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        var baseSr = baseGo.AddComponent<SpriteRenderer>();
        baseSr.sprite = pedestalSprite;
        baseSr.drawMode = SpriteDrawMode.Sliced;
        baseSr.size = new Vector2(1.4f, 1.0f);
        baseSr.color = new Color(0.45f, 0.42f, 0.5f);
        baseSr.sortingOrder = sortingOrder;

        // Orbe flotante teñido según la rareza
        var orbGo = new GameObject("Orbe");
        orbGo.transform.SetParent(root.transform, false);
        orbGo.transform.localPosition = new Vector3(0f, 1.9f, 0f);
        orbGo.transform.localScale = Vector3.one * 1.6f;
        var orbSr = orbGo.AddComponent<SpriteRenderer>();
        orbSr.sprite = orbSprite;
        orbSr.sortingOrder = sortingOrder + 1;

        // Texto en el mundo con el nombre, rareza y efecto
        var canvasGo = new GameObject("Etiqueta", typeof(RectTransform));
        canvasGo.transform.SetParent(root.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 4.2f, 0f);
        canvasGo.transform.localScale = Vector3.one * 0.03f;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = sortingOrder + 10;
        ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(360f, 120f);

        var textGo = new GameObject("Texto", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)textGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        var pedestal = root.AddComponent<UpgradePedestal>();
        pedestal.Setup(upgrade, orbSr, text, OnPedestalTaken);
        return pedestal;
    }

    private void ShowMessage(string message)
    {
        if (messageText != null) messageText.text = message;
    }
}

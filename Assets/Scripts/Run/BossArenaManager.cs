using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla una arena del Boss Rush.
///
/// Al empezar la primera arena de la partida se abre la tienda inicial para gastar los puntos de arranque.
/// Mientras dura el combate se miden el tiempo, los golpes recibidos y la precisión (CombatStats).
///
/// Al derrotar al jefe:
///   1. Resumen del rendimiento con la nota (S/A/B/C) y los puntos de mejora que gana.
///   2. Si hay una arena siguiente: se restaura la vida, se abre la tienda y luego la puerta (1.5 s después).
///   3. Si es la última arena: mensaje de victoria y vuelta al menú principal.
/// Esc abandona la partida y vuelve al menú.
/// </summary>
public class BossArenaManager : MonoBehaviour
{
    [Header("Jefe de esta arena")]
    [SerializeField] private EnemyHealth boss;
    [SerializeField] private string bossName = "El Jefe";

    [Header("Nota del combate")]
    [Tooltip("Tiempo (s) por debajo del cual se obtiene el máximo de puntos de tiempo")]
    [SerializeField] private float fastTime = 60f;
    [Tooltip("Tiempo (s) a partir del cual no se obtienen puntos de tiempo")]
    [SerializeField] private float slowTime = 150f;

    [Header("Mejoras")]
    [Tooltip("Abrir la tienda de puntos al vencer al jefe")]
    [SerializeField] private bool offerUpgrades = true;
    [Tooltip("Abrir la tienda antes del combate (solo en la primera arena de la partida)")]
    [SerializeField] private bool showStartShop = false;
    [Tooltip("Al derrotar al jefe, el jugador recupera toda su vida")]
    [SerializeField] private bool healPlayerOnVictory = true;
    [Tooltip("Segundos entre la muerte del jefe y la aparición del resumen")]
    [SerializeField] private float summaryDelay = 1.2f;

    [Header("Salida")]
    [SerializeField] private ExitDoor exitDoor;
    [SerializeField] private float doorOpenDelay = 1.5f;
    [Tooltip("Si es la última arena: segundos antes de volver al menú principal")]
    [SerializeField] private float finalVictoryDelay = 5f;

    [Header("Menú")]
    [Tooltip("Tecla para abandonar la partida y volver al menú principal")]
    [SerializeField] private KeyCode backToMenuKey = KeyCode.Escape;

    [Header("Mensajes en pantalla")]
    [SerializeField] private Text messageText;

    private void Start()
    {
        Time.timeScale = 1f;
        ShowMessage("");
        if (boss != null) boss.Died += _ => StartCoroutine(VictorySequence());
        StartCoroutine(StartSequence());
    }

    private void Update()
    {
        if (Input.GetKeyDown(backToMenuKey))
        {
            RunState.Reset();
            SceneManager.LoadScene(GameScenes.MainMenu);
        }
    }

    private IEnumerator StartSequence()
    {
        yield return null; // deja que PlayerRunStats aplique la build antes de abrir nada

        if (showStartShop && !RunState.StartShopDone)
        {
            RunState.StartShopDone = true;
            yield return RunPanels.ShowShop(true);
        }

        CombatStats.Begin();
    }

    private IEnumerator VictorySequence()
    {
        bool isLastArena = exitDoor == null || string.IsNullOrEmpty(exitDoor.NextSceneName);

        CombatStats.End();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth playerHealth = playerObj != null ? playerObj.GetComponent<PlayerHealth>() : null;
        float healthFraction = playerHealth != null ? playerHealth.CurrentHealth / (float)playerHealth.MaxHealth : 1f;
        PerformanceReport report = PerformanceReport.Build(fastTime, slowTime, healthFraction);

        ShowMessage("¡Jefe derrotado!");
        yield return new WaitForSeconds(summaryDelay);

        // Si el jugador murió mientras tanto, su reinicio de partida ya está en marcha
        if (playerHealth != null && playerHealth.IsDead) yield break;

        yield return RunPanels.ShowSummary(report, bossName, isLastArena);
        RunState.BossesDefeated++;

        if (isLastArena)
        {
            ShowMessage("¡VICTORIA!\nDerrotaste a todos los jefes");
            yield return new WaitForSeconds(finalVictoryDelay);
            RunState.Reset();
            SceneManager.LoadScene(GameScenes.MainMenu);
            yield break;
        }

        RunState.Points += report.Points;

        if (healPlayerOnVictory)
        {
            HealPlayer(playerHealth);
            ShowMessage("Vida restaurada");
        }

        if (offerUpgrades) yield return RunPanels.ShowShop(false);

        yield return new WaitForSeconds(doorOpenDelay);
        exitDoor.Open();
        ShowMessage("La puerta se abrió →");
    }

    private void HealPlayer(PlayerHealth health)
    {
        if (health != null && !health.IsDead) health.SetHealth(health.MaxHealth, health.MaxHealth);
    }

    private void ShowMessage(string message)
    {
        if (messageText != null) messageText.text = message;
    }
}

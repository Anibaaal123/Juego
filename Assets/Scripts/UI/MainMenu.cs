using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú de inicio: Jugar (empieza una partida nueva en la Arena 1), Controles y Salir.
/// Los botones llaman a estos métodos desde su evento OnClick.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject controlsPanel;
    [Tooltip("Botón seleccionado al abrir el menú (para jugar con teclado)")]
    [SerializeField] private Button firstButton;
    [SerializeField] private Button controlsBackButton;

    private void Start()
    {
        Time.timeScale = 1f; // por si se volvió al menú durante un hitstop
        ShowMain();
    }

    private void Update()
    {
        // Esc cierra el panel de controles
        if (controlsPanel != null && controlsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) ShowMain();
    }

    public void Play()
    {
        RunState.Reset();
        SceneManager.LoadScene(GameScenes.FirstArena);
    }

    public void ShowControls()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
        Select(controlsBackButton);
    }

    public void ShowMain()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        Select(firstButton);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Select(Button button)
    {
        if (button != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}

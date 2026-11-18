using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Puerta hacia la siguiente arena. Está oculta hasta que BossArenaManager la abre.
/// Al entrar, guarda la vida del jugador y carga la escena siguiente.
/// </summary>
public class ExitDoor : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private Vector2 enterBoxSize = new Vector2(2.0f, 4.0f);
    [SerializeField] private float pulseSpeed = 3f;

    private SpriteRenderer[] renderers;
    private bool isOpen;
    private bool used;

    public string NextSceneName
    {
        get => nextSceneName;
        set => nextSceneName = value;
    }

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        SetVisible(false);
    }

    public void Open()
    {
        isOpen = true;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        foreach (SpriteRenderer r in renderers) r.enabled = visible;
        foreach (Canvas c in GetComponentsInChildren<Canvas>(true)) c.enabled = visible;
    }

    private void Update()
    {
        if (!isOpen || used) return;

        float k = 0.65f + 0.35f * Mathf.Sin(Time.time * pulseSpeed);
        foreach (SpriteRenderer r in renderers)
        {
            Color c = r.color;
            r.color = new Color(c.r, c.g, c.b, k);
        }

        Vector2 center = (Vector2)transform.position + new Vector2(0f, enterBoxSize.y * 0.5f);
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, enterBoxSize, 0f))
        {
            PlayerRunStats stats = hit.GetComponentInParent<PlayerRunStats>();
            if (stats == null) continue;

            used = true;
            stats.SaveForNextArena();
            SceneManager.LoadScene(nextSceneName);
            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + new Vector3(0f, enterBoxSize.y * 0.5f, 0f), enterBoxSize);
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Necesario para recargar la escena

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Configuration")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Slider healthSlider;

    [Header("Restart Settings")]
    [Tooltip("Tiempo en segundos antes de recargar la escena tras morir")]
    [SerializeField] private float restartDelay = 2.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private Rigidbody2D rb;

    [Header("Habilidad: Segundo aliento")]
    [SerializeField] private float secondWindInvulnerability = 1.5f;

    private int currentHealth;
    private bool isDead = false;
    private float invulnerableUntil = -1f;
    private PlayerGuard guard;

    [HideInInspector] public bool isInvulnerable = false;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    /// <summary>Fija la vida máxima y la actual (mejoras y vida que se arrastra entre arenas).</summary>
    public void SetHealth(int newMax, int newCurrent)
    {
        maxHealth = Mathf.Max(1, newMax);
        currentHealth = Mathf.Clamp(newCurrent, 0, maxHealth);
        UpdateHealthUI();
    }

    /// <summary>Recupera vida sin pasar del máximo.</summary>
    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUI();
    }

    private void Awake()
    {
        currentHealth = maxHealth;

        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerAttack == null) playerAttack = GetComponent<PlayerAttack>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        UpdateHealthUI();
    }

    private void Update()
    {
        // Presionar la tecla R en cualquier momento reinicia la partida inmediatamente
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }
    }

    /// <summary>
    /// Aplica daño al jugador. Los golpes de los enemigos se pueden cubrir (daño reducido) o parar con la habilidad
    /// Parry (daño anulado); el fuego del piso y otros peligros del escenario usan unblockable = true.
    /// </summary>
    public void TakeDamage(int damage, bool unblockable = false)
    {
        if (isDead || isInvulnerable || Time.time < invulnerableUntil) return;

        if (!unblockable)
        {
            if (guard == null) guard = GetComponent<PlayerGuard>();
            if (guard != null)
            {
                if (guard.TryAbsorb()) return;                          // Parry: el golpe se anula por completo
                if (guard.IsGuarding) damage = guard.Block(damage);     // Cubierto: se recibe solo una parte
            }
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        CombatStats.RegisterHit(damage);

        // Habilidad Segundo aliento: una vez por partida, en vez de morir se revive con parte de la vida
        if (currentHealth <= 0 && TryUseSecondWind()) return;

        UpdateHealthUI();
        GameFeel.Shake(0.2f, 0.15f);

        if (audioSource != null && hurtSound != null && currentHealth > 0)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        if (animator != null && currentHealth > 0)
        {
            animator.SetTrigger("Hurt");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private bool TryUseSecondWind()
    {
        if (!RunState.Has(SkillType.SegundoAliento) || RunState.SecondWindUsed) return false;

        RunState.SecondWindUsed = true;
        currentHealth = Mathf.Max(1, Mathf.CeilToInt(maxHealth * BuildCatalog.SecondWindHealthFraction));
        UpdateHealthUI();
        StartCoroutine(SecondWindRoutine());
        return true;
    }

    private IEnumerator SecondWindRoutine()
    {
        invulnerableUntil = Time.time + secondWindInvulnerability;
        GameFeel.Shake(0.5f, 0.3f);
        GameFeel.HitStop(0.1f);
        GameFeel.FloatingText(transform.position + new Vector3(0f, 2.8f, 0f), "¡SEGUNDO ALIENTO!", new Color(1f, 0.85f, 0.3f), 1.5f);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float t = 0f;
        while (t < secondWindInvulnerability)
        {
            if (sr != null) sr.color = Mathf.Repeat(t * 10f, 1f) < 0.5f ? new Color(1f, 1f, 0.6f, 0.6f) : Color.white;
            t += Time.deltaTime;
            yield return null;
        }
        if (sr != null) sr.color = Color.white;
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerAttack != null) playerAttack.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }

        // Inicia la cuenta regresiva para reiniciar la escena tras la derrota
        StartCoroutine(RestartAfterDelayRoutine());
    }

    private IEnumerator RestartAfterDelayRoutine()
    {
        yield return new WaitForSeconds(restartDelay);
        RestartScene();
    }

    /// <summary>
    /// Boss rush con 1 sola vida: al morir (o con R) se reinicia la partida completa desde la Arena 1
    /// y se pierden las mejoras obtenidas.
    /// </summary>
    public void RestartScene()
    {
        Time.timeScale = 1f; // por si se reinicia con un panel (que pausa el juego) abierto
        RunState.Reset();
        SceneManager.LoadScene(GameScenes.FirstArena);
    }
}
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 150;
    [SerializeField] private Slider bossHealthSlider;

    [Header("Audio & Effects")]
    [SerializeField] private AudioClip bossDeathSound;
    [Tooltip("Duración del colapso del jefe antes de desaparecer (GDD: 1.0 s)")]
    [SerializeField] private float deathFadeDuration = 1.0f;
    [Tooltip("Tiempo total desde la muerte hasta reiniciar la escena")]
    [SerializeField] private float victoryRestartDelay = 3.5f;
    [Tooltip("Si está activo, la escena se reinicia al morir el jefe (comportamiento del prototipo)")]
    [SerializeField] private bool restartSceneOnDeath = true;

    [Header("Hit Feedback")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private bool showDamageNumbers = true;
    [Tooltip("Altura (sobre el pivote) donde aparecen los números de daño")]
    [SerializeField] private float damageNumberHeight = 3f;

    /// <summary>Se dispara una sola vez cuando la vida llega a 0.</summary>
    public event Action<EnemyHealth> Died;

    /// <summary>Se dispara cada vez que recibe daño (cantidad aplicada).</summary>
    public event Action<int> Damaged;

    /// <summary>Mientras sea true, el enemigo ignora todo el daño recibido.</summary>
    [HideInInspector] public bool isInvulnerable = false;

    /// <summary>Color normal del sprite (al que vuelve tras parpadear por un golpe).</summary>
    public Color BaseColor { get; set; } = Color.white;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private int currentHealth;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private Coroutine flashRoutine;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = maxHealth;
            bossHealthSlider.value = currentHealth;
        }
    }

    /// <summary>Aplica daño. Devuelve true si el golpe contó (no estaba muerto ni invulnerable).</summary>
    public bool TakeDamage(int damage)
    {
        if (isDead || isInvulnerable) return false;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (showDamageNumbers)
        {
            GameFeel.DamageNumber(transform.position + new Vector3(0f, damageNumberHeight, 0f), damage,
                                  damage >= 30 ? new Color(1f, 0.75f, 0.2f) : Color.white, damage >= 30 ? 1.4f : 1f);
        }
        Damaged?.Invoke(damage);

        if (bossHealthSlider != null)
        {
            bossHealthSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(DieRoutine());
        }
        else if (spriteRenderer != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HitFlashRoutine());
        }
        return true;
    }

    /// <summary>Empuja al enemigo 'distance' unidades en 'direction' durante 0.08 s.</summary>
    public void Knockback(Vector2 direction, float distance)
    {
        if (isDead || distance <= 0f) return;
        StartCoroutine(KnockbackRoutine(direction.normalized * distance, 0.08f));
    }

    private IEnumerator KnockbackRoutine(Vector2 total, float duration)
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        float t = 0f;
        while (t < duration)
        {
            float dt = Time.deltaTime;
            Vector2 step = total * (dt / duration);
            transform.position += (Vector3)step;
            if (body != null) body.position = transform.position;
            t += dt;
            yield return null;
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = BaseColor;
        flashRoutine = null;
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        if (flashRoutine != null) StopCoroutine(flashRoutine);

        if (bossHealthSlider != null)
        {
            bossHealthSlider.gameObject.SetActive(false);
        }

        if (bossDeathSound != null)
        {
            AudioSource.PlayClipAtPoint(bossDeathSound, transform.position, 1.0f);
        }

        // Apaga la IA y el daño del enemigo (cualquier script de comportamiento en este objeto)
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour != this) behaviour.enabled = false;
        }
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.speed = 0f;

        Died?.Invoke(this);

        // Colapso: el sprite se desvanece en deathFadeDuration segundos
        if (spriteRenderer != null)
        {
            float t = 0f;
            Color start = Color.white;
            while (t < deathFadeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / deathFadeDuration);
                spriteRenderer.color = new Color(start.r, start.g * (1f - k * 0.6f), start.b * (1f - k * 0.6f), 1f - k);
                yield return null;
            }
            spriteRenderer.enabled = false;
        }

        if (!restartSceneOnDeath) yield break;

        yield return new WaitForSeconds(Mathf.Max(0f, victoryRestartDelay - deathFadeDuration));
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

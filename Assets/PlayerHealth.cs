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

    private int currentHealth;
    private bool isDead = false;

    [HideInInspector] public bool isInvulnerable = false;

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

    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthUI();

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

    public void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
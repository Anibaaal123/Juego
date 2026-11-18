using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Dash / Dodge Settings")]
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1.0f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip dashSound;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerHealth playerHealth;
    private PlayerAttack playerAttack;
    private float horizontalInput;
    private bool isGrounded;
    private bool canDash = true;
    private bool isDashing = false;

    // Ralentización temporal (por ejemplo, baldosas eléctricas)
    private float speedMultiplier = 1f;
    private float slowEndTime = 0f;

    // Retroceso: mientras dure, el jugador no controla la velocidad horizontal
    private float knockbackEndTime = 0f;

    /// <summary>True si los pies tocan el suelo o una plataforma.</summary>
    public bool IsGrounded => isGrounded;

    /// <summary>Tiempo de espera entre dashes. Lo ajusta PlayerRunStats al aplicar mejoras.</summary>
    public float DashCooldown
    {
        get => dashCooldown;
        set => dashCooldown = Mathf.Max(0.05f, value);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        playerAttack = GetComponent<PlayerAttack>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (isDashing) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Detección de suelo
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        }

        // Actualiza el Animator con velocidad horizontal y estado de suelo
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
            animator.SetBool("IsGrounded", isGrounded);
        }

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // Dash con Shift
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(PerformDashRoutine());
        }

        FlipSprite();
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        if (speedMultiplier < 1f && Time.time >= slowEndTime) speedMultiplier = 1f;
        if (Time.time < knockbackEndTime) return;

        float attackMultiplier = playerAttack != null ? playerAttack.MoveSpeedMultiplier : 1f;
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed * speedMultiplier * attackMultiplier, rb.linearVelocity.y);
    }

    /// <summary>Lanza al jugador con 'velocity' y le quita el control horizontal durante 'duration' segundos.</summary>
    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        rb.linearVelocity = velocity;
        knockbackEndTime = Time.time + duration;
    }

    /// <summary>
    /// Reduce la velocidad de desplazamiento a moveSpeed * multiplier durante 'duration' segundos.
    /// Si ya hay una ralentización activa, se conserva la más fuerte y se renueva la duración.
    /// </summary>
    public void ApplySlow(float multiplier, float duration)
    {
        multiplier = Mathf.Clamp01(multiplier);
        if (Time.time >= slowEndTime || multiplier < speedMultiplier) speedMultiplier = multiplier;
        slowEndTime = Mathf.Max(slowEndTime, Time.time + duration);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
    }

    private IEnumerator PerformDashRoutine()
    {
        canDash = false;
        isDashing = true;

        if (playerHealth != null) playerHealth.isInvulnerable = true;
        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);

        if (audioSource != null && dashSound != null)
        {
            audioSource.PlayOneShot(dashSound);
        }

        float dashDirection = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
        if (horizontalInput != 0) dashDirection = Mathf.Sign(horizontalInput);

        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        if (playerHealth != null) playerHealth.isInvulnerable = false;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void FlipSprite()
    {
        if (spriteRenderer == null) return;

        if (horizontalInput > 0.01f)
        {
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
}
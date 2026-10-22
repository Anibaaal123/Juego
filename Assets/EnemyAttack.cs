using System.Collections;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Flight & Movement")]
    [SerializeField] private float flightSpeed = 2.8f;
    [SerializeField] private float stoppingDistance = 5.2f;
    [SerializeField] private float detectionRange = 16f;
    [Tooltip("Altura deseada respecto al jugador para lanzar el ataque desde el aire")]
    [SerializeField] private float preferredHeightOffset = 1.2f;
    [SerializeField] private float floatFrequency = 2.5f;
    [SerializeField] private float floatAmplitude = 0.4f;

    [Header("Combat Settings")]
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 fireAreaSize = new Vector2(4.5f, 2.5f);
    [SerializeField] private LayerMask playerLayer;

    [Header("Attack Timings")]
    [SerializeField] private float fireStartDelay = 0.65f;
    [SerializeField] private float flameDuration = 0.5f;
    [SerializeField] private float recoveryDelay = 0.3f;

    [Header("Components & Audio")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 10; 
    [SerializeField] private float contactDamageCooldown = 0.8f; 
    private float nextContactDamageTime = 0f;

    
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Time.time >= nextContactDamageTime)
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(contactDamage);
                nextContactDamageTime = Time.time + contactDamageCooldown;
            }
        }
    }

    private Transform playerTransform;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        LookAtPlayer();

        // Si está en rango de detección, persigue al jugador en el aire (X e Y)
        if (distanceToPlayer <= detectionRange && distanceToPlayer > stoppingDistance)
        {
            FlyTowardsPlayer();
        }
        else
        {
            HoverInPlace();
        }

        // Si está en distancia de combate y terminó el cooldown, ejecuta la ráfaga
        if (distanceToPlayer <= stoppingDistance && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(PerformAttackRoutine());
        }
    }

    private void FlyTowardsPlayer()
    {
        // Calcula la posición objetivo en el aire (manteniendo una ligera elevación sobre el héroe)
        Vector2 targetPosition = new Vector2(
            playerTransform.position.x,
            playerTransform.position.y + preferredHeightOffset
        );

        // Añade una suave ondulación vertical para simular aleteo
        float wave = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        targetPosition.y += wave;

        Vector2 flightDirection = (targetPosition - (Vector2)transform.position).normalized;

        if (rb != null)
        {
            rb.linearVelocity = flightDirection * flightSpeed;
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, flightSpeed * Time.deltaTime);
        }
    }

    private void HoverInPlace()
    {
        // Al detenerse a distancia de ataque, mantiene una ligera flotación en el sitio
        float wave = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, wave);
        }
        else
        {
            transform.position += new Vector3(0f, wave * Time.deltaTime, 0f);
        }
    }

    private void LookAtPlayer()
    {
        if (spriteRenderer == null) return;

        bool shouldFlip = playerTransform.position.x > transform.position.x;
        spriteRenderer.flipX = shouldFlip;

        if (attackPoint != null)
        {
            float xOffset = Mathf.Abs(attackPoint.localPosition.x);
            attackPoint.localPosition = new Vector3(shouldFlip ? xOffset : -xOffset, attackPoint.localPosition.y, 0f);
        }
    }

    private IEnumerator PerformAttackRoutine()
    {
        isAttacking = true;
        HoverInPlace();
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        yield return new WaitForSeconds(fireStartDelay);

        float timer = 0f;
        bool hasDamagedPlayer = false;

        while (timer < flameDuration)
        {
            Vector2 checkPos = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
            Collider2D hit = Physics2D.OverlapBox(checkPos, fireAreaSize, 0f, playerLayer);

            if (hit != null && !hasDamagedPlayer)
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    hasDamagedPlayer = true;
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(recoveryDelay);
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(attackPoint.position, fireAreaSize);
        }
    }
}
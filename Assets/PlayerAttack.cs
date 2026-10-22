using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackDuration = 0.35f;
    [SerializeField] private float attackCooldown = 0.15f;

    [Header("Detection Area")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1.8f; // Radio ligeramente mayor para alcanzar al jefe en el aire
    [SerializeField] private LayerMask enemyLayers;

    [Header("Components & Audio")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swordSlashSound;

    private bool isAttacking = false;
    private float nextAllowedAttackTime = 0f;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        UpdateAttackPointPosition();

        if (Input.GetButtonDown("Fire1") && !isAttacking && Time.time >= nextAllowedAttackTime)
        {
            StartCoroutine(PerformAttackRoutine());
        }
    }

    // Asegura que el punto de impacto siempre mire al frente del personaje
    private void UpdateAttackPointPosition()
    {
        if (attackPoint == null || spriteRenderer == null) return;

        float xOffset = Mathf.Abs(attackPoint.localPosition.x);
        bool isFacingLeft = spriteRenderer.flipX;

        attackPoint.localPosition = new Vector3(isFacingLeft ? -xOffset : xOffset, attackPoint.localPosition.y, 0f);
    }

    private IEnumerator PerformAttackRoutine()
    {
        isAttacking = true;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (audioSource != null && swordSlashSound != null)
        {
            audioSource.PlayOneShot(swordSlashSound);
        }

        yield return new WaitForSeconds(attackDuration * 0.4f);

        if (attackPoint != null)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

            if (hitEnemies.Length == 0)
            {
                Debug.Log("[PLAYER] Espadazo al aire (no tocó ningún enemigo).");
            }

            foreach (Collider2D enemy in hitEnemies)
            {
                // Busca el componente en el collider tocado o en su objeto padre
                EnemyHealth bossHealth = enemy.GetComponent<EnemyHealth>();
                if (bossHealth == null)
                {
                    bossHealth = enemy.GetComponentInParent<EnemyHealth>();
                }

                if (bossHealth != null)
                {
                    bossHealth.TakeDamage(attackDamage);
                }
            }
        }

        yield return new WaitForSeconds(attackDuration * 0.6f);

        isAttacking = false;
        nextAllowedAttackTime = Time.time + attackCooldown;
    }

    public void ApplyDamageBuff(int bonus)
    {
        attackDamage += bonus;
        Debug.Log($"[PLAYER] Daño aumentado permanentemente en +{bonus}. Daño total: {attackDamage}");
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
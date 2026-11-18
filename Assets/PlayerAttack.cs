using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ataques del jugador (clic izquierdo / Fire1):
///   • Normal: en el suelo, tocar y soltar. Daño = attackDamage.
///   • Cargado: en el suelo, mantener 0.6 s y soltar. Daño ×3.5, alcance ×1.5 y empuje fuerte.
///     Mientras carga se mueve al 35 % de velocidad.
/// En el aire no se puede cargar: el ataque normal sale al instante.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    private enum AttackKind { Normal, Charged }

    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackDuration = 0.35f;
    [SerializeField] private float attackCooldown = 0.15f;

    [Header("Detection Area")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1.8f; // Radio ligeramente mayor para alcanzar al jefe en el aire
    [SerializeField] private LayerMask enemyLayers;

    [Header("Ataque cargado")]
    [SerializeField] private float chargeTime = 0.6f;
    [SerializeField] private float chargedDamageMultiplier = 3.5f;
    [SerializeField] private float chargedRangeMultiplier = 1.5f;
    [SerializeField] private float chargedAttackDuration = 0.5f;
    [Tooltip("Velocidad de movimiento mientras carga (fracción de la normal)")]
    [SerializeField] private float chargeMoveMultiplier = 0.35f;
    [SerializeField] private Color chargingColor = new Color(1f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color fullChargeColor = new Color(1f, 1f, 0.75f, 1f);

    [Header("Game feel")]
    [SerializeField] private float normalHitStop = 0.05f;
    [SerializeField] private float chargedHitStop = 0.12f;
    [SerializeField] private float normalShake = 0.08f;
    [SerializeField] private float chargedShake = 0.3f;
    [SerializeField] private float normalKnockback = 0.35f;
    [SerializeField] private float chargedKnockback = 1.2f;

    [Header("Components & Audio")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swordSlashSound;

    private bool isAttacking = false;
    private float nextAllowedAttackTime = 0f;
    private bool isCharging = false;
    private float chargeStartTime;
    private PlayerMovement movement;

    /// <summary>Daño por golpe. Lo ajusta PlayerRunStats al aplicar mejoras.</summary>
    public int AttackDamage
    {
        get => attackDamage;
        set => attackDamage = Mathf.Max(1, value);
    }

    /// <summary>Multiplicador de velocidad que PlayerMovement aplica mientras se carga el ataque.</summary>
    public float MoveSpeedMultiplier => isCharging ? chargeMoveMultiplier : 1f;

    private bool IsFullyCharged => isCharging && Time.time - chargeStartTime >= chargeTime;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        movement = GetComponent<PlayerMovement>();
    }

    private void OnDisable()
    {
        CancelCharge();
    }

    private void Update()
    {
        UpdateAttackPointPosition();

        bool grounded = movement == null || movement.IsGrounded;
        bool canAttack = !isAttacking && Time.time >= nextAllowedAttackTime;

        if (Input.GetButtonDown("Fire1") && canAttack)
        {
            if (!grounded)
            {
                StartCoroutine(PerformAttackRoutine(AttackKind.Normal));
            }
            else
            {
                isCharging = true;
                chargeStartTime = Time.time;
            }
        }

        if (isCharging)
        {
            UpdateChargeVisual();

            if (!Input.GetButton("Fire1"))
            {
                AttackKind kind = IsFullyCharged ? AttackKind.Charged : AttackKind.Normal;
                CancelCharge();
                StartCoroutine(PerformAttackRoutine(kind));
            }
        }
    }

    private void UpdateChargeVisual()
    {
        if (spriteRenderer == null) return;
        float held = Time.time - chargeStartTime;
        if (held < 0.12f) return; // un toque rápido no muestra carga

        if (IsFullyCharged)
        {
            // Carga completa: parpadea para avisar que ya se puede soltar
            bool on = Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
            spriteRenderer.color = on ? fullChargeColor : Color.white;
        }
        else
        {
            spriteRenderer.color = Color.Lerp(Color.white, chargingColor, held / chargeTime);
        }
    }

    private void CancelCharge()
    {
        if (isCharging && spriteRenderer != null) spriteRenderer.color = Color.white;
        isCharging = false;
    }

    // Asegura que el punto de impacto siempre mire al frente del personaje
    private void UpdateAttackPointPosition()
    {
        if (attackPoint == null || spriteRenderer == null) return;

        float xOffset = Mathf.Abs(attackPoint.localPosition.x);
        bool isFacingLeft = spriteRenderer.flipX;

        attackPoint.localPosition = new Vector3(isFacingLeft ? -xOffset : xOffset, attackPoint.localPosition.y, 0f);
    }

    private IEnumerator PerformAttackRoutine(AttackKind kind)
    {
        isAttacking = true;
        float duration = kind == AttackKind.Charged ? chargedAttackDuration : attackDuration;
        float windupTime = duration * 0.4f;

        if (animator != null) animator.SetTrigger("Attack");
        if (audioSource != null && swordSlashSound != null)
        {
            audioSource.pitch = kind == AttackKind.Charged ? 0.8f : 1f;
            audioSource.PlayOneShot(swordSlashSound);
        }

        yield return new WaitForSeconds(windupTime);

        Vector2 center;
        float radius;
        int damage;
        switch (kind)
        {
            case AttackKind.Charged:
                Vector2 forward = new Vector2(spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f, 0f);
                center = (attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position) + forward * 0.6f;
                radius = attackRange * chargedRangeMultiplier;
                damage = Mathf.RoundToInt(attackDamage * chargedDamageMultiplier);
                break;
            default:
                center = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
                radius = attackRange;
                damage = attackDamage;
                break;
        }

        ShowSlash(kind, center);
        bool hitSomething = DealDamage(center, radius, damage, kind);

        if (hitSomething)
        {
            bool strong = kind == AttackKind.Charged;
            GameFeel.HitStop(strong ? chargedHitStop : normalHitStop);
            GameFeel.Shake(strong ? chargedShake : normalShake, strong ? 0.25f : 0.12f);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, duration - windupTime));

        if (audioSource != null) audioSource.pitch = 1f;
        isAttacking = false;
        nextAllowedAttackTime = Time.time + attackCooldown;
    }

    /// <summary>Daña a todos los enemigos dentro del círculo (una vez cada uno). Devuelve si acertó a alguno.</summary>
    private bool DealDamage(Vector2 center, float radius, int damage, AttackKind kind)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayers);
        var alreadyHit = new HashSet<EnemyHealth>();
        bool any = false;

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !alreadyHit.Add(enemy)) continue;
            if (!enemy.TakeDamage(damage)) continue;

            any = true;
            Vector2 away = new Vector2(Mathf.Sign(enemy.transform.position.x - transform.position.x), 0f);
            float push = kind == AttackKind.Charged ? chargedKnockback : normalKnockback;
            enemy.Knockback(away, push);
        }
        return any;
    }

    /// <summary>Medialuna visible que muestra hacia dónde va el golpe.</summary>
    private void ShowSlash(AttackKind kind, Vector2 center)
    {
        int order = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 10;
        bool left = spriteRenderer != null && spriteRenderer.flipX;
        switch (kind)
        {
            case AttackKind.Charged:
                ProceduralFx.Slash(transform.position, left ? -90f : 90f, 2.0f, new Color(1f, 0.85f, 0.35f, 1f), order);
                break;
            default:
                ProceduralFx.Slash(transform.position, left ? -90f : 90f, 1.2f, new Color(1f, 1f, 1f, 0.75f), order);
                break;
        }
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
            Gizmos.color = new Color(1f, 0.6f, 0f);
            Gizmos.DrawWireSphere(attackPoint.position, attackRange * chargedRangeMultiplier);
        }
    }
}

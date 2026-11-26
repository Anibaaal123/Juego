using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ataques del jugador (clic izquierdo / Fire1):
///   • Normal: en el suelo, tocar y soltar. Daño = attackDamage.
///   • Cargado: en el suelo, mantener 0.6 s y soltar. Daño ×3.5, alcance ×1.5 y empuje fuerte.
///     Mientras carga se mueve al 35 % de velocidad.
/// En el aire no se puede cargar: el ataque normal sale al instante.
///
/// Habilidades (se compran en la tienda, ver RunState):
///   • Combo de 3 golpes: clics seguidos (menos de 0.55 s entre uno y otro) encadenan 3 golpes;
///     el tercero hace ×1.8 de daño, tiene más alcance y empuja al jefe.
///   • Onda de espada: el golpe cargado lanza además una onda de corte a distancia (×1.5 del daño base).
///   • Vampirismo: cada golpe que acierta cura 2 HP.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    private enum AttackKind { Normal, Charged, Finisher }

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

    [Header("Habilidad: Combo de 3 golpes")]
    [Tooltip("Tiempo máximo entre el final de un golpe y el siguiente clic para seguir el combo")]
    [SerializeField] private float comboWindow = 0.55f;
    [SerializeField] private float finisherDamageMultiplier = 1.8f;
    [SerializeField] private float finisherRangeMultiplier = 1.2f;
    [SerializeField] private float finisherDuration = 0.45f;
    [SerializeField] private float finisherKnockback = 1.0f;

    [Header("Habilidad: Onda de espada")]
    [SerializeField] private float waveDamageMultiplier = 1.5f;

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
    private int comboStep;
    private float lastAttackEndTime = -999f;
    private PlayerMovement movement;
    private PlayerHealth health;
    private PlayerGuard guard;

    /// <summary>Daño por golpe. Lo ajusta PlayerRunStats según el atributo Daño.</summary>
    public int AttackDamage
    {
        get => attackDamage;
        set => attackDamage = Mathf.Max(1, value);
    }

    /// <summary>Segundos de carga del golpe cargado. Lo ajusta PlayerRunStats según el atributo Carga.</summary>
    public float ChargeTime
    {
        get => chargeTime;
        set => chargeTime = Mathf.Max(0.2f, value);
    }

    /// <summary>True durante un golpe (desde que sale hasta que termina su recuperación).</summary>
    public bool IsAttacking => isAttacking;

    /// <summary>Multiplicador de velocidad que PlayerMovement aplica mientras se carga el ataque.</summary>
    public float MoveSpeedMultiplier => isCharging ? chargeMoveMultiplier : 1f;

    private bool IsFullyCharged => isCharging && Time.time - chargeStartTime >= chargeTime;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
    }

    private void OnDisable()
    {
        CancelCharge();
    }

    private void Update()
    {
        UpdateAttackPointPosition();

        // La defensa se agrega por código al entrar a la arena: se busca perezosamente
        if (guard == null) guard = GetComponent<PlayerGuard>();
        bool guarding = guard != null && guard.IsGuarding;
        if (guarding && isCharging) CancelCharge();

        bool grounded = movement == null || movement.IsGrounded;
        bool canAttack = !isAttacking && !guarding && Time.time >= nextAllowedAttackTime;

        if (Input.GetButtonDown("Fire1") && canAttack)
        {
            if (!grounded)
            {
                BeginNormalAttack();
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
                bool charged = IsFullyCharged;
                CancelCharge();
                if (charged)
                {
                    comboStep = 0;
                    StartCoroutine(PerformAttackRoutine(AttackKind.Charged));
                }
                else
                {
                    BeginNormalAttack();
                }
            }
        }
    }

    /// <summary>Lanza un golpe normal; con la habilidad Combo, el tercer golpe seguido es el remate.</summary>
    private void BeginNormalAttack()
    {
        AttackKind kind = AttackKind.Normal;

        if (RunState.Has(SkillType.Combo))
        {
            if (Time.time - lastAttackEndTime > comboWindow) comboStep = 0;
            comboStep++;
            if (comboStep >= 3)
            {
                kind = AttackKind.Finisher;
                comboStep = 0;
            }
        }
        else
        {
            comboStep = 0;
        }

        StartCoroutine(PerformAttackRoutine(kind));
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
        CombatStats.RegisterSwing();

        float duration = kind == AttackKind.Charged ? chargedAttackDuration
                       : kind == AttackKind.Finisher ? finisherDuration
                       : attackDuration;
        float windupTime = duration * 0.4f;

        if (animator != null) animator.SetTrigger("Attack");
        if (audioSource != null && swordSlashSound != null)
        {
            audioSource.pitch = kind == AttackKind.Charged ? 0.8f : kind == AttackKind.Finisher ? 0.9f : 1f;
            audioSource.PlayOneShot(swordSlashSound);
        }

        yield return new WaitForSeconds(windupTime);

        Vector2 forward = new Vector2(spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f, 0f);
        Vector2 basePoint = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;

        Vector2 center;
        float radius;
        int damage;
        switch (kind)
        {
            case AttackKind.Charged:
                center = basePoint + forward * 0.6f;
                radius = attackRange * chargedRangeMultiplier;
                damage = Mathf.RoundToInt(attackDamage * chargedDamageMultiplier);
                break;
            case AttackKind.Finisher:
                center = basePoint + forward * 0.3f;
                radius = attackRange * finisherRangeMultiplier;
                damage = Mathf.RoundToInt(attackDamage * finisherDamageMultiplier);
                break;
            default:
                center = basePoint;
                radius = attackRange;
                damage = attackDamage;
                break;
        }

        ShowSlash(kind);
        bool hitSomething = DealDamage(center, radius, damage, kind);
        CombatStats.RegisterSwingResult(hitSomething, kind == AttackKind.Charged);

        if (hitSomething)
        {
            bool strong = kind == AttackKind.Charged;
            bool medium = kind == AttackKind.Finisher;
            GameFeel.HitStop(strong ? chargedHitStop : medium ? 0.08f : normalHitStop);
            GameFeel.Shake(strong ? chargedShake : medium ? 0.2f : normalShake, strong ? 0.25f : 0.12f);

            if (RunState.Has(SkillType.Vampirismo) && health != null && !health.IsDead)
            {
                health.Heal(BuildCatalog.VampirismHeal);
                GameFeel.FloatingText(transform.position + new Vector3(0f, 2.2f, 0f), $"+{BuildCatalog.VampirismHeal}",
                                      new Color(0.45f, 1f, 0.5f), 0.9f);
            }
        }

        if (kind == AttackKind.Charged && RunState.Has(SkillType.OndaEspada))
        {
            bool left = spriteRenderer != null && spriteRenderer.flipX;
            int order = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 10;
            SwordWave.Spawn(basePoint, left, Mathf.RoundToInt(attackDamage * waveDamageMultiplier), order);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, duration - windupTime));

        if (audioSource != null) audioSource.pitch = 1f;
        isAttacking = false;
        lastAttackEndTime = Time.time;
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
            float push = kind == AttackKind.Charged ? chargedKnockback : kind == AttackKind.Finisher ? finisherKnockback : normalKnockback;
            enemy.Knockback(away, push);
        }
        return any;
    }

    /// <summary>Medialuna visible que muestra hacia dónde va el golpe.</summary>
    private void ShowSlash(AttackKind kind)
    {
        int order = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 10;
        bool left = spriteRenderer != null && spriteRenderer.flipX;
        float angle = left ? -90f : 90f;

        switch (kind)
        {
            case AttackKind.Charged:
                ProceduralFx.Slash(transform.position, angle, 2.0f, new Color(1f, 0.85f, 0.35f, 1f), order);
                break;
            case AttackKind.Finisher:
                ProceduralFx.Slash(transform.position, angle, 1.6f, new Color(1f, 0.6f, 0.3f, 1f), order);
                break;
            default:
                ProceduralFx.Slash(transform.position, angle, 1.2f, new Color(1f, 1f, 1f, 0.75f), order);
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

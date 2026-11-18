using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe 2 — El Centinela (jefe de rango).
/// Mantiene distancia con el jugador desplazándose por el suelo a 4.0 u/s y alterna tres ataques:
///   1. Bola de fuego: telegrafía 0.45 s y dispara 1 proyectil apuntado (15 HP).
///   2. Abanico: cada 3er ataque a distancia, telegrafía 0.75 s y dispara 3 proyectiles (12 HP c/u).
///   3. Pilar de fuego: si el jugador se acerca a menos de 3.0 u, telegrafía 0.6 s, se convierte en
///      un pilar de fuego de 1.0 s (30 HP) y reaparece lejos del jugador. Es invulnerable mientras arde.
/// Todos los valores son editables en el Inspector.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class CentinelaBoss : MonoBehaviour
{
    private enum State { Idle, Reposition, Attacking, Dead }

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 4.0f;
    [Tooltip("Si el jugador está más cerca que esto, el jefe retrocede")]
    [SerializeField] private float minPreferredDistance = 7.0f;
    [Tooltip("Si el jugador está más lejos que esto, el jefe avanza")]
    [SerializeField] private float maxPreferredDistance = 12.0f;
    [Tooltip("Límites horizontales de la arena (el jefe nunca sale de ellos)")]
    [SerializeField] private float arenaMinX = -30f;
    [SerializeField] private float arenaMaxX = 30f;

    [Header("Ataque 1 — Bola de fuego")]
    [SerializeField] private float fireballTelegraph = 0.45f;
    [SerializeField] private float fireballSpeed = 9.0f;
    [SerializeField] private int fireballDamage = 15;
    [Tooltip("Tiempo de espera entre ataques a distancia")]
    [SerializeField] private float rangedCooldown = 1.4f;

    [Header("Ataque 2 — Abanico (cada N ataques)")]
    [SerializeField] private int volleyEveryNAttacks = 3;
    [SerializeField] private float volleyTelegraph = 0.75f;
    [SerializeField] private int volleyProjectiles = 3;
    [SerializeField] private float volleySpreadDegrees = 18f;
    [SerializeField] private int volleyDamage = 12;

    [Header("Ataque 3 — Pilar de fuego (castigo cuerpo a cuerpo)")]
    [SerializeField] private float pillarTriggerDistance = 3.0f;
    [SerializeField] private float pillarTelegraph = 0.6f;
    [SerializeField] private float pillarDuration = 1.0f;
    [SerializeField] private int pillarDamage = 30;
    [SerializeField] private Vector2 pillarHitboxSize = new Vector2(4.5f, 14f);
    [SerializeField] private float pillarCooldown = 5.0f;
    [Tooltip("Distancia a la que reaparece respecto del jugador")]
    [SerializeField] private float teleportDistance = 11f;
    [SerializeField] private float emergeDuration = 0.4f;

    [Header("Daño por contacto")]
    [SerializeField] private int contactDamage = 15;
    [SerializeField] private float contactDamageCooldown = 0.8f;
    [SerializeField] private Vector2 contactBoxSize = new Vector2(2.4f, 4.5f);
    [SerializeField] private Vector2 contactBoxOffset = new Vector2(0f, 2.4f);

    [Header("Referencias")]
    [SerializeField] private BossProjectile projectilePrefab;
    [Tooltip("Punto de disparo con el jefe mirando a la izquierda (se espeja solo)")]
    [SerializeField] private Vector2 mouthOffset = new Vector2(-1.4f, 3.2f);
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireballSound;
    [SerializeField] private AudioClip pillarSound;

    [Header("Telegrafiado visual")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.75f, 0.35f, 1f);

    private const string AnimIdle = "Idle";
    private const string AnimBreath = "Breath";
    private const string AnimBurnStart = "BurnStart";
    private const string AnimPillar = "Pillar";
    private const string AnimEmerge = "Emerge";

    private State state = State.Idle;
    private Transform player;
    private EnemyHealth health;
    private Rigidbody2D rb;
    private float nextRangedTime;
    private float nextPillarTime;
    private float nextContactTime;
    private int rangedAttackCount;
    private bool facingRight;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        health.Died += _ =>
        {
            state = State.Dead;
            StopAllCoroutines();
            // Las bolas de fuego que quedaban en el aire desaparecen con el jefe
            foreach (BossProjectile p in FindObjectsByType<BossProjectile>()) Destroy(p.gameObject);
        };
    }

    private void Start()
    {
        // Pequeño respiro al empezar la pelea
        nextRangedTime = Time.time + 1.5f;
        PlayAnim(AnimIdle);
    }

    private void Update()
    {
        if (state == State.Dead || player == null) return;

        HandleContactDamage();

        if (state == State.Attacking) return;

        float dx = player.position.x - transform.position.x;
        float distance = Mathf.Abs(dx);
        Face(dx);

        if (distance <= pillarTriggerDistance && Time.time >= nextPillarTime)
        {
            StartCoroutine(PillarRoutine());
            return;
        }

        if (Time.time >= nextRangedTime)
        {
            bool volley = volleyEveryNAttacks > 0 && (rangedAttackCount + 1) % volleyEveryNAttacks == 0;
            StartCoroutine(volley ? VolleyRoutine() : FireballRoutine());
            return;
        }

        // Mantener distancia
        float moveDir = 0f;
        if (distance < minPreferredDistance) moveDir = -Mathf.Sign(dx);
        else if (distance > maxPreferredDistance) moveDir = Mathf.Sign(dx);

        // Si está acorralado contra un borde, no intenta atravesarlo
        float nextX = transform.position.x + moveDir * moveSpeed * Time.deltaTime;
        if (nextX < arenaMinX || nextX > arenaMaxX) moveDir = 0f;

        state = moveDir != 0f ? State.Reposition : State.Idle;
        MoveHorizontally(moveDir * moveSpeed * Time.deltaTime);
    }

    // ---------------- Ataques ----------------

    private IEnumerator FireballRoutine()
    {
        state = State.Attacking;
        PlayAnim(AnimBreath);
        yield return Telegraph(fireballTelegraph);

        ShootAtPlayer(1, 0f, fireballDamage);
        rangedAttackCount++;

        yield return new WaitForSeconds(0.3f);
        EndAttack(rangedCooldown);
    }

    private IEnumerator VolleyRoutine()
    {
        state = State.Attacking;
        // Telegrafía más larga y visible: brilla antes de abrir la boca
        yield return Telegraph(volleyTelegraph - fireballTelegraph);
        PlayAnim(AnimBreath);
        yield return Telegraph(fireballTelegraph);

        ShootAtPlayer(volleyProjectiles, volleySpreadDegrees, volleyDamage);
        rangedAttackCount++;

        yield return new WaitForSeconds(0.45f);
        EndAttack(rangedCooldown + 0.4f);
    }

    private IEnumerator PillarRoutine()
    {
        state = State.Attacking;
        nextPillarTime = Time.time + pillarCooldown;

        PlayAnim(AnimBurnStart);
        yield return Telegraph(pillarTelegraph);

        // Se transforma en pilar: invulnerable y daña una sola vez
        health.isInvulnerable = true;
        PlayAnim(AnimPillar);
        PlaySound(pillarSound);

        bool damaged = false;
        float t = 0f;
        while (t < pillarDuration)
        {
            if (!damaged)
            {
                Vector2 center = (Vector2)transform.position + new Vector2(0f, pillarHitboxSize.y * 0.5f);
                foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, pillarHitboxSize, 0f))
                {
                    PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                    if (ph != null) { ph.TakeDamage(pillarDamage); damaged = true; break; }
                }
            }
            t += Time.deltaTime;
            yield return null;
        }

        // Desaparece y reaparece lejos del jugador, en el lado con más espacio
        spriteRenderer.enabled = false;
        float px = player.position.x;
        float roomLeft = px - arenaMinX;
        float roomRight = arenaMaxX - px;
        float targetX = roomRight >= roomLeft ? px + teleportDistance : px - teleportDistance;
        targetX = Mathf.Clamp(targetX, arenaMinX, arenaMaxX);
        SetX(targetX);
        Face(player.position.x - targetX);

        yield return new WaitForSeconds(0.25f);
        spriteRenderer.enabled = true;
        PlayAnim(AnimEmerge);
        yield return new WaitForSeconds(emergeDuration);
        health.isInvulnerable = false;

        EndAttack(rangedCooldown * 0.6f);
    }

    private void EndAttack(float cooldown)
    {
        nextRangedTime = Time.time + cooldown;
        state = State.Idle;
        PlayAnim(AnimIdle);
    }

    /// <summary>Espera 'duration' segundos tiñendo el sprite para avisar al jugador.</summary>
    private IEnumerator Telegraph(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            float pulse = Mathf.PingPong(t * 8f, 1f);
            spriteRenderer.color = Color.Lerp(Color.white, telegraphColor, pulse);
            t += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = Color.white;
    }

    private void ShootAtPlayer(int count, float spreadDegrees, int damage)
    {
        if (projectilePrefab == null) return;

        Vector2 mouth = MouthPosition();
        Vector2 target = (Vector2)player.position + new Vector2(0f, 0.8f);
        Vector2 baseDir = (target - mouth).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            float offset = count > 1 ? Mathf.Lerp(-spreadDegrees, spreadDegrees, i / (float)(count - 1)) : 0f;
            float a = (baseAngle + offset) * Mathf.Deg2Rad;
            BossProjectile p = Instantiate(projectilePrefab, mouth, Quaternion.identity);
            p.Launch(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), fireballSpeed, damage);
        }

        PlaySound(fireballSound);
    }

    // ---------------- Utilidades ----------------

    private void HandleContactDamage()
    {
        if (Time.time < nextContactTime || !spriteRenderer.enabled) return;

        Vector2 center = (Vector2)transform.position + contactBoxOffset;
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, contactBoxSize, 0f))
        {
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(contactDamage);
                nextContactTime = Time.time + contactDamageCooldown;
                return;
            }
        }
    }

    private Vector2 MouthPosition()
    {
        Vector2 offset = new Vector2(facingRight ? -mouthOffset.x : mouthOffset.x, mouthOffset.y);
        return (Vector2)transform.position + offset;
    }

    private void Face(float dx)
    {
        if (Mathf.Abs(dx) < 0.05f) return;
        facingRight = dx > 0f;
        // El sprite original mira a la izquierda
        spriteRenderer.flipX = facingRight;
    }

    private void MoveHorizontally(float delta)
    {
        if (delta == 0f) return;
        SetX(Mathf.Clamp(transform.position.x + delta, arenaMinX, arenaMaxX));
    }

    private void SetX(float x)
    {
        Vector2 p = new Vector2(x, transform.position.y);
        if (rb != null) rb.position = p;
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    private void PlayAnim(string stateName)
    {
        if (animator != null) animator.Play(stateName, 0, 0f);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    /// <summary>Lo usa el constructor de la arena para ajustar los límites a las paredes.</summary>
    public void SetArenaBounds(float minX, float maxX)
    {
        arenaMinX = minX;
        arenaMaxX = maxX;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pos, minPreferredDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pos, maxPreferredDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, pillarTriggerDistance);

        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawWireCube(pos + new Vector3(0f, pillarHitboxSize.y * 0.5f, 0f), pillarHitboxSize);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(pos + (Vector3)contactBoxOffset, contactBoxSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos + (Vector3)mouthOffset, 0.25f);
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe 2 — El Centinela (jefe de rango).
/// Mantiene distancia con el jugador desplazándose por el suelo a 4.0 u/s y alterna sus ataques:
///   1. Bola de fuego: telegrafía 0.45 s y dispara 1 proyectil apuntado (15 HP).
///   2. Abanico: cada 3er ataque a distancia, telegrafía 0.75 s y dispara 3 proyectiles (12 HP c/u).
///   3. Fuego rasante: dos bolas rasantes por el suelo, hay que saltarlas (15 HP c/u).
///   4. Embestida de fuego: se convierte en una columna que cruza la arena hacia el jugador (20 HP).
///   5. Golpe de brasas: si el jugador está cerca y el pilar no está listo, un golpe cuerpo a cuerpo (20 HP).
///   6. Pilar de fuego: si el jugador se acerca a menos de 3.0 u, telegrafía 0.6 s, se convierte en
///      un pilar de fuego de 1.0 s (30 HP) y reaparece lejos del jugador. Es invulnerable mientras arde.
/// Tocar al jefe NO hace daño: solo sus ataques. Todos los valores son editables en el Inspector.
/// Todos los valores son editables en el Inspector.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class CentinelaBoss : MonoBehaviour, IStunnable
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
    [SerializeField] private Vector2 pillarHitboxSize = new Vector2(3.8f, 6.5f);
    [SerializeField] private float pillarCooldown = 5.0f;
    [Tooltip("Distancia a la que reaparece respecto del jugador")]
    [SerializeField] private float teleportDistance = 11f;
    [SerializeField] private float emergeDuration = 0.4f;

    [Header("Ataque 4 — Fuego rasante (se esquiva saltando)")]
    [SerializeField] private float lowShotTelegraph = 0.5f;
    [SerializeField] private float lowShotSpeed = 10f;
    [SerializeField] private int lowShotDamage = 15;
    [SerializeField] private int lowShotCount = 2;
    [SerializeField] private float lowShotGap = 0.55f;

    [Header("Ataque 5 — Embestida de fuego")]
    [SerializeField] private float rushTelegraph = 0.7f;
    [SerializeField] private float rushSpeed = 17f;
    [SerializeField] private int rushDamage = 20;
    [SerializeField] private float rushOvershoot = 7f;
    [SerializeField] private float rushMinDistance = 6f;
    [SerializeField] private Vector2 rushHitboxSize = new Vector2(3.4f, 5.5f);

    [Header("Ataque 6 — Golpe de brasas (cuerpo a cuerpo)")]
    [SerializeField] private float strikeTriggerDistance = 4.2f;
    [SerializeField] private float strikeTelegraph = 0.75f;
    [SerializeField] private int strikeDamage = 20;
    [SerializeField] private float strikeCooldown = 2.0f;
    [SerializeField] private Vector2 strikeBoxSize = new Vector2(4.6f, 4.5f);
    [SerializeField] private float strikeBoxForward = 2.3f;

    [Header("Aspecto de la columna de fuego")]
    [Tooltip("El sprite del pilar mide casi 15 u; se achata para que lo visible coincida con la hitbox")]
    [SerializeField] private float columnHeightFactor = 0.5f;
    [SerializeField] private Color strikeMarkerColor = new Color(1f, 0.25f, 0.1f, 0.8f);

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
    private float nextStrikeTime;
    private Vector3 baseScale;
    private static Sprite whiteSprite;
    private float groundPlayerY;
    private int lastRanged = -1;
    private int rangedAttackCount;
    private bool facingRight;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        baseScale = transform.localScale;
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
            SetColumnLook(false);
            // Las bolas de fuego que quedaban en el aire desaparecen con el jefe
            foreach (BossProjectile p in FindObjectsByType<BossProjectile>()) Destroy(p.gameObject);
        };
    }

    private void Start()
    {
        // Pequeño respiro al empezar la pelea
        nextRangedTime = Time.time + 1.5f;
        if (player != null) groundPlayerY = player.position.y;
        PlayAnim(AnimIdle);
    }

    private void Update()
    {
        if (state == State.Dead || player == null) return;

        if (state == State.Attacking) return;

        float dx = player.position.x - transform.position.x;
        float distance = Mathf.Abs(dx);
        Face(dx);

        if (distance <= pillarTriggerDistance && Time.time >= nextPillarTime)
        {
            StartCoroutine(PillarRoutine());
            return;
        }

        // Golpe cuerpo a cuerpo cuando el pilar aún no está listo
        if (distance <= strikeTriggerDistance && Time.time >= nextStrikeTime)
        {
            StartCoroutine(StrikeRoutine());
            return;
        }

        if (Time.time >= nextRangedTime)
        {
            StartCoroutine(ChooseRangedAttack(distance));
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

    /// <summary>Elige el siguiente ataque a distancia: abanico cada N, y si no uno al azar distinto del anterior.</summary>
    private IEnumerator ChooseRangedAttack(float distance)
    {
        bool volley = volleyEveryNAttacks > 0 && (rangedAttackCount + 1) % volleyEveryNAttacks == 0;
        if (volley)
        {
            lastRanged = 1;
            yield return VolleyRoutine();
            yield break;
        }

        // 0 = bola de fuego, 2 = fuego rasante, 3 = embestida (solo si hay espacio)
        int pick;
        int guard = 0;
        do
        {
            pick = Random.Range(0, 3);
            if (pick == 1) pick = 3;
            guard++;
        } while ((pick == lastRanged || (pick == 3 && distance < rushMinDistance)) && guard < 12);
        lastRanged = pick;

        if (pick == 2) yield return LowShotRoutine();
        else if (pick == 3) yield return RushRoutine();
        else yield return FireballRoutine();
    }

    private IEnumerator LowShotRoutine()
    {
        state = State.Attacking;
        PlayAnim(AnimBreath);
        yield return Telegraph(lowShotTelegraph);

        for (int i = 0; i < lowShotCount; i++)
        {
            ShootLow();
            if (i < lowShotCount - 1) yield return new WaitForSeconds(lowShotGap);
        }
        rangedAttackCount++;

        yield return new WaitForSeconds(0.3f);
        EndAttack(rangedCooldown + 0.2f);
    }

    private void ShootLow()
    {
        if (projectilePrefab == null) return;
        Vector2 origin = new Vector2(MouthPosition().x, groundPlayerY - 0.3f);
        float dir = player.position.x >= transform.position.x ? 1f : -1f;
        BossProjectile p = Instantiate(projectilePrefab, origin, Quaternion.identity);
        p.Launch(new Vector2(dir, 0f), lowShotSpeed, lowShotDamage);
        PlaySound(fireballSound);
    }

    private IEnumerator RushRoutine()
    {
        state = State.Attacking;
        PlayAnim(AnimBurnStart);
        yield return Telegraph(rushTelegraph);

        float dir = player.position.x >= transform.position.x ? 1f : -1f;
        Face(dir);
        float startX = transform.position.x;
        float goalX = Mathf.Clamp(player.position.x + dir * rushOvershoot, arenaMinX, arenaMaxX);
        float travel = Mathf.Abs(goalX - startX);

        PlayAnim(AnimPillar);
        SetColumnLook(true);
        PlaySound(pillarSound);

        bool damaged = false;
        float moved = 0f;
        while (moved < travel)
        {
            float step = Mathf.Min(rushSpeed * Time.deltaTime, travel - moved);
            SetX(transform.position.x + dir * step);
            moved += step;

            if (!damaged)
            {
                Vector2 center = (Vector2)transform.position + new Vector2(0f, rushHitboxSize.y * 0.5f);
                foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, rushHitboxSize, 0f))
                {
                    PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                    if (ph != null) { ph.TakeDamage(rushDamage); damaged = true; break; }
                }
            }
            yield return null;
        }

        SetColumnLook(false);
        Face(player.position.x - transform.position.x);
        PlayAnim(AnimEmerge);
        yield return new WaitForSeconds(emergeDuration + 0.2f);
        rangedAttackCount++;
        EndAttack(rangedCooldown + 0.4f);
    }

    private IEnumerator StrikeRoutine()
    {
        state = State.Attacking;
        nextStrikeTime = Time.time + strikeCooldown;
        Face(player.position.x - transform.position.x);

        float dir = facingRight ? 1f : -1f;
        Vector2 center = (Vector2)transform.position + new Vector2(dir * strikeBoxForward, strikeBoxSize.y * 0.5f);

        // Aviso: zona marcada en el suelo y el jefe abre la boca cargando el golpe
        GameObject marker = SpawnStrikeMarker(center.x);
        PlayAnim(AnimBreath);
        yield return Telegraph(strikeTelegraph);
        if (marker != null) Destroy(marker);

        // Golpe: pequeño salto hacia adelante, destello y daño
        PlaySound(pillarSound);
        StartCoroutine(StrikeFlash(center, dir));
        float lunge = 1.2f;
        float t = 0f;
        while (t < 0.1f)
        {
            float step = lunge * Time.deltaTime / 0.1f;
            SetX(Mathf.Clamp(transform.position.x + dir * step, arenaMinX, arenaMaxX));
            t += Time.deltaTime;
            yield return null;
        }
        center = (Vector2)transform.position + new Vector2(dir * strikeBoxForward, strikeBoxSize.y * 0.5f);
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, strikeBoxSize, 0f))
        {
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null) { ph.TakeDamage(strikeDamage); break; }
        }
        GameFeel.Shake(0.2f, 0.2f);

        yield return new WaitForSeconds(0.45f);
        EndAttack(rangedCooldown * 0.5f);
    }

    private GameObject SpawnStrikeMarker(float centerX)
    {
        if (whiteSprite == null)
        {
            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int k = 0; k < px.Length; k++) px[k] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        var go = new GameObject("CentinelaWarning");
        go.transform.position = new Vector3(centerX, transform.position.y + 0.35f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(strikeBoxSize.x, 0.7f);
        sr.color = strikeMarkerColor;
        sr.sortingOrder = spriteRenderer.sortingOrder - 1;
        go.AddComponent<WarningPulse>();
        return go;
    }

    /// <summary>Destello de fuego que se desvanece donde cae el golpe.</summary>
    private IEnumerator StrikeFlash(Vector2 center, float dir)
    {
        var go = new GameObject("CentinelaStrikeFlash");
        go.transform.position = center;
        go.transform.localScale = new Vector3(1.4f, 2.6f, 1f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, dir > 0f ? -90f : 90f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralFx.GetSlashSprite();
        sr.sortingOrder = spriteRenderer.sortingOrder + 1;

        float t = 0f;
        const float life = 0.25f;
        while (t < life)
        {
            float k = t / life;
            sr.color = new Color(1f, Mathf.Lerp(0.75f, 0.3f, k), 0.2f, 1f - k);
            go.transform.localScale = new Vector3(1.4f + k * 0.8f, 2.6f + k * 0.6f, 1f);
            t += Time.deltaTime;
            yield return null;
        }
        Destroy(go);
    }

    /// <summary>Achata el sprite del pilar mientras dura la columna, para que lo que se ve sea lo que daña.</summary>
    private void SetColumnLook(bool on)
    {
        transform.localScale = on ? new Vector3(baseScale.x, baseScale.y * columnHeightFactor, baseScale.z) : baseScale;
    }

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
        SetColumnLook(true);
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
        SetColumnLook(false);
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

    // ---------------- Aturdimiento (parry del jugador) ----------------

    /// <summary>Interrumpe el ataque en curso y deja al jefe inmóvil y vulnerable 'duration' segundos.</summary>
    public void Stun(float duration)
    {
        // Mientras es un pilar de fuego (invulnerable) no se puede aturdir
        if (state == State.Dead || health.IsDead || health.isInvulnerable) return;

        StopAllCoroutines();
        SetColumnLook(false);
        state = State.Attacking; // bloquea la IA mientras dura el aturdimiento
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        spriteRenderer.enabled = true;
        GameFeel.FloatingText(transform.position + new Vector3(0f, 5.5f, 0f), "¡ATURDIDO!", new Color(0.6f, 0.85f, 1f), 1.3f);

        float t = 0f;
        while (t < duration)
        {
            spriteRenderer.color = Mathf.Repeat(t * 8f, 1f) < 0.5f ? new Color(0.6f, 0.8f, 1f) : health.BaseColor;
            t += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = health.BaseColor;
        EndAttack(0.6f);
    }

    // ---------------- Utilidades ----------------

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
        Gizmos.DrawWireCube(pos + new Vector3(0f, strikeBoxSize.y * 0.5f, 0f), strikeBoxSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos + (Vector3)mouthOffset, 0.25f);
    }
}

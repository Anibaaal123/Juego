using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe 1 — El Demonio (volador). Reemplaza a EnemyAttack.
/// Entre ataques persigue al jugador por el aire. Cada 'timeBetweenAttacks' elige un ataque al azar
/// (sin repetir el anterior), según la distancia:
///   1. Aliento de fuego: solo si está cerca. Telegrafía y quema un área frente a él.
///   2. Picada: se eleva, marca el suelo 0.6 s y se lanza en diagonal a 14 u/s (25 HP). Luego queda
///      0.7 s en el suelo, vulnerable.
///   3. Lluvia de fuego: sube, marca 5 columnas en el suelo 0.8 s y deja caer bolas de fuego (15 HP c/u).
/// Fase 2 (al 50 % de vida): ruge 1.0 s (invulnerable), +30 % velocidad, -30 % espera entre ataques,
/// 7 columnas de lluvia y 2 picadas seguidas.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class DemonBoss : MonoBehaviour, IStunnable
{
    private enum Attack { None, Breath, Dive, Rain }

    [Header("Vuelo")]
    [SerializeField] private float flightSpeed = 2.8f;
    [SerializeField] private float stoppingDistance = 8f;
    [SerializeField] private float preferredHeightOffset = 1.2f;
    [SerializeField] private float floatFrequency = 2.5f;
    [SerializeField] private float floatAmplitude = 0.4f;
    [SerializeField] private float arenaMinX = -30f;
    [SerializeField] private float arenaMaxX = 30f;

    [Header("Ritmo")]
    [SerializeField] private float timeBetweenAttacks = 1.8f;
    [SerializeField] private float firstAttackDelay = 2.0f;

    [Header("Ataque 1 — Aliento de fuego")]
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 fireAreaSize = new Vector2(4.5f, 2.5f);
    [SerializeField] private LayerMask playerLayer = 1;
    [SerializeField] private float fireStartDelay = 1.4f;
    [SerializeField] private float flameDuration = 0.5f;
    [SerializeField] private float recoveryDelay = 0.3f;

    [Header("Ataque 2 — Picada")]
    [SerializeField] private float diveRiseHeight = 7f;
    [SerializeField] private float diveSideOffset = 6f;
    [SerializeField] private float diveTelegraph = 0.6f;
    [SerializeField] private float diveSpeed = 14f;
    [SerializeField] private int diveDamage = 25;
    [SerializeField] private float diveRecovery = 0.7f;
    [SerializeField] private Vector2 diveHitboxSize = new Vector2(3f, 3f);

    [Header("Ataque 3 — Lluvia de fuego")]
    [SerializeField] private float rainHeightAboveGround = 13f;
    [SerializeField] private int rainColumns = 5;
    [SerializeField] private float rainColumnSpacing = 3.5f;
    [SerializeField] private float rainWarningTime = 0.8f;
    [SerializeField] private float rainFallSpeed = 12f;
    [SerializeField] private int rainDamage = 15;
    [SerializeField] private float rainRecovery = 0.5f;

    [Header("Fase 2 (al 50 % de vida)")]
    [SerializeField] private float phase2HealthFraction = 0.5f;
    [SerializeField] private float phase2SpeedMultiplier = 1.3f;
    [SerializeField] private float phase2CooldownMultiplier = 0.7f;
    [SerializeField] private int phase2RainColumns = 7;
    [SerializeField] private float phase2RainSpacing = 3.0f;
    [SerializeField] private int phase2DiveRepeats = 2;
    [SerializeField] private float roarDuration = 1.0f;
    [Tooltip("Opcional: sonido del rugido. Si está vacío se usa uno sintetizado")]
    [SerializeField] private AudioClip roarSound;
    [SerializeField] private float roarScalePulse = 0.2f;
    [SerializeField] private float roarShockwaveRadius = 9f;
    [SerializeField] private float roarPushSpeed = 14f;
    [SerializeField] private Color enragedTint = new Color(1f, 0.7f, 0.7f, 1f);

    [Header("Daño por contacto")]
    [SerializeField] private int contactDamage = 5;
    [SerializeField] private float contactDamageCooldown = 1.5f;

    [Header("Referencias")]
    [SerializeField] private BossProjectile projectilePrefab;
    [SerializeField] private Sprite warningSprite;
    [SerializeField] private LayerMask groundLayer = 1 << 6;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;

    [Header("Telegrafiado")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.6f, 0.25f, 1f);
    [SerializeField] private Color warningMarkerColor = new Color(1f, 0.2f, 0.1f, 0.85f);

    private Transform player;
    private EnemyHealth health;
    private bool busy;
    private bool phase2;
    private float nextAttackTime;
    private float nextContactDamageTime;
    private Attack lastAttack = Attack.None;
    private float groundY = -2.5f;

    private float SpeedMultiplier => phase2 ? phase2SpeedMultiplier : 1f;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        health.Died += _ =>
        {
            StopAllCoroutines();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            foreach (BossProjectile p in FindObjectsByType<BossProjectile>()) Destroy(p.gameObject);
        };
        health.Damaged += _ => CheckPhase2();
    }

    private void Start()
    {
        nextAttackTime = Time.time + firstAttackDelay;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 100f, groundLayer);
        if (hit.collider != null) groundY = hit.point.y;
    }

    private void Update()
    {
        if (player == null || busy || health.IsDead) return;

        LookAtPlayer();
        float distance = Vector2.Distance(transform.position, player.position);

        if (Time.time >= nextAttackTime)
        {
            Attack next = ChooseAttack(distance);
            lastAttack = next;
            StartCoroutine(RunAttack(next));
            return;
        }

        if (distance > stoppingDistance) FlyTowardsPlayer();
        else HoverInPlace();
    }

    // ---------------- Elección de ataques ----------------

    private Attack ChooseAttack(float distance)
    {
        bool close = distance <= stoppingDistance;
        for (int i = 0; i < 10; i++)
        {
            float r = Random.value;
            Attack pick = close
                ? (r < 0.5f ? Attack.Breath : r < 0.75f ? Attack.Dive : Attack.Rain)
                : (r < 0.5f ? Attack.Dive : Attack.Rain);
            if (pick != lastAttack) return pick;
        }
        return lastAttack == Attack.Dive ? Attack.Rain : Attack.Dive;
    }

    private IEnumerator RunAttack(Attack attack)
    {
        busy = true;
        switch (attack)
        {
            case Attack.Breath:
                yield return BreathRoutine();
                break;
            case Attack.Dive:
                int repeats = phase2 ? phase2DiveRepeats : 1;
                for (int i = 0; i < repeats; i++) yield return DiveRoutine();
                break;
            case Attack.Rain:
                yield return RainRoutine();
                break;
        }
        float cooldown = timeBetweenAttacks * (phase2 ? phase2CooldownMultiplier : 1f);
        nextAttackTime = Time.time + cooldown;
        busy = false;
    }

    // ---------------- Ataque 1: aliento ----------------

    private IEnumerator BreathRoutine()
    {
        SetVelocity(Vector2.zero);
        if (animator != null) animator.SetTrigger("Attack");
        PlaySound(attackSound);

        yield return new WaitForSeconds(fireStartDelay / SpeedMultiplier);

        float timer = 0f;
        bool hasDamaged = false;
        while (timer < flameDuration)
        {
            HoverInPlace();
            Vector2 checkPos = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
            Collider2D hit = Physics2D.OverlapBox(checkPos, fireAreaSize, 0f, playerLayer);
            if (hit != null && !hasDamaged)
            {
                PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
                if (ph != null) { ph.TakeDamage(attackDamage); hasDamaged = true; }
            }
            timer += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(recoveryDelay);
    }

    // ---------------- Ataque 2: picada ----------------

    private IEnumerator DiveRoutine()
    {
        // 1. Se eleva hacia un costado del jugador
        float side = transform.position.x < player.position.x ? -1f : 1f;
        Vector2 risePoint = new Vector2(ClampX(player.position.x + side * diveSideOffset), player.position.y + diveRiseHeight);
        yield return FlyTo(risePoint, flightSpeed * 3f * SpeedMultiplier, 1.2f);
        SetVelocity(Vector2.zero);
        LookAtPlayer();

        // 2. Marca el punto de impacto en el suelo y avisa
        Vector2 target = new Vector2(ClampX(player.position.x), groundY);
        GameObject marker = SpawnGroundMarker(target.x, 3f);
        yield return Telegraph(diveTelegraph / SpeedMultiplier);
        target = new Vector2(ClampX(player.position.x), groundY); // se ajusta a la posición final
        if (marker != null) marker.transform.position = new Vector3(target.x, groundY + 0.35f, 0f);

        // 3. Se lanza en diagonal hasta tocar el suelo
        float stopY = groundY + BodyHalfHeight() * 0.6f;
        Vector2 dir = (new Vector2(target.x, stopY) - (Vector2)transform.position).normalized;
        float speed = diveSpeed * SpeedMultiplier;
        bool hitPlayer = false;
        float t = 0f;
        PlaySound(attackSound);
        while (transform.position.y > stopY && t < 1.5f)
        {
            SetVelocity(dir * speed);
            if (!hitPlayer) hitPlayer = TryDamageInBox(transform.position, diveHitboxSize, diveDamage);
            t += Time.deltaTime;
            yield return null;
        }
        SetVelocity(Vector2.zero);
        if (marker != null) Destroy(marker);
        GameFeel.Shake(0.35f, 0.25f);

        // 4. Queda en el suelo, vulnerable
        yield return new WaitForSeconds(diveRecovery);
    }

    // ---------------- Ataque 3: lluvia de fuego ----------------

    private IEnumerator RainRoutine()
    {
        // 1. Sube sobre el jugador
        Vector2 top = new Vector2(ClampX(player.position.x), groundY + rainHeightAboveGround);
        yield return FlyTo(top, flightSpeed * 3f * SpeedMultiplier, 1.5f);
        SetVelocity(Vector2.zero);

        // 2. Marca las columnas en el suelo
        int columns = phase2 ? phase2RainColumns : rainColumns;
        float spacing = phase2 ? phase2RainSpacing : rainColumnSpacing;
        float[] xs = new float[columns];
        GameObject[] markers = new GameObject[columns];
        for (int i = 0; i < columns; i++)
        {
            xs[i] = ClampX(player.position.x + (i - (columns - 1) * 0.5f) * spacing + Random.Range(-0.5f, 0.5f));
            markers[i] = SpawnGroundMarker(xs[i], 1.4f);
        }
        if (animator != null) animator.SetTrigger("Attack");
        yield return Telegraph(rainWarningTime / SpeedMultiplier);

        // 3. Caen las bolas de fuego, una tras otra
        PlaySound(attackSound);
        for (int i = 0; i < columns; i++)
        {
            if (projectilePrefab != null)
            {
                Vector2 spawn = new Vector2(xs[i], groundY + rainHeightAboveGround + 1f);
                BossProjectile p = Instantiate(projectilePrefab, spawn, Quaternion.identity);
                p.Launch(Vector2.down, rainFallSpeed, rainDamage);
            }
            if (markers[i] != null) Destroy(markers[i], 1.2f);
            yield return new WaitForSeconds(0.08f);
        }
        yield return new WaitForSeconds(rainRecovery);
    }

    // ---------------- Aturdimiento (parry del jugador) ----------------

    /// <summary>Interrumpe el ataque en curso y deja al jefe inmóvil y vulnerable 'duration' segundos.</summary>
    public void Stun(float duration)
    {
        // Durante el rugido (invulnerable) no se puede aturdir
        if (health.IsDead || health.isInvulnerable) return;

        StopAllCoroutines();
        DestroyMarkers();
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        busy = true;
        SetVelocity(Vector2.zero);
        GameFeel.FloatingText(transform.position + new Vector3(0f, 4.5f, 0f), "¡ATURDIDO!", new Color(0.6f, 0.85f, 1f), 1.3f);

        float t = 0f;
        while (t < duration)
        {
            SetVelocity(Vector2.zero);
            spriteRenderer.color = Mathf.Repeat(t * 8f, 1f) < 0.5f ? new Color(0.6f, 0.8f, 1f) : health.BaseColor;
            t += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = health.BaseColor;
        nextAttackTime = Time.time + 0.6f;
        busy = false;
    }

    // ---------------- Fase 2 ----------------

    private void CheckPhase2()
    {
        if (phase2 || health.IsDead) return;
        if (health.CurrentHealth > health.MaxHealth * phase2HealthFraction) return;

        phase2 = true;
        StopAllCoroutines();
        DestroyMarkers();
        StartCoroutine(RoarRoutine());
    }

    private IEnumerator RoarRoutine()
    {
        busy = true;
        health.isInvulnerable = true;
        SetVelocity(Vector2.zero);
        if (animator != null) animator.SetTrigger("Attack"); // boca abierta

        // Sonido: el asignado en el Inspector o, si no hay, un rugido sintetizado
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.PlayOneShot(roarSound != null ? roarSound : ProceduralFx.GetRoarClip(), 1f);

        GameFeel.Shake(0.6f, roarDuration);
        ShowArenaMessage("¡El demonio se enfurece!");
        PushPlayerAway();

        Vector3 baseScale = transform.localScale;
        float t = 0f;
        while (t < roarDuration)
        {
            float k = t / roarDuration;
            // Se infla y vuelve a su tamaño
            transform.localScale = baseScale * (1f + roarScalePulse * Mathf.Sin(k * Mathf.PI));
            bool on = Mathf.Repeat(t * 10f, 1f) < 0.5f;
            spriteRenderer.color = on ? new Color(1f, 0.25f, 0.25f) : Color.white;
            SetVelocity(Vector2.zero);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = baseScale;

        // Enfurecido: queda teñido de rojo el resto de la pelea
        health.BaseColor = enragedTint;
        spriteRenderer.color = enragedTint;
        if (animator != null) animator.speed = phase2SpeedMultiplier;

        health.isInvulnerable = false;
        nextAttackTime = Time.time + 0.5f;
        busy = false;

        yield return new WaitForSeconds(1.5f);
        ShowArenaMessage("");
    }

    /// <summary>Onda expansiva del rugido: empuja al jugador hacia afuera (sin daño).</summary>
    private void PushPlayerAway()
    {
        if (player == null) return;
        Vector2 d = player.position - transform.position;
        if (d.magnitude > roarShockwaveRadius) return;

        float side = d.x >= 0f ? 1f : -1f;
        Vector2 push = new Vector2(side * roarPushSpeed, 6f);
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.ApplyKnockback(push, 0.3f);
        else if (player.TryGetComponent(out Rigidbody2D prb)) prb.linearVelocity = push;
    }

    private void ShowArenaMessage(string text)
    {
        GameObject go = GameObject.Find("ArenaMessage");
        UnityEngine.UI.Text label = go != null ? go.GetComponent<UnityEngine.UI.Text>() : null;
        if (label != null) label.text = text;
    }

    // ---------------- Movimiento ----------------

    private void FlyTowardsPlayer()
    {
        Vector2 target = new Vector2(player.position.x, player.position.y + preferredHeightOffset);
        target.y += Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        SetVelocity(dir * flightSpeed * SpeedMultiplier);
    }

    private void HoverInPlace()
    {
        SetVelocity(new Vector2(0f, Mathf.Sin(Time.time * floatFrequency) * floatAmplitude));
    }

    private IEnumerator FlyTo(Vector2 target, float speed, float maxTime)
    {
        float t = 0f;
        while (Vector2.Distance(transform.position, target) > 0.3f && t < maxTime)
        {
            SetVelocity((target - (Vector2)transform.position).normalized * speed);
            LookAtPlayer();
            t += Time.deltaTime;
            yield return null;
        }
    }

    private void SetVelocity(Vector2 v)
    {
        if (rb != null) rb.linearVelocity = v;
        else transform.position += (Vector3)(v * Time.deltaTime);
    }

    private void LookAtPlayer()
    {
        if (spriteRenderer == null || player == null) return;
        bool shouldFlip = player.position.x > transform.position.x;
        spriteRenderer.flipX = shouldFlip;
        if (attackPoint != null)
        {
            float xOffset = Mathf.Abs(attackPoint.localPosition.x);
            attackPoint.localPosition = new Vector3(shouldFlip ? xOffset : -xOffset, attackPoint.localPosition.y, 0f);
        }
    }

    // ---------------- Utilidades ----------------

    private IEnumerator Telegraph(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            HoverInPlace();
            spriteRenderer.color = Color.Lerp(health.BaseColor, telegraphColor, Mathf.PingPong(t * 8f, 1f));
            t += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = health.BaseColor;
    }

    private bool TryDamageInBox(Vector2 center, Vector2 size, int damage)
    {
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f, playerLayer))
        {
            PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
            if (ph != null) { ph.TakeDamage(damage); return true; }
        }
        return false;
    }

    private GameObject SpawnGroundMarker(float x, float width)
    {
        if (warningSprite == null) return null;
        var go = new GameObject("DemonWarning");
        go.transform.position = new Vector3(x, groundY + 0.35f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = warningSprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(width, 0.7f);
        sr.color = warningMarkerColor;
        sr.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 10;
        go.AddComponent<WarningPulse>();
        return go;
    }

    private void DestroyMarkers()
    {
        foreach (WarningPulse w in FindObjectsByType<WarningPulse>()) Destroy(w.gameObject);
    }

    private float BodyHalfHeight() => spriteRenderer != null ? spriteRenderer.bounds.extents.y : 2f;

    private float ClampX(float x) => Mathf.Clamp(x, arenaMinX, arenaMaxX);

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

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Time.time < nextContactDamageTime || health.IsDead) return;
        PlayerHealth ph = collision.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(contactDamage);
            nextContactDamageTime = Time.time + contactDamageCooldown;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, diveHitboxSize);
        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(attackPoint.position, fireAreaSize);
        }
    }
}

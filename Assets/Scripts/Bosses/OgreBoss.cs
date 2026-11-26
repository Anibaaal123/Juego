using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Jefe 3 — El Guardián (ogro, 450 HP). Camina por el suelo hacia el jugador y castiga la cercanía.
/// Elige sus ataques al azar según la distancia, sin repetir el mismo dos veces seguidas:
///   1. Garrotazo (fase 1): levanta el garrote 0.8 s y golpea el área de enfrente (35 HP).
///   2. Salto aplastante: salta sobre el jugador (30 HP al aterrizar) y suelta una onda de choque
///      por el suelo hacia cada lado (25 HP) que hay que saltar.
///   3. Golpe devastador: aviso largo de 1.5 s y un golpe que quita el 50 % de la vida máxima del jugador.
///      Cubrirse lo reduce al 15 % y un parry perfecto lo anula y aturde al jefe.
///   4. Embestida (fase 2): corre en línea recta (30 HP).
/// Fase 2 (al 50 % de vida): suelta el garrote, ruge (1.0 s invulnerable), es 35 % más rápido, ataca un 30 % más
/// seguido y el salto lanza dos ondas seguidas.
/// Todos los valores son editables en el Inspector.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class OgreBoss : MonoBehaviour, IStunnable
{
    private enum Attack { None, Club, Leap, Devastating, Charge }

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 2.6f;
    [Tooltip("Distancia (centro a centro) a la que se detiene frente al jugador")]
    [SerializeField] private float stopDistance = 3.4f;
    [SerializeField] private float arenaMinX = -5.5f;
    [SerializeField] private float arenaMaxX = 5.5f;
    [Tooltip("El dibujo original del ogro mira hacia la izquierda")]
    [SerializeField] private bool spriteFacesLeft = true;

    [Header("Ritmo")]
    [SerializeField] private float timeBetweenAttacks = 1.3f;
    [SerializeField] private float firstAttackDelay = 1.5f;

    [Header("Garrotazo (fase 1)")]
    [SerializeField] private float clubReach = 5f;
    [SerializeField] private float clubTelegraph = 0.8f;
    [SerializeField] private int clubDamage = 35;
    [SerializeField] private Vector2 clubBoxSize = new Vector2(4.4f, 4.5f);
    [SerializeField] private float clubBoxForward = 2.8f;

    [Header("Salto aplastante")]
    [SerializeField] private float leapTelegraph = 0.6f;
    [SerializeField] private float leapDuration = 0.85f;
    [SerializeField] private float leapHeight = 6f;
    [SerializeField] private int leapDamage = 30;
    [SerializeField] private Vector2 landingBoxSize = new Vector2(5f, 3.5f);
    [SerializeField] private float waveSpeed = 10f;
    [SerializeField] private int waveDamage = 25;
    [SerializeField] private float secondWaveDelay = 0.4f;

    [Header("Golpe devastador (quita el 50 % de la vida máxima)")]
    [SerializeField] private float devastatingTelegraph = 1.5f;
    [Range(0f, 1f)] [SerializeField] private float devastatingPercent = 0.5f;
    [SerializeField] private Vector2 devastatingBoxSize = new Vector2(7.5f, 6f);
    [SerializeField] private float devastatingBoxForward = 3.6f;
    [SerializeField] private float devastatingFirstDelay = 8f;
    [SerializeField] private float devastatingCooldown = 14f;
    [SerializeField] private float devastatingRecovery = 1.0f;

    [Header("Embestida (fase 2)")]
    [SerializeField] private float chargeTelegraph = 0.6f;
    [SerializeField] private float chargeSpeed = 11f;
    [SerializeField] private float chargeDuration = 0.7f;
    [SerializeField] private int chargeDamage = 30;
    [SerializeField] private Vector2 chargeBoxSize = new Vector2(4.5f, 5f);

    [Header("Fase 2 (al 50 % de vida)")]
    [SerializeField] private float phase2HealthFraction = 0.5f;
    [SerializeField] private float phase2SpeedMultiplier = 1.35f;
    [SerializeField] private float phase2CooldownMultiplier = 0.7f;
    [SerializeField] private float roarDuration = 1.0f;
    [SerializeField] private float roarShockwaveRadius = 10f;
    [SerializeField] private float roarPushSpeed = 14f;
    [SerializeField] private Color enragedTint = new Color(1f, 0.75f, 0.7f, 1f);


    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private Sprite warningSprite;

    [Header("Telegrafiado")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.75f, 0.45f, 1f);
    [SerializeField] private Color devastatingColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color warningMarkerColor = new Color(1f, 0.2f, 0.1f, 0.8f);

    private Transform player;
    private PlayerHealth playerHealth;
    private EnemyHealth health;
    private BoxCollider2D body;

    private bool busy;
    private bool phase2;
    private bool facingRight;
    private float groundY;
    private float nextAttackTime;
    private float nextDevastatingTime;
    private Attack lastAttack = Attack.None;
    private string currentAnim = "";
    private readonly List<GameObject> markers = new List<GameObject>();

    private float SpeedMultiplier => phase2 ? phase2SpeedMultiplier : 1f;
    private float AnimSpeed => phase2 ? 1.25f : 1f;
    private float Forward => facingRight ? 1f : -1f;
    private int SortingOrder => spriteRenderer != null ? spriteRenderer.sortingOrder : 3;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        body = GetComponent<BoxCollider2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        health.Died += _ =>
        {
            StopAllCoroutines();
            ClearMarkers();
            if (animator != null) animator.speed = 1f;
            ShowArenaMessage("");
            foreach (GroundWave wave in FindObjectsByType<GroundWave>()) Destroy(wave.gameObject);
        };
        health.Damaged += _ => CheckPhase2();
    }

    private void Start()
    {
        groundY = transform.position.y;
        nextAttackTime = Time.time + firstAttackDelay;
        nextDevastatingTime = Time.time + devastatingFirstDelay;
        FaceTarget();
        PlayAnim("Idle");
    }

    private void Update()
    {
        if (player == null || health.IsDead) return;

        if (busy) return;

        FaceTarget();
        float dx = player.position.x - transform.position.x;
        float distance = Mathf.Abs(dx);

        if (Time.time >= nextAttackTime)
        {
            StartCoroutine(RunAttack(ChooseAttack(distance)));
            return;
        }

        if (distance > stopDistance)
        {
            PlayAnim(Anim("Walk"));
            MoveX(Mathf.Sign(dx) * walkSpeed * SpeedMultiplier * Time.deltaTime);
        }
        else
        {
            PlayAnim(Anim("Idle"));
        }
    }

    // ---------------- Elección y ejecución de ataques ----------------

    private Attack ChooseAttack(float distance)
    {
        bool devastatingReady = Time.time >= nextDevastatingTime &&
                                distance <= devastatingBoxForward + devastatingBoxSize.x * 0.5f;
        if (devastatingReady && Random.value < 0.55f) return Attack.Devastating;

        Attack melee = phase2 ? Attack.Charge : Attack.Club;
        for (int i = 0; i < 8; i++)
        {
            Attack pick;
            if (distance <= clubReach) pick = Random.value < 0.6f ? melee : Attack.Leap;
            else pick = phase2 ? (Random.value < 0.55f ? Attack.Charge : Attack.Leap) : Attack.Leap;

            if (pick != lastAttack) return pick;
        }
        return lastAttack == Attack.Leap ? melee : Attack.Leap;
    }

    private IEnumerator RunAttack(Attack attack)
    {
        busy = true;
        lastAttack = attack;

        switch (attack)
        {
            case Attack.Club: yield return ClubRoutine(); break;
            case Attack.Leap: yield return LeapRoutine(); break;
            case Attack.Devastating: yield return DevastatingRoutine(); break;
            case Attack.Charge: yield return ChargeRoutine(); break;
        }

        if (animator != null) animator.speed = AnimSpeed;
        ClearMarkers();
        SetPos(transform.position.x, groundY);
        currentAnim = "";
        nextAttackTime = Time.time + timeBetweenAttacks * (phase2 ? phase2CooldownMultiplier : 1f);
        busy = false;
    }

    // 1. Garrotazo
    private IEnumerator ClubRoutine()
    {
        FaceTarget();
        Vector2 boxCenter = new Vector2(transform.position.x + Forward * clubBoxForward, groundY + clubBoxSize.y * 0.5f);
        SpawnMarker(boxCenter.x, clubBoxSize.x);
        PlaySound(attackSound);

        FreezePose("Attack", 0.3f);             // garrote en alto
        yield return Telegraph(clubTelegraph, telegraphColor);

        animator.speed = AnimSpeed;              // baja el garrote
        yield return new WaitForSeconds(0.1f / AnimSpeed);

        DamageBox(boxCenter, clubBoxSize, clubDamage);
        Impact(0.45f);
        yield return new WaitForSeconds(0.35f / AnimSpeed);
    }

    // 2. Salto aplastante
    private IEnumerator LeapRoutine()
    {
        float startX = transform.position.x;
        float targetX = Mathf.Clamp(player.position.x, arenaMinX, arenaMaxX);
        SpawnMarker(targetX, landingBoxSize.x);

        FreezePose(Anim("Idle"), 0.3f);
        yield return Telegraph(leapTelegraph, telegraphColor);

        FaceTowards(targetX > startX);
        PlaySound(attackSound);
        if (!phase2) FreezePose("Attack", 0.3f); // en el aire, con el garrote en alto

        float t = 0f;
        while (t < leapDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / leapDuration);
            SetPos(Mathf.Lerp(startX, targetX, k), groundY + 4f * leapHeight * k * (1f - k));
            yield return null;
        }
        SetPos(targetX, groundY);

        Impact(0.7f);
        DamageBox(new Vector2(targetX, groundY + landingBoxSize.y * 0.5f), landingBoxSize, leapDamage);
        SpawnWaves(targetX);

        if (phase2)
        {
            yield return new WaitForSeconds(secondWaveDelay);
            Impact(0.35f);
            SpawnWaves(transform.position.x);
        }

        animator.speed = AnimSpeed;
        yield return new WaitForSeconds(0.55f);
    }

    // 3. Golpe devastador: quita un % de la vida máxima del jugador
    private IEnumerator DevastatingRoutine()
    {
        FaceTarget();
        nextDevastatingTime = Time.time + devastatingCooldown;
        Vector2 boxCenter = new Vector2(transform.position.x + Forward * devastatingBoxForward,
                                        groundY + devastatingBoxSize.y * 0.5f);
        SpawnMarker(boxCenter.x, devastatingBoxSize.x);
        ShowArenaMessage("¡GOLPE DEVASTADOR!");
        PlaySound(attackSound);

        FreezePose(phase2 ? "IdleUnarmed" : "Attack", 0.3f);
        GameFeel.Shake(0.12f, devastatingTelegraph);   // retumbar mientras carga
        yield return Telegraph(devastatingTelegraph, devastatingColor);

        animator.speed = AnimSpeed;
        yield return new WaitForSeconds(0.1f / AnimSpeed);

        int damage = Mathf.CeilToInt((playerHealth != null ? playerHealth.MaxHealth : 100) * devastatingPercent);
        DamageBox(boxCenter, devastatingBoxSize, damage);
        Impact(0.9f);
        ShowArenaMessage("");

        // Se queda un instante agotado: es la ventana para castigarlo
        yield return new WaitForSeconds(devastatingRecovery);
    }

    // 4. Embestida (fase 2)
    private IEnumerator ChargeRoutine()
    {
        FaceTarget();
        float dir = Forward;
        float length = Mathf.Abs((dir > 0f ? arenaMaxX : arenaMinX) - transform.position.x);
        SpawnMarker(transform.position.x + dir * length * 0.5f, Mathf.Max(2f, length));
        PlaySound(attackSound);

        FreezePose("WalkUnarmed", 0.3f);
        yield return Telegraph(chargeTelegraph, telegraphColor);

        PlayAnim("WalkUnarmed");
        animator.speed = 2f;

        bool hit = false;
        float t = 0f;
        while (t < chargeDuration)
        {
            float nextX = transform.position.x + dir * chargeSpeed * Time.deltaTime;
            if (nextX < arenaMinX || nextX > arenaMaxX)
            {
                Impact(0.5f);   // choca contra el borde
                break;
            }
            SetPos(nextX, groundY);
            if (!hit) hit = DamageBox(new Vector2(nextX, groundY + chargeBoxSize.y * 0.5f), chargeBoxSize, chargeDamage);
            t += Time.deltaTime;
            yield return null;
        }

        animator.speed = AnimSpeed;
        yield return new WaitForSeconds(0.5f);
    }

    // ---------------- Fase 2 ----------------

    private void CheckPhase2()
    {
        if (phase2 || health.IsDead || health.CurrentHealth <= 0) return;
        if (health.CurrentHealth > health.MaxHealth * phase2HealthFraction) return;

        phase2 = true;
        StopAllCoroutines();
        ClearMarkers();
        StartCoroutine(RoarRoutine());
    }

    private IEnumerator RoarRoutine()
    {
        busy = true;
        health.isInvulnerable = true;
        SetPos(transform.position.x, groundY);
        animator.speed = 1f;
        currentAnim = "";
        PlayAnim(Anim("Idle"));    // ya sin garrote

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.PlayOneShot(ProceduralFx.GetRoarClip(), 1f);
        GameFeel.Shake(0.7f, roarDuration);
        ShowArenaMessage("¡El Guardián suelta su garrote!");
        PushPlayerAway();

        Vector3 baseScale = transform.localScale;
        float t = 0f;
        while (t < roarDuration)
        {
            float k = t / roarDuration;
            transform.localScale = baseScale * (1f + 0.12f * Mathf.Sin(k * Mathf.PI));
            spriteRenderer.color = Mathf.Repeat(t * 10f, 1f) < 0.5f ? new Color(1f, 0.3f, 0.3f) : Color.white;
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = baseScale;

        health.BaseColor = enragedTint;
        spriteRenderer.color = enragedTint;
        animator.speed = AnimSpeed;

        health.isInvulnerable = false;
        nextAttackTime = Time.time + 0.6f;
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

        Vector2 push = new Vector2((d.x >= 0f ? 1f : -1f) * roarPushSpeed, 6f);
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.ApplyKnockback(push, 0.3f);
    }

    // ---------------- Aturdimiento (parry del jugador) ----------------

    /// <summary>Interrumpe el ataque en curso y deja al jefe inmóvil y vulnerable 'duration' segundos.</summary>
    public void Stun(float duration)
    {
        // Durante el rugido (invulnerable) no se puede aturdir
        if (health.IsDead || health.isInvulnerable) return;

        StopAllCoroutines();
        ClearMarkers();
        ShowArenaMessage("");
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        busy = true;
        animator.speed = 1f;
        SetPos(transform.position.x, groundY);   // si estaba en el aire, cae al suelo
        currentAnim = "";
        PlayAnim(Anim("Idle"));
        GameFeel.FloatingText(transform.position + new Vector3(0f, 7.5f, 0f), "¡ATURDIDO!", new Color(0.6f, 0.85f, 1f), 1.3f);

        float t = 0f;
        while (t < duration)
        {
            spriteRenderer.color = Mathf.Repeat(t * 8f, 1f) < 0.5f ? new Color(0.6f, 0.8f, 1f) : health.BaseColor;
            t += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = health.BaseColor;
        nextAttackTime = Time.time + 0.6f;
        busy = false;
    }

    // ---------------- Utilidades ----------------

    private IEnumerator Telegraph(float duration, Color color)
    {
        float t = 0f;
        while (t < duration)
        {
            spriteRenderer.color = Color.Lerp(health.BaseColor, color, Mathf.PingPong(t * 8f, 1f));
            t += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = health.BaseColor;
    }

    private bool DamageBox(Vector2 center, Vector2 size, int damage)
    {
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Una onda hacia cada lado. Nacen 1.5 u a los costados del punto de aterrizaje: quien queda justo debajo del jefe
    /// recibe solo el golpe del aterrizaje, y las ondas castigan a quien se aleja corriendo por el suelo.
    /// </summary>
    private void SpawnWaves(float x)
    {
        const float sideOffset = 2.5f;
        GroundWave.Spawn(new Vector2(x - sideOffset, groundY), -1f, waveSpeed, waveDamage, arenaMinX - 3f, arenaMaxX + 3f, SortingOrder + 1);
        GroundWave.Spawn(new Vector2(x + sideOffset, groundY), 1f, waveSpeed, waveDamage, arenaMinX - 3f, arenaMaxX + 3f, SortingOrder + 1);
    }

    /// <summary>Golpe contra el suelo: sacudida de cámara y sonido grave.</summary>
    private void Impact(float strength)
    {
        GameFeel.Shake(0.5f * strength + 0.15f, 0.3f);
        PlaySound(ProceduralFx.GetImpactClip());
    }

    private void SpawnMarker(float centerX, float width)
    {
        if (warningSprite == null) return;

        var go = new GameObject("OgreWarning");
        go.transform.position = new Vector3(centerX, groundY + 0.35f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = warningSprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(width, 0.7f);
        sr.color = warningMarkerColor;
        sr.sortingOrder = SortingOrder + 1;
        go.AddComponent<WarningPulse>();
        markers.Add(go);
    }

    private void ClearMarkers()
    {
        foreach (GameObject go in markers)
            if (go != null) Destroy(go);
        markers.Clear();
    }

    private string Anim(string baseName) => phase2 ? baseName + "Unarmed" : baseName;

    private void PlayAnim(string clip)
    {
        if (animator == null || currentAnim == clip) return;
        currentAnim = clip;
        animator.Play("Ogre_" + clip, 0, 0f);   // los estados del controlador se llaman Ogre_<clip>
    }

    /// <summary>Congela una animación en un cuadro (para mantener la pose del aviso).</summary>
    private void FreezePose(string clip, float normalizedTime)
    {
        animator.speed = 0f;
        animator.Play("Ogre_" + clip, 0, normalizedTime);
        animator.Update(0f);
        currentAnim = clip;
    }

    private void FaceTarget()
    {
        if (player != null) FaceTowards(player.position.x > transform.position.x);
    }

    private void FaceTowards(bool right)
    {
        facingRight = right;
        spriteRenderer.flipX = spriteFacesLeft ? right : !right;
    }

    private void MoveX(float delta)
    {
        if (delta == 0f) return;
        SetPos(Mathf.Clamp(transform.position.x + delta, arenaMinX, arenaMaxX), transform.position.y);
    }

    private void SetPos(float x, float y)
    {
        Vector2 p = new Vector2(x, y);
        if (rb != null) rb.position = p;
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private void ShowArenaMessage(string text)
    {
        GameObject go = GameObject.Find("ArenaMessage");
        UnityEngine.UI.Text label = go != null ? go.GetComponent<UnityEngine.UI.Text>() : null;
        if (label != null) label.text = text;
    }

    /// <summary>Lo usa el constructor de la arena para ajustar los límites a las paredes.</summary>
    public void SetArenaBounds(float minX, float maxX)
    {
        arenaMinX = minX;
        arenaMaxX = maxX;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 p = transform.position;
        float fwd = Application.isPlaying ? Forward : 1f;
        Gizmos.DrawWireCube(p + new Vector3(fwd * clubBoxForward, clubBoxSize.y * 0.5f, 0f), clubBoxSize);
        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawWireCube(p + new Vector3(fwd * devastatingBoxForward, devastatingBoxSize.y * 0.5f, 0f), devastatingBoxSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3((arenaMinX + arenaMaxX) * 0.5f, p.y + 1f, 0f), new Vector3(arenaMaxX - arenaMinX, 2f, 0f));
    }
}

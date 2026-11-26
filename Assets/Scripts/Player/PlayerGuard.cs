using UnityEngine;

/// <summary>
/// Defensa del jugador con el clic derecho.
///
///   • Cubrirse (siempre disponible): mientras se mantiene apretado, los golpes enemigos hacen solo el 30 % de su
///     daño (mínimo 1) y el jugador se mueve al 50 % de velocidad. No se puede cubrir en pleno ataque.
///     El fuego del piso y otros peligros del escenario no se pueden bloquear.
///
///   • Parry (siempre disponible): al apretar se abre una ventana de 0.07 s en la que el golpe se anula por completo.
///       - Éxito: sin daño, el jefe cercano queda aturdido 1.5 s y no hay espera para volver a usarlo.
///       - Las bolas de fuego enemigas que lo tocan en la ventana rebotan hacia el jefe (ver BossProjectile).
///       - Fallo: 0.7 s de espera antes de poder abrir otra ventana (seguir cubierto no tiene espera).
/// </summary>
public class PlayerGuard : MonoBehaviour
{
    [Header("Cubrirse (siempre disponible)")]
    [Tooltip("Fracción del daño que se recibe al cubrirse (0.3 = recibe el 30 %)")]
    [SerializeField] private float guardDamageMultiplier = 0.3f;
    [Tooltip("Velocidad de movimiento mientras se cubre (fracción de la normal)")]
    [SerializeField] private float guardMoveMultiplier = 0.5f;
    [SerializeField] private Color guardColor = new Color(0.5f, 0.8f, 1f, 0.6f);
    [Tooltip("Depuración: mantiene la guardia activa sin apretar el clic derecho")]
    [SerializeField] private bool debugHoldGuard;

    [Header("Parry (siempre disponible)")]
    [Tooltip("Tiempo que dura la ventana del parry (unos 4 cuadros a 60 FPS): hay que apretar justo antes del golpe")]
    [SerializeField] private float window = 0.07f;
    [Tooltip("Espera tras un parry fallido")]
    [SerializeField] private float missCooldown = 0.7f;
    [SerializeField] private float stunDuration = 1.5f;
    [SerializeField] private float stunRange = 16f;
    [SerializeField] private Color parryColor = new Color(0.5f, 0.95f, 1f, 1f);

    private SpriteRenderer spriteRenderer;
    private PlayerHealth health;
    private PlayerAttack attack;
    private SpriteRenderer shield;

    private bool guardHeld;
    private float windowEnd = -1f;
    private float nextReadyTime;
    private bool succeededThisWindow;
    private bool tinted;

    /// <summary>True mientras el jugador se está cubriendo (clic derecho apretado y sin atacar).</summary>
    public bool IsGuarding => enabled && guardHeld && (attack == null || !attack.IsAttacking);

    /// <summary>Multiplicador de velocidad que PlayerMovement aplica mientras se cubre.</summary>
    public float MoveSpeedMultiplier => IsGuarding ? guardMoveMultiplier : 1f;

    public bool IsActive => Time.time < windowEnd;
    public bool IsReady => Time.time >= nextReadyTime;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<PlayerHealth>();
        attack = GetComponent<PlayerAttack>();
    }

    private void Update()
    {
        bool alive = health == null || !health.IsDead;
        guardHeld = alive && (Input.GetMouseButton(1) || debugHoldGuard);

        if (alive && Input.GetMouseButtonDown(1) && RunState.Has(SkillType.Parry) && IsReady) Activate();

        UpdateTint();
        UpdateShield();
    }

    private void OnDisable()
    {
        guardHeld = false;
        windowEnd = -1f;
        UpdateTint();
        if (shield != null) shield.enabled = false;
    }

    // ---------------- Cubrirse ----------------

    /// <summary>PlayerHealth lo llama si el golpe llegó estando cubierto. Devuelve el daño que sí se recibe.</summary>
    public int Block(int damage)
    {
        int reduced = Mathf.Max(1, Mathf.CeilToInt(damage * guardDamageMultiplier - 0.001f));

        GameFeel.HitStop(0.04f);
        GameFeel.Shake(0.12f, 0.1f);
        GameFeel.FloatingText(transform.position + new Vector3(0f, 2.6f, 0f), "¡CUBIERTO!", new Color(0.6f, 0.85f, 1f), 1.0f);
        return reduced;
    }

    // ---------------- Parry ----------------

    /// <summary>Abre la ventana del parry.</summary>
    public void Activate()
    {
        windowEnd = Time.time + window;
        nextReadyTime = windowEnd + missCooldown;
        succeededThisWindow = false;
    }

    /// <summary>PlayerHealth lo llama antes de aplicar daño. Devuelve true si el golpe quedó anulado por un parry.</summary>
    public bool TryAbsorb(bool ranged = false)
    {
        if (!RunState.Has(SkillType.Parry) || !IsActive) return false;
        Succeed(stunBoss: !ranged);
        return true;
    }

    /// <summary>Los proyectiles lo llaman al tocar al jugador. Devuelve true si deben rebotar (sin aturdir al jefe).</summary>
    public bool TryReflect()
    {
        if (!RunState.Has(SkillType.Parry) || !IsActive) return false;
        Succeed(stunBoss: false);
        return true;
    }

    private void Succeed(bool stunBoss)
    {
        if (succeededThisWindow) return;
        succeededThisWindow = true;

        CombatStats.RegisterParry();
        nextReadyTime = Time.time; // un parry exitoso no tiene castigo

        GameFeel.HitStop(0.08f);
        GameFeel.Shake(0.35f, 0.2f);
        GameFeel.FloatingText(transform.position + new Vector3(0f, 2.6f, 0f), "¡PARRY!", parryColor, 1.4f);

        int order = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 11;
        Vector3 center = transform.position + new Vector3(0f, 0.2f, 0f);
        ProceduralFx.Slash(center, 90f, 1.8f, new Color(1f, 1f, 1f, 0.95f), order);
        ProceduralFx.Slash(center, -90f, 1.8f, new Color(1f, 1f, 1f, 0.95f), order);

        // Un golpe cuerpo a cuerpo aturde al jefe cercano; devolver un proyectil no
        if (!stunBoss) return;
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>())
        {
            if (enemy.IsDead) continue;
            if (Vector2.Distance(enemy.transform.position, transform.position) > stunRange) continue;
            IStunnable stunnable = enemy.GetComponent<IStunnable>();
            if (stunnable != null) stunnable.Stun(stunDuration);
        }
    }

    // ---------------- Aspecto ----------------

    private void UpdateTint()
    {
        if (spriteRenderer == null) return;

        if (IsActive)
        {
            spriteRenderer.color = parryColor;
            tinted = true;
        }
        else if (tinted)
        {
            spriteRenderer.color = Color.white;
            tinted = false;
        }
    }

    /// <summary>Escudo semitransparente delante del jugador: celeste al cubrirse, blanco durante la ventana del parry.</summary>
    private void UpdateShield()
    {
        bool parryOn = IsActive;
        bool guardOn = IsGuarding;

        if (shield == null)
        {
            if (!parryOn && !guardOn) return;
            var go = new GameObject("Escudo");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * 0.8f;
            shield = go.AddComponent<SpriteRenderer>();
            shield.sprite = ProceduralFx.GetSlashSprite();
            shield.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 2) + 1;
        }

        shield.enabled = parryOn || guardOn;
        if (!shield.enabled) return;

        bool left = spriteRenderer != null && spriteRenderer.flipX;
        shield.transform.localPosition = new Vector3(left ? -0.2f : 0.2f, 0.3f, 0f);
        shield.transform.localRotation = Quaternion.Euler(0f, 0f, left ? -90f : 90f);
        shield.color = parryOn ? new Color(1f, 1f, 1f, 0.95f) : guardColor;
    }
}

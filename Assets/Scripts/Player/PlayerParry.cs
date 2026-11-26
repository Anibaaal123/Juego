using UnityEngine;

/// <summary>
/// Habilidad "Parry" (clic derecho). Abre una ventana de 0.10 s en la que cualquier golpe recibido se anula.
///   • Éxito: el daño se anula, el jefe cercano queda aturdido 0.8 s y no hay espera para volver a usarlo.
///   • Las bolas de fuego enemigas que lo tocan en la ventana rebotan hacia el jefe (ver BossProjectile).
///   • Fallo: 0.7 s de espera antes de poder intentarlo otra vez, para que no se pueda spamear.
/// Solo funciona si el jugador compró la habilidad (RunState).
/// </summary>
public class PlayerParry : MonoBehaviour
{
    [Header("Ventana")]
    [Tooltip("Tiempo que dura la ventana del parry (6 cuadros a 60 FPS): hay que apretar justo antes del golpe")]
    [SerializeField] private float window = 0.10f;
    [Tooltip("Espera tras un parry fallido")]
    [SerializeField] private float missCooldown = 0.7f;

    [Header("Efecto sobre el jefe")]
    [SerializeField] private float stunDuration = 0.8f;
    [SerializeField] private float stunRange = 16f;

    [Header("Aspecto")]
    [SerializeField] private Color parryColor = new Color(0.5f, 0.95f, 1f, 1f);

    private SpriteRenderer spriteRenderer;
    private float windowEnd = -1f;
    private float nextReadyTime;
    private bool succeededThisWindow;
    private bool tinted;

    public bool IsActive => Time.time < windowEnd;
    public bool IsReady => Time.time >= nextReadyTime;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!RunState.Has(SkillType.Parry)) return;

        if (Input.GetMouseButtonDown(1) && IsReady) Activate();
        UpdateTint();
    }

    private void OnDisable()
    {
        windowEnd = -1f;
        UpdateTint();
    }

    /// <summary>Abre la ventana del parry.</summary>
    public void Activate()
    {
        windowEnd = Time.time + window;
        nextReadyTime = windowEnd + missCooldown;
        succeededThisWindow = false;

        int order = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 10;
        Vector3 center = transform.position + new Vector3(0f, 0.2f, 0f);
        ProceduralFx.Slash(center, 90f, 0.8f, new Color(parryColor.r, parryColor.g, parryColor.b, 0.8f), order);
        ProceduralFx.Slash(center, -90f, 0.8f, new Color(parryColor.r, parryColor.g, parryColor.b, 0.8f), order);
    }

    /// <summary>PlayerHealth lo llama antes de aplicar daño. Devuelve true si el golpe quedó anulado.</summary>
    public bool TryAbsorb()
    {
        if (!RunState.Has(SkillType.Parry) || !IsActive) return false;
        Succeed();
        return true;
    }

    /// <summary>Los proyectiles lo llaman al tocar al jugador. Devuelve true si deben rebotar.</summary>
    public bool TryReflect()
    {
        if (!RunState.Has(SkillType.Parry) || !IsActive) return false;
        Succeed();
        return true;
    }

    private void Succeed()
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

        // Aturde al jefe más cercano
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>())
        {
            if (enemy.IsDead) continue;
            if (Vector2.Distance(enemy.transform.position, transform.position) > stunRange) continue;
            IStunnable stunnable = enemy.GetComponent<IStunnable>();
            if (stunnable != null) stunnable.Stun(stunDuration);
        }
    }

    private void UpdateTint()
    {
        if (spriteRenderer == null) return;

        bool active = IsActive;
        if (active)
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
}

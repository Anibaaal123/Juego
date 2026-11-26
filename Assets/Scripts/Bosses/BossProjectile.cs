using UnityEngine;

/// <summary>
/// Proyectil enemigo en línea recta. Daña al jugador al tocarlo y se destruye al chocar con el suelo,
/// con una pared o al acabarse su tiempo de vida. No depende de callbacks de física: revisa
/// solapamientos cada FixedUpdate, así funciona aunque no tenga Rigidbody2D.
///
/// Con la habilidad Parry del jugador, si lo toca mientras la ventana está abierta, el proyectil
/// rebota hacia el jefe más cercano (más rápido) y lo daña a él en vez de al jugador.
/// </summary>
public class BossProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 9f;
    [SerializeField] private int damage = 15;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float hitRadius = 0.45f;
    [Tooltip("Capas que destruyen el proyectil al tocarlas (Default = paredes, Suelo = piso y plataformas)")]
    [SerializeField] private LayerMask solidLayers = (1 << 0) | (1 << 6);

    [Header("Rebote por parry")]
    [SerializeField] private int reflectedDamage = 25;
    [SerializeField] private float reflectedSpeedMultiplier = 1.4f;
    [SerializeField] private Color reflectedColor = new Color(0.5f, 0.95f, 1f, 1f);

    private Vector2 direction = Vector2.left;
    private float deathTime;
    private bool hasHit;
    private bool reflected;

    public bool IsReflected => reflected;

    /// <summary>Configura el proyectil justo después de instanciarlo.</summary>
    public void Launch(Vector2 dir, float newSpeed, int newDamage)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.left;
        speed = newSpeed;
        damage = newDamage;
        PointSpriteAlongDirection();
    }

    private void Start()
    {
        deathTime = Time.time + lifetime;
    }

    private void FixedUpdate()
    {
        if (hasHit) return;

        transform.position += (Vector3)(direction * speed * Time.fixedDeltaTime);

        if (Time.time >= deathTime)
        {
            Destroy(gameObject);
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
        foreach (Collider2D hit in hits)
        {
            if (reflected)
            {
                // Rebotado: daña al jefe, ignora al jugador
                EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy != null && enemy.TakeDamage(reflectedDamage))
                {
                    GameFeel.HitStop(0.06f);
                    GameFeel.Shake(0.2f, 0.15f);
                    Explode();
                    return;
                }
            }
            else
            {
                PlayerHealth player = hit.GetComponent<PlayerHealth>();
                if (player != null)
                {
                    PlayerParry parry = player.GetComponent<PlayerParry>();
                    if (parry != null && parry.TryReflect())
                    {
                        Reflect();
                        return;
                    }

                    player.TakeDamage(damage);
                    Explode();
                    return;
                }
            }

            // El propio jugador está en la capa Default: no cuenta como pared (si no, la bola rebotada se destruiría al instante)
            if (!hit.isTrigger && ((1 << hit.gameObject.layer) & solidLayers) != 0 && hit.GetComponentInParent<PlayerHealth>() == null)
            {
                Explode();
                return;
            }
        }
    }

    /// <summary>Devuelve el proyectil hacia el jefe vivo más cercano.</summary>
    private void Reflect()
    {
        reflected = true;
        deathTime = Time.time + lifetime;
        speed *= reflectedSpeedMultiplier;

        EnemyHealth target = null;
        float best = float.MaxValue;
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>())
        {
            if (enemy.IsDead) continue;
            float d = Vector2.Distance(enemy.transform.position, transform.position);
            if (d < best) { best = d; target = enemy; }
        }

        if (target != null)
        {
            Vector2 aim = (Vector2)target.transform.position + new Vector2(0f, 1.5f);
            direction = (aim - (Vector2)transform.position).normalized;
        }
        else
        {
            direction = -direction;
        }

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = reflectedColor;
        PointSpriteAlongDirection();
    }

    private void PointSpriteAlongDirection()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // El sprite de la bola de fuego apunta a la izquierda por defecto
        transform.rotation = Quaternion.Euler(0f, 0f, angle + 180f);
    }

    private void Explode()
    {
        hasHit = true;
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}

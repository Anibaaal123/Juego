using UnityEngine;

/// <summary>
/// Proyectil enemigo en línea recta. Daña al jugador al tocarlo y se destruye al chocar con el suelo,
/// con una pared o al acabarse su tiempo de vida. No depende de callbacks de física: revisa
/// solapamientos cada FixedUpdate, así funciona aunque no tenga Rigidbody2D.
/// </summary>
public class BossProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 9f;
    [SerializeField] private int damage = 15;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float hitRadius = 0.45f;
    [Tooltip("Capas que destruyen el proyectil al tocarlas (Default = paredes, Suelo = piso y plataformas)")]
    [SerializeField] private LayerMask solidLayers = (1 << 0) | (1 << 6);

    private Vector2 direction = Vector2.left;
    private float deathTime;
    private bool hasHit;

    /// <summary>Configura el proyectil justo después de instanciarlo.</summary>
    public void Launch(Vector2 dir, float newSpeed, int newDamage)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.left;
        speed = newSpeed;
        damage = newDamage;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // El sprite de la bola de fuego apunta a la izquierda por defecto
        transform.rotation = Quaternion.Euler(0f, 0f, angle + 180f);
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
            PlayerHealth player = hit.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Explode();
                return;
            }

            if (!hit.isTrigger && ((1 << hit.gameObject.layer) & solidLayers) != 0)
            {
                Explode();
                return;
            }
        }
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

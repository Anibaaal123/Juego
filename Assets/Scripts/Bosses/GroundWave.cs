using UnityEngine;

/// <summary>
/// Onda de choque que recorre el suelo (la deja caer El Guardián al aterrizar). Es baja: hay que saltarla.
/// Daña al jugador una sola vez y desaparece al llegar al borde de la arena.
/// </summary>
public class GroundWave : MonoBehaviour
{
    private static readonly Vector2 HitboxSize = new Vector2(1.8f, 1.5f);

    private float direction;
    private float speed;
    private int damage;
    private float minX;
    private float maxX;

    /// <summary>origin = punto del suelo donde nace; direction = +1 derecha o -1 izquierda.</summary>
    public static GroundWave Spawn(Vector2 origin, float direction, float speed, int damage, float minX, float maxX, int sortingOrder)
    {
        var go = new GameObject("GroundWave");
        go.transform.position = new Vector3(origin.x, origin.y + 0.75f, 0f);
        // La medialuna se dibuja con la panza hacia abajo: girada 90° apunta hacia donde avanza la onda
        go.transform.rotation = Quaternion.Euler(0f, 0f, direction > 0f ? 90f : -90f);
        go.transform.localScale = new Vector3(0.38f, 1.2f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralFx.GetSlashSprite();
        sr.color = new Color(0.95f, 0.62f, 0.3f, 0.95f);
        sr.sortingOrder = sortingOrder;

        var wave = go.AddComponent<GroundWave>();
        wave.direction = direction > 0f ? 1f : -1f;
        wave.speed = speed;
        wave.damage = damage;
        wave.minX = minX;
        wave.maxX = maxX;
        return wave;
    }

    private void FixedUpdate()
    {
        transform.position += new Vector3(direction * speed * Time.fixedDeltaTime, 0f, 0f);

        if (transform.position.x < minX || transform.position.x > maxX)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 center = (Vector2)transform.position + new Vector2(direction * 0.5f, 0f);
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, HitboxSize, 0f))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
            if (player == null) continue;

            player.TakeDamage(damage, false, ranged: true);
            Destroy(gameObject);
            return;
        }
    }
}

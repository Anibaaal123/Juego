using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Onda de corte lanzada por el golpe cargado (habilidad "Onda de espada"). Viaja en línea recta a 16 u/s
/// y daña una vez a cada enemigo que atraviesa.
/// </summary>
public class SwordWave : MonoBehaviour
{
    private const float Speed = 16f;
    private const float Lifetime = 1.3f;
    private static readonly Vector2 HitboxSize = new Vector2(1.6f, 4f);

    private Vector2 direction;
    private int damage;
    private float dieTime;
    private SpriteRenderer spriteRenderer;
    private readonly HashSet<EnemyHealth> alreadyHit = new HashSet<EnemyHealth>();

    public static SwordWave Spawn(Vector2 position, bool facingLeft, int damage, int sortingOrder)
    {
        var go = new GameObject("SwordWave");
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, 0f, facingLeft ? -90f : 90f);
        go.transform.localScale = Vector3.one * 1.2f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralFx.GetSlashSprite();
        sr.color = new Color(1f, 0.85f, 0.35f, 0.95f);
        sr.sortingOrder = sortingOrder;

        var wave = go.AddComponent<SwordWave>();
        wave.direction = facingLeft ? Vector2.left : Vector2.right;
        wave.damage = damage;
        wave.dieTime = Time.time + Lifetime;
        wave.spriteRenderer = sr;
        return wave;
    }

    private void FixedUpdate()
    {
        transform.position += (Vector3)(direction * Speed * Time.fixedDeltaTime);

        if (Time.time >= dieTime)
        {
            Destroy(gameObject);
            return;
        }

        // Se va desvaneciendo hacia el final de su recorrido
        float remaining = Mathf.Clamp01((dieTime - Time.time) / 0.4f);
        Color c = spriteRenderer.color;
        spriteRenderer.color = new Color(c.r, c.g, c.b, 0.95f * remaining);

        Vector2 center = (Vector2)transform.position + direction * 0.9f;
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, HitboxSize, 0f))
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !alreadyHit.Add(enemy)) continue;

            if (enemy.TakeDamage(damage))
            {
                GameFeel.HitStop(0.04f);
                GameFeel.Shake(0.12f, 0.1f);
            }
        }
    }
}

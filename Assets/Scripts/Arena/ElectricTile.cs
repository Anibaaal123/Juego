using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Baldosa eléctrica de la Arena 2. La controla ElectricFloorController.
/// Estados: Inactiva → Advertencia (parpadea) → Activa (daña y ralentiza a quien esté encima).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ElectricTile : MonoBehaviour
{
    public enum TileState { Inactive, Warning, Active }

    [Header("Daño (GDD: 15 HP cada 0.5 s, ralentiza 50 % por 1.0 s)")]
    [SerializeField] private int damagePerTick = 15;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private float slowDuration = 1.0f;
    [Tooltip("El GDD dice 'cualquier entidad': si está activo, también daña a los jefes")]
    [SerializeField] private bool damageEnemies = true;

    [Header("Zona de detección (sobre la baldosa)")]
    [SerializeField] private float detectionHeight = 1.0f;

    [Header("Colores")]
    [SerializeField] private Color inactiveColor = new Color(0.22f, 0.26f, 0.34f, 0.9f);
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color activeColor = new Color(0.35f, 0.95f, 1f, 1f);
    [SerializeField] private float warningBlinkHz = 8f;

    public TileState State => state;

    private SpriteRenderer spriteRenderer;
    private TileState state = TileState.Inactive;
    private float stateTime;
    // Próximo instante en que cada entidad puede volver a recibir daño de esta baldosa
    private readonly Dictionary<Object, float> nextTickTime = new Dictionary<Object, float>();

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetState(TileState.Inactive);
    }

    public void SetState(TileState newState)
    {
        state = newState;
        stateTime = 0f;
        if (newState != TileState.Active) nextTickTime.Clear();
        UpdateVisual();
    }

    private void Update()
    {
        stateTime += Time.deltaTime;
        UpdateVisual();

        if (state == TileState.Active) ApplyEffects();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        switch (state)
        {
            case TileState.Inactive:
                spriteRenderer.color = inactiveColor;
                break;
            case TileState.Warning:
                bool on = Mathf.Repeat(stateTime * warningBlinkHz, 1f) < 0.5f;
                spriteRenderer.color = on ? warningColor : inactiveColor;
                break;
            case TileState.Active:
                // Parpadeo rápido para que se lea como electricidad
                float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(stateTime * 25f, 0f);
                spriteRenderer.color = activeColor * flicker + new Color(0f, 0f, 0f, 1f - flicker);
                break;
        }
    }

    private void ApplyEffects()
    {
        Bounds b = spriteRenderer.bounds;
        Vector2 center = new Vector2(b.center.x, b.max.y + detectionHeight * 0.5f - 0.1f);
        Vector2 size = new Vector2(b.size.x, detectionHeight);

        var handled = new HashSet<Object>();
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
            if (player != null && handled.Add(player))
            {
                PlayerMovement movement = player.GetComponent<PlayerMovement>();
                if (movement != null) movement.ApplySlow(slowMultiplier, slowDuration);
                if (TryTick(player)) player.TakeDamage(damagePerTick);
                continue;
            }

            if (!damageEnemies) continue;
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy != null && handled.Add(enemy) && TryTick(enemy))
            {
                enemy.TakeDamage(damagePerTick);
            }
        }
    }

    // Daño inmediato al pisar y luego cada tickInterval
    private bool TryTick(Object target)
    {
        if (nextTickTime.TryGetValue(target, out float next) && Time.time < next) return false;
        nextTickTime[target] = Time.time + tickInterval;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        Bounds b = sr.bounds;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(b.center.x, b.max.y + detectionHeight * 0.5f - 0.1f, 0f),
                            new Vector3(b.size.x, detectionHeight, 0f));
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Géiser de fuego de la Arena 2. Lo controla FireFloorController.
/// Estados:
///   Inactivo: grieta apagada.
///   Advertencia: la grieta parpadea y brotan llamas bajas (avisa al jugador).
///   Activo: una columna de fuego sube desde la grieta y daña 15 HP cada 0.5 s a quien esté dentro.
/// El jefe es inmune al fuego del piso (es su elemento); se puede reactivar con 'damageEnemies'.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FireVent : MonoBehaviour
{
    public enum VentState { Inactive, Warning, Active }

    [Header("Daño (GDD: 15 HP cada 0.5 s)")]
    [SerializeField] private int damagePerTick = 15;
    [SerializeField] private float tickInterval = 0.5f;
    [Tooltip("Los jefes son inmunes al fuego del piso")]
    [SerializeField] private bool damageEnemies = false;

    [Header("Columna de fuego")]
    [Tooltip("Cuadros de la columna de llamas (se alternan en bucle)")]
    [SerializeField] private Sprite[] flameSprites;
    [SerializeField] private float flameWidth = 6.4f;
    [SerializeField] private float flameHeight = 8f;
    [SerializeField] private float flameFps = 10f;
    [SerializeField] private int flameSortingOrder = 4;
    [Tooltip("Duración de la advertencia: las llamas bajas crecen durante este tiempo")]
    [SerializeField] private float warningGrowTime = 1.0f;

    [Header("Colores de la grieta")]
    [SerializeField] private Color inactiveColor = new Color(0.16f, 0.05f, 0.04f, 0.45f);
    [SerializeField] private Color warningColor = new Color(1f, 0.55f, 0.1f, 1f);
    [SerializeField] private Color activeColor = new Color(1f, 0.4f, 0.05f, 1f);
    [SerializeField] private float warningBlinkHz = 8f;

    public VentState State => state;

    private SpriteRenderer plate;
    private SpriteRenderer flame;
    private VentState state = VentState.Inactive;
    private float stateTime;
    private Vector2 flameBaseScale = Vector2.one;
    private bool warnedMissingSprites;

    // Próximo instante en que cada entidad puede volver a recibir daño de este géiser
    private readonly Dictionary<Object, float> nextTickTime = new Dictionary<Object, float>();

    private void Awake()
    {
        plate = GetComponent<SpriteRenderer>();
        CreateFlame();
        SetState(VentState.Inactive);
    }

    private void CreateFlame()
    {
        if (flameSprites == null || flameSprites.Length == 0 || flameSprites[0] == null)
        {
            if (!warnedMissingSprites) Debug.LogWarning("[FireVent] No tiene flameSprites: no se verá la columna de fuego.", this);
            warnedMissingSprites = true;
            return;
        }

        var go = new GameObject("Llamas");
        go.transform.SetParent(transform, false);
        // Nace en el borde superior de la grieta, con el pivote de las llamas abajo al centro
        go.transform.position = new Vector3(plate.bounds.center.x, plate.bounds.max.y, transform.position.z);

        flame = go.AddComponent<SpriteRenderer>();
        flame.sprite = flameSprites[0];
        flame.sortingOrder = flameSortingOrder;

        // El dibujo ocupa ~86 % del ancho del sprite: se escala para que la columna mida flameWidth × flameHeight
        Vector2 size = flameSprites[0].bounds.size;
        flameBaseScale = new Vector2(flameWidth / (size.x * 0.86f), flameHeight / size.y);
        go.transform.localScale = new Vector3(flameBaseScale.x, flameBaseScale.y, 1f);
        flame.enabled = false;
    }

    public void SetState(VentState newState)
    {
        state = newState;
        stateTime = 0f;
        if (newState != VentState.Active) nextTickTime.Clear();
        UpdateVisual();
    }

    private void Update()
    {
        stateTime += Time.deltaTime;
        UpdateVisual();

        if (state == VentState.Active) ApplyDamage();
    }

    private void UpdateVisual()
    {
        if (plate == null) return;

        switch (state)
        {
            case VentState.Inactive:
                plate.color = inactiveColor;
                if (flame != null) flame.enabled = false;
                break;

            case VentState.Warning:
            {
                bool on = Mathf.Repeat(stateTime * warningBlinkHz, 1f) < 0.5f;
                plate.color = on ? warningColor : inactiveColor;

                // Llamas bajas que van creciendo (de 12 % a 38 % de la altura final)
                float k = Mathf.Clamp01(stateTime / Mathf.Max(0.1f, warningGrowTime));
                SetFlame(Mathf.Lerp(0.12f, 0.38f, k), new Color(1f, 0.85f, 0.6f, 0.6f));
                break;
            }

            case VentState.Active:
            {
                float flicker = 0.8f + 0.2f * Mathf.PerlinNoise(stateTime * 20f, 0f);
                plate.color = activeColor * flicker + new Color(0f, 0f, 0f, 1f - flicker);
                SetFlame(1f, Color.white);
                break;
            }
        }
    }

    private void SetFlame(float heightFactor, Color tint)
    {
        if (flame == null) return;

        flame.enabled = true;
        flame.color = tint;
        flame.transform.localScale = new Vector3(flameBaseScale.x, flameBaseScale.y * heightFactor, 1f);

        int frame = Mathf.FloorToInt(Time.time * flameFps) % flameSprites.Length;
        flame.sprite = flameSprites[frame];
    }

    private void ApplyDamage()
    {
        Bounds b = plate.bounds;
        Vector2 center = new Vector2(b.center.x, b.max.y + flameHeight * 0.5f);
        Vector2 size = new Vector2(flameWidth * 0.85f, flameHeight);

        var handled = new HashSet<Object>();
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                if (handled.Add(player) && TryTick(player)) player.TakeDamage(damagePerTick, true); // el fuego del piso no se bloquea
                continue;
            }

            if (!damageEnemies) continue;
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy != null && handled.Add(enemy) && TryTick(enemy)) enemy.TakeDamage(damagePerTick);
        }
    }

    // Daño inmediato al entrar y luego cada tickInterval
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
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(new Vector3(b.center.x, b.max.y + flameHeight * 0.5f, 0f),
                            new Vector3(flameWidth * 0.85f, flameHeight, 0f));
    }
}

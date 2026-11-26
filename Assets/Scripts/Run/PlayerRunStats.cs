using UnityEngine;

/// <summary>
/// Aplica al jugador su build (atributos y habilidades de RunState) al entrar a cada arena y cada vez que compra algo,
/// y guarda su vida actual antes de pasar a la siguiente arena.
/// Va en el mismo objeto que PlayerAttack, PlayerMovement y PlayerHealth.
/// </summary>
public class PlayerRunStats : MonoBehaviour
{
    private PlayerAttack attack;
    private PlayerMovement movement;
    private PlayerHealth health;

    private int baseDamage;
    private float baseDashCooldown;
    private float baseChargeTime;
    private int baseMaxHealth;

    private void Awake()
    {
        attack = GetComponent<PlayerAttack>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();

        // El parry vive en un componente propio; se agrega solo para no tener que tocar las escenas
        if (GetComponent<PlayerParry>() == null) gameObject.AddComponent<PlayerParry>();

        // Valores base de la escena, antes de cualquier mejora
        if (attack != null)
        {
            baseDamage = attack.AttackDamage;
            baseChargeTime = attack.ChargeTime;
        }
        if (movement != null) baseDashCooldown = movement.DashCooldown;
        if (health != null) baseMaxHealth = health.MaxHealth;
    }

    private void Start()
    {
        ApplyStats();

        if (health != null)
        {
            int max = MaxHealth();
            int current = RunState.CarriedHealth >= 0 ? Mathf.Min(RunState.CarriedHealth, max) : max;
            health.SetHealth(max, current);
        }

        if (movement != null) movement.RefillDashCharges();
        SkillHud.Create(gameObject);
    }

    private int MaxHealth() => baseMaxHealth + BuildCatalog.HealthPerLevel * RunState.StatLevel(StatType.Vida);

    /// <summary>Recalcula daño, espera del dash y tiempo de carga a partir de los niveles de atributos.</summary>
    public void ApplyStats()
    {
        if (attack != null)
        {
            attack.AttackDamage = Mathf.RoundToInt(baseDamage * (1f + BuildCatalog.DamagePerLevel * RunState.StatLevel(StatType.Dano)));
            attack.ChargeTime = baseChargeTime - BuildCatalog.ChargeReductionPerLevel * RunState.StatLevel(StatType.Carga);
        }

        if (movement != null)
        {
            float reduction = BuildCatalog.DashReductionPerLevel * RunState.StatLevel(StatType.Dash);
            movement.DashCooldown = baseDashCooldown * Mathf.Max(0.2f, 1f - reduction);
        }
    }

    /// <summary>Se llama al comprar un nivel de atributo en la tienda.</summary>
    public void OnStatBought(StatType stat)
    {
        ApplyStats();

        // Cada nivel de vida también recupera esos HP
        if (stat == StatType.Vida && health != null)
            health.SetHealth(MaxHealth(), health.CurrentHealth + BuildCatalog.HealthPerLevel);
    }

    /// <summary>Se llama al comprar una habilidad en la tienda.</summary>
    public void OnSkillBought(SkillType skill)
    {
        ApplyStats();
        if (skill == SkillType.DashDoble && movement != null) movement.RefillDashCharges();
    }

    /// <summary>Guarda la vida actual para la siguiente arena.</summary>
    public void SaveForNextArena()
    {
        if (health != null) RunState.CarriedHealth = health.CurrentHealth;
    }
}

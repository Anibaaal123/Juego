using UnityEngine;

/// <summary>
/// Aplica al jugador las mejoras acumuladas en la partida (RunState) al entrar a cada arena,
/// y guarda su vida actual antes de pasar a la siguiente.
/// Va en el mismo objeto que PlayerAttack, PlayerMovement y PlayerHealth.
/// </summary>
public class PlayerRunStats : MonoBehaviour
{
    private PlayerAttack attack;
    private PlayerMovement movement;
    private PlayerHealth health;

    private int baseDamage;
    private float baseDashCooldown;
    private int baseMaxHealth;

    private void Awake()
    {
        attack = GetComponent<PlayerAttack>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();

        // Valores base de la escena, antes de cualquier mejora
        if (attack != null) baseDamage = attack.AttackDamage;
        if (movement != null) baseDashCooldown = movement.DashCooldown;
        if (health != null) baseMaxHealth = health.MaxHealth;
    }

    private void Start()
    {
        ApplyStats();
        int max = baseMaxHealth + RunState.BonusMaxHealth;
        int current = RunState.CarriedHealth >= 0 ? Mathf.Min(RunState.CarriedHealth, max) : max;
        if (health != null) health.SetHealth(max, current);
    }

    /// <summary>Recalcula daño, cooldown del dash y vida máxima a partir de los valores base + RunState.</summary>
    private void ApplyStats()
    {
        if (attack != null)
            attack.AttackDamage = Mathf.RoundToInt(baseDamage * (1f + RunState.DamageBonusPercent));

        if (movement != null)
            movement.DashCooldown = baseDashCooldown * Mathf.Max(0.1f, 1f - RunState.DashCooldownReductionPercent);
    }

    /// <summary>Aplica una mejora tomada de un pedestal (permanente durante la partida).</summary>
    public void ApplyUpgrade(Upgrade upgrade)
    {
        RunState.UpgradesTaken++;

        switch (upgrade.Type)
        {
            case UpgradeType.Poder:
                RunState.DamageBonusPercent += upgrade.DamagePercent;
                break;
            case UpgradeType.Agilidad:
                RunState.DashCooldownReductionPercent += upgrade.DashReductionPercent;
                break;
            case UpgradeType.Vital:
                RunState.BonusMaxHealth += upgrade.BonusHealth;
                if (health != null)
                {
                    int max = baseMaxHealth + RunState.BonusMaxHealth;
                    int current = upgrade.HealsToFull ? max : health.CurrentHealth + upgrade.BonusHealth;
                    health.SetHealth(max, current);
                }
                break;
        }

        ApplyStats();
        Debug.Log($"[RUN] Mejora: {upgrade.Title} ({upgrade.RarityName}) {upgrade.EffectText}. " +
                  $"Daño={attack?.AttackDamage} Dash={movement?.DashCooldown:F2}s HP={health?.CurrentHealth}/{health?.MaxHealth}");
    }

    /// <summary>Guarda la vida actual para la siguiente arena.</summary>
    public void SaveForNextArena()
    {
        if (health != null) RunState.CarriedHealth = health.CurrentHealth;
    }
}

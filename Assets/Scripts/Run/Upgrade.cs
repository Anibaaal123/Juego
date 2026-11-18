using UnityEngine;

public enum UpgradeType { Poder, Agilidad, Vital }
public enum UpgradeRarity { Comun, Raro, Legendario }

/// <summary>
/// Una mejora concreta (tipo + rareza) con los valores del GDD (sección 2.3):
///   Común  (roll 1-60):   +15 % daño | -15 % cooldown dash | +20 HP máx.
///   Raro   (roll 61-90):  +40 % daño | -35 % cooldown dash | +50 HP máx.
///   Legendario (91-100):  +80 % daño | -60 % cooldown dash | +100 HP máx. y curación al 100 %
/// </summary>
public readonly struct Upgrade
{
    public readonly UpgradeType Type;
    public readonly UpgradeRarity Rarity;

    public Upgrade(UpgradeType type, UpgradeRarity rarity)
    {
        Type = type;
        Rarity = rarity;
    }

    /// <summary>Tira Random.Range(1, 101) y decide la rareza: 60 % común, 30 % raro, 10 % legendario.</summary>
    public static UpgradeRarity RollRarity()
    {
        int roll = Random.Range(1, 101);
        if (roll <= 60) return UpgradeRarity.Comun;
        if (roll <= 90) return UpgradeRarity.Raro;
        return UpgradeRarity.Legendario;
    }

    public float DamagePercent => Rarity switch
    {
        UpgradeRarity.Comun => 0.15f,
        UpgradeRarity.Raro => 0.40f,
        _ => 0.80f,
    };

    public float DashReductionPercent => Rarity switch
    {
        UpgradeRarity.Comun => 0.15f,
        UpgradeRarity.Raro => 0.35f,
        _ => 0.60f,
    };

    public int BonusHealth => Rarity switch
    {
        UpgradeRarity.Comun => 20,
        UpgradeRarity.Raro => 50,
        _ => 100,
    };

    public bool HealsToFull => Type == UpgradeType.Vital && Rarity == UpgradeRarity.Legendario;

    public Color RarityColor => Rarity switch
    {
        UpgradeRarity.Comun => Color.white,
        UpgradeRarity.Raro => new Color(0.3f, 0.6f, 1f),
        _ => new Color(1f, 0.8f, 0.15f),
    };

    public string Title => Type switch
    {
        UpgradeType.Poder => "Núcleo de Poder",
        UpgradeType.Agilidad => "Núcleo de Agilidad",
        _ => "Núcleo Vital",
    };

    public string RarityName => Rarity switch
    {
        UpgradeRarity.Comun => "Común",
        UpgradeRarity.Raro => "Raro",
        _ => "Legendario",
    };

    public string EffectText => Type switch
    {
        UpgradeType.Poder => $"+{DamagePercent * 100f:0}% daño",
        UpgradeType.Agilidad => $"-{DashReductionPercent * 100f:0}% espera del dash",
        _ => HealsToFull ? $"+{BonusHealth} HP máx. y curación total" : $"+{BonusHealth} HP máx.",
    };
}

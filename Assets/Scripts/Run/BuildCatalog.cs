using System.Linq;

public enum StatType { Vida, Dano, Dash, Carga }

public enum SkillType { Parry, Combo, OndaEspada, DashDoble, Vampirismo, SegundoAliento }

/// <summary>
/// Catálogo de mejoras del personaje: atributos con niveles (baratos) y habilidades (caras, se compran una vez).
/// Los puntos se ganan al vencer a cada jefe según la nota del combate (ver PerformanceReport).
/// </summary>
public static class BuildCatalog
{
    public const int MaxStatLevel = 5;

    /// <summary>Costo de cada nivel de un atributo: el nivel 1 cuesta 1, el 2 cuesta 1, el 3 cuesta 2, el 4 cuesta 2 y el 5 cuesta 3.</summary>
    public static readonly int[] StatLevelCosts = { 1, 1, 2, 2, 3 };

    // Efecto de cada nivel de atributo
    public const int HealthPerLevel = 15;
    public const float DamagePerLevel = 0.10f;
    public const float DashReductionPerLevel = 0.10f;
    public const float ChargeReductionPerLevel = 0.08f;

    // Valores de las habilidades
    public const int VampirismHeal = 2;
    public const float SecondWindHealthFraction = 0.30f;

    public static readonly StatType[] AllStats = { StatType.Vida, StatType.Dano, StatType.Dash, StatType.Carga };

    public static readonly SkillType[] AllSkills =
    {
        SkillType.Parry, SkillType.Combo, SkillType.OndaEspada,
        SkillType.DashDoble, SkillType.Vampirismo, SkillType.SegundoAliento,
    };

    public static string StatName(StatType stat)
    {
        switch (stat)
        {
            case StatType.Vida: return "Vida";
            case StatType.Dano: return "Daño";
            case StatType.Dash: return "Dash";
            default: return "Carga";
        }
    }

    public static string StatEffect(StatType stat)
    {
        switch (stat)
        {
            case StatType.Vida: return $"+{HealthPerLevel} HP máximos por nivel";
            case StatType.Dano: return $"+{DamagePerLevel * 100f:0}% de daño por nivel";
            case StatType.Dash: return $"-{DashReductionPerLevel * 100f:0}% de espera del dash por nivel";
            default: return $"-{ChargeReductionPerLevel:0.00} s para cargar el golpe por nivel";
        }
    }

    public static string SkillName(SkillType skill)
    {
        switch (skill)
        {
            case SkillType.Parry: return "Parry";
            case SkillType.Combo: return "Combo de 3 golpes";
            case SkillType.OndaEspada: return "Onda de espada";
            case SkillType.DashDoble: return "Dash doble";
            case SkillType.Vampirismo: return "Vampirismo";
            default: return "Segundo aliento";
        }
    }

    public static string SkillDescription(SkillType skill)
    {
        switch (skill)
        {
            case SkillType.Parry:
                return "Al apretar clic derecho justo antes de un golpe (ventana de 0.10 s) lo anulas por completo, aturdes al jefe 0.8 s y devuelves sus bolas de fuego.";
            case SkillType.Combo:
                return "Clics seguidos encadenan 3 golpes. El tercero hace ×1.8 de daño y empuja al jefe.";
            case SkillType.OndaEspada:
                return "El golpe cargado lanza además una onda de corte a distancia.";
            case SkillType.DashDoble:
                return "El dash tiene 2 cargas: puedes esquivar dos veces seguidas.";
            case SkillType.Vampirismo:
                return $"Recuperas {VampirismHeal} HP por cada golpe que acierta.";
            default:
                return $"Una vez por partida, al caer a 0 HP revives con el {SecondWindHealthFraction * 100f:0}% de tu vida.";
        }
    }

    public static int SkillCost(SkillType skill)
    {
        switch (skill)
        {
            case SkillType.Parry:
            case SkillType.SegundoAliento:
                return 4;
            default:
                return 3;
        }
    }

    public static bool IsValid(SkillType skill) => AllSkills.Contains(skill);
}

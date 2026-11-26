using System.Collections.Generic;

/// <summary>
/// Estado de la partida de Boss Rush que sobrevive a los cambios de escena (arena → arena):
/// puntos de mejora sin gastar, niveles de atributos y habilidades compradas (la "build" del jugador).
/// Se reinicia al morir o al empezar una partida nueva.
/// </summary>
public static class RunState
{
    /// <summary>Puntos con los que se empieza una partida: solo alcanzan para atributos (la habilidad más barata cuesta 3).</summary>
    public const int InitialPoints = 2;

    private static readonly int[] statLevels = new int[4];
    private static readonly HashSet<SkillType> skills = new HashSet<SkillType>();

    /// <summary>Puntos de mejora disponibles. Lo que no se gasta se guarda para el siguiente jefe.</summary>
    public static int Points { get; set; } = InitialPoints;

    /// <summary>Vida con la que el jugador entra a la siguiente arena. -1 = vida completa.</summary>
    public static int CarriedHealth { get; set; } = -1;

    /// <summary>True si ya se gastó el "Segundo aliento" de esta partida.</summary>
    public static bool SecondWindUsed { get; set; }

    /// <summary>True si ya se mostró la tienda inicial de esta partida.</summary>
    public static bool StartShopDone { get; set; }

    /// <summary>Jefes vencidos en esta partida.</summary>
    public static int BossesDefeated { get; set; }

    public static int StatLevel(StatType stat) => statLevels[(int)stat];

    /// <summary>El Parry viene incluido desde el inicio; el resto de las habilidades se compran en la tienda.</summary>
    public static bool Has(SkillType skill) => skill == SkillType.Parry || skills.Contains(skill);

    /// <summary>Costo de subir el atributo al siguiente nivel, o -1 si ya está al máximo.</summary>
    public static int NextStatCost(StatType stat)
    {
        int level = StatLevel(stat);
        return level >= BuildCatalog.MaxStatLevel ? -1 : BuildCatalog.StatLevelCosts[level];
    }

    public static bool TryBuyStat(StatType stat)
    {
        int cost = NextStatCost(stat);
        if (cost < 0 || Points < cost) return false;
        Points -= cost;
        statLevels[(int)stat]++;
        return true;
    }

    public static bool TryBuySkill(SkillType skill)
    {
        if (Has(skill)) return false;
        int cost = BuildCatalog.SkillCost(skill);
        if (Points < cost) return false;
        Points -= cost;
        skills.Add(skill);
        return true;
    }

    public static void Reset()
    {
        Points = InitialPoints;
        for (int i = 0; i < statLevels.Length; i++) statLevels[i] = 0;
        skills.Clear();
        CarriedHealth = -1;
        SecondWindUsed = false;
        StartShopDone = false;
        BossesDefeated = 0;
    }
}

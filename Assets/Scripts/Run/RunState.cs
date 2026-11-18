/// <summary>
/// Estado de la partida de Boss Rush que sobrevive a los cambios de escena (arena → arena).
/// Se reinicia al morir o al empezar una partida nueva.
/// </summary>
public static class RunState
{
    /// <summary>Suma de los % de daño obtenidos (0.15 = +15 %). Se aplica sobre el daño base.</summary>
    public static float DamageBonusPercent { get; set; }

    /// <summary>Suma de los % de reducción del cooldown del dash (0.35 = -35 %).</summary>
    public static float DashCooldownReductionPercent { get; set; }

    /// <summary>HP máximos extra acumulados.</summary>
    public static int BonusMaxHealth { get; set; }

    /// <summary>Vida con la que el jugador entra a la siguiente arena. -1 = vida completa.</summary>
    public static int CarriedHealth { get; set; } = -1;

    /// <summary>Cuántas mejoras se han tomado en esta partida.</summary>
    public static int UpgradesTaken { get; set; }

    public static void Reset()
    {
        DamageBonusPercent = 0f;
        DashCooldownReductionPercent = 0f;
        BonusMaxHealth = 0;
        CarriedHealth = -1;
        UpgradesTaken = 0;
    }
}

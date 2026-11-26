using UnityEngine;

public enum Rank { C, B, A, S }

/// <summary>
/// Resumen del rendimiento del jugador contra un jefe y la nota resultante.
///   Tiempo (40 pts): máximo si termina en 'fastTime' o menos, 0 si tarda 'slowTime' o más.
///   Golpes recibidos (40 pts): 40 menos 10 por cada golpe recibido.
///   Precisión (20 pts): 20 × (golpes que acertaron / golpes lanzados).
/// Nota: S ≥ 85, A ≥ 65, B ≥ 40, C el resto. Puntos de mejora: S = 6, A = 4, B = 3, C = 2.
/// </summary>
public readonly struct PerformanceReport
{
    public const float MaxTimeScore = 40f;
    public const float MaxHitsScore = 40f;
    public const float MaxAccuracyScore = 20f;
    public const float HitPenalty = 10f;

    public const float RankSThreshold = 85f;
    public const float RankAThreshold = 65f;
    public const float RankBThreshold = 40f;

    public readonly float Seconds;
    public readonly int HitsTaken;
    public readonly int Swings;
    public readonly int SwingsLanded;
    public readonly int Parries;
    public readonly float HealthFraction;
    public readonly float TimeScore;
    public readonly float HitsScore;
    public readonly float AccuracyScore;

    public float Accuracy => Swings > 0 ? (float)SwingsLanded / Swings : 0f;
    public float Total => TimeScore + HitsScore + AccuracyScore;
    public int Points => PointsFor(Grade);

    public Rank Grade
    {
        get
        {
            float total = Total;
            if (total >= RankSThreshold) return Rank.S;
            if (total >= RankAThreshold) return Rank.A;
            if (total >= RankBThreshold) return Rank.B;
            return Rank.C;
        }
    }

    private PerformanceReport(float seconds, int hits, int swings, int landed, int parries, float healthFraction,
                              float timeScore, float hitsScore, float accuracyScore)
    {
        Seconds = seconds;
        HitsTaken = hits;
        Swings = swings;
        SwingsLanded = landed;
        Parries = parries;
        HealthFraction = healthFraction;
        TimeScore = timeScore;
        HitsScore = hitsScore;
        AccuracyScore = accuracyScore;
    }

    public static int PointsFor(Rank rank)
    {
        switch (rank)
        {
            case Rank.S: return 6;
            case Rank.A: return 4;
            case Rank.B: return 3;
            default: return 2;
        }
    }

    /// <summary>Calcula el reporte con las estadísticas de CombatStats. fastTime y slowTime definen la escala del tiempo.</summary>
    public static PerformanceReport Build(float fastTime, float slowTime, float healthFraction)
    {
        float seconds = CombatStats.Elapsed;
        float span = Mathf.Max(1f, slowTime - fastTime);
        float timeScore = MaxTimeScore * Mathf.Clamp01((slowTime - seconds) / span);
        float hitsScore = Mathf.Max(0f, MaxHitsScore - HitPenalty * CombatStats.HitsTaken);

        float accuracy = CombatStats.Swings > 0 ? (float)CombatStats.SwingsLanded / CombatStats.Swings : 0f;
        float accuracyScore = MaxAccuracyScore * accuracy;

        return new PerformanceReport(seconds, CombatStats.HitsTaken, CombatStats.Swings, CombatStats.SwingsLanded,
                                     CombatStats.Parries, Mathf.Clamp01(healthFraction), timeScore, hitsScore, accuracyScore);
    }
}

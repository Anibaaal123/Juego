using UnityEngine;

/// <summary>
/// Estadísticas del combate actual contra un jefe: tiempo, golpes recibidos, precisión y parries.
/// Empiezan a contar con Begin() y se congelan con End() cuando el jefe muere.
/// </summary>
public static class CombatStats
{
    public static float StartTime { get; private set; }
    public static float EndTime { get; private set; }
    public static bool Ended { get; private set; }

    public static int HitsTaken { get; private set; }
    public static int DamageTaken { get; private set; }
    public static int Swings { get; private set; }
    public static int SwingsLanded { get; private set; }
    public static int ChargedLanded { get; private set; }
    public static int Parries { get; private set; }

    public static float Elapsed => (Ended ? EndTime : Time.time) - StartTime;

    public static void Begin()
    {
        StartTime = Time.time;
        EndTime = 0f;
        Ended = false;
        HitsTaken = DamageTaken = Swings = SwingsLanded = ChargedLanded = Parries = 0;
    }

    public static void End()
    {
        if (Ended) return;
        EndTime = Time.time;
        Ended = true;
    }

    public static void RegisterHit(int damage)
    {
        if (Ended) return;
        HitsTaken++;
        DamageTaken += damage;
    }

    public static void RegisterSwing()
    {
        if (!Ended) Swings++;
    }

    public static void RegisterSwingResult(bool landed, bool charged)
    {
        if (Ended || !landed) return;
        SwingsLanded++;
        if (charged) ChargedLanded++;
    }

    public static void RegisterParry()
    {
        if (!Ended) Parries++;
    }
}

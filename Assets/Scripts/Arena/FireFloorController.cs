using System.Collections;
using UnityEngine;

/// <summary>
/// Controla el ciclo de los géiseres de fuego de la Arena 2.
/// Ciclo: espera 'idleTime' → elige 'ventsPerCycle' géiseres al azar → Advertencia 1.0 s → Fuego 2.0 s → repite.
/// </summary>
public class FireFloorController : MonoBehaviour
{
    [SerializeField] private FireVent[] vents;

    [Header("Ciclo (GDD: advertencia 1.0 s, descarga 2.0 s)")]
    [SerializeField] private float initialDelay = 3.0f;
    [SerializeField] private float idleTime = 2.5f;
    [SerializeField] private float warningTime = 1.0f;
    [SerializeField] private float activeTime = 2.0f;
    [Tooltip("Cuántos géiseres se activan a la vez en cada ciclo")]
    [SerializeField] private int ventsPerCycle = 2;

    [Tooltip("Opcional: el piso se apaga cuando muere este jefe")]
    [SerializeField] private EnemyHealth boss;

    private void Start()
    {
        if (vents == null || vents.Length == 0) vents = GetComponentsInChildren<FireVent>();
        if (boss != null) boss.Died += _ => StopFloor();
        StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            FireVent[] chosen = PickRandomVents(Mathf.Clamp(ventsPerCycle, 1, vents.Length));

            foreach (FireVent v in chosen) v.SetState(FireVent.VentState.Warning);
            yield return new WaitForSeconds(warningTime);

            foreach (FireVent v in chosen) v.SetState(FireVent.VentState.Active);
            yield return new WaitForSeconds(activeTime);

            foreach (FireVent v in chosen) v.SetState(FireVent.VentState.Inactive);
            yield return new WaitForSeconds(idleTime);
        }
    }

    private FireVent[] PickRandomVents(int count)
    {
        // Barajado parcial de Fisher-Yates sobre una copia
        FireVent[] pool = (FireVent[])vents.Clone();
        for (int i = 0; i < count; i++)
        {
            int j = Random.Range(i, pool.Length);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        FireVent[] result = new FireVent[count];
        System.Array.Copy(pool, result, count);
        return result;
    }

    /// <summary>Detiene el ciclo y apaga todos los géiseres (por ejemplo, al morir el jefe).</summary>
    public void StopFloor()
    {
        StopAllCoroutines();
        foreach (FireVent v in vents) v.SetState(FireVent.VentState.Inactive);
        enabled = false;
    }
}

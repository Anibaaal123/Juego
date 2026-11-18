using System.Collections;
using UnityEngine;

/// <summary>
/// Controla el ciclo de las baldosas eléctricas de la Arena 2.
/// Ciclo: espera 'idleTime' → elige 'tilesPerCycle' baldosas al azar → Advertencia 1.0 s → Activas 2.0 s → repite.
/// </summary>
public class ElectricFloorController : MonoBehaviour
{
    [SerializeField] private ElectricTile[] tiles;

    [Header("Ciclo (GDD: advertencia 1.0 s, descarga 2.0 s)")]
    [SerializeField] private float initialDelay = 3.0f;
    [SerializeField] private float idleTime = 2.5f;
    [SerializeField] private float warningTime = 1.0f;
    [SerializeField] private float activeTime = 2.0f;
    [Tooltip("Cuántas baldosas se activan a la vez en cada ciclo")]
    [SerializeField] private int tilesPerCycle = 2;

    [Tooltip("Opcional: el piso se apaga cuando muere este jefe")]
    [SerializeField] private EnemyHealth boss;

    private void Start()
    {
        if (tiles == null || tiles.Length == 0) tiles = GetComponentsInChildren<ElectricTile>();
        if (boss != null) boss.Died += _ => StopFloor();
        StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            ElectricTile[] chosen = PickRandomTiles(Mathf.Clamp(tilesPerCycle, 1, tiles.Length));

            foreach (ElectricTile t in chosen) t.SetState(ElectricTile.TileState.Warning);
            yield return new WaitForSeconds(warningTime);

            foreach (ElectricTile t in chosen) t.SetState(ElectricTile.TileState.Active);
            yield return new WaitForSeconds(activeTime);

            foreach (ElectricTile t in chosen) t.SetState(ElectricTile.TileState.Inactive);
            yield return new WaitForSeconds(idleTime);
        }
    }

    private ElectricTile[] PickRandomTiles(int count)
    {
        // Barajado parcial de Fisher-Yates sobre una copia
        ElectricTile[] pool = (ElectricTile[])tiles.Clone();
        for (int i = 0; i < count; i++)
        {
            int j = Random.Range(i, pool.Length);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        ElectricTile[] result = new ElectricTile[count];
        System.Array.Copy(pool, result, count);
        return result;
    }

    /// <summary>Detiene el ciclo y apaga todas las baldosas (por ejemplo, al morir el jefe).</summary>
    public void StopFloor()
    {
        StopAllCoroutines();
        foreach (ElectricTile t in tiles) t.SetState(ElectricTile.TileState.Inactive);
        enabled = false;
    }
}

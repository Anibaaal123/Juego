using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Atajo de pruebas: F1, F2 y F3 saltan directo a la Arena 1, 2 o 3. Al saltar a la 2 o la 3 se
/// entrega una build de ejemplo (atributos y Combo) para probar como si ya se hubieran vencido
/// los jefes anteriores. Se crea solo; para quitarlo basta con borrar este archivo.
/// </summary>
public class DebugArenaJump : MonoBehaviour
{
    private static readonly string[] Arenas = { "Arena1_Demonio", "Arena2_Centinela", "Arena3_Guardian" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<DebugArenaJump>() != null) return;
        var go = new GameObject("DebugArenaJump");
        go.AddComponent<DebugArenaJump>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        for (int i = 0; i < Arenas.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.F1 + i)) Jump(i);
        }
    }

    private static void Jump(int index)
    {
        Time.timeScale = 1f;
        RunState.Reset();
        if (index > 0)
        {
            RunState.Points = 30;
            RunState.TryBuyStat(StatType.Vida);
            RunState.TryBuyStat(StatType.Vida);
            RunState.TryBuyStat(StatType.Dano);
            RunState.TryBuyStat(StatType.Dano);
            RunState.TryBuyStat(StatType.Dash);
            RunState.TryBuySkill(SkillType.Combo);
            RunState.Points = 0;
            RunState.StartShopDone = true;
            RunState.BossesDefeated = index;
        }
        SceneManager.LoadScene(Arenas[index]);
    }
}

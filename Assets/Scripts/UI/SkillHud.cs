using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador en la esquina inferior izquierda: cargas del dash, estado del parry, segundo aliento y habilidades pasivas.
/// Lo crea PlayerRunStats al entrar a cada arena.
/// </summary>
public class SkillHud : MonoBehaviour
{
    private Text text;
    private PlayerMovement movement;
    private PlayerParry parry;

    public static SkillHud Create(GameObject player)
    {
        var go = new GameObject("SkillHud");
        var hud = go.AddComponent<SkillHud>();
        hud.movement = player.GetComponent<PlayerMovement>();
        hud.parry = player.GetComponent<PlayerParry>();

        Canvas canvas = UiKit.CreateCanvas("SkillHudCanvas", 50, go.transform);
        hud.text = UiKit.MakeText(canvas.transform, "Texto", "", 30, Color.white, Vector2.zero,
                                  new Vector2(290f, 150f), new Vector2(560f, 260f), TextAnchor.LowerLeft, FontStyle.Bold);
        return hud;
    }

    private void Update()
    {
        if (text == null) return;

        var sb = new System.Text.StringBuilder();

        if (movement != null) sb.AppendLine($"DASH  {movement.DashCharges}/{movement.MaxDashCharges}");

        if (RunState.Has(SkillType.Parry) && parry != null)
        {
            string state = parry.IsActive ? "¡ACTIVO!" : parry.IsReady ? "LISTO" : $"{parry.CooldownRemaining:0.0} s";
            sb.AppendLine($"PARRY (clic der.)  {state}");
        }

        if (RunState.Has(SkillType.SegundoAliento))
            sb.AppendLine("SEGUNDO ALIENTO  " + (RunState.SecondWindUsed ? "usado" : "listo"));

        string[] passive = new[] { SkillType.Combo, SkillType.OndaEspada, SkillType.Vampirismo }
                           .Where(RunState.Has).Select(BuildCatalog.SkillName).ToArray();
        if (passive.Length > 0) sb.AppendLine(string.Join("  ·  ", passive));

        text.text = sb.ToString().TrimEnd();
    }
}

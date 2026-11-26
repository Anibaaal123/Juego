using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantallas que aparecen al vencer a un jefe (el juego queda en pausa mientras están abiertas):
///   • ShowSummary: resumen del rendimiento (tiempo, golpes recibidos, precisión) y la nota con sus puntos.
///   • ShowShop: tienda donde se gastan los puntos en atributos y habilidades.
/// Se usan desde una corrutina: yield return RunPanels.ShowSummary(...);
/// </summary>
public static class RunPanels
{
    private static readonly Color Gold = new Color(1f, 0.78f, 0.25f);
    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color PanelBg = new Color(0.07f, 0.03f, 0.12f, 0.98f);
    private static readonly Color RowBg = new Color(0.16f, 0.08f, 0.24f, 1f);
    private static readonly Color RowOwnedBg = new Color(0.1f, 0.26f, 0.16f, 1f);
    private static readonly Color ButtonColor = new Color(0.36f, 0.16f, 0.5f, 1f);
    private static readonly Color Muted = new Color(0.78f, 0.76f, 0.86f);
    private static readonly Color PipOff = new Color(0.07f, 0.04f, 0.1f, 1f);

    private class StatRowView
    {
        public StatType Stat;
        public Image[] Pips;
        public Button Button;
    }

    private class SkillCardView
    {
        public SkillType Skill;
        public Image Background;
        public Button Button;
    }

    // ---------------- Pausa ----------------

    /// <summary>Pausa o reanuda el juego y bloquea los controles del jugador mientras hay un panel abierto.</summary>
    public static void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        SetEnabled(Object.FindFirstObjectByType<PlayerMovement>(), !paused);
        SetEnabled(Object.FindFirstObjectByType<PlayerAttack>(), !paused);
        SetEnabled(Object.FindFirstObjectByType<PlayerGuard>(), !paused);
    }

    private static void SetEnabled(Behaviour behaviour, bool enabled)
    {
        if (behaviour != null) behaviour.enabled = enabled;
    }

    // ---------------- Resumen del combate ----------------

    public static IEnumerator ShowSummary(PerformanceReport report, string bossName, bool isLastArena)
    {
        SetPaused(true);
        Canvas canvas = UiKit.CreateCanvas("ResumenCanvas", 300);
        Transform root = canvas.transform;

        UiKit.MakeImage("Fondo", root, Dim, UiKit.Center, Vector2.zero, new Vector2(4000f, 3000f));
        UiKit.MakeImage("Borde", root, Gold, UiKit.Center, Vector2.zero, new Vector2(1216f, 976f));
        UiKit.MakeImage("Panel", root, PanelBg, UiKit.Center, Vector2.zero, new Vector2(1200f, 960f));

        UiKit.MakeText(root, "Titulo", "¡JEFE DERROTADO!", 68, Gold, UiKit.Center, new Vector2(0f, 410f),
                       new Vector2(1100f, 90f), TextAnchor.MiddleCenter, FontStyle.Bold);
        UiKit.MakeText(root, "Jefe", bossName, 42, Color.white, UiKit.Center, new Vector2(0f, 345f),
                       new Vector2(1100f, 60f));

        var rows = new List<GameObject>
        {
            MakeSummaryRow(root, 250f, "Tiempo", FormatTime(report.Seconds), $"{report.TimeScore:0} / {PerformanceReport.MaxTimeScore:0}"),
            MakeSummaryRow(root, 180f, "Golpes recibidos", report.HitsTaken.ToString(), $"{report.HitsScore:0} / {PerformanceReport.MaxHitsScore:0}"),
            MakeSummaryRow(root, 110f, "Precisión", $"{report.Accuracy * 100f:0}%  ({report.SwingsLanded}/{report.Swings})", $"{report.AccuracyScore:0} / {PerformanceReport.MaxAccuracyScore:0}"),
            MakeSummaryRow(root, 40f, "Vida restante", $"{report.HealthFraction * 100f:0}%", ""),
        };
        if (RunState.Has(SkillType.Parry))
            rows.Add(MakeSummaryRow(root, -30f, "Parries exitosos", report.Parries.ToString(), ""));

        // Total y nota
        var totalGroup = new GameObject("Total", typeof(RectTransform));
        totalGroup.layer = 5;
        totalGroup.transform.SetParent(root, false);
        UiKit.MakeImage("Separador", totalGroup.transform, Gold, UiKit.Center, new Vector2(0f, -95f), new Vector2(1060f, 4f));
        UiKit.MakeText(totalGroup.transform, "Puntuacion", $"PUNTUACIÓN  {report.Total:0} / 100", 50, Color.white,
                       UiKit.Center, new Vector2(-230f, -180f), new Vector2(640f, 80f), TextAnchor.MiddleCenter, FontStyle.Bold);

        Color rankColor = RankColor(report.Grade);
        UiKit.MakeText(totalGroup.transform, "NotaEtiqueta", "NOTA", 34, Muted, UiKit.Center, new Vector2(290f, -150f),
                       new Vector2(200f, 50f));
        Text rankText = UiKit.MakeText(totalGroup.transform, "Nota", report.Grade.ToString(), 190, rankColor, UiKit.Center,
                                       new Vector2(430f, -190f), new Vector2(260f, 220f), TextAnchor.MiddleCenter, FontStyle.Bold);

        if (!isLastArena)
        {
            UiKit.MakeText(totalGroup.transform, "Puntos", $"+{report.Points} PUNTOS DE MEJORA", 46, Gold, UiKit.Center,
                           new Vector2(-170f, -290f), new Vector2(760f, 70f), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        foreach (GameObject row in rows) row.SetActive(false);
        totalGroup.SetActive(false);

        bool revealed = false, skip = false, done = false;
        Button continueButton = UiKit.MakeButton(root, "Continuar", "CONTINUAR", 42, UiKit.Center, new Vector2(0f, -400f),
                                                 new Vector2(440f, 90f), ButtonColor, () =>
                                                 {
                                                     if (revealed) done = true;
                                                     else skip = true;
                                                 });
        UiKit.SelectDefault(continueButton);

        // Las líneas van apareciendo una a una; un clic en CONTINUAR las muestra todas de golpe
        foreach (GameObject row in rows)
        {
            row.SetActive(true);
            float t = 0f;
            while (t < 0.5f && !skip)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        totalGroup.SetActive(true);
        float p = 0f;
        while (p < 1f && !skip)
        {
            p += Time.unscaledDeltaTime / 0.35f;
            rankText.transform.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p)));
            yield return null;
        }
        rankText.transform.localScale = Vector3.one;
        foreach (GameObject row in rows) row.SetActive(true);
        revealed = true;

        yield return new WaitUntil(() => done);

        Object.Destroy(canvas.gameObject);
        SetPaused(false);
    }

    private static GameObject MakeSummaryRow(Transform parent, float y, string label, string value, string score)
    {
        RectTransform rt = UiKit.MakeRect("Fila", parent, UiKit.Center, new Vector2(0f, y), new Vector2(1100f, 60f));
        UiKit.MakeText(rt, "Etiqueta", label, 38, Color.white, UiKit.Center, new Vector2(-300f, 0f), new Vector2(480f, 60f), TextAnchor.MiddleLeft);
        UiKit.MakeText(rt, "Valor", value, 38, Gold, UiKit.Center, new Vector2(130f, 0f), new Vector2(430f, 60f), TextAnchor.MiddleCenter, FontStyle.Bold);
        UiKit.MakeText(rt, "Puntaje", score, 38, Muted, UiKit.Center, new Vector2(440f, 0f), new Vector2(240f, 60f), TextAnchor.MiddleRight);
        return rt.gameObject;
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.RoundToInt(seconds);
        return $"{total / 60}:{total % 60:00}";
    }

    private static Color RankColor(Rank rank)
    {
        switch (rank)
        {
            case Rank.S: return new Color(1f, 0.82f, 0.2f);
            case Rank.A: return new Color(0.45f, 0.95f, 0.55f);
            case Rank.B: return new Color(0.45f, 0.7f, 1f);
            default: return new Color(0.78f, 0.78f, 0.84f);
        }
    }

    // ---------------- Tienda de mejoras ----------------

    /// <summary>Tienda para gastar puntos. isStart = true en la tienda inicial, antes del primer jefe.</summary>
    public static IEnumerator ShowShop(bool isStart)
    {
        SetPaused(true);
        PlayerRunStats stats = Object.FindFirstObjectByType<PlayerRunStats>();

        Canvas canvas = UiKit.CreateCanvas("TiendaCanvas", 300);
        Transform root = canvas.transform;

        UiKit.MakeImage("Fondo", root, Dim, UiKit.Center, Vector2.zero, new Vector2(4000f, 3000f));
        UiKit.MakeImage("Borde", root, Gold, UiKit.Center, Vector2.zero, new Vector2(1796f, 996f));
        UiKit.MakeImage("Panel", root, PanelBg, UiKit.Center, Vector2.zero, new Vector2(1780f, 980f));

        UiKit.MakeText(root, "Titulo", isStart ? "ELIGE TUS ATRIBUTOS INICIALES" : "MEJORA A TU PERSONAJE", 58, Gold, UiKit.Center,
                       new Vector2(-330f, 435f), new Vector2(1000f, 80f), TextAnchor.MiddleLeft, FontStyle.Bold);
        Text pointsText = UiKit.MakeText(root, "Puntos", "", 58, Gold, UiKit.Center, new Vector2(590f, 435f),
                                         new Vector2(560f, 80f), TextAnchor.MiddleRight, FontStyle.Bold);

        UiKit.MakeText(root, "HeaderAtributos", "ATRIBUTOS", 40, Color.white, UiKit.Center, new Vector2(-440f, 345f), new Vector2(820f, 50f), TextAnchor.MiddleLeft, FontStyle.Bold);
        UiKit.MakeText(root, "HeaderHabilidades", "HABILIDADES", 40, Color.white, UiKit.Center, new Vector2(440f, 345f), new Vector2(820f, 50f), TextAnchor.MiddleLeft, FontStyle.Bold);

        System.Action refresh = null;

        // Atributos (columna izquierda)
        var statViews = new List<StatRowView>();
        for (int i = 0; i < BuildCatalog.AllStats.Length; i++)
        {
            StatType stat = BuildCatalog.AllStats[i];
            StatRowView view = MakeStatRow(root, stat, 250f - 130f * i, () =>
            {
                if (!RunState.TryBuyStat(stat)) return;
                if (stats != null) stats.OnStatBought(stat);
                refresh();
            });
            statViews.Add(view);
        }

        // Habilidades (columna derecha): 3 al azar entre las que aún no tienes.
        // En la tienda inicial no se ofrecen: los puntos de arranque solo alcanzan para atributos.
        List<SkillType> offers = isStart
            ? new List<SkillType>()
            : BuildCatalog.AllSkills.Where(s => !RunState.Has(s)).OrderBy(_ => Random.value).Take(3).ToList();
        var skillViews = new List<SkillCardView>();
        for (int i = 0; i < offers.Count; i++)
        {
            SkillType skill = offers[i];
            SkillCardView view = MakeSkillCard(root, skill, 205f - 215f * i, () =>
            {
                if (!RunState.TryBuySkill(skill)) return;
                if (stats != null) stats.OnSkillBought(skill);
                refresh();
            });
            skillViews.Add(view);
        }
        if (offers.Count == 0)
        {
            string message = isStart
                ? "Las habilidades se desbloquean\nal vencer al primer jefe."
                : "¡Ya tienes todas las habilidades!";
            UiKit.MakeText(root, "SinOfertas", message, 38, Muted, UiKit.Center,
                           new Vector2(440f, 150f), new Vector2(820f, 120f));
        }

        Text ownedText = UiKit.MakeText(root, "Tuyas", "", 30, Muted, UiKit.Center, new Vector2(-300f, -440f),
                                        new Vector2(1000f, 60f), TextAnchor.MiddleLeft);

        bool done = false;
        Button continueButton = UiKit.MakeButton(root, "Continuar", isStart ? "EMPEZAR" : "CONTINUAR", 44, UiKit.Center,
                                                 new Vector2(640f, -440f), new Vector2(420f, 90f), ButtonColor, () => done = true);

        refresh = () =>
        {
            pointsText.text = $"PUNTOS: {RunState.Points}";

            foreach (StatRowView v in statViews)
            {
                int level = RunState.StatLevel(v.Stat);
                for (int i = 0; i < v.Pips.Length; i++) v.Pips[i].color = i < level ? Gold : PipOff;

                int cost = RunState.NextStatCost(v.Stat);
                v.Button.interactable = cost >= 0 && RunState.Points >= cost;
                UiKit.SetButtonLabel(v.Button, cost < 0 ? "MÁX" : $"SUBIR ({cost})");
            }

            foreach (SkillCardView v in skillViews)
            {
                bool owned = RunState.Has(v.Skill);
                int cost = BuildCatalog.SkillCost(v.Skill);
                v.Background.color = owned ? RowOwnedBg : RowBg;
                v.Button.interactable = !owned && RunState.Points >= cost;
                UiKit.SetButtonLabel(v.Button, owned ? "OBTENIDA" : $"COMPRAR ({cost})");
            }

            string[] names = BuildCatalog.AllSkills.Where(RunState.Has).Select(BuildCatalog.SkillName).ToArray();
            ownedText.text = "Tus habilidades: " + (names.Length > 0 ? string.Join("  ·  ", names) : "ninguna todavía");
        };
        refresh();
        UiKit.SelectDefault(continueButton);

        yield return new WaitUntil(() => done);

        Object.Destroy(canvas.gameObject);
        SetPaused(false);
    }

    private static StatRowView MakeStatRow(Transform parent, StatType stat, float y, UnityEngine.Events.UnityAction onBuy)
    {
        RectTransform row = UiKit.MakeRect("Atributo_" + stat, parent, UiKit.Center, new Vector2(-440f, y), new Vector2(820f, 110f));
        var bg = row.gameObject.AddComponent<Image>();
        bg.color = RowBg;
        bg.raycastTarget = false;

        UiKit.MakeText(row, "Nombre", BuildCatalog.StatName(stat), 38, Color.white, UiKit.Center, new Vector2(-160f, 22f),
                       new Vector2(480f, 46f), TextAnchor.MiddleLeft, FontStyle.Bold);
        UiKit.MakeText(row, "Efecto", BuildCatalog.StatEffect(stat), 25, Muted, UiKit.Center, new Vector2(-160f, -24f),
                       new Vector2(480f, 40f), TextAnchor.MiddleLeft);

        var pips = new Image[BuildCatalog.MaxStatLevel];
        for (int i = 0; i < pips.Length; i++)
        {
            UiKit.MakeImage("PipBorde" + i, row, Gold, UiKit.Center, new Vector2(55f + 36f * i, 0f), new Vector2(30f, 30f));
            pips[i] = UiKit.MakeImage("Pip" + i, row, PipOff, UiKit.Center, new Vector2(55f + 36f * i, 0f), new Vector2(24f, 24f));
        }

        Button button = UiKit.MakeButton(row, "Subir", "SUBIR", 30, UiKit.Center, new Vector2(330f, 0f), new Vector2(160f, 76f),
                                         ButtonColor, onBuy);
        return new StatRowView { Stat = stat, Pips = pips, Button = button };
    }

    private static SkillCardView MakeSkillCard(Transform parent, SkillType skill, float y, UnityEngine.Events.UnityAction onBuy)
    {
        RectTransform card = UiKit.MakeRect("Habilidad_" + skill, parent, UiKit.Center, new Vector2(440f, y), new Vector2(820f, 200f));
        var bg = card.gameObject.AddComponent<Image>();
        bg.color = RowBg;
        bg.raycastTarget = false;

        UiKit.MakeText(card, "Nombre", $"{BuildCatalog.SkillName(skill)}   ·   {BuildCatalog.SkillCost(skill)} pts", 38, Gold, UiKit.Center,
                       new Vector2(-130f, 62f), new Vector2(540f, 50f), TextAnchor.MiddleLeft, FontStyle.Bold);
        UiKit.MakeText(card, "Descripcion", BuildCatalog.SkillDescription(skill), 27, Color.white, UiKit.Center,
                       new Vector2(-130f, -20f), new Vector2(540f, 110f), TextAnchor.UpperLeft, FontStyle.Normal, true);

        Button button = UiKit.MakeButton(card, "Comprar", "COMPRAR", 30, UiKit.Center, new Vector2(300f, -45f),
                                         new Vector2(200f, 86f), ButtonColor, onBuy);
        return new SkillCardView { Skill = skill, Background = bg, Button = button };
    }
}

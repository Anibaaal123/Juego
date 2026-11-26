using System.Collections;
using UnityEngine;

/// <summary>
/// Efectos generados por código (sin archivos de arte ni audio):
///   • Slash: medialuna blanca que aparece y se desvanece (cortes del jugador).
///   • RoarClip: rugido grave sintetizado para los jefes.
/// Se generan una sola vez y se reutilizan.
/// </summary>
public static class ProceduralFx
{
    private static Sprite slashSprite;
    private static AudioClip roarClip;
    private static AudioClip impactClip;

    // ---------------- Corte ----------------

    /// <summary>
    /// Muestra una medialuna en 'position'. angleDegrees = 0 apunta hacia abajo, 90 hacia la derecha, -90 hacia la izquierda.
    /// </summary>
    public static void Slash(Vector3 position, float angleDegrees, float size, Color color, int sortingOrder)
    {
        var go = new GameObject("SlashFx");
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);
        go.transform.localScale = Vector3.one * size;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSlashSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        go.AddComponent<FadeAndDestroy>();
    }

    /// <summary>Medialuna reutilizable (la usan los cortes y la onda de espada).</summary>
    public static Sprite GetSlashSprite()
    {
        if (slashSprite != null) return slashSprite;

        // Media corona (mitad inferior de un anillo), más gruesa al centro
        const int w = 64, h = 34;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Vector2 c = new Vector2(w * 0.5f, h - 2f);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2 d = new Vector2(x + 0.5f, y + 0.5f) - c;
                float r = d.magnitude;
                float along = Mathf.Abs(d.x) / 30f;                   // 0 al centro, 1 en las puntas
                float thickness = Mathf.Lerp(7f, 1f, along);
                float inner = 29f - thickness;
                float a = (d.y < 0f && r <= 30f && r >= inner) ? 1f : 0f;
                if (a > 0f && r > 30f - 1.5f) a = 0.6f;              // borde suave
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        slashSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 16f);
        return slashSprite;
    }

    // ---------------- Rugido ----------------

    /// <summary>Golpe grave contra el suelo de ~0.45 s: un seno que baja de 95 a 38 Hz más un golpe de ruido al inicio.</summary>
    public static AudioClip GetImpactClip()
    {
        if (impactClip != null) return impactClip;

        const int rate = 44100;
        const float length = 0.45f;
        int samples = Mathf.RoundToInt(rate * length);
        var data = new float[samples];
        var rng = new System.Random(3);
        float phase = 0f, noise = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)rate;
            float freq = Mathf.Lerp(95f, 38f, Mathf.Clamp01(t / length));
            phase += freq / rate;
            noise = Mathf.Lerp(noise, (float)(rng.NextDouble() * 2.0 - 1.0), 0.25f);

            float body = Mathf.Sin(phase * 2f * Mathf.PI) * 0.9f;
            float crack = noise * 0.6f * Mathf.Exp(-t * 22f);
            float envelope = Mathf.Exp(-t * 7f) * Mathf.Clamp01(t / 0.004f);
            data[i] = Mathf.Clamp((body + crack) * envelope, -1f, 1f);
        }

        impactClip = AudioClip.Create("ImpactoSintetico", samples, 1, rate, false);
        impactClip.SetData(data, 0);
        return impactClip;
    }

    /// <summary>Rugido grave de ~1.2 s: diente de sierra que baja de 110 a 55 Hz + ruido, con vibrato.</summary>
    public static AudioClip GetRoarClip()
    {
        if (roarClip != null) return roarClip;

        const int rate = 44100;
        const float length = 1.2f;
        int samples = Mathf.RoundToInt(rate * length);
        var data = new float[samples];
        var rng = new System.Random(7);
        float phase = 0f, phase2 = 0f, noise = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)rate;
            float k = t / length;
            float freq = Mathf.Lerp(110f, 55f, k) * (1f + 0.04f * Mathf.Sin(t * 2f * Mathf.PI * 28f));
            phase = Mathf.Repeat(phase + freq / rate, 1f);
            phase2 = Mathf.Repeat(phase2 + freq * 1.5f / rate, 1f);
            float saw = phase * 2f - 1f;
            float saw2 = phase2 * 2f - 1f;
            noise = Mathf.Lerp(noise, (float)(rng.NextDouble() * 2.0 - 1.0), 0.15f); // ruido filtrado (grave)

            float attack = Mathf.Clamp01(t / 0.08f);
            float release = Mathf.Clamp01((length - t) / 0.4f);
            float tremolo = 0.75f + 0.25f * Mathf.Sin(t * 2f * Mathf.PI * 18f);
            float s = (saw * 0.5f + saw2 * 0.25f + noise * 0.9f) * attack * release * tremolo;
            data[i] = Mathf.Clamp(s * 0.7f, -1f, 1f);
        }

        roarClip = AudioClip.Create("RugidoSintetico", samples, 1, rate, false);
        roarClip.SetData(data, 0);
        return roarClip;
    }
}

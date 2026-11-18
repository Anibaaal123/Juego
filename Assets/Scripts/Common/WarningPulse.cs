using UnityEngine;

/// <summary>Hace parpadear un marcador de advertencia en el suelo.</summary>
public class WarningPulse : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color baseColor;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    private void Update()
    {
        float k = 0.45f + 0.55f * Mathf.PingPong(Time.time * 6f, 1f);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * k);
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Efectos de "sensación de golpe": hitstop (congelar el juego un instante), sacudida de cámara
/// y números de daño flotantes. Se crea solo la primera vez que se usa; no hay que ponerlo en la escena.
/// </summary>
public class GameFeel : MonoBehaviour
{
    private static GameFeel instance;

    private Coroutine hitStopRoutine;
    private Coroutine shakeRoutine;
    private Vector3 cameraRestPosition;
    private Transform shakingCamera;
    private float timeScaleBeforeHitStop = 1f;

    private static GameFeel Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[GameFeel]");
                instance = go.AddComponent<GameFeel>();
            }
            return instance;
        }
    }

    // ---------------- Hitstop ----------------

    /// <summary>Congela el juego 'duration' segundos reales (0.05 s ≈ 3 cuadros).</summary>
    public static void HitStop(float duration)
    {
        if (duration <= 0f) return;
        Instance.StartHitStop(duration);
    }

    private void StartHitStop(float duration)
    {
        if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
        else timeScaleBeforeHitStop = Time.timeScale;
        hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = timeScaleBeforeHitStop;
        hitStopRoutine = null;
    }

    // ---------------- Sacudida de cámara ----------------

    /// <summary>Sacude la cámara principal con la intensidad (unidades) y duración indicadas.</summary>
    public static void Shake(float intensity, float duration)
    {
        if (Camera.main == null || intensity <= 0f) return;
        Instance.StartShake(intensity, duration);
    }

    private void StartShake(float intensity, float duration)
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            if (shakingCamera != null) shakingCamera.localPosition = cameraRestPosition;
        }
        shakingCamera = Camera.main.transform;
        cameraRestPosition = shakingCamera.localPosition;
        shakeRoutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float t = 0f;
        while (t < duration && shakingCamera != null)
        {
            float k = 1f - t / duration; // se apaga de a poco
            Vector2 offset = Random.insideUnitCircle * intensity * k;
            shakingCamera.localPosition = cameraRestPosition + new Vector3(offset.x, offset.y, 0f);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (shakingCamera != null) shakingCamera.localPosition = cameraRestPosition;
        shakeRoutine = null;
    }

    // ---------------- Números de daño ----------------

    /// <summary>Muestra un número que sube y se desvanece en 0.7 s.</summary>
    public static void DamageNumber(Vector3 worldPosition, int amount, Color color, float scale = 1f)
    {
        FloatingText(worldPosition, amount.ToString(), color, scale);
    }

    /// <summary>Muestra un texto cualquiera (por ejemplo "¡PARRY!") que sube y se desvanece en 0.7 s.</summary>
    public static void FloatingText(Vector3 worldPosition, string content, Color color, float scale = 1f)
    {
        Instance.StartCoroutine(Instance.FloatingTextRoutine(worldPosition, content, color, scale));
    }

    private IEnumerator FloatingTextRoutine(Vector3 position, string content, Color color, float scale)
    {
        var canvasGo = new GameObject("DamageNumber", typeof(RectTransform));
        canvasGo.transform.position = position + new Vector3(Random.Range(-0.4f, 0.4f), 0f, 0f);
        canvasGo.transform.localScale = Vector3.one * 0.03f * scale;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(400f, 60f);

        var textGo = new GameObject("Texto", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)textGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 44;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.text = content;
        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        const float life = 0.7f;
        float t = 0f;
        Vector3 start = canvasGo.transform.position;
        while (t < life)
        {
            float k = t / life;
            canvasGo.transform.position = start + new Vector3(0f, 1.6f * k, 0f);
            text.color = new Color(color.r, color.g, color.b, 1f - k * k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Destroy(canvasGo);
    }

    private void OnDestroy()
    {
        // Si se cambia de escena en medio de un hitstop, no dejar el juego congelado
        if (hitStopRoutine != null) Time.timeScale = timeScaleBeforeHitStop;
        if (instance == this) instance = null;
    }
}

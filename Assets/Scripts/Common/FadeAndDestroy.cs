using System.Collections;
using UnityEngine;

/// <summary>Desvanece y agranda levemente un sprite, y luego lo destruye.</summary>
public class FadeAndDestroy : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.18f;

    private IEnumerator Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        Color c = sr.color;
        Vector3 scale = transform.localScale;
        float t = 0f;
        while (t < lifetime)
        {
            float k = t / lifetime;
            sr.color = new Color(c.r, c.g, c.b, c.a * (1f - k));
            transform.localScale = scale * (1f + 0.25f * k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}

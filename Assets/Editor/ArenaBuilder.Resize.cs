using UnityEngine;

/// <summary>
/// Tamaño de las arenas 1 y 2 (son una misma escena base). Originalmente medían unas 64 u de ancho,
/// lo que dejaba al héroe muy pequeño en pantalla; se acercan las paredes y la cámara.
/// </summary>
public static partial class ArenaBuilder
{
    /// <summary>Mitad del ancho jugable de las arenas 1 y 2 (en unidades).</summary>
    private const float ClassicHalfWidth = 26f;
    private const float ClassicCameraSize = 14.8f;

    private static void ResizeClassicArena()
    {
        float edge = GetColliderEdge("Wall_Right", left: true, fallback: 32f);
        if (Mathf.Abs(edge - ClassicHalfWidth) < 0.05f) return;   // ya tiene el tamaño nuevo
        float k = ClassicHalfWidth / edge;

        PlaceWalls(ClassicHalfWidth);

        // Las plataformas se acercan al centro en la misma proporción
        foreach (Transform t in Object.FindObjectsByType<Transform>())
        {
            if (!t.name.StartsWith("Platform_")) continue;
            Vector3 p = t.position;
            t.position = new Vector3(p.x * k, p.y, p.z);
        }

        // Paredes de calaveras: un hijo a cada lado, el grupo queda en el borde derecho
        GameObject skulls = GameObject.Find("HazardSkullGroup");
        if (skulls != null)
        {
            skulls.transform.position = new Vector3(ClassicHalfWidth, skulls.transform.position.y, 0f);
            foreach (Transform child in skulls.transform)
            {
                Vector3 lp = child.localPosition;
                child.localPosition = new Vector3(lp.x < -1f ? -2f * ClassicHalfWidth : 0f, lp.y, lp.z);
            }
        }

        GameObject enemy = GameObject.Find("Enemy");
        if (enemy != null)
        {
            Vector3 p = enemy.transform.position;
            enemy.transform.position = new Vector3(Mathf.Min(p.x, ClassicHalfWidth - 3.5f), p.y, p.z);
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographicSize = ClassicCameraSize;
            cam.transform.position = new Vector3(0f, 5.2f, -10f);
        }
    }
}

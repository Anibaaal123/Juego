using UnityEngine;

/// <summary>
/// Animación por cuadros mínima para objetos simples (proyectiles, efectos) que no necesitan un Animator.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SimpleSpriteAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private bool loop = true;

    private SpriteRenderer spriteRenderer;
    private float timer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || framesPerSecond <= 0f) return;

        timer += Time.deltaTime;
        int index = Mathf.FloorToInt(timer * framesPerSecond);
        index = loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1);
        spriteRenderer.sprite = frames[index];
    }
}

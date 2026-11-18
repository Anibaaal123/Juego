using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pedestal de mejora. Al aparecer tira su rareza (60/30/10 %) y se tiñe en blanco, azul o dorado.
/// Cuando el jugador se para encima, le aplica la mejora y avisa al BossArenaManager.
/// </summary>
public class UpgradePedestal : MonoBehaviour
{
    [SerializeField] private SpriteRenderer orbRenderer;
    [SerializeField] private Text label;
    [SerializeField] private Vector2 pickupBoxSize = new Vector2(1.6f, 3.0f);
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobFrequency = 2.0f;

    public Upgrade Upgrade { get; private set; }

    private Action<UpgradePedestal> onTaken;
    private Vector3 orbBasePosition;
    private bool taken;
    private bool armed;
    private float spawnTime;

    public void Setup(Upgrade upgrade, SpriteRenderer orb, Text text, Action<UpgradePedestal> takenCallback)
    {
        Upgrade = upgrade;
        orbRenderer = orb;
        label = text;
        onTaken = takenCallback;

        if (orbRenderer != null)
        {
            orbRenderer.color = upgrade.RarityColor;
            orbBasePosition = orbRenderer.transform.localPosition;
        }
        if (label != null)
        {
            label.text = $"{upgrade.Title}\n<b>{upgrade.RarityName}</b>\n{upgrade.EffectText}";
            label.color = upgrade.RarityColor;
        }
        spawnTime = Time.time;
    }

    private void Update()
    {
        if (taken) return;

        if (orbRenderer != null)
        {
            float bob = Mathf.Sin((Time.time - spawnTime) * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            orbRenderer.transform.localPosition = orbBasePosition + new Vector3(0f, bob, 0f);
        }

        // Pequeña espera para que el jugador no lo tome sin querer justo al aparecer
        if (Time.time - spawnTime < 0.4f) return;

        PlayerRunStats stats = FindPlayerOnTop();

        // Si el pedestal apareció debajo del jugador, primero tiene que salir y volver a pisarlo
        if (!armed)
        {
            if (stats == null) armed = true;
            return;
        }

        if (stats != null)
        {
            taken = true;
            stats.ApplyUpgrade(Upgrade);
            onTaken?.Invoke(this);
            Destroy(gameObject);
        }
    }

    private PlayerRunStats FindPlayerOnTop()
    {
        Vector2 center = (Vector2)transform.position + new Vector2(0f, pickupBoxSize.y * 0.5f);
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, pickupBoxSize, 0f))
        {
            PlayerRunStats stats = hit.GetComponentInParent<PlayerRunStats>();
            if (stats != null) return stats;
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + new Vector3(0f, pickupBoxSize.y * 0.5f, 0f), pickupBoxSize);
    }
}

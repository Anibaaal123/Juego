using UnityEngine;

public class HazardFire : MonoBehaviour
{
    [Header("Hazard Damage")]
    [SerializeField] private int damagePerTick = 15;
    [SerializeField] private float damageInterval = 0.8f;

    private float nextDamageTime = 0f;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time >= nextDamageTime)
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damagePerTick);
                nextDamageTime = Time.time + damageInterval;
            }
        }
    }
}
using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Power-Up Settings")]
    [SerializeField] private int bonusDamage = 5;
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerAttack playerAttack = collision.GetComponent<PlayerAttack>();
            if (playerAttack != null)
            {
                playerAttack.ApplyDamageBuff(bonusDamage);
            }

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
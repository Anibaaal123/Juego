using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 150;
    [SerializeField] private Slider bossHealthSlider;

    [Header("Audio & Effects")]
    [SerializeField] private AudioClip bossDeathSound;
    [SerializeField] private float victoryRestartDelay = 3.5f;

    private int currentHealth;
    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = maxHealth;
            bossHealthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (bossHealthSlider != null)
        {
            bossHealthSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(DieRoutine());
        }
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;

        if (bossHealthSlider != null)
        {
            bossHealthSlider.gameObject.SetActive(false);
        }

        if (bossDeathSound != null)
        {
            AudioSource.PlayClipAtPoint(bossDeathSound, transform.position, 1.0f);
        }

        
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        GetComponent<EnemyAttack>().enabled = false;

        yield return new WaitForSeconds(victoryRestartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
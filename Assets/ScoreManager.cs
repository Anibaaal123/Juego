using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private int totalScore = 0;

    public int TotalScore => totalScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddScore(int amount)
    {
        totalScore += amount;
        Debug.Log($"[SCORE] Collectible gathered (+{amount}). Total Score: {totalScore}");
    }
}
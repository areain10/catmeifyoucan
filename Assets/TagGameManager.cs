using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TagGameManager : MonoBehaviour
{
    public static TagGameManager Instance;

    [Header("Timers (seconds)")]
    public float p1StartTime = 15f;
    public float p2StartTime = 15f;

    [Header("Optional UI")]
    public TextMeshProUGUI debugText;

    GameObject p1;
    GameObject p2;

    GameObject itPlayer;

    float p1Remaining;
    float p2Remaining;

    bool gameOver;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        p1 = GameObject.FindGameObjectWithTag("P1");
        p2 = GameObject.FindGameObjectWithTag("P2");

        p1Remaining = p1StartTime;
        p2Remaining = p2StartTime;

        
        itPlayer = Random.value > 0.5f ? p1 : p2;

        Debug.Log($"IT starts as {itPlayer.name}");
    }

    void Update()
    {
        if (gameOver) return;

       
        if (itPlayer == p1) p1Remaining -= Time.deltaTime;
        else if (itPlayer == p2) p2Remaining -= Time.deltaTime;

        if (debugText != null)
        {
            debugText.text =
                $"IT: {itPlayer.name}\n" +
                $"P1 IT Time Left: {p1Remaining:F1}\n" +
                $"P2 IT Time Left: {p2Remaining:F1}";
        }

        
        if (p1Remaining <= 0f) EndGame(winner: p1);
        else if (p2Remaining <= 0f) EndGame(winner: p2);
    }

    public bool IsIt(GameObject player) => player == itPlayer;

    
    public void SwapIt(GameObject newIt)
    {
        if (gameOver) return;
        if (newIt == null) return;
        if (newIt == itPlayer) return;

        itPlayer = newIt;
        Debug.Log($"IT swapped to {itPlayer.name}");
    }

    void EndGame(GameObject winner)
    {
        gameOver = true;
        Debug.Log($"WINNER: {winner.name} (their IT timer hit 0 first)");
        Time.timeScale = 0f; // freeze for demo
    }

    
    public float GetRemaining(GameObject player)
    {
        if (player == p1) return p1Remaining;
        if (player == p2) return p2Remaining;
        return 0f;
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TagGameManager : MonoBehaviour
{
    public static TagGameManager Instance;

    [Header("Round Settings")]
    public float roundTime = 15f;
    public int roundsToWin = 3;

    [Header("Tiny Upgrades")]
    public float tagGraceSeconds = 0.75f;
    public int countdownSeconds = 3;
    public float betweenRoundsPause = 2f;

    [Header("Spawn Points")]
    [Tooltip("Where P1 teleports at the start of each round.")]
    public Transform p1SpawnPoint;
    [Tooltip("Where P2 teleports at the start of each round.")]
    public Transform p2SpawnPoint;

    [Header("Tag Separation")]
    public float knockbackForce = 10f;
    public float speedBoostMultiplier = 1.5f;
    public float speedBoostDuration = 0.5f;

    [Header("Tag Camera Effect")]
    public float timerFreezeDuration = 1.5f;

    [Header("Optional UI")]
    public TextMeshProUGUI hudText;
    public TextMeshProUGUI centerText;
    public Slider p1TimerSlider;
    public Slider p2TimerSlider;

    [Header("Game Start")]
    public bool gameStarted = false; // waits for MenuManager to start the game

    GameObject p1;
    GameObject p2;

    float p1Timer;
    float p2Timer;

    int p1Rounds;
    int p2Rounds;

    GameObject itPlayer;

    bool roundActive;
    public bool matchOver;
    public GameObject Winner { get; private set; }
    bool timerFrozen;

    bool firstRound = true;
    GameObject lastIt;

    float nextTagAllowedTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        p1 = GameObject.FindGameObjectWithTag("P1");
        p2 = GameObject.FindGameObjectWithTag("P2");

        // Do NOT start the first round automatically
        // Wait until MenuManager calls BeginGame()
    }

    void Update()
    {
        if (!gameStarted) return; // wait for Start button
        if (!roundActive || matchOver) return;

        if (itPlayer == p1 && !timerFrozen) p1Timer -= Time.deltaTime;
        else if (itPlayer == p2 && !timerFrozen) p2Timer -= Time.deltaTime;

        UpdateHUD();

        if (p1Timer <= 0f) EndRound(winner: p1);
        else if (p2Timer <= 0f) EndRound(winner: p2);
    }

    public void BeginGame()
    {
        if (!gameStarted)
        {
            gameStarted = true;
            StartCoroutine(RoundFlow(starting: true));
        }
    }

    IEnumerator RoundFlow(bool starting)
    {
        p1Timer = roundTime;
        p2Timer = roundTime;

        if (p1TimerSlider != null) { p1TimerSlider.maxValue = roundTime; p1TimerSlider.value = roundTime; }
        if (p2TimerSlider != null) { p2TimerSlider.maxValue = roundTime; p2TimerSlider.value = roundTime; }

        // --- Teleport players to spawn points ---
        TeleportToSpawn(p1, p1SpawnPoint);
        TeleportToSpawn(p2, p2SpawnPoint);

        // --- Lock players in place ---
        SetPlayersLocked(true);

        // --- Pick IT ---
        if (firstRound)
        {
            itPlayer = Random.value > 0.5f ? p1 : p2;
            firstRound = false;
        }
        else
        {
            itPlayer = (lastIt == p1) ? p2 : p1;
        }
        lastIt = itPlayer;

        ApplyItVisuals();

        roundActive = false;
        nextTagAllowedTime = Time.time + 999f;

        // --- Countdown ---
        SoundManager.Play("Countdown");
        for (int i = countdownSeconds; i > 0; i--)
        {
            SetCenterText(i.ToString());
            yield return new WaitForSeconds(0.8f);
        }
        SetCenterText("GO!");
        SoundManager.Play("OST");
        yield return new WaitForSeconds(0.35f);
        SetCenterText("");

        // --- Unlock players ---
        SetPlayersLocked(false);

        roundActive = true;
        nextTagAllowedTime = Time.time;

        UpdateHUD();
    }

    void TeleportToSpawn(GameObject player, Transform spawnPoint)
    {
        if (player == null || spawnPoint == null) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.MovePosition(spawnPoint.position);
        }
        else
        {
            player.transform.position = spawnPoint.position;
        }

        player.transform.rotation = spawnPoint.rotation;
    }

    void SetPlayersLocked(bool locked)
    {
        SetLocked(p1, locked);
        SetLocked(p2, locked);
    }

    void SetLocked(GameObject player, bool locked)
    {
        if (player == null) return;
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.inputLocked = locked;
    }

    void EndRound(GameObject winner)
    {
        if (!roundActive || matchOver) return;

        roundActive = false;

        SetPlayersLocked(true);

        if (winner == p1) p1Rounds++;
        else if (winner == p2) p2Rounds++;

        SetCenterText($"{winner.name} wins the round!");
        Debug.Log($"Round won by {winner.name}");

        if (p1Rounds >= roundsToWin || p2Rounds >= roundsToWin)
        {
            EndMatch(winner);
            return;
        }

        StartCoroutine(NextRoundAfterPause());
    }

    IEnumerator NextRoundAfterPause()
    {
        nextTagAllowedTime = Time.time + 999f;

        yield return new WaitForSeconds(betweenRoundsPause);
        SetCenterText("");
        yield return StartCoroutine(RoundFlow(starting: false));
    }

    void EndMatch(GameObject winner)
    {
        matchOver = true;
        Winner = winner;

        SetPlayersLocked(true);
        SetCenterText($"{winner.name} wins the MATCH!");
        Debug.Log($"MATCH WINNER: {winner.name}");
        Time.timeScale = 0f;
    }

    public bool IsIt(GameObject player) => player == itPlayer;

    public bool CanTagNow()
    {
        return roundActive && !matchOver && Time.time >= nextTagAllowedTime;
    }

    public void SwapIt(GameObject newIt)
    {
        if (!roundActive || matchOver) return;
        if (Time.time < nextTagAllowedTime) return;
        if (newIt == null || newIt == itPlayer) return;

        GameObject oldIt = itPlayer;
        itPlayer = newIt;
        lastIt = itPlayer;

        if (TagCameraEffect.Instance != null)
            TagCameraEffect.Instance.PlayTagEffect(itPlayer.transform, this.GetComponent<TagGameManager>());
        SetCenterText("TAG!");
        SoundManager.Play("Click");
        StartCoroutine(FreezeTimerCoroutine());

        ApplyTagKnockback(oldIt, newIt);

        PlayerController newItController = newIt.GetComponent<PlayerController>();
        if (newItController != null)
            StartCoroutine(SpeedBoostCoroutine(newItController));

        nextTagAllowedTime = Time.time + tagGraceSeconds;

        ApplyItVisuals();
        Debug.Log($"IT swapped to {itPlayer.name} (grace {tagGraceSeconds:0.00}s)");
    }

    IEnumerator FreezeTimerCoroutine()
    {
        timerFrozen = true;
        yield return new WaitForSeconds(timerFreezeDuration);
        timerFrozen = false;
        SetCenterText("");
    }

    void ApplyTagKnockback(GameObject tagger, GameObject tagged)
    {
        Vector3 awayDir = (tagged.transform.position - tagger.transform.position);

        if (awayDir.sqrMagnitude < 0.001f)
            awayDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        awayDir = new Vector3(awayDir.x, 0f, awayDir.z).normalized;

        Rigidbody taggedRb = tagged.GetComponent<Rigidbody>();
        Rigidbody taggerRb = tagger.GetComponent<Rigidbody>();

        if (taggedRb != null)
            taggedRb.AddForce(awayDir * knockbackForce, ForceMode.Impulse);

        if (taggerRb != null)
            taggerRb.AddForce(-awayDir * knockbackForce * 0.5f, ForceMode.Impulse);
    }

    IEnumerator SpeedBoostCoroutine(PlayerController pc)
    {
        float originalSpeed = pc.moveSpeed;
        pc.moveSpeed *= speedBoostMultiplier;
        yield return new WaitForSeconds(speedBoostDuration);
        pc.moveSpeed = originalSpeed;
    }

    void UpdateHUD()
    {
        if (p1TimerSlider != null) p1TimerSlider.value = Mathf.Max(0f, p1Timer);
        if (p2TimerSlider != null) p2TimerSlider.value = Mathf.Max(0f, p2Timer);

        if (hudText == null) return;

        hudText.text =
            $"IT: {itPlayer.name}\n" +
            $"P1 Time: {Mathf.Max(0f, p1Timer):F1} | Rounds: {p1Rounds}\n" +
            $"P2 Time: {Mathf.Max(0f, p2Timer):F1} | Rounds: {p2Rounds}";
    }

    public void SetCenterText(string s)
    {
        if (centerText != null) centerText.text = s;
    }

    void ApplyItVisuals()
    {
        if (p1 != null)
        {
            var ind = p1.GetComponentInChildren<PlayerItIndicator>();
            if (ind != null) ind.SetIt(itPlayer == p1);
        }
        if (p2 != null)
        {
            var ind = p2.GetComponentInChildren<PlayerItIndicator>();
            if (ind != null) ind.SetIt(itPlayer == p2);
        }
    }
}
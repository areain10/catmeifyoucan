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
    public float tagGraceSeconds = 0.75f;   // no re-tag window after IT swap
    public int countdownSeconds = 3;        // 3..2..1..GO
    public float betweenRoundsPause = 1.25f;

    [Header("Tag Separation")]
    public float knockbackForce = 10f;      // impulse applied to both players on tag
    public float speedBoostMultiplier = 1.5f;   // speed boost for newly tagged player
    public float speedBoostDuration = 0.5f;     // how long the speed boost lasts

    [Header("Tag Camera Effect")]
    [Tooltip("How long the timer stays paused during the tag camera effect (should match zoomDuration + freezeDuration in TagCameraEffect)")]
    public float timerFreezeDuration = 1.5f;

    [Header("Optional UI")]
    public TextMeshProUGUI hudText;        // shows timers/rounds/IT
    public TextMeshProUGUI centerText;     // countdown + GO + round winner
    public Slider p1TimerSlider;           // slider max should be set to roundTime
    public Slider p2TimerSlider;

    GameObject p1;
    GameObject p2;

    float p1Timer;
    float p2Timer;

    int p1Rounds;
    int p2Rounds;

    GameObject itPlayer;

    bool roundActive;
    bool matchOver;
    bool timerFrozen;

    // IT selection: first round random, then alternate
    bool firstRound = true;
    GameObject lastIt;

    // tag grace
    float nextTagAllowedTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        p1 = GameObject.FindGameObjectWithTag("P1");
        p2 = GameObject.FindGameObjectWithTag("P2");

        StartCoroutine(RoundFlow(starting: true));
    }

    void Update()
    {
        if (!roundActive || matchOver) return;


        if (itPlayer == p1 && !timerFrozen) p1Timer -= Time.deltaTime;
        else if (itPlayer == p2 && !timerFrozen) p2Timer -= Time.deltaTime;

        UpdateHUD();

        if (p1Timer <= 0f) EndRound(winner: p1);
        else if (p2Timer <= 0f) EndRound(winner: p2);
    }


    IEnumerator RoundFlow(bool starting)
    {

        p1Timer = roundTime;
        p2Timer = roundTime;

        if (p1TimerSlider != null) { p1TimerSlider.maxValue = roundTime; p1TimerSlider.value = roundTime; }
        if (p2TimerSlider != null) { p2TimerSlider.maxValue = roundTime; p2TimerSlider.value = roundTime; }


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


        for (int i = countdownSeconds; i > 0; i--)
        {
            SetCenterText(i.ToString());
            yield return new WaitForSeconds(1f);
        }
        SetCenterText("GO!");
        yield return new WaitForSeconds(0.35f);
        SetCenterText("");


        roundActive = true;
        nextTagAllowedTime = Time.time;

        UpdateHUD();
    }

    void EndRound(GameObject winner)
    {
        if (!roundActive || matchOver) return;

        roundActive = false;

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

        // --- Camera effect: freeze, zoom, shake ---
        if (TagCameraEffect.Instance != null)
            TagCameraEffect.Instance.PlayTagEffect(itPlayer.transform,this.GetComponent<TagGameManager>());
        SetCenterText("TAG!");
        StartCoroutine(FreezeTimerCoroutine());

        // --- Knockback: push both players away from each other ---
        ApplyTagKnockback(oldIt, newIt);

        // --- Speed boost for the newly tagged player ---
        PlayerController newItController = newIt.GetComponent<PlayerController>();
        if (newItController != null)
            StartCoroutine(SpeedBoostCoroutine(newItController));

        // grace period + phase-through handled in PlayerTagHitbox via nextTagAllowedTime
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

        // If they're somehow exactly overlapping, pick a random horizontal direction
        if (awayDir.sqrMagnitude < 0.001f)
            awayDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        awayDir = new Vector3(awayDir.x, 0f, awayDir.z).normalized;

        Rigidbody taggedRb = tagged.GetComponent<Rigidbody>();
        Rigidbody taggerRb = tagger.GetComponent<Rigidbody>();

        if (taggedRb != null)
            taggedRb.AddForce(awayDir * knockbackForce, ForceMode.Impulse);

        if (taggerRb != null)
            taggerRb.AddForce(-awayDir * knockbackForce * 0.5f, ForceMode.Impulse); // tagger gets a lighter pushback
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
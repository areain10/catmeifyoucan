using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PowerupManager : MonoBehaviour
{
    public static PowerupManager Instance;

    [Header("Spawnpoints")]
    [Tooltip("Drag in all possible spawnpoint Transforms. Blocks will appear at random ones.")]
    [SerializeField]
    public Transform[] spawnPoints;

    [Header("Spawn Timing")]
    [Tooltip("Minimum seconds between a new block spawning")]
    public float spawnIntervalMin = 4f;
    [Tooltip("Maximum seconds between a new block spawning")]
    public float spawnIntervalMax = 10f;

    [Header("Block Limits")]
    [Tooltip("Max number of lucky blocks alive in the world at once")]
    public int maxActiveBlocks = 3;

    [Header("Lucky Block")]
    [Tooltip("Prefab with a LuckyBlock component on it")]
    public GameObject luckyBlockPrefab;

    [Header("Powerups")]
    [Tooltip("Add a HairballTrapDefinition ScriptableObject here (and any future powerups)")]
    public PowerupDefinition[] availablePowerups;

    readonly List<LuckyBlock> activeBlocks = new List<LuckyBlock>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }



    IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(wait);

            // Clean up any blocks that got destroyed (collected)
            activeBlocks.RemoveAll(b => b == null);

            if (activeBlocks.Count >= maxActiveBlocks) continue;
            if (spawnPoints == null || spawnPoints.Length == 0) continue;

            // Pick a random spawnpoint that doesn't already have a block on it
            List<Transform> available = GetAvailableSpawnPoints();
            if (available.Count == 0) continue;

            Transform spawnAt = available[Random.Range(0, available.Count)];
            SpawnBlock(spawnAt.position);
        }
    }

    void SpawnBlock(Vector3 position)
    {
        if (luckyBlockPrefab == null)
        {
            Debug.LogWarning("PowerupManager: luckyBlockPrefab not assigned!");
            return;
        }

        GameObject obj = Instantiate(luckyBlockPrefab, position, Quaternion.identity);
        LuckyBlock block = obj.GetComponent<LuckyBlock>();

        if (block != null)
            activeBlocks.Add(block);

        Debug.Log($"Lucky block spawned at {position}");
    }

    List<Transform> GetAvailableSpawnPoints()
    {
        List<Transform> available = new List<Transform>();

        foreach (Transform sp in spawnPoints)
        {
            if (sp == null) continue;

            bool occupied = false;
            foreach (LuckyBlock block in activeBlocks)
            {
                if (block != null && Vector3.Distance(block.transform.position, sp.position) < 1f)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied) available.Add(sp);
        }

        return available;
    }

  

    public void AssignRandomPowerup(PlayerPowerupHolder holder)
    {
        if (availablePowerups == null || availablePowerups.Length == 0)
        {
            Debug.LogWarning("PowerupManager: no powerups defined!");
            return;
        }

        PowerupDefinition def = availablePowerups[Random.Range(0, availablePowerups.Length)];
        IPowerup powerup = def.CreateInstance();
        holder.GivePowerup(powerup);

        Debug.Log($"{holder.gameObject.name} received powerup: {def.name}");
    }

  
    public void NotifyBlockCollected(LuckyBlock block)
    {
        activeBlocks.Remove(block);
    }
}
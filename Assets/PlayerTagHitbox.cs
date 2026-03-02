using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerTagHitbox : MonoBehaviour
{
    [Header("Phase-Through")]
    [Tooltip("How long (seconds) the two players phase through each other after a tag. Should match or be slightly less than tagGraceSeconds in TagGameManager.")]
    public float phaseDuration = 0.6f;

    // Cached layer indices
    int playersLayer;
    int noCollideLayer; // a layer that doesn't collide with Players — see setup notes below

    Coroutine phaseRoutine;

    void Awake()
    {
        playersLayer = LayerMask.NameToLayer("Players");

        // "PlayerGhost" is a layer you create in Unity that has NO collision with "Players"
        // (set this in Edit > Project Settings > Physics > Layer Collision Matrix)
        noCollideLayer = LayerMask.NameToLayer("PlayerGhost");

        if (noCollideLayer == -1)
            Debug.LogWarning("PlayerTagHitbox: 'PlayerGhost' layer not found! Phase-through won't work. " +
                             "Create it in Edit > Project Settings > Tags and Layers, then uncheck its " +
                             "collision with 'Players' in the Physics Layer Collision Matrix.");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (TagGameManager.Instance == null) return;

        if (collision.gameObject.layer != playersLayer)
            return;

        if (!TagGameManager.Instance.CanTagNow())
            return;

        if (TagGameManager.Instance.IsIt(gameObject))
        {
            TagGameManager.Instance.SwapIt(collision.gameObject);

            // Start phase-through on BOTH players so they can separate freely
            StartPhaseThrough(gameObject);
            StartPhaseThrough(collision.gameObject);
        }
    }

    void StartPhaseThrough(GameObject player)
    {
        PlayerTagHitbox hitbox = player.GetComponent<PlayerTagHitbox>();
        if (hitbox == null) hitbox = player.GetComponentInChildren<PlayerTagHitbox>();

        if (hitbox != null)
        {
            if (hitbox.phaseRoutine != null)
                hitbox.StopCoroutine(hitbox.phaseRoutine);
            hitbox.phaseRoutine = hitbox.StartCoroutine(hitbox.PhaseThroughRoutine());
        }
    }

    IEnumerator PhaseThroughRoutine()
    {
        if (noCollideLayer == -1) yield break; // layer not set up, skip

        int originalLayer = gameObject.layer;
        gameObject.layer = noCollideLayer;

        yield return new WaitForSeconds(phaseDuration);

        gameObject.layer = originalLayer;
        phaseRoutine = null;
    }
}
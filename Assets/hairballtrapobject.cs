using System.Collections;
using UnityEngine;

public class HairballTrapObject : MonoBehaviour
{
    HairballTrapDefinition def;
    GameObject owner;

    [Header("Visuals")]
    public ParticleSystem tripParticles;
    public AudioSource tripSound;

    [Header("Lifetime")]
    public float maxLifetime = 20f;

    bool triggered = false;

    public void Init(HairballTrapDefinition definition, GameObject ownerPlayer)
    {
        def = definition;
        owner = ownerPlayer;
        Destroy(gameObject, maxLifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.gameObject == owner) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Players")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        Debug.Log("ISTRIPPED");
        triggered = true;
        pc.StartCoroutine(TripCoroutine(pc, other.gameObject));

        if (tripParticles != null)
        {
            tripParticles.transform.parent = null;
            tripParticles.Play();
        }
        if (tripSound != null)
        {
            tripSound.transform.parent = null;
            tripSound.Play();
        }

        Destroy(gameObject);
    }

    IEnumerator TripCoroutine(PlayerController pc, GameObject victim)
    {
        float originalSpeed = pc.moveSpeed;
        float originalAccel = pc.acceleration;
        bool originalDash = pc.enableDash;

        pc.moveSpeed = originalSpeed * def.tripSpeedMultiplier;
        pc.acceleration = 0f;
        pc.enableDash = false;

        Rigidbody rb = victim.GetComponent<Rigidbody>();
        bool victimIsIt = TagGameManager.Instance != null && TagGameManager.Instance.IsIt(victim);
        if (victimIsIt && rb != null)
        {
            Vector3 skidDir = Vector3.Cross(victim.transform.forward, Vector3.up).normalized;
            if (Random.value > 0.5f) skidDir = -skidDir;
            rb.AddForce(skidDir * 5f, ForceMode.Impulse);
        }

        // Cache original rotation before spinning
        Transform visual = pc.visualRoot != null ? pc.visualRoot : pc.transform;
        Quaternion originalRotation = visual.rotation;

        float elapsed = 0f;
        while (elapsed < def.tripDuration)
        {
            elapsed += Time.deltaTime;
            visual.Rotate(Vector3.up, def.spinSpeed * Time.deltaTime);
            yield return null;
        }

        // Restore original rotation exactly
        visual.rotation = originalRotation;

        pc.moveSpeed = originalSpeed;
        pc.acceleration = originalAccel;
        pc.enableDash = originalDash;

        Debug.Log($"{victim.name} recovered from hairball trip.");
    }
}
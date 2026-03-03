using System.Collections;
using UnityEngine;

public class LuckyBlock : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject blockVisual;         
    public ParticleSystem collectParticles; 

    [Header("Bounce Animation")]
    public bool bounceIdle = true;
    public float bounceHeight = 0.15f;
    public float bounceSpeed = 2.2f;

    bool collected = false;
    Vector3 startPos;

    void Awake()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (collected) return;

        if (bounceIdle)
        {
            float y = startPos.y + Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            transform.position = new Vector3(startPos.x, y, startPos.z);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Players")) return;

        PlayerPowerupHolder holder = other.GetComponent<PlayerPowerupHolder>();
        if (holder == null) holder = other.GetComponentInParent<PlayerPowerupHolder>();
        if (holder == null) return;


        if (holder.HasPowerup()) return;

        Collect(holder);
    }

    void Collect(PlayerPowerupHolder holder)
    {
        collected = true;

        PowerupManager.Instance.AssignRandomPowerup(holder);
        PowerupManager.Instance.NotifyBlockCollected(this);

        if (collectParticles != null)
        {
            collectParticles.transform.parent = null;
            collectParticles.Play();
        }

        if (blockVisual != null) blockVisual.SetActive(false);

      
        Destroy(gameObject, 0.1f);
    }
}
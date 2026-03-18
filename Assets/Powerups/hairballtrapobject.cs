using System.Collections;
using UnityEngine;

public class HairballTrapObject : MonoBehaviour
{
    HairballTrapDefinition def;
    GameObject owner;


    [Header("Lifetime")]
    public float maxLifetime = 20f;

    bool triggered = false;

    public Animator[] animators;

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
        
        Destroy(gameObject);
    }

    IEnumerator TripCoroutine(PlayerController pc, GameObject victim)
    {
        Rigidbody rb = victim.GetComponent<Rigidbody>();
        if (rb == null) rb = victim.GetComponentInParent<Rigidbody>();

        if (animators == null || animators.Length == 0)
        {
            animators = victim.GetComponentsInChildren<Animator>();
        }
            
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true; 
        }

        pc.stuck = true;

        SoundManager.Play("Meow");
        if (animators != null)
        {
            foreach (Animator anim in animators)
            {
                anim.SetBool("Yarn", true);
            }
        }

        yield return new WaitForSeconds(2f);

        if (animators != null)
        {
            foreach (Animator anim in animators)
            {
                anim.SetBool("Yarn", false);
            }
        }

        if (rb != null)
        {
            rb.isKinematic = false;
        }
            
        pc.stuck = false;
    }
}
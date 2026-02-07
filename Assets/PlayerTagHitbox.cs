using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerTagHitbox : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (TagGameManager.Instance == null) return;

        
        if (collision.gameObject.layer != LayerMask.NameToLayer("Players"))
            return;

        
        if (TagGameManager.Instance.IsIt(gameObject))
        {
            TagGameManager.Instance.SwapIt(collision.gameObject);
        }
    }
}

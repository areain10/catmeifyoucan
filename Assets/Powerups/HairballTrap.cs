using System.Collections;
using UnityEngine;


[CreateAssetMenu(menuName = "Powerups/Hairball Trap")]
public class HairballTrapDefinition : PowerupDefinition
{
    [Header("Trap Settings")]
    public GameObject hairballPrefab;   // the world object dropped

    [Header("Trip Settings")]
    public float tripDuration = .2f;       // how long the victim is stunned

    public override IPowerup CreateInstance()
    {
        return new HairballTrapPowerup(this);
    }
}



public class HairballTrapPowerup : IPowerup
{
    HairballTrapDefinition def;

    public Sprite Icon => def.icon;

    public HairballTrapPowerup(HairballTrapDefinition definition)
    {
        def = definition;
    }

    public void Activate(GameObject user)
    {
        if (def.hairballPrefab == null)
        {
            Debug.LogWarning("HairballTrap: no prefab assigned on HairballTrapDefinition!");
            return;
        }

        // Drop the hairball just behind the player based on their facing direction
        Vector3 behind = user.transform.position - user.transform.forward * 0.8f;
        behind.y = user.transform.position.y;

        GameObject trap = GameObject.Instantiate(def.hairballPrefab, behind, Quaternion.identity);

        HairballTrapObject trapObj = trap.GetComponent<HairballTrapObject>();
        if (trapObj != null)
        {
            trapObj.Init(def, user);  // pass owner so they don't trip on their own hairball
        }

        Debug.Log($"{user.name} dropped a hairball!");
    }
}


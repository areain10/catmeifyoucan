using UnityEngine;


public abstract class PowerupDefinition : ScriptableObject
{
    public Sprite icon;
    public abstract IPowerup CreateInstance();
}
using System.Collections;
using UnityEngine;



[CreateAssetMenu(menuName = "Powerups/Rocket Dash")]
public class RocketDashDefinition : PowerupDefinition
{
    [Header("Rocket Dash Settings")]
    [Tooltip("How fast the rocket launch sends the player.")]
    public float launchSpeed = 28f;

    [Tooltip("How long the rocket launch lasts (seconds).")]
    public float launchDuration = 0.22f;

    [Tooltip("Brief window after launch where the player keeps extra speed (bleed-off).")]
    public float bleedOffDuration = 0.15f;

    [Tooltip("Speed multiplier during bleed-off (1 = instant stop, 0.5 = half speed).")]
    public float bleedOffMultiplier = 0.5f;

    public override IPowerup CreateInstance() => new RocketDashPowerup(this);
}

public class RocketDashPowerup : IPowerup
{
    readonly RocketDashDefinition def;
    public Sprite Icon => def.icon;

    public RocketDashPowerup(RocketDashDefinition definition) { def = definition; }

    public void Activate(GameObject user)
    {
        PlayerController pc = user.GetComponent<PlayerController>();
        if (pc == null) return;

        // Use current input direction → fall back to last facing → fall back to forward
        Rigidbody rb = user.GetComponent<Rigidbody>();
        Vector3 dir = rb != null && rb.velocity.sqrMagnitude > 0.01f
            ? new Vector3(rb.velocity.x, 0f, rb.velocity.z).normalized
            : user.transform.forward;

        pc.StartCoroutine(RocketCoroutine(pc, rb, dir));
        Debug.Log($"{user.name} fired Rocket Dash!");
        SoundManager.Play("RocketDash", user.transform.position);
    }

    IEnumerator RocketCoroutine(PlayerController pc, Rigidbody rb, Vector3 dir)
    {
        // Temporarily disable normal movement control
        float origSpeed = pc.moveSpeed;
        float origAccel = pc.acceleration;
        float origDecel = pc.deceleration;
        bool origDash = pc.enableDash;

        pc.moveSpeed = 0f;
        pc.acceleration = 0f;
        pc.deceleration = 0f;
        pc.enableDash = false;

        // ---- Launch phase ----
        float elapsed = 0f;
        while (elapsed < def.launchDuration)
        {
            elapsed += Time.deltaTime;
            if (rb != null) rb.velocity = dir * def.launchSpeed;
            yield return null;
        }

        // ---- Bleed-off phase: smoothly slow down ----
        elapsed = 0f;
        float startSpeed = def.launchSpeed;
        while (elapsed < def.bleedOffDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / def.bleedOffDuration;
            float speed = Mathf.Lerp(startSpeed * def.bleedOffMultiplier, origSpeed, t);
            if (rb != null) rb.velocity = dir * speed;
            yield return null;
        }

        // ---- Restore ----
        pc.moveSpeed = origSpeed;
        pc.acceleration = origAccel;
        pc.deceleration = origDecel;
        pc.enableDash = origDash;
    }
}
using System.Collections;
using UnityEngine;



[CreateAssetMenu(menuName = "Powerups/Ghost")]
public class GhostDefinition : PowerupDefinition
{
    [Header("Ghost Settings")]
    [Tooltip("How long the player can phase through obstacles (seconds).")]
    public float duration = 3f;

    [Tooltip("Speed multiplier while ghosting (slightly faster feels great).")]
    public float speedMultiplier = 1.25f;

    [Tooltip("Tag on inner obstacle colliders the player phases through.")]
    public string obstacleTag = "Obstacle";

    [Tooltip("Tag on outer boundary colliders that always block the player.")]
    public string boundaryTag = "Boundary";

    [Tooltip("Visual tint/alpha applied to the player while ghosting (optional).")]
    public Color ghostTint = new Color(0.5f, 0.8f, 1f, 0.5f);

    public override IPowerup CreateInstance() => new GhostPowerup(this);
}

public class GhostPowerup : IPowerup
{
    readonly GhostDefinition def;
    public Sprite Icon => def.icon;

    public GhostPowerup(GhostDefinition definition) { def = definition; }

    public void Activate(GameObject user)
    {
        PlayerController pc = user.GetComponent<PlayerController>();
        if (pc == null) return;

        pc.StartCoroutine(GhostCoroutine(pc, user));
        Debug.Log($"{user.name} activated Ghost!");
        SoundManager.Play("Ghost", user.transform.position);
    }

    IEnumerator GhostCoroutine(PlayerController pc, GameObject user)
    {
        Collider playerCol = user.GetComponent<Collider>();
        if (playerCol == null) playerCol = user.GetComponentInChildren<Collider>();

        // ---- Cache all obstacle colliders and disable collision with player ----
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag(def.obstacleTag);
        foreach (var obs in obstacles)
        {
            Collider obsCol = obs.GetComponent<Collider>();
            if (obsCol != null && playerCol != null)
                Physics.IgnoreCollision(playerCol, obsCol, true);
        }

        // ---- Visual ghost tint ----
        Renderer[] renderers = user.GetComponentsInChildren<Renderer>();
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
            {
                originalColors[i] = renderers[i].material.color;
                renderers[i].material.color = def.ghostTint;
            }
        }

        // ---- Speed boost ----
        float origSpeed = pc.moveSpeed;
        pc.moveSpeed = origSpeed * def.speedMultiplier;

        // ---- Wait for duration ----
        yield return new WaitForSeconds(def.duration);

        // ---- Before restoring collision, make sure we're not stuck ----
        EjectFromObstacles(user, playerCol, obstacles);

        // ---- Restore collision ----
        foreach (var obs in obstacles)
        {
            Collider obsCol = obs.GetComponent<Collider>();
            if (obsCol != null && playerCol != null)
                Physics.IgnoreCollision(playerCol, obsCol, false);
        }

        // ---- Restore visuals ----
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }

        // ---- Restore speed ----
        pc.moveSpeed = origSpeed;

        Debug.Log($"{user.name} Ghost effect ended.");
    }

    /// <summary>
    /// Checks if the player is currently overlapping any obstacle.
    /// If so, uses the closest point on the obstacle's surface to push
    /// the player out before re-enabling collision.
    /// Boundary colliders are never phased through, so we never need
    /// to eject from those.
    /// </summary>
    void EjectFromObstacles(GameObject user, Collider playerCol, GameObject[] obstacles)
    {
        if (playerCol == null) return;

        // We do up to 5 iterations in case the player is wedged between multiple obstacles
        for (int iter = 0; iter < 5; iter++)
        {
            bool overlapping = false;

            foreach (var obs in obstacles)
            {
                if (obs == null) continue;
                Collider obsCol = obs.GetComponent<Collider>();
                if (obsCol == null) continue;

                // ComputePenetration tells us the exact overlap and push direction
                if (Physics.ComputePenetration(
                        playerCol, user.transform.position, user.transform.rotation,
                        obsCol, obs.transform.position, obs.transform.rotation,
                        out Vector3 dir, out float dist))
                {
                    // Push the player out along the separation direction
                    user.transform.position += dir * (dist + 0.02f); // tiny extra margin
                    overlapping = true;
                }
            }

            // Also make sure we didn't get pushed outside the boundary.
            // We can't use Physics.ComputePenetration to stay INSIDE a collider,
            // so instead we do a quick boundary check: find the closest point
            // on each boundary collider surface and if we're outside it, pull back.
            GameObject[] boundaries = GameObject.FindGameObjectsWithTag(def.boundaryTag);
            foreach (var boundary in boundaries)
            {
                if (boundary == null) continue;
                Collider bCol = boundary.GetComponent<Collider>();
                if (bCol == null) continue;

                // ClosestPoint returns the nearest point ON the collider surface.
                // If the player is INSIDE the boundary collider, closest point == player pos.
                // If outside, closest point is on the surface — snap back to it.
                Vector3 closest = bCol.ClosestPoint(user.transform.position);
                float distToBoundary = Vector3.Distance(closest, user.transform.position);

                // If the player is more than a tiny epsilon outside the boundary collider,
                // pull them back to the surface.
                if (!IsInsideCollider(bCol, user.transform.position))
                {
                    user.transform.position = closest + (user.transform.position - closest).normalized * -0.05f;
                }
            }

            if (!overlapping) break;
        }

        // Final velocity zero-out to prevent tunnelling momentum
        Rigidbody rb = user.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 vel = rb.velocity;
            rb.velocity = new Vector3(vel.x * 0.3f, 0f, vel.z * 0.3f);
        }
    }

    /// <summary>
    /// Returns true if the world point is inside the given collider.
    /// Uses ClosestPoint: if the point is inside, ClosestPoint returns the same point.
    /// </summary>
    bool IsInsideCollider(Collider col, Vector3 point)
    {
        Vector3 closest = col.ClosestPoint(point);
        return Vector3.Distance(closest, point) < 0.001f;
    }
}
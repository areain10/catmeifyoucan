using UnityEngine;

/// <summary>
/// Attach to each player root GameObject.
/// Stores the current powerup and handles the use input.
/// </summary>
public class PlayerPowerupHolder : MonoBehaviour
{
    [Header("Use Key")]
    public KeyCode useKeyP1 = KeyCode.Z;
    public KeyCode useKeyP2 = KeyCode.Slash;

    [Header("UI (optional)")]
    public GameObject powerupIconRoot;  // parent object shown when holding a powerup
    public UnityEngine.UI.Image powerupIcon; // icon image swapped per powerup

    [Range(1, 2)] public int playerIndex = 1;

    IPowerup currentPowerup;

    void Update()
    {
        if (currentPowerup == null) return;

        KeyCode useKey = playerIndex == 1 ? useKeyP1 : useKeyP2;
        if (Input.GetKeyDown(useKey))
            UsePowerup();
    }

    public void GivePowerup(IPowerup powerup)
    {
        currentPowerup = powerup;
        RefreshIcon();
    }

    public bool HasPowerup() => currentPowerup != null;

    void UsePowerup()
    {
        currentPowerup.Activate(gameObject);
        currentPowerup = null;
        RefreshIcon();
    }

    /// <summary>Called externally (e.g. round reset) to strip powerups.</summary>
    public void ClearPowerup()
    {
        currentPowerup = null;
        RefreshIcon();
    }

    void RefreshIcon()
    {
        if (powerupIconRoot != null)
            powerupIconRoot.SetActive(currentPowerup != null);

        if (powerupIcon != null && currentPowerup != null)
            powerupIcon.sprite = currentPowerup.Icon;
    }
}

/// <summary>
/// Interface every powerup must implement.
/// </summary>
public interface IPowerup
{
    Sprite Icon { get; }
    void Activate(GameObject user);
}

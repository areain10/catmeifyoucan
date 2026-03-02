using UnityEngine;

public class PlayerItIndicator : MonoBehaviour
{
    [Header("IT Icon")]
    public GameObject itIcon;

    void Awake()
    {
        SetIt(false);
    }

    public void SetIt(bool isIt)
    {
        if (itIcon != null) itIcon.SetActive(isIt);
    }
}
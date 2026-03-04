using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Camera))]
public class TagCameraFollow : MonoBehaviour
{
    
    [Header("Targets")]
    [Tooltip("If empty the script auto-finds every active PlayerController.")]
    public List<Transform> targets = new List<Transform>();

    [Header("Follow")]
    [Tooltip("How quickly the camera pans toward the midpoint (higher = snappier).")]
    public float positionSmoothSpeed = 4f;

    [Tooltip("Fixed Y (height) the camera stays at. Leave as 0 to use scene default.")]
    public float cameraHeight = 0f;

    [Header("Zoom / FOV")]
    [Tooltip("Extra world-unit padding added around the players so they're not right on the edge.")]
    public float padding = 3f;

    [Tooltip("Minimum orthographic size (ortho) or minimum FOV (perspective). " +
             "Prevents zooming in too close when players are near each other.")]
    public float minSize = 5f;

    [Tooltip("Maximum orthographic size (ortho) or maximum FOV (perspective). " +
             "Prevents zooming out too far when players are far apart.")]
    public float maxSize = 20f;

    [Tooltip("How smoothly the zoom changes (higher = snappier).")]
    public float zoomSmoothSpeed = 3f;

    [Header("Perspective Camera")]
    [Tooltip("If your camera is Perspective, tick this. " +
             "The script will adjust FOV to keep both players in frame.")]
    public bool isPerspective = false;

    [Tooltip("(Perspective only) Distance from the camera to the XZ play plane. " +
             "Should match the Y offset between camera and the arena floor.")]
    public float perspectiveHeight = 15f;


    Camera cam;
    bool effectRunning = false;   


    Vector3 desiredPosition;
    float desiredSize;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // Seed desired values so we don't snap on frame 1
        desiredPosition = transform.position;
        desiredSize = isPerspective ? cam.fieldOfView : cam.orthographicSize;

        if (cameraHeight == 0f)
            cameraHeight = transform.position.y;
    }

    void LateUpdate()
    {
        // While a tag-effect coroutine is playing, back off so we don't fight it.
        if (effectRunning) return;

        RefreshTargets();
        if (targets.Count == 0) return;

        // --- 1. Compute bounding box of all players (XZ plane only) ---
        Vector3 min = new Vector3(float.MaxValue, 0f, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, 0f, float.MinValue);

        foreach (Transform t in targets)
        {
            if (t == null) continue;
            if (t.position.x < min.x) min.x = t.position.x;
            if (t.position.x > max.x) max.x = t.position.x;
            if (t.position.z < min.z) min.z = t.position.z;
            if (t.position.z > max.z) max.z = t.position.z;
        }

        Vector3 center = new Vector3((min.x + max.x) * 0.5f, cameraHeight, (min.z + max.z) * 0.5f);

        // --- 2. Required size to fit all players ---
        float requiredSize = ComputeRequiredSize(min, max, center);
        requiredSize = Mathf.Clamp(requiredSize, minSize, maxSize);

        // --- 3. Smooth toward desired values ---
        float dt = Time.deltaTime;
        desiredPosition = center;
        desiredSize = requiredSize;

        Vector3 newPos = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed * dt);
        newPos.y = cameraHeight;
        transform.position = newPos;

        if (isPerspective)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, desiredSize, zoomSmoothSpeed * dt);
        }
        else
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, desiredSize, zoomSmoothSpeed * dt);
        }
    }



    float ComputeRequiredSize(Vector3 min, Vector3 max, Vector3 center)
    {
        float halfW = (max.x - min.x) * 0.5f + padding;
        float halfH = (max.z - min.z) * 0.5f + padding;

        if (isPerspective)
        {

            float halfExtent = Mathf.Max(halfH, halfW / cam.aspect);
            float fovRad = 2f * Mathf.Atan(halfExtent / perspectiveHeight);
            return Mathf.Rad2Deg * fovRad;
        }
        else
        {
  
            float sizeForHeight = halfH;
            float sizeForWidth = halfW / cam.aspect;
            return Mathf.Max(sizeForHeight, sizeForWidth);
        }
    }


    void RefreshTargets()
    {
        // If the inspector list is populated, trust it.
        if (targets.Count > 0)
        {
            targets.RemoveAll(t => t == null);
            return;
        }

        // Otherwise auto-find every active PlayerController.
        targets.Clear();
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (pc.gameObject.activeInHierarchy)
                targets.Add(pc.transform);
        }
    }



 
    public void SetEffectActive(bool active)
    {
        effectRunning = active;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
 
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(padding * 2f, 0.1f, padding * 2f));
    }
#endif
}
using System.Collections;
using UnityEngine;

public class TagCameraEffect : MonoBehaviour
{
    public static TagCameraEffect Instance;

    [Header("Time Freeze")]
    [Tooltip("How long the game freezes on impact (seconds, real-time)")]
    public float freezeDuration = 0.25f;

    [Header("Zoom")]
    [Tooltip("How close the camera zooms toward the new IT player")]
    public float zoomDistance = 5f;
    [Tooltip("How quickly the camera zooms in")]
    public float zoomInSpeed = 12f;
    [Tooltip("How quickly the camera zooms back out")]
    public float zoomOutSpeed = 5f;
    [Tooltip("Total time the zoom effect lasts (real-time seconds)")]
    public float zoomDuration = 1.2f;

    [Header("Shake")]
    public float shakeMagnitude = 0.18f;
    public float shakeDuration = 0.35f;

    // Internal
    Camera cam;
    Vector3 defaultLocalPos;
    float defaultFOV;

    Transform zoomTarget;
    Vector3 priorCamPos;
    float priorFOV;

    Coroutine activeEffect;

    // Reference to the follow cam on the same GameObject (optional)
    TagCameraFollow followCam;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        defaultLocalPos = transform.localPosition;
        defaultFOV = cam.fieldOfView;

        followCam = GetComponent<TagCameraFollow>();
    }

    /// <summary>Call this when a tag happens.</summary>
    public void PlayTagEffect(Transform newItTransform, TagGameManager owner)
    {
        if (activeEffect != null)
            StopCoroutine(activeEffect);

        activeEffect = StartCoroutine(TagEffectRoutine(newItTransform, owner));
    }

    IEnumerator TagEffectRoutine(Transform target, TagGameManager owner)
    {
        // Pause the follow cam so we can do our cinematic freely
        if (followCam != null) followCam.SetEffectActive(true);

        owner.SetCenterText("TAG!");
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(freezeDuration);
        Time.timeScale = 1f;

        Coroutine shake = StartCoroutine(ShakeRoutine());
        yield return StartCoroutine(ZoomRoutine(target));
        owner.SetCenterText("");
        yield return shake;

        activeEffect = null;

        // Resume the follow cam
        if (followCam != null) followCam.SetEffectActive(false);
    }

    IEnumerator ZoomRoutine(Transform target)
    {
        if (target == null) yield break;

        Vector3 startPos = transform.position;
        float startFOV = cam.fieldOfView;

        Vector3 dirToTarget = (target.position - transform.position).normalized;
        Vector3 zoomedPos = transform.position + dirToTarget * zoomDistance;
        float zoomedFOV = startFOV - 15f;

        float elapsed = 0f;
        float halfTime = zoomDuration * 0.4f;

        // Zoom IN
        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / halfTime);
            transform.position = Vector3.Lerp(startPos, zoomedPos, t);
            cam.fieldOfView = Mathf.Lerp(startFOV, zoomedFOV, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        elapsed = 0f;
        float outTime = zoomDuration * 0.6f;
        Vector3 midPos = transform.position;
        float midFOV = cam.fieldOfView;

        // Zoom OUT back to where follow cam left off
        while (elapsed < outTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / outTime);
            transform.position = Vector3.Lerp(midPos, startPos, t);
            cam.fieldOfView = Mathf.Lerp(midFOV, startFOV, t);
            yield return null;
        }

        // Snap to exact values to avoid drift
        transform.position = startPos;
        cam.fieldOfView = startFOV;
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        Vector3 originPos = transform.localPosition;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = Mathf.Lerp(shakeMagnitude, 0f, elapsed / shakeDuration);

            transform.localPosition = originPos + new Vector3(
                Random.Range(-1f, 1f) * strength,
                Random.Range(-1f, 1f) * strength,
                0f
            );

            yield return null;
        }

        transform.localPosition = originPos;
    }
}
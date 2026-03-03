using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player")]
    [Range(1, 2)] public int playerIndex = 1;

    [Header("Movement")]
    public float moveSpeed = 6.5f;
    public float acceleration = 35f;
    public float deceleration = 45f;

    [Tooltip("If true, input becomes 4-way only (no diagonals).")]
    public bool fourWayOnly = true;

    [Header("Dash (optional)")]
    public bool enableDash = true;
    public KeyCode dashKeyP1 = KeyCode.LeftShift;
    public KeyCode dashKeyP2 = KeyCode.RightShift;
    public float dashSpeed = 14f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 0.60f;

    [Header("Facing / visuals (optional)")]
    public Transform visualRoot;
    public bool faceMoveDirection = true;

    [Header("Animation")]
    public Animator animator; // optional assign; will auto-find on this GO
    [Tooltip("Animator parameter names")]
    public string paramMoveX = "MoveX";      // float (-1,0,1)
    public string paramMoveY = "MoveY";      // float (-1,0,1)
    public string paramSpeed = "Speed";      // float (0..1)
    public string paramIsMoving = "IsMoving";// bool
    public string paramDash = "Dash";        // trigger

    Rigidbody rb;
    string axisH, axisV;

    Vector2 rawInput;
    Vector2 input;         // snapped (4-way)
    Vector3 desiredVel;

    float dashTimer;
    float dashCooldownTimer;
    Vector3 dashDir;

    // last non-zero facing for idle direction
    Vector2 lastMoveDir = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        axisH = playerIndex == 1 ? "Horizontal1" : "Horizontal2";
        axisV = playerIndex == 1 ? "Vertical1" : "Vertical2";

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 1) Read raw input
        rawInput = new Vector2(Input.GetAxisRaw(axisH), Input.GetAxisRaw(axisV));

        // 2) Convert to 4-way if requested (no diagonals)
        input = fourWayOnly ? SnapTo4Way(rawInput) : rawInput;

        // Track last facing direction (for idle)
        if (input.sqrMagnitude > 0.001f)
            lastMoveDir = input;

        // Dash input
        if (enableDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            KeyCode dashKey = playerIndex == 1 ? dashKeyP1 : dashKeyP2;

            if (dashTimer <= 0f && dashCooldownTimer <= 0f && Input.GetKeyDown(dashKey))
            {
                Vector3 dir = new Vector3(input.x, 0f, input.y);

                if (dir.sqrMagnitude < 0.001f)
                {
                    // If no input, dash in last move direction (or velocity fallback)
                    Vector3 last = new Vector3(lastMoveDir.x, 0f, lastMoveDir.y);
                    dir = last.sqrMagnitude > 0.001f ? last : (rb.velocity.sqrMagnitude > 0.01f ? rb.velocity.normalized : Vector3.forward);
                }

                dashDir = dir.normalized;
                dashTimer = dashDuration;
                dashCooldownTimer = dashCooldown;

                // Animation trigger
                if (animator != null && !string.IsNullOrEmpty(paramDash))
                    animator.SetTrigger(paramDash);
            }
        }

        // Rotate visuals (optional)
        if (faceMoveDirection && visualRoot != null)
        {
            Vector3 look = new Vector3(lastMoveDir.x, 0f, lastMoveDir.y);
            if (look.sqrMagnitude > 0.001f)
                visualRoot.forward = look.normalized;
        }

        // 3) Push animation params
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        // Dash movement
        if (enableDash && dashTimer > 0f)
        {
            dashTimer -= Time.fixedDeltaTime;
            rb.velocity = dashDir * dashSpeed;
            return;
        }

        // Regular movement
        desiredVel = new Vector3(input.x, 0f, input.y) * moveSpeed;

        Vector3 vel = rb.velocity;
        Vector3 targetVel = new Vector3(desiredVel.x, 0f, desiredVel.z);

        Vector3 delta = targetVel - new Vector3(vel.x, 0f, vel.z);
        float rate = targetVel.sqrMagnitude > 0.001f ? acceleration : deceleration;

        Vector3 change = Vector3.ClampMagnitude(delta, rate * Time.fixedDeltaTime);
        rb.velocity = new Vector3(vel.x + change.x, 0f, vel.z + change.z);
    }

    // --- Helpers ---

    // Snaps input to cardinal directions only, no diagonals.
    // If both axes pressed, whichever magnitude is larger wins.
    Vector2 SnapTo4Way(Vector2 v)
    {
        if (v.sqrMagnitude < 0.001f) return Vector2.zero;

        // Choose dominant axis (no diagonal)
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            return new Vector2(Mathf.Sign(v.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(v.y));
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        bool moving = input.sqrMagnitude > 0.001f;

        // For direction, use lastMoveDir so idle keeps facing
        Vector2 dir = moving ? input : lastMoveDir;

        // These will be exactly -1, 0, or 1 in 4-way mode
        animator.SetFloat(paramMoveX, dir.x);
        animator.SetFloat(paramMoveY, dir.y);

        animator.SetBool(paramIsMoving, moving);

        // Speed normalized 0..1 (nice for blend trees)
        float speed01 = Mathf.Clamp01(rb.velocity.magnitude / Mathf.Max(0.001f, moveSpeed));
        animator.SetFloat(paramSpeed, speed01);
    }
}
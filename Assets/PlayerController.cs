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
    public Animator[] animators;
    public string paramMoveX;
    public string paramMoveY;
    //public string paramSpeed = "Speed";
    public string paramIsMoving = "IsMoving";
    //public string paramDash = "Dash";

    // Set to true to freeze the player in place (used during countdown)
    [HideInInspector] public bool inputLocked = false;

    public bool stuck = false;

    Rigidbody rb;
    string axisH, axisV;

    Vector2 rawInput;
    Vector2 input;
    Vector3 desiredVel;

    float dashTimer;
    float dashCooldownTimer;
    Vector3 dashDir;

    Vector2 lastMoveDir = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        axisH = playerIndex == 1 ? "Horizontal1" : "Horizontal2";
        axisV = playerIndex == 1 ? "Vertical1" : "Vertical2";

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        animators = GetComponentsInChildren<Animator>();
    }

    void Update()
    {
        if (stuck) return;
        // Locked during countdown — clear input so animations go idle
        if (inputLocked)
        {
            input = Vector2.zero;
            rawInput = Vector2.zero;
            UpdateAnimator();
            return;
        }

        rawInput = new Vector2(Input.GetAxisRaw(axisH), Input.GetAxisRaw(axisV));
        input = fourWayOnly ? SnapTo4Way(rawInput) : rawInput;

        if (input.sqrMagnitude > 0.001f)
            lastMoveDir = input;

        if (enableDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            KeyCode dashKey = playerIndex == 1 ? dashKeyP1 : dashKeyP2;

            if (dashTimer <= 0f && dashCooldownTimer <= 0f && Input.GetKeyDown(dashKey))
            {
                Vector3 dir = new Vector3(input.x, 0f, input.y);

                if (dir.sqrMagnitude < 0.001f)
                {
                    Vector3 last = new Vector3(lastMoveDir.x, 0f, lastMoveDir.y);
                    dir = last.sqrMagnitude > 0.001f ? last : (rb.velocity.sqrMagnitude > 0.01f ? rb.velocity.normalized : Vector3.forward);
                }

                dashDir = dir.normalized;
                dashTimer = dashDuration;
                dashCooldownTimer = dashCooldown;

                //if (animator != null && !string.IsNullOrEmpty(paramDash))
                    //animator.SetTrigger(paramDash);
            }
        }

        if (faceMoveDirection && visualRoot != null)
        {
            Vector3 look = new Vector3(lastMoveDir.x, 0f, lastMoveDir.y);
            if (look.sqrMagnitude > 0.001f)
                visualRoot.forward = look.normalized;
        }

        if(input != Vector2.zero)
        {
            UpdateAnimator();
        }

        bool moving = input.sqrMagnitude > 0.001f;
        
        foreach (Animator anim in animators)
        {
            if (moving)
            {
                anim.SetBool(paramIsMoving, true);
            }
            else
            {
                anim.SetBool(paramIsMoving, false);
            }
        }
    }

    void FixedUpdate()
    {
        // Hard stop while locked
        if (inputLocked)
        {
            rb.velocity = Vector3.zero;
            dashTimer = 0f;
            return;
        }

        if (enableDash && dashTimer > 0f)
        {
            dashTimer -= Time.fixedDeltaTime;
            rb.velocity = dashDir * dashSpeed;
            return;
        }

        desiredVel = new Vector3(input.x, 0f, input.y) * moveSpeed;

        Vector3 vel = rb.velocity;
        Vector3 targetVel = new Vector3(desiredVel.x, 0f, desiredVel.z);

        Vector3 delta = targetVel - new Vector3(vel.x, 0f, vel.z);
        float rate = targetVel.sqrMagnitude > 0.001f ? acceleration : deceleration;

        Vector3 change = Vector3.ClampMagnitude(delta, rate * Time.fixedDeltaTime);
        rb.velocity = new Vector3(vel.x + change.x, 0f, vel.z + change.z);
    }

    Vector2 SnapTo4Way(Vector2 v)
    {
        if (v.sqrMagnitude < 0.001f) return Vector2.zero;

        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            return new Vector2(Mathf.Sign(v.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(v.y));
    }

    void UpdateAnimator()
    {
        foreach (Animator anim in animators)
        {
            if (anim == null) return;

            anim.SetFloat(paramMoveX, input.x);
            anim.SetFloat(paramMoveY, input.y);
        }

            //float speed01 = Mathf.Clamp01(rb.velocity.magnitude / Mathf.Max(0.001f, moveSpeed));
            //animator.SetFloat(paramSpeed, speed01);
        }
}
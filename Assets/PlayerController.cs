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
    public bool normalizeDiagonal = true;

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

    Rigidbody rb;
    string axisH, axisV;

    Vector2 input;
    Vector3 desiredVel;

    float dashTimer;
    float dashCooldownTimer;
    Vector3 dashDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        axisH = playerIndex == 1 ? "Horizontal1" : "Horizontal2";
        axisV = playerIndex == 1 ? "Vertical1" : "Vertical2";

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw(axisH), Input.GetAxisRaw(axisV));
        if (normalizeDiagonal && input.sqrMagnitude > 1f) input = input.normalized;

        if (enableDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            KeyCode dashKey = playerIndex == 1 ? dashKeyP1 : dashKeyP2;

            if (dashTimer <= 0f && dashCooldownTimer <= 0f && Input.GetKeyDown(dashKey))
            {
                Vector3 dir = new Vector3(input.x, 0f, input.y);
                if (dir.sqrMagnitude < 0.001f)
                    dir = rb.velocity.sqrMagnitude > 0.01f ? rb.velocity.normalized : Vector3.forward;

                dashDir = dir.normalized;
                dashTimer = dashDuration;
                dashCooldownTimer = dashCooldown;
            }
        }

        
        if (faceMoveDirection && visualRoot != null)
        {
            Vector3 look = new Vector3(input.x, 0f, input.y);
            if (look.sqrMagnitude > 0.001f)
                visualRoot.forward = look.normalized;
        }
    }

    void FixedUpdate()
    {
        
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
}

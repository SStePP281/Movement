using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Movement : MonoBehaviour
{
    [Header("Rotate")]
    [SerializeField] private float rotateSpeed = 15.0f;

    [Header("RideClerance")]
    [SerializeField] private float rayLenght = 1.5f;
    [SerializeField] private float rideSpringDamper;
    [SerializeField] private float rideSpringStrength;
    [SerializeField] private float rideHeight;

    [Header("TorqueForce")]
    [SerializeField] private float uprightJointSpringStrength;
    [SerializeField] private float uprightJointSpringDamper;

    [Header("Movemt")]
    [SerializeField] private float maxSpeed = 8.0f;
    [SerializeField] private float acceleration = 200.0f;
    [SerializeField] private AnimationCurve accelerationFactorFromDot;
    [SerializeField] private float maxAccelerationForce = 150.0f;
    [SerializeField] private AnimationCurve maxAccelerationForceFactorFromDot;
    [SerializeField] private Vector3 forceScale = new Vector3(1.0f, 0.0f, 1.0f);
    [SerializeField] private float airFactorControl = 0.6f;

    [Header("Slope")]
    [SerializeField] private float maxSlopeAngle = 25.0f;
    [SerializeField] private float slopeSpeedMultiplier = 0.6f;

    [Header("Collide&Slide")]
    [SerializeField] private new SphereCollider collider;
    [SerializeField] private float skinWidht = 0.25f;

    [Header("Jumping")]
    [SerializeField] float jumpForce = 8.5f;

    private RaycastHit floorRayHit;
    private bool isFloorRayHit;

    Bounds bounds;

    private Quaternion targetRot;
    private Vector2 movementInput;
    private Vector3 m_goalVel;
    private Vector3 m_unitGoal;
    private Vector3 groundVel;

    private Vector3 cameraMoveDir;

    private bool isGrounded;
    private bool isJumpPressed;
    private bool isJumping;

    private PlayerInputAction inputActions;
    private Rigidbody body;
    private Transform cameraTransform;

    private void Awake()
    {
        targetRot = transform.rotation;
        cameraTransform = Camera.main.transform;

        bounds = collider.bounds;
        bounds.Expand(-2 * skinWidht);

        body = GetComponent<Rigidbody>();

        inputActions = new PlayerInputAction();
        inputActions.Enable();

        inputActions.PlayerController.Move.started += OnMove;
        inputActions.PlayerController.Move.performed += OnMove;
        inputActions.PlayerController.Move.canceled += OnMove;

        inputActions.PlayerController.Jump.started += OnJump;
        inputActions.PlayerController.Jump.canceled += OnJump;
    }

    private void FixedUpdate()
    {
        DetectFloor();

        HandleRotate();

        HandleGravity();

        HandleRideHeight();
        HandleUpringForce();
        HandleMovement();
        HandleJump();
    }

    private void DetectFloor()
    {
        isFloorRayHit = Physics.Raycast(transform.position + Vector3.down * collider.bounds.extents.y, Vector3.down, out floorRayHit, rayLenght);
    }

    private void HandleGravity()
    {
        body.AddForce(transform.up * Physics.gravity.y * 2.0f);
    }

    private void HandleRotate()
    {
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        cameraMoveDir = movementInput.x * cameraRight + movementInput.y * cameraForward;
        cameraMoveDir.Normalize();

        if (movementInput.magnitude == 0.0f) { return; }

        Quaternion targetRotation = Quaternion.LookRotation(cameraMoveDir);
        body.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime));

        targetRot = targetRotation;
    }

    private void HandleRideHeight()
    {
        if (!isFloorRayHit)
        {
            isGrounded = false;
            groundVel = Vector3.zero;
            return;
        }

        Vector3 vel = body.velocity;
        Vector3 rayDir = transform.TransformDirection(-floorRayHit.normal);

        Vector3 otherVel = Vector3.zero;
        Rigidbody hitBody = floorRayHit.rigidbody;

        if (hitBody != null)
        {
            otherVel = hitBody.velocity;
            groundVel = hitBody.velocity;
        }
        else
        {
            groundVel = Vector3.zero;
        }

        float rayDirVel = Vector3.Dot(rayDir, vel);
        float otherDirVel = Vector3.Dot(rayDir, otherVel);

        float relVel = rayDirVel - otherDirVel;

        float x = floorRayHit.distance - rideHeight;

        float springForce = (x * rideSpringStrength) - (relVel * rideSpringDamper);

        body.AddForce(rayDir * springForce);

        if (hitBody != null)
        {
            hitBody.AddForceAtPosition(rayDir * -springForce, floorRayHit.point);
        }

        isGrounded = x < 0.15f;
    }

    private void HandleUpringForce()
    {
        Quaternion current = transform.rotation;
        Quaternion toGoal = ShortestRotation(current, targetRot);

        toGoal.ToAngleAxis(out float rotDegrees, out Vector3 rotAxis);
        rotAxis.Normalize();

        float rotRadians = rotDegrees * Mathf.Deg2Rad;

        body.AddTorque((rotAxis * (rotRadians * uprightJointSpringStrength)) - (body.angularVelocity * uprightJointSpringDamper));
    }

    private void HandleMovement()
    {
        m_unitGoal = GetMovementAxis();
        Vector3 unitVel = m_goalVel.normalized;
        float velDot = Vector3.Dot(m_unitGoal, unitVel);

        float accel = acceleration * accelerationFactorFromDot.Evaluate(velDot);

        Vector3 goalVel = m_unitGoal * GetMoveSpeed();
        //goalVel = CollideAndSlide(goalVel, transform.position);

        m_goalVel = Vector3.MoveTowards(m_goalVel, (goalVel + groundVel), accel * Time.fixedDeltaTime);

        Vector3 neededAccel = (m_goalVel - body.velocity) / Time.fixedDeltaTime;
        float maxAccel = maxAccelerationForce * maxAccelerationForceFactorFromDot.Evaluate(velDot);
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);

        body.AddForce(Vector3.Scale(body.mass * neededAccel, forceScale));
    }

    private void HandleJump()
    {
        if (isGrounded && !isJumping && isJumpPressed)
        {
            isJumping = true;

            body.velocity -= body.velocity.y * Vector3.up;

            body.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else if (isJumping && !isJumpPressed)
        {
            isJumping = false;
        }
    }

    private Vector3 GetMovementAxis()
    {
        Vector3 moveDir = cameraMoveDir;

        if (isFloorRayHit && isGrounded)
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, floorRayHit.normal).normalized;
        }

        return moveDir;
    }

    private float GetMoveSpeed()
    {
        float baseSpeed = isGrounded ? maxSpeed : maxSpeed * airFactorControl;

        if (isFloorRayHit && isGrounded)
        {
            float slopeAngle = Vector3.Angle(Vector3.up, floorRayHit.normal);

            float normalizedSlope = Mathf.Clamp01(slopeAngle / maxSlopeAngle);

            float moveDirDot = Vector3.Dot(transform.forward, floorRayHit.normal);

            if (Mathf.Abs(normalizedSlope) < 0.01f)
            {
                normalizedSlope = 0.0f;
            }

            baseSpeed *= (1.0f + moveDirDot * normalizedSlope * slopeSpeedMultiplier);
        }

        return baseSpeed;
    }

    private Vector3 CollideAndSlide(Vector3 velocity, Vector3 pos, int depth = 0)
    {
        if (depth >= 3) { return Vector3.zero; }

        float speed = velocity.magnitude;

        if (speed <= 0.001f) return velocity;

        Vector3 direction = velocity.normalized;
        float distance = speed * Time.fixedDeltaTime;

        if (Physics.SphereCast(pos, bounds.extents.x, direction, out RaycastHit hit, distance + skinWidht))
        {
            if (hit.rigidbody != null) { return velocity; }

            float stopDistance = Mathf.Max(0, hit.distance - skinWidht);
            float ratio = stopDistance / distance;

            Vector3 movementComponent = direction * (speed * ratio);
            Vector3 leftover = velocity - movementComponent;

            Vector3 slideDirection = Vector3.ProjectOnPlane(leftover, hit.normal);

            return movementComponent + CollideAndSlide(slideDirection, pos, depth + 1);
        }

        return velocity;
    }

    private Quaternion ShortestRotation(Quaternion from, Quaternion to)
    {
        from = from.normalized;
        to = to.normalized;

        float dot = Quaternion.Dot(from, to);

        if (dot < 0.0f)
        {
            to = new Quaternion(-to.x, -to.y, -to.z, -to.w);
            dot = -dot;
        }

        Quaternion result = to * Quaternion.Inverse(from);

        return result.normalized;
    }

    private void OnMove(InputAction.CallbackContext cntx)
    {
        movementInput = cntx.ReadValue<Vector2>().normalized;
    }

    private void OnJump(InputAction.CallbackContext cntx)
    {
        isJumpPressed = cntx.ReadValueAsButton();
    }
}
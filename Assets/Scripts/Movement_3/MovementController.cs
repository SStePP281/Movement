using System;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    private const int COMBAT_COUNT_MAX = 3;

    [Header("GroundDetectParams")]
    [SerializeField] private LayerMask ignoreGroundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float groundCheckRadiuse = 0.5f;

    [Header("Rotate")]
    [SerializeField] private float rotateSpeed = 15.0f;

    [Header("Movement")]
    [SerializeField] private float maxMoveSpeed = 10.0f;
    [SerializeField] private float moveAcceleration = 3.0f;
    [SerializeField] private AnimationCurve accelerationFactorFromDot;
    [SerializeField] private float maxMoveAcceleration = 150.0f;
    [SerializeField] private AnimationCurve maxAccelerationForceFactorFromDot;
    [SerializeField] private float airFactorControl = 0.6f;
    [SerializeField] private Vector3 moveForceScale = new Vector3(1.0f, 0.0f, 1.0f);

    [Header("SlopeAndSlide")]
    [SerializeField] private float maxSlopeAngle = 25.0f;
    [SerializeField] private float slopeSpeedMultiplier = 1.2f;
    [SerializeField] private float slideAcceleration = 5.0f;
    [SerializeField] private float maxSlideSpeed = 15.0f;
    [SerializeField] private float slideRotationSpeed = 5.0f;
    [SerializeField] private float slideFactorControl = 0.3f;

    [Header("Jumping")]
    [SerializeField] private float baseJumpHieght = 5.0f;
    [SerializeField] private float maxJumpingTime = 1.0f;
    [SerializeField] private float combatJumpHeightCoef = 1.15f;
    [SerializeField] private float timeBettwenCombatJump = 0.5f;
    [SerializeField] private float coyoteTime = 0.33f;
    [SerializeField] private float jumpThresoldTime = 0.2f;
    [SerializeField] private float fallMultiplier = 2.0f;
    [SerializeField] private float maxFallSpeed = 30.0f;
    [SerializeField] private float fallResistanceFactor = 3f;

    [Header("Ride_Height")]
    [SerializeField] private float floorRayLength = 1.0f;
    [SerializeField] private float rideHeight = 0.5f;
    [SerializeField] private float rideSpringStrength = 100.0f;
    [SerializeField] private float rideSpringDamper = 5.0f;

    [Header("Upright")]
    [SerializeField] private float uprightJointSpringStrength = 100.0f;
    [SerializeField] private float uprightJointSpringDamper = 10.0f;

    [Header("Ragdol")]
    [SerializeField] private float maxFallTime = 2.0f;

    /*Floor params*/
    private bool isFloorRayHit;
    private RaycastHit floorHit;
    private float floorAngle;

    /*Take params*/
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Transform cameraTransform;

    /*Move params*/
    private Vector3 cameraMoveDirection;
    private Vector3 lastGoalVelocity;
    private Vector3 groundVelocity;

    public float MoveVelocity { get; private set; }

    /*Slide params*/
    private bool isSliding;
    private Vector3 lastSlideVelocity;
    private Vector3 slideDirection;

    /*Jump params*/
    private ReactiveVariable<bool> isGrounded;
    private bool isJumping;
    private bool isJumpCut;
    private bool rideHeightEnable = true;
    private float[] initialJumpGravity;
    private float[] initialJumpVelocity;
    private float lastGroundedTime;
    private float landedTime;
    private bool isJumpPressedLastes = false;
    private float lastStartJumpPressedTime;
    private float lastJumpTime;

    public ReactiveVariable<int> CombatJumpCount;
    public ReactiveVariable<bool> IsJumpAnimation;

    /*Rotate params*/
    private Quaternion targetUprightRotation;

    /*Update params*/
    private float deltaTime;
    private bool isCanMove;

    /*Ragdol params*/
    private bool isFalling;
    public Action<Vector3> OnFalling;

    public void Initialize(float deltaTime)
    {
        this.deltaTime = deltaTime;

        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        isCanMove = true;
        targetUprightRotation = transform.rotation;
        cameraTransform = Camera.main.transform;

        initialJumpGravity = new float[COMBAT_COUNT_MAX];
        initialJumpVelocity = new float[COMBAT_COUNT_MAX];
        SetupJumpVariables();

        isGrounded = new ReactiveVariable<bool>(false);
        isGrounded.OnValueChanged += OnValueChanged_IsGrounded;

        CombatJumpCount = new ReactiveVariable<int>(0);
        IsJumpAnimation = new ReactiveVariable<bool>(false);
    }

    public void OnRagdolEnable()
    {
        isCanMove = false;
        capsule.enabled = false;
        rb.isKinematic = true;
    }

    public void OnRagdolDisable()
    {
        isCanMove = true;
        capsule.enabled = true;
        rb.isKinematic = false;

        HandleDetec();
    }

    private void OnValueChanged_IsGrounded(bool priviouse, bool current)
    {
        if (current)
        {
            landedTime = Time.time;
            IsJumpAnimation.Value = false;
            isJumpCut = false;
            rideHeightEnable = true;
            isFalling = false;
        }

        if (!current)
        {
            lastGroundedTime = Time.time;
        }
    }

    public void MovementUpdate(InputData inputData)
    {
        if (!isCanMove) { return; }

        HandleRotate(inputData.movementInput);
        HandleJump(inputData.isJumpPressed);
        HandleDetec();
        HandleRideHeight();
        HandleUpringForce();
        HandleSlide(inputData.movementInput);
        HandleMove();
        HandleGravity(inputData.isJumpPressed);
    }

    private void HandleGravity(bool isJumpPressed)
    {
        float gravityTick = initialJumpGravity[CombatJumpCount.Value];

        float verticalVelocity = 0.0f;

        bool isFalling = rb.velocity.y <= 0.0f || isJumpCut;

        if (isGrounded.Value)
        {
            verticalVelocity = -0.1f;
        }
        else if (isFalling)
        {
            verticalVelocity = gravityTick * fallMultiplier;
        }
        else
        {
            verticalVelocity = gravityTick;
        }

        Vector3 needAcceleration = verticalVelocity * Vector3.up;

        rb.AddForce(needAcceleration * rb.mass);

        if (-rb.velocity.y > maxFallSpeed)
        {
            float excessFallSpeed = rb.velocity.y + maxFallSpeed;

            float resistanceForceMagnitude = -excessFallSpeed * fallResistanceFactor;

            rb.AddForce(Vector3.up * resistanceForceMagnitude, ForceMode.Force);
        }
    }

    private void HandleJump(bool isJumpPressed)
    {
        if (isJumpPressed)
        {
            if (!isJumpPressedLastes)
            {
                isJumpPressedLastes = true;
                lastStartJumpPressedTime = Time.time;
            }
        }
        else
        {
            if (isJumpPressedLastes)
            {
                isJumpPressedLastes = false;
            }
        }

        if (isJumpPressed && !isJumping && IsCanJump())
        {
            isJumping = true;
            isJumpCut = false;
            IsJumpAnimation.Value = true;
            rideHeightEnable = false;
            CombatJumpCount.Value = Time.time - landedTime <= timeBettwenCombatJump ? (CombatJumpCount.Value + 1) % COMBAT_COUNT_MAX : 0;
            lastJumpTime = Time.time;

            if (rb.velocity.y < 0.0f)
            {
                rb.velocity -= Vector3.up * rb.velocity.y;
            }

            rb.AddForce(Vector3.up * initialJumpVelocity[CombatJumpCount.Value], ForceMode.Impulse);
        }
        else if (isJumping && !isJumpPressed)
        {
            if (isGrounded.Value)
            {
                IsJumpAnimation.Value = false;
            }

            isJumping = false;
            isJumpCut = true;
            rideHeightEnable = true;
        }
    }

    private void HandleSlide(Vector2 movementInput)
    {
        if (!isSliding || isJumping) 
        {
            lastSlideVelocity = Vector3.zero;
            return; 
        }

        slideDirection = Vector3.ProjectOnPlane(Vector3.down, floorHit.normal).normalized;

        Vector3 slideVelocity = slideDirection * maxSlideSpeed;

        lastSlideVelocity = Vector3.MoveTowards(lastSlideVelocity, slideVelocity, slideAcceleration * deltaTime);

        if (movementInput.magnitude == 0.0f)
        {
            Vector3 horizontalSlideDirection = new Vector3(slideDirection.x, 0.0f, slideDirection.z);
            Quaternion targetRotation = Quaternion.LookRotation(horizontalSlideDirection);
            targetRotation = Quaternion.Slerp(rb.rotation, targetRotation, slideRotationSpeed * deltaTime);

            rb.MoveRotation(targetRotation);
            Vector3 euler = targetRotation.eulerAngles;
            targetUprightRotation = Quaternion.Euler(0f, euler.y, 0f);
        }
    }

    private void HandleMove()
    {
        Vector3 unitGoal = GetMoveAxis();
        Vector3 unitVelocity = lastGoalVelocity.normalized;
        float velDot = Vector3.Dot(unitGoal, unitVelocity);

        float acceleration = moveAcceleration * accelerationFactorFromDot.Evaluate(velDot);

        Vector3 goalVel = unitGoal * GetMoveSpeed();

        lastGoalVelocity = Vector3.MoveTowards(lastGoalVelocity, (goalVel + groundVelocity), acceleration * deltaTime);

        Vector3 needAcceleration = (lastGoalVelocity + lastSlideVelocity - rb.velocity) / deltaTime;
        float maxAcceleration = maxMoveAcceleration * maxAccelerationForceFactorFromDot.Evaluate(velDot);
        needAcceleration = Vector3.ClampMagnitude(needAcceleration, maxAcceleration);

        rb.AddForce(Vector3.Scale(rb.mass * needAcceleration, moveForceScale));

        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);
        MoveVelocity = horizontalVelocity.magnitude / maxMoveSpeed;
    }

    private void HandleUpringForce()
    {
        Quaternion current = transform.rotation;
        Quaternion toGoal = ShortestRotation(current, targetUprightRotation);

        toGoal.ToAngleAxis(out float rotDegrees, out Vector3 rotAxis);
        rotAxis.Normalize();

        float rotRadians = rotDegrees * Mathf.Deg2Rad;

        rb.AddTorque((rotAxis * (rotRadians * uprightJointSpringStrength)) - (rb.angularVelocity * uprightJointSpringDamper));
    }

    private void HandleRideHeight()
    {
        if (!rideHeightEnable || !isFloorRayHit)
        {
            groundVelocity = Vector3.zero;
            return;
        }

        Vector3 currentVelocity = rb.velocity;
        Vector3 rayDirection = Vector3.down;

        Vector3 otherVelocity = Vector3.zero;
        Rigidbody hitBody = floorHit.rigidbody;

        if (hitBody != null)
        {
            otherVelocity = hitBody.velocity;
            groundVelocity = hitBody.velocity;
        }
        else
        {
            groundVelocity = Vector3.zero;
        }

        float rayDirectionVelocity = Vector3.Dot(rayDirection, currentVelocity);
        float otherRayDirectionVelocity = Vector3.Dot(rayDirection, otherVelocity);

        float relativeVelocity = rayDirectionVelocity - otherRayDirectionVelocity;

        float deltaHeight = floorHit.distance - rideHeight;

        float springForce = (deltaHeight * rideSpringStrength) - (relativeVelocity * rideSpringDamper);

        rb.AddForce(rayDirection * springForce);

        if (hitBody != null)
        {
            hitBody.AddForceAtPosition(rayDirection * -springForce, floorHit.point);
        }
    }

    private void HandleDetec()
    {
        Vector3 startRayPosition = rb.position + Vector3.down * (capsule.height * 0.95f / 2.0f - capsule.center.y);

        isFloorRayHit = Physics.Raycast(startRayPosition, Vector3.down, out floorHit, floorRayLength);

        if (isFloorRayHit)
        {
            floorAngle = Vector3.Angle(Vector3.up, floorHit.normal);
            if (floorAngle < 0.1f) { floorAngle = 0.0f; }

            isSliding = floorAngle > maxSlopeAngle && !isJumping;

            isGrounded.Value = Physics.CheckSphere(startRayPosition - Vector3.up * groundCheckDistance, groundCheckRadiuse);
        }
        else
        {
            isGrounded.Value = false;
            isSliding = false;
            floorAngle = 0.0f;
        }

        if (!isGrounded.Value && !isJumping && !isFalling && Time.time - lastGroundedTime > maxFallTime)
        {
            isFalling = true;
            OnFalling?.Invoke(rb.velocity);
        }
    }

    private void HandleRotate(Vector2 movementInput)
    {
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraMoveDirection = movementInput.x * cameraRight + movementInput.y * cameraForward;
        cameraMoveDirection.y = 0.0f;
        cameraMoveDirection.Normalize();

        if (movementInput.magnitude == 0.0f) { return; }

        Quaternion targetRotation = Quaternion.LookRotation(cameraMoveDirection);
        float rotationSpeed = isGrounded.Value ? rotateSpeed : airFactorControl;
        targetRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * deltaTime);

        rb.MoveRotation(targetRotation);
        Vector3 euler = targetRotation.eulerAngles;
        targetUprightRotation = Quaternion.Euler(0f, euler.y, 0f);
    }    

    private float GetMoveSpeed()
    {
        float moveSpeed = maxMoveSpeed;

        if (isSliding)
        {
            moveSpeed *= slideFactorControl;
        }
        else if (isGrounded.Value && isFloorRayHit)
        {
            float normalizedSlope = Mathf.Clamp01(floorAngle / maxSlopeAngle);

            float moveDirectionDot = Vector3.Dot(rb.transform.forward, floorHit.normal);

            if (Mathf.Abs(normalizedSlope) < 0.01f)
            {
                normalizedSlope = 0.0f;
            }

            moveSpeed *= (1.0f + normalizedSlope * moveDirectionDot * slopeSpeedMultiplier);
        }

        return moveSpeed;
    }

    private Vector3 GetMoveAxis()
    {
        Vector3 moveDir = cameraMoveDirection;
        
        if (isGrounded.Value && isFloorRayHit)
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, floorHit.normal).normalized;
        }

        return moveDir;
    }

    private bool IsCanJump()
    {
        bool bettwen = (Time.time - lastJumpTime >= maxJumpingTime / 2.0f);
        bool coyote = (Time.time - lastGroundedTime <= coyoteTime && !isJumpCut);

        bool buffer = Time.time - lastStartJumpPressedTime <= jumpThresoldTime;

        return buffer && (isGrounded.Value || coyote) && !isSliding && bettwen;
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

    private void SetupJumpVariables()
    {
        float timeToApex = maxJumpingTime / 2.0f;

        initialJumpGravity[0] = (-2 * baseJumpHieght) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity[0] = (2 * baseJumpHieght) / timeToApex;

        initialJumpGravity[1] = (-2 * (baseJumpHieght + combatJumpHeightCoef)) / Mathf.Pow((timeToApex * 1.25f), 2);
        initialJumpVelocity[1] = (2 * (baseJumpHieght + combatJumpHeightCoef)) / (timeToApex * 1.25f);

        initialJumpGravity[2] = (-2 * (baseJumpHieght + combatJumpHeightCoef * 2)) / Mathf.Pow((timeToApex * 1.5f), 2);
        initialJumpVelocity[2] = (2 * (baseJumpHieght + combatJumpHeightCoef * 2)) / (timeToApex * 1.5f);
    }

    ///*DEBUG*/
    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Vector3 startRayPosition = rb.position + Vector3.down * (capsule.height * 0.95f / 2.0f - capsule.center.y);
    //    Gizmos.DrawSphere(startRayPosition - Vector3.up * groundCheckDistance, groundCheckRadiuse);

    //    Gizmos.color = Color.cyan;
    //    Gizmos.DrawRay(startRayPosition, Vector3.down * floorRayLength);
    //}
}
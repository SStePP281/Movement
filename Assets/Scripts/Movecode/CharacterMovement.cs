using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("MovementParametr")]
    [SerializeField] private float maxMoveSpeed = 5.0f;
    [SerializeField] private float acceleration = 5.0f;
    [SerializeField] private float deceleration = 1.0f;
    [SerializeField] private float speedResistanceFactor = 5f;
    [SerializeField][Range(0.1f, 1f)] private float airControlFactor = 0.5f;
    [SerializeField] private float maxFallMoveSpeed = 30.0f;
    [SerializeField] private float rotationSpeed = 15.0f;

    [Header("Slope Handling")]
    [SerializeField] private float slopeCheckDistance = 0.5f;
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slopeSpeedMultiplier = 0.5f;

    [Header("JumpingParamentr")]
    [SerializeField] private float baseJumpHieght = 5.0f;
    [SerializeField] private float maxJumpingTime = 1.0f;
    [SerializeField] private float combatJumpHeightCoef = 1.15f;
    [SerializeField] private float fallMultiplier = 1.5f;
    [SerializeField] private float fallResistanceFactor = 3f;
    [SerializeField] private float timeBettwenCombatJump = 0.5f;
    [SerializeField] private float coyoteTime = 0.1f;

    public Vector3 Velocity { get => rb.velocity; }
    public bool IsGrounded { get => isGrounded; }
    public ReactiveVariable<int> jumpCombatCount;
    public ReactiveVariable<bool> isJumpAnimation;

    private Rigidbody rb;
    private Transform cameraTransform;

    Vector3 moveDirection;
    private bool isGrounded;
    private Vector3 floorNormal;
    private float slopeAmount;
    private RaycastHit slopeHit;

    private float landedTime;
    private bool isJumping;
    private float lastGroundedTime;

    private float[] initialJumpVelocity;
    private float[] initialJumpGravity;

    public void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        initialJumpVelocity = new float[3];
        initialJumpGravity = new float[3];

        jumpCombatCount = new ReactiveVariable<int>();
        isJumpAnimation = new ReactiveVariable<bool>();

        cameraTransform = Camera.main.transform;

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.velocity = Vector3.zero;

        SetupJumpVariables();

        HandleCheckGround();
    }

    public void MovementProcess(CharacterInputData inputData, float deltaTime)
    {
        HandleRotation(inputData.movement, deltaTime);

        HandleJump(inputData.isJumpPressed);
        HandleGravity(inputData.isJumpPressed, deltaTime);
        HandleCheckGround();
        HandleMovement(deltaTime);
    }

    private void HandleRotation(Vector2 currentMovementInput, float deltaTime)
    {
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        moveDirection = currentMovementInput.x * cameraRight + currentMovementInput.y * cameraForward;
        moveDirection.Normalize();

        if (currentMovementInput.magnitude == 0.0f) { return; }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime));
    }

    private void HandleJump(bool isJumpPressed)
    {
        if (!isJumping && isJumpPressed && (isGrounded || Time.time - lastGroundedTime <= coyoteTime))
        {
            jumpCombatCount.Value = Time.time - landedTime <= timeBettwenCombatJump ? (jumpCombatCount.Value + 1) % 3 : 0;
            isJumping = true;
            isJumpAnimation.Value = true;

            rb.velocity -= Vector3.up * rb.velocity.y;
            rb.AddForce(Vector3.up * initialJumpVelocity[jumpCombatCount.Value], ForceMode.VelocityChange);
        }
        else if (isJumping && !isJumpPressed)
        {
            isJumping = false;
        }
    }

    private void HandleGravity(bool isJumpPressed, float deltaTime)
    {
        float gravityThisTick = initialJumpGravity[jumpCombatCount.Value] * deltaTime;

        float verticalVelocity = 0.0f;

        bool isFalling = rb.velocity.y <= 0.0f || !isJumpPressed;

        if (isGrounded)
        {
            verticalVelocity = -0.1f;
            lastGroundedTime = Time.time;
        }
        else if (isFalling)
        {
            verticalVelocity += gravityThisTick * fallMultiplier;
        }
        else
        {
            verticalVelocity += gravityThisTick;
        }

        rb.AddForce(Vector3.up * verticalVelocity, ForceMode.VelocityChange);

        if (-rb.velocity.y > maxFallMoveSpeed)
        {
            float excessFallSpeed = rb.velocity.y + maxFallMoveSpeed;

            float resistanceForceMagnitude = -excessFallSpeed * fallResistanceFactor;

            rb.AddForce(Vector3.up * resistanceForceMagnitude, ForceMode.Force);
        }

        if (rb.velocity.magnitude < 0.01f && moveDirection.magnitude < 0.01f)
        {
            rb.velocity = Vector3.up * rb.velocity.y;
        }
    }

    private void HandleCheckGround()
    {
        bool wasGrounded = isGrounded;

        isGrounded = FloorRaycasts() != Vector3.zero;

        if (!wasGrounded && isGrounded)
        {
            landedTime = Time.time;
            isJumpAnimation.Value = false;
        }

        slopeAmount = isGrounded ? Vector3.Dot(transform.forward, floorNormal) : 0.0f;
    }

    private Vector3 FloorRaycasts()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            floorNormal = hit.normal;

            if (Vector3.Angle(floorNormal, Vector3.up) < maxSlopeAngle)
            {
                return hit.point;
            }
        }

        return Vector3.zero;
    }

    private void HandleMovement(float deltaTime)
    {
        float baseTargetSpeed = maxMoveSpeed;

        Vector3 moveDirOnPlane = moveDirection;

        if (Mathf.Abs(slopeAmount) > 0.01f)
        {
            baseTargetSpeed *= (1.0f + slopeAmount * slopeSpeedMultiplier);
        }

        if (isGrounded)
        {
            moveDirOnPlane = Vector3.ProjectOnPlane(moveDirection, floorNormal).normalized;
            moveDirOnPlane.y = 0;
            moveDirOnPlane.Normalize();
        }

        Vector3 targetVelocity = moveDirOnPlane * baseTargetSpeed;

        Vector3 currentVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        Vector3 velocityDiff = targetVelocity - currentVelocity;

        float controlFactor = isGrounded ? 1.0f : airControlFactor;

        float accelerationFactor = moveDirection.magnitude > 0.01f ?
            acceleration * controlFactor :
            deceleration * controlFactor;

        velocityDiff = Vector3.ClampMagnitude(velocityDiff, accelerationFactor * deltaTime);

        rb.AddForce(velocityDiff / deltaTime, ForceMode.Acceleration);

        float currentSpeed = currentVelocity.magnitude;
        float absoluteMaxSpeed = maxMoveSpeed * 1.5f;

        if (currentSpeed > absoluteMaxSpeed)
        {
            float excessSpeed = currentSpeed - absoluteMaxSpeed;
            Vector3 resistanceDirection = -currentVelocity.normalized;
            float resistanceForceMagnitude = excessSpeed * speedResistanceFactor;
            Vector3 resistanceForce = resistanceDirection * resistanceForceMagnitude;
            rb.AddForce(resistanceForce, ForceMode.Force);
        }
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
}
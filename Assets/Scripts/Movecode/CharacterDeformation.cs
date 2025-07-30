using UnityEngine;

public class CharacterDeformation : MonoBehaviour
{
    [SerializeField] private Transform playerVisual;

    [Header("Squash & Stretch")]
    [SerializeField] private float jumpStretchY = 0.25f;
    [SerializeField] private float jumpSquashXZ = 0.15f;
    [SerializeField] private float landSquashY = 0.35f;
    [SerializeField] private float landStretchXZ = 0.25f;
    [SerializeField] private float effectSpeed = 15f;
    [SerializeField] private float landEffectDuration = 0.3f;

    [Header("Landing Force Settings")]
    [SerializeField] private float maxLandingSpeed = 20f;

    [Header("Inertia Lean Settings")]
    [SerializeField] private float leanMultiplier = 0.02f;
    [SerializeField] private float leanSmoothTime = 0.1f;
    [SerializeField] private float maxRollAngle = 30f;
    [SerializeField] private float maxPitchAngle = 20f;
    [SerializeField] private float leanReturnSpeed = 10f;

    [Header("Nose Punch Settings")]
    [SerializeField] private float punchThreshold = 1.5f;
    [SerializeField] private float punchStrength = 1.5f;
    [SerializeField] private float punchDamping = 5f;

    private Vector3 baseScale;
    private bool wasGrounded;
    private float landEffectTimer;
    private float landLastVerticalVelocity;
    private float landEffectMultiplier;

    private Vector3 currentLean;
    private Vector3 leanVelocity;
    private float punchVelocity;
    private float lastLocalZVelocity;

    private void Awake()
    {
        baseScale = playerVisual.localScale;
        wasGrounded = true;
        currentLean = Vector3.zero;
    }

    private void OnDisable()
    {
        playerVisual.localScale = baseScale;
        playerVisual.localRotation = Quaternion.identity;
    }

    public void SquashAndStretch(bool isGround, Vector3 velocity)
    {
        ApplySquashStretch(isGround, velocity.y);
        ApplyInertiaLean(velocity);
    }

    private void ApplySquashStretch(bool isGrounded, float verticalVel)
    {
        Vector3 targetScale = baseScale;

        if (!isGrounded)
        {
            targetScale.y = baseScale.y * (1 + jumpStretchY);
            targetScale.x = baseScale.x * (1 - jumpSquashXZ);
            targetScale.z = baseScale.z * (1 - jumpSquashXZ);
        }
        else if (!wasGrounded)
        {    
            landEffectMultiplier = Mathf.Clamp01(Mathf.Abs(landLastVerticalVelocity) / maxLandingSpeed);
            landEffectTimer = landEffectDuration * landEffectMultiplier;
            targetScale.y = baseScale.y * (1 - landSquashY * landEffectMultiplier);
            targetScale.x = baseScale.x * (1 + landStretchXZ * landEffectMultiplier);
            targetScale.z = baseScale.z * (1 + landStretchXZ * landEffectMultiplier);
        }
        else if (landEffectTimer > 0f)
        {
            landEffectTimer -= Time.deltaTime;
            float t = landEffectTimer / (landEffectDuration * landEffectMultiplier);
            targetScale.y = baseScale.y * (1 - landSquashY * landEffectMultiplier * t);
            targetScale.x = baseScale.x * (1 + landStretchXZ * landEffectMultiplier * t);
            targetScale.z = baseScale.z * (1 + landStretchXZ * landEffectMultiplier * t);
        }

        playerVisual.localScale = Vector3.Lerp(playerVisual.localScale, targetScale, effectSpeed * Time.deltaTime);
        landLastVerticalVelocity = verticalVel;
        wasGrounded = isGrounded;
    }

    private void ApplyInertiaLean(Vector3 velocity)
    {
        Vector3 worldHorizontalVel = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 localVelocity = playerVisual.InverseTransformDirection(worldHorizontalVel);

        float decel = (localVelocity.z - lastLocalZVelocity) / Time.deltaTime;
        if (decel < -punchThreshold)
        {
            punchVelocity = punchStrength;
        }
        punchVelocity = Mathf.Lerp(punchVelocity, 0f, Time.deltaTime * punchDamping);
        lastLocalZVelocity = localVelocity.z;

        Vector3 targetLean = localVelocity.sqrMagnitude < 0.0001f
            ? Vector3.zero
            : -localVelocity.normalized * localVelocity.magnitude * leanMultiplier;

        currentLean = Vector3.SmoothDamp(currentLean, targetLean, ref leanVelocity, leanSmoothTime);

        float roll = Mathf.Clamp(currentLean.x * maxRollAngle, -maxRollAngle, maxRollAngle);
        float pitch = Mathf.Clamp(currentLean.z * maxPitchAngle, -maxPitchAngle, maxPitchAngle);
        pitch += punchVelocity;

        Quaternion targetRot = Quaternion.Euler(pitch, 0f, roll);
        playerVisual.localRotation = Quaternion.Slerp(playerVisual.localRotation, targetRot, Time.deltaTime * leanReturnSpeed);
    }
}

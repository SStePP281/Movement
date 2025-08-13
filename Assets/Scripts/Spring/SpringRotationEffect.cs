using UnityEngine;

public class SpringRotationEffect : BaseSpringEffect
{
    [SerializeField] private Vector3 rotateDirection = Vector3.forward;
    [SerializeField] private float maxRotationAngle = 90.0f;
    [SerializeField] private bool isFinal = false;

    public override void OnSpringUpdate(float springValue, float springVelocity)
    {
        if (isFinal)
        {
            float finalSpringValue = springValue * maxRotationAngle;
            transform.localRotation = Quaternion.Euler(rotateDirection * finalSpringValue);
        }
        else
        {
            float finalSpringValue = springVelocity * maxRotationAngle;
            transform.localRotation = Quaternion.Euler(rotateDirection * finalSpringValue);
        }
    }
}

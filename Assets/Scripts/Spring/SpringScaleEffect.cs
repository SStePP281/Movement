using UnityEngine;

public class SpringScaleEffect : BaseSpringEffect
{
    [SerializeField] private Vector3 scaleDirection = Vector3.one;
    [SerializeField] private float normalScale = 1.0f;
    [SerializeField] private float targetScale = 0.0f;

    public override void OnSpringUpdate(float springValue, float springVelocity)
    {
        float adjSpringScale = Mathf.Abs(springValue);
        float finalSpringScale = Mathf.LerpUnclamped(normalScale, targetScale, adjSpringScale);

        float delta = finalSpringScale - 1.0f;
        Vector3 finaScale = new Vector3(
            scaleDirection.x >= 0.0f ? 1.0f - delta : 1.0f + delta,
            scaleDirection.y >= 0.0f ? 1.0f - delta : 1.0f + delta,
            scaleDirection.z >= 0.0f ? 1.0f - delta : 1.0f + delta
            );

        transform.localScale = finaScale;
    }
}

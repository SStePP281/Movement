using UnityEngine;

public class SpringAlphaEffect : BaseSpringEffect
{
    [SerializeField] private AnimationCurve alphaCurve;

    private IAlphaTarget alphaTarget;

    private void Awake()
    {
        alphaTarget = GetComponent<IAlphaTarget>();
    }

    public override void OnSpringUpdate(float springValue, float springVelocity)
    {
        float adjSpringValue = Mathf.Abs(springValue);
        float finalAlpha = Mathf.LerpUnclamped(1.0f, 0.0f, alphaCurve.Evaluate(adjSpringValue));

        alphaTarget.SetAlpha(finalAlpha);
    }
}

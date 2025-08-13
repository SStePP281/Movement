using UnityEngine;

public class SpringUI : MonoBehaviour
{
    [Header("Take")]
    [SerializeField] private BaseSpringEffect[] springEffects;

    [Header("Spring")]
    [SerializeField] private float springStrength = 15.0f;
    [SerializeField] private float springDamper = 0.3f;

    private ReactiveVariable<float> currentValue;
    private float targetValue;

    private DampedSpringMotionParams springParam;
    private float springVelocity;

    private void Awake()
    {
        currentValue = new ReactiveVariable<float>(targetValue);
        currentValue.OnValueChanged += CurrentValue_OnValueChanged;
    }

    private void Update()
    {
        MathSpring.CalculateSpringParams(ref springParam, Time.deltaTime, springStrength, springDamper);

        if (Input.GetMouseButtonDown(0))
        {
            targetValue = Mathf.Clamp(targetValue - 1, -1, 1);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            targetValue = Mathf.Clamp(targetValue + 1, -1, 1);
        }

        float curVal = currentValue.Value;
        MathSpring.UpdateSpring(ref curVal, ref springVelocity, targetValue, ref springParam);
        currentValue.Value = curVal;
    }

    private void CurrentValue_OnValueChanged(float priviouse, float newValue)
    {
        foreach (var effect in springEffects)
        {
            effect.OnSpringUpdate(newValue, springVelocity);
        }
    }
}

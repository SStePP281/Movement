using UnityEngine;

public class ButtonScript : MonoBehaviour
{
    [Header("Spring")]
    [SerializeField] private float strength = 5.0f;
    [SerializeField] private float damper = 0.3f;

    [SerializeField] private BaseSpringEffect[] springEffects;

    [Header("Nudgle")]
    [SerializeField] private float nudgleDelay = 1.0f;
    [SerializeField] private float nudgleStrength = 1.0f;

    private bool wasPressed = false;

    private ReactiveVariable<float> currentValue;
    private float targetValue;

    private DampedSpringMotionParams springParam;
    private float springVelocity;


    private void Awake()
    {
        currentValue = new ReactiveVariable<float>(targetValue);
        currentValue.OnValueChanged += CurrentValue_OnValueChanged;
    }

    //private void Start()
    //{
    //    StartCoroutine(ExcuteNedgleLoop());
    //}

    private void Update()
    {
        SetButtonPressed(Input.GetMouseButton(0));

        MathSpring.CalculateSpringParams(ref springParam, Time.deltaTime, strength, damper);

        float curVal = currentValue.Value;
        MathSpring.UpdateSpring(ref curVal, ref springVelocity, targetValue, ref springParam);
        currentValue.Value = curVal;
    }

    private void SetButtonPressed(bool isPressed)
    {
        targetValue = isPressed ? 1 : 0;
    }

    private void CurrentValue_OnValueChanged(float priviouse, float newValue)
    {
        foreach (var effect in springEffects)
        {
            effect.OnSpringUpdate(newValue, springVelocity);
        }
    }

    //private void OnDestroy()
    //{
    //    StopAllCoroutines();
    //}

    //private IEnumerator ExcuteNedgleLoop()
    //{
    //    while (true)
    //    {
    //        yield return new WaitForSeconds(nudgleDelay);

    //        scaleVelocity += nudgleStrength;
    //    }
    //}
}

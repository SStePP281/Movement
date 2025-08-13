using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MySliderUI : MonoBehaviour
{
    [Header("Teke")]
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI currentValue;
    [SerializeField] private TextMeshProUGUI minValue;
    [SerializeField] private TextMeshProUGUI maxValue;
    [SerializeField] private TextMeshProUGUI sliderName;

    [Header("Set")]
    [SerializeField] private string sliderNameText;

    private void Awake()
    {
        slider.value = (slider.maxValue + slider.minValue) / 2.0f;
        Slider_OnValueChanged(slider.value);

        minValue.text = slider.minValue.ToString();
        maxValue.text = slider.maxValue.ToString();
        sliderName.text = sliderNameText;
    }

    private void Start()
    {
        slider.onValueChanged.AddListener(Slider_OnValueChanged);
    }

    private void Slider_OnValueChanged(float newValue)
    {
        currentValue.text = ((int)(newValue * 100) / 100.0f).ToString();
    }

    public void SetValue(float newValue)
    {
        slider.value = Mathf.Clamp(newValue, slider.minValue, slider.maxValue);
    }
}

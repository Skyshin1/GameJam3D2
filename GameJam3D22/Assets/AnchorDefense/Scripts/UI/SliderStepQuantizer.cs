using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Slider))]
    public sealed class SliderStepQuantizer : MonoBehaviour
    {
        [SerializeField, Min(1)] private int intervalCount = 10;

        private Slider slider;

        public int IntervalCount => intervalCount;

        public void Configure(int intervals)
        {
            intervalCount = Mathf.Max(1, intervals);
        }

        public void SetValueWithoutNotify(float value)
        {
            EnsureSlider();
            slider.SetValueWithoutNotify(Quantize(value));
        }

        private void Awake()
        {
            EnsureSlider();
            slider.onValueChanged.AddListener(HandleValueChanged);
            slider.SetValueWithoutNotify(Quantize(slider.value));
        }

        private void OnDestroy()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(HandleValueChanged);
            }
        }

        private void HandleValueChanged(float value)
        {
            float quantized = Quantize(value);
            if (!Mathf.Approximately(value, quantized))
            {
                slider.SetValueWithoutNotify(quantized);
            }
        }

        private float Quantize(float value)
        {
            float normalized = Mathf.InverseLerp(slider.minValue, slider.maxValue, value);
            float stepped = Mathf.Round(normalized * intervalCount) / intervalCount;
            return Mathf.Lerp(slider.minValue, slider.maxValue, stepped);
        }

        private void EnsureSlider()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }
        }
    }
}

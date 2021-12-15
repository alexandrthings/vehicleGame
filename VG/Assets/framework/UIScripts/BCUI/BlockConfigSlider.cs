using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace VehicleBase.UI
{
    public class BlockConfigSlider : BlockConfigElement
    {
        public TextMeshProUGUI lowText;
        public TextMeshProUGUI highText;

        public TMP_InputField sliderInput;

        public Slider slider;

        private float prevValue;
        private string prevInput;
        private bool IntOrBool;

        void Update()
        {
            if (!Mathf.Approximately(slider.value, prevValue)) // clunky way to check
                SetValue(slider.value);

            if (sliderInput.text != prevInput)
                if (float.TryParse(sliderInput.text, out float number))
                    SetValue(number);

            prevValue = slider.value;
            prevInput = sliderInput.text;
        }

        public override void SetupProperty(string name, bool intOrBool, float min, float max, float currentValue)
        {
            IntOrBool = intOrBool;

            base.SetupProperty(name, intOrBool, min, max, currentValue);
            lowText.text = System.Math.Round(min, 1).ToString();
            highText.text = System.Math.Round(max, 1).ToString();

            slider.wholeNumbers = intOrBool;
            sliderInput.contentType = intOrBool ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;

            slider.minValue = min;
            slider.maxValue = max;

            slider.value = currentValue;
            sliderInput.text = currentValue.ToString();

            enabled = true;
        }

        public override void SetValue(float value)
        {
            value = ClampValue(value);

            if (IntOrBool)
                value = Mathf.RoundToInt(value);

            slider.value = value;
            sliderInput.text = System.Math.Round(value, 1).ToString();

            BlockConfigUI.SetProperty(targetVariable, value);
        }
    }
}
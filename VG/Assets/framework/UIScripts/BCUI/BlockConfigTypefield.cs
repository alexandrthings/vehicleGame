using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VehicleBase.UI
{
    public class BlockConfigTypefield : BlockConfigElement
    {
        public TMP_InputField typefield;
        public string prevInput;

        public void Update()
        {
            if (typefield.text != prevInput)
                if (float.TryParse(typefield.text, out float number))
                    SetValue(number);

            prevInput = typefield.text;
        }

        public override void SetupProperty(string name, bool intOrBool, float min, float max, float currentValue)
        {
            base.SetupProperty(name, intOrBool, min, max, currentValue);

            targetVariable = name;
            typefield.text = currentValue.ToString();
            typefield.contentType = intOrBool ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;

            enabled = true;
        }

        public override void SetValue(float value)
        {
            value = ClampValue(value);

            typefield.text = value.ToString();

            BlockConfigUI.SetProperty(targetVariable, value);
        }
    }
}
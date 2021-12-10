using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ASTankGame.UI
{
    public class BlockConfigElement : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public string targetVariable = "var";

        public float Min = -999999999;
        public float Max = 9999999999;

        public virtual void SetupProperty(string name, bool intOrBool, float min, float max, float currentValue)
        {
            targetVariable = name;
            titleText.text = name;
            Min = min;
            Max = max;
        }

        public float ClampValue(float value)
        {
            if (value > Max)
                return Max;
            if (value < Min)
                return Min;

            return value;
        }

        public virtual void SetValue(float value)
        {

        }
    }
}
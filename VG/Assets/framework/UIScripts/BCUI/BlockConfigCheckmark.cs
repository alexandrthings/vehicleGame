using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.UI
{
    public class BlockConfigCheckmark : BlockConfigElement
    {
        public GameObject onObject;

        private bool checkOn;

        public void Toggle()
        {
            SetValue(checkOn ? 1 : 0);
        }

        public override void SetupProperty(string name, bool intOrBool, float min, float max, float currentValue)
        {
            targetVariable = name;
            titleText.text = targetVariable;
            checkOn = Mathf.Approximately(currentValue, 1);

            onObject.SetActive(checkOn);
        }

        public override void SetValue(float value)
        {
            checkOn = !checkOn;
            BlockConfigUI.SetProperty(targetVariable, checkOn ? 1 : 0);
            onObject.SetActive(checkOn);
        }
    }
}
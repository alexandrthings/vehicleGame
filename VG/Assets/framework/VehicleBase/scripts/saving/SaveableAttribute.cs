using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Vehicles.XML
{
    /// <summary>
    /// Place on any int, bool, or float field to make it an editable property in Block Editor.
    /// This will automatically save its value in the vehicle's XML file. 
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ConfigurableSettingAttribute : System.Attribute
    {
        public CUIType cuiType;
        public float minLimit = -9999999999f;
        public float maxLimit = 9999999999f;
        public string minOverride = "null";
        public string maxOverride = "null";

        public ConfigurableSettingAttribute(CUIType ctype)
        {
            cuiType = ctype;
        }

        public ConfigurableSettingAttribute(CUIType ctype, float min, float max)
        {
            cuiType = ctype;
            minLimit = min;
            maxLimit = max;
        }

        /// <summary>
        /// overload with optional override field names
        /// </summary>
        /// <param name="ctype">which ui element to display</param>
        /// <param name="min">min value possible</param>
        /// <param name="max">max value possible</param>
        /// <param name="minOverrideName">leave blank if no field override</param>
        /// <param name="maxOverrideName">leave blank if no field override</param>
        public ConfigurableSettingAttribute(CUIType ctype, float min, float max, string minOverrideName, string maxOverrideName)
        {
            cuiType = ctype;
            minLimit = min;
            maxLimit = max;
            if (minOverrideName != "")
                minOverride = minOverrideName;
            if (maxOverrideName != "")
                maxOverride = maxOverrideName;
        }
    }

    public enum CUIType
    {
        Typefield, 
        Checkbox,
        Slider,
        FireGroup
    }
}
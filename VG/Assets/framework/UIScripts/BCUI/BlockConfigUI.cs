using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using ASTankGame.Vehicles.BlockBehaviors;
using ASTankGame.Vehicles.XML;
using TMPro;
using UnityEngine;

namespace ASTankGame.UI
{
    public class BlockConfigUI : Menu
    {
        public static BlockConfigUI instance;

        public TextMeshProUGUI BlockNameText;
        public Transform prefabSpawnPoint;

        public GameObject typefieldPrefab;
        public GameObject sliderPrefab;
        public GameObject checkmarkPrefab;
        public GameObject fgPrefab;

        public GOBehavior target;

        private List<BlockConfigElement> blockPropertyObjects = new List<BlockConfigElement>();

        private string[] propertyNames;
        private float[] propertyValues;
        private FieldInfo[] propertyAttributes;
        private List<ConfigurableSettingAttribute> castPropertyAttributes = new List<ConfigurableSettingAttribute>();

        public void Start()
        {
            if (instance == null)
                instance = this;

            typefieldPrefab.SetActive(false);
            sliderPrefab.SetActive(false);
            checkmarkPrefab.SetActive(false);
        }

        public void SetTargetLocal(GOBehavior tgt)
        {
            if (tgt == null)
                return;

            foreach (BlockConfigElement bce in blockPropertyObjects)
                Destroy(bce.gameObject);

            blockPropertyObjects.Clear();

            target = tgt;
            BlockNameText.text = target.name + " Configuration";
            propertyAttributes = GOBehavior.GetConfigurableSettingAttributes(target);
            GOBehavior.GetConfigurableData(target, out propertyValues, out propertyNames);
            castPropertyAttributes.Clear();

            Vector3 spawnPos = prefabSpawnPoint.position;

            for (int i = 0; i < propertyAttributes.Length; i++)
            {
                castPropertyAttributes.Add(System.Attribute.GetCustomAttribute(propertyAttributes[i], typeof(ConfigurableSettingAttribute)) as ConfigurableSettingAttribute);

                GameObject gobject = gameObject; // set to this object for compiler to not bitch at me

                switch (castPropertyAttributes[i].cuiType)
                {
                    case CUIType.Typefield:
                        gobject = (GameObject) Object.Instantiate(typefieldPrefab, spawnPos, Quaternion.identity,
                            prefabSpawnPoint.transform);
                        break;
                    case CUIType.Checkbox:
                        gobject = (GameObject) Object.Instantiate(checkmarkPrefab, spawnPos, Quaternion.identity,
                            prefabSpawnPoint.transform);
                        break;
                    case CUIType.Slider:
                        gobject = (GameObject) Object.Instantiate(sliderPrefab, spawnPos, Quaternion.identity,
                            prefabSpawnPoint.transform);
                        break;
                    case CUIType.FireGroup:

                        break;
                }

                spawnPos += Vector3.down * 100;

                gobject.SetActive(true);

                BlockConfigElement bce = gobject.GetComponent<BlockConfigElement>();
                blockPropertyObjects.Add(bce);

                if (bce == null)
                {
                    Debug.LogError("finding blockConfigElement went terribly wrong.");
                    return;
                }

                float value = 0;
                bool intOrBool = propertyAttributes[i].FieldType != typeof(float);

                if (propertyAttributes[i].FieldType == typeof(float))
                    value = (float) propertyAttributes[i].GetValue(target);
                else if (propertyAttributes[i].FieldType == typeof(int))
                    value = (int) propertyAttributes[i].GetValue(target);
                else if (propertyAttributes[i].FieldType == typeof(bool))
                    value = (bool) propertyAttributes[i].GetValue(target) ? 1 : 0;

                float min = castPropertyAttributes[i].minLimit;
                float max = castPropertyAttributes[i].maxLimit;

                if (castPropertyAttributes[i].maxOverride != "null" || castPropertyAttributes[i].minOverride != "null") // find override field value
                {
                    FieldInfo minFieldInfo = GOBehavior.GetFieldWithName(castPropertyAttributes[i].minOverride, target);
                    FieldInfo maxFieldInfo = GOBehavior.GetFieldWithName(castPropertyAttributes[i].maxOverride, target);
                    if (minFieldInfo != null)
                    {
                        object minVal = minFieldInfo.GetValue(target);
                        if (minVal is float)
                            min = (float) minVal;
                        else if (minVal is int)
                            min = (int) minVal;
                        else 
                            Debug.LogWarning($"Override Object {castPropertyAttributes[i].minOverride} is not a float or int!");
                    }

                    if (maxFieldInfo != null)
                    {
                        object maxVal = maxFieldInfo.GetValue(target);
                        if (maxVal is float)
                            max = (float)maxVal;
                        else if (maxVal is int)
                            min = (int)maxVal;
                        else
                            Debug.LogWarning($"Override Object {castPropertyAttributes[i].minOverride} is not a float or int!");
                    }
                }

                bce.SetupProperty(propertyAttributes[i].Name, intOrBool, min, max, value);
            }

            UIManager.ActivateMenu(menuName);
        }

        public static void SetProperty(string propName, float value)
        {
            GOBehavior.SetConfigurableData(instance.target, propName, value);
        }

        public override void OnDeactivate()
        {
            foreach (BlockConfigElement bce in blockPropertyObjects)
                Destroy(bce.gameObject);

            blockPropertyObjects.Clear();

            target = null;

            castPropertyAttributes.Clear();
        }

        public static void SetTarget(GOBehavior tgt)
        {
            instance.SetTargetLocal(tgt);
        }
    }
}
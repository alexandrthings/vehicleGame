using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ASTankGame.Vehicles;
using ASTankGame.Vehicles.XML;
using Unity.VisualScripting;
using UnityEngine;

namespace ASTankGame.Vehicles.BlockBehaviors
{
    /// <summary>
    /// be sure to base.onEnable and .onDisable if editing those
    /// </summary>
    public class GOBehavior : MonoBehaviour
    {
        public Vehicle vehicle;

        [Header("Parameters")]
        public float lowScaleLimit = 0f;
        public float highScaleLimit = 0.25f;

        protected Vector3Int localPos;
        public int fwd { get; private set; }
        public int up { get; private set; }

        public Vector3Int[] BlockLinks { get { return blockLinks; } }
        public Vector3Int mirrorOffset = Vector3Int.zero;

        /// <summary>
        /// LINK MUST BE WITHIN 128 BLOCKS OF ORIGIN ON ALL AXIS DUE TO SBYTE LIMITATIONS
        /// </summary>
        [SerializeField] private Vector3Int[] blockLinks;

        public virtual void OnEnable()
        {
            if (transform.parent.name.Contains("DISABLE"))
            {
                this.enabled = false;
                return;
            }
            else
                this.enabled = true;   

            if (vehicle == null)
                GetParentVehicle();

            vehicle.ReadInterfacedBehavior(this, true);
        }

        public virtual void OnDisable()
        {
            if (vehicle != null)
                vehicle.ReadInterfacedBehavior(this, false);
        }

        public void SetOrientation(Vector3Int pos, int f, int u)
        {
            localPos = pos;
            fwd = f;
            up = u;
        }

        public virtual void OnExplode() // call if block destroyed
        {

        }

        public void GetParentVehicle()
        {
            Transform currentlyChecked = transform;

            while (currentlyChecked != null)
            {
                if (currentlyChecked.TryGetComponent(out vehicle))
                    return;

                currentlyChecked = currentlyChecked.parent;
            }
        }

        public void CalculateMirrorOffset()
        {
            int maxX = 0;
            int minX = 0;

            for (int i = 0; i < blockLinks.Length; i++)
            {
                if (blockLinks[i].x > maxX)
                    maxX = blockLinks[i].x;
                if (blockLinks[i].x < minX)
                    minX = blockLinks[i].x;
            }

            mirrorOffset = new Vector3Int(-(maxX + minX), 0, 0);
        }

        // get values for configurable, saveable data
        public static void GetConfigurableData(GOBehavior beh, out float[] data, out string[] namesData)
        {
            var fieldInfo = beh.GetType().GetFields();

            List<float> dataValues = new List<float>();
            List<string> dataNames = new List<string>();

            foreach (var item in fieldInfo)
            {
                var attribute = System.Attribute.GetCustomAttribute(item, typeof(ConfigurableSettingAttribute));

                if (attribute != null)
                {
                    if (item.GetValue(beh) is bool)
                    {
                        bool c = (bool)item.GetValue(beh);
                        dataValues.Add(c ? 1 : 0);
                        dataNames.Add(item.Name);
                    }
                    else if (item.GetValue(beh) is float)
                    {
                        float c = (float)item.GetValue(beh);
                        dataValues.Add(c);
                        dataNames.Add(item.Name);
                    }
                    else if (item.GetValue(beh) is int)
                    {
                        int c = (int)item.GetValue(beh);
                        dataValues.Add(c);
                        dataNames.Add(item.Name);
                    }
                    else
                    {
                        Debug.LogError($"ConfigSetting attribute used incorrectly at {item.Name}");
                    }
                }
            }

            data = dataValues.ToArray();
            namesData = dataNames.ToArray();
        }

        public static void SetConfigurableData(GOBehavior beh, string name, float value)
        {
            var fieldInfo = beh.GetType().GetFields();

            foreach (var item in fieldInfo)
            {
                var attribute = System.Attribute.GetCustomAttribute(item, typeof(ConfigurableSettingAttribute));

                if (attribute != null)
                {
                    if (item.Name != name)
                        continue;

                    if (item.FieldType == typeof(float))
                    {
                        item.SetValue(beh, value);
                    }
                    else if (item.FieldType == typeof(int))
                    {
                        item.SetValue(beh, (int)value);
                    }
                    else if (item.FieldType == typeof(bool))
                    {
                        item.SetValue(beh, value > 0);
                    }

                }
            }

            beh.OnConfigUpdate();
        }

        public static FieldInfo[] GetConfigurableSettingAttributes(GOBehavior obj)
        {
            var fieldInfo = obj.GetType().GetFields();
            List<FieldInfo> attrs = new List<FieldInfo>();

            foreach (var item in fieldInfo)
            {
                var attribute = System.Attribute.GetCustomAttribute(item, typeof(ConfigurableSettingAttribute));

                if (attribute != null)
                {
                    if (item.GetValue(obj) is bool || item.GetValue(obj) is float || item.GetValue(obj) is int)
                    {
                        attrs.Add(item);
                    }
                    else
                    {
                        Debug.LogError($"ConfigSetting attribute used incorrectly at {item.Name}");
                    }
                }
            }

            return attrs.ToArray();
        }

        public static FieldInfo GetFieldWithName(string name, GOBehavior target)
        {
            if (name == "null" || name == "")
                return null;

            var fieldInfo = target.GetType().GetFields();

            foreach (var item in fieldInfo)
            {
                if (item.Name == name)
                    return item;
            }

            return null;
        }

        public virtual void OnConfigUpdate()
        {

        }
#if UNITY_EDITOR
        public virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;

            if (blockLinks != null)
            foreach (Vector3Int pos in blockLinks)
            {
                Gizmos.DrawWireSphere(transform.position + transform.forward * pos.z * transform.localScale.x + transform.right * pos.x * transform.localScale.x + transform.up * pos.y * transform.localScale.x, 0.2f * transform.localScale.x);
            }
        }
        #endif
    }
}
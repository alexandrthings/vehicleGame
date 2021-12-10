using System;
using System.Collections;
using System.Collections.Generic;
using MiscUtil.Collections.Extensions;
using ASTankGame.Vehicles;
using ASTankGame.Vehicles.BlockBehaviors;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ASTankGame.UI
{
    /// <summary>
    /// actively updated UI like hp, fuel, electricity of vehicle, etc.
    /// </summary>
    public class ActiveUI : MonoBehaviour
    {
        public static ActiveUI instance;

        [Header("UI variants")]
        [SerializeField] private RectTransform vehicleUI;
        [SerializeField] private RectTransform buildUI;
        [SerializeField] private RectTransform characterUI;

        [Header("Firegroups")]
        [SerializeField] private GameObject fgPrefab;
        private List<GameObject> fgs = new List<GameObject>();
        [SerializeField] private float fgOffsetDistance;

        [Header("Vehicle fields")]
        [SerializeField] private RectTransform ammoRot;
        [SerializeField] private RectTransform elecRot;
        [SerializeField] private RectTransform motorRot;
        [SerializeField] private RectTransform fuelRot;

        [SerializeField] private TextMeshProUGUI fuelText;
        [SerializeField] private TextMeshProUGUI infoText;

        [SerializeField] private float rotAngle = -82.55f;

        private Vehicle boundVehicle;

        private UIState state;

        void Start()
        {
            if (instance != null && instance != this) Destroy(this);
            else instance = this;

            ChangeUITo(UIState.Character);
        }

        void Update()
        {
            //if (UIManager.GetActiveCharacter().v)
        }

        void FixedUpdate()
        {
            switch (state)
            {
                case UIState.Vehicle:
                    float percent = boundVehicle.VResources.GetAmmoPercent();
                    ammoRot.eulerAngles = new Vector3(0, 0, Mathf.Clamp((1-percent), 0, 2) * rotAngle);

                    percent = boundVehicle.VResources.GetEnergyPercent();
                    elecRot.eulerAngles = new Vector3(0, 0, Mathf.Clamp((1 - percent), 0, 2) * rotAngle);

                    percent = boundVehicle.VResources.GetEnginePercent();
                    motorRot.eulerAngles = new Vector3(0, 0, Mathf.Clamp((1 - percent), 0, 2) * rotAngle);

                    percent = boundVehicle.VResources.GetFuelPercent();
                    fuelRot.eulerAngles = new Vector3(0, 0, (1 - percent) * -90);

                    fuelText.text = Math.Round(percent * 100, 1).ToString() + " %";

                    infoText.text = "THR: " + Math.Round(boundVehicle.Throttle * 100, 1) + "%\n"
                                    + "SPD: " + Math.Round(boundVehicle.RB.velocity.magnitude * 3.6, 1) + " kph\n"
                                    + "ALT: " + Math.Round(boundVehicle.transform.position.y * 3.6) + " m";
                    break;
                case UIState.Build:

                    break;
                default:

                    break;
            }
        }

        public static void BindVehicle(Vehicle toBind)
        {
            instance.boundVehicle = toBind;
            instance.SetupVehicleUI();
        }

        public void SetupVehicleUI()
        {
            if (boundVehicle == null)
                return;

            for (int i = 0; i < fgs.Count; i++) // clear out old fire groups
                Destroy(fgs[i]);

            fgs.Clear();

            for (int i = 0; i < 10; i++) // instantiate fg slots as needed
            {
                if (boundVehicle.FGs[i].isEmpty) continue;

                fgs.Add((GameObject)Object.Instantiate(fgPrefab, fgPrefab.transform.parent));

                fgs[fgs.Count - 1].GetComponent<FGUIScript>().SetText(i, boundVehicle.FGs[i].boundWeapons.Count);
            }

            int offsetCounter = - fgs.Count + fgs.Count/2;
            for (int i = 0; i < fgs.Count; i++) // finally, position fg ui
            {
                fgs[i].transform.position += Vector3.right * offsetCounter * fgOffsetDistance;
            }
        }

        public static void ChangeUITo(UIState _state)
        {
            instance.vehicleUI.gameObject.SetActive(false);
            instance.buildUI.gameObject.SetActive(false);
            instance.state = _state;

            switch (_state)
            {
                case UIState.Vehicle:
                if (instance.boundVehicle != null)
                    instance.vehicleUI.gameObject.SetActive(true);
                break;
                case UIState.Build:
                    instance.buildUI.gameObject.SetActive(true);
                    break;
            }
        }
    }

    public enum UIState
    {
        Vehicle,
        Character,
        Build
    }
}
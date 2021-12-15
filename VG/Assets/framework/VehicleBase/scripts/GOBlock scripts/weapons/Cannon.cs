using System.Collections;
using System.Collections.Generic;
using VehicleBase.Damage;
using VehicleBase.Utility;
using VehicleBase.Vehicles.Ammo;
using VehicleBase.Vehicles.XML;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors.Weapons
{
    public class Cannon : GOBehavior, IWeapon
    {
        public bool isTurret { get { return false; } }
        public bool controlTurrets { get { return ControlTurrets; } }
        [SerializeField] private bool ControlTurrets;

        private ITargetable horizontalGuidance;
        private ITargetable verticalGuidance;

        [ConfigurableSetting(CUIType.Typefield, 0, 4)]
        public int ammoType = 0;

        [ConfigurableSetting(CUIType.Slider, 1000, 30000)]
        public float barrelLengthMM = 5000f;

        [SerializeField]
        private float maxDiameterMM = 3000f;
        [ConfigurableSetting(CUIType.Slider, 10, 500, "", "maxDiameterMM")]
        public float diameterMM = 100f;

        private float velocity = 100;
        private float optimumPropellantBurnLength = 5000f;
        private const float VelocityPerMM = 0.005f;
        private const float DiameterToLengthMultiplier = 0.001f;

        private float recoilForce = 1000;
        private const float velocityToRecoilMultiplier = 1.2f;

        private float reloadTime = 10f;
        private const float diameterToReloadMultiplier = 0.1f;

        [SerializeField] private Transform barrel;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject particles;
        public Object shellPrefab;

        public void Start()
        {
            if (transform.parent.name.Contains("DISABLE"))
                return;

            // find weapon guidance
            Vehicle current = vehicle;
            while (current != null)
            {
                if (horizontalGuidance != null && verticalGuidance != null)
                    break;

                if (current.IsTurret)
                {
                    if (current.attachmentPoints.Count > 0 && current.attachmentPoints[0].GetType() == typeof(TurretRing) && (current.attachmentPoints[0] as TurretRing).isTurret)
                    {
                        if ((current.attachmentPoints[0] as TurretRing).horizontalGuidance)
                        {
                            if (horizontalGuidance == null)
                            {
                                horizontalGuidance = current.attachmentPoints[0] as ITargetable;
                            }
                        }
                        else
                        {
                            if (verticalGuidance == null)
                            {
                                verticalGuidance = current.attachmentPoints[0] as ITargetable;
                            }
                        }
                    }
                }
                current = current.parentVehicle;
            }

            SelectAmmo(ammoType);
        }

        public bool Fire()
        {
            GameObject shellFired = (GameObject) Object.Instantiate(shellPrefab, firePoint.position, firePoint.rotation);

            if (shellFired.TryGetComponent(out Shell shell))
                shell.SetupShell(diameterMM, barrelLengthMM * VelocityPerMM, 10);

            vehicle.RB.AddForceAtPosition(-transform.forward * recoilForce, firePoint.position);
            
            particles.SetActive(false);
            particles.SetActive(true);
            return true;
        }

        public float GetGuidanceMisalignment()
        {
            return 0;
        }

        public void SelectAmmo(int type)
        {
            shellPrefab = GlobalAmmoManager.GetAmmoPrefab(type);
        }

        public void SetTarget(Vector3 target)
        {
            if (horizontalGuidance != null)
                horizontalGuidance.SetTarget(target);

            if (verticalGuidance != null)
                verticalGuidance.SetTarget(target);
        }

        public override void OnConfigUpdate()
        {
            barrel.localScale = new Vector3(diameterMM/maxDiameterMM, diameterMM/maxDiameterMM, barrelLengthMM/ (250/vehicle.Scale));
            SelectAmmo(ammoType);

            optimumPropellantBurnLength = diameterMM * DiameterToLengthMultiplier;
            recoilForce = barrelLengthMM * VelocityPerMM * diameterMM * velocityToRecoilMultiplier;

            particles.transform.position = firePoint.position;
        }
    }
}
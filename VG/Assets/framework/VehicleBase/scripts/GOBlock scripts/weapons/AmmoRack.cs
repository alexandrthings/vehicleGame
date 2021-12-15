using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public class AmmoRack : GOBehavior, IAmmoRack
    {
        public float maxAmmo { get { return MaxAmmo; } }
        private float MaxAmmo = 100;

        public float currentAmmo { get { return CurrentAmmo; } }
        private float CurrentAmmo = 100;

        public override void OnEnable()
        {
            base.OnEnable();

            vehicle.VResources.ReadAmmoRack(this, true);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            vehicle.VResources.ReadAmmoRack(this, false);
        }
    }
}
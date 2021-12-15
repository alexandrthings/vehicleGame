using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public class FuelTank : GOBehavior, IFuelTank
    {
        public float maxCapacity { get { return maxFuelCapacity; } }
        [SerializeField] private float maxFuelCapacity = 0;

        public float fuel { get { return fuelInTank; } }
        [SerializeField] private float fuelInTank = 0;

        public float Refuel(float amount)
        {
            if (amount + fuelInTank > maxFuelCapacity)
            {
                float spent = maxFuelCapacity - fuelInTank;
                fuelInTank = maxFuelCapacity;
                return spent;
            }

            fuelInTank += amount;

            return 0;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public interface IFuelTank
    {
        float maxCapacity { get; }
        float fuel { get; }
        float Refuel(float amount);
    }
}
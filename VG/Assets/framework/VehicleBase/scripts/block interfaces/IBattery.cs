using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public interface IBattery
    {
        float storageCapacity { get; }
    }
}
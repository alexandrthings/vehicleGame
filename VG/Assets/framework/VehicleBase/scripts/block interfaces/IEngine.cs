using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public interface IEngine
    {
        float maxPower { get; }
        float fuelConsumption { get; }
        void SetUsage(float usagePercent);
    }
}
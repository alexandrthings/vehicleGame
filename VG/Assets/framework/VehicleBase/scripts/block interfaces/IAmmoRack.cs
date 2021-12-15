using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public interface IAmmoRack
    {
        float maxAmmo { get; }
        float currentAmmo { get; }
    }
}
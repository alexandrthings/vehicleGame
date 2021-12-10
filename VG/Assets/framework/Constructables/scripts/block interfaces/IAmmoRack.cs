using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Vehicles.BlockBehaviors
{
    public interface IAmmoRack
    {
        float maxAmmo { get; }
        float currentAmmo { get; }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Vehicles.BlockBehaviors
{
    public interface IEnergyGenerator : IBattery
    {
        float energyPerSecond { get; }
    }
}
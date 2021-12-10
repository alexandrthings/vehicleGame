using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Vehicles.BlockBehaviors
{
    public interface IPoweredPart
    {
        float maxPowerUsage { get; }
        float powerPerSecond { get; }
        void Run(float efficiency);
    }
}
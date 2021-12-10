using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Vehicles.BlockBehaviors
{
    public interface IElectricalPart
    {
        float energyUsePerSecond { get; }
        void RunElectric(float efficiency);
    }
}
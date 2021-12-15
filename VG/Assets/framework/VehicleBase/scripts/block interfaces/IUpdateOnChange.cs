using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    /// <summary>
    /// For behaviors that update on block updates
    /// </summary>
    public interface IUpdateOnChange
    {
        void Updated();
    }
}
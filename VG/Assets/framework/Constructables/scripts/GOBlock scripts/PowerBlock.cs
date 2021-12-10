using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Vehicles.BlockBehaviors
{
    public class PowerBlock : GOBehavior, IPoweredPart
    {
        public float maxPowerUsage {
            get { return MaxPowerUsage; }
        } 
        [SerializeField] protected float MaxPowerUsage = 100;

        public float powerPerSecond
        {
            get { return currentPowerUsage; }
        }
        [SerializeField] protected float currentPowerUsage = 10;

        public virtual void Run(float efficiency)
        {

        }
    }
}
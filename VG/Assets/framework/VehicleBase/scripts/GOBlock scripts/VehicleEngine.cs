using System.Collections;
using System.Collections.Generic;
using VehicleBase.Vehicles.BlockBehaviors;
using UnityEngine;

public class VehicleEngine : GOBehavior, IEngine, IEnergyGenerator
{
    public float maxPower { get { return maxEnginePower; } }
    public float energyPerSecond { get { return maxAlternatorGeneration; } }
    public float storageCapacity { get { return maxBatteryStorage; } }
    public float fuelConsumption { get { return maxFuelConsumption; } }

    [SerializeField] private float maxEnginePower = 100;
    [SerializeField] private float maxAlternatorGeneration = 10;
    [SerializeField] private float maxBatteryStorage = 30;
    [SerializeField] private float maxFuelConsumption = 10;

    [SerializeField] private AudioSource engineSound;

    // centralized management
    public void SetUsage(float usage)
    {

    }
}

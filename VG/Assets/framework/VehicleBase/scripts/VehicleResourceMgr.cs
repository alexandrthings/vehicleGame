using System;
using System.Collections;
using System.Collections.Generic;
using VehicleBase.Vehicles.BlockBehaviors;
using UnityEngine;

namespace VehicleBase.Vehicles
{
    public class VehicleResourceMgr : MonoBehaviour
    {
        private Vehicle vehicle;

        #region resources and power
        private List<IAmmoRack> readyRacks = new List<IAmmoRack>();
        private float maxAmmo = 0;
        private float ammo = 0;

        private List<IEngine> engines = new List<IEngine>();
        private float maxEnginePower = 0;
        private float enginePowerLeft = 0;
        private float maxFuelConsumption = 0;

        private List<IFuelTank> fuelTanks = new List<IFuelTank>();
        private float fuel = 0f;
        private float maxFuel = 0f;

        private List<IEnergyGenerator> generators = new List<IEnergyGenerator>();
        private float energyPerSecond = 0;

        private List<IBattery> batteries = new List<IBattery>();
        private float maxBatteryStorage = 0;
        private float energyStored = 0;

        private List<IPoweredPart> poweredParts = new List<IPoweredPart>();
        private List<IElectricalPart> electricalParts = new List<IElectricalPart>();
        #endregion

        void OnEnable()
        {
            vehicle = transform.GetComponent<Vehicle>();
        }

        private void FixedUpdate()
        {
            UpdatePower();
        }

        private void Update()
        {
            UpdateEnergy();
        }

        #region frame updates
        private void UpdatePower()
        {
            if (fuel > 0)
                enginePowerLeft = maxEnginePower;
            else
            {
                enginePowerLeft = 0;
            }

            for (int i = 0; i < poweredParts.Count; i++)
            {
                poweredParts[i].Run(Mathf.Clamp01(enginePowerLeft/(poweredParts[i].powerPerSecond + 0.0000001f)));
                enginePowerLeft -= poweredParts[i].powerPerSecond;
            }

            fuel -= (1 - enginePowerLeft / (maxEnginePower + 0.00001f)) * maxFuelConsumption * Time.fixedDeltaTime;
            fuel = Mathf.Clamp(fuel, 0, maxFuel);
        }

        private void UpdateEnergy()
        {
            energyStored += energyPerSecond * Time.deltaTime;

            for (int i = 0; i < electricalParts.Count; i++)
            {
                electricalParts[i].RunElectric(Mathf.Clamp01(energyStored / (electricalParts[i].energyUsePerSecond + 0.0000001f) ) );
                energyStored -= electricalParts[i].energyUsePerSecond * Time.deltaTime;
            }
        }

        public float GetFuelPercent()
        {
            return fuel / (maxFuel + 0.000001f);
        }

        public float GetEnergyPercent()
        {
            return energyStored / (maxBatteryStorage + 0.00001f);
        }

        public float GetEnginePercent()
        {
            return enginePowerLeft / (maxEnginePower + 0.0001f);
        }

        public float GetAmmoPercent()
        {
            float[] ammoCurMax = AmmoGetRecursive();

            return ammoCurMax[0]/(ammoCurMax[1]+0.000000001f);
        }

        public float[] AmmoGetRecursive()
        {
            float[] ammoCurMax = new float[2];

            // add current
            ammoCurMax[0] += ammo;
            ammoCurMax[1] += maxAmmo;

            foreach (Vehicle child in vehicle.Subvehicles)
            {
                float[] childAmmo = child.VResources.AmmoGetRecursive();
                ammoCurMax[0] += childAmmo[0];
                ammoCurMax[1] += childAmmo[1];
            }

            return ammoCurMax;
        }
        #endregion

        #region resource use and transfer
        public float AddOrRemoveAmmo(float amt) // returns ammo amt transferred
        {
            if (amt >= 0) // if adding be sure to return excess
            {
                if (ammo + amt > maxAmmo)
                {
                    float left = maxAmmo - ammo;
                    ammo = maxAmmo;
                    return left;
                }
                else
                {
                    ammo += amt;
                    return amt;
                }
            }
            else // if removing be sure to not add more than there actually is
            {
                if (ammo - amt < 0)
                {
                    float left = ammo;
                    ammo = 0;
                    Debug.LogWarning($"Pulled {amt}, left {left}, ammo {ammo}");
                    return left;
                }
                else
                {
                    ammo -= amt;
                    return amt;
                }
            }
        }

        public bool UseAmmo(float amount)
        {
            if (ammo >= amount)
            {
                ammo -= amount;
                return true;
            }

            return false;
        }

        public void PullAmmo(VehicleResourceMgr destination, float amount)
        {
            ammo -= destination.AddOrRemoveAmmo(amount);
        }
        #endregion

        #region reading blocks
        public void ReadAmmoRack(MonoBehaviour behavior, bool add)
        {
            IAmmoRack rack = behavior as IAmmoRack;
            if (rack != null)
                if (add) AddAmmoRack(rack); else RemoveAmmoRack(rack);
        }

        // there's probably a better way to do this...
        public void ReadInterfacedBehavior(MonoBehaviour behavior, bool add)
        {
            IEngine engine = behavior as IEngine;
            if (engine != null)
                if (add) AddEngine(engine); else RemoveEngine(engine);

            IBattery battery = behavior as IBattery;
            if (battery != null)
                if (add) AddBattery(battery); else RemoveBattery(battery);

            IEnergyGenerator generator = behavior as IEnergyGenerator;
            if (generator != null)
                if (add) AddGenerator(generator); else RemoveGenerator(generator);

            IPoweredPart part = behavior as IPoweredPart;
            if (part != null)
                if (add) AddPowerUser(part);else RemovePowerUser(part);

            IElectricalPart electricalPart = behavior as IElectricalPart;
            if (electricalPart != null)
                if (add) AddElectricityUser(electricalPart); else RemoveElectricityUser(electricalPart);

            IFuelTank fuelTank = behavior as IFuelTank;
            if (fuelTank != null)
                if (add) AddFuelTank(fuelTank); else RemoveFuelTank(fuelTank);
        }

        public void AddEngine(IEngine engine)
        {
            engines.Add(engine);
            maxEnginePower += engine.maxPower;
            maxFuelConsumption += engine.fuelConsumption;
        }

        public void RemoveEngine(IEngine engine)
        {
            engines.Remove(engine);
            maxEnginePower -= engine.maxPower;
            maxFuelConsumption -= engine.fuelConsumption;
        }

        public void AddBattery(IBattery battery)
        {
            batteries.Add(battery);
            maxBatteryStorage += battery.storageCapacity;
        }

        public void RemoveBattery(IBattery battery)
        {
            batteries.Remove(battery);
            maxBatteryStorage -= battery.storageCapacity;
            energyStored -= battery.storageCapacity;
        }

        public void AddGenerator(IEnergyGenerator generator)
        {
            generators.Add(generator);
            energyPerSecond += generator.energyPerSecond;
        }

        public void RemoveGenerator(IEnergyGenerator generator)
        {
            generators.Remove(generator);
            energyPerSecond -= generator.energyPerSecond;
        }

        public void AddPowerUser(IPoweredPart part)
        {
            poweredParts.Add(part);
        }

        public void RemovePowerUser(IPoweredPart part)
        {
            poweredParts.Remove(part);
        }

        public void AddElectricityUser(IElectricalPart part)
        {
            electricalParts.Add(part);
        }

        public void RemoveElectricityUser(IElectricalPart part)
        {
            electricalParts.Remove(part);
        }

        public void AddFuelTank(IFuelTank tank)
        {
            fuelTanks.Add(tank);
            maxFuel += tank.maxCapacity;
            fuel += tank.fuel;
        }

        public void RemoveFuelTank(IFuelTank tank)
        {
            fuelTanks.Remove(tank);
            maxFuel -= tank.maxCapacity;
            fuel -= tank.fuel;
        }

        public void AddAmmoRack(IAmmoRack rack)
        {
            readyRacks.Add(rack);
            maxAmmo += rack.maxAmmo;
        }

        public void RemoveAmmoRack(IAmmoRack rack)
        {
            readyRacks.Remove(rack);
            maxAmmo -= rack.maxAmmo;
        }
        #endregion
    }
}

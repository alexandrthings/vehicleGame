using System.Collections;
using System.Collections.Generic;
using VehicleBase.Vehicles.BlockBehaviors.Weapons;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public class FireGroup
    {
        public Vector3 Target;
        public List<IWeapon> boundWeapons = new List<IWeapon>();
        public bool isEmpty { get { return boundWeapons.Count == 0; } }

        private int id;

        public void AddWeapon(IWeapon weapon)
        {
            boundWeapons.Add(weapon);
        }

        public void RemoveWeapon(IWeapon weapon)
        {
            if (boundWeapons.Contains(weapon))
                boundWeapons.Remove(weapon);
        }

        public void Fire()
        {
            for (int i = 0; i < boundWeapons.Count; i++)
            {
                boundWeapons[i].Fire();
            }
        }

        public void SetTarget(Vector3 target)
        {
            for (int i = 0; i < boundWeapons.Count; i++)
            {
                boundWeapons[i].SetTarget(target);
            }
        }
    }
}
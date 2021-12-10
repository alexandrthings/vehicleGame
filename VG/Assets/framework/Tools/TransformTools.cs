using System.Collections;
using System.Collections.Generic;
using ASTankGame.Vehicles;
using ASTankGame.Vehicles.BlockBehaviors;
using UnityEngine;

namespace ASTankGame.Utility
{
    public static class TransformTools
    {
        public static Transform GetHeirarchyParent(Transform origin)
        {
            Transform currentTransform = origin;
            while (currentTransform.parent != null)
            {
                currentTransform = currentTransform.parent;
            }

            return currentTransform;
        }

        public static Vehicle GetMasterVehicle(Vehicle origin)
        {
            if (origin.parentVehicle == null)
                return origin;

            return origin.parentVehicle.masterVehicle;
            /*
            Vehicle current = origin;
            if (origin.parentVehicle == null || origin == origin.parentVehicle)
                return current;

            while (current.parentVehicle != null)
            {
                current = origin.parentVehicle;
            }

            return current;*/
        }

        public static Vehicle GetParentVehicle(Transform origin)
        {
            Vehicle result;
            Transform currentTransform = origin;
            while (currentTransform != null)
            {
                if (currentTransform.TryGetComponent<Vehicle>(out result))
                    return result;

                currentTransform = currentTransform.parent;
            }

            return null;
        }

        public static Vehicle GetParentVehicleECT(Transform origin)
        {
            Vehicle result;
            Transform currentTransform = origin.parent;
            while (currentTransform != null)
            {
                if (currentTransform.TryGetComponent<Vehicle>(out result))
                    return result;

                currentTransform = currentTransform.parent;
            }

            return null;
        }

        public static bool TryGetGOComponent(Transform origin, out GOBehavior component)
        {
            Transform currentTarget = origin;
            while (currentTarget.parent != null)
            {
                if (currentTarget.TryGetComponent<GOBehavior>(out component))
                {
                    return true;
                }

                currentTarget = currentTarget.parent;
            }

            component = null;
            return false;
        }

        public static bool TryGetSubComponent(Transform origin, out SubObjectComponent component)
        {
            Transform currentTarget = origin;
            while (currentTarget.parent != null)
            {
                if (currentTarget.TryGetComponent<SubObjectComponent>(out component))
                {
                    return true;
                }

                currentTarget = currentTarget.parent;
            }

            component = null;
            return false;
        }
    }
}
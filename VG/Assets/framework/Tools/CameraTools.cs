using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using ASTankGame.Utility;
using ASTankGame.Vehicles;
using ASTankGame.Vehicles.BlockBehaviors;
using UnityEngine;

public class CameraTools : MonoBehaviour
{
    public static CameraTools instance;

    public LayerMask vehicleMask;
    public LayerMask defaultMask;

    public Vector3 targetPos;

    public void Start()
    {
        instance = this;
    }

    public void Update()
    {
        var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000, defaultMask))
        {
            targetPos = hit.point;
        }
        else
        {
            targetPos = Camera.main.transform.position + Camera.main.transform.forward * 1000;
        }
    }

    public static bool VehicleCurrentlyLookedAt(float distance, out Vehicle vehicl)
    {
        var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        vehicl = null;

        if (Physics.Raycast(ray, out hit, distance, instance.vehicleMask))
        {
            vehicl = TransformTools.GetMasterVehicle(TransformTools.GetParentVehicleECT(hit.transform));
            return true;
        }

        return false;
    }

    public static bool GBCurrentlyLookedAt(float distance, out GOBehavior gb)
    {
        var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        gb = null;

        if (Physics.Raycast(ray, out hit, distance, instance.vehicleMask))
        {
            TransformTools.TryGetGOComponent(hit.collider.transform, out gb);
            return true;
        }

        return false;
    }
}

using System.Collections;
using System.Collections.Generic;
using VehicleBase.Utility;
using VehicleBase.Vehicles;
using VehicleBase.Vehicles.Blocks;
using VehicleBase.Vehicles.XML;
using Unity.VisualScripting;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    /// <summary>
    /// A component made for subobject blocks
    /// </summary>
    public class SubObjectComponent : GOBehavior
    {
        [SerializeField] public Vehicle subVehicle;
        [SerializeField] public Transform attachPoint;
        [SerializeField] protected Joint connection;
        public Vector3Int[] connectionPos;

        public override void OnEnable()
        {
            if (transform.parent.name.Contains("DISABLE"))
            {
                this.enabled = false;
                return;
            }

            base.OnEnable();

            Setup();
        }

        public virtual void Start()
        {
            subVehicle.transform.position = attachPoint.position;
            connection.connectedBody = subVehicle.RB;
        }

        public virtual void Setup()
        {
            GetParentVehicle();

            GameObject newVeh = (GameObject)Object.Instantiate(VehicleXMLManager.ins.InstantiatableVehicle,  attachPoint.position, attachPoint.rotation, attachPoint);

            //Debug.Log($"Pos {attachPoint.position}, rot {attachPoint.rotation}");

            subVehicle = newVeh.GetComponent<Vehicle>();

            vehicle.Subvehicles.Add(subVehicle);
            subVehicle.attachmentPoints.Add(this);

            //attachPoint.parent = null;
            //connection.massScale = 1000;

            for (int i = 0; i < connectionPos.Length; i++)
                subVehicle.AddBlockLocal(Block.SubobjectLink(), connectionPos[i], 1, 3, false);
        }

        public void OnDestroy()
        {
            if (transform.parent.name.Contains("DISABLE"))
                return;

            //Debug.Log("being called");

            for (int i = 0; i < connectionPos.Length; i++)
                subVehicle.RemoveBlockLocal(connectionPos[i], false);

            subVehicle.attachmentPoints.Remove(this);
            Destroy(connection);

            vehicle.Subvehicles.Remove(subVehicle);
            subVehicle.MainObjectDisconnect(false);
        }
    }
}
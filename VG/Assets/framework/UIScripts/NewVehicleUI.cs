using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using ASTankGame.Characters;
using ASTankGame.Vehicles;
using ASTankGame.Vehicles.Blocks.Management;
using Unity.VisualScripting;
using UnityEngine;

namespace ASTankGame.UI
{
    public class NewVehicleUI : Menu
    {
        [SerializeField] private Object defaultVehicle;
        [SerializeField] private LayerMask lookMask;

        public static NewVehicleUI ins;

        void Start()
        {
            ins = this;
        }

        public void SpawnSmallVehicle()
        {
            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;
            Vector3 spawnPos;

            if (Physics.Raycast(ray, out hit, 5, lookMask))
            {
                spawnPos = hit.point;
            }
            else
            {
                spawnPos = UnityEngine.Camera.main.gameObject.transform.position +
                         UnityEngine.Camera.main.gameObject.transform.forward * 5;
            }

            Vehicle veh = defaultVehicle.GetComponent<Vehicle>();

            veh.SetScale(0.25f);

            GameObject vehicleObject = (GameObject)Object.Instantiate(defaultVehicle, spawnPos, UnityEngine.Camera.main.gameObject.transform.rotation);

            veh = vehicleObject.GetComponent<Vehicle>();
            veh.AddBlockLocal(GlobalBlockManager.GetBlockByID(0), Vector3Int.zero, 1, 3, true);

            Deactivate();
        }

        public void SpawnMediumVehicle()
        {
            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;
            Vector3 spawnPos;

            if (Physics.Raycast(ray, out hit, 5, lookMask))
            {
                spawnPos = hit.point;
            }
            else
            {
                spawnPos = UnityEngine.Camera.main.gameObject.transform.position +
                           UnityEngine.Camera.main.gameObject.transform.forward * 5;
            }

            Vehicle veh = defaultVehicle.GetComponent<Vehicle>();

            veh.SetScale(1f);

            GameObject vehicleObject = (GameObject)Object.Instantiate(defaultVehicle, spawnPos, UnityEngine.Camera.main.gameObject.transform.rotation);

            veh = vehicleObject.GetComponent<Vehicle>();
            veh.AddBlockLocal(GlobalBlockManager.GetBlockByID(0), Vector3Int.zero, 1, 3, true);

            Deactivate();
        }

        public void SpawnLargeVehicle()
        {
            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;
            Vector3 spawnPos;

            if (Physics.Raycast(ray, out hit, 5, lookMask))
            {
                spawnPos = hit.point;
            }
            else
            {
                spawnPos = UnityEngine.Camera.main.gameObject.transform.position +
                           UnityEngine.Camera.main.gameObject.transform.forward * 5;
            }

            Vehicle veh = defaultVehicle.GetComponent<Vehicle>();

            veh.SetScale(2f);

            GameObject vehicleObject = (GameObject)Object.Instantiate(defaultVehicle, spawnPos, UnityEngine.Camera.main.gameObject.transform.rotation);

            veh = vehicleObject.GetComponent<Vehicle>();
            veh.AddBlockLocal(GlobalBlockManager.GetBlockByID(0), Vector3Int.zero, 1, 3, true);

            Deactivate();
        }
    }
}
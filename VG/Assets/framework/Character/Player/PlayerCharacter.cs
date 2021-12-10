using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Cinemachine.Utility;
using ASTankGame.Camera;
using ASTankGame.Characters;
using ASTankGame.Characters.Magic;
using ASTankGame.Characters.Movement;
using ASTankGame.UI;
using ASTankGame.Vehicles.BlockBehaviors;
using ASTankGame.Vehicles.Building;
using UnityEngine;

namespace ASTankGame.Characters
{
    public class PlayerCharacter : Character
    {
        public static PlayerCharacter pc;

        [SerializeField] private LayerMask CameraLookMask;

        public static Vector3 playerPos;
        public static Vector3 playerLookHdg;

        public override void Start()
        { 
            base.Start();

            pc = this; // check if not local player

            Cursor.lockState = CursorLockMode.Locked;
        }

        public override void Update()
        {
            #region set properites
            TargetForward = Vector3.Scale(target - transform.position, Vector3.one - Vector3.up).normalized;
            TargetRight = Vector3.Cross(Vector3.up, TargetForward).normalized;

            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000, CameraLookMask))
            {
                target = hit.point;
            }
            else
            {
                target = UnityEngine.Camera.main.gameObject.transform.position +
                         UnityEngine.Camera.main.gameObject.transform.forward * 1000;
            }

            playerPos = transform.position;
            playerLookHdg = (target - transform.position).normalized;
            #endregion

            ActionChecks();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public void QueueVariantAction(int index, int variant)
        {
            QueueAnimation(index + variant);
        }

        public void StartVariantAction(int index, int variant)
        {
            StartAnimation(index + variant);
        }

        public void PrimaryFire(bool enable)
        {
            if (seat != null)
                seat.Fire();
        }

        public void SecondaryFire(bool enable)
        {

        }

        public override void EnterSeat(Seat toEnter)
        {
            base.EnterSeat(toEnter);

            if (toEnter != null)
            {
                ThirdPersonCamera.instance.Target = toEnter.vehicle.transform.GetComponent<Rigidbody>();
                ThirdPersonCamera.ToggleActive(true);

                ActiveUI.BindVehicle(toEnter.vehicle); // this is terrible 
                ActiveUI.ChangeUITo(UIState.Vehicle);
            }
            else
            {
                ThirdPersonCamera.instance.Target = null;
                ThirdPersonCamera.ToggleActive(false);
                ActiveUI.ChangeUITo(Constructor.ins.build ? UIState.Build : UIState.Character);
            }
        }

#if UNITY_EDITOR
        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(target, 0.1f);
            base.OnDrawGizmos();
        }
#endif
    }
}

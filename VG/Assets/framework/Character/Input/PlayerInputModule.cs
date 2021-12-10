using System.Collections;
using System.Collections.Generic;
using ASTankGame.Characters.ASInput;
using ASTankGame.Vehicles.BlockBehaviors;
using UnityEngine;

namespace ASTankGame.Characters.ASInput
{
    public class PlayerInputModule : InputModule
    {
        [SerializeField] private LayerMask vehicleMask;

        public override void Start()
        {
            base.Start();
        }

        public override void Update()
        {
            myCharacter.WASD.Set(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), Input.GetAxis("Yaw"));
            myCharacter.throttle = Input.GetAxis("Throttle");
            /*
            if (Input.GetButtonDown("Fire1"))
                myCharacter.QueueAnimation(0);
            if (Input.GetButtonDown("Fire2"))
                myCharacter.QueueAnimation(1);

            if (Input.GetKeyDown(KeyCode.LeftShift))
                myCharacter.Dodge();
                */
            if (Input.GetButtonDown("Jump"))
                myCharacter.Jump();
            
            if (Input.GetKeyDown(KeyCode.Y))
                TryBoardSeat();

            if (Input.GetButtonDown("Fire1"))
                (myCharacter as PlayerCharacter).PrimaryFire(true);
        }

        public void TryBoardSeat()
        {
            if (myCharacter.state == CharacterState.Sit)
            {
                myCharacter.EnterSeat(null);
                return;
            }

            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, 1000, vehicleMask))
            {
                return;
            }

            Seat seat;

            if (hit.collider.transform.TryGetComponent<Seat>(out seat))
            {
                myCharacter.EnterSeat(seat);
            }
        }
    }
}
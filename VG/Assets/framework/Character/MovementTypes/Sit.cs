using System.Collections;
using System.Collections.Generic;
using VehicleBase.Characters.Movement;
using VehicleBase.Vehicles.BlockBehaviors;
using UnityEngine;

namespace VehicleBase.Characters.Movement
{
    public class Sit : MoveType
    {
        public Seat seat;

        public override void Begin()
        {
            base.Begin();

            rb.isKinematic = true;

            transform.parent = seat.transform;
            transform.localPosition = seat.offset;
            transform.localRotation = Quaternion.identity;

            character.SetIK(seat.leftIK, seat.rightIK);
        }

        public override void Start()
        {
            base.Start();
        }

        // Update is called once per frame
        public override void Update()
        {

        }

        public override void End()
        {
            character.SetIK(null, null);
        }
    }
}
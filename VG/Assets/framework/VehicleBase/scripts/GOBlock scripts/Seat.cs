using System.Collections;
using System.Collections.Generic;
using VehicleBase.Characters;
using VehicleBase.Vehicles;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public class Seat : GOBehavior, IControlInput
    {
        public Vector3 target { get; private set; }
        public float ws { get; private set; }
        public float ad { get; private set; }
        public float qe { get; private set; }
        public float rf { get; private set; }

        public Character passenger;

        public bool DriverSeat;

        [SerializeField]
        public Vector3 offset;

        [SerializeField] private int selectedFireGroup = 0;

        public bool animatedWheel;
        public Transform steeringWheel;
        public float rotAngle;

        private float curVel, curRot;
        public Transform leftIK;
        public Transform rightIK;

        void Update()
        {
            if (passenger != null)
            {
                target = passenger.target;
                ws = passenger.WASD.y;
                ad = passenger.WASD.x;
                qe = passenger.WASD.z;
                rf = passenger.throttle;

                if (selectedFireGroup != -1)
                    vehicle.SetFireGroupTarget(selectedFireGroup, target);

                if (animatedWheel)
                {
                    curRot = Mathf.SmoothDamp(curRot, -ad * rotAngle, ref curVel, 0.1f);
                    steeringWheel.localEulerAngles = new Vector3(0, 0, curRot);
                }
            }
        }

        public void Fire()
        {
            vehicle.FireFireGroup(selectedFireGroup);
        }

        public void SeatUpdate()
        {
            if (passenger != null)
            {
                vehicle.ActivateInput(this);
            }
            else
            {
                vehicle.DeactivateInput(this);
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (passenger != null)
                passenger.EnterSeat(null);
        }

        public void EjectPassenger()
        {
            passenger.EnterSeat(null);
        }
    }
}
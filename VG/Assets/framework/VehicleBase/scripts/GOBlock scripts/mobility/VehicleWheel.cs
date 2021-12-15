using System.Collections;
using System.Collections.Generic;
using VehicleBase;
using VehicleBase.Utility;
using VehicleBase.Vehicles;
using VehicleBase.Vehicles.BlockBehaviors;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public class VehicleWheel : PowerBlock, IUpdateOnChange
    {
        [SerializeField] private Transform suspensionBottom;
        [SerializeField] private Transform wheel;
        [SerializeField] private WheelCollider wcollider;

        
        [SerializeField] private float maxTorque;
        [SerializeField] private float maxRot;

        private float rotRef;
        private float speedRef;

        private float throttle = 0;

        void Start()
        {
            JointSpring spring = new JointSpring();
            spring.spring = wcollider.suspensionSpring.spring * transform.localScale.x;
            spring.damper = wcollider.suspensionSpring.damper * transform.localScale.x;
            wcollider.suspensionSpring = spring;
        }

        public override void Run(float efficiency)
        {
            wcollider.motorTorque = throttle * vehicle.Input.y * maxTorque * efficiency;
            wcollider.steerAngle = Mathf.SmoothDamp(wcollider.steerAngle, vehicle.Input.x * maxRot, ref rotRef, 0.4f);
        }

        public void Updated()
        {

        }

        void FixedUpdate()
        {
            Vector3 wPos;
            Quaternion wRot;

            wcollider.GetWorldPose(out wPos, out wRot);

            wheel.position = wPos;
            wheel.rotation = wRot;

            suspensionBottom.localPosition = new Vector3(suspensionBottom.localPosition.x, wheel.localPosition.y, suspensionBottom.localPosition.z);

            throttle = Mathf.SmoothDamp(throttle, Mathf.Abs(vehicle.Input.y), ref speedRef, 0.2f);
            currentPowerUsage = throttle * maxPowerUsage + 0.00001f;
        }
    }
}
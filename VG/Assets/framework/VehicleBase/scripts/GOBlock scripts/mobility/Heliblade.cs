using System.Collections;
using System.Collections.Generic;
using VehicleBase.PID;
using VehicleBase.Vehicles.BlockBehaviors;
using Unity.VisualScripting;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public class Heliblade : PowerBlock, IUpdateOnChange
    {
        [SerializeField] private bool liftOnlyMode = true;
        [SerializeField] private float maxLiftDisplacement = 5f;
        [SerializeField] private float maxLift = 100;
        [SerializeField] private float counterTorque = 30f;
        [SerializeField] private float maxYawForce = 100;

        private float spinupFactor = 0f;

        [SerializeField] private AnimationCurve liftCoefficcient;
        [SerializeField] private AnimationCurve dragCoefficcient;
        [SerializeField] private float dragConstant = 300f;

        [SerializeField] private ThingToSpin[] spinThings;

        [SerializeField]
        protected Vector3 liftDisplacement = Vector3.zero;

        private float distToCOM = 0;

        private float collective = 0;

        void Update()
        {
            collective = vehicle.Throttle;

            currentPowerUsage = maxPowerUsage * spinupFactor;

            liftDisplacement = new Vector3(
                vehicle.rollPID, 
                0, 
                vehicle.pitchPID
                );

            for (int i = 0; i < spinThings.Length; i++)
            {
                spinThings[i].Spin(spinupFactor);
            }
        }

        public override void Run(float efficiency)
        {
            float target = vehicle.Active ? 1 * efficiency : 0;

            spinupFactor = Mathf.Lerp(spinupFactor, target, spinupFactor * 0.001f + 0.003f);
        }

        void FixedUpdate()
        {
            if (liftDisplacement.magnitude > maxLiftDisplacement * transform.localScale.x)
                liftDisplacement = liftDisplacement.normalized * maxLiftDisplacement * transform.localScale.x;

            vehicle.RB.AddForceAtPosition(Simulate(), transform.position - transform.forward * liftDisplacement.z + transform.right * liftDisplacement.x); 

            vehicle.RB.AddRelativeTorque((Vector3.up * spinupFactor * counterTorque + Vector3.up * maxYawForce * vehicle.yawPID) * collective / (distToCOM + 1f) );
        }

        public Vector3 Simulate()
        {
            float verticalDot = Vector3.Dot(vehicle.RB.velocity.normalized, transform.up);

            Vector3 liftForce = transform.up * spinupFactor * maxLift * collective * liftCoefficcient.Evaluate(verticalDot);
            Vector3 dragForce = -vehicle.RB.velocity.normalized * vehicle.RB.velocity.magnitude * dragConstant * spinupFactor * (collective + 0.1f) * dragCoefficcient.Evaluate(verticalDot);

            return liftForce + dragForce;
        }

        public virtual void Updated()
        {
            Vector2 rbOffset = new Vector2(vehicle.RB.position.x - localPos.x, vehicle.RB.position.z - localPos.z);

            distToCOM = rbOffset.magnitude;

            liftOnlyMode = rbOffset.magnitude >= maxLiftDisplacement;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + (transform.forward * liftDisplacement.z + transform.right * liftDisplacement.x) * transform.localScale.x, Vector3.one * 0.2f);
        }

    }

    [System.Serializable]
    public class ThingToSpin
    {
        public Transform ToSpin;
        public float maxDPS = 360f;

        public void Spin(float DPS)
        {
            ToSpin.localEulerAngles = new Vector3(0, ToSpin.localEulerAngles.y + DPS * maxDPS * Time.deltaTime, 0);
        }
    }
}
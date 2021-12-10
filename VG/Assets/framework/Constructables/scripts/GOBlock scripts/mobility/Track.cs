using System.Collections;
using System.Collections.Generic;
using ASTankGame.Vehicles.XML;
using UnityEngine;

namespace ASTankGame.Vehicles.BlockBehaviors
{
    public class Track : PowerBlock, IUpdateOnChange
    {
        public float maxSpeed = 20;
        public float frictionCoefficcient = 1;

        [SerializeField] private Transform[] sprocketHelpers;
        [SerializeField] private Transform[] trackBones;
        [SerializeField] private Transform[] rollers;
        [SerializeField] private Vector3[] raycPositions = new Vector3[0];

        public float sprocketTravel = 0.1f;
        public float suspensionTravel = 2;
        [SerializeField] private LayerMask groundMask;

        [SerializeField] private float propulsionForce = 1000f;
        [ConfigurableSetting(CUIType.Slider, 1000, 30000)]
        [SerializeField] private float spring = 10000f;
        [ConfigurableSetting(CUIType.Slider, 1000, 5000)]
        [SerializeField] private float damper = 3000f;

        private float suspensionMult = 0.75f;
        private float prevAvgPos = 0;

        private bool rhs;

        void Start()
        {
            raycPositions = new Vector3[trackBones.Length];

            for (int i = 0; i < trackBones.Length; i++)
            {
                trackBones[i].parent = transform;
                raycPositions[i] = trackBones[i].localPosition + Vector3.up * suspensionTravel;
            }
        }

        void FixedUpdate()
        {
            RaycastHit hit;
            Vector3 throttle = vehicle.transform.forward * Mathf.Clamp(vehicle.Input.y + (vehicle.Input.x * (rhs ? -1 : 1) ), -1, 1 ) * maxSpeed;
            float newAvg = 0;

            // lift and suspension
            for (int i = 0; i < raycPositions.Length; i++)
            {
                Vector3 position = transform.TransformPoint(raycPositions[i]);
                if (Physics.Raycast(position, -transform.up, out hit,
                    suspensionTravel * vehicle.Scale, groundMask))
                {
                    Vector3 springForce = ( transform.up * (spring * (1 - hit.distance / suspensionTravel)) - transform.up * (prevAvgPos - hit.distance) ) * suspensionMult;
                    Vector3 tractionForce = Vector3.ProjectOnPlane(-vehicle.RB.velocity + throttle, transform.up).normalized * vehicle.RB.mass * frictionCoefficcient;
                    //Vector3 pForce = throttle * propulsionForce;

                    vehicle.RB.AddForceAtPosition(springForce + tractionForce, position);
                    trackBones[i].localPosition = raycPositions[i] - Vector3.up * hit.distance / vehicle.Scale;

                    newAvg += hit.distance;
                }
                else
                {
                    trackBones[i].localPosition = raycPositions[i] - Vector3.up * suspensionTravel;
                    newAvg += suspensionTravel;
                }
            }

            prevAvgPos = newAvg / raycPositions.Length;

            // sprocket thrust
            for (int i = 0; i < sprocketHelpers.Length; i++)
            {
                if (Physics.Raycast(sprocketHelpers[i].position, sprocketHelpers[i].forward, out hit,
                    sprocketTravel * vehicle.Scale, groundMask))
                {
                    Vector3 springForce = transform.up * (spring * (1 - hit.distance / sprocketTravel));
                    Vector3 tractionForce = Vector3.ProjectOnPlane(-vehicle.RB.velocity.normalized, transform.up) * vehicle.RB.mass * frictionCoefficcient;
                    Vector3 pForce = throttle * propulsionForce;

                    vehicle.RB.AddForceAtPosition(springForce + tractionForce + pForce, sprocketHelpers[i].position);
                }
            }
        }

        public override void Run(float efficiency)
        {
            
        }

        public void Updated()
        {
            rhs = vehicle.RB.centerOfMass.x < transform.localPosition.x;
            suspensionMult = vehicle.RB.mass / 2000;
        }

#if UNITY_EDITOR
        public override void OnDrawGizmosSelected()
        {
            for (int i = 0; i < raycPositions.Length; i++)
            {
                Vector3 position = transform.TransformPoint(raycPositions[i]);

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(position, 0.1f);
            }
        }
#endif
    }
}
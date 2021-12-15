using System;
using System.Collections;
using System.Collections.Generic;
using VehicleBase.Vehicles.BlockBehaviors;
using VehicleBase.Vehicles.BlockBehaviors.Weapons;
using VehicleBase.Vehicles.Blocks;
using VehicleBase.Vehicles.Chunks;
using VehicleBase.Vehicles.XML;
using Unity.VisualScripting;
using UnityEngine;

namespace VehicleBase.Vehicles.BlockBehaviors
{
    public class TurretRing : SubObjectComponent, IElectricalPart, ITargetable, IUpdateOnChange
    {
        #region variables
        public bool isTurret { get { return true; } }
        public bool horizontalGuidance { get; private set; }

        public float energyUsePerSecond { get; private set; }

        [SerializeField] private Transform ring;
        [SerializeField] private float maxRotSpeed = 30f;
        [SerializeField] private float maxMass = 1000f;
        [SerializeField] private float maxEnergyUse = 100f;

        //private bool restrictPos, restrictNeg;

        // for determining rps
        private float dps;
        private float prevAngl;

        private float rotSpeed;
        private HingeJoint jt;
        [SerializeField] private Vector3 tgt;

        [ConfigurableSetting(CUIType.Checkbox)] public bool useLimits;
        [ConfigurableSetting(CUIType.Slider, 0, 180)] public float maxLimit = 180;
        [ConfigurableSetting(CUIType.Slider, -180, 0)] public float minLimit = -180;
        #endregion

        public override void OnEnable()
        {
            if (transform.parent.name.Contains("DISABLE"))
            {
                this.enabled = false;
                return;
            }

            Setup();

            subVehicle.IsTurret = true;
            ring.parent = subVehicle.transform;
            ring.localScale = vehicle.Scale * Vector3.one;

            vehicle.ReadInterfacedBehavior(this, true);
            //attachPoint = connection.transform;

            maxMass *= transform.localScale.x;

            connection = vehicle.transform.AddComponent<HingeJoint>();
            jt = connection as HingeJoint;
        }

        public override void Start()
        {
            connection.connectedBody = subVehicle.RB;

            subVehicle.transform.position = attachPoint.position;
            subVehicle.transform.rotation = attachPoint.rotation;

            jt.autoConfigureConnectedAnchor = false;
            jt.enableCollision = true;
            jt.useSpring = true;

            int up = vehicle.GetBlock(transform.position).up;

            horizontalGuidance = false;
            if (BlockTable.MatchingAxis(up, 3))
                horizontalGuidance = true;
            else // figure out of this is a vertical/vertical configuration
            {
                Vehicle current = vehicle;
                while (current != null)
                {
                    if (current.IsTurret)
                    {
                        TurretRing tring = current.attachmentPoints[0] as TurretRing;
                        if (tring != null && !tring.horizontalGuidance)
                        {
                            horizontalGuidance = true;
                            break;
                        }
                    }

                    current = current.parentVehicle;
                }
            }

            jt.anchor = GetATPos();

            //Debug.Log(BlockTable.dirToVector[up]);

            jt.axis = BlockTable.dirToVector[up];

            //jt.useMotor = true;

            //motor.force = 10000f;
            //jt.motor = motor;
            //Debug.Log(jt.axis);

            jt.connectedAnchor = Vector3.zero;
        }

        public void RunElectric(float efficiency)
        {
            if (jt == null)
                return;

            Quaternion tgtRot = Quaternion.Lerp(attachPoint.rotation, Quaternion.LookRotation(tgt - attachPoint.position, transform.up), rotSpeed/(efficiency+0.00001f));
            attachPoint.rotation = tgtRot;
            attachPoint.localEulerAngles = Vector3.Scale(attachPoint.localEulerAngles, Vector3.up);

            dps = Mathf.Abs(jt.angle - prevAngl); // there's def a better way to do this
            prevAngl = jt.angle;

            // i think that converts from timespace to degreespace
            energyUsePerSecond = maxEnergyUse * (dps/maxRotSpeed);

            JointSpring spring = new JointSpring();

            spring.spring = 400 * subVehicle.Mass;
            spring.damper = 100 * subVehicle.Mass;
            spring.targetPosition = -Convert360To180(attachPoint.localEulerAngles.y);

            if (tgt == Vector3.zero) // its unlikely anything will ever target that exact position so its the catchphrase
                spring.targetPosition = 0;

            jt.spring = spring;

            //subVehicle.RB.MoveRotation(attachPoint.rotation);
        }

        public void Updated()
        {
            float speedFactor = Mathf.Clamp01(maxMass / (subVehicle.Mass + 0.00001f));
            rotSpeed = maxRotSpeed * speedFactor;
        }

        /*
        public bool IsHorizontalGuidance()
        {
            //return BlockTable.MatchingAxis(up, 3);
            return horizontalGuidance;
        }*/

        public override void OnConfigUpdate()
        {
            jt.useLimits = useLimits;

            if (useLimits)
            {
                JointLimits limits = new JointLimits();
                limits.min = minLimit;
                limits.max = maxLimit;
                jt.limits = limits;
            }
        }

        // little hack that works surprisingly well
        Vector3 GetATPos()
        {
            Transform prevParent = attachPoint.parent;
            attachPoint.parent = vehicle.transform;
            Vector3 pos = attachPoint.localPosition;
            attachPoint.parent = prevParent;

            return pos;

            /*
            return (attachPoint.localPosition.x * transform.right + 
                   attachPoint.localPosition.y * transform.up +
                   attachPoint.localPosition.z * transform.forward) * vehicle.Scale;*/
        }

        public float GetGuidanceMisalignment()
        {
            return 0;
        }

        public float Convert360To180(float number)
        {
            if (number > 360)
                number -= 360;

            if (number < 180)
                return number;
            else
                return -(360 - number);
        }

        public void SetTarget(Vector3 target)
        {
            tgt = target;
        }
    }
}

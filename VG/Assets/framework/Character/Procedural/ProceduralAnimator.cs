using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using VehicleBase.Characters.Movement;
using JetBrains.Annotations;
using UnityEngine;

namespace VehicleBase.Characters.Animation
{
    public class ProceduralAnimator : MonoBehaviour
    {
        #region acceleration lean
        [Header("Acceleration lean")]
        [Tooltip("This is the center of mass of the character to swivel for acceleration around")]
        public Transform COMSwivel;
        public Transform Root;
        public float smoothTime;
        public float maxAngle;

        private float yRef;
        private float xRef;
        [SerializeField] private float yAccel;
        [SerializeField] private float xAccel;

        private Vector3 prevVel;
        #endregion

        #region headlook
        private float headLook;
        private float refHeadLook;
        #endregion

        #region Default values
        [SerializeField] private float maxVel;
        #endregion

        #region various variables
        public int[] layers;

        // index is index, first number is target weight, second number is current, third is ref

        public bool Ready;

        /*[HideInInspector]*/ public Animator animator;

        [Header("Debug")]
        public int DebIndex;
        public bool DebFull;
        public int DebLayer;
        public bool debugMessages;

        Character character;
        private Rigidbody rb;

       [SerializeField] private int acting = 0;

        #endregion

        void Start()
        {
            character = transform.GetComponent<Character>();
            rb = transform.GetComponent<Rigidbody>();
            prevVel = Vector3.zero;

            acting = -1;
        }

        void Update()
        {
            if (acting == -1)
            {
                WalkAnim();

                animator.SetBool("Grounded", character.Grounded);
                animator.SetFloat("yVelocity", rb.velocity.y);
            }
            else
            {
                if (animator.GetCurrentAnimatorStateInfo(acting).normalizedTime >= 0.95f && animator.GetInteger("actionSelect") == -1)
                {
                    ResetLayers();
                    acting = -1;
                }

                yAccel = Mathf.SmoothDamp(yAccel, 0, ref yRef, smoothTime/3);
                xAccel = Mathf.SmoothDamp(xAccel, 0, ref xRef, smoothTime/3);

                COMSwivel.localEulerAngles = Vector3.right * Mathf.Clamp(yAccel * maxAngle, -20, 20) + Vector3.forward * Mathf.Clamp(xAccel * maxAngle, -20, 20);
                //COMSwivel.localEulerAngles = Vector3.zero;

                /*
                if (animator.GetCurrentAnimatorStateInfo(acting).normalizedTime >= 0.80f && !animator.GetBool("startAction"))
                {
                    StopAction();
                }*/
            }
        }

        #region walk animation
        public void WalkAnim()
        {
            // acceleration tilt
            Vector3 acceleration = (rb.velocity - prevVel) / Time.deltaTime / 100;
            prevVel = rb.velocity;

            float curaccel = Mathf.Clamp01(Mathf.Abs(character.WASD.x) + Mathf.Abs(character.WASD.y));

            Vector3 ProjectedY = Vector3.Project(acceleration, transform.forward);
            yAccel = Mathf.SmoothDamp(yAccel, ProjectedY.magnitude * Vector3.Dot(transform.forward, ProjectedY.normalized) * 5, ref yRef, smoothTime);

            Vector3 ProjectedX = Vector3.Project(acceleration, transform.right);
            xAccel = Mathf.SmoothDamp(xAccel, ProjectedX.magnitude * -Vector3.Dot(transform.right, ProjectedX.normalized) * 5, ref xRef, smoothTime);

            float frontvel = Vector3.Project(rb.velocity, rb.velocity).magnitude;

            animator.SetFloat("Forward", frontvel / maxVel);

            COMSwivel.localEulerAngles = Vector3.right * Mathf.Clamp(yAccel * maxAngle, -20, 20) + Vector3.forward * Mathf.Clamp(xAccel * maxAngle, -20, 20);

            // head look
            headLook = Mathf.SmoothDamp(headLook, AngleToWASD(character), ref refHeadLook, 0.1f);

            animator.SetFloat("LookAngle", headLook);
        }

        public float AngleToWASD(Character _char)
        {
            Vector3 relative = _char.transform.InverseTransformPoint(transform.position + _char.TargetForward * _char.WASD.y + _char.TargetRight * _char.WASD.x);

            float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;

            return angle;
        }

        public float AngleToTarget(Character _char)
        {
            Vector3 relative = transform.InverseTransformPoint(transform.position + _char.TargetForward * _char.WASD.y + _char.TargetRight * _char.WASD.x);

            float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;

            return angle;
        }
        #endregion

        #region actions
        public void StartAction(ProceduralAction _action)
        {
            if (acting != _action.layer && acting != 0)
            {
                ResetLayers();
            }
            else StopAllCoroutines(); // cancel smooth de-weighting if the same layer is being started

            animator.SetLayerWeight(_action.layer, 1);

            animator.SetInteger("actionSelect", _action.index);
            animator.SetInteger("layerSelect", _action.layer);
            animator.SetBool("startAction", true);

            acting = _action.layer;
        }

        public void ContinueChain()
        {
            animator.SetBool("continueChain", true);
        }

        public void StopAction()
        {
            if (debugMessages) Debug.Log("stopped");

            animator.SetInteger("actionSelect", -1);
            animator.SetInteger("layerSelect", -1);
        }
        #endregion

        #region layers
        public IEnumerator UpdateLayerWeight(int layer, float target)
        {
            float layerValue = animator.GetLayerWeight(layer);
            float layerRef = 0;

            // get it decently close
            while (layerValue < target - 0.05f || layerValue > target + 0.05f)
            {
                layerValue = Mathf.SmoothDamp(layerValue, target, ref layerRef, 0.1f, Mathf.Infinity, Time.deltaTime);

                animator.SetLayerWeight(layer, layerValue);

                yield return new WaitForEndOfFrame();
            }

            // finish it
            if (acting == -1)
            {
                layerValue = target;
                animator.SetLayerWeight(layer, layerValue);
            }
            else if (layer == acting)
                animator.SetLayerWeight(layer, 1); // needs to be here or it can get stuck if re-initiating action
        }

        public void ResetLayers()
        {
            if (acting == -1) return;

            if (debugMessages) Debug.Log("reset");

            StartCoroutine(UpdateLayerWeight(acting, 0));
        }
#endregion

        public void OnDrawGizmos()
        {
            if (character != null)
                Gizmos.DrawLine(character.transform.position, transform.position + character.TargetForward * character.WASD.y + character.TargetRight * character.WASD.x);
        }
    }
}
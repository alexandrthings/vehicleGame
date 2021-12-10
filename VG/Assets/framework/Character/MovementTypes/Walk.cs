using System.Collections;
using System.Collections.Generic;
using ASTankGame.Characters.Movement;
using UnityEngine;

namespace ASTankGame.Characters.Movement
{
    public class Walk : MoveType
    {
        // maximum speed we can accelerate to
        public float DecelerationForce = 10000;

        public bool hover;
        public bool Strafe;

        public float MaxAccelSpeed = 10;

        public float JumpForce;

        private float xRef;
        private float yRef;

        [SerializeField]
        private float turnaroundTimer;

        public override void Begin()
        {
            base.Begin();
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Update()
        {
            base.Update();

            if (!Run)
                return;
        }

        public override void FixedUpdate()
        {
            if (!Run)
                return;

            float addUpDown = Input.GetAxis("UpDown");

            // movement
            float curaccel = Mathf.Clamp01(Mathf.Abs(character.WASD.x) + Mathf.Abs(character.WASD.y));

            if (hover)
                curaccel = Mathf.Clamp01(curaccel + Mathf.Abs(addUpDown));

            Vector3 move;

            if (curaccel > 0.1f)
                move = ((character.TargetForward * character.WASD.y + character.TargetRight * character.WASD.x + Vector3.up * addUpDown).normalized * curaccel * MaxAccelSpeed - rb.velocity) * MoveForce;
            else
                move = -rb.velocity * DecelerationForce;

            // if hover dont scale vertical axis
            if (hover)
                rb.useGravity = false;
            else
            {
                move = Vector3.Scale(move, Vector3.forward + Vector3.right);
                rb.useGravity = true;
            }

            if (rb.velocity.magnitude > MaxAccelSpeed && Vector3.Dot(rb.velocity, move) > 0)
            {
                move = Vector3.ProjectOnPlane(move, -rb.velocity.normalized);
            }

            rb.AddForce(move * Time.fixedDeltaTime);

            // different forms of rotation
            if (!Strafe)
            {
                // rotation
                if (rb.velocity.magnitude > (turnaroundTimer > 0 ? 3f : 0.2f))
                {
                    Quaternion targetRot = Quaternion.LookRotation(Vector3.Scale(rb.velocity, Vector3.one - Vector3.up).normalized, Vector3.up);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 0.6f);

                    turnaroundTimer = 0.5f;
                }
                else if (character.WASD.magnitude > 0.1f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(
                        character.TargetForward * character.WASD.y + character.TargetRight * character.WASD.x,
                        Vector3.up);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 0.2f);
                }

                turnaroundTimer -= Time.fixedDeltaTime;
            }
            else
            {
                Quaternion targetRot = Quaternion.LookRotation(character.TargetForward);

                transform.rotation = targetRot;
            }

            if (!character.Grounded && !hover)
            {
                //rb.velocity += Vector3.up * 0.1f;
                character.SwitchToState(CharacterState.Airborne);
                return;
            }
        }
    }
}

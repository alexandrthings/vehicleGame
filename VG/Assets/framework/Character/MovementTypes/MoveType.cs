using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Characters.Movement
{
    /// <summary>
    /// to setup, just call base.Begin || character, rb, animator, PrevState, TimeInState exposed
    /// </summary>
    public abstract class MoveType : MonoBehaviour
    {
        [HideInInspector] public Character character;
        [HideInInspector] public Rigidbody rb;

        public CharacterState ThisState;
        [HideInInspector] public CharacterState PrevState;

        //public float Softcap = 10f;
        //public float LinearDrag = 5f;
        public float MoveForce = 1500f;

        public float TimeInState;

        public bool Run;

        /// <summary>
        /// Sets softcap, linear drag, and begins state timer
        /// </summary>
        public virtual void Begin()
        {
            if (character == null)
                Start();

            rb.isKinematic = false;

            TimeInState = 0;
        }

        public virtual void Start()
        {
            character = transform.GetComponent<Character>();
            rb = transform.GetComponent<Rigidbody>();
            //animator = character.animator;
        }

        public virtual void FixedUpdate()
        {
            if (!Run)
                return;
        }

        /// <summary>
        /// Ticks timer up.
        /// </summary>
        public virtual void Update()
        {
            if (!Run)
                return;

            TimeInState += Time.deltaTime;
        }

        public virtual void End()
        {

        }

        public virtual IEnumerator AccelerateForTime(Vector3 velocity, float timeout)
        {
            float t = 0;
            while (t < timeout)
            {
                // movement
                Vector3 move = velocity - rb.velocity;

                if (rb.velocity.magnitude > velocity.magnitude)
                {
                    move = Vector3.ProjectOnPlane(move, -rb.velocity.normalized);
                }

                rb.AddForce(move * MoveForce * Time.fixedDeltaTime);

                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

    }

}

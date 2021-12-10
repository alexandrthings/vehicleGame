using System.Collections;
using System.Collections.Generic;
using ASTankGame.Characters;
using ASTankGame.Characters.Movement;
using UnityEngine;

namespace ASTankGame.Characters.Movement
{
    public class Clamber : MoveType
    {
        public Vector3 target;
        public Vector3 direction;

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
        }

        public override void FixedUpdate()
        {
            if (!Run)
                return;

            transform.position = Vector3.Lerp(transform.position, target, 0.1f);
            rb.velocity = Vector3.zero;

            /*if (TimeInState > 0.6f)
            {
                character.SwitchToState(CharacterState.Walking);
            }*/
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target, 0.1f);
            Gizmos.DrawLine(target, target + direction*2f);
        }
    }
}
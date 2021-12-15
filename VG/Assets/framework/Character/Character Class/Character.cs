using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using VehicleBase.Characters.Animation;
using VehicleBase.Characters.Movement;
using VehicleBase.Damage;
using DitzelGames.FastIK;
using VehicleBase.Vehicles.BlockBehaviors;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Debug = UnityEngine.Debug;

namespace VehicleBase.Characters
{
    public class Character : MonoBehaviour, IDamageableObject
    {
        #region stats
        [Header("Default movement")]
        public float JumpPower = 10000;
        protected Rigidbody rb;

        public Collider bumper;
        public bool Collide = true;

        public Seat seat;

        [Header("Animator")] public ProceduralAnimator animator;

        public CharacterState state { get; private set; }
        [SerializeField]protected MoveType currentMoveType;
        protected Dictionary<CharacterState, MoveType> moveTypes = new Dictionary<CharacterState, MoveType>();

        public float groundCheckRadius = 0.4f;
        public float groundCheckUp = 0.5f;
        public bool Grounded;

        public Vector3 groundSlope;
        [SerializeField] private LayerMask GroundMask;

        [Header("IKs")]
        [SerializeField] private FastIKFabric LeftIK;
        [SerializeField] private FastIKFabric RightIK;

        [Header("Control attributes")] public Vector3 WASD;
        public Vector3 target;
        public float throttle;

        public Vector3 TargetForward { get; protected set; }
        public Vector3 TargetRight { get; protected set; }

        public float MaxHP { get; set; }
        public float Health { get; set; }

        public float Armor { get; set; }

        #endregion

        #region actions

        [SerializeField] public List<ProceduralAction> actions = new List<ProceduralAction>();
        [SerializeField] public List<ProceduralAction> chainActions = new List<ProceduralAction>();

        public ProceduralAction activeAction;
        public int dodgeAction;
        public int climbAction;
        public int sitAction;
        public int interactAction;

        public ActionIndexAndLayer queuedAction = new ActionIndexAndLayer(-1, 0, false);
        protected float queueTimeout = 0;

        protected float actionTimer;
        protected int chainOrigin;

        #endregion

        #region run
        public virtual void Start()
        {
            rb = transform.GetComponent<Rigidbody>();
            //animator = transform.GetComponent<Animator>();

            MoveType[] moves = transform.GetComponents<MoveType>();

            for (int i = 0; i < moves.Length; i++)
            {
                moveTypes.Add(moves[i].ThisState, moves[i]);
            }

            SwitchToState(CharacterState.Walking);
            activeAction = new ProceduralAction("empty", -100, 0, 0, 0);
            actionTimer = 1;
        }

        public virtual void Update()
        {
            TargetForward = Vector3.Scale(target - transform.position, Vector3.one - Vector3.up).normalized;
            TargetRight = Vector3.Cross(Vector3.up, TargetForward).normalized;

            ActionChecks();
        }

        public virtual void FixedUpdate()
        {
            Grounded = Physics.CheckSphere(transform.position, groundCheckRadius, GroundMask);

            if (!Collide)
                return;

            RaycastHit hit;
            if (Physics.SphereCast(transform.position + Vector3.up * groundCheckUp, groundCheckRadius, Vector3.down,
                out hit, groundCheckUp, GroundMask))
            {
                if (transform.position.y < hit.point.y && Vector3.Dot(hit.normal, Vector3.up) > 0.5f)
                {
                    float emulatedYVel = (hit.point.y - transform.position.y);
                    transform.position = transform.position + Vector3.up * emulatedYVel;

                    //Vector3 move = Vector3.Scale(((transform.position - hit.point).normalized * groundCheckRadius), new Vector3(1, -1, 1));

                    //transform.position += move;

                    if (rb.velocity.y < 0)
                    {
                        rb.velocity = Vector3.Scale(rb.velocity, Vector3.forward + Vector3.right);
                        //rb.useGravity = false;
                    }
                }
            }

            //if (rb.velocity.y > 0 || !Grounded)
            //rb.useGravity = true;
        }

        #endregion

        #region special actions
        public void Clamber(Vector3 _target, Vector3 _direction)
        {
            if (!moveTypes.ContainsKey(CharacterState.Clamber))
                return;

            Clamber clamb = (Clamber)moveTypes[CharacterState.Clamber];
            clamb.target = _target;
            clamb.direction = _direction;

            StartAnimation(climbAction);
        }

        public virtual void Dodge()
        {
            if (!moveTypes.ContainsKey(CharacterState.Dodge))
                return;

            Dodge dodge = (Dodge) moveTypes[CharacterState.Dodge];
            dodge.entryInput = WASD;
            dodge.duration = actions[dodgeAction].duration;

            QueueAnimation(dodgeAction);
        }

        public virtual void EnterSeat(Seat toEnter)
        {
            if (!moveTypes.ContainsKey(CharacterState.Sit))
                return;

            Sit sit = (Sit)moveTypes[CharacterState.Sit];

            if (toEnter == null && sit.seat != null)
            {
                seat = null;
                sit.seat.passenger = null;

                sit.seat.SeatUpdate();

                sit.seat = null;
                transform.parent = null;

                ForceStopAnimation();

                bumper.enabled = true;
                Collide = true;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                return;
            }

            seat = toEnter;
            sit.seat = toEnter;
            sit.seat.passenger = this;

            sit.seat.SeatUpdate();

            bumper.enabled = false;
            Collide = false;

            rb.useGravity = false;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;

            QueueAnimation(sitAction);
        }
        #endregion

        #region universal movement

        public void SetIK(Transform targetLeft, Transform targetRight)
        {
            LeftIK.Target = targetLeft;
            RightIK.Target = targetRight;

            LeftIK.enabled = (targetLeft != null);
            RightIK.enabled = (targetRight != null);
        }

        public void Jump()
        {
            if (Grounded && rb.velocity.y < 0.1f && activeAction.index == -100)
            {
                rb.AddForce(Vector3.up * JumpPower);
                //SwitchToState(CharacterState.Airborne);
            }
        }
        #endregion

        #region actions and states
        public void ActionChecks()
        {
            actionTimer += Time.deltaTime;

            // if theres an active action AND a queued action, check if we can interrupt current action and start a new one
            if (queuedAction.index != -1)
                if (activeAction.index != -100)
                {
                    if (actionTimer > activeAction.interruptTime)
                    {
                        StartAnimation(queuedAction.index);
                    }
                }
                else
                {
                    StartAnimation(queuedAction.index);
                }

            if (activeAction.index != -100) // for some reason this won't null,
                if (actionTimer >= activeAction.duration * 0.98f)
                {
                    SwitchToState(Grounded ? CharacterState.Walking : CharacterState.Airborne);
                    //activeAction = null; // even though we set it to null right here and it just repeats
                    activeAction = new ProceduralAction("", -100, 0, 0, 0);
                    animator.StopAction();
                    chainOrigin = -1;
                }
        }

        public void SwitchToState(CharacterState _state)
        {
            if (!moveTypes.ContainsKey(_state))
                return;

            moveTypes[state].End();

            moveTypes[state].Run = false;
            moveTypes[_state].PrevState = state;

            currentMoveType = moveTypes[_state];

            state = _state;

            rb.drag = 0;

            rb.useGravity = true;

            moveTypes[state].Begin();

            moveTypes[state].Run = true;
        }

        public void SwitchToState(CharacterState _state, float locktime)
        {
            if (currentMoveType.TimeInState < locktime)
                return;

            SwitchToState(_state);
        }

        public virtual void QueueAnimation(int index)
        {
            if (activeAction.index != -100)
            {
                if (actionTimer < 0.2f)
                    return;

                if (chainOrigin == index && activeAction.childAction != -1)
                {
                    queuedAction.index = activeAction.childAction;
                    queuedAction.layer = actions[index].layer;
                    queuedAction.chain = true;

                    return;
                }
            }

            queuedAction.index = index;
            queuedAction.layer = actions[index].layer;
            queuedAction.chain = false;
        }

        public virtual void StartAnimation(int index)
        {
            if (!actions[index].Ready())
                return;

            actionTimer = 0;

            if (queuedAction.chain == false)
            {
                activeAction = actions[index];
                animator.StartAction(activeAction);
                chainOrigin = index;
            }
            else
            {
                activeAction = chainActions[index];
                animator.ContinueChain();
            }

            activeAction.lastActivated = Time.time;

            if (activeAction.type == ActionType.FullAction)
                SwitchToState(activeAction.state);

            queuedAction.index = -1;
            queuedAction.layer = 0;
            queuedAction.chain = false;
        }

        public virtual void StopAnimation()
        {
            if (activeAction.index != -100) // for some reason this won't null,
                if (actionTimer >= activeAction.duration * 0.98f)
                {
                    SwitchToState(Grounded ? CharacterState.Walking : CharacterState.Airborne);
                    //activeAction = null; // even though we set it to null right here and it just repeats
                    activeAction = new ProceduralAction("", -100, 0, 0, 0);
                    animator.StopAction();
                    chainOrigin = -1;
                }
        }

        public void ForceStopAnimation()
        {
            if (activeAction.index != -100) // for some reason this won't null,
            {
                SwitchToState(Grounded ? CharacterState.Walking : CharacterState.Airborne);
                //activeAction = null; // even though we set it to null right here and it just repeats
                activeAction = new ProceduralAction("", -100, 0, 0, 0);
                animator.StopAction();
                chainOrigin = -1;
            }
        }

        public virtual int GetAnimationIndex(string name)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].name.ToLower() == name.ToLower())
                {
                    return i;
                }
            }

            return -1;
        }
        #endregion

        float Conv180to360(float angle)
        {
            return angle < 180 ? angle : -360 + angle;
        }

        /*
        public float GetAngleToTarget()
        {
            Vector3 relative = transform.InverseTransformPoint(TargetPosition);
            return Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
        }*/

        public void TakeDamage(float _amount, Vector3 _position, DamageType _type)
        {

        }
#if UNITY_EDITOR
        public virtual void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + TargetForward);
            Gizmos.DrawLine(transform.position, transform.position + TargetRight);
        }
#endif
    }
    
    [Serializable]
    public class ProceduralAction
    {
        [Header("Name and Index")]
        public string name;
        public int index;
        public int layer;
        public float interruptTime;
        public float duration;

        [Header("Gameplay Affectors")]
        public ActionType type;
        public CharacterState state;
        public float speed = 1;

        public float Cooldown;
        [HideInInspector] public float lastActivated;

        public int childAction;

        public ProceduralAction(string _name, int _index, int _layer, float _duration, float _interrupt)
        {
            name = _name;
            index = _index;
            layer = _layer;
            duration = _duration;
            interruptTime = _interrupt;
            type = ActionType.FullAction;
            state = CharacterState.StaticAttack;
            speed = 1;
        }

        public bool Ready()
        { 
            return Time.time - lastActivated > Cooldown;
        }
    }

    [Serializable]
    public struct ActionIndexAndLayer
    {
        public int index;
        public int layer;
        public bool chain;

        public ActionIndexAndLayer(int _index, int _layer, bool _chain)
        {
            index = _index;
            layer = _layer;
            chain = _chain;
        }
    }

    public enum ActionType
    {
        CutsceneAction,
        FullAction,
        UpperBodyAction,
        Inaction
    }

    public enum CharacterState
    {
        Walking,
        Airborne,
        Jumping,
        Dodge,
        Slide,
        Blink,
        Yeet,
        Sit,
        Clamber,
        StaticAttack,
        MobileAttack
    }
}

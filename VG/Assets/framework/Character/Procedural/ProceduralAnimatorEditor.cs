#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using VehicleBase.Characters;
using VehicleBase.Characters.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomEditor(typeof(Character), true)]
public class ProceduralAnimatorEditor : Editor
{
    private int childCounter;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Character animator = (Character)target;


        if (GUILayout.Button("Find actions"))
        {
            FindActions(animator);
        }

        if (GUILayout.Button("Stop actions"))
        {
            animator.animator.StopAction();
        }
    }

    public void FindActions(Character charac)
    {
        childCounter = 0;

        AnimatorController AnimCont = (AnimatorController)charac.animator.animator.runtimeAnimatorController;
        // fsfr the animator starts returning incorrect layer index if i do something to it
       // int[] layerIndex = new int[2]
            // {charac.animator.animator.GetLayerIndex("FullBody"), charac.animator.animator.GetLayerIndex("UpperBody")};
       //     {2, 1}; // fuck you GetLayerIndex, you're not working -A

        charac.actions.Clear();
        charac.chainActions.Clear();

        for (int i = 0; i < charac.animator.layers.Length; i++)
        {
            FindFunction(charac, AnimCont, charac.animator.layers[i]);
        }
        
    }

    // FIRST TRANSITION NEEDS TO BE EMPTY
    public void FindFunction(Character _charac, AnimatorController cont, int layer)
    {
        AnimatorStateTransition cancelTransition = cont.layers[layer].stateMachine.anyStateTransitions[0];

        AnimatorCondition cancel1 = new AnimatorCondition();

        cancel1.mode = AnimatorConditionMode.NotEqual;
        cancel1.parameter = "layerSelect";
        cancel1.threshold = layer;

        cancelTransition.conditions = new AnimatorCondition[] {cancel1};

        // take the first steps of each chain (ignore 1st one because it's the empty one)
        for (int t = 1; t < cont.layers[layer].stateMachine.anyStateTransitions.Length; t++)
        {
            AnimatorStateTransition transition = cont.layers[layer].stateMachine.anyStateTransitions[t];
            AnimatorState destState = transition.destinationState;

            AnimatorCondition newCond = new AnimatorCondition();
            AnimatorCondition newCond2 = new AnimatorCondition();
            AnimatorCondition newCond3 = new AnimatorCondition();
            AnimationClip clip = (AnimationClip) transition.destinationState.motion;

            // condition for selector to be equal
            newCond.mode = AnimatorConditionMode.Equals;
            newCond.parameter = "actionSelect";
            newCond.threshold = t-1;

            // condition to start
            newCond2.mode = AnimatorConditionMode.If;
            newCond2.parameter = "startAction";
            newCond2.threshold = 1;

            // condition for right layer
            newCond3.mode = AnimatorConditionMode.Equals;
            newCond3.parameter = "layerSelect";
            newCond3.threshold = layer;

            transition.conditions = new AnimatorCondition[] {newCond, newCond2, newCond3};
            transition.canTransitionToSelf = false;

            //Debug.Log(transition.destinationState.name);

            List<ProceduralAction> actions = new List<ProceduralAction>()
                {new ProceduralAction(transition.destinationState.name, t - 1, layer, clip.length, clip.length * 0.8f)};

            int w = 0;

            newCond2.parameter = "continueChain";

            // add chainables
            while (destState.transitions.Length > 0)
            {
                transition = destState.transitions[0];
                destState = transition.destinationState;

                transition.conditions = new AnimatorCondition[] {newCond2};
                clip = (AnimationClip) destState.motion;

                actions.Add(new ProceduralAction(destState.name, w, layer, clip.length, clip.length * 0.8f));

                actions[w].childAction = childCounter;

                childCounter++;
                w++;
            }

            actions[actions.Count - 1].childAction = -1;

            // set child to be the next action in array
            for (int c = 1; c < actions.Count; c++)
            {
                _charac.chainActions.Add(actions[c]);
            }

            _charac.actions.Add(actions[0]);
        }
        //Debug.LogWarning($"Disconnected state {cont.layers[0].stateMachine.states[i].state.name}");

        //IFFIRSTSTATE:

        //_charac.fullActions.Add(new ProceduralAction(state.name, state.motion.averageDuration, new int[] {0}));

        for (int i = 0; i < _charac.actions.Count; i++)
        {
            if (_charac.actions[i].name.ToLower() == "dodge")
            {
                _charac.dodgeAction = _charac.actions[i].index;
                _charac.actions[i].state = CharacterState.Dodge;
                continue;
            }

            if (_charac.actions[i].name.ToLower() == "climb")
            {
                _charac.climbAction = _charac.actions[i].index;
                _charac.actions[i].state = CharacterState.Clamber;
                continue;
            }

            _charac.actions[i].state = CharacterState.StaticAttack;
        }

        AssetDatabase.SaveAssets();
    }

    public AnimatorState GetNextActions(AnimatorState _state)
    {
        AnimatorState state = _state.transitions[0].destinationState;

        return state;
    }

    public int GetStateIndex(AnimatorState state, AnimatorController cont, int layer)
    {
        int ind = 0;

        for (int i = 0; i < cont.layers[layer].stateMachine.states.Length; i++)
        {
            if (cont.layers[layer].stateMachine.states[i].state == state)
                return i;
        }

        return ind;
    }
}
#endif
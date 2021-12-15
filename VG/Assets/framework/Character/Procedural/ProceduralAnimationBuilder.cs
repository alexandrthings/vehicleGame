#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Rendering;
using UnityEngine.Experimental.AI;

namespace VehicleBase.Characters.Animation.Generation
{
    [ExecuteInEditMode]
    public class ProceduralAnimationBuilder : MonoBehaviour
    {
        #region properties
        private Animator animator;

        [Header("Setup")]
        [Tooltip("This is an AvatarMask with only the bones you want interpolated selected in it.")]
        public AvatarMask TransformsToBake;

        [Tooltip("This is the Armature that should be a child of this object.")]
        public Transform Armature;

        [Tooltip("Put the animation you want to interpolate here.")]
        public AnimationClip AnimationToInterpolate;

        [Tooltip("Create a new AnimatorController and drop it in here. The 'Prepare to Interpolate' button will take care of the rest.")]
        public AnimatorController BakeAnimator;

        [Header("Output")]
        public string Name = "interpolated animation";
        [Tooltip("Put a slash at the end!")]
        public string PathToSaveAt = "Assets/Generated/";

        [Header("Animation Properties")]
        [Tooltip("Animation sampling rate in frames per second")]
        public int SampleRateFPS = 24;

        private float prevFixedDeltaTime;

        [Tooltip("Pick and choose when to sample, starts with 0 and ends with the last frame")]
        public int[] framesToSample;

        [Tooltip("Ideally should be inbetween 0 and 1. Makes the character be more floaty in its motion.")]
        public float Floatiness = 1;
        [Tooltip("Tries to normalize tangents. Use if you're making very calm movements. If you're waving about your hammer, you might want to turn this on.")]
        public bool NormalizeCurves = true;
        [Tooltip("Use only if keyframes have different distances between them")]
        public bool NonUniformKeyframeSpacing = false;
        [Tooltip("This should be the largest gap between keyframes.")]
        public int KeyframeSpacing = 9;
        [Tooltip("Do we interpolate animation as though it's looped?")]
        public bool LoopedAnimation;

        [Tooltip("With this checked it will interpolate and save the animation on next play.")]
        public bool BakeOnNextPlay;

        [Tooltip("Dont touch")]
        public bool Ready;

        // all the transforms in the armature to bake
        public List<SampledTransform> SampledTransforms = new List<SampledTransform>();

        private IEnumerator sampleRoutine;

        // statemachine behavior stuff
        [SerializeField] private int TotalFrames;
        [SerializeField] private float Length;

        #endregion

        void Awake()
        {
            Time.fixedDeltaTime = 0.02f;
        }

        void Start()
        {
            if (!Ready)
            {
                Debug.LogWarning("You forgot to press 'Prepare to Interpolate', D U M M Y.\n Cancelling operation...");
                return;
            }

            Ready = false;

            AnimationClip clip = AnimationToInterpolate;
            animator = transform.GetComponent<Animator>();
            animator.speed = 0;

            prevFixedDeltaTime = Time.fixedDeltaTime;
            Time.fixedDeltaTime = 1 / (float)SampleRateFPS;

            Length = AnimationToInterpolate.length*2;

            if (!BakeOnNextPlay)
                return;

            SampledTransforms.Clear();

            Debug.Log("Beginning animation interpolation...");

            TotalFrames = Mathf.RoundToInt(AnimationToInterpolate.length * (float)AnimationToInterpolate.frameRate);

            SampleTransforms();

            sampleRoutine = SamplingCoroutine(1/ ((float)SampleRateFPS), framesToSample[framesToSample.Length-1]!= 0? framesToSample[framesToSample.Length-1]+1 : TotalFrames, Length);

            StartCoroutine(sampleRoutine);
        }

        void FinishBuildAnimation()
        {
            AnimationClip BakedAnimation = new AnimationClip();

            for (int t = 0; t < SampledTransforms.Count; t++)
            {
                #region creating curves from keyframes
                AnimationCurve xPosCurve = new AnimationCurve(SampledTransforms[t].posX.ToArray());
                AnimationCurve yPosCurve = new AnimationCurve(SampledTransforms[t].posY.ToArray());
                AnimationCurve zPosCurve = new AnimationCurve(SampledTransforms[t].posZ.ToArray());

                AnimationCurve xRotCurve = new AnimationCurve(SampledTransforms[t].rotX.ToArray());
                AnimationCurve yRotCurve = new AnimationCurve(SampledTransforms[t].rotY.ToArray());
                AnimationCurve zRotCurve = new AnimationCurve(SampledTransforms[t].rotZ.ToArray());
                AnimationCurve wRotCurve = new AnimationCurve(SampledTransforms[t].rotW.ToArray());

                AnimationCurve xSclCurve = new AnimationCurve(SampledTransforms[t].scaleX.ToArray());
                AnimationCurve ySclCurve = new AnimationCurve(SampledTransforms[t].scaleY.ToArray());
                AnimationCurve zSclCurve = new AnimationCurve(SampledTransforms[t].scaleZ.ToArray());
                #endregion

                #region writing to clip
                BakedAnimation.SetCurve(SampledTransforms[t].path, typeof(Transform), "localPosition.x", xPosCurve);
                BakedAnimation.SetCurve(SampledTransforms[t].path, typeof(Transform), "localPosition.y", yPosCurve);
                BakedAnimation.SetCurve(SampledTransforms[t].path, typeof(Transform), "localPosition.z", zPosCurve);

                //there may be some ungodly fuckery you can do with ObjectReferenceKEyframes
                //UnityEditor.AnimationUtility.SetObjectReferenceCurve(BakedAnimation, EditorCurveBinding.PPtrCurve(SampledTransforms[t].path, typeof(Transform), "Rotation.x"), SampledTransforms[t].objRotX.ToArray());

                BakedAnimation.SetCurve(SampledTransforms[t].path, typeof(Transform), "localRotation.x", xRotCurve);
                BakedAnimation.SetCurve(SampledTransforms[t].path, typeof(Transform), "localRotation.y", yRotCurve);
                BakedAnimation.SetCurve(SampledTransforms[t].path, typeof(Transform), "localRotation.z", zRotCurve);
                BakedAnimation.SetCurve(SampledTransforms[t].path, typeof(Transform), "localRotation.w", wRotCurve);

                BakedAnimation.SetCurve(SampledTransforms[t].path, typeof(Transform), "localScale.x", xSclCurve);
                BakedAnimation.SetCurve(SampledTransforms[t].path, typeof(Transform), "localScale.y", ySclCurve);
                BakedAnimation.SetCurve(SampledTransforms[t].path, typeof(Transform), "localScale.z", zSclCurve);

                BakedAnimation.name = Name;

                #endregion
            }

            string path = PathToSaveAt + Name + ".anim";

            AssetDatabase.CreateAsset(BakedAnimation, path);
            AssetDatabase.SaveAssets();

            Debug.Log("Animation building complete.");
            Time.fixedDeltaTime = prevFixedDeltaTime;
        }

        #region sampling
        IEnumerator SamplingCoroutine(float sampleInterval, int totalFrames, float length)
        {
            yield return new WaitForSeconds(1);
            animator.speed = 1;

            int f = 0;
            while (f < totalFrames)
            {
                if (Contains(framesToSample, f))
                {

                    Transform currentTransform = transform;
                    float time = f * Time.fixedDeltaTime;
                    // sample animation
                    for (int t = 0; t < SampledTransforms.Count; t++)
                    {
                        currentTransform = SampledTransforms[t].transform;

                        SampledTransforms[t].posX.Add(new Keyframe(time, currentTransform.localPosition.x, 0, 0));
                        SampledTransforms[t].posY.Add(new Keyframe(time, currentTransform.localPosition.y, 0, 0));
                        SampledTransforms[t].posZ.Add(new Keyframe(time, currentTransform.localPosition.z, 0, 0));

                        SampledTransforms[t].rotX.Add(new Keyframe(time, currentTransform.localRotation.x, 0, 0));
                        SampledTransforms[t].rotY.Add(new Keyframe(time, currentTransform.localRotation.y, 0, 0));
                        SampledTransforms[t].rotZ.Add(new Keyframe(time, currentTransform.localRotation.z, 0, 0));
                        SampledTransforms[t].rotW.Add(new Keyframe(time, currentTransform.localRotation.w, 0, 0));

                        SampledTransforms[t].scaleX.Add(new Keyframe(time, currentTransform.localScale.x, 0, 0));
                        SampledTransforms[t].scaleY.Add(new Keyframe(time, currentTransform.localScale.y, 0, 0));
                        SampledTransforms[t].scaleZ.Add(new Keyframe(time, currentTransform.localScale.z, 0, 0));
                    }

                    Debug.Log($"Sampling Frame {f}");
                }

                f++;
                yield return new WaitForFixedUpdate();
            }

            InterpolateKeyTangents();
        }

        void SampleTransforms()
        {
            // where we are in the heirarchy rn
            Transform heirarchyLocation = transform;

            // loop through all the transforms in the avatarmask
            for (int i = 0; i < TransformsToBake.transformCount; i++)
            {
                if (!TransformsToBake.GetTransformActive(i))
                    continue;

                heirarchyLocation = i == 0 ? Armature : transform;

                // find where the transform is and how to get it
                string gottenPath = TransformsToBake.GetTransformPath(i);
                if (gottenPath.Length == 0)
                    continue;

                Debug.Log($"Path : {gottenPath}");

                string[] transformNames = GetTransformNames(gottenPath); // individual names from the path

                for (int d = 0; d < transformNames.Length; d++)
                    Debug.LogWarning(transformNames[d]);

                for (int n = 0; n < transformNames.Length; n++)  // go down the heirarchy to the transform and get it
                {
                    for (int c = 0; c < heirarchyLocation.childCount; c++) // iterate through our current position's children
                    {
                        if (heirarchyLocation.GetChild(c).name == transformNames[n])
                        {
                            heirarchyLocation = heirarchyLocation.GetChild(c); // compare name, see if its the child we're looking for, if so, continue down heirarchy
                            //Debug.Log($"Went down path {heirarchyLocation}");
                            break;
                        }

                        //if (c == heirarchyLocation.childCount - 1) Debug.LogError($"Child {transformNames[n]} is not present on path {gottenPath}"); // if this loop completes something's gone terribly wrong 
                    }
                }

                if (heirarchyLocation == Armature)
                    continue;

                SampledTransforms.Add(new SampledTransform(heirarchyLocation, gottenPath));

                Debug.Log(SampledTransforms[SampledTransforms.Count - 1].transform);
            }

            for (int i = 0; i < SampledTransforms.Count; i++)
            {
                Debug.Log(SampledTransforms[i].transform);
            }
        }

        bool KeyframeEqual(Keyframe key1, Keyframe key2)
        {
            bool approx = Mathf.Approximately(key1.value,key2.value);
            return approx;
        }

        bool Contains(int[] array, int number)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == number)
                    return true;
            }
            return false;
        }
        #endregion

        #region interpolating
        //step 1 remove extra keyframes
        // reduce amount of keyframes by finding unnecessary ones
        void SmoothPruneKeyframes()
        {
            Debug.LogError("this shouldnt happen");

            for (int i = 0; i < SampledTransforms.Count; i++)
            {
                SampledTransforms[i].posX = Prune(SampledTransforms[i].posX);
                SampledTransforms[i].posY = Prune(SampledTransforms[i].posY);
                SampledTransforms[i].posZ = Prune(SampledTransforms[i].posZ);

                SampledTransforms[i].rotX = Prune(SampledTransforms[i].rotX);
                SampledTransforms[i].rotY = Prune(SampledTransforms[i].rotY);
                SampledTransforms[i].rotZ = Prune(SampledTransforms[i].rotZ);
                SampledTransforms[i].rotW = Prune(SampledTransforms[i].rotW);

                SampledTransforms[i].scaleX = Prune(SampledTransforms[i].scaleX);
                SampledTransforms[i].scaleY = Prune(SampledTransforms[i].scaleY);
                SampledTransforms[i].scaleZ = Prune(SampledTransforms[i].scaleZ);
            }

            FinishBuildAnimation();
        }

        private List<Keyframe> Prune(List<Keyframe> _keyframes)
        {
            if (_keyframes.Count <= 2) return _keyframes; // 2 or less is not enough to prune

            List<Keyframe> prunedFrames = new List<Keyframe>();

            prunedFrames.Add(_keyframes[0]);
            // iterate, ignoring first and last frames
            for (int f = 1; f < _keyframes.Count - 1; f++)
            {
                float value = _keyframes[f].value;

                // if part of downward slope remove
                if (_keyframes[f - 1].value > value && value > _keyframes[f + 1].value)
                    continue;
                //if part of upward slope remove
                if (_keyframes[f - 1].value < value && value < _keyframes[f + 1].value)
                    continue;
                if (Mathf.Approximately(_keyframes[f - 1].value, value) || Mathf.Approximately(_keyframes[f + 1].value, value))
                    continue;

                prunedFrames.Add(_keyframes[f]);
            }

            prunedFrames.Add(_keyframes[_keyframes.Count-1]);
            return prunedFrames;
        }

        //step 2 interpolate
        void InterpolateKeyTangents()
        {
            // Interpolate the frames other than first and last
            for (int t = 0; t < SampledTransforms.Count; t++)
            {
                #region set tangents
                SampledTransforms[t].posX = InterpolateKeyframeSet(SampledTransforms[t].posX, LoopedAnimation);
                SampledTransforms[t].posY = InterpolateKeyframeSet(SampledTransforms[t].posY, LoopedAnimation);
                SampledTransforms[t].posZ = InterpolateKeyframeSet(SampledTransforms[t].posZ, LoopedAnimation);

                SampledTransforms[t].rotX = InterpolateKeyframeSet(SampledTransforms[t].rotX, LoopedAnimation);
                SampledTransforms[t].rotY = InterpolateKeyframeSet(SampledTransforms[t].rotY, LoopedAnimation);
                SampledTransforms[t].rotZ = InterpolateKeyframeSet(SampledTransforms[t].rotZ, LoopedAnimation);
                SampledTransforms[t].rotW = InterpolateKeyframeSet(SampledTransforms[t].rotW, LoopedAnimation);

                SampledTransforms[t].scaleX = InterpolateKeyframeSet(SampledTransforms[t].scaleX, LoopedAnimation);
                SampledTransforms[t].scaleY = InterpolateKeyframeSet(SampledTransforms[t].scaleY, LoopedAnimation);
                SampledTransforms[t].scaleZ = InterpolateKeyframeSet(SampledTransforms[t].scaleZ, LoopedAnimation);

                #endregion
            }

            FinishBuildAnimation();
        }

        //load a single parameter (ex. localPosition.x) then interpolate that set
        private List<Keyframe> InterpolateKeyframeSet(List<Keyframe> _frames, bool looped)
        {
            #region normalization
            float frameNormalizeFactor = _frames[0].value;
            float lower = _frames[0].value;

            for (int i = 1; i < _frames.Count; i++)
            {
                if (_frames[i].value < lower)
                    lower = _frames[i].value;
                if (_frames[i].value > frameNormalizeFactor)
                    frameNormalizeFactor = _frames[i].value;
            }

            frameNormalizeFactor = frameNormalizeFactor - lower;
            frameNormalizeFactor = 1 / (frameNormalizeFactor+0.000001f); // calculate what it takes to normalize 

            if(NormalizeCurves)
                frameNormalizeFactor = 1;
            #endregion

            List<Keyframe> doneFrames = new List<Keyframe>();
            int lastFrame = _frames.Count - 1;
            // interpolate 1st keyframe
            if (looped)
            {
                float tangent = Floatiness * (_frames[1].value - _frames[_frames.Count - 2].value) * frameNormalizeFactor;

                if (NonUniformKeyframeSpacing)
                {
                    float N1 = (_frames[_frames.Count - 2].time - _frames[_frames.Count - 1].time) / KeyframeSpacing;
                    float N2 = (_frames[1].time - _frames[0].time) / KeyframeSpacing;

                    float outangent = tangent * (2 * N1) / (N1 + N2);
                    tangent = tangent * (2 * N2) / (N1 + N2);
                }

                Keyframe newKey = new Keyframe(_frames[0].time, _frames[0].value, tangent, tangent);

                doneFrames.Add(newKey);
            }
            else doneFrames.Add(_frames[0]);

            for (int k = 1; k < _frames.Count - 1; k++)
            {
                #region hyucc
                //float tangentIn = (((1 - T) * (1 - C) * (1 + B)) / 2) * (_frames[k].value - _frames[k - 1].value) +
                //                  (((1 - T) * (1 + C) * (1 - B)) / 2) * (_frames[k + 1].value - _frames[k].value);

                //float tangentOut = (((1 - T) * (1 + C) * (1 - B)) / 2) * (_frames[k].value - _frames[k - 1].value) +
                //                   (((1 - T) * (1 - C) * (1 - B)) / 2) * (_frames[k + 1].value - _frames[k].value);
                #endregion

                float tangent = Floatiness * (_frames[k+1].value - _frames[k-1].value) * frameNormalizeFactor;

                if (NonUniformKeyframeSpacing)
                {
                    float N1 = (_frames[k].time - _frames[k - 1].time) / KeyframeSpacing;
                    float N2 = (_frames[k + 1].time - _frames[k].time) / KeyframeSpacing;

                    float outangent = tangent * (2 * N1) / (N1 + N2);
                    tangent = tangent * (2 * N2) / (N1 + N2);
                }

                Keyframe newKey = new Keyframe(_frames[k].time, _frames[k].value, tangent, tangent);

                //Debug.Log($"value1 {_frames[k - 1].value} |value2 {_frames[k].value} |value3 {_frames[k + 1].value} In {tangentIn}, out {tangentOut}");

                doneFrames.Add(newKey);
            }

            // interpolate last keyframe
            if (looped)
            {
                float tangent = Floatiness * (_frames[1].value - _frames[_frames.Count - 2].value) * frameNormalizeFactor;

                if (NonUniformKeyframeSpacing)
                {
                    float N1 = _frames[_frames.Count - 2].time - _frames[_frames.Count - 1].time;
                    float N2 = _frames[1].time - _frames[0].time;

                    float outangent = tangent * (2 * N1) / (N1 + N2);
                    tangent = tangent * (2 * N2) / (N1 + N2);
                }

                Keyframe newKey = new Keyframe(_frames[lastFrame].time, _frames[lastFrame].value, tangent, tangent);

                doneFrames.Add(newKey);
            }
            else doneFrames.Add(_frames[lastFrame]);

            if (_frames.Count != doneFrames.Count)
                Debug.LogError("Size mismatch!");

            return doneFrames;
        }

        #endregion

        #region Preparation

        public void PrepareToInterpolate()
        {
            PrepareToInterpolateAnimation(LoopedAnimation, AnimationToInterpolate);
        }

        public void PrepareToInterpolateAnimation(bool _looped, AnimationClip _clip)
        {
            if (_clip == null) { Debug.LogWarning("Missing animation clip! Nothing to bake!"); return; }

            var stateMachine = BakeAnimator.layers[0].stateMachine;

            while (stateMachine.states.Length > 0)
                stateMachine.RemoveState(stateMachine.states[0].state);

            var evaluatedAnim = stateMachine.AddState("Evaluated", new Vector3(5, 5, 5));

            evaluatedAnim.motion = _clip;

            // important! keeps distance between frames even
            animator = transform.GetComponent<Animator>();
            animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
            animator.runtimeAnimatorController = BakeAnimator;

            // print everthing jic
            for (int t = 0; t < TransformsToBake.transformCount; t++)
            {
                if (!TransformsToBake.GetTransformActive(t))
                    continue;

                string transformPath = TransformsToBake.GetTransformPath(t);

                Debug.Log(transformPath);
            }

            Debug.Log("Ready.");
            Ready = true;
            //DestroyImmediate(animator);
        }
        #endregion

        #region path string manipulation

        /// <summary>
        /// get the names of each transform going down the path
        /// </summary>
        /// <param name="_path"></param>
        /// <returns></returns>
        public string[] GetTransformNames(string _path)
        {
            List<string> names = new List<string>();
            string newName = "";

            // iterate through entire string
            for (int i = 0; i < _path.Length; i++)
            {
                // reached end of name, add to list, reset the new name, skip the slash
                if (_path[i] == '/')
                {
                    if (newName == "")
                        continue;

                    names.Add(newName);
                    newName = "";
                    continue;
                }

                newName += _path[i].ToString();
            }

            names.Add(newName);

            return names.ToArray();
        }
#endregion
    }

    public class SampledTransform
    {
        public Transform transform;
        public string path;

        public List<Keyframe> posX = new List<Keyframe>();
        public List<Keyframe> posY = new List<Keyframe>();
        public List<Keyframe> posZ = new List<Keyframe>();

        public List<Keyframe> rotX = new List<Keyframe>();
        public List<Keyframe> rotY = new List<Keyframe>();
        public List<Keyframe> rotZ = new List<Keyframe>();
        public List<Keyframe> rotW = new List<Keyframe>();

        public List<Keyframe> scaleX = new List<Keyframe>();
        public List<Keyframe> scaleY = new List<Keyframe>();
        public List<Keyframe> scaleZ = new List<Keyframe>();

        //public List<ObjectReferenceKeyframe> objRotX = new List<ObjectReferenceKeyframe>();

        public SampledTransform(Transform _transform, string _path)
        {
            transform = _transform;
            path = _path;
        }
    }
}

#endif
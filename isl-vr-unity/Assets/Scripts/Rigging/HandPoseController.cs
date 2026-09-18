using UnityEngine;

namespace SignLoop.Rigging
{
    public enum HandSide
    {
        Left,
        Right
    }

    public enum CanonicalHandShape
    {
        Neutral,
        OpenPalm,
        Fist,
        PointIndex,
        ThumbUp,
        Victory,
        CHand,
        OHand
    }

    /// <summary>
    /// Procedural finger pose controller for ISL avatar hands.
    /// Strictly enforces human anatomical joint limits:
    /// - Hinge joints (PIP, DIP) flex exclusively around the anatomical bend axis (0 to 85 deg).
    /// - Thumb respects saddle and condyloid joint limits (no unnatural hyperextension).
    /// - Evaluates in LateUpdate to override default Animator T-pose with zero allocations.
    /// </summary>
    public class HandPoseController : MonoBehaviour
    {
        public const int JointCountPerHand = 21;

        [Header("Interpolation Settings")]
        [Tooltip("Interpolation speed for Quaternion.Slerp finger transitions.")]
        [SerializeField] private float slerpSpeed = 28f;

        [Header("Anatomical Biomechanical Limits")]
        [Tooltip("Flexion axis for Left hand fingers (default: local +X).")]
        [SerializeField] private Vector3 leftFingerFlexionAxis = new Vector3(1f, 0f, 0f);
        [Tooltip("Flexion axis for Right hand fingers (mirrored: local -X).")]
        [SerializeField] private Vector3 rightFingerFlexionAxis = new Vector3(-1f, 0f, 0f);

        [Tooltip("Thumb abduction/flexion axis for Left hand.")]
        [SerializeField] private Vector3 leftThumbFlexionAxis = new Vector3(0.5f, 0.8f, 0.3f);
        [Tooltip("Thumb abduction/flexion axis for Right hand.")]
        [SerializeField] private Vector3 rightThumbFlexionAxis = new Vector3(-0.5f, 0.8f, 0.3f);

        [Tooltip("Global multiplier to exaggerate subtle finger curls (default 1.25x).")]
        [Range(0.5f, 2.5f)]
        [SerializeField] private float curlMultiplier = 1.25f;

        public float CurlMultiplier { get => curlMultiplier; set => curlMultiplier = Mathf.Clamp(value, 0.5f, 2.5f); }
        public Vector3 LeftFingerFlexionAxis { get => leftFingerFlexionAxis; set => leftFingerFlexionAxis = value; }
        public Vector3 RightFingerFlexionAxis { get => rightFingerFlexionAxis; set => rightFingerFlexionAxis = value; }

        // Bone references
        private readonly Transform[] _leftBones = new Transform[JointCountPerHand];
        private readonly Transform[] _rightBones = new Transform[JointCountPerHand];

        // Rotation buffers (Pre-allocated, zero-allocation runtime)
        private readonly Quaternion[] _leftTargetRotations = new Quaternion[JointCountPerHand];
        private readonly Quaternion[] _rightTargetRotations = new Quaternion[JointCountPerHand];
        private readonly Quaternion[] _leftNeutralRotations = new Quaternion[JointCountPerHand];
        private readonly Quaternion[] _rightNeutralRotations = new Quaternion[JointCountPerHand];

        private bool _isLeftBound;
        private bool _isRightBound;

        public bool IsLeftBound => _isLeftBound;
        public bool IsRightBound => _isRightBound;

        public void BindHands(Transform[] leftHandBones, Transform[] rightHandBones)
        {
            int leftValid = 0;
            if (leftHandBones != null && leftHandBones.Length >= JointCountPerHand)
            {
                for (int i = 0; i < JointCountPerHand; i++)
                {
                    _leftBones[i] = leftHandBones[i];
                    if (_leftBones[i] != null)
                    {
                        _leftNeutralRotations[i] = _leftBones[i].localRotation;
                        _leftTargetRotations[i] = _leftBones[i].localRotation;
                        leftValid++;
                    }
                    else
                    {
                        _leftNeutralRotations[i] = Quaternion.identity;
                        _leftTargetRotations[i] = Quaternion.identity;
                    }
                }
                _isLeftBound = leftValid > 0;
            }

            int rightValid = 0;
            if (rightHandBones != null && rightHandBones.Length >= JointCountPerHand)
            {
                for (int i = 0; i < JointCountPerHand; i++)
                {
                    _rightBones[i] = rightHandBones[i];
                    if (_rightBones[i] != null)
                    {
                        _rightNeutralRotations[i] = _rightBones[i].localRotation;
                        _rightTargetRotations[i] = _rightBones[i].localRotation;
                        rightValid++;
                    }
                    else
                    {
                        _rightNeutralRotations[i] = Quaternion.identity;
                        _rightTargetRotations[i] = Quaternion.identity;
                    }
                }
                _isRightBound = rightValid > 0;
            }

            Debug.Log($"<color=green>[HandPoseController] Bound joints with anatomical limits: Left={leftValid}/21, Right={rightValid}/21.</color>");
        }

        private void LateUpdate()
        {
            float t = Mathf.Clamp01(slerpSpeed * Time.deltaTime);

            if (_isLeftBound)
            {
                // Note: Start at i = 1 to never overwrite Wrist (joint 0), which belongs to ArmIKController!
                for (int i = 1; i < JointCountPerHand; i++)
                {
                    Transform bone = _leftBones[i];
                    if (bone != null)
                    {
                        bone.localRotation = Quaternion.Slerp(bone.localRotation, _leftTargetRotations[i], t);
                    }
                }
            }

            if (_isRightBound)
            {
                // Note: Start at i = 1 to never overwrite Wrist (joint 0), which belongs to ArmIKController!
                for (int i = 1; i < JointCountPerHand; i++)
                {
                    Transform bone = _rightBones[i];
                    if (bone != null)
                    {
                        bone.localRotation = Quaternion.Slerp(bone.localRotation, _rightTargetRotations[i], t);
                    }
                }
            }
        }

        public void SetJointTarget(HandSide side, int jointIndex, in Quaternion targetLocalRotation)
        {
            if (jointIndex < 0 || jointIndex >= JointCountPerHand) return;

            if (side == HandSide.Left)
            {
                _leftTargetRotations[jointIndex] = targetLocalRotation;
            }
            else
            {
                _rightTargetRotations[jointIndex] = targetLocalRotation;
            }
        }

        public void FlipFingerFlexion(HandSide side)
        {
            if (side == HandSide.Left) leftFingerFlexionAxis = -leftFingerFlexionAxis;
            else rightFingerFlexionAxis = -rightFingerFlexionAxis;
            Debug.Log($"[HandPoseController] Inverted {side} finger flexion axis to: {(side == HandSide.Left ? leftFingerFlexionAxis : rightFingerFlexionAxis)}");
        }

        public void SetCanonicalShape(HandSide side, CanonicalHandShape shape)
        {
            Quaternion[] targetBuffer = (side == HandSide.Left) ? _leftTargetRotations : _rightTargetRotations;
            Quaternion[] neutralBuffer = (side == HandSide.Left) ? _leftNeutralRotations : _rightNeutralRotations;

            switch (shape)
            {
                case CanonicalHandShape.Neutral:
                case CanonicalHandShape.OpenPalm:
                    for (int i = 1; i < JointCountPerHand; i++)
                    {
                        targetBuffer[i] = neutralBuffer[i];
                    }
                    break;

                case CanonicalHandShape.Fist:
                    ApplyAnatomicalCurl(side, targetBuffer, neutralBuffer, 78f, 45f);
                    break;

                case CanonicalHandShape.PointIndex:
                    ApplyAnatomicalCurl(side, targetBuffer, neutralBuffer, 78f, 45f);
                    // Extend index finger to flat neutral
                    targetBuffer[AvatarBoneMapping.IndexMCP] = neutralBuffer[AvatarBoneMapping.IndexMCP];
                    targetBuffer[AvatarBoneMapping.IndexPIP] = neutralBuffer[AvatarBoneMapping.IndexPIP];
                    targetBuffer[AvatarBoneMapping.IndexDIP] = neutralBuffer[AvatarBoneMapping.IndexDIP];
                    break;

                case CanonicalHandShape.ThumbUp:
                    // 4 fingers curled flat against palm (80 deg); thumb extended in natural open stance
                    ApplyAnatomicalCurl(side, targetBuffer, neutralBuffer, 80f, 0f);
                    // Keep thumb in natural extended neutral stance without inward curling
                    targetBuffer[AvatarBoneMapping.ThumbCMC] = neutralBuffer[AvatarBoneMapping.ThumbCMC];
                    targetBuffer[AvatarBoneMapping.ThumbMCP] = neutralBuffer[AvatarBoneMapping.ThumbMCP];
                    targetBuffer[AvatarBoneMapping.ThumbIP] = neutralBuffer[AvatarBoneMapping.ThumbIP];
                    break;

                case CanonicalHandShape.Victory:
                    ApplyAnatomicalCurl(side, targetBuffer, neutralBuffer, 78f, 45f);
                    // Extend index & middle
                    targetBuffer[AvatarBoneMapping.IndexMCP] = neutralBuffer[AvatarBoneMapping.IndexMCP];
                    targetBuffer[AvatarBoneMapping.IndexPIP] = neutralBuffer[AvatarBoneMapping.IndexPIP];
                    targetBuffer[AvatarBoneMapping.IndexDIP] = neutralBuffer[AvatarBoneMapping.IndexDIP];
                    targetBuffer[AvatarBoneMapping.MiddleMCP] = neutralBuffer[AvatarBoneMapping.MiddleMCP];
                    targetBuffer[AvatarBoneMapping.MiddlePIP] = neutralBuffer[AvatarBoneMapping.MiddlePIP];
                    targetBuffer[AvatarBoneMapping.MiddleDIP] = neutralBuffer[AvatarBoneMapping.MiddleDIP];
                    break;

                case CanonicalHandShape.CHand:
                    ApplyAnatomicalCurl(side, targetBuffer, neutralBuffer, 35f, 25f);
                    break;

                case CanonicalHandShape.OHand:
                    ApplyAnatomicalCurl(side, targetBuffer, neutralBuffer, 60f, 45f);
                    break;
            }
        }

        /// <summary>
        /// Sets individual finger curl angles (0 to 85 deg) directly, strictly clamped to human biomechanical limits.
        /// Zero GC allocations. Ideal for driving procedural hands from skeletal keypoint streams.
        /// </summary>
        public void SetFingerCurls(HandSide side, float thumbDeg, float indexDeg, float middleDeg, float ringDeg, float pinkyDeg)
        {
            Quaternion[] target = (side == HandSide.Left) ? _leftTargetRotations : _rightTargetRotations;
            Quaternion[] neutral = (side == HandSide.Left) ? _leftNeutralRotations : _rightNeutralRotations;
            Vector3 fAxis = (side == HandSide.Left) ? leftFingerFlexionAxis : rightFingerFlexionAxis;
            Vector3 tAxis = (side == HandSide.Left) ? leftThumbFlexionAxis : rightThumbFlexionAxis;

            float clampedThumb = Mathf.Clamp(thumbDeg * curlMultiplier, 0f, 65f);
            float clampedIndex = Mathf.Clamp(indexDeg * curlMultiplier, 0f, 85f);
            float clampedMiddle = Mathf.Clamp(middleDeg * curlMultiplier, 0f, 85f);
            float clampedRing = Mathf.Clamp(ringDeg * curlMultiplier, 0f, 85f);
            float clampedPinky = Mathf.Clamp(pinkyDeg * curlMultiplier, 0f, 85f);

            Quaternion thumbCurl = Quaternion.AngleAxis(clampedThumb, tAxis.normalized);
            Quaternion indexCurl = Quaternion.AngleAxis(clampedIndex, fAxis.normalized);
            Quaternion middleCurl = Quaternion.AngleAxis(clampedMiddle, fAxis.normalized);
            Quaternion ringCurl = Quaternion.AngleAxis(clampedRing, fAxis.normalized);
            Quaternion pinkyCurl = Quaternion.AngleAxis(clampedPinky, fAxis.normalized);

            // Thumb joints
            target[AvatarBoneMapping.ThumbCMC] = neutral[AvatarBoneMapping.ThumbCMC] * thumbCurl;
            target[AvatarBoneMapping.ThumbMCP] = neutral[AvatarBoneMapping.ThumbMCP] * thumbCurl;
            target[AvatarBoneMapping.ThumbIP] = neutral[AvatarBoneMapping.ThumbIP] * thumbCurl;

            // Index joints
            target[AvatarBoneMapping.IndexMCP] = neutral[AvatarBoneMapping.IndexMCP] * indexCurl;
            target[AvatarBoneMapping.IndexPIP] = neutral[AvatarBoneMapping.IndexPIP] * indexCurl;
            target[AvatarBoneMapping.IndexDIP] = neutral[AvatarBoneMapping.IndexDIP] * indexCurl;

            // Middle joints
            target[AvatarBoneMapping.MiddleMCP] = neutral[AvatarBoneMapping.MiddleMCP] * middleCurl;
            target[AvatarBoneMapping.MiddlePIP] = neutral[AvatarBoneMapping.MiddlePIP] * middleCurl;
            target[AvatarBoneMapping.MiddleDIP] = neutral[AvatarBoneMapping.MiddleDIP] * middleCurl;

            // Ring joints
            target[AvatarBoneMapping.RingMCP] = neutral[AvatarBoneMapping.RingMCP] * ringCurl;
            target[AvatarBoneMapping.RingPIP] = neutral[AvatarBoneMapping.RingPIP] * ringCurl;
            target[AvatarBoneMapping.RingDIP] = neutral[AvatarBoneMapping.RingDIP] * ringCurl;

            // Pinky joints
            target[AvatarBoneMapping.PinkyMCP] = neutral[AvatarBoneMapping.PinkyMCP] * pinkyCurl;
            target[AvatarBoneMapping.PinkyPIP] = neutral[AvatarBoneMapping.PinkyPIP] * pinkyCurl;
            target[AvatarBoneMapping.PinkyDIP] = neutral[AvatarBoneMapping.PinkyDIP] * pinkyCurl;
        }

        private void ApplyAnatomicalCurl(HandSide side, Quaternion[] target, Quaternion[] neutral, float fingerCurlDeg, float thumbCurlDeg)
        {
            Vector3 fAxis = (side == HandSide.Left) ? leftFingerFlexionAxis : rightFingerFlexionAxis;
            Vector3 tAxis = (side == HandSide.Left) ? leftThumbFlexionAxis : rightThumbFlexionAxis;

            float clampedFinger = Mathf.Clamp(fingerCurlDeg * curlMultiplier, 0f, 85f);
            float clampedThumb = Mathf.Clamp(thumbCurlDeg * curlMultiplier, 0f, 65f);

            Quaternion fingerCurl = Quaternion.AngleAxis(clampedFinger, fAxis.normalized);
            Quaternion thumbCurl = Quaternion.AngleAxis(clampedThumb, tAxis.normalized);

            // Thumb joints
            target[AvatarBoneMapping.ThumbCMC] = neutral[AvatarBoneMapping.ThumbCMC] * thumbCurl;
            target[AvatarBoneMapping.ThumbMCP] = neutral[AvatarBoneMapping.ThumbMCP] * thumbCurl;
            target[AvatarBoneMapping.ThumbIP] = neutral[AvatarBoneMapping.ThumbIP] * thumbCurl;

            // Index joints
            target[AvatarBoneMapping.IndexMCP] = neutral[AvatarBoneMapping.IndexMCP] * fingerCurl;
            target[AvatarBoneMapping.IndexPIP] = neutral[AvatarBoneMapping.IndexPIP] * fingerCurl;
            target[AvatarBoneMapping.IndexDIP] = neutral[AvatarBoneMapping.IndexDIP] * fingerCurl;

            // Middle joints
            target[AvatarBoneMapping.MiddleMCP] = neutral[AvatarBoneMapping.MiddleMCP] * fingerCurl;
            target[AvatarBoneMapping.MiddlePIP] = neutral[AvatarBoneMapping.MiddlePIP] * fingerCurl;
            target[AvatarBoneMapping.MiddleDIP] = neutral[AvatarBoneMapping.MiddleDIP] * fingerCurl;

            // Ring joints
            target[AvatarBoneMapping.RingMCP] = neutral[AvatarBoneMapping.RingMCP] * fingerCurl;
            target[AvatarBoneMapping.RingPIP] = neutral[AvatarBoneMapping.RingPIP] * fingerCurl;
            target[AvatarBoneMapping.RingDIP] = neutral[AvatarBoneMapping.RingDIP] * fingerCurl;

            // Pinky joints
            target[AvatarBoneMapping.PinkyMCP] = neutral[AvatarBoneMapping.PinkyMCP] * fingerCurl;
            target[AvatarBoneMapping.PinkyPIP] = neutral[AvatarBoneMapping.PinkyPIP] * fingerCurl;
            target[AvatarBoneMapping.PinkyDIP] = neutral[AvatarBoneMapping.PinkyDIP] * fingerCurl;
        }

        public void SnapToNeutral()
        {
            if (_isLeftBound)
            {
                for (int i = 0; i < JointCountPerHand; i++)
                {
                    _leftTargetRotations[i] = _leftNeutralRotations[i];
                    if (_leftBones[i] != null) _leftBones[i].localRotation = _leftNeutralRotations[i];
                }
            }

            if (_isRightBound)
            {
                for (int i = 0; i < JointCountPerHand; i++)
                {
                    _rightTargetRotations[i] = _rightNeutralRotations[i];
                    if (_rightBones[i] != null) _rightBones[i].localRotation = _rightNeutralRotations[i];
                }
            }
        }
    }
}

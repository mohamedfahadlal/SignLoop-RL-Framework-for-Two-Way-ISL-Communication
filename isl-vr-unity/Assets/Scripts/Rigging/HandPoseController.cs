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
        [Tooltip("Flexion axis for Left hand fingers (local +X bends into palm in Avaturn humanoid rig).")]
        [SerializeField] private Vector3 leftFingerFlexionAxis = new Vector3(1f, 0f, 0f);
        [Tooltip("Flexion axis for Right hand fingers (local +X bends into palm in Avaturn humanoid rig).")]
        [SerializeField] private Vector3 rightFingerFlexionAxis = new Vector3(1f, 0f, 0f);

        [Tooltip("Thumb abduction/flexion axis for Left hand.")]
        [SerializeField] private Vector3 leftThumbFlexionAxis = new Vector3(0.7f, -0.2f, 0.6f);
        [Tooltip("Thumb abduction/flexion axis for Right hand.")]
        [SerializeField] private Vector3 rightThumbFlexionAxis = new Vector3(0.7f, 0.2f, -0.6f);

        [Tooltip("Global multiplier for finger curls (default 1.0x).")]
        [Range(0.5f, 2.0f)]
        [SerializeField] private float curlMultiplier = 1.0f;

        public float CurlMultiplier { get => curlMultiplier; set => curlMultiplier = Mathf.Clamp(value, 0.5f, 2.0f); }
        public Vector3 LeftFingerFlexionAxis { get => leftFingerFlexionAxis; set => leftFingerFlexionAxis = value; }
        public Vector3 RightFingerFlexionAxis { get => rightFingerFlexionAxis; set => rightFingerFlexionAxis = value; }
        public Vector3 LeftThumbFlexionAxis { get => leftThumbFlexionAxis; set => leftThumbFlexionAxis = value; }
        public Vector3 RightThumbFlexionAxis { get => rightThumbFlexionAxis; set => rightThumbFlexionAxis = value; }

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
            if (side == HandSide.Left)
            {
                leftFingerFlexionAxis = -leftFingerFlexionAxis;
                leftThumbFlexionAxis = -leftThumbFlexionAxis;
            }
            else
            {
                rightFingerFlexionAxis = -rightFingerFlexionAxis;
                rightThumbFlexionAxis = -rightThumbFlexionAxis;
            }
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
        /// Applies hierarchical anatomical joint distribution: MCP (35%), PIP (50%), DIP (35%)
        /// so fingers curl naturally into the palm without impossible hyperextension or self-intersection.
        /// </summary>
        public void SetFingerCurls(HandSide side, float thumbDeg, float indexDeg, float middleDeg, float ringDeg, float pinkyDeg)
        {
            Quaternion[] target = (side == HandSide.Left) ? _leftTargetRotations : _rightTargetRotations;
            Quaternion[] neutral = (side == HandSide.Left) ? _leftNeutralRotations : _rightNeutralRotations;
            Vector3 fAxis = (side == HandSide.Left) ? leftFingerFlexionAxis.normalized : rightFingerFlexionAxis.normalized;
            Vector3 tAxis = (side == HandSide.Left) ? leftThumbFlexionAxis.normalized : rightThumbFlexionAxis.normalized;

            // Strict anatomical positive clamping: flexion ONLY towards palm, 0 to human joint limits
            float cThumb = Mathf.Clamp(thumbDeg * curlMultiplier, 0f, 65f);
            float cIndex = Mathf.Clamp(indexDeg * curlMultiplier, 0f, 85f);
            float cMiddle = Mathf.Clamp(middleDeg * curlMultiplier, 0f, 85f);
            float cRing = Mathf.Clamp(ringDeg * curlMultiplier, 0f, 85f);
            float cPinky = Mathf.Clamp(pinkyDeg * curlMultiplier, 0f, 85f);

            // --- THUMB: 3-Joint Anatomical Opposition & Flexion ---
            float tCMC = cThumb * 0.30f;
            float tMCP = cThumb * 0.45f;
            float tIP = cThumb * 0.35f;
            target[AvatarBoneMapping.ThumbCMC] = neutral[AvatarBoneMapping.ThumbCMC] * Quaternion.AngleAxis(tCMC, tAxis);
            target[AvatarBoneMapping.ThumbMCP] = neutral[AvatarBoneMapping.ThumbMCP] * Quaternion.AngleAxis(tMCP, tAxis);
            target[AvatarBoneMapping.ThumbIP] = neutral[AvatarBoneMapping.ThumbIP] * Quaternion.AngleAxis(tIP, tAxis);

            // --- INDEX FINGER: Knuckle (35%), PIP (50%), DIP (35%) ---
            target[AvatarBoneMapping.IndexMCP] = neutral[AvatarBoneMapping.IndexMCP] * Quaternion.AngleAxis(cIndex * 0.35f, fAxis);
            target[AvatarBoneMapping.IndexPIP] = neutral[AvatarBoneMapping.IndexPIP] * Quaternion.AngleAxis(cIndex * 0.50f, fAxis);
            target[AvatarBoneMapping.IndexDIP] = neutral[AvatarBoneMapping.IndexDIP] * Quaternion.AngleAxis(cIndex * 0.35f, fAxis);

            // --- MIDDLE FINGER ---
            target[AvatarBoneMapping.MiddleMCP] = neutral[AvatarBoneMapping.MiddleMCP] * Quaternion.AngleAxis(cMiddle * 0.35f, fAxis);
            target[AvatarBoneMapping.MiddlePIP] = neutral[AvatarBoneMapping.MiddlePIP] * Quaternion.AngleAxis(cMiddle * 0.50f, fAxis);
            target[AvatarBoneMapping.MiddleDIP] = neutral[AvatarBoneMapping.MiddleDIP] * Quaternion.AngleAxis(cMiddle * 0.35f, fAxis);

            // --- RING FINGER ---
            target[AvatarBoneMapping.RingMCP] = neutral[AvatarBoneMapping.RingMCP] * Quaternion.AngleAxis(cRing * 0.35f, fAxis);
            target[AvatarBoneMapping.RingPIP] = neutral[AvatarBoneMapping.RingPIP] * Quaternion.AngleAxis(cRing * 0.50f, fAxis);
            target[AvatarBoneMapping.RingDIP] = neutral[AvatarBoneMapping.RingDIP] * Quaternion.AngleAxis(cRing * 0.35f, fAxis);

            // --- PINKY FINGER ---
            target[AvatarBoneMapping.PinkyMCP] = neutral[AvatarBoneMapping.PinkyMCP] * Quaternion.AngleAxis(cPinky * 0.35f, fAxis);
            target[AvatarBoneMapping.PinkyPIP] = neutral[AvatarBoneMapping.PinkyPIP] * Quaternion.AngleAxis(cPinky * 0.50f, fAxis);
            target[AvatarBoneMapping.PinkyDIP] = neutral[AvatarBoneMapping.PinkyDIP] * Quaternion.AngleAxis(cPinky * 0.35f, fAxis);
        }

        private void ApplyAnatomicalCurl(HandSide side, Quaternion[] target, Quaternion[] neutral, float fingerCurlDeg, float thumbCurlDeg)
        {
            Vector3 fAxis = (side == HandSide.Left) ? leftFingerFlexionAxis.normalized : rightFingerFlexionAxis.normalized;
            Vector3 tAxis = (side == HandSide.Left) ? leftThumbFlexionAxis.normalized : rightThumbFlexionAxis.normalized;

            float cFinger = Mathf.Clamp(fingerCurlDeg * curlMultiplier, 0f, 85f);
            float cThumb = Mathf.Clamp(thumbCurlDeg * curlMultiplier, 0f, 65f);

            // Thumb joints
            target[AvatarBoneMapping.ThumbCMC] = neutral[AvatarBoneMapping.ThumbCMC] * Quaternion.AngleAxis(cThumb * 0.30f, tAxis);
            target[AvatarBoneMapping.ThumbMCP] = neutral[AvatarBoneMapping.ThumbMCP] * Quaternion.AngleAxis(cThumb * 0.45f, tAxis);
            target[AvatarBoneMapping.ThumbIP] = neutral[AvatarBoneMapping.ThumbIP] * Quaternion.AngleAxis(cThumb * 0.35f, tAxis);

            // 4 Fingers
            float mcpRot = cFinger * 0.35f;
            float pipRot = cFinger * 0.50f;
            float dipRot = cFinger * 0.35f;

            Quaternion qMCP = Quaternion.AngleAxis(mcpRot, fAxis);
            Quaternion qPIP = Quaternion.AngleAxis(pipRot, fAxis);
            Quaternion qDIP = Quaternion.AngleAxis(dipRot, fAxis);

            // Index
            target[AvatarBoneMapping.IndexMCP] = neutral[AvatarBoneMapping.IndexMCP] * qMCP;
            target[AvatarBoneMapping.IndexPIP] = neutral[AvatarBoneMapping.IndexPIP] * qPIP;
            target[AvatarBoneMapping.IndexDIP] = neutral[AvatarBoneMapping.IndexDIP] * qDIP;

            // Middle
            target[AvatarBoneMapping.MiddleMCP] = neutral[AvatarBoneMapping.MiddleMCP] * qMCP;
            target[AvatarBoneMapping.MiddlePIP] = neutral[AvatarBoneMapping.MiddlePIP] * qPIP;
            target[AvatarBoneMapping.MiddleDIP] = neutral[AvatarBoneMapping.MiddleDIP] * qDIP;

            // Ring
            target[AvatarBoneMapping.RingMCP] = neutral[AvatarBoneMapping.RingMCP] * qMCP;
            target[AvatarBoneMapping.RingPIP] = neutral[AvatarBoneMapping.RingPIP] * qPIP;
            target[AvatarBoneMapping.RingDIP] = neutral[AvatarBoneMapping.RingDIP] * qDIP;

            // Pinky
            target[AvatarBoneMapping.PinkyMCP] = neutral[AvatarBoneMapping.PinkyMCP] * qMCP;
            target[AvatarBoneMapping.PinkyPIP] = neutral[AvatarBoneMapping.PinkyPIP] * qPIP;
            target[AvatarBoneMapping.PinkyDIP] = neutral[AvatarBoneMapping.PinkyDIP] * qDIP;
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

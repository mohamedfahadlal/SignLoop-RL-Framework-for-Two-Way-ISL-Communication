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
    /// Manages 21 joints per hand using zero-allocation Quaternion.Slerp interpolation.
    /// Decoupled from the 3D avatar mesh via AvatarBoneMapping.
    /// </summary>
    public class HandPoseController : MonoBehaviour
    {
        public const int JointCountPerHand = 21;

        [Header("Interpolation Settings")]
        [Tooltip("Interpolation speed for Quaternion.Slerp finger transitions.")]
        [SerializeField] private float slerpSpeed = 12f;

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

        /// <summary>
        /// Binds 21 skeletal joints for left and right hands from AvatarBoneMapping.
        /// </summary>
        public void BindHands(Transform[] leftHandBones, Transform[] rightHandBones)
        {
            if (leftHandBones != null && leftHandBones.Length >= JointCountPerHand)
            {
                for (int i = 0; i < JointCountPerHand; i++)
                {
                    _leftBones[i] = leftHandBones[i];
                    if (_leftBones[i] != null)
                    {
                        _leftNeutralRotations[i] = _leftBones[i].localRotation;
                        _leftTargetRotations[i] = _leftBones[i].localRotation;
                    }
                    else
                    {
                        _leftNeutralRotations[i] = Quaternion.identity;
                        _leftTargetRotations[i] = Quaternion.identity;
                    }
                }
                _isLeftBound = true;
            }

            if (rightHandBones != null && rightHandBones.Length >= JointCountPerHand)
            {
                for (int i = 0; i < JointCountPerHand; i++)
                {
                    _rightBones[i] = rightHandBones[i];
                    if (_rightBones[i] != null)
                    {
                        _rightNeutralRotations[i] = _rightBones[i].localRotation;
                        _rightTargetRotations[i] = _rightBones[i].localRotation;
                    }
                    else
                    {
                        _rightNeutralRotations[i] = Quaternion.identity;
                        _rightTargetRotations[i] = Quaternion.identity;
                    }
                }
                _isRightBound = true;
            }

            Debug.Log("[HandPoseController] Initialized and bound 21 joints per hand.");
        }

        private void Update()
        {
            float t = slerpSpeed * Time.deltaTime;

            // Zero-allocation Quaternion.Slerp update loop for left hand
            if (_isLeftBound)
            {
                for (int i = 0; i < JointCountPerHand; i++)
                {
                    Transform bone = _leftBones[i];
                    if (bone != null)
                    {
                        bone.localRotation = Quaternion.Slerp(bone.localRotation, _leftTargetRotations[i], t);
                    }
                }
            }

            // Zero-allocation Quaternion.Slerp update loop for right hand
            if (_isRightBound)
            {
                for (int i = 0; i < JointCountPerHand; i++)
                {
                    Transform bone = _rightBones[i];
                    if (bone != null)
                    {
                        bone.localRotation = Quaternion.Slerp(bone.localRotation, _rightTargetRotations[i], t);
                    }
                }
            }
        }

        /// <summary>
        /// Sets a specific joint's target rotation. Zero allocations.
        /// </summary>
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

        /// <summary>
        /// Applies a canonical ISL hand posture preset to the target buffer.
        /// </summary>
        public void SetCanonicalShape(HandSide side, CanonicalHandShape shape)
        {
            Quaternion[] targetBuffer = (side == HandSide.Left) ? _leftTargetRotations : _rightTargetRotations;
            Quaternion[] neutralBuffer = (side == HandSide.Left) ? _leftNeutralRotations : _rightNeutralRotations;

            switch (shape)
            {
                case CanonicalHandShape.Neutral:
                case CanonicalHandShape.OpenPalm:
                    for (int i = 0; i < JointCountPerHand; i++)
                    {
                        targetBuffer[i] = neutralBuffer[i];
                    }
                    break;

                case CanonicalHandShape.Fist:
                    // Curl all fingers tightly around X axis
                    ApplyCurl(targetBuffer, neutralBuffer, fingerCurlDeg: 80f, thumbCurlDeg: 50f);
                    break;

                case CanonicalHandShape.PointIndex:
                    // Index extended, others curled
                    ApplyCurl(targetBuffer, neutralBuffer, fingerCurlDeg: 80f, thumbCurlDeg: 50f);
                    // Reset Index MCP, PIP, DIP
                    targetBuffer[AvatarBoneMapping.IndexMCP] = neutralBuffer[AvatarBoneMapping.IndexMCP];
                    targetBuffer[AvatarBoneMapping.IndexPIP] = neutralBuffer[AvatarBoneMapping.IndexPIP];
                    targetBuffer[AvatarBoneMapping.IndexDIP] = neutralBuffer[AvatarBoneMapping.IndexDIP];
                    break;

                case CanonicalHandShape.ThumbUp:
                    // All fingers curled, thumb extended upward
                    ApplyCurl(targetBuffer, neutralBuffer, fingerCurlDeg: 80f, thumbCurlDeg: 0f);
                    break;

                case CanonicalHandShape.Victory:
                    // Index & Middle extended, others curled
                    ApplyCurl(targetBuffer, neutralBuffer, fingerCurlDeg: 80f, thumbCurlDeg: 50f);
                    targetBuffer[AvatarBoneMapping.IndexMCP] = neutralBuffer[AvatarBoneMapping.IndexMCP];
                    targetBuffer[AvatarBoneMapping.IndexPIP] = neutralBuffer[AvatarBoneMapping.IndexPIP];
                    targetBuffer[AvatarBoneMapping.IndexDIP] = neutralBuffer[AvatarBoneMapping.IndexDIP];
                    targetBuffer[AvatarBoneMapping.MiddleMCP] = neutralBuffer[AvatarBoneMapping.MiddleMCP];
                    targetBuffer[AvatarBoneMapping.MiddlePIP] = neutralBuffer[AvatarBoneMapping.MiddlePIP];
                    targetBuffer[AvatarBoneMapping.MiddleDIP] = neutralBuffer[AvatarBoneMapping.MiddleDIP];
                    break;

                case CanonicalHandShape.CHand:
                    // Curved open arc
                    ApplyCurl(targetBuffer, neutralBuffer, fingerCurlDeg: 35f, thumbCurlDeg: 25f);
                    break;

                case CanonicalHandShape.OHand:
                    // Tips meeting thumb
                    ApplyCurl(targetBuffer, neutralBuffer, fingerCurlDeg: 60f, thumbCurlDeg: 45f);
                    break;
            }
        }

        private static void ApplyCurl(Quaternion[] target, Quaternion[] neutral, float fingerCurlDeg, float thumbCurlDeg)
        {
            Quaternion fingerCurl = Quaternion.Euler(fingerCurlDeg, 0f, 0f);
            Quaternion thumbCurl = Quaternion.Euler(0f, thumbCurlDeg, thumbCurlDeg * 0.5f);

            // Thumb
            target[AvatarBoneMapping.ThumbCMC] = neutral[AvatarBoneMapping.ThumbCMC] * thumbCurl;
            target[AvatarBoneMapping.ThumbMCP] = neutral[AvatarBoneMapping.ThumbMCP] * thumbCurl;
            target[AvatarBoneMapping.ThumbIP] = neutral[AvatarBoneMapping.ThumbIP] * thumbCurl;

            // Index
            target[AvatarBoneMapping.IndexMCP] = neutral[AvatarBoneMapping.IndexMCP] * fingerCurl;
            target[AvatarBoneMapping.IndexPIP] = neutral[AvatarBoneMapping.IndexPIP] * fingerCurl;
            target[AvatarBoneMapping.IndexDIP] = neutral[AvatarBoneMapping.IndexDIP] * fingerCurl;

            // Middle
            target[AvatarBoneMapping.MiddleMCP] = neutral[AvatarBoneMapping.MiddleMCP] * fingerCurl;
            target[AvatarBoneMapping.MiddlePIP] = neutral[AvatarBoneMapping.MiddlePIP] * fingerCurl;
            target[AvatarBoneMapping.MiddleDIP] = neutral[AvatarBoneMapping.MiddleDIP] * fingerCurl;

            // Ring
            target[AvatarBoneMapping.RingMCP] = neutral[AvatarBoneMapping.RingMCP] * fingerCurl;
            target[AvatarBoneMapping.RingPIP] = neutral[AvatarBoneMapping.RingPIP] * fingerCurl;
            target[AvatarBoneMapping.RingDIP] = neutral[AvatarBoneMapping.RingDIP] * fingerCurl;

            // Pinky
            target[AvatarBoneMapping.PinkyMCP] = neutral[AvatarBoneMapping.PinkyMCP] * fingerCurl;
            target[AvatarBoneMapping.PinkyPIP] = neutral[AvatarBoneMapping.PinkyPIP] * fingerCurl;
            target[AvatarBoneMapping.PinkyDIP] = neutral[AvatarBoneMapping.PinkyDIP] * fingerCurl;
        }

        /// <summary>
        /// Immediately snaps all joints to neutral rotation without interpolation.
        /// </summary>
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

using UnityEngine;

namespace SignLoop.Rigging
{
    /// <summary>
    /// Component placed on avatar prefabs (or auto-generated) that maps skeleton transforms
    /// to standard ISL procedural animation hooks. Decouples the 3D model from the rig controllers.
    /// </summary>
    [DisallowMultipleComponent]
    public class AvatarBoneMapping : MonoBehaviour
    {
        public const int JointCountPerHand = 21;

        // Canonical 21-joint indices per hand matching MediaPipe Holistic
        public const int Wrist = 0;
        public const int ThumbCMC = 1, ThumbMCP = 2, ThumbIP = 3, ThumbTip = 4;
        public const int IndexMCP = 5, IndexPIP = 6, IndexDIP = 7, IndexTip = 8;
        public const int MiddleMCP = 9, MiddlePIP = 10, MiddleDIP = 11, MiddleTip = 12;
        public const int RingMCP = 13, RingPIP = 14, RingDIP = 15, RingTip = 16;
        public const int PinkyMCP = 17, PinkyPIP = 18, PinkyDIP = 19, PinkyTip = 20;

        [Header("Face Mesh (ARKit / FACS)")]
        [Tooltip("SkinnedMeshRenderer containing facial blendshapes.")]
        public SkinnedMeshRenderer faceMesh;

        [Header("Left Arm Skeleton")]
        public Transform leftUpperArm;
        public Transform leftForearm;
        public Transform leftHand;

        [Header("Right Arm Skeleton")]
        public Transform rightUpperArm;
        public Transform rightForearm;
        public Transform rightHand;

        [Header("Left Hand 21 Joints")]
        [Tooltip("21 joint transforms: Wrist (0), Thumb (1-4), Index (5-8), Middle (9-12), Ring (13-16), Pinky (17-20)")]
        public Transform[] leftHandBones = new Transform[JointCountPerHand];

        [Header("Right Hand 21 Joints")]
        [Tooltip("21 joint transforms: Wrist (0), Thumb (1-4), Index (5-8), Middle (9-12), Ring (13-16), Pinky (17-20)")]
        public Transform[] rightHandBones = new Transform[JointCountPerHand];

        /// <summary>
        /// Attempts to auto-detect bone transforms from a Unity Humanoid Animator.
        /// </summary>
        public bool AutoPopulate(Animator animator)
        {
            if (animator == null || !animator.isHuman)
            {
                Debug.LogWarning("[AvatarBoneMapping] Cannot auto-populate: Animator is missing or not configured as Humanoid.");
                return false;
            }

            // Arms
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

            // Left Hand Joints
            leftHandBones[Wrist] = leftHand;
            leftHandBones[ThumbCMC] = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
            leftHandBones[ThumbMCP] = animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
            leftHandBones[ThumbIP] = animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal);
            if (leftHandBones[ThumbIP] != null && leftHandBones[ThumbIP].childCount > 0)
                leftHandBones[ThumbTip] = leftHandBones[ThumbIP].GetChild(0);

            leftHandBones[IndexMCP] = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
            leftHandBones[IndexPIP] = animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate);
            leftHandBones[IndexDIP] = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal);
            if (leftHandBones[IndexDIP] != null && leftHandBones[IndexDIP].childCount > 0)
                leftHandBones[IndexTip] = leftHandBones[IndexDIP].GetChild(0);

            leftHandBones[MiddleMCP] = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
            leftHandBones[MiddlePIP] = animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate);
            leftHandBones[MiddleDIP] = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal);
            if (leftHandBones[MiddleDIP] != null && leftHandBones[MiddleDIP].childCount > 0)
                leftHandBones[MiddleTip] = leftHandBones[MiddleDIP].GetChild(0);

            leftHandBones[RingMCP] = animator.GetBoneTransform(HumanBodyBones.LeftRingProximal);
            leftHandBones[RingPIP] = animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate);
            leftHandBones[RingDIP] = animator.GetBoneTransform(HumanBodyBones.LeftRingDistal);
            if (leftHandBones[RingDIP] != null && leftHandBones[RingDIP].childCount > 0)
                leftHandBones[RingTip] = leftHandBones[RingDIP].GetChild(0);

            leftHandBones[PinkyMCP] = animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal);
            leftHandBones[PinkyPIP] = animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate);
            leftHandBones[PinkyDIP] = animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal);
            if (leftHandBones[PinkyDIP] != null && leftHandBones[PinkyDIP].childCount > 0)
                leftHandBones[PinkyTip] = leftHandBones[PinkyDIP].GetChild(0);

            // Right Hand Joints
            rightHandBones[Wrist] = rightHand;
            rightHandBones[ThumbCMC] = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
            rightHandBones[ThumbMCP] = animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
            rightHandBones[ThumbIP] = animator.GetBoneTransform(HumanBodyBones.RightThumbDistal);
            if (rightHandBones[ThumbIP] != null && rightHandBones[ThumbIP].childCount > 0)
                rightHandBones[ThumbTip] = rightHandBones[ThumbIP].GetChild(0);

            rightHandBones[IndexMCP] = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            rightHandBones[IndexPIP] = animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
            rightHandBones[IndexDIP] = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal);
            if (rightHandBones[IndexDIP] != null && rightHandBones[IndexDIP].childCount > 0)
                rightHandBones[IndexTip] = rightHandBones[IndexDIP].GetChild(0);

            rightHandBones[MiddleMCP] = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            rightHandBones[MiddlePIP] = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
            rightHandBones[MiddleDIP] = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
            if (rightHandBones[MiddleDIP] != null && rightHandBones[MiddleDIP].childCount > 0)
                rightHandBones[MiddleTip] = rightHandBones[MiddleDIP].GetChild(0);

            rightHandBones[RingMCP] = animator.GetBoneTransform(HumanBodyBones.RightRingProximal);
            rightHandBones[RingPIP] = animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate);
            rightHandBones[RingDIP] = animator.GetBoneTransform(HumanBodyBones.RightRingDistal);
            if (rightHandBones[RingDIP] != null && rightHandBones[RingDIP].childCount > 0)
                rightHandBones[RingTip] = rightHandBones[RingDIP].GetChild(0);

            rightHandBones[PinkyMCP] = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
            rightHandBones[PinkyPIP] = animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate);
            rightHandBones[PinkyDIP] = animator.GetBoneTransform(HumanBodyBones.RightLittleDistal);
            if (rightHandBones[PinkyDIP] != null && rightHandBones[PinkyDIP].childCount > 0)
                rightHandBones[PinkyTip] = rightHandBones[PinkyDIP].GetChild(0);

            // Auto-detect face mesh if null
            if (faceMesh == null)
            {
                var smrs = animator.GetComponentsInChildren<SkinnedMeshRenderer>();
                for (int i = 0; i < smrs.Length; i++)
                {
                    if (smrs[i].sharedMesh != null && smrs[i].sharedMesh.blendShapeCount > 10)
                    {
                        faceMesh = smrs[i];
                        break;
                    }
                }
            }

            Debug.Log("[AvatarBoneMapping] Auto-population completed successfully.");
            return true;
        }
    }
}

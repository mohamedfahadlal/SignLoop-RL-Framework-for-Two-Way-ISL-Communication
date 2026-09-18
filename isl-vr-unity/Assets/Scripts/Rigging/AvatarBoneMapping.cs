using System;
using System.Collections.Generic;
using UnityEngine;

namespace SignLoop.Rigging
{
    /// <summary>
    /// Component placed on avatar prefabs (or auto-generated) that maps skeleton transforms
    /// to standard ISL procedural animation hooks. Decouples the 3D model from the rig controllers.
    /// Supports both Unity Humanoid auto-detection and name-based hierarchy fallback.
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
        /// Attempts to auto-detect bone transforms from Humanoid Animator or named hierarchy fallback.
        /// </summary>
        public bool AutoPopulate(Animator animator)
        {
            bool mappedViaHumanoid = false;

            if (animator != null && animator.isHuman)
            {
                mappedViaHumanoid = TryPopulateFromHumanoid(animator);
            }

            if (!mappedViaHumanoid)
            {
                Debug.Log("[AvatarBoneMapping] Humanoid mapping unavailable; executing name-based hierarchy fallback.");
                PopulateByName(transform);
            }

            // Auto-detect face mesh if null
            if (faceMesh == null)
            {
                var smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
                for (int i = 0; i < smrs.Length; i++)
                {
                    if (smrs[i].sharedMesh != null && smrs[i].sharedMesh.blendShapeCount > 0)
                    {
                        faceMesh = smrs[i];
                        break;
                    }
                }
                // Fallback: pick body mesh even if 0 blendshapes
                if (faceMesh == null && smrs.Length > 0)
                {
                    faceMesh = smrs[0];
                }
            }

            Debug.Log($"[AvatarBoneMapping] Mapping complete. FaceMesh: {(faceMesh != null ? faceMesh.name : "None")}, LeftArm: {(leftUpperArm != null ? "Bound" : "Missing")}, RightArm: {(rightUpperArm != null ? "Bound" : "Missing")}.");
            return leftUpperArm != null && rightUpperArm != null;
        }

        private bool TryPopulateFromHumanoid(Animator animator)
        {
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

            // Left Hand
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

            // Right Hand
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

            return leftUpperArm != null && rightUpperArm != null;
        }

        private void PopulateByName(Transform root)
        {
            var dict = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            CollectTransforms(root, dict);

            leftUpperArm = FindInDict(dict, "LeftArm", "LeftUpperArm", "left_arm");
            leftForearm = FindInDict(dict, "LeftForeArm", "LeftLowerArm", "left_forearm");
            leftHand = FindInDict(dict, "LeftHand", "LeftWrist", "left_hand");

            rightUpperArm = FindInDict(dict, "RightArm", "RightUpperArm", "right_arm");
            rightForearm = FindInDict(dict, "RightForeArm", "RightLowerArm", "right_forearm");
            rightHand = FindInDict(dict, "RightHand", "RightWrist", "right_hand");

            // Left Fingers
            leftHandBones[Wrist] = leftHand;
            leftHandBones[ThumbCMC] = FindInDict(dict, "LeftHandThumb1", "LeftThumbProximal");
            leftHandBones[ThumbMCP] = FindInDict(dict, "LeftHandThumb2", "LeftThumbIntermediate");
            leftHandBones[ThumbIP] = FindInDict(dict, "LeftHandThumb3", "LeftThumbDistal");

            leftHandBones[IndexMCP] = FindInDict(dict, "LeftHandIndex1", "LeftIndexProximal");
            leftHandBones[IndexPIP] = FindInDict(dict, "LeftHandIndex2", "LeftIndexIntermediate");
            leftHandBones[IndexDIP] = FindInDict(dict, "LeftHandIndex3", "LeftIndexDistal");

            leftHandBones[MiddleMCP] = FindInDict(dict, "LeftHandMiddle1", "LeftMiddleProximal");
            leftHandBones[MiddlePIP] = FindInDict(dict, "LeftHandMiddle2", "LeftMiddleIntermediate");
            leftHandBones[MiddleDIP] = FindInDict(dict, "LeftHandMiddle3", "LeftMiddleDistal");

            leftHandBones[RingMCP] = FindInDict(dict, "LeftHandRing1", "LeftRingProximal");
            leftHandBones[RingPIP] = FindInDict(dict, "LeftHandRing2", "LeftRingIntermediate");
            leftHandBones[RingDIP] = FindInDict(dict, "LeftHandRing3", "LeftRingDistal");

            leftHandBones[PinkyMCP] = FindInDict(dict, "LeftHandPinky1", "LeftLittleProximal");
            leftHandBones[PinkyPIP] = FindInDict(dict, "LeftHandPinky2", "LeftLittleIntermediate");
            leftHandBones[PinkyDIP] = FindInDict(dict, "LeftHandPinky3", "LeftLittleDistal");

            // Right Fingers
            rightHandBones[Wrist] = rightHand;
            rightHandBones[ThumbCMC] = FindInDict(dict, "RightHandThumb1", "RightThumbProximal");
            rightHandBones[ThumbMCP] = FindInDict(dict, "RightHandThumb2", "RightThumbIntermediate");
            rightHandBones[ThumbIP] = FindInDict(dict, "RightHandThumb3", "RightThumbDistal");

            rightHandBones[IndexMCP] = FindInDict(dict, "RightHandIndex1", "RightIndexProximal");
            rightHandBones[IndexPIP] = FindInDict(dict, "RightHandIndex2", "RightIndexIntermediate");
            rightHandBones[IndexDIP] = FindInDict(dict, "RightHandIndex3", "RightIndexDistal");

            rightHandBones[MiddleMCP] = FindInDict(dict, "RightHandMiddle1", "RightMiddleProximal");
            rightHandBones[MiddlePIP] = FindInDict(dict, "RightHandMiddle2", "RightMiddleIntermediate");
            rightHandBones[MiddleDIP] = FindInDict(dict, "RightHandMiddle3", "RightMiddleDistal");

            rightHandBones[RingMCP] = FindInDict(dict, "RightHandRing1", "RightRingProximal");
            rightHandBones[RingPIP] = FindInDict(dict, "RightHandRing2", "RightRingIntermediate");
            rightHandBones[RingDIP] = FindInDict(dict, "RightHandRing3", "RightRingDistal");

            rightHandBones[PinkyMCP] = FindInDict(dict, "RightHandPinky1", "RightLittleProximal");
            rightHandBones[PinkyPIP] = FindInDict(dict, "RightHandPinky2", "RightLittleIntermediate");
            rightHandBones[PinkyDIP] = FindInDict(dict, "RightHandPinky3", "RightLittleDistal");
        }

        private static void CollectTransforms(Transform current, Dictionary<string, Transform> dict)
        {
            if (!dict.ContainsKey(current.name))
            {
                dict.Add(current.name, current);
            }
            for (int i = 0; i < current.childCount; i++)
            {
                CollectTransforms(current.GetChild(i), dict);
            }
        }

        private static Transform FindInDict(Dictionary<string, Transform> dict, params string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                if (dict.TryGetValue(candidates[i], out var t))
                {
                    return t;
                }
            }
            return null;
        }
    }
}

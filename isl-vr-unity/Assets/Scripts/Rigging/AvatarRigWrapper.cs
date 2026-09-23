using UnityEngine;
using SignLoop.Avatar;

namespace SignLoop.Rigging
{
    /// <summary>
    /// Master Prefab Wrapper for SignLoop procedural ISL avatar.
    /// Decouples avatar mesh/armature from IK, finger pose, and facial animation drivers.
    /// Allows seamless swapping from prototype (54k tris) to production (&lt;=25k tris) meshes
    /// with zero script breakage.
    /// </summary>
    public class AvatarRigWrapper : MonoBehaviour
    {
        [Header("Rig Controllers")]
        [SerializeField] private ArmIKController armIK;
        [SerializeField] private HandPoseController handPose;
        [SerializeField] private ARKitFaceController faceController;
        [SerializeField] private ISLSignPlayer signPlayer;

        [Header("Avatar Container & Instance")]
        [Tooltip("If true, dynamically re-binds if a new avatar prefab is spawned at runtime.")]
        [SerializeField] private bool autoBindOnStart = true;
        
        [Header("Diagnostics")]
        public bool showIKSpheres = true;
        public bool showRigLines = false;

        private Animator _animator;
        private bool _isBound = false;

        [Tooltip("Transform slot where the avatar mesh prefab is parented.")]
        [SerializeField] private Transform avatarSlot;

        [Tooltip("Currently active avatar model in the slot.")]
        [SerializeField] private GameObject currentAvatar;

        public ArmIKController ArmIK => armIK;
        public HandPoseController HandPose => handPose;
        public ARKitFaceController FaceController => faceController;
        public ISLSignPlayer SignPlayer => signPlayer;
        public GameObject CurrentAvatar => currentAvatar;

        private void Awake()
        {
            EnsureDependencies();
            AutoBind();
        }

        private void EnsureDependencies()
        {
            if (armIK == null) armIK = GetComponentInChildren<ArmIKController>();
            if (handPose == null) handPose = GetComponentInChildren<HandPoseController>();
            if (faceController == null) faceController = GetComponentInChildren<ARKitFaceController>();
            if (signPlayer == null) signPlayer = GetComponentInChildren<ISLSignPlayer>();
            if (avatarSlot == null)
            {
                var slotTrans = transform.Find("AvatarSlot");
                avatarSlot = (slotTrans != null) ? slotTrans : transform;
            }
        }

        /// <summary>
        /// Finds and binds the avatar model within the slot or children.
        /// </summary>
        public void AutoBind()
        {
            EnsureDependencies();
            GameObject target = currentAvatar;

            // If currentAvatar is missing or is the empty AvatarSlot container, find the real model inside
            if (target == null || target.name == "AvatarSlot" || target == gameObject)
            {
                target = FindActualAvatarModel();
            }

            if (target != null)
            {
                BindAvatar(target);
            }
            else
            {
                Debug.LogWarning("[AvatarRigWrapper] No avatar model found under avatarSlot or children.");
            }
        }

        private GameObject FindActualAvatarModel()
        {
            Transform container = (avatarSlot != null && avatarSlot != transform) ? avatarSlot : transform;

            // 1. Check direct children of container
            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                if (child.name != "AvatarSlot" && (child.GetComponentInChildren<SkinnedMeshRenderer>() != null || child.GetComponentInChildren<Animator>() != null))
                {
                    return child.gameObject;
                }
            }

            // 2. Search all SkinnedMeshRenderers
            var smrs = container.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (smrs.Length > 0)
            {
                Transform t = smrs[0].transform;
                while (t.parent != null && t.parent != container && t.parent != transform)
                {
                    t = t.parent;
                }
                return t.gameObject;
            }

            return null;
        }

        /// <summary>
        /// Instantiates and binds a new avatar prefab, cleanly replacing the existing one.
        /// </summary>
        public bool SwapAvatar(GameObject newAvatarPrefab)
        {
            if (newAvatarPrefab == null)
            {
                Debug.LogError("[AvatarRigWrapper] Cannot swap avatar: newAvatarPrefab is null.");
                return false;
            }

            // Destroy existing instance
            if (currentAvatar != null)
            {
                Destroy(currentAvatar);
                currentAvatar = null;
            }

            // Instantiate new prefab under slot
            Transform parent = (avatarSlot != null) ? avatarSlot : transform;
            GameObject newInstance = Instantiate(newAvatarPrefab, parent);
            newInstance.transform.localPosition = Vector3.zero;
            newInstance.transform.localRotation = Quaternion.identity;
            newInstance.transform.localScale = Vector3.one;

            return BindAvatar(newInstance);
        }

        /// <summary>
        /// Binds an existing avatar GameObject hierarchy to the rig controllers.
        /// </summary>
        public bool BindAvatar(GameObject avatarInstance)
        {
            if (avatarInstance == null) return false;

            // If passed the container itself, drill down to the actual model
            if (avatarInstance.name == "AvatarSlot" && avatarInstance.transform.childCount > 0)
            {
                avatarInstance = avatarInstance.transform.GetChild(0).gameObject;
            }

            currentAvatar = avatarInstance;

            // Retrieve or create AvatarBoneMapping
            var mapping = currentAvatar.GetComponent<AvatarBoneMapping>();
            if (mapping == null)
            {
                mapping = currentAvatar.AddComponent<AvatarBoneMapping>();
            }

            var animator = currentAvatar.GetComponent<Animator>();
            if (animator == null) animator = currentAvatar.GetComponentInParent<Animator>();
            if (animator == null) animator = GetComponent<Animator>();
            
            _animator = animator;

            mapping.AutoPopulate(animator);

            bool allBound = true;

            // 1. Re-bind Facial Blendshapes
            if (faceController != null)
            {
                if (mapping.faceMesh != null)
                {
                    faceController.Initialize(mapping.faceMesh);
                }
                else
                {
                    Debug.LogWarning("[AvatarRigWrapper] AvatarBoneMapping has no faceMesh assigned.");
                    allBound = false;
                }
            }

            // 2. Re-bind Arm IK Constraints
            if (armIK != null)
            {
                armIK.BindBones(
                    mapping.leftUpperArm, mapping.leftForearm, mapping.leftHand,
                    mapping.rightUpperArm, mapping.rightForearm, mapping.rightHand
                );
            }

            // 3. Re-bind 21-joint Finger Chains
            if (handPose != null)
            {
                handPose.BindHands(mapping.leftHandBones, mapping.rightHandBones);
            }

            Debug.Log($"<color=green>[AvatarRigWrapper] Avatar '{avatarInstance.name}' bound successfully (Complete: {allBound}).</color>");
            return allBound;
        }

        private void OnDrawGizmos()
        {
            if (showIKSpheres && armIK != null && armIK.LeftArmTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(armIK.LeftArmTarget.position, 0.05f);
                Gizmos.DrawWireSphere(armIK.RightArmTarget.position, 0.05f);
                
                Gizmos.color = Color.yellow;
                if (armIK.LeftElbowHint != null) Gizmos.DrawWireSphere(armIK.LeftElbowHint.position, 0.04f);
                if (armIK.RightElbowHint != null) Gizmos.DrawWireSphere(armIK.RightElbowHint.position, 0.04f);
            }

            if (!showRigLines || _animator == null) return;
            
            // Draw Elbow Hint Directional Lines
            if (armIK != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // semi-transparent yellow
                Transform lElbow = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                Transform rElbow = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                if (lElbow != null && armIK.LeftElbowHint != null) Gizmos.DrawLine(lElbow.position, armIK.LeftElbowHint.position);
                if (rElbow != null && armIK.RightElbowHint != null) Gizmos.DrawLine(rElbow.position, armIK.RightElbowHint.position);
            }
            
            Gizmos.color = Color.green;
            DrawBoneLine(HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm);
            DrawBoneLine(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm); // Upper Arm
            DrawBoneLine(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);     // Forearm
            
            DrawBoneLine(HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm);
            DrawBoneLine(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm); // Upper Arm
            DrawBoneLine(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);     // Forearm

            Gizmos.color = Color.cyan;
            DrawFingerLines(true);
            DrawFingerLines(false);
        }

        private void DrawFingerLines(bool isLeft)
        {
            HumanBodyBones hand = isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;

            // Thumb
            DrawBoneLine(hand, isLeft ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal);
            DrawBoneLine(isLeft ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal, isLeft ? HumanBodyBones.LeftThumbIntermediate : HumanBodyBones.RightThumbIntermediate);
            DrawBoneLine(isLeft ? HumanBodyBones.LeftThumbIntermediate : HumanBodyBones.RightThumbIntermediate, isLeft ? HumanBodyBones.LeftThumbDistal : HumanBodyBones.RightThumbDistal);

            // Index
            DrawBoneLine(hand, isLeft ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
            DrawBoneLine(isLeft ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal, isLeft ? HumanBodyBones.LeftIndexIntermediate : HumanBodyBones.RightIndexIntermediate);
            DrawBoneLine(isLeft ? HumanBodyBones.LeftIndexIntermediate : HumanBodyBones.RightIndexIntermediate, isLeft ? HumanBodyBones.LeftIndexDistal : HumanBodyBones.RightIndexDistal);

            // Middle
            DrawBoneLine(hand, isLeft ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            DrawBoneLine(isLeft ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal, isLeft ? HumanBodyBones.LeftMiddleIntermediate : HumanBodyBones.RightMiddleIntermediate);
            DrawBoneLine(isLeft ? HumanBodyBones.LeftMiddleIntermediate : HumanBodyBones.RightMiddleIntermediate, isLeft ? HumanBodyBones.LeftMiddleDistal : HumanBodyBones.RightMiddleDistal);

            // Ring
            DrawBoneLine(hand, isLeft ? HumanBodyBones.LeftRingProximal : HumanBodyBones.RightRingProximal);
            DrawBoneLine(isLeft ? HumanBodyBones.LeftRingProximal : HumanBodyBones.RightRingProximal, isLeft ? HumanBodyBones.LeftRingIntermediate : HumanBodyBones.RightRingIntermediate);
            DrawBoneLine(isLeft ? HumanBodyBones.LeftRingIntermediate : HumanBodyBones.RightRingIntermediate, isLeft ? HumanBodyBones.LeftRingDistal : HumanBodyBones.RightRingDistal);

            // Pinky
            DrawBoneLine(hand, isLeft ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
            DrawBoneLine(isLeft ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal, isLeft ? HumanBodyBones.LeftLittleIntermediate : HumanBodyBones.RightLittleIntermediate);
            DrawBoneLine(isLeft ? HumanBodyBones.LeftLittleIntermediate : HumanBodyBones.RightLittleIntermediate, isLeft ? HumanBodyBones.LeftLittleDistal : HumanBodyBones.RightLittleDistal);
        }

        private void DrawBoneLine(HumanBodyBones parent, HumanBodyBones child)
        {
            if (_animator == null) return;
            Transform p = _animator.GetBoneTransform(parent);
            Transform c = _animator.GetBoneTransform(child);
            if (p != null && c != null)
            {
                Gizmos.DrawLine(p.position, c.position);
                Gizmos.DrawWireSphere(p.position, 0.005f);
            }
        }
    }
}

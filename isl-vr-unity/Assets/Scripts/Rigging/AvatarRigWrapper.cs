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

        [Header("Avatar Container & Instance")]
        [Tooltip("Transform slot where the avatar mesh prefab is parented.")]
        [SerializeField] private Transform avatarSlot;

        [Tooltip("Currently active avatar model in the slot.")]
        [SerializeField] private GameObject currentAvatar;

        public ArmIKController ArmIK => armIK;
        public HandPoseController HandPose => handPose;
        public ARKitFaceController FaceController => faceController;
        public GameObject CurrentAvatar => currentAvatar;

        private void Awake()
        {
            EnsureDependencies();

            if (currentAvatar != null)
            {
                BindAvatar(currentAvatar);
            }
            else if (avatarSlot != null && avatarSlot.childCount > 0)
            {
                BindAvatar(avatarSlot.GetChild(0).gameObject);
            }
        }

        private void EnsureDependencies()
        {
            if (armIK == null) armIK = GetComponentInChildren<ArmIKController>();
            if (handPose == null) handPose = GetComponentInChildren<HandPoseController>();
            if (faceController == null) faceController = GetComponentInChildren<ARKitFaceController>();
            if (avatarSlot == null) avatarSlot = transform;
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

            currentAvatar = avatarInstance;

            // Retrieve or create AvatarBoneMapping
            var mapping = currentAvatar.GetComponent<AvatarBoneMapping>();
            if (mapping == null)
            {
                mapping = currentAvatar.AddComponent<AvatarBoneMapping>();
                var animator = currentAvatar.GetComponent<Animator>();
                if (animator != null)
                {
                    mapping.AutoPopulate(animator);
                }
            }

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

            Debug.Log($"[AvatarRigWrapper] Avatar '{avatarInstance.name}' bound successfully (Complete: {allBound}).");
            return allBound;
        }
    }
}

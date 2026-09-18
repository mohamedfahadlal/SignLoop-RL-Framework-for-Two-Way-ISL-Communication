#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using SignLoop.Avatar;
using SignLoop.Rigging;

namespace SignLoop.Editor
{
    public static class ISLAvatarRigSetupMenu
    {
        private const string PrototypeModelPath = "Assets/Models/AvatarPrototype/model.fbx";

        [MenuItem("SignLoop/Setup ISL Avatar Rig in Scene", false, 10)]
        public static void SetupAvatarRig()
        {
            // Remove previous incomplete setup if exists
            var existing = GameObject.Find("ISL_AvatarRig");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // 1. Create Root Rig
            GameObject root = new GameObject("ISL_AvatarRig");
            Undo.RegisterCreatedObjectUndo(root, "Setup ISL Avatar Rig");

            var wrapper = root.AddComponent<AvatarRigWrapper>();
            var armIK = root.AddComponent<ArmIKController>();
            var handPose = root.AddComponent<HandPoseController>();
            var faceController = root.AddComponent<ARKitFaceController>();
            var tester = root.AddComponent<DesktopGestureTester>();
            
            var rootAnimator = root.GetComponent<Animator>();
            if (rootAnimator == null) rootAnimator = root.AddComponent<Animator>();

            var rigBuilder = root.GetComponent<RigBuilder>();
            if (rigBuilder == null) rigBuilder = root.AddComponent<RigBuilder>();

            // 2. Create Avatar Slot
            GameObject slotObj = new GameObject("AvatarSlot");
            slotObj.transform.SetParent(root.transform, false);

            // 3. Instantiate Prototype Avatar if present
            GameObject avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrototypeModelPath);
            GameObject avatarInstance = null;
            if (avatarPrefab != null)
            {
                avatarInstance = (GameObject)PrefabUtility.InstantiatePrefab(avatarPrefab, slotObj.transform);
                avatarInstance.transform.localPosition = Vector3.zero;
                avatarInstance.transform.localRotation = Quaternion.identity;

                // Sync Humanoid Avatar definition if available
                var avatarAnimator = avatarInstance.GetComponent<Animator>();
                if (avatarAnimator != null && avatarAnimator.avatar != null && rootAnimator != null)
                {
                    rootAnimator.avatar = avatarAnimator.avatar;
                }
            }

            // 4. Create Animation Rig Layer
            GameObject rigLayerObj = new GameObject("IK_RigLayer");
            rigLayerObj.transform.SetParent(root.transform, false);
            var rig = rigLayerObj.AddComponent<Rig>();

            // Left Arm IK Constraint & Targets
            GameObject leftArmIKObj = new GameObject("LeftArm_TwoBoneIK");
            leftArmIKObj.transform.SetParent(rigLayerObj.transform, false);
            var leftConstraint = leftArmIKObj.AddComponent<TwoBoneIKConstraint>();

            GameObject leftTarget = new GameObject("LeftHand_IKTarget");
            leftTarget.transform.SetParent(root.transform, false);
            leftTarget.transform.localPosition = new Vector3(-0.25f, 1.2f, 0.35f);

            GameObject leftHint = new GameObject("LeftElbow_Hint");
            leftHint.transform.SetParent(root.transform, false);
            leftHint.transform.localPosition = new Vector3(-0.4f, 1.2f, -0.1f);

            // Right Arm IK Constraint & Targets
            GameObject rightArmIKObj = new GameObject("RightArm_TwoBoneIK");
            rightArmIKObj.transform.SetParent(rigLayerObj.transform, false);
            var rightConstraint = rightArmIKObj.AddComponent<TwoBoneIKConstraint>();

            GameObject rightTarget = new GameObject("RightHand_IKTarget");
            rightTarget.transform.SetParent(root.transform, false);
            rightTarget.transform.localPosition = new Vector3(0.25f, 1.2f, 0.35f);

            GameObject rightHint = new GameObject("RightElbow_Hint");
            rightHint.transform.SetParent(root.transform, false);
            rightHint.transform.localPosition = new Vector3(0.4f, 1.2f, -0.1f);

            // Wire ArmIKController serialized fields
            var soArmIK = new SerializedObject(armIK);
            soArmIK.FindProperty("armRig").objectReferenceValue = rig;
            soArmIK.FindProperty("rigBuilder").objectReferenceValue = rigBuilder;
            soArmIK.FindProperty("leftArmConstraint").objectReferenceValue = leftConstraint;
            soArmIK.FindProperty("leftArmTarget").objectReferenceValue = leftTarget.transform;
            soArmIK.FindProperty("leftElbowHint").objectReferenceValue = leftHint.transform;
            soArmIK.FindProperty("rightArmConstraint").objectReferenceValue = rightConstraint;
            soArmIK.FindProperty("rightArmTarget").objectReferenceValue = rightTarget.transform;
            soArmIK.FindProperty("rightElbowHint").objectReferenceValue = rightHint.transform;
            soArmIK.ApplyModifiedProperties();

            // Wire RigBuilder layers
            var soRigBuilder = new SerializedObject(rigBuilder);
            var layersProp = soRigBuilder.FindProperty("m_RigLayers");
            layersProp.arraySize = 1;
            var element = layersProp.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("m_Rig").objectReferenceValue = rig;
            element.FindPropertyRelative("m_Active").boolValue = true;
            soRigBuilder.ApplyModifiedProperties();

            // Wire AvatarRigWrapper serialized fields
            var soWrapper = new SerializedObject(wrapper);
            soWrapper.FindProperty("armIK").objectReferenceValue = armIK;
            soWrapper.FindProperty("handPose").objectReferenceValue = handPose;
            soWrapper.FindProperty("faceController").objectReferenceValue = faceController;
            soWrapper.FindProperty("avatarSlot").objectReferenceValue = slotObj.transform;
            soWrapper.FindProperty("currentAvatar").objectReferenceValue = avatarInstance;
            soWrapper.ApplyModifiedProperties();

            // Bind avatar if instantiated
            if (avatarInstance != null)
            {
                wrapper.BindAvatar(avatarInstance);
            }

            Selection.activeGameObject = root;
            Debug.Log("<color=green>[SignLoop] ISL Avatar Rig successfully created and wired in scene!</color>");
        }

        [MenuItem("SignLoop/Attach Desktop Tester to Current Rig", false, 20)]
        public static void AttachTesterToExistingRig()
        {
            var rig = GameObject.Find("ISL_AvatarRig");
            if (rig == null)
            {
                var wrapper = Object.FindFirstObjectByType<AvatarRigWrapper>();
                if (wrapper != null) rig = wrapper.gameObject;
            }

            if (rig == null)
            {
                Debug.LogError("[SignLoop] Could not find ISL_AvatarRig in active scene. Please run 'SignLoop > Setup ISL Avatar Rig in Scene' first.");
                return;
            }

            var tester = rig.GetComponent<DesktopGestureTester>();
            if (tester == null)
            {
                tester = Undo.AddComponent<DesktopGestureTester>(rig);
            }

            Selection.activeGameObject = rig;
            Debug.Log("<color=green>[SignLoop] Attached DesktopGestureTester to " + rig.name + "! Press Play to test gestures and mouse orbit.</color>");
        }
    }
}
#endif

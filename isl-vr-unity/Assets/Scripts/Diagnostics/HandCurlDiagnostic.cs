using UnityEngine;

namespace SignLoop.Diagnostics
{
    [ExecuteAlways]
    public class HandCurlDiagnostic : MonoBehaviour
    {
        [ContextMenu("Run Diagnostic")]
        private void Start()
        {
            RunCheck();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                RunCheck();
            };
        }
#endif

        public static void RunCheck()
        {
            var rig = FindFirstObjectByType<SignLoop.Rigging.AvatarRigWrapper>();
            if (rig == null)
            {
                Debug.LogWarning("[HandCurlDiagnostic] AvatarRigWrapper not found in scene.");
                return;
            }

            rig.AutoBind();
            var hp = rig.HandPose ?? rig.GetComponentInChildren<SignLoop.Rigging.HandPoseController>() ?? FindFirstObjectByType<SignLoop.Rigging.HandPoseController>();
            var mapping = rig.GetComponentInChildren<SignLoop.Rigging.AvatarBoneMapping>() ?? FindFirstObjectByType<SignLoop.Rigging.AvatarBoneMapping>();
            if (mapping == null || hp == null)
            {
                Debug.LogWarning("[HandCurlDiagnostic] Mapping or HandPoseController not found.");
                return;
            }

            Debug.Log("<color=yellow>=== HAND CURL BIOMECHANICAL DIAGNOSTIC ===</color>");

            // Test Left Hand
            TestHand(mapping.leftHandBones, hp.LeftFingerFlexionAxis, hp.LeftThumbFlexionAxis, "LEFT");

            // Test Right Hand
            TestHand(mapping.rightHandBones, hp.RightFingerFlexionAxis, hp.RightThumbFlexionAxis, "RIGHT");
        }

        private static void TestHand(Transform[] bones, Vector3 fAxis, Vector3 tAxis, string sideName)
        {
            if (bones == null || bones.Length < 21) return;

            Transform wrist = bones[SignLoop.Rigging.AvatarBoneMapping.Wrist];
            Transform mcp = bones[SignLoop.Rigging.AvatarBoneMapping.IndexMCP];
            Transform pip = bones[SignLoop.Rigging.AvatarBoneMapping.IndexPIP];
            Transform dip = bones[SignLoop.Rigging.AvatarBoneMapping.IndexDIP];
            Transform tip = (bones[SignLoop.Rigging.AvatarBoneMapping.IndexTip] != null)
                ? bones[SignLoop.Rigging.AvatarBoneMapping.IndexTip]
                : ((dip != null) ? dip : pip);

            if (mcp == null || pip == null || wrist == null) return;

            Vector3 wristPos = wrist.position;
            Vector3 tipNeutralPos = tip.position;
            float distNeutral = Vector3.Distance(tipNeutralPos, wristPos);
            
            Quaternion origMCP = mcp.localRotation;
            Quaternion origPIP = pip.localRotation;
            Quaternion origDIP = (dip != null) ? dip.localRotation : Quaternion.identity;

            Vector3[] testAxes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            
            Debug.Log($"[{sideName} FINGER] Tip->Wrist Neutral: {distNeutral:F3}m");
            
            foreach (var axis in testAxes)
            {
                mcp.localRotation = origMCP * Quaternion.AngleAxis(80f * 0.35f, axis);
                pip.localRotation = origPIP * Quaternion.AngleAxis(80f * 0.50f, axis);
                if (dip != null) dip.localRotation = origDIP * Quaternion.AngleAxis(80f * 0.35f, axis);
                
                float dist = Vector3.Distance(tip.position, wrist.position);
                Debug.Log($"[{sideName}] Axis {axis}: Dist={dist:F3}m (Delta={dist - distNeutral:F3}m)");
            }

            mcp.localRotation = origMCP;
            pip.localRotation = origPIP;
            if (dip != null) dip.localRotation = origDIP;
        }
    }
}

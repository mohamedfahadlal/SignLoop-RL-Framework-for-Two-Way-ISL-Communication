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

            Transform thumbCMC = bones[SignLoop.Rigging.AvatarBoneMapping.ThumbCMC];
            Transform thumbMCP = bones[SignLoop.Rigging.AvatarBoneMapping.ThumbMCP];
            Transform thumbIP = bones[SignLoop.Rigging.AvatarBoneMapping.ThumbIP];
            Transform thumbTip = (bones[SignLoop.Rigging.AvatarBoneMapping.ThumbTip] != null)
                ? bones[SignLoop.Rigging.AvatarBoneMapping.ThumbTip]
                : thumbIP;
            Transform middleMCP = bones[SignLoop.Rigging.AvatarBoneMapping.MiddleMCP];

            if (mcp == null || pip == null || wrist == null) return;

            // 1. TEST FINGERS (Index)
            Vector3 wristPos = wrist.position;
            Vector3 tipNeutralPos = tip.position;

            Quaternion origMCP = mcp.localRotation;
            Quaternion origPIP = pip.localRotation;
            Quaternion origDIP = (dip != null) ? dip.localRotation : Quaternion.identity;

            // Flex +60 deg with fAxis
            mcp.localRotation = origMCP * Quaternion.AngleAxis(60f * 0.35f, fAxis);
            pip.localRotation = origPIP * Quaternion.AngleAxis(60f * 0.50f, fAxis);
            if (dip != null) dip.localRotation = origDIP * Quaternion.AngleAxis(60f * 0.35f, fAxis);
            Vector3 tipCurledPos = tip.position;

            // Flex -60 deg with fAxis
            mcp.localRotation = origMCP * Quaternion.AngleAxis(-60f * 0.35f, fAxis);
            pip.localRotation = origPIP * Quaternion.AngleAxis(-60f * 0.50f, fAxis);
            if (dip != null) dip.localRotation = origDIP * Quaternion.AngleAxis(-60f * 0.35f, fAxis);
            Vector3 tipNegPos = tip.position;

            // Restore finger
            mcp.localRotation = origMCP;
            pip.localRotation = origPIP;
            if (dip != null) dip.localRotation = origDIP;

            float distNeutral = Vector3.Distance(tipNeutralPos, wristPos);
            float distCurledPos = Vector3.Distance(tipCurledPos, wristPos);
            float distCurledNeg = Vector3.Distance(tipNegPos, wristPos);

            Debug.Log($"[{sideName} FINGER] Axis: {fAxis} | Tip->Wrist Neutral: {distNeutral:F3}m | (+Axis): {distCurledPos:F3}m | (-Axis): {distCurledNeg:F3}m");
            if (distCurledPos < distNeutral && distCurledPos < distCurledNeg)
            {
                Debug.Log($"<color=green>[{sideName} FINGER] POSITIVE axis ({fAxis}) curls INTO palm! (tip gets {distNeutral - distCurledPos:F3}m closer to wrist)</color>");
            }
            else if (distCurledNeg < distNeutral && distCurledNeg < distCurledPos)
            {
                Debug.Log($"<color=red>[{sideName} FINGER] NEGATIVE axis ({-fAxis}) curls INTO palm! POSITIVE curls BACKWARDS/HYPEREXTENDS!</color>");
            }

            // 2. TEST THUMB (ThumbTip to MiddleMCP/Palm Center)
            if (thumbCMC != null && thumbMCP != null && thumbIP != null && middleMCP != null)
            {
                Vector3 palmRef = middleMCP.position;
                Vector3 tTipNeutral = thumbTip.position;

                Quaternion origTCMC = thumbCMC.localRotation;
                Quaternion origTMCP = thumbMCP.localRotation;
                Quaternion origTIP = thumbIP.localRotation;

                // Flex +50 deg with tAxis
                thumbCMC.localRotation = origTCMC * Quaternion.AngleAxis(50f * 0.30f, tAxis);
                thumbMCP.localRotation = origTMCP * Quaternion.AngleAxis(50f * 0.45f, tAxis);
                thumbIP.localRotation = origTIP * Quaternion.AngleAxis(50f * 0.35f, tAxis);
                Vector3 tTipPos = thumbTip.position;

                // Flex -50 deg with tAxis
                thumbCMC.localRotation = origTCMC * Quaternion.AngleAxis(-50f * 0.30f, tAxis);
                thumbMCP.localRotation = origTMCP * Quaternion.AngleAxis(-50f * 0.45f, tAxis);
                thumbIP.localRotation = origTIP * Quaternion.AngleAxis(-50f * 0.35f, tAxis);
                Vector3 tTipNeg = thumbTip.position;

                // Restore thumb
                thumbCMC.localRotation = origTCMC;
                thumbMCP.localRotation = origTMCP;
                thumbIP.localRotation = origTIP;

                float tDistNeutral = Vector3.Distance(tTipNeutral, palmRef);
                float tDistPos = Vector3.Distance(tTipPos, palmRef);
                float tDistNeg = Vector3.Distance(tTipNeg, palmRef);

                Debug.Log($"[{sideName} THUMB] Axis: {tAxis} | Tip->Palm Neutral: {tDistNeutral:F3}m | (+Axis): {tDistPos:F3}m | (-Axis): {tDistNeg:F3}m");
                if (tDistPos < tDistNeutral && tDistPos < tDistNeg)
                {
                    Debug.Log($"<color=green>[{sideName} THUMB] POSITIVE axis ({tAxis}) opposes INTO palm! (tip gets {tDistNeutral - tDistPos:F3}m closer to palm)</color>");
                }
                else if (tDistNeg < tDistNeutral && tDistNeg < tDistPos)
                {
                    Debug.Log($"<color=red>[{sideName} THUMB] NEGATIVE axis ({-tAxis}) opposes INTO palm! POSITIVE abducts away!</color>");
                }
            }
        }
    }
}

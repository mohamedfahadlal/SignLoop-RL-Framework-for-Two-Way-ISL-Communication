using UnityEngine;
using System.Text;

namespace SignLoop.Diagnostics
{
    public class AvatarRigValidator : MonoBehaviour
    {
        public static void ValidateRig(Animator anim)
        {
            if (anim == null)
            {
                Debug.LogError("<b>[Rig Validator]</b> No Animator found! Cannot validate.");
                return;
            }

            if (!anim.isHuman)
            {
                Debug.LogError("<b>[Rig Validator]</b> Animator is NOT configured as Humanoid! ISL requires a Humanoid rig.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b><color=cyan>=== AVATAR RIG VALIDATION REPORT ===</color></b>");

            // Check core arm bones
            bool hasArms = CheckBone(anim, HumanBodyBones.LeftUpperArm, sb) &&
                           CheckBone(anim, HumanBodyBones.LeftLowerArm, sb) &&
                           CheckBone(anim, HumanBodyBones.LeftHand, sb) &&
                           CheckBone(anim, HumanBodyBones.RightUpperArm, sb) &&
                           CheckBone(anim, HumanBodyBones.RightLowerArm, sb) &&
                           CheckBone(anim, HumanBodyBones.RightHand, sb);

            if (!hasArms) sb.AppendLine("<color=red>CRITICAL ERROR: Missing core arm/hand bones.</color>");
            else sb.AppendLine("<color=green>Core Arms: OK</color>");

            // Check fingers
            int missingLeftFingers = CountMissingFingers(anim, true);
            int missingRightFingers = CountMissingFingers(anim, false);

            if (missingLeftFingers > 0 || missingRightFingers > 0)
            {
                sb.AppendLine($"<color=red>FINGERS INCOMPLETE: Left missing {missingLeftFingers}, Right missing {missingRightFingers}.</color>");
                sb.AppendLine("<b>Warning:</b> This model lacks a full 21-joint skeleton. ISL signs will look deformed.");
            }
            else
            {
                sb.AppendLine("<color=green>Finger Rigging: PERFECT (All 30 finger phalanges found).</color>");
                AnalyzeFingerAxes(anim, sb);
            }

            Debug.Log(sb.ToString());
        }

        private static bool CheckBone(Animator anim, HumanBodyBones bone, StringBuilder sb)
        {
            Transform t = anim.GetBoneTransform(bone);
            if (t == null)
            {
                sb.AppendLine($"Missing Bone: {bone}");
                return false;
            }
            return true;
        }

        private static int CountMissingFingers(Animator anim, bool isLeft)
        {
            int missing = 0;
            HumanBodyBones[] required = isLeft ? new HumanBodyBones[] {
                HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal,
                HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal,
                HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal,
                HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal,
                HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal
            } : new HumanBodyBones[] {
                HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal,
                HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal,
                HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
                HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal,
                HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal
            };

            foreach (var b in required)
            {
                if (anim.GetBoneTransform(b) == null) missing++;
            }
            return missing;
        }

        private static void AnalyzeFingerAxes(Animator anim, StringBuilder sb)
        {
            Transform indexProx = anim.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
            Transform indexInter = anim.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate);
            if (indexProx != null && indexInter != null)
            {
                Vector3 localDir = indexProx.InverseTransformPoint(indexInter.position).normalized;
                sb.AppendLine($"<color=yellow>Left Index Bone points towards local axis: {localDir:F2}</color>");
                
                if (Mathf.Abs(localDir.y) > 0.8f) sb.AppendLine("-> Bone Length is along Y. Flexion axis should likely be X or Z.");
                else if (Mathf.Abs(localDir.x) > 0.8f) sb.AppendLine("-> Bone Length is along X. Flexion axis should likely be Y or Z.");
                else if (Mathf.Abs(localDir.z) > 0.8f) sb.AppendLine("-> Bone Length is along Z. Flexion axis should likely be X or Y.");
            }
        }
    }
}

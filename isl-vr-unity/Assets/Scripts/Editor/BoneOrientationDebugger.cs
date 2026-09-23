#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SignLoop.Rigging;

namespace SignLoop.Editor
{
    public static class BoneOrientationDebugger
    {
        [InitializeOnLoadMethod]
        public static void InspectBones()
        {
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/AvatarPrototype/model.fbx");
            if (modelPrefab == null)
            {
                Debug.LogWarning("[BoneOrientationDebugger] model.fbx not found.");
                return;
            }

            Debug.Log("================ BONE ORIENTATION DIAGNOSTIC ================");
            
            // Search transforms in prefab
            Transform[] all = modelPrefab.GetComponentsInChildren<Transform>(true);
            Transform lHand = null, rHand = null;
            Transform lThumb1 = null, lThumb2 = null, lThumb3 = null;
            Transform rThumb1 = null, rThumb2 = null, rThumb3 = null;
            Transform lIndex1 = null, rIndex1 = null;

            foreach (var t in all)
            {
                string n = t.name.ToLower();
                if (n.Contains("lefthandthumb1") || n == "leftthumbproximal") lThumb1 = t;
                if (n.Contains("lefthandthumb2") || n == "leftthumbintermediate") lThumb2 = t;
                if (n.Contains("lefthandthumb3") || n == "leftthumbdistal") lThumb3 = t;
                if (n.Contains("righthandthumb1") || n == "rightthumbproximal") rThumb1 = t;
                if (n.Contains("righthandthumb2") || n == "rightthumbintermediate") rThumb2 = t;
                if (n.Contains("righthandthumb3") || n == "rightthumbdistal") rThumb3 = t;
                if (n.Contains("lefthandindex1") || n == "leftindexproximal") lIndex1 = t;
                if (n.Contains("righthandindex1") || n == "rightindexproximal") rIndex1 = t;
                if (n == "lefthand" || n == "left_hand") lHand = t;
                if (n == "righthand" || n == "right_hand") rHand = t;
            }

            if (lHand != null) Debug.Log($"LHand: localPos={lHand.localPosition}, localRot={lHand.localEulerAngles}");
            if (rHand != null) Debug.Log($"RHand: localPos={rHand.localPosition}, localRot={rHand.localEulerAngles}");

            if (lIndex1 != null)
            {
                Transform child = lIndex1.childCount > 0 ? lIndex1.GetChild(0) : null;
                Vector3 boneDir = child != null ? child.localPosition.normalized : Vector3.zero;
                Debug.Log($"LIndex1: localEuler={lIndex1.localEulerAngles}, boneExtentDir={boneDir}");
            }

            if (rIndex1 != null)
            {
                Transform child = rIndex1.childCount > 0 ? rIndex1.GetChild(0) : null;
                Vector3 boneDir = child != null ? child.localPosition.normalized : Vector3.zero;
                Debug.Log($"RIndex1: localEuler={rIndex1.localEulerAngles}, boneExtentDir={boneDir}");
            }

            if (lThumb1 != null)
            {
                Transform child = lThumb1.childCount > 0 ? lThumb1.GetChild(0) : null;
                Vector3 boneDir = child != null ? child.localPosition.normalized : Vector3.zero;
                Debug.Log($"LThumb1: localEuler={lThumb1.localEulerAngles}, boneExtentDir={boneDir}");
            }

            if (rThumb1 != null)
            {
                Transform child = rThumb1.childCount > 0 ? rThumb1.GetChild(0) : null;
                Vector3 boneDir = child != null ? child.localPosition.normalized : Vector3.zero;
                Debug.Log($"RThumb1: localEuler={rThumb1.localEulerAngles}, boneExtentDir={boneDir}");
            }

            Debug.Log("=============================================================");
        }
    }
}
#endif

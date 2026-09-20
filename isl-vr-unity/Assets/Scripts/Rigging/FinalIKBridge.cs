using UnityEngine;
using System.Reflection;
using System;

namespace SignLoop.Rigging
{
    public class FinalIKBridge : MonoBehaviour
    {
        public static void SetupVRIK(AvatarRigWrapper rigWrapper)
        {
            if (rigWrapper == null || rigWrapper.ArmIK == null || rigWrapper.CurrentAvatar == null)
            {
                Debug.LogWarning("[FinalIKBridge] Missing Rig Wrapper dependencies.");
                return;
            }

            var avatar = rigWrapper.CurrentAvatar;
            var armIK = rigWrapper.ArmIK;

            // Use reflection to find VRIK Type across all loaded assemblies
            Type vrikType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                vrikType = assembly.GetType("RootMotion.FinalIK.VRIK");
                if (vrikType != null) break;
            }

            if (vrikType == null)
            {
                Debug.LogError("[FinalIKBridge] Could not find Final IK (VRIK) in the project! Please ensure it is imported.");
                return;
            }

            // Add or Get VRIK component
            Component vrikComp = avatar.GetComponent(vrikType);
            if (vrikComp == null)
            {
                vrikComp = avatar.AddComponent(vrikType);
            }

            // Get the solver field
            FieldInfo solverField = vrikType.GetField("solver");
            if (solverField != null)
            {
                object solver = solverField.GetValue(vrikComp);
                if (solver != null)
                {
                    Type solverType = solver.GetType();
                    
                    // Assign Left Arm Target
                    FieldInfo leftArmField = solverType.GetField("leftArm");
                    if (leftArmField != null)
                    {
                        object leftArm = leftArmField.GetValue(solver);
                        Type armType = leftArm.GetType();
                        FieldInfo targetField = armType.GetField("target");
                        if (targetField != null) targetField.SetValue(leftArm, armIK.LeftArmTarget);
                        
                        FieldInfo bendGoalField = armType.GetField("bendGoal");
                        if (bendGoalField != null) bendGoalField.SetValue(leftArm, armIK.LeftElbowHint);
                    }

                    // Assign Right Arm Target
                    FieldInfo rightArmField = solverType.GetField("rightArm");
                    if (rightArmField != null)
                    {
                        object rightArm = rightArmField.GetValue(solver);
                        Type armType = rightArm.GetType();
                        FieldInfo targetField = armType.GetField("target");
                        if (targetField != null) targetField.SetValue(rightArm, armIK.RightArmTarget);
                        
                        FieldInfo bendGoalField = armType.GetField("bendGoal");
                        if (bendGoalField != null) bendGoalField.SetValue(rightArm, armIK.RightElbowHint);
                    }

                    // Set weights
                    MethodInfo initiateMethod = vrikType.GetMethod("Initiate", BindingFlags.Public | BindingFlags.Instance);
                    if (initiateMethod != null) initiateMethod.Invoke(vrikComp, null); // Force initialization to bind bones
                }
            }

            // Turn off our procedural solver
            armIK.useProceduralSolver = false;
            
            // Log success
            Debug.Log("<color=green>[FinalIKBridge] Final IK successfully bound to targets! Procedural solver disabled.</color>");
        }
    }
}

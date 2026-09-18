using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SignLoop.Rigging
{
    /// <summary>
    /// Scaffolds and manages Unity Animation Rigging TwoBoneIKConstraints for avatar arms.
    /// Persistent IK targets remain stable while underlying avatar skeletal bones can be swapped.
    /// Zero allocations in Update().
    /// </summary>
    public class ArmIKController : MonoBehaviour
    {
        [Header("Animation Rigging Rig Components")]
        [Tooltip("The Rig component containing the constraints.")]
        [SerializeField] private Rig armRig;
        [SerializeField] private RigBuilder rigBuilder;

        [Header("Left Arm Constraints & Targets")]
        [SerializeField] private TwoBoneIKConstraint leftArmConstraint;
        [SerializeField] private Transform leftArmTarget;
        [SerializeField] private Transform leftElbowHint;

        [Header("Right Arm Constraints & Targets")]
        [SerializeField] private TwoBoneIKConstraint rightArmConstraint;
        [SerializeField] private Transform rightArmTarget;
        [SerializeField] private Transform rightElbowHint;

        public Transform LeftArmTarget => leftArmTarget;
        public Transform RightArmTarget => rightArmTarget;
        public Transform LeftElbowHint => leftElbowHint;
        public Transform RightElbowHint => rightElbowHint;

        /// <summary>
        /// Binds new arm skeletal bones to the existing TwoBoneIKConstraints and rebuilds the Rig.
        /// </summary>
        public void BindBones(
            Transform leftUpper, Transform leftMid, Transform leftTip,
            Transform rightUpper, Transform rightMid, Transform rightTip)
        {
            if (leftArmConstraint != null)
            {
                var data = leftArmConstraint.data;
                data.root = leftUpper;
                data.mid = leftMid;
                data.tip = leftTip;
                data.target = leftArmTarget;
                data.hint = leftElbowHint;
                data.targetPositionWeight = 1f;
                data.targetRotationWeight = 1f;
                data.hintWeight = 1f;
                leftArmConstraint.data = data;
            }

            if (rightArmConstraint != null)
            {
                var data = rightArmConstraint.data;
                data.root = rightUpper;
                data.mid = rightMid;
                data.tip = rightTip;
                data.target = rightArmTarget;
                data.hint = rightElbowHint;
                data.targetPositionWeight = 1f;
                data.targetRotationWeight = 1f;
                data.hintWeight = 1f;
                rightArmConstraint.data = data;
            }

            if (rigBuilder != null)
            {
                rigBuilder.Build();
            }

            Debug.Log("[ArmIKController] Successfully bound arm bones to TwoBoneIKConstraints and rebuilt Rig.");
        }

        /// <summary>
        /// Moves the left hand IK target. Zero allocations.
        /// </summary>
        public void SetLeftArmTarget(in Vector3 position, in Quaternion rotation, float weight = 1f)
        {
            if (leftArmTarget != null)
            {
                leftArmTarget.position = position;
                leftArmTarget.rotation = rotation;
            }

            if (leftArmConstraint != null)
            {
                leftArmConstraint.weight = weight;
            }
        }

        /// <summary>
        /// Moves the right hand IK target. Zero allocations.
        /// </summary>
        public void SetRightArmTarget(in Vector3 position, in Quaternion rotation, float weight = 1f)
        {
            if (rightArmTarget != null)
            {
                rightArmTarget.position = position;
                rightArmTarget.rotation = rotation;
            }

            if (rightArmConstraint != null)
            {
                rightArmConstraint.weight = weight;
            }
        }

        /// <summary>
        /// Sets the left elbow pole hint position. Zero allocations.
        /// </summary>
        public void SetLeftElbowHint(in Vector3 position, float weight = 1f)
        {
            if (leftElbowHint != null)
            {
                leftElbowHint.position = position;
            }

            if (leftArmConstraint != null)
            {
                var data = leftArmConstraint.data;
                data.hintWeight = weight;
                leftArmConstraint.data = data;
            }
        }

        /// <summary>
        /// Sets the right elbow pole hint position. Zero allocations.
        /// </summary>
        public void SetRightElbowHint(in Vector3 position, float weight = 1f)
        {
            if (rightElbowHint != null)
            {
                rightElbowHint.position = position;
            }

            if (rightArmConstraint != null)
            {
                var data = rightArmConstraint.data;
                data.hintWeight = weight;
                rightArmConstraint.data = data;
            }
        }

        /// <summary>
        /// Sets the overall weight of the arm rigging layer.
        /// </summary>
        public void SetRigWeight(float weight)
        {
            if (armRig != null)
            {
                armRig.weight = Mathf.Clamp01(weight);
            }
        }
    }
}

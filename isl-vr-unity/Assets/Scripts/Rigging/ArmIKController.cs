using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SignLoop.Rigging
{
    /// <summary>
    /// Scaffolds and manages Two-Bone IK for avatar arms.
    /// Features self-healing target resolution and an analytical Law-of-Cosines TwoBoneIK solver
    /// running in LateUpdate(). Guaranteed to work without requiring an active AnimatorController.
    /// Zero allocations in Update() and LateUpdate().
    /// </summary>
    [ExecuteAlways]
    public class ArmIKController : MonoBehaviour
    {
        [Header("Animation Rigging Rig Components")]
        [SerializeField] private Rig armRig;
        [SerializeField] private RigBuilder rigBuilder;

        [Header("Procedural Solver Fallback")]
        [Tooltip("Enables built-in analytical Two-Bone IK in LateUpdate. Works without an AnimatorController.")]
        [SerializeField] private bool useProceduralSolver = true;
        [Range(0f, 1f)]
        [SerializeField] private float solverWeight = 1f;
        [Tooltip("If true, overrides wrist bone rotation with targetRot. If false, wrist preserves natural forearm alignment without unnatural twisting.")]
        [SerializeField] private bool matchWristRotation = false;

        [Header("Left Arm Constraints & Targets")]
        [SerializeField] private TwoBoneIKConstraint leftArmConstraint;
        [SerializeField] private Transform leftArmTarget;
        [SerializeField] private Transform leftElbowHint;

        [Header("Right Arm Constraints & Targets")]
        [SerializeField] private TwoBoneIKConstraint rightArmConstraint;
        [SerializeField] private Transform rightArmTarget;
        [SerializeField] private Transform rightElbowHint;

        // Cached bone transforms for procedural solver
        private Transform _leftUpper, _leftMid, _leftTip;
        private Transform _rightUpper, _rightMid, _rightTip;
        private bool _isLeftArmBound;
        private bool _isRightArmBound;

        public Transform LeftArmTarget => leftArmTarget;
        public Transform RightArmTarget => rightArmTarget;
        public Transform LeftElbowHint => leftElbowHint;
        public Transform RightElbowHint => rightElbowHint;
        public bool IsLeftArmBound => _isLeftArmBound;
        public bool IsRightArmBound => _isRightArmBound;

        private void Awake()
        {
            EnsureTargets();
        }

        private void OnEnable()
        {
            EnsureTargets();
        }

        /// <summary>
        /// Self-heals IK target transforms if unassigned or missing in the hierarchy.
        /// </summary>
        public void EnsureTargets()
        {
            Transform parentRig = transform;

            if (leftArmTarget == null)
            {
                var t = parentRig.Find("LeftHand_IKTarget");
                if (t != null) leftArmTarget = t;
                else
                {
                    var go = new GameObject("LeftHand_IKTarget");
                    go.transform.SetParent(parentRig, false);
                    go.transform.localPosition = new Vector3(-0.25f, 1.15f, 0.35f);
                    leftArmTarget = go.transform;
                }
            }

            if (rightArmTarget == null)
            {
                var t = parentRig.Find("RightHand_IKTarget");
                if (t != null) rightArmTarget = t;
                else
                {
                    var go = new GameObject("RightHand_IKTarget");
                    go.transform.SetParent(parentRig, false);
                    go.transform.localPosition = new Vector3(0.25f, 1.15f, 0.35f);
                    rightArmTarget = go.transform;
                }
            }

            if (leftElbowHint == null)
            {
                var t = parentRig.Find("LeftElbow_Hint");
                if (t != null) leftElbowHint = t;
                else
                {
                    var go = new GameObject("LeftElbow_Hint");
                    go.transform.SetParent(parentRig, false);
                    go.transform.localPosition = new Vector3(-0.45f, 1.15f, -0.2f);
                    leftElbowHint = go.transform;
                }
            }

            if (rightElbowHint == null)
            {
                var t = parentRig.Find("RightElbow_Hint");
                if (t != null) rightElbowHint = t;
                else
                {
                    var go = new GameObject("RightElbow_Hint");
                    go.transform.SetParent(parentRig, false);
                    go.transform.localPosition = new Vector3(0.45f, 1.15f, -0.2f);
                    rightElbowHint = go.transform;
                }
            }
        }

        /// <summary>
        /// Binds new arm skeletal bones to the constraints and procedural solver.
        /// </summary>
        public void BindBones(
            Transform leftUpper, Transform leftMid, Transform leftTip,
            Transform rightUpper, Transform rightMid, Transform rightTip)
        {
            EnsureTargets();

            _leftUpper = leftUpper;
            _leftMid = leftMid;
            _leftTip = leftTip;
            _isLeftArmBound = (_leftUpper != null && _leftMid != null && _leftTip != null);

            _rightUpper = rightUpper;
            _rightMid = rightMid;
            _rightTip = rightTip;
            _isRightArmBound = (_rightUpper != null && _rightMid != null && _rightTip != null);

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

            Debug.Log($"<color=green>[ArmIKController] Arm bones bound (Left: {_isLeftArmBound}, Right: {_isRightArmBound}).</color>");
        }

        private void LateUpdate()
        {
            if (!useProceduralSolver || solverWeight <= 0f) return;

            EnsureTargets();

            // Solve Left Arm IK
            if (_isLeftArmBound && leftArmTarget != null)
            {
                Vector3 hintPos = (leftElbowHint != null) ? leftElbowHint.position : (_leftUpper.position + Vector3.back * 0.3f);
                SolveTwoBoneIK(_leftUpper, _leftMid, _leftTip, leftArmTarget.position, leftArmTarget.rotation, hintPos, solverWeight, matchWristRotation);
            }

            // Solve Right Arm IK
            if (_isRightArmBound && rightArmTarget != null)
            {
                Vector3 hintPos = (rightElbowHint != null) ? rightElbowHint.position : (_rightUpper.position + Vector3.back * 0.3f);
                SolveTwoBoneIK(_rightUpper, _rightMid, _rightTip, rightArmTarget.position, rightArmTarget.rotation, hintPos, solverWeight, matchWristRotation);
            }
        }

        /// <summary>
        /// Analytical Two-Bone IK solver based on Law of Cosines.
        /// Zero allocations. Operates directly on bone transforms in LateUpdate.
        /// </summary>
        public static void SolveTwoBoneIK(
            Transform root, Transform mid, Transform tip,
            Vector3 targetPos, Quaternion targetRot, Vector3 hintPos, float weight, bool matchWrist = false)
        {
            if (root == null || mid == null || tip == null) return;

            Vector3 a = root.position;
            Vector3 b = mid.position;
            Vector3 c = tip.position;
            Vector3 at = targetPos - a;
            Vector3 ab = b - a;

            float lenAB = ab.magnitude;
            float lenBC = (c - b).magnitude;
            float maxReach = (lenAB + lenBC) * 0.999f;
            float distAT = Mathf.Clamp(at.magnitude, 0.001f, maxReach);

            if (lenAB < 0.001f || lenBC < 0.001f) return;

            // Law of cosines for angle at root
            float cosA = Mathf.Clamp((lenAB * lenAB + distAT * distAT - lenBC * lenBC) / (2f * lenAB * distAT), -1f, 1f);
            float angleA = Mathf.Acos(cosA) * Mathf.Rad2Deg;

            // Bend plane normal derived from target and hint vector
            Vector3 hintDir = hintPos - a;
            Vector3 normal = Vector3.Cross(at, hintDir);
            if (normal.sqrMagnitude < 0.0001f) normal = Vector3.Cross(at, root.up);
            if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;
            normal.Normalize();

            // Select candidate angle that points elbow toward hint
            Vector3 dir1 = Quaternion.AngleAxis(angleA, normal) * at.normalized;
            Vector3 dir2 = Quaternion.AngleAxis(-angleA, normal) * at.normalized;
            Vector3 elbow1 = a + dir1 * lenAB;
            Vector3 elbow2 = a + dir2 * lenAB;
            Vector3 dirUpper = ((elbow1 - hintPos).sqrMagnitude < (elbow2 - hintPos).sqrMagnitude) ? dir1 : dir2;

            // 1. Rotate root (UpperArm)
            Quaternion deltaUpper = Quaternion.FromToRotation(ab, dirUpper);
            root.rotation = Quaternion.Slerp(root.rotation, deltaUpper * root.rotation, weight);

            // 2. Rotate mid (Forearm)
            Vector3 currentForearmDir = tip.position - mid.position;
            Vector3 targetForearmDir = targetPos - mid.position;
            if (currentForearmDir.sqrMagnitude > 0.0001f && targetForearmDir.sqrMagnitude > 0.0001f)
            {
                Quaternion deltaForearm = Quaternion.FromToRotation(currentForearmDir, targetForearmDir);
                mid.rotation = Quaternion.Slerp(mid.rotation, deltaForearm * mid.rotation, weight);
            }

            // 3. Match tip rotation only if explicitly requested; otherwise wrist naturally follows forearm
            if (matchWrist)
            {
                tip.rotation = Quaternion.Slerp(tip.rotation, targetRot, weight);
            }
        }

        public void SetLeftArmTarget(in Vector3 position, in Quaternion rotation, float weight = 1f)
        {
            EnsureTargets();
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

        public void SetRightArmTarget(in Vector3 position, in Quaternion rotation, float weight = 1f)
        {
            EnsureTargets();
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

        public void SetLeftElbowHint(in Vector3 position, float weight = 1f)
        {
            EnsureTargets();
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

        public void SetRightElbowHint(in Vector3 position, float weight = 1f)
        {
            EnsureTargets();
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

        public bool MatchWristRotation
        {
            get => matchWristRotation;
            set => matchWristRotation = value;
        }

        public void SetMatchWristRotation(bool enable)
        {
            matchWristRotation = enable;
        }

        public void SetRigWeight(float weight)
        {
            solverWeight = Mathf.Clamp01(weight);
            if (armRig != null)
            {
                armRig.weight = solverWeight;
            }
        }
    }
}

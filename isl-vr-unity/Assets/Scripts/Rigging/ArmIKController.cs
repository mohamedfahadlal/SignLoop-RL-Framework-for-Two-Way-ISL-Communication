using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SignLoop.Rigging
{
    /// <summary>
    /// Scaffolds and manages Two-Bone IK for avatar arms.
    /// Features self-healing target resolution and an analytical Law-of-Cosines TwoBoneIK solver
    /// running in LateUpdate(). Guaranteed to work without requiring an active AnimatorController.
    /// Strictly eliminates frame-to-frame roll accumulation (Berry phase) and enforces anatomical limits.
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
        [Tooltip("If true, overrides wrist bone rotation with targetRot clamped to human limits. If false, wrist preserves natural forearm alignment.")]
        [SerializeField] private bool matchWristRotation = false;

        [Header("Wrist Axial Roll Offset (Pronation / Supination)")]
        [Range(-180f, 180f)]
        [SerializeField] private float leftWristRollOffset = 0f;
        [Range(-180f, 180f)]
        [SerializeField] private float rightWristRollOffset = 0f;

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

        // Cached pristine bind pose local rotations and bone lengths (prevents drift)
        private Quaternion _leftUpperBindLocalRot = Quaternion.identity;
        private Quaternion _leftMidBindLocalRot = Quaternion.identity;
        private Quaternion _leftTipBindLocalRot = Quaternion.identity;
        private float _leftUpperLen = 0.28f;
        private float _leftForearmLen = 0.25f;

        private Quaternion _rightUpperBindLocalRot = Quaternion.identity;
        private Quaternion _rightMidBindLocalRot = Quaternion.identity;
        private Quaternion _rightTipBindLocalRot = Quaternion.identity;
        private float _rightUpperLen = 0.28f;
        private float _rightForearmLen = 0.25f;

        private bool _bindPoseCaptured = false;

        public Transform LeftArmTarget => leftArmTarget;
        public Transform RightArmTarget => rightArmTarget;
        public Transform LeftElbowHint => leftElbowHint;
        public Transform RightElbowHint => rightElbowHint;
        public bool IsLeftArmBound => _isLeftArmBound;
        public bool IsRightArmBound => _isRightArmBound;

        public float LeftWristRollOffset { get => leftWristRollOffset; set => leftWristRollOffset = value; }
        public float RightWristRollOffset { get => rightWristRollOffset; set => rightWristRollOffset = value; }

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

            CaptureBindPose();

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

        /// <summary>
        /// Captures pristine bind local rotations and bone lengths to prevent frame-to-frame drift.
        /// </summary>
        public void CaptureBindPose()
        {
            if (_isLeftArmBound)
            {
                _leftUpperBindLocalRot = _leftUpper.localRotation;
                _leftMidBindLocalRot = _leftMid.localRotation;
                _leftTipBindLocalRot = _leftTip.localRotation;
                _leftUpperLen = Mathf.Max(0.05f, Vector3.Distance(_leftUpper.position, _leftMid.position));
                _leftForearmLen = Mathf.Max(0.05f, Vector3.Distance(_leftMid.position, _leftTip.position));
            }

            if (_isRightArmBound)
            {
                _rightUpperBindLocalRot = _rightUpper.localRotation;
                _rightMidBindLocalRot = _rightMid.localRotation;
                _rightTipBindLocalRot = _rightTip.localRotation;
                _rightUpperLen = Mathf.Max(0.05f, Vector3.Distance(_rightUpper.position, _rightMid.position));
                _rightForearmLen = Mathf.Max(0.05f, Vector3.Distance(_rightMid.position, _rightTip.position));
            }

            _bindPoseCaptured = true;
        }

        private void LateUpdate()
        {
            if (!useProceduralSolver || solverWeight <= 0f) return;

            EnsureTargets();
            if (!_bindPoseCaptured) CaptureBindPose();

            // Solve Left Arm IK
            if (_isLeftArmBound && leftArmTarget != null)
            {
                Vector3 hintPos = (leftElbowHint != null) ? leftElbowHint.position : (_leftUpper.position - transform.forward * 0.3f - transform.right * 0.2f);
                SolveArmIK(
                    _leftUpper, _leftMid, _leftTip,
                    _leftUpperBindLocalRot, _leftMidBindLocalRot, _leftTipBindLocalRot,
                    _leftUpperLen, _leftForearmLen,
                    leftArmTarget.position, leftArmTarget.rotation, hintPos,
                    solverWeight, matchWristRotation, leftWristRollOffset, isLeft: true
                );
            }

            // Solve Right Arm IK
            if (_isRightArmBound && rightArmTarget != null)
            {
                Vector3 hintPos = (rightElbowHint != null) ? rightElbowHint.position : (_rightUpper.position - transform.forward * 0.3f + transform.right * 0.2f);
                SolveArmIK(
                    _rightUpper, _rightMid, _rightTip,
                    _rightUpperBindLocalRot, _rightMidBindLocalRot, _rightTipBindLocalRot,
                    _rightUpperLen, _rightForearmLen,
                    rightArmTarget.position, rightArmTarget.rotation, hintPos,
                    solverWeight, matchWristRotation, rightWristRollOffset, isLeft: false
                );
            }
        }

        /// <summary>
        /// Mathematically stable, drift-free Two-Bone IK solver with human joint limits.
        /// Zero allocations in Update() and LateUpdate().
        /// Resets bones to bind pose each frame to eliminate frame-to-frame roll accumulation (Berry phase)
        /// and clamps wrist rotation to prevent anatomical inversions.
        /// </summary>
        public void SolveArmIK(
            Transform root, Transform mid, Transform tip,
            Quaternion bindRootRot, Quaternion bindMidRot, Quaternion bindTipRot,
            float lenAB, float lenBC,
            Vector3 targetPos, Quaternion targetRot, Vector3 hintPos,
            float weight, bool matchWrist, float rollOffsetDeg, bool isLeft)
        {
            if (root == null || mid == null || tip == null) return;

            // 1. Reset bones to pristine bind local rotations to eliminate any frame-to-frame drift or stuck rotations
            root.localRotation = bindRootRot;
            mid.localRotation = bindMidRot;
            tip.localRotation = bindTipRot;

            Vector3 a = root.position;
            Vector3 at = targetPos - a;
            float distAT = at.magnitude;

            // Anatomical reach limits: min reach prevents elbow self-collapse; max reach prevents hyper-extension singularity
            float minReach = Mathf.Max(Mathf.Abs(lenAB - lenBC) + 0.05f, 0.12f);
            float maxReach = (lenAB + lenBC) * 0.998f;
            float clampedDist = Mathf.Clamp(distAT, minReach, maxReach);

            Vector3 u = (distAT > 1e-4f) ? (at / distAT) : (isLeft ? -transform.right : transform.right);
            Vector3 clampedTarget = a + u * clampedDist;

            // 2. Law of Cosines for Shoulder (Angle A)
            float cosA = Mathf.Clamp((lenAB * lenAB + clampedDist * clampedDist - lenBC * lenBC) / (2f * lenAB * clampedDist), -1f, 1f);
            float angleA = Mathf.Acos(cosA); // radians

            // 3. Biomechanically stable bend direction (pole vector)
            // Human elbows naturally bend backward and outward
            Vector3 avatarFwd = transform.forward;
            Vector3 avatarSide = isLeft ? -transform.right : transform.right;
            Vector3 naturalElbowDir = (-avatarFwd * 0.7f + avatarSide * 0.7f).normalized;

            // Project hint vector onto plane perpendicular to u
            Vector3 hintDir = hintPos - a;
            Vector3 bendDir = hintDir - Vector3.Dot(hintDir, u) * u;

            Vector3 naturalBendProj = naturalElbowDir - Vector3.Dot(naturalElbowDir, u) * u;
            if (naturalBendProj.sqrMagnitude > 1e-4f)
            {
                naturalBendProj.Normalize();
            }
            else
            {
                naturalBendProj = -transform.up;
            }

            if (bendDir.sqrMagnitude < 1e-4f || Vector3.Dot(bendDir.normalized, naturalBendProj) < -0.1f)
            {
                bendDir = naturalBendProj;
            }
            else
            {
                bendDir.Normalize();
                // Bias slightly toward natural human outward bend to prevent sudden snaps
                bendDir = Vector3.Slerp(bendDir, naturalBendProj, 0.2f).normalized;
            }

            // 4. Exact 3D elbow position B on the arm triangle
            Vector3 elbowPos = a + lenAB * (Mathf.Cos(angleA) * u + Mathf.Sin(angleA) * bendDir);

            // 5. Rotate Upper Arm from pristine bind pose
            Vector3 initialAB = mid.position - root.position;
            Vector3 targetAB = elbowPos - a;
            if (initialAB.sqrMagnitude > 1e-6f && targetAB.sqrMagnitude > 1e-6f)
            {
                Quaternion deltaUpper = Quaternion.FromToRotation(initialAB, targetAB);
                root.rotation = Quaternion.Slerp(root.rotation, deltaUpper * root.rotation, weight);
            }

            // 6. Rotate Forearm from its new position to clamped wrist target
            Vector3 initialBC = tip.position - mid.position;
            Vector3 targetBC = clampedTarget - mid.position;
            if (initialBC.sqrMagnitude > 1e-6f && targetBC.sqrMagnitude > 1e-6f)
            {
                Quaternion deltaForearm = Quaternion.FromToRotation(initialBC, targetBC);
                mid.rotation = Quaternion.Slerp(mid.rotation, deltaForearm * mid.rotation, weight);
            }

            // 7. Hand/Wrist Orientation & Anatomical Protection
            Quaternion naturalWristRot = mid.rotation * bindTipRot;

            if (Mathf.Abs(rollOffsetDeg) > 0.01f)
            {
                // Axial roll around forearm axis (pronation / supination)
                Vector3 forearmAxis = (clampedTarget - mid.position).normalized;
                Quaternion rollRot = Quaternion.AngleAxis(rollOffsetDeg, forearmAxis);
                naturalWristRot = rollRot * naturalWristRot;
            }

            if (matchWrist)
            {
                // Clamp target wrist rotation relative to forearm to human physiological range (max 70 deg)
                // This strictly prevents inverted / backwards hand poses from noisy video landmarks
                Quaternion clampedWristRot = Quaternion.RotateTowards(naturalWristRot, targetRot, 70f);
                tip.rotation = Quaternion.Slerp(naturalWristRot, clampedWristRot, weight);
            }
            else
            {
                tip.rotation = Quaternion.Slerp(tip.rotation, naturalWristRot, weight);
            }
        }

        /// <summary>
        /// Backwards-compatible static solver entry point.
        /// </summary>
        public static void SolveTwoBoneIK(
            Transform root, Transform mid, Transform tip,
            Vector3 targetPos, Quaternion targetRot, Vector3 hintPos, float weight, bool matchWrist = false)
        {
            if (root == null || mid == null || tip == null) return;
            Vector3 a = root.position;
            Vector3 b = mid.position;
            Vector3 c = tip.position;
            float lenAB = Vector3.Distance(a, b);
            float lenBC = Vector3.Distance(b, c);
            Quaternion bindTip = tip.localRotation;

            Vector3 at = targetPos - a;
            float d = Mathf.Clamp(at.magnitude, 0.12f, (lenAB + lenBC) * 0.998f);
            Vector3 u = at.normalized;
            float cosA = Mathf.Clamp((lenAB * lenAB + d * d - lenBC * lenBC) / (2f * lenAB * d), -1f, 1f);
            float angleA = Mathf.Acos(cosA);

            Vector3 hintDir = hintPos - a;
            Vector3 bendDir = hintDir - Vector3.Dot(hintDir, u) * u;
            if (bendDir.sqrMagnitude < 1e-4f) bendDir = Vector3.Cross(u, Vector3.up);
            if (bendDir.sqrMagnitude < 1e-4f) bendDir = Vector3.right;
            bendDir.Normalize();

            Vector3 elbowPos = a + lenAB * (Mathf.Cos(angleA) * u + Mathf.Sin(angleA) * bendDir);

            Quaternion deltaUpper = Quaternion.FromToRotation(b - a, elbowPos - a);
            root.rotation = deltaUpper * root.rotation;

            Quaternion deltaMid = Quaternion.FromToRotation(c - mid.position, (a + u * d) - mid.position);
            mid.rotation = deltaMid * mid.rotation;

            if (matchWrist)
            {
                tip.rotation = Quaternion.RotateTowards(mid.rotation * bindTip, targetRot, 70f);
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

        public void SetWristRollOffset(bool isLeft, float rollDegrees)
        {
            if (isLeft) leftWristRollOffset = rollDegrees;
            else rightWristRollOffset = rollDegrees;
        }

        public void ResetWristRollOffsets()
        {
            leftWristRollOffset = 0f;
            rightWristRollOffset = 0f;
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

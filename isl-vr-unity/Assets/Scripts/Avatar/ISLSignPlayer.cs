using System;
using System.Collections.Generic;
using UnityEngine;
using SignLoop.Rigging;

namespace SignLoop.Avatar
{
    [Serializable]
    public struct Vector3Data
    {
        public float x;
        public float y;
        public float z;
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public struct QuaternionData
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public Quaternion ToQuaternion()
        {
            if (Mathf.Abs(x) < 1e-5f && Mathf.Abs(y) < 1e-5f && Mathf.Abs(z) < 1e-5f && Mathf.Abs(w) < 1e-5f)
                return Quaternion.identity;
            return new Quaternion(x, y, z, w);
        }
    }

    [Serializable]
    public class ISLSignFrameData
    {
        public float time;
        public Vector3Data leftWristPos;
        public Vector3Data leftElbowHint;
        public QuaternionData leftWristRot;
        public float leftThumbCurl;
        public float leftIndexCurl;
        public float leftMiddleCurl;
        public float leftRingCurl;
        public float leftPinkyCurl;
        public bool leftHandActive;

        public Vector3Data rightWristPos;
        public Vector3Data rightElbowHint;
        public QuaternionData rightWristRot;
        public float rightThumbCurl;
        public float rightIndexCurl;
        public float rightMiddleCurl;
        public float rightRingCurl;
        public float rightPinkyCurl;
        public bool rightHandActive;
    }

    [Serializable]
    public class ISLSignClipData
    {
        public string signName;
        public float duration;
        public int frameCount;
        public ISLSignFrameData[] frames;
    }

    /// <summary>
    /// ISL Sign Motion Clip Player for SignLoop.
    /// Streams 30-frame authentic skeletal landmark trajectories from the INCLUDE / Zenodo dataset
    /// to drive avatar Arm IK, Elbow Hints, Wrist Orientations, and 21-joint Finger Curls.
    /// Strictly guarantees 0 heap allocations in LateUpdate() for Quest 3 VR performance.
    /// </summary>
    public class ISLSignPlayer : MonoBehaviour
    {
        [Header("Rig Controllers")]
        [SerializeField] private ArmIKController armIK;
        [SerializeField] private HandPoseController handPose;
        [SerializeField] private AvatarRigWrapper rigWrapper;

        [Header("Preloaded ISL Clips (.json TextAssets)")]
        [SerializeField] private TextAsset[] signClipAssets;

        [Header("Playback Timing & Hold")]
        [Tooltip("Speed multiplier for clip playback (1.0 = normal pacing).")]
        [SerializeField] private float playbackSpeed = 1.0f;
        [Tooltip("Duration to hold the final completed sign posture so it can be clearly inspected.")]
        [SerializeField] private float holdDuration = 0.8f;
        [Tooltip("Duration to smoothly blend arms and fingers back to neutral rest stance.")]
        [SerializeField] private float leadOutDuration = 0.5f;
        [SerializeField] private bool loop = false;

        [Header("Wrist Alignment")]
        [Tooltip("If true, overrides wrist orientation with clip video rotation. If false, wrists align naturally with forearm IK for 100% stable, human-safe motion.")]
        [SerializeField] private bool matchWristRotation = false;

        // Clip library
        private readonly Dictionary<string, ISLSignClipData> _clipLibrary = new Dictionary<string, ISLSignClipData>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _availableSignNames = new List<string>();

        // Playback state
        private ISLSignClipData _currentClip;
        private float _playheadTime = 0f;
        private bool _isPlaying = false;
        private bool _isPaused = false;

        // Blending from previous state
        private Vector3 _blendStartLWrist, _blendStartRWrist;
        private Vector3 _blendStartLElbow, _blendStartRElbow;
        private float _blendTimer = 0f;
        private const float BlendDuration = 0.3f; // smooth 300ms blend into sign

        // Rest stance targets
        private static readonly Vector3 RestLWrist = new Vector3(-0.25f, 0.90f, 0.20f);
        private static readonly Vector3 RestRWrist = new Vector3(0.25f, 0.90f, 0.20f);
        private static readonly Vector3 RestLElbow = new Vector3(-0.38f, 1.05f, -0.10f);
        private static readonly Vector3 RestRElbow = new Vector3(0.38f, 1.05f, -0.10f);

        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;
        public bool Loop { get => loop; set => loop = value; }
        public bool MatchWristRotation { get => matchWristRotation; set => matchWristRotation = value; }
        public float PlaybackSpeed { get => playbackSpeed; set => playbackSpeed = Mathf.Clamp(value, 0.2f, 3.0f); }
        public string CurrentSignName => _currentClip != null ? _currentClip.signName : "None";
        public float TotalDuration => _currentClip != null ? (_currentClip.duration + holdDuration + leadOutDuration) : 0f;
        public float NormalizedProgress => TotalDuration > 0f ? Mathf.Clamp01(_playheadTime / TotalDuration) : 0f;
        public string PlaybackPhaseText
        {
            get
            {
                if (!_isPlaying || _currentClip == null) return "Idle";
                if (_playheadTime < _currentClip.duration) return $"Playing ({(NormalizedProgress * 100f):F0}%)";
                if (_playheadTime < _currentClip.duration + holdDuration) return "Holding Sign Posture";
                return "Returning to Rest";
            }
        }
        public IReadOnlyList<string> AvailableSignNames => _availableSignNames;

        private void Awake()
        {
            FindDependencies();
            LoadClipLibrary();
        }

        private void OnEnable()
        {
            FindDependencies();
        }

        public void FindDependencies()
        {
            if (rigWrapper == null) rigWrapper = GetComponentInParent<AvatarRigWrapper>();
            if (rigWrapper == null) rigWrapper = FindFirstObjectByType<AvatarRigWrapper>();

            if (rigWrapper != null)
            {
                if (armIK == null) armIK = rigWrapper.ArmIK;
                if (handPose == null) handPose = rigWrapper.HandPose;
            }

            if (armIK == null) armIK = GetComponentInChildren<ArmIKController>();
            if (handPose == null) handPose = GetComponentInChildren<HandPoseController>();
        }

        public void LoadClipLibrary()
        {
            _clipLibrary.Clear();
            _availableSignNames.Clear();

            // 1. Load explicitly assigned TextAssets
            if (signClipAssets != null)
            {
                for (int i = 0; i < signClipAssets.Length; i++)
                {
                    if (signClipAssets[i] != null)
                    {
                        RegisterClipFromJson(signClipAssets[i].text, signClipAssets[i].name);
                    }
                }
            }

            // 2. Load from Resources if available
            var resourcesClips = Resources.LoadAll<TextAsset>("ISLClips");
            for (int i = 0; i < resourcesClips.Length; i++)
            {
                if (resourcesClips[i] != null && !_clipLibrary.ContainsKey(resourcesClips[i].name))
                {
                    RegisterClipFromJson(resourcesClips[i].text, resourcesClips[i].name);
                }
            }

            // 3. Live disk read fallback for editor & desktop testing
            try
            {
                string clipsDir = System.IO.Path.Combine(Application.dataPath, "Resources", "ISLClips");
                if (System.IO.Directory.Exists(clipsDir))
                {
                    string[] jsonFiles = System.IO.Directory.GetFiles(clipsDir, "*.json");
                    for (int i = 0; i < jsonFiles.Length; i++)
                    {
                        string fName = System.IO.Path.GetFileNameWithoutExtension(jsonFiles[i]);
                        string content = System.IO.File.ReadAllText(jsonFiles[i]);
                        RegisterClipFromJson(content, fName);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ISLSignPlayer] Direct disk clip load info: {ex.Message}");
            }

            Debug.Log($"<color=green>[ISLSignPlayer] Loaded {_clipLibrary.Count} authentic ISL sign clips.</color>");
        }

        public void RegisterClipFromJson(string jsonContent, string fallbackName = "Unnamed")
        {
            try
            {
                ISLSignClipData clip = JsonUtility.FromJson<ISLSignClipData>(jsonContent);
                if (clip != null && clip.frames != null && clip.frames.Length > 0)
                {
                    string name = string.IsNullOrEmpty(clip.signName) ? fallbackName : clip.signName;
                    clip.signName = name;
                    _clipLibrary[name] = clip;
                    if (!_availableSignNames.Contains(name))
                    {
                        _availableSignNames.Add(name);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ISLSignPlayer] Failed to parse clip JSON '{fallbackName}': {ex.Message}");
            }
        }

        public bool PlaySign(string signName)
        {
            if (string.IsNullOrEmpty(signName)) return false;

            if (_clipLibrary.TryGetValue(signName, out var clip))
            {
                PlayClip(clip);
                return true;
            }

            Debug.LogWarning($"[ISLSignPlayer] Sign '{signName}' not found in clip library.");
            return false;
        }

        public void PlayClip(ISLSignClipData clip)
        {
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

            FindDependencies();

            _currentClip = clip;
            _playheadTime = 0f;
            _isPlaying = true;
            _isPaused = false;
            _blendTimer = 0f;

            // Capture initial positions for smooth 300ms blending
            if (armIK != null && armIK.LeftArmTarget != null)
            {
                _blendStartLWrist = armIK.LeftArmTarget.position;
                _blendStartRWrist = armIK.RightArmTarget.position;
                _blendStartLElbow = armIK.LeftElbowHint != null ? armIK.LeftElbowHint.position : RestLElbow;
                _blendStartRElbow = armIK.RightElbowHint != null ? armIK.RightElbowHint.position : RestRElbow;
            }
            else
            {
                _blendStartLWrist = RestLWrist;
                _blendStartRWrist = RestRWrist;
                _blendStartLElbow = RestLElbow;
                _blendStartRElbow = RestRElbow;
            }

            // Enable wrist orientation matching for real human sign playback
            if (armIK != null)
            {
                armIK.ResetWristRollOffsets();
                armIK.SetMatchWristRotation(matchWristRotation);
            }

            Debug.Log($"<color=cyan>[ISLSignPlayer] Playing authentic ISL sign: '{clip.signName}' ({clip.frameCount} frames, {clip.duration:F1}s, hold={holdDuration:F1}s).</color>");
        }

        public void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
            _playheadTime = 0f;
            _currentClip = null;

            if (armIK != null)
            {
                armIK.SetLeftArmTarget(RestLWrist, Quaternion.identity);
                armIK.SetRightArmTarget(RestRWrist, Quaternion.identity);
                armIK.ResetWristRollOffsets();
                armIK.SetMatchWristRotation(false);
            }

            if (handPose != null)
            {
                handPose.SetCanonicalShape(HandSide.Left, CanonicalHandShape.Neutral);
                handPose.SetCanonicalShape(HandSide.Right, CanonicalHandShape.Neutral);
            }
        }

        public void Pause() => _isPaused = true;
        public void Resume() => _isPaused = false;

        private void LateUpdate()
        {
            if (!_isPlaying || _isPaused || _currentClip == null || _currentClip.frames.Length == 0) return;

            FindDependencies();
            if (armIK == null || handPose == null) return;

            // Advance playhead
            _playheadTime += Time.deltaTime * playbackSpeed;

            float activeDuration = _currentClip.duration;
            float totalDuration = activeDuration + holdDuration + leadOutDuration;

            if (_playheadTime >= totalDuration)
            {
                if (loop)
                {
                    _playheadTime = 0f;
                    _blendTimer = 0f;
                    _blendStartLWrist = armIK.LeftArmTarget.position;
                    _blendStartRWrist = armIK.RightArmTarget.position;
                    _blendStartLElbow = armIK.LeftElbowHint != null ? armIK.LeftElbowHint.position : RestLElbow;
                    _blendStartRElbow = armIK.RightElbowHint != null ? armIK.RightElbowHint.position : RestRElbow;
                }
                else
                {
                    Stop();
                    return;
                }
            }

            Vector3 rawLWrist, rawRWrist, rawLElbow, rawRElbow;
            Quaternion rawLRot, rawRRot;
            float lThumb, lIndex, lMiddle, lRing, lPinky;
            float rThumb, rIndex, rMiddle, rRing, rPinky;

            ISLSignFrameData lastFrame = _currentClip.frames[_currentClip.frameCount - 1];

            if (_playheadTime < activeDuration)
            {
                // Phase 1: Active Trajectory Interpolation
                float normalizedT = Mathf.Clamp01(_playheadTime / activeDuration);
                float framePos = normalizedT * (_currentClip.frameCount - 1);
                int frameAIdx = Mathf.Clamp(Mathf.FloorToInt(framePos), 0, _currentClip.frameCount - 1);
                int frameBIdx = Mathf.Clamp(frameAIdx + 1, 0, _currentClip.frameCount - 1);
                float frameFrac = framePos - frameAIdx;

                ISLSignFrameData frameA = _currentClip.frames[frameAIdx];
                ISLSignFrameData frameB = _currentClip.frames[frameBIdx];

                rawLWrist = Vector3.Lerp(frameA.leftWristPos.ToVector3(), frameB.leftWristPos.ToVector3(), frameFrac);
                rawRWrist = Vector3.Lerp(frameA.rightWristPos.ToVector3(), frameB.rightWristPos.ToVector3(), frameFrac);
                rawLElbow = Vector3.Lerp(frameA.leftElbowHint.ToVector3(), frameB.leftElbowHint.ToVector3(), frameFrac);
                rawRElbow = Vector3.Lerp(frameA.rightElbowHint.ToVector3(), frameB.rightElbowHint.ToVector3(), frameFrac);

                rawLRot = Quaternion.Slerp(frameA.leftWristRot.ToQuaternion(), frameB.leftWristRot.ToQuaternion(), frameFrac);
                rawRRot = Quaternion.Slerp(frameA.rightWristRot.ToQuaternion(), frameB.rightWristRot.ToQuaternion(), frameFrac);

                // Smooth lead-in blend from starting avatar pose
                if (_blendTimer < BlendDuration)
                {
                    _blendTimer += Time.deltaTime;
                    float blendFactor = Mathf.SmoothStep(0f, 1f, _blendTimer / BlendDuration);
                    rawLWrist = Vector3.Lerp(_blendStartLWrist, rawLWrist, blendFactor);
                    rawRWrist = Vector3.Lerp(_blendStartRWrist, rawRWrist, blendFactor);
                    rawLElbow = Vector3.Lerp(_blendStartLElbow, rawLElbow, blendFactor);
                    rawRElbow = Vector3.Lerp(_blendStartRElbow, rawRElbow, blendFactor);
                }

                lThumb = Mathf.Lerp(frameA.leftThumbCurl, frameB.leftThumbCurl, frameFrac);
                lIndex = Mathf.Lerp(frameA.leftIndexCurl, frameB.leftIndexCurl, frameFrac);
                lMiddle = Mathf.Lerp(frameA.leftMiddleCurl, frameB.leftMiddleCurl, frameFrac);
                lRing = Mathf.Lerp(frameA.leftRingCurl, frameB.leftRingCurl, frameFrac);
                lPinky = Mathf.Lerp(frameA.leftPinkyCurl, frameB.leftPinkyCurl, frameFrac);

                rThumb = Mathf.Lerp(frameA.rightThumbCurl, frameB.rightThumbCurl, frameFrac);
                rIndex = Mathf.Lerp(frameA.rightIndexCurl, frameB.rightIndexCurl, frameFrac);
                rMiddle = Mathf.Lerp(frameA.rightMiddleCurl, frameB.rightMiddleCurl, frameFrac);
                rRing = Mathf.Lerp(frameA.rightRingCurl, frameB.rightRingCurl, frameFrac);
                rPinky = Mathf.Lerp(frameA.rightPinkyCurl, frameB.rightPinkyCurl, frameFrac);
            }
            else if (_playheadTime < activeDuration + holdDuration)
            {
                // Phase 2: Steady Hold of Completed Sign Posture (Human-readable observation)
                rawLWrist = lastFrame.leftWristPos.ToVector3();
                rawRWrist = lastFrame.rightWristPos.ToVector3();
                rawLElbow = lastFrame.leftElbowHint.ToVector3();
                rawRElbow = lastFrame.rightElbowHint.ToVector3();

                rawLRot = lastFrame.leftWristRot.ToQuaternion();
                rawRRot = lastFrame.rightWristRot.ToQuaternion();

                lThumb = lastFrame.leftThumbCurl;
                lIndex = lastFrame.leftIndexCurl;
                lMiddle = lastFrame.leftMiddleCurl;
                lRing = lastFrame.leftRingCurl;
                lPinky = lastFrame.leftPinkyCurl;

                rThumb = lastFrame.rightThumbCurl;
                rIndex = lastFrame.rightIndexCurl;
                rMiddle = lastFrame.rightMiddleCurl;
                rRing = lastFrame.rightRingCurl;
                rPinky = lastFrame.rightPinkyCurl;
            }
            else
            {
                // Phase 3: Smooth Lead-Out / Return Lerp to Rest Stance
                float leadOutT = Mathf.Clamp01((_playheadTime - (activeDuration + holdDuration)) / Mathf.Max(0.01f, leadOutDuration));
                float easeOut = Mathf.SmoothStep(0f, 1f, leadOutT);

                rawLWrist = Vector3.Lerp(lastFrame.leftWristPos.ToVector3(), RestLWrist, easeOut);
                rawRWrist = Vector3.Lerp(lastFrame.rightWristPos.ToVector3(), RestRWrist, easeOut);
                rawLElbow = Vector3.Lerp(lastFrame.leftElbowHint.ToVector3(), RestLElbow, easeOut);
                rawRElbow = Vector3.Lerp(lastFrame.rightElbowHint.ToVector3(), RestRElbow, easeOut);

                rawLRot = Quaternion.Slerp(lastFrame.leftWristRot.ToQuaternion(), Quaternion.identity, easeOut);
                rawRRot = Quaternion.Slerp(lastFrame.rightWristRot.ToQuaternion(), Quaternion.identity, easeOut);

                lThumb = Mathf.Lerp(lastFrame.leftThumbCurl, 0f, easeOut);
                lIndex = Mathf.Lerp(lastFrame.leftIndexCurl, 0f, easeOut);
                lMiddle = Mathf.Lerp(lastFrame.leftMiddleCurl, 0f, easeOut);
                lRing = Mathf.Lerp(lastFrame.leftRingCurl, 0f, easeOut);
                lPinky = Mathf.Lerp(lastFrame.leftPinkyCurl, 0f, easeOut);

                rThumb = Mathf.Lerp(lastFrame.rightThumbCurl, 0f, easeOut);
                rIndex = Mathf.Lerp(lastFrame.rightIndexCurl, 0f, easeOut);
                rMiddle = Mathf.Lerp(lastFrame.rightMiddleCurl, 0f, easeOut);
                rRing = Mathf.Lerp(lastFrame.rightRingCurl, 0f, easeOut);
                rPinky = Mathf.Lerp(lastFrame.rightPinkyCurl, 0f, easeOut);
            }

            // Apply to ArmIK
            armIK.SetLeftArmTarget(rawLWrist, rawLRot);
            armIK.SetRightArmTarget(rawRWrist, rawRRot);
            armIK.SetLeftElbowHint(rawLElbow);
            armIK.SetRightElbowHint(rawRElbow);

            // Apply to HandPoseController
            handPose.SetFingerCurls(HandSide.Left, lThumb, lIndex, lMiddle, lRing, lPinky);
            handPose.SetFingerCurls(HandSide.Right, rThumb, rIndex, rMiddle, rRing, rPinky);
        }
    }
}

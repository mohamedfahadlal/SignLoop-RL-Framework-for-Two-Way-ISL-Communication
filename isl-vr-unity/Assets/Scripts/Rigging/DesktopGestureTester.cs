using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SignLoop.Rigging
{
    /// <summary>
    /// Interactive desktop testing harness for ISL procedural animation.
    /// Provides on-screen GUI buttons, keyboard hotkeys (1-8, Space),
    /// and mouse orbit/zoom controls to inspect avatar gestures in Play Mode.
    /// Supports both New Input System and legacy input backends.
    /// </summary>
    [ExecuteAlways]
    public class DesktopGestureTester : MonoBehaviour
    {
        [Header("Target Rig Controllers")]
        [SerializeField] private AvatarRigWrapper avatarWrapper;
        [SerializeField] private HandPoseController handPose;
        [SerializeField] private ArmIKController armIK;
        [SerializeField] private SignLoop.Avatar.ISLSignPlayer signPlayer;

        [Header("Camera Orbit & Zoom (Hold Right-Click to Orbit, Scroll to Zoom)")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector3 orbitTargetOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private float orbitSpeed = 3f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minDistance = 0.5f;
        [SerializeField] private float maxDistance = 3.5f;

        [Header("Arm Procedural Wave Test")]
        [SerializeField] private bool waveArms = false;
        [SerializeField] private float waveSpeed = 2.5f;
        [SerializeField] private float waveAmplitude = 0.12f;

        private Vector3 _initialLeftPos;
        private Vector3 _initialRightPos;
        private bool _capturedInitial;
        private bool _flipThumb180 = false;

        private float _yaw = 0f;
        private float _pitch = 10f;
        private float _distance = 1.6f;

        private void Start()
        {
            FindDependencies();
            InitCameraOrbit();
        }

        private void OnEnable()
        {
            FindDependencies();
            SignLoop.Diagnostics.HandCurlDiagnostic.RunCheck();
        }

        private void FindDependencies()
        {
            if (avatarWrapper == null) avatarWrapper = GetComponent<AvatarRigWrapper>();
            if (avatarWrapper == null) avatarWrapper = FindFirstObjectByType<AvatarRigWrapper>();

            if (avatarWrapper != null)
            {
                if (handPose == null) handPose = avatarWrapper.HandPose;
                if (armIK == null) armIK = avatarWrapper.ArmIK;

                if (handPose == null || !handPose.IsLeftBound)
                {
                    avatarWrapper.AutoBind();
                    if (handPose == null) handPose = avatarWrapper.HandPose;
                    if (armIK == null) armIK = avatarWrapper.ArmIK;
                }
            }

            if (signPlayer == null) signPlayer = GetComponent<SignLoop.Avatar.ISLSignPlayer>();
            if (signPlayer == null) signPlayer = GetComponentInChildren<SignLoop.Avatar.ISLSignPlayer>();
            if (signPlayer == null) signPlayer = FindFirstObjectByType<SignLoop.Avatar.ISLSignPlayer>();
            if (signPlayer == null && avatarWrapper != null)
            {
                signPlayer = avatarWrapper.gameObject.GetComponent<SignLoop.Avatar.ISLSignPlayer>();
                if (signPlayer == null) signPlayer = avatarWrapper.gameObject.AddComponent<SignLoop.Avatar.ISLSignPlayer>();
            }

            if (targetCamera == null) targetCamera = Camera.main;

            if (armIK != null && armIK.LeftArmTarget != null && armIK.RightArmTarget != null && !_capturedInitial)
            {
                _initialLeftPos = armIK.LeftArmTarget.position;
                _initialRightPos = armIK.RightArmTarget.position;
                _capturedInitial = true;
            }
        }

        private void InitCameraOrbit()
        {
            if (targetCamera != null)
            {
                targetCamera.nearClipPlane = 0.03f;
                targetCamera.allowMSAA = true;
                targetCamera.allowDynamicResolution = false;
                QualitySettings.antiAliasing = 8;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

                Vector3 target = transform.position + orbitTargetOffset;
                Vector3 toCam = targetCamera.transform.position - target;
                if (toCam.sqrMagnitude > 0.01f)
                {
                    _distance = Mathf.Clamp(toCam.magnitude, minDistance, maxDistance);
                    _pitch = targetCamera.transform.eulerAngles.x;
                    _yaw = targetCamera.transform.eulerAngles.y;
                }
                else
                {
                    _distance = 1.6f;
                    _yaw = 0f;
                    _pitch = 10f;
                }
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            HandleInput();
            HandleCameraOrbit();

            // Procedural arm waving test
            if (waveArms && _capturedInitial && armIK != null)
            {
                float offset = Mathf.Sin(Time.time * waveSpeed) * waveAmplitude;
                float vertical = Mathf.Cos(Time.time * waveSpeed * 0.5f) * (waveAmplitude * 0.5f);

                Vector3 leftPos = _initialLeftPos + new Vector3(0f, vertical, offset);
                Vector3 rightPos = _initialRightPos + new Vector3(0f, -vertical, -offset);

                armIK.SetLeftArmTarget(leftPos, armIK.LeftArmTarget.rotation);
                armIK.SetRightArmTarget(rightPos, armIK.RightArmTarget.rotation);
            }
        }

        private void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) ApplyHandShape(CanonicalHandShape.Neutral);
                else if (kb.digit2Key.wasPressedThisFrame) ApplyHandShape(CanonicalHandShape.OpenPalm);
                else if (kb.digit3Key.wasPressedThisFrame) ApplyHandShape(CanonicalHandShape.Fist);
                else if (kb.digit4Key.wasPressedThisFrame) ApplyHandShape(CanonicalHandShape.PointIndex);
                else if (kb.digit5Key.wasPressedThisFrame) ApplyHandShape(CanonicalHandShape.ThumbUp);
                else if (kb.digit6Key.wasPressedThisFrame) ApplyHandShape(CanonicalHandShape.Victory);
                else if (kb.digit7Key.wasPressedThisFrame) ApplyHandShape(CanonicalHandShape.CHand);
                else if (kb.digit8Key.wasPressedThisFrame) ApplyHandShape(CanonicalHandShape.OHand);

                if (kb.spaceKey.wasPressedThisFrame)
                {
                    waveArms = !waveArms;
                }
                return;
            }
#endif
            // Legacy input fallback
            if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyHandShape(CanonicalHandShape.Neutral);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyHandShape(CanonicalHandShape.OpenPalm);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyHandShape(CanonicalHandShape.Fist);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) ApplyHandShape(CanonicalHandShape.PointIndex);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) ApplyHandShape(CanonicalHandShape.ThumbUp);
            else if (Input.GetKeyDown(KeyCode.Alpha6)) ApplyHandShape(CanonicalHandShape.Victory);
            else if (Input.GetKeyDown(KeyCode.Alpha7)) ApplyHandShape(CanonicalHandShape.CHand);
            else if (Input.GetKeyDown(KeyCode.Alpha8)) ApplyHandShape(CanonicalHandShape.OHand);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                waveArms = !waveArms;
            }
        }

        private void HandleCameraOrbit()
        {
            if (targetCamera == null) return;

            bool isOrbiting = false;
            float mouseX = 0f;
            float mouseY = 0f;
            float scroll = 0f;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                isOrbiting = mouse.rightButton.isPressed || mouse.middleButton.isPressed;
                if (isOrbiting)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    mouseX = delta.x * 0.1f;
                    mouseY = delta.y * 0.1f;
                }
                scroll = mouse.scroll.ReadValue().y * 0.001f;
            }
#else
            isOrbiting = Input.GetMouseButton(1) || Input.GetMouseButton(2);
            if (isOrbiting)
            {
                mouseX = Input.GetAxis("Mouse X");
                mouseY = Input.GetAxis("Mouse Y");
            }
            scroll = Input.mouseScrollDelta.y * 0.1f;
#endif

            if (isOrbiting)
            {
                _yaw += mouseX * orbitSpeed;
                _pitch -= mouseY * orbitSpeed;
                _pitch = Mathf.Clamp(_pitch, -20f, 75f);
            }

            if (Mathf.Abs(scroll) > 0.001f)
            {
                _distance = Mathf.Clamp(_distance - scroll * zoomSpeed, minDistance, maxDistance);
            }

            Vector3 target = transform.position + orbitTargetOffset;
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 camPos = target - (rot * Vector3.forward * _distance);

            targetCamera.transform.position = camPos;
            targetCamera.transform.rotation = rot;
        }

        private Vector2 _scrollPos;

        public void ApplyHandShape(CanonicalHandShape shape)
        {
            if (handPose == null && avatarWrapper != null) handPose = avatarWrapper.HandPose;
            if (armIK == null && avatarWrapper != null) armIK = avatarWrapper.ArmIK;

            if (handPose != null)
            {
                handPose.SetCanonicalShape(HandSide.Left, shape);
                handPose.SetCanonicalShape(HandSide.Right, shape);

                if (shape == CanonicalHandShape.ThumbUp && armIK != null)
                {
                    // Rotate wrists around forearm axis (pronation/supination) so thumbs point straight UP (+Y)
                    float rollSign = _flipThumb180 ? 1f : -1f;
                    armIK.SetWristRollOffset(isLeft: true, 90f * rollSign);
                    armIK.SetWristRollOffset(isLeft: false, -90f * rollSign);
                    armIK.SetMatchWristRotation(false);
                }
                else if (armIK != null)
                {
                    // Reset wrist roll to natural forearm alignment
                    armIK.ResetWristRollOffsets();
                    armIK.SetMatchWristRotation(false);
                }

                Debug.Log($"[DesktopGestureTester] Applied ISL Hand Shape: {shape}");
            }
            else
            {
                Debug.LogWarning("[DesktopGestureTester] HandPoseController reference not bound.");
            }
        }

        // ISL Categories & Sign Browser State
        private int _selectedCategoryIndex = 0;
        private static readonly string[] CategoryTabs = { "All", "Core", "Transit", "People", "Places", "Jobs", "Misc" };
        private string _searchQuery = "";
        private readonly List<string> _filteredSignsCache = new List<string>(80);

        private static readonly HashSet<string> CoreCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Hello", "ThankYou", "HowAreYou", "GoodMorning", "You", "I", "Sign", "India"
        };

        private static readonly HashSet<string> TransportCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Plane", "Car", "Truck", "Bicycle", "Bus", "Boat", "Train", "TrainTicket", "Transportation"
        };

        private static readonly HashSet<string> PeopleCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Baby", "Boy", "Girl", "Child", "Adult", "Man", "Woman", "Mother", "Father", "Parent",
            "Son", "Daughter", "Brother", "Sister", "Family", "Grandfather", "Grandmother",
            "Husband", "Wife", "Friend", "Neighbour", "King", "Queen", "President", "Crowd"
        };

        private static readonly HashSet<string> PlacesCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "House", "City", "StreetOrRoad", "TrainStation", "Restaurant", "Court", "School",
            "Office", "University", "Park", "StoreOrShop", "Library", "Hospital", "Temple",
            "Market", "Bank", "Ground", "Location"
        };

        private static readonly HashSet<string> JobsCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Doctor", "Teacher", "Student", "Lawyer", "Patient", "Waiter", "Police", "Soldier",
            "Artist", "Author", "Manager", "Reporter", "Actor", "Secretary", "Priest", "Player", "Job"
        };

        private static readonly HashSet<string> MiscCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Bird", "SmallLittle", "Second"
        };

        private static readonly string[] FallbackSignCatalog = new string[]
        {
            "Actor", "Adult", "Artist", "Author", "Baby", "Bank", "Bicycle", "Bird", "Boat", "Boy",
            "Brother", "Bus", "Car", "Child", "City", "Court", "Crowd", "Daughter", "Doctor", "Family",
            "Father", "Friend", "Girl", "GoodMorning", "Grandfather", "Grandmother", "Ground", "Hello",
            "Hospital", "House", "HowAreYou", "Husband", "I", "India", "Job", "King", "Lawyer",
            "Library", "Location", "Man", "Manager", "Market", "Mother", "Neighbour", "Office", "Parent",
            "Park", "Patient", "Plane", "Player", "Police", "President", "Priest", "Queen", "Reporter",
            "Restaurant", "School", "Second", "Secretary", "Sign", "Sister", "SmallLittle", "Soldier",
            "Son", "StoreOrShop", "StreetOrRoad", "Student", "Teacher", "Temple", "ThankYou",
            "Train", "TrainStation", "TrainTicket", "Transportation", "Truck", "University", "Waiter",
            "Wife", "Woman", "You"
        };

        private List<string> GetFilteredSigns()
        {
            _filteredSignsCache.Clear();
            IReadOnlyList<string> sourceList = (signPlayer != null && signPlayer.AvailableSignNames.Count > 0)
                ? signPlayer.AvailableSignNames
                : FallbackSignCatalog;

            for (int i = 0; i < sourceList.Count; i++)
            {
                string sign = sourceList[i];

                if (_selectedCategoryIndex > 0)
                {
                    bool matchCat = false;
                    switch (_selectedCategoryIndex)
                    {
                        case 1: matchCat = CoreCategory.Contains(sign); break;
                        case 2: matchCat = TransportCategory.Contains(sign); break;
                        case 3: matchCat = PeopleCategory.Contains(sign); break;
                        case 4: matchCat = PlacesCategory.Contains(sign); break;
                        case 5: matchCat = JobsCategory.Contains(sign); break;
                        case 6: matchCat = MiscCategory.Contains(sign); break;
                    }
                    if (!matchCat) continue;
                }

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    if (sign.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                _filteredSignsCache.Add(sign);
            }

            return _filteredSignsCache;
        }

        private void DrawSignButton(string signName)
        {
            bool isCurrent = signPlayer != null && signPlayer.IsPlaying &&
                             string.Equals(signPlayer.CurrentSignName, signName, StringComparison.OrdinalIgnoreCase);

            Color prevColor = GUI.backgroundColor;
            if (isCurrent) GUI.backgroundColor = new Color(0.3f, 0.85f, 1.0f);

            if (GUILayout.Button(signName, GUILayout.Height(24)))
            {
                if (signPlayer != null)
                {
                    signPlayer.PlaySign(signName);
                }
            }

            GUI.backgroundColor = prevColor;
        }

        private void OnGUI()
        {
            GUI.depth = 0;
            float panelHeight = Mathf.Min(680f, Screen.height - 30);
            GUILayout.BeginArea(new Rect(15, 15, 340, panelHeight), "SignLoop ISL Motion Studio", GUI.skin.window);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            // Display Quality Tip
            GUILayout.Label("<color=#FFFF55><b>Game View HD Tip:</b>\nSet Game tab Scale to 1x & uncheck 'Low Resolution Aspect Ratios'</color>");
            GUILayout.Space(4);

            // Section 1: Real ISL Sign Motion Clips
            int totalSignCount = (signPlayer != null && signPlayer.AvailableSignNames.Count > 0) ? signPlayer.AvailableSignNames.Count : FallbackSignCatalog.Length;
            GUILayout.Label($"<b>Authentic ISL Signs ({totalSignCount} Signs Available):</b>");
            if (signPlayer != null)
            {
                if (signPlayer.IsPlaying)
                {
                    GUILayout.Label($"<color=cyan><b>Sign:</b> {signPlayer.CurrentSignName}</color>");
                    GUILayout.Label($"<b>Phase:</b> <color=orange>{signPlayer.PlaybackPhaseText}</color>");
                    GUILayout.Label($"<b>R Curls:</b> T:{signPlayer.CurrentRightThumbCurl:F0}° I:{signPlayer.CurrentRightIndexCurl:F0}° M:{signPlayer.CurrentRightMiddleCurl:F0}° R:{signPlayer.CurrentRightRingCurl:F0}° P:{signPlayer.CurrentRightPinkyCurl:F0}°");
                    Rect pRect = GUILayoutUtility.GetRect(300, 10);
                    GUI.HorizontalScrollbar(pRect, 0, signPlayer.NormalizedProgress, 0, 1);

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(signPlayer.IsPaused ? "Resume" : "Pause"))
                    {
                        if (signPlayer.IsPaused) signPlayer.Resume();
                        else signPlayer.Pause();
                    }
                    if (GUILayout.Button("Stop / Reset"))
                    {
                        signPlayer.Stop();
                    }
                    GUILayout.EndHorizontal();
                }

                signPlayer.Loop = GUILayout.Toggle(signPlayer.Loop, " Loop Sign Playback");
                signPlayer.MatchWristRotation = GUILayout.Toggle(signPlayer.MatchWristRotation, " Override Wrist Rotations (Video Quats)");

                // Pacing & Speed Control
                GUILayout.Space(2);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>Speed:</b> {signPlayer.PlaybackSpeed:F2}x", GUILayout.Width(90));
                signPlayer.PlaybackSpeed = GUILayout.HorizontalSlider(signPlayer.PlaybackSpeed, 0.25f, 2.0f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("0.5x Slow")) signPlayer.PlaybackSpeed = 0.5f;
                if (GUILayout.Button("0.75x")) signPlayer.PlaybackSpeed = 0.75f;
                if (GUILayout.Button("1.0x Norm")) signPlayer.PlaybackSpeed = 1.0f;
                if (GUILayout.Button("1.5x Fast")) signPlayer.PlaybackSpeed = 1.5f;
                GUILayout.EndHorizontal();

                if (GUILayout.Button("🔄 Reload All Clips From Disk"))
                {
                    signPlayer.LoadClipLibrary();
                }

                GUILayout.Space(6);

                // Search Bar
                GUILayout.BeginHorizontal();
                GUILayout.Label("<b>Search:</b>", GUILayout.Width(52));
                _searchQuery = GUILayout.TextField(_searchQuery);
                if (!string.IsNullOrEmpty(_searchQuery) && GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    _searchQuery = "";
                }
                GUILayout.EndHorizontal();

                // Category Filter Buttons
                GUILayout.Space(2);
                GUILayout.BeginHorizontal();
                for (int c = 0; c < 4; c++)
                {
                    Color prevBg = GUI.backgroundColor;
                    if (_selectedCategoryIndex == c) GUI.backgroundColor = new Color(0.3f, 0.85f, 1.0f);
                    if (GUILayout.Button(CategoryTabs[c])) _selectedCategoryIndex = c;
                    GUI.backgroundColor = prevBg;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                for (int c = 4; c < CategoryTabs.Length; c++)
                {
                    Color prevBg = GUI.backgroundColor;
                    if (_selectedCategoryIndex == c) GUI.backgroundColor = new Color(0.3f, 0.85f, 1.0f);
                    if (GUILayout.Button(CategoryTabs[c])) _selectedCategoryIndex = c;
                    GUI.backgroundColor = prevBg;
                }
                GUILayout.EndHorizontal();

                // Filtered Sign Button Grid (2 columns)
                List<string> filtered = GetFilteredSigns();
                GUILayout.Space(4);
                GUILayout.Label($"<b>Select Sign ({filtered.Count}):</b>");

                for (int i = 0; i < filtered.Count; i += 2)
                {
                    GUILayout.BeginHorizontal();
                    DrawSignButton(filtered[i]);
                    if (i + 1 < filtered.Count)
                    {
                        DrawSignButton(filtered[i + 1]);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("<color=yellow>ISLSignPlayer initializing...</color>");
                if (GUILayout.Button("Find Sign Player"))
                {
                    FindDependencies();
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Finger Curls & Posture Sensitivity:</b>");
            if (handPose != null)
            {
                GUILayout.Label($"<b>Flexion Axes:</b> L={handPose.LeftFingerFlexionAxis.x:F0} | R={handPose.RightFingerFlexionAxis.x:F0}");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>Curl Scale:</b> {handPose.CurlMultiplier:F2}x", GUILayout.Width(100));
                handPose.CurlMultiplier = GUILayout.HorizontalSlider(handPose.CurlMultiplier, 0.5f, 2.5f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Invert L Curls")) handPose.FlipFingerFlexion(HandSide.Left);
                if (GUILayout.Button("Invert R Curls")) handPose.FlipFingerFlexion(HandSide.Right);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Natural (+Z Palm)"))
                {
                    handPose.LeftFingerFlexionAxis = new Vector3(0f, 0f, 1f);
                    handPose.RightFingerFlexionAxis = new Vector3(0f, 0f, 1f);
                    handPose.LeftThumbFlexionAxis = new Vector3(0f, 0.7f, 0.7f);
                    handPose.RightThumbFlexionAxis = new Vector3(0f, -0.7f, 0.7f);
                }
                if (GUILayout.Button("Inverted (-Z)"))
                {
                    handPose.LeftFingerFlexionAxis = new Vector3(0f, 0f, -1f);
                    handPose.RightFingerFlexionAxis = new Vector3(0f, 0f, -1f);
                    handPose.LeftThumbFlexionAxis = new Vector3(0f, -0.7f, -0.7f);
                    handPose.RightThumbFlexionAxis = new Vector3(0f, 0.7f, -0.7f);
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("🔍 Run Curl Biomechanical Test"))
                {
                    SignLoop.Diagnostics.HandCurlDiagnostic.RunCheck();
                }
            }

            GUILayout.Space(4);
            GUILayout.Label("<b>Static Finger Postures (Keys 1-8):</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1. Neutral")) ApplyHandShape(CanonicalHandShape.Neutral);
            if (GUILayout.Button("2. OpenPalm")) ApplyHandShape(CanonicalHandShape.OpenPalm);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("3. Fist")) ApplyHandShape(CanonicalHandShape.Fist);
            if (GUILayout.Button("4. PointIndex")) ApplyHandShape(CanonicalHandShape.PointIndex);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("5. ThumbUp")) ApplyHandShape(CanonicalHandShape.ThumbUp);
            if (GUILayout.Button("6. Victory")) ApplyHandShape(CanonicalHandShape.Victory);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("7. C-Hand")) ApplyHandShape(CanonicalHandShape.CHand);
            if (GUILayout.Button("8. O-Hand")) ApplyHandShape(CanonicalHandShape.OHand);
            GUILayout.EndHorizontal();

            if (GUILayout.Button(_flipThumb180 ? "Thumb Up: Roll Inverted [Click to Toggle]" : "Thumb Up: Standard [Click to Toggle]"))
            {
                _flipThumb180 = !_flipThumb180;
                ApplyHandShape(CanonicalHandShape.ThumbUp);
            }

            if (armIK != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>Wrist Roll:</b> {armIK.LeftWristRollOffset:F0}°", GUILayout.Width(110));
                float curRoll = armIK.LeftWristRollOffset;
                float newRoll = GUILayout.HorizontalSlider(curRoll, -180f, 180f);
                if (Mathf.Abs(newRoll - curRoll) > 0.5f)
                {
                    armIK.SetWristRollOffset(isLeft: true, newRoll);
                    armIK.SetWristRollOffset(isLeft: false, -newRoll);
                }
                if (GUILayout.Button("Reset", GUILayout.Width(45)))
                {
                    armIK.ResetWristRollOffsets();
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Arm IK:</b>");
            waveArms = GUILayout.Toggle(waveArms, " Wave Arms Procedural (Space)");

            GUILayout.Space(6);
            string statusColor = (handPose != null && handPose.IsLeftBound) ? "green" : "red";
            string statusText = (handPose != null && handPose.IsLeftBound) ? "Bound" : "Unbound";
            string armColor = (armIK != null && armIK.IsLeftArmBound) ? "green" : "red";
            string armText = (armIK != null && armIK.IsLeftArmBound) ? "IK Active" : "Unbound";
            GUILayout.Label($"<b>Fingers:</b> <color={statusColor}>{statusText}</color> | <b>Arms:</b> <color={armColor}>{armText}</color>");

            if (GUILayout.Button("Re-Bind Avatar Rig"))
            {
                if (avatarWrapper != null) avatarWrapper.AutoBind();
            }

            GUILayout.Space(6);
            GUILayout.Label("<size=10><b>Mouse Controls:</b>\n• Right-Click + Drag: Orbit Camera\n• Scroll Wheel: Zoom Hands/Face</size>");

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}

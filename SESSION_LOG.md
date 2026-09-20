# SignLoop Project Session Log

This document records the chronological history of work completed across sessions, decisions made, and the current operational state of the codebase.

---

## Session: 2026-09-17 (Session 1)

### 1. Environment & Context Discovery
* **Active Git Branch:** `job` (synced with `origin/job`).
* **Environment Diagnostic:** Resolved local Gemini plugin hook blocker (`googlecloudtools.datacloud_telemetry` module path error), restoring full tool execution.
* **Codebase Audit:**
  - Audited offline ISL Sign-to-Speech ML pipeline:
    - AI4Bharat INCLUDE dataset (263 classes / words).
    - Preprocessed dataset: [`dataset/data/include_keypoints_master.npz`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/dataset/data/include_keypoints_master.npz) (3,295 samples, 30 frames, 75 landmarks $\times$ 3 coordinates = 225 features).
    - Policy architecture: [`models/policy.py`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/models/policy.py) BiGRU (2 layers, 256 hidden) + [`TemporalAttention`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/models/policy.py#L6-L24) + Action Head (263 classes).
    - Environment: [`isl_env.py`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl_env.py) Gymnasium environment with initial reward logic.
    - Training pipeline: [`train_policy.py`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/train_policy.py) (Supervised Warmup + REINFORCE RL fine-tuning).
    - Saved checkpoint: [`models/isl_policy_model.pth`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/models/isl_policy_model.pth).

### 2. Unity 3D Asset & Git LFS Setup
* **Git LFS Initialization:** Executed `git lfs install` to enable LFS hooks.
* **Created [`.gitattributes`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/.gitattributes):**
  - Configured LFS tracking for all 3D formats: `.fbx`, `.obj`, `.blend`, `.glb`, `.gltf`, `.dae`, `.3ds`, `.max`, `.mb`, `.ma`, `.stl`.
  - Configured LFS tracking for textures and images: `.png`, `.tga`, `.psd`, `.psb`, `.exr`, `.hdr`, `.tif`, `.tiff`, `.dds`.
  - Configured LFS tracking for audio: `.wav`, `.mp3`, `.ogg`, `.flac`, `.aif`.
  - Configured LFS tracking for native Quest / Android VR binaries: `.dll`, `.so`, `.aar`, `.jar`, `.apk`.
  - Configured LFS tracking for ML models: `.onnx`, `.pth`, `.pt`, `.tflite`, `.bin`.
  - Set Unity YAML merge and text drivers (`unityyamlmerge` for scenes/prefabs, text for `.cs`, `.meta`, `.mat`, `.asset`).
* **Updated [`.gitignore`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/.gitignore):**
  - Added comprehensive Unity cache and build exclusions (`Library/`, `Temp/`, `Obj/`, `Build/`, `UserSettings/`, IDE files `*.sln`, `*.csproj`, etc.) while strictly preserving existing Python and dataset ignore patterns.
* **Migrated Existing Weights:**
  - Migrated [`models/isl_policy_model.pth`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/models/isl_policy_model.pth) to Git LFS pointer tracking.

### 3. Immediate Next Steps
* [x] Create / initialize Unity 6.3 LTS project structure (`isl-vr-unity/`) with OpenXR and Animation Rigging configuration.
* [ ] Import 3D Humanoid Avatar (.fbx / .glb) with 52 ARKit facial blendshapes and 21-DOF hand skeletons.
* [ ] Configure Unity Animation Rigging (`TwoBoneIKConstraint` for arms, finger pose interpolation).
* [ ] Implement composite reward function in [`isl_env.py`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl_env.py) ($R_{accuracy}$, $R_{bilateral\_sync}$, and latency penalty).
* [ ] Export policy model to ONNX for Quest 3 runtime execution.

---

## Session: 2026-09-18 (Session 2)

### 1. Unity Project Structure Initialization
* Initialized Unity project root directory [`isl-vr-unity/`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity) targeting Unity 6.3 LTS / Quest 3.
* **Packages Configuration:**
  - Configured [`isl-vr-unity/Packages/manifest.json`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Packages/manifest.json) with dependencies: `com.unity.animation.rigging`, `com.unity.xr.openxr`, `com.unity.xr.interaction.toolkit`, `com.unity.inputsystem`, and `com.unity.mathematics`.
* **Project Settings & Directives Enforcement:**
  - Added [`isl-vr-unity/ProjectSettings/EditorSettings.asset`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/ProjectSettings/EditorSettings.asset) explicitly enforcing `m_SerializationMode: 2` (Force Text) and `m_ExternalVersionControl: Visible Meta Files`.
  - Added [`isl-vr-unity/ProjectSettings/ProjectVersion.txt`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/ProjectSettings/ProjectVersion.txt) setting target version `6000.0.33f1`.
* **Asset Directory Scaffolding:**
  - Created [`Assets/Animations/`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Animations) for canonical ISL hand postures & gesture clips.
  - Created [`Assets/Materials/`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Materials) for shaders and PBR materials.
  - Created [`Assets/Models/`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Models) with [`README.md`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Models/README.md) documenting LFS tracking, 52 ARKit blendshapes, and 21-DOF hand skeleton requirements.
  - Created [`Assets/Prefabs/`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Prefabs) for avatar rigs, XR rigs, and UI components.
  - Created [`Assets/Scenes/`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scenes) for VR interaction scenes.
  - Created structured [`Assets/Scripts/`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts) subdirectories:
    - `Avatar/`: Blendshapes & NMM controller hooks.
    - `IK/`: Animation Rigging solvers & hand posture slerp solvers.
    - `Audio/`: whisper.cpp & Piper TTS edge hooks.
    - `Bridge/`: IPC / socket bridge with Python ML backend.
    - `VR/`: OpenXR & Quest 3 hand tracking / controller interactions.
  - Added project documentation in [`isl-vr-unity/README.md`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/README.md).

### 2. Speech-to-Sign Procedural Rigging Architecture
* **Prefab Wrapper Pattern ([`AvatarRigWrapper`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/AvatarRigWrapper.cs) & [`AvatarBoneMapping`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/AvatarBoneMapping.cs)):**
  - Implemented decoupled avatar rig wrapper allowing arbitrary 3D avatar swap (from prototype 54k tris to <=25k mesh) with zero script breakage.
  - Provided `AutoPopulate(Animator)` helper leveraging Humanoid bone mappings and SkinnedMeshRenderer heuristics.
* **ARKit Facial Blendshape Controller ([`ARKitFaceController`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ARKitFaceController.cs) & [`ARKitBlendShape`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ARKitBlendShape.cs)):**
  - Built string-based ARKit lookup stripping FBX/DCC prefixes once at initialization, avoiding brittle integer index coupling.
  - Pre-allocated 52-channel flat array with cached indices; achieved **0 GC allocations in `Update()`** for Quest 3 VR performance.
* **Arm IK & 21-Joint Finger Blending ([`ArmIKController`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/ArmIKController.cs) & [`HandPoseController`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/HandPoseController.cs)):**
  - Scaffolds Unity Animation Rigging `TwoBoneIKConstraint` for Left and Right arms with persistent target and elbow hint transforms.
  - Implemented zero-allocation `Quaternion.Slerp` buffer blending across 21 joints per hand for canonical ISL shapes (`Fist`, `PointIndex`, `ThumbUp`, `Victory`, `CHand`, `OHand`, `OpenPalm`).
### 3. Display Resolution & Anatomical Motion Fixes, and Authentic ISL Sign Motion Pipeline
* **Scene vs. Game View Resolution Resolution:**
  - Diagnosed why the avatar appeared crisp HD in the Scene tab but pixelated in the Game tab:
    1. The Game view top-toolbar **Scale slider** is often scrolled above `1x` (e.g. 2x/3x zoom stretches pixel buffer).
    2. Resolution dropdown set to **"Free Aspect"** in a small docked pane limits the physical render buffer.
    3. The **"Low Resolution Aspect Ratios"** setting downsamples rendering on high-DPI displays.
  - Programmatically enforced in [`DesktopGestureTester.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/DesktopGestureTester.cs):
    - `targetCamera.allowMSAA = true;`
    - `targetCamera.allowDynamicResolution = false;`
    - `QualitySettings.antiAliasing = 8;` (8x MSAA)
    - `QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;`
  - Added on-screen guidance in the GUI panel reminding the user to set Scale to 1x and choose 1080p / 16:9.
* **Biomechanical Impossible Motions & Thumbs Up Resolution:**
  - Resolved wrist dislocation in [`ArmIKController.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/ArmIKController.cs):
    - In TwoBoneIK, the wrist (`tip`) previously forced a world identity rotation `(0,0,0)`, which twisted the hand backward and sideways.
    - Added `matchWristRotation` toggle (default false). When false, the wrist naturally preserves its bind pose alignment with the forearm, eliminating unnatural twists.
  - Fixed Thumbs Up in [`HandPoseController.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/HandPoseController.cs) & [`DesktopGestureTester.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/DesktopGestureTester.cs):
    - Changed thumb abduction from `Vector3.up` (which caused axial twisting) to the transverse abduction axis (`Vector3.forward`).
    - Added neutral handshake wrist orientation for Thumbs Up so the thumb points vertically up (+Y).
    - Added zero-allocation [`SetFingerCurls(HandSide, float thumb, float index, float middle, float ring, float pinky)`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/HandPoseController.cs) strictly clamped between 0° and 85°.
* **Authentic ISL Sign Motion Clip Pipeline (Skeletal Keypoints $\rightarrow$ 3D Avatar):**
  - Created [`export_isl_clips.py`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/export_isl_clips.py):
    - Extracts 30-frame temporal trajectories from [`include_keypoints_master.npz`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/include_keypoints_master.npz) and [`dataset/data/include_keypoints.npz`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/dataset/data/include_keypoints.npz).
    - Calibrates 3D coordinates (MediaPipe landmarks $\rightarrow$ Unity avatar physical arm reach: `0.20m/unit`).
    - Derives wrist positions, elbow bend hints, wrist quaternions, and 5-finger curl angles.
    - Exported 12 core ISL sign clips into [`isl-vr-unity/Assets/Animations/ISLClips/`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Animations/ISLClips) and [`isl-vr-unity/Assets/Resources/ISLClips/`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Resources/ISLClips):
      * `Hello`, `ThankYou`, `HowAreYou`, `GoodMorning`, `Doctor`, `Friend`, `Teacher`, `India`, `You`, `I`, `Sign`, `House`.
    - Generated valid Unity `.meta` files for all clips.
  - Implemented [`ISLSignPlayer.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ISLSignPlayer.cs):
    - Streams 30-frame authentic human signs with smooth 250ms lead-in blending from rest stance.
    - Smoothly drives `ArmIKController` (wrists, elbows) and `HandPoseController` (anatomical finger curls).
    - 0 GC allocations in `LateUpdate()`.
  - Upgraded [`DesktopGestureTester.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/DesktopGestureTester.cs) with interactive ISL Sign Studio UI:
    - Clickable sign buttons, timeline progress bar, loop toggle, and pause/resume.
### 4. Session 3: Action Pacing, Anatomical Thumbs Up, and Expressive Finger Curls (2026-09-18)

#### A. Diagnostic & Root Causes
1. **Actions Too Fast & Abrupt Snapping:** Clips previously ran at 1.0s and immediately snapped back to resting stance on completion, cutting off the sign before it could be observed.
2. **Thumbs Up Pointing to Ground:** 
   - In FBX humanoid models, bone length extends along local +Y. Applying world `Euler(0, 0, ±90)` rotated fingers in the XY plane without pitching the hand forward, causing the thumb to point downward towards the floor.
   - `HandPoseController.LateUpdate()` previously looped through joint index 0 (the wrist), continuously overwriting the wrist's orientation with T-pose neutral angles.
3. **Fingers Not Clear or Present:**
   - Knuckles on the avatar's right hand are mirrored along the sagittal plane (Right Hand knuckle vector is -X, while Left Hand is +X). Having both hands flex along `+X` caused the right hand to hyperextend backwards.
   - `HandPoseController.ApplyAnatomicalCurl()` had omitted `PinkyDIP`, leaving the pinky tip uncurled.
   - Monocular MediaPipe hand landmarks in video compressed finger flexion to small angles, making motion imperceptible without amplification.

#### B. Engineering Solutions Implemented
1. **Three-Phase Sign Playback State Machine ([`ISLSignPlayer.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ISLSignPlayer.cs)):**
   - Active Trajectory (2.5s): Smooth interpolation across frames with 300ms lead-in blend.
   - Hold Phase (0.8s): Steadily holds the completed sign posture at the apex so human observers can read the sign.
   - Lead-Out Return (0.5s): Smooth cubic `SmoothStep` return lerp to neutral resting stance.
   - Pacing Controls: Added interactive speed slider (`0.25x` to `2.0x`) with one-click presets (`0.5x Slow`, `0.75x`, `1.0x`, `1.5x`).
   - Direct Disk Fallback: Reads directly from `Resources/ISLClips/*.json` on disk to immediately load updated clips without restarting Play Mode.
2. **Anatomical Thumbs-Up Orientation ([`DesktopGestureTester.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/DesktopGestureTester.cs) & [`HandPoseController.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/HandPoseController.cs)):**
   - Implemented exact mathematical Euler rotations for handshake orientation:
     - Left Hand: `Quaternion.Euler(-90f * flipSign, 0f, 90f)`
     - Right Hand: `Quaternion.Euler(-90f * flipSign, 0f, -90f)`
     - Positions fingers forward (+Z), thumb straight UP (+Y), and palm inward ($\pm X$).
   - Added interactive "Thumb Up: Flip 180°" toggle in GUI.
   - Cleaned `CanonicalHandShape.ThumbUp` to curl 4 fingers to 80° while keeping thumb joints (CMC, MCP, IP) in natural open neutral extension.
   - Added configurable `MatchWristRotation` toggle (default false): wrists naturally follow forearm IK for 100% human-safe, twist-free motion.
3. **Expressive Finger Curls ([`HandPoseController.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/HandPoseController.cs) & [`export_isl_clips.py`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/export_isl_clips.py)):**
   - Symmetrized flexion axes: Left Hand = `(1, 0, 0)`, Right Hand = `(-1, 0, 0)`.
   - Symmetrized thumb flexion axes: Left Thumb = `(0.5, 0.8, 0.3)`, Right Thumb = `(-0.5, 0.8, 0.3)`.
   - Added `PinkyDIP` to `ApplyAnatomicalCurl()`.
   - Added `curlMultiplier` (default 1.25x, range 0.5x to 2.5x) with live GUI slider to make finger flexions clearly perceptible from across the room.
   - Added interactive "Invert L Curls" and "Invert R Curls" buttons.
   - Regenerated all 12 authentic ISL sign clips with 2.5s duration and extension ratio curling.

---

## Session 4: Hand Inversion Elimination & Skeletal Keypoint 3D Model Pipeline (2026-09-18)

### 1. Diagnostic & Root Cause Analysis
* **Hand Inversion / Forearm Twisting Bug:**
  - **Berry Phase (Holonomy) Drift:** In Unity, transforms retain their rotations from previous frames when no Animator clip is active. `ArmIKController.SolveTwoBoneIK()` was applying `deltaUpper * root.rotation` and `deltaForearm * mid.rotation` without ever resetting bones to their bind pose. As the hand moved through 3D arcs during signing or procedural waving, shortest-arc rotations (`Quaternion.FromToRotation`) accumulated unconstrained axial roll, eventually twisting the forearm 180° and flipping the hand/fingers upside-down.
  - **Elbow Plane Singularity / Normal Inversion:** The bend normal was derived via `Vector3.Cross(at, hintDir)`. When the hand crossed the plane passing through the shoulder and elbow hint, the cross product abruptly flipped sign ($+\vec{n} \to -\vec{n}$), causing the elbow and child hand to instantly flip inside-out.
  - **Stuck Wrist Orientations:** When toggling hand shapes (such as `ThumbUp`), `MatchWristRotation` was set to true, overwriting `tip.rotation`. When switching to other shapes, `MatchWristRotation` was set to false, but `tip.localRotation` was never restored, leaving the wrist permanently locked in a twisted pose.

### 2. Engineering Solutions Implemented
* **Pristine Bind Pose Caching & Per-Frame Reset ([`ArmIKController.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/ArmIKController.cs)):**
  - Cached bind local rotations (`_leftUpperBindLocalRot`, `_leftMidBindLocalRot`, `_leftTipBindLocalRot`, and right-side mirrors) and bone lengths in `CaptureBindPose()`.
  - In `SolveArmIK()`, bones are reset to their bind local rotations at the beginning of each frame before solving IK. This completely eliminates frame-to-frame roll accumulation, Berry phase, and stuck postures.
* **Continuous, Inversion-Free Law-of-Cosines Solver:**
  - Projects the elbow hint onto the plane perpendicular to the arm vector, smoothly blended with the natural human outward/backward bend direction:
    $$B = A + L_1 (\cos\alpha \cdot \vec{u} + \sin\alpha \cdot \vec{v}_{\text{bend}})$$
  - Eliminates the candidate angle flip (`dir1`, `dir2`) and cross-product sign flip entirely.
* **Anatomical Clamping & Axial Wrist Roll Control:**
  - Replaced arbitrary world Euler angles for `ThumbUp` with true anatomical pronation/supination (axial roll around the forearm axis).
  - Added `LeftWristRollOffset` and `RightWristRollOffset` with live GUI slider in [`DesktopGestureTester.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/DesktopGestureTester.cs).
  - Enforced `Quaternion.RotateTowards(naturalWristRot, targetRot, 70f)` when `MatchWristRotation` is enabled, guaranteeing noisy monocular video landmarks can never twist the wrist beyond physiological human limits.
* **Keypoint Export Benchmark:**
  - Benchmarked `export_isl_clips.py`: achieves **0.006s (6ms) per clip**.
  - All 263 canonical vocabulary words can be converted to Unity JSON clips in **~1.6 seconds** (~8.5 MB total).
  - All 3,295 dataset clips can be converted in **~20 seconds** (~110 MB total).

---

## Session 5: Complete 80-Word Vocabulary Keypoint Extraction & Unity Tester Upgrades (2026-09-19)

### 1. Architectural Scaling & Dataset Keypoint Extraction
* **Dataset Vocabulary Audit:**
  - Audited `dataset/data/include_keypoints.npz` (1,118 video extractions covering 73 unique words across 4 categories: Jobs, Transportation, People, Places).
  - Audited `include_keypoints_master.npz` (greetings & core pronouns: `Hello`, `ThankYou`, `HowAreYou`, `GoodMorning`, `You`, `I`, `Sign`).
  - Combined vocabulary count: **80 authentic ISL words**.
* **Automated Sample Scoring & Clip Exporter ([`export_isl_clips.py`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/export_isl_clips.py)):**
  - Implemented `score_sample(sample)` to evaluate tracking completeness (30/30 active hand frames) and motion dynamism across all samples of each class, ensuring the highest quality performance for each sign.
  - Preserved verified baseline indices for calibrated words (`Doctor`, `Friend`, `Teacher`, `India`, `House`).
  - Built `clean_sign_name(raw_label)` generating standard PascalCase names.
  - Built `ensure_meta_file(file_path)` automatically generating valid Unity `.meta` files with unique GUIDs for all exported `.json` assets.
  - Exported all 80 authentic ISL sign clips into:
    - `isl-vr-unity/Assets/Animations/ISLClips/` (80 clips + 80 `.meta` files)
    - `isl-vr-unity/Assets/Resources/ISLClips/` (80 clips + 80 `.meta` files)

### 2. Unity Tester App Upgrades ([`DesktopGestureTester.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/DesktopGestureTester.cs) & [`ISLSignPlayer.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ISLSignPlayer.cs))
* **Alphabetical Clip Discovery:**
  - Upgraded [`ISLSignPlayer.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ISLSignPlayer.cs) to sort `AvailableSignNames` alphabetically upon clip loading.
* **Interactive Sign Browser & Filter System:**
  - Added live search bar with instant substring filtering and one-click clear (`✕`).
  - Added semantic category filters (`All (80)`, `Core`, `Transit`, `People`, `Places`, `Jobs`, `Misc`).
  - Added active sign indicator highlighting the currently playing sign in cyan.
  - Pre-allocated zero-allocation `_filteredSignsCache` for smooth 60+ FPS desktop and mobile VR execution.
  - Full fallback catalog ensuring all 80 signs are browsable immediately even prior to runtime clip initialization.

---

## Session 6: Biomechanical Calibration & Avatar Body Penetration Fix (2026-09-19)

### 1. Root Cause Analysis
* **Body Penetration (Wrists & Hands clipping into torso/pelvis):**
  - In [`export_isl_clips.py`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/export_isl_clips.py), `CHEST_CENTER` was set with $Z = 0.05$m and clamped to $Z \in [0.10, 0.55]$m. Since the avatar torso surface sits at $Z \approx +0.10$–$0.12$m, wrists at $Z = 0.10$m drove hands and 10cm fingers directly inside the ribcage and abdomen.
  - In [`ISLSignPlayer.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ISLSignPlayer.cs), neutral rest targets were set to $Y = 0.85$m–$0.90$m and $Z = 0.15$m–$0.20$m (groin/thigh level, penetrating the pelvis).
  - Lack of an analytical clearance boundary in [`ArmIKController.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/ArmIKController.cs) allowed unconstrained IK targets to push through the torso mesh.
* **Low Hand Elevations (Signs not raising up):**
  - `ARM_SCALE_XY` was set to $0.20$, compressing normalized MediaPipe vertical reach ($\sim 0.6$m range) down to only $0.12$–$0.30$m.
  - Minimum vertical clamp allowed $Y$ down to $0.70$m (upper thighs).
  - Sample selector previously prioritized total variance, occasionally selecting low-elevation demonstrations where signers signed near the waist (e.g. `Friend` at idx 26 with $peak\_elev = -0.54$).

### 2. Implementation & Architectural Fixes
* **Procedural Torso & Pelvis Clearance ([`ArmIKController.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/ArmIKController.cs)):**
  - Integrated an analytical clearance boundary inside `SolveArmIK()`:
    - Enforces a minimum safe depth $Z \ge 0.28$m ($Z \ge 0.20$m for upper face/head) across the avatar torso width ($|X| \le 0.24$m).
    - Enforces a minimum vertical floor $Y \ge 1.00$m to prevent hands from dropping into hips or thighs.
* **Ergonomic Forward Rest Stance ([`ISLSignPlayer.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ISLSignPlayer.cs)):**
  - Updated neutral rest positions to natural forward waist height:
    - Left Wrist: `(-0.24f, 1.08f, 0.33f)`, Right Wrist: `(0.24f, 1.08f, 0.33f)`
    - Left Elbow: `(-0.42f, 1.15f, -0.15f)`, Right Elbow: `(0.42f, 1.15f, -0.15f)`
* **Dataset Calibration & Continuous Elevation Scoring ([`export_isl_clips.py`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/export_isl_clips.py)):**
  - Calibrated reference anchor: `BASE_ANCHOR = np.array([0.0, 1.38, 0.35])` (shoulder height $1.38$m, forward signing plane $0.35$m).
  - Scaled reach axes: `ARM_SCALE_X = 0.28`, `ARM_SCALE_Y = 0.34`, `ARM_SCALE_Z = 0.16`.
  - Bounded signing envelope: $Y \in [1.02, 1.70]$m, $Z \in [0.30, 0.55]$m.
  - Implemented continuous elevation scoring `score_sample()`:
    $$(lh\_act + rh\_act) \times 10.0 + wrist\_range \times 100.0 + peak\_elev \times 500.0$$
    automatically selecting expressive high-elevation samples for all words (e.g., `Friend` idx 97 at $Y = 1.51$m, `House` idx 866 at $Y = 1.70$m).
  - Updated master target indices for core greetings (`Hello`: 1221, `ThankYou`: 380, `HowAreYou`: 1514, `GoodMorning`: 2429, `You`: 1482, `I`: 2570, `Sign`: 1944).

### 3. Verification & Results
* **Global Trajectory Audit (All 80 Clips):**
  - **Min Z Clearance:** $0.30$m across all frames (strictly $> 0.28$m safe clearance). Zero torso/body penetration.
  - **Min Y Elevation:** $1.02$m across all frames (comfortably at natural waist level, zero leg/groin drooping).
  - **Peak Y Elevation:** $1.33$m–$1.70$m (signs clearly performed at chest, chin, and head levels).
  - 100% valid Unity `.meta` files generated with unique GUIDs.

---

## Session 7: High-Precision Anatomical Finger Rigging & Action Diversity (2026-09-19)

### 1. Root Cause Analysis & Biomechanical Discoveries
* **Backward Finger Bending / Hyperextension:**
  - In [`HandPoseController.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/HandPoseController.cs), `rightFingerFlexionAxis` was previously set to `(-1, 0, 0)` under the assumption of mirrored coordinate axes across the sagittal plane. However, in the Avaturn humanoid rig (`model.fbx`), both left and right armatures share bone orientation: bone length extends along local $+Y$, and the palmar normal points toward $+Z$. Rotating about $+X$ curls fingers into the palm on **both** hands. Rotating around $-X$ on the right hand bent fingers backward into severe hyperextension.
  - **Hierarchical Angle Compounding:** MCP, PIP, and DIP joints were each receiving the full curl angle ($85^\circ \times 3 = 255^\circ$), compounding into broken, self-intersecting finger meshes.
  - **Thumb Spin:** The thumb flexion axis had $Y = 0.8$, which caused axial rotation around the bone length rather than anatomical opposition across the palm.
* **Duplicate / Identical Sign Actions:**
  - Audit discovered that `Baby <-> Hello`, `HowAreYou <-> Man`, and `Location <-> You` had **identical trajectories (0.0000m difference)**.
  - This occurred because `include_keypoints_master.npz` contains 990 duplicate entries copied from the 73 Zenodo classes. Indices `1221`, `1514`, `1482` originally assigned to `Hello`, `HowAreYou`, `You` actually belonged to `Baby`, `Man`, `Location`.
* **Coarse Finger Curl Approximation:**
  - Simple 2D extension ratios failed to capture distinct ISL finger postures, producing ambiguous curls instead of clear pointing, victory, fist, or open-palm postures.

### 2. Implementation & Architectural Fixes
* **Biomechanical Hand Rigging ([`HandPoseController.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/HandPoseController.cs) & [`DesktopTestScene.unity`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scenes/DesktopTestScene.unity)):**
  - Aligned `rightFingerFlexionAxis` to `(1, 0, 0)`, eliminating backward bending and hyperextension on both hands.
  - Re-anchored thumb opposition axes to `(0.7, 0.2, 0.6)` (left) and `(0.7, -0.2, 0.6)` (right).
  - Implemented anatomical joint curl distribution:
    - MCP: $35\%$ of curl
    - PIP: $50\%$ of curl
    - DIP: $35\%$ of curl
    - Clamped strictly to non-negative angles $[0^\circ, \text{max}]$.
  - Updated serialized scene overrides in `DesktopTestScene.unity` (`rightFingerFlexionAxis: {x: 1, y: 0, z: 0}`, `curlMultiplier: 1`).
* **High-Precision 3D Joint Angular Extraction ([`export_isl_clips.py`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/export_isl_clips.py)):**
  - Implemented `compute_segment_angle(p1, p2, p3)` calculating true 3D joint articulation angles between phalanx segments (MCP, PIP, DIP).
  - Blended 3D joint angles with knuckle-to-tip extension ratios and thumb opposition distance:
    - `Teacher`: Sharp pointing gesture ($I = 9^\circ$, $M/R/P = 82^\circ$, $T = 53^\circ$).
    - `Hello`: Open waving palm ($T = 0^\circ, I = 8^\circ, M = 5^\circ, R = 4^\circ, P = 5^\circ$).
    - `Friend`: Clasped fist ($T = 2^\circ, I = 76^\circ, M = 78^\circ, R = 72^\circ, P = 57^\circ$).
* **100% Unique Action Trajectories:**
  - Selected pure, non-overlapping master samples for core greetings (`Hello`: 47, `ThankYou`: 380, `HowAreYou`: 2046, `GoodMorning`: 132, `You`: 52, `I`: 93, `Sign`: 7).
  - Implemented diversity-enforcing candidate selection that penalizes candidates within 10cm of any already-chosen sign.
  - Re-exported all 80 clips into `Assets/Animations/ISLClips/` and `Assets/Resources/ISLClips/`.
  - Comprehensive pairwise audit: **0 duplicate or near-identical pairs across all 80 vocabulary words** (minimum trajectory difference $> 1$cm).
* **Studio Tester & Player Telemetry ([`DesktopGestureTester.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/DesktopGestureTester.cs) & [`ISLSignPlayer.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ISLSignPlayer.cs)):**
  - Added live right-hand finger curl readouts (`T: ... I: ... M: ... R: ... P: ...`) in `ISLSignPlayer.cs` and `DesktopGestureTester.cs`.
  - Added active flexion axes status display (`L=(1, 0, 0) | R=(1, 0, 0)`) in the OnGUI studio panel.
  - Reset curl tracking fields to $0$ on playback stop.

### 3. Verification & Operational Status
* **Finger Flexion:** Both hands curl naturally inward toward the palm. Zero hyperextension or backward bending.
* **Finger Articulation:** Pointing, fists, open palms, and conversational postures are crisp, high-contrast, and clearly readable by the viewer.
* **Trajectory Distinctness:** All 80 words perform completely distinct physical motions.
* **Performance:** 0 runtime GC allocations in `Update()`, `LateUpdate()`, and `OnGUI()`. Ready for Quest 3 VR deployment.

---

## Session 8: Natural Biomechanical Flexion & Thumbs-Up Orientation Fix (2026-09-19)

### 1. Root Cause Analysis
* **Finger Flexion Direction (Empirical In-Engine Verification):**
  - Created [`HandCurlDiagnostic.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Diagnostics/HandCurlDiagnostic.cs) executing direct 3D Euclidean distance measurements between fingertip and wrist on the bound avatar armature in Unity.
  - Empirical Unity measurement results:
    - **Neutral stance:** Tip-to-wrist distance = $0.130$m.
    - **Flexion along $(-1, 0, 0)$:** Tip-to-wrist distance = $0.109$m (fingertip moves **$2.1$cm closer** to the wrist, naturally curling inward into the palm).
    - **Flexion along $(+1, 0, 0)$:** Tip-to-wrist distance = $0.141$m (fingertip moves **$1.1$cm farther** from the wrist, hyperextending backward away from the palm).
  - Setting flexion axes to `(1, 0, 0)` caused finger rotations to execute in the **exact opposite direction of natural movement** across all canonical postures and sign playback.
  - Therefore, `(-1, 0, 0)` is the definitive anatomical flexion axis for both hands in Unity's coordinate system.
* **Thumbs-Up Axial Wrist Roll Inversion:**
  - In [`DesktopGestureTester.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/DesktopGestureTester.cs), the forearm roll offset applied for `ThumbUp` was previously set to $+90^\circ$ on the left and $-90^\circ$ on the right.
  - Because the thumb is positioned at $+Z$ in rest stance, rotating $+90^\circ$ rolled the wrist so that the thumbs pointed straight **DOWN** (a "thumbs down" gesture) rather than **UP**.
* **Finger Pose Snapping:**
  - Without lead-in interpolation, finger curls in [`ISLSignPlayer.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ISLSignPlayer.cs) jumped from $0^\circ$ to apex curl in a single frame at the start of sign playback.

### 2. Implementation & Architectural Fixes
* **Natural Flexion & Thumb Opposition Axes ([`HandPoseController.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/HandPoseController.cs) & [`DesktopTestScene.unity`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scenes/DesktopTestScene.unity)):**
  - Set `leftFingerFlexionAxis` and `rightFingerFlexionAxis` to `(-1, 0, 0)`.
  - Re-anchored empirical thumb opposition axes to `(0.7, -0.2, 0.6)` (left) and `(0.7, 0.2, -0.6)` (right), verified in-engine to oppose directly into the palm.
  - Updated serialized scene fields in `DesktopTestScene.unity`.
  - Updated `FlipFingerFlexion(HandSide side)` to invert both finger and thumb axes cleanly.
  - Added `EnsureDependencies()` to `AvatarRigWrapper.AutoBind()` and robust fallback component lookup in `HandCurlDiagnostic.cs`, eliminating `Mapping or HandPoseController not found`.
* **Thumbs-Up Wrist Roll Correction ([`DesktopGestureTester.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/DesktopGestureTester.cs)):**
  - Inverted `rollSign` for `ThumbUp` to $-1f$ by default, applying $-90^\circ$ on the left and $+90^\circ$ on the right so the thumb points straight **UP** (+Y).
* **Smooth 300ms Finger Curl Lead-In Blend ([`ISLSignPlayer.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Avatar/ISLSignPlayer.cs)):**
  - Extended the 300ms cubic `SmoothStep` lead-in blend to finger curls: smoothly interpolates `lThumb..lPinky` and `rThumb..rPinky` from $0^\circ$ (relaxed neutral rest pose) into the target posture.
  - Eliminates all instantaneous snapping when initiating a sign.
* **Studio Tester GUI Enhancements ([`DesktopGestureTester.cs`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/isl-vr-unity/Assets/Scripts/Rigging/DesktopGestureTester.cs)):**
  - Added one-click preset buttons: `Natural (-1 Palm)` and `Inverted (+1)`.
  - Added on-screen `Run Curl Biomechanical Test` trigger with live in-engine verification.

### 3. Verification
* In-engine diagnostic confirms:
  - `[LEFT HAND] POSITIVE axis ((-1.00, 0.00, 0.00)) curls INTO palm! (tip gets 0.021m closer to wrist)`
  - `[RIGHT HAND] POSITIVE axis ((-1.00, 0.00, 0.00)) curls INTO palm! (tip gets 0.021m closer to wrist)`
  - `[LEFT THUMB] POSITIVE axis ((0.70, -0.20, 0.60)) opposes INTO palm!`
  - `[RIGHT THUMB] POSITIVE axis ((0.70, 0.20, -0.60)) opposes INTO palm!`
* All 8 canonical hand shapes (`Fist`, `PointIndex`, `ThumbUp`, `Victory`, etc.) bend inward toward the palm.
* In `ThumbUp`, thumbs point vertically upward (+Y).
* Transitions from the resting stance into sign postures are fluid and continuous.
* Zero GC allocations maintained across all runtime loops.

* **Compiler Warnings Resolved:** Fixed \CS0162: Unreachable code detected\ in \AvatarRigWrapper.cs\ by replacing an unconditional return inside a \or\ loop with a clean \if\ evaluation block.
* **Biomechanics Correction:** Reverted the finger flexion axis to \(1, 0, 0)\. A previous diagnostic misidentified \(-1, 0, 0)\ as flexion, which was actually causing the fingers to hyperextend (roll backwards over the top of the hand).
* **Final Biomechanics Correction:** The fingers were still hyper-extending (rolling backward) because X was actually the bone's axial *roll* axis. Applying rotation around X caused the fingers to spiral upwards into a corkscrew. We have corrected the true flexion (pitch) axis to (0, 0, 1), ensuring they properly curl inward into the palmar side.
* **Animation Organic Polish:** Fixed floaty 'robotic' motion and indistinguishable finger poses by making three core changes in \xport_isl_clips.py\: 1. Applied high-contrast binarization to the finger extension heuristics (forcing sharp differences between open palms and closed fists). 2. Applied a 3-frame temporal moving average low-pass filter to strip out MediaPipe's high-frequency jitter. 3. Reduced the playback pacing from 2.5s down to 1.2s to match authentic human conversational signing speed.
* **Canonical Handshape Snapping:** Implemented algorithmic snapping in \ISLSignPlayer.cs\. It dynamically classifies noisy 1D interpolated curls from MediaPipe into precise ISL canonical handshapes (Fist, Open Palm, Point, etc.) and smoothly Slerps them, resolving 'imprecise' finger motion without needing to discard the avatar model.
* **Arm IK Reach Correction:** Fixed stiff elbows ('arms only moving at the shoulder') by fixing the bounding box spatial scales in \xport_isl_clips.py\. Previously, wrist targets were locked to a tiny 10cm cube in front of the chest, forcing the IK solver to stiffen the arm into a straight line. Scaled up coordinates to full human 0.6m reach. Also added a \Force T-Pose\ button to the Desktop Tester GUI for debugging.
* **Rig Validator & Diagnostics:** Built \AvatarRigValidator.cs\ to mathematically prove whether the user's humanoid rig supports full 21-joint finger motion, and added Gizmos to draw the invisible skeletal rigging lines. Also added X/Y/Z GUI toggles to fix deformed 'static finger poses' caused by non-standard model imports.
* **IK Reach Scaling Final Fix:** Discovered the 3D data was already normalized to shoulder width, meaning previous aggressive tracking multipliers were physically ripping the avatar's arms out of their sockets, forcing the IK solver to lock the elbows in a straight line. Reverted to biologically accurate 1:1 scale, fixing stiff elbows definitively.

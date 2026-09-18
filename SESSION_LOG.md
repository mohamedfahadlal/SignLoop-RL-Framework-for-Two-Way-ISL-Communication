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

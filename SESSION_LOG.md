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
  - Added Unity-compliant `.meta` text serialization files for all scripts and folders.



# GEMINI.md - SignLoop Project Context & Agent Directives

## 1. Project Overview
**SignLoop** is a two-way, 100% offline **Indian Sign Language (ISL) Virtual Reality Communication System** targeted for the **Meta Quest 3 (Horizon OS)**.

* **Sign-to-Speech (ISL $\rightarrow$ Audio):** Real-time gesture recognition driven by a PyTorch Reinforcement Learning (RL) policy network analyzing 3D hand/body landmarks, translating signs into words, and speaking via local Piper TTS.
* **Speech-to-Sign (Audio $\rightarrow$ ISL):** Offline speech recognition (whisper.cpp) driving procedural 3D avatar animation in Unity 6.3 LTS using **Unity Animation Rigging** (arm IK trajectories, 52 ARKit facial blendshapes for Non-Manual Markers, and 21-joint finger pose interpolation).

---

## 2. Technology Stack & Key Dependencies

| Domain | Stack / Framework | Key Details |
| :--- | :--- | :--- |
| **VR Engine** | Unity 6.3 LTS (C#) | OpenXR, Android Build Support (Horizon OS), Meta XR Core SDK |
| **Rigging & IK** | Unity Animation Rigging (`com.unity.animation.rigging`) | `TwoBoneIKConstraint` (arms), `OverrideTransform` / Quaternion Slerp (canonical ISL finger postures) |
| **3D Assets** | Rigged Humanoid (.fbx / .glb) | 52 ARKit/FACS blendshapes, 21-DOF finger armatures (tracked via Git LFS) |
| **ML / RL Policy** | Python 3.12, PyTorch 2.13, Gymnasium 1.3 | BiGRU + Temporal Attention, REINFORCE fine-tuning |
| **Feature Extraction** | MediaPipe Holistic (0.10.14) | 75 3D landmarks (33 pose + 21 left hand + 21 right hand = 225 values/frame) |
| **Local Audio AI**| whisper.cpp (STT), Piper TTS (TTS) | Quantized, zero-cloud edge inference running entirely offline |
| **Version Control**| Git + Git LFS | Strict LFS tracking for 3D meshes, textures, audio, and ML weights |

---

## 3. Repository Architecture

```text
SignLoop-RL-Framework-for-Two-Way-ISL-Communication/
├── GEMINI.md                     # Agent context & operational directives (this file)
├── SESSION_LOG.md                # Chronological session history and task status
├── README.md                     # Master project README and quickstart guide
├── .gitattributes                # Git LFS tracking rules for 3D, audio, and ML binaries
├── .gitignore                    # Unity, Python venv, and large dataset ignore rules
├── requirements.txt              # Locked Python 3.12 dependencies
│
├── isl_env.py                    # Custom Gymnasium environment (ISLEnv)
├── train_policy.py               # Two-stage training: Supervised Warmup + REINFORCE RL
├── evaluate.py                   # Deterministic evaluation harness
├── data_prep.py                  # Offline MediaPipe landmark extraction & normalization
├── export_isl_clips.py           # Dataset-to-Unity ISL clip converter (extension-ratio finger curls)
├── check_dataset_structure.py    # Local video indexing against Hugging Face metadata
├── check_include.py              # Full 263-class metadata indexer
│
├── models/
│   ├── policy.py                 # ISLPolicyNetwork (BiGRU + Temporal Attention)
│   ├── model.py                  # Alternative network definition
│   ├── isl_policy_model.pth      # Trained PyTorch checkpoint (Git LFS tracked)
│   └── gemini.md                 # Models sub-package documentation
│
├── utils/
│   ├── __init__.py
│   └── dataset.py                # PyTorch Dataset and DataLoader helpers
│
├── dataset/data/                 # Local data directory (ignored by git, except npz outputs)
│   ├── include_keypoints_master.npz # 3295 clips, 30 frames, 225 features, 263 classes
│   ├── include_manifest.csv
│   └── raw_include/              # Raw Zenodo video downloads
│
└── isl-vr-unity/                 # Primary Unity 6.3 LTS VR Project
    ├── Assets/
    │   ├── Animations/
    │   │   └── ISLClips/         # Authentic 30-frame ISL gesture clips (.json)
    │   ├── Resources/
    │   │   └── ISLClips/         # Dynamic runtime clip library (.json)
    │   ├── Models/               # Rigged 3D avatars & mesh components (LFS)
    │   │   ├── AvatarPrototype/  # 54k prototype humanoid with 52 ARKit shapes & 21-joint hands
    │   │   └── README.md         # Rig and armature specifications
    │   ├── Prefabs/              # Avatar rigs & XR rig setups
    │   ├── Scenes/               # Main VR interaction scenes
    │   └── Scripts/              # C# controllers & procedural rigging
    │       ├── Avatar/
    │       │   ├── ISLSignPlayer.cs       # 3-phase sign player (Active -> Hold -> Return)
    │       │   ├── ARKitFaceController.cs # 52 ARKit blendshapes (0 GC allocations)
    │       │   └── ARKitBlendShape.cs     # Canonical blendshape enum
    │       ├── Rigging/
    │       │   ├── DesktopGestureTester.cs# Interactive desktop test harness (Speed, Curls, Orbit)
    │       │   ├── HandPoseController.cs  # 21-joint finger pose interpolation (mirrored axes)
    │       │   ├── ArmIKController.cs     # Analytical TwoBoneIK solver & forearm alignment
    │       │   ├── AvatarRigWrapper.cs    # Prefab wrapper decoupling models from rigs
    │       │   └── AvatarBoneMapping.cs   # Skeleton auto-population & bone binding
    │       ├── Audio/            # whisper.cpp (STT) & Piper TTS hooks
    │       ├── Bridge/           # IPC / socket bridge to Python ML policy
    │       └── VR/               # OpenXR & Quest 3 hand tracking / controller interactions
    ├── Packages/                 # manifest.json & package-lock
    └── ProjectSettings/          # Input, OpenXR, and Editor text serialization
```

---

## 4. ML Subsystem Specifications

* **Observation Space:** 30 temporal frames $\times$ 225 spatial features (`(30, 225)` float32).
  - 33 Pose landmarks + 21 Left Hand + 21 Right Hand = 75 nodes $\times$ 3 coordinates $(x, y, z)$.
  - Centered on nose (landmark 0) and scaled by shoulder distance (landmarks 11–12).
* **Action Space:** `Discrete(263)` corresponding to words in the AI4Bharat INCLUDE dataset.
* **Network Architecture (`ISLPolicyNetwork`):**
  - Input Projection: `Linear(225, 256) -> BatchNorm1d(256) -> ReLU -> Dropout(0.3)`
  - Encoder: 2-layer `BiGRU(input_size=256, hidden_size=256, bidirectional=True)`
  - Attention: `TemporalAttention` over 30 frames (weighted sum over temporal dimension)
  - Action Head: `Linear(512, 128) -> ReLU -> Dropout(0.3) -> Linear(128, 263)`

---

## 5. Procedural Rigging & Biomechanical Directives

* **Sign Playback Lifecycle (3-Phase State Machine):**
  - **Phase 1 (Active Trajectory, 2.5s):** Smooth interpolation across 30 temporal frames with a 300ms lead-in blend from starting rest pose.
  - **Phase 2 (Posture Hold, 0.8s):** Steadily holds the final completed sign posture at the apex so conversational partners can clearly comprehend the sign.
  - **Phase 3 (Lead-Out Blend, 0.5s):** Smooth cubic `SmoothStep` return lerp back to the natural resting stance (`RestLWrist`, `RestRWrist`).
* **Procedural Two-Bone IK & Drift-Free Solvers (`ArmIKController`):**
  - **Pristine Bind-Pose Reset:** Bones (`root`, `mid`, `tip`) MUST be reset to their pristine bind local rotations at the beginning of each frame before solving IK. This strictly eliminates frame-to-frame roll accumulation (Berry phase / geometric holonomy) that causes 180° forearm twisting and inverted hands.
  - **Continuous Law-of-Cosines Elbow Derivation:**
    $$B = A + L_1 \left( \cos\alpha \cdot \vec{u} + \sin\alpha \cdot \vec{v}_{\text{bend}} \right)$$
    $\vec{v}_{\text{bend}}$ is projected onto the plane perpendicular to the arm vector $\vec{u}$ and smoothly blended with the natural human outward/backward bend direction. Never derive the bend normal via an unconstrained `Vector3.Cross(at, hintDir)` that flips sign when crossing planes.
  - **Anatomical Clamping & Forearm Pronation/Supination:**
    - Forearm hinge is strictly aligned with the elbow bend plane (the human humeroulnar joint does not twist).
    - Thumbs-Up and Handshake orientations MUST be driven via axial roll offsets around the forearm axis (`LeftWristRollOffset`, `RightWristRollOffset`), NOT unconstrained world Euler angles.
    - When `MatchWristRotation` is enabled, wrist deviation is strictly clamped using `Quaternion.RotateTowards(naturalWristRot, targetRot, 70f)`. Noisy monocular video landmark tracking can NEVER twist the wrist beyond physiological human limits or invert the hand.
* **Anatomical Clamping & Armature Orientation:**
  - In the humanoid rig (`model.fbx`) imported into Unity:
    - Left Hand finger flexion axis: `(-1, 0, 0)`
    - Right Hand finger flexion axis: `(-1, 0, 0)` (rotation around local $-X$ curls fingers inward into the palm on both hands; positive $+X$ rotation causes dorsal hyperextension / backward bending).
    - Thumb opposition axes: `(0.7, -0.2, 0.6)` (left) and `(0.7, 0.2, -0.6)` (right).
    - Thumbs-Up wrist roll offsets: Left `-90f`, Right `+90f` ensuring thumbs point straight UP (+Y).
  - Joint curl distribution across the phalanx hierarchy: MCP 35%, PIP 50%, DIP 35% of total curl.
  - Finger curls strictly clamped to non-negative angles between $0^\circ$ and $85^\circ$; thumb curls between $0^\circ$ and $65^\circ$.
  - Wrist joint 0 is preserved for `ArmIKController`; `HandPoseController` MUST only manipulate finger joints ($i \ge 1$).
* **Performance Budget (Meta Quest 3 / Horizon OS):**
  - Zero GC allocations in `Update()` and `LateUpdate()`. Pre-allocate all buffers and Quaternions.
  - Avatar geometry: Target $\le 25\text{k}$ triangles for multi-avatar VR scenes; maintain 72+ FPS mobile VR framerate.

---

## 6. Unity & Git LFS Directives

* **Always maintain Git LFS integrity:**
  - 3D models (`.fbx`, `.obj`, `.blend`, `.glb`), textures (`.png`, `.tga`, `.psd`), audio (`.wav`), and weights (`.onnx`, `.pth`) MUST be tracked by Git LFS.
  - Never commit raw binary blobs directly to standard git history.
* **Unity Project Settings:**
  - **Asset Serialization:** Always set to `Force Text` under *Edit > Project Settings > Editor*.
  - **Version Control:** Always set to `Visible Meta Files`.
  - Always commit the matching `.meta` file when adding or moving Unity assets.

---

## 7. Session Log Directive
Whenever finishing a work session or completing major milestones, record the progress, changes, and next steps in [`SESSION_LOG.md`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/SESSION_LOG.md).

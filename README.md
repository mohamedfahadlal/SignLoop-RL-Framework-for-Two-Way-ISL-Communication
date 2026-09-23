# SignLoop: Offline Two-Way Indian Sign Language (ISL) VR Communication System

[![Target Engine](https://img.shields.io/badge/Unity-6.3%20LTS%20(6000.0.33f1)-blue.svg)](https://unity.com/)
[![Target Hardware](https://img.shields.io/badge/Target%20Hardware-Meta%20Quest%203%20(Horizon%20OS)-purple.svg)](https://www.meta.com/quest/quest-3/)
[![Python](https://img.shields.io/badge/Python-3.12-blue.svg)](https://www.python.org/)
[![PyTorch](https://img.shields.io/badge/PyTorch-2.1.2-red.svg)](https://pytorch.org/)
[![Gymnasium](https://img.shields.io/badge/Gymnasium-1.0.0-green.svg)](https://gymnasium.farama.org/)
[![MediaPipe](https://img.shields.io/badge/MediaPipe-0.10.14-orange.svg)](https://mediapipe.dev/)
[![Version Control](https://img.shields.io/badge/Git%20LFS-Enabled-brightgreen.svg)](https://git-lfs.github.com/)

**SignLoop** is a two-way, 100% offline **Indian Sign Language (ISL) Virtual Reality Communication System** targeted for the **Meta Quest 3 (Horizon OS)**. It bridges communication between Deaf / Hard-of-Hearing signers and non-signing hearing individuals in shared virtual environments without requiring cloud connectivity.

---

## 1. System Architecture: The Two-Way Translation Loop

SignLoop operates as a bidirectional, real-time edge translation pipeline:

```text
       ┌─────────────────────────────────────────────────────────────────┐
       │                   SignLoop Two-Way VR Pipeline                  │
       └─────────────────────────────────────────────────────────────────┘

  [DEAF / SIGNING USER]                                  [HEARING USER]
           │                                                    ▲
           │ (Quest 3 Hand Tracking)                            │ (Local Audio)
           ▼                                                    │
  ┌──────────────────┐                                 ┌──────────────────┐
  │ 3D Landmarks     │                                 │ Local Piper TTS  │
  │ (MediaPipe / XR) │                                 │ (Fast Edge TTS)  │
  └────────┬─────────┘                                 └────────▲─────────┘
           │ (30 frames x 225 coords)                           │ (Predicted Word)
           ▼                                                    │
  ┌─────────────────────────────────────────────────────────────┴─────────┐
  │ SIGN-TO-SPEECH ML SUBSYSTEM (models/policy.py)                        │
  │ • Input Projection: Linear(225, 256) + BatchNorm + ReLU               │
  │ • BiGRU Sequence Encoder: 2 layers, 256 hidden units                  │
  │ • Temporal Attention: Weighted temporal pooling over 30 frames        │
  │ • Policy Action Head: Discrete(263) classification over INCLUDE dataset│
  └───────────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────┐
  │ SPEECH-TO-SIGN PROCEDURAL AVATAR SUBSYSTEM (isl-vr-unity)             │
  │ • Offline STT: whisper.cpp quantized edge transcription              │
  │ • Arm IK Trajectories: Unity Animation Rigging TwoBoneIKConstraint    │
  │ • 21-Joint Finger Postures: HandPoseController quaternion slerp      │
  │ • 52 ARKit Facial Blendshapes: ARKitFaceController Non-Manual Markers │
  │ • 3-Phase Playback: Active Trajectory (2.5s) -> Hold (0.8s) -> Return │
  └─────────────────────────────────────────────────────────────┬─────────┘
           │ (Procedural 3D Mesh Animation)                     │ (Spoken Audio)
           ▼                                                    │
  [3D AVATAR IN VR]                                    [MICROPHONE INPUT]
```

---

## 2. Technology Stack

| Domain | Framework / Tool | Key Details |
| :--- | :--- | :--- |
| **VR Engine** | Unity 6.3 LTS (6000.0.33f1) | OpenXR, Android Build Target (Horizon OS), Meta XR Core SDK |
| **Rigging & IK** | Unity Animation Rigging | `TwoBoneIKConstraint` (arms), drift-free Law-of-Cosines solver, per-frame bind reset, axial wrist roll |
| **Finger Animation** | Procedural 21-Joint Slerp | Mirrored knuckle flexion axes, $0^\circ$–$85^\circ$ anatomical joint limits, live curl scaling |
| **Facial Animation**| ARKit / FACS Blendshapes | 52 facial blendshape channels for Non-Manual Markers (NMMs), 0 GC allocations |
| **ML / RL Policy** | PyTorch 2.1.2, Gymnasium 1.0 | BiGRU + Temporal Attention, REINFORCE fine-tuning over 263 sign classes |
| **Keypoint Tracking**| MediaPipe Holistic 0.10.14 | 75 landmarks (33 pose + 21 left hand + 21 right hand = 225 spatial coordinates) |
| **Local Audio AI** | whisper.cpp (STT), Piper TTS | Quantized zero-cloud inference running locally on edge hardware |
| **Version Control** | Git + Git LFS | Strict LFS binary tracking for 3D meshes, textures, audio, and ML weights |

---

## 3. Project Structure

```text
SignLoop-RL-Framework-for-Two-Way-ISL-Communication/
├── GEMINI.md                     # Master agent context & operational directives
├── SESSION_LOG.md                # Chronological session history and task status
├── README.md                     # Master project overview & guide (this file)
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
│   ├── include_keypoints_master.npz # 3,295 clips, 30 frames, 225 features, 263 classes
│   ├── include_manifest.csv
│   └── raw_include/              # Raw Zenodo video downloads
│
└── isl-vr-unity/                 # Primary Unity 6.3 LTS VR Project
    ├── Assets/
    │   ├── Animations/
    │   │   └── ISLClips/         # Authentic 30-frame ISL gesture clips (.json)
    │   ├── Resources/
    │   │   └── ISLClips/         # Dynamic runtime clip library (.json)
    │   ├── Materials/            # Avatar, environment, and UI PBR materials
    │   ├── Models/               # Rigged 3D avatars (52 ARKit blendshapes, 21-DOF hand armatures - LFS tracked)
    │   │   ├── AvatarPrototype/  # 54k prototype humanoid with 52 ARKit shapes & 21-joint hands
    │   │   └── README.md         # Rig and armature specifications
    │   ├── Prefabs/              # Avatar rigs, XR Origin rigs, UI components
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
    │       ├── Audio/            # whisper.cpp (STT) hooks & Piper TTS playback
    │       ├── Bridge/           # IPC / Socket bridge communicating with Python ML policy
    │       └── VR/               # XR input, Quest 3 hand tracking / controller interactions
    ├── Packages/
    │   └── manifest.json         # Package dependencies (Animation Rigging, OpenXR, Input System)
    └── ProjectSettings/          # Serialized settings (Force Text, Visible Meta Files)
```

---

## 4. Current Milestone Status

* [x] **Stage 1: Offline Dataset Indexing & Landmark Normalization**
  - Processed AI4Bharat INCLUDE dataset into normalized 3D landmark tensors.
  - Master dataset: `dataset/data/include_keypoints_master.npz` (3,295 samples, 30 frames, 225 spatial features across 263 classes).
* [x] **Stage 2: Policy Architecture & RL Training Harness**
  - Built `ISLPolicyNetwork` with 2-layer BiGRU, temporal attention, and discrete action head.
  - Implemented Gymnasium environment `ISLEnv`.
  - Checkpoint: `models/isl_policy_model.pth`.
* [x] **Stage 3: Unity 6.3 LTS VR Rigging & Desktop Motion Studio**
  - Implemented modular `AvatarRigWrapper` and `AvatarBoneMapping` allowing arbitrary avatar mesh swapping with zero script breakage.
  - Zero-allocation 52 ARKit facial blendshape manager (`ARKitFaceController`).
  - Drift-free analytical Two-Bone IK arm solver with per-frame bind-pose reset and anatomical forearm pronation/supination (`ArmIKController`).
  - Anatomically clamped 21-joint finger pose controller with mirrored knuckle flexion axes (`HandPoseController`).
  - 3-Phase ISL sign player: Active Trajectory (2.5s) $\to$ Apex Posture Hold (0.8s) $\to$ Smooth Return (0.5s) (`ISLSignPlayer`).
  - Comprehensive desktop motion studio with mouse orbit/zoom, speed slider, reload buttons, live search, semantic categories, and canonical handshape hotkeys (`DesktopGestureTester`).
  - Exported 80 authentic ISL sign clips across Greetings, Transportation, People & Family, Places, Professions, and Core Vocabulary.
* [ ] **Stage 4: Edge Audio AI & Quest 3 VR Deployment**
  - whisper.cpp edge STT integration.
  - Piper TTS edge synthesis integration.
  - Meta Quest 3 standalone build and hand-tracking packaging.

---

## 5. Getting Started

### Prerequisites
* **OS:** Windows 10 / 11
* **Python:** 3.12 (MediaPipe 0.10.14 requires Python 3.11 or 3.12; 3.13 is unsupported)
* **Unity:** 6.3 LTS (`6000.0.33f1` or later) with Android Build Support & OpenXR
* **Git LFS:** Installed and initialized (`git lfs install`)

### Python ML Setup
```bash
# 1. Clone the repository
git clone https://github.com/mohamedfahadlal/SignLoop-RL-Framework-for-Two-Way-ISL-Communication.git
cd SignLoop-RL-Framework-for-Two-Way-ISL-Communication

# 2. Ensure Git LFS is active and pull tracked assets
git lfs install
git lfs pull

# 3. Create and activate a Python 3.12 virtual environment
py -3.12 -m venv isl_venv
.\isl_venv\Scripts\activate

# 4. Install dependencies
pip install -r requirements.txt

# 5. Export authentic ISL motion clips to Unity
python export_isl_clips.py

# 6. (Optional) Run ML policy evaluation
python evaluate.py
```

### Unity 6.3 LTS VR Setup
1. Open **Unity Hub**.
2. Click **Add** > **Add project from disk** and select `isl-vr-unity/`.
3. Ensure the project opens with **Unity 6.3 LTS (6000.x)**.
4. Go to **File > Build Profiles** (or *Build Settings*) and switch platform to **Android**.
5. Go to **Edit > Project Settings > XR Plugin Management**:
   - Enable **OpenXR**.
   - Check the **Meta Quest Support** feature group under the Android tab.
6. Verify under **Edit > Project Settings > Editor**:
   - Asset Serialization: **Force Text**
   - Version Control: **Visible Meta Files**

---

## 6. Desktop Testing in Unity Editor (No VR Headset Required)

You can fully test and inspect avatar animations, arm IK, finger postures, and sign playback directly inside the Unity Editor:

1. Open the project in Unity and press the **Play** button.
2. In the **Game** tab:
   - Verify the top-toolbar **Scale** slider is set to **1x** (do not zoom).
   - Uncheck **Low Resolution Aspect Ratios** for crisp HD rendering.
3. Use the **SignLoop ISL Motion Studio** GUI window:
   - **Authentic Signs:** Click any sign button (`Hello`, `ThankYou`, `House`, `Doctor`, `India`, etc.) to observe the 3-phase trajectory, hold phase, and smooth return.
   - **Pacing & Speed:** Drag the Speed Slider (`0.25x` to `2.0x`) or click `0.5x Slow`, `0.75x`, or `1.0x Norm`.
   - **Finger Curl Scale:** Adjust the **Curl Scale** slider (`0.5x` to `2.5x`) to increase or decrease finger flexion expressiveness.
   - **Static Handshapes (Keys 1–8):** Test canonical ISL handshapes:
     * `1` = Neutral
     * `2` = Open Palm
     * `3` = Fist
     * `4` = Point Index
     * `5` = Thumbs Up (points vertically skyward)
     * `6` = Victory
     * `7` = C-Hand
     * `8` = O-Hand
   - **Thumb Up & Wrist Roll Controls:**
     * Click **"Thumb Up: Standard / Roll Inverted"** to test handshake and thumbs-up postures.
     * Adjust the **Wrist Roll Slider** (`-180°` to `+180°`) with **Reset** button to fine-tune forearm pronation/supination in real time.
   - **Camera Controls:**
     * **Right-Click + Drag:** Orbit camera around avatar.
     * **Scroll Wheel:** Zoom in/out to inspect hand and finger curls closely.
     * **Spacebar:** Toggle procedural arm waving test.

---

## 7. Directives for Contributors & Agents

* **Git LFS Integrity:** Never commit raw binary files (`.fbx`, `.glb`, `.onnx`, `.pth`, `.wav`, `.png`) to standard git tree; ensure `.gitattributes` tracks all binary extensions.
* **Meta Files:** Always commit matching `.meta` files for any newly added or modified Unity assets.
* **Zero Runtime GC:** Keep runtime `Update()` and `LateUpdate()` loops free of heap allocations (no `new Vector3()`, LINQ, or string formatting per frame).
* **Session Documentation:** Whenever completing major milestones or ending a working session, update [`SESSION_LOG.md`](SESSION_LOG.md) and [`GEMINI.md`](GEMINI.md).

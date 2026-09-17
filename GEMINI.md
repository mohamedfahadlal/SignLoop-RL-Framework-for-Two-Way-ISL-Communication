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
├── .gitattributes                # Git LFS tracking rules for 3D, audio, and ML binaries
├── .gitignore                    # Unity, Python venv, and large dataset ignore rules
├── requirements.txt              # Locked Python 3.12 dependencies
│
├── isl_env.py                    # Custom Gymnasium environment (ISLEnv)
├── train_policy.py               # Two-stage training: Supervised Warmup + REINFORCE RL
├── evaluate.py                   # Deterministic evaluation harness
├── data_prep.py                  # Offline MediaPipe landmark extraction & normalization
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
    │   ├── Animations/           # Canonical ISL hand postures & clips
    │   ├── Models/               # Rigged 3D avatars & mesh components (LFS)
    │   ├── Prefabs/              # Avatar rigs & XR rig setups
    │   ├── Scenes/               # Main VR interaction scenes
    │   └── Scripts/              # C# controllers (IK solvers, audio hooks, IPC bridge)
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

## 5. Unity & Git LFS Directives

* **Always maintain Git LFS integrity:**
  - 3D models (`.fbx`, `.obj`, `.blend`, `.glb`), textures (`.png`, `.tga`, `.psd`), audio (`.wav`), and weights (`.onnx`, `.pth`) MUST be tracked by Git LFS.
  - Never commit raw binary blobs directly to standard git history.
* **Unity Project Settings:**
  - **Asset Serialization:** Always set to `Force Text` under *Edit > Project Settings > Editor*.
  - **Version Control:** Always set to `Visible Meta Files`.
  - Always commit the matching `.meta` file when adding or moving Unity assets.

---

## 6. Session Log Directive
Whenever finishing a work session or completing major milestones, record the progress, changes, and next steps in [`SESSION_LOG.md`](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/SESSION_LOG.md).

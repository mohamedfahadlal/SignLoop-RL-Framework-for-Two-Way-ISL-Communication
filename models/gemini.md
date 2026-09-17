# GEMINI.md - Project Context & Agent Directives

## 1. Project Overview
This repository contains a two-way, 100% offline **Indian Sign Language (ISL) Virtual Reality Communication System** targeted for the **Meta Quest 3**.
* **Sign-to-Speech:** Real-time gesture recognition driven by a PyTorch Reinforcement Learning (RL) policy network analyzing hand/face landmarks from Quest 3 sensors, outputting audio via local Piper TTS.
* **Speech-to-Sign:** Offline speech recognition (whisper.cpp) driving procedural 3D avatar animation in Unity 6.3 LTS using **Unity Animation Rigging** (arm trajectories, 52 ARKit facial blendshapes for Non-Manual Markers, and 21-joint finger pose interpolation).

---

## 2. Technology Stack & Key Dependencies

| Domain | Stack / Framework | Key Details |
| :--- | :--- | :--- |
| **VR Engine** | Unity 6.3 LTS (C#) | Built with OpenXR, Android Build Support (Horizon OS), Meta XR Core SDK |
| **Rigging & IK** | Unity Animation Rigging (`com.unity.animation.rigging`) | `TwoBoneIKConstraint` (arms), `OverrideTransform` / Quaternion Slerp (canonical ISL finger postures) |
| **3D Assets** | Avaturn / Rigged Humanoid (.fbx / .glb) | 52 ARKit/FACS blendshapes, 21-DOF finger armatures |
| **ML / RL Policy** | Python 3.12, PyTorch, MediaPipe | Trajectory extraction, gesture recognition policy network |
| **Local Audio AI**| whisper.cpp (STT), Piper TTS (TTS) | Quantized, zero-cloud edge inference running entirely offline |
| **Version Control**| Git + Git LFS | Strict tracking of 3D meshes, binaries, and YAML serialization |

---

## 3. Repository Architecture

```text
.
├── GEMINI.md                     # Agent context & operational instructions
├── .gitattributes                # Git LFS tracking rules for 3D/audio assets
├── .gitignore                    # Comprehensive Unity & Python ignore file
├── rl_policy/                    # PyTorch RL training, evaluation & checkpoints
│   ├── models/                   # Policy architectures & weights (.pt, .onnx)
│   ├── datasets/                 # Landmark data & ISL ground-truth trajectories
│   └── src/                      # Feature extraction & environment definitions
├── scripts/                      # Tooling, batch inference & preprocessing
└── isl-vr-unity/                 # Primary Unity 6.3 LTS VR Project
    ├── Assets/
    │   ├── Animations/           # Canonical ISL hand postures & clips
    │   ├── Models/               # Rigged 3D avatars & mesh components (LFS)
    │   ├── Prefabs/              # Avatar rigs & XR rig setups
    │   ├── Scenes/               # Main VR interaction scenes
    │   └── Scripts/              # C# controllers (IK solvers, audio hooks, IPC bridge)
    ├── Packages/                 # manifest.json & package-lock
    └── ProjectSettings/          # Input, OpenXR, and Editor text serialization
# isl-vr-unity: SignLoop Unity 6.3 LTS VR Project

This directory contains the Unity 6.3 LTS Virtual Reality project for **SignLoop**, targeted for the **Meta Quest 3 (Horizon OS)**.

## Project Structure
```text
isl-vr-unity/
├── Assets/
│   ├── Animations/      # Canonical ISL hand postures, gesture transitions & clips
│   ├── Materials/       # Avatar, environment, and UI PBR materials
│   ├── Models/          # Rigged 3D avatars (52 ARKit blendshapes, 21-DOF hand armatures - LFS tracked)
│   ├── Prefabs/         # Avatar rigs, XR Origin rigs, UI components
│   ├── Scenes/          # VR interaction scenes (MainCommunicationScene, CalibrationScene)
│   └── Scripts/
│       ├── Audio/       # whisper.cpp (STT) hooks & Piper TTS playback
│       ├── Avatar/      # Blendshape controllers (52 ARKit NMMs) & pose managers
│       ├── Bridge/      # IPC / Socket bridge communicating with Python ML policy
│       ├── IK/          # Unity Animation Rigging solvers (TwoBoneIKConstraint, finger pose slerp)
│       └── VR/          # XR input, Quest 3 hand tracking / controller interactions
├── Packages/
│   └── manifest.json    # Package dependencies (Animation Rigging, OpenXR, Input System)
└── ProjectSettings/     # Serialized settings (Force Text, Visible Meta Files)
```

## Setup Instructions
1. Open Unity Hub.
2. Click **Add** > **Add project from disk** and select this directory (`isl-vr-unity`).
3. Ensure Unity Editor version **6000.x (Unity 6 / 6.3 LTS)** is selected.
4. Set Build Target to **Android** (Meta Quest 3).
5. In **Project Settings > XR Plugin Management**, ensure **OpenXR** is enabled with the **Meta Quest Support** feature group active.
6. Verify under **Edit > Project Settings > Editor**:
   - Asset Serialization: **Force Text**
   - Version Control: **Visible Meta Files**

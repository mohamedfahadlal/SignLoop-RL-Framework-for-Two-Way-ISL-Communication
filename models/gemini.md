# models/gemini.md - ML & Policy Subsystem Directives

> [!NOTE]
> For the repository-wide master context and architecture guide, refer to the root [GEMINI.md](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/GEMINI.md).
> For chronological progress and session logs, refer to [SESSION_LOG.md](file:///D:/Github/SignLoop-RL-Framework-for-Two-Way-ISL-Communication/SESSION_LOG.md).

## Subsystem Overview
This directory contains the core PyTorch model definitions, checkpoints, and policy network architectures for the Sign-to-Speech recognition pipeline.

### Files
* `policy.py`: Primary production model architecture `ISLPolicyNetwork` with feature projection, 2-layer BiGRU, `TemporalAttention`, and discrete action distribution generation (`get_action`).
* `model.py`: Standalone BiGRU + Attention classifier.
* `isl_policy_model.pth`: Pretrained & fine-tuned model checkpoint (Git LFS tracked).

### Model Configuration
* **Input:** `(Batch, 30, 225)` — 30 frames of 75 MediaPipe Holistic landmarks (X, Y, Z).
* **Output:** `(Batch, 263)` — Logits over the 263 INCLUDE sign vocabulary classes.
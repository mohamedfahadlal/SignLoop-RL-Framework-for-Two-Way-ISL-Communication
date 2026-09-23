"""
retrain_clean.py
=================
Retrains the ISLPolicyNetwork on the CLEAN 263-class master dataset
(where every class ID maps to exactly ONE unique word), then exports
the final model to ONNX and copies it into the Tauri app.

Run with:
    isl_venv\\Scripts\\python retrain_clean.py
"""
import sys, json, shutil
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parent
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import TensorDataset, DataLoader
from tqdm import tqdm

from models.policy import ISLPolicyNetwork

# ──────────────────────────────────────────────────────────────────
DATA_PATH    = ROOT_DIR / "dataset/data/include_keypoints_master.npz"
SAVE_PATH    = ROOT_DIR / "models/isl_policy_clean.pth"
ONNX_PATH    = ROOT_DIR / "models/isl_policy_clean.onnx"
TAURI_ONNX   = ROOT_DIR / "signloop-app/src-tauri/isl_policy_10k.onnx"

PRETRAIN_EPOCHS = 60    # more epochs on 263 classes
RL_EPISODES     = 0     # skip RL for speed; supervised alone is solid
BATCH_SIZE      = 64
LR              = 1e-3
WEIGHT_DECAY    = 1e-4
# ──────────────────────────────────────────────────────────────────


def verify_dataset(data):
    """Confirm no label collisions before training."""
    lm = json.loads(str(data["label_map"]))
    from collections import defaultdict
    id_to_words = defaultdict(list)
    for word, idx in lm.items():
        id_to_words[idx].append(word)
    collisions = {k: v for k, v in id_to_words.items() if len(v) > 1}
    if collisions:
        print(f"[ERROR] {len(collisions)} class ID collisions detected! Run merge_npz.py first.")
        for k, v in list(collisions.items())[:3]:
            print(f"  ID {k}: {v}")
        sys.exit(1)
    print(f"[OK] Dataset verified: {len(lm)} unique classes, 0 collisions.")


def main():
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Device: {device}")

    # ── Load & verify dataset ──────────────────────────────────────
    print(f"\nLoading {DATA_PATH}...")
    data = np.load(DATA_PATH, allow_pickle=True)
    verify_dataset(data)

    X_raw = data["X"]
    y_raw = data["y"]
    num_classes = int(y_raw.max()) + 1
    print(f"Samples: {len(y_raw)} | Classes: {num_classes}")

    # Flatten (N, 30, 75, 3) -> (N, 30, 225) if needed
    if X_raw.ndim == 4:
        X_raw = X_raw.reshape(X_raw.shape[0], 30, 225)

    # 80/20 split (same indices as ISLEnv so the split is consistent)
    split_idx = int(len(y_raw) * 0.8)
    X_train, y_train = X_raw[:split_idx], y_raw[:split_idx]
    X_val,   y_val   = X_raw[split_idx:], y_raw[split_idx:]

    print(f"Train: {len(y_train)} | Val: {len(y_val)}")
    print(f"Train class coverage: {len(np.unique(y_train))}/{num_classes}")
    print(f"Val   class coverage: {len(np.unique(y_val))}/{num_classes}")

    # ── Dataloader ────────────────────────────────────────────────
    train_ds = TensorDataset(
        torch.tensor(X_train, dtype=torch.float32),
        torch.tensor(y_train, dtype=torch.long)
    )
    val_ds = TensorDataset(
        torch.tensor(X_val,   dtype=torch.float32),
        torch.tensor(y_val,   dtype=torch.long)
    )
    train_loader = DataLoader(train_ds, batch_size=BATCH_SIZE, shuffle=True)
    val_loader   = DataLoader(val_ds,   batch_size=BATCH_SIZE, shuffle=False)

    # ── Build model ───────────────────────────────────────────────
    policy = ISLPolicyNetwork(input_dim=225, num_classes=num_classes).to(device)
    optimizer = optim.AdamW(policy.parameters(), lr=LR, weight_decay=WEIGHT_DECAY)
    scheduler = optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=PRETRAIN_EPOCHS)
    ce_loss = nn.CrossEntropyLoss()

    # ── Training loop ─────────────────────────────────────────────
    best_val_acc = 0.0
    print(f"\n[STAGE 1] Supervised Training ({PRETRAIN_EPOCHS} epochs)...")
    for epoch in range(1, PRETRAIN_EPOCHS + 1):
        # Train
        policy.train()
        t_loss, t_correct, t_total = 0.0, 0, 0
        for bx, by in train_loader:
            bx, by = bx.to(device), by.to(device)
            optimizer.zero_grad()
            logits, _ = policy(bx)
            loss = ce_loss(logits, by)
            loss.backward()
            torch.nn.utils.clip_grad_norm_(policy.parameters(), 1.0)
            optimizer.step()
            t_loss    += loss.item() * bx.size(0)
            t_correct += (logits.argmax(-1) == by).sum().item()
            t_total   += by.size(0)
        scheduler.step()

        # Validate
        policy.eval()
        v_correct, v_total = 0, 0
        with torch.no_grad():
            for bx, by in val_loader:
                bx, by = bx.to(device), by.to(device)
                logits, _ = policy(bx)
                v_correct += (logits.argmax(-1) == by).sum().item()
                v_total   += by.size(0)

        train_acc = (t_correct / t_total) * 100
        val_acc   = (v_correct / v_total) * 100

        print(f"Epoch {epoch:03d}/{PRETRAIN_EPOCHS} | "
              f"Loss: {t_loss/t_total:.4f} | "
              f"Train Acc: {train_acc:.1f}% | "
              f"Val Acc: {val_acc:.1f}%")

        # Save best checkpoint
        if val_acc > best_val_acc:
            best_val_acc = val_acc
            torch.save(policy.state_dict(), SAVE_PATH)
            print(f"  -> New best val acc: {val_acc:.1f}% — saved to {SAVE_PATH.name}")

    print(f"\nBest Val Accuracy: {best_val_acc:.1f}%")

    # ── Load best weights & export ONNX ──────────────────────────
    print("\nLoading best checkpoint and exporting to ONNX...")
    policy.load_state_dict(torch.load(SAVE_PATH, map_location=device))
    policy.eval()

    dummy_input = torch.randn(1, 30, 225, dtype=torch.float32).to(device)
    torch.onnx.export(
        policy,
        dummy_input,
        str(ONNX_PATH),
        export_params=True,
        opset_version=14,
        do_constant_folding=True,
        input_names=["input_state"],
        output_names=["logits", "attention_weights"],
        dynamic_axes={"input_state": {0: "batch_size"}, "logits": {0: "batch_size"}}
    )
    print(f"Exported ONNX model: {ONNX_PATH}")

    # ── Copy ONNX into Tauri app ──────────────────────────────────
    shutil.copy2(ONNX_PATH, TAURI_ONNX)
    print(f"Copied ONNX -> {TAURI_ONNX}")

    print("\n[DONE] Retrain complete. Rebuild Tauri app to use the new model.")


if __name__ == "__main__":
    main()

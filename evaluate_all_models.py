import os
import csv
import numpy as np
import torch

from models.policy import ISLPolicyNetwork
from ai4bharat.adapter import load_and_convert_npz
from ai4bharat.load_models import (
    load_lstm,
    load_transformer_small,
    load_transformer_large,
)


# ============================================================
# CONFIG
# ============================================================

DATASET_PATH = "dataset/data/include_keypoints_master.npz"
MODEL_DIR = "models"

NUM_CLASSES = 263
BATCH_SIZE = 64

SIGNLOOP_MODELS = {
    "SignLoop_10k": "isl_policy_10k.pth",
    "SignLoop_Baseline": "isl_policy_baseline.pth",
    "SignLoop_Model": "isl_policy_model.pth",
    "SignLoop_NoSync": "isl_policy_no_sync.pth",
}


# ============================================================
# DEVICE
# ============================================================

device = torch.device(
    "cuda" if torch.cuda.is_available() else "cpu"
)

print("=" * 70)
print("SIGNLOOP + AI4BHARAT MODEL EVALUATION")
print("=" * 70)

print(f"Device: {device}")
print()


# ============================================================
# LOAD DATASET
# ============================================================

print("Loading dataset...")

data = np.load(
    DATASET_PATH,
    allow_pickle=True
)

X = data["X"]
y = data["y"]

print(f"Original X shape : {X.shape}")
print(f"Labels shape     : {y.shape}")
print(f"Number of classes: {len(np.unique(y))}")
print()


# ============================================================
# LABEL MAP
# ============================================================

label_map = None

if "label_map" in data:
    try:
        label_map_raw = data["label_map"]

        if isinstance(label_map_raw, np.ndarray):
            label_map_raw = label_map_raw.item()

        if isinstance(label_map_raw, dict):
            label_map = label_map_raw
        else:
            import json
            label_map = json.loads(str(label_map_raw))

    except Exception:
        label_map = None


# ============================================================
# SIGNLOOP MODEL EVALUATION
# ============================================================

def evaluate_signloop_model(model_path):

    print("-" * 70)
    print(f"Loading SignLoop model: {model_path}")

    model = ISLPolicyNetwork(
        input_dim=225,
        num_classes=NUM_CLASSES
    )

    checkpoint = torch.load(
        model_path,
        map_location=device,
        weights_only=False
    )

    if isinstance(checkpoint, dict) and "model_state_dict" in checkpoint:
        state_dict = checkpoint["model_state_dict"]

    elif isinstance(checkpoint, dict) and "state_dict" in checkpoint:
        state_dict = checkpoint["state_dict"]

    else:
        state_dict = checkpoint

    model.load_state_dict(
        state_dict,
        strict=True
    )

    model.to(device)
    model.eval()

    predictions = []

    with torch.no_grad():

        for start in range(
            0,
            len(X),
            BATCH_SIZE
        ):

            end = min(
                start + BATCH_SIZE,
                len(X)
            )

            batch = X[start:end]

            # (N, 30, 75, 3)
            # → (N, 30, 225)

            batch = torch.tensor(
                batch,
                dtype=torch.float32,
                device=device
            )

            batch = batch.reshape(
                batch.shape[0],
                batch.shape[1],
                -1
            )

            output = model(batch)

            # ISLPolicyNetwork returns a tuple.
            # The first element is the class logits.
            logits = output[0]

            pred = torch.argmax(
                logits,
                dim=-1
            )

            predictions.extend(
                pred.cpu().numpy()
            )

    predictions = np.asarray(
        predictions
    )

    accuracy = np.mean(
        predictions == y
    )

    print(f"Accuracy: {accuracy * 100:.2f}%")

    return predictions, accuracy


# ============================================================
# AI4BHARAT DATA CONVERSION
# ============================================================

print("=" * 70)
print("Converting dataset for AI4Bharat...")
print("=" * 70)

X_ai4bharat, y_ai, _ = load_and_convert_npz(
    DATASET_PATH
)

print(
    f"AI4Bharat X shape: {X_ai4bharat.shape}"
)

print()


# ============================================================
# AI4BHARAT MODEL EVALUATION
# ============================================================

def evaluate_ai4bharat_model(
    model,
    model_name
):

    print("-" * 70)
    print(f"Evaluating: {model_name}")

    predictions = []

    with torch.no_grad():

        for start in range(
            0,
            len(X_ai4bharat),
            BATCH_SIZE
        ):

            end = min(
                start + BATCH_SIZE,
                len(X_ai4bharat)
            )

            batch = torch.from_numpy(
                X_ai4bharat[start:end]
            ).float().to(device)

            logits = model(batch)

            pred = torch.argmax(
                logits,
                dim=-1
            )

            predictions.extend(
                pred.cpu().numpy()
            )

    predictions = np.asarray(
        predictions
    )

    accuracy = np.mean(
        predictions == y_ai
    )

    print(f"Accuracy: {accuracy * 100:.2f}%")

    return predictions, accuracy


# ============================================================
# RUN ALL MODELS
# ============================================================

all_predictions = {}
results = []


# ------------------------------------------------------------
# SignLoop models
# ------------------------------------------------------------

for model_name, filename in SIGNLOOP_MODELS.items():

    path = os.path.join(
        MODEL_DIR,
        filename
    )

    predictions, accuracy = evaluate_signloop_model(
        path
    )

    all_predictions[model_name] = predictions

    results.append({
        "Model": model_name,
        "Accuracy": accuracy
    })


# ------------------------------------------------------------
# AI4Bharat LSTM
# ------------------------------------------------------------

print()
print("=" * 70)
print("Loading AI4Bharat models")
print("=" * 70)

ai_lstm = load_lstm(device)

predictions, accuracy = evaluate_ai4bharat_model(
    ai_lstm,
    "AI4Bharat_LSTM"
)

all_predictions["AI4Bharat_LSTM"] = predictions

results.append({
    "Model": "AI4Bharat_LSTM",
    "Accuracy": accuracy
})


# ------------------------------------------------------------
# Transformer Small
# ------------------------------------------------------------

ai_transformer_small = load_transformer_small(
    device
)

predictions, accuracy = evaluate_ai4bharat_model(
    ai_transformer_small,
    "AI4Bharat_Transformer_Small"
)

all_predictions[
    "AI4Bharat_Transformer_Small"
] = predictions

results.append({
    "Model": "AI4Bharat_Transformer_Small",
    "Accuracy": accuracy
})


# ------------------------------------------------------------
# Transformer Large
# ------------------------------------------------------------

ai_transformer_large = load_transformer_large(
    device
)

predictions, accuracy = evaluate_ai4bharat_model(
    ai_transformer_large,
    "AI4Bharat_Transformer_Large"
)

all_predictions[
    "AI4Bharat_Transformer_Large"
] = predictions

results.append({
    "Model": "AI4Bharat_Transformer_Large",
    "Accuracy": accuracy
})


# ============================================================
# SUMMARY
# ============================================================

print()
print()
print("=" * 70)
print("FINAL RESULTS")
print("=" * 70)

for result in results:

    print(
        f"{result['Model']:35s}"
        f"{result['Accuracy'] * 100:8.2f}%"
    )

print("=" * 70)


# ============================================================
# SAVE PREDICTIONS
# ============================================================

output_file = "model_predictions.csv"

model_names = list(
    all_predictions.keys()
)

with open(
    output_file,
    "w",
    newline="",
    encoding="utf-8"
) as f:

    writer = csv.writer(f)

    header = [
        "Sample",
        "Actual_Class"
    ] + model_names

    writer.writerow(header)

    for i in range(len(y)):

        row = [
            i,
            int(y[i])
        ]

        for model_name in model_names:

            row.append(
                int(all_predictions[model_name][i])
            )

        writer.writerow(row)


print()
print(
    f"Predictions saved to: {output_file}"
)
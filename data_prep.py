"""
Stage 1 — Data preparation for INCLUDE / INCLUDE50.

Confirmed setup for this project:
- The videos are RAW (.MOV/.MP4 clips), one per sign, downloaded from Zenodo.
- They came unzipped into per-download wrapper folders (e.g. "Adjectives_1of8"),
  so the true Category/Word structure sits one level deeper than expected.
- check_dataset_structure.py already resolved this: it reads the HF metadata,
  matches each video_path to its real file regardless of wrapper folder, and
  writes data/include_manifest.csv with a `resolved_path` + `label` column.

This script:
  1. Reads that manifest (NOT the raw folder tree directly).
  2. Runs MediaPipe Holistic once per video to extract per-frame landmarks.
  3. Normalizes + resamples every clip to a fixed 30-frame window.
  4. Saves one compressed .npz the Gymnasium env loads directly.

Run check_dataset_structure.py first if data/include_manifest.csv doesn't exist yet.
"""

import json
import numpy as np
import pandas as pd
import cv2
import mediapipe as mp
from pathlib import Path
from tqdm import tqdm

MANIFEST_PATH = Path("dataset/data/include_manifest.csv")
OUT_PATH = Path("dataset/data/include_keypoints.npz")

WINDOW_SIZE = 30

mp_holistic = mp.solutions.holistic

# Landmark counts per MediaPipe Holistic output (fixed by the library)
N_POSE = 33
N_HAND = 21   # per hand


def extract_landmarks_from_video(video_path: Path, holistic) -> np.ndarray | None:
    """
    Run MediaPipe Holistic over every frame of one clip.
    Returns array of shape (n_frames, N_POSE + N_HAND*2, 3), or None if the
    video couldn't be read / had no detections at all.
    """
    cap = cv2.VideoCapture(str(video_path))
    frames = []

    while True:
        ok, frame = cap.read()
        if not ok:
            break
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = holistic.process(frame_rgb)

        pose = _landmarks_to_array(results.pose_landmarks, N_POSE)
        left_hand = _landmarks_to_array(results.left_hand_landmarks, N_HAND)
        right_hand = _landmarks_to_array(results.right_hand_landmarks, N_HAND)

        frames.append(np.concatenate([pose, left_hand, right_hand], axis=0))

    cap.release()
    if not frames:
        return None
    return np.stack(frames)  # (n_frames, 75, 3)


def _landmarks_to_array(landmark_list, n_expected: int) -> np.ndarray:
    """MediaPipe returns None when it doesn't detect that part in a frame —
    fill with zeros rather than dropping the frame, so timing stays aligned."""
    if landmark_list is None:
        return np.zeros((n_expected, 3), dtype=np.float32)
    return np.array([[lm.x, lm.y, lm.z] for lm in landmark_list.landmark], dtype=np.float32)


def normalize_sequence(coords: np.ndarray) -> np.ndarray:
    """
    Scale/position invariance: center on the nose (pose landmark 0) and scale
    by shoulder width (pose landmarks 11-12), so it doesn't matter how close
    to the camera or where in frame the signer was.
    coords shape: (n_frames, 75, 3)
    """
    nose = coords[:, 0:1, :]                                    # (n_frames, 1, 3)
    centered = coords - nose
    shoulder_width = np.linalg.norm(
        coords[:, 11, :] - coords[:, 12, :], axis=-1, keepdims=True
    ) + 1e-6
    return centered / shoulder_width[..., None]


def resample_to_window(coords: np.ndarray, window: int = WINDOW_SIZE) -> np.ndarray:
    n_frames = coords.shape[0]
    if n_frames == window:
        return coords
    idx = np.linspace(0, n_frames - 1, window)
    idx_floor = np.floor(idx).astype(int)
    return coords[idx_floor]


def build_dataset():
    manifest = pd.read_csv(MANIFEST_PATH)
    manifest = manifest[manifest["status"] == "found"].reset_index(drop=True)

    words = sorted(manifest["label"].unique())
    label_map = {w: i for i, w in enumerate(words)}

    X, y = [], []

    with mp_holistic.Holistic(static_image_mode=False, model_complexity=1) as holistic:
        for _, row in tqdm(manifest.iterrows(), total=len(manifest), desc="Extracting keypoints"):
            video_path = Path(row["resolved_path"])
            raw_coords = extract_landmarks_from_video(video_path, holistic)
            if raw_coords is None:
                continue

            coords = normalize_sequence(raw_coords)
            coords = resample_to_window(coords)

            X.append(coords)
            y.append(label_map[row["label"]])

    X = np.stack(X)
    y = np.array(y)
    np.savez_compressed(OUT_PATH, X=X, y=y, label_map=json.dumps(label_map))
    print(f"Saved {len(y)} samples, {len(label_map)} vocab words -> {OUT_PATH}")


if __name__ == "__main__":
    build_dataset()

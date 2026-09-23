import numpy as np
import pandas as pd
import json
from pathlib import Path

DATASET_DIR = Path("dataset/data")
FINAL_OUTPUT = DATASET_DIR / "include_keypoints_master.npz"

def smart_merge():
    # 1. First Pass: Collect all unique words across all 4 parts
    print("Collecting unique vocabulary across all parts...")
    all_words = set()
    for i in range(1, 5):
        npz_path = DATASET_DIR / f"include_keypoints_{i}.npz"
        if not npz_path.exists():
            continue
        data = np.load(npz_path, allow_pickle=True)
        part_map = json.loads(str(data["label_map"]))
        all_words.update(part_map.keys())

    unique_words = sorted(list(all_words))
    unified_label_map = {word: idx for idx, word in enumerate(unique_words)}
    print(f"Total Unique Classes across all partitions: {len(unique_words)}")

    all_X = []
    all_y = []
    seen_videos = set()
    total_duplicates_removed = 0

    # 2. Second Pass: Re-index labels and merge arrays
    for i in range(1, 5):
        csv_path = DATASET_DIR / f"include_manifest_{i}.csv"
        npz_path = DATASET_DIR / f"include_keypoints_{i}.npz"

        if not csv_path.exists() or not npz_path.exists():
            print(f"⚠️ Skipping Part {i}: Missing CSV or NPZ file.")
            continue

        print(f"\nProcessing Part {i}...")

        # Load CSV and get found videos
        df = pd.read_csv(csv_path)
        found_videos = df[df["status"] == "found"]["video_path"].tolist()

        # Load NPZ arrays
        data = np.load(npz_path, allow_pickle=True)
        X, local_y = data["X"], data["y"]
        part_map = json.loads(str(data["label_map"]))

        # Invert local mapping: local_id -> word
        part_id_to_word = {int(v): k for k, v in part_map.items()}

        # Re-map each local label to its true unified global ID
        global_y = np.array([unified_label_map[part_id_to_word[int(lid)]] for lid in local_y], dtype=np.int64)

        # Filter duplicates
        keep_indices = []
        for idx, video in enumerate(found_videos):
            if video not in seen_videos:
                seen_videos.add(video)
                keep_indices.append(idx)
            else:
                total_duplicates_removed += 1

        all_X.append(X[keep_indices])
        all_y.append(global_y[keep_indices])

        print(f"Kept {len(keep_indices)} unique videos from Part {i}.")

    # 3. Stack arrays
    print("\nStacking clean arrays...")
    final_X = np.concatenate(all_X, axis=0)
    final_y = np.concatenate(all_y, axis=0)

    # 4. Deterministic shuffle with seed=42 so all 263 classes are distributed across train/test splits
    print("Deterministically shuffling dataset (seed=42)...")
    rng = np.random.RandomState(42)
    perm = rng.permutation(len(final_y))
    final_X = final_X[perm]
    final_y = final_y[perm]

    # 5. Save clean master NPZ
    np.savez_compressed(
        FINAL_OUTPUT,
        X=final_X,
        y=final_y,
        label_map=json.dumps(unified_label_map)
    )

    # 6. Also sync vocab.json directly to signloop-app
    vocab_json_path = Path("signloop-app/src/vocab.json")
    with open(vocab_json_path, "w") as f:
        json.dump(unique_words, f, indent=2)
    print(f"Updated {vocab_json_path} with {len(unique_words)} words.")

    print("\n[SUCCESS] Smart Merge Complete!")
    print(f"Total Duplicates Removed: {total_duplicates_removed}")
    print(f"Final Master Dataset Size: {len(final_y)} unique videos")
    print(f"Total Unique Classes in y: {len(np.unique(final_y))}")

if __name__ == "__main__":
    smart_merge()
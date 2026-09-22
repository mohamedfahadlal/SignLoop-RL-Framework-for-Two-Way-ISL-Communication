import numpy as np
import pandas as pd
import json
from pathlib import Path

DATASET_DIR = Path("dataset/data")
FINAL_OUTPUT = DATASET_DIR / "include_keypoints_master.npz"

def smart_merge():
    all_X = []
    all_y = []
    unified_label_map = {}
    seen_videos = set()
    total_duplicates_removed = 0
    
    # Loop through parts 1 to 4
    for i in range(1, 5):
        csv_path = DATASET_DIR / f"include_manifest_{i}.csv"
        npz_path = DATASET_DIR / f"include_keypoints_{i}.npz"
        
        if not csv_path.exists() or not npz_path.exists():
            print(f"⚠️ Skipping Part {i}: Missing CSV or NPZ file.")
            continue
            
        print(f"\nProcessing Part {i}...")
        
        # 1. Load CSV and get the list of 'found' videos
        df = pd.read_csv(csv_path)
        found_videos = df[df["status"] == "found"]["video_path"].tolist()
        
        # 2. Load NPZ mathematical arrays
        data = np.load(npz_path, allow_pickle=True)
        X, y = data["X"], data["y"]
        
        # 3. Merge vocabulary mapping
        part_map = json.loads(str(data["label_map"]))
        unified_label_map.update(part_map)
        
        # 4. Filter out duplicates using the CSV as a guide
        keep_indices = []
        for idx, video in enumerate(found_videos):
            if video not in seen_videos:
                seen_videos.add(video)
                keep_indices.append(idx)
            else:
                total_duplicates_removed += 1
                
        # 5. Append ONLY the unique arrays
        all_X.append(X[keep_indices])
        all_y.append(y[keep_indices])
        
        print(f"Kept {len(keep_indices)} unique videos from Part {i}.")

    # Stack and compress the final master arrays
    print("\nStacking clean arrays (this might take a few seconds)...")
    final_X = np.concatenate(all_X, axis=0)
    final_y = np.concatenate(all_y, axis=0)

    np.savez_compressed(
        FINAL_OUTPUT, 
        X=final_X, 
        y=final_y, 
        label_map=json.dumps(unified_label_map)
    )
    
    print("\n✅ Smart Merge Complete!")
    print(f"Total Duplicates Removed: {total_duplicates_removed}")
    print(f"Final Master Dataset Size: {len(final_y)} unique videos")

if __name__ == "__main__":
    smart_merge()
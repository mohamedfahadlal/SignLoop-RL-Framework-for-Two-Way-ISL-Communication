"""
Run this AFTER unzipping your Zenodo videos into data/raw_include/.
This script indexes the full INCLUDE dataset (263 words) and checks
whether each metadata entry resolves to a local video file.
"""

import re
from pathlib import Path

import pandas as pd

from pathlib import Path
import pandas as pd

RAW_DATA_DIR = Path("dataset/data/raw_include")  # adjust if your unzip landed elsewhere
MANIFEST_OUT = Path("dataset/data/include_manifest.csv")

# Ensure destination folder exists
MANIFEST_OUT.parent.mkdir(parents=True, exist_ok=True)


def load_metadata() -> pd.DataFrame:
    from datasets import load_dataset
    
    print("Downloading metadata from Hugging Face...")
    dataset = load_dataset("ai4bharat/INCLUDE", split="train")
    df = dataset.to_pandas()
    
    # Full INCLUDE dataset: keep all 263 words; downstream code can filter if needed.
    return df

    return df


def normalize_string(s: str) -> str:
    """Removes all spaces, punctuation, and makes lowercase (e.g. '1. loud' -> '1loud')"""
    return re.sub(r'[^a-z0-9]', '', s.lower())


def index_local_videos(root: Path) -> dict:
    """
    Map (word_folder, filename) -> actual resolved Path.
    Handles videos nested under 'Extra' folders, while skipping
    macOS metadata clutter and duplicate filename collisions.
    """

    index = {}
    for ext in ("*.MOV", "*.mov", "*.MP4", "*.mp4"):
        for p in root.rglob(ext):
    index = {}
    dupes = 0
    for ext in ("*.MOV", "*.mov", "*.MP4", "*.mp4"):
        for p in root.rglob(ext):
            if "__MACOSX" in p.parts or p.name.startswith("._"):
                continue

            # If a clip is nested under an 'Extra' folder, use the grandparent label;
            # otherwise use the immediate parent folder name.
            parent_name = p.parent.parent.name if p.parent.name == "Extra" else p.parent.name
            key = (parent_name, p.name)

            if key in index:
                dupes += 1
            index[key] = p

    if dupes:
        print(f"Warning: {dupes} duplicate filename collisions found.")

    print(f"Indexed {len(index)} local video files.")
    return index

    return index

def check_files_exist(df: pd.DataFrame, index: dict) -> pd.DataFrame:
    resolved_paths, statuses = [], []

    for _, row in df.iterrows():
        vp = row["video_path"]
        parts = Path(vp).parts
        file_stem = Path(vp).stem

        word_label = parts[-2]
        parent_label = row["parent_label"]

        norm_label = normalize_string(word_label)
        norm_parent = normalize_string(parent_label)

        # Prefer the exact (word folder, filename) lookup first.
        key = (word_label, parts[-1])
        match = index.get(key)

        if not match:
            candidates = []
            for (folder, filename), path in index.items():
                if filename == parts[-1] or Path(filename).stem == file_stem:
                    candidates.append(path)

            if len(candidates) == 1:
                match = candidates[0]
            elif len(candidates) > 1:
                for c in candidates:
                    norm_parts = [normalize_string(p) for p in c.parts]
                    if norm_label in norm_parts or any(norm_parent in p for p in norm_parts):
                        match = c
                        break

                if not match:
                    match = candidates[0]

        resolved_paths.append(str(match) if match else None)
        statuses.append("found" if match else "missing")

        resolved_paths.append(str(match) if match else None)
        statuses.append("found" if match else "missing")

    df = df.copy()
    df["resolved_path"] = resolved_paths
    df["status"] = statuses

    n_found = (df["status"] == "found").sum()
    n_missing = (df["status"] == "missing").sum()
    print(f"\nTotal rows checked: {len(df)}")
    print(f"Found on disk:      {n_found}")
    print(f"Missing:            {n_missing}")

    if n_missing:
        print("\nFirst few missing (word, filename) pairs:")
        missing_df = df[df["status"] == "missing"]
        for vp in missing_df["video_path"].head(5):
            print(" ", vp)

    return df

if __name__ == "__main__":
    df = load_metadata()
    print(f"\nUnique words in scope: {df['label'].nunique()}")

    index = index_local_videos(RAW_DATA_DIR)

    result = check_files_exist(df, index)

    # Save the manifest for data_prep.py to use
    result.to_csv(MANIFEST_OUT, index=False)
    print(f"\nManifest written to {MANIFEST_OUT} — data_prep.py reads resolved_path from here.")


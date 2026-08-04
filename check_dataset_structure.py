"""
Run this AFTER unzipping your Zenodo videos into data/raw_include/.
This version targets the FULL INCLUDE dataset (all 263 words),
ignoring the INCLUDE-50 subset entirely.
"""

import pandas as pd
import re
from pathlib import Path

RAW_DATA_DIR = Path("dataset/data/raw_include")
MANIFEST_OUT = Path("dataset/data/include_manifest.csv")


def load_metadata() -> pd.DataFrame:
    from datasets import load_dataset
    
    print("Downloading metadata from Hugging Face...")
    dataset = load_dataset("ai4bharat/INCLUDE", split="train")
    df = dataset.to_pandas()
    
    # No filtering at all: we want the entire INCLUDE dataset
    return df


def normalize_string(s: str) -> str:
    """Removes all spaces, punctuation, and makes lowercase (e.g. '1. loud' -> '1loud')"""
    return re.sub(r'[^a-z0-9]', '', s.lower())


def index_local_videos(root: Path) -> dict:
    """Maps the filename stem -> list of matching Path objects."""
    index = {}
    for ext in ("*.MOV", "*.mov", "*.MP4", "*.mp4"):
        for p in root.rglob(ext):
            if "__MACOSX" in p.parts or p.name.startswith("._"):
                continue
            index.setdefault(p.stem, []).append(p)
            
    total_files = sum(len(paths) for paths in index.values())
    duplicates = sum(1 for paths in index.values() if len(paths) > 1)
    
    print(f"Indexed {total_files} local video files.")
    if duplicates:
        print(f"Note: Found {duplicates} filenames that appear multiple times. Disambiguation enabled.")
        
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

        candidates = index.get(file_stem, [])
        match = None

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

    df = df.copy()
    df["resolved_path"] = resolved_paths
    df["status"] = statuses

    n_found = (df["status"] == "found").sum()
    n_missing = (df["status"] == "missing").sum()
    
    print(f"\nTotal rows checked: {len(df)}")
    print(f"Found on disk:      {n_found}")
    print(f"Missing:            {n_missing}")

    return df


if __name__ == "__main__":
    df = load_metadata()
    print(f"\nUnique words in scope: {df['label'].nunique()}")

    index = index_local_videos(RAW_DATA_DIR)
    
    result = check_files_exist(df, index)
    
    # Save the manifest for data_prep.py to use
    result.to_csv(MANIFEST_OUT, index=False)
    print(f"\nManifest written to {MANIFEST_OUT} — data_prep.py reads resolved_path from here.")
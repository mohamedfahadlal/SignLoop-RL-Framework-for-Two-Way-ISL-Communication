"""
Run this AFTER unzipping the Zenodo videos into data/raw_include/.

It does two things:
1. Pulls the authoritative metadata (parent_label, label, video_path, include_50)
   from the Hugging Face parquet — this is your ground-truth label source,
   more reliable than trusting folder names.
2. Checks how many of those video_paths actually exist on disk, and reports
   any path-prefix mismatch (the most common issue after unzipping Zenodo files).
"""

import pandas as pd
from pathlib import Path

RAW_DATA_DIR = Path("dataset/data/raw_include")   # adjust if your unzip landed elsewhere
INCLUDE_50_ONLY = True                     # set False if you're using the full 263-word set
MANIFEST_OUT = Path("dataset/data/include_manifest.csv")

'''
HF_PARQUET_URL = (
    "hf://datasets/ai4bharat/INCLUDE/default/train-00000-of-00001.parquet"
)'''


def load_metadata() -> pd.DataFrame:
    from datasets import load_dataset
    
    # This officially connects to Hugging Face and downloads the metadata safely
    print("Downloading metadata from Hugging Face...")
    dataset = load_dataset("ai4bharat/INCLUDE", split="train")
    df = dataset.to_pandas()
    
    if INCLUDE_50_ONLY:
        df = df[df["include_50"] == True].reset_index(drop=True)
    return df

def index_local_videos(root: Path) -> dict:
    """
    Map (word_folder, filename) -> actual resolved Path, regardless of which
    zip-wrapper folder it landed in (e.g. 'Adjectives_1of8'). This makes the
    match robust to the extra nesting confirmed in this project's download.
    """
    index = {}
    dupes = 0
    for ext in ("*.MOV", "*.mov", "*.MP4", "*.mp4"):
        for p in root.rglob(ext):
            key = (p.parent.name, p.name)   # e.g. ("1. loud", "MVI_5177.MOV")
            if key in index:
                dupes += 1
            index[key] = p
    if dupes:
        print(f"Warning: {dupes} filename collisions across different wrapper folders.")
    return index


def check_files_exist(df: pd.DataFrame, index: dict) -> pd.DataFrame:
    resolved_paths, statuses = [], []

    for vp in df["video_path"]:
        # vp looks like "Adjectives/1. loud/MVI_5177.MOV" -> take last 2 parts
        parts = Path(vp).parts
        key = (parts[-2], parts[-1])
        match = index.get(key)
        resolved_paths.append(str(match) if match else None)
        statuses.append("found" if match else "missing")

    df = df.copy()
    df["resolved_path"] = resolved_paths
    df["status"] = statuses

    n_found = (df["status"] == "found").sum()
    n_missing = (df["status"] == "missing").sum()
    print(f"Total rows checked: {len(df)}")
    print(f"Found on disk:      {n_found}")
    print(f"Missing:            {n_missing}")

    if n_missing:
        print("\nFirst few missing (word, filename) pairs:")
        missing_df = df[df["status"] == "missing"]
        for vp in missing_df["video_path"].head(5):
            print(" ", vp)
        print("\nIf this is a large fraction, double check the zip extracted the")
        print("category you expect, or that MOV vs MP4 extensions aren't mixed up.")

    return df


if __name__ == "__main__":
    df = load_metadata()
    print(df.head())
    print("\nUnique words in scope:", df["label"].nunique())

    index = index_local_videos(RAW_DATA_DIR)
    print(f"Indexed {len(index)} local video files across all wrapper folders.")

    result = check_files_exist(df, index)
    result.to_csv(MANIFEST_OUT, index=False)
    print(f"\nManifest written to {MANIFEST_OUT} — data_prep.py reads resolved_path from here.")

<<<<<<< HEAD
"""
Run this AFTER unzipping your Zenodo videos into data/raw_include/.
This version targets the FULL INCLUDE dataset (all 263 words),
ignoring the INCLUDE-50 subset entirely.
"""

import pandas as pd
import re
=======
>>>>>>> 313753d9e03645d94091dfc5710987c80c4264dd
from pathlib import Path
import pandas as pd

<<<<<<< HEAD

RAW_DATA_DIR = Path("dataset/data/raw_include")   # adjust if your unzip landed elsewhere
INCLUDE_50_ONLY = False                 # set False if you're using the full 263-word set
MANIFEST_OUT = Path("dataset/data/include_manifest.csv")
=======
# 1. Update Path to match your actual directory layout
RAW_DATA_DIR = Path("datasets/data/raw_include") 
MANIFEST_OUT = Path("dataset/data/include_manifest.csv")

# Ensure destination folder exists
MANIFEST_OUT.parent.mkdir(parents=True, exist_ok=True)
>>>>>>> 313753d9e03645d94091dfc5710987c80c4264dd

TARGET_CATEGORIES = ["Jobs", "Means of Transportation", "People", "Places"]

def load_metadata() -> pd.DataFrame:
    from datasets import load_dataset
    
    print("Downloading metadata from Hugging Face...")
    dataset = load_dataset("ai4bharat/INCLUDE", split="train")
    df = dataset.to_pandas()
    
<<<<<<< HEAD
    # No filtering at all: we want the entire INCLUDE dataset
=======
    # Clean category names for consistent matching
    df["parent_label_clean"] = df["parent_label"].str.replace("_", " ")
    target_clean = [cat.replace("_", " ") for cat in TARGET_CATEGORIES]
    
    # Filter metadata strictly to your 4 downloaded categories
    df = df[df["parent_label_clean"].isin(target_clean)].reset_index(drop=True)
>>>>>>> 313753d9e03645d94091dfc5710987c80c4264dd
    return df


def normalize_string(s: str) -> str:
    """Removes all spaces, punctuation, and makes lowercase (e.g. '1. loud' -> '1loud')"""
    return re.sub(r'[^a-z0-9]', '', s.lower())


def index_local_videos(root: Path) -> dict:
<<<<<<< HEAD
    """Maps the filename stem -> list of matching Path objects."""
=======
    """
    Map (word_folder, filename) -> actual resolved Path.
    Handles videos nested inside 'Extra' subfolders.
    """
>>>>>>> 313753d9e03645d94091dfc5710987c80c4264dd
    index = {}
    for ext in ("*.MOV", "*.mov", "*.MP4", "*.mp4"):
        for p in root.rglob(ext):
<<<<<<< HEAD
            if "__MACOSX" in p.parts or p.name.startswith("._"):
                continue
            index.setdefault(p.stem, []).append(p)
            
    total_files = sum(len(paths) for paths in index.values())
    duplicates = sum(1 for paths in index.values() if len(paths) > 1)
    
    print(f"Indexed {total_files} local video files.")
    if duplicates:
        print(f"Note: Found {duplicates} filenames that appear multiple times. Disambiguation enabled.")
        
=======
            # If inside an 'Extra' folder, take the grandparent folder name (e.g. '19. House')
            parent_name = p.parent.parent.name if p.parent.name == "Extra" else p.parent.name
            key = (parent_name, p.name)
            
            if key in index:
                dupes += 1
            index[key] = p
            
    if dupes:
        print(f"Warning: {dupes} duplicate filename collisions found.")
>>>>>>> 313753d9e03645d94091dfc5710987c80c4264dd
    return index

def check_files_exist(df: pd.DataFrame, index: dict) -> pd.DataFrame:
    resolved_paths, statuses = [], []

<<<<<<< HEAD
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

=======
    for vp in df["video_path"]:
        parts = Path(vp).parts
        # parts[-2] is the word folder (e.g., '1. loud'), parts[-1] is the video filename
        key = (parts[-2], parts[-1])
        match = index.get(key)
        
>>>>>>> 313753d9e03645d94091dfc5710987c80c4264dd
        resolved_paths.append(str(match) if match else None)
        statuses.append("found" if match else "missing")

    df = df.copy()
    df["resolved_path"] = resolved_paths
    df["status"] = statuses

    n_found = (df["status"] == "found").sum()
    n_missing = (df["status"] == "missing").sum()
<<<<<<< HEAD
    
    print(f"\nTotal rows checked: {len(df)}")
    print(f"Found on disk:      {n_found}")
    print(f"Missing:            {n_missing}")

=======
    print(f"Total rows checked in scope: {len(df)}")
    print(f"Found on disk:               {n_found}")
    print(f"Missing:                     {n_missing}")

    if n_missing:
        print("\nFirst few missing (word, filename) pairs:")
        missing_df = df[df["status"] == "missing"]
        for vp in missing_df["video_path"].head(5):
            print(" ", vp)

>>>>>>> 313753d9e03645d94091dfc5710987c80c4264dd
    return df

if __name__ == "__main__":
    df = load_metadata()
<<<<<<< HEAD
    print(f"\nUnique words in scope: {df['label'].nunique()}")

    index = index_local_videos(RAW_DATA_DIR)
    
    result = check_files_exist(df, index)
    
    # Save the manifest for data_prep.py to use
    result.to_csv(MANIFEST_OUT, index=False)
    print(f"\nManifest written to {MANIFEST_OUT} — data_prep.py reads resolved_path from here.")
=======
    print(f"Unique target words in scope: {df['label'].nunique()}")

    index = index_local_videos(RAW_DATA_DIR)
    print(f"Indexed {len(index)} local video files across target folders.")

    result = check_files_exist(df, index)
    
    # Save output (Make sure include_manifest.csv is closed in Excel/VS Code)
    result.to_csv(MANIFEST_OUT, index=False)
    print(f"\nManifest successfully written to {MANIFEST_OUT}")
>>>>>>> 313753d9e03645d94091dfc5710987c80c4264dd

from pathlib import Path
import pandas as pd

# 1. Base directory paths
RAW_DATA_DIR = Path("datasets/data/raw_include") 
MANIFEST_OUT = Path("dataset/data/include_manifest_full.csv")

# Ensure target folder exists
MANIFEST_OUT.parent.mkdir(parents=True, exist_ok=True)

def load_full_metadata() -> pd.DataFrame:
    from datasets import load_dataset
    
    print("Downloading FULL metadata table from Hugging Face (263 classes)...")
    dataset = load_dataset("ai4bharat/INCLUDE", split="train")
    df = dataset.to_pandas()
    
    # Standardize string representations
    df["parent_label_clean"] = df["parent_label"].str.replace("_", " ")
    
    print(f"Total entries in official metadata: {len(df)}")
    print(f"Total unique classes (words): {df['label'].nunique()}")
    print(f"Total parent categories: {df['parent_label'].nunique()}")
    
    return df

def index_local_videos(root: Path) -> dict:
    """
    Map (word_folder, filename) -> actual resolved Path on disk.
    Handles nested subfolders such as 'Extra' wrapper folders automatically.
    """
    index = {}
    dupes = 0
    
    # Scan recursively for all video extensions across all 15 category folders
    for ext in ("*.MOV", "*.mov", "*.MP4", "*.mp4"):
        for p in root.rglob(ext):
            # If inside an 'Extra' wrapper folder, take the grandparent folder name (e.g. '19. House')
            parent_name = p.parent.parent.name if p.parent.name == "Extra" else p.parent.name
            key = (parent_name, p.name)
            
            if key in index:
                dupes += 1
            index[key] = p
            
    if dupes:
        print(f"Note: {dupes} duplicate filename collisions handled during index mapping.")
    return index

def check_files_exist(df: pd.DataFrame, index: dict) -> pd.DataFrame:
    resolved_paths, statuses = [], []

    for vp in df["video_path"]:
        parts = Path(vp).parts
        # parts[-2] is the word folder (e.g., '99. Job'), parts[-1] is the video filename
        key = (parts[-2], parts[-1])
        match = index.get(key)
        
        resolved_paths.append(str(match) if match else None)
        statuses.append("found" if match else "missing")

    df = df.copy()
    df["resolved_path"] = resolved_paths
    df["status"] = statuses

    n_found = (df["status"] == "found").sum()
    n_missing = (df["status"] == "missing").sum()
    
    print("\n--- Manifest Indexing Results ---")
    print(f"Total metadata records checked: {len(df)}")
    print(f"Found on disk:                  {n_found}")
    print(f"Missing from disk:               {n_missing}")

    if n_missing > 0:
        print(f"\nNote: Missing {n_missing} files. (Expected if running on local dataset slices).")
        print("Sample missing video paths:")
        missing_df = df[df["status"] == "missing"]
        for vp in missing_df["video_path"].head(5):
            print("  -", vp)

    return df

if __name__ == "__main__":
    df = load_full_metadata()

    print(f"\nIndexing local video files under {RAW_DATA_DIR}...")
    index = index_local_videos(RAW_DATA_DIR)
    print(f"Indexed {len(index)} total video clips on disk.")

    result = check_files_exist(df, index)
    
    # Save complete 263-class manifest CSV
    result.to_csv(MANIFEST_OUT, index=False)
    print(f"\nFull manifest successfully generated at: {MANIFEST_OUT}")
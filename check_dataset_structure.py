from pathlib import Path
import pandas as pd

# 1. Update Path to match your actual directory layout
RAW_DATA_DIR = Path("datasets/data/raw_include") 
MANIFEST_OUT = Path("dataset/data/include_manifest.csv")

# Ensure destination folder exists
MANIFEST_OUT.parent.mkdir(parents=True, exist_ok=True)

TARGET_CATEGORIES = ["Jobs", "Means of Transportation", "People", "Places"]

def load_metadata() -> pd.DataFrame:
    from datasets import load_dataset
    
    print("Downloading metadata from Hugging Face...")
    dataset = load_dataset("ai4bharat/INCLUDE", split="train")
    df = dataset.to_pandas()
    
    # Clean category names for consistent matching
    df["parent_label_clean"] = df["parent_label"].str.replace("_", " ")
    target_clean = [cat.replace("_", " ") for cat in TARGET_CATEGORIES]
    
    # Filter metadata strictly to your 4 downloaded categories
    df = df[df["parent_label_clean"].isin(target_clean)].reset_index(drop=True)
    return df

def index_local_videos(root: Path) -> dict:
    """
    Map (word_folder, filename) -> actual resolved Path.
    Handles videos nested inside 'Extra' subfolders.
    """
    index = {}
    dupes = 0
    for ext in ("*.MOV", "*.mov", "*.MP4", "*.mp4"):
        for p in root.rglob(ext):
            # If inside an 'Extra' folder, take the grandparent folder name (e.g. '19. House')
            parent_name = p.parent.parent.name if p.parent.name == "Extra" else p.parent.name
            key = (parent_name, p.name)
            
            if key in index:
                dupes += 1
            index[key] = p
            
    if dupes:
        print(f"Warning: {dupes} duplicate filename collisions found.")
    return index

def check_files_exist(df: pd.DataFrame, index: dict) -> pd.DataFrame:
    resolved_paths, statuses = [], []

    for vp in df["video_path"]:
        parts = Path(vp).parts
        # parts[-2] is the word folder (e.g., '1. loud'), parts[-1] is the video filename
        key = (parts[-2], parts[-1])
        match = index.get(key)
        
        resolved_paths.append(str(match) if match else None)
        statuses.append("found" if match else "missing")

    df = df.copy()
    df["resolved_path"] = resolved_paths
    df["status"] = statuses

    n_found = (df["status"] == "found").sum()
    n_missing = (df["status"] == "missing").sum()
    print(f"Total rows checked in scope: {len(df)}")
    print(f"Found on disk:               {n_found}")
    print(f"Missing:                     {n_missing}")

    if n_missing:
        print("\nFirst few missing (word, filename) pairs:")
        missing_df = df[df["status"] == "missing"]
        for vp in missing_df["video_path"].head(5):
            print(" ", vp)

    return df

if __name__ == "__main__":
    df = load_metadata()
    print(f"Unique target words in scope: {df['label'].nunique()}")

    index = index_local_videos(RAW_DATA_DIR)
    print(f"Indexed {len(index)} local video files across target folders.")

    result = check_files_exist(df, index)
    
    # Save output (Make sure include_manifest.csv is closed in Excel/VS Code)
    result.to_csv(MANIFEST_OUT, index=False)
    print(f"\nManifest successfully written to {MANIFEST_OUT}")
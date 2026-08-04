import pandas as pd
from pathlib import Path

def audit_team_data():
    dataset_dir = Path("dataset/data")
    manifest_files = list(dataset_dir.glob("*manifest*.csv"))
    
    if not manifest_files:
        print("No manifest files found!")
        return

    # Combine all 4 CSVs into one massive dataframe
    dfs = [pd.read_csv(f) for f in manifest_files]
    master_df = pd.concat(dfs, ignore_index=True)

    # Filter to only count videos that were physically found on your drives
    found_df = master_df[master_df["status"] == "found"]
    
    print(f"--- TEAM DATA AUDIT ---")
    print(f"Total manifests checked: {len(manifest_files)}")
    print(f"Total videos successfully preprocessed: {len(found_df)}")
    
    # Check if teammates processed overlapping data
    duplicates = found_df.duplicated(subset=['video_path']).sum()
    if duplicates > 0:
        print(f"⚠️ WARNING: Found {duplicates} duplicate videos!")
        print("It looks like teammates processed some of the same zip files.")
    else:
        print("✅ SUCCESS: No duplicate videos found. Perfect data split!")

if __name__ == "__main__":
    audit_team_data()
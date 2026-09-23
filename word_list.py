import numpy as np
import json

# 1. Load your master npz file
data = np.load('dataset/data/include_keypoints_master.npz', allow_pickle=True)
label_map_raw = data['label_map']

# Extract item if it's a numpy scalar
if hasattr(label_map_raw, 'item'):
    label_map = label_map_raw.item()
else:
    label_map = label_map_raw

# If it's stored as a string, parse it safely into a dictionary
if isinstance(label_map, str):
    try:
        label_map = json.loads(label_map)
    except Exception:
        label_map = ast.literal_eval(label_map)

# 2. Invert and map the dictionary to an ordered array
max_index = max(label_map.values())
vocab_list = ["" for _ in range(max_index + 1)]

for word, index in label_map.items():
    vocab_list[index] = word

# 3. Save directly to your Tauri React src folder
with open("signloop-app/src/vocab.json", "w") as f:
    json.dump(vocab_list, f, indent=2)

print(f"Successfully generated vocab.json with {len(vocab_list)} classes!")
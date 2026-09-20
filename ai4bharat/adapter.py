import json
import numpy as np
import torch


# Your dataset:
# 33 pose landmarks
# 21 left-hand landmarks
# 21 right-hand landmarks
#
# AI4Bharat:
# 25 pose landmarks
# 21 left-hand landmarks
# 21 right-hand landmarks
#
# Therefore:
# 25*2 + 21*2 + 21*2 = 134


def convert_to_ai4bharat(X):
    """
    Convert SignLoop keypoints:

        (N, 30, 75, 3)

    into AI4Bharat format:

        (N, 169, 134)

    Only X and Y coordinates are used.
    """

    X = np.asarray(X, dtype=np.float32)

    if X.ndim != 4:
        raise ValueError(
            f"Expected 4D input (N, frames, landmarks, 3), "
            f"got {X.shape}"
        )

    n_samples, n_frames, n_landmarks, n_coords = X.shape

    if n_landmarks != 75:
        raise ValueError(
            f"Expected 75 landmarks, got {n_landmarks}"
        )

    if n_coords != 3:
        raise ValueError(
            f"Expected 3 coordinates (x,y,z), got {n_coords}"
        )

    # ---------------------------------------------------------
    # Landmark layout in your dataset
    #
    # 0:33   -> pose
    # 33:54  -> left hand
    # 54:75  -> right hand
    # ---------------------------------------------------------

    pose = X[:, :, :33, :2]
    left_hand = X[:, :, 33:54, :2]
    right_hand = X[:, :, 54:75, :2]

    # AI4Bharat uses 25 pose landmarks.
    pose = pose[:, :, :25, :]

    # Flatten each frame
    pose = pose.reshape(n_samples, n_frames, 25 * 2)
    left_hand = left_hand.reshape(n_samples, n_frames, 21 * 2)
    right_hand = right_hand.reshape(n_samples, n_frames, 21 * 2)

    # 50 + 42 + 42 = 134
    features = np.concatenate(
        [pose, left_hand, right_hand],
        axis=-1,
    )

    if features.shape[-1] != 134:
        raise RuntimeError(
            f"AI4Bharat feature conversion failed: "
            f"{features.shape}"
        )

    # ---------------------------------------------------------
    # AI4Bharat evaluator uses max_frame_len = 169
    # ---------------------------------------------------------

    target_frames = 169

    if n_frames > target_frames:
        features = features[:, :target_frames, :]

    elif n_frames < target_frames:

        padding = np.zeros(
            (
                n_samples,
                target_frames - n_frames,
                134,
            ),
            dtype=np.float32,
        )

        features = np.concatenate(
            [features, padding],
            axis=1,
        )

    return features.astype(np.float32)


def load_and_convert_npz(npz_path):
    """
    Load the SignLoop master NPZ and convert
    the keypoints to AI4Bharat format.
    """

    data = np.load(
        npz_path,
        allow_pickle=True,
    )

    X = data["X"]
    y = data["y"]

    if "label_map" in data:
        label_map_raw = data["label_map"]

        try:
            if isinstance(label_map_raw, np.ndarray):
                label_map_raw = label_map_raw.item()

            label_map = json.loads(str(label_map_raw))

        except Exception:
            label_map = None
    else:
        label_map = None

    X_ai4bharat = convert_to_ai4bharat(X)

    return X_ai4bharat, y, label_map


class AI4BharatDataset(torch.utils.data.Dataset):

    def __init__(self, X, y):
        self.X = torch.from_numpy(X).float()
        self.y = torch.from_numpy(
            np.asarray(y, dtype=np.int64)
        ).long()

    def __len__(self):
        return len(self.X)

    def __getitem__(self, index):
        return self.X[index], self.y[index]
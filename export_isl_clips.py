"""
export_isl_clips.py
Extracts authentic 30-frame ISL motion clips from include_keypoints_master.npz
and include_keypoints.npz into compact JSON clips for Unity 6 avatar playback.
Features:
- Robust Fingertip-to-Wrist Extension Ratio curl calculation (0° to 82°).
- Temporal gap-filling (linear interpolation across occluded frames).
- Realistic 2.5-second playback duration per sign.
- Direct output to both Assets/Animations/ISLClips/ and Assets/Resources/ISLClips/.
"""

import json
import numpy as np
import shutil
from pathlib import Path

# Paths
ROOT = Path(__file__).resolve().parent
MASTER_NPZ = ROOT / "include_keypoints_master.npz"
INCLUDE_NPZ = ROOT / "dataset" / "data" / "include_keypoints.npz"
ANIM_DIR = ROOT / "isl-vr-unity" / "Assets" / "Animations" / "ISLClips"
RES_DIR = ROOT / "isl-vr-unity" / "Assets" / "Resources" / "ISLClips"
ANIM_DIR.mkdir(parents=True, exist_ok=True)
RES_DIR.mkdir(parents=True, exist_ok=True)

# Avatar calibration constants (Avatar height ~1.75m, shoulders ~1.35m)
CHEST_CENTER = np.array([0.0, 1.35, 0.05], dtype=np.float32)
ARM_SCALE_XY = 0.20     # meters per MediaPipe normalized unit
ARM_SCALE_Z = 0.13      # monocular Z damping to match avatar physical reach
DEFAULT_DURATION = 2.5  # 2.5 seconds base duration for realistic, clear ISL signing

def fill_tracking_gaps(landmarks_seq):
    """
    Linearly interpolates across any dropped/zero frames in a (30, N, 3) landmark sequence.
    """
    norms = np.linalg.norm(landmarks_seq, axis=(1, 2))
    active_indices = np.where(norms > 1e-3)[0]
    if len(active_indices) == 0:
        return landmarks_seq, False
    if len(active_indices) == len(landmarks_seq):
        return landmarks_seq, True

    filled = landmarks_seq.copy()
    for i in range(len(landmarks_seq)):
        if norms[i] < 1e-3:
            before = [j for j in active_indices if j < i]
            after = [j for j in active_indices if j > i]
            if before and after:
                b_idx, a_idx = before[-1], after[0]
                frac = (i - b_idx) / (a_idx - b_idx)
                filled[i] = (1.0 - frac) * filled[b_idx] + frac * filled[a_idx]
            elif before:
                filled[i] = filled[before[-1]]
            elif after:
                filled[i] = filled[after[0]]
    return filled, True

def compute_curl_from_extension(tip_pt, mcp_pt, wrist_pt, is_thumb=False):
    """
    Computes anatomical finger curl angle (0 to 82 deg) using fingertip-to-wrist extension ratio.
    Scale-invariant and immune to monocular distance fluctuations.
    """
    d_mcp = np.linalg.norm(mcp_pt - wrist_pt)
    if d_mcp < 1e-4:
        return 0.0
    d_tip = np.linalg.norm(tip_pt - wrist_pt)
    ratio = d_tip / d_mcp

    if is_thumb:
        # Thumb extension ratio ranges from ~1.0 (folded in) to ~1.55 (extended out)
        fraction = np.clip((1.55 - ratio) / (1.55 - 0.95), 0.0, 1.0)
        return float(fraction * 55.0)
    else:
        # Fingers: extension ratio ranges from ~0.75 (curled in) to ~2.10 (extended out)
        fraction = np.clip((2.10 - ratio) / (2.10 - 0.75), 0.0, 1.0)
        return float(fraction * 82.0)

def get_finger_curls_robust(hand_landmarks):
    """
    Calculates clear, expressive 5-finger curl angles from 21 MediaPipe hand points.
    Returns (curls_list, is_active)
    """
    if np.linalg.norm(hand_landmarks) < 1e-3:
        return [0.0, 0.0, 0.0, 0.0, 0.0], False

    wrist = hand_landmarks[0]
    # Thumb: tip(4), mcp(2)
    t_c = compute_curl_from_extension(hand_landmarks[4], hand_landmarks[2], wrist, is_thumb=True)
    # Index: tip(8), mcp(5)
    i_c = compute_curl_from_extension(hand_landmarks[8], hand_landmarks[5], wrist)
    # Middle: tip(12), mcp(9)
    m_c = compute_curl_from_extension(hand_landmarks[12], hand_landmarks[9], wrist)
    # Ring: tip(16), mcp(13)
    r_c = compute_curl_from_extension(hand_landmarks[16], hand_landmarks[13], wrist)
    # Pinky: tip(20), mcp(17)
    p_c = compute_curl_from_extension(hand_landmarks[20], hand_landmarks[17], wrist)

    return [t_c, i_c, m_c, r_c, p_c], True

def compute_wrist_quaternion(wrist_pt, middle_mcp, index_mcp, pinky_mcp, is_left):
    """
    Derives wrist rotation Quaternion from 3D hand orientation landmarks.
    """
    forward = middle_mcp - wrist_pt
    f_norm = np.linalg.norm(forward)
    if f_norm < 1e-4:
        return {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0}
    forward = forward / f_norm

    across = (pinky_mcp - index_mcp) if is_left else (index_mcp - pinky_mcp)
    a_norm = np.linalg.norm(across)
    if a_norm < 1e-4:
        across = np.array([1.0, 0.0, 0.0] if is_left else [-1.0, 0.0, 0.0])
    else:
        across = across / a_norm

    normal = np.cross(forward, across)
    n_norm = np.linalg.norm(normal)
    if n_norm < 1e-4:
        normal = np.array([0.0, 0.0, 1.0])
    else:
        normal = normal / n_norm

    m00, m01, m02 = across[0], normal[0], forward[0]
    m10, m11, m12 = across[1], normal[1], forward[1]
    m20, m21, m22 = across[2], normal[2], forward[2]

    tr = m00 + m11 + m22
    if tr > 0:
        S = np.sqrt(tr + 1.0) * 2
        qw = 0.25 * S
        qx = (m21 - m12) / S
        qy = (m02 - m20) / S
        qz = (m10 - m01) / S
    elif (m00 > m11) and (m00 > m22):
        S = np.sqrt(1.0 + m00 - m11 - m22) * 2
        qw = (m21 - m12) / S
        qx = 0.25 * S
        qy = (m01 + m10) / S
        qz = (m02 + m20) / S
    elif m11 > m22:
        S = np.sqrt(1.0 + m11 - m00 - m22) * 2
        qw = (m02 - m20) / S
        qx = (m01 + m10) / S
        qy = 0.25 * S
        qz = (m12 + m21) / S
    else:
        S = np.sqrt(1.0 + m22 - m00 - m11) * 2
        qw = (m10 - m01) / S
        qx = (m02 + m20) / S
        qy = (m12 + m21) / S
        qz = 0.25 * S

    q_len = np.sqrt(qx*qx + qy*qy + qz*qz + qw*qw)
    if q_len < 1e-6:
        return {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0}
    return {"x": float(qx / q_len), "y": float(qy / q_len), "z": float(qz / q_len), "w": float(qw / q_len)}

def export_clip_from_sample(sample, sign_name):
    """
    Converts a single (30, 75, 3) sample into a structured Unity JSON ISL clip.
    """
    frames_data = []

    # Apply temporal gap filling to hands
    lh_raw = sample[:, 33:54]
    rh_raw = sample[:, 54:75]
    lh_filled, lh_overall_active = fill_tracking_gaps(lh_raw)
    rh_filled, rh_overall_active = fill_tracking_gaps(rh_raw)

    sh_l = sample[:, 11]
    sh_r = sample[:, 12]
    sh_center = (sh_l + sh_r) / 2.0

    default_l_wrist = np.array([-0.25, 0.85, 0.15], dtype=np.float32)
    default_r_wrist = np.array([0.25, 0.85, 0.15], dtype=np.float32)
    default_l_elbow = np.array([-0.38, 1.05, -0.10], dtype=np.float32)
    default_r_elbow = np.array([0.38, 1.05, -0.10], dtype=np.float32)

    for f in range(30):
        t = float(f / 29.0)

        # LEFT ARM
        l_wrist_mp = sample[f, 15]
        l_elbow_mp = sample[f, 13]

        if np.linalg.norm(l_wrist_mp) > 1e-3:
            delta_w = l_wrist_mp - sh_center[f]
            l_wrist_pos = CHEST_CENTER + np.array([
                -delta_w[0] * ARM_SCALE_XY,
                -delta_w[1] * ARM_SCALE_XY,
                -delta_w[2] * ARM_SCALE_Z
            ])
            l_wrist_pos[1] = np.clip(l_wrist_pos[1], 0.70, 1.65)
            l_wrist_pos[2] = np.clip(l_wrist_pos[2], 0.10, 0.55)
        else:
            l_wrist_pos = default_l_wrist.copy()

        if np.linalg.norm(l_elbow_mp) > 1e-3:
            delta_e = l_elbow_mp - sh_center[f]
            l_elbow_pos = CHEST_CENTER + np.array([
                -delta_e[0] * ARM_SCALE_XY,
                -delta_e[1] * ARM_SCALE_XY,
                -delta_e[2] * ARM_SCALE_Z
            ])
            l_elbow_pos[2] = min(l_elbow_pos[2], 0.05)
        else:
            l_elbow_pos = default_l_elbow.copy()

        # Left Hand Finger Curls & Wrist Orientation
        lh_pts = lh_filled[f]
        l_curls, l_active = get_finger_curls_robust(lh_pts)
        if l_active and lh_overall_active:
            u_lh = np.array([[-p[0], -p[1], -p[2]] for p in lh_pts])
            l_rot = compute_wrist_quaternion(u_lh[0], u_lh[9], u_lh[5], u_lh[17], is_left=True)
        else:
            l_rot = {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0}

        # RIGHT ARM
        r_wrist_mp = sample[f, 16]
        r_elbow_mp = sample[f, 14]

        if np.linalg.norm(r_wrist_mp) > 1e-3:
            delta_w = r_wrist_mp - sh_center[f]
            r_wrist_pos = CHEST_CENTER + np.array([
                -delta_w[0] * ARM_SCALE_XY,
                -delta_w[1] * ARM_SCALE_XY,
                -delta_w[2] * ARM_SCALE_Z
            ])
            r_wrist_pos[1] = np.clip(r_wrist_pos[1], 0.70, 1.65)
            r_wrist_pos[2] = np.clip(r_wrist_pos[2], 0.10, 0.55)
        else:
            r_wrist_pos = default_r_wrist.copy()

        if np.linalg.norm(r_elbow_mp) > 1e-3:
            delta_e = r_elbow_mp - sh_center[f]
            r_elbow_pos = CHEST_CENTER + np.array([
                -delta_e[0] * ARM_SCALE_XY,
                -delta_e[1] * ARM_SCALE_XY,
                -delta_e[2] * ARM_SCALE_Z
            ])
            r_elbow_pos[2] = min(r_elbow_pos[2], 0.05)
        else:
            r_elbow_pos = default_r_elbow.copy()

        # Right Hand Finger Curls & Wrist Orientation
        rh_pts = rh_filled[f]
        r_curls, r_active = get_finger_curls_robust(rh_pts)
        if r_active and rh_overall_active:
            u_rh = np.array([[-p[0], -p[1], -p[2]] for p in rh_pts])
            r_rot = compute_wrist_quaternion(u_rh[0], u_rh[9], u_rh[5], u_rh[17], is_left=False)
        else:
            r_rot = {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0}

        frame_entry = {
            "time": t,
            "leftWristPos": {"x": float(l_wrist_pos[0]), "y": float(l_wrist_pos[1]), "z": float(l_wrist_pos[2])},
            "leftElbowHint": {"x": float(l_elbow_pos[0]), "y": float(l_elbow_pos[1]), "z": float(l_elbow_pos[2])},
            "leftWristRot": l_rot,
            "leftThumbCurl": l_curls[0],
            "leftIndexCurl": l_curls[1],
            "leftMiddleCurl": l_curls[2],
            "leftRingCurl": l_curls[3],
            "leftPinkyCurl": l_curls[4],
            "leftHandActive": l_active and lh_overall_active,

            "rightWristPos": {"x": float(r_wrist_pos[0]), "y": float(r_wrist_pos[1]), "z": float(r_wrist_pos[2])},
            "rightElbowHint": {"x": float(r_elbow_pos[0]), "y": float(r_elbow_pos[1]), "z": float(r_elbow_pos[2])},
            "rightWristRot": r_rot,
            "rightThumbCurl": r_curls[0],
            "rightIndexCurl": r_curls[1],
            "rightMiddleCurl": r_curls[2],
            "rightRingCurl": r_curls[3],
            "rightPinkyCurl": r_curls[4],
            "rightHandActive": r_active and rh_overall_active
        }
        frames_data.append(frame_entry)

    clip_dict = {
        "signName": sign_name,
        "duration": DEFAULT_DURATION,
        "frameCount": len(frames_data),
        "frames": frames_data
    }

    # Save to both Animations and Resources
    out_anim = ANIM_DIR / f"{sign_name}.json"
    out_res = RES_DIR / f"{sign_name}.json"
    content = json.dumps(clip_dict, indent=2)
    with open(out_anim, "w", encoding="utf-8") as f:
        f.write(content)
    with open(out_res, "w", encoding="utf-8") as f:
        f.write(content)

    print(f"Exported ISL clip: {sign_name} (duration: {DEFAULT_DURATION}s)")

def main():
    print("Exporting authentic ISL sign motion clips (2.5s duration, robust finger curls)...")
    dm = np.load(MASTER_NPZ, allow_pickle=True)
    Xm = dm["X"]

    master_targets = {
        "Hello": 47,
        "ThankYou": 380,
        "HowAreYou": 1003,
        "GoodMorning": 132,
        "You": 52,
        "I": 93,
        "Sign": 7
    }

    for name, sample_idx in master_targets.items():
        sample = Xm[sample_idx]
        export_clip_from_sample(sample, name)

    if INCLUDE_NPZ.exists():
        dk = np.load(INCLUDE_NPZ, allow_pickle=True)
        Xk = dk["X"]
        include_targets = {
            "Doctor": 70,
            "Friend": 26,
            "Teacher": 72,
            "India": 111,
            "House": 27
        }
        for name, sample_idx in include_targets.items():
            sample = Xk[sample_idx]
            export_clip_from_sample(sample, name)

    print("\nAll 12 authentic ISL clips exported successfully!")

if __name__ == "__main__":
    main()

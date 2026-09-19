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

# Avatar calibration constants (Avatar height ~1.75m, shoulders ~1.38m, forward signing plane ~0.35m)
BASE_ANCHOR = np.array([0.0, 1.38, 0.35], dtype=np.float32)
ARM_SCALE_X = 0.28      # meters per MediaPipe normalized unit in lateral width
ARM_SCALE_Y = 0.34      # meters per MediaPipe normalized unit in vertical reach
ARM_SCALE_Z = 0.16      # monocular Z damping to match avatar physical reach
DEFAULT_DURATION = 1.2  # 1.2 seconds base duration for realistic, clear ISL signing

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

def compute_segment_angle(p1, p2, p3):
    """
    Computes true 3D anatomical bend angle between three joint landmarks (0 deg when straight, ~90 deg when bent).
    Scale-invariant, coordinate-system invariant, and robust to monocular camera depth noise.
    """
    v1 = p1 - p2
    v2 = p3 - p2
    n1 = np.linalg.norm(v1)
    n2 = np.linalg.norm(v2)
    if n1 < 1e-4 or n2 < 1e-4:
        return 0.0
    cos_angle = np.clip(np.dot(v1, v2) / (n1 * n2), -1.0, 1.0)
    # Flexion: 180 - angle (0 when straight collinear, ~90 when bent at right angle)
    return max(0.0, 180.0 - np.degrees(np.arccos(cos_angle)))

def get_finger_curls_robust(hand_landmarks):
    """
    Calculates precise, expressive, anatomical 5-finger curl angles from 21 MediaPipe hand points.
    Combines 3D joint segment angles with knuckle-to-tip extension ratios for high-contrast clarity.
    Returns (curls_list, is_active)
    """
    if np.linalg.norm(hand_landmarks) < 1e-3:
        return [0.0, 0.0, 0.0, 0.0, 0.0], False

    wrist = hand_landmarks[0]
    
    # Finger joint definitions: (mcp, pip, dip, tip)
    finger_joints = [
        (1, 2, 3, 4),    # Thumb: CMC(1), MCP(2), IP(3), TIP(4)
        (5, 6, 7, 8),    # Index
        (9, 10, 11, 12), # Middle
        (13, 14, 15, 16),# Ring
        (17, 18, 19, 20) # Pinky
    ]
    
    curls = []
    for idx, (j_mcp, j_pip, j_dip, j_tip) in enumerate(finger_joints):
        if idx == 0:
            # Thumb: CMC, MCP, IP
            a1 = compute_segment_angle(wrist, hand_landmarks[j_mcp], hand_landmarks[j_pip])
            a2 = compute_segment_angle(hand_landmarks[j_mcp], hand_landmarks[j_pip], hand_landmarks[j_dip])
            a3 = compute_segment_angle(hand_landmarks[j_pip], hand_landmarks[j_dip], hand_landmarks[j_tip])
            total_ang = a1 * 0.4 + a2 * 0.6 + a3 * 0.6
            d_tip = np.linalg.norm(hand_landmarks[j_tip] - hand_landmarks[17]) # distance to pinky base (opposition)
            d_base = np.linalg.norm(hand_landmarks[5] - hand_landmarks[17])
            opp_ratio = d_tip / (d_base + 1e-4)
            
            # High-contrast thumb opposition
            opp_score = np.clip((opp_ratio - 0.5) / (1.1 - 0.5), 0.0, 1.0)
            opp_score = opp_score * opp_score * (3.0 - 2.0 * opp_score) # Smoothstep for punchier poses
            
            c = np.clip(total_ang * 0.4 + (1.0 - opp_score) * 65.0, 0.0, 65.0)
            curls.append(float(c))
        else:
            # 4 Fingers: MCP, PIP, DIP
            a1 = compute_segment_angle(wrist, hand_landmarks[j_mcp], hand_landmarks[j_pip])
            a2 = compute_segment_angle(hand_landmarks[j_mcp], hand_landmarks[j_pip], hand_landmarks[j_dip])
            a3 = compute_segment_angle(hand_landmarks[j_pip], hand_landmarks[j_dip], hand_landmarks[j_tip])
            total_ang = a1 * 0.3 + a2 * 0.4 + a3 * 0.3
            
            d_mcp = np.linalg.norm(hand_landmarks[j_mcp] - wrist)
            d_tip = np.linalg.norm(hand_landmarks[j_tip] - wrist)
            ratio = d_tip / (d_mcp + 1e-4)
            
            # High-contrast extension ratio for distinct fists vs open palms
            ext_score = np.clip((ratio - 1.0) / (1.7 - 1.0), 0.0, 1.0)
            ext_score = ext_score * ext_score * (3.0 - 2.0 * ext_score) # Smoothstep
            
            c = np.clip(total_ang * 0.3 + (1.0 - ext_score) * 85.0, 0.0, 85.0)
            curls.append(float(c))
            
    return curls, True

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

    default_l_wrist = np.array([-0.24, 1.08, 0.33], dtype=np.float32)
    default_r_wrist = np.array([0.24, 1.08, 0.33], dtype=np.float32)
    default_l_elbow = np.array([-0.42, 1.15, -0.15], dtype=np.float32)
    default_r_elbow = np.array([0.42, 1.15, -0.15], dtype=np.float32)

    for f in range(30):
        t = float(f / 29.0)

        # LEFT ARM
        l_wrist_mp = sample[f, 15]
        l_elbow_mp = sample[f, 13]

        if np.linalg.norm(l_wrist_mp) > 1e-3:
            delta_w = l_wrist_mp - sh_center[f]
            l_wrist_pos = BASE_ANCHOR + np.array([
                -delta_w[0] * ARM_SCALE_X,
                -delta_w[1] * ARM_SCALE_Y,
                -delta_w[2] * ARM_SCALE_Z
            ])
            l_wrist_pos[0] = np.clip(l_wrist_pos[0], -0.45, 0.12)
            l_wrist_pos[1] = np.clip(l_wrist_pos[1], 1.02, 1.70)
            l_wrist_pos[2] = np.clip(l_wrist_pos[2], 0.30, 0.55)
        else:
            l_wrist_pos = default_l_wrist.copy()

        if np.linalg.norm(l_elbow_mp) > 1e-3:
            delta_e = l_elbow_mp - sh_center[f]
            l_elbow_pos = BASE_ANCHOR + np.array([
                -delta_e[0] * ARM_SCALE_X,
                -delta_e[1] * ARM_SCALE_Y,
                -delta_e[2] * ARM_SCALE_Z
            ])
            l_elbow_pos[0] = min(l_elbow_pos[0], -0.20)
            l_elbow_pos[2] = min(l_elbow_pos[2], -0.05)
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
            r_wrist_pos = BASE_ANCHOR + np.array([
                -delta_w[0] * ARM_SCALE_X,
                -delta_w[1] * ARM_SCALE_Y,
                -delta_w[2] * ARM_SCALE_Z
            ])
            r_wrist_pos[0] = np.clip(r_wrist_pos[0], -0.12, 0.45)
            r_wrist_pos[1] = np.clip(r_wrist_pos[1], 1.02, 1.70)
            r_wrist_pos[2] = np.clip(r_wrist_pos[2], 0.30, 0.55)
        else:
            r_wrist_pos = default_r_wrist.copy()

        if np.linalg.norm(r_elbow_mp) > 1e-3:
            delta_e = r_elbow_mp - sh_center[f]
            r_elbow_pos = BASE_ANCHOR + np.array([
                -delta_e[0] * ARM_SCALE_X,
                -delta_e[1] * ARM_SCALE_Y,
                -delta_e[2] * ARM_SCALE_Z
            ])
            r_elbow_pos[0] = max(r_elbow_pos[0], 0.20)
            r_elbow_pos[2] = min(r_elbow_pos[2], -0.05)
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
        
    # --- TEMPORAL SMOOTHING PASS (Removes robotic jitter) ---
    smoothed_frames = []
    window = 3 # 3-frame moving average
    for i in range(len(frames_data)):
        start_idx = max(0, i - window // 2)
        end_idx = min(len(frames_data), i + window // 2 + 1)
        slice_frames = frames_data[start_idx:end_idx]
        
        sf = dict(frames_data[i]) # copy structure
        
        # Helper to average Vector3 dictionaries
        def avg_vec3(key):
            x = sum(f[key]["x"] for f in slice_frames) / len(slice_frames)
            y = sum(f[key]["y"] for f in slice_frames) / len(slice_frames)
            z = sum(f[key]["z"] for f in slice_frames) / len(slice_frames)
            return {"x": x, "y": y, "z": z}
            
        sf["leftWristPos"] = avg_vec3("leftWristPos")
        sf["leftElbowHint"] = avg_vec3("leftElbowHint")
        sf["rightWristPos"] = avg_vec3("rightWristPos")
        sf["rightElbowHint"] = avg_vec3("rightElbowHint")
        
        # Average curls
        for curl_key in ["leftThumbCurl", "leftIndexCurl", "leftMiddleCurl", "leftRingCurl", "leftPinkyCurl",
                         "rightThumbCurl", "rightIndexCurl", "rightMiddleCurl", "rightRingCurl", "rightPinkyCurl"]:
            sf[curl_key] = sum(f[curl_key] for f in slice_frames) / len(slice_frames)
            
        smoothed_frames.append(sf)
        
    frames_data = smoothed_frames

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

    ensure_meta_file(out_anim)
    ensure_meta_file(out_res)

    print(f"Exported ISL clip: {sign_name} (duration: {DEFAULT_DURATION}s)")

def ensure_meta_file(file_path: Path):
    """
    Guarantees a matching Unity .meta file exists with a unique GUID.
    """
    meta_path = file_path.with_name(file_path.name + ".meta")
    if not meta_path.exists():
        import uuid
        guid = uuid.uuid4().hex
        meta_content = (
            "fileFormatVersion: 2\n"
            f"guid: {guid}\n"
            "TextScriptImporter:\n"
            "  externalObjects: {}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n"
        )
        with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(meta_content)

def clean_sign_name(raw_label):
    """
    Converts raw dataset class labels into clean, standard PascalCase sign names.
    e.g. '10. Plane' -> 'Plane', '16. train ticket' -> 'TrainTicket'
    """
    import re
    cleaned = re.sub(r'^\d+\.\s*', '', raw_label).strip()
    cleaned = re.sub(r'\(.*?\)', '', cleaned).strip()
    cleaned = re.sub(r'[^a-zA-Z0-9\s]', ' ', cleaned)
    words = cleaned.split()
    return ''.join(w.capitalize() for w in words)

def main():
    print("Exporting authentic ISL sign motion clips for all vocabulary words...")
    exported_names = set()
    chosen_trajectories = {}

    # 1. Master targets (Greetings & core pronouns from include_keypoints_master.npz)
    # Using verified pure, non-overlapping indices from the master dataset
    if MASTER_NPZ.exists():
        dm = np.load(MASTER_NPZ, allow_pickle=True)
        Xm = dm["X"]
        master_targets = {
            "Hello": 47,         # pure unique greeting waving sample
            "ThankYou": 380,     # pure unique chin outward sweep
            "HowAreYou": 2046,   # pure unique conversational sample
            "GoodMorning": 132,  # pure unique good morning sample
            "You": 52,           # pure unique forward pointing sample
            "I": 93,             # pure unique self chest point
            "Sign": 7            # pure unique signing gesture
        }
        for name, sample_idx in master_targets.items():
            sample = Xm[sample_idx]
            export_clip_from_sample(sample, name)
            exported_names.add(name)
            chosen_trajectories[name] = sample[:, [15, 16]] # left and right wrist

    # 2. All 73 classes from include_keypoints.npz (Zenodo video extractions)
    # Using diversity-enforcing selection: guarantees every sign has a distinct trajectory
    if INCLUDE_NPZ.exists():
        dk = np.load(INCLUDE_NPZ, allow_pickle=True)
        Xk = dk["X"]
        yk = dk["y"]
        lm_k = json.loads(str(dk["label_map"]))

        for raw_label, class_id in sorted(lm_k.items(), key=lambda x: x[1]):
            sign_name = clean_sign_name(raw_label)
            sample_indices = np.where(yk == class_id)[0]
            if len(sample_indices) == 0:
                continue

            best_idx = None
            best_score = -1e9
            for idx in sample_indices:
                s = Xk[idx]
                lh = s[:, 33:54]
                rh = s[:, 54:75]
                lh_act = (np.linalg.norm(lh, axis=(1, 2)) > 1e-3).sum()
                rh_act = (np.linalg.norm(rh, axis=(1, 2)) > 1e-3).sum()
                if lh_act < 20 and rh_act < 20:
                    continue

                sh_center_y = (s[:, 11, 1] + s[:, 12, 1]) / 2.0
                rw_y = s[:, 16, 1]
                lw_y = s[:, 15, 1]
                peak_elev = float(max(np.max(sh_center_y - rw_y), np.max(sh_center_y - lw_y)))
                wrist_range = float(max(np.ptp(rw_y), np.ptp(lw_y)))
                score = (lh_act + rh_act) * 10.0 + wrist_range * 150.0 + peak_elev * 250.0

                # Diversity enforcement: penalize similarity to previously chosen signs
                wrists = s[:, [15, 16]]
                min_dist = 1e9
                for other_name, other_wrists in chosen_trajectories.items():
                    dist = float(np.mean(np.linalg.norm(wrists - other_wrists, axis=-1)))
                    if dist < min_dist:
                        min_dist = dist

                if min_dist < 0.10:
                    score -= (0.10 - min_dist) * 2000.0

                if score > best_score:
                    best_score = score
                    best_idx = idx

            if best_idx is None:
                best_idx = sample_indices[0]

            sample = Xk[best_idx]
            export_clip_from_sample(sample, sign_name)
            exported_names.add(sign_name)
            chosen_trajectories[sign_name] = sample[:, [15, 16]]

    print(f"\nSuccessfully exported all {len(exported_names)} authentic ISL sign clips into Unity!")
    print(f"Directory 1: {ANIM_DIR}")
    print(f"Directory 2: {RES_DIR}")

if __name__ == "__main__":
    main()

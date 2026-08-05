import jiwer
import numpy as np
import warnings

def get_accuracy_reward(correct_text: str, predicted_text: str) -> float:
    """
    Calculates R_accuracy using Word Error Rate.
    Clamps the result at 0 to prevent destabilizing negative rewards.
    """
    # Handle empty strings to avoid division by zero in WER calculation
    if not correct_text:
        return 0.0
        
    error_rate = jiwer.wer(correct_text, predicted_text)
    return max(0.0, 1.0 - error_rate)

def get_length_penalty(predicted_text: str, lambda_weight: float = 0.01) -> float:
    """
    Calculates L_penalty based on the number of tokens generated.
    """
    # Count tokens by splitting the string
    num_tokens = len(predicted_text.split())
    
    # Apply negative linear penalty
    penalty = -abs(lambda_weight) * num_tokens
    return penalty

def get_bilateral_reward(frame_buffer: np.ndarray) -> float:
    """
    Computes a bilateral rhythm and synchronization reward based on the 
    correlation of left and right wrist speeds over a 30-frame window.
    """
    # Ensure there are enough frames to calculate temporal differences
    if len(frame_buffer) < 2:
        return 0.0
        
    # Ensure buffer is 2D: (30, 225)
    if frame_buffer.ndim == 3 and frame_buffer.shape[-1] == 3:
        frame_buffer = frame_buffer.reshape(frame_buffer.shape[0], -1)

    # MediaPipe Pose landmarks: Left Wrist = Index 15, Right Wrist = Index 16
    LEFT_WRIST_START = 15 * 3
    RIGHT_WRIST_START = 16 * 3
    
    # Extract 3D trajectories: Shape (30, 3)
    left_wrist = frame_buffer[:, LEFT_WRIST_START : LEFT_WRIST_START + 3]
    right_wrist = frame_buffer[:, RIGHT_WRIST_START : RIGHT_WRIST_START + 3]
    
    # 1. Calculate frame-to-frame velocities
    left_vel = np.diff(left_wrist, axis=0)
    right_vel = np.diff(right_wrist, axis=0)
    
    # 2. Calculate scalar speed (magnitude of the velocity vectors)
    left_speed = np.linalg.norm(left_vel, axis=1)
    right_speed = np.linalg.norm(right_vel, axis=1)
    
    # 3. Synchronous Movement Calculation (Pearson Correlation)
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        # np.corrcoef returns a 2x2 matrix; we take the off-diagonal value [0, 1]
        correlation = np.corrcoef(left_speed, right_speed)[0, 1]
        
    # If hands are perfectly stationary, correlation returns NaN.
    # Default to neutral 0.5 to avoid penalizing static sign holds.
    if np.isnan(correlation):
        return 0.5
        
    # 4. Normalize the correlation from [-1, 1] to [0.0, 1.0]
    sync_reward = (correlation + 1.0) / 2.0
    
    return float(np.clip(sync_reward, 0.0, 1.0))

def calculate_total_reward(correct_text: str, predicted_text: str, frame_buffer: np.ndarray, 
                           alpha: float = 1.0, beta: float = 0.2, lambda_weight: float = 0.01) -> float:
    """
    Fuses accuracy, bilateral synchronization, and length penalty into the final RLVR reward.
    
    Args:
        correct_text (str): Ground-truth text.
        predicted_text (str): The model's sequence prediction.
        frame_buffer (np.ndarray): The (30, 225) spatial sequence tensor.
        alpha (float): Weight for WER accuracy.
        beta (float): Weight for bilateral sync bonus.
        lambda_weight (float): Weight for length penalty.
        
    Returns:
        float: The final step reward for the policy gradient update.
    """
    # 1. Base Accuracy (1 - WER)
    r_acc = get_accuracy_reward(correct_text, predicted_text)
    
    # 2. Bilateral Sync Bonus
    r_bilateral = get_bilateral_reward(frame_buffer)
    
    # 3. Length Penalty (Already negative)
    r_penalty = get_length_penalty(predicted_text, lambda_weight)
    
    # Total Reward Equation
    total_reward = (alpha * r_acc) + (beta * r_bilateral) + r_penalty
    
    return total_reward

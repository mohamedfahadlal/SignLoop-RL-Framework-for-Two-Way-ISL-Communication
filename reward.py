import jiwer

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
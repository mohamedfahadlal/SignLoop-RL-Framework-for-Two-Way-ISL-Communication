import sys
from pathlib import Path
import torch
import numpy as np

ROOT_DIR = Path(__file__).resolve().parent
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

# NOTE: Ensure this matches your actual policy file name (e.g., policy_2 or policy_3)
from models.policy import ISLPolicyNetwork
from isl_env import ISLEnv

def evaluate_policy(num_trials=500):
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    env = ISLEnv(data_path="dataset/data/include_keypoints_master.npz")
    
    policy = ISLPolicyNetwork(input_dim=225, num_classes=env.action_space.n).to(device)
    policy.load_state_dict(torch.load("models/isl_policy_model.pth", map_location=device))
    policy.eval()
    
    # Tracking Variables
    correct = 0
    total_reward = 0.0
    total_acc_score = 0.0
    total_penalty = 0.0
    
    print(f"\nRunning {num_trials} Deterministic Evaluation Episodes...")
    
    with torch.no_grad():
        for _ in range(num_trials):
            state, _ = env.reset()
            state_t = torch.tensor(state, dtype=torch.float32).to(device)
            
            # Deterministic Argmax Inference
            logits, _ = policy(state_t)
            action = logits.argmax(dim=-1).item()
            
            # Step the environment and capture the reward and info dictionary
            _, step_reward, _, _, info = env.step(action)
            
            # Tally exact matches
            if info["guessed_word_id"] == info["expected_word_id"]:
                correct += 1
                
            # Tally your newly added RL metrics
            total_reward += step_reward
            total_acc_score += info.get("accuracy_score", 0.0)
            total_penalty += info.get("length_penalty", 0.0)
                
    # Calculate Averages
    exact_match_accuracy = (correct / num_trials) * 100
    avg_reward = total_reward / num_trials
    avg_acc_score = total_acc_score / num_trials
    avg_penalty = total_penalty / num_trials
    
    print(f"="*50)
    print(f"Final Trained Policy Evaluation Results:")
    print(f"Exact Match Accuracy : {exact_match_accuracy:.2f}%")
    print(f"Average Total Reward : {avg_reward:.4f}")
    print(f"Average Acc Metric   : {avg_acc_score:.4f}")
    print(f"Average Len Penalty  : {avg_penalty:.4f}")
    print(f"="*50)

if __name__ == "__main__":
    evaluate_policy()
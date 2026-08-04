import sys
from pathlib import Path
import torch
import numpy as np

ROOT_DIR = Path(__file__).resolve().parent
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

from models.policy import ISLPolicyNetwork
from isl_env import ISLEnv

def evaluate_policy(num_trials=500):
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    env = ISLEnv(data_path="dataset/data/include_keypoints_master.npz")
    
    policy = ISLPolicyNetwork(input_dim=225, num_classes=env.action_space.n).to(device)
    policy.load_state_dict(torch.load("models/isl_policy_model.pth", map_location=device))
    policy.eval()
    
    correct = 0
    print(f"\nRunning {num_trials} Deterministic Evaluation Episodes...")
    
    with torch.no_grad():
        for _ in range(num_trials):
            state, _ = env.reset()
            state_t = torch.tensor(state, dtype=torch.float32).to(device)
            
            # Deterministic Argmax Inference
            logits, _ = policy(state_t)
            action = logits.argmax(dim=-1).item()
            
            _, _, _, _, info = env.step(action)
            if info["guessed_word_id"] == info["expected_word_id"]:
                correct += 1
                
    accuracy = (correct / num_trials) * 100
    print(f"="*50)
    print(f"Final Trained Policy Evaluation Accuracy: {accuracy:.2f}%")
    print(f"="*50)

if __name__ == "__main__":
    evaluate_policy()
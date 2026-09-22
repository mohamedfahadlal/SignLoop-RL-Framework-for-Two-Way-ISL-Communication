import sys
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parent
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

import torch
import torch.nn as nn
import torch.optim as optim
import torch.nn.functional as F
from tqdm import tqdm
import json

from models.policy import ISLPolicyNetwork
from isl_env import ISLEnv

def train_policy_stable(pretrain_epochs=35, rl_episodes=15000, batch_size=32, 
                        alpha=1.5, beta=0.1, lambda_weight=0.01, 
                        save_path="models/isl_policy_10k.pth", 
                        log_path="models/rl_training_history.json",split = "train"):
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Using device: {device}")
    
    # 1. Initialize Environment
    env = ISLEnv(data_path="dataset/data/include_keypoints_master.npz", 
                 alpha=alpha, beta=beta, lambda_weight=lambda_weight)
    num_classes = env.action_space.n
    
    # 2. Policy Network & Optimizer
    policy = ISLPolicyNetwork(input_dim=225, num_classes=num_classes).to(device)
    optimizer = optim.AdamW(policy.parameters(), lr=1e-3, weight_decay=1e-4)
    ce_loss_fn = nn.CrossEntropyLoss()

    training_history = {
        "episode": [],
        "reward": [],
        "baseline": [],
        "advantage": []
    }
    
    # =========================================================
    # STAGE 1: Supervised Warmup (Cross-Entropy Loss)
    # =========================================================
    print("\n[STAGE 1] Supervised Warmup (35 Epochs)...")
    
    X_tensor = torch.tensor(env.X, dtype=torch.float32)
    y_tensor = torch.tensor(env.y, dtype=torch.long)
    dataset = torch.utils.data.TensorDataset(X_tensor, y_tensor)
    loader = torch.utils.data.DataLoader(dataset, batch_size=batch_size, shuffle=True)
    
    for epoch in range(1, pretrain_epochs + 1):
        policy.train()
        total_loss, correct, total = 0.0, 0, 0
        
        for batch_x, batch_y in loader:
            batch_x, batch_y = batch_x.to(device), batch_y.to(device)
            
            optimizer.zero_grad()
            logits, _ = policy(batch_x)
            loss = ce_loss_fn(logits, batch_y)
            loss.backward()
            optimizer.step()
            
            total_loss += loss.item() * batch_x.size(0)
            preds = logits.argmax(dim=-1)
            correct += (preds == batch_y).sum().item()
            total += batch_y.size(0)
            
        print(f"Warmup Epoch {epoch:02d}/{pretrain_epochs:02d} | "
              f"Loss: {total_loss/total:.4f} | Acc: {(correct/total)*100:.2f}%")

    # =========================================================
    # STAGE 2: Stable RL Fine-Tuning (REINFORCE + Advantage)
    # =========================================================
    if rl_episodes > 0:
        print("\n[STAGE 2] Stable Policy Gradient RL Fine-Tuning...")
        policy.train()
        
        # LOWER LEARNING RATE FOR FINE-TUNING TO PREVENT DESTRUCTION OF WEIGHTS
        rl_optimizer = optim.AdamW(policy.parameters(), lr=1e-5, weight_decay=1e-4)
        
        running_baseline = 0.0
        correct_guesses = 0
        pbar = tqdm(range(1, rl_episodes + 1), desc="RL Fine-tuning", unit="ep")
        
        
        rl_optimizer.zero_grad()
        batch_loss = 0.0
        
        for episode in pbar:
            state, _ = env.reset()
            
            # Forward pass with temperature scaling to encourage exploitation
            state_t = torch.tensor(state, dtype=torch.float32).to(device)
            logits, _ = policy(state_t)
            probs = F.softmax(logits / 0.6, dim=-1) # Temperature = 0.8
            
            dist = torch.distributions.Categorical(probs)
            action = dist.sample()
            log_prob = dist.log_prob(action)
            
            # Step through Environment
            _, reward, _, _, info = env.step(action.item())
            is_correct = (info["guessed_word_id"] == info["expected_word_id"])
            
            # Calculate Advantage (R - Moving Average Baseline)
            running_baseline = 0.95 * running_baseline + 0.05 * reward
            advantage = reward - running_baseline
            
            # Policy Gradient Loss accumulated over batch
            loss = (-log_prob * advantage) / batch_size
            loss.backward()
            
            if is_correct:
                correct_guesses += 1

            training_history["episode"].append(episode)
            training_history["reward"].append(float(reward))
            training_history["baseline"].append(float(running_baseline))
            training_history["advantage"].append(float(advantage))
                
            # Execute optimizer step ONLY once every 32 episodes (Batch Accumulation)
            if episode % batch_size == 0:
                torch.nn.utils.clip_grad_norm_(policy.parameters(), max_norm=1.0)
                rl_optimizer.step()
                rl_optimizer.zero_grad()
                
            acc = (correct_guesses / episode) * 100
            pbar.set_postfix({
                "RL Acc": f"{acc:.1f}%",
                "Baseline": f"{running_baseline:.2f}"
            })

    # Save trained checkpoint
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    torch.save(policy.state_dict(), save_path)
    print(f"\nTraining Complete! Checkpoint saved to {save_path}")

    log_path = Path(log_path)
    with open(log_path, "w") as f:
        json.dump(training_history, f)
    print(f"Training logs saved to {log_path}")

    print("\nExporting model to ONNX format...")
    onnx_path = save_path.with_suffix(".onnx")
    
    # Create a dummy input matching your observation space (1 batch, 30 frames, 225 features)
    dummy_input = torch.randn(1, 30, 225, dtype=torch.float32).to(device)
    
    # Export the frozen weights
    torch.onnx.export(
        policy, 
        dummy_input, 
        str(onnx_path),
        export_params=True,
        opset_version=14,
        do_constant_folding=True,
        input_names=['input_state'],
        output_names=['logits', 'attention_weights'],
        dynamic_axes={'input_state': {0: 'batch_size'}, 'logits': {0: 'batch_size'}}
    )
    print(f"ONNX model successfully exported to {onnx_path}")

if __name__ == "__main__":
    train_policy_stable(pretrain_epochs=35, rl_episodes=15000)
import torch
import json
from pathlib import Path
from train_policy import train_policy_stable
from evaluate import evaluate_policy
from isl_env import ISLEnv

def run_ablation_study():
    # Define the 5 model variants for the thesis matrix
    experiments = [
        {
            "name": "Supervised_Baseline",
            "desc": "Standard Cross-Entropy Loss (No RL)",
            "alpha": 0.0, "beta": 0.0, "lambda_weight": 0.0,
            "pretrain_epochs": 15, "rl_episodes": 0
        },
        {
            "name": "Ablation_1_Accuracy_Only",
            "desc": "R = R_accuracy",
            "alpha": 1.5, "beta": 0.0, "lambda_weight": 0.0,
            "pretrain_epochs": 15, "rl_episodes": 15000
        },
        {
            "name": "Ablation_2_Plus_Sync",
            "desc": "R = R_accuracy + beta * R_sync",
            "alpha": 1.5, "beta": 0.1, "lambda_weight": 0.0,
            "pretrain_epochs": 15, "rl_episodes": 15000
        },
        {
            "name": "Ablation_3_Plus_Brevity",
            "desc": "R = R_accuracy - lambda * L_penalty",
            "alpha": 1.5, "beta": 0.0, "lambda_weight": 0.01,
            "pretrain_epochs": 15, "rl_episodes": 15000
        },
        {
            "name": "Full_SignLoop_RL",
            "desc": "R = R_accuracy + beta * R_sync - lambda * L_penalty",
            "alpha": 1.5, "beta": 0.1, "lambda_weight": 0.01,
            "pretrain_epochs": 15, "rl_episodes": 15000
        }
    ]

    results_matrix = {}

    for exp in experiments:
        print(f"\n{'='*60}")
        print(f"🚀 STARTING EXPERIMENT: {exp['name']}")
        print(f"📝 {exp['desc']}")
        print(f"{'='*60}")
        
        model_save_path = f"models/{exp['name']}.pth"
        log_save_path = f"models/{exp['name']}_history.json"
        
        # 1. Train the variant
        train_policy_stable(
            pretrain_epochs=exp['pretrain_epochs'], 
            rl_episodes=exp['rl_episodes'],
            alpha=exp['alpha'],
            beta=exp['beta'],
            lambda_weight=exp['lambda_weight'],
            save_path=model_save_path,
            log_path=log_save_path
        )
        
        # 2. Evaluate the variant
        metrics = evaluate_policy(
            model_path=model_save_path,
            alpha=exp['alpha'],
            beta=exp['beta'],
            lambda_weight=exp['lambda_weight'],
            num_trials=500
        )
        
        results_matrix[exp['name']] = metrics
        
    # 3. Save final ablation matrix to JSON
    with open("models/ablation_results_matrix.json", "w") as f:
        json.dump(results_matrix, f, indent=4)
        
    print("\n✅ ABLATION STUDY COMPLETE! Matrix saved to models/ablation_results_matrix.json")

if __name__ == "__main__":
    run_ablation_study()
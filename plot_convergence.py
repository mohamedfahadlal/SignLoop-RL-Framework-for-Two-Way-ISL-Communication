import json
import numpy as np
import matplotlib.pyplot as plt
from pathlib import Path

def smooth(scalars, weight=0.95):
    """
    Exponential moving average smoothing for noisy RL reward plots.
    weight between 0 and 1. Higher = smoother.
    """
    last = scalars[0]
    smoothed = []
    for point in scalars:
        smoothed_val = last * weight + (1 - weight) * point
        smoothed.append(smoothed_val)
        last = smoothed_val
    return smoothed

def generate_paper_plots(log_path="models/rl_training_history.json", save_path="rl_convergence_plot.png"):
    # Load training data
    with open(log_path, "r") as f:
        history = json.load(f)
        
    episodes = history["episode"]
    rewards = history["reward"]
    baselines = history["baseline"]
    advantages = history["advantage"]
    
    # Apply smoothing
    smooth_rewards = smooth(rewards, weight=0.98)
    smooth_advantages = smooth(advantages, weight=0.98)
    
    # Configure plot style for publication
    plt.style.use("seaborn-v0_8-whitegrid")
    fig, (ax1, ax2) = plt.subplots(2, 1, figsize=(10, 8), sharex=True)
    
    # --- SUBPLOT 1: Reward & Baseline Convergence ---
    ax1.plot(episodes, rewards, color="lightsteelblue", alpha=0.3, label="Raw Reward")
    ax1.plot(episodes, smooth_rewards, color="navy", linewidth=2, label="Smoothed Reward")
    ax1.plot(episodes, baselines, color="darkorange", linewidth=2, linestyle="--", label="Moving Baseline")
    
    ax1.set_title("REINFORCE Policy Gradient Convergence with Self-Critic Baseline", fontsize=14, fontweight="bold")
    ax1.set_ylabel("Total Reward (R)", fontsize=12)
    ax1.legend(loc="lower right")
    
    # --- SUBPLOT 2: Advantage Delta ---
    ax2.plot(episodes, advantages, color="lightcoral", alpha=0.3, label="Raw Advantage")
    ax2.plot(episodes, smooth_advantages, color="darkred", linewidth=2, label="Smoothed Advantage (R - Baseline)")
    ax2.axhline(0, color="black", linestyle="--", linewidth=1.5, alpha=0.7)
    
    ax2.set_title("Advantage Function Convergence", fontsize=14, fontweight="bold")
    ax2.set_xlabel("Training Episodes", fontsize=12)
    ax2.set_ylabel("Advantage", fontsize=12)
    ax2.legend(loc="lower right")
    
    # Format and save
    plt.tight_layout()
    plt.savefig(save_path, dpi=300, bbox_inches="tight")
    print(f"High-resolution publication plot saved to: {save_path}")
    plt.show()

if __name__ == "__main__":
    generate_paper_plots()
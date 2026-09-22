import sys
from pathlib import Path
import torch
import numpy as np
import time
import json
import jiwer
import nltk
import warnings
from nltk.translate.bleu_score import sentence_bleu, SmoothingFunction

ROOT_DIR = Path(__file__).resolve().parent
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

# NOTE: Ensure this matches your actual policy file name (e.g., policy_2 or policy_3)
from models.policy import ISLPolicyNetwork
from isl_env import ISLEnv

smooth_fn = SmoothingFunction().method1

def evaluate_policy(num_trials=500, model_path="models/isl_policy_10k.pth", 
                    alpha=1.5, beta=0.1, lambda_weight=0.01,split = "test"):
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    env = ISLEnv(data_path="dataset/data/include_keypoints_master.npz",
                 alpha=alpha, beta=beta, lambda_weight=lambda_weight)
    
    policy = ISLPolicyNetwork(input_dim=225, num_classes=env.action_space.n).to(device)
    model_path = Path(model_path)
    if not model_path.exists():
        raise FileNotFoundError(f"Model checkpoint not found at: {model_path}")
    policy.load_state_dict(torch.load(model_path, map_location=device))
    policy.eval()
    
    # Tracking Arrays for Corpus Metrics
    references = []
    hypotheses = []
    latencies_ms = []
    bleu_scores = []
    
    print(f"\nRunning {num_trials} Deterministic Evaluation Episodes...")
    
    # Reset GPU memory stats if using CUDA
    if torch.cuda.is_available():
        torch.cuda.reset_peak_memory_stats()

    with torch.no_grad():
        for _ in range(num_trials):
            state, _ = env.reset()
            state_t = torch.tensor(state, dtype=torch.float32).to(device)

            
            
            # --- START TIMING ---
            start_time = time.perf_counter()
            
            logits, _ = policy(state_t)
            action = logits.argmax(dim=-1).item()
            
            # --- END TIMING ---
            latency = (time.perf_counter() - start_time) * 1000  # Convert to ms
            latencies_ms.append(latency)
            
            _, _, _, _, info = env.step(action)
            
            # Save texts for WER/BLEU calculation
            ref_text = info["correct_text"].strip()
            pred_text = info["predicted_text"].strip()
            
            if ref_text and pred_text:
                references.append(ref_text)
                hypotheses.append(pred_text)
                
                # Calculate Sentence BLEU (using unigram weights for single words)
                bleu = sentence_bleu(
                    references, 
                    hypotheses, 
                    weights=(1.0, 0.0, 0.0, 0.0), # CHANGED TO UNIGRAM
                    smoothing_function=smooth_fn
                )
                bleu_scores.append(bleu)

    # --- CALCULATE FINAL METRICS ---
    # 1. Translation Quality
    corpus_wer = jiwer.wer(references, hypotheses)
    avg_bleu = np.mean(bleu_scores)
    accuracy_score = max(0.0, 1.0 - corpus_wer)
    
    # 2. Runtime Benchmarks
    avg_latency_ms = np.mean(latencies_ms)
    fps = 1000.0 / avg_latency_ms if avg_latency_ms > 0 else 0
    vram_mb = torch.cuda.max_memory_allocated(device) / (1024 * 1024) if torch.cuda.is_available() else 0.0
    
    print(f"="*50)
    print(f"Primary Performance Metrics:")
    print(f"Corpus WER        : {corpus_wer:.4f}")
    print(f"Corpus BLEU       : {avg_bleu:.4f}")
    print(f"Accuracy (1 - WER): {accuracy_score:.4f}")
    print(f"\nSystem Efficiency Benchmarks:")
    print(f"Avg Inference Latency : {avg_latency_ms:.2f} ms")
    print(f"Frame Processing Speed: {fps:.1f} FPS")
    print(f"Peak VRAM Footprint   : {vram_mb:.2f} MB")
    print(f"="*50)

    return {
        "WER": corpus_wer,
        "BLEU": avg_bleu,
        "Accuracy": accuracy_score,
        "Latency_ms": avg_latency_ms,
        "FPS": fps,
        "VRAM_MB": vram_mb
    }

    
if __name__ == "__main__":
    evaluate_policy()
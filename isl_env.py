# Import your policy network
from models.policy import ISLPolicyNetwork # Adjust this import name if your file is named differently 
import gymnasium as gym
from gymnasium import spaces
import numpy as np
import json
import torch
import sys
import reward
from pathlib import Path

class ISLEnv(gym.Env):
    """
    Custom OpenAI Gymnasium Environment for Two-Way ISL Translation.
    State Space: 30-frame rolling window of 3D hand + pose coordinates.
    Action Space: Discrete word selection from the ISL vocabulary.
    """
    
    def __init__(self, data_path="dataset/data/include_keypoints_master.npz"):
        super(ISLEnv, self).__init__()
        
        # 1. LOAD THE MASTER DATASET
        print(f"Loading environment dataset from {data_path}...")
        data = np.load(data_path, allow_pickle=True)
        self.X = data["X"]
        self.y = data["y"]
        self.label_map = json.loads(str(data["label_map"]))
        
        # THE FIX: Create the id_to_word dictionary by flipping the label_map
        # This safely handles it whether your map is {"Word": ID} or {"ID": "Word"}
        self.id_to_word = {int(v) if str(v).isdigit() else int(k): k if str(v).isdigit() else v for k, v in self.label_map.items()}
        
        self.vocab_size = len(self.label_map)
        
        print(f"Environment initialized with {len(self.X)} videos and {self.vocab_size} words.")

        # 2. DEFINE ACTION SPACE (Discrete Vocabulary)
        # The agent outputs a single integer corresponding to a word ID.
        self.action_space = spaces.Discrete(self.vocab_size)
        
        # 3. DEFINE OBSERVATION SPACE (Continuous 3D Spatiotemporal Tensor)
        # 30 frames. 75 landmarks (33 pose + 21 left hand + 21 right hand). 3 axes (X, Y, Z).
        # Total features per frame = 75 * 3 = 225. Matrix shape = (30, 225)
        self.observation_space = spaces.Box(
            low=-np.inf, 
            high=np.inf, 
            shape=(30, 225), 
            dtype=np.float32
        )
        
        self.current_sample_idx = 0

    def reset(self, seed=None, options=None):
        """
        Starts a new episode by picking a random video from the dataset.
        """
        super().reset(seed=seed)
        
        # Pick a random video tensor from our merged dataset
        self.current_sample_idx = np.random.randint(0, len(self.X))
        self.current_step = 0
        # Ensure the state is formatted exactly as float32 for PyTorch later
        state = self.X[self.current_sample_idx].astype(np.float32)
        
        return state, {}

    def step(self, action: int):
        # 1. Identify the IDs
        predicted_id = int(action)
        actual_id = int(self.y[self.current_sample_idx]) 

        # 2. Map discrete IDs back to English text strings
        raw_predicted_text = self.id_to_word.get(predicted_id, "")
        raw_correct_text = self.id_to_word.get(actual_id, "")

        # Clean the strings ("74. Tomorrow" becomes "Tomorrow")
        predicted_text = raw_predicted_text.split('. ')[-1] if '. ' in raw_predicted_text else raw_predicted_text
        correct_text = raw_correct_text.split('. ')[-1] if '. ' in raw_correct_text else raw_correct_text

        # 3. GET THE PHYSICAL FRAMES (For Member 3's Kinematics Math)
        # We grab the 30-frame x 225-landmark tensor for the current video
        current_frame_buffer = self.X[self.current_sample_idx]

        # 4. CALCULATE THE UNIFIED REWARD
        # Using the wrapper function Member 3 built with the thesis weights
        step_reward = reward.calculate_total_reward(
            correct_text=correct_text,
            predicted_text=predicted_text,
            frame_buffer=current_frame_buffer,
            alpha=1.0, 
            beta=0.2, 
            lambda_weight=0.01
        )

        # (Optional but recommended) Calculate individual parts for the debug logger
        r_acc = reward.get_accuracy_reward(correct_text, predicted_text)
        r_bilateral = reward.get_bilateral_reward(current_frame_buffer)
        l_penalty = reward.get_length_penalty(predicted_text, lambda_weight=0.01)

        # 5. Move to the next frame/state
        self.current_step += 1
        terminated = True  # Single-word episodes
        truncated = False
        
        # Next state (Placeholder for reset loop)
        next_state = np.zeros((30, 225), dtype=np.float32) 
        
        # 6. Update the Info Dictionary for Member 4's Evaluation Script
        info = {
            "predicted_text": predicted_text,
            "correct_text": correct_text,
            "guessed_word_id": predicted_id,
            "expected_word_id": actual_id,
            "accuracy_score": r_acc,
            "sync_bonus": r_bilateral,
            "length_penalty": l_penalty
        }

        return next_state, step_reward, terminated, truncated, info


if __name__ == "__main__":

    for i in range (100):
        # Ensure the root directory is in the path so we can import the policy
        ROOT_DIR = Path(__file__).resolve().parent
        if str(ROOT_DIR) not in sys.path:
            sys.path.insert(0, str(ROOT_DIR))
            

        
        env = ISLEnv()
        initial_state, _ = env.reset()
        print(f"\nState matrix shape successfully loaded as: {initial_state.shape}")
        
        # 1. Set up the device and initialize the policy network
        device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        policy = ISLPolicyNetwork(input_dim=225, num_classes=env.action_space.n).to(device)
        
        # 2. Load the trained weights saved by train_policy.py
        model_path = Path("models/isl_policy_model.pth")
        if model_path.exists():
            policy.load_state_dict(torch.load(model_path, map_location=device))
            print(f"Successfully loaded trained policy from {model_path}!")
            policy.eval() # Set to evaluation mode for inference
        else:
            print(f"Warning: No trained model found at {model_path}. Using randomized untrained weights.")
        
    # 3. Get the action from the policy using get_action()
    # The get_action method returns a tuple: (action_item, log_prob)
    
        with torch.no_grad():
            action, _ = policy.get_action(initial_state, temperature=1.0, device=device)
        
        # 4. Step the environment using the policy's chosen action
        next_state, step_reward, done, truncated, info = env.step(action)
        
        # 5. Print the results using the correct keys from your updated info dictionary
        print("\n--- Translation Results ---")
        print(f"Agent guessed: '{info.get('predicted_text')}' (ID: {info.get('guessed_word_id')})")
        print(f"Actual text  : '{info.get('correct_text')}' (ID: {info.get('expected_word_id')})")
        print(f"Total Reward : {step_reward:.4f}")
        print(f"  -> Accuracy Score: {info.get('accuracy_score'):.4f}") 
        print(f"  -> Length Penalty: {info.get('length_penalty'):.4f}")



  
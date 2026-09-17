import gymnasium as gym
from gymnasium import spaces
import numpy as np
import json
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
        
        # Ensure the state is formatted exactly as float32 for PyTorch later
        state = self.X[self.current_sample_idx].astype(np.float32)
        
        return state, {}

    def step(self, action):
        """
        The agent takes a guess (action). The environment returns a reward.
        """
        correct_label = self.y[self.current_sample_idx]
        
        # --- PLACEHOLDER REWARD LOGIC ---
        # Members 2 & 3 will replace this block with the real R_accuracy, 
        # R_bilateral_sync, and L_penalty math later.
        if action == correct_label:
            reward = 1.0  # Guessed correctly!
        else:
            reward = 0.0  # Guessed wrong.
        # --------------------------------
            
        # Since this is offline classification-style RL, the episode 
        # terminates immediately after the agent makes its translation guess.
        terminated = True
        truncated = False
        
        # Gym expects a next_state, but since the episode is over, we just return zeros
        next_state = np.zeros(self.observation_space.shape, dtype=np.float32)
        
        # We pass info out so you can print it to the terminal while debugging
        info = {
            "expected_word_id": correct_label,
            "guessed_word_id": action
        }
        
        return next_state, reward, terminated, truncated, info


if __name__ == "__main__":
    
    env = ISLEnv()
    initial_state, _ = env.reset()
    print(f"\nState matrix shape successfully loaded as: {initial_state.shape}")
    
   
    random_guess = env.action_space.sample()
    next_state, reward, done, _, info = env.step(random_guess)
    
    print(f"Agent guessed ID: {info['guessed_word_id']} | Actual ID was: {info['expected_word_id']}")
    print(f"Reward received: {reward}")
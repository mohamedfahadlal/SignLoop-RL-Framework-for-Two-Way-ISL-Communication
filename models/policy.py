import torch
import torch.nn as nn
import torch.nn.functional as F
from torch.distributions import Categorical

class TemporalAttention(nn.Module):
    """
    Computes attention weights over the temporal frames to focus on 
    critical motion phases in the gesture sequence.
    """
    def __init__(self, hidden_dim):
        super(TemporalAttention, self).__init__()
        self.attn = nn.Sequential(
            nn.Linear(hidden_dim * 2, hidden_dim),
            nn.Tanh(),
            nn.Linear(hidden_dim, 1)
        )

    def forward(self, rnn_out):
        # rnn_out shape: (batch_size, seq_len, hidden_dim * 2)
        weights = F.softmax(self.attn(rnn_out), dim=1) # (batch_size, seq_len, 1)
        context = torch.sum(weights * rnn_out, dim=1)  # (batch_size, hidden_dim * 2)
        return context, weights

class ISLPolicyNetwork(nn.Module):
    """
    BiGRU + Temporal Attention Policy Network (π_θ).
    Maps raw spatiotemporal keypoint states into discrete action distributions.
    """
    def __init__(self, input_dim=225, hidden_dim=256, num_classes=263, dropout=0.3):
        super(ISLPolicyNetwork, self).__init__()
        
        # 1. Feature Projection Layer
        self.fc_input = nn.Sequential(
            nn.Linear(input_dim, hidden_dim),
            nn.BatchNorm1d(hidden_dim),
            nn.ReLU(),
            nn.Dropout(dropout)
        )
        
        # 2. Spatiotemporal Recurrent Encoder (BiGRU)
        self.bigru = nn.GRU(
            input_size=hidden_dim,
            hidden_size=hidden_dim,
            num_layers=2,
            batch_first=True,
            bidirectional=True,
            dropout=dropout
        )
        
        # 3. Temporal Attention Layer
        self.attention = TemporalAttention(hidden_dim)
        
        # 4. Policy Action Head
        self.action_head = nn.Sequential(
            nn.Linear(hidden_dim * 2, 128),
            nn.ReLU(),
            nn.Dropout(dropout),
            nn.Linear(128, num_classes)
        )

    def forward(self, x):
        # Ensure tensor is batched and properly flattened to (Batch, Sequence, 225)
        if x.dim() == 2:
            x = x.unsqueeze(0) # (30, 225) -> (1, 30, 225)
            
        if x.dim() == 4:
            # (Batch, 30, 75, 3) -> (Batch, 30, 225)
            batch_size, seq_len, num_nodes, coords = x.shape
            x = x.view(batch_size, seq_len, num_nodes * coords)
            
        if x.dim() == 3 and x.shape[-1] == 3:
            # Unbatched (30, 75, 3) -> Batched (1, 30, 225)
            x = x.unsqueeze(0)
            batch_size, seq_len, num_nodes, coords = x.shape
            x = x.view(batch_size, seq_len, num_nodes * coords)

        batch_size, seq_len, feat_dim = x.shape
        
        # Project keypoint features
        x_flat = x.reshape(-1, feat_dim)
        x_proj = self.fc_input(x_flat)
        x_seq = x_proj.view(batch_size, seq_len, -1)
        
        # Sequential pass & Attention pooling
        rnn_out, _ = self.bigru(x_seq)
        context, weights = self.attention(rnn_out)
        
        # Logits over discrete vocabulary
        logits = self.action_head(context)
        return logits, weights

    def get_action(self, state, temperature=1.0, device="cpu"):
        state_t = torch.tensor(state, dtype=torch.float32).to(device)
        logits, _ = self.forward(state_t)
        
        scaled_logits = logits / temperature
        probs = F.softmax(scaled_logits, dim=-1)
        
        dist = Categorical(probs)
        action = dist.sample()
        
        return action.item(), dist.log_prob(action)
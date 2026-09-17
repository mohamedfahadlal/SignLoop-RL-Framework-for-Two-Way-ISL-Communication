import torch
import torch.nn as nn
import torch.nn.functional as F

class TemporalAttention(nn.Module):
    """
    Computes frame-level attention weights over the 30-frame temporal dimension.
    Allows the model to focus on key gesture movement phases.
    """
    def __init__(self, hidden_dim):
        super(TemporalAttention, self).__init__()
        self.attn = nn.Linear(hidden_dim * 2, 1)

    def forward(self, rnn_out):
        # rnn_out shape: (batch_size, seq_len, hidden_dim * 2)
        weights = F.softmax(self.attn(rnn_out), dim=1) # (batch_size, seq_len, 1)
        context = torch.sum(weights * rnn_out, dim=1) # (batch_size, hidden_dim * 2)
        return context, weights

class ISLSignPolicyNetwork(nn.Module):
    """
    BiGRU + Temporal Attention Architecture for Live ISL Translation.
    Input shape: (Batch_Size, 30_Frames, 225_Features)
    """
    def __init__(self, input_dim=225, hidden_dim=256, num_layers=2, num_classes=263, dropout=0.3):
        super(ISLSignPolicyNetwork, self).__init__()
        
        # Spatial Feature Projection Layer
        self.fc_input = nn.Sequential(
            nn.Linear(input_dim, hidden_dim),
            nn.BatchNorm1d(hidden_dim),
            nn.ReLU(),
            nn.Dropout(dropout)
        )
        
        # Sequential Bidirectional Recurrent Layer
        self.bigru = nn.GRU(
            input_size=hidden_dim,
            hidden_size=hidden_dim,
            num_layers=num_layers,
            batch_first=True,
            bidirectional=True,
            dropout=dropout if num_layers > 1 else 0
        )
        
        # Temporal Attention Mechanism
        self.attention = TemporalAttention(hidden_dim)
        
        # Policy / Classification Output Head
        self.classifier = nn.Sequential(
            nn.Linear(hidden_dim * 2, 128),
            nn.ReLU(),
            nn.Dropout(dropout),
            nn.Linear(128, num_classes)
        )

    def forward(self, x):
        # x shape: (batch_size, seq_len=30, features=225)
        batch_size, seq_len, feat_dim = x.shape
        
        # Flatten sequence for batch norm projection
        x_flat = x.view(-1, feat_dim)
        x_proj = self.fc_input(x_flat)
        x_seq = x_proj.view(batch_size, seq_len, -1)
        
        # BiGRU Forward Pass
        rnn_out, _ = self.bigru(x_seq) # (batch_size, 30, hidden_dim * 2)
        
        # Apply Temporal Attention
        context, attn_weights = self.attention(rnn_out)
        
        # Output Logits
        logits = self.classifier(context)
        return logits, attn_weights

if __name__ == "__main__":
    # Test instantiation with a random tensor batch
    dummy_input = torch.randn(8, 30, 225) # Batch of 8 clips, 30 frames, 225 features
    model = ISLSignPolicyNetwork(input_dim=225, num_classes=263)
    output, attn = model(dummy_input)
    print(f"Input shape:  {dummy_input.shape}")
    print(f"Output shape: {output.shape}")
    print("Model initialized successfully!")
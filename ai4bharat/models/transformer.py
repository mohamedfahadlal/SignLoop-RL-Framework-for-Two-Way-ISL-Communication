import torch
import torch.nn as nn
import torch.nn.functional as F
from transformers import BertLayer


class PositionEmbedding(nn.Module):
    def __init__(self, config):
        super().__init__()

        self.position_embeddings = nn.Embedding(
            config.max_position_embeddings,
            config.hidden_size
        )

        # IMPORTANT:
        # Official checkpoint uses "LayerNorm"
        # with capital L and N.
        self.LayerNorm = nn.LayerNorm(
            config.hidden_size,
            eps=config.layer_norm_eps
        )

        self.dropout = nn.Dropout(
            config.hidden_dropout_prob
        )

        self.register_buffer(
            "position_ids",
            torch.arange(
                config.max_position_embeddings
            ).expand((1, -1))
        )

    def forward(self, x):
        seq_length = x.size(1)

        position_ids = self.position_ids[:, :seq_length]

        position_embeddings = self.position_embeddings(
            position_ids
        )

        x = x + position_embeddings

        x = self.LayerNorm(x)

        x = self.dropout(x)

        return x


class INCLUDETransformer(nn.Module):
    def __init__(self, config, num_classes=263):
        super().__init__()

        # Official checkpoint expects l1
        self.l1 = nn.Linear(
            config.input_size,
            config.hidden_size
        )

        self.embedding = PositionEmbedding(config)

        self.layers = nn.ModuleList([
            BertLayer(config.model_config)
            for _ in range(config.num_hidden_layers)
        ])

        # Official checkpoint expects l2
        self.l2 = nn.Linear(
            config.hidden_size,
            num_classes
        )

    def forward(self, x):

        x = self.l1(x)

        x = self.embedding(x)

        for layer in self.layers:
            x = layer(x)

        x = torch.max(
            x,
            dim=1
        ).values

        x = F.dropout(
            x,
            p=0.2,
            training=self.training
        )

        return self.l2(x)
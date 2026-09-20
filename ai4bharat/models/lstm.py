import torch
import torch.nn as nn
import torch.nn.functional as F


class INCLUDELSTM(nn.Module):
    def __init__(self, config, num_classes=263):
        super().__init__()

        self.lstm = nn.LSTM(
            input_size=config.input_size,
            hidden_size=config.hidden_size,
            num_layers=config.num_layers,
            batch_first=config.batch_first,
            bidirectional=config.bidirectional,
            dropout=config.dropout,
        )

        in_features = (
            config.hidden_size * 2
            if config.bidirectional
            else config.hidden_size
        )

        self.l1 = nn.Linear(
    in_features=in_features,
    out_features=num_classes
)

    def forward(self, x):
        x, _ = self.lstm(x)

        x = torch.max(x, dim=1).values

        x = F.dropout(
            x,
            p=0.3,
            training=self.training
        )

        return self.l1(x)
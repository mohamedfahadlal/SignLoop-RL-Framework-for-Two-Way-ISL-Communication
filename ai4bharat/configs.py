from dataclasses import dataclass, field

from transformers import BertConfig


@dataclass
class LSTMConfig:
    input_size: int = 134
    hidden_size: int = 256
    num_layers: int = 5
    batch_first: bool = True
    bidirectional: bool = True
    dropout: float = 0.2


@dataclass
class TransformerConfig:
    size: str

    input_size: int = 134
    max_position_embeddings: int = 256
    layer_norm_eps: float = 1e-12
    hidden_dropout_prob: float = 0.1

    hidden_size: int = 512
    num_attention_heads: int = 8
    num_hidden_layers: int = 4

    model_config: object = field(init=False)

    def __post_init__(self):
        if self.size not in ["small", "large"]:
            raise ValueError("size must be 'small' or 'large'")

        if self.size == "small":
            self.hidden_size = 256
            self.num_attention_heads = 4
            self.num_hidden_layers = 2

        self.model_config = BertConfig(
            hidden_size=self.hidden_size,
            num_attention_heads=self.num_attention_heads,
            num_hidden_layers=self.num_hidden_layers,
            max_position_embeddings=self.max_position_embeddings,
            layer_norm_eps=self.layer_norm_eps,
            hidden_dropout_prob=self.hidden_dropout_prob,
        )
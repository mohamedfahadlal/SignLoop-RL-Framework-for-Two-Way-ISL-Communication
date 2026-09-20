import os
import torch

from ai4bharat.configs import (
    LSTMConfig,
    TransformerConfig,
)

from ai4bharat.models import (
    INCLUDELSTM,
    INCLUDETransformer,
)


MODEL_DIR = "models/ai4bharat"


def load_checkpoint(path, model, device):
    """
    Load an AI4Bharat checkpoint.
    """

    checkpoint = torch.load(
    path,
    map_location=device,
    weights_only=False
)

    # Official checkpoints contain the model
    # parameters under the "model" key.
    if isinstance(checkpoint, dict) and "model" in checkpoint:
        state_dict = checkpoint["model"]
    else:
        state_dict = checkpoint

    model.load_state_dict(
        state_dict,
        strict=True,
    )

    model.to(device)
    model.eval()

    return model


def load_lstm(device):
    path = os.path.join(
        MODEL_DIR,
        "include_no_cnn_lstm.pth",
    )

    config = LSTMConfig()

    model = INCLUDELSTM(
        config=config,
        num_classes=263,
    )

    return load_checkpoint(
        path,
        model,
        device,
    )


def load_transformer_small(device):
    path = os.path.join(
        MODEL_DIR,
        "include_no_cnn_transformer_small.pth",
    )

    config = TransformerConfig(
        size="small",
    )

    model = INCLUDETransformer(
        config=config,
        num_classes=263,
    )

    return load_checkpoint(
        path,
        model,
        device,
    )


def load_transformer_large(device):
    path = os.path.join(
        MODEL_DIR,
        "include_no_cnn_transformer_large.pth",
    )

    config = TransformerConfig(
        size="large",
    )

    model = INCLUDETransformer(
        config=config,
        num_classes=263,
    )

    return load_checkpoint(
        path,
        model,
        device,
    )


if __name__ == "__main__":

    device = torch.device(
        "cuda" if torch.cuda.is_available() else "cpu"
    )

    print("Device:", device)
    print()

    print("Loading LSTM...")
    load_lstm(device)
    print("LSTM loaded successfully.")

    print("Loading Transformer Small...")
    load_transformer_small(device)
    print("Transformer Small loaded successfully.")

    print("Loading Transformer Large...")
    load_transformer_large(device)
    print("Transformer Large loaded successfully.")

    print()
    print("=" * 60)
    print("ALL AI4BHARAT MODELS LOADED SUCCESSFULLY")
    print("=" * 60)
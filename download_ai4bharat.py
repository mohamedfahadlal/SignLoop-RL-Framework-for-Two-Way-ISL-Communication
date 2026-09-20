import os
import torch


MODELS = {
    "include_no_cnn_lstm.pth":
        "https://api.wandb.ai/files/abdur-ai4bharat/include-no-cnn/2prih6pi/augs_lstm.pth",

    "include_no_cnn_transformer_large.pth":
        "https://api.wandb.ai/files/abdur-ai4bharat/include-no-cnn/1nywb73r/augs_transformer.pth",

    "include_no_cnn_transformer_small.pth":
        "https://api.wandb.ai/files/abdur-ai4bharat/include-no-cnn/2kuznb3t/augs_transformer.pth",
}


MODEL_DIR = "models/ai4bharat"

os.makedirs(MODEL_DIR, exist_ok=True)


for filename, url in MODELS.items():

    output_path = os.path.join(
        MODEL_DIR,
        filename
    )

    if os.path.exists(output_path):
        print(f"Already exists: {filename}")
        continue

    print()
    print("=" * 60)
    print(f"Downloading: {filename}")
    print("=" * 60)

    try:

        torch.hub.download_url_to_file(
            url,
            output_path,
            progress=True
        )

        print(f"Downloaded: {output_path}")

    except Exception as e:

        print(f"ERROR downloading {filename}")
        print(e)
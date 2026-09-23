import numpy as np
import torch
from torch.utils.data import Dataset, DataLoader

class ISLKeypointDataset(Dataset):
    def __init__(self, npz_path):
        data = np.load(npz_path, allow_pickle=True)
        self.X = data['X'] # Expected shape: (N_samples, 30, 225) or (N_samples, 30, 75, 3)
        self.y = data['y'] # Class index labels
        
        # If stored as (N, 30, 75, 3), flatten spatial dimensions to (N, 30, 225)
        if len(self.X.shape) == 4:
            N, T, V, C = self.X.shape
            self.X = self.X.reshape(N, T, V * C)

    def __len__(self):
        return len(self.X)

    def __getitem__(self, idx):
        x_tensor = torch.tensor(self.X[idx], dtype=torch.float32)
        y_tensor = torch.tensor(self.y[idx], dtype=torch.long)
        return x_tensor, y_tensor

def get_dataloaders(npz_path, batch_size=32, train_split=0.8):
    dataset = ISLKeypointDataset(npz_path)
    train_size = int(len(dataset) * train_split)
    val_size = len(dataset) - train_size
    
    train_ds, val_ds = torch.utils.data.random_split(dataset, [train_size, val_size])
    
    train_loader = DataLoader(train_ds, batch_size=batch_size, shuffle=True)
    val_loader = DataLoader(val_ds, batch_size=batch_size, shuffle=False)
    
    return train_loader, val_loader
import torch
import torchvision
import os

from PIL import Image

import matplotlib.pyplot as plt
import numpy as np

import torch.nn as nn
import torch.nn.functional as F

import torch.optim as optim

def LoadNetTXT(model, path):
	data_dict = {}
	fin = open(path, 'r')
	i = 0
	odd = 1
	prev_key = None
	while True:
		s = fin.readline().strip()
		if not s: break
		elif odd: prev_key = s
		else:
			val = eval(s)
			if type(val) != type([]): data_dict[prev_key] = torch.FloatTensor([eval(s)])[0]
			else:                     data_dict[prev_key] = torch.FloatTensor(eval(s))
			i += 1
		odd = (odd + 1) % 2

	# Replace existing values with loaded
	own_state = model.state_dict()
	for k, v in data_dict.items():
		if k in own_state:
			try: own_state[k].copy_(v)
			except: sys.exit(0)

class DetectHand:
	
	def __init__(self):
		self.transform = torchvision.transforms.Compose(
			[torchvision.transforms.ToTensor(),
			torchvision.transforms.Normalize((0.5, 0.5, 0.5), (0.5, 0.5, 0.5))]
		)
		self.propagate = Net()
		LoadNetTXT(self.propagate, 'handNet.txt')
		
	
	def ProcessImg(self, filename, xPieces=8, yPieces=6):
		#Crop image
		im = Image.open(filename)
		imgwidth, imgheight = im.size
		height = imgheight // yPieces
		width = imgwidth // xPieces
		
		images = []
		
		for i in reversed(range(yPieces)):
			for j in range(xPieces):
				box = (j * width, i * height, (j + 1) * width, (i + 1) * height)
				
				images.append(self.LoadImg(im.crop(box)))
		
		return torch.stack(images)
		
	def LoadImg(self, image):
		if self.transform:
			image = self.transform(image)
		return image
		

class Net(nn.Module):
	
    def __init__(self):
        super().__init__()
        self.conv1 = nn.Conv2d(3, 6, 4) 
        self.pool = nn.MaxPool2d(2) 
        self.conv2 = nn.Conv2d(6, 16, 4) 
        self.fc1 = nn.Linear(16*4*4, 120)
        self.fc2 = nn.Linear(120, 84)
        self.fc3 = nn.Linear(84, 48)
        self.fc4 = nn.Linear(48, 16)
        self.fc5 = nn.Linear(16, 1)

    def forward(self, x):
        #CNN:

        #x (3 colour channels, 25x25 image) (excuding batch dimension)        => x.shape = 03x25x25 
        x = self.conv1(x) #6 feature maps (output channels), 4x4 kernal       => x.shape = 06x22x22
        x = F.relu(x) #returns input tensor with all negative values set to 0 => x.shape = 06x22x22
        x = self.pool(x) #2x2 kernal returning max value each step:           => x.shape = 06x11x11
        x = self.conv2(x) #16 output channels, 4x4 kernal                     => x.shape = 16x08x08
        x = F.relu(x) #                                                       => x.shape = 16x08x08
        x = self.pool(x) #                                                    => x.shape = 16x04x04
        x = torch.flatten(x, 1) #flatten all dimensions except batch          => x.shape = 256

        #FFNN
        x = F.relu(self.fc1(x)) #256 input nodes to 120 hidden nodes          => x.shape = 120
        x = F.relu(self.fc2(x)) #120 hidden nodes to 84 hidden nodes          => x.shape = 84
        x = F.relu(self.fc3(x)) #84 hidden nodes to 48 hidden nodes           => x.shape = 84
        x = F.relu(self.fc4(x)) #48 hidden nodes to 16 hidden nodes           => x.shape = 84
        x = torch.sigmoid(self.fc5(x))        #16 hidden nodes to 1 ouput node (overkill?)  => x.shape = 1
        return x

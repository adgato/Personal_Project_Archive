from camera import arCamera
from UI import Interface
import os, datetime

def screenshot(filename):
	os.system('raspi2png; convert snapshot.png -crop 960x720+960+340 -resize 200 ' + filename)

camera  = arCamera()
UI      = Interface()

camera.Enable()

try:
	while True:
		shotName = datetime.datetime.now().strftime('images/%H_%M_%S.png')		
		screenshot(shotName)
				
		UI.ClearFrame()
		UI.UpdateText()
		UI.Update()
except Exception: 
	camera.Disable()
#in future use add gyroscopic funtionality for enhanced experience

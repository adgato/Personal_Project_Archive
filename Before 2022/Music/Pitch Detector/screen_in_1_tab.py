from PIL import ImageGrab
import cv2
import numpy

while True:
    screenshot = ImageGrab.grab()
    screenshot = cv2.cvtColor(numpy.array(screenshot), cv2.COLOR_RGB2BGR)

    cv2.imshow('', screenshot)
    cv2.waitKey(1)

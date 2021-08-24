import pyaudio
import struct
import numpy as np

import cv2
import imageio

#move the GIF frame forward by the specified number of frames
def iterateGIF(gif, frameidx, nframes, delay=1):
    for frame in range(frameidx, frameidx + nframes):
        if frame%delay == 0:
            cv2.imshow('', cv2.cvtColor(gif[int(frame/delay)%len(gif)], cv2.COLOR_BGR2RGB))
            cv2.waitKey(1)
    return frame

#remove higher frequency noises, so only the bass can be heard
def smoothData(y, box_pts):
    box = np.ones(box_pts)/box_pts
    y_smooth = np.convolve(y, box, mode='same')
    return y_smooth

pa       = pyaudio.PyAudio()
FORMAT   = pyaudio.paInt16
CHANNELS = 1
RATE     = 44100
CHUNK    = 1024

#should work but if it dosent you can set the index manually
for ii in range(pa.get_device_count()):
   if 'VoiceMeeter Output' in pa.get_device_info_by_index(ii)['name']:
       index = ii
       break

stream = pa.open(format = FORMAT,
                 channels = CHANNELS,
                 rate = RATE,
                 input = True,
                 input_device_index = index,
                 frames_per_buffer = CHUNK)

maxd = 0

gif = imageio.get_reader('torus.gif') #or any gif you want
gif = [frame for frame in gif]

frame = iterateGIF(gif, 0, 1)
keepMoving = True #works better False for some gifs
while True:

   data = stream.read(CHUNK, exception_on_overflow = False)
   data = np.frombuffer(data, np.int16)
   
   pmaxd = maxd
   maxd = np.amax(smoothData(abs(data), 300))

   frame = iterateGIF(gif, frame, 1 + int(keepMoving) + int(abs(maxd-pmaxd)/1000), delay=1) #a delay of 2 would skip every second frame update




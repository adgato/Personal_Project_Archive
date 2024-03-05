from picamera import mmal, mmalobj as mo
import time

class arCamera:
        
        def __init__(self):
                camera = mo.MMALCamera()
                self.splitter = mo.MMALSplitter()
                self.render_l = mo.MMALRenderer()
                self.render_r = mo.MMALRenderer()

                x = 960
                y = 720
                camera.outputs[0].framesize = (x, y)

                camera.outputs[0].framerate = 60
                camera.outputs[0].commit()

                display     = self.render_l.inputs[0].params[mmal.MMAL_PARAMETER_DISPLAYREGION]
                display.set = mmal.MMAL_DISPLAY_SET_FULLSCREEN | mmal.MMAL_DISPLAY_SET_DEST_RECT
                display.fullscreen = False
                
                display.dest_rect = mmal.MMAL_RECT_T(0, 180, x, 1040)
                self.render_l.inputs[0].params[mmal.MMAL_PARAMETER_DISPLAYREGION] = display

                display.dest_rect = mmal.MMAL_RECT_T(x, 180, x, 1040)
                self.render_r.inputs[0].params[mmal.MMAL_PARAMETER_DISPLAYREGION] = display

                display.set = mmal.MMAL_DISPLAY_SET_ALPHA
                display.alpha = 200
                self.render_l.inputs[0].params[mmal.MMAL_PARAMETER_DISPLAYREGION] = display
                        
                self.splitter.connect(camera.outputs[0])
                self.render_l.connect(self.splitter.outputs[0])
                self.render_r.connect(self.splitter.outputs[1])
        
        def Enable(self):
                self.splitter.enable()
                self.render_l.enable()
                self.render_r.enable()
                time.sleep(2)

        def Disable(self):
                self.splitter.disable()
                self.render_l.disable()
                self.render_r.disable()



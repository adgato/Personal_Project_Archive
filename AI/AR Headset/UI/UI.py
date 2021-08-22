import turtle, datetime, time, os

class Interface():
    
    def __init__(self):
        turtle.tracer(0)
        
        self.screen = turtle.Screen()
        self.screen.setup(width=1.0, height=1.0)
        self.screen.bgcolor('black')
        
        self.text = turtle.Turtle()
        self.text.hideturtle()
        self.text.penup()
        
        self.frame = turtle.Turtle()
        self.frame.color('white')
        self.frame.hideturtle()
        
        self.fill = turtle.Turtle()
        self.fill.hideturtle()
        
        self.photoState = 'Photo'
        
    def Update(self):
        turtle.update()
        
    def UpdateText(self):
        self.text.clear()
        
        self.text.color('white')
        self.text.goto(-960,265)
        self.text.write(datetime.datetime.now().strftime('%H:%M:%S'), font=('Consolas',18,'normal'))
        
        self.text.color('black')
        self.text.goto(-780, 70)
        self.text.write(self.photoState, font=('Consolas',18,'normal'), align='center')
        
    def FillFrame(self, i, certainty):
        
        if i == 33: #save screenshot
                boxcolour = 'FF00' 
                if self.photoState == 'Running': 
                        self.photoState = 'Photo'
                elif certainty >= 0.5:
                        if self.photoState == 'Photo': self.photoState = 'Confirm?'
                        elif self.photoState == 'Confirm?': 
                                self.photoState = 'Running'
                                boxcolour = 'FFFF'
                elif self.photoState == 'Confirm?': 
                        self.photoState = 'Photo'
        else:         
                boxcolour = '0000'
        
        x = i % 8
        y = 5 - int(i / 8)
        
        self.fill.fillcolor('#' + format(int(255*certainty), '02x') + boxcolour)
        self.fill.penup()
        self.fill.goto(120*x-960,-120*y+265)
        self.fill.begin_fill()
        for i in range(4):
            self.fill.forward(120)
            self.fill.right(90)
        self.fill.end_fill()
    
    def ClearFrame(self):
        self.fill.clear()

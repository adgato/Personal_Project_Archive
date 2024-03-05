import turtle, os, glob, sys
from tkinter import PhotoImage
from PIL import Image

def imgCrop(filename, xPieces, yPieces, mark):
    im = Image.open(filename)
    imgwidth, imgheight = im.size
    height = imgheight // yPieces
    width = imgwidth // xPieces
    for i in reversed(range(yPieces)):
        for j in range(xPieces):
            box = (j * width, i * height, (j + 1) * width, (i + 1) * height)
            a = im.crop(box)
            a.save("data/" + str(int(mark[j][-i-1][1]))+'_'+str(j)+'-'+str(5-i)+'_'+filename.replace('images\\',''))

    print('Cropped', filename)

def onMove(self, fun, add=None):
    def eventfun(event): fun(self.cv.canvasx(event.x) / self.xscale, -self.cv.canvasy(event.y) / self.yscale)
    self.cv.bind('<Motion>', eventfun, add)

def fillFrame(x, y):
    if -400 < x < 400 and -300 < y < 300:
        cord.clear()
        i = int(x/100-int(x<0))+4
        j = int(y/100+int(y>0))+2
        cord.write(str(int(mark[i][j][1]))+'_'+str(j)+'-'+str(i)+'_'+files[0].replace('images\\',''), font=('Consolas',18,'normal'))
        fill.clear()
        fill.penup()
        fill.goto((i-4)*100,(j-2)*100)
        fill.begin_fill()
        for i in range(4):
            fill.forward(100)
            fill.right(90)
        fill.end_fill()

def selectFrame(x, y):
    if -400 < x < 400 and -300 < y < 300:

        selected    = mark[int(x/100-int(x<0))+4][int(y/100+int(y>0))+2]
        
        if selected[1] == False:
            selected[1] = True
            
            selected[0].goto(fill.xcor(), fill.ycor())
            selected[0].begin_fill()
            for i in range(4):
                selected[0].forward(100)
                selected[0].right(90)
            selected[0].end_fill()
        else:
            selected[1] = False
            selected[0].clear()

        fillFrame(x, y)

def drawGrid():
    grid.clear()
    for i in range(9):
        grid.penup()
        grid.goto(100*i-400,300)
        grid.pendown()
        grid.goto(100*i-400,-300)
    for i in range(7):
        grid.penup()
        grid.goto(-400,-100*i+300)
        grid.pendown()
        grid.goto(400,-100*i+300)

def generateFiles(init=False):
    if not(init):
        imgCrop(files[0], 8, 6, mark)
        files.remove(files[0])
    try:
        larger = PhotoImage(file=files[0]).zoom(4,4)
        turtle.Screen().addshape("larger", turtle.Shape("image", larger))
        img = turtle.Turtle("larger")
        drawGrid()
        fill.fillcolor('blue')
        for i in range(8):
            for j in range(6): mark[i][j] = [fill.clone(), False]
        fill.fillcolor('red')
    except:
        print('All images labelled')
        quit()


turtle.tracer(0)

screen = turtle.Screen()
screen.bgcolor('black')

grid = turtle.Turtle()
grid.hideturtle()
grid.penup()
grid.color('black')

cord = turtle.Turtle()
cord.hideturtle()
cord.penup()
cord.color('white')
cord.goto(-400,310)

fill = turtle.Turtle()
fill.hideturtle()
fill.penup()

mark = [[[] for j in range(6)] for i in range(8)]

files = glob.glob('images/*.png')
generateFiles(init=True)

turtle.listen()
turtle.onkey(generateFiles, "Return")
turtle.onscreenclick(selectFrame)
while True:
    turtle.update()
    onMove(screen, fillFrame)

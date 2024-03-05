import numpy as np, turtle, time

def getConditions(draw, epi=20):
    c = []
    t = np.array([t/len(draw) for t in range(len(draw))])
    for i in range(epi):
        if i%2 == 0: n = int(-i/2)
        else       : n = int((i+1)/2)
        c.append(np.sum(draw * np.exp(-2*np.pi*1j*n*t))/len(draw))
    return c, t

def configEpicycles(c, t):
    draw   = 0
    clocks = []
    for i in range(len(c)):
        if i%2 == 0: n = int(-i/2)
        else       : n = int((i+1)/2)
        clocks.append(c[i] * np.exp(n*2*np.pi*t*1j))
        draw += c[i] * np.exp(n*2*np.pi*t*1j)
    return draw, clocks

def getTracers():
    p = turtle.Turtle()
    p.speed(0)
    p.shapesize(0.2)
    p.shape('circle')
    p.color('red')
    p.penup()

    q = p.clone()

    return p,q

def drawAsVectors(clocks, size=1, repeat=2):

    turtle.tracer(0)
    
    p,q = getTracers()
    p.shapesize(0.5); p.stamp(); p.shapesize(0.2)
    
    draw = 0
    for i in clocks: draw += i
    
    for i in range(len(draw)*repeat):
        i = i%len(draw)
        q.clear()
        p.goto(draw[i].real*size,draw[i].imag*size)
        p.pendown()
        
        q.penup()
        for k in range(len(clocks)):
            if k-1 < 0: q.goto(           clocks[k][i].real*size,            clocks[k][i].imag*size)
            else      : q.goto(q.xcor() + clocks[k][i].real*size, q.ycor() + clocks[k][i].imag*size)
            q.stamp()
            q.pendown()
            
        turtle.update()
        time.sleep(0.05)

def Line():
    screen = turtle.Screen()
    screen.bgcolor('black')
    t = turtle.Turtle()
    t.shape('turtle')
    t.color('white')
    t.speed(0)

    Break = []

    def enter(): Break.append('break')
        
    def dragging(x, y):
        t.ondrag(None)
        t.setheading(t.towards(x, y))
        t.goto(x,y)
        t.ondrag(dragging)

    turtle.tracer(0)
    turtle.listen()

    t.ondrag(dragging)
    turtle.onkey(enter, "Return")

    xs, ys = [], []
    while True:
        ox,oy = t.xcor(), t.ycor()
        turtle.update()
        x,y  = t.xcor(),t.ycor()
        if (x,y) != (ox,oy):
            xs.append(x)
            ys.append(y)
        if Break: break

    t.goto(0,0)
    t.hideturtle()

    return np.array(xs)+(np.array(ys)*1j)


draw   = Line()

c, t = getConditions(draw)

fin, clocks = configEpicycles(c, t)

drawAsVectors(clocks)


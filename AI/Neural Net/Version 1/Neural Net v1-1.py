import turtle

x=[[13,0],[7,0],[6,0],[5,0],[1,0],[-3,1]]
y=[1,1,1,-1,-1,-1]
w=[[0,0,0,0]]
b=[0]
def integration(x,weights):
    xdotw=0
    for i in range(len(x)): xdotw+=x[i]*weights[i+1]
    return xdotw+weights[0]

def update(x,w,b,i):
    wt=[]
    for q in range(len(x[0])): wt.append(w[len(w)-1][q]+y[i]*x[i][q])
    bt=b[len(b)-1]+y[i]
    tt=[wt,bt]
    return tt
def check(x,w,b,i):
    wt=[]
    for q in range(len(x[0])): wt.append(w[len(w)-1][q]*x[i][q])
    return sum(wt)+b[len(b)-1]
def perceptron(x,w,b):
 correct=0
 tries=0
 while correct<len(x) and tries<100:
    tries+=1
    for i in range(len(x)):
        if (b[len(b)-1]==0 or w[len(w)-1][0]==0 or w[len(w)-1][1]==0):
            correct=0
            new=update(x,w,b,i)
            w.append(new[0])
            b.append(new[1])
        elif (check(x,w,b,i)<0 and y[i]<0) or (check(x,w,b,i)>0 and y[i]>0): correct+=1
        else:
            correct=0
            new=update(x,w,b,i)
            w.append(new[0])
            b.append(new[1])
 w[len(w)-1].insert(0,b[len(b)-1])
 output=w[len(w)-1]
 if tries>=100: print('best fit')
 return output

weights=perceptron(x,w,b)
print(weights)
print()
for i in range(len(x)):
    print(integration(x[i],weights),y[i])

points=[]
for i in range(len(x)):
    points.append(turtle.Turtle())
    if y[i]==-1: points[i].color('red')
    else: points[i].color('blue')
    points[i].shapesize(0.4)
    points[i].shape('circle')
    points[i].speed(0)
    points[i].penup()
    points[i].goto(x[i][0]*10,x[i][1]*10)
line=turtle.Turtle()
line.hideturtle()
line.speed(0)
line.penup()
if min(weights)==0:
    print('no can do for you 1d vector sorry')
    quit()
else:
    slope = -(weights[0]/weights[2])/(weights[0]/weights[1])
    intercept = -weights[0]/weights[2]
for xc in range(min(x)[0]-20,max(x)[0]+20):
        y = (slope*xc) + intercept
        line.goto(xc*10,y*10)
        line.pendown()

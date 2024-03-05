import turtle as t
import random as ran
import math as m
import time
Turtle=[]
Speed=[]
Size=[]
Radius=[]
Risk=[]
Fruits=[]
Traits=[Turtle,Speed,Size,Radius,Risk]
FOOD=[]
ENERGY=[]
def clone(amount,papa):
    for baby in range(amount):
        for trait in Traits:
            if trait==Speed or trait==Size: v=4 #vairation
            if trait==Radius: v=12
            if trait==Risk: v=30
            if trait==Turtle: Turtle.append(t.Turtle())
            elif baby==0: trait.append(trait[papa]) #identical clone
            else:
                if trait[papa]-v<=0: lowerbound=1
                else: lowerbound=trait[papa]-v
                trait.append(round(ran.uniform(lowerbound,trait[papa]+v),1)) #slight mutation
        FOOD.append(0)
        ENERGY.append(900)
        newborn=Turtle[len(Turtle)-1]
        newborn.hideturtle()
        if Speed[len(Turtle)-1]/45>1: r=1
        else: r=Speed[len(Turtle)-1]/45
        if Radius[len(Turtle)-1]/180>1: g=1
        else: g=Radius[len(Turtle)-1]/180
        if Risk[len(Turtle)-1]/450>1: b=1
        else: b=Risk[len(Turtle)-1]/900
        newborn.color(r,g,b)
        newborn.turtlesize(Size[len(Turtle)-1]/13)
        newborn.shape('turtle')
        newborn.penup()
        newborn.speed(0)
        if Turtle[papa].xcor()>480: newborn.goto(490,ran.randint(-490,490))
        elif Turtle[papa].xcor()<-480: newborn.goto(-490,ran.randint(-490,490))
        elif Turtle[papa].ycor()>480: newborn.goto(ran.randint(-490,490),490)
        elif Turtle[papa].ycor()<-480: newborn.goto(ran.randint(-490,490),-490)
        newborn.showturtle()

def addFruit(amount):
    for food in range(amount):
        Fruits.append(t.Turtle())
        fruit=Fruits[len(Fruits)-1]
        fruit.hideturtle()
        fruit.color('lime')
        fruit.shape('circle')
        fruit.turtlesize(0.5)
        fruit.penup()
        fruit.speed(0)
        if ran.randint(1,3)==1: fruit.goto(ran.randint(-250,250),ran.randint(-250,250))
        else: fruit.goto(ran.randint(-490,490),ran.randint(-490,490))
        fruit.showturtle()

def kill(i):
    Turtle[i].color('red')
    Turtle[i].goto(900,0)
    Turtle.remove(Turtle[i])
    Speed.remove(Speed[i])
    Size.remove(Size[i])
    Radius.remove(Radius[i])
    Risk.remove(Risk[i])
    FOOD.remove(FOOD[i])
    ENERGY.remove(ENERGY[i])

def sense(t):
    for i in range(len(Fruits)):
     try:
        distance= m.sqrt(m.pow(Turtle[t].xcor()-Fruits[i].xcor(),2)+m.pow(Turtle[t].ycor()-Fruits[i].ycor(),2))
        if distance<Radius[t]:
            while distance<Radius[t]:
                Turtle[t].setheading(Turtle[t].towards(Fruits[i]))
                Turtle[t].forward(Speed[t]**2/10)
                distance= m.sqrt(m.pow(Turtle[t].xcor()-Fruits[i].xcor(),2)+m.pow(Turtle[t].ycor()-Fruits[i].ycor(),2))
                if distance<30:
                    FOOD[t]+=1
                    Fruits[i].goto(900,0)
            return True
     except IndexError: pass
    for i in range(len(Turtle)):
     try:
        if t!=i:
         distance= m.sqrt(m.pow(Turtle[t].xcor()-Turtle[i].xcor(),2)+m.pow(Turtle[t].ycor()-Turtle[i].ycor(),2))
         if distance<Radius[t]:
            if Size[t]>Size[i]*1.1:
                while distance<Radius[t]:
                    Turtle[t].setheading(Turtle[t].towards(Turtle[i]))
                    Turtle[t].forward(Speed[t]**2/10)
                    distance= m.sqrt(m.pow(Turtle[t].xcor()-Turtle[i].xcor(),2)+m.pow(Turtle[t].ycor()-Turtle[i].ycor(),2))
                    if distance<30:
                        FOOD[t]+=1
                        kill(i)
                return True     
            elif Size[t]*1.1<Size[i]:
                while distance<Radius[t]:
                    Turtle[t].towards((Turtle[i].xcor()+180)%180,(Turtle[i].ycor()+180)%180)
                    Turtle[t].forward(Speed[t]**2/10)
                    distance= m.sqrt(m.pow(Turtle[t].xcor()-Turtle[i].xcor(),2)+m.pow(Turtle[t].ycor()-Turtle[i].ycor(),2))
                return True
            else: pass
     except IndexError: pass

def right(t):
    Turtle[t].setheading(0)
    if not(Turtle[t].xcor()+Speed[t]>490): Turtle[t].forward(Speed[t]**2/10)
    else: Turtle[t].goto(490,Turtle[t].ycor())
def left(t):
    Turtle[t].setheading(180)
    if not(Turtle[t].xcor()-Speed[t]<-490): Turtle[t].forward(Speed[t]**2/10)
    else: Turtle[t].goto(-490,Turtle[t].ycor())
def down(t):
    Turtle[t].setheading(-90)
    if not(Turtle[t].ycor()-Speed[t]<-490): Turtle[t].forward(Speed[t]**2/10)
    else: Turtle[t].goto(Turtle[t].xcor(),-490)
def up(t):
    Turtle[t].setheading(90)
    if not(Turtle[t].ycor()+Speed[t]>490): Turtle[t].forward(Speed[t]**2/10)
    else: Turtle[t].goto(Turtle[t].xcor(),490)
def random(t):
    random=ran.randint(1,4)
    if random==1: right(t)
    elif random==2: left(t)
    elif random==3: down(t)
    else: up(t)

def home(t):
    if Turtle[t].xcor()>0:
        if Turtle[t].ycor()>Turtle[t].xcor(): up(t)
        else: right(t)
    else:
        if Turtle[t].ycor()<Turtle[t].xcor(): down(t)
        else: left(t)

def move(t):
    if Turtle[t].xcor()>0:
        if Turtle[t].ycor()>Turtle[t].xcor():
            if ran.randint(1,2)==1: down(t)
            else: random(t)
        else:
            if ran.randint(1,2)==1: left(t)
            else: random(t)
    else:
        if Turtle[t].ycor()<Turtle[t].xcor():
            if ran.randint(1,2)==1: up(t)
            else: random(t)
        else:
            if ran.randint(1,2)==1: right(t)
            else: random(t)
            
def setUp(amount):
    border=t.Turtle()
    border.hideturtle()
    border.speed(0)
    border.penup()
    border.goto(-500,500)
    border.pendown()
    border.pensize(5)
    border.goto(500,500)
    border.goto(500,-500)
    border.goto(-500,-500)
    border.goto(-500,500)
    del border
    addFruit(20)
    for baby in range(amount):
        Turtle.append(t.Turtle())
        newborn=Turtle[len(Turtle)-1]
        newborn.hideturtle()
        Speed.append(15)
        Size.append(15)
        Radius.append(60)
        Risk.append(300)
        FOOD.append(0)
        ENERGY.append(900)
        if Speed[len(Turtle)-1]/25>1: r=1
        else: r=Speed[len(Turtle)-1]/25
        if Radius[len(Turtle)-1]/140>1: g=1
        else: g=Radius[len(Turtle)-1]/140
        if Risk[len(Turtle)-1]/450>1: b=1
        else: b=Risk[len(Turtle)-1]/900
        newborn.color(r,g,b)
        newborn.turtlesize(Size[len(Turtle)-1]/13)
        newborn.shape('turtle')
        newborn.penup()
        newborn.speed(0)
        position=ran.randint(1,4)
        if position==1:   newborn.goto(-490,ran.randint(-490,490))
        elif position==2: newborn.goto(490,ran.randint(-490,490))
        elif position==3: newborn.goto(ran.randint(-490,490),-490)
        elif position==4: newborn.goto(ran.randint(-490,490),490)
        newborn.showturtle()
   
setUp(10)
generation=0
print('Generation: '+str(generation))
print('Average Speed: '+str(round(sum(Speed)/len(Speed),1)))
print('Average Size: '+str(round(sum(Size)/len(Size),1)))
print('Average Radius: '+str(round(sum(Radius)/len(Radius),1)))
print('Average Risk: '+str(round(sum(Risk)/len(Risk),1)))
print()
tillnextfruit=0
while True:
 tillnextfruit+=1
 for i in range(len(Turtle)):
     homed=False
     try:
      if ENERGY[i]<Risk[i] and FOOD[i]>0:
         home(i)
         if (Turtle[i].xcor()**2)**0.5 > 480 or (Turtle[i].ycor()**2)**0.5 > 480 :
             clone(FOOD[i],i)
             kill(i)
         homed=True
     except IndexError:continue
     if not(sense(i)):
        if not(homed):
         try:move(i)
         except IndexError:continue
     ENERGY[i]=ENERGY[i]-round((Size[i]*Speed[i]**2/40+Radius[i]/3)/10,1)
     if ENERGY[i]<0: kill(i)
 if tillnextfruit==90:
     tillnextfruit=0
     generation+=1
     addFruit(14)
     try:
      print('Generation: '+str(generation))
      print('Average Speed: '+str(round(sum(Speed)/len(Speed),1)))
      print('Average Size: '+str(round(sum(Size)/len(Size),1)))
      print('Average Radius: '+str(round(sum(Radius)/len(Radius),1)))
      print('Average Risk: '+str(round(sum(Risk)/len(Risk),1)))
      print()
     except ZeroDivisionError: break
print('Extinction at Generation: '+str(generation))    


#+1 speed = -2 moves default=15
#+1 size  = -1 move         =15
#+1 radius= -0.5 moves      =60
#+1 risk  = -0 moves        =10

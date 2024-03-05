import turtle, math, time, random

def GenRandom(number, types, angles=[]):
    particles = []
    for i in range(number):
        if len(angles) == i: angles.append(random.randint(-80,80))
        particles.append({'pos':turtle.Turtle(), 'type':types[i], 'angle':angles[i]})
    return particles

def Config(particle, pos):
    particle.update({'n':0})
    particle['pos'].hideturtle()
    particle['pos'].speed(0)
    particle['pos'].penup()
    particle['pos'].goto(pos[0], pos[1])
    particle['pos'].pendown()
    if   particle['type'] == 'electron': particle['pos'].color('blue')
    elif particle['type'] == 'positron': particle['pos'].color('red')
    elif particle['type'] == 'photon'  :
        particle['pos'].color('green')
        particle.update({'n':90})
        particle.update({'wave':particle['pos'].clone()})
        particle['pos'].penup()
    return particle
    
def Update(particle):
    
    particle['pos'].setheading(particle['angle'])
    
    if particle['type'] == 'electron' or particle['type'] == 'positron':
        particle['pos'].forward(1)
        if particle['n']%100 == 0:
            if particle['type'] == 'positron': particle['pos'].setheading(180+particle['angle'])
            particle['pos'].stamp()
            if particle['type'] == 'positron': particle['pos'].setheading(180+particle['angle'])
        particle['n'] += 1
            
    elif particle['type'] == 'photon':
        particle['pos'].forward(25/30)
        if round(particle['n']-90,1)%360==0: particle['wave'].goto(particle['pos'].xcor(), particle['pos'].ycor())
        particle['wave'].setheading(particle['angle']+math.sin(math.radians(particle['n']))*50)
        particle['wave'].forward(1)
        particle['n'] += 6

def Distance(a, b):
    return ((a[0]-b[0])**2 + (a[1]-b[1])**2)**0.5

def SyncUpdate(particles):

    done     = True
    rm_index = []
    
    for i in range(len(particles)):
        if not(-400 <= particles[i]['pos'].xcor() < 400 and -400 < particles[i]['pos'].ycor() < 400) and i not in rm_index:
            rm_index.append(i)
            continue
        done = False
        Update(particles[i])

    for k in sorted(rm_index)[::-1]: particles.remove(particles[k])

    return done

def Interactable(a, b, distance=15):
    if Distance([a.xcor(), a.ycor()],[b.xcor(), b.ycor()]) < distance: return True
    else: return False
    
def Interact(particles, distance=200):
    
    done_index = []
    rm_index   = []
    
    for i in range(len(particles)):
        for q in range(len(particles)):
            if (i == q) or (i in done_index) or (q in done_index) or (particles[i]['type'] == particles[q]['type']): continue #2 like particles cannot interact because charge is conserved

            if abs(particles[i]['pos'].towards(particles[q]['pos']) - particles[i]['pos'].heading()) < 3: #inc distance maybe it seems alright atm
                
                particles[i]['angle'] = particles[i]['pos'].towards(particles[q]['pos'])
                if particles[i]['angle'] > 180: particles[i]['angle'] -= 360
                update = 0
                while not(Interactable(particles[i]['pos'], particles[q]['pos'], distance=5)):
                    Update(particles[i])
                    if update%3==0: turtle.update()
                    update += 1
                
                particles[i]['pos'].goto(particles[q]['pos'].xcor(), particles[q]['pos'].ycor())
                if   particles[i]['type'] == 'photon': particles[i]['wave'].goto(particles[q]['pos'].xcor(), particles[q]['pos'].ycor())
                elif particles[q]['type'] == 'photon': particles[q]['wave'].goto(particles[q]['pos'].xcor(), particles[q]['pos'].ycor())
                
                node = turtle.Turtle()
                node.speed(0)
                node.shape('circle')
                node.shapesize(0.5)
                node.penup()
                node.goto(particles[q]['pos'].xcor(), particles[q]['pos'].ycor())
                node.color('white')

                if   particles[i]['type'] == 'photon': other = particles[q]['type']
                elif particles[q]['type'] == 'photon': other = particles[i]['type']
                else                                 : other = 'photon'

                particles.append({'pos':turtle.Turtle(), 'type':other, 'angle': (particles[i]['angle']+particles[q]['angle']) / 2})
                Config(particles[-1], (particles[q]['pos'].xcor(), particles[q]['pos'].ycor()))
                
                for k in (i,q): rm_index.append(k)
                for k in (i,q): done_index.append(k)
 
    for k in sorted(rm_index)[::-1]: particles.remove(particles[k])

def Decay(particles, chance=500):

    og_len   = eval(str(len(particles)))
    rm_index = []
    chance  *= len(particles)
    
    for i in range(og_len):

        if -350 < particles[i]['pos'].xcor() < 350:
            if random.randint(0,chance) == 0:
                
                angle = (random.randint(10,40) + abs(particles[i]['angle'])) * (random.randint(0,1)*2-1)
                if abs(angle) > 80: angle = (10 + abs(particles[i]['angle'])) * (random.randint(0,1)*2-1)
                if abs(angle) > 80: angle = 80; print('conservation of beauty and momentum error!\nprioritising beauty...')
                
                if particles[i]['type'] == 'photon':
                    types = ['electron','positron']
                    random.shuffle(types)
                else:
                    types  = ['photon',particles[i]['type']]
                    random.shuffle(types)

                new_particles = GenRandom(2, types, angles=[angle, -angle])

                for q in range(len(new_particles)):
                    Config(new_particles[q], (particles[i]['pos'].xcor(), particles[i]['pos'].ycor()))
                particles += new_particles

                node = turtle.Turtle()
                node.speed(0)
                node.shape('circle')
                node.shapesize(0.5)
                node.penup()
                node.goto(particles[i]['pos'].xcor(), particles[i]['pos'].ycor())
                node.color('white')

                rm_index.append(i)

    for k in sorted(rm_index)[::-1]: particles.remove(particles[k])


def Draw():
    for i in range(10000):
        turtle.tracer(0, 0)
        if SyncUpdate(particles): break
        if i%3==0: turtle.update()
        Interact(particles)
        Decay(particles, chance=500-int(i/10))
        
while True:
    screen = turtle.Screen()
    screen.bgcolor('black')
    screen.setup(width=1.0, height=1.0)
    
    turtle.tracer(0, 0)
    box = turtle.Turtle()
    box.color('white')
    box.hideturtle()
    box.speed(0)
    box.penup()
    box.goto(-400, 410); box.write('time >')
    box.goto(-450, -400); box.write('space ^')
    box.goto(-400,400)
    box.pendown()
    for i in [400,-400]:
        for q in [i,i*-1]: box.goto(i,q)

    types = [random.choice(['electron','positron','photon']),random.choice(['electron','positron','photon'])]

    particles = GenRandom(2, types, angles=[random.randint(10,30),random.randint(-30,-10)])

    for i in range(len(particles)): Config(particles[i], (-400,i*200-100))

    Draw()
    screen.exitonclick()
    try: turtle.bye()
    except: pass

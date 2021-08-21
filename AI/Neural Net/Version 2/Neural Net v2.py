import numpy as np, math, turtle, matplotlib.pyplot as plt, collections

class NeuralNetwork:

    def __init__(self, layer_sizes):
        weight_shapes   = [(a,b) for a,b in zip(layer_sizes[1:],layer_sizes[:-1])]
        self.numLayers = len(layer_sizes)
        self.W = [np.array(np.random.standard_normal(s)/s[1]**.5) for s in weight_shapes]
        self.B = [np.zeros((s,1)) for s in layer_sizes[1:]]
        self.A = np.asarray([np.zeros(size) for size in layer_sizes], dtype=object)


    #PROCESSES
    def forwardpropagation(self, a):
        self.A[0] = np.array([a]).T
        for L in range(self.numLayers-1):
            Z = np.dot(self.W[L], self.A[L]) + self.B[L]
            self.A[L+1] = self.sigmoid(Z)
        return self.A[-1]


    def backpropagation(self, x, y): #could optimise by simplifying transpositions (i just transposed until it worked, then stopped :)

        self.forwardpropagation(x)
        
        wDeltas = [np.zeros(w.shape) for w in self.W]
        bDeltas = [np.zeros(b.shape) for b in self.B]
        L = -1
        
        aDeltas = 2*(self.A[L] - np.array([y]).T) #derivitive of the cost function
        while L>-self.numLayers:
            zDeltas    = aDeltas * self.sigmoid_prime(self.A[L]) #how much layer L's activation's unsigmoided integrations affected the cost
            
            #times this by the other operand used in forwardpropagation to get how much the element affected the cost
            bDeltas[L] = zDeltas #* 1 (the bias can be seen as the weight to an activation of 1)
            wDeltas[L] = np.dot(zDeltas, self.A[L-1].T)
            aDeltas    = np.dot(zDeltas.T, self.W[L]).T
            L -= 1
        return wDeltas, bDeltas

            
    def train(self, x, y, batch_size=0, r=1, epochs=10, verbose=False):
        
        data    = [(x[i], y[i]) for i in range(len(x))]
        if batch_size == 0: batch_size = len(data)
        batches = [data[i:i+batch_size] for i in range(0, len(data), batch_size)]
        
        notified = 100
        print('\nTraining:')
        if verbose: net.mapNet()
        
        for epoch in range(epochs):
            for batch in batches:
                Wgradients = [np.zeros(w.shape) for w in self.W]
                Bgradients = [np.zeros(b.shape) for b in self.B]
                
                for x,y in batch:
                    wDeltas, bDeltas = self.backpropagation(x, y)
                    Wgradients       = [Wgradients[L] + wDeltas[L] for L in range(len(Wgradients))]
                    Bgradients       = [Bgradients[L] + bDeltas[L] for L in range(len(Bgradients))]
                    
                self.W = [self.W[L] - (Wgradients[L]/len(batch)) * r   for L in range(len(Wgradients))]
                self.B = [self.B[L] - (Bgradients[L]/len(batch)) * r   for L in range(len(Bgradients))]
                
            if round(epoch/epochs*100)%1==0 and round(epoch/epochs*100) != notified:
                notified = round(epoch/epochs*100)
                if verbose: net.mapNet(update=True,message=str(round(epoch/epochs*100))+'%')
                if round(epoch/epochs*100)%10==0: print(' ',round(epoch/epochs*100),' '*(3-len(str(round(epoch/epochs*100))))+'% complete')
                
        if notified != 100: print('  100 % complete')


    def test(self, x, y):
        correct = 0
        cost    = 0
        for i in range(len(x)):
            guess = self.forwardpropagation(x[i])
            if self.list(guess.T[0]).index(max(guess.T[0])) == y[i].index(max(y[i])) and sum(sum((guess.T - y[i])**2))<0.1: correct += 1
            cost += sum(sum((guess.T - y[i])**2))
        print('\nTest results:')
        print('  Score:',round(correct/len(x)*100,2),'%')
        print('  Cost :',cost/len(x))

        
    #FUNCTIONS
    @staticmethod
    def sigmoid(x): return 1/(1+np.exp(-x))

    @staticmethod
    def sigmoid_prime(x): return x * (1 - x) #we use self.A as it is the sigmoid of self.Z, so it shortcuts the need to sigmoid each X here, and to store each Z

    def list(self, array):
        try: array = array.tolist()
        except AttributeError: pass
        for i in range(len(array)):
            try: array[i] = array[i].tolist()
            except: pass
            try:
                for q in range(len(array[i])):
                    try: array[i] = array[i].tolist()
                    except: pass
            except: pass
        return array 


    #VISUALISATIONS
    def mapCost(self, x, y, position, value_range=4):

        if position[0][0]+1 != position[1][0]: print('Invalid weight'); return
        position = (position[0][0],position[1][1],position[0][1])
        print('\nCost Graph:\n  Loading...')
        
        detail    = 1000
        original  = self.W[position[0]][position[1]][position[2]]
        costs     = []
        
        for w in range(-value_range*int(detail/2),value_range*int(detail/2)):
            self.W[position[0]][position[1]][position[2]] = w/(detail/2)
            cost = 0
            for i in range(len(x)): cost += sum(sum((self.forwardpropagation(x[i]).T - y[i] ) **2))    
            costs.append(cost/len(x))

        self.W[position[0]][position[1]][position[2]] = original
        cost     = 0
        gradient = [np.zeros(w.shape) for w in self.W]
        
        
        for i in range(len(x)):
            wDeltas, bDeltas = self.backpropagation(x[i], y[i])
            gradient = [gradient[L] + wDeltas[L] for L in range(len(gradient))]
            cost += sum(sum((self.A[-1].T - y[i] ) **2))

        cost    /= len(x)
        gradient = self.list([gradient[L]/len(x) for L in range(len(gradient))])[position[0]][position[1]][position[2]]
        

        plt.plot([w/(detail/2) for w in range(-value_range*int(detail/2),value_range*int(detail/2))], costs, label='Cost Function')
        xs = []
        ys = []
        
        for i in range(0,int(max(costs)*10000),value_range): #for high costs replace 0 with: int(min(costs)*10000)
            i/=10000
            if (i-(cost-gradient*original))/gradient > -value_range and (i-(cost-gradient*original))/gradient < value_range:
                xs.append((i-(cost-gradient*original))/gradient)
                ys.append(i)

        plt.plot(xs,ys,label='Partial derivative of the Cost Function')
        plt.xlabel('Possible values of Weight connecting Activation ({0},{3}) to Activation ({2},{1})'.format(position[0],position[1],position[0]+1,position[2]))
        plt.ylabel('Cost')
        plt.plot(original, cost,'bo',label='Cost of current Weight')
        plt.legend()
        print('  Loaded')
        plt.show()


    def mapDecision(self, x, y, detail=80, inputs=(0,1), constant=0, confidence=0.9):
        if len(inputs) > 2: print('Error: 2d map can only be generated for 2 inputs'); return
        
        print('\nRelations Graph:\n  Loading...')
        
        pX      = [[a/detail*2,b/detail*2] for a in range(int(-detail/2),int(detail/2)+1) for b in range(int(-detail/2),int(detail/2)+1)]
        outputs = []
        for a,b in pX:
            oX = [constant for i in x[0]]
            oX[inputs[0]], oX[inputs[1]] = a,b
            outputs.append((a, b, self.forwardpropagation(oX)))
        for i in range(len(y[0])):
            xs, ys = [[output[k] for output in outputs if output[2][i][0]>confidence] for k in range(2)]
            plt.plot(xs,ys,'s',label='Confidently output '+str(i),alpha=0.3)
        #for q in range(1,3):
        #    xs, ys = [[output[k] for output in outputs if max(collections.Counter([round(output[2][i][0],q) for i in range(len(y[0]))]).values())>1] for k in range(2)]
        #    plt.plot(xs,ys,'k.',alpha=0.3*q, label=['Paritally','Very'][q-1]+' unconfident regions')
            
        plt.xlabel('Values of input '+str(inputs[0]))
        plt.ylabel('Values of input '+str(inputs[1]))
        plt.legend()
        print('  Loaded')
        plt.show()

        
    def mapNet(self, update=False, message='Map of Neural Net', pause=False):
        
        turtle.tracer(0, 0)
        mess = turtle.Turtle()
        mess.penup()
        mess.hideturtle()
        mess.goto(0,500)
        mess.color('white')
        mess.write(str(message),align='center',font=("Consolas", 20, "normal"))
        
        if not(update):
            screen = turtle.Screen()
            screen.bgcolor('black')
            screen.setup(width=1.0, height=1.0)
            
            self.aNodes = []
            self.bNodes = []
            self.wLines = turtle.Turtle()
            self.wLines.speed(0)
            self.wLines.hideturtle()
            
            
            for L in range(len(self.A)):
                
                self.aNodes.append([])
                ycor=0
                ID = 0
                
                for each_node in self.A[L]:
                    
                    bias=1
                    if   L == 0        : color = 'yellow'
                    elif L == len(self.A)-1: color = 'dark orange'; bias=0
                    else           : color = 'green'

                    self.aNodes[-1].append(turtle.Turtle())
                    self.aNodes[-1][-1].shape('circle')
                    self.aNodes[-1][-1].color(color)
                    self.aNodes[-1][-1].speed(0)
                    self.aNodes[-1][-1].penup()
                    self.aNodes[-1][-1].goto(-900+(100*(18/len(self.A)))*(L),ycor-25)
                    self.aNodes[-1][-1].write('Activation: ({0},{1})'.format(L,ID),align='center',font=("Consolas", 8, "normal"))
                    self.aNodes[-1][-1].goto(-900+(100*(18/len(self.A)))*(L),ycor)
                    ID += 1
                    
                    try: sign = ((ycor**2)**0.5/ycor)
                    except: sign = 1
                    if len(self.A[L])>18: sign *= (900)/(50*(len(self.A[L])+bias))
                    if sign>0: ycor += 50 * sign 
                    ycor *= -1
                    
                if color == 'dark orange': continue
                self.bNodes.append(turtle.Turtle())
                self.bNodes[-1].shape('circle')
                self.bNodes[-1].color('lime')
                self.bNodes[-1].speed(0)
                self.bNodes[-1].penup()
                self.bNodes[-1].goto(-900+(100*(18/len(self.A)))*(L),(ycor**2)**0.5-25)
                self.bNodes[-1].write('Bias: {0}'.format(L),align='center',font=("Consolas", 8, "normal"))
                self.bNodes[-1].goto(-900+(100*(18/len(self.A)))*(L),(ycor**2)**0.5)
        else: self.wLines.clear()

        copyW = [np.copy(w) for w in self.W]
        copyB = [np.copy(b) for b in self.B]
        W = self.list(copyW)
        B = self.list(copyB)
        
        for L in range(len(W)):
            for D in range(len(W[L])):
                for S in range(len(W[L][D])):
                    
                    if W[L][D][S] > 1 or W[L][D][S] < -1:
                        W[L][D][S] = (W[L][D][S]**2)**0.5/W[L][D][S]

                    if W[L][D][S]>=0: self.wLines.color(0,0, W[L][D][S])
                    else:                      self.wLines.color(-W[L][D][S],0,0)
                    
                    self.wLines.penup()
                    self.wLines.goto(self.aNodes[L][S].xcor(),self.aNodes[L][S].ycor())
                    self.wLines.pendown()
                    self.wLines.goto(self.aNodes[L+1][D].xcor(),self.aNodes[L+1][D].ycor())
                
                if B[L][D][0] > 1 or B[L][D][0] < -1:
                        B[L][D][0] = (B[L][D][0]**2)**0.5/B[L][D][0]

                if B[L][D][0]>=0: self.wLines.color(0,0, B[L][D][0])
                else:         self.wLines.color(-B[L][D][0],0,0)
                
                self.wLines.penup()     
                self.wLines.goto(self.bNodes[L].xcor(),self.bNodes[L].ycor())
                self.wLines.pendown()
                self.wLines.goto(self.aNodes[L+1][D].xcor(),self.aNodes[L+1][D].ycor())
        turtle.update()
        if pause: turtle.exitonclick()
        try: mess.clear()
        except: pass
            

net=NeuralNetwork((4,3,3))

x = [[1,-1,1,1],
     [1,-1,-1,-1],
     [1,-1,1,-1],
     [1,-1,-1,1],
     [-1,1,1,1],
     [-1,1,-1,-1],
     [-1,1,1,-1],
     [-1,1,-1,1]]

y = [[1,0,0],[1,1,0],[1,0,1],[1,0,1],[0,1,1],[0,1,0],[0,1,1],[0,1,1]]

net.train(x, y, epochs=10000, r=1, verbose=True)
net.mapCost(x, y, ((0,0),(1,1)))
net.mapDecision(x, y, inputs=(1,2))

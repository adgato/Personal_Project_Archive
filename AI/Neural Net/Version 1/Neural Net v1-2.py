questions=["Have they bought men's deoderant? ","Have they bought women's sport-wear? ","Have they bought sport event tickets? ","Have they bought a football? "]
x=[[1,-1,1,1],[1,-1,-1,-1],[1,-1,1,-1],[1,-1,-1,1], [-1,1,1,1],[-1,1,-1,-1],[-1,1,1,-1],[-1,1,-1,1]]
a=[1,1,1,1,-1,-1,-1,-1]
b=[1,-1,-1,-1,1,-1,1,1]
c=[1,-1,-1,-1,1,-1,-1,-1]
aa=[1,-1,1,1,-1,-1,-1,-1]
y=[a,b,c]
yy=[aa]
output="interested in buying men's sport-wear"

hidden=[]
import math as m, turtle
def integration(x,weights):
    xdotw=0
    for i in range(len(weights)-1): xdotw+=x[i]*weights[i+1]
    return xdotw+weights[0]
def sigmoid(v): return 1/(1+m.exp(-v))
def update(x,w,b,i,y):
    wt=[]
    for q in range(len(x[0])): wt.append(w[len(w)-1][q]+y[i]*x[i][q])
    bt=b[len(b)-1]+y[i]
    tt=[wt,bt]
    return tt
def check(x,w,b,i):
    wt=[]
    for q in range(len(x[0])): wt.append(w[len(w)-1][q]*x[i][q])
    return sum(wt)+b[len(b)-1]
def xor(x):
    try:
     nx=[]
     for i in range(len(x)):
        dif = x[i][0]
        for q in range(len(x[i])):
            if q!=0: dif -= x[i][q]
        nx.append([int((dif**2)**0.5)])
    except Exception: nx=x**2
    return nx

def perceptron(x,y):
     xor=0
     unsucess=0
     while unsucess<2:
           w=[[0 for i in x]] #added 18/08/21 bc it didn't work without it...?
           b=[0]
           for i in range(len(x[0])):
               w[0].append([0 for i in x[0]])
               correct=0
               tries=0
               unsucess+=1
               while correct<len(x) and tries<40:
                   tries+=1
                   for i in range(len(x)):
                       if (check(x,w,b,i)<sigmoid(check(x,w,b,i)) and y[i]<0) or (check(x,w,b,i)>sigmoid(check(x,w,b,i)) and y[i]>0): correct+=1
                       else:
                           correct=0
                           new=update(x,w,b,i,y)
                           w.append(new[0])
                           b.append(new[1])
           if not(correct<len(x)):
               failure=False
               break
           if unsucess==1:
               xor=1
               x=xor(x)
           failure=True      
     if failure: print('best fit')
     w[len(w)-1].insert(0,b[len(b)-1])
     output=w[len(w)-1]
     return [output,xor]

def nlayernetwork(x,y,yy): #y = current layer yy = next layer
 weights=[]
 Fx=[]
 for i in range(len(y[0])): Fx.append([])
 for i in range(len(y)):
    weights.append(perceptron(x,y[i]))
    if xor==1: Ex=xor(x)
    else: Ex=x
    sig=[]
    for q in range(len(Ex)):
        #print(round(sigmoid(integration(Ex[q],weights[i][0]))),y[i][q])
        sig.append(sigmoid(integration(Ex[q],weights[i][0])))
    for i in range(len(sig)): Fx[i].append(sig[i])
 for i in range(len(yy)): weights.append(perceptron(Fx,yy[i]))
 return weights

heavies=nlayernetwork(x,y,yy)

print('Answer with 1 for yes and -1 for no')
person=[]
for i in range(len(questions)): person.append(int(input(questions[i])))
sig=[]
for q in range(len(heavies)-1):
    if heavies[q][1]==1: person[q]=xor(person[q])
    sig.append(sigmoid(integration(person,heavies[q][0])))
for i in range(len(sig)): hidden.append(sig[i])
print(round(sigmoid(integration(hidden,heavies[len(heavies)-1][0]))*100),"percent likley to be",output)



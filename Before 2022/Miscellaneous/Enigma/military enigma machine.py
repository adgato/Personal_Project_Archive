#28/08/21 NOTE: I've done a bit of tidying up (gaps, captial letters, tab indents and not space), apparently I was an even messier coder than now when I first started!

import random

skip = input('Skip set up? (y/n) ')
print()


#VARIABLES
alph = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'
ALPH =      [0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25] #28/08/21 lol could've used list(range(26))

therotors = [[4, 10, 12, 5, 11, 6, 3, 16, 21, 25, 13, 19, 14, 22, 24, 7, 23, 20, 18, 15, 0, 8, 1, 17, 2, 9],
             [0, 9, 3, 10, 18, 8, 17, 20, 23, 1, 11, 7, 22, 19, 12, 2, 16, 6, 25, 13, 15, 24, 5, 21, 14, 4],
             [1, 3, 5, 7, 9, 11, 2, 15, 17, 19, 23, 21, 25, 13, 24, 4, 8, 22, 6, 0, 10, 12, 20, 18, 16, 14],
             [4, 18, 14, 21, 15, 25, 9, 0, 24, 16, 20, 8, 17, 7, 23, 11, 13, 5, 19, 6, 10, 3, 2, 12, 22, 1],
             [21, 25, 1, 17, 6, 8, 19, 24, 20, 15, 18, 3, 13, 7, 11, 23, 0, 22, 12, 9, 16, 14, 5, 4, 2, 10],
             [9, 15, 6, 21, 14, 20, 12, 5, 24, 16, 1, 4, 13, 7, 25, 17, 3, 10, 0, 18, 23, 11, 8, 2, 19, 22],
             [13, 25, 9, 7, 6, 17, 2, 23, 12, 24, 18, 22, 1, 14, 20, 5, 0, 8, 21, 11, 15, 4, 10, 16, 3, 19],
             [5, 10, 16, 7, 19, 11, 23, 14, 2, 1, 9, 18, 15, 3, 25, 17, 0, 12, 4, 22, 13, 8, 20, 24, 6, 21]]

reflectorb = [24,17,20,7,16,18,11,3,15,23,13,6,14,10,12,8,4,1,5,25,2,22,21,9,0,19]

rotor       = []
plugboard   = []
cipherword  = ''
finalcipher = ''
numessage   = ''

buff = 4 #not sure why buff needs to be 4 but ok
orders = ['right','middle','left']


#SET UP
if skip == 'y':
    rotor.append(therotors[2])
    rotor.append(therotors[1])
    rotor.append(therotors[0])
    clickr = alph.index('A')
    clickm = alph.index('A')
    clickl = alph.index('A')

else:
    for q in range(3):
        
        validation = 0
        while validation == 0:
            order = input('Which rotor would you like in the ' + orders[q] + ' postion? (1-8) ')
            if order.isnumeric() and int(order) <= 8: validation = 1
                
        rotor.append(therotors[int(order)-1])

    validation = 0
    while validation == 0:
        grundstellung = input('\nWhat are the starting positions for said rotors? (e.g.AAA/FGA etc.) ')
        grundstellung = grundstellung.upper()
        if len(grundstellung) == 3 and grundstellung.isalpha(): validation = 1
        else: print('\nStarting positions must be only 3 LETTERS long')
        
    clickl = alph.index(grundstellung[0])
    clickm = alph.index(grundstellung[1])
    clickr = alph.index(grundstellung[2])
    
validation = 0
while validation == 0:
    plugboard = list(input('\nPress enter to generate a random plugboard set up, or paste a plugboard set up here: '))

    if len(plugboard) == 20: validation = 1
    else:
        plugboard = []
        showpb = ''
        
        while len(plugboard) < 20:
            choice = alph[random.choice(ALPH)]
            if choice in plugboard: continue
            else: plugboard.append(choice)

        for i in range(len(plugboard)): showpb += plugboard[i]
        print('\nPlugboard chosen:', showpb)
        ok = input('Is this plugboard ok? (y/n) ')
        if ok == 'y': validation = 1
         
validation = 0
while validation == 0:
    message = input('\nWhat is the message you would like to translate? ')
    message = message.upper()
    if message.isalpha(): validation=1
    else: print('The message must consist of letters only')
    
    
#ACTUAL STUFF
for mess in message:
    if mess in plugboard:
        if int(plugboard.index(mess))%2 == 0: numessage += plugboard[plugboard.index(mess)+1]
        else:                                 numessage += plugboard[plugboard.index(mess)-1]
    else:                                     numessage += mess

for mess in numessage:
    clickr = (clickr + 1) % 26
    if (clickr + buff) % 26 == 0: clickm = (clickm + 1) % 26

    cipher = alph.index(mess)
    for i in range(len(rotor)):
        if i == 0: cipher = (cipher + clickr) % 26
        if i == 1: cipher = (cipher + clickm) % 26
        if i == 2: cipher = (cipher + clickl) % 26

        cipher = rotor[i][cipher]
        if i == 0: cipher = (cipher - clickr) % 26
        if i == 1: cipher = (cipher - clickm) % 26
        if i == 2: cipher = (cipher - clickl) % 26

    cipher=reflectorb[cipher]

    for i in range(len(rotor)):
        if i == 2: cipher =(cipher + clickr) % 26
        if i == 1: cipher =(cipher + clickm) % 26
        if i == 0: cipher =(cipher + clickl) % 26

        for c in ALPH:
            if cipher == rotor[2-i][c]:
                cipher = c
                break

        if i == 2: cipher = (cipher - clickr) % 26
        if i == 1: cipher = (cipher - clickm) % 26
        if i == 0: cipher = (cipher - clickl) % 26

    cipherword += str(alph[cipher])
    
    if (clickm - buff) % 26 == 0:
        clickl = (clickl + 1) % 26
        clickm = (clickm + 1) % 26

for c in cipherword:
    if c in plugboard:
        if int(plugboard.index(c)) % 2 == 0: finalcipher += plugboard[plugboard.index(c)+1]
        else:                                finalcipher += plugboard[plugboard.index(c)-1]
    else:                                    finalcipher += c

print('\nYour translated message is:', finalcipher)

from glob import glob
import random

train = open('data/trainData.csv','a+')
test = open('data/testData.csv','a+')


for i in glob('data/*.png'):
    
    if random.randint(0,9): train.write(i + ', ' + i[5] + '\n')
    else:                   test.write(i + ', ' + i[5] + '\n')

    
train.close()
test.close()

train = open('data/trainDataSmall.csv','a+')
test = open('data/testDataSmall.csv','a+')

c = 0
for i in glob('data/*.png'):
    if i[5] == '1':
        if random.randint(0,9): train.write(i + ', ' + i[5] + '\n')
        else:                   test.write(i + ', ' + i[5] + '\n')
        c += 1

for i in glob('data/*.png'):
    if i[5] != '1':
        if random.randint(0,9): train.write(i + ', ' + i[5] + '\n')
        else:                   test.write(i + ', ' + i[5] + '\n')
        c -= 1
        
    if c == 0: break

    
train.close()
test.close()

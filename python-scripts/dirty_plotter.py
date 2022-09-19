import os
import csv
import matplotlib.pyplot as plt
import numpy as np

data_path = r'C:\Users\lucad\NeAF\data'

filepath = data_path + '\\level.csv'
if not os.path.isfile(filepath):
    print("File not found! Exiting.")
    exit()

dbs = []

with open(filepath, newline='') as csvfile:
    csvreader = csv.reader(csvfile, delimiter=',')
    for row in csvreader:
        row = [float(e) for e in row]
        dbs.append(row)

for direc in dbs:
    plt.plot(direc)
plt.show()

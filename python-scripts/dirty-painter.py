import os
import csv
import matplotlib.pyplot as plt

data_path = r'C:\Users\lucad\NeAF\data'

filepath = data_path + '\\paint_me.csv'
if not os.path.isfile(filepath):
    print("File not found! Exiting.")
    exit()

dbs = []

with open(filepath, newline='') as csvfile:
    csvreader = csv.reader(csvfile, delimiter=',')
    for row in csvreader:
        row = [float(e) for e in row]
        dbs.append(row)

print(dbs)

imgplot = plt.imshow(dbs, cmap='gray', vmin=0, vmax=1)
plt.show()


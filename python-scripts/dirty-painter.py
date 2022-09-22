import os
import csv
import matplotlib.pyplot as plt
import matplotlib

data_path = r'C:\Users\lucad\NeAF\data'
if not os.path.isdir(data_path):
    data_path = r'C:\Users\lucad\Repos\NeAF-Audio-Simulator\data'


filepath = data_path + '\\paint_me.csv'
if not os.path.isfile(filepath):
    print("File not found! Exiting.")
    exit()

dbs = []
matplotlib.use('tkagg')
print(matplotlib.get_backend())

with open(filepath, newline='') as csvfile:
    csvreader = csv.reader(csvfile, delimiter=',')
    for row in csvreader:
        row = [float(e) for e in row]
        dbs.append(row)

print(dbs)

imgplot = plt.imshow(dbs, cmap='gray', vmin=0.6, vmax=0.8)
plt.show()


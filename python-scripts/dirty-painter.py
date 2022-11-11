import os
import csv
import matplotlib.pyplot as plt
import matplotlib
from argparse import ArgumentParser

parser = ArgumentParser()
parser.add_argument('-f', '--file')
args = parser.parse_args()

data_path = r'C:\Users\lucad\NeAF\data'
if not os.path.isdir(data_path):
    data_path = r'C:\Users\lucad\Repos\NeAF-Audio-Simulator\global_data'
print(f"Data path is {data_path}!")

if args.file is not None:
    filename = args.file
else:
    filename = 'paint_me.csv'
print(f"filename is {filename}")
filepath = data_path + '\\' + filename
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

# imgplot = plt.imshow(dbs, cmap='gray', vmin=0.6, vmax=0.8)
imgplot = plt.imshow(dbs, cmap='gray')
plt.show()


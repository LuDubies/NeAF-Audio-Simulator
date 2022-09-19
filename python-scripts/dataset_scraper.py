import os
import json
data_path = r'C:\Users\lucad\NeAF\data'
header_fileext = '.json'
recording_fileext = '-samples.bin'

files = os.listdir(data_path)

files.sort(reverse=True)
headers = list(filter(lambda x: 'samples' not in x, files))
recordings = list(filter(lambda x: 'samples' in x, files))


# throw away headers without recordings and vice versa
headers = [h for h in headers if [r for r in recordings if h.removesuffix(header_fileext) in r]]
recordings = [r for r in recordings if [h for h in headers if r.removesuffix(recording_fileext) in h]]

files = list(zip(headers, recordings))
print(files)

for h, r in files:
    filepath = data_path + '\\' + h
    print(os.path.isfile(filepath))
    with open(filepath, 'r') as fp:
        head = json.load(fp)
        print(head["Version"])
        print(head["Recordings"])



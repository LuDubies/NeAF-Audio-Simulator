import h5py
import numpy as np
from progress.bar import Bar
import shutil
import argparse as ap


def create_target_sofa(file, orig, tasks):
    shutil.copy2(orig, file)
    with h5py.File(file, 'r+') as f, h5py.File(orig, 'r') as o:

        ir = f['Data.IR']
        iro = o['Data.IR']
        sp = f['SourcePosition']

        if 'zero' in tasks:
            bar = Bar("Zeroing IR", max=ir.shape[0])
            for m in range(ir.shape[0]):
                ir[m, 0, :] = np.zeros(ir.shape[2])
                ir[m, 1, :] = np.zeros(ir.shape[2])
                bar.next()
            bar.finish()
        if 'trim' in tasks:
            backward = [(i, list(p)) for i, p in enumerate(sp) if p[0] < 0.44]
            print(backward)
            print(f"{len(backward)} sources ({(len(backward)/ir.shape[0]) * 100:.2f}%)")
            bar = Bar("Trimming HRTF", max=ir.shape[0])
            for m in range(ir.shape[0]):
                if m in [i for (i, p) in backward]:
                    ir[m, 0, :] = np.zeros(ir.shape[2])
                    ir[m, 1, :] = np.zeros(ir.shape[2])
                bar.next()
            bar.finish()
        if 'filter' in tasks:
            front = np.array([1, 0, 0])
            frontiness = np.power(np.clip(np.dot(sp, front), 0, 1), 3)
            bar = Bar("Building Filter", max=ir.shape[0])
            for m in range(ir.shape[0]):
                ir[m, 0, :] = ir[m, 0, :] * frontiness[m]
                ir[m, 1, :] = ir[m, 1, :] * frontiness[m]
                bar.next()
            bar.finish()

        f.flush()


if __name__ == "__main__":
    SOFA_PATH = r'../Assets/StreamingAssets/'

    parser = ap.ArgumentParser()
    parser.add_argument('filename', default='NEW_HRTF.sofa')
    parser.add_argument('-z', '--zero', action='store_true')
    parser.add_argument('-t', '--trim')
    parser.add_argument('-f', '--filter', action='store_true')
    parser.add_argument('-o', '--original', default='MRT01.sofa')
    args = parser.parse_args()
    filename = args.filename
    original = args.original
    options = []
    if args.zero: options.append('zero')
    if args.filter: options.append('filter')
    if args.trim is not None:
        options.append('trim')
    create_target_sofa(SOFA_PATH + filename, SOFA_PATH + original, options)

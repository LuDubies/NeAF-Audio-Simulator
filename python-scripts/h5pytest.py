import h5py
import numpy as np
from progress.bar import Bar
import shutil
import argparse as ap


def create_target_sofa(file, orig, options, parameter):
    shutil.copy2(orig, file)
    with h5py.File(file, 'r+') as f, h5py.File(orig, 'r') as o:

        ir = f['Data.IR']
        iro = o['Data.IR']
        sp = f['SourcePosition']

        # ensure sampling rate is 48000
        sr = f['Data.SamplingRate']
        sr[0] = 48000

        if 'zero' in options:
            bar = Bar("Zeroing IR", max=ir.shape[0])
            for m in range(ir.shape[0]):
                ir[m, 0, :] = np.zeros(ir.shape[2])
                ir[m, 1, :] = np.zeros(ir.shape[2])
                bar.next()
            bar.finish()
        if 'default' in options:
            bar = Bar("Trying to build default IR", max=ir.shape[0])
            for m in range(ir.shape[0]):
                new_ir = np.zeros(ir.shape[2])
                new_ir[0] = 1
                ir[m, 0, :] = new_ir
                ir[m, 1, :] = new_ir
                bar.next()
            bar.finish()
        if 'trim' in options:
            backward = [(i, list(p)) for i, p in enumerate(sp) if p[0] < param]
            print(backward)
            print(f"{len(backward)} sources ({(len(backward)/ir.shape[0]) * 100:.2f}%)")
            bar = Bar("Trimming HRTF", max=ir.shape[0])
            for m in range(ir.shape[0]):
                if m in [i for (i, p) in backward]:
                    ir[m, 0, :] = np.zeros(ir.shape[2])
                    ir[m, 1, :] = np.zeros(ir.shape[2])
                bar.next()
            bar.finish()
        if 'filter' in options:
            front = np.array([1, 0, 0])
            #dots = np.clip(np.dot(sp / np.linalg.norm(sp), front), 0, 1)
            dots = np.clip(np.dot(sp, front), 0, 1)
            print(f"dot-prods are between {np.min(dots)} and {np.max(dots)}")
            frontiness = np.exp(((np.square(dots) - 1) / float(param)))
            print(f"factors are between {np.min(frontiness)} and {np.max(frontiness)}")
            print(f"Building filter with sigma {param}.")
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
    parser.add_argument('-d', '--default', action='store_true')
    parser.add_argument('-t', '--trim')
    parser.add_argument('-f', '--filter')
    parser.add_argument('-o', '--original', default='my_default.sofa')
    args = parser.parse_args()
    filename = args.filename
    original = args.original
    operations = []
    param = None
    if args.zero:
        operations.append('zero')
    if args.default:
        operations.append('default')
    if args.filter is not None:
        operations.append('filter')
        param = args.filter
    if args.trim is not None:
        operations.append('trim')
        param = args.trim
    print(param)
    create_target_sofa(SOFA_PATH + filename, SOFA_PATH + original, operations, param)

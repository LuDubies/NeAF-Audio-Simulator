import h5py
import numpy as np
from progress.bar import Bar
import shutil


def main(filename, original, option):
    shutil.copy2(original, filename)
    with h5py.File(filename, 'r+') as f, h5py.File(original, 'r') as o:

        ir = f['Data.IR']
        iro = o['Data.IR']
        sp = f['SourcePosition']

        if option == 'zero':
            bar = Bar("Zeroing IR", max=ir.shape[0])
            for m in range(ir.shape[0]):
                ir[m, 0, :] = np.zeros(ir.shape[2])
                ir[m, 1, :] = np.zeros(ir.shape[2])
                bar.next()
            bar.finish()
        if option == 'cone':
            forward = [(i, list(p)) for i, p in enumerate(sp) if p[0] > 0.5]
            print(forward)
            print(f"{len(forward)} sources ({(len(forward)/ir.shape[0]) * 100:.2f}%)")
            bar = Bar("Building Cone", max=ir.shape[0])
            for m in range(ir.shape[0]):
                if m in [i for (i, p) in forward]:
                    ir[m, 0, :] = iro[m, 0, :]
                    ir[m, 1, :] = iro[m, 1, :]
                bar.next()
            bar.finish()
        if option == 'trim':
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
        f.flush()


if __name__ == "__main__":
    SOFA_PATH = r'../Assets/StreamingAssets/'
    FILENAME = 'TRIM_044.sofa'
    ORIGINAL = SOFA_PATH + 'MRT01.sofa'
    OPTION = 'trim'
    main(SOFA_PATH + FILENAME, ORIGINAL, OPTION)

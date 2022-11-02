import h5py
import numpy as np
from progress.bar import Bar


def main(filename, option):
    with h5py.File(filename, 'r+') as f:
        print(f.keys())
        ir = f['Data.IR']
        print(ir.shape)
        print(ir.dtype)
        print(type(ir))

        bar = Bar("Editing HDF5", max=ir.shape[0])

        if option == 'zero':
            for m in range(ir.shape[0]):
                for n in range(ir.shape[2]):
                    ir[m, 0, n] = 0.0
                    ir[m, 1, n] = 0.0
                bar.next()

        f.flush()
        bar.finish()


if __name__ == "__main__":
    FILENAME = r'../Assets/StreamingAssets/ZEROED.sofa'
    OPTION = 'zero'
    main(FILENAME, OPTION)

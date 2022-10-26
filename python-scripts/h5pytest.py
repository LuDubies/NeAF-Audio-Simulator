import h5py
import numpy as np


def main():
    with h5py.File(r'../Assets/StreamingAssets/MANIP02.sofa', 'r+') as f:
        print(f.keys())
        ir = f['Data.IR']
        print(ir.shape)
        print(ir.dtype)
        print(type(ir))
        for n in range(ir.shape[2]):
            ir[10, 0, n] = 0.0
            ir[10, 1, n] = 0.0
        f.flush()
        #YYYYYYYYYYYYYEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEESSSSSSSSS


if __name__ == "__main__":
    main()

import sofa
import numpy as np

# TODO parameterize
TARGET_SOFA = r'../Assets/StreamingAssets/MANIP02.sofa'
FRONT_SOURCE_INDEX = 13

def main():
    hrtf = sofa.Database.open(TARGET_SOFA)

    complete_ir = hrtf.Data.IR.get_values()

    # indexing is Source/Measurement, Receiver, N
    to_fix = []

    for idx, val in np.ndenumerate(complete_ir):
        if idx[0] != FRONT_SOURCE_INDEX and val != 0:
            to_fix.append(idx)

    if not to_fix:
        print("SOFA file is clear")
    else:
        print(f"{len(to_fix):,} cells need editing!")
        if len(to_fix) < 100:
            print(f"{to_fix}")


if __name__ == "__main__":
    main()

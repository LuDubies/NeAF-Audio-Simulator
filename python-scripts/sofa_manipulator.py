import sofa
import numpy as np


TARGET_SOFA = r'./hrtf/MRT01_manip.sofa'


def main():
    hrtf = sofa.Database.open(TARGET_SOFA)
    hrtf.Metadata.dump()
    hrtf.Dimensions.dump()
    hrtf.Variables.dump()

    try:

        saved_positions = hrtf.Source.Position.get_values(system="cartesian")

        # inspect source
        forward_positions = [pos for pos in saved_positions if -0.0001 < pos[1] < 0.0001 and pos[0] > 0]
        print(len(forward_positions))
        print(forward_positions)

        measurement_indices = [(idx, pos) for idx, pos in enumerate(saved_positions) if
                               pos[0] > 0.99 and abs(pos[1]) < 0.001 and abs(pos[2]) < 0.001]
        if len(measurement_indices) > 1:
            raise Exception(f"{len(measurement_indices)} is invalid measurement count for FRONT")
        print(len(measurement_indices))

        hrtf.save()
    except Exception as e:  ## sofa raises plain Exception...
        print(f"Unsuccessful, raised \"{e}\"")
        # raise e
    finally:
        hrtf.close()
        print("Done")


if __name__ == "__main__":
    main()

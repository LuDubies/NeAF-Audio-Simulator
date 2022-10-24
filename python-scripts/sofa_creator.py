import sofa
import numpy as np

SOURCE_SOFA = r'./hrtf/MRT01.sofa'
TARGET_SOFA = r'./hrtf/front_filter.sofa'


def main():
    hrtf = sofa.Database.open(SOURCE_SOFA)

    # create own HRTF
    measurements = hrtf.Dimensions.M
    data_length = hrtf.Dimensions.N
    max_string_length = hrtf.Dimensions.S
    receivers = hrtf.Dimensions.R

    hrir = sofa.Database.create(TARGET_SOFA, "SimpleFreeFieldHRIR",
                                dimensions={"M": measurements, "N": data_length, "S": max_string_length})
    try:
        hrir.Listener.initialize(fixed=["Position", "View", "Up"])
        hrir.Source.initialize(variances=["Position"])
        hrir.Receiver.initialize(fixed=["Position"], count=receivers)
        hrir.Emitter.initialize(fixed=["Position"])

        hrir.Data.initialize()

        hrir.Data.SamplingRate = 44100.

        hrir.Room.Type = "free field"
        hrir.Room.create_attribute("Location", "fake location")
        hrir.Room.Description = "fake room"

        print("\n--------------------------------------------\n")


        # add source positions
        saved_positions = hrtf.Source.Position.get_values(system="cartesian")

        # inspect source
        forward_positions = [pos for pos in saved_positions if -0.0001 < pos[1] < 0.0001 and pos[0] > 0]
        print(len(forward_positions))
        print(forward_positions)

        hrir.Source.Position.set_values(saved_positions, system='cartesian')

        # only set IR for front position
        (front_idx, front_pos) = max(enumerate(saved_positions), key=lambda ip: ip[1][0])
        print(f"FRONT at {front_pos} with index {front_idx}.")

        for m in range(hrir.Dimensions.M):
            ir0 = hrtf.Data.IR.get_values(indices={"M": m, "R": 0, "E": 0})
            ir1 = hrtf.Data.IR.get_values(indices={"M": m, "R": 1, "E": 0})
            if m != front_idx:
                ir0.fill(0.)
                ir1.fill(0.)
            else:
                print("leaving front measurement intact")
            hrir.Data.IR.set_values(ir0, indices={"M": m, "R": 0, "E": 0})
            hrir.Data.IR.set_values(ir1, indices={"M": m, "R": 1, "E": 0})

        hrir.save()

        print("\n--------------------------------------------\n")
        print(hrir.Variables.list_variables())
        print(hrtf.Variables.list_variables())
        for variable in hrtf.Variables.list_variables():
            print(f"{variable}: -- {getattr(hrir, variable).get_values()} -- {getattr(hrtf, variable).get_values()}")

    except Exception as e:  # sofa raises plain Exception...
        print(f"Unsuccessful, raised \"{e}\"")
        # raise e
    finally:
        hrtf.close()
        hrir.close()
        print("Done")


if __name__ == "__main__":
    main()

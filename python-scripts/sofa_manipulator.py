import sofa
import numpy as np

SOURCE_SOFA = r'./hrtf/MRT01.sofa'
TARGET_SOFA = r'./hrtf/front_filter.sofa'


def main():
    hrtf = sofa.Database.open(SOURCE_SOFA)
    hrtf.Metadata.dump()
    hrtf.Dimensions.dump()
    hrtf.Variables.dump()

    # create own HRTF
    measurements = hrtf.Dimensions.M
    data_length = hrtf.Dimensions.N
    max_string_length = 128
    receivers = hrtf.Dimensions.R

    hrir = sofa.Database.create(TARGET_SOFA, "SimpleFreeFieldHRIR",
                                dimensions={"M": measurements, "N": data_length, "S": max_string_length})
    try:
        hrir.Listener.initialize(fixed=["Position", "View", "Up"])
        hrir.Source.initialize(variances=["Position", "View"])
        hrir.Receiver.initialize(fixed=["Position"], count=receivers)
        hrir.Emitter.initialize(fixed=["Position"])

        hrir.Data.initialize(variances=["Delay"])

        hrir.Room.Type = "shoebox"
        hrir.Room.create_attribute("Location", "fake location")
        hrir.Room.create_variable("Temperature", ("M",))
        hrir.Room.Temperature.Units = "celsius"
        hrir.Room.Temperature = 28
        hrir.Room.create_string_array("Description", ("M", "S"))

        print("Attributes and metadata")
        hrir.Metadata.dump()

        print("Dimensions")
        hrir.Dimensions.dump()

        print("Variables")
        hrir.Variables.dump()

        # TODO #
        # 1 - filter source positions for forward cone + cleanup coordinates
        # 2 - add source positions including "fixed" ones
        # 3 - add IR to forward cone for one receiver
        #

        # -- 1 --

        def cleaner(position):
            # set sources directly on the x,z-plane if they are in front of the listener
            if -0.0001 < position[1] < 0.0001 and position[0] > 0:
                position[1] = 0
                if -0.05 < position[2] < 0.05:
                    position[2] = 0
                    position[0] = 1

        saved_positions = hrtf.Source.Position.get_values(system="cartesian")

        for pos in saved_positions:
            cleaner(pos)

        # inspect source
        forward_positions = [pos for pos in saved_positions if -0.0001 < pos[1] < 0.0001 and pos[0] > 0]
        print(len(forward_positions))
        print(forward_positions)

        # -- 2 --
        hrir.Source.Position.set_values(saved_positions, system='cartesian')

        # -- 3 --
        measurement_indices = [(idx, pos) for idx, pos in enumerate(saved_positions) if pos[0] > 0.99 and abs(pos[1]) < 0.001 and abs(pos[2]) < 0.001]
        if len(measurement_indices) > 1:
            raise Exception("Too many valid measurements for FRONT")
        front_measurement = measurement_indices[0][0]
        front_ir = hrtf.Data.IR.get_values(indices={"M": front_measurement, "R": 0, "E": 0})
        hrir.Data.IR.set_values(front_ir, indices={"M": front_measurement, "R": 0, "E": 0})

        hrir.save()
    except Exception as e:  ## sofa raises plain Exception...
        print(f"Unsuccessful, raised {e}")
        hrtf.close()
        hrir.close()
        # raise e
    finally:
        hrtf.close()
        hrir.close()
        print("Done")


if __name__ == "__main__":
    main()

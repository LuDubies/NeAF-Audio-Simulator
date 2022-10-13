import sofa
import matplotlib.pyplot as plt
import matplotlib
from mpl_toolkits import mplot3d
import numpy as np


def plot_coordinates(coords, title):
    x0 = coords
    n0 = coords
    fig = plt.figure(figsize=(15, 15))
    ax = fig.add_subplot(111, projection='3d')
    q = ax.quiver(x0[:, 0], x0[:, 1], x0[:, 2], n0[:, 0],
                  n0[:, 1], n0[:, 2], length=0.1)
    plt.xlabel('x (m)')
    plt.ylabel('y (m)')
    plt.title(title)
    return q

matplotlib.use('tkagg')


HRTF_path = r'./hrtf/MRT01.sofa'
HRTF = sofa.Database.open(HRTF_path)
HRTF.Metadata.dump()

HRTF.Dimensions.dump()
HRTF.Variables.dump()

# plot Source positions
source_positions = HRTF.Source.Position.get_values(system="cartesian")
plot_coordinates(source_positions, 'Source positions')


# plot Data.IR at M=5 for E=0
measurement = 2000
emitter = 0
legend = []

# print some stuff
print(f'Sampling Rate: {HRTF.Data.SamplingRate.get_values(indices={"M": measurement})}')

t = np.arange(0, HRTF.Dimensions.N)*HRTF.Data.SamplingRate.get_values(indices={"M": measurement})

plt.figure(figsize=(15, 5))
for receiver in np.arange(HRTF.Dimensions.R):
    plt.plot(t, HRTF.Data.IR.get_values(indices={"M": measurement, "R": receiver, "E": emitter}))
    legend.append('Receiver {0}'.format(receiver))
plt.title('HRIR at M={0} for emitter {1}'.format(measurement, emitter))
plt.legend(legend)
plt.xlabel('$t$ in s')
plt.ylabel(r'$h(t)$')
plt.grid()

saved_measurement = HRTF.Data.IR.get_values(indices={"M": measurement, "R": 0, "E": emitter})
HRTF.close()

# plt.show()

# create own HRTF

HRIR_path = r'./hrtf/selfmade.sofa'
measurements = 5
data_length = 256
max_string_length = 128
receivers = 2

HRIR = sofa.Database.create(HRIR_path, "SimpleFreeFieldHRIR",
                            dimensions={"M": measurements, "N": data_length, "S": max_string_length})
HRIR.Listener.initialize(fixed=["Position", "View", "Up"])
HRIR.Source.initialize(variances=["Position", "View"])
HRIR.Receiver.initialize(fixed=["Position"], count=receivers)
HRIR.Emitter.initialize(fixed=["Position"])

HRIR.Data.initialize(variances=["Delay"])


HRIR.Room.Type = "shoebox"
HRIR.Room.initialize(variances=["CornerA", "CornerB"])
HRIR.Room.create_attribute("Location", "various recording locations")
HRIR.Room.create_variable("Temperature", ("M",))
HRIR.Room.Temperature.Units = "celsius"
HRIR.Room.Temperature = 28
HRIR.Room.create_string_array("Description", ("M", "S"))

print("Attributes and metadata")
HRIR.Metadata.dump()

print("Dimensions")
HRIR.Dimensions.dump()

print("Variables")
HRIR.Variables.dump()

# enter measurement from other hrtf
HRIR.Data.IR.set_values(saved_measurement, indices={"M": 1, "R": 0, "E": 0})

# plot new creation
source_positions = HRIR.Source.Position.get_values(system="cartesian")
plot_coordinates(source_positions, 'Source positions')

measurement = 1
legend = []

# print some stuff
print(f'Sampling Rate: {HRIR.Data.SamplingRate.get_values(indices={"M": measurement})}')

t = np.arange(0, HRIR.Dimensions.N)*HRIR.Data.SamplingRate.get_values(indices={"M": measurement})

plt.figure(figsize=(15, 5))
for receiver in np.arange(HRIR.Dimensions.R):
    plt.plot(t, HRIR.Data.IR.get_values(indices={"M": measurement, "R": receiver, "E": emitter}))
    legend.append('Receiver {0}'.format(receiver))
plt.title('HRIR at M={0} for emitter {1}'.format(measurement, emitter))
plt.legend(legend)
plt.xlabel('$t$ in s')
plt.ylabel(r'$h(t)$')
plt.grid()

HRIR.save()
HRIR.close()

plt.show()

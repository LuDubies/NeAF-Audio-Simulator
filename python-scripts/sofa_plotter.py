import sofa
import matplotlib.pyplot as plt
import matplotlib.colors as clrs
import matplotlib
from mpl_toolkits import mplot3d
import numpy as np
from argparse import ArgumentParser


SOFA_DIR = r'../Assets/StreamingAssets/'


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


def main():

    parser = ArgumentParser()
    parser.add_argument('filename')
    args = parser.parse_args()

    matplotlib.use('tkagg')

    HRTF = sofa.Database.open(SOFA_DIR + args.filename)

    # plot Source positions
    source_positions = HRTF.Source.Position.get_values(system="cartesian")
    plot_coordinates(source_positions, 'Source positions')

    measurement = 1
    legend = []

    t = np.arange(0, HRTF.Dimensions.N)*HRTF.Data.SamplingRate.get_values(indices={"M": measurement})

    plt.figure(figsize=(15, 5))
    for receiver in np.arange(HRTF.Dimensions.R):
        plt.plot(t, HRTF.Data.IR.get_values(indices={"M": measurement, "R": receiver, "E": 0}))
        legend.append('Receiver {0}'.format(receiver))
    plt.title('HRIR at M={0} for emitter {1}'.format(measurement, 0))
    plt.legend(legend)
    plt.xlabel('$t$ in s')
    plt.ylabel(r'$h(t)$')
    plt.grid()

    HRTF.close()

    plt.show()

if __name__ == "__main__":
    main()

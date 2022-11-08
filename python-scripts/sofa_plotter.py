import sofa
import matplotlib.pyplot as plt
import matplotlib.colors as clrs
import matplotlib
from mpl_toolkits import mplot3d
import numpy as np


SOURCE_SOFA = r'../Assets/StreamingAssets/TRIM_04.sofa'


def plot_coordinates(coords, title):
    x0 = coords
    n0 = coords

    # color mapping the different arrows
    colors = []
    marker_col = clrs.BASE_COLORS['r']
    neutral_col = clrs.BASE_COLORS['b']
    for c in x0:
        if abs(c[1]) < 0.01:
            colors.append(marker_col)
        else:
            colors.append(neutral_col)
    print(f"Marked arrow count is {len([col for col in colors if col == marker_col])}")

    fig = plt.figure(figsize=(15, 15))
    ax = fig.add_subplot(111, projection='3d')
    q = ax.quiver(x0[:, 0], x0[:, 1], x0[:, 2], n0[:, 0],
                  n0[:, 1], n0[:, 2], color=colors, length=0.1)
    plt.xlabel('x (m)')
    plt.ylabel('y (m)')
    plt.title(title)
    return q

def main():

    matplotlib.use('tkagg')

    HRTF = sofa.Database.open(SOURCE_SOFA)

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

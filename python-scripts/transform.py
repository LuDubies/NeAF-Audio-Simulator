import numpy as np
from argparse import ArgumentParser
import os
import json


def rotmat(a, b):
    a, b = a / np.linalg.norm(a), b / np.linalg.norm(b)
    v = np.cross(a, b)
    c = np.dot(a, b)
    # handle exception for the opposite direction input
    if c < -1 + 1e-10:
        return rotmat(a + np.random.uniform(-1e-2, 1e-2, 3), b)
    s = np.linalg.norm(v)
    kmat = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]])
    return np.eye(3) + kmat + kmat.dot(kmat) * ((1 - c) / (s ** 2 + 1e-10))


def qvec2rotmat(qvec):
    return np.array([
        [
            1 - 2 * qvec[2] ** 2 - 2 * qvec[3] ** 2,
            2 * qvec[1] * qvec[2] - 2 * qvec[0] * qvec[3],
            2 * qvec[3] * qvec[1] + 2 * qvec[0] * qvec[2]
        ], [
            2 * qvec[1] * qvec[2] + 2 * qvec[0] * qvec[3],
            1 - 2 * qvec[1] ** 2 - 2 * qvec[3] ** 2,
            2 * qvec[2] * qvec[3] - 2 * qvec[0] * qvec[1]
        ], [
            2 * qvec[3] * qvec[1] - 2 * qvec[0] * qvec[2],
            2 * qvec[2] * qvec[3] + 2 * qvec[0] * qvec[1],
            1 - 2 * qvec[1] ** 2 - 2 * qvec[2] ** 2
        ]
    ])


def transform_transforms(s, t, use_colmap):
    transforms = []
    out = dict()
    target_file = ""
    with open(s, 'r') as tf:
        target_file = os.path.join(os.path.dirname(os.path.realpath(tf.name)), t)
        frame_details = tf.readlines()
        frame_details = [fd.replace(',', '.') for fd in frame_details]  # in case c# decides to write ,
        camera_parameters = frame_details[0]
        frame_details = frame_details[1::]

        # get camera parameters
        _, w, h, cax, cay = tuple(camera_parameters.split())
        out.update({
            "camera_angle_x": float(cax),
            "camera_angle_y": float(cay),
            "w": int(w),
            "h": int(h),
            "fl_x": 1375.52,
            "fl_y": 1374.49,
            "k1": 0.0578421,
            "k2": -0.0805099,
            "p1": -0.000980296,
            "p2": 0.00015575,
            "cx": 554.558,
            "cy": 965.268,
            "aabb_scale": 4,
        })

        # get the parameteers for all pictures
        for frame in frame_details:
            vals = frame.split(" ")
            qvec = np.array(tuple(map(float, vals[1:5])))
            tvec = np.array(tuple(map(float, vals[5:8])))

            if use_colmap:
                bottom = np.array([0.0, 0.0, 0.0, 1.0]).reshape([1, 4])
                up = np.zeros(3)
                R = qvec2rotmat(-qvec)
                t = tvec.reshape([3, 1])
                m = np.concatenate([np.concatenate([R, t], 1), bottom], 0)
                c2w = np.linalg.inv(m)
                c2w[0:3, 2] *= -1  # flip the y and z axis
                c2w[0:3, 1] *= -1
                c2w = c2w[[1, 0, 2, 3], :]  # swap y and z
                c2w[2, :] *= -1  # flip whole world upside down

                up += c2w[0:3, 1]
                up = up / np.linalg.norm(up)
                R = rotmat(up, [0, 0, 1])  # rotate up vector to [0,0,1]
                R = np.pad(R, [0, 1])
                R[-1, -1] = 1

                c2w = np.matmul(R, c2w)
                transforms.append({"file_path": "capture-" + vals[0] + ".png", "transform_matrix": c2w})
            else:
                bottom = np.array([0.0, 0.0, 0.0, 1.0]).reshape([1, 4])
                R = qvec2rotmat(qvec)
                t = tvec.reshape([3, 1])
                t = t[[2, 0, 1], :]

                # Rotations
                R = np.matmul(rotmat([0, 1, 0], [0, 0, 1]), R)  # rotate camera y down
                R = np.matmul(rotmat([0, 1, 0], [1, 0, 0]), R)  # rotate camera to face center

                m = np.concatenate([np.concatenate([R, t], 1), bottom], 0)

                transforms.append({"file_path": "capture-" + vals[0] + ".png", "transform_matrix": m})

    out.update({"frames": transforms})
    for f in out["frames"]:
        f["transform_matrix"] = f["transform_matrix"].tolist()

    with open(target_file, 'w') as target:
        json.dump(out, target, indent=2)


if __name__ == "__main__":
    parser = ArgumentParser()
    parser.add_argument('-s', '--source', default='transforms.txt')
    parser.add_argument('-t', '--target', default='transforms.json')
    parser.add_argument('-c', '--colmap', action='store_true')
    args = parser.parse_args()

    transform_transforms(args.source, args.target, args.colmap)



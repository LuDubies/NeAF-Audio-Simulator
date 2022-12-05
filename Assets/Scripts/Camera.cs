using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class Camera {
    private Vector3 origin;
    private Vector3 direction;

    private PictureMode picmode;

    private int width;
    private int height;

    private float[] image;
    private System.DateTime timestamp;
    private int saves;

    public Camera(Vector3 origin, Vector3 direction, int width, int height, PictureMode pm) {
        this.origin = origin;
        this.direction = direction;
        this.width = width;
        this.height = height;
        this.picmode = pm;
        image = new float[width * height];
        timestamp = System.DateTime.Now;
        saves = 0;
    }

    public Vector3 getPosition(int w, int h, float precision) {
        if (picmode == PictureMode.Translated) {
            return origin +
            (width / (float)2 - w - 0.5f * precision) * Vector3.Cross(direction, Vector3.up).normalized * precision +
            (height / (float)2 - h - 0.5f * precision) * Vector3.up * precision;
        } else {
            return origin;
        }
    }

    public Quaternion getRotation(int w, int h) {
        if (picmode == PictureMode.Translated) {
            return Quaternion.LookRotation(direction);
        } else {
            float y_angle = -(width / 2) + w;
            float x_angle = (height / 2) - h;
            return Quaternion.LookRotation(Quaternion.Euler(x_angle, y_angle, 0) * direction);
        }
    }
    public Vector3 getNPosition(int n, float precision) {
        return getPosition(n % width, n / width, precision);
    }
    public Quaternion getNRotation(int n) {
        return getRotation(n % width, n / width);
    }

    public void setOrigin(Vector3 origin) { this.origin = origin; }
    public Vector3 getOrigin() { return this.origin; }
    public void setDirection(Vector3 direction) { this.direction = direction; }
    public Quaternion getDirection() { return Quaternion.LookRotation(this.direction); }
    public void setWidthHeight(int width, int height) { this.width = width; this.height = height; image = new float[width * height]; }
    public void setPixel(int width, int height, float val) { image[width + height * this.width] = val; }
    public void setNPixel(int n, float val) { image[n] = val; }
    public void setPictureMode(PictureMode pm) { this.picmode = pm; }

    public void saveImage() {
        string dataDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../data/"));
        dataDir = Path.Combine(dataDir, timestamp.ToString("MM-ddTHH-mm-ss"));
        string transformPath = Path.Combine(dataDir, "transforms.txt");
        if(!Directory.Exists(dataDir)) {
            Directory.CreateDirectory(dataDir);
            File.AppendAllText(transformPath, $"camera {width} {height} {(width/(float)360) * 2 * Mathf.PI} {height/(float)360 * 2 * Mathf.PI}" + Environment.NewLine);
        }
        string filename = $"capture-{++saves}" + ".png";
        Debug.Log($"Saving image to {dataDir} as {filename}.");
        Texture2D imageTex = new Texture2D(width, height);
        var colorMap = image.Select(x => Color.Lerp(Color.black, Color.white, x));
        imageTex.SetPixels(colorMap.ToArray());
        File.WriteAllBytes(Path.Combine(dataDir, filename), imageTex.EncodeToPNG());
        Quaternion rotation = Quaternion.LookRotation(direction);
        // save rotation (Quat) and position (Vec3)
        File.AppendAllText(transformPath, $"{saves} {rotation.w} {rotation.x} {rotation.y} {rotation.z} {origin.x} {origin.y} {origin.z}" + Environment.NewLine);
    }
}


using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Measurer))]
[CanEditMultipleObjects]
public class MeasurerEditor : Editor
{
    /*
    override public void OnInspectorGUI()
    {
        var measurer = target as Measurer;

        measurer.debugMode = EditorGUILayout.Toggle("Debug Mode", measurer.debugMode);
    }
    */
}

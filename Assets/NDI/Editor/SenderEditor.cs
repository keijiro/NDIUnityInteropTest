using UnityEngine;
using UnityEditor;

namespace NDI.Editor {

[CanEditMultipleObjects]
[CustomEditor(typeof(Sender))]
sealed class SenderEditor : UnityEditor.Editor
{
    SerializedProperty _ndiName;
    SerializedProperty _enableAlpha;
    SerializedProperty _captureMethod;
    SerializedProperty _sourceCamera;
    SerializedProperty _sourceTexture;

    static class Styles
    {
        public static Label NdiName = "NDI Name";
    }

    void OnEnable()
    {
        var finder = new PropertyFinder(serializedObject);
        _ndiName = finder["_ndiName"];
        _enableAlpha = finder["_enableAlpha"];
        _captureMethod = finder["_captureMethod"];
        _sourceCamera = finder["_sourceCamera"];
        _sourceTexture = finder["_sourceTexture"];
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_ndiName, Styles.NdiName);
        EditorGUILayout.PropertyField(_enableAlpha);

        EditorGUILayout.PropertyField(_captureMethod);

        EditorGUI.indentLevel++;

        if (_captureMethod.hasMultipleDifferentValues ||
            _captureMethod.enumValueIndex == (int)CaptureMethod.Camera)
            EditorGUILayout.PropertyField(_sourceCamera);

        if (_captureMethod.hasMultipleDifferentValues ||
            _captureMethod.enumValueIndex == (int)CaptureMethod.Texture)
            EditorGUILayout.PropertyField(_sourceTexture);

        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }
}

}

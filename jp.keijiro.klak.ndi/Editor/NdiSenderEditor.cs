using UnityEngine;
using UnityEditor;

namespace Klak.Ndi.Editor {

[CanEditMultipleObjects]
[CustomEditor(typeof(NdiSender))]
sealed class NdiSenderEditor : UnityEditor.Editor
{
    static class Labels
    {
        public static Label NdiName = "NDI Name";
    }

    AutoProperty _ndiName;
    AutoProperty _keepAlpha;
    AutoProperty _captureMethod;
    AutoProperty _sourceCamera;
    AutoProperty _sourceTexture;

    void OnEnable() => AutoProperty.Scan(this);

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // To update the NdiSender internal state on property modification, we
        // also manually update the C# properties.

        // NDI Name
        if (_captureMethod.Target.hasMultipleDifferentValues ||
            _captureMethod.Target.enumValueIndex != (int)CaptureMethod.GameView)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedTextField(_ndiName, Labels.NdiName);
            if (EditorGUI.EndChangeCheck()) // update-on-mod
                foreach (NdiSender send in targets)
                    send.ndiName = _ndiName.Target.stringValue;
        }

        // Keep Alpha
        EditorGUILayout.PropertyField(_keepAlpha);

        // Capture Method
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_captureMethod);
        if (EditorGUI.EndChangeCheck()) // update-on-mod
            foreach (NdiSender send in targets)
                send.captureMethod = (CaptureMethod)_captureMethod.Target.enumValueIndex;

        EditorGUI.indentLevel++;

        // Source Camera
        if (_captureMethod.Target.hasMultipleDifferentValues ||
            _captureMethod.Target.enumValueIndex == (int)CaptureMethod.Camera)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_sourceCamera);
            if (EditorGUI.EndChangeCheck()) // update-on-mod
                foreach (NdiSender send in targets)
                    send.sourceCamera = (Camera)_sourceCamera.Target.objectReferenceValue;
        }

        // Source Texture
        if (_captureMethod.Target.hasMultipleDifferentValues ||
            _captureMethod.Target.enumValueIndex == (int)CaptureMethod.Texture)
            EditorGUILayout.PropertyField(_sourceTexture);

        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }
}

} // namespace Klak.Ndi.Editor

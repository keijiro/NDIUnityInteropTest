using UnityEngine;
using UnityEditor;

namespace NDI.Editor {

[CanEditMultipleObjects]
[CustomEditor(typeof(Sender))]
sealed class SenderEditor : UnityEditor.Editor
{
    SerializedProperty _sourceName;
    SerializedProperty _enableAlpha;

    void OnEnable()
    {
        var finder = new PropertyFinder(serializedObject);
        _sourceName = finder["_sourceName"];
        _enableAlpha = finder["_enableAlpha"];
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_sourceName);
        EditorGUILayout.PropertyField(_enableAlpha);

        serializedObject.ApplyModifiedProperties();
    }
}

}

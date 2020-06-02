using UnityEngine;
using UnityEditor;

namespace NDI.Editor {

[CanEditMultipleObjects]
[CustomEditor(typeof(Sender))]
sealed class SenderEditor : UnityEditor.Editor
{
    SerializedProperty _name;
    SerializedProperty _enableAlpha;

    void OnEnable()
    {
        var finder = new PropertyFinder(serializedObject);
        _name = finder["_name"];
        _enableAlpha = finder["_enableAlpha"];
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_name);
        EditorGUILayout.PropertyField(_enableAlpha);

        serializedObject.ApplyModifiedProperties();
    }
}

}

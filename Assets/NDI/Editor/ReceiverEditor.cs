using UnityEngine;
using UnityEditor;

namespace NDI.Editor {

[CanEditMultipleObjects]
[CustomEditor(typeof(Receiver))]
sealed class ReceiverEditor : UnityEditor.Editor
{
    NdiFind _ndiFind;
    SerializedProperty _sourceName;

    static class Styles
    {
        public static Label Select = "Select";
    }

    // Create and show the source name dropdown.
    void ShowSourceNameDropdown(Rect rect)
    {
        var menu = new GenericMenu();

        var sources = _ndiFind.CurrentSources;

        if (sources.Length > 0)
        {
            foreach (var source in sources)
            {
                var name = source.NdiName;
                menu.AddItem(new GUIContent(name), false, OnSelectSource, name);
            }
        }
        else
        {
            menu.AddItem(new GUIContent("No source available"), false, null);
        }

        menu.DropDown(rect);
    }

    // Source name selection callback
    void OnSelectSource(object name)
    {
        serializedObject.Update();
        _sourceName.stringValue = (string)name;
        serializedObject.ApplyModifiedProperties();
        RequestReconnect();
    }

    // Request receiver reconnection.
    void RequestReconnect()
    {
        foreach (Receiver receiver in targets) receiver.RequestReconnect();
    }

    void OnEnable()
    {
        _ndiFind = NdiFind.Create();

        var finder = new PropertyFinder(serializedObject);
        _sourceName = finder["_sourceName"];
    }

    void OnDisable()
    {
        _ndiFind?.Dispose();
        _ndiFind = null;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginHorizontal();

        // Source name text field
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.DelayedTextField(_sourceName);
        if (EditorGUI.EndChangeCheck()) RequestReconnect();

        // Source name dropdown
        var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(60));
        if (EditorGUI.DropdownButton(rect, Styles.Select, FocusType.Keyboard))
            ShowSourceNameDropdown(rect);

        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}

}

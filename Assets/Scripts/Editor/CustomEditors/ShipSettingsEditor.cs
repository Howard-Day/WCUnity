using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShipSettings))]
public class ShipSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var instance = (ShipSettings)target;
        if (Application.isPlaying)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Formation", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("In Flight", (instance.Flight != null).ToString());
            if (instance.Flight != null)
            {
                FormationEditorGUI.DrawFlightGUI(instance.Flight);
            }
        }
    }

    private void OnSceneGUI()
    {
        var instance = (ShipSettings)target;
        if (Application.isPlaying) {
            if (instance.Flight != null) {
                FormationEditorGUI.DrawTemplate(instance.Flight);
            }
        }
    }
}

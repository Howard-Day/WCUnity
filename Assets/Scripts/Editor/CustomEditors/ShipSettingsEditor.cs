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
            EditorGUILayout.LabelField("In formation", (instance.Formation != null).ToString());
            if (instance.Formation != null)
            {
                FormationEditorGUI.DrawFormationGUI(instance.Formation);
            }
        }
    }
}

using UnityEditor;
using UnityEngine;

public static class FormationEditorGUI
{
    public static void DrawFormationGUI(Formation formation)
    {
        EditorGUILayout.LabelField("Formation " + formation.ID);
        EditorGUI.indentLevel++;
        foreach (var ship in formation)
        {
            if (ship != null)
            {
                string name = ship.DisplayName;
                if (ship == formation.Leader) name += " (leader)";
                if (GUILayout.Button(name))
                {
                    UnityEditor.Selection.activeObject = ship;
                }

                DrawTemplate(formation);
            }
        }
        UnityEditor.EditorGUI.indentLevel--;
    }

    private static void DrawTemplate(Formation formation)
    {
        var leader = formation.Leader;
        if (leader != null)
        {
            var template = formation.Template;

        }
    }
}

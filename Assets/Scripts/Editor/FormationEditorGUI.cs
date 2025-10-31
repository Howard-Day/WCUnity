using UnityEditor;
using UnityEngine;

public static class FormationEditorGUI
{
    public static void DrawFormationGUI(Formation formation)
    {
        EditorGUILayout.LabelField("Formation " + formation.ID);
        EditorGUI.indentLevel++;
        formation.Scale = EditorGUILayout.Slider(formation.Scale, .5f, 3f);
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
            }
        }
        UnityEditor.EditorGUI.indentLevel--;
    }

    /// <summary>
    /// Draw the template for the given Formation in the Scene
    /// view, relative to the Leader.
    /// </summary>
    /// <param name="formation"></param>
    public static void DrawTemplate(Formation formation)
    {
        var leader = formation.Leader;
        if (leader != null)
        {
            var template = formation.Template;
            for (int i = 0; i < formation.Template.Slots.Count; i++)
            {
                Transform slot = formation.Template.Slots[i];
                if (slot != null)
                {
                    var offset = formation.Template.GetLocalOffset(0, i) * formation.Scale;
                    DrawWedge(leader.transform, i, offset);
                }
            }
        }
    }

    private static GUIStyle labelStyle;
    /// <summary>
    /// Draw a wedge shape representing a slot in a formation.
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="index"></param>
    /// <param name="offset"></param>
    public static void DrawWedge(Transform slot, int index, Vector3 offset)
    {
        const float LENGTH = 20; // Starfighters are big
        const float HALF_LENGTH = LENGTH / 2f;
        const float QUARTER_LENGTH = LENGTH / 4f;
        Handles.color = new Color(1f, 1f, 1f, .5f);

        Vector3 horizontalA = new Vector3(0, 0, HALF_LENGTH) + offset;
        Vector3 horizontalB = new Vector3(QUARTER_LENGTH, 0, -HALF_LENGTH) + offset;
        Vector3 horizontalC = new Vector3(-QUARTER_LENGTH, 0, -HALF_LENGTH) + offset;
        DrawPoints(slot, horizontalA, horizontalB, horizontalC);

        Vector3 verticalA = new Vector3(0, 0, HALF_LENGTH) + offset;
        Vector3 verticalB = new Vector3(0, QUARTER_LENGTH, -HALF_LENGTH) + offset;
        Vector3 verticalC = new Vector3(0, -QUARTER_LENGTH, -HALF_LENGTH) + offset;
        DrawPoints(slot, verticalA, verticalB, verticalC);

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.fontSize = 30;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
        }

        Vector3 labelPosition = slot.TransformPoint(offset) + new Vector3(0, 7, 0);
        Handles.Label(labelPosition, $"Slot {index}", labelStyle);
    }

    private static void DrawPoints(Transform slot, Vector3 a, Vector3 b, Vector3 c)
    {
        Handles.DrawAAConvexPolygon(slot.TransformPoint(a), slot.TransformPoint(b), slot.TransformPoint(c));
    }
}

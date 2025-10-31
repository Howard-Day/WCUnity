using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FormationTemplate))]
public class FormationTemplateEditor : Editor
{
    const int GRID_INCREMENT = 10;

    private float scaleFactor = 1f;
    private GUIStyle labelStyle;

    private void OnEnable()
    {
        Tools.hidden = true;
    }

    private void OnDisable()
    {
        Tools.hidden = false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var formation = (FormationTemplate)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Flip X axis"))
        {
            FlipXAxis(formation);
        }

        if (GUILayout.Button("Flip Y axis"))
        {
            FlipYAxis(formation);
        }

        if (GUILayout.Button("Flip Z axis"))
        {
            FlipZAxis(formation);
        }

        // Scaling
        scaleFactor = EditorGUILayout.FloatField("Scale factor", scaleFactor);
        if (scaleFactor < .1f) scaleFactor = .1f;
        if (GUILayout.Button("Apply"))
        {
            ApplyScale(formation);
        }
    }

    private void FlipXAxis(FormationTemplate formation)
    {
        Undo.RecordObjects(formation.Slots as Transform[], "Flip formation (x-axis)");
        FlipAxis(formation, 0);
    }

    private void FlipYAxis(FormationTemplate formation)
    {
        Undo.RecordObjects(formation.Slots as Transform[], "Flip formation (y-axis)");
        FlipAxis(formation, 1);
    }

    private void FlipZAxis(FormationTemplate formation)
    {
        Undo.RecordObjects(formation.Slots as Transform[], "Flip formation (z-axis)");
        FlipAxis(formation, 2);
    }

    private void FlipAxis(FormationTemplate formation, int axisIndex)
    {
        foreach (var slot in formation.Slots)
        {
            Vector3 localPos = slot.localPosition * scaleFactor;
            localPos[axisIndex] *= -1;
            slot.localPosition = localPos;
            EditorUtility.SetDirty(slot);
        }
    }

    private void ApplyScale(FormationTemplate formation)
    {
        Undo.RecordObjects(formation.Slots as Transform[], "Scale formation");
        foreach (var slot in formation.Slots)
        {
            Vector3 localPos = slot.localPosition * scaleFactor;
            localPos.x = Round(localPos.x, GRID_INCREMENT);
            localPos.y = Round(localPos.y, GRID_INCREMENT);
            localPos.z = Round(localPos.z, GRID_INCREMENT);
            slot.localPosition = localPos;
            EditorUtility.SetDirty(slot);
        }
    }

    private void OnSceneGUI()
    {
        var formation = (FormationTemplate)target;
        for (int i = 0; i < formation.Slots.Count; i++)
        {
            var slot = formation.Slots[i];
            if (slot != null)
            {
                DrawWedge(slot, i);

                string groupName = "Slot " + i;
                if (Undo.GetCurrentGroupName() != groupName)
                {
                    Undo.SetCurrentGroupName(groupName);
                }
                int groupIndex = Undo.GetCurrentGroup();

                Vector3 newPosition = Handles.PositionHandle(slot.position, Quaternion.identity);
                newPosition.x = Round(newPosition.x, GRID_INCREMENT);
                newPosition.y = Round(newPosition.y, GRID_INCREMENT);
                newPosition.z = Round(newPosition.z, GRID_INCREMENT);
                if (newPosition != slot.position)
                {
                    Undo.RecordObject(slot, "Move Slot " + i);
                    slot.position = newPosition;
                    EditorUtility.SetDirty(slot);
                }
                else
                {
                    Undo.CollapseUndoOperations(groupIndex);
                }
            }
        }
    }

    private int Round(float value, int increment)
    {
        int result = Mathf.RoundToInt(value / increment) * increment;
        return result;
    }

    private void DrawWedge(Transform slot, int index)
    {
        const float LENGTH = 20; // Starfighters are big
        const float HALF_LENGTH = LENGTH / 2f;
        const float QUARTER_LENGTH = LENGTH / 4f;
        Handles.color = new Color(1f, 1f, 1f, .5f);

        Vector3 horizontalA = new Vector3(0, 0, HALF_LENGTH);
        Vector3 horizontalB = new Vector3(QUARTER_LENGTH, 0, -HALF_LENGTH);
        Vector3 horizontalC = new Vector3(-QUARTER_LENGTH, 0, -HALF_LENGTH);
        DrawPoints(slot, horizontalA, horizontalB, horizontalC);

        Vector3 vericalA = new Vector3(0, 0, HALF_LENGTH);
        Vector3 vericalB = new Vector3(0, QUARTER_LENGTH, -HALF_LENGTH);
        Vector3 vericalC = new Vector3(0, -QUARTER_LENGTH, -HALF_LENGTH);
        DrawPoints(slot, vericalA, vericalB, vericalC);

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.fontSize = 30;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
        }

        Handles.Label(slot.position + slot.up * 7, $"Slot {index}", labelStyle);
    }

    private void DrawPoints(Transform slot, Vector3 a, Vector3 b, Vector3 c)
    {
        Handles.DrawAAConvexPolygon(slot.TransformPoint(a), slot.TransformPoint(b), slot.TransformPoint(c));
    }
}

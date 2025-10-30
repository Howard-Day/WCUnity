using OneManEscapePlan.Common;
using UnityEngine;
using UnityEngine.Assertions;

public class FormationTemplate : MonoBehaviour
{
    [SerializeField, Required] private string displayName;
    [SerializeField, NonNull] private Transform[] slots;
   
    public int MaxShips => slots.Length;
    public string DisplayName => displayName;

    void Awake()
    {
        Assert.IsFalse(slots.Length == 0);
    }

    public Transform GetSlot(int index)
    {
        return slots[index];
    }

    public Vector3 GetLocalOffset(int slot1Index, int slot2Index)
    {
        return slots[slot2Index].localPosition - slots[slot1Index].localPosition;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 1f, .5f);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                Gizmos.DrawSphere(slots[i].position, 2.5f);
            }
        }
    }

    [UnityEditor.CustomEditor(typeof(FormationTemplate))]
    protected class FormationTemplateEditor : UnityEditor.Editor
    {
        const int GRID_INCREMENT = 10;

        private float scaleFactor = 1f;
        private GUIStyle labelStyle;

        private void OnEnable()
        {
            UnityEditor.Tools.hidden = true;
        }

        private void OnDisable()
        {
            UnityEditor.Tools.hidden = false;
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
            scaleFactor = UnityEditor.EditorGUILayout.FloatField("Scale factor", scaleFactor);
            if (scaleFactor < .1f) scaleFactor = .1f;
            if (GUILayout.Button("Apply"))
            {
                ApplyScale(formation);
            }
        }

        private void FlipXAxis(FormationTemplate formation)
        {
            UnityEditor.Undo.RecordObjects(formation.slots, "Flip formation (x-axis)");
            FlipAxis(formation, 0);
        }

        private void FlipYAxis(FormationTemplate formation)
        {
            UnityEditor.Undo.RecordObjects(formation.slots, "Flip formation (y-axis)");
            FlipAxis(formation, 1);
        }

        private void FlipZAxis(FormationTemplate formation)
        {
            UnityEditor.Undo.RecordObjects(formation.slots, "Flip formation (z-axis)");
            FlipAxis(formation, 2);
        }

        private void FlipAxis(FormationTemplate formation, int axisIndex)
        {
            foreach (var slot in formation.slots)
            {
                Vector3 localPos = slot.localPosition * scaleFactor;
                localPos[axisIndex] *= -1;
                slot.localPosition = localPos;
                UnityEditor.EditorUtility.SetDirty(slot);
            }
        }

        private void ApplyScale(FormationTemplate formation)
        {
            UnityEditor.Undo.RecordObjects(formation.slots, "Scale formation");
            foreach (var slot in formation.slots)
            {
                Vector3 localPos = slot.localPosition * scaleFactor;
                localPos.x = Round(localPos.x, GRID_INCREMENT);
                localPos.y = Round(localPos.y, GRID_INCREMENT);
                localPos.z = Round(localPos.z, GRID_INCREMENT);
                slot.localPosition = localPos;
                UnityEditor.EditorUtility.SetDirty(slot);
            }
        }

        private void OnSceneGUI()
        {
            var formation = (FormationTemplate)target;
            for (int i = 0; i < formation.slots.Length; i++)
            {
                var slot = formation.slots[i];
                if (slot != null)
                {
                    DrawWedge(slot, i);

                    string groupName = "Slot " + i;
                    if (UnityEditor.Undo.GetCurrentGroupName() != groupName)
                    {
                        UnityEditor.Undo.SetCurrentGroupName(groupName);
                    }
                    int groupIndex = UnityEditor.Undo.GetCurrentGroup();

                    Vector3 newPosition = UnityEditor.Handles.PositionHandle(slot.position, Quaternion.identity);
                    newPosition.x = Round(newPosition.x, GRID_INCREMENT);
                    newPosition.y = Round(newPosition.y, GRID_INCREMENT);
                    newPosition.z = Round(newPosition.z, GRID_INCREMENT);
                    if (newPosition != slot.position)
                    {
                        UnityEditor.Undo.RecordObject(slot, "Move Slot " + i);
                        slot.position = newPosition;
                        UnityEditor.EditorUtility.SetDirty(slot);
                    } else
                    {
                        UnityEditor.Undo.CollapseUndoOperations(groupIndex);
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
            const float HALF_LENGTH = LENGTH  / 2f;
            const float QUARTER_LENGTH = LENGTH  / 4f;
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, .5f);

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
                labelStyle = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
                labelStyle.fontSize = 30;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.normal.textColor = Color.white;
            }

            UnityEditor.Handles.Label(slot.position + slot.up * 7, $"Slot {index}", labelStyle);
        }

        private void DrawPoints(Transform slot, Vector3 a, Vector3 b, Vector3 c)
        {
            UnityEditor.Handles.DrawAAConvexPolygon(slot.TransformPoint(a), slot.TransformPoint(b), slot.TransformPoint(c));
        }
    }
#endif
}

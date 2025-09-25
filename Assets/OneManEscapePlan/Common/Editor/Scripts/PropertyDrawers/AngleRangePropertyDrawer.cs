/// ©2022 Kevin Foley. 
/// See accompanying license file.

using OneManEscapePlan.Common.Scripts.DataStructures;
using UnityEditor;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.Editor {

	[CustomPropertyDrawer(typeof(AngleRange))]
	public class AngleRangePropertyDrawer : PropertyDrawer {

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			if (property.isExpanded) return base.GetPropertyHeight(property, label) * 2;
			return base.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			label = EditorGUI.BeginProperty(position, label, property);
			Color defaultBackgroundColor = GUI.backgroundColor;

			var valueProperty = property.FindPropertyRelative("value");
			var minProperty = valueProperty.FindPropertyRelative("start").FindPropertyRelative("rawDegrees");
			var sizeProperty = valueProperty.FindPropertyRelative("size");

			if (sizeProperty.floatValue < 0) GUI.backgroundColor = Color.red;

			float defaultLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 35;

			Rect rect = position;
			if (property.isExpanded) rect.height /= 2f;

			var min = new Angle(minProperty.floatValue);
			var max = new Angle(minProperty.floatValue + sizeProperty.floatValue);
			label.text += $":  {min.ToString180()} - {max.ToString180()}";
			property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
			if (property.isExpanded) {
				// We manually indent because EditorGUI.indentLevel doesn't work well for
				// multiple fields on one line.
				int previousIndentLevel = EditorGUI.indentLevel;
				int indent = (previousIndentLevel + 1) * 15;
				EditorGUI.indentLevel = 0;

				rect.y += rect.height;
				rect.width = (rect.width - indent - 8) / 3f;
				rect.x += indent;

				minProperty.floatValue = EditorGUI.FloatField(rect, "Start", minProperty.floatValue);
				rect.x += rect.width + 4;
				float maxF = EditorGUI.FloatField(rect, "End", minProperty.floatValue + sizeProperty.floatValue);
				sizeProperty.floatValue = maxF - minProperty.floatValue;
				rect.x += rect.width + 4;
				sizeProperty.floatValue = EditorGUI.FloatField(rect, "Size", sizeProperty.floatValue);

				EditorGUI.indentLevel = previousIndentLevel;
			}

			GUI.backgroundColor = defaultBackgroundColor;
			EditorGUIUtility.labelWidth = defaultLabelWidth;

			EditorGUI.EndProperty();
		}
	}
}

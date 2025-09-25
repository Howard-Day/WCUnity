/// ©2022 Kevin Foley. 
/// See accompanying license file.

using OneManEscapePlan.Common.Scripts.DataStructures;
using UnityEditor;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.Editor {

	[CustomPropertyDrawer(typeof(Distance))]
	public class DistancePropertyDrawer : PropertyDrawer {

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			if (property.isExpanded) return base.GetPropertyHeight(property, label) * 2;
			return base.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			label = EditorGUI.BeginProperty(position, label, property);

			var metersProperty = property.FindPropertyRelative("meters");
			float meters = metersProperty.floatValue;

			float defaultLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 30;

			Rect rect = position;
			if (property.isExpanded) rect.height /= 2f;

			Distance distance = Distance.FromMeters(meters);
			label.text += ":  " + distance.ToString("F2");

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

				meters = EditorGUI.FloatField(rect, "m", meters);
				if (meters != distance.Meters) {
					metersProperty.floatValue = meters;
				}

				rect.x += rect.width + 4;
				float feet = EditorGUI.FloatField(rect, "feet", distance.Feet);
				if (feet != distance.Feet) {
					Distance newDistance = Distance.FromFeet(feet);
					metersProperty.floatValue = newDistance.Meters;
				}

				rect.x += rect.width + 4;
				float miles = EditorGUI.FloatField(rect, "miles", distance.Miles);
				if (miles != distance.Miles) {
					Distance newDistance = Distance.FromMiles(miles);
					metersProperty.floatValue = newDistance.Meters;
				}
				
				EditorGUI.indentLevel = previousIndentLevel;
			}

			EditorGUIUtility.labelWidth = defaultLabelWidth;

			EditorGUI.EndProperty();
		}
	}
}

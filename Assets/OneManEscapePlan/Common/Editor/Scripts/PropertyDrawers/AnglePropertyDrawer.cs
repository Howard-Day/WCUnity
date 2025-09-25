/// ©2023 Kevin Foley. 
/// See accompanying license file.

using OneManEscapePlan.Common.Scripts.DataStructures;
using UnityEditor;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.Editor {

	[CustomPropertyDrawer(typeof(Angle))]
	public class AnglePropertyDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			label = EditorGUI.BeginProperty(position, label, property);

			const int SECOND_LABEL_WIDTH = 45;
			const int SPACING = 10;

			bool isWide = (position.width > 225);
			if (isWide)	position.width -= SECOND_LABEL_WIDTH + SPACING;

			var degreesProp = property.FindPropertyRelative("rawDegrees");
			EditorGUI.PropertyField(position, degreesProp, label);

			if (isWide) {
				position.x += position.width + SPACING;
				position.width = SECOND_LABEL_WIDTH;

				var angle = new Angle(degreesProp.floatValue);
				var previousIndentLevel = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
				EditorGUI.LabelField(position, angle.ToString360());
				EditorGUI.indentLevel = previousIndentLevel;
			}

			EditorGUI.EndProperty();
		}
	}
}

/// ©2022 Kevin Foley. 
/// See accompanying license file.

using OneManEscapePlan.Common.Scripts.DataStructures;
using UnityEditor;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.Editor {

	[CustomPropertyDrawer(typeof(Acceleration))]
	public class AccelerationPropertyDrawer : PropertyDrawer {

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			if (property.isExpanded) return base.GetPropertyHeight(property, label) * 2;
			return base.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			label = EditorGUI.BeginProperty(position, label, property);

			var metersPerSecSquaredProperty = property.FindPropertyRelative("metersPerSecSquared");
			float mpss = metersPerSecSquaredProperty.floatValue;

			float defaultLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 30;

			Rect rect = position;
			if (property.isExpanded) rect.height /= 2f;

			Acceleration acceleration = Acceleration.FromMetersPerSecondSquared(mpss);
			label.text += ":  " + acceleration.ToString("F2");

			property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
			if (property.isExpanded) {
				// We manually indent because EditorGUI.indentLevel doesn't work well for
				// multiple fields on one line.
				int previousIndentLevel = EditorGUI.indentLevel;
				int indent = (previousIndentLevel + 1) * 15;
				EditorGUI.indentLevel = 0;

				rect.y += rect.height;
				rect.width = (rect.width - indent - 4) / 2f;
				rect.x += indent;

				mpss = EditorGUI.FloatField(rect, "m/s²", mpss);
				if (mpss != acceleration.MetersPerSecondSquared) {
					metersPerSecSquaredProperty.floatValue = mpss;
				}

				rect.x += rect.width + 4;
				float fpss = EditorGUI.FloatField(rect, "fps²", acceleration.FeetPerSecondSquared);
				if (fpss != acceleration.FeetPerSecondSquared) {
					Acceleration newAcceleration = Acceleration.FromFeetPerSecondSquared(fpss);
					metersPerSecSquaredProperty.floatValue = newAcceleration.MetersPerSecondSquared;
				}
				
				EditorGUI.indentLevel = previousIndentLevel;
			}

			EditorGUIUtility.labelWidth = defaultLabelWidth;

			EditorGUI.EndProperty();
		}
	}
}

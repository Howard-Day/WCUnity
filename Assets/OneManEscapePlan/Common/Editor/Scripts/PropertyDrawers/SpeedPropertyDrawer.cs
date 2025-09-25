/// ©2022 Kevin Foley. 
/// See accompanying license file.

using OneManEscapePlan.Common.Scripts.DataStructures;
using UnityEditor;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.Editor {

	[CustomPropertyDrawer(typeof(Speed))]
	public class SpeedPropertyDrawer : PropertyDrawer {

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			if (property.isExpanded) return base.GetPropertyHeight(property, label) * 2;
			return base.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			label = EditorGUI.BeginProperty(position, label, property);

			var metersPerSecondProperty = property.FindPropertyRelative("metersPerSecond");
			float mps = metersPerSecondProperty.floatValue;

			float defaultLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 30;

			Rect rect = position;
			if (property.isExpanded) rect.height /= 2f;

			Speed speed = Speed.FromMetersPerSecond(mps);
			label.text += ":  " + speed.ToString("F2");

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

				mps = EditorGUI.FloatField(rect, "m/s", mps);
				if (mps != speed.MetersPerSecond) {
					metersPerSecondProperty.floatValue = mps;
				}

				rect.x += rect.width + 4;
				float kph = EditorGUI.FloatField(rect, "kph", speed.KPH);
				if (kph != speed.KPH) {
					Speed newSpeed = Speed.FromKPH(kph);
					metersPerSecondProperty.floatValue = newSpeed.MetersPerSecond;
				}

				rect.x += rect.width + 4;
				float mph = EditorGUI.FloatField(rect, "mph", speed.MPH);
				if (mph != speed.MPH) {
					Speed newSpeed = Speed.FromMPH(mph);
					metersPerSecondProperty.floatValue = newSpeed.MetersPerSecond;
				}

				EditorGUI.indentLevel = previousIndentLevel;
			}

			EditorGUIUtility.labelWidth = defaultLabelWidth;

			EditorGUI.EndProperty();
		}
	}
}

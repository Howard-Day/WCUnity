/// ©2022 Kevin Foley. 
/// See accompanying license file.

using OneManEscapePlan.Common.Scripts.DataStructures;
using UnityEditor;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.Editor {

	[CustomPropertyDrawer(typeof(TimeSpanF))]
	public class TimeSpanFPropertyDrawer : PropertyDrawer {

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			if (property.isExpanded) return base.GetPropertyHeight(property, label) * 2;
			return base.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			label = EditorGUI.BeginProperty(position, label, property);

			var secondsProperty = property.FindPropertyRelative("seconds");
			float totalSeconds = secondsProperty.floatValue;

			float defaultLabelWidth = EditorGUIUtility.labelWidth;

			Rect rect = position;
			if (property.isExpanded) rect.height /= 2f;

			TimeSpanF timeSpan = TimeSpanF.FromSeconds(totalSeconds);
			label.text += ":  " + timeSpan.ToString();

			property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
			if (property.isExpanded) {
				bool isWide = rect.width > 225;

				// We manually indent because EditorGUI.indentLevel doesn't work well for
				// multiple fields on one line.
				int previousIndentLevel = EditorGUI.indentLevel;
				int indent = (previousIndentLevel + 1) * 15;
				EditorGUI.indentLevel = 0;

				rect.y += rect.height;
				rect.x += indent;
				rect.width -= indent;

				string hoursLabel;
				string minutesLabel;
				string secondsLabel;
				if (isWide) {
					hoursLabel = "Hours";
					minutesLabel = "Minutes";
					secondsLabel = "Seconds";
				} else {
					hoursLabel = "H";
					minutesLabel = "M";
					secondsLabel = "S";
				}
				float hoursLabelWidth = EditorStyles.label.CalcSize(new GUIContent(hoursLabel)).x;
				float minutesLabelWidth = EditorStyles.label.CalcSize(new GUIContent(minutesLabel)).x;
				float secondsLabelWidth = EditorStyles.label.CalcSize(new GUIContent(secondsLabel)).x;

				float availableSpace = (rect.width - hoursLabelWidth - minutesLabelWidth - secondsLabelWidth - 8);
				float fieldWidth = availableSpace / 3f;

				EditorGUIUtility.labelWidth = hoursLabelWidth;
				rect.width = fieldWidth + hoursLabelWidth;
				int hours = EditorGUI.IntField(rect, hoursLabel, timeSpan.Hours);

				rect.x += rect.width + 4;
				rect.width = fieldWidth + minutesLabelWidth;

				EditorGUIUtility.labelWidth = minutesLabelWidth;
				int minutes = EditorGUI.IntField(rect, minutesLabel, timeSpan.Minutes);

				rect.x += rect.width + 4;
				rect.width = fieldWidth + secondsLabelWidth;

				EditorGUIUtility.labelWidth = secondsLabelWidth;
				float seconds = EditorGUI.FloatField(rect, secondsLabel, timeSpan.Seconds);

				TimeSpanF newTimeSpan = new TimeSpanF(hours, minutes, seconds);
				if (newTimeSpan != timeSpan) {
					secondsProperty.floatValue = newTimeSpan.TotalSeconds;
				}

				EditorGUI.indentLevel = previousIndentLevel;
			}

			EditorGUIUtility.labelWidth = defaultLabelWidth;

			EditorGUI.EndProperty();
		}
	}
}

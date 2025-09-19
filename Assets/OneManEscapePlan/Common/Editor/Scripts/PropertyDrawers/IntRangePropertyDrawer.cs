/// ©2022 Kevin Foley. 
/// See accompanying license file.

using OneManEscapePlan.Common.Scripts.DataStructures;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace OneManEscapePlan.Common.Scripts.Editor {

	[CustomPropertyDrawer(typeof(IntRange))]
	[CustomPropertyDrawer(typeof(IntRangeValue))]
	public class IntRangePropertyDrawer : PropertyDrawer {

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			if (property.isExpanded) return base.GetPropertyHeight(property, label) * 2;
			return base.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			label = EditorGUI.BeginProperty(position, label, property);
			Color defaultBackgroundColor = GUI.backgroundColor;

			var minProperty = property.FindPropertyRelative("min");
			var maxProperty = property.FindPropertyRelative("max");

			if (maxProperty.intValue < minProperty.intValue) GUI.backgroundColor = Color.red;

			float defaultLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 40;

			Rect rect = position;
			if (property.isExpanded) rect.height /= 2f;

			label.text += ":  " + minProperty.intValue + " - " + maxProperty.intValue;
			property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
			if (property.isExpanded) {
				// We manually indent because EditorGUI.indentLevel doesn't work well for
				// multiple fields on one line.
				int previousIndentLevel = EditorGUI.indentLevel;
				int indent = (previousIndentLevel + 1) * 15;
				EditorGUI.indentLevel = 0;

				rect.y += rect.height;
				rect.width = (rect.width - indent - 5) / 2f;
				rect.x += indent;

				minProperty.intValue = EditorGUI.IntField(rect, "Min", minProperty.intValue);
				rect.x += rect.width + 5;
				maxProperty.intValue = EditorGUI.IntField(rect, "Max", maxProperty.intValue);

				EditorGUI.indentLevel = previousIndentLevel;
			}

			GUI.backgroundColor = defaultBackgroundColor;
			EditorGUIUtility.labelWidth = defaultLabelWidth;

			EditorGUI.EndProperty();
		}
	}
}

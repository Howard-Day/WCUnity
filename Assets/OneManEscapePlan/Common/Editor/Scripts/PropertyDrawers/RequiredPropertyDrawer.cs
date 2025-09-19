/// ©2024 Kevin Foley. 
/// See accompanying license file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.Editor {
	/// <summary>
	/// The [Required] attribute can be applied to serialized string and reference-type fields to add syntax coloring in
	/// the inspector. If a field marked with [Required] has not been assigned a value, it will be colored red in the 
	/// Inspector, making it easy to spot fields that you forgot to assign values to. Works for arrays and lists as well
	/// as for single-value fields.
	/// </summary>
	/// <remarks>
	/// This is similar to <seealso cref="NonNullPropertyDrawer"/>, but also supports strings.
	/// </remarks>
	/// <example>
	///		[SerializeField] [Required] Rigidbody body;
	///		[SerializeField] [Required] string url;
	///		[SerializeField] [Required] List{AudioClip} clips;
	///		[SerializeField] [Required] List{string} names;
	///		[SerializeField] [Required] Transform[] transforms;
	///		[SerializeField] [Required] string[] labels;
	/// </example>
	[CustomPropertyDrawer(typeof(RequiredAttribute))]
	public class RequiredPropertyDrawer : PropertyDrawer {

		const float HELP_BOX_HEIGHT = 25;
		const float PADDING = 2;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			float height = base.GetPropertyHeight(property, label);
			if (property.propertyType != SerializedPropertyType.ObjectReference && property.propertyType != SerializedPropertyType.String) {
				height += HELP_BOX_HEIGHT + PADDING;
			}
			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			// NOTE: Addressables asset references (e.g. AssetReferenceT) are not supported,
			// because EditorGUI.PropertyField() won't use their custom property drawers.

			Color defaultBackgroundColor = GUI.backgroundColor;
			if (property.propertyType == SerializedPropertyType.ObjectReference) {
				if (property.objectReferenceValue == null) {
					GUI.backgroundColor = Color.red;
				}
			} else if (property.propertyType == SerializedPropertyType.String) {
				if (string.IsNullOrEmpty(property.stringValue)) {
					GUI.backgroundColor = Color.red;
				}
			} else {
				float fieldHeight = position.height - HELP_BOX_HEIGHT - PADDING;
				position.height = HELP_BOX_HEIGHT;
				EditorGUI.HelpBox(position, "[Required] attribute is only valid on string and reference-type fields.", MessageType.Warning);
				position.height = fieldHeight;
				position.y += HELP_BOX_HEIGHT + PADDING;
			}
			
			EditorGUI.PropertyField(position, property, label);
			GUI.backgroundColor = defaultBackgroundColor;
		}
	}
}

/// ©2022 Kevin Foley.
/// See accompanying license file.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Assertions;

namespace OneManEscapePlan.Common.Scripts.Utility {
#if UNITY_EDITOR
	/// <summary>
	/// Extend this class for custom Editors that are nested in the class they edit. Just provides
	/// wrappers for EditorGUILayout without having to import the UnityEditor namespace in your
	/// component class.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks>
	/// When we create a nested Editor inside the class that it edits, we usually have to reference
	/// the UnityEditor namespace. If we're not careful, this will throw errors when we try to build
	/// the project because the UnityEditor namespace is not available in builds.
	/// 
	/// One solution is to wrap the `using` statement in a compiler directive:
	/// 
	///		#if UNITY_EDITOR
	///		using UnityEditor;
	///		#endif
	/// 
	/// However, with this approach, it's easy to accidentally reference something in the UnityEditor
	/// namespace outside of our nested custom Editor. Also, Visual Studio sometimes places new auto-
	/// generated `using` statements inside the compiler directive, again causing builds to fail.
	/// 
	/// Another way to handle this without a `using` statement is by manually qualifying all 
	/// references to the UnityEditor namespace, e.g.
	/// 
	///		UnityEditor.EditorGUILayout.LabelField("Health", hp);
	///		
	/// but this gets cumbersome for a complex Editor.
	/// 
	/// We can instead extend NestedEditor{T} to get access to common Editor functions without having
	/// to import or qualify the UnityEditor namespace.
	/// </remarks>
	/// <example>
	/// 
	/// </example>
	public class NestedEditor<T> : Editor where T : UnityEngine.Object {
		#region PROPERTIES
		protected T Instance => target as T;
		protected GUIStyle boldLabelStyle => EditorStyles.boldLabel;
		#endregion

		protected void LabelField(string label) {
			EditorGUILayout.LabelField(label);
		}
		
		protected void BoldLabelField(string label) {
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
		}

		protected void LabelField(string label, GUIStyle style) {
			EditorGUILayout.LabelField(label, style);
		}

		protected void LabelField(string label, string label2) {
			EditorGUILayout.LabelField(label, label2);
		}
		
		protected void BoldLabelField(string label, string label2) {
			EditorGUILayout.LabelField(label, label2, EditorStyles.boldLabel);
		}

		protected void LabelField(string label, string label2, GUIStyle style) {
			EditorGUILayout.LabelField(label, label2, style);
		}

		protected bool Toggle(bool value) {
			return EditorGUILayout.Toggle(value);
		}

		protected bool Toggle(string label, bool value) {
			return EditorGUILayout.Toggle(label, value);
		}

		protected int IntField(string label, int value) {
			return EditorGUILayout.IntField(label, value);
		}

		protected float FloatField(string label, float value) {
			return EditorGUILayout.FloatField(label, value);
		}

		protected bool Foldout(bool isExpanded, string label) {
			return EditorGUILayout.Foldout(isExpanded, label);
		}

		protected void Space(float value) {
			EditorGUILayout.Space(value);
		}

		protected void SetDirty(UnityEngine.Object obj) {
			EditorUtility.SetDirty(obj);
		}

		protected string TextField(string label, string value) {
			return EditorGUILayout.TextField(label, value);
		}
		
		protected string TextArea(string text) {
			return EditorGUILayout.TextArea(text);
		}
	}
#endif
}

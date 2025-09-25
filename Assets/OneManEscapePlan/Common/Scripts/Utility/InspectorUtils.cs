#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.Utility {
	public static class InspectorUtils {
		/// <summary>
		/// Draw an object field for the specified type of ScriptableObject. If the object
		/// is selected, we draw the Editor for that object in-line. The user can collapse
		/// the in-line Editor if it is not needed.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="target"></param>
		/// <param name="propertiesModel"></param>
		/// <param name="editor"></param>
		/// <param name="assetTypeName"></param>
		/// <param name="isExpanded"></param>
		public static void DrawScriptableObject<T>(Object target, ref T propertiesModel, ref Editor editor, string assetTypeName, ref bool isExpanded) where T : ScriptableObject {
			GUILayout.Space(5);
			Undo.RecordObject(target, "Select " + assetTypeName);

			EditorGUILayout.BeginHorizontal();
			if (propertiesModel != null) isExpanded = EditorGUILayout.Foldout(isExpanded, assetTypeName, EditorStyles.foldoutHeader);
			EditorGUI.BeginChangeCheck();
			if (propertiesModel == null) propertiesModel = (T)EditorGUILayout.ObjectField(assetTypeName, propertiesModel, typeof(T), allowSceneObjects: false);
			else propertiesModel = (T)EditorGUILayout.ObjectField(propertiesModel, typeof(T), allowSceneObjects: false, GUILayout.ExpandWidth(false));
			if (propertiesModel == null) DrawCreateAssetButton(target, ref propertiesModel, assetTypeName);
			EditorGUILayout.EndHorizontal();
			if (EditorGUI.EndChangeCheck()) {
				EditorUtility.SetDirty(target);
			}

			if (isExpanded) {
				// If properties model is not null, draw its Editor in-line.
				if (propertiesModel != null) {
					if (editor == null || editor.target != propertiesModel) {
						if (editor != null) Object.DestroyImmediate(editor);
						editor = Editor.CreateEditor(propertiesModel);
					}

					EditorGUI.indentLevel++;
					EditorGUILayout.BeginVertical("box");
					editor.DrawDefaultInspector();
					EditorGUILayout.EndVertical();
					EditorGUI.indentLevel--;
				}
			}
		}

		public static void DrawCreateAssetButton<T>(Object target, ref T model, string assetTypeName) where T : ScriptableObject {
			if (model == null) {
				if (GUILayout.Button($"Create", GUILayout.Width(60))) {
					CreateAsset(target, ref model, $"{target.name} {assetTypeName}");
				}
			}
		}

		public static void CreateAsset<T>(Object target, ref T reference, string assetName) where T : ScriptableObject {
			var path = AssetDatabase.GetAssetPath(target).Replace(target.name, assetName);
			if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) {
				EditorUtility.DisplayDialog("Error", "A file already exists at " + path, "OK");
				return;
			}
			var instance = ScriptableObject.CreateInstance<T>();
			AssetDatabase.CreateAsset(instance, path);
			reference = instance;
		}
	}
}
#endif
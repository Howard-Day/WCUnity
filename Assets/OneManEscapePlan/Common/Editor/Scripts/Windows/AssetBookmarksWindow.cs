/// ©2024 Kevin Foley. 
/// See accompanying license file.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OneManEscapePlan.Common.Editor.Scripts.Windows {
	/// <summary>
	/// This window lets you save a list of assets which you frequently access,
	/// making it easier to jump between assets without having to navigate or
	/// search in the Project panel.
	/// </summary>
	public class AssetBookmarksWindow : EditorWindow {
		#region STATIC
		[MenuItem("Assets/Bookmarks", priority = 5)]
		public static AssetBookmarksWindow ShowWindow() {
			var window = EditorWindow.GetWindow<AssetBookmarksWindow>("Bookmarks");
			window.minSize = new Vector2(300, 200);
			return window;
		}
		#endregion

		#region FIELDS
		const string PREFS_KEY = "AssetBookmarks";

		[SerializeField] private List<Object> bookmarks = new List<Object>();

		private SerializedObject so;
		private SerializedProperty bookmarksProp;
		#endregion

		#region PROPERTIES

		#endregion

		#region UNITY LIFECYCLE
		private void Awake() {
			Init();
			Load();
		}

		void Init() {
			so = new SerializedObject(this);
			bookmarksProp = so.FindProperty(nameof(bookmarks));
		}

		private void OnGUI() {
			if (so == null) Init();

			EditorGUILayout.PropertyField(bookmarksProp);

			using (var layout = new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Reset")) {
					Load();
				}
				EditorGUILayout.Space(1, true);
				if (GUILayout.Button("Save")) {
					Save();
				}
			}
		}
		#endregion

		private void Load() {
			string json = EditorPrefs.GetString(PREFS_KEY, null);
			if (json != null) {
				try {
					var sb = JsonUtility.FromJson<SavedBookmarks>(json);
					bookmarks.Clear();
					foreach (var path in sb.paths) {
						var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
						bookmarks.Add(asset);
					}
					Init();
				} catch (System.Exception ex) {
					EditorUtility.DisplayDialog("Error reading saved bookmarks", ex.Message, "OK");
				}
			}
		}

		private void Save() {
			List<string> paths = new List<string>(bookmarks.Count);
			for (int i = 0; i < bookmarksProp.arraySize; i++) {
				Object item = bookmarksProp.GetArrayElementAtIndex(i).objectReferenceValue;
				if (item != null) {
					paths.Add(AssetDatabase.GetAssetPath(item));
				}
			}

			var sb = new SavedBookmarks(paths);
			var json = JsonUtility.ToJson(sb);
			EditorPrefs.SetString(PREFS_KEY, json);
		}

		[System.Serializable]
		protected class SavedBookmarks {
			[SerializeField] public List<string> paths;

			public SavedBookmarks(List<string> paths) {
				this.paths = paths;
			}
		}
	}
}
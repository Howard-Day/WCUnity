/// © 2018-2019 Kevin Foley
/// For distribution only on the Unity Asset Store
/// Terms/EULA: https://unity3d.com/legal/as_terms

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OneManEscapePlan.SpaceRailShooter.Scripts.Settings {
	/// <summary>
	/// Defines an asset file where we can set render settings. Works in tandem with <see cref="Effects.PixelCamera"/>
	/// </summary>
	/// COMPLEXITY: Advanced
	/// CONCEPTS: Render textures, Custom inspectors
	[CreateAssetMenu(fileName = "SRSRenderSettings", menuName = "OneManEscapePlan/Render Settings ", order = 3)]
	public class SRSRenderSettings : ScriptableObject {

		[HideInInspector]
		[SerializeField]
		private bool useCustomSettings = true;

		[HideInInspector]
		[SerializeField]
		private RenderTexture renderTexture;

		[HideInInspector]
		[SerializeField]
		private int verticalResolution = 224;

		[HideInInspector]
		[Range(10, 120)]
		[SerializeField]
		private int framerate = 20;

		public bool UseCustomSettings {
			get {
				return useCustomSettings;
			}
		}

		public int VerticalResolution {
			get {
				return verticalResolution;
			}
		}

		public RenderTexture RenderTexture {
			get {
				return renderTexture;
			}
		}

		public int Framerate {
			get {
				return framerate;
			}
		}

#if UNITY_EDITOR
		/// <summary>
		/// Draws a custom inspector for the RenderSettings asset
		/// </summary>
		[CustomEditor(typeof(SRSRenderSettings))]
		private class SpaceRailShooterSettingsInspector : Editor {

			private const string DIALOG_TITLE = "Warning";
			private const string DIALOG_MESSAGE = "This will adjust project quality settings for the entire project.";
			private const string DIALOG_OK = "Continue";
			private const string DIALOG_CANCEL = "Cancel";

			public override void OnInspectorGUI() {
				float labelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 180;

				DrawDefaultInspector();

				SRSRenderSettings settings = (SRSRenderSettings)target;

				string tooltip = "Indicates that the game should render at a custom resolution rather than the native resolution of the window";
				settings.useCustomSettings = EditorGUILayout.Toggle(new GUIContent("Use Custom Settings", tooltip), settings.useCustomSettings);

				EditorGUI.BeginChangeCheck();

				//we only display all the custom render settings if the "use custom settings" option is checked
				if (settings.useCustomSettings) {

					settings.renderTexture = (RenderTexture)EditorGUILayout.ObjectField("Render texture", settings.renderTexture, typeof(RenderTexture), false);

					string tooltip2 = "The vertical resolution that the game is rendered at. The horizontal resolution will be dictated by the aspect ratio of the window";
					settings.verticalResolution = EditorGUILayout.IntField(new GUIContent("Custom vertical resolution", tooltip2), settings.verticalResolution);
					if (settings.verticalResolution < 100) settings.verticalResolution = 100;

					settings.framerate = EditorGUILayout.IntSlider("Framerate", settings.framerate, 10, 120);

					EditorGUILayout.Space();

					EditorGUILayout.LabelField("Quality level", QualitySettings.GetQualityLevel().ToString());

					EditorGUILayout.Space();

					if (GUILayout.Button("Apply 16-bit preset")) {
						if (EditorUtility.DisplayDialog(DIALOG_TITLE, DIALOG_MESSAGE, DIALOG_OK, DIALOG_CANCEL)) {
							settings.verticalResolution = 224;
							settings.framerate = 20;
							setQuality(0);
						}
					}
					if (GUILayout.Button("Apply 32-bit preset")) {
						if (EditorUtility.DisplayDialog(DIALOG_TITLE, DIALOG_MESSAGE, DIALOG_OK, DIALOG_CANCEL)) {
							settings.verticalResolution = 240;
							settings.framerate = 30;
							setQuality(1);
						}
					}
					if (GUILayout.Button("Apply 64-bit preset")) {
						if (EditorUtility.DisplayDialog(DIALOG_TITLE, DIALOG_MESSAGE, DIALOG_OK, DIALOG_CANCEL)) {
							settings.verticalResolution = 480;
							settings.framerate = 30;
							setQuality(2);
						}
					}
					if (GUILayout.Button("Apply HD preset")) {
						if (EditorUtility.DisplayDialog(DIALOG_TITLE, DIALOG_MESSAGE, DIALOG_OK, DIALOG_CANCEL)) {
							settings.verticalResolution = 720;
							settings.framerate = 60;
							setQuality(3);
						}
					}

					if (settings.renderTexture != null) {
						settings.renderTexture.Release();
						settings.renderTexture.height = settings.verticalResolution;
					}
				}

				if (EditorGUI.EndChangeCheck()) {
					EditorUtility.SetDirty(target);
				}

				EditorGUIUtility.labelWidth = labelWidth;
			}
		}

		private static void setQuality(int level) {
			try {
				QualitySettings.SetQualityLevel(level, true);
			} catch (System.Exception ex) {
				Debug.LogWarning("Unable to apply quality level " + level + ": " + ex.Message);
			}
		}
#endif
	}
}
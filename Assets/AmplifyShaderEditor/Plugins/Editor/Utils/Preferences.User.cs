// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	public partial class Preferences
	{
		public enum ShowOption
		{
			Always = 0,
			OnNewVersion = 1,
			Never = 2
		}

		public class User
		{
			private class Styles
			{
				public static readonly GUIContent StartUp                       = new GUIContent( "Show start screen on Unity launch", "You can set if you want to see the start screen everytime Unity launchs, only just when there's a new version available or never." );
				public static readonly GUIContent AlwaysSnapToGrid              = new GUIContent( "Always Snap to Grid", "Always snap to grid when dragging nodes around, instead of using control." );
				public static readonly GUIContent EnableUndo                    = new GUIContent( "Enable Undo (unstable)", "Enables undo for actions within the shader graph canvas. Currently unstable, use with caution." );
				public static readonly GUIContent ClearLog                      = new GUIContent( "Clear Log on Update", "Clears the previously generated log each time the Update button is pressed." );
				public static readonly GUIContent LogShaderCompile              = new GUIContent( "Log Shader Compile", "Log message to console when a shader compilation is finished." );
				public static readonly GUIContent LogBatchCompile               = new GUIContent( "Log Batch Compile", "Log message to console when a batch compilation is finished." );
				public static readonly GUIContent UpdateOnSceneSave             = new GUIContent( "Update on Scene save (Ctrl+S)", "ASE is aware of Ctrl+S and will use it to save shader." );
				public static readonly GUIContent DisablePreviews               = new GUIContent( "Disable Node Previews", "Disable preview on nodes from being updated to boost up performance on large graphs." );
				public static readonly GUIContent DisableMaterialMode           = new GUIContent( "Disable Material Mode", "Disable enter Material Mode graph when double-clicking on material asset." );
				public static readonly GUIContent ForceTemplateMinShaderModel   = new GUIContent( "Force Template Min. Shader Model", "If active, when loading a shader its shader model will be replaced by the one specified in template if what is loaded is below the one set over the template." );
				public static readonly GUIContent ForceTemplateInlineProperties = new GUIContent( "Force Template Inline Properties", "If active, defaults all inline properties to template values." );
			}

			private class Defaults
			{
				public const ShowOption StartUp                 = ShowOption.Always;
				public const bool AlwaysSnapToGrid              = true;
				public const bool EnableUndo                    = false;
				public const bool ClearLog                      = true;
				public const bool LogShaderCompile              = false;
				public const bool LogBatchCompile               = false;
				public const bool UpdateOnSceneSave             = true;
				public const bool DisablePreviews               = false;
				public const bool DisableMaterialMode           = false;
				public const bool ForceTemplateMinShaderModel   = true;
				public const bool ForceTemplateInlineProperties = false;
			}

			// @diogo: make this private
			public class Keys
			{
				public static string StartUp                       = "ASELastSession";
				public static string AlwaysSnapToGrid              = "ASEAlwaysSnapToGrid";
				public static string EnableUndo                    = "ASEEnableUndo";
				public static string ClearLog                      = "ASEClearLog";
				public static string LogShaderCompile              = "ASELogShaderCompile";
				public static string LogBatchCompile               = "ASELogBatchCompile";
				public static string UpdateOnSceneSave             = "ASEUpdateOnSceneSave";
				public static string DisablePreviews               = "ASEActivatePreviews";
				public static string DisableMaterialMode           = "ASEDisableMaterialMode";
				public static string ForceTemplateMinShaderModel   = "ASEForceTemplateMinShaderModel";
				public static string ForceTemplateInlineProperties = "ASEForceTemplateInlineProperties";
			}

			public static ShowOption StartUp                 = Defaults.StartUp;
			public static bool AlwaysSnapToGrid              = Defaults.AlwaysSnapToGrid;
			public static bool EnableUndo                    = Defaults.EnableUndo;
			public static bool ClearLog                      = Defaults.ClearLog;
			public static bool LogShaderCompile              = Defaults.LogShaderCompile;
			public static bool LogBatchCompile               = Defaults.LogBatchCompile;
			public static bool UpdateOnSceneSave             = Defaults.UpdateOnSceneSave;
			public static bool DisablePreviews               = Defaults.DisablePreviews;
			public static bool DisableMaterialMode           = Defaults.DisableMaterialMode;
			public static bool ForceTemplateMinShaderModel   = Defaults.ForceTemplateMinShaderModel;
			public static bool ForceTemplateInlineProperties = Defaults.ForceTemplateInlineProperties;

			public static void ResetSettings()
			{
				EditorPrefs.DeleteKey( Keys.StartUp );
				EditorPrefs.DeleteKey( Keys.AlwaysSnapToGrid );
				EditorPrefs.DeleteKey( Keys.EnableUndo );
				EditorPrefs.DeleteKey( Keys.ClearLog );
				EditorPrefs.DeleteKey( Keys.LogShaderCompile );
				EditorPrefs.DeleteKey( Keys.LogBatchCompile );
				EditorPrefs.DeleteKey( Keys.UpdateOnSceneSave );
				EditorPrefs.DeleteKey( Keys.DisablePreviews );
				EditorPrefs.DeleteKey( Keys.DisableMaterialMode );
				EditorPrefs.DeleteKey( Keys.ForceTemplateMinShaderModel );
				EditorPrefs.DeleteKey( Keys.ForceTemplateInlineProperties );

				StartUp                       = Defaults.StartUp;
				AlwaysSnapToGrid              = Defaults.AlwaysSnapToGrid;
				EnableUndo                    = Defaults.EnableUndo;
				ClearLog                      = Defaults.ClearLog;
				LogShaderCompile              = Defaults.LogShaderCompile;
				LogBatchCompile               = Defaults.LogBatchCompile;
				UpdateOnSceneSave             = Defaults.UpdateOnSceneSave;
				DisablePreviews               = Defaults.DisablePreviews;
				DisableMaterialMode           = Defaults.DisableMaterialMode;
				ForceTemplateMinShaderModel   = Defaults.ForceTemplateMinShaderModel;
				ForceTemplateInlineProperties = Defaults.ForceTemplateInlineProperties;
			}

			public static void LoadSettings()
			{
				StartUp                       = ( ShowOption )EditorPrefs.GetInt( Keys.StartUp, ( int )Defaults.StartUp );
				AlwaysSnapToGrid              = EditorPrefs.GetBool( Keys.AlwaysSnapToGrid, Defaults.AlwaysSnapToGrid );
				EnableUndo                    = EditorPrefs.GetBool( Keys.EnableUndo, Defaults.EnableUndo );
				ClearLog                      = EditorPrefs.GetBool( Keys.ClearLog, Defaults.ClearLog );
				LogShaderCompile              = EditorPrefs.GetBool( Keys.LogShaderCompile, Defaults.LogShaderCompile );
				LogBatchCompile               = EditorPrefs.GetBool( Keys.LogBatchCompile, Defaults.LogBatchCompile );
				UpdateOnSceneSave             = EditorPrefs.GetBool( Keys.UpdateOnSceneSave, Defaults.UpdateOnSceneSave );
				DisablePreviews               = EditorPrefs.GetBool( Keys.DisablePreviews, Defaults.DisablePreviews );
				DisableMaterialMode           = EditorPrefs.GetBool( Keys.DisableMaterialMode, Defaults.DisableMaterialMode );
				ForceTemplateMinShaderModel   = EditorPrefs.GetBool( Keys.ForceTemplateMinShaderModel, Defaults.ForceTemplateMinShaderModel );
				ForceTemplateInlineProperties = EditorPrefs.GetBool( Keys.ForceTemplateInlineProperties, Defaults.ForceTemplateInlineProperties );
			}

			public static void SaveSettings()
			{
				bool prevDisablePreviews = EditorPrefs.GetBool(  Keys.DisablePreviews, false );
				if ( DisablePreviews != prevDisablePreviews )
				{
					UIUtils.ActivatePreviews( !DisablePreviews );
				}

				EditorPrefs.SetInt(  Keys.StartUp, ( int )StartUp );
				EditorPrefs.SetBool( Keys.AlwaysSnapToGrid, AlwaysSnapToGrid );
				EditorPrefs.SetBool( Keys.EnableUndo, EnableUndo );
				EditorPrefs.SetBool( Keys.ClearLog, ClearLog );
				EditorPrefs.SetBool( Keys.LogShaderCompile, LogShaderCompile );
				EditorPrefs.SetBool( Keys.LogBatchCompile, LogBatchCompile );
				EditorPrefs.SetBool( Keys.UpdateOnSceneSave, UpdateOnSceneSave );
				EditorPrefs.SetBool( Keys.DisablePreviews, DisablePreviews );
				EditorPrefs.SetBool( Keys.DisableMaterialMode, DisableMaterialMode );
				EditorPrefs.SetBool( Keys.ForceTemplateMinShaderModel, ForceTemplateMinShaderModel );
				EditorPrefs.SetBool( Keys.ForceTemplateInlineProperties, ForceTemplateInlineProperties );
			}

			public static void InspectorLayout()
			{
				StartUp                       = ( ShowOption )EditorGUILayout.EnumPopup( Styles.StartUp, StartUp );
				AlwaysSnapToGrid              = EditorGUILayout.Toggle( Styles.AlwaysSnapToGrid, AlwaysSnapToGrid );
				EnableUndo                    = EditorGUILayout.Toggle( Styles.EnableUndo, EnableUndo );
				ClearLog                      = EditorGUILayout.Toggle( Styles.ClearLog, ClearLog );
				LogShaderCompile              = EditorGUILayout.Toggle( Styles.LogShaderCompile, LogShaderCompile );
				LogBatchCompile               = EditorGUILayout.Toggle( Styles.LogBatchCompile, LogBatchCompile );
				UpdateOnSceneSave             = EditorGUILayout.Toggle( Styles.UpdateOnSceneSave, UpdateOnSceneSave );
				DisablePreviews               = EditorGUILayout.Toggle( Styles.DisablePreviews, DisablePreviews );
				DisableMaterialMode           = EditorGUILayout.Toggle( Styles.DisableMaterialMode, DisableMaterialMode );
				ForceTemplateMinShaderModel   = EditorGUILayout.Toggle( Styles.ForceTemplateMinShaderModel, ForceTemplateMinShaderModel );
				ForceTemplateInlineProperties = EditorGUILayout.Toggle( Styles.ForceTemplateInlineProperties, ForceTemplateInlineProperties );
			}
		}
	}
}

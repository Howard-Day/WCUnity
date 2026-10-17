// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

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

		[NoAutoStaticsCleanup]
		public class User
		{
			private class Styles
			{
				public static readonly GUIContent StartUp                       = new GUIContent( "Show start screen on Unity launch", "You can set if you want to see the start screen everytime Unity launchs, only just when there's a new version available or never." );
				public static readonly GUIContent AlwaysSnapToGrid              = new GUIContent( "Always Snap to Grid", "Always snap to grid when dragging nodes around, instead of using control." );
				public static readonly GUIContent ShowReferenceWires            = new GUIContent( "Show Reference Wires on Selection", "Draw dashed wires linking reference nodes to their source node while either end is selected." );
				public static readonly GUIContent EnableUndo                    = new GUIContent( "Enable Undo", "Enables undo for actions within the shader graph canvas." );
				public static readonly GUIContent CompressUndoSnapshots         = new GUIContent( "Compress Undo Snapshots (experimental)", "Stores undo snapshots Deflate-compressed to cut undo memory usage. Disable to compare against uncompressed behavior." );
				public static readonly GUIContent IncrementalUndo               = new GUIContent( "Incremental Undo (experimental)", "Applies undo/redo by patching only the nodes and wires that changed, verifying the result against the snapshot checksum; any mismatch falls back to a full reload. Requires Enable Undo." );
				public static readonly GUIContent OptimizedBranching            = new GUIContent( "Optimized Branching (experimental)", "Generates runtime branch (if/else) contents inside the branch scope so only the taken path executes. Disable to restore legacy flat generation for debugging." );
				public static readonly GUIContent ClearLog                      = new GUIContent( "Clear Log on Update", "Clears the previously generated log each time the Update button is pressed." );
				public static readonly GUIContent LogShaderLoad                 = new GUIContent( "Log Shader Load", "Log message to console when a shader loading is finished." );
				public static readonly GUIContent LogShaderCompile              = new GUIContent( "Log Shader Compile", "Log message to console when a shader compilation is finished." );
				public static readonly GUIContent LogBatchCompile               = new GUIContent( "Log Batch Compile", "Log message to console when a batch compilation is finished." );
				public static readonly GUIContent GraphLogToConsole             = new GUIContent( "Graph Log to Console", "Mirror all ASE log entries to Unity's Console window, prefixed with the shader path for traceability." );
				public static readonly GUIContent UpdateOnSceneSave             = new GUIContent( "Update on Scene save (Ctrl+S)", "ASE is aware of Ctrl+S and will use it to save shader." );
				public static readonly GUIContent PreviewUpdateFrequency        = new GUIContent( "Preview Update Frequency", "Frequency limit, in Hz/FPS, at which we allow previews to refresh." );
				public static readonly GUIContent PreviewQuality                = new GUIContent( "Preview Quality", "Adjusts size and precision of preview textures to save VRAM: Low, Medium, High (default), Very High" );
				public static readonly GUIContent DisablePreviews               = new GUIContent( "Disable Node Previews", "Disable preview on nodes from being updated to boost up performance on large graphs." );
				public static readonly GUIContent PooledPreviews                = new GUIContent( "Pool SF Previews", "Release preview textures of nodes inside Shader Functions after they are computed. Drastically cuts VRAM on graphs with many/nested functions; the functions' own output previews are unaffected." );
				public static readonly GUIContent ZoomScaledPreviews            = new GUIContent( "Zoom-Scaled Previews", "Lower the preview texture resolution while the canvas is zoomed out, when full resolution is not visible anyway. Cuts VRAM and preview GPU cost on large graphs; previews re-render at full resolution when zooming back in." );
				public static readonly GUIContent PreviewPickerDecimals         = new GUIContent( "Color Picker Decimals", "Number of decimal places shown by the preview color picker (hold Ctrl over a node preview to read the pixel under the cursor)." );
				public static readonly GUIContent ShowStats                     = new GUIContent( "Show Stats", "Draw a small performance overlay in the top-left of the canvas: frame time, preview compute time, node count, preview-RT pool usage and zoom LOD." );
				public static readonly GUIContent DisableMaterialMode           = new GUIContent( "Disable Material Mode", "Disable enter Material Mode graph when double-clicking on material asset." );
				public static readonly GUIContent ForceTemplateMinShaderModel   = new GUIContent( "Force Template Min. Shader Model", "If active, when loading a shader its shader model will be replaced by the one specified in template if what is loaded is below the one set over the template." );
				public static readonly GUIContent ForceTemplateInlineProperties = new GUIContent( "Force Template Inline Properties", "If active, defaults all inline properties to template values." );
				public static readonly GUIContent UndoProfiling                 = new GUIContent( "Undo Profiling", "Logs undo snapshot and restore timings to the console. The statistics below are always collected regardless of this toggle." );
				public static readonly GUIContent AutoBackupEnabled             = new GUIContent( "Enabled", "Periodically saves a copy of the open shader or shader function to disk as a safety net." );
				public static readonly GUIContent AutoBackupFrequency           = new GUIContent( "Backup Frequency (sec)", "How often, in seconds, a backup is taken while the graph is being edited." );
				public static readonly GUIContent AutoBackupFolder              = new GUIContent( "Backup Folder", "Root folder where backups are stored. Leave empty to use the default ( Library/AmplifyShaderEditor/AutoBackup )." );
				public static readonly GUIContent AutoBackupHistorySize         = new GUIContent( "Backup History Size", "Maximum number of backups kept per shader/shader function. Older backups beyond this count are deleted." );
			}

			private class Defaults
			{
				public const ShowOption StartUp                 = ShowOption.Always;
				public const bool AlwaysSnapToGrid              = true;
				public const bool ShowReferenceWires            = true;
				public const bool EnableUndo                    = true;
				public const bool CompressUndoSnapshots         = true;
				public const bool IncrementalUndo               = true;
				public const bool OptimizedBranching            = true;
				public const bool ClearLog                      = true;
				public const bool LogShaderLoad                 = false;
				public const bool LogShaderCompile              = false;
				public const bool LogBatchCompile               = false;
				public const bool GraphLogToConsole             = false;
				public const bool UpdateOnSceneSave             = true;
				public const int  PreviewUpdateFrequency        = 60;
				public const int  PreviewQuality                = 2;
				public const bool DisablePreviews               = false;
				public const bool PooledPreviews                = true;
				public const bool ZoomScaledPreviews            = true;
				public const int  PreviewPickerDecimals         = 3;
				public const bool ShowStats                     = false;
				public const bool DisableMaterialMode           = false;
				public const bool ForceTemplateMinShaderModel   = false;
				public const bool ForceTemplateInlineProperties = false;
				public const bool UndoProfiling                 = false;
				public const bool   AutoBackupEnabled           = true;
				public const int    AutoBackupFrequency         = 300;
				public const string AutoBackupFolder            = "";
				public const int    AutoBackupHistorySize       = 50;
				public const bool PreviewsFoldout               = false;
				public const bool LoggingFoldout                = false;
				public const bool TemplatesFoldout              = false;
				public const bool AutoBackupFoldout             = false;
			}

			// @diogo: make this private
			[NoAutoStaticsCleanup]
			public class Keys
			{
				public static string StartUp                       = "ASELastSession";
				public static string AlwaysSnapToGrid              = "ASEAlwaysSnapToGrid";
				public static string ShowReferenceWires            = "ASEShowReferenceWires";
				// Key bumped to V2 so the on-by-default change reaches everyone: users who previously
				// stored the old "ASEEnableUndo" key ( default-off era ) fall through to the new default
				// instead of keeping their stale value. They can still opt out under the new key.
				public static string EnableUndo                    = "ASEEnableUndoV2";
				public static string CompressUndoSnapshots         = "ASECompressUndoSnapshots";
				public static string IncrementalUndo               = "ASEIncrementalUndo";
				public static string OptimizedBranching            = "ASEOptimizedBranching";
				public static string ClearLog                      = "ASEClearLog";
				public static string LogShaderLoad                 = "ASELogShaderLoad";
				public static string LogShaderCompile              = "ASELogShaderCompile";
				public static string LogBatchCompile               = "ASELogBatchCompile";
				public static string GraphLogToConsole             = "ASEGraphLogToConsole";
				public static string UpdateOnSceneSave             = "ASEUpdateOnSceneSave";
				public static string PreviewUpdateFrequency        = "ASEPreviewUpdateFrequency";
				public static string PreviewQuality                = "ASEPreviewQuality";
				public static string DisablePreviews               = "ASEActivatePreviews";
				public static string PooledPreviews                = "ASEPooledPreviews";
				public static string ZoomScaledPreviews            = "ASEZoomScaledPreviews";
				public static string PreviewPickerDecimals         = "ASEPreviewPickerDecimals";
				public static string ShowStats                     = "ASEShowStats";
				public static string DisableMaterialMode           = "ASEDisableMaterialMode";
				public static string ForceTemplateMinShaderModel   = "ASEForceTemplateMinShaderModel";
				public static string ForceTemplateInlineProperties = "ASEForceTemplateInlineProperties";
				public static string UndoProfiling                 = "ASEUndoProfiling";
				public static string AutoBackupEnabled             = "ASEAutoBackupEnabled";
				// Key bumped to Sec so the value stored while this was expressed in minutes is not silently
				// reinterpreted as seconds; stale minute-values fall through to the new default instead.
				public static string AutoBackupFrequency           = "ASEAutoBackupFrequencySec";
				public static string AutoBackupFolder              = "ASEAutoBackupFolder";
				public static string AutoBackupHistorySize         = "ASEAutoBackupHistorySize";
				public static string PreviewsFoldout               = "ASEPreviewsFoldout";
				public static string LoggingFoldout                = "ASELoggingFoldout";
				public static string TemplatesFoldout              = "ASETemplatesFoldout";
				public static string AutoBackupFoldout             = "ASEAutoBackupFoldout";
			}

			public static ShowOption StartUp                 = Defaults.StartUp;
			public static bool AlwaysSnapToGrid              = Defaults.AlwaysSnapToGrid;
			public static bool ShowReferenceWires            = Defaults.ShowReferenceWires;
			public static bool EnableUndo                    = Defaults.EnableUndo;
			public static bool CompressUndoSnapshots         = Defaults.CompressUndoSnapshots;
			public static bool IncrementalUndo               = Defaults.IncrementalUndo;
			public static bool OptimizedBranching            = Defaults.OptimizedBranching;
			public static bool ClearLog                      = Defaults.ClearLog;
			public static bool LogShaderLoad                 = Defaults.LogShaderLoad;
			public static bool LogShaderCompile              = Defaults.LogShaderCompile;
			public static bool LogBatchCompile               = Defaults.LogBatchCompile;
			public static bool GraphLogToConsole             = Defaults.GraphLogToConsole;
			public static bool UpdateOnSceneSave             = Defaults.UpdateOnSceneSave;
			public static int  PreviewUpdateFrequency        = Defaults.PreviewUpdateFrequency;
			public static int  PreviewQuality                = Defaults.PreviewQuality;
			public static bool DisablePreviews               = Defaults.DisablePreviews;
			public static bool PooledPreviews                = Defaults.PooledPreviews;
			public static bool ZoomScaledPreviews            = Defaults.ZoomScaledPreviews;
			public static int  PreviewPickerDecimals         = Defaults.PreviewPickerDecimals;
			public static bool ShowStats                     = Defaults.ShowStats;
			public static bool DisableMaterialMode           = Defaults.DisableMaterialMode;
			public static bool ForceTemplateMinShaderModel   = Defaults.ForceTemplateMinShaderModel;
			public static bool ForceTemplateInlineProperties = Defaults.ForceTemplateInlineProperties;
			public static bool UndoProfiling                 = Defaults.UndoProfiling;
			public static bool   AutoBackupEnabled           = Defaults.AutoBackupEnabled;
			public static int    AutoBackupFrequency         = Defaults.AutoBackupFrequency;
			public static string AutoBackupFolder            = Defaults.AutoBackupFolder;
			public static int    AutoBackupHistorySize       = Defaults.AutoBackupHistorySize;

			// Foldout state for the User settings groups ( persisted ); collapsed by default.
			private static bool PreviewsFoldout              = Defaults.PreviewsFoldout;
			private static bool LoggingFoldout               = Defaults.LoggingFoldout;
			private static bool TemplatesFoldout             = Defaults.TemplatesFoldout;

			// Foldout state for the Auto Backup subsection ( persisted ); collapsed by default.
			private static bool AutoBackupFoldout            = Defaults.AutoBackupFoldout;

			public static void ResetSettings()
			{
				EditorPrefs.DeleteKey( Keys.StartUp );
				EditorPrefs.DeleteKey( Keys.AlwaysSnapToGrid );
				EditorPrefs.DeleteKey( Keys.ShowReferenceWires );
				EditorPrefs.DeleteKey( Keys.EnableUndo );
				EditorPrefs.DeleteKey( Keys.CompressUndoSnapshots );
				EditorPrefs.DeleteKey( Keys.IncrementalUndo );
				EditorPrefs.DeleteKey( Keys.OptimizedBranching );
				EditorPrefs.DeleteKey( Keys.ClearLog );
				EditorPrefs.DeleteKey( Keys.LogShaderLoad );
				EditorPrefs.DeleteKey( Keys.LogShaderCompile );
				EditorPrefs.DeleteKey( Keys.LogBatchCompile );
				EditorPrefs.DeleteKey( Keys.GraphLogToConsole );
				EditorPrefs.DeleteKey( Keys.UpdateOnSceneSave );
				EditorPrefs.DeleteKey( Keys.PreviewUpdateFrequency );
				EditorPrefs.DeleteKey( Keys.PreviewQuality );
				EditorPrefs.DeleteKey( Keys.DisablePreviews );
				EditorPrefs.DeleteKey( Keys.PooledPreviews );
				EditorPrefs.DeleteKey( Keys.ZoomScaledPreviews );
				EditorPrefs.DeleteKey( Keys.PreviewPickerDecimals );
				EditorPrefs.DeleteKey( Keys.ShowStats );
				EditorPrefs.DeleteKey( Keys.DisableMaterialMode );
				EditorPrefs.DeleteKey( Keys.ForceTemplateMinShaderModel );
				EditorPrefs.DeleteKey( Keys.ForceTemplateInlineProperties );
				EditorPrefs.DeleteKey( Keys.UndoProfiling );
				EditorPrefs.DeleteKey( Keys.AutoBackupEnabled );
				EditorPrefs.DeleteKey( Keys.AutoBackupFrequency );
				EditorPrefs.DeleteKey( Keys.AutoBackupFolder );
				EditorPrefs.DeleteKey( Keys.AutoBackupHistorySize );
				EditorPrefs.DeleteKey( Keys.PreviewsFoldout );
				EditorPrefs.DeleteKey( Keys.LoggingFoldout );
				EditorPrefs.DeleteKey( Keys.TemplatesFoldout );
				EditorPrefs.DeleteKey( Keys.AutoBackupFoldout );

				StartUp                       = Defaults.StartUp;
				AlwaysSnapToGrid              = Defaults.AlwaysSnapToGrid;
				ShowReferenceWires            = Defaults.ShowReferenceWires;
				EnableUndo                    = Defaults.EnableUndo;
				CompressUndoSnapshots         = Defaults.CompressUndoSnapshots;
				IncrementalUndo               = Defaults.IncrementalUndo;
				OptimizedBranching            = Defaults.OptimizedBranching;
				ClearLog                      = Defaults.ClearLog;
				LogShaderLoad                 = Defaults.LogShaderLoad;
				LogShaderCompile              = Defaults.LogShaderCompile;
				LogBatchCompile               = Defaults.LogBatchCompile;
				GraphLogToConsole             = Defaults.GraphLogToConsole;
				UpdateOnSceneSave             = Defaults.UpdateOnSceneSave;
				PreviewUpdateFrequency        = Defaults.PreviewUpdateFrequency;
				PreviewQuality                = Defaults.PreviewQuality;
				DisablePreviews               = Defaults.DisablePreviews;
				PooledPreviews                = Defaults.PooledPreviews;
				ZoomScaledPreviews            = Defaults.ZoomScaledPreviews;
				PreviewPickerDecimals         = Defaults.PreviewPickerDecimals;
				ShowStats                     = Defaults.ShowStats;
				DisableMaterialMode           = Defaults.DisableMaterialMode;
				ForceTemplateMinShaderModel   = Defaults.ForceTemplateMinShaderModel;
				ForceTemplateInlineProperties = Defaults.ForceTemplateInlineProperties;
				UndoProfiling                 = Defaults.UndoProfiling;
				AutoBackupEnabled             = Defaults.AutoBackupEnabled;
				AutoBackupFrequency           = Defaults.AutoBackupFrequency;
				AutoBackupFolder              = Defaults.AutoBackupFolder;
				AutoBackupHistorySize         = Defaults.AutoBackupHistorySize;
				PreviewsFoldout               = Defaults.PreviewsFoldout;
				LoggingFoldout                = Defaults.LoggingFoldout;
				TemplatesFoldout              = Defaults.TemplatesFoldout;
				AutoBackupFoldout             = Defaults.AutoBackupFoldout;
			}

			public static void LoadSettings()
			{
				StartUp                       = ( ShowOption )EditorPrefs.GetInt( Keys.StartUp, ( int )Defaults.StartUp );
				AlwaysSnapToGrid              = EditorPrefs.GetBool( Keys.AlwaysSnapToGrid, Defaults.AlwaysSnapToGrid );
				ShowReferenceWires            = EditorPrefs.GetBool( Keys.ShowReferenceWires, Defaults.ShowReferenceWires );
				EnableUndo                    = EditorPrefs.GetBool( Keys.EnableUndo, Defaults.EnableUndo );
				OptimizedBranching            = EditorPrefs.GetBool( Keys.OptimizedBranching, Defaults.OptimizedBranching );
				// @diogo: no longer user options - forced to defaults (ON); togglable and persisted only
				// on ASE development builds, via the Developer prefs section (see DeveloperLayout)
				CompressUndoSnapshots         = DebugConsoleWindow.DeveloperMode ? EditorPrefs.GetBool( Keys.CompressUndoSnapshots, Defaults.CompressUndoSnapshots ) : Defaults.CompressUndoSnapshots;
				IncrementalUndo               = DebugConsoleWindow.DeveloperMode ? EditorPrefs.GetBool( Keys.IncrementalUndo, Defaults.IncrementalUndo ) : Defaults.IncrementalUndo;
				ClearLog                      = EditorPrefs.GetBool( Keys.ClearLog, Defaults.ClearLog );
				LogShaderLoad                 = EditorPrefs.GetBool( Keys.LogShaderLoad, Defaults.LogShaderLoad );
				LogShaderCompile              = EditorPrefs.GetBool( Keys.LogShaderCompile, Defaults.LogShaderCompile );
				LogBatchCompile               = EditorPrefs.GetBool( Keys.LogBatchCompile, Defaults.LogBatchCompile );
				GraphLogToConsole             = EditorPrefs.GetBool( Keys.GraphLogToConsole, Defaults.GraphLogToConsole );
				UpdateOnSceneSave             = EditorPrefs.GetBool( Keys.UpdateOnSceneSave, Defaults.UpdateOnSceneSave );
				PreviewUpdateFrequency        = EditorPrefs.GetInt(  Keys.PreviewUpdateFrequency, Defaults.PreviewUpdateFrequency );
				PreviewQuality                = EditorPrefs.GetInt(  Keys.PreviewQuality, Defaults.PreviewQuality );
				DisablePreviews               = EditorPrefs.GetBool( Keys.DisablePreviews, Defaults.DisablePreviews );
				// @diogo: dev-only options, same forced-vs-persisted contract as the undo toggles above
				PooledPreviews                = DebugConsoleWindow.DeveloperMode ? EditorPrefs.GetBool( Keys.PooledPreviews, Defaults.PooledPreviews ) : Defaults.PooledPreviews;
				ZoomScaledPreviews            = DebugConsoleWindow.DeveloperMode ? EditorPrefs.GetBool( Keys.ZoomScaledPreviews, Defaults.ZoomScaledPreviews ) : Defaults.ZoomScaledPreviews;
				PreviewPickerDecimals         = EditorPrefs.GetInt(  Keys.PreviewPickerDecimals, Defaults.PreviewPickerDecimals );
				ShowStats                     = EditorPrefs.GetBool( Keys.ShowStats, Defaults.ShowStats );
				DisableMaterialMode           = EditorPrefs.GetBool( Keys.DisableMaterialMode, Defaults.DisableMaterialMode );
				ForceTemplateMinShaderModel   = EditorPrefs.GetBool( Keys.ForceTemplateMinShaderModel, Defaults.ForceTemplateMinShaderModel );
				ForceTemplateInlineProperties = EditorPrefs.GetBool( Keys.ForceTemplateInlineProperties, Defaults.ForceTemplateInlineProperties );
				UndoProfiling                 = DebugConsoleWindow.DeveloperMode ? EditorPrefs.GetBool( Keys.UndoProfiling, Defaults.UndoProfiling ) : Defaults.UndoProfiling;
				AutoBackupEnabled             = EditorPrefs.GetBool(   Keys.AutoBackupEnabled, Defaults.AutoBackupEnabled );
				AutoBackupFrequency           = EditorPrefs.GetInt(    Keys.AutoBackupFrequency, Defaults.AutoBackupFrequency );
				AutoBackupFolder              = EditorPrefs.GetString( Keys.AutoBackupFolder, Defaults.AutoBackupFolder );
				AutoBackupHistorySize         = EditorPrefs.GetInt(    Keys.AutoBackupHistorySize, Defaults.AutoBackupHistorySize );
				PreviewsFoldout               = EditorPrefs.GetBool( Keys.PreviewsFoldout, Defaults.PreviewsFoldout );
				LoggingFoldout                = EditorPrefs.GetBool( Keys.LoggingFoldout, Defaults.LoggingFoldout );
				TemplatesFoldout              = EditorPrefs.GetBool( Keys.TemplatesFoldout, Defaults.TemplatesFoldout );
				AutoBackupFoldout             = EditorPrefs.GetBool( Keys.AutoBackupFoldout, Defaults.AutoBackupFoldout );
			}

			public static void SaveSettings()
			{
				bool prevDisablePreviews = EditorPrefs.GetBool(  Keys.DisablePreviews, false );
				if ( DisablePreviews != prevDisablePreviews )
				{
					UIUtils.ActivatePreviews( !DisablePreviews );
				}

				// @diogo: toggling pooled SF previews changes which preview RTs are retained; force a
				// full preview refresh so the new regime releases/reallocates on the next pass.
				bool prevPooledPreviews = EditorPrefs.GetBool(  Keys.PooledPreviews, false );
				if ( PooledPreviews != prevPooledPreviews )
				{
					UIUtils.ActivatePreviews( true );
				}

				EditorPrefs.SetInt(  Keys.StartUp, ( int )StartUp );
				EditorPrefs.SetBool( Keys.AlwaysSnapToGrid, AlwaysSnapToGrid );
				EditorPrefs.SetBool( Keys.ShowReferenceWires, ShowReferenceWires );
				EditorPrefs.SetBool( Keys.EnableUndo, EnableUndo );
				EditorPrefs.SetBool( Keys.OptimizedBranching, OptimizedBranching );
				EditorPrefs.SetBool( Keys.CompressUndoSnapshots, CompressUndoSnapshots );
				EditorPrefs.SetBool( Keys.IncrementalUndo, IncrementalUndo );
				EditorPrefs.SetBool( Keys.ClearLog, ClearLog );
				EditorPrefs.SetBool( Keys.LogShaderLoad, LogShaderLoad );
				EditorPrefs.SetBool( Keys.LogShaderCompile, LogShaderCompile );
				EditorPrefs.SetBool( Keys.LogBatchCompile, LogBatchCompile );
				EditorPrefs.SetBool( Keys.GraphLogToConsole, GraphLogToConsole );
				EditorPrefs.SetBool( Keys.UpdateOnSceneSave, UpdateOnSceneSave );
				EditorPrefs.SetInt(  Keys.PreviewUpdateFrequency, PreviewUpdateFrequency );
				EditorPrefs.SetInt(  Keys.PreviewQuality, PreviewQuality );
				EditorPrefs.SetBool( Keys.DisablePreviews, DisablePreviews );
				EditorPrefs.SetBool( Keys.PooledPreviews, PooledPreviews );
				EditorPrefs.SetBool( Keys.ZoomScaledPreviews, ZoomScaledPreviews );
				EditorPrefs.SetInt(  Keys.PreviewPickerDecimals, PreviewPickerDecimals );
				EditorPrefs.SetBool( Keys.ShowStats, ShowStats );
				EditorPrefs.SetBool( Keys.DisableMaterialMode, DisableMaterialMode );
				EditorPrefs.SetBool( Keys.ForceTemplateMinShaderModel, ForceTemplateMinShaderModel );
				EditorPrefs.SetBool( Keys.ForceTemplateInlineProperties, ForceTemplateInlineProperties );
				EditorPrefs.SetBool( Keys.UndoProfiling, UndoProfiling );
				EditorPrefs.SetBool( Keys.AutoBackupEnabled, AutoBackupEnabled );
				EditorPrefs.SetInt( Keys.AutoBackupFrequency, AutoBackupFrequency );
				EditorPrefs.SetString( Keys.AutoBackupFolder, AutoBackupFolder );
				EditorPrefs.SetInt( Keys.AutoBackupHistorySize, AutoBackupHistorySize );
				EditorPrefs.SetBool( Keys.PreviewsFoldout, PreviewsFoldout );
				EditorPrefs.SetBool( Keys.LoggingFoldout, LoggingFoldout );
				EditorPrefs.SetBool( Keys.TemplatesFoldout, TemplatesFoldout );
				EditorPrefs.SetBool( Keys.AutoBackupFoldout, AutoBackupFoldout );
			}

			static readonly string[] FrequencyOptions = { "30 hz", "60 hz", "120 hz", "240 hz", "Unlimited" };
			static readonly int[] FrequencyOptionsIndexToValue = { 30, 60, 120, 240, 10000 };
			static readonly Dictionary<int, int> FrequencyOptionsValueToIndex = new Dictionary<int, int>()
			{
				{ FrequencyOptionsIndexToValue[ 0 ], 0 },
				{ FrequencyOptionsIndexToValue[ 1 ], 1 },
				{ FrequencyOptionsIndexToValue[ 2 ], 2 },
				{ FrequencyOptionsIndexToValue[ 3 ], 3 },
				{ FrequencyOptionsIndexToValue[ 4 ], 4 }
			};

			static readonly string[] PreviewQualityOptions = { "Low", "Medium", "High", "Very High" };

			// @diogo: option index doubles as the decimal count, so no index/value mapping is needed
			static readonly string[] PreviewPickerDecimalsOptions = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

			public static int PreviewSize { get { return ( PreviewQuality < 2 ) ? 64 : ( ( PreviewQuality < 3 ) ? 128 : 256 ); } }
			public static RenderTextureFormat PreviewFormat { get { return ( PreviewQuality < 1 ) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGBFloat; } }

			public static void InspectorLayout()
			{
				StartUp                       = ( ShowOption )EditorGUILayout.EnumPopup( Styles.StartUp, StartUp );
				AlwaysSnapToGrid              = EditorGUILayout.Toggle( Styles.AlwaysSnapToGrid, AlwaysSnapToGrid );
				ShowReferenceWires            = EditorGUILayout.Toggle( Styles.ShowReferenceWires, ShowReferenceWires );
				EnableUndo                    = EditorGUILayout.Toggle( Styles.EnableUndo, EnableUndo );
				UpdateOnSceneSave             = EditorGUILayout.Toggle( Styles.UpdateOnSceneSave, UpdateOnSceneSave );
				DisableMaterialMode           = EditorGUILayout.Toggle( Styles.DisableMaterialMode, DisableMaterialMode );
				OptimizedBranching            = EditorGUILayout.Toggle( Styles.OptimizedBranching, OptimizedBranching );

				PreviewsFoldout = EditorGUILayout.Foldout( PreviewsFoldout, "Previews", true );
				if ( PreviewsFoldout )
				{
					EditorGUI.indentLevel++;
					PreviewUpdateFrequency        = FrequencyOptionsIndexToValue[ EditorGUILayout.Popup( Styles.PreviewUpdateFrequency, FrequencyOptionsValueToIndex[ PreviewUpdateFrequency ], FrequencyOptions ) ];
					PreviewQuality                = ( int )EditorGUILayout.Popup( Styles.PreviewQuality, PreviewQuality, PreviewQualityOptions );
					DisablePreviews               = EditorGUILayout.Toggle( Styles.DisablePreviews, DisablePreviews );
					PreviewPickerDecimals         = EditorGUILayout.Popup( Styles.PreviewPickerDecimals, PreviewPickerDecimals, PreviewPickerDecimalsOptions );
					ShowStats                     = EditorGUILayout.Toggle( Styles.ShowStats, ShowStats );
					EditorGUI.indentLevel--;
				}

				LoggingFoldout = EditorGUILayout.Foldout( LoggingFoldout, "Logging", true );
				if ( LoggingFoldout )
				{
					EditorGUI.indentLevel++;
					ClearLog                      = EditorGUILayout.Toggle( Styles.ClearLog, ClearLog );
					LogShaderLoad                 = EditorGUILayout.Toggle( Styles.LogShaderLoad, LogShaderLoad );
					LogShaderCompile              = EditorGUILayout.Toggle( Styles.LogShaderCompile, LogShaderCompile );
					LogBatchCompile               = EditorGUILayout.Toggle( Styles.LogBatchCompile, LogBatchCompile );
					GraphLogToConsole             = EditorGUILayout.Toggle( Styles.GraphLogToConsole, GraphLogToConsole );
					EditorGUI.indentLevel--;
				}

				TemplatesFoldout = EditorGUILayout.Foldout( TemplatesFoldout, "Templates", true );
				if ( TemplatesFoldout )
				{
					EditorGUI.indentLevel++;
					ForceTemplateMinShaderModel   = EditorGUILayout.Toggle( Styles.ForceTemplateMinShaderModel, ForceTemplateMinShaderModel );
					ForceTemplateInlineProperties = EditorGUILayout.Toggle( Styles.ForceTemplateInlineProperties, ForceTemplateInlineProperties );
					EditorGUI.indentLevel--;
				}
			}

			// Auto Backup subsection: periodic disk snapshots of the open graph as a crash/oops safety net.
			// Backups are taken by the editor window's timer; this only edits the settings that drive it.
			public static void AutoBackupLayout()
			{
				AutoBackupFoldout = EditorGUILayout.Foldout( AutoBackupFoldout, "Auto Backup", true );
				if ( !AutoBackupFoldout )
				{
					return;
				}

				EditorGUI.indentLevel++;
				AutoBackupEnabled = EditorGUILayout.Toggle( Styles.AutoBackupEnabled, AutoBackupEnabled );

				using ( new EditorGUI.DisabledScope( !AutoBackupEnabled ) )
				{
					AutoBackupFrequency = Mathf.Max( 1, EditorGUILayout.IntField( Styles.AutoBackupFrequency, AutoBackupFrequency ) );

					EditorGUILayout.BeginHorizontal();
					// Show the actual folder in use: the custom value, or the default when none is set, as a
					// project-relative path unless it lives outside the project. Only commit on an explicit
					// edit so an untouched field keeps the empty ( = default ) preference.
					string effectiveFolder = string.IsNullOrEmpty( AutoBackupFolder ) ? AutoBackup.DefaultRelativeFolder : AutoBackupFolder;
					string shownFolder = AutoBackup.ToProjectRelative( effectiveFolder );
					EditorGUI.BeginChangeCheck();
					shownFolder = EditorGUILayout.TextField( Styles.AutoBackupFolder, shownFolder );
					if ( EditorGUI.EndChangeCheck() )
					{
						AutoBackupFolder = shownFolder;
					}
					if ( GUILayout.Button( "Browse", GUILayout.Width( 60 ) ) )
					{
						string picked = EditorUtility.OpenFolderPanel( "Auto Backup Folder", AutoBackup.ResolveRoot(), string.Empty );
						if ( !string.IsNullOrEmpty( picked ) )
						{
							AutoBackupFolder = AutoBackup.ToProjectRelative( picked );
							GUI.FocusControl( null );
						}
					}
					EditorGUILayout.EndHorizontal();

					AutoBackupHistorySize = Mathf.Max( 1, EditorGUILayout.IntField( Styles.AutoBackupHistorySize, AutoBackupHistorySize ) );
				}
				EditorGUI.indentLevel--;
			}

			// Developer/diagnostics section: the undo profiling toggle plus read-only counters from
			// UndoProfiler. The size/footprint rows read the focused editor's live snapshot, so they show
			// "-" when no shader graph window is open.
			public static void DeveloperLayout()
			{
				// @diogo: these ship forced to defaults and hidden from users; exposed here for A/B and
				// debugging (persisted in developer mode only - see LoadSettings)
				CompressUndoSnapshots = EditorGUILayout.Toggle( Styles.CompressUndoSnapshots, CompressUndoSnapshots );
				IncrementalUndo = EditorGUILayout.Toggle( Styles.IncrementalUndo, IncrementalUndo );
				PooledPreviews = EditorGUILayout.Toggle( Styles.PooledPreviews, PooledPreviews );
				ZoomScaledPreviews = EditorGUILayout.Toggle( Styles.ZoomScaledPreviews, ZoomScaledPreviews );
				UndoProfiling = EditorGUILayout.Toggle( Styles.UndoProfiling, UndoProfiling );

				EditorGUILayout.Space( 5 );
				EditorGUILayout.LabelField( "Undo Statistics", EditorStyles.boldLabel );

				AmplifyShaderEditorWindow window = UIUtils.CurrentWindow;
				bool hasWindow = window != null;
				int liveBytes = hasWindow ? window.CurrentUndoSnapshotStoredBytes : 0;
				int liveNodes = ( hasWindow && window.MainGraphInstance != null ) ? window.MainGraphInstance.AllNodes.Count : 0;

				EditorGUILayout.LabelField( "Active graph nodes", hasWindow ? liveNodes.ToString() : "-" );
				EditorGUILayout.LabelField( "Current snapshot size (stored)", hasWindow ? FormatKB( liveBytes ) : "-" );
				EditorGUILayout.LabelField( "Undo steps recorded", UndoProfiler.StepsRecorded.ToString() );
				EditorGUILayout.LabelField( "Est. undo memory", hasWindow ? FormatKB( ( long )UndoProfiler.StepsRecorded * liveBytes ) : "-" );
				EditorGUILayout.LabelField( "Snapshots serialized", UndoProfiler.SerializeCount.ToString() );
				EditorGUILayout.LabelField( "Serialize time (last / avg)", string.Format( "{0:0.00} / {1:0.00} ms", UndoProfiler.LastSerializeMs, UndoProfiler.AverageSerializeMs ) );
				EditorGUILayout.LabelField( "Compress time (last / avg)", string.Format( "{0:0.00} / {1:0.00} ms", UndoProfiler.LastCompressMs, UndoProfiler.AverageCompressMs ) );
				EditorGUILayout.LabelField( "Restores performed", UndoProfiler.RestoreCount.ToString() );
				EditorGUILayout.LabelField( "Restore time (last / avg)", string.Format( "{0:0.00} / {1:0.00} ms", UndoProfiler.LastRestoreMs, UndoProfiler.AverageRestoreMs ) );
				EditorGUILayout.LabelField( "Restore breakdown (load / refresh / view)", string.Format( "{0:0.00} / {1:0.00} / {2:0.00} ms", UndoProfiler.LastRestoreLoadMs, UndoProfiler.LastRestoreRefreshMs, UndoProfiler.LastRestoreViewStateMs ) );
				EditorGUILayout.LabelField( "Restored node count", UndoProfiler.LastRestoreNodeCount.ToString() );
				EditorGUILayout.LabelField( "LoadFromMeta (clean / type / create / add / wire / post / other)", string.Format( "{0:0.00} / {1:0.00} / {2:0.00} / {3:0.00} / {4:0.00} / {5:0.00} / {6:0.00} ms", UndoProfiler.LastLoadCleanMs, UndoProfiler.LastLoadTypeResolveMs, UndoProfiler.LastLoadCreateMs, UndoProfiler.LastLoadAddNodeMs, UndoProfiler.LastLoadWireMs, UndoProfiler.LastLoadPostMs, UndoProfiler.LastLoadOtherMs ) );
				EditorGUILayout.LabelField( "LoadFromMeta nodes / nested calls", string.Format( "{0} / {1}", UndoProfiler.LastLoadNodeCount, UndoProfiler.LastLoadNestedCalls ) );
				EditorGUILayout.LabelField( "Shadow diff (runs / bails)", string.Format( "{0} / {1}", UndoProfiler.ShadowDiffCount, UndoProfiler.ShadowBailCount ) );
				EditorGUILayout.LabelField( "Last shadow diff", UndoProfiler.LastShadowSummary );
				EditorGUILayout.LabelField( "Incremental patch (attempts / patched / bailed / verify-failed)", string.Format( "{0} / {1} / {2} / {3}", UndoProfiler.PatchAttemptCount, UndoProfiler.PatchSuccessCount, UndoProfiler.PatchBailCount, UndoProfiler.PatchVerifyFailCount ) );
				EditorGUILayout.LabelField( "Last incremental patch", UndoProfiler.LastPatchSummary );
				UndoProfiler.InjectVerifyFailure = EditorGUILayout.Toggle( "Inject verify failure (next patch)", UndoProfiler.InjectVerifyFailure );
				UndoProfiler.InjectPatchException = EditorGUILayout.Toggle( "Inject patch exception (next patch)", UndoProfiler.InjectPatchException );
				EditorGUILayout.LabelField( "Total serialized (session)", FormatMB( UndoProfiler.TotalSerializedChars ) );
				EditorGUILayout.LabelField( "Total compressed (session)", FormatMB( UndoProfiler.TotalCompressedBytes ) );

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if ( GUILayout.Button( "Reset Statistics" ) )
				{
					UndoProfiler.ResetStatistics();
				}
				EditorGUILayout.EndHorizontal();
			}

			private static string FormatKB( long chars )
			{
				return string.Format( "{0:0.0} KB", chars / 1024.0 );
			}

			private static string FormatMB( long chars )
			{
				return string.Format( "{0:0.00} MB", chars / ( 1024.0 * 1024.0 ) );
			}
		}
	}
}

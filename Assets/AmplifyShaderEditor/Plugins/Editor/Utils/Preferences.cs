// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	public partial class Preferences
	{
		[SettingsProvider]
		public static SettingsProvider AmplifyShaderEditorSettings()
		{
			var provider = new SettingsProvider( "Preferences/Amplify Shader Editor", SettingsScope.User )
			{
				guiHandler = ( string searchContext ) =>
				{
					PreferencesGUI();
				},

				keywords = new HashSet<string>( new[] { "start", "screen", "import", "shader", "templates", "macros", "macros", "define", "symbol" } ),

			};
			return provider;
		}

		private static void ResetSettings()
		{
			User.ResetSettings();
			Project.ResetSettings();

			User.SaveSettings();
			Project.SaveSettings();
		}

		private static void LoadSettings()
		{
			User.LoadSettings();
			Project.LoadSettings();
		}

		public static void Initialize()
		{
			LoadSettings();
		}

		public static void PreferencesGUI()
		{
			var cache = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 250;

			EditorGUILayout.Space( 5 );

			EditorGUI.BeginChangeCheck();
			{
				EditorGUILayout.LabelField( "User", EditorStyles.boldLabel );
				User.InspectorLayout();
				User.AutoBackupLayout();
			}
			if ( EditorGUI.EndChangeCheck() )
			{
				User.SaveSettings();
			}

			EditorGUILayout.Space( 5 );

			EditorGUI.BeginChangeCheck();
			{
				EditorGUILayout.LabelField( "Project", EditorStyles.boldLabel );
				Project.InspectorLayout();
			}
			if ( EditorGUI.EndChangeCheck() )
			{
				Project.SaveSettings();
			}

			// @diogo: dev-only diagnostics (undo toggles, profiling, statistics, fault injection); hidden
			// from users - shown only on ASE development builds (see ASE_CONSOLE_WINDOW)
			if ( DebugConsoleWindow.DeveloperMode )
			{
				EditorGUILayout.Space( 5 );

				EditorGUI.BeginChangeCheck();
				{
					EditorGUILayout.LabelField( "Developer", EditorStyles.boldLabel );
					User.DeveloperLayout();
				}
				if ( EditorGUI.EndChangeCheck() )
				{
					User.SaveSettings();
				}
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if( GUILayout.Button( "Reset and Forget All" ) )
			{
				ResetSettings();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUIUtility.labelWidth = cache;
		}
	}
}

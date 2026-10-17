// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	public partial class Preferences
	{
		[NoAutoStaticsCleanup]
		public class Project
		{
			public static bool AutoSRP                => Values.AutoSRP;
			public static bool DefineSymbol           => Values.DefineSymbol;
			public static string[] TemplateExtensions => Values.TemplateExtensions;

			private class Styles
			{
				public static readonly GUIContent AutoSRP            = new GUIContent( "Auto import SRP shader templates", "By default Amplify Shader Editor checks for your SRP version and automatically imports the correct corresponding shader templates.\nTurn this OFF if you prefer to import them manually." );
				public static readonly GUIContent DefineSymbol       = new GUIContent( "Add Amplify Shader Editor define symbol", "Turning it OFF will disable the automatic insertion of the define symbol and remove it from the list while turning it ON will do the opposite.\nThis is used for compatibility with other plugins, if you are not sure if you need this leave it ON." );
				public static readonly GUIContent TemplateExtensions = new GUIContent( "Template Extensions", "Supported file extensions for parsing shader templates." );
			}

			private class Defaults
			{
				public static readonly int Version                 = 1;
				public static readonly bool AutoSRP                = true;
				public static readonly bool DefineSymbol           = true;
				public static readonly string[] TemplateExtensions = { ".shader", ".template.shader" };
			}

			[Serializable]
			private struct Layout
			{
				public int Version;
				public bool AutoSRP;
				public bool DefineSymbol;
				public string[] TemplateExtensions;
			}

			private const string RelativePath = "ProjectSettings/AmplifyShaderEditor.asset";
			private static string FullPath = Path.GetFullPath( RelativePath );
			private static Layout Values = new Layout();
			private static long Timestamp = 0;

			public static void ResetSettings()
			{
				Values.Version            = Defaults.Version;
				Values.AutoSRP            = Defaults.AutoSRP;
				Values.DefineSymbol       = Defaults.DefineSymbol;
				Values.TemplateExtensions = Defaults.TemplateExtensions;
			}

			public static void LoadSettings()
			{
				try
				{
					Values = JsonUtility.FromJson<Layout>( File.ReadAllText( FullPath ) );
					Timestamp = File.GetLastWriteTime( FullPath ).Ticks;

					if ( Values.TemplateExtensions == null || Values.TemplateExtensions.Length == 0 )
					{
						// Layout for Template Extensions changed and won't get deserialized from older versions; recreate, if applicable
						Values.TemplateExtensions = Defaults.TemplateExtensions;
					}
				}
				catch ( System.Exception e )
				{
					if ( e.GetType() == typeof( FileNotFoundException ) )
					{
						ResetSettings();
						SaveSettings();
					}
					else
					{
						Debug.LogWarning( "[AmplifyTexture] Failed importing \"" + RelativePath + "\". Reverting to default settings." );
					}
				}
			}

			public static void SaveSettings()
			{
				if ( DefineSymbol )
				{
					IOUtils.SetAmplifyDefineSymbolOnBuildTargetGroup( EditorUserBuildSettings.selectedBuildTargetGroup );
				}
				else
				{
					IOUtils.RemoveAmplifyDefineSymbolOnBuildTargetGroup( EditorUserBuildSettings.selectedBuildTargetGroup );
				}

				try
				{
					File.WriteAllText( FullPath, JsonUtility.ToJson( Values, true ) );
				}
				catch ( System.Exception )
				{
					// TODO: Not critical?
				}
			}

			private static char[] Separators = { ';', ',', ' ' }; // All separators are converted to the first one [0]
			private static string ExtensionsArrayToString( string[] extensions )
			{
				return string.Join( Separators[ 0 ].ToString(), extensions );
			}
			private static string[] ExtensionsStringToArray( string extensions )
			{
				return extensions.Split( Separators, StringSplitOptions.RemoveEmptyEntries )
								 .Select( s => ( "." + s.TrimStart( '.' ) ).ToLowerInvariant() )
								 .ToArray();;
			}

			public static void InspectorLayout()
			{
				bool fileExists = File.Exists( FullPath );
				if ( fileExists )
				{
					long fileTimestamp = File.GetLastWriteTime( FullPath ).Ticks;
					if ( fileTimestamp != Timestamp )
					{
						LoadSettings();
						Timestamp = fileTimestamp;
					}
				}

				EditorGUI.BeginChangeCheck();

				Values.AutoSRP            = EditorGUILayout.Toggle( Styles.AutoSRP, Values.AutoSRP );
				Values.DefineSymbol       = EditorGUILayout.Toggle( Styles.DefineSymbol, Values.DefineSymbol );
				string templateExtensions = EditorGUILayout.TextField( Styles.TemplateExtensions, ExtensionsArrayToString( Values.TemplateExtensions ) );

				if ( !fileExists || EditorGUI.EndChangeCheck() )
				{
					Values.TemplateExtensions = ExtensionsStringToArray( templateExtensions );
					SaveSettings();
				}
			}
		}
	}
}

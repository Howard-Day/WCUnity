// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	public partial class Preferences
	{
		public class Project
		{
			public static bool AutoSRP              => Values.AutoSRP;
			public static bool DefineSymbol         => Values.DefineSymbol;
			public static string TemplateExtensions => Values.TemplateExtensions;

			private class Styles
			{
				public static readonly GUIContent AutoSRP            = new GUIContent( "Auto import SRP shader templates", "By default Amplify Shader Editor checks for your SRP version and automatically imports the correct corresponding shader templates.\nTurn this OFF if you prefer to import them manually." );
				public static readonly GUIContent DefineSymbol       = new GUIContent( "Add Amplify Shader Editor define symbol", "Turning it OFF will disable the automatic insertion of the define symbol and remove it from the list while turning it ON will do the opposite.\nThis is used for compatibility with other plugins, if you are not sure if you need this leave it ON." );
				public static readonly GUIContent TemplateExtensions = new GUIContent( "Template Extensions", "Supported file extensions for parsing shader templates." );
			}

			private class Defaults
			{
				public const bool AutoSRP              = true;
				public const bool DefineSymbol         = true;
				public const string TemplateExtensions = ".shader;.shader.template";
			}
		
			[Serializable]
			private struct Layout
			{
				public bool AutoSRP;
				public bool DefineSymbol;
				public string TemplateExtensions;
			}

			private const string RelativePath = "ProjectSettings/AmplifyShaderEditor.asset";
			private static string FullPath = Path.GetFullPath( RelativePath );
			private static Layout Values = new Layout();

			public static void ResetSettings()
			{
				Values.AutoSRP            = Defaults.AutoSRP;
				Values.DefineSymbol       = Defaults.DefineSymbol;
				Values.TemplateExtensions = Defaults.TemplateExtensions;
			}

			public static void LoadSettings()
			{
				try
				{
					Values = JsonUtility.FromJson<Layout>( File.ReadAllText( FullPath ) );
				}
				catch ( System.Exception e )
				{
					if ( e.GetType() == typeof( FileNotFoundException ) )
					{
						ResetSettings();

						// @diogo: try to retrieve the old preferences if file does not exist yet
						Values.AutoSRP      = EditorPrefs.GetBool( "ASEAutoSRP", Defaults.AutoSRP );
						Values.DefineSymbol = EditorPrefs.GetBool( "ASEDefineSymbol", Defaults.DefineSymbol );

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
					File.WriteAllText( FullPath, JsonUtility.ToJson( Values ) );
				}
				catch ( System.Exception )
				{
					// TODO: Not critical?
				}
			}

			public static void InspectorLayout()
			{
				Values.AutoSRP            = EditorGUILayout.Toggle( Styles.AutoSRP, Values.AutoSRP );
				Values.DefineSymbol       = EditorGUILayout.Toggle( Styles.DefineSymbol, Values.DefineSymbol );
				Values.TemplateExtensions = EditorGUILayout.TextField( Styles.TemplateExtensions, Values.TemplateExtensions );
			}
		}
	}
}

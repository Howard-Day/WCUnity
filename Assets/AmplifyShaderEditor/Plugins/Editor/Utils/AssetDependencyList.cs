// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	// Inspector section listing the shader functions and shaders whose graphs reference an asset,
	// with the option to open and resave them.
	public class AssetDependencyList
	{
		private class Dependency
		{
			public bool Selected = true;
			public string AssetName;
			public string AssetPath;
			public Dependency( string name, string path )
			{
				Selected = true;
				AssetName = name;
				AssetPath = path;
			}
		}

		private List<Dependency> m_dependencies = new List<Dependency>();

		public void Draw( UnityEngine.Object asset, string assetKind )
		{
			if ( GUILayout.Button( "Search Direct Dependencies" ) )
			{
				m_dependencies.Clear();
				string guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( asset ) );

				string[] allSFs = AssetDatabase.FindAssets( "t:AmplifyShaderFunction", null );
				foreach ( string guid1 in allSFs )
				{
					string sfPath = AssetDatabase.GUIDToAssetPath( guid1 );
					bool found = AmplifyShaderFunctionEditor.SearchForGUID( guid, sfPath );
					if ( found )
					{
						string n = Path.GetFileName( sfPath );
						m_dependencies.Add( new Dependency( n, sfPath ) );
					}
				}

				string[] allSHs = AssetDatabase.FindAssets( "t:Shader", null );
				foreach ( string guid1 in allSHs )
				{
					string shPath = AssetDatabase.GUIDToAssetPath( guid1 );
					bool found = AmplifyShaderFunctionEditor.SearchForGUID( guid, shPath );
					if ( found )
					{
						string n = Path.GetFileName( shPath );
						m_dependencies.Add( new Dependency( n, shPath ) );
					}
				}
			}

			GUI.enabled = ( m_dependencies.Count > 0 );

			int selectedCount = 0;
			for ( int i = 0; i < m_dependencies.Count; i++ )
			{
				selectedCount += m_dependencies[ i ].Selected ? 1 : 0;
			}

			if ( GUILayout.Button( "Open and Save " + ( selectedCount == m_dependencies.Count ? "All" : "Selected" ) ) )
			{
				List<string> assetPaths = new List<string>();
				for ( int i = 0; i < m_dependencies.Count; i++ )
				{
					if ( m_dependencies[ i ].Selected )
					{
						assetPaths.Add( m_dependencies[ i ].AssetPath );
					}
				}

				bool doit = EditorUtility.DisplayDialog( "Open and Save All", "This will try to open all shader function and shaders that use this " + assetKind + " and save them in quick succession, this may irreversibly break your files if something goes wrong. Are you sure you want to try?", "Yes, I'll take the risk", "No, I'll do it myself" );
				if ( doit )
				{
					AmplifyShaderEditorWindow.LoadAndSaveList( assetPaths.ToArray() );
				}
			}
			GUI.enabled = true;

			EditorGUILayout.Space();

			for ( int i = 0; i < m_dependencies.Count; i++ )
			{
				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.BeginVertical( GUILayout.Width( 12 ) );
				GUILayout.FlexibleSpace();
				m_dependencies[ i ].Selected = GUILayout.Toggle( m_dependencies[ i ].Selected, "", GUILayout.Width( 12 ), GUILayout.Height( 20 ) );
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndVertical();

				GUILayout.Space( 3 );

				if ( GUILayout.Button( new GUIContent( m_dependencies[ i ].AssetName, m_dependencies[ i ].AssetPath ), "minibuttonleft" ) )
				{
					SelectAtPath( m_dependencies[ i ].AssetPath );
				}

				if ( GUILayout.Button( "edit", "minibuttonright", GUILayout.Width( 100 ) ) )
				{
					if ( m_dependencies[ i ].AssetName.EndsWith( ".asset" ) )
					{
						AmplifyShaderFunction obj = AssetDatabase.LoadAssetAtPath<AmplifyShaderFunction>( m_dependencies[ i ].AssetPath );
						AmplifyShaderEditorWindow.LoadShaderFunctionToASE( obj, false );
					}
					else
					{
						Shader obj = AssetDatabase.LoadAssetAtPath<Shader>( m_dependencies[ i ].AssetPath );
						AmplifyShaderEditorWindow.ConvertShaderToASE( obj );
					}
				}

				EditorGUILayout.EndHorizontal();
			}
		}

		private static void SelectAtPath( string path )
		{
			UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( path );
			EditorGUIUtility.PingObject( obj );
		}
	}
}

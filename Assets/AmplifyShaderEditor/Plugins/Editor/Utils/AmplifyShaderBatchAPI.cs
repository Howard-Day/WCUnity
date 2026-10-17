// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	// scriptable entry points for regenerating ( "resaving" ) the shaders of a Batch Set. These start
	// ASE's existing batch resave ( InitBatch + LoadAndSaveList ), which reloads each shader's graph and
	// writes it back to disk. NOTE: ASE's batch is driven by the editor window's OnGUI state machine and
	// runs asynchronously across editor frames; it does not block and is not currently headless-safe under
	// -batchmode ( see docs/headless-batch-processing.md ). Each method returns the number of shaders
	// submitted, not completed. The Open* counterparts take the same inputs but only load each shader into
	// the editor ( LoadList ), one tab per shader, and leave it there. Nothing is written to disk and every
	// graph stays live until closed, so large lists are expensive.
	public static class AmplifyShaderBatchAPI
	{
		// Resaves every enabled shader in a single set. Returns the number of shaders submitted.
		public static int ResaveBatchSet( AmplifyShaderBatchSet set )
		{
			return Resave( CollectSet( set ) );
		}

		// Resaves a single set loaded from its asset path ( e.g. "Assets/Sets/Foliage.asset" ).
		public static int ResaveBatchSetAtPath( string setAssetPath )
		{
			return Resave( CollectSet( LoadSet( setAssetPath ) ) );
		}

		// Resaves every enabled shader across the given sets ( deduplicated ).
		public static int ResaveBatchSets( IEnumerable<AmplifyShaderBatchSet> sets )
		{
			return Resave( CollectSets( sets ) );
		}

		// Resaves multiple sets loaded from their asset paths.
		public static int ResaveBatchSetsAtPaths( IEnumerable<string> setAssetPaths )
		{
			return Resave( CollectSets( LoadSets( setAssetPaths ) ) );
		}

		// Resaves every enabled shader across all sets flagged "Active".
		public static int ResaveActiveBatchSets()
		{
			return Resave( CollectActiveSets() );
		}

		// Resaves an explicit list of ASE shaders, deduplicated.
		public static int ResaveShaders( IEnumerable<Shader> shaders )
		{
			return Resave( CollectShaders( shaders ) );
		}

		// Resaves an explicit list of ASE shaders loaded from their asset paths.
		public static int ResaveShadersAtPaths( IEnumerable<string> shaderPaths )
		{
			return Resave( CollectShadersAtPaths( shaderPaths ) );
		}

		// Opens every enabled shader in a single set. Returns the number of shaders opened.
		public static int OpenBatchSet( AmplifyShaderBatchSet set )
		{
			return Open( CollectSet( set ) );
		}

		// Opens a single set loaded from its asset path ( e.g. "Assets/Sets/Foliage.asset" ).
		public static int OpenBatchSetAtPath( string setAssetPath )
		{
			return Open( CollectSet( LoadSet( setAssetPath ) ) );
		}

		// Opens every enabled shader across the given sets ( deduplicated ).
		public static int OpenBatchSets( IEnumerable<AmplifyShaderBatchSet> sets )
		{
			return Open( CollectSets( sets ) );
		}

		// Opens multiple sets loaded from their asset paths.
		public static int OpenBatchSetsAtPaths( IEnumerable<string> setAssetPaths )
		{
			return Open( CollectSets( LoadSets( setAssetPaths ) ) );
		}

		// Opens every enabled shader across all sets flagged "Active".
		public static int OpenActiveBatchSets()
		{
			return Open( CollectActiveSets() );
		}

		// Opens an explicit list of ASE shaders, deduplicated.
		public static int OpenShaders( IEnumerable<Shader> shaders )
		{
			return Open( CollectShaders( shaders ) );
		}

		// Opens an explicit list of ASE shaders loaded from their asset paths.
		public static int OpenShadersAtPaths( IEnumerable<string> shaderPaths )
		{
			return Open( CollectShadersAtPaths( shaderPaths ) );
		}

		// loads a Batch Set from its asset path, logging when there is none there
		private static AmplifyShaderBatchSet LoadSet( string setAssetPath )
		{
			AmplifyShaderBatchSet set = AssetDatabase.LoadAssetAtPath<AmplifyShaderBatchSet>( setAssetPath );
			if ( set == null )
			{
				Debug.LogError( "[AmplifyShaderEditor] No Batch Set found at \"" + setAssetPath + "\"." );
			}
			return set;
		}

		private static List<AmplifyShaderBatchSet> LoadSets( IEnumerable<string> setAssetPaths )
		{
			List<AmplifyShaderBatchSet> sets = new List<AmplifyShaderBatchSet>();
			if ( setAssetPaths != null )
			{
				foreach ( string path in setAssetPaths )
				{
					AmplifyShaderBatchSet set = LoadSet( path );
					if ( set != null )
					{
						sets.Add( set );
					}
				}
			}
			return sets;
		}

		private static List<string> CollectSet( AmplifyShaderBatchSet set )
		{
			List<string> paths = new List<string>();
			GatherSet( set, paths );
			return paths;
		}

		private static List<string> CollectSets( IEnumerable<AmplifyShaderBatchSet> sets )
		{
			List<string> paths = new List<string>();
			if ( sets != null )
			{
				foreach ( AmplifyShaderBatchSet set in sets )
				{
					GatherSet( set, paths );
				}
			}
			return paths;
		}

		private static List<string> CollectActiveSets()
		{
			List<string> paths = new List<string>();
			string[] guids = AssetDatabase.FindAssets( "t:AmplifyShaderBatchSet" );
			for ( int i = 0; i < guids.Length; i++ )
			{
				string path = AssetDatabase.GUIDToAssetPath( guids[ i ] );
				AmplifyShaderBatchSet set = AssetDatabase.LoadAssetAtPath<AmplifyShaderBatchSet>( path );
				if ( set != null && set.Active )
				{
					GatherSet( set, paths );
				}
			}
			return paths;
		}

		private static List<string> CollectShaders( IEnumerable<Shader> shaders )
		{
			List<string> paths = new List<string>();
			if ( shaders != null )
			{
				foreach ( Shader shader in shaders )
				{
					TryAdd( shader, paths );
				}
			}
			return paths;
		}

		private static List<string> CollectShadersAtPaths( IEnumerable<string> shaderPaths )
		{
			List<string> paths = new List<string>();
			if ( shaderPaths != null )
			{
				foreach ( string path in shaderPaths )
				{
					Shader shader = AssetDatabase.LoadAssetAtPath<Shader>( path );
					if ( shader == null )
					{
						Debug.LogError( "[AmplifyShaderEditor] No shader found at \"" + path + "\"." );
						continue;
					}
					TryAdd( shader, paths );
				}
			}
			return paths;
		}

		// appends a set's enabled, ASE, not-yet-listed shaders to the gathered list
		private static void GatherSet( AmplifyShaderBatchSet set, List<string> paths )
		{
			if ( set == null )
			{
				return;
			}
			for ( int i = 0; i < set.Entries.Count; i++ )
			{
				AmplifyShaderBatchSet.Entry entry = set.Entries[ i ];
				if ( entry != null && entry.Enabled )
				{
					TryAdd( entry.Shader, paths );
				}
			}
		}

		// adds a shader's asset path if it is a valid ASE shader and not already listed
		private static void TryAdd( Shader shader, List<string> paths )
		{
			if ( shader == null || !IOUtils.IsASEShader( shader ) )
			{
				return;
			}
			string path = AssetDatabase.GetAssetPath( shader );
			if ( !string.IsNullOrEmpty( path ) && !paths.Contains( path ) )
			{
				paths.Add( path );
			}
		}

		private static int Resave( List<string> paths )
		{
			if ( paths.Count == 0 )
			{
				return 0;
			}
			if ( AmplifyShaderEditorWindow.IsBatchProcessing )
			{
				Debug.LogWarning( "[AmplifyShaderEditor] A batch resave is already running; ignoring the new request." );
				return 0;
			}
			AmplifyShaderEditorWindow.InitBatch( paths.Count );
			AmplifyShaderEditorWindow.LoadAndSaveList( paths.ToArray() );
			return paths.Count;
		}

		private static int Open( List<string> paths )
		{
			if ( paths.Count == 0 )
			{
				return 0;
			}
			if ( AmplifyShaderEditorWindow.IsBatchProcessing )
			{
				Debug.LogWarning( "[AmplifyShaderEditor] A batch resave is running; ignoring the open request." );
				return 0;
			}
			AmplifyShaderEditorWindow.LoadList( paths.ToArray() );
			return paths.Count;
		}
	}
}

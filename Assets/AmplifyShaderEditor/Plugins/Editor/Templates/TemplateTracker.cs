// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

//#define DEBUG_TRACKER

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[NoAutoStaticsCleanup]
	public static class TemplateTracker
	{
		[Serializable]
		struct AssetDescriptor
		{
			public string guid;
			public string path;
			public long timestamp;
			public bool isTemplate;

			public AssetDescriptor( string guid, string path, long timestamp, bool isTemplate )
			{
				this.guid = guid;
				this.path = path;
				this.timestamp = timestamp;
				this.isTemplate = isTemplate;
			}
		};

		[Serializable]
		class Cache
		{
			public int version = CacheVersion;
			public List<AssetDescriptor> assets = new List<AssetDescriptor>();
		};

		static readonly Dictionary<string,AssetDescriptor> s_knownAssets = new Dictionary<string,AssetDescriptor>();
		static readonly Dictionary<string,AssetDescriptor> s_knownTemplates = new Dictionary<string,AssetDescriptor>();

		static readonly Dictionary<string,HashSet<TemplateManifest>> s_knownManifests = new Dictionary<string, HashSet<TemplateManifest>>();
		public static Dictionary<string,HashSet<TemplateManifest>> KnownManifests => s_knownManifests;

		static bool s_updateQueued;
		static bool s_cacheLoaded;

		const string CacheFilePath = "Library/AmplifyShaderEditor.Templates.json";
		const int CacheVersion = 1;

	#if DEBUG_TRACKER
		const bool DebugEnabled = true;
	#else
		const bool DebugEnabled = false;
	#endif

		public static void Initialize()
		{
			EditorApplication.projectChanged -= ProjectChangedCall;
			EditorApplication.projectChanged += ProjectChangedCall;

			CompilationPipeline.compilationFinished -= CompilationFinishedCall;
			CompilationPipeline.compilationFinished += CompilationFinishedCall;

			using ( new ScopedTimer( "TemplateTracker: Loading Cache", DebugEnabled ) )
			{
				LoadCache();
			}

			Update();
		}

		public static void ProjectChangedCall() => QueueUpdate();
		public static void CompilationFinishedCall( object value ) => QueueUpdate();

		public static void QueueUpdate()
		{
			if ( !s_updateQueued )
			{
				s_updateQueued = true;
				EditorApplication.delayCall += RunQueuedUpdate;
			}
		}

		static void RunQueuedUpdate()
		{
			s_updateQueued = false;

			// Unity still compiling? Re-queue
			if ( EditorApplication.isCompiling )
			{
				QueueUpdate();
				return;
			}

			Update();
		}

		static void Update()
		{
			using ( new ScopedTimer( "TemplateTracker: Scanning Template Manifests", DebugEnabled ) )
			{
				ScanTemplateManifests();
			}

			using ( new ScopedTimer( "TemplateTracker: Requesting Package Info", DebugEnabled ) )
			{
				ASEPackageManagerHelper.RequestInfo();
				ASEPackageManagerHelper.Update();
			}

			var added = new List<AssetDescriptor>();
			var changed = new List<AssetDescriptor>();
			var removed = new List<AssetDescriptor>();

			using ( new ScopedTimer( "TemplateTracker: Scanning", DebugEnabled ) )
			{
				Scan( out added, out changed, out removed );
			}

			bool templatesChanged = ( added.Count > 0 || changed.Count > 0 || removed.Count > 0 );

			// Save cache file, if necessary
			if ( templatesChanged || !File.Exists( CacheFilePath ) )
			{
				using ( new ScopedTimer( "TemplateTracker: Saving Cache", DebugEnabled ) )
				{
					SaveCache();
				}
			}

			// Only keep going if changes are detected
			using ( new ScopedTimer( "TemplateTracker: Processing", DebugEnabled ) )
			{
				Process( added, changed, removed );
			}

		#if DEBUG_TRACKER
			Debug.LogFormat( "[AmplifyShaderEditor] TemplateTracker: Current( {0} ), Added( {1} ), Changed( {2} ), Removed( {3} ), Manifests( {4} )",
				s_knownTemplates.Count, added.Count, changed.Count, removed.Count, s_knownManifests.Count );
		#endif
		}

		static void LoadCache()
		{
			if ( !s_cacheLoaded )
			{
				s_cacheLoaded = true;
				s_knownAssets.Clear();
				s_knownTemplates.Clear();

				if ( File.Exists( CacheFilePath ) )
				{
					try
					{
						string json = File.ReadAllText( CacheFilePath );
						var cache = JsonUtility.FromJson<Cache>( json );
						if ( cache != null && cache.version == CacheVersion && cache.assets != null )
						{
							foreach ( AssetDescriptor asset in cache.assets )
							{
								if ( !string.IsNullOrEmpty( asset.guid ) && !string.IsNullOrEmpty( asset.path ) )
								{
									s_knownAssets[ asset.guid ] = asset;
									if ( asset.isTemplate )
									{
										s_knownTemplates[ asset.guid ] = asset;
									}
								}
							}
						}
					}
					catch ( Exception e )
					{
						Debug.LogWarning( "[AmplifyShaderEditor] TemplateTracker: Failed to load tracker cache. Rebuilding." + e.Message );
						s_knownAssets.Clear();
						s_knownTemplates.Clear();
					}
				}
			}
		}

		static void SaveCache()
		{
			try
			{
				var cache = new Cache();
				cache.assets.AddRange( s_knownAssets.Values );

				string json = JsonUtility.ToJson( cache, DebugEnabled );
				File.WriteAllText( CacheFilePath, json );
			}
			catch ( Exception e )
			{
				Debug.LogWarning( "[AmplifyShaderEditor] TemplateTracker: Failed to save template cache." + e.Message );
			}
		}

		public static void ScanTemplateManifests()
		{
			string[] manifestGUIDs = AssetDatabase.FindAssets( "t:AmplifyShaderEditor.TemplateManifest" );
			foreach ( var guid in manifestGUIDs )
			{
				var manifest = AssetDatabase.LoadAssetAtPath<TemplateManifest>( AssetDatabase.GUIDToAssetPath( guid ) );
				if ( !string.IsNullOrEmpty( manifest.PackageName ) )
				{
					if ( !s_knownManifests.TryGetValue( manifest.PackageName, out HashSet<TemplateManifest> manifestSet ) )
					{
						manifestSet = new HashSet<TemplateManifest>();
						s_knownManifests.TryAdd( manifest.PackageName, manifestSet );
					}

					manifestSet.Add( manifest );
				}
			}

			foreach ( var manifestSet in s_knownManifests.Values )
			{
				manifestSet.RemoveWhere( item => item == null || string.IsNullOrEmpty( item.PackageName ) );
			}
		}

		public static List<(string, string)> FindTemplateCandidateAssets()
		{
			string[] allPaths;
			using ( new ScopedTimer( "TemplateTracker: Scanning.GetAllAssetPaths", DebugEnabled ) )
			{
				allPaths = AssetDatabase.GetAllAssetPaths();
			}

			var paths = new ConcurrentBag<string>();
			var candidates = new List<(string, string)>();

			using ( new ScopedTimer( "TemplateTracker: Scanning.PathFiltering", DebugEnabled ) )
			{
				Parallel.For( 0, allPaths.Length, i =>
				{
					string path = allPaths[ i ];
					for ( int j = 0; j < Preferences.Project.TemplateExtensions.Length; j++ )
					{
						if ( path.EndsWith( Preferences.Project.TemplateExtensions[ j ], StringComparison.OrdinalIgnoreCase ) )
						{
							paths.Add( path );
							break;
						}
					}
				} );
			}

			using ( new ScopedTimer( "TemplateTracker: Scanning.PathToGUID", DebugEnabled ) )
			{
				foreach ( string path in paths )
				{
					candidates.Add( ( AssetDatabase.AssetPathToGUID( path ), path ) );
				}
			}

			return candidates;
		}

		static void Scan( out List<AssetDescriptor> added, out List<AssetDescriptor> changed, out List<AssetDescriptor> removed )
		{
			var candidates = FindTemplateCandidateAssets();

			var assets = new ConcurrentDictionary<string, AssetDescriptor>( Environment.ProcessorCount, candidates.Count );
			var templates = new ConcurrentDictionary<string, AssetDescriptor>( Environment.ProcessorCount, candidates.Count );

			using ( new ScopedTimer( "TemplateTracker: Scanning.CheckTemplates", DebugEnabled ) )
			{
				Parallel.For( 0, candidates.Count, i =>
				{
					string guid = candidates[ i ].Item1;
					string path = candidates[ i ].Item2;
					long timestamp = File.GetLastWriteTime( path ).Ticks;

					bool isTemplate;
					if ( s_knownAssets.TryGetValue( guid, out AssetDescriptor asset ) && asset.timestamp == timestamp )
					{
						// Known asset with same timestamp; do nothing
					}
					else
					{
						// Unknown or modified asset; do full check
						isTemplate = TemplateHelperFunctions.CheckIfTemplate( path );
						asset = new AssetDescriptor( guid, path, timestamp, isTemplate );
					}

					assets.TryAdd( guid, asset );
					if ( asset.isTemplate )
					{
						templates.TryAdd( guid, asset );
					}
				} );
			}

			added = new List<AssetDescriptor>( assets.Count );
			changed = new List<AssetDescriptor>( assets.Count );
			removed = new List<AssetDescriptor>( assets.Count );

			// Find added or changed templates
			foreach ( AssetDescriptor template in templates.Values )
			{
				if ( s_knownTemplates.TryGetValue( template.guid, out AssetDescriptor duplicate ) )
				{
					if ( template.timestamp != duplicate.timestamp )
					{
						// Template was modified
						changed.Add( template );
					}
					else
					{
						// No changes to template
					}
				}
				else
				{
					// New template was added
					added.Add( template );
				}
			}

			// Find removed templates
			foreach ( AssetDescriptor template in s_knownTemplates.Values )
			{
				if ( !templates.ContainsKey( template.guid ) )
				{
					// Template was removed
					removed.Add( template );
				}
			}

			// Fill known assets
			s_knownAssets.Clear();
			foreach ( AssetDescriptor asset in assets.Values )
			{
				s_knownAssets[ asset.guid ] = asset;
			}

			// Fill known templates
			s_knownTemplates.Clear();
			foreach ( AssetDescriptor template in templates.Values )
			{
				s_knownTemplates[ template.guid ] = template;
			}
		}

		static void Process( List<AssetDescriptor> added, List<AssetDescriptor> changed, List<AssetDescriptor> removed )
		{
			var imported = new List<AssetDescriptor>();
			imported.AddRange( added );
			imported.AddRange( changed );

			if ( imported.Count > 0 )
			{
				EditorUtility.DisplayProgressBar( "Amplify Shader Editor", "Updating Templates....", 0.0f );
			}

			bool refreshMenuItems = false;
			int progressIteration = 0;
			int progressIterationCount = imported.Count + removed.Count;

			// Process imported templates (added or changed)
			foreach ( var template in imported )
			{
				TemplateDataParent templateData = TemplatesManager.Instance.GetTemplate( template.guid );
				if ( templateData != null )
				{
					refreshMenuItems = templateData.Reload() || refreshMenuItems;
					int windowCount = IOUtils.AllOpenedWindows.Count;
					AmplifyShaderEditorWindow currWindow = UIUtils.CurrentWindow;
					for ( int windowIdx = 0; windowIdx < windowCount; windowIdx++ )
					{
						if ( IOUtils.AllOpenedWindows[ windowIdx ].OutsideGraph.CurrentCanvasMode == NodeAvailability.TemplateShader )
						{
							if ( IOUtils.AllOpenedWindows[ windowIdx ].OutsideGraph.MultiPassMasterNodes.NodesList[ 0 ].CurrentTemplate == templateData )
							{
								UIUtils.CurrentWindow = IOUtils.AllOpenedWindows[ windowIdx ];
								IOUtils.AllOpenedWindows[ windowIdx ].OutsideGraph.ForceMultiPassMasterNodesRefresh();
							}
						}
					}
					UIUtils.CurrentWindow = currWindow;
				}
				else
				{
					refreshMenuItems = true;
					TemplatesManager.Instance.QueueRegisterTemplate( template.guid, template.path );
				}

				EditorUtility.DisplayProgressBar( "Amplify Shader Editor", "Updating Templates....", ++progressIteration / ( float )progressIterationCount );
			}

			// Process deleted templates
			foreach ( var template in removed )
			{
				TemplateDataParent templateData = TemplatesManager.Instance.GetTemplate( template.guid );
				if ( templateData != null )
				{
					// Close any window using that template
					int windowCount = IOUtils.AllOpenedWindows.Count;
					for ( int windowIdx = 0; windowIdx < windowCount; windowIdx++ )
					{
						TemplateMasterNode masterNode = IOUtils.AllOpenedWindows[ windowIdx ].CurrentGraph.CurrentMasterNode as TemplateMasterNode;
						if ( masterNode != null && masterNode.CurrentTemplate.GUID.Equals( templateData.GUID ) )
						{
							IOUtils.AllOpenedWindows[ windowIdx ].Close();
						}
					}

					TemplatesManager.Instance.RemoveTemplate( templateData );
					refreshMenuItems = true;
				}

				EditorUtility.DisplayProgressBar( "Amplify Shader Editor", "Updating Templates....", ++progressIteration / ( float )progressIterationCount );
			}

			// Add templates that are known but missing from manager instance
			foreach ( var template in s_knownTemplates.Values )
			{
				TemplateDataParent templateData = TemplatesManager.Instance.GetTemplate( template.guid );
				if ( templateData == null )
				{
					TemplatesManager.Instance.QueueRegisterTemplate( template.guid, template.path );
				}
			}
			TemplatesManager.Instance.FlushRegisterTemplateQueue();

			EditorUtility.ClearProgressBar();

			if ( !refreshMenuItems )
			{
				// diogo: double check current menu templates, to see if they match the current known templates
				var templateMenuItems = TemplatesManager.Instance.ExtractTemplateMenuItems();
				var knownTemplates = new HashSet<string>( s_knownTemplates.Keys );

				refreshMenuItems = !templateMenuItems.SetEquals( knownTemplates );
			}

			if ( refreshMenuItems )
			{
				refreshMenuItems = false;
				TemplatesManager.Instance.CreateTemplateMenuItems();

				AmplifyShaderEditorWindow currWindow = UIUtils.CurrentWindow;

				int windowCount = IOUtils.AllOpenedWindows.Count;
				for ( int windowIdx = 0; windowIdx < windowCount; windowIdx++ )
				{
					UIUtils.CurrentWindow = IOUtils.AllOpenedWindows[ windowIdx ];
					IOUtils.AllOpenedWindows[ windowIdx ].CurrentGraph.ForceCategoryRefresh();
				}
				UIUtils.CurrentWindow = currWindow;
			}

			// reimport menu items at the end of everything, hopefully preventing import loops
			TemplatesManager.Instance.ReimportMenuItems();
		}
	}
}

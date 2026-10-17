using System;
using AmplifyShaderEditor;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using EditorGUILayout = UnityEditor.EditorGUILayout;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[NoAutoStaticsCleanup]
	public class ShaderUtility : EditorWindow
	{
		[System.Serializable]
		class ShaderAsset
		{
			public string AssetPath;
			public Shader Shader;
			public bool Selected;
			public int TextureCount;
			public int FetchCount;
			public bool ReadOnly;
			// @diogo: Names of the referenced shader functions that no longer resolve; null when all do.
			public List<string> MissingFunctions;
			public bool HasMissingFunctions => MissingFunctions != null && MissingFunctions.Count > 0;

			public ShaderAsset( string path, Shader shader, bool selected )
			{
				AssetPath = path;
				Shader = shader;
				Selected = selected;
			}
		};

		private const string PREF_RESAVE_SKIP_CONFIRM = "AmplifyShaderEditor.ShaderUtility.ResaveSkipConfirm";
		private const string PREF_SEARCH_PATHS = "AmplifyShaderEditor.ShaderUtility.SearchPaths";

		[SerializeField] private List<ShaderAsset> m_shaders = new List<ShaderAsset>();

		// Folders the fetch is scoped to ( mirrors Batch Update Shaders' "Extra Paths" list ).
		[SerializeField] private List<string> m_searchPaths = new List<string>() { "Assets" };
		private ReorderableList m_searchPathsList;

		[SerializeField] private bool m_hideUnselected = false;
		private Vector2 m_shaderScrollPos = Vector2.zero;
		private readonly HashSet<ShaderAsset> m_highlighted = new HashSet<ShaderAsset>();
		private ShaderAsset m_selectionAnchor = null;

		private struct NodeEntry
		{
			public string DisplayName;
			public string TypeName;
		}

		[SerializeField] private string m_nodeSearch = string.Empty;
		[SerializeField] private string m_selectedTypeName = string.Empty;
		private List<NodeEntry> m_nodeTypeCache = null;
		private List<NodeEntry> m_filteredItems = new List<NodeEntry>();
		private int m_highlightedIndex = 0;
		private Vector2 m_autocompleteScrollPos = Vector2.zero;
		private bool m_showAutocomplete = false;
		private GUIStyle m_statsLabelStyle = null;
		private GUIStyle m_dropHintStyle = null;
		private GUIStyle m_readOnlyLabelStyle = null;
		private Dictionary<string, int> m_functionFetchCounts = null;
		private Dictionary<string, Dictionary<string, int>> m_functionSwitchCounts = null;
		private GUIStyle m_missingLabelStyle = null;

		// @diogo: Project-wide shader function identity, the oracle for the missing-function check. Static so
		// the context-menu resave, which has no window instance, resolves exactly like the window does.
		private static HashSet<string> m_functionGuids = null;
		private static HashSet<string> m_functionNames = null;

		[SerializeField] private int m_platformIndex = 0;
		private static string[] m_platformLabels;
		private static RenderPlatformInfo[] m_platformInfoFiltered;

		private enum SortMethod
		{
			PATH,
			FILENAME,
			NAME
		}

		[SerializeField] private SortMethod m_sortMethod = SortMethod.PATH;

		// A batch is running while ASE owns the resave queue; block re-entrant operations.
		private bool IsBatchRunning { get { return AmplifyShaderEditorWindow.IsBatchProcessing; } }

		[MenuItem( "Window/Amplify Shader Editor/Shader Utility", false, priority: 1100 )]
		private static void ShowWindow()
		{
			ShowUtilityWindow();
		}

		private static ShaderUtility ShowUtilityWindow()
		{
			var window = GetWindow<ShaderUtility>();
			window.titleContent = new GUIContent( "Shader Utility" );
			window.minSize = new Vector2( 302, 350 );

			// Default opening size: 906 wide ( 3x the Batch Update Shaders baseline width of 302 ) by 788 tall
			// ( 50% taller than the previous 525 default ), centered on the main editor window.
			float width = 650f;
			float height = 800f;
			Rect main = EditorGUIUtility.GetMainWindowPosition();
			window.position = new Rect( main.x + ( main.width - width ) * 0.5f, main.y + ( main.height - height ) * 0.5f, width, height );

			window.Show();
			return window;
		}

		// @diogo: Opens ( or focuses ) the Shader Utility scoped to a selection. Folders replace the search-path
		// list and are fetched immediately; individually picked shaders are then added on top. With no
		// folders the search-path list is left alone and only those shaders are listed.
		public static void OpenScopedTo( List<string> folderPaths, List<string> shaderPaths )
		{
			ShaderUtility window = ShowUtilityWindow();
			if ( folderPaths.Count > 0 )
			{
				window.m_searchPaths.Clear();
				window.m_searchPaths.AddRange( folderPaths );
				window.SaveSearchPaths();
				window.FetchShaders();
			}
			else
			{
				window.ClearShaders();
			}
			window.AddShaderPaths( shaderPaths );
		}

		// @diogo: Opens every directly-selected ASE shader in the graph editor, each in its own tab. Folders are
		// intentionally ignored ( unlike the sibling commands ) so folder-wide work stays in the Shader Utility.
		[MenuItem( "Assets/Amplify Shader Tools/Open in Editor", true )]
		private static bool ValidateOpenInEditorForSelection()
		{
			return GetSelectedShaderPaths().Count > 0;
		}

		[MenuItem( "Assets/Amplify Shader Tools/Open in Editor", false, 1099 )]
		private static void OpenInEditorForSelection()
		{
			List<string> shaders = FilterASEShaders( GetSelectedShaderPaths() );
			if ( shaders.Count == 0 )
			{
				EditorUtility.DisplayDialog( "Open in Editor",
					"None of the selected shaders were made with Amplify Shader Editor.", "OK" );
				return;
			}

			if ( shaders.Count > 10 )
			{
				bool proceed = EditorUtility.DisplayDialog( "Open in Editor",
					$"This will open {shaders.Count} shaders in the editor, each in its own tab. Do you wish to proceed?",
					$"Open {shaders.Count}",
					"Cancel" );
				if ( !proceed )
				{
					return;
				}
			}

			foreach ( string path in shaders )
			{
				Shader shader = AssetDatabase.LoadAssetAtPath<Shader>( path );
				if ( shader != null )
				{
					AmplifyShaderEditorWindow.ConvertShaderToASE( shader );
				}
			}
		}

		[MenuItem( "Assets/Amplify Shader Tools/Open in Shader Utility", true )]
		private static bool ValidateOpenShaderUtilityForSelection()
		{
			return GetSelectedFolderPaths().Count > 0 || GetSelectedShaderPaths().Count > 0;
		}

		[MenuItem( "Assets/Amplify Shader Tools/Open in Shader Utility", false, 1100 )]
		private static void OpenShaderUtilityForSelection()
		{
			List<string> folders = GetSelectedFolderPaths();
			List<string> shaders = FilterASEShaders( GetSelectedShaderPaths() );
			if ( folders.Count == 0 && shaders.Count == 0 )
			{
				EditorUtility.DisplayDialog( "Open in Shader Utility",
					"None of the selected shaders were made with Amplify Shader Editor.", "OK" );
				return;
			}
			OpenScopedTo( folders, shaders );
		}

		[MenuItem( "Assets/Amplify Shader Tools/Resave Shaders", true )]
		private static bool ValidateResaveShadersInSelection()
		{
			return GetSelectedFolderPaths().Count > 0 || GetSelectedShaderPaths().Count > 0;
		}

		[MenuItem( "Assets/Amplify Shader Tools/Resave Shaders", false, 1101 )]
		private static void ResaveShadersInSelection()
		{
			ResaveAllInSelection( GetSelectedFolderPaths(), GetSelectedShaderPaths() );
		}

		// @diogo: Project-relative paths of every currently selected asset that is a folder ( de-duplicated,
		// empty when none are folders ).
		private static List<string> GetSelectedFolderPaths()
		{
			List<string> folders = new List<string>();
			foreach ( UnityEngine.Object obj in Selection.objects )
			{
				if ( obj == null )
				{
					continue;
				}
				string path = AssetDatabase.GetAssetPath( obj );
				if ( !string.IsNullOrEmpty( path ) && AssetDatabase.IsValidFolder( path ) && !folders.Contains( path ) )
				{
					folders.Add( path );
				}
			}
			return folders;
		}

		// @diogo: Project-relative paths of every currently selected shader asset ( de-duplicated ). Kept cheap
		// on purpose: menu validation runs on every context-menu open, so the ASE check that has to read
		// each file from disk is deferred to the menu action itself.
		private static List<string> GetSelectedShaderPaths()
		{
			List<string> shaders = new List<string>();
			foreach ( UnityEngine.Object obj in Selection.objects )
			{
				if ( !( obj is Shader ) )
				{
					continue;
				}
				string path = AssetDatabase.GetAssetPath( obj );
				if ( !string.IsNullOrEmpty( path ) && !shaders.Contains( path ) )
				{
					shaders.Add( path );
				}
			}
			return shaders;
		}

		private static List<string> FilterASEShaders( List<string> shaderPaths )
		{
			List<string> filtered = new List<string>();
			foreach ( string path in shaderPaths )
			{
				Shader shader = AssetDatabase.LoadAssetAtPath<Shader>( path );
				if ( shader != null && IOUtils.IsASEShader( shader ) )
				{
					filtered.Add( path );
				}
			}
			return filtered;
		}

		// @diogo: Resaves every ASE shader picked directly plus every one found ( recursively ) under the selected
		// folders, without opening the window. Read-only shaders are skipped. Mirrors ResaveShaders'
		// partition / confirm / batch flow.
		private static void ResaveAllInSelection( List<string> folderPaths, List<string> shaderPaths )
		{
			if ( AmplifyShaderEditorWindow.IsBatchProcessing )
			{
				return;
			}

			List<string> writable = new List<string>();
			List<string> readOnly = new List<string>();
			var missing = new List<(string path, Shader shader, string funcs)>();
			HashSet<string> seen = new HashSet<string>();
			BuildFunctionIdentityIndex();
			// @diogo: FindAssets treats an empty folder array as "search the whole project", so only ever call it
			// when at least one folder is selected.
			if ( folderPaths.Count > 0 )
			{
				string[] shaderGuids = AssetDatabase.FindAssets( "t:Shader", folderPaths.ToArray() );
				foreach ( string guid in shaderGuids )
				{
					CollectResavableShader( AssetDatabase.GUIDToAssetPath( guid ), seen, writable, readOnly, missing );
				}
			}
			foreach ( string shaderPath in shaderPaths )
			{
				CollectResavableShader( shaderPath, seen, writable, readOnly, missing );
			}

			string scope = GetSelectionScopeLabel( folderPaths, shaderPaths );

			if ( writable.Count == 0 && readOnly.Count == 0 && missing.Count == 0 )
			{
				EditorUtility.DisplayDialog( "Resave All Shaders",
					$"No Amplify shaders were found in {scope}.", "OK" );
				return;
			}

			if ( writable.Count == 0 )
			{
				LogSkippedShaders( readOnly, missing );
				EditorUtility.DisplayDialog( "Resave All Shaders",
					$"None of the {readOnly.Count + missing.Count} Amplify shader(s) in {scope} can be resaved: {DescribeSkipped( readOnly, missing )}.",
					"OK" );
				return;
			}

			int skipped = readOnly.Count + missing.Count;
			string skippedNote = skipped > 0
				? $"{skipped} of {writable.Count + skipped} Amplify shader(s) will be skipped: {DescribeSkipped( readOnly, missing )}.\n\n"
				: string.Empty;
			bool proceed = EditorUtility.DisplayDialog( "Resave All Shaders",
				$"{skippedNote}This will resave {writable.Count} Amplify shader(s) in {scope}, which can take a while and tie up your machine during the process. Do you wish to proceed?",
				$"Resave {writable.Count}",
				"Cancel" );
			if ( !proceed )
			{
				return;
			}

			LogSkippedShaders( readOnly, missing );
			AmplifyShaderEditorWindow.InitBatch( writable.Count );
			AmplifyShaderEditorWindow.LoadAndSaveList( writable.ToArray() );
		}

		// @diogo: Sorts one candidate path into the writable/read-only buckets, ignoring non-ASE shaders and
		// paths already seen ( a shader can be both picked directly and covered by a selected folder ).
		private static void CollectResavableShader( string assetPath, HashSet<string> seen, List<string> writable, List<string> readOnly, List<(string path, Shader shader, string funcs)> missing )
		{
			if ( !seen.Add( assetPath ) )
			{
				return;
			}
			Shader shader = AssetDatabase.LoadAssetAtPath<Shader>( assetPath );
			if ( shader != null && IOUtils.IsASEShader( shader ) )
			{
				List<string> missingFunctions = ReadMissingFunctions( assetPath, out bool couldNotRead );
				if ( couldNotRead )
				{
					missing.Add( ( assetPath, shader, null ) );
				}
				else if ( missingFunctions != null )
				{
					missing.Add( ( assetPath, shader, string.Join( ", ", missingFunctions ) ) );
				}
				else
				{
					( IsReadOnlyShader( assetPath ) ? readOnly : writable ).Add( assetPath );
				}
			}
		}

		// @diogo: Human-readable description of what the operation covers, for the confirmation dialogs. Only a
		// single-item scope is named outright: a shader count would clash with the count of Amplify
		// shaders actually found, since the selection can hold shaders made outside ASE.
		private static string GetSelectionScopeLabel( List<string> folderPaths, List<string> shaderPaths )
		{
			string label;
			if ( shaderPaths.Count == 0 )
			{
				label = folderPaths.Count == 1 ? $"'{folderPaths[ 0 ]}'" : $"the {folderPaths.Count} selected folders";
			}
			else if ( folderPaths.Count == 0 && shaderPaths.Count == 1 )
			{
				label = $"'{shaderPaths[ 0 ]}'";
			}
			else
			{
				label = "the current selection";
			}
			return label;
		}

		private void OnEnable()
		{
			LoadSearchPaths();
		}

		private void OnDisable()
		{
			SaveSearchPaths();
		}

		[System.Serializable]
		private class SearchPathsPref
		{
			public List<string> Paths = new List<string>();
		}

		// @diogo: EditorPrefs is machine-global; key by the project folder name so each project keeps its own
		// list and a moved project keeps it too. productGUID/productName can't be the discriminator:
		// copy/pasted projects ( e.g. ASE_SRP_* ) share them. Lowercased for case stability on Windows.
		private static string SearchPathsPrefKey
		{
			get
			{
				string projectName = Path.GetFileName( Path.GetDirectoryName( Application.dataPath ) );
				return PREF_SEARCH_PATHS + "." + projectName.ToLowerInvariant();
			}
		}

		// @diogo: Persist the search-path list in EditorPrefs so it survives window close and Unity restarts
		// ( [SerializeField] only survives domain reloads ).
		private void LoadSearchPaths()
		{
			string json = EditorPrefs.GetString( SearchPathsPrefKey, string.Empty );
			if ( string.IsNullOrEmpty( json ) )
			{
				return;
			}
			try
			{
				SearchPathsPref pref = JsonUtility.FromJson<SearchPathsPref>( json );
				if ( pref != null && pref.Paths != null )
				{
					m_searchPaths.Clear();
					m_searchPaths.AddRange( pref.Paths );
				}
			}
			catch ( Exception ) { }
		}

		private void SaveSearchPaths()
		{
			EditorPrefs.SetString( SearchPathsPrefKey, JsonUtility.ToJson( new SearchPathsPref { Paths = m_searchPaths } ) );
		}

		private void OnGUI()
		{
			bool batchRunning = IsBatchRunning;

			EditorGUI.BeginDisabledGroup( batchRunning );
			{
				EnsureSearchPathsList();
				EditorGUI.BeginChangeCheck();
				m_searchPathsList.DoLayoutList();
				Rect searchPathsRect = GUILayoutUtility.GetLastRect();
				if ( EditorGUI.EndChangeCheck() )
				{
					SaveSearchPaths();
				}
				if ( !batchRunning )
				{
					HandleSearchPathDrop( searchPathsRect );
				}

				EditorGUILayout.BeginHorizontal();
				Color prevFetchBgColor = GUI.backgroundColor;
				if ( m_shaders.Count == 0 )
				{
					GUI.backgroundColor = new Color( 0.5f, 1f, 0.5f );
				}
				if ( GUILayout.Button( "Fetch Shaders" ) )
				{
					FetchShaders();
				}
				GUI.backgroundColor = prevFetchBgColor;
				if ( GUILayout.Button( "Clear" ) )
				{
					ClearShaders();
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUI.EndDisabledGroup();

			int shaderCount = m_shaders.Count;
			int selectedCount = GetSelectedCount();
			EditorGUILayout.LabelField( "Total Shaders", $"{shaderCount}" );
			EditorGUILayout.LabelField( "Selected Shaders", $"{selectedCount}" );

			if ( batchRunning )
			{
				int total = AmplifyShaderEditorWindow.BatchTotal;
				int done = AmplifyShaderEditorWindow.BatchDone;
				if ( total > 0 )
				{
					Rect progressRect = EditorGUILayout.GetControlRect( false, 20f );
					EditorGUI.ProgressBar( progressRect, (float)done / total, $"{done} / {total}" );
					if ( GUILayout.Button( "Cancel" ) )
					{
						AmplifyShaderEditorWindow.CancelBatch();
					}
				}
				else
				{
					EditorGUILayout.HelpBox( "Batch operation in progress...", MessageType.Info );
				}
				Repaint();
			}

			EditorGUI.BeginDisabledGroup( batchRunning || selectedCount == 0 );
			{
				Color prevBgColor = GUI.backgroundColor;
				if ( !batchRunning && selectedCount > 0 )
					GUI.backgroundColor = new Color( 0.5f, 1f, 0.5f );

				if ( GUILayout.Button( "Resave Selected Shaders" ) )
				{
					ResaveShaders();
				}

				GUI.backgroundColor = prevBgColor;

				// Batch enable/disable of a render platform across the selected shaders.
				EditorGUILayout.Space();
				EnsurePlatformLabels();
				EditorGUILayout.LabelField( "Batch Platform", EditorStyles.boldLabel );
				m_platformIndex = EditorGUILayout.Popup( "Platform", m_platformIndex, m_platformLabels );
				EditorGUILayout.BeginHorizontal();
				if ( GUILayout.Button( "Enable on Selected" ) )
				{
					ModifyPlatformOnSelected( true );
				}
				if ( GUILayout.Button( "Disable on Selected" ) )
				{
					ModifyPlatformOnSelected( false );
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUI.EndDisabledGroup();

			// Select every shader whose graph contains a given node type.
			EditorGUILayout.Space();
			EditorGUILayout.LabelField( "Find Shaders Using Node", EditorStyles.boldLabel );

			if ( m_showAutocomplete && m_filteredItems.Count > 0 && GUI.GetNameOfFocusedControl() == "NodeSearchField" )
			{
				Event e = Event.current;
				if ( e.type == EventType.KeyDown )
				{
					if ( e.keyCode == KeyCode.DownArrow )
					{
						m_highlightedIndex = Mathf.Min( m_highlightedIndex + 1, m_filteredItems.Count - 1 );
						ScrollAutocompleteToHighlight();
						e.Use();
					}
					else if ( e.keyCode == KeyCode.UpArrow )
					{
						m_highlightedIndex = Mathf.Max( m_highlightedIndex - 1, 0 );
						ScrollAutocompleteToHighlight();
						e.Use();
					}
					else if ( e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter )
					{
						CommitAutocomplete( m_highlightedIndex );
						e.Use();
					}
					else if ( e.keyCode == KeyCode.Escape )
					{
						m_showAutocomplete = false;
						e.Use();
					}
				}
			}

			EditorGUILayout.BeginHorizontal();
			GUI.SetNextControlName( "NodeSearchField" );
			string newSearch = EditorGUILayout.TextField( "Node Type", m_nodeSearch );
			if ( newSearch != m_nodeSearch )
			{
				m_nodeSearch = newSearch;
				UpdateFilteredTypes();
			}
			EditorGUI.BeginDisabledGroup( string.IsNullOrEmpty( m_nodeSearch ) || m_shaders.Count == 0 );
			if ( GUILayout.Button( "Select", GUILayout.Width( 60 ) ) )
			{
				m_showAutocomplete = false;
				SelectShadersUsingNode();
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if ( m_showAutocomplete && m_filteredItems.Count > 0 )
			{
				float itemHeight = EditorGUIUtility.singleLineHeight + 2;
				float listHeight = Mathf.Min( m_filteredItems.Count * itemHeight, itemHeight * 8 );
				Color highlightColor = EditorGUIUtility.isProSkin
					? new Color( 0.3f, 0.5f, 0.85f, 0.4f )
					: new Color( 0.2f, 0.4f, 0.9f, 0.25f );
				int clickedIndex = -1;
				EditorGUILayout.BeginVertical( EditorStyles.helpBox );
				m_autocompleteScrollPos = GUILayout.BeginScrollView( m_autocompleteScrollPos,
					GUILayout.Height( listHeight ) );
				for ( int i = 0; i < m_filteredItems.Count; i++ )
				{
					Rect itemRect = EditorGUILayout.GetControlRect( false, itemHeight );
					if ( i == m_highlightedIndex )
					{
						EditorGUI.DrawRect( itemRect, highlightColor );
					}
					if ( GUI.Button( itemRect, m_filteredItems[ i ].DisplayName, EditorStyles.label ) )
					{
						clickedIndex = i;
					}
				}
				GUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
				if ( clickedIndex >= 0 )
				{
					CommitAutocomplete( clickedIndex );
					if ( m_shaders.Count > 0 && !string.IsNullOrEmpty( m_selectedTypeName ) )
					{
						SelectShadersUsingNode();
					}
				}
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField( "Shaders", EditorStyles.boldLabel );
			{
				SortMethod newSort = ( SortMethod )EditorGUILayout.EnumPopup( "Sort By", m_sortMethod );
				if ( newSort != m_sortMethod )
				{
					m_sortMethod = newSort;
					m_shaders.Sort( ( x, y ) => String.Compare( GetShaderSortString( x ), GetShaderSortString( y ) ) );
				}

				m_hideUnselected = EditorGUILayout.ToggleLeft( "Hide Unselected", m_hideUnselected );

				EditorGUILayout.BeginHorizontal();
				if ( GUILayout.Button( "Select All" ) )
				{
					foreach ( ShaderAsset asset in m_shaders )
					{
						asset.Selected = true;
					}
				}
				if ( GUILayout.Button( "Deselect All" ) )
				{
					foreach ( ShaderAsset asset in m_shaders )
					{
						asset.Selected = false;
					}
				}
				if ( GUILayout.Button( "Remove Selected" ) )
				{
					m_shaders.RemoveAll( a => a.Selected );
					m_highlighted.RemoveWhere( a => !m_shaders.Contains( a ) );
					if ( m_selectionAnchor != null && !m_shaders.Contains( m_selectionAnchor ) )
					{
						m_selectionAnchor = null;
					}
				}
				if ( GUILayout.Button( "Remove Unselected" ) )
				{
					m_shaders.RemoveAll( a => !a.Selected );
					m_highlighted.RemoveWhere( a => !m_shaders.Contains( a ) );
					if ( m_selectionAnchor != null && !m_shaders.Contains( m_selectionAnchor ) )
					{
						m_selectionAnchor = null;
					}
				}
				EditorGUILayout.EndHorizontal();

				if ( m_statsLabelStyle == null )
				{
					m_statsLabelStyle = new GUIStyle( EditorStyles.label );
					m_statsLabelStyle.alignment = TextAnchor.MiddleLeft;
				}
				// @diogo: re-sync every frame; a copy cached before the dark skin loads stays black otherwise
				m_statsLabelStyle.normal.textColor = EditorStyles.label.normal.textColor;

				// One region that fills all space below the buttons: it is both the shader list and the
				// drop target, so shaders can be dragged in whether or not the list already has entries.
				Rect dropArea = EditorGUILayout.BeginVertical( GUILayout.ExpandHeight( true ) );
				m_shaderScrollPos = GUILayout.BeginScrollView( m_shaderScrollPos, GUIStyle.none,
					GUI.skin.verticalScrollbar );
				List<ShaderAsset> visible = new List<ShaderAsset>();
				foreach ( ShaderAsset asset in m_shaders )
				{
					if ( !m_hideUnselected || asset.Selected )
					{
						visible.Add( asset );
					}
				}
				for ( int vi = 0; vi < visible.Count; vi++ )
				{
					ShaderAsset asset = visible[ vi ];
					Rect rowRect = EditorGUILayout.BeginHorizontal();
					if ( Event.current.type == EventType.Repaint && m_highlighted.Contains( asset ) )
					{
						Color focusColor = EditorGUIUtility.isProSkin
							? new Color( 0.3f, 0.5f, 0.85f, 0.3f )
							: new Color( 0.2f, 0.4f, 0.9f, 0.2f );
						EditorGUI.DrawRect( rowRect, focusColor );
					}
					EditorGUI.BeginChangeCheck();
					bool toggled = EditorGUILayout.Toggle( asset.Selected, GUILayout.Width( 14 ) );
					if ( EditorGUI.EndChangeCheck() )
					{
						if ( m_highlighted.Count > 1 && m_highlighted.Contains( asset ) )
						{
							// Clicking the checkbox of any highlighted row applies that new state to the
							// whole multi-selection, so the entire selection toggles/untoggles at once.
							foreach ( ShaderAsset sel in m_highlighted )
							{
								sel.Selected = toggled;
							}
						}
						else
						{
							asset.Selected = toggled;
						}
					}
					Rect labelRect = EditorGUILayout.GetControlRect();
					if ( asset.HasMissingFunctions )
					{
						EnsureMissingLabelStyle();
						string names = string.Join( ", ", asset.MissingFunctions );
						EditorGUI.LabelField( labelRect, new GUIContent( GetShaderSortString( asset ) + "   (missing function)",
							$"Skipped: this shader references shader function(s) that no longer exist in the project ({names}). "
							+ "Resaving it would drop those nodes and every wire connected to them." ), m_missingLabelStyle );
					}
					else if ( asset.ReadOnly )
					{
						EnsureReadOnlyLabelStyle();
						EditorGUI.LabelField( labelRect, GetShaderSortString( asset ) + "   (read-only)", m_readOnlyLabelStyle );
					}
					else
					{
						EditorGUI.LabelField( labelRect, GetShaderSortString( asset ) );
					}
					if ( Event.current.type == EventType.MouseDown && labelRect.Contains( Event.current.mousePosition ) )
					{
						HandleRowClick( visible, vi, asset, Event.current );
						Repaint();
						Event.current.Use();
					}
					var statsContent = new GUIContent( $"{asset.TextureCount} tex, {asset.FetchCount} fetch",
					"Texture and fetch counts are estimates. Values may be inaccurate and will improve over time." );
				EditorGUILayout.LabelField( statsContent, m_statsLabelStyle, GUILayout.Width( 120 ) );
					if ( GUILayout.Button( "Find", GUILayout.Width( 40 ) ) )
					{
						Selection.activeObject = asset.Shader;
						EditorGUIUtility.PingObject( asset.Shader );
					}
					EditorGUILayout.EndHorizontal();
				}
				GUILayout.EndScrollView();
				EditorGUILayout.EndVertical();

				// @diogo: drawn outside the scroll view so dropArea's window-space rect isn't clipped away.
				if ( m_shaders.Count == 0 && Event.current.type == EventType.Repaint )
				{
					if ( m_dropHintStyle == null )
					{
						m_dropHintStyle = new GUIStyle( EditorStyles.centeredGreyMiniLabel );
					}
					// @diogo: re-sync every frame; a copy cached before the skin loads keeps default
					// black/upper-left values otherwise
					m_dropHintStyle.alignment = TextAnchor.MiddleCenter;
					m_dropHintStyle.normal.textColor = EditorStyles.centeredGreyMiniLabel.normal.textColor;
					GUI.Label( dropArea, "Drag shaders here to add them", m_dropHintStyle );
				}

				Event dropEvt = Event.current;
				if ( ( dropEvt.type == EventType.DragUpdated || dropEvt.type == EventType.DragPerform )
					&& dropArea.Contains( dropEvt.mousePosition ) )
				{
					bool hasShaders = false;
					foreach ( UnityEngine.Object obj in DragAndDrop.objectReferences )
					{
						if ( obj is Shader ) { hasShaders = true; break; }
					}
					if ( hasShaders )
					{
						DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
						if ( dropEvt.type == EventType.DragUpdated )
						{
							EditorGUI.DrawRect( dropArea, new Color( 0.3f, 0.5f, 0.85f, 0.1f ) );
							Repaint();
						}
						else
						{
							DragAndDrop.AcceptDrag();
							AddDraggedShaders();
						}
						dropEvt.Use();
					}
				}
			}
		}

		// Row highlighting drives the "Toggle Selected" batch action and is independent of each entry's
		// checkbox: plain click selects only that row, Ctrl/Cmd+click adds or removes a row, and Shift+click
		// selects the contiguous range from the anchor. Indices are into the currently visible list so that
		// range selection respects the "Hide Unselected" filter and sort order.
		private void HandleRowClick( List<ShaderAsset> visible, int clickedVisibleIndex, ShaderAsset asset, Event evt )
		{
			if ( evt.clickCount == 2 )
			{
				Selection.activeObject = asset.Shader;
				EditorGUIUtility.PingObject( asset.Shader );
				return;
			}

			if ( evt.shift && m_selectionAnchor != null )
			{
				int anchorIndex = visible.IndexOf( m_selectionAnchor );
				if ( anchorIndex >= 0 )
				{
					m_highlighted.Clear();
					int lo = Mathf.Min( anchorIndex, clickedVisibleIndex );
					int hi = Mathf.Max( anchorIndex, clickedVisibleIndex );
					for ( int i = lo; i <= hi; i++ )
					{
						m_highlighted.Add( visible[ i ] );
					}
					return;
				}
			}

			if ( evt.control || evt.command )
			{
				if ( !m_highlighted.Remove( asset ) )
				{
					m_highlighted.Add( asset );
				}
			}
			else
			{
				m_highlighted.Clear();
				m_highlighted.Add( asset );
			}
			m_selectionAnchor = asset;
		}

		private int GetSelectedCount()
		{
			int count = 0;
			foreach ( ShaderAsset asset in m_shaders )
			{
				count += asset.Selected ? 1 : 0;
			}

			return count;
		}

		// Editable list of folder roots, mirroring Batch Update Shaders' "Extra Paths" ( text field + Browse per
		// row, add/remove/reorder ). Built lazily so it wraps the live m_searchPaths instance.
		private void EnsureSearchPathsList()
		{
			if ( m_searchPathsList != null )
			{
				return;
			}

			m_searchPathsList = new ReorderableList( m_searchPaths, typeof( string ), true, true, true, true );
			m_searchPathsList.elementHeight = 18;

			m_searchPathsList.drawHeaderCallback = ( Rect rect ) =>
			{
				EditorGUI.LabelField( rect, "Search Paths" );
			};

			m_searchPathsList.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
			{
				rect.height = EditorGUIUtility.singleLineHeight;
				rect.width -= 55;
				m_searchPaths[ index ] = EditorGUI.TextField( rect, "Path " + index, m_searchPaths[ index ] );

				rect.x += rect.width;
				rect.width = 55;
				if ( GUI.Button( rect, "Browse" ) )
				{
					m_searchPaths[ index ] = ASESaveBundleTool.FetchPath( "Folder Path", m_searchPaths[ index ] );
				}
			};

			m_searchPathsList.onAddCallback = ( ReorderableList list ) =>
			{
				m_searchPaths.Add( "Assets" );
			};

			m_searchPathsList.onRemoveCallback = ( ReorderableList list ) =>
			{
				int idx = ( list.index >= 0 && list.index < m_searchPaths.Count ) ? list.index : m_searchPaths.Count - 1;
				if ( idx >= 0 )
				{
					m_searchPaths.RemoveAt( idx );
				}
			};
		}

		// @diogo: Accepts folders dragged onto the search-path list, appending each ( project-relative ) folder
		// that isn't already listed. Mirrors the shader-list drop handling in OnGUI.
		private void HandleSearchPathDrop( Rect dropArea )
		{
			Event evt = Event.current;
			if ( ( evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform )
				|| !dropArea.Contains( evt.mousePosition ) )
			{
				return;
			}

			List<string> folders = new List<string>();
			foreach ( UnityEngine.Object obj in DragAndDrop.objectReferences )
			{
				string path = AssetDatabase.GetAssetPath( obj );
				if ( !string.IsNullOrEmpty( path ) && AssetDatabase.IsValidFolder( path ) && !folders.Contains( path ) )
				{
					folders.Add( path );
				}
			}
			if ( folders.Count == 0 )
			{
				return;
			}

			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			if ( evt.type == EventType.DragPerform )
			{
				DragAndDrop.AcceptDrag();
				bool anyAdded = false;
				foreach ( string folder in folders )
				{
					if ( !m_searchPaths.Contains( folder ) )
					{
						m_searchPaths.Add( folder );
						anyAdded = true;
					}
				}
				if ( anyAdded )
				{
					SaveSearchPaths();
				}
			}
			evt.Use();
			Repaint();
		}

		private void FetchShaders()
		{
			m_shaders.Clear();
			m_highlighted.Clear();
			m_selectionAnchor = null;
			m_functionFetchCounts = null;
			m_functionSwitchCounts = null;
			EnsureFunctionFetchCounts();
			BuildFunctionIdentityIndex();

			// Collect the valid, project-relative folder roots from the search-path list ( empty/invalid entries
			// are skipped ). Overlapping roots are fine - the asset paths are de-duplicated below.
			List<string> roots = new List<string>();
			foreach ( string raw in m_searchPaths )
			{
				string path = raw != null ? raw.Trim().Replace( '\\', '/' ) : string.Empty;
				if ( !string.IsNullOrEmpty( path ) && IsValidSearchRoot( path ) && !roots.Contains( path ) )
				{
					roots.Add( path );
				}
			}

			if ( roots.Count > 0 )
			{
				HashSet<string> seen = new HashSet<string>();
				string[] shaderGuids = AssetDatabase.FindAssets( "t:Shader", roots.ToArray() );
				foreach ( string guid in shaderGuids )
				{
					string assetPath = AssetDatabase.GUIDToAssetPath( guid );
					if ( !seen.Add( assetPath ) )
					{
						continue;
					}
					Shader shader = AssetDatabase.LoadAssetAtPath<Shader>( assetPath );
					if ( shader != null && IOUtils.IsASEShader( shader ) )
					{
						ShaderAsset asset = new ShaderAsset( assetPath, shader, true );
						asset.ReadOnly = IsReadOnlyShader( assetPath );
						asset.TextureCount = CountTextureProperties( shader );
						try
						{
							string fileText = File.ReadAllText( assetPath );
							asset.FetchCount = CountFetchesInShader( fileText );
							asset.MissingFunctions = FindMissingFunctions( fileText );
						}
						catch ( Exception )
						{
							asset.FetchCount = 0;
						}
						m_shaders.Add( asset );
					}
				}
			}
			m_shaders.Sort( ( x, y ) => String.Compare( GetShaderSortString( x ), GetShaderSortString( y ) ) );
		}

		private void ClearShaders()
		{
			m_shaders.Clear();
			m_highlighted.Clear();
			m_selectionAnchor = null;
		}

		private void AddDraggedShaders()
		{
			EnsureFunctionFetchCounts();
			BuildFunctionIdentityIndex();
			bool anyAdded = false;
			foreach ( UnityEngine.Object obj in DragAndDrop.objectReferences )
			{
				if ( !( obj is Shader shader ) )
				{
					continue;
				}
				if ( AddShaderAsset( AssetDatabase.GetAssetPath( shader ) ) )
				{
					anyAdded = true;
				}
			}
			if ( anyAdded )
			{
				m_shaders.Sort( ( x, y ) => String.Compare( GetShaderSortString( x ), GetShaderSortString( y ) ) );
				Repaint();
			}
		}

		// @diogo: Appends the shaders at the given paths to the current list, leaving whatever is already
		// listed in place.
		private void AddShaderPaths( List<string> shaderPaths )
		{
			EnsureFunctionFetchCounts();
			BuildFunctionIdentityIndex();
			bool anyAdded = false;
			foreach ( string path in shaderPaths )
			{
				if ( AddShaderAsset( path ) )
				{
					anyAdded = true;
				}
			}
			if ( anyAdded )
			{
				m_shaders.Sort( ( x, y ) => String.Compare( GetShaderSortString( x ), GetShaderSortString( y ) ) );
				Repaint();
			}
		}

		// @diogo: Builds and appends one list entry; returns false when the path is unusable or already listed.
		private bool AddShaderAsset( string path )
		{
			if ( string.IsNullOrEmpty( path ) || m_shaders.Exists( a => a.AssetPath == path ) )
			{
				return false;
			}
			Shader shader = AssetDatabase.LoadAssetAtPath<Shader>( path );
			if ( shader == null )
			{
				return false;
			}
			ShaderAsset asset = new ShaderAsset( path, shader, true );
			asset.ReadOnly = IsReadOnlyShader( path );
			asset.TextureCount = CountTextureProperties( shader );
			try
			{
				string fileText = File.ReadAllText( path );
				asset.FetchCount = CountFetchesInShader( fileText );
				asset.MissingFunctions = FindMissingFunctions( fileText );
			}
			catch ( Exception ) { }
			m_shaders.Add( asset );
			return true;
		}

		private static int CountOccurrences( string text, string pattern )
		{
			int count = 0;
			int index = 0;
			while ( ( index = text.IndexOf( pattern, index, StringComparison.Ordinal ) ) >= 0 )
			{
				count++;
				index += pattern.Length;
			}
			return count;
		}

		private void EnsureFunctionFetchCounts()
		{
			if ( m_functionFetchCounts != null )
			{
				return;
			}
			m_functionFetchCounts = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase );
			m_functionSwitchCounts = new Dictionary<string, Dictionary<string, int>>( StringComparer.OrdinalIgnoreCase );
			string[] guids = AssetDatabase.FindAssets( "t:AmplifyShaderFunction" );
			foreach ( string guid in guids )
			{
				string path = AssetDatabase.GUIDToAssetPath( guid );
				string funcName = Path.GetFileNameWithoutExtension( path );
				try
				{
					IndexFunctionAsset( funcName, File.ReadAllText( path ) );
				}
				catch ( Exception ) { }
			}
		}

		private void IndexFunctionAsset( string funcName, string rawText )
		{
			// YAML wraps long value strings with actual newlines + indentation; collapse to spaces
			// so that the literal two-char \n (ASE node separator) is the only delimiter we split on.
			var sb = new System.Text.StringBuilder( rawText.Length );
			for ( int i = 0; i < rawText.Length; )
			{
				char c = rawText[ i ];
				if ( c == '\r' || c == '\n' )
				{
					while ( i < rawText.Length && ( rawText[ i ] == '\r' || rawText[ i ] == '\n' ) ) { i++; }
					while ( i < rawText.Length && ( rawText[ i ] == ' ' || rawText[ i ] == '\t' ) ) { i++; }
					sb.Append( ' ' );
				}
				else
				{
					sb.Append( c );
					i++;
				}
			}
			string[] lines = sb.ToString().Split( new[] { @"\n" }, StringSplitOptions.RemoveEmptyEntries );

			var samplerIds = new HashSet<int>();
			var switchNodes = new Dictionary<int, int>();              // switchId → numOptions
			var getLocalVarRefs = new Dictionary<int, int>();          // getLocalVarNodeId → registerLocalVarNodeId
			var reverseByPort = new Dictionary<long, List<int>>();     // (destId*100000+destPort) → srcIds
			var reverseByNode = new Dictionary<int, List<int>>();      // destId → all srcIds

			foreach ( string rawLine in lines )
			{
				string line = rawLine.Trim();

				if ( line.StartsWith( "WireConnection;", StringComparison.Ordinal ) )
				{
					string[] p = line.Split( ';' );
					if ( p.Length >= 5 &&
						int.TryParse( p[ 1 ], out int destId ) &&
						int.TryParse( p[ 2 ], out int destPort ) &&
						int.TryParse( p[ 3 ], out int srcId ) )
					{
						long portKey = (long)destId * 100000L + destPort;
						if ( !reverseByPort.ContainsKey( portKey ) ) { reverseByPort[ portKey ] = new List<int>(); }
						reverseByPort[ portKey ].Add( srcId );
						if ( !reverseByNode.ContainsKey( destId ) ) { reverseByNode[ destId ] = new List<int>(); }
						reverseByNode[ destId ].Add( srcId );
					}
					continue;
				}

				if ( !line.StartsWith( "Node;", StringComparison.Ordinal ) )
				{
					continue;
				}

				int samplerMark = line.IndexOf( "AmplifyShaderEditor.SamplerNode,", StringComparison.Ordinal );
				if ( samplerMark >= 0 )
				{
					if ( TryParseNodeId( line, samplerMark, out int id ) ) { samplerIds.Add( id ); }
					continue;
				}

				int switchMark = line.IndexOf( "AmplifyShaderEditor.FunctionSwitch,", StringComparison.Ordinal );
				if ( switchMark >= 0 )
				{
					if ( TryParseSwitchInfo( line, switchMark, out int switchId, out int numOptions ) )
					{
						switchNodes[ switchId ] = numOptions;
					}
					continue;
				}

				int getVarMark = line.IndexOf( "AmplifyShaderEditor.GetLocalVarNode,", StringComparison.Ordinal );
				if ( getVarMark >= 0 )
				{
					if ( TryParseGetLocalVarRef( line, getVarMark, out int nodeId, out int refId ) )
					{
						getLocalVarRefs[ nodeId ] = refId;
					}
				}
			}

			if ( switchNodes.Count > 0 )
			{
				var switchCounts = new Dictionary<string, int>();
				foreach ( var kvp in switchNodes )
				{
					for ( int option = 0; option < kvp.Value; option++ )
					{
						switchCounts[ kvp.Key + ":" + option ] =
							BFSCountSamplers( kvp.Key, option, samplerIds, reverseByPort, reverseByNode, getLocalVarRefs );
					}
				}
				m_functionSwitchCounts[ funcName ] = switchCounts;
			}
			else if ( samplerIds.Count > 0 )
			{
				m_functionFetchCounts[ funcName ] = samplerIds.Count;
			}
		}

		private static bool TryParseNodeId( string line, int markerPos, out int id )
		{
			id = 0;
			int semi = line.IndexOf( ';', markerPos );
			if ( semi < 0 ) { return false; }
			int s = semi + 1, e = line.IndexOf( ';', s );
			return e > s && int.TryParse( line.Substring( s, e - s ).Trim(), out id );
		}

		private static bool TryParseSwitchInfo( string line, int markerPos, out int switchId, out int numOptions )
		{
			switchId = -1;
			numOptions = 0;
			// ID: skip 1 semicolon from marker
			int fp = line.IndexOf( ';', markerPos );
			if ( fp < 0 ) { return false; }
			int idS = fp + 1, idE = line.IndexOf( ';', idS );
			if ( idE <= idS || !int.TryParse( line.Substring( idS, idE - idS ).Trim(), out switchId ) ) { return false; }
			// numOptions: 6 more semicolons after the id end (pos, precision, bool, name, bool, int → numOptions)
			fp = idE;
			for ( int i = 0; i < 6 && fp >= 0; i++ ) { fp = line.IndexOf( ';', fp + 1 ); }
			if ( fp < 0 ) { return false; }
			int optS = fp + 1, optE = line.IndexOf( ';', optS );
			return optE > optS && int.TryParse( line.Substring( optS, optE - optS ).Trim(), out numOptions ) && numOptions > 0;
		}

		private static bool TryParseGetLocalVarRef( string line, int markerPos, out int nodeId, out int refId )
		{
			nodeId = -1;
			refId = -1;
			// Format: ...GetLocalVarNode,...;nodeId;pos;precision;bool;refId;varName;...
			int fp = line.IndexOf( ';', markerPos );
			if ( fp < 0 ) { return false; }
			int idS = fp + 1, idE = line.IndexOf( ';', idS );
			if ( idE <= idS || !int.TryParse( line.Substring( idS, idE - idS ).Trim(), out nodeId ) ) { return false; }
			// Skip 3 semicolons (pos, precision, bool) → refId
			fp = idE;
			for ( int i = 0; i < 3 && fp >= 0; i++ ) { fp = line.IndexOf( ';', fp + 1 ); }
			if ( fp < 0 ) { return false; }
			int refS = fp + 1, refE = line.IndexOf( ';', refS );
			return refE > refS && int.TryParse( line.Substring( refS, refE - refS ).Trim(), out refId );
		}

		private static int BFSCountSamplers( int switchId, int switchPort, HashSet<int> samplerIds,
			Dictionary<long, List<int>> reverseByPort, Dictionary<int, List<int>> reverseByNode,
			Dictionary<int, int> getLocalVarRefs )
		{
			var visited = new HashSet<int>();
			var queue = new Queue<int>();
			int count = 0;
			long startKey = (long)switchId * 100000L + switchPort;
			if ( reverseByPort.TryGetValue( startKey, out List<int> initialSrcs ) )
			{
				foreach ( int src in initialSrcs )
				{
					if ( visited.Add( src ) ) { queue.Enqueue( src ); }
				}
			}
			while ( queue.Count > 0 )
			{
				int nodeId = queue.Dequeue();
				if ( samplerIds.Contains( nodeId ) )
				{
					count++;
					continue;
				}
				// Follow GetLocalVar → RegisterLocalVar links (local variable cross-references)
				if ( getLocalVarRefs.TryGetValue( nodeId, out int refId ) )
				{
					if ( visited.Add( refId ) ) { queue.Enqueue( refId ); }
				}
				if ( reverseByNode.TryGetValue( nodeId, out List<int> srcs ) )
				{
					foreach ( int src in srcs )
					{
						if ( visited.Add( src ) ) { queue.Enqueue( src ); }
					}
				}
			}
			return count;
		}

		private int CountFunctionNodeFetches( string text )
		{
			const string marker = "AmplifyShaderEditor.FunctionNode,";
			int total = 0;
			int pos = 0;
			while ( ( pos = text.IndexOf( marker, pos, StringComparison.Ordinal ) ) >= 0 )
			{
				string funcName = null;
				string allOptions = null;
				if ( pos > 0 && text[ pos - 1 ] == ';' )
				{
					// Old format: field 5 = funcName, field 10 = allOptions
					int fp = pos;
					bool ok = true;
					for ( int i = 0; i < 5; i++ )
					{
						fp = text.IndexOf( ';', fp + 1 );
						if ( fp < 0 ) { ok = false; break; }
					}
					if ( ok )
					{
						int nameStart = fp + 1;
						int nameEnd = text.IndexOf( ';', nameStart );
						if ( nameEnd > nameStart )
						{
							funcName = text.Substring( nameStart, nameEnd - nameStart ).Trim();
							// allOptions is 5 more fields after funcName
							fp = nameEnd;
							for ( int i = 0; i < 5 && ok; i++ )
							{
								fp = text.IndexOf( ';', fp + 1 );
								if ( fp < 0 ) { ok = false; break; }
							}
							if ( ok )
							{
								int optStart = fp + 1;
								int optEnd = text.IndexOf( ';', optStart );
								if ( optEnd > optStart )
								{
									allOptions = text.Substring( optStart, optEnd - optStart ).Trim();
								}
							}
						}
					}
				}
				else if ( pos > 0 && text[ pos - 1 ] == '"' )
				{
					// JSON Lines format: funcName = params[2], allOptions = params[7]
					int lineEnd = text.IndexOf( '\n', pos );
					if ( lineEnd < 0 ) { lineEnd = text.Length; }
					int paramsIdx = text.IndexOf( "\"params\":[", pos, lineEnd - pos, StringComparison.Ordinal );
					if ( paramsIdx >= 0 )
					{
						int strPos = paramsIdx + 10;
						for ( int paramIdx = 0; paramIdx <= 7 && strPos < lineEnd; paramIdx++ )
						{
							int openQ = text.IndexOf( '"', strPos );
							if ( openQ < 0 || openQ >= lineEnd ) { break; }
							int closeQ = text.IndexOf( '"', openQ + 1 );
							if ( closeQ < 0 || closeQ >= lineEnd ) { break; }
							if ( paramIdx == 2 )
							{
								funcName = text.Substring( openQ + 1, closeQ - openQ - 1 );
							}
							else if ( paramIdx == 7 )
							{
								allOptions = text.Substring( openQ + 1, closeQ - openQ - 1 );
							}
							strPos = closeQ + 1;
						}
					}
				}
				if ( funcName != null )
				{
					total += ResolveFunctionFetchCount( funcName, allOptions );
				}
				pos += marker.Length;
			}
			return total;
		}

		private int ResolveFunctionFetchCount( string funcName, string allOptions )
		{
			if ( allOptions != null && m_functionSwitchCounts != null &&
				m_functionSwitchCounts.TryGetValue( funcName, out var switchCounts ) )
			{
				// allOptions format: "numSwitches,switchId1,opt1,switchId2,opt2,..."
				// Use the first switch's selected option to determine the fetch count.
				string[] parts = allOptions.Split( ',' );
				if ( parts.Length >= 3 &&
					int.TryParse( parts[ 1 ].Trim(), out int switchId ) &&
					int.TryParse( parts[ 2 ].Trim(), out int option ) )
				{
					string key = switchId + ":" + option;
					if ( switchCounts.TryGetValue( key, out int count ) )
					{
						return count;
					}
				}
			}
			if ( m_functionFetchCounts.TryGetValue( funcName, out int fallback ) )
			{
				return fallback;
			}
			return 0;
		}

		private int CountPOMFetches( string text )
		{
			const string marker = "AmplifyShaderEditor.ParallaxOcclusionMappingNode,";
			int total = 0;
			int pos = 0;
			while ( ( pos = text.IndexOf( marker, pos, StringComparison.Ordinal ) ) >= 0 )
			{
				int minSamples = 8;
				if ( pos > 0 && text[ pos - 1 ] == ';' )
				{
					// Old semicolon format: Node;Type;id;pos;precision;bool;selectedChannelInt;minSamples;...
					// Skip 6 semicolons from the type field start to reach minSamples.
					int fieldPos = pos;
					bool ok = true;
					for ( int i = 0; i < 6; i++ )
					{
						fieldPos = text.IndexOf( ';', fieldPos + 1 );
						if ( fieldPos < 0 ) { ok = false; break; }
					}
					if ( ok )
					{
						int valStart = fieldPos + 1;
						int valEnd = text.IndexOf( ';', valStart );
						if ( valEnd > valStart )
						{
							int.TryParse( text.Substring( valStart, valEnd - valStart ).Trim(), out minSamples );
						}
					}
				}
				else if ( pos > 0 && text[ pos - 1 ] == '"' )
				{
					// New JSON Lines format: params[3] = minSamples.m_value
					// params = ["precision","bool","selectedChannelInt","minSamples",...]
					int lineEnd = text.IndexOf( '\n', pos );
					if ( lineEnd < 0 ) { lineEnd = text.Length; }
					int paramsIdx = text.IndexOf( "\"params\":[", pos, lineEnd - pos, StringComparison.Ordinal );
					if ( paramsIdx >= 0 )
					{
						int strPos = paramsIdx + 10;
						bool ok = true;
						for ( int i = 0; i < 3 && ok; i++ )
						{
							int openQ = text.IndexOf( '"', strPos );
							if ( openQ < 0 || openQ >= lineEnd ) { ok = false; break; }
							int closeQ = text.IndexOf( '"', openQ + 1 );
							if ( closeQ < 0 || closeQ >= lineEnd ) { ok = false; break; }
							strPos = closeQ + 1;
						}
						if ( ok )
						{
							int openQ = text.IndexOf( '"', strPos );
							if ( openQ >= 0 && openQ < lineEnd )
							{
								int closeQ = text.IndexOf( '"', openQ + 1 );
								if ( closeQ > openQ && closeQ <= lineEnd )
								{
									int.TryParse( text.Substring( openQ + 1, closeQ - openQ - 1 ), out minSamples );
								}
							}
						}
					}
				}
				total += minSamples;
				pos += marker.Length;
			}
			return total;
		}

		private int CountFetchesInShader( string text )
		{
			int direct = CountOccurrences( text, "AmplifyShaderEditor.SamplerNode" );
			int pom = CountPOMFetches( text );
			int triplanar = CountOccurrences( text, "AmplifyShaderEditor.TriplanarNode" ) * 3;
			int fromFunctions = CountFunctionNodeFetches( text );
			return direct + pom + triplanar + fromFunctions;
		}

		// Count of exposed texture properties ( a robust, read-only proxy for "textures in a shader" that
		// doesn't require loading the ASE graph ).
		private static int CountTextureProperties( Shader shader )
		{
			int count = 0;
			int propertyCount = shader.GetPropertyCount();
			for ( int i = 0; i < propertyCount; i++ )
			{
				if ( shader.GetPropertyType( i ) == UnityEngine.Rendering.ShaderPropertyType.Texture )
				{
					count++;
				}
			}
			return count;
		}

		private string GetShaderSortString( ShaderAsset asset )
		{
			string label = asset.AssetPath;
			switch ( m_sortMethod )
			{
				case SortMethod.FILENAME:
					label = Path.GetFileName( asset.AssetPath );
					break;
				case SortMethod.NAME:
					label = asset.Shader.name;
					break;
			}

			return label;
		}

		private void PartitionSelectedPaths( out List<string> writable, out List<string> readOnly, out List<(string path, Shader shader, string funcs)> missing )
		{
			writable = new List<string>();
			readOnly = new List<string>();
			missing = new List<(string path, Shader shader, string funcs)>();
			// @diogo: Rebuild so a shader function deleted after Fetch is caught before the write is attempted.
			BuildFunctionIdentityIndex();
			foreach ( ShaderAsset asset in m_shaders )
			{
				if ( !asset.Selected )
				{
					continue;
				}
				// @diogo: Re-check now so a file made read-only after Fetch is caught before the write is attempted.
				asset.ReadOnly = IsReadOnlyShader( asset.AssetPath );
				// @diogo: Keep the Fetch-time result if the recheck read fails, so a flagged shader stays skipped.
				List<string> reread = ReadMissingFunctions( asset.AssetPath, out bool couldNotRead );
				if ( !couldNotRead )
				{
					asset.MissingFunctions = reread;
				}
				if ( asset.HasMissingFunctions )
				{
					missing.Add( ( asset.AssetPath, asset.Shader, string.Join( ", ", asset.MissingFunctions ) ) );
				}
				else
				{
					( asset.ReadOnly ? readOnly : writable ).Add( asset.AssetPath );
				}
			}
		}

		// @diogo: FindAssets accepts the virtual "Packages" root even though IsValidFolder returns false for it.
		private static bool IsValidSearchRoot( string path )
		{
			return path == "Packages" || AssetDatabase.IsValidFolder( path );
		}

		// @diogo: A shader is read-only if it lives in an immutable package ( registry/git/builtin/tarball, in
		// the read-only cache ) or its file on disk is flagged read-only ( set manually or VCS-locked ).
		// Embedded/Local packages and Assets are otherwise writable.
		private static bool IsReadOnlyShader( string assetPath )
		{
			if ( assetPath.StartsWith( "Packages/", StringComparison.Ordinal ) )
			{
				UnityEditor.PackageManager.PackageInfo info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath( assetPath );
				if ( info != null
					&& info.source != UnityEditor.PackageManager.PackageSource.Embedded
					&& info.source != UnityEditor.PackageManager.PackageSource.Local )
				{
					return true;
				}
			}
			try
			{
				string fullPath = Path.GetFullPath( assetPath );
				return File.Exists( fullPath ) && ( File.GetAttributes( fullPath ) & FileAttributes.ReadOnly ) != 0;
			}
			catch ( Exception )
			{
				return false;
			}
		}

		// @diogo: Rebuilds the shader function identity index. Called before every fetch/batch so functions
		// created or deleted since the last build are seen.
		private static void BuildFunctionIdentityIndex()
		{
			m_functionGuids = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
			m_functionNames = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
			foreach ( string guid in AssetDatabase.FindAssets( "t:AmplifyShaderFunction" ) )
			{
				m_functionGuids.Add( guid );
				m_functionNames.Add( Path.GetFileNameWithoutExtension( AssetDatabase.GUIDToAssetPath( guid ) ) );
			}
		}

		// @diogo: Names of the shader functions this graph references that no longer resolve. Resaving such a
		// shader is destructive: FunctionNode.WriteToString re-derives the GUID from the unresolved
		// function and writes it empty, and the node has no ports, so every wire on it is dropped.
		private static List<string> FindMissingFunctions( string text )
		{
			const string marker = "AmplifyShaderEditor.FunctionNode";
			List<string> missing = null;
			int pos = 0;
			while ( ( pos = text.IndexOf( marker, pos, StringComparison.Ordinal ) ) >= 0 )
			{
				int after = pos + marker.Length;
				// @diogo: ',' assembly-qualified, ';' bare (older graphs); anything else is a longer type name.
				char delimiter = after < text.Length ? text[ after ] : '\0';
				if ( pos > 0 && ( delimiter == ',' || delimiter == ';' ) )
				{
					string funcName = null;
					string funcGUID = null;
					bool parsed = false;
					if ( text[ pos - 1 ] == '"' )
					{
						parsed = TryParseJsonFunctionRef( text, pos, out funcName, out funcGUID );
					}
					else if ( text[ pos - 1 ] == ';' )
					{
						parsed = TryParseLegacyFunctionRef( text, pos, out funcName, out funcGUID );
					}
					if ( parsed && !IsFunctionResolvable( funcName, funcGUID ) )
					{
						if ( missing == null )
						{
							missing = new List<string>();
						}
						if ( !missing.Contains( funcName ) )
						{
							missing.Add( funcName );
						}
					}
				}
				pos = after;
			}
			return missing;
		}

		// @diogo: Mirrors FunctionNode.ReadFromString's lookup chain, so a function that merely moved (GUID
		// changed, name kept) is not reported. GUIDs retired into native nodes by ShaderFunctionToNode
		// are deliberately unresolvable and must not count as missing either.
		private static bool IsFunctionResolvable( string funcName, string funcGUID )
		{
			if ( IsFunctionGUID( funcGUID ) && ( m_functionGuids.Contains( funcGUID ) || ShaderFunctionToNode.IsEligible( funcGUID ) ) )
			{
				return true;
			}
			return !string.IsNullOrEmpty( funcName ) && m_functionNames.Contains( funcName );
		}

		// @diogo: Graphs older than version 13704 store no GUID at all, and a mis-parse must never read as
		// "missing"; anything that is not 32 hex chars falls back to the filename match alone.
		private static bool IsFunctionGUID( string value )
		{
			if ( string.IsNullOrEmpty( value ) || value.Length != 32 )
			{
				return false;
			}
			foreach ( char c in value )
			{
				bool isHex = ( c >= '0' && c <= '9' ) || ( c >= 'a' && c <= 'f' ) || ( c >= 'A' && c <= 'F' );
				if ( !isHex )
				{
					return false;
				}
			}
			return true;
		}

		// @diogo: Params are ["precision","?",funcName,orderIndex,headerTitle,graphId,funcGUID,options,...].
		private static bool TryParseJsonFunctionRef( string text, int pos, out string funcName, out string funcGUID )
		{
			funcName = null;
			funcGUID = null;
			int lineEnd = text.IndexOf( '\n', pos );
			if ( lineEnd < 0 )
			{
				lineEnd = text.Length;
			}
			int paramsIdx = text.IndexOf( "\"params\":[", pos, lineEnd - pos, StringComparison.Ordinal );
			if ( paramsIdx < 0 )
			{
				return false;
			}
			int strPos = paramsIdx + 10;
			for ( int paramIdx = 0; paramIdx <= 6 && strPos < lineEnd; paramIdx++ )
			{
				int openQ = text.IndexOf( '"', strPos );
				if ( openQ < 0 || openQ >= lineEnd )
				{
					break;
				}
				int closeQ = text.IndexOf( '"', openQ + 1 );
				if ( closeQ < 0 || closeQ >= lineEnd )
				{
					break;
				}
				if ( paramIdx == 2 )
				{
					funcName = text.Substring( openQ + 1, closeQ - openQ - 1 );
				}
				else if ( paramIdx == 6 )
				{
					funcGUID = text.Substring( openQ + 1, closeQ - openQ - 1 );
				}
				strPos = closeQ + 1;
			}
			return funcName != null;
		}

		// @diogo: Fields after the type are id, pos, precision, ?, funcName, orderIndex, headerTitle, graphId,
		// funcGUID, options; so funcName is 5 separators in and funcGUID 3 more past its terminator.
		private static bool TryParseLegacyFunctionRef( string text, int pos, out string funcName, out string funcGUID )
		{
			funcName = null;
			funcGUID = null;
			int fp = pos;
			for ( int i = 0; i < 5; i++ )
			{
				fp = text.IndexOf( ';', fp + 1 );
				if ( fp < 0 )
				{
					return false;
				}
			}
			int nameStart = fp + 1;
			int nameEnd = text.IndexOf( ';', nameStart );
			if ( nameEnd <= nameStart )
			{
				return false;
			}
			funcName = text.Substring( nameStart, nameEnd - nameStart ).Trim();

			fp = nameEnd;
			for ( int i = 0; i < 3; i++ )
			{
				fp = text.IndexOf( ';', fp + 1 );
				if ( fp < 0 )
				{
					return true;
				}
			}
			int guidStart = fp + 1;
			int guidEnd = text.IndexOf( ';', guidStart );
			if ( guidEnd > guidStart )
			{
				funcGUID = text.Substring( guidStart, guidEnd - guidStart ).Trim();
			}
			return true;
		}

		// @diogo: couldNotRead lets the write paths skip a shader they cannot inspect rather than assume it is
		// safe: an unreadable file must never be resaved on the strength of a null "no missing" result.
		private static List<string> ReadMissingFunctions( string assetPath, out bool couldNotRead )
		{
			try
			{
				couldNotRead = false;
				return FindMissingFunctions( File.ReadAllText( assetPath ) );
			}
			catch ( Exception )
			{
				couldNotRead = true;
				return null;
			}
		}

		// @diogo: Reads as "2 are read-only and 1 references missing shader functions" for the skip dialogs.
		private static string DescribeSkipped( List<string> readOnly, List<(string path, Shader shader, string funcs)> missing )
		{
			string readOnlyPart = $"{readOnly.Count} read-only";
			string missingPart = $"{missing.Count} referencing missing shader functions (see Console)";
			if ( missing.Count == 0 )
			{
				return readOnlyPart;
			}
			if ( readOnly.Count == 0 )
			{
				return missingPart;
			}
			return readOnlyPart + " and " + missingPart;
		}

		private static void LogSkippedShaders( List<string> readOnly, List<(string path, Shader shader, string funcs)> missing )
		{
			if ( readOnly.Count > 0 )
			{
				Debug.Log( $"[Shader Utility] Skipping {readOnly.Count} read-only shader(s):\n" + string.Join( "\n", readOnly ) );
			}
			foreach ( var entry in missing )
			{
				string fileName = Path.GetFileName( entry.path );
				if ( entry.funcs == null )
				{
					Debug.LogWarning( $"[Shader Utility] Skipping \"{fileName}\": could not read file to verify missing shader functions", entry.shader );
				}
				else
				{
					Debug.LogWarning( $"[Shader Utility] Skipping \"{fileName}\" due to missing shader functions: {entry.funcs}", entry.shader );
				}
			}
		}

		private void EnsureMissingLabelStyle()
		{
			if ( m_missingLabelStyle == null )
			{
				m_missingLabelStyle = new GUIStyle( EditorStyles.label );
			}
			// @diogo: re-sync every frame for the same reason as the read-only style below
			m_missingLabelStyle.normal.textColor = EditorGUIUtility.isProSkin
				? new Color( 1f, 0.42f, 0.42f )
				: new Color( 0.7f, 0f, 0f );
		}

		private void EnsureReadOnlyLabelStyle()
		{
			if ( m_readOnlyLabelStyle == null )
			{
				m_readOnlyLabelStyle = new GUIStyle( EditorStyles.label );
			}
			// @diogo: re-sync every frame; a copy cached before the dark skin loads stays black otherwise
			Color c = EditorStyles.label.normal.textColor;
			c.a = 0.5f;
			m_readOnlyLabelStyle.normal.textColor = c;
		}

		private void ResaveShaders()
		{
			PartitionSelectedPaths( out List<string> writable, out List<string> readOnly, out var missing );
			if ( writable.Count == 0 && readOnly.Count == 0 && missing.Count == 0 )
			{
				return;
			}

			if ( writable.Count == 0 )
			{
				LogSkippedShaders( readOnly, missing );
				EditorUtility.DisplayDialog( "Resave Selected Shaders",
					$"None of the {readOnly.Count + missing.Count} selected shader(s) can be resaved: {DescribeSkipped( readOnly, missing )}.",
					"OK" );
				return;
			}

			// @diogo: One confirm only. When skipped shaders are present the skip notice is folded into the
			// time-cost warning; the "don't show again" pref only suppresses the plain time-cost confirm.
			bool proceed;
			int skipped = readOnly.Count + missing.Count;
			if ( skipped > 0 )
			{
				proceed = EditorUtility.DisplayDialog( "Resave Selected Shaders",
					$"{skipped} of {writable.Count + skipped} selected shader(s) will be skipped: {DescribeSkipped( readOnly, missing )}.\n\nThis will resave the remaining {writable.Count} shader(s), which can take a while and tie up your machine during the process. Do you wish to proceed?",
					$"Resave {writable.Count}",
					"Cancel" );
			}
			else
			{
				proceed = EditorPrefs.GetBool( PREF_RESAVE_SKIP_CONFIRM, false ) || ResaveConfirmDialog.ShowDialog();
			}
			if ( !proceed )
			{
				return;
			}

			LogSkippedShaders( readOnly, missing );
			AmplifyShaderEditorWindow.InitBatch( writable.Count );
			AmplifyShaderEditorWindow.LoadAndSaveList( writable.ToArray() );
		}

		private static void EnsurePlatformLabels()
		{
			if ( m_platformLabels != null )
			{
				return;
			}
			var filtered = new System.Collections.Generic.List<RenderPlatformInfo>();
			foreach ( RenderPlatformInfo info in RenderingPlatformOpHelper.RenderingPlatformsInfo )
			{
				if ( info.Value != RenderPlatforms.playstation )
				{
					filtered.Add( info );
				}
			}
			m_platformInfoFiltered = filtered.ToArray();
			m_platformLabels = new string[ m_platformInfoFiltered.Length ];
			for ( int i = 0; i < m_platformInfoFiltered.Length; i++ )
			{
				m_platformLabels[ i ] = m_platformInfoFiltered[ i ].Label.Trim().Replace( '/', '∕' );
			}
		}

		private void ModifyPlatformOnSelected( bool enable )
		{
			PartitionSelectedPaths( out List<string> writable, out List<string> readOnly, out var missing );
			if ( writable.Count == 0 && readOnly.Count == 0 && missing.Count == 0 )
			{
				return;
			}

			EnsurePlatformLabels();
			m_platformIndex = Mathf.Clamp( m_platformIndex, 0, m_platformInfoFiltered.Length - 1 );
			RenderPlatformInfo info = m_platformInfoFiltered[ m_platformIndex ];
			string action = enable ? "Enable" : "Disable";

			if ( writable.Count == 0 )
			{
				LogSkippedShaders( readOnly, missing );
				EditorUtility.DisplayDialog( action + " Platform",
					$"None of the {readOnly.Count + missing.Count} selected shader(s) can be modified: {DescribeSkipped( readOnly, missing )}.",
					"OK" );
				return;
			}

			int skipped = readOnly.Count + missing.Count;
			string skippedNote = skipped > 0
				? $" {skipped} shader(s) will be skipped: {DescribeSkipped( readOnly, missing )}."
				: string.Empty;
			bool ok = EditorUtility.DisplayDialog(
				action + " Platform",
				$"This will {( enable ? "enable" : "disable" )} '{info.Label.Trim()}' on {writable.Count} selected shader(s), reloading and resaving each.{skippedNote} This can take a while and tie up your machine during the process. Do you wish to proceed?",
				"OK",
				"Cancel"
			);
			if ( !ok )
			{
				return;
			}
			LogSkippedShaders( readOnly, missing );

			AmplifyShaderEditorWindow.LoadModifyAndSaveList( writable.ToArray(), info.Value, enable );
		}

		private void EnsureNodeTypeCache()
		{
			if ( m_nodeTypeCache != null )
			{
				return;
			}
			m_nodeTypeCache = new List<NodeEntry>();
			Type baseType = typeof( ParentNode );
			foreach ( Type type in baseType.Assembly.GetTypes() )
			{
				if ( type.IsAbstract || !baseType.IsAssignableFrom( type ) || type.Namespace != "AmplifyShaderEditor" )
				{
					continue;
				}
				object[] rawAttrs = type.GetCustomAttributes( typeof( NodeAttributes ), false );
				if ( rawAttrs.Length == 0 || !( rawAttrs[ 0 ] is NodeAttributes attr ) || !attr.Available )
				{
					continue;
				}
				m_nodeTypeCache.Add( new NodeEntry { DisplayName = attr.Name, TypeName = type.Name } );
			}
			m_nodeTypeCache.Sort( ( a, b ) => string.Compare( a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase ) );
		}

		private void UpdateFilteredTypes()
		{
			m_filteredItems.Clear();
			m_highlightedIndex = 0;
			if ( string.IsNullOrEmpty( m_nodeSearch ) )
			{
				m_showAutocomplete = false;
				m_selectedTypeName = string.Empty;
				return;
			}
			EnsureNodeTypeCache();
			string lower = m_nodeSearch.ToLowerInvariant();
			foreach ( NodeEntry entry in m_nodeTypeCache )
			{
				if ( entry.DisplayName.ToLowerInvariant().Contains( lower ) )
				{
					m_filteredItems.Add( entry );
				}
			}
			m_showAutocomplete = m_filteredItems.Count > 0;
			m_selectedTypeName = m_nodeSearch.Trim();
		}

		private void CommitAutocomplete( int index )
		{
			if ( index < 0 || index >= m_filteredItems.Count )
			{
				return;
			}
			NodeEntry entry = m_filteredItems[ index ];
			m_nodeSearch = entry.DisplayName;
			m_selectedTypeName = entry.TypeName;
			m_showAutocomplete = false;
			m_filteredItems.Clear();
			// Drop focus and end edit mode so the search field re-reads the committed value next repaint;
			// keeping it focused would leave the field showing the stale text the user had typed.
			GUIUtility.keyboardControl = 0;
			EditorGUIUtility.editingTextField = false;
			GUI.FocusControl( null );
			Repaint();
		}

		private class ResaveConfirmDialog : EditorWindow
		{
			private bool m_rememberChoice = false;
			private bool m_confirmed = false;

			public static bool ShowDialog()
			{
				var dialog = CreateInstance<ResaveConfirmDialog>();
				dialog.titleContent = new GUIContent( "Resave Shaders" );
				Vector2 size = new Vector2( 410, 95 );
				dialog.minSize = dialog.maxSize = size;
				Rect main = EditorGUIUtility.GetMainWindowPosition();
				dialog.position = new Rect(
					main.x + ( main.width - size.x ) * 0.5f,
					main.y + ( main.height - size.y ) * 0.5f,
					size.x,
					size.y
				);
				dialog.ShowModal();
				return dialog.m_confirmed;
			}

			private void OnGUI()
			{
				GUILayout.Space( 10 );
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space( 15 );
				GUILayout.Label( EditorGUIUtility.IconContent( "console.infoicon" ), GUILayout.Width( 36 ), GUILayout.Height( 36 ) );
				GUILayout.Space( 4 );

				EditorGUILayout.BeginVertical();
				{
					EditorGUILayout.LabelField( "This can take a while and tie up your machine during the process. Do you wish to proceed?", EditorStyles.wordWrappedLabel );
					GUILayout.Space( 20 );
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Space( 10 );

						EditorGUILayout.BeginVertical();
						{
							EditorGUILayout.BeginHorizontal();
							{
								m_rememberChoice = EditorGUILayout.Toggle( m_rememberChoice, GUILayout.Width( 15 ) );
								GUILayout.Space( 1 );
								EditorGUILayout.BeginVertical();
								GUILayout.Space( 3 );
								if ( GUILayout.Button( "Don't show again", EditorStyles.label ) )
								{
									m_rememberChoice = !m_rememberChoice;
								}
								EditorGUILayout.EndVertical();
								GUILayout.FlexibleSpace();
							}
							EditorGUILayout.EndHorizontal();
						}
						EditorGUILayout.EndVertical();

						if ( GUILayout.Button( "Continue", GUILayout.Width( 92 ), GUILayout.Height( 23 ) ) )
						{
							m_confirmed = true;
							if ( m_rememberChoice )
							{
								EditorPrefs.SetBool( PREF_RESAVE_SKIP_CONFIRM, true );
							}
							Close();
						}
						GUILayout.Space( 5 );
						if ( GUILayout.Button( "Cancel", GUILayout.Width( 92 ), GUILayout.Height( 23 ) ) )
						{
							m_confirmed = false;
							Close();
						}
						GUILayout.Space( 3 );
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();
				GUILayout.Space( 6 );
			}
		}

		private void ScrollAutocompleteToHighlight()
		{
			float itemHeight = EditorGUIUtility.singleLineHeight + 2;
			float visibleHeight = itemHeight * 8;
			float targetTop = m_highlightedIndex * itemHeight;
			if ( targetTop < m_autocompleteScrollPos.y )
			{
				m_autocompleteScrollPos.y = targetTop;
			}
			else if ( targetTop + itemHeight > m_autocompleteScrollPos.y + visibleHeight )
			{
				m_autocompleteScrollPos.y = targetTop + itemHeight - visibleHeight;
			}
			Repaint();
		}

		// Selects every fetched shader whose serialized ASE graph references a node type matching the search
		// text. ASE serializes nodes as "Node;AmplifyShaderEditor.<TypeName>, AmplifyShaderEditor;..." so a
		// case-insensitive match against the "AmplifyShaderEditor.<search>" prefix finds them ( e.g. "Fresnel"
		// or "FresnelNode" both find FresnelNode ). Type names are class names, which may differ from a node's
		// display name.
		private void SelectShadersUsingNode()
		{
			string token = "AmplifyShaderEditor." + m_selectedTypeName;
			int matches = 0;
			foreach ( ShaderAsset asset in m_shaders )
			{
				bool used = false;
				try
				{
					string text = File.ReadAllText( asset.AssetPath );
					used = text.IndexOf( token, StringComparison.OrdinalIgnoreCase ) >= 0;
				}
				catch ( Exception )
				{
					used = false;
				}
				asset.Selected = used;
				if ( used )
				{
					matches++;
				}
			}
			Debug.Log( $"[Shader Utility] {matches} shader(s) use a node matching \"{m_selectedTypeName}\"." );
		}
	}
}

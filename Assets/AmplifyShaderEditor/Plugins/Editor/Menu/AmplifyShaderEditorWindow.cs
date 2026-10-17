// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Networking;

namespace AmplifyShaderEditor
{
	// Disabling Substance Deprecated warning

	public partial class AmplifyShaderEditorWindow : SearchableEditorWindow, ISerializationCallbackReceiver
	{
		public const string PreviewSizeGlobalVariable = "_ASEPreviewSize";

		public const double InactivitySaveTime = 1.0;

		public const string CopyCommand = "Copy";
		public const string PasteCommand = "Paste";
		public const string SelectAll = "SelectAll";
		public const string Duplicate = "Duplicate";
		public const string ObjectSelectorClosed = "ObjectSelectorClosed";
		public const string LiveShaderError = "Live Shader only works with an assigned Master Node on the graph";

		private const string OnlineSharingWarning = "Please enable \"Allow downloads over HTTP*\" in Player Settings to use the Online Sharing feature.";

		//public Texture2D MasterNodeOnTexture = null;
		//public Texture2D MasterNodeOffTexture = null;

		//public Texture2D GPUInstancedOnTexture = null;
		//public Texture2D GPUInstancedOffTexture = null;

		private bool m_initialized = false;
		private bool m_checkInvalidConnections = false;
		private bool m_afterDeserializeFlag = true;


		[SerializeField]
		private ParentGraph m_customGraph = null;

		// UI
		private Rect m_graphArea;
		private Texture2D m_graphBgTexture;
		private Texture2D m_graphFgTexture;
		private GUIStyle m_graphFontStyle;
		//private GUIStyle _borderStyle;
		private Texture2D m_wireTexture;

		[SerializeField]
		private InnerWindowEditorVariables m_innerEditorVariables;

		[SerializeField]
		private string m_lastpath;

		[SerializeField]
		private ASESelectionMode m_selectionMode = ASESelectionMode.Shader;

		[SerializeField]
		private DuplicatePreventionBuffer m_duplicatePreventionBuffer;

		[SerializeField]
		private double m_inactivityTime = 0;

		// Prevent save ops every tick when on live mode
		[SerializeField]
		private double m_lastTimeSaved = 0;

		[SerializeField]
		private bool m_cacheSaveOp = false;
		private const double SaveTime = 1;

		private bool m_markedToSave = false;

		// Graph logic
		[SerializeField]
		private ParentGraph m_mainGraphInstance;
		public ParentGraph MainGraphInstance { get { return m_mainGraphInstance; } }

		// Camera control
		[SerializeField]
		private Vector2 m_cameraOffset;

		private float m_cameraSpeed = 1;

		private Rect m_cameraInfo;

		[SerializeField]
		private float m_cameraZoom;

		[SerializeField]
		private Vector2 m_minNodePos;

		[SerializeField]
		private Vector2 m_maxNodePos;

		[SerializeField]
		private bool m_isDirty;

		[SerializeField]
		private bool m_saveIsDirty;

		[SerializeField]
		private bool m_repaintIsDirty;

		[SerializeField]
		private bool m_liveShaderEditing = false;

		[SerializeField]
		private bool m_shaderIsModified = false;

		// Checksum of the graph as it was at the last save/load. The Save indicator stays green
		// ( unmodified ) while the current graph still matches this; editing away from it - or
		// undoing/redoing to a state that differs - turns it off. See EvaluateModifiedState.
		[SerializeField]
		private string m_savedGraphChecksum = string.Empty;

		// Requests a one-shot re-evaluation of the modified state on the next OnGUI, set at discrete
		// commit points ( edit-gesture end, undo/redo ) so the whole-graph checksum is computed once per
		// commit instead of every frame.
		private bool m_recheckModified = false;

		// When an undo/redo triggers the re-evaluation, this holds the exact snapshot string the undo
		// step restored ( the pre-reload bytes ). EvaluateModifiedState compares it directly instead of
		// re-serializing the reloaded graph, which would not reproduce those bytes. Null for plain edits.
		private string m_recheckSnapshot = null;

		// @diogo: diagnostic only ( UndoProfiling ): the saved baseline bytes behind m_savedGraphChecksum,
		// kept so a modified verdict on an undo/redo restore can be diffed against the baseline
		private string m_savedGraphSnapshot = null;

		// Latched after an undo/redo: the checksum verdict EvaluateModifiedState just computed is
		// authoritative until the next real edit. The freshly reloaded graph keeps re-asserting SaveIsDirty
		// for several frames ( node/signal churn from the rebuild ), and the OnGUI dirty path would
		// otherwise flip the indicator back to modified frame after frame. While this is set that path
		// consumes the dirty without overriding the verdict. Cleared by any genuine edit ( RegisterGraphUndo )
		// and by a fresh baseline capture ( save/load ).
		private bool m_undoVerdictActive = false;

		// Auto-backup scheduling. Editor-time based and intentionally not serialized: a domain reload
		// re-arms the timer and re-takes a first backup, which is harmless. m_lastAutoBackupChecksum lets
		// the timer skip writing when the graph has not changed since the previous backup, so an idle
		// graph does not fill the history with identical copies.
		private double m_nextAutoBackupTime = 0;
		private string m_lastAutoBackupChecksum = string.Empty;

		// Throttles the periodic repaint that keeps the Auto Backup panel's relative timestamps current
		// while that tab is open ( see UpdateNodePreviewListAndTime ).
		private double m_nextAutoBackupRepaint = 0;

		// Snapshot the backup panel asked to restore. Deferred and applied at a controlled point in OnGUI
		// ( same place the undo reload runs ) so the graph rebuild never happens mid-draw.
		private string m_pendingBackupSnapshot = null;

		[SerializeField]
		private string m_lastOpenedLocation = string.Empty;

		[SerializeField]
		private bool m_zoomChanged = true;

		// @diogo: zoom-scaled previews debounce (QA #5d): the size the current zoom LOD asks for and when it
		// first differed from the size in effect. Committed only after it holds for a moment AND the zoom
		// itself stopped changing, so the full preview re-render never lands mid-gesture.
		private int m_zoomPreviewCandidateSize = 0;
		private double m_zoomPreviewCandidateTime = 0;
		private float m_zoomPreviewLastZoom = -1;

		[SerializeField]
		private float m_lastWindowWidth = 0;

		[SerializeField]
		private int m_graphCount = 0;

		[SerializeField]
		private double m_currentInactiveTime = 0;

		private bool m_ctrlSCallback = false;
		private ToolButtonType? m_pendingToolButton = null;

		private bool m_altBoxSelection = false;
		private bool m_altDragStarted = false;
		private bool m_altPressDown = false;
		private bool m_altAvailable = true;

		// Events
		private Vector3 m_currentMousePos;
		private Vector2 m_keyEvtMousePos2D;
		private Vector2 m_currentMousePos2D;
		private Event m_currentEvent;
		private string m_currentCommandName = string.Empty;
		private bool m_insideEditorWindow;
		private bool m_controlKeyPressed = false;

		private bool m_lostFocus = false;
		// Selection box for multiple node selection
		private bool m_multipleSelectionActive = false;
		private bool m_lmbPressed = false;
		private Vector2 m_multipleSelectionStart;
		private Rect m_multipleSelectionArea = new Rect( 0, 0, 0, 0 );
		private bool m_autoPanDirActive = false;
		private bool m_forceAutoPanDir = false;
		private bool m_loadShaderOnSelection = false;
		private bool m_refreshAvailableNodes = false;
		private double m_time;

		//Context Menu
		private Vector2 m_rmbStartPos;
		private Vector2 m_altKeyStartPos;
		private GraphContextMenu m_contextMenu;
		private ShortcutsManager m_shortcutManager;

		// @diogo: a canvas "Change Type" pick ( issue #635 ), applied on the next frame from a safe event
		//         context because GenericMenu item callbacks fire outside the normal GUI event flow. The id set
		//         is snapshotted at menu-build time - one entry for a single node, N for a multi-selection.
		private int[] m_pendingTypeChangeNodeIds = null;
		private System.Type m_pendingTypeChangeType = null;

		[SerializeField]
		private NodeAvailability m_currentNodeAvailability = NodeAvailability.SurfaceShader;
		//Clipboard
		private Clipboard m_clipboard;

		//Node Parameters Window
		private NodeParametersWindow m_nodeParametersWindow;

		// Tools Window
		private ToolsWindow m_toolsWindow;

		private ConsoleLogWindow m_consoleLogWindow;

		//Editor Options
		private OptionsWindow m_optionsWindow;

		// Mode Window
		private ShaderEditorModeWindow m_modeWindow;

		// Tools Window
		private TipsWindow m_tipsWindow;

		//Palette Window
		private PaletteWindow m_paletteWindow;

		private ContextPalette m_contextPalette;
		private PalettePopUp m_palettePopup;
		private System.Type m_paletteChosenType;
		private AmplifyShaderFunction m_paletteChosenFunction;

		// In-Editor Message System
		GenericMessageUI m_genericMessageUI;
		private GUIContent m_genericMessageContent;

		// Drag&Drop Tool
		private DragAndDropTool m_dragAndDropTool;

		//Custom Styles
		//private CustomStylesContainer m_customStyles;

		private AmplifyShaderFunction m_previousShaderFunction;

		private List<MenuParent> m_registeredMenus;

		private PreMadeShaders m_preMadeShaders;

		private AutoPanData[] m_autoPanArea;

		private DrawInfo m_drawInfo;
		private KeyCode m_lastKeyPressed = KeyCode.None;
		private System.Type m_commentaryTypeNode;

		private int m_onLoadDone = 0;

		private float m_copyPasteDeltaMul = 0;
		private Vector2 m_copyPasteInitialPos = Vector2.zero;
		private Vector2 m_copyPasteDeltaPos = Vector2.zero;

		private int m_repaintCount = 0;
		private bool m_forceUpdateFromMaterialFlag = false;

		private UnityEngine.Object m_delayedLoadObject = null;
		private double m_focusOnSelectionTimestamp;
		private double m_focusOnMasterNodeTimestamp;
		private double m_wiredDoubleTapTimestamp;

		private bool m_globalPreview = false;
		private bool m_globalShowInternalData = true;

		private const double AutoZoomTime = 0.25;
		private const double ToggleTime = 0.25;
		private const double WiredDoubleTapTime = 0.25;
		private const double DoubleClickTime = 0.25;

		private Material m_delayedMaterialSet = null;

		private bool m_mouseDownOnValidArea = false;

		private bool m_removedKeyboardFocus = false;

		private int m_lastHotControl = -1;

		[SerializeField]
		private bool m_isShaderFunctionWindow = false;

		private string m_currentTitle = string.Empty;
		private bool m_currentTitleMod = false;

		//private Material m_maskingMaterial = null;
		//private int m_cachedProjectInLinearId = -1;
		private int m_cachedEditorTimeId = -1;
		private int m_cachedEditorDeltaTimeId = -1;
		// @diogo: EMA-smoothed preview delta time, stands in for Time.smoothDeltaTime in the Time node's smoothDt preview.
		private double m_previewSmoothDeltaTime = 0;
		//private float m_repaintFrequency = 15;
		//private double m_repaintTimestamp = 0;

		// Smooth Zoom
		private bool m_smoothZoom = false;
		private float m_targetZoom;
		private double m_zoomTime;
		private Vector2 m_zoomPivot;
		private float m_targetZoomIncrement;
		private float m_zoomVelocity = 0;

		// Smooth Pan
		private bool m_smoothOffset = false;
		private double m_offsetTime;
		private Vector2 m_targetOffset;
		private Vector2 m_camVelocity = Vector2.zero;

		// Auto-Compile samples
		private bool m_forcingMaterialUpdateFlag = false;
		private bool m_forcingMaterialUpdateOp = false;
		private List<Material> m_materialsToUpdate = new List<Material>();

		private NodeExporterUtils m_nodeExporterUtils;

		// Snapshot-based undo ( behind Preferences.User.EnableUndo ). See GraphUndoProxy.
		// @diogo: serialized so the reference survives domain reload; Unity's undo records point at this
		// instance, and the post-reload LoadFromDisk must clear THEM in FullCleanUndoStack — losing the
		// reference orphans those records and leaves dead ASE entries on the undo stack
		[SerializeField]
		private GraphUndoProxy m_undoProxy;
		[NonSerialized]
		private bool m_reloadFromSnapshot = false;
		// True once a continuous gesture ( slider drag, node move, typing ) has taken its single
		// pre-edit snapshot. Reset on the next mouse press ( and on undo restore ) so each gesture is
		// one undo step; a release must not reset it, its own event still carries the gesture's final
		// value change.
		[NonSerialized]
		private bool m_gestureUndoActive = false;
		// @diogo: >0 while a non-user cascade runs ( post-load/deserialize template refresh, undo restore );
		// RegisterGraphUndo is a no-op then, so mechanical DeleteConnection calls don't pollute the undo stack
		[NonSerialized]
		private int m_suppressUndoRegistration = 0;
		// @diogo: latest serialize of the live graph, handed over by the proxy whenever Unity snapshots
		// it - including the redo-stack capture inside every undo/redo, which runs right before the
		// restore. The patcher reuses it as its current-state capture; a stale value can only cost a
		// verify failure ( the oracle re-serializes reality ), answered by one honest-serialize retry.
		[NonSerialized]
		private string m_lastLiveGraphSerialize = null;
		// @diogo: per-phase timing from the last ReloadGraphFromSnapshot call, read by RestoreFromUndoSnapshot
		[NonSerialized]
		private double m_lastReloadLoadMs = 0.0;
		[NonSerialized]
		private double m_lastReloadRefreshMs = 0.0;
		[NonSerialized]
		private double m_lastReloadViewStateMs = 0.0;
		[NonSerialized]
		private int m_lastReloadNodeCount = 0;

		//[SerializeField]
		//private AmplifyShaderFunction m_openedShaderFunction;

		[SerializeField]
		private bool m_openedAssetFromNode = false;

		private bool m_nodesLoadedCorrectly = false;
		private GUIContent NodesExceptionMessage = new GUIContent( "ASE is unable to load correctly due to some faulty other classes/plugin in your project. We advise to review all your imported plugins." );


		private bool m_outdatedShaderFromTemplateLoaded = false;
		private bool m_replaceMasterNode = false;
		private AvailableShaderTypes m_replaceMasterNodeType;
		private string m_replaceMasterNodeData;
		private bool m_replaceMasterNodeDataFromCache;
		// Section foldouts ( Common Properties / SubShader / Pass / LOD ) captured from the master
		// node being replaced, reapplied to the new one so a template switch keeps them as-is.
		private string[] m_replaceMasterNodeViewState = null;
		private NodeWireReferencesUtils m_wireReferenceUtils = new NodeWireReferencesUtils();

		private ParentNode m_nodeToFocus = null;
		private float m_zoomToFocus = 1.0f;
		private bool m_selectNodeToFocus = true;

		[NonSerialized]
		public Dictionary<string, bool> VisitedChanged = new Dictionary<string, bool>();

		[SerializeField]
		private List<Toast> m_messages = new List<Toast>();

		[SerializeField]
		private float m_maxMsgWidth = 100;

		[SerializeField]
		private float m_msgBoxHeight = 88;

		[SerializeField]
		private bool m_msgBoxResized = false;

		[SerializeField]
		private bool m_maximizeMessages = false;

		[NonSerialized]
		private Dictionary<(string,string), OutputPort> m_savedList = new Dictionary<(string,string), OutputPort>();

		public int m_frameCounter = 0;
		public double m_fpsTime = 0;
		public string m_fpsDisplay = string.Empty;

		// @diogo: canvas stats overlay ( Preferences.User.ShowStats ), EMA-smoothed timings in ms (QA #5).
		private readonly System.Diagnostics.Stopwatch m_statsStopwatch = new System.Diagnostics.Stopwatch();
		private double m_statsFrameMs = 0;
		private double m_statsPreviewPassMs = 0;
		private double m_statsVramMB = 0;
		private double m_statsVramTimer = 999;
		private GUIStyle m_statsStyle = null;

#if UNITY_EDITOR_WIN
		// ScreenShot vars
		IntPtr m_aseHandle;
		private Rect m_prevWindowRect;
		private Vector2 m_prevCameraOffset;
		private float m_prevCameraZoom;
		private bool m_openSavedFolder = false;
		private bool m_takeScreenShot = false;
#endif
		public bool CheckFunctions = false;

		// Unity Menu item
		[MenuItem( "Window/Amplify Shader Editor/Open Canvas", false, 1000 )]
		static void OpenMainShaderGraph()
		{
			if( IOUtils.AllOpenedWindows.Count > 0 )
			{
				AmplifyShaderEditorWindow currentWindow = CreateTab( "Empty", UIUtils.ShaderIcon );
				UIUtils.CurrentWindow = currentWindow;
				currentWindow.CreateNewGraph( "Empty" );
				currentWindow.Show();
			}
			else
			{
				AmplifyShaderEditorWindow currentWindow = OpenWindow( "Empty", UIUtils.ShaderIcon );
				currentWindow.CreateNewGraph( "Empty" );
				//currentWindow.Show();
			}
		}

		public static string GenerateTabTitle( string original, bool modified = false )
		{
			GUIContent content = new GUIContent( original );
			GUIStyle tabStyle = new GUIStyle( (GUIStyle)"dragtabdropwindow" );//  GUI.skin.FindStyle( "dragtabdropwindow" );
			string finalTitle = string.Empty;
			bool addEllipsis = false;
			for( int i = 1; i <= original.Length; i++ )
			{
				content.text = original.Substring( 0, i );
				Vector2 titleSize = tabStyle.CalcSize( content );
				int maxSize = modified ? 62 : 69;
				if( titleSize.x > maxSize )
				{
					addEllipsis = true;
					break;
				}
				else
				{
					finalTitle = content.text;
				}
			}
			if( addEllipsis )
				finalTitle += "..";
			if( modified )
				finalTitle += "*";
			return finalTitle;
		}

		public static void ConvertShaderToASE( Shader shader, bool OpenOnSeparateWindow = false )
		{
			if( UIUtils.IsUnityNativeShader( shader ) )
			{
				Debug.LogWarningFormat( "Action not allowed. Attempting to load the native {0} shader into Amplify Shader Editor", shader.name );
				return;
			}
			if ( IOUtils.IsASEShader( shader ) )
			{
				if ( OpenOnSeparateWindow )
				{
					AmplifyShaderEditorWindow currentWindow = CreateTab( shader.name , UIUtils.ShaderIcon );
					UIUtils.CurrentWindow = currentWindow;
					currentWindow.Show();
				}
				else
				{
					string guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( shader ) );
					if( IOUtils.AllOpenedWindows.Count > 0 )
					{
						AmplifyShaderEditorWindow openedTab = null;
						for( int i = 0 ; i < IOUtils.AllOpenedWindows.Count ; i++ )
						{
							//if( AssetDatabase.GetAssetPath( shader ).Equals( IOUtils.AllOpenedWindows[ i ].LastOpenedLocation ) )
							if( guid.Equals( IOUtils.AllOpenedWindows[ i ].GUID ) )
							{
								openedTab = IOUtils.AllOpenedWindows[ i ];
								break;
							}
						}

						if( openedTab != null )
						{
							openedTab.wantsMouseMove = true;
							openedTab.ShowTab();
							UIUtils.CurrentWindow = openedTab;
						}
						else
						{
							EditorWindow openedWindow = AmplifyShaderEditorWindow.GetWindow<AmplifyShaderEditorWindow>();
							AmplifyShaderEditorWindow currentWindow = CreateTab();
							WindowHelper.AddTab( openedWindow , currentWindow );
							UIUtils.CurrentWindow = currentWindow;
						}
					}
					else
					{
						AmplifyShaderEditorWindow currentWindow = OpenWindow( shader.name , UIUtils.ShaderIcon );
						UIUtils.CurrentWindow = currentWindow;
					}
				}

				UIUtils.CurrentWindow.LoadProjectSelected( shader );
			}
			else
			{
				Debug.LogWarning( "[AmplifyShaderEditor] Can't open shader " + shader.name + " because it was not created in ASE." );

				//UIUtils.CreateEmptyFromInvalid( shader );
				//UIUtils.ShowMessage( "Trying to open shader not created on ASE! BEWARE, old data will be lost if saving it here!", MessageSeverity.Warning );
				//if( UIUtils.CurrentWindow.LiveShaderEditing )
				//{
				//	UIUtils.ShowMessage( "Disabling Live Shader Editing. Must manually re-enable it.", MessageSeverity.Warning );
				//	UIUtils.CurrentWindow.LiveShaderEditing = false;
				//}
			}
		}

		public static void LoadMaterialToASE( Material material )
		{
			string guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( material.shader ) );

			if( IOUtils.AllOpenedWindows.Count > 0 )
			{
				AmplifyShaderEditorWindow openedTab = null;
				for( int i = 0; i < IOUtils.AllOpenedWindows.Count; i++ )
				{
					//if( AssetDatabase.GetAssetPath( material.shader ).Equals( IOUtils.AllOpenedWindows[ i ].LastOpenedLocation ) )
					if( guid.Equals( IOUtils.AllOpenedWindows[ i ].GUID ) )
					{
						openedTab = IOUtils.AllOpenedWindows[ i ];
						break;
					}
				}

				if( openedTab != null )
				{
					openedTab.wantsMouseMove = true;
					openedTab.ShowTab();
					UIUtils.CurrentWindow = openedTab;
				}
				else
				{
					EditorWindow openedWindow = AmplifyShaderEditorWindow.GetWindow<AmplifyShaderEditorWindow>();
					AmplifyShaderEditorWindow currentWindow = CreateTab();
					WindowHelper.AddTab( openedWindow, currentWindow );
					UIUtils.CurrentWindow = currentWindow;
				}
			}
			else
			{
				AmplifyShaderEditorWindow currentWindow = OpenWindow( material.name, UIUtils.MaterialIcon );
				UIUtils.CurrentWindow = currentWindow;
			}

			if( IOUtils.IsASEShader( material.shader ) )
			{
				UIUtils.CurrentWindow.LoadProjectSelected( material );
			}
			else
			{
				UIUtils.CreateEmptyFromInvalid( material.shader );
				UIUtils.SetDelayedMaterialMode( material );
			}
		}

		public static void LoadShaderFunctionToASE( AmplifyShaderFunction shaderFunction, bool openedAssetFromNode , bool OpenOnSeparateWindow = false )
		{
			string guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( shaderFunction ) );
			if( OpenOnSeparateWindow )
			{
				AmplifyShaderEditorWindow currentWindow = CreateTab( shaderFunction.FunctionName , UIUtils.ShaderFunctionIcon );
				UIUtils.CurrentWindow = currentWindow;
				currentWindow.Show();
			}
			else
			{
				if( IOUtils.AllOpenedWindows.Count > 0 )
				{
					AmplifyShaderEditorWindow openedTab = null;
					for( int i = 0 ; i < IOUtils.AllOpenedWindows.Count ; i++ )
					{
						//if( AssetDatabase.GetAssetPath( shaderFunction ).Equals( IOUtils.AllOpenedWindows[ i ].LastOpenedLocation ) )
						if( guid.Equals( IOUtils.AllOpenedWindows[ i ].GUID ) )
						{
							openedTab = IOUtils.AllOpenedWindows[ i ];
							break;
						}
					}

					if( openedTab != null )
					{
						openedTab.wantsMouseMove = true;
						openedTab.ShowTab();
						UIUtils.CurrentWindow = openedTab;
					}
					else
					{
						EditorWindow openedWindow = AmplifyShaderEditorWindow.GetWindow<AmplifyShaderEditorWindow>();
						AmplifyShaderEditorWindow currentWindow = CreateTab();
						WindowHelper.AddTab( openedWindow , currentWindow );
						UIUtils.CurrentWindow = currentWindow;
					}
				}
				else
				{
					AmplifyShaderEditorWindow currentWindow = OpenWindow( shaderFunction.FunctionName , UIUtils.ShaderFunctionIcon );
					UIUtils.CurrentWindow = currentWindow;
				}
			}

			UIUtils.CurrentWindow.OpenedAssetFromNode = openedAssetFromNode;
			if( IOUtils.IsShaderFunction( shaderFunction.FunctionInfo ) )
			{
				UIUtils.CurrentWindow.LoadProjectSelected( shaderFunction );
			}
			else
			{
				UIUtils.CurrentWindow.titleContent.text = GenerateTabTitle( shaderFunction.FunctionName );
				UIUtils.CurrentWindow.titleContent.image = UIUtils.ShaderFunctionIcon;
				UIUtils.CreateEmptyFunction( shaderFunction );
			}
		}

		public static AmplifyShaderEditorWindow OpenWindow( string title = null, Texture icon = null )
		{
			AmplifyShaderEditorWindow currentWindow = (AmplifyShaderEditorWindow)AmplifyShaderEditorWindow.GetWindow( typeof( AmplifyShaderEditorWindow ), false );
			currentWindow.minSize = new Vector2( ( Constants.MINIMIZE_WINDOW_LOCK_SIZE - 150 ), 270 );
			currentWindow.wantsMouseMove = true;
			if( title != null )
				currentWindow.titleContent.text = GenerateTabTitle( title );
			if( icon != null )
				currentWindow.titleContent.image = icon;
			return currentWindow;
		}

		public static AmplifyShaderEditorWindow CreateTab( string title = null, Texture icon = null )
		{
			AmplifyShaderEditorWindow currentWindow = EditorWindow.CreateInstance<AmplifyShaderEditorWindow>();
			currentWindow.minSize = new Vector2( ( Constants.MINIMIZE_WINDOW_LOCK_SIZE - 150 ), 270 );
			currentWindow.wantsMouseMove = true;
			if( title != null )
				currentWindow.titleContent.text = GenerateTabTitle( title );
			if( icon != null )
				currentWindow.titleContent.image = icon;
			return currentWindow;
		}

		public double CalculateInactivityTime()
		{
			double currTime = EditorApplication.timeSinceStartup;
			switch( Event.current.type )
			{
				case EventType.MouseDown:
				case EventType.MouseUp:
				//case EventType.MouseMove:
				case EventType.MouseDrag:
				case EventType.KeyDown:
				case EventType.KeyUp:
				case EventType.ScrollWheel:
				case EventType.DragUpdated:
				case EventType.DragPerform:
				case EventType.DragExited:
				case EventType.ValidateCommand:
				case EventType.ExecuteCommand:
				{
					m_inactivityTime = currTime;
					return 0;
				}
			}
			return currTime - m_inactivityTime;
		}

		public void ActivatePreviews( bool value )
		{
			m_mainGraphInstance.ActivatePreviews( value );
		}

		// Shader Graph window
		public override void OnEnable()
		{
			base.OnEnable();

			UndoUtils.RegisterUndoRedoCallback( UndoRedoPerformed );
			m_nodeExporterUtils = new NodeExporterUtils( this );

			ASEPackageManagerHelper.RequestInfo();
			ASEPackageManagerHelper.Update();

			Shader.SetGlobalVector( PreviewSizeGlobalVariable, new Vector4( UIUtils.CurrentPreviewSize , UIUtils.CurrentPreviewSize , 0, 0 ) );

			if( m_innerEditorVariables == null )
			{
				m_innerEditorVariables = new InnerWindowEditorVariables();
				m_innerEditorVariables.Initialize();
			}

			if( m_mainGraphInstance == null )
			{
				m_mainGraphInstance = CreateInstance<ParentGraph>();
				m_mainGraphInstance.Init();
				m_mainGraphInstance.ParentWindow = this;
				m_mainGraphInstance.SetGraphId( 0 );
			}

			if ( m_undoProxy == null )
			{
				m_undoProxy = CreateInstance<GraphUndoProxy>();
				m_undoProxy.hideFlags = HideFlags.HideAndDontSave;
			}
			m_undoProxy.Init( this );
			// @diogo: the domain-reload deserialize sets PendingRestore; consume it so the next foreign
			// undo pop cannot trigger a stale restore
			m_undoProxy.PendingRestore = false;

			m_mainGraphInstance.ResetEvents();
			m_mainGraphInstance.OnNodeEvent += OnNodeStoppedMovingEvent;
			m_mainGraphInstance.OnMaterialUpdatedEvent += OnMaterialUpdated;
			m_mainGraphInstance.OnShaderUpdatedEvent += OnShaderUpdated;
			m_mainGraphInstance.OnEmptyGraphDetectedEvt += OnEmptyGraphDetected;
			m_mainGraphInstance.OnNodeRemovedEvent += m_toolsWindow.OnNodeRemovedFromGraph;
			m_mainGraphInstance.OnNodeEvent += OnGraphNodeSetChanged;
			m_mainGraphInstance.OnNodeRemovedEvent += OnGraphNodeSetChanged;
			GraphCount = 1;

			IOUtils.Init();
			IOUtils.AllOpenedWindows.Add( this );

			// Only runs once for multiple windows
			EditorApplication.update -= IOUtils.UpdateIO;
			EditorApplication.update += IOUtils.UpdateIO;

			//EditorApplication.update -= UpdateTime;
			EditorApplication.update -= UpdateNodePreviewListAndTime;
			//EditorApplication.update += UpdateTime;

			EditorApplication.update += UpdateNodePreviewListAndTime;


			if( CurrentSelection == ASESelectionMode.ShaderFunction )
			{
				IsShaderFunctionWindow = true;
			}
			else
			{
				IsShaderFunctionWindow = false;
			}

			m_optionsWindow = new OptionsWindow( this );
			m_optionsWindow.Init();

			m_contextMenu = new GraphContextMenu( m_mainGraphInstance );
			m_nodesLoadedCorrectly = m_contextMenu.CorrectlyLoaded;

			m_paletteWindow = new PaletteWindow( this )
			{
				Resizable = true
			};
			m_paletteWindow.OnPaletteNodeCreateEvt += OnPaletteNodeCreate;
			m_registeredMenus.Add( m_paletteWindow );

			// @diogo: side panels bound each other on resize instead of being capped at half the window
			m_paletteWindow.OppositeMenu = m_nodeParametersWindow;
			m_nodeParametersWindow.OppositeMenu = m_paletteWindow;

			m_contextPalette = new ContextPalette( this );
			m_contextPalette.OnPaletteNodeCreateEvt += OnContextPaletteNodeCreate;
			m_registeredMenus.Add( m_contextPalette );

			m_genericMessageUI = new GenericMessageUI();
			m_genericMessageUI.OnMessageDisplayEvent += ShowMessageImmediately;

			Selection.selectionChanged += OnProjectSelectionChanged;
			EditorApplication.projectChanged += OnProjectWindowChanged;

			m_focusOnSelectionTimestamp = EditorApplication.timeSinceStartup;
			m_focusOnMasterNodeTimestamp = EditorApplication.timeSinceStartup;

			// @diogo: panel open/close is machine-global (EditorPrefs); no per-graph override, so it survives a full restart
			m_nodeParametersWindow.IsMaximized = m_innerEditorVariables.NodeParametersMaximized;
			m_paletteWindow.IsMaximized = m_innerEditorVariables.NodePaletteMaximized;

			// @diogo: a hand sized message box is machine-global too; the draw clamps it to the window,
			// so a size from a larger window comes back whole once there is room for it again
			if ( EditorVariablesManager.MsgBoxResized.Value )
			{
				m_msgBoxResized = true;
				m_maxMsgWidth = EditorVariablesManager.MsgBoxWidth.Value;
				m_msgBoxHeight = EditorVariablesManager.MsgBoxHeight.Value;
			}

			m_shortcutManager = new ShortcutsManager();
			// REGISTER NODE SHORTCUTS
			foreach( KeyValuePair<KeyCode, ShortcutKeyData> kvp in m_contextMenu.NodeShortcuts )
			{
				m_shortcutManager.RegisterNodesShortcuts( kvp.Key, kvp.Value.Name );
			}

			// REGISTER EDITOR SHORTCUTS

			m_shortcutManager.RegisterEditorShortcut( true, EventModifiers.FunctionKey, KeyCode.F1, "Open Selected Node Wiki page", () =>
			{
				List<ParentNode> selectedNodes = m_mainGraphInstance.SelectedNodes;
				if( selectedNodes != null && selectedNodes.Count == 1 )
				{
					FunctionNode shaderFunctionNode = selectedNodes[ 0 ] as FunctionNode;
					if( shaderFunctionNode != null )
					{
						string url = ( string.IsNullOrEmpty( shaderFunctionNode.Function.URL ) ) ?
										Constants.NodeCommonUrl + UIUtils.UrlReplaceInvalidStrings( shaderFunctionNode.Function.FunctionName ) :
										shaderFunctionNode.Function.URL;
						Application.OpenURL( url );
					}
					else
					{
						if( selectedNodes[ 0 ].Attributes != null )
						{
							Application.OpenURL( selectedNodes[ 0 ].Attributes.NodeUrl );
						}
						else
						{
							UIUtils.ShowMessage( "Selected node doesn't have valid attibutes to get URL from." );
						}

					}
				}
			} );


			m_shortcutManager.RegisterEditorShortcut( true, KeyCode.C, "Create Commentary", () =>
			{
				// Create commentary
				ParentNode[] selectedNodes = m_mainGraphInstance.SelectedNodes.ToArray();
				RegisterGraphUndo( "Adding Commentary Node" );
				CommentaryNode node = m_mainGraphInstance.CreateNode( m_commentaryTypeNode, true, -1, false ) as CommentaryNode;
				node.CreateFromSelectedNodes( TranformedMousePos, selectedNodes );
				node.Focus();
				m_mainGraphInstance.DeSelectAll();
				m_mainGraphInstance.SelectNode( node, false, false );
				SetSaveIsDirty();
				ForceRepaint();
			} );


			m_shortcutManager.RegisterEditorShortcut( true, KeyCode.F, "Focus On Selection", () =>
			{
				OnToolButtonPressed( ToolButtonType.FocusOnSelection );
				ForceRepaint();
			} );

			//m_shortcutManager.RegisterEditorShortcut( true, EventModifiers.None, KeyCode.B, "New Master Node", () =>
			//{
			//	OnToolButtonPressed( ToolButtonType.MasterNode );
			//	ForceRepaint();
			//} );

			m_shortcutManager.RegisterEditorShortcut( true, EventModifiers.None, KeyCode.Space, "Open Node Palette", null, () =>
			{
				m_contextPalette.Show( m_currentMousePos2D, m_cameraInfo );
			} );


			m_shortcutManager.RegisterEditorShortcut( true, KeyCode.W, "Toggle Colored Line Mode", () =>
			{
				m_optionsWindow.ColoredPorts = !m_optionsWindow.ColoredPorts;
				ForceRepaint();

			} );

			m_shortcutManager.RegisterEditorShortcut( true, EventModifiers.Control, KeyCode.W, "Toggle Multi-Line Mode", () =>
			{
				m_optionsWindow.MultiLinePorts = !m_optionsWindow.MultiLinePorts;
				ForceRepaint();
			} );

			m_shortcutManager.RegisterEditorShortcut( true, KeyCode.P, "Global Preview", () =>
			{
				GlobalPreview = !GlobalPreview;
				EditorPrefs.SetBool( "GlobalPreview", GlobalPreview );

				ForceRepaint();
			} );

			GlobalShowInternalData = EditorPrefs.GetBool( "ASEGlobalShowInternalData", true );
			m_shortcutManager.RegisterEditorShortcut( true, KeyCode.I, "Global Show Internal Data", () =>
			{
				GlobalShowInternalData = !GlobalShowInternalData;
				EditorPrefs.SetBool( "ASEGlobalShowInternalData", GlobalShowInternalData );
				ForceRepaint();
			} );

			m_shortcutManager.RegisterEditorShortcut( true, EventModifiers.FunctionKey, KeyCode.Delete, "Delete selected nodes", DeleteSelectedNodeWithRepaint );
			m_shortcutManager.RegisterEditorShortcut( true, EventModifiers.FunctionKey, KeyCode.Backspace, "Delete selected nodes", DeleteSelectedNodeWithRepaint );

			m_liveShaderEditing = m_innerEditorVariables.LiveMode;

			UpdateLiveUI();
		}

		public AmplifyShaderEditorWindow()
		{
			m_minNodePos = new Vector2( float.MaxValue, float.MaxValue );
			m_maxNodePos = new Vector2( float.MinValue, float.MinValue );

			m_duplicatePreventionBuffer = new DuplicatePreventionBuffer();
			m_commentaryTypeNode = typeof( CommentaryNode );
			titleContent = new GUIContent( "Shader Editor" );
			autoRepaintOnSceneChange = true;

			m_currentMousePos = new Vector3( 0, 0, 0 );
			m_keyEvtMousePos2D = new Vector2( 0, 0 );
			m_multipleSelectionStart = new Vector2( 0, 0 );
			m_initialized = false;
			m_graphBgTexture = null;
			m_graphFgTexture = null;

			m_cameraOffset = new Vector2( 0, 0 );
			CameraZoom = 1;

			m_registeredMenus = new List<MenuParent>();

			m_nodeParametersWindow = new NodeParametersWindow( this )
			{
				Resizable = true
			};
			m_registeredMenus.Add( m_nodeParametersWindow );

			m_modeWindow = new ShaderEditorModeWindow( this );
			//_registeredMenus.Add( _modeWindow );

			m_toolsWindow = new ToolsWindow( this );
			m_toolsWindow.ToolButtonPressedEvt += OnToolsWindowButtonPressed;

			m_consoleLogWindow = new ConsoleLogWindow( this );

			m_tipsWindow = new TipsWindow( this );

			m_registeredMenus.Add( m_toolsWindow );
			//m_registeredMenus.Add( m_consoleLogWindow );

			m_palettePopup = new PalettePopUp();

			m_clipboard = new Clipboard();

			m_genericMessageContent = new GUIContent();
			m_dragAndDropTool = new DragAndDropTool();
			m_dragAndDropTool.OnValidDropObjectEvt += OnValidObjectsDropped;

			//_confirmationWindow = new ConfirmationWindow( 100, 100, 300, 100 );

			m_saveIsDirty = false;

			m_preMadeShaders = new PreMadeShaders();

			float autoPanSpeed = 2;
			m_autoPanArea = new AutoPanData[ 4 ];
			m_autoPanArea[ 0 ] = new AutoPanData( AutoPanLocation.TOP, 25, autoPanSpeed * Vector2.up );
			m_autoPanArea[ 1 ] = new AutoPanData( AutoPanLocation.BOTTOM, 25, autoPanSpeed * Vector2.down );
			m_autoPanArea[ 2 ] = new AutoPanData( AutoPanLocation.LEFT, 25, autoPanSpeed * Vector2.right );
			m_autoPanArea[ 3 ] = new AutoPanData( AutoPanLocation.RIGHT, 25, autoPanSpeed * Vector2.left );

			m_drawInfo = new DrawInfo();
			UIUtils.CurrentWindow = this;

			m_repaintIsDirty = false;
			m_initialized = false;
		}

		public void SetStandardShader()
		{
			m_mainGraphInstance.ReplaceMasterNode( AvailableShaderTypes.SurfaceShader );
			m_mainGraphInstance.FireMasterNodeReplacedEvent();
		}

		public void SetTemplateShader( string templateName, bool writeDefaultData )
		{
			TemplateDataParent templateData = TemplatesManager.Instance.GetTemplate( ( string.IsNullOrEmpty( templateName ) ? "6e114a916ca3e4b4bb51972669d463bf" : templateName ) );
			m_mainGraphInstance.ReplaceMasterNode( AvailableShaderTypes.Template, writeDefaultData, templateData );
		}

		public void DeleteSelectedNodeWithRepaint()
		{
			DeleteSelectedNodes();
			SetSaveIsDirty();
		}


		void UndoRedoPerformed()
		{
			m_repaintIsDirty = true;
			m_saveIsDirty = true;
			m_removedKeyboardFocus = true;

			// Snapshot-based undo: rebuild the live graph on the next OnGUI ( cannot do it here,
			// mid-callback ) — but only if Unity actually restored *this* window's proxy, signalled by
			// PendingRestore ( set in the proxy's OnAfterDeserialize ). undoRedoPerformed is a global
			// callback, so without this gate every other open ASE window, and any unrelated Unity undo,
			// would clobber its graph by reloading a stale snapshot.
			if ( Preferences.User.EnableUndo && m_undoProxy != null && m_undoProxy.PendingRestore )
			{
				m_undoProxy.PendingRestore = false;
				m_reloadFromSnapshot = true;
			}

			// @diogo: diagnostic: one line per Ctrl+Z/Y press; "proxy restored: no" means the popped entry was a foreign object (e.g. the material)
			if ( Preferences.User.UndoProfiling )
			{
				Debug.Log( string.Format( "[ASE Undo] undo/redo performed ( proxy restored: {0}, current group: {1} \"{2}\", proxy {3}, deserialized x{4} )", m_reloadFromSnapshot ? "yes" : "no",
					Undo.GetCurrentGroup(), Undo.GetCurrentGroupName(), m_undoProxy != null ? AssetUtils.GetEntityId( m_undoProxy ).ToString() : "0", m_undoProxy != null ? m_undoProxy.DeserializeCount : 0 ) );
			}
		}

		void Destroy()
		{
			// @diogo: undoRedoPerformed is a static event; without this a closed window stays subscribed until domain reload
			UndoUtils.UnregisterUndoRedoCallback( UndoRedoPerformed );

			UndoUtils.ClearUndo( this );

			if ( m_undoProxy != null )
			{
				UndoUtils.ClearUndo( m_undoProxy );
				DestroyImmediate( m_undoProxy );
				m_undoProxy = null;
			}

			m_initialized = false;

			m_nodeExporterUtils.Destroy();
			m_nodeExporterUtils = null;

			m_delayedMaterialSet = null;

			m_materialsToUpdate.Clear();
			m_materialsToUpdate = null;

			GLDraw.Destroy();

			UIUtils.Destroy();
			m_preMadeShaders.Destroy();
			m_preMadeShaders = null;

			m_registeredMenus.Clear();
			m_registeredMenus = null;

			UIUtils.CurrentWindow = this;
			m_mainGraphInstance.Destroy();
			ScriptableObject.DestroyImmediate( m_mainGraphInstance );
			m_mainGraphInstance = null;

			Resources.UnloadAsset( m_graphBgTexture );
			m_graphBgTexture = null;

			Resources.UnloadAsset( m_graphFgTexture );
			m_graphFgTexture = null;

			Resources.UnloadAsset( m_wireTexture );
			m_wireTexture = null;

			m_contextMenu.Destroy();
			m_contextMenu = null;

			m_shortcutManager.Destroy();
			m_shortcutManager = null;

			m_nodeParametersWindow.Destroy();
			m_nodeParametersWindow = null;


			m_modeWindow.Destroy();
			m_modeWindow = null;

			m_toolsWindow.Destroy();
			m_toolsWindow = null;

			m_consoleLogWindow.Destroy();
			m_consoleLogWindow = null;

			m_tipsWindow.Destroy();
			m_tipsWindow = null;

			m_optionsWindow.Destroy();
			m_optionsWindow = null;

			m_paletteWindow.Destroy();
			m_paletteWindow = null;

			m_palettePopup.Destroy();
			m_palettePopup = null;

			m_contextPalette.Destroy();
			m_contextPalette = null;

			m_clipboard.ClearClipboard();
			m_clipboard = null;

			m_genericMessageUI.Destroy();
			m_genericMessageUI = null;
			m_genericMessageContent = null;

			m_dragAndDropTool.Destroy();
			m_dragAndDropTool = null;

			//m_openedShaderFunction = null;

			UIUtils.CurrentWindow = null;
			m_duplicatePreventionBuffer.ReleaseAllData();
			m_duplicatePreventionBuffer = null;
			EditorApplication.projectChanged -= OnProjectWindowChanged;
			Selection.selectionChanged -= OnProjectSelectionChanged;

			IOUtils.AllOpenedWindows.Remove( this );

			IOUtils.Destroy();
		}

		void Init()
		{
			// = AssetDatabase.LoadAssetAtPath( Constants.ASEPath + "", typeof( Texture2D ) ) as Texture2D;
			m_graphBgTexture = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( IOUtils.GraphBgTextureGUID ), typeof( Texture2D ) ) as Texture2D;
			if( m_graphBgTexture != null )
			{
				m_graphFgTexture = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( IOUtils.GraphFgTextureGUID ), typeof( Texture2D ) ) as Texture2D;

				//Setup usable area
				m_cameraInfo = position;
				m_graphArea = new Rect( 0, 0, m_cameraInfo.width, m_cameraInfo.height );

				// Creating style state to show current selected object
				m_graphFontStyle = new GUIStyle()
				{
					fontSize = 32,
					alignment = TextAnchor.MiddleCenter,
					fixedWidth = m_cameraInfo.width,
					fixedHeight = 50,
					stretchWidth = true,
					stretchHeight = true
				};
				m_graphFontStyle.normal.textColor = Color.white;

				m_wireTexture = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( IOUtils.WireTextureGUID ), typeof( Texture2D ) ) as Texture2D;

				m_initialized = m_graphBgTexture != null &&
								m_graphFgTexture != null &&
								m_wireTexture != null;
			}
		}

		[OnOpenAsset(0)]
	#if UNITY_6000_4_OR_NEWER
		static bool OnOpenAsset( EntityId entityId, int line )
	#else
		static bool OnOpenAsset( int entityId, int line )
	#endif
		{
			// This test is needed since it is what is used when we both click the button to open generated code inside the canvas
			// ( in there we call AssetDatabase.OpenAsset with line set to 1 to let ASE know that we want to ignore normal shader opening )
			// And click on shader errors/warnings over the shader inspector
			// ( Line is greater than -1 focusing on where the error/warning is )

			if( line > -1 )
			{
				return false;
			}

			UnityEngine.Object selection = AssetUtils.EntityIdToObject( new AssetUtils.EntityId( entityId ) );

			ASEPackageManagerHelper.RequestInfo();
			ASEPackageManagerHelper.Update();
			if( ASEPackageManagerHelper.IsProcessing )
			{
				Shader selectedShader = selection as Shader;
				if( selectedShader != null )
				{
					if( IOUtils.IsASEShader( selectedShader ) )
					{
						ASEPackageManagerHelper.SetupLateShader( selectedShader );
						return true;
					}
				}
				else
				{
					Material mat = selection as Material;
					if( mat != null )
					{
						if( !Preferences.User.DisableMaterialMode && IOUtils.IsASEShader( mat.shader ) )
						{
							ASEPackageManagerHelper.SetupLateMaterial( mat );
							return true;
						}
					}
					else
					{
						AmplifyShaderFunction shaderFunction = selection as AmplifyShaderFunction;
						if( shaderFunction != null )
						{
							if( IOUtils.IsShaderFunction( shaderFunction.FunctionInfo ) )
							{
								ASEPackageManagerHelper.SetupLateShaderFunction( shaderFunction );
								return true;
							}
						}
					}
				}
			}
			else
			{
				Shader selectedShader = selection as Shader;
				if( selectedShader != null )
				{
					if( IOUtils.IsASEShader( selectedShader ) )
					{
						ConvertShaderToASE( selectedShader );
						return true;
					}
				}
				else
				{
					Material mat = selection as Material;
					if( mat != null )
					{
						if( !Preferences.User.DisableMaterialMode && IOUtils.IsASEShader( mat.shader ) )
						{
							LoadMaterialToASE( mat );
							return true;
						}
					}
					else
					{
						AmplifyShaderFunction shaderFunction = selection as AmplifyShaderFunction;
						if( shaderFunction != null )
						{
							if( IOUtils.IsShaderFunction( shaderFunction.FunctionInfo ) )
							{
								LoadShaderFunctionToASE( shaderFunction, false );
								return true;
							}
						}
					}
				}
			}

			return false;
		}

		[MenuItem( "Assets/Create/Amplify Shader/Surface", false, 84 )]
		[MenuItem( "Assets/Create/Shader/Amplify Surface Shader" )]
		static void CreateConfirmationStandardShader()
		{
			var endNameEditAction = ScriptableObject.CreateInstance<DoCreateStandardShader>();
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists( AssetUtils.EntityId.None, endNameEditAction, "New Amplify Shader.shader", AssetPreview.GetMiniTypeThumbnail( typeof( Shader ) ), null );
		}

		static void CreateNewShader( string customPath , string customShaderName )
		{

			string path = string.Empty;
			if( string.IsNullOrEmpty( customPath ) )
			{
				if( Selection.activeObject != null )
				{
					path = ( IOUtils.dataPath + AssetDatabase.GetAssetPath( Selection.activeObject ) );
				}
				else
				{
					UnityEngine.Object[] selection = Selection.GetFiltered( typeof( UnityEngine.Object ), SelectionMode.DeepAssets );
					if( selection.Length > 0 && selection[ 0 ] != null )
					{
						path = ( IOUtils.dataPath + AssetDatabase.GetAssetPath( selection[ 0 ] ) );
					}
					else
					{
						path = Application.dataPath;
					}

				}

				if( path.IndexOf( '.' ) > -1 )
				{
					path = path.Substring( 0, path.LastIndexOf( '/' ) );
				}
				path += "/";
			}
			else
			{
				path = customPath;
			}

			if( IOUtils.AllOpenedWindows.Count > 0 )
			{
				EditorWindow openedWindow = AmplifyShaderEditorWindow.GetWindow<AmplifyShaderEditorWindow>();
				AmplifyShaderEditorWindow currentWindow = CreateTab();
				WindowHelper.AddTab( openedWindow, currentWindow );
				UIUtils.CurrentWindow = currentWindow;
				Shader shader = UIUtils.CreateNewEmpty( path, customShaderName );
				Selection.activeObject = shader;
			}
			else
			{
				AmplifyShaderEditorWindow currentWindow = OpenWindow();
				UIUtils.CurrentWindow = currentWindow;
				Shader shader = UIUtils.CreateNewEmpty( path, customShaderName );
				Selection.activeObject = shader;
			}
			//Selection.objects = new UnityEngine.Object[] { shader };
		}

		public static void CreateConfirmationTemplateShader( string templateGuid )
		{
			UIUtils.NewTemplateGUID = templateGuid;
			var endNameEditAction = ScriptableObject.CreateInstance<DoCreateTemplateShader>();
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists( AssetUtils.EntityId.None, endNameEditAction, "New Amplify Shader.shader", AssetPreview.GetMiniTypeThumbnail( typeof( Shader ) ), null );
		}

		public static Shader CreateNewTemplateShader( string templateGUID , string customPath = null, string customShaderName = null )
		{
			string path = string.Empty;
			if( string.IsNullOrEmpty( customPath ) )
			{
				path = Selection.activeObject == null ? Application.dataPath : ( IOUtils.dataPath + AssetDatabase.GetAssetPath( Selection.activeObject ) );
				if( path.IndexOf( '.' ) > -1 )
				{
					path = path.Substring( 0, path.LastIndexOf( '/' ) );
				}
				path += "/";
			}
			else
			{
				path = customPath;
			}
			Shader shader = null;
			if( IOUtils.AllOpenedWindows.Count > 0 )
			{
				EditorWindow openedWindow = AmplifyShaderEditorWindow.GetWindow<AmplifyShaderEditorWindow>();
				AmplifyShaderEditorWindow currentWindow = CreateTab();
				WindowHelper.AddTab( openedWindow, currentWindow );
				UIUtils.CurrentWindow = currentWindow;
				shader = UIUtils.CreateNewEmptyTemplate( templateGUID, path, customShaderName );
				Selection.activeObject = shader;
			}
			else
			{
				AmplifyShaderEditorWindow currentWindow = OpenWindow();
				UIUtils.CurrentWindow = currentWindow;
				shader = UIUtils.CreateNewEmptyTemplate( templateGUID, path, customShaderName );
				Selection.activeObject = shader;
			}

			//Selection.objects = new UnityEngine.Object[] { shader };
			return shader;
		}

		[MenuItem( "Assets/Create/Amplify Shader Function", false, 85 )]
		[MenuItem( "Assets/Create/Shader/Amplify Shader Function" )]
		static void CreateNewShaderFunction()
		{
			AmplifyShaderFunction asset = ScriptableObject.CreateInstance<AmplifyShaderFunction>();
			var endNameEditAction = ScriptableObject.CreateInstance<DoCreateFunction>();
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists( AssetUtils.GetEntityId( asset), endNameEditAction, "New Shader Function.asset", AssetPreview.GetMiniThumbnail( asset ), null );
		}

		public void UpdateTabTitle( string newTitle, bool modified )
		{
			string[] titleArray = newTitle.Split( '/' );
			newTitle = titleArray[ titleArray.Length - 1 ];

			if( !( m_currentTitle.Equals( newTitle ) && m_currentTitleMod == modified ) )
			{
				this.titleContent.text = GenerateTabTitle( newTitle, modified );
			}
			m_currentTitle = newTitle;
			m_currentTitleMod = modified;
		}

		public void OnProjectWindowChanged()
		{
			Shader selectedShader = Selection.activeObject as Shader;
			if( selectedShader != null )
			{
				if( m_mainGraphInstance != null && m_mainGraphInstance.CurrentMasterNode != null && selectedShader == m_mainGraphInstance.CurrentMasterNode.CurrentShader )
				{
					m_lastOpenedLocation = AssetDatabase.GetAssetPath( selectedShader );
				}
			}
		}

		public void LoadProjectSelected( UnityEngine.Object selectedObject = null )
		{
			bool hasFocus = true;
			if( EditorWindow.focusedWindow != this )
			{
				hasFocus = false;
			}

			if( hasFocus && m_mainGraphInstance != null && m_mainGraphInstance.CurrentMasterNode != null )
			{
				LoadObject( selectedObject ?? Selection.activeObject );
			}
			else
			{
				m_delayedLoadObject = selectedObject ?? Selection.activeObject;
			}

			if( !hasFocus )
				Focus();
		}

		// @diogo: the pending object is cleared before the load, so a node throwing on the post-load draw
		// fails once instead of reloading the shader on every OnGUI (#676)
		private void LoadDelayedObject()
		{
			UnityEngine.Object delayedLoadObject = m_delayedLoadObject;
			m_delayedLoadObject = null;
			LoadObject( delayedLoadObject );
		}

		public void LoadObject( UnityEngine.Object objToLoad )
		{
			Shader selectedShader = objToLoad as Shader;
			Material selectedMaterial = objToLoad as Material;
			AmplifyShaderFunction selectedFunction = objToLoad as AmplifyShaderFunction;

			if( selectedFunction != null )
			{
				IsShaderFunctionWindow = true;
				m_mainGraphInstance.CurrentCanvasMode = NodeAvailability.ShaderFunction;
			}
			else
			{
				IsShaderFunctionWindow = false;
			}

			ASESelectionMode selectedFileType = ASESelectionMode.Shader;
			if( selectedShader != null )
			{
				selectedFileType = ASESelectionMode.Shader;
			}
			else if( selectedMaterial != null )
			{
				selectedFileType = ASESelectionMode.Material;
			}
			else if( selectedFunction != null )
			{
				selectedFileType = ASESelectionMode.ShaderFunction;
			}


			switch( CurrentSelection )
			{
				case ASESelectionMode.Shader:
				{
					if( ShaderIsModified )
					{
						Shader currShader = m_mainGraphInstance.CurrentMasterNode.CurrentShader;
						bool savePrevious = UIUtils.DisplayDialog( AssetDatabase.GetAssetPath( currShader ) );
						OnSaveShader( savePrevious, currShader, null, null );
					}
				}
				break;
				case ASESelectionMode.Material:
				{
					if( ShaderIsModified )
					{
						Shader currShader = m_mainGraphInstance.CurrentMasterNode.CurrentShader;
						bool savePrevious = UIUtils.DisplayDialog( AssetDatabase.GetAssetPath( currShader ) );
						OnSaveShader( savePrevious, currShader, m_mainGraphInstance.CurrentMasterNode.CurrentMaterial, null );
					}
				}
				break;
				case ASESelectionMode.ShaderFunction:
				{
					if( ShaderIsModified )
					{
						bool savePrevious = UIUtils.DisplayDialog( AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShaderFunction ) );
						OnSaveShader( savePrevious, null, null, selectedFunction );
					}
				}
				break;
			}

			switch( selectedFileType )
			{
				case ASESelectionMode.Shader:
				{
					LoadDroppedObject( true, selectedShader, null );
				}
				break;
				case ASESelectionMode.Material:
				{
					LoadDroppedObject( true, selectedMaterial.shader, selectedMaterial );
				}
				break;
				case ASESelectionMode.ShaderFunction:
				{
					LoadDroppedObject( true, null, null, selectedFunction );
				}
				break;
			}

			//m_openedShaderFunction = m_mainGraphInstance.CurrentShaderFunction;

			//Need to force one graph draw because it wont call OnGui propertly since its focuses somewhere else
			// Focus() doesn't fix this since it only changes keyboard focus
			m_drawInfo.InvertedZoom = 1 / m_cameraZoom;

			// @diogo: m_drawInfo still carries the last OnGUI event; a stale Repaint turns this draw into a real
			// repaint pass, which throws when the load runs outside OnGUI (batch APIs). Only the logic pass is
			// needed here, so run it as a no-op event and hand the stale value back to the caller's pass
			EventType cachedEventType = m_drawInfo.CurrentEventType;
			m_drawInfo.CurrentEventType = EventType.Ignore;

			m_mainGraphInstance.IsLoading = true;
			try
			{
				m_mainGraphInstance.Draw( m_drawInfo );
			}
			finally
			{
				m_mainGraphInstance.IsLoading = false;
				m_drawInfo.CurrentEventType = cachedEventType;
			}
			ShaderIsModified = false;
			Focus();
			Repaint();
		}

		public void OnProjectSelectionChanged()
		{
			if( m_loadShaderOnSelection )
			{
				LoadProjectSelected();
			}
		}

		ShaderLoadResult OnSaveShader( bool value, Shader shader, Material material, AmplifyShaderFunction function )
		{
			if( value )
			{
				SaveToDisk( false );
			}

			return value ? ShaderLoadResult.LOADED : ShaderLoadResult.FILE_NOT_FOUND;
		}

		public void ResetCameraSettings()
		{
			m_cameraInfo = position;
			m_cameraOffset = new Vector2( m_cameraInfo.width * 0.5f, m_cameraInfo.height * 0.5f );
			CameraZoom = 1;
		}

		public void Reset()
		{
			if( m_mainGraphInstance == null )
			{
				m_mainGraphInstance = CreateInstance<ParentGraph>();
				m_mainGraphInstance.Init();
				m_mainGraphInstance.ParentWindow = this;
				m_mainGraphInstance.SetGraphId( 0 );
			}
			m_mainGraphInstance.ResetEvents();
			m_mainGraphInstance.OnNodeEvent += OnNodeStoppedMovingEvent;
			m_mainGraphInstance.OnMaterialUpdatedEvent += OnMaterialUpdated;
			m_mainGraphInstance.OnShaderUpdatedEvent += OnShaderUpdated;
			m_mainGraphInstance.OnEmptyGraphDetectedEvt += OnEmptyGraphDetected;
			m_mainGraphInstance.OnNodeRemovedEvent += m_toolsWindow.OnNodeRemovedFromGraph;
			m_mainGraphInstance.OnNodeEvent += OnGraphNodeSetChanged;
			m_mainGraphInstance.OnNodeRemovedEvent += OnGraphNodeSetChanged;
			m_outdatedShaderFromTemplateLoaded = false;
			GraphCount = 1;

			FullCleanUndoStack();
			m_toolsWindow.BorderStyle = null;
			m_selectionMode = ASESelectionMode.Shader;
			ResetCameraSettings();
			UIUtils.ResetMainSkin();
			m_duplicatePreventionBuffer.ReleaseAllData();
			if( m_genericMessageUI != null )
				m_genericMessageUI.CleanUpMessageStack();
		}


		public Shader CreateNewGraph( string name )
		{
			Reset();
			UIUtils.DirtyMask = false;
			m_mainGraphInstance.CreateNewEmpty( name );
			m_lastOpenedLocation = string.Empty;
			UIUtils.DirtyMask = true;
			return m_mainGraphInstance.CurrentMasterNode.CurrentShader;
		}

		public Shader CreateNewTemplateGraph( string templateGUID )
		{
			Reset();
			UIUtils.DirtyMask = false;
			m_mainGraphInstance.CreateNewEmptyTemplate( templateGUID );
			m_lastOpenedLocation = string.Empty;
			UIUtils.DirtyMask = true;
			return m_mainGraphInstance.CurrentMasterNode.CurrentShader;
		}

		public Shader CreateNewGraph( Shader shader )
		{
			Reset();
			UIUtils.DirtyMask = false;
			m_mainGraphInstance.CreateNewEmpty( shader.name );
			m_mainGraphInstance.CurrentMasterNode.CurrentShader = shader;

			m_lastOpenedLocation = string.Empty;
			UIUtils.DirtyMask = true;
			return m_mainGraphInstance.CurrentMasterNode.CurrentShader;
		}

		public void CreateNewFunctionGraph( AmplifyShaderFunction shaderFunction )
		{
			Reset();
			UIUtils.DirtyMask = false;
			m_mainGraphInstance.CreateNewEmptyFunction( shaderFunction );
			m_mainGraphInstance.CurrentShaderFunction = shaderFunction;

			m_lastOpenedLocation = AssetDatabase.GetAssetPath( shaderFunction ); //string.Empty;
			UIUtils.DirtyMask = true;
			//return m_mainGraphInstance.CurrentMasterNode.CurrentShader;
		}

		private void FinishedShaderLoadLog( double compileTimeInSeconds )
		{
			string name = string.Empty;
			if ( m_mainGraphInstance.CurrentShader != null )
			{
				name = " \"" + AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShader ) + "\"";
			}
			else if ( m_mainGraphInstance.CurrentShaderFunction != null )
			{
				name = " \"" + AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShaderFunction ) + "\"";
			}
			Debug.Log( "[AmplifyShaderEditor] Finished loading" + name + " in " + compileTimeInSeconds.ToString( "0.00" ) + " seconds." );
		}

		private void FinishedShaderCompileLog( double compileTimeInSeconds )
		{
			string name = string.Empty;
			if ( m_mainGraphInstance.CurrentShader != null )
			{
				name = " \"" + AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShader ) + "\"";
			}
			else if ( m_mainGraphInstance.CurrentShaderFunction != null )
			{
				name = " \"" + AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShaderFunction ) + "\"";
			}
			Debug.Log( "[AmplifyShaderEditor] Finished compiling" + name + " in " + compileTimeInSeconds.ToString( "0.00" ) + " seconds." );
		}

		public static bool s_isSavingToDisk = false;
		public static bool IsSavingToDisk { get { return s_isSavingToDisk; } }

		public bool SaveToDisk( bool checkTimestamp )
		{
			if( checkTimestamp )
			{
				if( !m_cacheSaveOp )
				{
					m_lastTimeSaved = EditorApplication.timeSinceStartup;
					m_cacheSaveOp = true;
				}
				return false;
			}

			// No undo-stack clear here: under the snapshot model the proxy keeps its history as
			// serialized strings decoupled from the live nodes, so it stays valid across a save ( saving
			// writes the shader, it does not reload the graph ). Clearing was a legacy per-object-undo
			// safeguard; with live/auto-save ( SaveToDisk on every action ) it wiped undo every edit.

			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			var timer = new System.Diagnostics.Stopwatch();
			timer.Start();

			s_isSavingToDisk = true;

			m_customGraph = null;
			m_cacheSaveOp = false;
			ShaderIsModified = false;
			m_mainGraphInstance.LoadedShaderVersion = VersionInfo.FullNumber;
			m_lastTimeSaved = EditorApplication.timeSinceStartup;

			bool succeeded = false;

			if ( m_mainGraphInstance.CurrentMasterNodeId == Constants.INVALID_NODE_ID )
			{
				Shader currentShader = m_mainGraphInstance.CurrentMasterNode != null ? m_mainGraphInstance.CurrentMasterNode.CurrentShader : null;
				string newShader;
				if( !String.IsNullOrEmpty( m_lastOpenedLocation ) )
				{
					newShader = m_lastOpenedLocation;
				}
				else if( currentShader != null )
				{
					newShader = AssetDatabase.GetAssetPath( currentShader );
				}
				else
				{
					newShader = EditorUtility.SaveFilePanel( "Select Shader to save", Application.dataPath, "MyShader", "shader" );
				}

				if( !String.IsNullOrEmpty( newShader ) )
				{
					ShowMessage( "No Master node assigned.\nShader file will only have node info" );
					IOUtils.StartSaveThread( GenerateGraphInfo(), newShader );
					AssetDatabase.Refresh();
					LoadFromDisk( newShader );
					succeeded = true;
				}
			}
			else if( m_mainGraphInstance.CurrentMasterNode != null )
			{
				//m_mainGraphInstance.CurrentStandardSurface.ForceReordering();
				Shader currShader = m_mainGraphInstance.CurrentMasterNode.CurrentShader;
				if( currShader != null )
				{
					m_mainGraphInstance.FireMasterNode( currShader );
					Material material = m_mainGraphInstance.CurrentMaterial;
					m_lastpath = ( material != null ) ? AssetDatabase.GetAssetPath( material ) : AssetDatabase.GetAssetPath( currShader );
					EditorPrefs.SetString( IOUtils.LAST_OPENED_OBJ_ID, m_lastpath );
					if( IOUtils.OnShaderSavedEvent != null )
					{
						string info = string.Empty;
						if( !m_mainGraphInstance.IsStandardSurface )
						{
							TemplateMultiPassMasterNode masterNode = m_mainGraphInstance.GetMainMasterNodeOfLOD( -1 );
							if( masterNode != null )
							{
								info = masterNode.CurrentTemplate.GUID;
							}
						}
						IOUtils.OnShaderSavedEvent( currShader, !m_mainGraphInstance.IsStandardSurface, info );
					}
					succeeded = true;
				}
				else
				{

					string shaderName;
					string pathName;
					IOUtils.GetShaderName( out shaderName, out pathName, Constants.DefaultShaderName, UIUtils.LatestOpenedFolder );
					if( !String.IsNullOrEmpty( pathName ) )
					{
						UIUtils.CurrentWindow.CurrentGraph.CurrentMasterNode.SetName( shaderName );
						m_mainGraphInstance.FireMasterNode( pathName, true );
						m_lastpath = pathName;
						EditorPrefs.SetString( IOUtils.LAST_OPENED_OBJ_ID, pathName );
						succeeded = true;
					}
				}
			}
			else
			{
				//m_nodeParametersWindow.ForceReordering();
				m_mainGraphInstance.ResetNodesLocalVariables();

				List<FunctionInput> functionInputNodes = UIUtils.FunctionInputList();
				functionInputNodes.Sort( ( x, y ) => { return x.OrderIndex.CompareTo( y.OrderIndex ); } );
				for( int i = 0; i < functionInputNodes.Count; i++ )
				{
					functionInputNodes[ i ].OrderIndex = i;
				}

				List<FunctionOutput> functionOutputNodes = UIUtils.FunctionOutputList();
				functionOutputNodes.Sort( ( x, y ) => { return x.OrderIndex.CompareTo( y.OrderIndex ); } );
				for( int i = 0; i < functionOutputNodes.Count; i++ )
				{
					functionOutputNodes[ i ].OrderIndex = i;
				}

				m_mainGraphInstance.CurrentShaderFunction.AdditionalDirectives.UpdateSaveItemsFromDirectives();
				m_mainGraphInstance.CurrentShaderFunction.FunctionInfo = GenerateGraphInfo();
				m_mainGraphInstance.CurrentShaderFunction.FunctionInfo = IOUtils.AddAdditionalInfo( m_mainGraphInstance.CurrentShaderFunction.FunctionInfo );

				if( AssetDatabase.IsMainAsset( m_mainGraphInstance.CurrentShaderFunction ) )
				{
					EditorUtility.SetDirty( m_mainGraphInstance.CurrentShaderFunction );
				}
				else
				{
					//Debug.Log( LastOpenedLocation );
					//AssetDatabase.CreateAsset( m_mainGraphInstance.CurrentShaderFunction, LastOpenedLocation );
				}

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				m_mainGraphInstance.CurrentShaderFunction.AdditionalDirectives.UpdateDirectivesFromSaveItems();
				IOUtils.FunctionNodeChanged = true;
				m_lastpath = AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShaderFunction );
				//EditorPrefs.SetString( IOUtils.LAST_OPENED_OBJ_ID, AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShaderFunction ) );
				succeeded = true;
			}

			if ( succeeded )
			{
				// The current graph is now the persisted state; make it the baseline the Save
				// indicator compares against.
				CaptureSavedChecksum();
				MigrateUnsavedBackupHistory();
			}

			s_isSavingToDisk = false;

			timer.Stop();
			if ( Preferences.User.LogShaderCompile )
			{
				FinishedShaderCompileLog( timer.Elapsed.TotalSeconds );
			}

			System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			return succeeded;
		}

		// @diogo: toolbar clicks land mid-OnGUI, before the side panels draw; a save there reimports the shader,
		// which re-enters IMGUI and wipes the layout stack those panels are about to use, so run it at frame end
		void OnToolsWindowButtonPressed( ToolButtonType type )
		{
			m_pendingToolButton = type;
		}

		public void OnToolButtonPressed( ToolButtonType type )
		{
			switch( type )
			{
				case ToolButtonType.New:
				{
					UIUtils.CreateNewEmpty();
				}
				break;
				case ToolButtonType.Open:
				{
					UIUtils.OpenFile();
				}
				break;
				case ToolButtonType.Save:
				{
					SaveToDisk( false );
				}
				break;
				case ToolButtonType.Library:
				{
					ShowShaderLibrary();
				}
				break;
				case ToolButtonType.Options: { } break;
				case ToolButtonType.Update:
				{
					if( Preferences.User.ClearLog )
					{
						m_consoleLogWindow.ClearMessages();
					}
					SaveToDisk( false );
				}
				break;
				case ToolButtonType.Live:
				{
					m_liveShaderEditing = !m_liveShaderEditing;
					m_innerEditorVariables.LiveMode = m_liveShaderEditing;
					// 0 off
					// 1 on
					// 2 pending
					if( m_liveShaderEditing && m_mainGraphInstance.CurrentMasterNode != null && m_mainGraphInstance.CurrentMasterNode.CurrentShader == null )
					{
						m_liveShaderEditing = false;
						m_innerEditorVariables.LiveMode = false;
					}

					UpdateLiveUI();

					if( m_liveShaderEditing )
					{
						SaveToDisk( false );
					}
				}
				break;
				case ToolButtonType.OpenSourceCode:
				{
					AssetDatabase.OpenAsset( m_mainGraphInstance.CurrentMasterNode.CurrentShader, 1 );
				}
				break;
				case ToolButtonType.MasterNode:
				{
					m_mainGraphInstance.AssignMasterNode();
				}
				break;

				case ToolButtonType.FocusOnMasterNode:
				{
					double currTime = EditorApplication.timeSinceStartup;
					bool autoZoom = ( currTime - m_focusOnMasterNodeTimestamp ) < AutoZoomTime;
					m_focusOnMasterNodeTimestamp = currTime;
					FocusOnNode( m_mainGraphInstance.CurrentMasterNode, autoZoom ? 1 : m_cameraZoom, true );
				}
				break;

				case ToolButtonType.FocusOnSelection:
				{
					FocusZoom( false, true, true );
				}
				break;
				case ToolButtonType.ShowInfoWindow:
				{
					PortLegendInfo.OpenWindow();
				}
				break;
				case ToolButtonType.ShowTipsWindow:
				{
					TipsWindow.ShowWindow( true );
				}
				break;
				case ToolButtonType.ShowConsole:
				{
					m_consoleLogWindow.Toggle();
				}
				break;
				case ToolButtonType.Share:
				{
					List<ParentNode> selectedNodes = m_mainGraphInstance.SelectedNodes;
					if ( selectedNodes.Count > 0 )
					{
						// @diogo: the upload reads the payload back out of EditorPrefs, and the link it
						// returns is what belongs in the OS clipboard once it arrives
						CopyToClipboard( false );
						StartPasteRequest();
					}
					else
					{
						ShowMessage( "No nodes selected to share" );
					}
				}
				break;
				case ToolButtonType.TakeScreenshot:
				{
#if UNITY_EDITOR_WIN
					this.Focus();
					m_aseHandle = WindowsUtil.GetActiveWindow();
					//m_aseHandle = FindASEWindowHandle();

					bool takeit = EditorUtility.DisplayDialog( "Take Screenshot", "This is a work in progress feature that will undock itself if needed, increase the window outside of your screen resolution to take the shot, if something fails (ie: graph too big) you may need to restart Unity, do you wish to continue?", "Yes", "Cancel" );
					if( !takeit )
						break;

					if( this.IsDocked() )
					{
						this.Undock();
						this.Focus();
						m_aseHandle = WindowsUtil.GetActiveWindow();
					}

					int windowLong = WindowsUtil.GetWindowLong( m_aseHandle, WindowsUtil.GWL_STYLE );

					List<ParentNode> selectedNodes = m_mainGraphInstance.AllNodes;

					Vector2 minPos = new Vector2( float.MaxValue, float.MaxValue );
					Vector2 maxPos = new Vector2( float.MinValue, float.MinValue );
					Vector2 centroid = Vector2.zero;

					for( int i = 0; i < selectedNodes.Count; i++ )
					{
						Rect currPos = selectedNodes[ i ].TruePosition;
						minPos.x = ( currPos.x < minPos.x ) ? currPos.x : minPos.x;
						minPos.y = ( currPos.y < minPos.y ) ? currPos.y : minPos.y;

						maxPos.x = ( ( currPos.x + currPos.width ) > maxPos.x ) ? ( currPos.x + currPos.width ) : maxPos.x;
						maxPos.y = ( ( currPos.y + currPos.height ) > maxPos.y ) ? ( currPos.y + currPos.height ) : maxPos.y;
					}

					centroid = ( maxPos - minPos );

					m_prevCameraOffset = m_cameraOffset;
					m_prevCameraZoom = CameraZoom;

					WindowsUtil.SetWindowLong( m_aseHandle, WindowsUtil.GWL_STYLE, (int)( windowLong & ~( WindowsUtil.WS_SIZEBOX ) ) );
					var rect = new WindowsUtil.Rect();
					WindowsUtil.GetWindowRect( m_aseHandle, ref rect );
					m_prevWindowRect = new Rect( rect.Left, rect.Top, rect.Width, rect.Height );

					WindowsUtil.SetWindowPos( m_aseHandle, 0, (int)m_prevWindowRect.xMin, (int)m_prevWindowRect.yMin, (int)centroid.x, (int)centroid.y, 0x0040 );
					WindowsUtil.SetWindowLong( m_aseHandle, WindowsUtil.GWL_STYLE, (int)( windowLong ) );

					m_takeScreenShot = true;
#else
					EditorUtility.DisplayDialog( "Take Screenshot", "This is a work in progress feature that only works in Windows environment", "Ok" );
#endif
				}
				break;
				case ToolButtonType.CleanUnusedNodes:
				{
					m_mainGraphInstance.CleanUnusedNodes();
				}
				break;
				case ToolButtonType.Help:
				{
					Application.OpenURL( Constants.HelpURL );
				}
				break;
			}
		}

#if UNITY_EDITOR_WIN
		IntPtr FindASEWindowHandle()
		{
			System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();

			IntPtr[] winPtrs = WindowsUtil.GetProcessWindows( process.Id );
			m_aseHandle = IntPtr.Zero;
			bool found = false;
			for( int i = 0; i < winPtrs.Length; i++ )
			{
				WindowsUtil.EnumChildWindows( winPtrs[ i ], delegate ( System.IntPtr hwnd, System.IntPtr param )
				{
					System.Text.StringBuilder Title = new System.Text.StringBuilder( 256 );
					WindowsUtil.GetWindowText( hwnd, Title, Title.Capacity );

					if( Title.ToString().Contains( "AmplifyShaderEditor.AmplifyShaderEditorWindow" ) )
					{
						if( !found )
						{
							m_aseHandle = winPtrs[ i ];
							found = true;
						}
					}

					return true;
				}, System.IntPtr.Zero );
			}

			return m_aseHandle;
		}

		void OpenSavedFolder()
		{
			m_openSavedFolder = false;

			var path = System.IO.Path.GetFullPath( Application.dataPath + "\\..\\ScreenshotASE.png" );
			EditorUtility.RevealInFinder( path );
			GUIUtility.ExitGUI();
		}

		void TakeScreenShot()
		{
			m_takeScreenShot = false;

			var cacher = RenderTexture.active;
			RenderTexture.active = null;

			int pixelWidth = Mathf.RoundToInt( position.width * EditorGUIUtility.pixelsPerPoint );
			int pixelHeight = Mathf.RoundToInt( position.height * EditorGUIUtility.pixelsPerPoint );

			Texture2D m_screenshotTex2D = new Texture2D( pixelWidth, pixelHeight, TextureFormat.RGB24, false );
			m_screenshotTex2D.ReadPixels( new Rect( 0, 0, m_screenshotTex2D.width, m_screenshotTex2D.height ), 0, 0 );
			m_screenshotTex2D.Apply();

			byte[] bytes = m_screenshotTex2D.EncodeToPNG();

			var path = System.IO.Path.GetFullPath( Application.dataPath + "\\..\\ScreenshotASE.png" );
			System.IO.File.WriteAllBytes( path, bytes );

			RenderTexture.active = cacher;

			ShowMessage( "[AmplifyShaderEditor] Screenshot successfully taken and saved at: " + path, consoleLog:true );

			WindowsUtil.SetWindowPos( m_aseHandle, 0, (int)m_prevWindowRect.xMin, (int)m_prevWindowRect.yMin, (int)m_prevWindowRect.width, (int)m_prevWindowRect.height, 0x0040 );
			m_cameraOffset = m_prevCameraOffset;
			CameraZoom = m_prevCameraZoom;

			m_openSavedFolder = true;
		}
#endif


		void UpdateLiveUI()
		{
			if( m_toolsWindow != null )
			{
				m_toolsWindow.SetStateOnButton( ToolButtonType.Live, ( m_liveShaderEditing ) ? 1 : 0 );
			}
		}

		void FocusZoom( bool forceAllNodes, bool doubleTap, bool smooth = true, float maxZoom = float.MaxValue )
		{
			List<ParentNode> selectedNodes = ( m_mainGraphInstance.SelectedNodes.Count > 0 ) && !forceAllNodes ? m_mainGraphInstance.SelectedNodes : m_mainGraphInstance.AllNodes;

			Vector2 minPos = new Vector2( float.MaxValue, float.MaxValue );
			Vector2 maxPos = new Vector2( float.MinValue, float.MinValue );
			Vector2 centroid = Vector2.zero;

			for( int i = 0; i < selectedNodes.Count; i++ )
			{
				Rect currPos = selectedNodes[ i ].TruePosition;

				minPos.x = ( currPos.x < minPos.x ) ? currPos.x : minPos.x;
				minPos.y = ( currPos.y < minPos.y ) ? currPos.y : minPos.y;

				maxPos.x = ( ( currPos.x + currPos.width ) > maxPos.x ) ? ( currPos.x + currPos.width ) : maxPos.x;
				maxPos.y = ( ( currPos.y + currPos.height ) > maxPos.y ) ? ( currPos.y + currPos.height ) : maxPos.y;

			}

			centroid = ( maxPos - minPos );

			double currTime = EditorApplication.timeSinceStartup;
			bool autoZoom = ( currTime - m_focusOnSelectionTimestamp ) < AutoZoomTime;
			if( !doubleTap )
				autoZoom = true;
			m_focusOnSelectionTimestamp = currTime;

			float zoom = m_cameraZoom;
			if( autoZoom )
			{
				zoom = 1f;
				float canvasWidth = m_cameraInfo.width;
				if( m_nodeParametersWindow.IsMaximized )
					canvasWidth -= m_nodeParametersWindow.RealWidth;
				if( m_paletteWindow.IsMaximized )
					canvasWidth -= m_paletteWindow.RealWidth;
				canvasWidth -= 40;
				//float canvasWidth = AvailableCanvasWidth;// - 20;
				float canvasHeight = AvailableCanvasHeight - 60;
				if( centroid.x > canvasWidth ||
					centroid.y > canvasHeight )
				{
					float hZoom = float.MinValue;
					float vZoom = float.MinValue;
					if( centroid.x > canvasWidth )
					{
						hZoom = ( centroid.x ) / canvasWidth;
					}

					if( centroid.y > canvasHeight )
					{
						vZoom = ( centroid.y ) / canvasHeight;
					}
					zoom = ( hZoom > vZoom ) ? hZoom : vZoom;
				}
			}

			zoom = Mathf.Min( zoom, maxZoom );

			minPos.y -= 20 * zoom;
			if( m_nodeParametersWindow.IsMaximized )
				minPos.x -= m_nodeParametersWindow.RealWidth * 0.5f * zoom;
			if( m_paletteWindow.IsMaximized )
				minPos.x += m_paletteWindow.RealWidth * 0.5f * zoom;

			FocusOnPoint( minPos + centroid * 0.5f, zoom, smooth );
		}

		public void FocusOnNode( int nodeId, float zoom, bool selectNode, bool late = false )
		{
			ParentNode node = m_mainGraphInstance.GetNode( nodeId );
			if( node != null )
			{
				FocusOnNode( node, zoom, selectNode, late );
			}
		}

		public void FocusOnNode( ParentNode node, float zoom, bool selectNode, bool late = false )
		{
			if( late )
			{
				m_nodeToFocus = node;
				m_zoomToFocus = zoom;
				m_selectNodeToFocus = selectNode;
				return;
			}

			if( selectNode )
			{
				m_mainGraphInstance.SelectNode( node, false, false );
			}

			Vector2 nodePoint = node.CenterPosition;
			nodePoint.x = nodePoint.x - ( m_nodeParametersWindow.RealWidth * 0.5f + m_paletteWindow.RealWidth * 0.5f ) * ( zoom > 0.999f ? zoom : CameraZoom );
			FocusOnPoint( nodePoint, zoom );
		}

		public void FocusOnPoint( Vector2 point, float zoom, bool smooth = true )
		{
			if( zoom > 0.999f )
			{
				if( smooth )
					SmoothZoom( zoom );
				else
					CameraZoom = zoom;
			}

			if( smooth )
				SmoothCameraOffset( -point + new Vector2( ( m_cameraInfo.width ) * 0.5f, m_cameraInfo.height * 0.5f ) * CameraZoom );
			else
				m_cameraOffset = -point + new Vector2( ( m_cameraInfo.width ) * 0.5f, m_cameraInfo.height * 0.5f ) * CameraZoom;
		}

		void SmoothZoom( float newZoom )
		{
			m_smoothZoom = true;
			m_zoomTime = 0;
			m_targetZoom = newZoom;
			m_zoomPivot = m_graphArea.center;
		}

		void SmoothCameraOffset( Vector2 newOffset )
		{
			m_smoothOffset = true;
			m_offsetTime = 0;
			m_targetOffset = newOffset;
		}

		void PreTestLeftMouseDown()
		{
			if( m_currentEvent.type == EventType.MouseDown && m_currentEvent.button == ButtonClickId.LeftMouseButton )
			{
				ParentNode node = m_mainGraphInstance.CheckNodeAt( m_currentMousePos );
				if( node != null )
				{
					m_mainGraphInstance.NodeClicked = node.UniqueId;
					return;
				}
			}

			m_mainGraphInstance.NodeClicked = -1;
		}



		void OnLeftMouseDown()
		{
			Focus();

			// @diogo: a canvas click lands outside the palette; commit any in-progress backup rename and clear its selection.
			if ( m_paletteWindow != null )
			{
				m_paletteWindow.CommitAndClearBackupSelection();
			}

			if( m_lastKeyPressed == KeyCode.Q )
			{
				m_rmbStartPos = m_currentMousePos2D;
				UseCurrentEvent();
				return;
			}

			m_mouseDownOnValidArea = true;
			m_lmbPressed = true;
			if( m_currentEvent.alt )
			{
				m_altBoxSelection = true;
			}

			UIUtils.ShowContextOnPick = true;
			ParentNode node = ( m_mainGraphInstance.NodeClicked < 0 ) ? m_mainGraphInstance.CheckNodeAt( m_currentMousePos ) : m_mainGraphInstance.GetClickedNode();
			if( node != null )
			{
				m_mainGraphInstance.NodeClicked = node.UniqueId;
				m_altBoxSelection = false;

				if( m_contextMenu.CheckShortcutKey() )
				{
					if( node.ConnStatus == NodeConnectionStatus.Island )
					{
						if( !m_multipleSelectionActive )
						{
							ParentNode newNode = m_contextMenu.CreateNodeFromShortcutKey();
							if( newNode != null )
							{
								newNode.ContainerGraph = m_mainGraphInstance;
								newNode.Vec2Position = TranformedMousePos;
								m_mainGraphInstance.AddNode( newNode, true );
								m_mainGraphInstance.SelectNode( newNode, false, false );
								ForceRepaint();
							}
							( node as CommentaryNode ).AddNodeToCommentary( newNode );
						}
					}
				}
				else
				{
					if( node.OnClick( m_currentMousePos2D ) )
					{
						if( !node.Selected )
						{
							m_mainGraphInstance.SelectNode( node, ( m_currentEvent.modifiers == EventModifiers.Shift || m_currentEvent.modifiers == EventModifiers.Control ), true );
						}
						else if( m_currentEvent.modifiers == EventModifiers.Shift || m_currentEvent.modifiers == EventModifiers.Control )
						{
							m_mainGraphInstance.DeselectNode( node );
						}

						if( m_currentEvent.alt )
						{
							int conn = 0;
							for( int i = 0; i < node.InputPorts.Count; i++ )
							{
								if( node.InputPorts[ i ].IsConnected )
									conn++;
							}

							if( node.InputPorts.Count > 0 && node.OutputPorts.Count > 0 && conn > 0 && node.OutputPorts[ 0 ].IsConnected )
							{
								m_altDragStarted = true;
							}
						}

					}

					if( m_currentEvent.alt )
					{
						if( node.InputPorts.Count > 0 && node.OutputPorts.Count > 0 && node.InputPorts[ 0 ].IsConnected && node.OutputPorts[ 0 ].IsConnected )
						{
							m_altDragStarted = true;
						}
					}

					return;
				}
			}
			else if( !m_multipleSelectionActive )
			{
				ParentNode newNode = m_contextMenu.CreateNodeFromShortcutKey();
				if( newNode != null )
				{
					newNode.ContainerGraph = m_mainGraphInstance;
					newNode.Vec2Position = TranformedMousePos;
					m_mainGraphInstance.AddNode( newNode, true );
					m_mainGraphInstance.SelectNode( newNode, false, false );
					SetSaveIsDirty();
					ForceRepaint();
				}
				else
				{
					List<WireBezierReference> wireRefs = m_mainGraphInstance.GetWireBezierListInPos( m_currentMousePos2D );
					if( wireRefs != null && wireRefs.Count > 0 )
					{
						for( int i = 0; i < wireRefs.Count; i++ )
						{
							// Place wire code here
							ParentNode outNode = m_mainGraphInstance.GetNode( wireRefs[ i ].OutNodeId );
							ParentNode inNode = m_mainGraphInstance.GetNode( wireRefs[ i ].InNodeId );

							OutputPort outputPort = outNode.GetOutputPortByUniqueId( wireRefs[ i ].OutPortId );
							InputPort inputPort = inNode.GetInputPortByUniqueId( wireRefs[ i ].InPortId );

							// Calculate the 4 points for bezier taking into account wire nodes and their automatic tangents
							Vector3 endPos = new Vector3( inputPort.Position.x, inputPort.Position.y );
							Vector3 startPos = new Vector3( outputPort.Position.x, outputPort.Position.y );

							float mag = ( endPos - startPos ).magnitude;
							float resizedMag = Mathf.Min( mag, Constants.HORIZONTAL_TANGENT_SIZE * m_drawInfo.InvertedZoom );

							Vector3 startTangent = new Vector3( startPos.x + resizedMag, startPos.y );
							Vector3 endTangent = new Vector3( endPos.x - resizedMag, endPos.y );

							if( inNode != null && inNode.GetType() == typeof( WireNode ) )
								endTangent = endPos + ( ( inNode as WireNode ).TangentDirection ) * mag * 0.33f;

							if( outNode != null && outNode.GetType() == typeof( WireNode ) )
								startTangent = startPos - ( ( outNode as WireNode ).TangentDirection ) * mag * 0.33f;

							float dist = HandleUtility.DistancePointBezier( m_currentMousePos, startPos, endPos, startTangent, endTangent );
							if( dist < 10 )
							{
								double doubleTapTime = EditorApplication.timeSinceStartup;
								bool doubleTap = ( doubleTapTime - m_wiredDoubleTapTimestamp ) < WiredDoubleTapTime;
								m_wiredDoubleTapTimestamp = doubleTapTime;

								if( doubleTap )
								{
									RegisterGraphUndo( Constants.UndoCreateConnectionId );

									ParentNode wireNode = m_mainGraphInstance.CreateNode( typeof( WireNode ), true );
									if( wireNode != null )
									{
										wireNode.Vec2Position = TranformedMousePos;

										m_mainGraphInstance.CreateConnection( wireNode.InputPorts[ 0 ].NodeId, wireNode.InputPorts[ 0 ].PortId, outputPort.NodeId, outputPort.PortId );
										m_mainGraphInstance.CreateConnection( inputPort.NodeId, inputPort.PortId, wireNode.OutputPorts[ 0 ].NodeId, wireNode.OutputPorts[ 0 ].PortId );

										SetSaveIsDirty();
										ForceRepaint();
										UndoUtils.IncrementCurrentGroup();
									}
								}

								break;
							}
						}
					}
					//Reset focus from any textfield which may be selected at this time
					GUIUtility.keyboardControl = 0;
				}
			}

			if( m_currentEvent.modifiers != EventModifiers.Shift && m_currentEvent.modifiers != EventModifiers.Control && !m_altBoxSelection )
				m_mainGraphInstance.DeSelectAll();

			if( m_wireReferenceUtils.ValidReferences() )
			{
				m_wireReferenceUtils.InvalidateReferences();
				return;
			}

			if( !m_contextMenu.CheckShortcutKey() && m_currentEvent.modifiers != EventModifiers.Shift && m_currentEvent.modifiers != EventModifiers.Control || m_altBoxSelection )
			{
				// Only activate multiple selection if no node is selected and shift key not pressed
				m_multipleSelectionActive = true;

				m_multipleSelectionStart = TranformedMousePos;
				m_multipleSelectionArea.position = m_multipleSelectionStart;
				m_multipleSelectionArea.size = Vector2.zero;
			}

			UseCurrentEvent();
		}

		void OnLeftMouseDrag()
		{
			if( m_lostFocus )
			{
				m_lostFocus = false;
				return;
			}

			if( m_lastKeyPressed == KeyCode.Q )
			{
				if( m_currentEvent.alt )
				{
					ModifyZoom( Constants.ALT_CAMERA_ZOOM_SPEED * ( m_currentEvent.delta.x + m_currentEvent.delta.y ), m_altKeyStartPos );
				}
				else
				{
					m_cameraOffset += m_cameraZoom * m_currentEvent.delta;
				}
				UseCurrentEvent();
				return;
			}

			if( m_altDragStarted )
			{
				m_altDragStarted = false;

				if( m_currentEvent.modifiers == EventModifiers.Alt && CurrentGraph.SelectedNodes.Count == 1 )
				{
					ParentNode node = CurrentGraph.SelectedNodes[ 0 ];
					int lastId = 0;
					int conn = 0;
					for( int i = 0; i < node.InputPorts.Count; i++ )
					{
						if( node.InputPorts[ i ].IsConnected )
						{
							conn++;
							lastId = i;
						}
					}

					if( conn > 1 )
						lastId = 0;



					OutputPort outputPort = node.InputPorts[ lastId ].GetOutputConnection( 0 );
					ParentNode outputNode = m_mainGraphInstance.GetNode( outputPort.NodeId );
					bool outputIsWireNode = outputNode is WireNode;

					RegisterGraphUndo( Constants.UndoCreateConnectionId );

					List<InputPort> inputPorts = new List<InputPort>();
					for( int i = 0; i < node.OutputPorts[ 0 ].ConnectionCount; i++ )
					{
						InputPort inputPort = node.OutputPorts[ 0 ].GetInputConnection( i );
						inputPorts.Add( inputPort );
					}

					for( int i = 0; i < inputPorts.Count; i++ )
					{
						if( outputIsWireNode )
						{
							if( i == 0 )
							{
								m_mainGraphInstance.CreateConnection( inputPorts[ i ].NodeId, inputPorts[ i ].PortId, outputPort.NodeId, outputPort.PortId );
							}
							else
							{
								UIUtils.DeleteConnection( true, inputPorts[ i ].NodeId, inputPorts[ i ].PortId, false, true );
							}
						}
						else
						{
							m_mainGraphInstance.CreateConnection( inputPorts[ i ].NodeId, inputPorts[ i ].PortId, outputPort.NodeId, outputPort.PortId );
						}
					}

					UIUtils.DeleteConnection( true, node.UniqueId, node.InputPorts[ lastId ].PortId, false, true );

					SetSaveIsDirty();
					ForceRepaint();
				}
			}

			if( !m_wireReferenceUtils.ValidReferences() && !m_altBoxSelection )
			{
				if( m_mouseDownOnValidArea && m_insideEditorWindow )
				{
					if( m_currentEvent.control || Preferences.User.AlwaysSnapToGrid )
					{
						m_mainGraphInstance.MoveSelectedNodes( m_cameraZoom * m_currentEvent.delta, true );
					}
					else
					{
						m_mainGraphInstance.MoveSelectedNodes( m_cameraZoom * m_currentEvent.delta );
					}
					//m_mainGraphInstance.MoveSelectedNodes( m_cameraZoom * m_currentEvent.delta );
					m_autoPanDirActive = true;

					// @diogo: repaint at drag cadence; a pure move dirties no preview, so the throttled update loop never repaints it (and skips entirely with Disable Previews on)
					Repaint();
				}
			}
			else
			{
				List<ParentNode> nodes = m_mainGraphInstance.GetNodesInGrid( m_drawInfo.TransformedMousePos );
				if( nodes != null && nodes.Count > 0 )
				{
					Vector2 currentPortPos = new Vector2();
					Vector2 mousePos = TranformedMousePos;

					if( m_wireReferenceUtils.InputPortReference.IsValid )
					{
						OutputPort currentPort = null;
						float smallestDistance = float.MaxValue;
						Vector2 smallestPosition = Vector2.zero;
						for( int nodeIdx = 0; nodeIdx < nodes.Count; nodeIdx++ )
						{
							List<OutputPort> outputPorts = nodes[ nodeIdx ].OutputPorts;
							if( outputPorts != null )
							{
								for( int o = 0; o < outputPorts.Count; o++ )
								{
									if( outputPorts[ o ].Available )
									{
										currentPortPos.x = outputPorts[ o ].Position.x;
										currentPortPos.y = outputPorts[ o ].Position.y;

										currentPortPos = currentPortPos * m_cameraZoom - m_cameraOffset;
										float dist = ( mousePos - currentPortPos ).sqrMagnitude;
										if( dist < smallestDistance )
										{
											smallestDistance = dist;
											smallestPosition = currentPortPos;
											currentPort = outputPorts[ o ];
										}
									}
								}
							}
						}

						if( currentPort != null && currentPort.Available && ( smallestDistance < Constants.SNAP_SQR_DIST || currentPort.InsideActiveArea( ( mousePos + m_cameraOffset ) / m_cameraZoom ) ) )
						{
							m_wireReferenceUtils.ActivateSnap( smallestPosition, currentPort );
						}
						else
						{
							m_wireReferenceUtils.DeactivateSnap();
						}
					}

					if( m_wireReferenceUtils.OutputPortReference.IsValid )
					{
						InputPort currentPort = null;
						float smallestDistance = float.MaxValue;
						Vector2 smallestPosition = Vector2.zero;
						for( int nodeIdx = 0; nodeIdx < nodes.Count; nodeIdx++ )
						{
							List<InputPort> inputPorts = nodes[ nodeIdx ].InputPorts;
							if( inputPorts != null )
							{
								for( int i = 0; i < inputPorts.Count; i++ )
								{
									if( inputPorts[ i ].Available )
									{
										currentPortPos.x = inputPorts[ i ].Position.x;
										currentPortPos.y = inputPorts[ i ].Position.y;

										currentPortPos = currentPortPos * m_cameraZoom - m_cameraOffset;
										float dist = ( mousePos - currentPortPos ).sqrMagnitude;
										if( dist < smallestDistance )
										{
											smallestDistance = dist;
											smallestPosition = currentPortPos;
											currentPort = inputPorts[ i ];
										}
									}
								}
							}
						}
						if( currentPort != null && currentPort.Available && ( smallestDistance < Constants.SNAP_SQR_DIST || currentPort.InsideActiveArea( ( mousePos + m_cameraOffset ) / m_cameraZoom ) ) )
						{
							m_wireReferenceUtils.ActivateSnap( smallestPosition, currentPort );
						}
						else
						{
							m_wireReferenceUtils.DeactivateSnap();
						}
					}
				}
				else if( m_wireReferenceUtils.SnapEnabled )
				{
					m_wireReferenceUtils.DeactivateSnap();
				}
			}
			UseCurrentEvent();
		}

		public void OnLeftMouseUp()
		{
			m_lmbPressed = false;
			if( m_multipleSelectionActive )
			{
				//m_multipleSelectionActive = false;
				UpdateSelectionArea();
				//m_mainGraphInstance.MultipleSelection( m_multipleSelectionArea, ( m_currentEvent.modifiers == EventModifiers.Shift || m_currentEvent.modifiers == EventModifiers.Control ), true );
				if( m_currentEvent.alt && m_altBoxSelection )
				{
					m_mainGraphInstance.MultipleSelection( m_multipleSelectionArea, !m_currentEvent.shift );
				}
				else
				{
					m_mainGraphInstance.DeSelectAll();
					m_mainGraphInstance.MultipleSelection( m_multipleSelectionArea );
				}
			}

			if( m_wireReferenceUtils.ValidReferences() )
			{
				//Check if there is some kind of port beneath the mouse ... if so connect to it
				ParentNode targetNode = m_wireReferenceUtils.SnapEnabled ? m_mainGraphInstance.GetNode( m_wireReferenceUtils.SnapPort.NodeId ) : m_mainGraphInstance.CheckNodeAt( m_currentMousePos );
				if( targetNode != null && targetNode.ConnStatus != NodeConnectionStatus.Island )
				{
					if( m_wireReferenceUtils.InputPortReference.IsValid && m_wireReferenceUtils.InputPortReference.NodeId != targetNode.UniqueId )
					{
						OutputPort outputPort = m_wireReferenceUtils.SnapEnabled ? targetNode.GetOutputPortByUniqueId( m_wireReferenceUtils.SnapPort.PortId ) : targetNode.CheckOutputPortAt( m_currentMousePos );
						if( outputPort != null && !outputPort.Locked && ( !m_wireReferenceUtils.InputPortReference.TypeLocked ||
													m_wireReferenceUtils.InputPortReference.DataType == WirePortDataType.OBJECT ||
													( m_wireReferenceUtils.InputPortReference.TypeLocked && outputPort.DataType == m_wireReferenceUtils.InputPortReference.DataType ) ) )
						{

							ParentNode originNode = m_mainGraphInstance.GetNode( m_wireReferenceUtils.InputPortReference.NodeId );
							InputPort inputPort = originNode.GetInputPortByUniqueId( m_wireReferenceUtils.InputPortReference.PortId );
							RegisterGraphUndo( Constants.UndoCreateConnectionId );

							if( inputPort.NotFreeForAllTypes && outputPort.NotFreeForAllTypes )
							{
								if( !inputPort.CheckValidType( outputPort ) )
								{
									UIUtils.ShowIncompatiblePortMessage( true, originNode, inputPort, targetNode, outputPort );
									m_wireReferenceUtils.InvalidateReferences();
									UseCurrentEvent();
									return;
								}

								if( !outputPort.CheckValidType( inputPort ) )
								{
									UIUtils.ShowIncompatiblePortMessage( false, targetNode, outputPort, originNode, inputPort );
									m_wireReferenceUtils.InvalidateReferences();
									UseCurrentEvent();
									return;
								}
							}

							inputPort.DummyAdd( outputPort.NodeId, outputPort.PortId );
							outputPort.DummyAdd( m_wireReferenceUtils.InputPortReference.NodeId, m_wireReferenceUtils.InputPortReference.PortId );

							if( UIUtils.DetectNodeLoopsFrom( originNode, new Dictionary<int, int>() ) )
							{
								inputPort.DummyRemove();
								outputPort.DummyRemove();
								m_wireReferenceUtils.InvalidateReferences();
								ShowMessage( "Infinite Loop detected" );
								UseCurrentEvent();
								return;
							}

							inputPort.DummyRemove();
							outputPort.DummyRemove();

							if( inputPort.IsConnected )
							{
								DeleteConnection( true, m_wireReferenceUtils.InputPortReference.NodeId, m_wireReferenceUtils.InputPortReference.PortId, true, false );
							}

							//link output to input
							if( outputPort.ConnectTo( m_wireReferenceUtils.InputPortReference.NodeId, m_wireReferenceUtils.InputPortReference.PortId, m_wireReferenceUtils.InputPortReference.DataType, m_wireReferenceUtils.InputPortReference.TypeLocked ) )
								targetNode.OnOutputPortConnected( outputPort.PortId, m_wireReferenceUtils.InputPortReference.NodeId, m_wireReferenceUtils.InputPortReference.PortId );

							//link input to output
							if( inputPort.ConnectTo( outputPort.NodeId, outputPort.PortId, outputPort.DataType, m_wireReferenceUtils.InputPortReference.TypeLocked ) )
								originNode.OnInputPortConnected( m_wireReferenceUtils.InputPortReference.PortId, targetNode.UniqueId, outputPort.PortId );
							m_mainGraphInstance.MarkWireHighlights();
						}
						else if( outputPort != null && m_wireReferenceUtils.InputPortReference.TypeLocked && m_wireReferenceUtils.InputPortReference.DataType != outputPort.DataType )
						{
							ShowMessage( "Attempting to connect a port locked to type " + m_wireReferenceUtils.InputPortReference.DataType + " into a port of type " + outputPort.DataType );
						}
						ShaderIsModified = true;
						SetSaveIsDirty();
					}

					if( m_wireReferenceUtils.OutputPortReference.IsValid && m_wireReferenceUtils.OutputPortReference.NodeId != targetNode.UniqueId )
					{
						InputPort inputPort = m_wireReferenceUtils.SnapEnabled ? targetNode.GetInputPortByUniqueId( m_wireReferenceUtils.SnapPort.PortId ) : targetNode.CheckInputPortAt( m_currentMousePos );
						if( inputPort != null && !inputPort.Locked && ( !inputPort.TypeLocked ||
													 inputPort.DataType == WirePortDataType.OBJECT ||
													 ( inputPort.TypeLocked && inputPort.DataType == m_wireReferenceUtils.OutputPortReference.DataType ) ) )
						{
							ParentNode originNode = m_mainGraphInstance.GetNode( m_wireReferenceUtils.OutputPortReference.NodeId );
							OutputPort outputPort = originNode.GetOutputPortByUniqueId( m_wireReferenceUtils.OutputPortReference.PortId );

							RegisterGraphUndo( Constants.UndoCreateConnectionId );

							if( inputPort.NotFreeForAllTypes && outputPort.NotFreeForAllTypes )
							{
								if( !inputPort.CheckValidType( outputPort ) )
								{
									UIUtils.ShowIncompatiblePortMessage( true, targetNode, inputPort, originNode, outputPort );
									m_wireReferenceUtils.InvalidateReferences();
									UseCurrentEvent();
									return;
								}

								if( !outputPort.CheckValidType( inputPort ) )
								{
									UIUtils.ShowIncompatiblePortMessage( false, originNode, outputPort, targetNode, inputPort );
									m_wireReferenceUtils.InvalidateReferences();
									UseCurrentEvent();
									return;
								}
							}

							inputPort.DummyAdd( m_wireReferenceUtils.OutputPortReference.NodeId, m_wireReferenceUtils.OutputPortReference.PortId );
							outputPort.DummyAdd( inputPort.NodeId, inputPort.PortId );
							if( UIUtils.DetectNodeLoopsFrom( targetNode, new Dictionary<int, int>() ) )
							{
								inputPort.DummyRemove();
								outputPort.DummyRemove();
								m_wireReferenceUtils.InvalidateReferences();
								ShowMessage( "Infinite Loop detected" );
								UseCurrentEvent();
								return;
							}

							inputPort.DummyRemove();
							outputPort.DummyRemove();

							if( inputPort.IsConnected )
							{
								if( m_currentEvent.control && m_wireReferenceUtils.SwitchPortReference.IsValid )
								{
									ParentNode oldOutputNode = UIUtils.GetNode( inputPort.GetConnection( 0 ).NodeId );
									OutputPort oldOutputPort = oldOutputNode.GetOutputPortByUniqueId( inputPort.GetConnection( 0 ).PortId );

									ParentNode switchNode = UIUtils.GetNode( m_wireReferenceUtils.SwitchPortReference.NodeId );
									InputPort switchPort = switchNode.GetInputPortByUniqueId( m_wireReferenceUtils.SwitchPortReference.PortId );

									switchPort.DummyAdd( oldOutputPort.NodeId, oldOutputPort.PortId );
									oldOutputPort.DummyAdd( switchPort.NodeId, switchPort.PortId );
									if( UIUtils.DetectNodeLoopsFrom( switchNode, new Dictionary<int, int>() ) )
									{
										switchPort.DummyRemove();
										oldOutputPort.DummyRemove();
										m_wireReferenceUtils.InvalidateReferences();
										ShowMessage( "Infinite Loop detected" );
										UseCurrentEvent();
										return;
									}

									switchPort.DummyRemove();
									oldOutputPort.DummyRemove();

									DeleteConnection( true, inputPort.NodeId, inputPort.PortId, true, false );
									ConnectInputToOutput( switchPort.NodeId, switchPort.PortId, oldOutputPort.NodeId, oldOutputPort.PortId );
								}
								else
								{
									DeleteConnection( true, inputPort.NodeId, inputPort.PortId, true, false );
								}
							}
							inputPort.InvalidateAllConnections();


							//link input to output
							if( inputPort.ConnectTo( m_wireReferenceUtils.OutputPortReference.NodeId, m_wireReferenceUtils.OutputPortReference.PortId, m_wireReferenceUtils.OutputPortReference.DataType, inputPort.TypeLocked ) )
								targetNode.OnInputPortConnected( inputPort.PortId, m_wireReferenceUtils.OutputPortReference.NodeId, m_wireReferenceUtils.OutputPortReference.PortId );
							//link output to input

							if( outputPort.ConnectTo( inputPort.NodeId, inputPort.PortId, inputPort.DataType, inputPort.TypeLocked ) )
								originNode.OnOutputPortConnected( m_wireReferenceUtils.OutputPortReference.PortId, targetNode.UniqueId, inputPort.PortId );
							m_mainGraphInstance.MarkWireHighlights();
						}
						else if( inputPort != null && inputPort.TypeLocked && inputPort.DataType != m_wireReferenceUtils.OutputPortReference.DataType )
						{
							ShowMessage( "Attempting to connect a " + m_wireReferenceUtils.OutputPortReference.DataType + " to a port locked to type " + inputPort.DataType );
						}
						ShaderIsModified = true;
						SetSaveIsDirty();
					}
					m_wireReferenceUtils.InvalidateReferences();
				}
				else
				{
					if( UIUtils.ShowContextOnPick )
						m_contextPalette.Show( m_currentMousePos2D, m_cameraInfo );
					else
						m_wireReferenceUtils.InvalidateReferences();
				}
			}
			else if( m_currentEvent.modifiers == EventModifiers.Alt && m_altAvailable && CurrentGraph.SelectedNodes.Count == 1 && !m_altBoxSelection && !m_multipleSelectionActive )
			{
				List<WireBezierReference> wireRefs = m_mainGraphInstance.GetWireBezierListInPos( m_currentMousePos2D );
				if( wireRefs != null && wireRefs.Count > 0 )
				{
					float closestDist = 50;
					int closestId = 0;

					for( int i = 0; i < wireRefs.Count; i++ )
					{
						ParentNode outNode = m_mainGraphInstance.GetNode( wireRefs[ i ].OutNodeId );
						ParentNode inNode = m_mainGraphInstance.GetNode( wireRefs[ i ].InNodeId );

						if( outNode == CurrentGraph.SelectedNodes[ 0 ] || inNode == CurrentGraph.SelectedNodes[ 0 ] )
							continue;

						OutputPort outputPort = outNode.GetOutputPortByUniqueId( wireRefs[ i ].OutPortId );
						InputPort inputPort = inNode.GetInputPortByUniqueId( wireRefs[ i ].InPortId );

						// Calculate the 4 points for bezier taking into account wire nodes and their automatic tangents
						Vector3 endPos = new Vector3( inputPort.Position.x, inputPort.Position.y );
						Vector3 startPos = new Vector3( outputPort.Position.x, outputPort.Position.y );

						float mag = ( endPos - startPos ).magnitude;
						float resizedMag = Mathf.Min( mag, Constants.HORIZONTAL_TANGENT_SIZE * m_drawInfo.InvertedZoom );

						Vector3 startTangent = new Vector3( startPos.x + resizedMag, startPos.y );
						Vector3 endTangent = new Vector3( endPos.x - resizedMag, endPos.y );

						if( inNode != null && inNode.GetType() == typeof( WireNode ) )
							endTangent = endPos + ( ( inNode as WireNode ).TangentDirection ) * mag * 0.33f;

						if( outNode != null && outNode.GetType() == typeof( WireNode ) )
							startTangent = startPos - ( ( outNode as WireNode ).TangentDirection ) * mag * 0.33f;

						//Vector2 pos = ( CurrentGraph.SelectedNodes[0].CenterPosition + m_cameraOffset ) / m_cameraZoom;

						float dist = HandleUtility.DistancePointBezier( /*pos*/ m_currentMousePos, startPos, endPos, startTangent, endTangent );
						if( dist < 40 )
						{
							if( dist < closestDist )
							{
								closestDist = dist;
								closestId = i;
							}
						}
					}

					if( closestDist < 40 )
					{
						ParentNode outNode = m_mainGraphInstance.GetNode( wireRefs[ closestId ].OutNodeId );
						ParentNode inNode = m_mainGraphInstance.GetNode( wireRefs[ closestId ].InNodeId );

						OutputPort outputPort = outNode.GetOutputPortByUniqueId( wireRefs[ closestId ].OutPortId );
						InputPort inputPort = inNode.GetInputPortByUniqueId( wireRefs[ closestId ].InPortId );

						ParentNode selectedNode = CurrentGraph.SelectedNodes[ 0 ];
						if( selectedNode.InputPorts.Count > 0 && selectedNode.OutputPorts.Count > 0 )
						{
							RegisterGraphUndo( Constants.UndoCreateConnectionId );

							m_mainGraphInstance.CreateConnection( selectedNode.UniqueId, selectedNode.InputPorts[ 0 ].PortId, outputPort.NodeId, outputPort.PortId );
							m_mainGraphInstance.CreateConnection( inputPort.NodeId, inputPort.PortId, selectedNode.UniqueId, selectedNode.OutputPorts[ 0 ].PortId );
						}

						SetSaveIsDirty();
						ForceRepaint();
					}
				}
			}
			UIUtils.ShowContextOnPick = true;
			m_altBoxSelection = false;
			m_multipleSelectionActive = false;
			UseCurrentEvent();
		}

		public void ConnectInputToOutput( int inNodeId, int inPortId, int outNodeId, int outPortId, bool registerUndo = true )
		{
			ParentNode inNode = m_mainGraphInstance.GetNode( inNodeId );
			ParentNode outNode = m_mainGraphInstance.GetNode( outNodeId );
			if( inNode != null && outNode != null )
			{
				InputPort inPort = inNode.GetInputPortByUniqueId( inPortId );
				OutputPort outPort = outNode.GetOutputPortByUniqueId( outPortId );
				if( inPort != null && outPort != null )
				{
					if( registerUndo )
					{
						RegisterGraphUndo( Constants.UndoCreateConnectionId );
					}

					if( inPort.ConnectTo( outNodeId, outPortId, outPort.DataType, inPort.TypeLocked ) )
					{
						inNode.OnInputPortConnected( inPortId, outNodeId, outPortId );
					}

					if( outPort.ConnectTo( inNodeId, inPortId, inPort.DataType, inPort.TypeLocked ) )
					{
						outNode.OnOutputPortConnected( outPortId, inNodeId, inPortId );
					}
				}
				m_mainGraphInstance.MarkWireHighlights();
				ShaderIsModified = true;
			}
		}

		void OnRightMouseDown()
		{
			Focus();
			m_rmbStartPos = m_currentMousePos2D;
			UseCurrentEvent();
		}

		void OnRightMouseDrag()
		{
			// We look at the control to detect when user hits a tooltip ( which has a hot control of 0 )
			// This needs to be checked because on this first "frame" of hitting a tooltip because it generates incorrect mouse delta values
			if( GUIUtility.hotControl == 0 && m_lastHotControl != 0 )
			{
				m_lastHotControl = GUIUtility.hotControl;
				return;
			}

			m_lastHotControl = GUIUtility.hotControl;
			if( m_currentEvent.alt )
			{
				ModifyZoom( Constants.ALT_CAMERA_ZOOM_SPEED * ( m_currentEvent.delta.x + m_currentEvent.delta.y ), m_altKeyStartPos );
			}
			else
			{
				m_cameraOffset += m_cameraZoom * m_currentEvent.delta;
			}
			UseCurrentEvent();
		}

		void OnRightMouseUp()
		{
			//Resetting the hot control test variable so it can be used again on right mouse drag detection ( if we did not do this then m_lastHotControl could be left with a a value of 0 and wouldn't be able to be correctly used on rthe drag )
			m_lastHotControl = -1;

			if( ( m_rmbStartPos - m_currentMousePos2D ).sqrMagnitude < Constants.RMB_SCREEN_DIST )
			{
				ParentNode node = m_mainGraphInstance.CheckNodeAt( m_currentMousePos, true );
				if( node == null )
				{
					m_contextPalette.Show( m_currentMousePos2D, m_cameraInfo );
				}
				else if( node is PropertyNode propertyNode && PropertyNode.IsChangeableType( propertyNode.GetType() ) )
				{
					ShowPropertyTypeChangeMenu( propertyNode );
				}
			}
			UseCurrentEvent();
		}

		// @diogo: canvas right-click "Change Type" submenu for the interchangeable property nodes ( issue #635 ).
		//         The pick is deferred ( m_pendingTypeChangeType ) since GenericMenu callbacks fire outside OnGUI.
		void ShowPropertyTypeChangeMenu( PropertyNode node )
		{
			GenericMenu menu = new GenericMenu();
			AddPropertyTypeChangeItems( menu, node );
			menu.ShowAsContext();
		}

		/// <summary>
		/// Appends the "Change Type" submenu for the given node (or its changeable selection) to an existing menu.
		/// </summary>
		public void AddPropertyTypeChangeItems( GenericMenu menu, PropertyNode node )
		{
			// A batch applies to every changeable property node in the selection, but only when the clicked node
			// is itself part of that selection ( right-click never changes selection ); otherwise the change is
			// scoped to the clicked node alone.
			List<PropertyNode> changeNodes = new List<PropertyNode>();
			if( node.Selected )
			{
				List<ParentNode> selected = m_mainGraphInstance.SelectedNodes;
				for( int i = 0; i < selected.Count; i++ )
				{
					if( selected[ i ] is PropertyNode selectedProperty && PropertyNode.IsChangeableType( selectedProperty.GetType() ) )
						changeNodes.Add( selectedProperty );
				}
			}
			if( changeNodes.Count < 2 )
			{
				changeNodes.Clear();
				changeNodes.Add( node );
			}

			int[] nodeIds = new int[ changeNodes.Count ];
			for( int i = 0; i < changeNodes.Count; i++ )
				nodeIds[ i ] = changeNodes[ i ].UniqueId;

			bool batch = changeNodes.Count > 1;
			string submenu = batch ? ( "Change Type (" + changeNodes.Count + ")/" ) : "Change Type/";

			for( int i = 0; i < PropertyNode.ChangeableTypes.Length; i++ )
			{
				System.Type targetType = PropertyNode.ChangeableTypes[ i ];

				// Hide a target type only when it would change nothing: the single node's own type, or a batch
				// whose every node is already that type. A mixed batch keeps all types ( there is no one "current" ).
				bool allAlready = true;
				for( int j = 0; j < changeNodes.Count; j++ )
				{
					if( changeNodes[ j ].GetType() != targetType )
					{
						allAlready = false;
						break;
					}
				}
				if( allAlready )
					continue;

				NodeAttributes attributes = m_contextMenu.GetNodeAttributesForType( targetType );
				string label = ( attributes != null ) ? attributes.Name : targetType.Name;
				menu.AddItem( new GUIContent( submenu + label ), false, () =>
				{
					m_pendingTypeChangeNodeIds = nodeIds;
					m_pendingTypeChangeType = targetType;
				} );
			}
		}

		// @diogo: swap one or more property nodes for a different property type in place ( issue #635 ), preserving
		//         each node's name, order index, attributes and output connections. The whole batch is a single undo
		//         step: one snapshot up front, sub-op undo registration suppressed across the loop, panel refresh and
		//         reselection done once at the end.
		public void PerformPropertyTypeChange( int[] sourceNodeIds, System.Type targetType )
		{
			if( sourceNodeIds == null || targetType == null || !PropertyNode.IsChangeableType( targetType ) )
				return;

			// Only nodes that still exist and actually change type; an all-already-target pick must not push an
			// empty undo step.
			List<int> changeIds = new List<int>( sourceNodeIds.Length );
			for( int i = 0; i < sourceNodeIds.Length; i++ )
			{
				PropertyNode source = m_mainGraphInstance.GetNode( sourceNodeIds[ i ] ) as PropertyNode;
				if( source != null && source.GetType() != targetType )
					changeIds.Add( sourceNodeIds[ i ] );
			}
			if( changeIds.Count == 0 )
				return;

			// @diogo: a reference-mode node borrows both the name AND the type of its source; changing a source
			//         without changing the nodes that reference it would leave those references pointing at a node
			//         of the wrong type, dropping every link to None. Pull every dependent reference into the batch
			//         so the links survive the change, but warn first since it changes nodes the user did not pick.
			HashSet<int> changeIdSet = new HashSet<int>( changeIds );
			List<int> dependentReferenceIds = new List<int>();
			List<PropertyNode> rawProperties = m_mainGraphInstance.RawPropertyNodes.NodesList;
			for( int i = 0; i < rawProperties.Count; i++ )
			{
				// @diogo: RawPropertyNodes also holds nodes living inside Function Node interior graphs, and node
				//         ids are per-graph, so an id-set test on those would match unrelated main graph nodes;
				//         only same-graph candidates can genuinely reference a changed node.
				PropertyNode candidate = rawProperties[ i ];
				if( candidate == null || candidate.ContainerGraph != m_mainGraphInstance || !candidate.IsPropertyReference || changeIdSet.Contains( candidate.UniqueId ) )
				{
					continue;
				}

				PropertyNode referenced = candidate.PropertyReference;
				if( referenced != null && changeIdSet.Contains( referenced.UniqueId ) )
				{
					dependentReferenceIds.Add( candidate.UniqueId );
				}
			}

			if( dependentReferenceIds.Count > 0 )
			{
				NodeAttributes targetAttributes = m_contextMenu.GetNodeAttributesForType( targetType );
				string targetName = ( targetAttributes != null ) ? targetAttributes.Name : targetType.Name;
				string countLabel = ( dependentReferenceIds.Count == 1 )
					? "1 linked reference node"
					: dependentReferenceIds.Count + " linked reference nodes";
				string refWord = ( dependentReferenceIds.Count == 1 ) ? "reference" : "references";
				bool proceed = EditorUtility.DisplayDialog( "Change Property Type",
					countLabel + " will also be changed to " + targetName + " to keep the " + refWord + " valid.\n\nContinue?",
					"Continue", "Cancel" );
				if( !proceed )
				{
					return;
				}

				changeIds.AddRange( dependentReferenceIds );
			}

			// Capture each reference node's source id while every source is still alive: a source destroyed
			// earlier in the loop serializes as -1 on a later node ( WritePropertyData resolves through the
			// PropertyReference getter ), which would drop the link order-dependently.
			Dictionary<int, int> referenceSources = new Dictionary<int, int>();
			for( int i = 0; i < changeIds.Count; i++ )
			{
				PropertyNode source = m_mainGraphInstance.GetNode( changeIds[ i ] ) as PropertyNode;
				if( source != null && source.IsPropertyReference )
					referenceSources[ changeIds[ i ] ] = source.ReferencedNodeId;
			}

			RegisterGraphUndo( "Change Property Type" );
			BeginSuppressUndoRegistration();

			// old source id -> new node id, for reselection and reference remapping
			Dictionary<int, int> idMap = new Dictionary<int, int>( changeIds.Count );
			try
			{
				for( int i = 0; i < changeIds.Count; i++ )
				{
					int newId = ChangeSinglePropertyType( changeIds[ i ], targetType );
					if( newId >= 0 )
						idMap[ changeIds[ i ] ] = newId;
				}

				RemapChangedReferences( idMap, referenceSources );
			}
			finally
			{
				EndSuppressUndoRegistration();
			}

			// Reselect the whole batch: converted nodes at their new ids, already-target nodes ( filtered out
			// above ) kept as-is, so a multi-selection stays fully selected after the change.
			List<int> reselectIds = new List<int>( sourceNodeIds.Length );
			for( int i = 0; i < sourceNodeIds.Length; i++ )
			{
				int newId;
				if( idMap.TryGetValue( sourceNodeIds[ i ], out newId ) )
					reselectIds.Add( newId );
				else if( m_mainGraphInstance.GetNode( sourceNodeIds[ i ] ) != null )
					reselectIds.Add( sourceNodeIds[ i ] );
			}
			if( reselectIds.Count > 0 )
				m_mainGraphInstance.SelectNodesFromIds( reselectIds.ToArray() );

			// @diogo: each swap unregisters one property node and registers another ( net-zero count ), so the
			//         count-gated Material Properties / parameters lists never rebuild on their own.
			MasterNode masterNode = m_mainGraphInstance.CurrentMasterNode;
			if( masterNode != null )
			{
				masterNode.InvalidateMaterialPropertyCount();
			}
			m_nodeParametersWindow.InvalidateMaterialPropertyCount();

			ShaderIsModified = true;
			SetSaveIsDirty();
			ForceRepaint();
		}

		// @diogo: single-node core of the change. Returns the new node's id, or -1 if the source is gone, already
		//         the target type, or the new node could not be created. Takes no undo snapshot and does no
		//         selection / panel refresh - the batch driver owns those so the whole batch is one undo step.
		private int ChangeSinglePropertyType( int sourceNodeId, System.Type targetType )
		{
			PropertyNode source = m_mainGraphInstance.GetNode( sourceNodeId ) as PropertyNode;
			if( source == null || source.GetType() == targetType )
				return -1;

			Vector2 position = source.Vec2Position;
			Vector4 defaultValue = source.TypeChangeDefaultValue;
			Vector4 materialValue = source.TypeChangeMaterialValue;
			Vector2 rangeValue = source.TypeChangeRangeValue;
			string propertyData = source.SerializePropertyData();

			PropertyNode target = CreateNode( targetType, position, null, false ) as PropertyNode;
			if( target == null )
				return -1;

			// Move every connected output port onto the matching port ( same id ) on the new node; ports the
			// new type lacks ( e.g. a Vector4 sub-component when changing to Float ) are dropped.
			for( int i = 0; i < source.OutputPorts.Count; i++ )
			{
				OutputPort sourcePort = source.OutputPorts[ i ];
				if( !sourcePort.IsConnected )
					continue;

				OutputPort targetPort = target.GetOutputPortByUniqueId( sourcePort.PortId );
				if( targetPort == null )
					continue;

				WireReference[] connections = sourcePort.ExternalReferences.ToArray();
				for( int c = 0; c < connections.Length; c++ )
				{
					m_mainGraphInstance.CreateConnection( connections[ c ].NodeId, connections[ c ].PortId, target.UniqueId, targetPort.PortId, false );
				}
			}

			// Drop the source ( releases its uniform name ) before the new node claims that same name.
			m_mainGraphInstance.DestroyNode( source, false );

			target.ApplyChangedPropertyData( propertyData );
			target.SetTypeChangeValues( defaultValue, materialValue );
			target.SetTypeChangeRange( rangeValue );
			target.PreviewIsDirty = true;

			return target.UniqueId;
		}

		// @diogo: preserve reference links whose BOTH ends were converted in the batch. referenceSources maps each
		//         converted reference node's old id -> its old source id ( captured before the loop, so a source
		//         destroyed mid-loop doesn't matter ). When the source was converted too, re-point the new reference
		//         node at the new source ( both are now the target type, so SetPropertyReference accepts it ). A
		//         source that wasn't converted keeps its original type, so the link stays None on the type mismatch;
		//         the reference node stays in reference mode either way.
		private void RemapChangedReferences( Dictionary<int, int> idMap, Dictionary<int, int> referenceSources )
		{
			foreach( KeyValuePair<int, int> entry in referenceSources )
			{
				int newReferenceId;
				int newSourceId;
				if( idMap.TryGetValue( entry.Key, out newReferenceId ) && idMap.TryGetValue( entry.Value, out newSourceId ) )
				{
					PropertyNode newReference = m_mainGraphInstance.GetNode( newReferenceId ) as PropertyNode;
					PropertyNode newSource = m_mainGraphInstance.GetNode( newSourceId ) as PropertyNode;
					if( newReference != null && newSource != null && newReference.IsPropertyReference )
						newReference.SetPropertyReference( newSource );
				}
			}
		}

		void UpdateSelectionArea()
		{
			m_multipleSelectionArea.size = TranformedMousePos - m_multipleSelectionStart;
		}

		public void OnValidObjectsDropped( UnityEngine.Object[] droppedObjs )
		{
			bool propagateDraggedObjsToNode = true;
			// Only supporting single drag&drop object selection
			if( droppedObjs.Length == 1 )
			{
				// Material property dragged from the master node Material Properties list
				PropertyNode droppedProperty = droppedObjs[ 0 ] as PropertyNode;
				if( droppedProperty != null )
				{
					CreateNodeReferencingProperty( droppedProperty );
					return;
				}

				// @diogo: dropping a struct definition places the node that uses it, the same way dropping
				// a shader function places a Function Node - the asset is the definition, the node the use
				AmplifyShaderStruct droppedStruct = droppedObjs[ 0 ] as AmplifyShaderStruct;
				if ( droppedStruct != null )
				{
					CreateNodeReferencingStruct( droppedStruct );
					return;
				}

				ShaderIsModified = true;
				SetSaveIsDirty();
				// Check if its a shader, material or game object  and if so load the shader graph code from it
				Shader newShader = droppedObjs[ 0 ] as Shader;
				Material newMaterial = null;
				if( newShader == null )
				{
					newMaterial = droppedObjs[ 0 ] as Material;
					bool isProcedural = ( newMaterial != null );
					if( newMaterial != null && !isProcedural )
					{
						if( UIUtils.IsUnityNativeShader( AssetDatabase.GetAssetPath( newMaterial.shader ) ) )
						{
							return;
						}
						//newShader = newMaterial.shader;
						LoadMaterialToASE( newMaterial );
						//m_mainGraphInstance.UpdateMaterialOnMasterNode( newMaterial );
					}
					else
					{
						GameObject go = droppedObjs[ 0 ] as GameObject;
						if( go != null )
						{
							Renderer renderer = go.GetComponent<Renderer>();
							if( renderer )
							{
								newMaterial = renderer.sharedMaterial;
								newShader = newMaterial.shader;
							}
						}
					}
				}

				if( newShader != null )
				{
					ConvertShaderToASE( newShader );

					propagateDraggedObjsToNode = false;
				}

				// if not shader loading then propagate the seletion to whats below the mouse
				if( propagateDraggedObjsToNode )
				{
					ParentNode node = m_mainGraphInstance.CheckNodeAt( m_currentMousePos );
					if( node != null )
					{
						// if there's a node then pass the object into it to see if there's a setup with it
						node.OnObjectDropped( droppedObjs[ 0 ] );
					}
					else
					{
						// If not then check if there's a node that can be created through the dropped object
						ParentNode newNode = m_contextMenu.CreateNodeFromCastType( droppedObjs[ 0 ].GetType() );
						if( newNode )
						{
							newNode.ContainerGraph = m_mainGraphInstance;
							newNode.Vec2Position = TranformedMousePos;
							m_mainGraphInstance.AddNode( newNode, true );
							newNode.SetupFromCastObject( droppedObjs[ 0 ] );
							m_mainGraphInstance.SelectNode( newNode, false, false );
							ForceRepaint();
							bool find = false;
							if( newNode is FunctionNode && CurrentGraph.CurrentShaderFunction != null )
								find = SearchFunctionNodeRecursively( CurrentGraph.CurrentShaderFunction );

							if( find )
							{
								DestroyNode( newNode, false );
								ShowMessage( "Shader Function loop detected, new node was removed to prevent errors." );
							}
						}
					}
				}
			}
		}

		void CreateNodeReferencingStruct( AmplifyShaderStruct structAsset )
		{
			StructNode node = CreateNode( typeof( StructNode ), TranformedMousePos, selectNode: true ) as StructNode;
			if ( node != null )
			{
				// @diogo: Reference, never a copy of the fields - a dropped asset is meant to stay the one
				// definition, and taking a private copy of it is what Demote to Local is for
				node.SetReferenceTo( structAsset );

				// @diogo: dropping a definition says which struct to make, so the node arrives ready to
				// make one - the empty node from the palette is the one that waits to be told
				node.ExposeAsBuilder();
			}

			ShaderIsModified = true;
			SetSaveIsDirty();
		}

		void CreateNodeReferencingProperty( PropertyNode propertyNode )
		{
			// Dropped property node must belong to the currently edited graph
			if( m_mainGraphInstance.GetNode( propertyNode.UniqueId ) != propertyNode )
				return;

			// Avoid selecting the new node so the master node properties panel stays in place,
			// allowing other entries to be dragged from the Material Properties list in sequence
			if ( propertyNode.CanBeReferenced )
			{
				PropertyNode newProperty = CreateNode( propertyNode.GetType(), TranformedMousePos, selectNode: false ) as PropertyNode;
				newProperty.SetReferenceTo( propertyNode );

				// @diogo: the drop registers then unregisters a property node ( net-zero count ), so the count-gated lists never rebuild
				MasterNode masterNode = m_mainGraphInstance.CurrentMasterNode;
				if ( masterNode != null )
				{
					masterNode.InvalidateMaterialPropertyCount();
				}
				m_nodeParametersWindow.InvalidateMaterialPropertyCount();
			}
			else
			{
				ShowMessage( string.Format( "Property '{0}' doesn't support being referenced", propertyNode.PropertyInspectorName ) );
				return;
			}

			ShaderIsModified = true;
			SetSaveIsDirty();
			ForceRepaint();
		}

		public bool SearchFunctionNodeRecursively( AmplifyShaderFunction function )
		{
			List<FunctionNode> graphList = UIUtils.FunctionList();

			bool nodeFind = false;

			for( int i = 0; i < graphList.Count; i++ )
			{
				ParentGraph temp = CustomGraph;
				CustomGraph = graphList[ i ].FunctionGraph;
				nodeFind = SearchFunctionNodeRecursively( function );
				CustomGraph = temp;

				//Debug.Log( "tested = " + node.Function.FunctionName + " : " + function.FunctionName );

				if( graphList[ i ].Function == function )
					return true;
			}

			return nodeFind;
		}

		public void SetDelayedMaterialMode( Material material )
		{
			if( material == null )
				return;
			m_delayedMaterialSet = material;
		}

		public ShaderLoadResult LoadDroppedObject( bool value, Shader shader, Material material, AmplifyShaderFunction shaderFunction = null )
		{
			UIUtils.CurrentWindow = this;
			ShaderLoadResult result;
			if( shaderFunction != null )
			{
				string assetDatapath = AssetDatabase.GetAssetPath( shaderFunction );
				string latestOpenedFolder = Application.dataPath + assetDatapath.Substring( 6 );
				UIUtils.LatestOpenedFolder = latestOpenedFolder.Substring( 0, latestOpenedFolder.LastIndexOf( '/' ) + 1 );
				result = LoadFromDisk( assetDatapath, shaderFunction );
				CurrentSelection = ASESelectionMode.ShaderFunction;
				IsShaderFunctionWindow = true;
				titleContent.text = GenerateTabTitle( shaderFunction.FunctionName );
				titleContent.image = UIUtils.ShaderFunctionIcon;
				m_lastpath = assetDatapath;
				m_nodeParametersWindow.OnShaderFunctionLoad();
				//EditorPrefs.SetString( IOUtils.LAST_OPENED_OBJ_ID, assetDatapath );
			}
			else if( value && shader != null )
			{
				string assetDatapath = AssetDatabase.GetAssetPath( shader );
				string latestOpenedFolder = Application.dataPath + assetDatapath.Substring( 6 );
				UIUtils.LatestOpenedFolder = latestOpenedFolder.Substring( 0, latestOpenedFolder.LastIndexOf( '/' ) + 1 );
				result = LoadFromDisk( assetDatapath );
				switch( result )
				{
					case ShaderLoadResult.LOADED:
					{
						m_mainGraphInstance.UpdateShaderOnMasterNode( shader );
					}
					break;
					case ShaderLoadResult.ASE_INFO_NOT_FOUND:
					{
						ShowMessage( "Loaded shader wasn't created with ASE. Saving it will remove previous data." );
						UIUtils.CreateEmptyFromInvalid( shader );
					}
					break;
					case ShaderLoadResult.FILE_NOT_FOUND:
					case ShaderLoadResult.UNITY_NATIVE_PATHS:
					{
						UIUtils.CreateEmptyFromInvalid( shader );
					}
					break;
				}

				m_mainGraphInstance.UpdateMaterialOnMasterNode( material );
				m_mainGraphInstance.SetMaterialModeOnGraph( material );

				if( material != null )
				{
					CurrentSelection = ASESelectionMode.Material;
					IsShaderFunctionWindow = false;
					titleContent.text = GenerateTabTitle( material.name );
					titleContent.image = UIUtils.MaterialIcon;
					if( material.HasProperty( IOUtils.DefaultASEDirtyCheckId ) )
					{
						material.SetInt( IOUtils.DefaultASEDirtyCheckId, 1 );
					}
					m_lastpath = AssetDatabase.GetAssetPath( material );
					EditorPrefs.SetString( IOUtils.LAST_OPENED_OBJ_ID, m_lastpath );
				}
				else
				{
					CurrentSelection = ASESelectionMode.Shader;
					IsShaderFunctionWindow = false;
					titleContent.text = GenerateTabTitle( shader.name );
					titleContent.image = UIUtils.ShaderIcon;
					m_lastpath = AssetDatabase.GetAssetPath( shader );
					EditorPrefs.SetString( IOUtils.LAST_OPENED_OBJ_ID, m_lastpath );
				}
			}
			else
			{
				result = ShaderLoadResult.FILE_NOT_FOUND;
			}
			return result;
		}

		bool InsideMenus( Vector2 position )
		{
			for( int i = 0; i < m_registeredMenus.Count; i++ )
			{
				if( m_registeredMenus[ i ].IsInside( position ) )
				{
					return true;
				}
			}
			// @diogo: the console isn't a registered menu, but its message box has to block canvas input
			// all the same or dragging its resize grips also rubber band selects behind it
			return m_consoleLogWindow != null && m_consoleLogWindow.IsInside( position );
		}

		void HandleGUIEvents()
		{
			if( m_currentEvent.type == EventType.KeyDown )
			{
				m_contextMenu.UpdateKeyPress( m_currentEvent.keyCode );
			}
			else if( m_currentEvent.type == EventType.KeyUp )
			{
				m_contextMenu.UpdateKeyReleased( m_currentEvent.keyCode );
			}

			if( InsideMenus( m_currentMousePos2D ) )
			{
				if( m_currentEvent.type == EventType.Used )
					m_mouseDownOnValidArea = false;

				if( m_currentEvent.type == EventType.MouseDown )
				{
					m_mouseDownOnValidArea = false;
					UseCurrentEvent();
				}
				return;
			}
			else if( m_nodeParametersWindow.IsResizing || m_paletteWindow.IsResizing )
			{
				m_mouseDownOnValidArea = false;
			}

			int controlID = GUIUtility.GetControlID( FocusType.Passive );
			switch( m_currentEvent.GetTypeForControl( controlID ) )
			{
				case EventType.MouseDown:
				{
					GUIUtility.hotControl = controlID;
					switch( m_currentEvent.button )
					{
						case ButtonClickId.LeftMouseButton:
						{
							OnLeftMouseDown();
						}
						break;
						case ButtonClickId.RightMouseButton:
						case ButtonClickId.MiddleMouseButton:
						{
							OnRightMouseDown();
						}
						break;
					}
				}
				break;
				case EventType.MouseMove:
				{
					m_keyEvtMousePos2D = m_currentEvent.mousePosition;
				}
				break;
				case EventType.MouseUp:
				{
					GUIUtility.hotControl = 0;
					switch( m_currentEvent.button )
					{
						case ButtonClickId.LeftMouseButton:
						{
							OnLeftMouseUp();
						}
						break;
						case ButtonClickId.MiddleMouseButton: break;
						case ButtonClickId.RightMouseButton:
						{
							OnRightMouseUp();
						}
						break;
					}
				}
				break;
				case EventType.MouseDrag:
				{
					switch( m_currentEvent.button )
					{
						case ButtonClickId.LeftMouseButton:
						{
							OnLeftMouseDrag();
						}
						break;
						case ButtonClickId.MiddleMouseButton:
						case ButtonClickId.RightMouseButton:
						{
							OnRightMouseDrag();
						}
						break;
					}
				}
				break;
				case EventType.ScrollWheel:
				{
					OnScrollWheel();
				}
				break;
				case EventType.KeyDown:
				{
					OnKeyboardDown();
				}
				break;
				case EventType.KeyUp:
				{
					OnKeyboardUp();
				}
				break;
				case EventType.ValidateCommand:
				{
					switch( m_currentEvent.commandName )
					{
						case CopyCommand:
						case PasteCommand:
						case SelectAll:
						case Duplicate:
						{
							m_currentEvent.Use();
						}
						break;
						case ObjectSelectorClosed:
						{
							m_mouseDownOnValidArea = false;
						}
						break;
					}
				}
				break;
				case EventType.ExecuteCommand:
				{
					m_currentEvent.Use();
					switch( m_currentEvent.commandName )
					{
						case CopyCommand:
						{
							CopyToClipboard( true );
						}
						break;
						case PasteCommand:
						{
							PasteFromClipboard( true, true );
						}
						break;
						case SelectAll:
						{
							m_mainGraphInstance.SelectAll();
							ForceRepaint();
						}
						break;
						case Duplicate:
						{
							// @diogo: a duplicate pastes straight back, so it has no reason to replace
							// whatever the user is carrying in the OS clipboard
							CopyToClipboard( false );
							PasteFromClipboard( true, false );
						}
						break;
						case ObjectSelectorClosed:
						{
							m_mouseDownOnValidArea = false;
						}
						break;
					}
				}
				break;
				case EventType.Repaint:
				{
				}
				break;
			}

			m_dragAndDropTool.TestDragAndDrop( m_graphArea );

		}

		public void DeleteConnection( bool isInput, int nodeId, int portId, bool registerOnLog, bool propagateCallback )
		{
			m_mainGraphInstance.DeleteConnection( isInput, nodeId, portId, registerOnLog, propagateCallback );
		}

		void DeleteSelectedNodes()
		{
			if( m_mainGraphInstance.SelectedNodes.Count == 0 )
				return;

			List<ParentNode> selectedNodeList = new List<ParentNode>( m_mainGraphInstance.SelectedNodes.Count );
			foreach( var node in m_mainGraphInstance.SelectedNodes )
			{
				// @diogo: needs work... can't delete single wire nodes
				//if ( node.GetType() == typeof( WireNode ) )
				//{
				//	// @diogo: these seem to be marked for deletion and processed on a late-stage sweep via ParentGraph.UndoableDeleteSelectedNodes()
				//	continue;
				//}

				node.Rewire();
				selectedNodeList.Add( node );
			}

			var selectedNodes = selectedNodeList.ToArray();

			// snapshot the whole graph once before the delete; undo restores it as a unit
			RegisterGraphUndo( Constants.UndoDeleteNodeId );


			// @diogo: unalive all selected nodes first, so we can check against that to prevent double-deleting nodes
			for ( int i = 0; i < selectedNodes.Length; i++ )
			{
				selectedNodes[ i ].Alive = false;
			}

			//Record deleting connections
			for ( int i = 0; i < selectedNodes.Length; i++ )
			{
				m_mainGraphInstance.DeleteAllConnectionFromNode( selectedNodes[ i ], false, true, true );
			}
			//Delete
			m_mainGraphInstance.DeleteNodesOnArray( ref selectedNodes );



			EditorUtility.SetDirty( this );

			ForceRepaint();
		}

		void OnKeyboardUp()
		{
			CheckKeyboardCameraUp();

			if( m_altPressDown )
			{
				m_altPressDown = false;
			}

			if( m_shortcutManager.ActivateShortcut( m_currentEvent.modifiers, m_lastKeyPressed, false ) )
			{
				ForceRepaint();
			}
			m_lastKeyPressed = KeyCode.None;
		}

		bool OnKeyboardPress( KeyCode code )
		{
			return ( m_currentEvent.keyCode == code && m_lastKeyPressed == KeyCode.None );
		}

		void CheckKeyboardCameraDown()
		{
			if( m_contextPalette.IsActive )
				return;
			if( m_currentEvent.alt )
			{
				bool foundKey = false;
				float dir = 0;
				switch( m_currentEvent.keyCode )
				{
					case KeyCode.UpArrow: foundKey = true; dir = 1; break;
					case KeyCode.DownArrow: foundKey = true; dir = -1; break;
					case KeyCode.LeftArrow: foundKey = true; dir = 1; break;
					case KeyCode.RightArrow: foundKey = true; dir = -1; break;
				}
				if( foundKey )
				{
					ModifyZoom( Constants.ALT_CAMERA_ZOOM_SPEED * dir * m_cameraSpeed, new Vector2( m_cameraInfo.width * 0.5f, m_cameraInfo.height * 0.5f ) );
					if( m_cameraSpeed < 15 )
						m_cameraSpeed += 0.2f;
					UseCurrentEvent();
				}


			}
			else
			{
				bool foundKey = false;
				Vector2 dir = Vector2.zero;
				switch( m_currentEvent.keyCode )
				{
					case KeyCode.UpArrow: foundKey = true; dir = Vector2.up; break;
					case KeyCode.DownArrow: foundKey = true; dir = Vector2.down; break;
					case KeyCode.LeftArrow: foundKey = true; dir = Vector2.right; break;
					case KeyCode.RightArrow: foundKey = true; dir = Vector2.left; break;
				}
				if( foundKey )
				{
					m_cameraOffset += m_cameraZoom * m_cameraSpeed * dir;
					if( m_cameraSpeed < 15 )
						m_cameraSpeed += 0.2f;

					UseCurrentEvent();
				}
			}
		}

		void CheckKeyboardCameraUp()
		{
			switch( m_currentEvent.keyCode )
			{
				case KeyCode.UpArrow:
				case KeyCode.DownArrow:
				case KeyCode.LeftArrow:
				case KeyCode.RightArrow: m_cameraSpeed = 1; break;
			}
		}

		void OnKeyboardDown()
		{
			if ( Preferences.User.UpdateOnSceneSave && ( m_currentEvent.control || m_currentEvent.command ) && m_currentEvent.keyCode == KeyCode.S )
			{
				SetCtrlSCallback( false );
				m_currentEvent.Use();
				return;
			}

			//if( DebugConsoleWindow.DeveloperMode )
			//{
			//	if( OnKeyboardPress( KeyCode.F8 ) )
			//	{
			//		Shader currShader = CurrentGraph.CurrentShader;
			//		ShaderUtilEx.OpenCompiledShader( currShader, ShaderInspectorPlatformsPopupEx.GetCurrentMode(), ShaderInspectorPlatformsPopupEx.GetCurrentPlatformMask(), ShaderInspectorPlatformsPopupEx.GetCurrentVariantStripping() == 0 );

			//		string filename = Application.dataPath;
			//		filename = filename.Replace( "Assets", "Temp/Compiled-" );
			//		string shaderFilename = AssetDatabase.GetAssetPath( currShader );
			//		int lastIndex = shaderFilename.LastIndexOf( '/' ) + 1;
			//		filename = filename + shaderFilename.Substring( lastIndex );

			//		string compiledContents = IOUtils.LoadTextFileFromDisk( filename );
			//		Debug.Log( compiledContents );
			//	}

			//	if( OnKeyboardPress( KeyCode.F9 ) )
			//	{
			//		m_nodeExporterUtils.CalculateShaderInstructions( CurrentGraph.CurrentShader );
			//	}
			//}

			CheckKeyboardCameraDown();

			if( m_lastKeyPressed == KeyCode.None )
			{
				m_shortcutManager.ActivateShortcut( m_currentEvent.modifiers, m_currentEvent.keyCode, true );
			}

			if( m_currentEvent.control && m_currentEvent.shift && m_currentEvent.keyCode == KeyCode.V )
			{
				PasteFromClipboard( false, true );
			}

			if( !m_altPressDown && ( OnKeyboardPress( KeyCode.LeftAlt ) || OnKeyboardPress( KeyCode.RightAlt ) || OnKeyboardPress( KeyCode.AltGr ) ) )
			{
				m_altPressDown = true;
				m_altAvailable = true;
				m_altKeyStartPos = m_currentMousePos2D;
			}

			if( m_currentEvent.keyCode != KeyCode.None && m_currentEvent.modifiers == EventModifiers.None )
			{
				m_lastKeyPressed = m_currentEvent.keyCode;
			}

			// MP Modification Begin
			// Custom Horizontal Alignment
			if (m_currentEvent.keyCode == KeyCode.Q)
			{
				Vector2 medianPos = Vector2.zero;
				m_lastKeyPressed = m_currentEvent.keyCode;
				foreach (var node in m_mainGraphInstance.SelectedNodes)
				{
					medianPos += node.Vec2Position;
				}

				medianPos /= m_mainGraphInstance.SelectedNodes.Count;

				foreach (var node in m_mainGraphInstance.SelectedNodes)
				{
					Vector2 cachePos = node.Vec2Position;
					node.Vec2Position = new Vector2(cachePos.x, medianPos.y);
				}
			}

			// Custom Vertical Alignment
			if (m_currentEvent.keyCode == KeyCode.E)
			{
				Vector2 medianPos = Vector2.zero;
				m_lastKeyPressed = m_currentEvent.keyCode;
				foreach (var node in m_mainGraphInstance.SelectedNodes)
				{
					medianPos += node.Vec2Position;
				}

				medianPos /= m_mainGraphInstance.SelectedNodes.Count;

				foreach (var node in m_mainGraphInstance.SelectedNodes)
				{
					Vector2 cachePos = node.Vec2Position;
					node.Vec2Position = new Vector2(medianPos.x, cachePos.y);
				}
			}
			// MP Modification End
		}

		IEnumerator m_coroutine;

		private void StartPasteRequest()
		{
			m_coroutine = SendPostCoroutine( "https://paste.amplify.pt/api/create" );
			EditorApplication.update += PasteRequest;
		}

		IEnumerator SendPostCoroutine( string url )
		{
			WWWForm form = new WWWForm();
			form.AddField( "text", Clipboard.ClipboardId + ";" + EditorPrefs.GetString( Clipboard.ClipboardId, string.Empty ) );
			form.AddField( "title", "ASE Copy" );
			form.AddField( "name", "ASE" );
			form.AddField( "private", "1" );
			form.AddField( "lang", "text" );
			form.AddField( "expire", "0" );

			UnityWebRequest www = UnityWebRequest.Post( url, form );

			www.SendWebRequest();

			yield return www;
		}

		public void PasteRequest()
		{
			UnityWebRequest www = (UnityWebRequest)m_coroutine.Current;
			if( !m_coroutine.MoveNext() && www != null )
			{
				if( !www.isDone )
				{
					m_coroutine.MoveNext();
				}
				else
				{
					if( www.result == UnityWebRequest.Result.ConnectionError )
					{
						Debug.Log( "[AmplifyShaderEditor]\n" + www.error );
					}
					else
					{
						// Print Body
						string finalURL = www.downloadHandler.text;

						if( finalURL.IndexOf( "paste.amplify.pt/view/" ) > -1 )
						{
							System.Text.RegularExpressions.Regex parser = new System.Text.RegularExpressions.Regex( @".*(http:\/\/paste.amplify.pt\/view\/)([0-9a-z]*).*", System.Text.RegularExpressions.RegexOptions.Singleline );
							finalURL = parser.Replace( finalURL, "$1raw/$2" );

							ShowMessage( "Link copied to clipboard\n"+ finalURL, consoleLog:false );
							Debug.Log( "[AmplifyShaderEditor] Link copied to clipboard\n"+ finalURL+"\n" );
							// Copy Paste to clipboard
							EditorGUIUtility.systemCopyBuffer = finalURL;
						}
						else
						{
							Debug.Log( "[AmplifyShaderEditor] Failed to generate paste:\n" + finalURL );
						}
					}
					EditorApplication.update -= PasteRequest;
				}
			}
		}

		void OnScrollWheel()
		{
			ModifyZoomSmooth( m_currentEvent.delta.y, m_currentMousePos2D );
			UseCurrentEvent();
		}

		void ModifyZoom( float zoomIncrement, Vector2 pivot )
		{
			float minCam = Mathf.Min( ( m_cameraInfo.width - ( m_nodeParametersWindow.RealWidth + m_paletteWindow.RealWidth ) ), ( m_cameraInfo.height - ( m_toolsWindow.Height ) ) );
			if( minCam < 1 )
				minCam = 1;

			float dynamicMaxZoom = m_mainGraphInstance.MaxNodeDist / minCam;

			Vector2 canvasPos = TranformPosition( pivot );
			if( zoomIncrement < 0 )
				CameraZoom = Mathf.Max( m_cameraZoom + zoomIncrement * Constants.CAMERA_ZOOM_SPEED, Constants.CAMERA_MIN_ZOOM );
			else if( CameraZoom < Mathf.Max( Constants.CAMERA_MAX_ZOOM, dynamicMaxZoom ) )
				CameraZoom = m_cameraZoom + zoomIncrement * Constants.CAMERA_ZOOM_SPEED;// Mathf.Min( m_cameraZoom + zoomIncrement * Constants.CAMERA_ZOOM_SPEED, Mathf.Max( Constants.CAMERA_MAX_ZOOM, dynamicMaxZoom ) );
			m_cameraOffset.x = pivot.x * m_cameraZoom - canvasPos.x;
			m_cameraOffset.y = pivot.y * m_cameraZoom - canvasPos.y;
		}

		void ModifyZoomSmooth( float zoomIncrement, Vector2 pivot )
		{
			if( m_smoothZoom && Mathf.Sign( m_targetZoomIncrement * zoomIncrement ) >= 0 )
				m_targetZoomIncrement += zoomIncrement;
			else
				m_targetZoomIncrement = zoomIncrement;

			m_smoothZoom = true;
			m_zoomTime = 0;

			float minCam = Mathf.Min( ( m_cameraInfo.width - ( m_nodeParametersWindow.RealWidth + m_paletteWindow.RealWidth ) ), ( m_cameraInfo.height - ( m_toolsWindow.Height ) ) );
			if( minCam < 1 )
				minCam = 1;

			float dynamicMaxZoom = m_mainGraphInstance.MaxNodeDist / minCam;
			if( m_targetZoomIncrement < 0 )
				m_targetZoom = Mathf.Max( m_cameraZoom + m_targetZoomIncrement * Constants.CAMERA_ZOOM_SPEED, Constants.CAMERA_MIN_ZOOM );
			else if( CameraZoom < Mathf.Max( Constants.CAMERA_MAX_ZOOM, dynamicMaxZoom ) )
				m_targetZoom = m_cameraZoom + m_targetZoomIncrement * Constants.CAMERA_ZOOM_SPEED;// Mathf.Min( m_cameraZoom + zoomIncrement * Constants.CAMERA_ZOOM_SPEED, Mathf.Max( Constants.CAMERA_MAX_ZOOM, dynamicMaxZoom ) );

			m_zoomPivot = pivot;
		}

		void OnSelectionChange()
		{
			ForceRepaint();
		}

		private void OnFocus()
		{
			EditorGUI.FocusTextInControl( null );
//			m_fixOnFocus = true;
		}

		void OnLostFocus()
		{
			m_lostFocus = true;
			m_multipleSelectionActive = false;
			m_controlKeyPressed = false;
			m_wireReferenceUtils.InvalidateReferences();
			if( m_genericMessageUI != null )
				m_genericMessageUI.CleanUpMessageStack();
			m_nodeParametersWindow.OnLostFocus();
			m_paletteWindow.OnLostFocus();
			m_contextMenu.ResetShortcutKeyStates();
		}

		void CopyToClipboard( bool mirrorToSystem )
		{
			m_copyPasteDeltaMul = 0;
			m_copyPasteDeltaPos = new Vector2( float.MaxValue, float.MaxValue );
			m_clipboard.ClearClipboard();
			m_copyPasteInitialPos = m_mainGraphInstance.SelectedNodesCentroid;
			m_clipboard.AddToClipboard( m_mainGraphInstance.SelectedNodes, m_copyPasteInitialPos, m_mainGraphInstance, mirrorToSystem );
		}

		static string NodeAvailabilityRequirement( int availabilityFlags )
		{
			List<string> contexts = new List<string>();
			if ( ( availabilityFlags & ( int )NodeAvailability.SurfaceShader ) != 0 )
			{
				contexts.Add( "a Surface shader" );
			}
			if ( ( availabilityFlags & ( int )NodeAvailability.CustomLighting ) != 0 )
			{
				contexts.Add( "a Surface shader using Custom Lighting" );
			}
			if ( ( availabilityFlags & ( int )NodeAvailability.TemplateShader ) != 0 )
			{
				contexts.Add( "a Template shader" );
			}
			// @diogo: function windows accept the full palette, so a Shader Function is always a valid destination
			contexts.Add( "a Shader Function" );
			return string.Join( " or ", contexts );
		}

		// @diogo: effective version boundaries, newest first. Gates mix operators, so a boundary is not the
		// literal in the gate: ">= N" breaks at N, "> N" at N + 1, "< N" at N, "<= N" at N + 1. Versions
		// between two boundaries read identically, so one candidate per boundary covers the whole range.
		// 19913 is absent on purpose, since it is the build that started stamping and unstamped data is
		// therefore older. Regenerate this when a release adds a UIUtils.CurrentShaderVersion() gate.
		private static readonly int[] ClipboardVersionCandidates =
		{
			19910, 19906, 19905, 19902, 19901, 19801, 19702, 19500, 19104, 19100, 19000
		};

		// @diogo: clipboard data written before stamping says nothing about its own layout, so the layout is
		// found by trial fit: read every node at each candidate and keep the newest one they all agree with.
		// Returns 0 when nothing fits, leaving the caller on the current build's format.
		private int InferClipboardVersion()
		{
			int nodeCount = m_clipboard.CurrentClipboardStrData.Count;
			string[][] payloads = new string[ nodeCount ][];
			for ( int i = 0; i < nodeCount; i++ )
			{
				payloads[ i ] = JsonGraphFormat.SplitInstruction( m_clipboard.CurrentClipboardStrData[ i ].Data );
			}

			int inferredVersion = 0;
			// @diogo: one entry per rejected candidate, kept for the diagnostic below since the probe is
			// otherwise silent and there is no other way to tell inference apart from its fallback
			List<string> rejections = new List<string>();
			int cachedVersion = m_mainGraphInstance.LoadedShaderVersion;
			int cachedInstancePropertyCount = m_mainGraphInstance.InstancePropertyCount;
			// @diogo: node reads can write graph wide settings; master nodes are the ones that do and are
			// never probed, but the probe has to leave nothing behind whatever a future node type touches
			NodeAvailability cachedCanvasMode = m_mainGraphInstance.CurrentCanvasMode;
			PrecisionType cachedPrecision = m_mainGraphInstance.CurrentPrecision;
			TemplateSRPType cachedSRPType = m_mainGraphInstance.CurrentSRPType;
			bool cachedInhibitMessages = UIUtils.InhibitMessages;
			bool cachedLogEnabled = Debug.unityLogger.logEnabled;
			// @diogo: every rejected candidate is a deliberately failed read, so the console and the canvas
			// have to stay quiet until the winner is picked
			UIUtils.InhibitMessages = true;
			Debug.unityLogger.logEnabled = false;
			try
			{
				// @diogo: read without consuming, since no live node holds it and the probe must not shift ids
				int scratchId = m_mainGraphInstance.NextValidId;
				for ( int c = 0; c < ClipboardVersionCandidates.Length && inferredVersion == 0; c++ )
				{
					m_mainGraphInstance.LoadedShaderVersion = ClipboardVersionCandidates[ c ];
					bool fits = true;
					// @diogo: all nodes share one version, so the first misfit settles the candidate
					for ( int i = 0; i < nodeCount && fits; i++ )
					{
						fits = ClipboardNodeFitsVersion( payloads[ i ], scratchId, out string rejection );
						if ( !fits )
						{
							rejections.Add( ClipboardVersionCandidates[ c ] + ": " + rejection );
						}
					}
					if ( fits )
					{
						inferredVersion = ClipboardVersionCandidates[ c ];
					}
				}
			}
			catch ( Exception )
			{
				// @diogo: inference is best effort and deliberately reads data at versions that never wrote
				// it, so nothing thrown here may cost a paste that would otherwise have gone through
				inferredVersion = 0;
			}
			finally
			{
				Debug.unityLogger.logEnabled = cachedLogEnabled;
				UIUtils.InhibitMessages = cachedInhibitMessages;
				m_mainGraphInstance.CurrentSRPType = cachedSRPType;
				m_mainGraphInstance.CurrentPrecision = cachedPrecision;
				m_mainGraphInstance.CurrentCanvasMode = cachedCanvasMode;
				m_mainGraphInstance.InstancePropertyCount = cachedInstancePropertyCount;
				m_mainGraphInstance.LoadedShaderVersion = cachedVersion;
			}

			// @diogo: has to come after the restore above, which is what re-enables logging
			if ( Preferences.User.GraphLogToConsole )
			{
				string outcome = inferredVersion > 0 ? "read as version " + inferredVersion : "not recognized, reading it with the current format";
				string detail = rejections.Count > 0 ? "\n  " + string.Join( "\n  ", rejections ) : string.Empty;
				Debug.Log( "[AmplifyShaderEditor] Unstamped clipboard data " + outcome + detail );
			}
			return inferredVersion;
		}

		// @diogo: deserializes one node purely to see whether the current version reads its fields cleanly.
		// The node never joins the graph: AddNode would assign the id a second time, and each assignment
		// subscribes handlers that Destroy only unwinds once, while DestroyNode evicts from the node
		// dictionary by id and would take the live node holding that id with it.
		private bool ClipboardNodeFitsVersion( string[] parameters, int scratchId, out string rejection )
		{
			rejection = null;
			System.Type nodeType = System.Type.GetType( parameters[ IOUtils.NodeTypeId ] );
			if ( nodeType == null )
			{
				return true;
			}

			// @diogo: reading a master node writes canvas mode, precision and SRP type straight onto the live
			// graph and can even queue a template swap, all of it outside the read's own catch, so one is
			// never deserialized here; its verdict would be discarded anyway
			if ( typeof( MasterNode ).IsAssignableFrom( nodeType ) )
			{
				return true;
			}

			// @diogo: every version gate a function node reads through predates 19000, so its verdict cannot
			// vary across candidates and probing it only pays for a full sub graph load and teardown, which
			// also frees uniform names out of the window wide register that live nodes share
			if ( typeof( FunctionNode ).IsAssignableFrom( nodeType ) )
			{
				return true;
			}

			if ( !( ScriptableObject.CreateInstance( nodeType ) is ParentNode probeNode ) )
			{
				return true;
			}

			try
			{
				probeNode.IsNodeBeingCopied = true;
				probeNode.ContainerGraph = m_mainGraphInstance;
				if ( !probeNode.ClipboardFullReadFromString( ref parameters ) )
				{
					rejection = nodeType.Name + " threw while reading";
					return false;
				}
				if ( probeNode.SilentReadFailure )
				{
					rejection = nodeType.Name + " ran out of fields";
					return false;
				}
				if ( probeNode.ReadParamIdx != parameters.Length )
				{
					rejection = nodeType.Name + " read " + probeNode.ReadParamIdx + " of " + parameters.Length + " fields";
					return false;
				}
				return true;
			}
			finally
			{
				// @diogo: releases keyed on the id would otherwise free the live node that owns it
				probeNode.SetBaseUniqueId( scratchId );
				probeNode.Destroy();
				DestroyImmediate( probeNode );
			}
		}

		ParentNode CreateNodeFromClipboardData( int clipId, List<string> unavailableNodeNames, List<string> failedNodeNames )
		{
			string[] parameters = JsonGraphFormat.SplitInstruction( m_clipboard.CurrentClipboardStrData[ clipId ].Data );
			System.Type nodeType = System.Type.GetType( parameters[ IOUtils.NodeTypeId ] );
			NodeAttributes attributes = m_contextMenu.GetNodeAttributesForType( nodeType );
			// @diogo: function windows list every node in the palette, so paste must not re-filter by availability there
			if( attributes != null && !IsShaderFunctionWindow && !UIUtils.GetNodeAvailabilityInBitArray( attributes.NodeAvailabilityFlags, m_mainGraphInstance.CurrentCanvasMode ) && !UIUtils.GetNodeAvailabilityInBitArray( attributes.NodeAvailabilityFlags, m_currentNodeAvailability ) )
			{
				string entry = attributes.Name + " (requires " + NodeAvailabilityRequirement( attributes.NodeAvailabilityFlags ) + ")";
				if ( !unavailableNodeNames.Contains( entry ) )
				{
					unavailableNodeNames.Add( entry );
				}
				return null;
			}

			ParentNode newNode = (ParentNode)ScriptableObject.CreateInstance( nodeType );
			if( newNode != null )
			{
				newNode.IsNodeBeingCopied = true;
				newNode.ContainerGraph = m_mainGraphInstance;
				bool readSuccess = newNode.ClipboardFullReadFromString( ref parameters );
				m_mainGraphInstance.AddNode( newNode, true, true, true, false );
				newNode.IsNodeBeingCopied = false;
				if ( !readSuccess )
				{
					// @diogo: a partially read node is kept anyway; dropping it costs the user more than the
					// wrong settings do, so it is only flagged by type in the warning below
					string entry = attributes != null ? attributes.Name : nodeType.Name;
					if ( !failedNodeNames.Contains( entry ) )
					{
						failedNodeNames.Add( entry );
					}
				}
				m_clipboard.CurrentClipboardStrData[ clipId ].NewNodeId = newNode.UniqueId;
				return newNode;
			}
			return null;
		}

		void CreateConnectionsFromClipboardData( int clipId )
		{
			if( String.IsNullOrEmpty( m_clipboard.CurrentClipboardStrData[ clipId ].Connections ) )
				return;
			string[] lines = m_clipboard.CurrentClipboardStrData[ clipId ].Connections.Split( IOUtils.LINE_TERMINATOR );

			for( int lineIdx = 0; lineIdx < lines.Length; lineIdx++ )
			{
				string[] parameters = JsonGraphFormat.SplitInstruction( lines[ lineIdx ] );

				int InNodeId = 0;
				int InPortId = 0;
				int OutNodeId = 0;
				int OutPortId = 0;

				try
				{
					InNodeId = Convert.ToInt32( parameters[ IOUtils.InNodeId ] );
					InPortId = Convert.ToInt32( parameters[ IOUtils.InPortId ] );

					OutNodeId = Convert.ToInt32( parameters[ IOUtils.OutNodeId ] );
					OutPortId = Convert.ToInt32( parameters[ IOUtils.OutPortId ] );
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}


				int newInNodeId = m_clipboard.GeNewNodeId( InNodeId );
				int newOutNodeId = m_clipboard.GeNewNodeId( OutNodeId );

				if( newInNodeId > -1 && newOutNodeId > -1 )
				{
					ParentNode inNode = m_mainGraphInstance.GetNode( newInNodeId );
					ParentNode outNode = m_mainGraphInstance.GetNode( newOutNodeId );

					InputPort inputPort = null;
					OutputPort outputPort = null;

					if( inNode != null && outNode != null )
					{
						inNode.IsNodeBeingCopied = true;
						outNode.IsNodeBeingCopied = true;
						inputPort = inNode.GetInputPortByUniqueId( InPortId );
						outputPort = outNode.GetOutputPortByUniqueId( OutPortId );
						if( inputPort != null && outputPort != null )
						{
							// @diogo: a locked input refuses the wire, so skip the output side too, or the
							// graph keeps a one-way reference that no repair pass can see
							if ( inputPort.ConnectTo( newOutNodeId, OutPortId, outputPort.DataType, false ) )
							{
								outputPort.ConnectTo( newInNodeId, InPortId, inputPort.DataType, inputPort.TypeLocked );

								inNode.OnInputPortConnected( InPortId, newOutNodeId, OutPortId );
								outNode.OnOutputPortConnected( OutPortId, newInNodeId, InPortId );
							}
						}

						inNode.IsNodeBeingCopied = false;
						outNode.IsNodeBeingCopied = false;
					}
				}
			}
		}

		private void StartGetRequest( string url )
		{
			m_coroutine = SendGetCoroutine( url );
			EditorApplication.update += GetRequest;
		}

		IEnumerator SendGetCoroutine( string url )
		{
			UnityWebRequest www = UnityWebRequest.Get( url );
			www.SendWebRequest();
			yield return www;
		}

		public void GetRequest()
		{
			UnityWebRequest www = (UnityWebRequest)m_coroutine.Current;
			if( !m_coroutine.MoveNext() )
			{
				if( !www.isDone )
				{
					m_coroutine.MoveNext();
				}
				else
				{
					if( www.result == UnityWebRequest.Result.ConnectionError )
					{
						Debug.Log( "[AmplifyShaderEditor]\n" + www.error );
					}
					else
					{
						string data = www.downloadHandler.text;
						if( data.IndexOf( Clipboard.ClipboardId + ";" ) > -1 )
						{
							data = www.downloadHandler.text.Replace( Clipboard.ClipboardId + ";", "" );
							if( data.IndexOf( "<div " ) > -1 )
							{
								System.Text.RegularExpressions.Regex parser = new System.Text.RegularExpressions.Regex( @"(.*)<div .*", System.Text.RegularExpressions.RegexOptions.Singleline );
								data = parser.Replace( data, "$1" );
							}
							EditorGUIUtility.systemCopyBuffer = string.Empty;
							Debug.Log( "[AmplifyShaderEditor] Successfully downloaded snippet!" );
							EditorPrefs.SetString( Clipboard.ClipboardId, data );
							try
							{
								// send paste event instead to make sure it runs properly
								Event e = new Event();
								e.type = EventType.ExecuteCommand;
								e.commandName = PasteCommand;
								this.SendEvent( e );
							}
							catch( Exception )
							{
								EditorGUIUtility.systemCopyBuffer = string.Empty;
								EditorApplication.update -= GetRequest;
								throw;
							}
						}
						else
						{
							Debug.Log( "[AmplifyShaderEditor] Error downloading, snippet might not exist anymore, clearing clipboard..." );
							EditorGUIUtility.systemCopyBuffer = string.Empty;
						}
					}
					EditorApplication.update -= GetRequest;
				}
			}
		}

		void ReconnectClipboardReferences( List<ParentNode> createdNodes )
		{
			// @diogo: Restore reference connections (e.g. Switch node) lost due to new UniqueIDs
			foreach ( ParentNode node in createdNodes )
			{
				node.ReconnectClipboardReferences( m_clipboard );
			}

		}

		// fromSystem consults the OS clipboard, so a payload copied elsewhere wins over the last local
		// copy. A duplicate reads only what it just wrote, and must not see an older copy sitting there.
		void PasteFromClipboard( bool copyConnections, bool fromSystem )
		{
			if ( fromSystem )
			{
				// @diogo: CRLF comes from text editors and chat clients a shared payload passes through;
				// node data is JSON-escaped, so a literal CR in it is always transport damage
				string result = EditorGUIUtility.systemCopyBuffer.Replace( "\r\n", "\n" );

				// @diogo: the scheme upgrade covers legacy share links and has to stay scoped to them,
				// since a copied payload can carry an http:// of its own inside user text
				if ( result.IndexOf( "https://paste.amplify.pt/view/raw/" ) > -1 || result.IndexOf( "http://paste.amplify.pt/view/raw/" ) > -1 )
				{
					StartGetRequest( result.Replace( "http://", "https://" ) );
					return;
				}

				if( result.IndexOf( Clipboard.ClipboardId + ";" ) > -1 )
				{
					result = result.Replace( Clipboard.ClipboardId + ";", "" );
					EditorPrefs.SetString( Clipboard.ClipboardId, result );
				}
			}

			// IsDuplicating gates master-node reassignment on load ( OutputNode.ReadFromString ), so it
			// must always be cleared: a leaked true makes every later snapshot-undo reload skip
			// AssignMasterNode and leave the graph with no master. Reset in finally to cover both the
			// empty-clipboard early return and any exception in the paste body.
			int loadedVersion = m_mainGraphInstance.LoadedShaderVersion;
			m_mainGraphInstance.IsDuplicating = true;
			try
			{
				m_copyPasteInitialPos = m_clipboard.GetDataFromEditorPrefs();
				if( m_clipboard.CurrentClipboardStrData.Count == 0 )
				{
					return;
				}

				// @diogo: node data has to be read with the gates of the version that wrote it; clipboards from
				// before stamping carry none, so their layout is inferred and only then falls back to this
				// build's format
				if ( m_clipboard.ClipboardVersion > 0 )
				{
					m_mainGraphInstance.LoadedShaderVersion = m_clipboard.ClipboardVersion;
				}
				else
				{
					int inferredVersion = InferClipboardVersion();
					if ( inferredVersion > 0 )
					{
						m_mainGraphInstance.LoadedShaderVersion = inferredVersion;
					}
				}

				Vector2 deltaPos = TranformedKeyEvtMousePos - m_copyPasteInitialPos;
				if( ( m_copyPasteDeltaPos - deltaPos ).magnitude > 5.0f )
				{
					m_copyPasteDeltaMul = 0;
				}
				else
				{
					m_copyPasteDeltaMul += 1;
				}
				m_copyPasteDeltaPos = deltaPos;

				m_mainGraphInstance.DeSelectAll();
				UIUtils.InhibitMessages = true;

				if( m_clipboard.CurrentClipboardStrData.Count > 0 )
				{
					RegisterGraphUndo( Constants.UndoPasteNodeId );
				}

				List<ParentNode> createdNodes = new List<ParentNode>();
				List<string> unavailableNodeNames = new List<string>();
				List<string> failedNodeNames = new List<string>();
				for( int i = 0; i < m_clipboard.CurrentClipboardStrData.Count; i++ )
				{
					ParentNode node = CreateNodeFromClipboardData( i, unavailableNodeNames, failedNodeNames );
					if( node != null )
					{
						m_clipboard.CurrentClipboardStrData[ i ].NewNodeId = node.UniqueId;
						Vector2 pos = node.Vec2Position;
						node.Vec2Position = pos + deltaPos + m_copyPasteDeltaMul * Constants.CopyPasteDeltaPos;
						//node.RefreshExternalReferences();
						node.AfterDuplication();
						createdNodes.Add( node );
						m_mainGraphInstance.SelectNode( node, true, false );
					}
				}

				if( copyConnections )
				{
					for( int i = 0; i < m_clipboard.CurrentClipboardStrData.Count; i++ )
					{
						CreateConnectionsFromClipboardData( i );
					}

					ReconnectClipboardReferences( createdNodes );
				}

				// Refresh external references must always be called after all nodes are created
				for( int i = 0; i < createdNodes.Count; i++ )
				{
					createdNodes[ i ].RefreshExternalReferences();
				}
				createdNodes.Clear();
				createdNodes = null;
				//Need to force increment on Undo because if not Undo may incorrectly group consecutive pastes
				UndoUtils.IncrementCurrentGroup();

				UIUtils.InhibitMessages = false;
				if( unavailableNodeNames.Count > 0 )
				{
					ShowMessage( "Unable to paste: " + string.Join( ", ", unavailableNodeNames ), MessageSeverity.Warning );
				}
				if ( failedNodeNames.Count > 0 )
				{
					ShowMessage( "Pasted " + string.Join( ", ", failedNodeNames ) + " from a different ASE version; some settings may be wrong.", MessageSeverity.Warning );
				}
				ShaderIsModified = true;
				SetSaveIsDirty();
				ForceRepaint();
			}
			finally
			{
				// @diogo: the clear above runs before the warnings so they are not swallowed; this one covers
				// the paths that never reach it, since a leaked true mutes every later canvas message
				UIUtils.InhibitMessages = false;
				m_mainGraphInstance.IsDuplicating = false;
				m_mainGraphInstance.LoadedShaderVersion = loadedVersion;
			}
		}

		public string GenerateGraphInfo()
		{
			EditorPrefs.SetString( GUID,
							m_cameraInfo.x.ToString() + IOUtils.FIELD_SEPARATOR +
							m_cameraInfo.y.ToString() + IOUtils.FIELD_SEPARATOR +
							m_cameraInfo.width.ToString() + IOUtils.FIELD_SEPARATOR +
							m_cameraInfo.height.ToString() + IOUtils.FIELD_SEPARATOR +
							m_cameraOffset.x.ToString() + IOUtils.FIELD_SEPARATOR +
							m_cameraOffset.y.ToString() + IOUtils.FIELD_SEPARATOR +
							m_cameraZoom.ToString() + IOUtils.FIELD_SEPARATOR +
							m_nodeParametersWindow.IsMaximized + IOUtils.FIELD_SEPARATOR +
							m_paletteWindow.IsMaximized + '\n' );

			string graphInfo = IOUtils.ShaderBodyBegin + '\n';
			string nodesInfo = "";
			string connectionsInfo = "";

			graphInfo += VersionInfo.FullLabel + '\n';

			m_mainGraphInstance.WriteToStringDepthOrdered( ref nodesInfo, ref connectionsInfo );

			graphInfo += nodesInfo;
			graphInfo += connectionsInfo;
			graphInfo += IOUtils.ShaderBodyEnd + '\n';

			return graphInfo;
		}

		// @diogo: per-instruction node-creation interpreter, shared with the ( upcoming ) incremental undo patcher
		private static void CreateNodeFromInstruction( ParentGraph graph, GraphContextMenu contextMenu, ref string[] parameters, System.Diagnostics.Stopwatch bucketTimer, bool fetchMaterialValues = true )
		{
			bucketTimer.Restart();
			System.Type type = IOUtils.GetNodeType( parameters[ IOUtils.NodeTypeId ] );
			if( type != null )
			{
				System.Type oldType = type;
				NodeAttributes attribs = contextMenu.GetNodeAttributesForType( type );
				if( attribs == null )
				{
					attribs = contextMenu.GetDeprecatedNodeAttributesForType( type );
					if( attribs != null )
					{
						if( attribs.Deprecated && attribs.DeprecatedAlternativeType != null )
						{
							type = attribs.DeprecatedAlternativeType;
							//ShowMessage( string.Format( "Node {0} is deprecated and was replaced by {1} ", attribs.Name, attribs.DeprecatedAlternative ) );
						}
					}
				}
				bucketTimer.Stop();
				UndoProfiler.AddLoadTypeResolve( bucketTimer.Elapsed.TotalMilliseconds );

				bucketTimer.Restart();
				ParentNode newNode = (ParentNode)ScriptableObject.CreateInstance( type );
				bucketTimer.Stop();
				UndoProfiler.AddLoadCreate( bucketTimer.Elapsed.TotalMilliseconds );
				if( newNode != null )
				{
					try
					{
						newNode.ContainerGraph = graph;
						if( oldType != type )
						{
							newNode.ParentReadFromString( ref parameters );
							newNode.ReadFromDeprecated( ref parameters, oldType );
							newNode.WasDeprecated = true;
						}
						else
							newNode.ReadFromString( ref parameters );

						if( oldType == type )
						{
							newNode.ReadInputDataFromString( ref parameters );
							if( UIUtils.CurrentShaderVersion() > 5107 )
							{
								newNode.ReadOutputDataFromString( ref parameters );
							}
						}
					}
					catch( Exception e )
					{
						Debug.LogException( e, newNode );
					}

					// @diogo: shader function graphs load through here, so a retired SF nested in another SF
					// was left as a Missing Function
					newNode = ShaderFunctionToNode.ConvertIfEligible( newNode );

					bucketTimer.Restart();
					graph.AddNode( newNode, false, true, false, fetchMaterialValues );
					bucketTimer.Stop();
					UndoProfiler.AddLoadAddNode( bucketTimer.Elapsed.TotalMilliseconds );
					UndoProfiler.AddLoadNodes( 1 );
				}
			}
			else
			{
				bucketTimer.Stop();
				UndoProfiler.AddLoadTypeResolve( bucketTimer.Elapsed.TotalMilliseconds );
				UIUtils.ShowMessage( string.Format( "{0} is not a valid ASE node ", parameters[ IOUtils.NodeTypeId ] ), MessageSeverity.Error );
			}
		}

		// @diogo: per-instruction wire-creation interpreter, shared with the ( upcoming ) incremental undo patcher
		private static void ApplyWireInstruction( ParentGraph graph, ref string[] parameters, System.Diagnostics.Stopwatch bucketTimer )
		{
			bucketTimer.Restart();
			int InNodeId = 0;
			int InPortId = 0;
			int OutNodeId = 0;
			int OutPortId = 0;

			try
			{
				InNodeId = Convert.ToInt32( parameters[ IOUtils.InNodeId ] );
				InPortId = Convert.ToInt32( parameters[ IOUtils.InPortId ] );
				OutNodeId = Convert.ToInt32( parameters[ IOUtils.OutNodeId ] );
				OutPortId = Convert.ToInt32( parameters[ IOUtils.OutPortId ] );
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}

			ParentNode inNode = graph.GetNode( InNodeId );
			ParentNode outNode = graph.GetNode( OutNodeId );

			//if ( UIUtils.CurrentShaderVersion() < 5002 )
			//{
			//	InPortId = inNode.VersionConvertInputPortId( InPortId );
			//	OutPortId = outNode.VersionConvertOutputPortId( OutPortId );
			//}

			InputPort inputPort = null;
			OutputPort outputPort = null;
			if( inNode != null && outNode != null )
			{

				if( UIUtils.CurrentShaderVersion() < 5002 )
				{
					InPortId = inNode.VersionConvertInputPortId( InPortId );
					OutPortId = outNode.VersionConvertOutputPortId( OutPortId );

					if( inNode.WasDeprecated )
						InPortId = inNode.InputIdFromDeprecated( InPortId );
					if( outNode.WasDeprecated )
						OutPortId = outNode.OutputIdFromDeprecated( OutPortId );

					inputPort = inNode.GetInputPortByArrayId( InPortId );
					outputPort = outNode.GetOutputPortByArrayId( OutPortId );
				}
				else
				{
					if( inNode.WasDeprecated )
						InPortId = inNode.InputIdFromDeprecated( InPortId );
					if( outNode.WasDeprecated )
						OutPortId = outNode.OutputIdFromDeprecated( OutPortId );

					inputPort = inNode.GetInputPortByUniqueId( InPortId );
					outputPort = outNode.GetOutputPortByUniqueId( OutPortId );
				}

				if( inputPort != null && outputPort != null )
				{
					// @diogo: as on an interactive connection, a free-for-all end takes the wire's type, so
					// the type it was saved with is no contract to check against
					bool checkTypes = inputPort.NotFreeForAllTypes && outputPort.NotFreeForAllTypes;
					bool inputCompatible = !checkTypes || inputPort.CheckValidType( outputPort );
					bool outputCompatible = !checkTypes || outputPort.CheckValidType( inputPort );
					if( inputCompatible && outputCompatible )
					{
						// @diogo: a locked input refuses the wire, so skip the output side too, or the
						// graph keeps a one-way reference that no repair pass can see
						if ( inputPort.ConnectTo( OutNodeId, OutPortId, outputPort.DataType, false ) )
						{
							outputPort.ConnectTo( InNodeId, InPortId, inputPort.DataType, inputPort.TypeLocked );

							inNode.OnInputPortConnected( InPortId, OutNodeId, OutPortId, false );
							outNode.OnOutputPortConnected( OutPortId, InNodeId, InPortId );
						}
					}
					else if ( outputPort.DataType == WirePortDataType.OBJECT )
					{
						// @diogo: a wildcard producer, e.g. Get Local Var, only learns its type in the post-load refresh
						graph.DeferWire( inputPort, outputPort );
					}
					else if( DebugConsoleWindow.DeveloperMode )
					{
						if( !inputCompatible )
							UIUtils.ShowIncompatiblePortMessage( true, inNode, inputPort, outNode, outputPort );

						if( !outputCompatible )
							UIUtils.ShowIncompatiblePortMessage( true, outNode, outputPort, inNode, inputPort );
					}
				}
				else if( DebugConsoleWindow.DeveloperMode )
				{
					if( inputPort == null )
					{
						UIUtils.ShowMessage( "Input Port " + InPortId + " doesn't exist on node " + InNodeId, MessageSeverity.Error );
					}
					else
					{
						UIUtils.ShowMessage( "Output Port " + OutPortId + " doesn't exist on node " + OutNodeId, MessageSeverity.Error );
					}
				}
			}
			else if( DebugConsoleWindow.DeveloperMode )
			{
				if( inNode == null )
				{
					UIUtils.ShowMessage( "Input node " + InNodeId + " doesn't exist", MessageSeverity.Error );
				}
				else
				{
					UIUtils.ShowMessage( "Output node " + OutNodeId + " doesn't exist", MessageSeverity.Error );
				}
			}
			bucketTimer.Stop();
			UndoProfiler.AddLoadWire( bucketTimer.Elapsed.TotalMilliseconds );
		}

		// TODO: this need to be fused to the main load function somehow
		public static void LoadFromMeta( ref ParentGraph graph, GraphContextMenu contextMenu, string meta, bool useParsedCache = false )
		{
			// @diogo: reentrant via FunctionNode; depth tracking accumulates bucket timings across nested calls
			UndoProfiler.BeginLoadFromMeta();

			// @diogo: loading is mechanical - nothing inside it may register undo steps
			AmplifyShaderEditorWindow undoSuppressWindow = graph != null ? graph.ParentWindow : null;
			if ( undoSuppressWindow != null )
			{
				undoSuppressWindow.BeginSuppressUndoRegistration();
			}
			try
			{
				System.Diagnostics.Stopwatch bucketTimer = new System.Diagnostics.Stopwatch();

				graph.IsLoading = true;
				bucketTimer.Restart();
				graph.CleanNodes();
				bucketTimer.Stop();
				UndoProfiler.AddLoadClean( bucketTimer.Elapsed.TotalMilliseconds );

				string[] cameraParams = new string[ 0 ];
				// @diogo: nested shader functions pass their asset text, which the cache parses once per distinct
				// text; snapshot restores keep parsing since no two snapshots share one (docs/shader-function-load-cache.md)
				ParsedGraphMeta parsed = useParsedCache ? ShaderFunctionMetaCache.Get( meta ) : ShaderFunctionMetaCache.Parse( meta );
				if( parsed != null )
				{
					if( parsed.HasVersion )
					{
						graph.LoadedShaderVersion = parsed.Version;
					}

					// All nodes must be created before wiring the connections ...
					// Since all nodes on the save op are written before the wires, we can safely create them
					// If that order is not maintained the it's because of external editing and its the users responsability
					for( int instructionIdx = 0; instructionIdx < parsed.Instructions.Count; instructionIdx++ )
					{
						// @diogo: a copy per use, so a reader writing into its parameters cannot poison the cache
						string[] parameters = ( string[] )parsed.Instructions[ instructionIdx ].Clone();
						JsonGraphFormat.LastInstructionFromJson = parsed.FromJson[ instructionIdx ];
						switch( parameters[ 0 ] )
						{
							case IOUtils.NodeParam:
							{
								CreateNodeFromInstruction( graph, contextMenu, ref parameters, bucketTimer );
							}
							break;
							case IOUtils.WireConnectionParam:
							{
								ApplyWireInstruction( graph, ref parameters, bucketTimer );
							}
							break;
						}
					}
				}

				bucketTimer.Restart();
				graph.CheckForDuplicates();
				graph.UpdateRegisters();
				graph.RefreshExternalReferences();
				graph.ApplyDeferredWires();
				graph.ForceSignalPropagationOnMasterNode();
				bucketTimer.Stop();
				UndoProfiler.AddLoadPost( bucketTimer.Elapsed.TotalMilliseconds );
				graph.LoadedShaderVersion = VersionInfo.FullNumber;
				//Reset();
				graph.IsLoading = false;
			}
			finally
			{
				if ( undoSuppressWindow != null )
				{
					undoSuppressWindow.EndSuppressUndoRegistration();
				}
				UndoProfiler.EndLoadFromMeta();
			}
		}

		public ShaderLoadResult LoadFromDisk( string pathname, AmplifyShaderFunction shaderFunction = null )
		{
			var timer = new System.Diagnostics.Stopwatch();
			timer.Start();

			m_mainGraphInstance.IsLoading = true;
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			FullCleanUndoStack();

			InlinePropertyTable.Initialize();

			UIUtils.DirtyMask = false;
			if( UIUtils.IsUnityNativeShader( pathname ) )
			{
				ShowMessage( "Cannot edit native unity shaders.\nReplacing by a new one." );
				return ShaderLoadResult.UNITY_NATIVE_PATHS;
			}

			m_lastOpenedLocation = pathname;
			Lastpath = pathname;

			string buffer = string.Empty;
			if( shaderFunction == null )
				buffer = IOUtils.LoadTextFileFromDisk( pathname );
			else
				buffer = shaderFunction.FunctionInfo;

			if( String.IsNullOrEmpty( buffer ) )
			{
				ShowMessage( "Could not open file " + pathname );
				return ShaderLoadResult.FILE_NOT_FOUND;
			}

			if( !IOUtils.HasValidShaderBody( ref buffer ) )
			{
				return ShaderLoadResult.ASE_INFO_NOT_FOUND;
			}

			m_mainGraphInstance.CleanNodes();
			Reset();

			string[] cameraParams = new string[ 0 ];
			Shader shader = null;
			ShaderLoadResult loadResult = ShaderLoadResult.LOADED;
			// Find checksum value on body
			int checksumId = buffer.IndexOf( IOUtils.CHECKSUM );
			if( checksumId > -1 )
			{
				string checkSumStoredValue = buffer.Substring( checksumId );
				string trimmedBuffer = buffer.Remove( checksumId );

				string[] typeValuePair = checkSumStoredValue.Split( IOUtils.VALUE_SEPARATOR );
				if( typeValuePair != null && typeValuePair.Length == 2 )
				{
					// Check read checksum and compare with the actual shader body to detect external changes
					string currentChecksumValue = IOUtils.CreateChecksum( trimmedBuffer );
					if( DebugConsoleWindow.DeveloperMode && !currentChecksumValue.Equals( typeValuePair[ 1 ] ) )
					{
						ShowMessage( "Wrong checksum" );
					}

					trimmedBuffer = trimmedBuffer.Replace( "\r", string.Empty );
					// find node info body
					int shaderBodyId = trimmedBuffer.IndexOf( IOUtils.ShaderBodyBegin );
					if( shaderBodyId > -1 )
					{
						trimmedBuffer = trimmedBuffer.Substring( shaderBodyId );
						//Find set of instructions
						string[] instructions = trimmedBuffer.Split( IOUtils.LINE_TERMINATOR );
						// First line is to be ignored and second line contains version
						string[] versionParams = instructions[ 1 ].Split( IOUtils.VALUE_SEPARATOR );
						if( versionParams.Length == 2 )
						{
							int version = 0;
							try
							{
								version = Convert.ToInt32( versionParams[ 1 ] );
							}
							catch( Exception e )
							{
								Debug.LogException( e );
							}

							if( version > VersionInfo.FullNumber )
							{
								ShowMessage( "This shader was created on a new ASE version\nPlease install v." + version );
							}

							if( DebugConsoleWindow.DeveloperMode )
							{
								if( version < VersionInfo.FullNumber )
								{
									ShowMessage( "This shader was created on a older ASE version\nSaving will update it to the new one." );
								}
							}

							m_mainGraphInstance.LoadedShaderVersion = version;
						}
						else
						{
							ShowMessage( "Corrupted version" );
						}

						// @diogo: second line MAY contain camera information ( position, size, offset and zoom )
						string[] split = instructions[ 2 ].Split( IOUtils.FIELD_SEPARATOR );
						if ( split.Length == 9 && float.TryParse( split[ 0 ], NumberStyles.Any, CultureInfo.InvariantCulture, out float value ) )
						{
							// @diogo: valid camera parameter serialization
							cameraParams = split;
						}

						// valid instructions are only between the line after version and the line before the last one ( which contains ShaderBodyEnd )
						for ( int instructionIdx = 0; instructionIdx < instructions.Length - 1; instructionIdx++ )
						{
							//TODO: After all is working, convert string parameters to ints in order to speed up reading
							string[] parameters = JsonGraphFormat.SplitInstruction( instructions[ instructionIdx ] );

							// All nodes must be created before wiring the connections ...
							// Since all nodes on the save op are written before the wires, we can safely create them
							// If that order is not maintained the it's because of external editing and its the users responsability
							switch( parameters[ 0 ] )
							{
								case IOUtils.NodeParam:
								{
									System.Type type = IOUtils.GetNodeType( parameters[ IOUtils.NodeTypeId ] );

									if( type != null )
									{
										System.Type oldType = type;
										NodeAttributes attribs = m_contextMenu.GetNodeAttributesForType( type );
										if( attribs == null )
										{
											attribs = m_contextMenu.GetDeprecatedNodeAttributesForType( type );
											if( attribs != null )
											{
												// @diogo: only a node being swapped out is worth a console line; one that stays
												//         in the graph explains itself through its own LEGACY tab
												if( attribs.Deprecated && attribs.DeprecatedAlternativeType != null )
												{
													type = attribs.DeprecatedAlternativeType;
													ShowMessage( string.Format( "Node {0} is deprecated and was replaced by {1} ", attribs.Name, attribs.DeprecatedAlternative ) );
												}
											}
										}

										ParentNode newNode = (ParentNode)ScriptableObject.CreateInstance( type );
										if( newNode != null )
										{
											try
											{
												newNode.ContainerGraph = m_mainGraphInstance;
												if( oldType != type )
												{
													newNode.ParentReadFromString( ref parameters );
													newNode.ReadFromDeprecated( ref parameters, oldType );
													newNode.WasDeprecated = true;
												}
												else
													newNode.ReadFromString( ref parameters );

												if( oldType == type )
												{
													newNode.ReadInputDataFromString( ref parameters );
													if( UIUtils.CurrentShaderVersion() > 5107 )
													{
														newNode.ReadOutputDataFromString( ref parameters );
													}
												}
											}
											catch( Exception e )
											{
												Debug.LogException( e, newNode );
											}

											newNode = ShaderFunctionToNode.ConvertIfEligible( newNode );

											m_mainGraphInstance.AddNode( newNode, false, true, false );
										}
									}
									else
									{
										ShowMessage( string.Format( "{0} is not a valid ASE node ", parameters[ IOUtils.NodeTypeId ] ), MessageSeverity.Error );
									}
								}
								break;
								case IOUtils.WireConnectionParam:
								{
									int InNodeId = 0;
									int InPortId = 0;
									int OutNodeId = 0;
									int OutPortId = 0;

									try
									{
										InNodeId = Convert.ToInt32( parameters[ IOUtils.InNodeId ] );
										InPortId = Convert.ToInt32( parameters[ IOUtils.InPortId ] );
										OutNodeId = Convert.ToInt32( parameters[ IOUtils.OutNodeId ] );
										OutPortId = Convert.ToInt32( parameters[ IOUtils.OutPortId ] );
									}
									catch( Exception e )
									{
										Debug.LogException( e );
									}

									ParentNode inNode = m_mainGraphInstance.GetNode( InNodeId );
									ParentNode outNode = m_mainGraphInstance.GetNode( OutNodeId );

									//if ( UIUtils.CurrentShaderVersion() < 5002 )
									//{
									//	InPortId = inNode.VersionConvertInputPortId( InPortId );
									//	OutPortId = outNode.VersionConvertOutputPortId( OutPortId );
									//}

									InputPort inputPort = null;
									OutputPort outputPort = null;
									if( inNode != null && outNode != null )
									{

										if( UIUtils.CurrentShaderVersion() < 5002 )
										{
											InPortId = inNode.VersionConvertInputPortId( InPortId );
											OutPortId = outNode.VersionConvertOutputPortId( OutPortId );

											if( inNode.WasDeprecated )
												InPortId = inNode.InputIdFromDeprecated( InPortId );
											if( outNode.WasDeprecated )
												OutPortId = outNode.OutputIdFromDeprecated( OutPortId );

											inputPort = inNode.GetInputPortByArrayId( InPortId );
											outputPort = outNode.GetOutputPortByArrayId( OutPortId );
										}
										else
										{
											if( inNode.WasDeprecated )
												InPortId = inNode.InputIdFromDeprecated( InPortId );
											if( outNode.WasDeprecated )
												OutPortId = outNode.OutputIdFromDeprecated( OutPortId );

											inputPort = inNode.GetInputPortByUniqueId( InPortId );
											outputPort = outNode.GetOutputPortByUniqueId( OutPortId );
										}

										if( inputPort != null && outputPort != null )
										{
											// @diogo: see ApplyWireInstruction
											bool checkTypes = inputPort.NotFreeForAllTypes && outputPort.NotFreeForAllTypes;
											bool inputCompatible = !checkTypes || inputPort.CheckValidType( outputPort );
											bool outputCompatible = !checkTypes || outputPort.CheckValidType( inputPort );
											if( inputCompatible && outputCompatible )
											{
												// @diogo: a locked input refuses the wire, so skip the output side too, or
												// the graph keeps a one-way reference that no repair pass can see
												if ( inputPort.ConnectTo( OutNodeId, OutPortId, outputPort.DataType, false ) )
												{
													outputPort.ConnectTo( InNodeId, InPortId, inputPort.DataType, inputPort.TypeLocked );

													inNode.OnInputPortConnected( InPortId, OutNodeId, OutPortId, false );
													outNode.OnOutputPortConnected( OutPortId, InNodeId, InPortId );
												}
											}
											else if ( outputPort.DataType == WirePortDataType.OBJECT )
											{
												m_mainGraphInstance.DeferWire( inputPort, outputPort );
											}
											else if( DebugConsoleWindow.DeveloperMode )
											{
												if( !inputCompatible )
													UIUtils.ShowIncompatiblePortMessage( true, inNode, inputPort, outNode, outputPort );

												if( !outputCompatible )
													UIUtils.ShowIncompatiblePortMessage( true, outNode, outputPort, inNode, inputPort );
											}
										}
										else if( DebugConsoleWindow.DeveloperMode )
										{
											if( inputPort == null )
											{
												UIUtils.ShowMessage( "Input Port " + InPortId + " doesn't exist on node " + InNodeId, MessageSeverity.Error );
											}
											else
											{
												UIUtils.ShowMessage( "Output Port " + OutPortId + " doesn't exist on node " + OutNodeId, MessageSeverity.Error );
											}
										}
									}
									else if( DebugConsoleWindow.DeveloperMode )
									{
										if( inNode == null )
										{
											UIUtils.ShowMessage( "Input node " + InNodeId + " doesn't exist", MessageSeverity.Error );
										}
										else
										{
											UIUtils.ShowMessage( "Output node " + OutNodeId + " doesn't exist", MessageSeverity.Error );
										}
									}
								}
								break;
							}
						}

						if ( shaderFunction != null )
						{
							m_onLoadDone = 2;
						}
						else
						{
							shader = AssetDatabase.LoadAssetAtPath<Shader>( pathname );
							if ( shader )
							{

								m_onLoadDone = 2;
							}
							else
							{
								ShowMessage( "Could not load shader asset" );
							}
						}
					}
					else
					{
						ShowMessage( "Graph info not found" );
					}
				}
				else
				{
					ShowMessage( "Corrupted checksum" );
				}
			}
			else
			{
				ShowMessage( "Checksum not found" );
			}

			//m_mainGraphInstance.LoadedShaderVersion = m_versionInfo.FullNumber;
			// @diogo: post-load refresh is mechanical - hiding invisible passes deletes their connections and must not register undo steps
			BeginSuppressUndoRegistration();
			try
			{
				if( UIUtils.CurrentMasterNode() )
					UIUtils.CurrentMasterNode().ForcePortType();

				UIUtils.DirtyMask = true;
				m_checkInvalidConnections = true;

				m_mainGraphInstance.CheckForDuplicates();
				m_mainGraphInstance.UpdateRegisters();
				m_mainGraphInstance.RefreshExternalReferences();
				m_mainGraphInstance.ApplyDeferredWires();
				m_mainGraphInstance.ForceSignalPropagationOnMasterNode();

				InlinePropertyTable.ResolveDependencies();

				if( shaderFunction != null )
				{
					//if( CurrentGraph.CurrentFunctionOutput == null )
					//{
					//	//Fix in case a function output node is not marked as main node
					//	CurrentGraph.AssignMasterNode( UIUtils.FunctionOutputList()[ 0 ], false );
					//}
					shaderFunction.ResetDirectivesOrigin();
					CurrentGraph.CurrentShaderFunction = shaderFunction;
				}
				else
				{
					if( shader != null )
					{
						m_mainGraphInstance.UpdateShaderOnMasterNode( shader );
						if( m_mainGraphInstance.CurrentCanvasMode == NodeAvailability.TemplateShader )
						{
							m_mainGraphInstance.RefreshLinkedMasterNodes( false );
							m_mainGraphInstance.OnRefreshLinkedPortsComplete();
							//m_mainGraphInstance.SetLateOptionsRefresh();
						}
					}
				}
			}
			finally
			{
				EndSuppressUndoRegistration();
			}

			// @diogo: set camera parameters
			if ( shaderFunction != null || shader != null )
			{
				cameraParams = ( cameraParams.Length != 0 ) ? cameraParams : EditorPrefs.GetString( GUID, "" ).Split( IOUtils.FIELD_SEPARATOR );

				if ( cameraParams.Length == 9 )
				{
					try
					{
						var cameraInfo = new Rect( Convert.ToSingle( cameraParams[ 0 ] ), Convert.ToSingle( cameraParams[ 1 ] ), Convert.ToSingle( cameraParams[ 2 ] ), Convert.ToSingle( cameraParams[ 3 ] ) );
						var cameraOffset = new Vector2( Convert.ToSingle( cameraParams[ 4 ] ), Convert.ToSingle( cameraParams[ 5 ] ) );
						float cameraZoom = Convert.ToSingle( cameraParams[ 6 ] );

						float centerWidth = ( this.position.width - cameraInfo.width ) * 0.5f * cameraZoom;
						float centerHeight = ( this.position.height - cameraInfo.height ) * 0.5f * cameraZoom;

						cameraInfo.x += centerWidth;
						cameraInfo.y += centerHeight;

						cameraOffset.x += centerWidth;
						cameraOffset.y += centerHeight;

						m_cameraInfo = cameraInfo;
						m_cameraOffset = cameraOffset;
						CameraZoom = cameraZoom;
						// @diogo: camera-info fields 7-8 (panel maximized) are intentionally not applied; panel open/close is machine-global, not per-graph
					}
					catch ( Exception )
					{
						FocusZoom( true, false, false );
					}
				}
				else
				{
					FocusZoom( true, false, false );
				}
			}

			m_mainGraphInstance.LoadedShaderVersion = VersionInfo.FullNumber;

			System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;

			m_mainGraphInstance.IsLoading = false;

			// @diogo: disk load is a manual parse (no OnAfterDeserialize), so schedule the frame reorder explicitly
			m_mainGraphInstance.ScheduleFrameReorder();

			// The freshly loaded graph is the on-disk baseline the Save indicator compares against.
			CaptureSavedChecksum();

			//Remove focus from UI elements so no UI is incorrectly selected from previous loads
			//Shader Name textfield was sometimes incorrectly selected
			GUI.FocusControl( null );

			timer.Stop();
			if ( Preferences.User.LogShaderLoad )
			{
				FinishedShaderLoadLog( timer.Elapsed.TotalSeconds );
			}
			return loadResult;
		}

		public void FullCleanUndoStack()
		{
			UndoUtils.ClearUndo( this );
			// @diogo: snapshot undo entries live on the proxy; stale ones surviving a load would let an
			// undo walk past the load boundary and apply a previous graph's state
			if ( m_undoProxy != null )
			{
				UndoUtils.ClearUndo( m_undoProxy );
			}
			m_mainGraphInstance.FullCleanUndoStack();
			UndoProfiler.ResetHistory();
		}

		// === Snapshot-based undo ( behind Preferences.User.EnableUndo ). See GraphUndoProxy. ===

		// Stored byte size of the live undo snapshot, raw or compressed depending on the current
		// preference ( 0 when undo is unavailable ). Surfaced in the Preferences Developer section as a
		// per-step memory-footprint gauge. Reads the already-stored bytes ( no decompression ) since this
		// is polled every prefs GUI frame.
		public int CurrentUndoSnapshotStoredBytes { get { return m_undoProxy != null ? m_undoProxy.StoredSnapshotBytes : 0; } }

		// Full-graph meta string used as an undo snapshot. Mirrors the save body but skips the
		// EditorPrefs camera write GenerateGraphInfo performs, so it is cheap enough to call per
		// edit. Returns null when the graph cannot be safely serialized ( e.g. mid-load ).
		public string GenerateGraphSnapshot()
		{
			if ( m_mainGraphInstance == null || m_mainGraphInstance.IsLoading )
			{
				return null;
			}

			// @diogo: proxy captures run inside Unity's undo processing, outside OnGUI's per-frame culture pin;
			// a comma-decimal OS locale would otherwise serialize floats that break VECTOR_SEPARATOR fields
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			try
			{
				string nodesInfo = string.Empty;
				string connectionsInfo = string.Empty;
				m_mainGraphInstance.WriteToStringDepthOrdered( ref nodesInfo, ref connectionsInfo );

				string body = IOUtils.ShaderBodyBegin + '\n' + VersionInfo.FullLabel + '\n' +
					nodesInfo + connectionsInfo + IOUtils.ShaderBodyEnd + '\n';
				return IOUtils.AddAdditionalInfo( body );
			}
			catch ( Exception e )
			{
				// A graph whose master node could not resolve its template ( e.g. the template package is
				// still loading ) cannot be serialized - its Pass getter throws. Honor the null contract
				// above so the checksum/undo/backup callers degrade gracefully instead of crashing the load.
				if ( DebugConsoleWindow.DeveloperMode )
					Debug.LogWarning( "[AmplifyShaderEditor] Undo snapshot skipped: the graph is not currently serializable (a master node has no resolvable template Pass). " + e );

				return null;
			}
		}

		// @diogo: called by the undo proxy with every fresh live-graph serialize ( see m_lastLiveGraphSerialize )
		public void NotifyLiveGraphSerialized( string snapshot )
		{
			m_lastLiveGraphSerialize = snapshot;
		}

		// Order-independent checksum of an already-serialized graph snapshot. The order nodes are written
		// in is derived from graph depth and differs between the disk-load and snapshot-reload paths for
		// the same graph ( not a meaningful change ), so the lines are sorted before hashing: the checksum
		// then reflects *what* the graph contains, not the order it happens to serialize in. The snapshot's
		// own trailing //CHKSM line is dropped first - it is a hash of the *ordered* body, so it differs
		// whenever node order differs and would reintroduce the very order dependence the sort removes.
		// Returns empty for an empty/null snapshot.
		private string ChecksumOfSnapshot( string snapshot )
		{
			if ( string.IsNullOrEmpty( snapshot ) )
			{
				return string.Empty;
			}

			string[] lines = snapshot.Split( '\n' );

			// @diogo: mask session-ordered fields (the master-node category index) so the checksum compares
			// persistent state only - the value is re-derived from the template GUID on load and drifts
			// across domain reloads; shared by the patch oracle, the Save indicator and its saved baseline
			for ( int i = 0; i < lines.Length; i++ )
			{
				lines[ i ] = UndoSnapshotDiff.NormalizeForComparison( lines[ i ] );
			}

			Array.Sort( lines, StringComparer.Ordinal );

			List<string> contentLines = new List<string>( lines.Length );
			for ( int i = 0; i < lines.Length; i++ )
			{
				if ( !lines[ i ].StartsWith( IOUtils.CHECKSUM, StringComparison.Ordinal ) )
				{
					contentLines.Add( lines[ i ] );
				}
			}

			return IOUtils.CreateChecksum( string.Join( "\n", contentLines ) );
		}

		// Checksum of the current live graph. Used to capture the saved baseline.
		private string ComputeGraphChecksum()
		{
			return ChecksumOfSnapshot( GenerateGraphSnapshot() );
		}

		// Captures the current graph as the "saved" baseline. Called on save and on disk load so the
		// Save indicator reads as unmodified ( green ) for the freshly persisted state.
		private void CaptureSavedChecksum()
		{
			// Fresh baseline: any prior post-undo verdict latch no longer applies.
			m_undoVerdictActive = false;
			string snapshot = GenerateGraphSnapshot();
			m_savedGraphChecksum = ChecksumOfSnapshot( snapshot );
			// @diogo: diagnostic only: keep the baseline bytes so a suspect indicator verdict can be diffed
			m_savedGraphSnapshot = Preferences.User.UndoProfiling ? snapshot : null;
		}

		// Sets the modified indicator by comparing the current graph against the last saved/loaded
		// state: green when they match. Computes a whole-graph checksum, so it is only called at
		// discrete commit points ( gesture end, undo/redo ), never per frame. With no baseline yet
		// ( e.g. a brand-new unsaved graph ) the existing flag behavior is left untouched.
		//
		// For an undo/redo, m_recheckSnapshot holds the exact bytes the undo step restored, and that is
		// what gets compared. Re-serializing the *reloaded* graph instead does not reproduce those bytes
		// ( node listing order and sub-graph ids shift on the LoadFromMeta path ), so the baseline -
		// itself captured from a non-reloaded graph - would never match and the indicator would stay
		// stuck as modified. Comparing the restored bytes keeps both sides on the same ( non-reloaded )
		// footing. For a plain edit there is no restored snapshot and the live graph is serialized.
		private void EvaluateModifiedState()
		{
			string recheckSnapshot = m_recheckSnapshot;
			m_recheckSnapshot = null;

			if ( string.IsNullOrEmpty( m_savedGraphChecksum ) )
			{
				return;
			}

			string current = recheckSnapshot != null ? ChecksumOfSnapshot( recheckSnapshot ) : ComputeGraphChecksum();
			if ( string.IsNullOrEmpty( current ) )
			{
				// Graph not serializable right now ( e.g. mid-load ); leave the indicator as-is.
				return;
			}

			ShaderIsModified = ( current != m_savedGraphChecksum );

			// @diogo: diagnostic only: a modified verdict logs which lines drifted from the saved baseline;
			// plain edits re-serialize on purpose - this only runs when the profiling pref is on
			if ( ShaderIsModified && !string.IsNullOrEmpty( m_savedGraphSnapshot ) )
			{
				string modifiedSnapshot = ( recheckSnapshot != null ) ? recheckSnapshot : GenerateGraphSnapshot();
				UndoProfiler.RecordIndicatorMismatch( UndoSnapshotDiff.Diff( modifiedSnapshot, m_savedGraphSnapshot ) );
			}

			// An undo/redo's verdict stands until the next edit; latch it so the post-reload SaveIsDirty
			// churn cannot override it on following frames. A plain edit needs no latch ( and clears any
			// stale one ) - its mid-edit "colored" feedback should keep flowing through the dirty path.
			m_undoVerdictActive = ( recheckSnapshot != null );
		}

		// Auto-backup timer tick. Called from UpdateNodePreviewListAndTime ( focused window only ), so
		// backups track the graph the user is actively editing. The first tick after open/enable only arms
		// the timer; later ticks take a backup once the interval elapses.
		private void AutoBackupTick()
		{
			if ( !Preferences.User.AutoBackupEnabled || IsBatchProcessing )
			{
				return;
			}

			double now = EditorApplication.timeSinceStartup;
			double interval = Mathf.Max( 1, Preferences.User.AutoBackupFrequency );
			if ( m_nextAutoBackupTime <= 0 )
			{
				m_nextAutoBackupTime = now + interval;
				return;
			}

			if ( now < m_nextAutoBackupTime )
			{
				return;
			}

			m_nextAutoBackupTime = now + interval;
			PerformAutoBackup( false );
		}

		// Writes a backup of the current graph. force=true ( the panel's Backup Now button ) writes even
		// when the graph is unchanged; the scheduled path skips an unchanged graph so the history is not
		// filled with identical copies.
		public void PerformAutoBackup( bool force )
		{
			string snapshot = GenerateGraphSnapshot();
			if ( string.IsNullOrEmpty( snapshot ) )
			{
				return;
			}

			MigrateUnsavedBackupHistory();

			string checksum = ChecksumOfSnapshot( snapshot );
			if ( !force && checksum == m_lastAutoBackupChecksum )
			{
				return;
			}

			AutoBackup.SaveBackup( AutoBackupKey, AutoBackupDisplayName, snapshot );
			m_lastAutoBackupChecksum = checksum;

			// The write happens off the GUI ( timer in the update loop ); repaint so the Auto Backup panel
			// reflects the new entry right away instead of waiting for the next input event.
			Repaint();
		}

		// Queues a backup restore from the panel. The actual reload is deferred to OnGUI ( see
		// m_pendingBackupSnapshot ) so it does not run in the middle of the palette's draw.
		public void RequestBackupRestore( string snapshot )
		{
			m_pendingBackupSnapshot = snapshot;
		}

		// Replaces the live graph with a stored backup snapshot. Registers an undo first so the load is
		// reversible, then reuses the snapshot reload path ( see ReloadGraphFromSnapshot ).
		public void LoadFromBackup( string snapshot )
		{
			if ( string.IsNullOrEmpty( snapshot ) )
			{
				return;
			}

			RegisterGraphUndo( "Load Auto Backup" );
			ReloadGraphFromSnapshot( snapshot, null );
		}

		private const string UnsavedBackupKeyPrefix = "unsaved-";

		// Stable per-graph key for grouping backups: the asset GUID once saved, else a per-window id while
		// the graph has never been written to disk.
		public string AutoBackupKey
		{
			get
			{
				string path = CurrentBackupAssetPath();
				if ( !string.IsNullOrEmpty( path ) )
				{
					string guid = AssetDatabase.AssetPathToGUID( path );
					if ( !string.IsNullOrEmpty( guid ) )
					{
						return guid;
					}
				}

				return UnsavedBackupKeyPrefix + AssetUtils.GetEntityId( this );
			}
		}

		// @diogo: once an unsaved graph is saved, carry its backup history from the per-session folder into the GUID folder.
		private void MigrateUnsavedBackupHistory()
		{
			string key = AutoBackupKey;
			if ( !key.StartsWith( UnsavedBackupKeyPrefix ) )
			{
				AutoBackup.MigrateKey( UnsavedBackupKeyPrefix + AssetUtils.GetEntityId( this ), key );
			}
		}

		// Human-readable name for the backup file and history header.
		public string AutoBackupDisplayName
		{
			get
			{
				if ( IsShaderFunctionWindow )
				{
					return m_mainGraphInstance.CurrentShaderFunction != null ? m_mainGraphInstance.CurrentShaderFunction.FunctionName : "Unsaved Function";
				}

				MasterNode masterNode = m_mainGraphInstance.CurrentMasterNode;
				if ( masterNode != null && !string.IsNullOrEmpty( masterNode.ShaderName ) )
				{
					return masterNode.ShaderName;
				}

				return "Unsaved Shader";
			}
		}

		private string CurrentBackupAssetPath()
		{
			if ( IsShaderFunctionWindow )
			{
				return m_mainGraphInstance.CurrentShaderFunction != null ? AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShaderFunction ) : string.Empty;
			}

			// @diogo: key off the shader, not the material, so editing a shader via a material reuses one backup folder.
			Shader shader = m_mainGraphInstance.CurrentShader;
			if ( shader != null )
			{
				return AssetDatabase.GetAssetPath( shader );
			}

			if ( m_selectionMode == ASESelectionMode.Material && m_mainGraphInstance.CurrentMaterial != null )
			{
				return AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentMaterial );
			}

			return string.Empty;
		}

		public int[] GetSelectedNodeIds()
		{
			if ( m_mainGraphInstance == null )
			{
				return new int[ 0 ];
			}

			List<ParentNode> selected = m_mainGraphInstance.SelectedNodes;
			if ( selected == null || selected.Count == 0 )
			{
				return new int[ 0 ];
			}

			int[] ids = new int[ selected.Count ];
			for ( int i = 0; i < selected.Count; i++ )
			{
				ids[ i ] = selected[ i ].UniqueId;
			}
			return ids;
		}

		// @diogo: brackets for mechanical cascades ( post-load template refresh, undo restore ) whose
		// mutations must not register undo steps; counter because brackets can nest
		public void BeginSuppressUndoRegistration() { m_suppressUndoRegistration++; }
		public void EndSuppressUndoRegistration() { m_suppressUndoRegistration--; }

		// Call this *before* a graph mutation to push the pre-edit state onto Unity's undo stack.
		public void RegisterGraphUndo( string name )
		{
			if ( m_suppressUndoRegistration > 0 )
			{
				return;
			}

			// A genuine edit is about to mutate the graph: drop the post-undo verdict latch so the
			// indicator resumes normal modified tracking ( both real-edit paths route through here ).
			m_undoVerdictActive = false;

			if ( Preferences.User.EnableUndo && m_undoProxy != null && !IsBatchProcessing )
			{
				m_undoProxy.RegisterUndo( name );
			}
		}

		// Coalesced variant for continuous gestures ( slider drags, node moves, typing ). Takes a
		// single pre-edit snapshot per gesture; further calls are ignored until the gesture ends on
		// the next mouse press ( see the rawType reset in OnGUI ). Without this, one drag would push
		// a separate undo step per frame.
		public void RegisterGraphUndoGesture( string name )
		{
			if ( !Preferences.User.EnableUndo || m_undoProxy == null || m_gestureUndoActive || m_suppressUndoRegistration > 0 )
			{
				return;
			}

			RegisterGraphUndo( name );
			m_gestureUndoActive = true;
		}

		// Rebuilds the live graph from the proxy's snapshot string, either by patching only the changed
		// nodes/wires or through the normal load path, then restores the recorded selection. Driven by
		// the Undo.undoRedoPerformed callback.
		public void RestoreFromUndoSnapshot()
		{
			if ( m_undoProxy == null )
			{
				return;
			}

			// @diogo: a restore invalidates any in-flight gesture; the next edit must take its own pre-edit snapshot
			m_gestureUndoActive = false;

			// @diogo: undoRedoPerformed fires outside OnGUI's per-frame culture pin; parse and serialize of
			// snapshot floats must run invariant like every other load path ( see LoadFromDisk )
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			string snapshot = m_undoProxy.SerializedGraph;
			if ( string.IsNullOrEmpty( snapshot ) )
			{
				return;
			}

			// @diogo: Stage 1 shadow mode only runs when incremental undo is OFF - the live patcher logs its own diff
			if ( !Preferences.User.IncrementalUndo && Preferences.User.UndoProfiling )
			{
				System.Diagnostics.Stopwatch shadowTimer = System.Diagnostics.Stopwatch.StartNew();
				string currentMeta = GenerateGraphSnapshot();
				UndoSnapshotDiff.SnapshotDiffResult diffResult = UndoSnapshotDiff.Diff( currentMeta, snapshot );
				shadowTimer.Stop();
				UndoProfiler.RecordShadowDiff( diffResult, shadowTimer.Elapsed.TotalMilliseconds );
			}

			// @diogo: Stage 2 live patching; any bail, exception or oracle mismatch falls through to the full reload below
			if ( Preferences.User.IncrementalUndo )
			{
				// @diogo: first attempt reuses the redo-stack serialize Unity captured moments ago in this
				// same undo event; if that capture was stale in any way, the failed attempt is retried once
				// with an honest serialize before conceding to the full reload
				bool cachedCurrent = !string.IsNullOrEmpty( m_lastLiveGraphSerialize );
				bool patched = TryPatchGraphFromSnapshot( snapshot, m_undoProxy.SelectedNodeIds, cachedCurrent );
				if ( !patched && cachedCurrent )
				{
					patched = TryPatchGraphFromSnapshot( snapshot, m_undoProxy.SelectedNodeIds, false );
				}
				if ( patched )
				{
					PushRestoredValuesToMaterial();
					return;
				}
			}

			System.Diagnostics.Stopwatch restoreTimer = System.Diagnostics.Stopwatch.StartNew();
			ReloadGraphFromSnapshot( snapshot, m_undoProxy.SelectedNodeIds );
			restoreTimer.Stop();
			UndoProfiler.RecordRestore( restoreTimer.Elapsed.TotalMilliseconds, m_lastReloadLoadMs, m_lastReloadRefreshMs, m_lastReloadViewStateMs, m_lastReloadNodeCount );
			PushRestoredValuesToMaterial();
		}

		// @diogo: an undo restore sets node values from the snapshot only; push them into the bound material
		// too, or it keeps the pre-undo values and the next refetch ( e.g. _ASEDirtyCheck ) re-imposes them
		private void PushRestoredValuesToMaterial()
		{
			Material material = m_mainGraphInstance.CurrentMaterial;
			if ( material != null )
			{
				m_mainGraphInstance.UpdateMaterialOnPropertyNodes( material );
				if ( MaterialInspector.Instance != null )
				{
					MaterialInspector.Instance.Repaint();
				}
			}
		}

		// Forces the Material Properties and function port lists to rebuild from the live graph. Both
		// only refresh when the node count changes, so an order-only restore would otherwise keep the
		// pre-undo ordering and hold on to nodes the restore replaced.
		private void InvalidatePropertyLists()
		{
			MasterNode masterNode = m_mainGraphInstance.CurrentMasterNode;
			if ( masterNode != null )
			{
				masterNode.InvalidateMaterialPropertyCount();
			}
			m_nodeParametersWindow.InvalidateMaterialPropertyCount();
			m_nodeParametersWindow.ForceUpdate = true;
		}

		// Rebuilds the live graph from a snapshot meta string through the normal load path, preserving
		// transient editor view state and re-linking the runtime asset references the snapshot does not
		// carry. Shared by undo/redo ( RestoreFromUndoSnapshot ) and backup loads ( LoadFromBackup );
		// selectedNodeIds is the selection to restore afterward ( null for none ).
		private void ReloadGraphFromSnapshot( string snapshot, int[] selectedNodeIds )
		{
			// @diogo: backup restores can also arrive outside OnGUI's per-frame culture pin
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			AmplifyShaderEditorWindow cached = UIUtils.CurrentWindow;
			UIUtils.CurrentWindow = this;

			System.Diagnostics.Stopwatch viewStateTimer = new System.Diagnostics.Stopwatch();

			viewStateTimer.Start();
			// Capture transient editor-only view state ( foldouts, etc. ) keyed by node id. The reload
			// below recreates every node fresh and would otherwise reset it, so undoing a value edit
			// would also collapse foldouts the user had open. Reapplied after the reload.
			Dictionary<int, string[]> viewState = new Dictionary<int, string[]>();
			List<ParentNode> preReloadNodes = m_mainGraphInstance.AllNodes;
			for ( int i = 0; i < preReloadNodes.Count; i++ )
			{
				ParentNode node = preReloadNodes[ i ];
				List<string> state = new List<string>();
				node.WriteUndoViewState( state );
				viewState[ node.UniqueId ] = state.ToArray();
			}

			// A shader-function window keeps its function-asset link on the FunctionOutput node
			// ( CurrentShaderFunction => CurrentFunctionOutput.Function ), a runtime reference that is
			// not carried in the snapshot meta. Capture it so the reload can re-link it; otherwise it
			// comes back null and the function-window self-close guard ( IsShaderFunctionWindow &&
			// CurrentShaderFunction == null ) fires, closing the window on undo.
			AmplifyShaderFunction cachedShaderFunction = m_mainGraphInstance.CurrentShaderFunction;

			// Likewise the output Shader asset is a runtime reference on the master node
			// ( CurrentShader ), not carried in the snapshot meta. Capture it so Save still targets the
			// existing shader after an undo instead of prompting for a save location.
			Shader cachedShader = m_mainGraphInstance.CurrentShader;

			// @diogo: the bound material is a runtime reference on the master node too; capture it or a material-mode window silently drops to shader mode after undo
			Material cachedMaterial = m_mainGraphInstance.CurrentMaterial;

			// The master node's section foldouts are restored by node id below, but a template change
			// replaces the master node with a *new* id, so undoing one would miss it. Capture the
			// current master node's view state by role too and reapply it to whatever master node the
			// reload produces.
			string[] cachedMasterViewState = null;
			if ( m_mainGraphInstance.CurrentMasterNode != null )
			{
				List<string> masterState = new List<string>();
				m_mainGraphInstance.CurrentMasterNode.WriteUndoViewState( masterState );
				cachedMasterViewState = masterState.ToArray();
			}
			viewStateTimer.Stop();

			// Clear the name-dedup registry so the fully-recreated nodes re-register from a clean
			// slate. The in-place reload recreates every node, so nothing needs the old entries,
			// and this prevents a stale uniform-name registration leaking across the rebuild.
			m_duplicatePreventionBuffer.ReleaseAllData();

			// Reset the sub-graph id counter the same way a disk load ( Reset ) does, so Function nodes
			// re-derive the *same* m_functionGraphId on every reload. Without this the counter keeps
			// climbing ( id = Max( saved, GraphCount ) ), so a reloaded Function node serializes a
			// different id each undo - which never matches the saved baseline and leaves the graph
			// looking permanently modified.
			GraphCount = 1;

			System.Diagnostics.Stopwatch loadTimer = System.Diagnostics.Stopwatch.StartNew();
			LoadFromMeta( ref m_mainGraphInstance, m_contextMenu, snapshot );
			loadTimer.Stop();

			System.Diagnostics.Stopwatch refreshTimer = System.Diagnostics.Stopwatch.StartNew();
			// @diogo: the refresh below is mechanical - its DeleteConnection calls must not push undo steps
			BeginSuppressUndoRegistration();
			try
			{
				// LoadFromMeta is a manual parse ( no Unity deserialize callback ), so the post-load
				// master-node options/port reconfiguration that normally runs on m_afterDeserializeFlag
				// never fires. Trigger it explicitly so reloaded master nodes don't show every port and
				// option expanded. Mirrors the RefreshLinkedMasterNodes pass the disk load performs.
				if ( UIUtils.CurrentMasterNode() != null )
				{
					UIUtils.CurrentMasterNode().ForcePortType();
				}

				// Apply the staged template-option values ( OnRefreshLinkedPortsComplete -> SetReadOptions )
				// BEFORE the deferred cascade runs. SetLateOptionsRefresh's deferred pass executes the option
				// cascade ( RefreshLinkedMasterNodes( true ) ) *before* OnRefreshLinkedPortsComplete re-applies
				// the saved option values, so without applying them here first the cascade re-derives
				// port/option visibility from defaults - leaving e.g. Surface Type = Transparent but its
				// dependent options and Distortion ports hidden. Mirrors the LoadFromDisk template branch.
				if ( m_mainGraphInstance.CurrentCanvasMode == NodeAvailability.TemplateShader )
				{
					m_mainGraphInstance.RefreshLinkedMasterNodes( false );
					m_mainGraphInstance.OnRefreshLinkedPortsComplete();
				}
			}
			finally
			{
				EndSuppressUndoRegistration();
			}
			m_mainGraphInstance.SetLateOptionsRefresh();

			// @diogo: LoadFromMeta is a manual parse too, so schedule the frame reorder like the disk load does
			m_mainGraphInstance.ScheduleFrameReorder();
			refreshTimer.Stop();

			// Re-link the function asset onto the recreated FunctionOutput node ( see capture above )
			// so a function window does not self-close after an undo.
			if ( cachedShaderFunction != null )
			{
				m_mainGraphInstance.CurrentShaderFunction = cachedShaderFunction;
			}

			// Re-link the output shader asset onto the recreated master node ( see capture above ) so
			// Save writes over the existing shader instead of opening the save-location browser.
			if ( cachedShader != null && m_mainGraphInstance.CurrentMasterNode != null )
			{
				m_mainGraphInstance.CurrentMasterNode.CurrentShader = cachedShader;
			}

			// @diogo: re-bind the material and re-enter material mode without fetching - restored values must come from the snapshot, not the material
			if ( cachedMaterial != null && m_mainGraphInstance.CurrentMasterNode != null )
			{
				m_mainGraphInstance.UpdateMaterialOnMasterNode( cachedMaterial );
				m_mainGraphInstance.SetMaterialModeOnGraph( cachedMaterial, false );
			}

			viewStateTimer.Start();
			m_mainGraphInstance.SelectNodesFromIds( selectedNodeIds );

			// Reapply the captured view state to the recreated nodes ( ids survive the snapshot ).
			List<ParentNode> reloadedNodes = m_mainGraphInstance.AllNodes;
			for ( int i = 0; i < reloadedNodes.Count; i++ )
			{
				ParentNode node = reloadedNodes[ i ];
				if ( viewState.TryGetValue( node.UniqueId, out string[] state ) )
				{
					int index = 0;
					node.ReadUndoViewState( state, ref index );
				}
			}

			// Reapply the master node's foldouts by role: across a template-switch undo its id changes,
			// so the per-id pass above misses it.
			if ( cachedMasterViewState != null && m_mainGraphInstance.CurrentMasterNode != null )
			{
				int masterIndex = 0;
				m_mainGraphInstance.CurrentMasterNode.ReadUndoViewState( cachedMasterViewState, ref masterIndex );
			}
			viewStateTimer.Stop();

			InvalidatePropertyLists();

			m_lastReloadLoadMs = loadTimer.Elapsed.TotalMilliseconds;
			m_lastReloadRefreshMs = refreshTimer.Elapsed.TotalMilliseconds;
			m_lastReloadViewStateMs = viewStateTimer.Elapsed.TotalMilliseconds;
			m_lastReloadNodeCount = m_mainGraphInstance.AllNodes.Count;

			UIUtils.CurrentWindow = cached;
			m_repaintIsDirty = true;
			m_saveIsDirty = true;

			// Re-evaluate the Save indicator against the saved baseline: undoing/redoing back to the
			// saved state turns it green again, landing on a different state keeps it colored. Hand the
			// recheck the exact snapshot the undo step restored so it compares those bytes rather than
			// re-serializing the freshly reloaded graph ( see EvaluateModifiedState ).
			m_recheckSnapshot = snapshot;
			m_recheckModified = true;

			// @diogo: the reloaded graph does not re-serialize to the snapshot bytes ( node listing order
			// and sub-graph ids shift on the LoadFromMeta path ), so drop the capture instead of trusting it
			m_lastLiveGraphSerialize = null;
		}

		// @diogo: incremental patch bail threshold: above this fraction of changed node lines a full reload is comparable anyway ( see docs/undo-incremental-restore.md )
		private const double PatchNodeBudgetRatio = 0.25;

		// @diogo: Stage 2 incremental undo: destroy/recreate only the nodes whose lines differ from the target
		// snapshot, set-diff the wires, then verify the patched graph against the target checksum; returns false
		// on any doubt and the caller runs the full reload ( normative procedure in docs/undo-incremental-restore.md )
		private bool TryPatchGraphFromSnapshot( string targetSnapshot, int[] selectedNodeIds, bool useCachedCurrent )
		{
			UndoProfiler.RecordPatchAttempt();
			System.Diagnostics.Stopwatch totalTimer = System.Diagnostics.Stopwatch.StartNew();
			System.Diagnostics.Stopwatch phaseTimer = System.Diagnostics.Stopwatch.StartNew();

			// @diogo: current state captures BEFORE any patch state is touched; IsLoading is false here.
			// The cached capture is the serialize Unity forced during this undo event ( see
			// m_lastLiveGraphSerialize ) - if it is stale the oracle fails and the caller retries honest.
			string currentMeta = useCachedCurrent ? m_lastLiveGraphSerialize : GenerateGraphSnapshot();
			double serializeMs = phaseTimer.Elapsed.TotalMilliseconds;
			phaseTimer.Restart();
			if ( string.IsNullOrEmpty( currentMeta ) )
			{
				UndoProfiler.RecordPatchBail( "no current snapshot" );
				return false;
			}

			UndoSnapshotDiff.SnapshotDiffResult diff = UndoSnapshotDiff.Diff( currentMeta, targetSnapshot );
			double diffMs = phaseTimer.Elapsed.TotalMilliseconds;

			if ( diff.Bailed )
			{
				UndoProfiler.RecordPatchBail( diff.BailReason );
				return false;
			}
			if ( diff.MasterNodeTouched )
			{
				UndoProfiler.RecordPatchBail( "master node touched", diff );
				return false;
			}
			if ( diff.FunctionNodeRecreated )
			{
				UndoProfiler.RecordPatchBail( "function node changed/added", diff );
				return false;
			}
			if ( !int.TryParse( diff.Version, out int targetVersion ) || targetVersion != VersionInfo.FullNumber )
			{
				UndoProfiler.RecordPatchBail( "target version is not the running version", diff );
				return false;
			}
			int patchSize = diff.ChangedIds.Count + diff.AddedIds.Count + diff.RemovedIds.Count;
			if ( patchSize > ( int )( PatchNodeBudgetRatio * Mathf.Max( diff.CurrentNodeCount, diff.TargetNodeCount ) ) )
			{
				UndoProfiler.RecordPatchBail( "patch exceeds node budget", diff );
				return false;
			}

			AmplifyShaderEditorWindow cachedWindow = UIUtils.CurrentWindow;
			UIUtils.CurrentWindow = this;
			bool isLoadingSet = false;
			try
			{
				phaseTimer.Restart();

				// @diogo: commentary closure, transitive: recreating or removing a framed node detaches it from its
				// kept frame via NodeDestroyed, so the frame ( and any outer frames ) must be recreated from the target
				HashSet<int> recreateIds = new HashSet<int>( diff.ChangedIds );
				Queue<int> commentaryScan = new Queue<int>( diff.ChangedIds );
				for ( int i = 0; i < diff.RemovedIds.Count; i++ )
				{
					commentaryScan.Enqueue( diff.RemovedIds[ i ] );
				}
				while ( commentaryScan.Count > 0 )
				{
					ParentNode scanNode = m_mainGraphInstance.GetNode( commentaryScan.Dequeue() );
					if ( scanNode != null && scanNode.CommentaryParent > -1 && recreateIds.Add( scanNode.CommentaryParent ) )
					{
						commentaryScan.Enqueue( scanNode.CommentaryParent );
					}
				}

				// @diogo: pre-validate every id we are about to destroy so most bails leave the graph untouched
				HashSet<int> destroyIds = new HashSet<int>( recreateIds );
				destroyIds.UnionWith( diff.RemovedIds );
				foreach ( int id in destroyIds )
				{
					if ( m_mainGraphInstance.GetNode( id ) == null )
					{
						UndoProfiler.RecordPatchBail( "live node " + id + " not found" );
						return false;
					}
				}

				// @diogo: position-only nodes are patched in place, so they must exist live too
				for ( int i = 0; i < diff.PositionOnlyIds.Count; i++ )
				{
					if ( m_mainGraphInstance.GetNode( diff.PositionOnlyIds[ i ] ) == null )
					{
						UndoProfiler.RecordPatchBail( "live node " + diff.PositionOnlyIds[ i ] + " not found" );
						return false;
					}
				}

				// @diogo: transient editor view state survives on kept nodes natively; capture it for recreated ones only
				Dictionary<int, string[]> viewState = new Dictionary<int, string[]>();
				foreach ( int id in recreateIds )
				{
					ParentNode node = m_mainGraphInstance.GetNode( id );
					if ( node != null )
					{
						List<string> state = new List<string>();
						node.WriteUndoViewState( state );
						viewState[ id ] = state.ToArray();
					}
				}

				// @diogo: DestroyNode does not deselect; clear selection before destroying, reapply from the proxy at the end
				m_mainGraphInstance.DeSelectAll();

				m_mainGraphInstance.IsLoading = true;
				isLoadingSet = true;

				// @diogo: destroy quietly ( propagateCallbacks:false ): neighbor disconnect callbacks trim ports and
				// reset types on kept adaptive nodes, state their unchanged lines must keep; mirrors CleanNodes semantics
				foreach ( int id in destroyIds )
				{
					m_mainGraphInstance.DestroyNode( m_mainGraphInstance.GetNode( id ), false, false, false );
				}

				// @diogo: dev-only fault injection: blow up mid-surgery (destroys done, nothing recreated yet)
				// to exercise the catch -> full-reload recovery from a half-applied patch
				if ( UndoProfiler.InjectPatchException )
				{
					UndoProfiler.InjectPatchException = false;
					throw new Exception( "injected patch exception (developer toggle)" );
				}

				// @diogo: recreate in target line order through the exact load interpreter; never fetch material values,
				// matching the full reload where CurrentMaterial is unavailable mid-load
				HashSet<int> addedSet = new HashSet<int>( diff.AddedIds );
				System.Diagnostics.Stopwatch bucketTimer = new System.Diagnostics.Stopwatch();
				foreach ( int id in diff.TargetNodeOrder )
				{
					if ( recreateIds.Contains( id ) || addedSet.Contains( id ) )
					{
						string[] parameters = JsonGraphFormat.SplitInstruction( diff.TargetNodeLines[ id ] );
						CreateNodeFromInstruction( m_mainGraphInstance, m_contextMenu, ref parameters, bucketTimer, false );
					}
				}

				// @diogo: position-only fast path: write the target position onto the kept live node instead
				// of destroy+recreate - keeps bulk moves proportional and spares moved master/function nodes
				// their policy bails; reads the pos field off the raw line, and the oracle verify still covers it
				foreach ( int id in diff.PositionOnlyIds )
				{
					// @diogo: a commentary swept into the recreate closure was already rebuilt from its
					// target line, position included
					if ( recreateIds.Contains( id ) )
					{
						continue;
					}
					if ( !UndoSnapshotDiff.TryGetNodePosition( diff.TargetNodeLines[ id ], out Vector2 targetPosition ) )
					{
						UndoProfiler.RecordPatchBail( "bad position line on id " + id );
						return false;
					}
					ParentNode positionNode = m_mainGraphInstance.GetNode( id );
					positionNode.Vec2Position = targetPosition;
				}

				// @diogo: disconnect from the input side only - pair-exact, one wire line per connected input port -
				// and quietly ( propagateCallback:false ): both endpoints are kept nodes whose lines must stay identical
				foreach ( string wireLine in diff.WiresToDisconnect )
				{
					string[] parameters = JsonGraphFormat.SplitInstruction( wireLine );
					int inNodeId = Convert.ToInt32( parameters[ IOUtils.InNodeId ] );
					int inPortId = Convert.ToInt32( parameters[ IOUtils.InPortId ] );
					m_mainGraphInstance.DeleteConnection( true, inNodeId, inPortId, false, false, false );
				}

				foreach ( string wireLine in diff.WiresToConnect )
				{
					string[] parameters = JsonGraphFormat.SplitInstruction( wireLine );

					// @diogo: a wire that cannot fully resolve means the patched graph shape is already wrong -
					// bail to the full reload instead of silently skipping the wire and leaving it to the oracle
					ParentNode wireInNode = m_mainGraphInstance.GetNode( Convert.ToInt32( parameters[ IOUtils.InNodeId ] ) );
					ParentNode wireOutNode = m_mainGraphInstance.GetNode( Convert.ToInt32( parameters[ IOUtils.OutNodeId ] ) );
					if ( wireInNode == null || wireOutNode == null ||
						wireInNode.GetInputPortByUniqueId( Convert.ToInt32( parameters[ IOUtils.InPortId ] ) ) == null ||
						wireOutNode.GetOutputPortByUniqueId( Convert.ToInt32( parameters[ IOUtils.OutPortId ] ) ) == null )
					{
						UndoProfiler.RecordPatchBail( "unresolvable wire " + wireLine );
						return false;
					}

					ApplyWireInstruction( m_mainGraphInstance, ref parameters, bucketTimer );
				}

				// @diogo: recreated frames hold pending member ids; resolve them eagerly in target order ( outer frames
				// first ) so membership serializes correctly for the oracle instead of waiting for the next layout
				foreach ( int id in diff.TargetNodeOrder )
				{
					if ( m_mainGraphInstance.GetNode( id ) is CommentaryNode commentary )
					{
						commentary.ResolvePendingMemberIds();
					}
				}

				double applyMs = phaseTimer.Elapsed.TotalMilliseconds;
				phaseTimer.Restart();

				// @diogo: scoped post-passes: RefreshExternalReferences is a run-once load contract, recreated nodes only
				m_mainGraphInstance.CheckForDuplicates();
				m_mainGraphInstance.UpdateRegisters();
				foreach ( int id in diff.TargetNodeOrder )
				{
					if ( recreateIds.Contains( id ) || addedSet.Contains( id ) )
					{
						ParentNode node = m_mainGraphInstance.GetNode( id );
						if ( node != null )
						{
							node.RefreshExternalReferences();
						}
					}
				}
				m_mainGraphInstance.ApplyDeferredWires();

				// @diogo: activation counters are increment-only and non-serialized: reset to the fresh-load precondition,
				// then re-propagate from the master node, then drop any WireNode auto-delete marks the disconnects queued
				m_mainGraphInstance.ResetActivationStates();
				m_mainGraphInstance.ForceSignalPropagationOnMasterNode();
				m_mainGraphInstance.ClearMarkedForDeletion();

				m_mainGraphInstance.IsLoading = false;
				isLoadingSet = false;

				m_mainGraphInstance.ScheduleFrameReorder();

				double postMs = phaseTimer.Elapsed.TotalMilliseconds;
				phaseTimer.Restart();

				// @diogo: dev-only fault injection: consume the one-shot toggle and force the oracle to miss
				bool injectVerifyFailure = UndoProfiler.InjectVerifyFailure;
				UndoProfiler.InjectVerifyFailure = false;

				// @diogo: the oracle: the patched graph must serialize to the exact target state, order aside
				string patchedMeta = GenerateGraphSnapshot();
				double verifyMs = phaseTimer.Elapsed.TotalMilliseconds;
				phaseTimer.Restart();
				bool verifyPassed = !injectVerifyFailure && !string.IsNullOrEmpty( patchedMeta ) &&
					ChecksumOfSnapshot( patchedMeta ).Equals( ChecksumOfSnapshot( targetSnapshot ) );
				double checksumMs = phaseTimer.Elapsed.TotalMilliseconds;
				phaseTimer.Restart();
				if ( !verifyPassed )
				{
					if ( injectVerifyFailure && Preferences.User.UndoProfiling )
					{
						Debug.Log( "[ASE Undo] verify failure injected via developer toggle" );
					}

					// @diogo: diagnostic only: diff the patched serialize against the target so the log names the drifting lines
					UndoSnapshotDiff.SnapshotDiffResult verifyDiff = null;
					if ( Preferences.User.UndoProfiling && !string.IsNullOrEmpty( patchedMeta ) )
					{
						verifyDiff = UndoSnapshotDiff.Diff( patchedMeta, targetSnapshot );
					}
					UndoProfiler.RecordPatchVerifyFail( totalTimer.Elapsed.TotalMilliseconds, recreateIds.Count + addedSet.Count, verifyDiff );
					return false;
				}

				foreach ( KeyValuePair<int, string[]> entry in viewState )
				{
					ParentNode node = m_mainGraphInstance.GetNode( entry.Key );
					if ( node != null )
					{
						int index = 0;
						node.ReadUndoViewState( entry.Value, ref index );
					}
				}

				m_mainGraphInstance.SelectNodesFromIds( selectedNodeIds );
				double selectMs = phaseTimer.Elapsed.TotalMilliseconds;

				InvalidatePropertyLists();

				m_repaintIsDirty = true;
				m_saveIsDirty = true;

				// @diogo: same recheck contract as the full reload: compare the restored bytes, not a re-serialize
				m_recheckSnapshot = targetSnapshot;
				m_recheckModified = true;

				// @diogo: the oracle just proved the live graph serializes to the target, so the target is
				// now the freshest live-state capture
				m_lastLiveGraphSerialize = targetSnapshot;

				UndoProfiler.RecordPatchSuccess( totalTimer.Elapsed.TotalMilliseconds, serializeMs, diffMs, applyMs, postMs, verifyMs, checksumMs, selectMs,
					diff.ChangedIds.Count, diff.PositionOnlyIds.Count, diff.AddedIds.Count, diff.RemovedIds.Count, diff.WiresToConnect.Count, diff.WiresToDisconnect.Count );
				return true;
			}
			catch ( Exception e )
			{
				if ( DebugConsoleWindow.DeveloperMode )
				{
					Debug.LogException( e );
				}
				UndoProfiler.RecordPatchBail( "exception: " + e.GetType().Name );
				return false;
			}
			finally
			{
				if ( isLoadingSet )
				{
					m_mainGraphInstance.IsLoading = false;
				}
				UIUtils.CurrentWindow = cachedWindow;
			}
		}

		public void ShowPortInfo()
		{
			GetWindow<PortLegendInfo>();
		}

		public void ShowShaderLibrary()
		{
			GetWindow<ShaderLibrary>();
		}

		public void ShowMessage( string message, MessageSeverity severity = MessageSeverity.Normal, bool registerTimestamp = true, bool consoleLog = false )
		{
			ShowMessage( -1, message, severity, registerTimestamp, consoleLog );
		}

		public void ShowMessage( int messageOwner, string message, MessageSeverity severity = MessageSeverity.Normal, bool registerTimestamp = true, bool consoleLog = false )
		{
			if( UIUtils.InhibitMessages || m_genericMessageUI == null )
				return;

			m_consoleLogWindow.AddMessage( severity, message , messageOwner);

			MarkToRepaint();

			if( consoleLog )
			{
				switch( severity )
				{
					case MessageSeverity.Normal:
					{
						Debug.Log( message );
					}
					break;
					case MessageSeverity.Warning:
					{
						Debug.LogWarning( message );
					}
					break;
					case MessageSeverity.Error:
					{
						Debug.LogError( message );
					}
					break;
				}
			}

			if ( Preferences.User.GraphLogToConsole )
			{
				string shaderPath = string.Empty;
				if ( m_mainGraphInstance != null )
				{
					if ( m_mainGraphInstance.CurrentShader != null )
					{
						shaderPath = " [" + AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShader ) + "]";
					}
					else if ( m_mainGraphInstance.CurrentShaderFunction != null )
					{
						shaderPath = " [" + AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShaderFunction ) + "]";
					}
				}
				string fullMessage = "[AmplifyShaderEditor]" + shaderPath + " " + message;
				switch ( severity )
				{
					case MessageSeverity.Normal:
					{
						Debug.Log( fullMessage );
					}
					break;
					case MessageSeverity.Warning:
					{
						Debug.LogWarning( fullMessage );
					}
					break;
					case MessageSeverity.Error:
					{
						Debug.LogError( fullMessage );
					}
					break;
				}
			}
		}

		// NOTE: this can probably be removed safely
		public void ShowMessageImmediately( string message, MessageSeverity severity = MessageSeverity.Normal, bool consoleLog = true )
		{
			if( UIUtils.InhibitMessages )
				return;

			switch( severity )
			{
				case MessageSeverity.Normal:
				{
					m_genericMessageContent.text = message;
					if( consoleLog )
					{
						Debug.Log( message );
					}
				}
				break;
				case MessageSeverity.Warning:
				{
					m_genericMessageContent.text = "Warning!\n" + message;
					if( consoleLog )
					{
						Debug.LogWarning( message );
					}
				}
				break;
				case MessageSeverity.Error:
				{
					m_genericMessageContent.text = "Error!!!\n" + message;
					if( consoleLog )
					{
						Debug.LogError( message );
					}
				}
				break;
			}

			try
			{
				ShowNotification( m_genericMessageContent );
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}
		}

		public bool MouseInteracted = false;

		// @diogo: keeps both side panels at the width the user asked for until they would touch, past which
		// the remaining room is divided proportionally between them
		private void RefitSidePanels()
		{
			float available = position.width;
			float desired = 0;

			if ( m_nodeParametersWindow.IsMaximized )
			{
				desired += m_nodeParametersWindow.DesiredWidth;
			}
			else
			{
				available -= m_nodeParametersWindow.RealWidth;
			}

			if ( m_paletteWindow.IsMaximized )
			{
				desired += m_paletteWindow.DesiredWidth;
			}
			else
			{
				available -= m_paletteWindow.RealWidth;
			}

			if ( desired > 0 )
			{
				float scale = Mathf.Clamp01( available / desired );
				if ( m_nodeParametersWindow.IsMaximized )
				{
					m_nodeParametersWindow.ResizeTo( m_nodeParametersWindow.DesiredWidth * scale );
				}

				if ( m_paletteWindow.IsMaximized )
				{
					m_paletteWindow.ResizeTo( m_paletteWindow.DesiredWidth * scale );
				}
			}
		}

		void OnGUI()
		{
			if( ASEPackageManagerHelper.CheckImporter )
				return;

#if UNITY_EDITOR_WIN
			if( m_openSavedFolder && Event.current.type == EventType.Repaint )
			{
				OpenSavedFolder();
				return;
			}
#endif
			AmplifyShaderEditorWindow cacheWindow = UIUtils.CurrentWindow;
			UIUtils.CurrentWindow = this;

			if( !m_initialized || (object)UIUtils.MainSkin == null || !UIUtils.Initialized )
			{
				UIUtils.InitMainSkin();
				Init();
			}

			m_currentEvent = Event.current;
			if( m_currentEvent.type == EventType.ExecuteCommand || m_currentEvent.type == EventType.ValidateCommand )
				m_currentCommandName = m_currentEvent.commandName;
			else
				m_currentCommandName = string.Empty;

			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			MouseInteracted = false;

			// End the gesture-coalescing window on a mouse press so the next drag/edit takes a fresh
			// pre-edit snapshot. rawType is used because a slider/field that consumes the event would
			// otherwise hide it from this top-level pass. Only a press ends the gesture: a slider
			// commits its final value change during the MouseUp pass itself, so resetting on release
			// ( before that change is processed ) split one drag into two undo steps.
			EventType undoGestureRawType = m_currentEvent.rawType;
			if ( undoGestureRawType == EventType.MouseUp || undoGestureRawType == EventType.MouseDown )
			{
				if ( m_gestureUndoActive )
				{
					// An edit gesture ( node move, slider/value drag ) just ended; re-check whether the
					// graph still matches the last saved state so the Save indicator can return to green.
					m_recheckModified = true;
				}
				if ( undoGestureRawType == EventType.MouseDown )
				{
					m_gestureUndoActive = false;
				}
			}

			if( m_reloadFromSnapshot )
			{
				m_reloadFromSnapshot = false;
				RestoreFromUndoSnapshot();
			}

			if( m_pendingBackupSnapshot != null )
			{
				string backupSnapshot = m_pendingBackupSnapshot;
				m_pendingBackupSnapshot = null;
				LoadFromBackup( backupSnapshot );
			}

			if( m_pendingTypeChangeType != null )
			{
				int[] changeNodeIds = m_pendingTypeChangeNodeIds;
				System.Type changeType = m_pendingTypeChangeType;
				m_pendingTypeChangeNodeIds = null;
				m_pendingTypeChangeType = null;
				PerformPropertyTypeChange( changeNodeIds, changeType );
			}

			if( m_refreshAvailableNodes )
			{
				RefreshAvaibleNodes();
			}

			if( m_previousShaderFunction != CurrentGraph.CurrentShaderFunction )
			{
				m_nodeParametersWindow.ForceUpdate = true;
				m_previousShaderFunction = CurrentGraph.CurrentShaderFunction;
			}

			if( m_nodeToFocus != null && m_currentEvent.type == EventType.Layout )
			{
				FocusOnNode( m_nodeToFocus, m_zoomToFocus, m_selectNodeToFocus );
				m_nodeToFocus = null;
			}

			m_mainGraphInstance.OnDuplicateEventWrapper();

			m_currentInactiveTime = CalculateInactivityTime();

			if( m_nodeParametersWindow != null && m_innerEditorVariables.NodeParametersMaximized != m_nodeParametersWindow.IsMaximized )
				m_innerEditorVariables.NodeParametersMaximized = m_nodeParametersWindow.IsMaximized;
			if( m_paletteWindow != null && m_innerEditorVariables.NodePaletteMaximized != m_paletteWindow.IsMaximized )
				m_innerEditorVariables.NodePaletteMaximized = m_paletteWindow.IsMaximized;

			if( m_checkInvalidConnections )
			{
				m_checkInvalidConnections = false;
				// @diogo: deferred post-load cleanup, not a user edit: its DeleteConnection calls must not register undo steps
				BeginSuppressUndoRegistration();
				try
				{
					m_mainGraphInstance.DeleteInvalidConnections();
				}
				finally
				{
					EndSuppressUndoRegistration();
				}
			}

			//if ( m_repaintIsDirty )
			//{
			//	m_repaintIsDirty = false;
			//	ForceRepaint();
			//}

			if( m_forcingMaterialUpdateFlag )
			{
				Focus();
				if( m_materialsToUpdate.Count > 0 )
				{
					float percentage = 100.0f * (float)( UIUtils.TotalExampleMaterials - m_materialsToUpdate.Count ) / (float)UIUtils.TotalExampleMaterials;
					if( m_forcingMaterialUpdateOp ) // Read
					{
						Debug.Log( percentage + "% Recompiling " + m_materialsToUpdate[ 0 ].name );
						LoadDroppedObject( true, m_materialsToUpdate[ 0 ].shader, m_materialsToUpdate[ 0 ] );
					}
					else // Write
					{
						Debug.Log( percentage + "% Saving " + m_materialsToUpdate[ 0 ].name );
						SaveToDisk( false );
						m_materialsToUpdate.RemoveAt( 0 );
					}
					m_forcingMaterialUpdateOp = !m_forcingMaterialUpdateOp;
				}
				else
				{
					Debug.Log( "100% - All Materials compiled " );
					m_forcingMaterialUpdateFlag = false;
				}
			}


			if( m_removedKeyboardFocus )
			{
				m_removedKeyboardFocus = false;
				GUIUtility.keyboardControl = 0;
			}


			Vector2 pos = m_currentEvent.mousePosition;
			pos.x += position.x;
			pos.y += position.y;
			m_insideEditorWindow = position.Contains( pos );

			if( m_delayedLoadObject != null && m_mainGraphInstance.CurrentMasterNode != null )
			{
				m_mainGraphInstance.SetLateOptionsRefresh();
				LoadDelayedObject();
			}
			else if( m_delayedLoadObject != null && m_mainGraphInstance.CurrentOutputNode != null )
			{
				m_mainGraphInstance.SetLateOptionsRefresh();
				LoadDelayedObject();
			}

			if( m_delayedMaterialSet != null && m_mainGraphInstance.CurrentMasterNode != null )
			{
				m_mainGraphInstance.UpdateMaterialOnMasterNode( m_delayedMaterialSet );
				m_mainGraphInstance.SetMaterialModeOnGraph( m_delayedMaterialSet );
				CurrentSelection = ASESelectionMode.Material;
				IsShaderFunctionWindow = false;
				m_delayedMaterialSet = null;
			}

			Material currentMaterial = m_mainGraphInstance.CurrentMaterial;
			if( m_forceUpdateFromMaterialFlag )
			{
				Focus();
				m_forceUpdateFromMaterialFlag = false;
				if( currentMaterial != null )
				{
					m_mainGraphInstance.CopyValuesFromMaterial( currentMaterial );
					m_repaintIsDirty = true;
				}
			}

			m_repaintCount = 0;
			m_cameraInfo = position;

			//if( m_currentEvent.type == EventType.keyDown )
			if( m_currentEvent.type == EventType.Repaint )
				m_keyEvtMousePos2D = m_currentEvent.mousePosition;

			m_currentMousePos2D = m_currentEvent.mousePosition;
			m_currentMousePos.x = m_currentMousePos2D.x;
			m_currentMousePos.y = m_currentMousePos2D.y;

			// @diogo: the preview color picker keys off Ctrl alone, and Repaint events carry no reliable modifier
			// state, so sample it from the events that do. Repaint directly instead of flagging m_repaintIsDirty:
			// that request is only consumed after the preview pass throttle, which lags the readout behind the
			// cursor by several frames. Cursor tracking only pays the extra repaints while Ctrl is actually held.
			if ( m_currentEvent.type != EventType.Repaint && m_currentEvent.type != EventType.Layout )
			{
				bool controlChanged = m_controlKeyPressed != m_currentEvent.control;
				m_controlKeyPressed = m_currentEvent.control;

				if ( controlChanged || ( m_controlKeyPressed && m_currentEvent.type == EventType.MouseMove ) )
				{
					Repaint();
				}
			}

			m_graphArea.width = m_cameraInfo.width;
			m_graphArea.height = m_cameraInfo.height;

			m_autoPanDirActive = m_lmbPressed || m_forceAutoPanDir || m_multipleSelectionActive || m_wireReferenceUtils.ValidReferences();


			// Need to use it in order to prevent Mismatched LayoutGroup on ValidateCommand when rendering nodes
			//if( Event.current.type == EventType.ValidateCommand )
			//{
			//	Event.current.Use();
			//}

			// Nodes Graph background area
			//GUILayout.BeginArea( m_graphArea, "Nodes" );
			{
				// Camera movement is simulated by grabing the current camera offset, transforming it into texture space and manipulating the tiled texture uv coords
				GUI.DrawTextureWithTexCoords( m_graphArea, m_graphBgTexture,
					new Rect( ( -m_cameraOffset.x / m_graphBgTexture.width ),
								( m_cameraOffset.y / m_graphBgTexture.height ) - m_cameraZoom * m_cameraInfo.height / m_graphBgTexture.height,
								m_cameraZoom * m_cameraInfo.width / m_graphBgTexture.width,
								m_cameraZoom * m_cameraInfo.height / m_graphBgTexture.height ) );

				Color col = GUI.color;
				GUI.color = new Color( 1, 1, 1, 0.7f );
				GUI.DrawTexture( m_graphArea, m_graphFgTexture, ScaleMode.StretchToFill, true );
				GUI.color = col;
			}
			//GUILayout.EndArea();

			if( DebugConsoleWindow.DeveloperMode && m_currentEvent.type == EventType.Repaint )
			{
				GUI.Label( new Rect(Screen.width - 60, 40, 60, 50), m_fpsDisplay );
			}

			bool restoreMouse = false;
			if( InsideMenus( m_currentMousePos2D ) /*|| _confirmationWindow.IsActive*/ )
			{
				if( Event.current.type == EventType.MouseDown )
				{
					restoreMouse = true;
					Event.current.type = EventType.Ignore;
				}

				// Must guarantee that mouse up ops on menus will reset auto pan if it is set
				if( m_currentEvent.type == EventType.MouseUp && m_currentEvent.button == ButtonClickId.LeftMouseButton )
				{
					m_lmbPressed = false;
				}

			}
			// Nodes
			//GUILayout.BeginArea( m_graphArea );
			{
				m_drawInfo.CameraArea = m_cameraInfo;
				m_drawInfo.TransformedCameraArea = m_graphArea;

				m_drawInfo.MousePosition = m_currentMousePos2D;
				m_drawInfo.CameraOffset = m_cameraOffset;
				m_drawInfo.InvertedZoom = 1 / m_cameraZoom;
				m_drawInfo.LeftMouseButtonPressed = m_currentEvent.button == ButtonClickId.LeftMouseButton;
				m_drawInfo.CurrentEventType = m_currentEvent.type;
				m_drawInfo.ZoomChanged = m_zoomChanged;

				m_drawInfo.TransformedMousePos = m_currentMousePos2D * m_cameraZoom - m_cameraOffset;

				if( m_drawInfo.CurrentEventType == EventType.Repaint )
					UIUtils.UpdateMainSkin( m_drawInfo );

				// Draw mode indicator
				m_modeWindow.Draw( m_graphArea, m_currentMousePos2D, m_mainGraphInstance.CurrentShader, currentMaterial,
									0.5f * ( m_graphArea.width - m_paletteWindow.RealWidth - m_nodeParametersWindow.RealWidth ),
									( m_nodeParametersWindow.IsMaximized ? m_nodeParametersWindow.RealWidth : 0 ),
									( m_paletteWindow.IsMaximized ? m_paletteWindow.RealWidth : 0 )/*, m_openedAssetFromNode*/ );

				PreTestLeftMouseDown();
				//m_consoleLogWindow.Draw( m_graphArea, m_currentMousePos2D, m_currentEvent.button, false, m_paletteWindow.IsMaximized ? m_paletteWindow.RealWidth : 0 );
				//m_mainGraphInstance.DrawBezierBoundingBox();
				//CheckNodeReplacement();

				// Main Graph Draw
				m_repaintIsDirty = m_mainGraphInstance.Draw( m_drawInfo ) || m_repaintIsDirty;

				m_mainGraphInstance.DrawGrid( m_drawInfo );
				bool hasUnusedConnNodes = m_mainGraphInstance.HasUnConnectedNodes;
				m_toolsWindow.SetStateOnButton( ToolButtonType.CleanUnusedNodes, hasUnusedConnNodes ? 1 : 0 );

				m_zoomChanged = false;

				MasterNode masterNode = m_mainGraphInstance.CurrentMasterNode;
				if( masterNode != null )
				{
					m_toolsWindow.DrawShaderTitle( m_nodeParametersWindow, m_paletteWindow, AvailableCanvasWidth, m_graphArea.height, masterNode.CroppedShaderName );
				}
				else if( m_mainGraphInstance.CurrentOutputNode != null )
				{
					string functionName = string.Empty;

					if( m_mainGraphInstance.CurrentShaderFunction != null )
						functionName = m_mainGraphInstance.CurrentShaderFunction.FunctionName;
					m_toolsWindow.DrawShaderTitle( m_nodeParametersWindow, m_paletteWindow, AvailableCanvasWidth, m_graphArea.height, functionName );
				}
			}
			//m_consoleLogWindow.Draw( m_graphArea, m_currentMousePos2D, m_currentEvent.button, false, m_paletteWindow.IsMaximized ? m_paletteWindow.RealWidth : 0 );
			//GUILayout.EndArea();

			if( restoreMouse )
			{
				Event.current.type = EventType.MouseDown;
				m_drawInfo.CurrentEventType = EventType.MouseDown;
			}

			m_toolsWindow.InitialX = m_nodeParametersWindow.RealWidth;
			m_toolsWindow.Width = m_cameraInfo.width - ( m_nodeParametersWindow.RealWidth + m_paletteWindow.RealWidth );
			// @diogo: skip interactive side panels during a batch resave: the save state machine ( end of OnGUI )
			// swaps/closes the graph mid-frame, so a panel's control count can differ between Layout and Repaint.
			// IsBatchProcessing is stable across a frame; each Draw is self-balanced, so skipping it can't unbalance layout.
			if ( !IsBatchProcessing )
			{
				m_toolsWindow.Draw( m_cameraInfo, m_currentMousePos2D, m_currentEvent.button, false );

				m_tipsWindow.Draw( m_cameraInfo, m_currentMousePos2D, m_currentEvent.button, false );
			}

			// @diogo: side panels used to be force-closed on a narrow window; they now give up width instead
			if ( position.width != m_lastWindowWidth )
			{
				RefitSidePanels();
			}

			ParentNode selectedNode = ( m_mainGraphInstance.SelectedNodes.Count == 1 ) ? m_mainGraphInstance.SelectedNodes[ 0 ] : m_mainGraphInstance.CurrentMasterNode;

			// @diogo: the node parameters panel draws ( and consumes its click ) before the palette, so a click there never
			// reaches the palette's deselect; clear the backup selection ( and commit any rename ) here first.
			if ( m_currentEvent.type == EventType.MouseDown && m_paletteWindow != null && m_nodeParametersWindow.IsInside( m_currentMousePos2D ) )
			{
				m_paletteWindow.CommitAndClearBackupSelection();
			}

			m_repaintIsDirty = ( State == OpenSaveState.NONE && m_nodeParametersWindow.Draw( m_cameraInfo, selectedNode, m_currentMousePos2D, m_currentEvent.button, false ) ) || m_repaintIsDirty; //TODO: If multiple nodes from the same type are selected also show a parameters window which modifies all of them
			if( m_nodeParametersWindow.IsResizing )
				m_repaintIsDirty = true;

			// Test to ignore mouse on main palette when inside context palette ... IsInside also takes active state into account
			bool ignoreMouseForPalette = m_contextPalette.IsInside( m_currentMousePos2D );
			if( ignoreMouseForPalette && Event.current.type == EventType.MouseDown )
			{
				Event.current.type = EventType.Ignore;
				m_drawInfo.CurrentEventType = EventType.Ignore;
			}

			// @diogo: palette skipped during batch resave for the same reason as the tools/tips panels above.
			if ( !IsBatchProcessing )
			{
				m_paletteWindow.Draw( m_cameraInfo, m_currentMousePos2D, m_currentEvent.button, !m_contextPalette.IsActive );
			}
			if( m_paletteWindow.IsResizing )
			{
				m_repaintIsDirty = true;
			}

			if( ignoreMouseForPalette )
			{
				if( restoreMouse )
				{
					Event.current.type = EventType.MouseDown;
					m_drawInfo.CurrentEventType = EventType.MouseDown;
				}
			}

			// @diogo: console log skipped during batch resave for the same reason as the tools/tips/palette panels above.
			if ( !IsBatchProcessing )
			{
				m_consoleLogWindow.Draw( m_graphArea, m_currentMousePos2D, m_currentEvent.button, false, m_nodeParametersWindow.IsMaximized ? m_nodeParametersWindow.RealWidth : 0, m_paletteWindow.IsMaximized ? m_paletteWindow.RealWidth : 0 );
			}

			if( Preferences.User.ShowStats && m_currentEvent.type == EventType.Repaint )
			{
				DrawStatsOverlay();
			}

			// @diogo: context palette / palette popup skipped during batch resave, same rationale as the panels above.
			if( m_contextPalette.IsActive && !IsBatchProcessing )
			{
				m_contextPalette.Draw( m_cameraInfo, m_currentMousePos2D, m_currentEvent.button, m_contextPalette.IsActive );
			}

			if( m_palettePopup.IsActive && !IsBatchProcessing )
			{
				m_palettePopup.Draw( m_currentMousePos2D );
				m_repaintIsDirty = true;
				int controlID = GUIUtility.GetControlID( FocusType.Passive );
				if( m_currentEvent.GetTypeForControl( controlID ) == EventType.MouseUp )
				{
					if( m_currentEvent.button == ButtonClickId.LeftMouseButton )
					{
						m_palettePopup.Deactivate();
						if( !InsideMenus( m_currentMousePos2D ) )
						{
							ParentNode newNode = CreateNode( m_paletteChosenType, TranformedMousePos, m_paletteChosenFunction );
							//Debug.Log("created menu");
							m_mainGraphInstance.SelectNode( newNode, false, false );

							bool find = false;
							if( newNode is FunctionNode && CurrentGraph.CurrentShaderFunction != null )
								find = SearchFunctionNodeRecursively( CurrentGraph.CurrentShaderFunction );

							if( find )
							{
								DestroyNode( newNode, false );
								ShowMessage( "Shader Function loop detected, new node was removed to prevent errors." );
							}
							else
							{
								newNode.RefreshExternalReferences();
							}
						}
					}
				}
			}

			// Handle all events ( mouse interaction + others )
			if( !MouseInteracted )
				HandleGUIEvents();

			if( m_currentEvent.type == EventType.Repaint )
			{
				m_mainGraphInstance.UpdateMarkForDeletion();
			}
			// UI Overlay
			// Selection Box
			if( m_multipleSelectionActive )
			{
				UpdateSelectionArea();
				Rect transformedArea = m_multipleSelectionArea;
				transformedArea.position = ( transformedArea.position + m_cameraOffset ) / m_cameraZoom;
				transformedArea.size /= m_cameraZoom;

				if( transformedArea.width < 0 )
				{
					transformedArea.width = -transformedArea.width;
					transformedArea.x -= transformedArea.width;
				}

				if( transformedArea.height < 0 )
				{
					transformedArea.height = -transformedArea.height;
					transformedArea.y -= transformedArea.height;
				}
				Color original = GUI.color;
				GUI.color = Constants.BoxSelectionColor;
				GUI.Label( transformedArea, "", UIUtils.Box );
				GUI.color = original;
				//GUI.backgroundColor = original;
			}

			bool isResizing = m_nodeParametersWindow.IsResizing || m_paletteWindow.IsResizing;
			//Test boundaries for auto-pan
			if( !isResizing && m_autoPanDirActive )
			{
				m_autoPanArea[ (int)AutoPanLocation.LEFT ].AdjustInitialX = m_nodeParametersWindow.IsMaximized ? m_nodeParametersWindow.RealWidth : 0;
				m_autoPanArea[ (int)AutoPanLocation.RIGHT ].AdjustInitialX = m_paletteWindow.IsMaximized ? -m_paletteWindow.RealWidth : 0;
				Vector2 autoPanDir = Vector2.zero;
				for( int i = 0; i < m_autoPanArea.Length; i++ )
				{
					if( m_autoPanArea[ i ].CheckArea( m_currentMousePos2D, m_cameraInfo, false ) )
					{
						autoPanDir += m_autoPanArea[ i ].Velocity;
					}
				}
				m_cameraOffset += autoPanDir;
				if( !m_wireReferenceUtils.ValidReferences() && m_insideEditorWindow && !m_altBoxSelection )
				{
					m_mainGraphInstance.MoveSelectedNodes( -autoPanDir );
				}

				m_repaintIsDirty = true;
			}

			m_isDirty = m_isDirty || m_mainGraphInstance.IsDirty;
			if( m_isDirty )
			{
				m_isDirty = false;
				//ShaderIsModified = true;
				EditorUtility.SetDirty( this );
			}

			m_saveIsDirty = m_saveIsDirty || m_mainGraphInstance.SaveIsDirty;
			if( m_liveShaderEditing )
			{
				if( m_saveIsDirty )
				{
					if( focusedWindow == this && m_currentInactiveTime > InactivitySaveTime && !(EditorGUIUtility.editingTextField && EditorGUIUtility.keyboardControl!=0) )
					{
						m_saveIsDirty = false;
						if( m_mainGraphInstance.CurrentMasterNodeId != Constants.INVALID_NODE_ID )
						{
							SaveToDisk( true );
						}
						else
						{
							ShowMessage( LiveShaderError );
						}
					}
				}
			}
			else if( m_saveIsDirty )
			{
				// While an undo/redo verdict is latched, the dirty here is post-reload churn, not a user
				// edit: consume it but leave the checksum verdict ( set in EvaluateModifiedState ) intact.
				if( !m_undoVerdictActive )
				{
					ShaderIsModified = true;
				}
				m_saveIsDirty = false;
			}

			if( m_onLoadDone > 0 )
			{
				m_onLoadDone--;
				if( m_onLoadDone == 0 )
				{
					ShaderIsModified = false;
				}
			}

			// One-shot accurate re-evaluation requested by a finished edit gesture or an undo/redo:
			// the flag set above only ever turns the indicator on; this compares against the saved
			// baseline so it can also turn back to green when the graph matches what was last saved.
			if( m_recheckModified )
			{
				m_recheckModified = false;
				EvaluateModifiedState();
			}

			if( m_cacheSaveOp )
			{
				if( ( EditorApplication.timeSinceStartup - m_lastTimeSaved ) > SaveTime )
				{
					SaveToDisk( false );
				}
			}
			m_genericMessageUI.CheckForMessages();

			if( m_ctrlSCallback )
			{
				m_ctrlSCallback = false;
				OnToolButtonPressed( ToolButtonType.Update );
			}

			if ( m_pendingToolButton.HasValue )
			{
				ToolButtonType pendingToolButton = m_pendingToolButton.Value;
				m_pendingToolButton = null;
				OnToolButtonPressed( pendingToolButton );
			}

			m_lastWindowWidth = position.width;
			m_nodeExporterUtils.Update();

			if( m_markedToSave )
			{
				m_markedToSave = false;
				SaveToDisk( false );
			}

			if( CheckFunctions )
				CheckFunctions = false;

			System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;

			UIUtils.CurrentWindow = cacheWindow;
			if( !m_nodesLoadedCorrectly )
			{
				try
				{
					ShowNotification( NodesExceptionMessage );
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
			}

			CheckNodeReplacement();
#if UNITY_EDITOR_WIN
			if( m_takeScreenShot )
				FocusZoom( true, false, false );

			if( m_takeScreenShot && Event.current.type == EventType.Repaint )
				TakeScreenShot();
#endif

			ProcessSaveStateMachine();
		}

		bool m_markToClose = false;

		void OnInspectorUpdate()
		{
			ASEPackageManagerHelper.Update();

			if( m_afterDeserializeFlag )
			{
				m_afterDeserializeFlag = false;
				//m_mainGraphInstance.ParentWindow = this;
			}

			if( (IsShaderFunctionWindow && CurrentGraph.CurrentShaderFunction == null) || m_markToClose )
			{
				Close();
			}
		}

		public void SetCtrlSCallback( bool imediate )
		{
			//MasterNode node = _mainGraphInstance.CurrentMasterNode;
			if( /*node != null && node.CurrentShader != null && */m_shaderIsModified )
			{
				if( imediate )
				{
					OnToolButtonPressed( ToolButtonType.Update );
				}
				else
				{
					m_ctrlSCallback = true;
				}
			}
		}

		public void SetSaveIsDirty()
		{
			m_saveIsDirty = true && UIUtils.DirtyMask;
		}

		public void OnPaletteNodeCreate( System.Type type, string name, AmplifyShaderFunction function )
		{
			m_mainGraphInstance.DeSelectAll();
			m_paletteChosenType = type;
			m_paletteChosenFunction = function;
			m_palettePopup.Activate( name );
		}

		public void OnContextPaletteNodeCreate( System.Type type, string name, AmplifyShaderFunction function )
		{
			m_mainGraphInstance.DeSelectAll();
			ParentNode newNode = CreateNode( type, m_contextPalette.StartDropPosition * m_cameraZoom - m_cameraOffset, function );
			//Debug.Log( "created context" );
			m_mainGraphInstance.SelectNode( newNode, false, false );
			bool find = false;
			if( newNode is FunctionNode && CurrentGraph.CurrentShaderFunction != null )
				find = SearchFunctionNodeRecursively( CurrentGraph.CurrentShaderFunction );

			if( find )
			{
				DestroyNode( newNode, false );
				ShowMessage( "Shader Function loop detected, new node was removed to prevent errors." );
			}
			else
			{
				newNode.RefreshExternalReferences();
			}
		}

		void OnNodeStoppedMovingEvent( ParentNode node )
		{
			CheckZoomBoundaries( node.Vec2Position );
			//ShaderIsModified = true;
		}

		// @diogo: the palette ranks entries by how often they occur in the graph, so a changed node set invalidates it.
		// null-checked because the graph events are bound before the palette itself is created.
		void OnGraphNodeSetChanged( ParentNode node )
		{
			if ( m_paletteWindow != null )
			{
				m_paletteWindow.ForceUpdate = true;
			}
		}

		void OnRefreshFunctionNodeEvent( FunctionNode node )
		{
			Debug.Log( node );
		}

		private const string ShaderIsModifiedMessage = "Click to save changes.";
		private const string ShaderIsNotModified = "No changes to save, up-to-date.";
		void OnMaterialUpdated( MasterNode masterNode )
		{
			if( masterNode != null )
			{
				if( masterNode.CurrentMaterial )
				{
					m_toolsWindow.SetStateOnButton( ToolButtonType.Update, ShaderIsModified ? 0 : 2, ShaderIsModified ? ShaderIsModifiedMessage : ShaderIsNotModified);
				}
				else
				{
					m_toolsWindow.SetStateOnButton( ToolButtonType.Update, 1, "Set an active Material in the Master Node." );
				}
				UpdateLiveUI();
			}
			else
			{
				m_toolsWindow.SetStateOnButton( ToolButtonType.Update, 1, "Set an active Material in the Master Node." );
			}
		}

		void OnShaderUpdated( MasterNode masterNode )
		{
			m_toolsWindow.SetStateOnButton( ToolButtonType.OpenSourceCode, masterNode.CurrentShader != null ? 1 : 0 );
		}

		public void CheckZoomBoundaries( Vector2 newPosition )
		{
			if( newPosition.x < m_minNodePos.x )
			{
				m_minNodePos.x = newPosition.x;
			}
			else if( newPosition.x > m_maxNodePos.x )
			{
				m_maxNodePos.x = newPosition.x;
			}

			if( newPosition.y < m_minNodePos.y )
			{
				m_minNodePos.y = newPosition.y;
			}
			else if( newPosition.y > m_maxNodePos.y )
			{
				m_maxNodePos.y = newPosition.y;
			}
		}
		public void DestroyNode( ParentNode node, bool registerUndo = true ) { m_mainGraphInstance.DestroyNode( node, registerUndo ); }
		public ParentNode CreateNode( System.Type type, Vector2 position, AmplifyShaderFunction function = null, bool selectNode = true )
		{
			ParentNode node;
			if( function == null )
				node = m_mainGraphInstance.CreateNode( type, true );
			else
				node = m_mainGraphInstance.CreateNode( function, true );

			Vector2 newPosition = position;
			node.Vec2Position = newPosition;
			CheckZoomBoundaries( newPosition );

			// Connect node if a wire is active
			if( m_wireReferenceUtils.ValidReferences() )
			{
				if( m_wireReferenceUtils.InputPortReference.IsValid )
				{
					ParentNode originNode = m_mainGraphInstance.GetNode( m_wireReferenceUtils.InputPortReference.NodeId );
					InputPort originPort = originNode.GetInputPortByUniqueId( m_wireReferenceUtils.InputPortReference.PortId );
					OutputPort outputPort = node.GetFirstOutputPortOfType( m_wireReferenceUtils.InputPortReference.DataType, true );
					// @diogo: linking a single side would leave a one-way reference, so require both ends
					if( outputPort != null && !originPort.Locked && !outputPort.Locked && originPort.CheckValidType( outputPort ) && ( !m_wireReferenceUtils.InputPortReference.TypeLocked ||
												m_wireReferenceUtils.InputPortReference.DataType == WirePortDataType.OBJECT ||
												( m_wireReferenceUtils.InputPortReference.TypeLocked && outputPort.DataType == m_wireReferenceUtils.InputPortReference.DataType ) ) )
					{

						//link output to input
						if( outputPort.ConnectTo( m_wireReferenceUtils.InputPortReference.NodeId, m_wireReferenceUtils.InputPortReference.PortId, m_wireReferenceUtils.InputPortReference.DataType, m_wireReferenceUtils.InputPortReference.TypeLocked ) )
							node.OnOutputPortConnected( outputPort.PortId, m_wireReferenceUtils.InputPortReference.NodeId, m_wireReferenceUtils.InputPortReference.PortId );

						//link input to output
						if( originPort.ConnectTo( outputPort.NodeId, outputPort.PortId, outputPort.DataType, m_wireReferenceUtils.InputPortReference.TypeLocked ) )
							originNode.OnInputPortConnected( m_wireReferenceUtils.InputPortReference.PortId, node.UniqueId, outputPort.PortId );
					}
				}

				if( m_wireReferenceUtils.OutputPortReference.IsValid )
				{
					ParentNode originNode = m_mainGraphInstance.GetNode( m_wireReferenceUtils.OutputPortReference.NodeId );
					InputPort inputPort = node.GetFirstInputPortOfType( m_wireReferenceUtils.OutputPortReference.DataType, true );

					// @diogo: GetFirstInputPortOfType has no lock filter, so it can hand back a locked
					// port whose ConnectTo then silently refuses, leaving a one-way reference
					if( inputPort != null && !inputPort.Locked && ( !inputPort.TypeLocked ||
													inputPort.DataType == WirePortDataType.OBJECT ||
													( inputPort.TypeLocked && inputPort.DataType == m_wireReferenceUtils.OutputPortReference.DataType ) ) )
					{

						inputPort.InvalidateAllConnections();
						//link input to output
						if( inputPort.ConnectTo( m_wireReferenceUtils.OutputPortReference.NodeId, m_wireReferenceUtils.OutputPortReference.PortId, m_wireReferenceUtils.OutputPortReference.DataType, inputPort.TypeLocked ) )
							node.OnInputPortConnected( inputPort.PortId, m_wireReferenceUtils.OutputPortReference.NodeId, m_wireReferenceUtils.OutputPortReference.PortId );
						//link output to input

						if( originNode.GetOutputPortByUniqueId( m_wireReferenceUtils.OutputPortReference.PortId ).ConnectTo( inputPort.NodeId, inputPort.PortId, m_wireReferenceUtils.OutputPortReference.DataType, inputPort.TypeLocked ) )
							originNode.OnOutputPortConnected( m_wireReferenceUtils.OutputPortReference.PortId, node.UniqueId, inputPort.PortId );
					}
				}
				m_wireReferenceUtils.InvalidateReferences();

				//for ( int i = 0; i < m_mainGraphInstance.VisibleNodes.Count; i++ )
				//{
				//	m_mainGraphInstance.VisibleNodes[ i ].OnNodeInteraction( node );
				//}
			}

			if( selectNode )
				m_mainGraphInstance.SelectNode( node, false, false );
			//_repaintIsDirty = true

			SetSaveIsDirty();
			ForceRepaint();
			return node;
		}

		private double m_previewUpdateLimiterTime = 0;

		// @diogo: per-window transient preview-RT pool (pooled SF previews, QA #5). SF-internal nodes borrow
		// RTs from here during a preview pass and hand them back when their SF finishes computing.
		private ASEPreviewRTPool m_previewRTPool = new ASEPreviewRTPool();
		public ASEPreviewRTPool PreviewRTPool { get { return m_previewRTPool; } }

		public void UpdateNodePreviewListAndTime()
		{
			if( UIUtils.CurrentWindow != this || !UIUtils.CurrentWindow.hasFocus )
				return;

			AutoBackupTick();

			// While the Auto Backup tab is open, repaint at a low rate so its relative timestamps stay
			// current ( the panel otherwise only redraws on input ). Gated on the tab being visible.
			if( m_paletteWindow != null && m_paletteWindow.IsAutoBackupTabActive )
			{
				double repaintNow = EditorApplication.timeSinceStartup;
				if( repaintNow >= m_nextAutoBackupRepaint )
				{
					m_nextAutoBackupRepaint = repaintNow + 1.0;
					Repaint();
				}
			}

			double deltaTime = Time.realtimeSinceStartup - m_time;
			m_time = Time.realtimeSinceStartup;

			// @diogo: skip absurd deltas ( first tick / regaining focus ) so the smoothed reading does not spike.
			if ( deltaTime < 1.0 )
			{
				m_previewSmoothDeltaTime += ( deltaTime - m_previewSmoothDeltaTime ) * 0.1;
			}

			// @diogo: EMA-smoothed frame time for the stats overlay ( Preferences.User.ShowStats ). Skip absurd
			// deltas ( first tick / regaining focus ) so the reading does not momentarily crater.
			if( Preferences.User.ShowStats && deltaTime < 1.0 )
			{
				m_statsFrameMs += ( deltaTime * 1000.0 - m_statsFrameMs ) * 0.1;
			}

			if ( DebugConsoleWindow.DeveloperMode )
			{
				m_frameCounter++;
				if( m_frameCounter >= 60 )
				{
					m_fpsDisplay = ( 60 / ( Time.realtimeSinceStartup - m_fpsTime ) ).ToString( "N2" );
					m_fpsTime = Time.realtimeSinceStartup;
					m_frameCounter = 0;
				}
			}

			if( m_smoothZoom )
			{
				m_repaintIsDirty = true;
				if( Mathf.Abs( m_targetZoom - m_cameraZoom ) < 0.001f )
				{
					m_smoothZoom = false;
					m_cameraZoom = m_targetZoom;
					m_zoomTime = 0;
				}
				else
				{
					m_zoomTime += deltaTime;
					Vector2 canvasPos = m_zoomPivot * m_cameraZoom;
					m_cameraZoom = Mathf.SmoothDamp( m_cameraZoom, m_targetZoom, ref m_zoomVelocity, 0.1f, 10000, (float)deltaTime * 1.5f );
					canvasPos = canvasPos - m_zoomPivot * m_cameraZoom;
					m_cameraOffset = m_cameraOffset - canvasPos;
					m_targetOffset = m_targetOffset - canvasPos;
				}

			}

			if( m_smoothOffset )
			{
				m_repaintIsDirty = true;
				if( ( m_targetOffset - m_cameraOffset ).SqrMagnitude() < 1f )
				{
					m_smoothOffset = false;
					m_offsetTime = 0;
				}
				else
				{
					m_offsetTime += deltaTime;
					m_cameraOffset = Vector2.SmoothDamp( m_cameraOffset, m_targetOffset, ref m_camVelocity, 0.1f, 100000, (float)deltaTime * 1.5f );
				}
			}

			if( m_cachedEditorTimeId == -1 )
				m_cachedEditorTimeId = Shader.PropertyToID( "_EditorTime" );

			if( m_cachedEditorDeltaTimeId == -1 )
				m_cachedEditorDeltaTimeId = Shader.PropertyToID( "_EditorDeltaTime" );

			//Update Game View?
			//Shader.SetGlobalVector( "_Time", new Vector4( Time.realtimeSinceStartup / 20, Time.realtimeSinceStartup, Time.realtimeSinceStartup * 2, Time.realtimeSinceStartup * 3 ) );

			//System.Type T = System.Type.GetType( "UnityEditor.GameView,UnityEditor" );
			//UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll( T );
			//EditorWindow gameView = ( array.Length <= 0 ) ? null : ( ( EditorWindow ) array[ 0 ] );
			//gameView.Repaint();

			if( RenderSettings.sun != null )
			{
				Vector3 lightdir = -RenderSettings.sun.transform.forward;//.rotation.eulerAngles;
				Color lightColor = RenderSettings.sun.color.linear;
				float lightIntensity = RenderSettings.sun.intensity;

				// Deprecated
				Shader.SetGlobalVector( "_EditorWorldLightPos", new Vector4( lightdir.x, lightdir.y, lightdir.z, 0 ) );
				Shader.SetGlobalVector( "_EditorLightColor", new Vector4( lightColor.r, lightColor.g, lightColor.b, lightIntensity ) );

				// New
				Shader.SetGlobalVector( "preview_EditorLightDirection", lightdir.normalized );
				Shader.SetGlobalVector( "preview_EditorLightColor", new Vector3( lightColor.r, lightColor.g, lightColor.b ) );
				Shader.SetGlobalFloat( "preview_EditorLightIntensity", lightIntensity );
			}

			// Deprecated
			Shader.SetGlobalFloat( "_EditorTime", (float)m_time );
			Shader.SetGlobalFloat( "_EditorDeltaTime", (float)deltaTime );

			// New
			Shader.SetGlobalFloat( "preview_EditorTime", (float)m_time );
			Shader.SetGlobalFloat( "preview_EditorDeltaTime", (float)deltaTime );
			Shader.SetGlobalFloat( "preview_EditorSmoothDeltaTime", (float)m_previewSmoothDeltaTime );
			Shader.SetGlobalFloat( "preview_EditorLastTime", (float)( m_time - deltaTime ) );

			// @diogo: limit preview update frequency to keep the CPU usage under control
			m_previewUpdateLimiterTime += deltaTime;
			if ( Math.Abs( m_previewUpdateLimiterTime ) < 1.0 / Preferences.User.PreviewUpdateFrequency )
			{
				return;
			}
			m_previewUpdateLimiterTime = 0;

			// @diogo: skip the preview pass during a batch resave - the graph is loaded only to resave and is
			// never displayed, so re-rendering every ( all-dirty ) node's preview is wasted GPU work that floods
			// the graphics ring buffer ( interactive drawing is already gated the same way ).
			if ( !Preferences.User.DisablePreviews && !IsBatchProcessing )
			{
				UpdateZoomScaledPreviewSize();

				bool statsMeasure = Preferences.User.ShowStats;
				if( statsMeasure )
				{
					m_statsStopwatch.Restart();
				}

				/////////// UPDATE PREVIEWS //////////////
				UIUtils.CheckNullMaterials();
				UIUtils.SetPreviewShaderConstants();
				ParentNode.BeginPreviewPass();
				//CurrentGraph.AllNodes.Sort( ( x, y ) => { return x.Depth.CompareTo( y.Depth ); } );
				int nodeCount = CurrentGraph.AllNodes.Count;
				for( int i = nodeCount - 1; i >= 0; i-- )
				{
					ParentNode node = CurrentGraph.AllNodes[ i ];
					// @diogo: root only on nodes whose preview is actually shown; RecursivePreviewUpdate walks
					// upstream, so this renders just the visible previews and their producer cones. Nothing shown
					// ( all collapsed / zoomed out ) renders nothing.
					if( node != null && node.IsPreviewVisible && !VisitedChanged.ContainsKey( node.OutputId ) )
					{
						bool result = node.RecursivePreviewUpdate();
						if( result )
							m_repaintIsDirty = true;
					}
				}

				VisitedChanged.Clear();
				// @diogo: every SF hands its pool RTs back when it finishes computing, so the pool must be
				// fully free here. Deliberately NO blanket ReturnAll(): that would re-free a stray un-returned
				// RT while its port still references it ( another node would then be handed the same RT ->
				// corruption ). Leaving a leak checked out instead surfaces it as visible pool growth, and the
				// developer-mode check below flags it (pooled SF previews, QA #5).
				if( DebugConsoleWindow.DeveloperMode && m_previewRTPool.InUse != 0 )
				{
					Debug.LogWarning( "[ASE] Preview RT pool leak: " + m_previewRTPool.InUse + " RT(s) not returned this pass." );
				}

				if( statsMeasure )
				{
					m_statsStopwatch.Stop();
					m_statsPreviewPassMs += ( m_statsStopwatch.Elapsed.TotalMilliseconds - m_statsPreviewPassMs ) * 0.1;
				}

			}

			// @diogo: consume the repaint request even when previews are disabled; smooth zoom/pan and node
			// drags set m_repaintIsDirty without doing preview work. Skipped only during batch resaves ( no canvas ).
			if( !IsBatchProcessing && m_repaintIsDirty )
			{
				m_repaintIsDirty = false;
				Repaint();
			}

			// @diogo: stats overlay upkeep ( Preferences.User.ShowStats ). Runs regardless of DisablePreviews so the
			// overlay stays live; 'Preview pass' decays toward zero when previews are disabled ( no compute pass ).
			if( Preferences.User.ShowStats )
			{
				if( Preferences.User.DisablePreviews )
				{
					m_statsPreviewPassMs += ( 0.0 - m_statsPreviewPassMs ) * 0.1;
				}

				// @diogo: throttled ( ~1 Hz ) preview-RT VRAM estimate: owned per-port RTs ( ACTUAL sizes - constants
				// are 1x1 ) + transient pool ( always full size ).
				m_statsVramTimer += deltaTime;
				if( m_statsVramTimer >= 1.0 )
				{
					m_statsVramTimer = 0.0;
					long bytes = SumOwnedPreviewBytes( CurrentGraph ) + m_previewRTPool.Allocated * PreviewRTBytes();
					m_statsVramMB = bytes / ( 1024.0 * 1024.0 );
				}

				Repaint();
			}
		}

		public void ForceRepaint()
		{
			m_repaintCount += 1;
			m_repaintIsDirty = true;
			//Repaint();
		}

		// @diogo: zoom-scaled previews (QA #5d). Maps the canvas zoom LOD to a preview RT size ( base, /2
		// or /4 of the Preview Quality size ) and, once the target size holds for a moment ( debounce ),
		// commits it: allocation sites read UIUtils.CurrentPreviewSize, the pool flushes itself on the next
		// checkout, and a recursive dirty makes every preview ( including SF-internal frontier RTs ) re-render
		// and re-allocate at the new size on this same pass. Swatches never upsample: each LOD band's size
		// stays at or above the on-screen pixel size of a node preview in that band.
		private void UpdateZoomScaledPreviewSize()
		{
			int baseSize = Preferences.User.PreviewSize;
			int size = baseSize;
			if( Preferences.User.ZoomScaledPreviews && CurrentGraph != null )
			{
				switch( CurrentGraph.LodLevel )
				{
					case ParentGraph.NodeLOD.LOD0:
					case ParentGraph.NodeLOD.LOD1:
					{
						size = baseSize;
					}
					break;
					case ParentGraph.NodeLOD.LOD2:
					case ParentGraph.NodeLOD.LOD3:
					{
						size = baseSize / 2;
					}
					break;
					default:
					{
						size = baseSize / 4;
					}
					break;
				}
			}

			if( size == UIUtils.CurrentPreviewSize )
			{
				m_zoomPreviewCandidateSize = 0;
				return;
			}

			// @diogo: hold the commit while the user is still zooming ( including smooth-zoom animation ) so
			// the re-render burst only happens once movement settles.
			double now = EditorApplication.timeSinceStartup;
			if( CameraZoom != m_zoomPreviewLastZoom || m_smoothZoom )
			{
				m_zoomPreviewLastZoom = CameraZoom;
				m_zoomPreviewCandidateSize = size;
				m_zoomPreviewCandidateTime = now;
				return;
			}

			if( size != m_zoomPreviewCandidateSize )
			{
				m_zoomPreviewCandidateSize = size;
				m_zoomPreviewCandidateTime = now;
				return;
			}

			if( now - m_zoomPreviewCandidateTime < 0.25 )
			{
				return;
			}

			m_zoomPreviewCandidateSize = 0;
			UIUtils.CurrentPreviewSize = size;
			Shader.SetGlobalVector( PreviewSizeGlobalVariable, new Vector4( size, size, 0, 0 ) );
			ActivatePreviewsRecursive( CurrentGraph );
		}

		// @diogo: dirties every preview in the graph AND inside nested Shader Functions, so a preview-size
		// change re-renders ( and thus re-allocates ) every RT, including SF-internal frontier RTs that a
		// plain main-graph dirty would leave at the old size (QA #5d).
		private void ActivatePreviewsRecursive( ParentGraph graph )
		{
			List<ParentNode> nodes = graph.AllNodes;
			for( int i = 0; i < nodes.Count; i++ )
			{
				ParentNode node = nodes[ i ];
				if( node == null )
				{
					continue;
				}

				node.PreviewIsDirty = true;
				if( node is FunctionNode fn && fn.FunctionGraph != null )
				{
					ActivatePreviewsRecursive( fn.FunctionGraph );
				}
			}
		}

		// @diogo: optional perf overlay ( Preferences.User.ShowStats ), top-left of the canvas. Shows the
		// EMA-smoothed frame time and preview compute time plus node count, preview-RT pool usage and zoom LOD
		// - handy for comparing preview settings ( e.g. pooled SF previews ) OFF vs ON (QA #5).
		private void DrawStatsOverlay()
		{
			double fps = m_statsFrameMs > 0.0001 ? 1000.0 / m_statsFrameMs : 0.0;
			int nodeCount = CurrentGraph != null ? CurrentGraph.AllNodes.Count : 0;
			int lod = CurrentGraph != null ? ( int )CurrentGraph.LodLevel : 0;
			string text = string.Format(
				"Frame: {0:0.0} ms ({1:0} fps)\nPreview pass: {2:0.00} ms\nNodes: {3}\nPool RTs: {4} / {5}\nEst. VRAM: {6:0.0} MB\nZoom LOD: {7}",
				m_statsFrameMs, fps, m_statsPreviewPassMs, nodeCount, m_previewRTPool.InUse, m_previewRTPool.Allocated, m_statsVramMB, lod );

			if( m_statsStyle == null )
			{
				m_statsStyle = new GUIStyle( EditorStyles.label );
				m_statsStyle.normal.textColor = Color.white;
				m_statsStyle.alignment = TextAnchor.UpperLeft;
				m_statsStyle.padding = new RectOffset( 6, 6, 4, 4 );
				m_statsStyle.richText = false;
			}

			float x = m_nodeParametersWindow.RealWidth + 10;
			float y = m_toolsWindow.Height + 2;
			Rect box = new Rect( x, y, 200, 112 );

			Color prevColor = GUI.color;
			GUI.color = new Color( 0f, 0f, 0f, 0.65f );
			GUI.DrawTexture( box, Texture2D.whiteTexture );
			GUI.color = prevColor;

			GUI.Label( box, text, m_statsStyle );
		}

		// @diogo: sums the ACTUAL byte size of the preview RTs owned by output ports across the graph and its shader
		// functions, for the VRAM estimate ( Preferences.User.ShowStats ). Reads each RT's real dimensions so 1x1
		// constant previews are counted at their true size. FunctionNode output ports alias their inner FunctionOutput
		// RTs, so they are skipped here and summed once via recursion (QA #5).
		private long SumOwnedPreviewBytes( ParentGraph graph )
		{
			if( graph == null )
			{
				return 0;
			}

			long bytes = 0;
			List<ParentNode> nodes = graph.AllNodes;
			for( int i = 0; i < nodes.Count; i++ )
			{
				ParentNode node = nodes[ i ];
				if( node == null )
				{
					continue;
				}

				if( node is FunctionNode fn )
				{
					bytes += SumOwnedPreviewBytes( fn.FunctionGraph );
					continue;
				}

				List<OutputPort> outputs = node.OutputPorts;
				if( outputs != null )
				{
					for( int p = 0; p < outputs.Count; p++ )
					{
						ASEPreviewSlice slice = outputs[ p ].OwnedPreviewSlice;
						if( slice.IsValid )
						{
							bytes += slice.Bytes;
						}
					}
				}
			}
			return bytes;
		}

		// @diogo: bytes-per-pixel for a preview RT format, for the actual-size VRAM estimate (QA #5).
		private long PreviewRTBytesPerPixel( RenderTextureFormat format )
		{
			switch( format )
			{
				case RenderTextureFormat.ARGBFloat: return 16;
				case RenderTextureFormat.ARGBHalf: return 8;
				case RenderTextureFormat.ARGB32:
				case RenderTextureFormat.Default: return 4;
				default: return 8;
			}
		}

		// @diogo: byte size of a single full-size preview RT at the current ( zoom-scaled ) size, for the
		// pool's VRAM estimate ( pool RTs are always allocated at the current preview size ) (QA #5).
		private long PreviewRTBytes()
		{
			long side = UIUtils.CurrentPreviewSize;
			return side * side * PreviewRTBytesPerPixel( Preferences.User.PreviewFormat );
		}

		public void ForceUpdateFromMaterial() { m_forceUpdateFromMaterialFlag = true; }
		void UseCurrentEvent()
		{
			m_currentEvent.Use();
		}



		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			m_afterDeserializeFlag = true;

			//m_customGraph = null;
		}

		void OnDestroy()
		{
			m_ctrlSCallback = false;
			AbortBatchOnWindowClose();
			Destroy();
		}

		public override void OnDisable()
		{
			base.OnDisable();
			m_ctrlSCallback = false;
			//EditorApplication.update -= UpdateTime;
			EditorApplication.update -= UpdateNodePreviewListAndTime;

			// @diogo: release the transient preview-RT pool's GPU memory (pooled SF previews, QA #5).
			m_previewRTPool.Flush();

			EditorApplication.update -= IOUtils.UpdateIO;

			for( int i = 0; i < IOUtils.AllOpenedWindows.Count; i++ )
			{
				if( IOUtils.AllOpenedWindows[ i ] != this )
				{
					EditorApplication.update += IOUtils.UpdateIO;
					break;
				}
			}
		}

		void OnEmptyGraphDetected( ParentGraph graph )
		{
			if( m_delayedLoadObject != null )
			{
				LoadDelayedObject();
				Repaint();
			}
			else
			{
				if( !string.IsNullOrEmpty( Lastpath ) )
				{
					Shader shader = AssetDatabase.LoadAssetAtPath<Shader>( Lastpath );
					if( shader == null )
					{
						Material material = AssetDatabase.LoadAssetAtPath<Material>( Lastpath );
						if( material != null )
						{
							LoadDroppedObject( true, material.shader, material, null );
						}
						else
						{
							AmplifyShaderFunction function = AssetDatabase.LoadAssetAtPath<AmplifyShaderFunction>( Lastpath );
							if( function != null )
							{
								LoadDroppedObject( true, null, null, function );
							}
						}
					}
					else
					{
						LoadDroppedObject( true, shader, null, null );
					}
					Repaint();
				}
			}
		}


		public void ForceMaterialsToUpdate( ref Dictionary<string, string> availableMaterials )
		{
			m_forcingMaterialUpdateOp = true;
			m_forcingMaterialUpdateFlag = true;
			m_materialsToUpdate.Clear();
			foreach( KeyValuePair<string, string> kvp in availableMaterials )
			{
				Material material = AssetDatabase.LoadAssetAtPath<Material>( AssetDatabase.GUIDToAssetPath( kvp.Value ) );
				if( material != null )
				{
					m_materialsToUpdate.Add( material );
				}
			}
		}

		public void SetOutdatedShaderFromTemplate()
		{
			m_outdatedShaderFromTemplateLoaded = true;
		}

		public void DelayedReplaceMasterNode( TemplateMultiPassMasterNode masterNode, string newTemplateGUID )
		{
			ReplaceMasterNode( new MasterNodeCategoriesData( AvailableShaderTypes.Template, newTemplateGUID ), false );
		}

		public void ReplaceMasterNode( MasterNodeCategoriesData data, bool cacheMasterNodes )
		{
			// Capture the current master node's section foldouts so the new master node ( built next
			// frame in CheckNodeReplacement ) keeps them as-is instead of resetting to defaults.
			if ( m_mainGraphInstance.CurrentMasterNode != null )
			{
				List<string> masterViewState = new List<string>();
				m_mainGraphInstance.CurrentMasterNode.WriteUndoViewState( masterViewState );
				m_replaceMasterNodeViewState = masterViewState.ToArray();
			}

			// save connection list before switching
			m_savedList.Clear();
			int count = m_mainGraphInstance.CurrentMasterNode.InputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_mainGraphInstance.CurrentMasterNode.InputPorts[ i ].IsConnected )
				{
					string name = m_mainGraphInstance.CurrentMasterNode.InputPorts[ i ].Name;
					string externalLinkID = m_mainGraphInstance.CurrentMasterNode.InputPorts[ i ].ExternalLinkId;

					( string, string ) ids = ( name, externalLinkID );

					if( !m_savedList.ContainsKey( ids ) )
					{
						OutputPort op = m_mainGraphInstance.CurrentMasterNode.InputPorts[ i ].GetOutputConnection();
						m_savedList.Add( ids, op );
					}
				}
			}

			m_replaceMasterNodeType = data.Category;
			m_replaceMasterNode = true;
			m_replaceMasterNodeData = data.Name;
			m_replaceMasterNodeDataFromCache = cacheMasterNodes;
			if( cacheMasterNodes )
			{
				m_clipboard.AddMultiPassNodesToClipboard( m_mainGraphInstance.MultiPassMasterNodes.NodesList, true, -1 );
				for( int i = 0; i < m_mainGraphInstance.LodMultiPassMasternodes.Count; i++ )
				{
					if( m_mainGraphInstance.LodMultiPassMasternodes[ i ].Count > 0 )
						m_clipboard.AddMultiPassNodesToClipboard( m_mainGraphInstance.LodMultiPassMasternodes[ i ].NodesList, false, i );
				}
			}
		}

		void CheckNodeReplacement()
		{
			if( m_replaceMasterNode )
			{
				m_replaceMasterNode = false;
				switch( m_replaceMasterNodeType )
				{
					default:
					case AvailableShaderTypes.SurfaceShader:
					{
						SetStandardShader();
						if( IOUtils.OnShaderTypeChangedEvent != null )
						{
							IOUtils.OnShaderTypeChangedEvent( m_mainGraphInstance.CurrentShader, false, string.Empty );
						}
					}
					break;
					case AvailableShaderTypes.Template:
					{

						TemplateDataParent templateData = TemplatesManager.Instance.GetTemplate( m_replaceMasterNodeData );
						if( m_replaceMasterNodeDataFromCache )
						{
							// @diogo: while restoring, cascades run before the cached option selections are
							// re-applied; flag it so they can't wipe the surviving nodes' wires or positions
							m_mainGraphInstance.IsReplacingMasterNodes = true;
							try
							{
								m_mainGraphInstance.CrossCheckTemplateNodes( templateData, m_mainGraphInstance.MultiPassMasterNodes.NodesList , -1 );
								for( int i = 0; i < m_mainGraphInstance.LodMultiPassMasternodes.Count; i++ )
								{
									if( m_mainGraphInstance.LodMultiPassMasternodes[ i ].Count > 0 )
										m_mainGraphInstance.CrossCheckTemplateNodes( templateData, m_mainGraphInstance.LodMultiPassMasternodes[ i ].NodesList, i );
								}

								//Getting data from clipboard must be done after cross check all lists
								m_clipboard.GetMultiPassNodesFromClipboard( m_mainGraphInstance.MultiPassMasterNodes.NodesList,-1 );
								for( int i = 0; i < m_mainGraphInstance.LodMultiPassMasternodes.Count; i++ )
								{
									if( m_mainGraphInstance.LodMultiPassMasternodes[ i ].Count > 0 )
										m_clipboard.GetMultiPassNodesFromClipboard( m_mainGraphInstance.LodMultiPassMasternodes[i].NodesList,  i );
								}
								m_clipboard.ResetMultipassNodesData();
							}
							finally
							{
								m_mainGraphInstance.IsReplacingMasterNodes = false;
							}
						}
						else
						{
							SetTemplateShader( m_replaceMasterNodeData, false );
						}

						if( IOUtils.OnShaderTypeChangedEvent != null )
						{
							IOUtils.OnShaderTypeChangedEvent( m_mainGraphInstance.CurrentShader, true, templateData.GUID );
						}
					}
					break;
				}
				// Reapply the captured section foldouts to the freshly built master node ( see capture in
				// ReplaceMasterNode ) so switching template keeps the sections open/closed as they were.
				if ( m_replaceMasterNodeViewState != null )
				{
					if ( m_mainGraphInstance.CurrentMasterNode != null )
					{
						int viewStateIdx = 0;
						m_mainGraphInstance.CurrentMasterNode.ReadUndoViewState( m_replaceMasterNodeViewState, ref viewStateIdx );
					}
					m_replaceMasterNodeViewState = null;
				}
			}
			else if( m_outdatedShaderFromTemplateLoaded )
			{
				m_outdatedShaderFromTemplateLoaded = false;
				TemplateMultiPassMasterNode masterNode = m_mainGraphInstance.CurrentMasterNode as TemplateMultiPassMasterNode;
				if( masterNode != null )
				{
					ReplaceMasterNode( masterNode.CurrentCategoriesData, true );
				}
			}

			// restore possible connections by name
			if( m_savedList.Count > 0 )
			{
				foreach( var item in m_savedList )
				{
					string name = item.Key.Item1;
					string externalLinkID = item.Key.Item2;

					OutputPort op = item.Value;
					InputPort ip = m_mainGraphInstance.CurrentMasterNode.InputPorts.Find( x => x.Name == name );

					if ( ip == null)
					{
						// @diogo: try again, by externalLinkID
						ip = m_mainGraphInstance.CurrentMasterNode.InputPorts.Find( x => x.ExternalLinkId == externalLinkID );
					}

					if( op != null && ip != null && ip.Visible )
					{
						var iNode = UIUtils.GetNode( ip.NodeId );
						var oNode = UIUtils.GetNode( op.NodeId );
						ip.ConnectTo( oNode.UniqueId, op.PortId, op.DataType, false );
						op.ConnectTo( iNode.UniqueId, ip.PortId, ip.DataType, ip.TypeLocked );

						iNode.OnInputPortConnected( ip.PortId, oNode.UniqueId, op.PortId );
						oNode.OnOutputPortConnected( op.PortId, iNode.UniqueId, ip.PortId );
					}
				}
			}
			m_savedList.Clear();
		}

		public Vector2 TranformPosition( Vector2 pos )
		{
			return pos * m_cameraZoom - m_cameraOffset;
		}

		public void UpdateTabTitle()
		{
			if( m_isShaderFunctionWindow )
			{
				AmplifyShaderFunction openedShaderFunction = m_mainGraphInstance.CurrentShaderFunction;
				if( openedShaderFunction != null )
				{
					this.titleContent.text = GenerateTabTitle( openedShaderFunction.FunctionName );
				}
			}
			else
			{
				if( m_selectionMode == ASESelectionMode.Material )
				{
					this.titleContent.text = GenerateTabTitle( m_mainGraphInstance.CurrentMaterial.name );
				}
				else
				{
					this.titleContent.text = GenerateTabTitle( m_mainGraphInstance.CurrentShader.name );
				}
			}
		}

		public ParentGraph CustomGraph
		{
			get { return m_customGraph; }
			set { m_customGraph = value; }
		}

		public ParentGraph CurrentGraph
		{
			get
			{
				if( m_customGraph != null )
					return m_customGraph;

				return m_mainGraphInstance;
			}
		}

		public void RefreshAvaibleNodes()
		{
			if( m_contextMenu != null && m_mainGraphInstance != null )
			{
				m_contextMenu.RefreshNodes( m_mainGraphInstance );
				m_paletteWindow.ForceUpdate = true;
				m_contextPalette.ForceUpdate = true;
				m_refreshAvailableNodes = false;
			}
		}

		public void LateRefreshAvailableNodes()
		{
			m_refreshAvailableNodes = true;
		}

		public ParentGraph OutsideGraph { get { return m_mainGraphInstance; } }

		public bool ShaderIsModified
		{
			get { return m_shaderIsModified; }
			set
			{
				m_shaderIsModified = value && UIUtils.DirtyMask;

				m_toolsWindow.SetStateOnButton( ToolButtonType.Save, m_shaderIsModified ? 1 : 0 );
				if( !IsShaderFunctionWindow )
				{
					MasterNode masterNode = m_mainGraphInstance.CurrentMasterNode;
					if( masterNode != null && masterNode.CurrentShader != null )
					{
						m_toolsWindow.SetStateOnButton( ToolButtonType.Update, ShaderIsModified ? 0 : 2, ShaderIsModified ? ShaderIsModifiedMessage : ShaderIsNotModified );
						UpdateTabTitle( masterNode.ShaderName, m_shaderIsModified );
					}
					else
					{
						m_toolsWindow.SetStateOnButton( ToolButtonType.Update, 1 );
					}

					//if( m_mainGraphInstance.CurrentStandardSurface != null )
					//	UpdateTabTitle( m_mainGraphInstance.CurrentStandardSurface.ShaderName, m_shaderIsModified );
				}
				else
				{
					m_toolsWindow.SetStateOnButton( ToolButtonType.Update, m_shaderIsModified ? 0 : 2 );
					if( m_mainGraphInstance.CurrentShaderFunction != null )
						UpdateTabTitle( m_mainGraphInstance.CurrentShaderFunction.FunctionName, m_shaderIsModified );
				}

			}
		}
		public void MarkToRepaint() { m_repaintIsDirty = true; }
		public void RequestSave() { m_markedToSave = true; }
		public void RequestRepaint() { m_repaintIsDirty = true; }
		public OptionsWindow Options { get { return m_optionsWindow; } }
		public GraphContextMenu ContextMenuInstance { get { return m_contextMenu; } set { m_contextMenu = value; } }
		public ShortcutsManager ShortcutManagerInstance { get { return m_shortcutManager; } }

		public bool GlobalPreview
		{
			get { return m_globalPreview; }
			set { m_globalPreview = value; }
		}

		public bool GlobalShowInternalData
		{
			get { return m_globalShowInternalData; }
			set { m_globalShowInternalData = value; }
		}

		public double EditorTime
		{
			get { return m_time; }
			set { m_time = value; }
		}

		public ASESelectionMode CurrentSelection
		{
			get { return m_selectionMode; }
			set
			{
				m_selectionMode = value;
				switch( m_selectionMode )
				{
					default:
					case ASESelectionMode.Shader:
					{
						m_toolsWindow.BorderStyle = UIUtils.GetCustomStyle( CustomStyle.ShaderBorder );
					}
					break;
					case ASESelectionMode.Material:
					{
						m_toolsWindow.BorderStyle = UIUtils.GetCustomStyle( CustomStyle.MaterialBorder );
					}
					break;
					case ASESelectionMode.ShaderFunction:
					{
						m_toolsWindow.BorderStyle = UIUtils.GetCustomStyle( CustomStyle.ShaderFunctionBorder );
					}
					break;
				}
			}
		}

		public bool LiveShaderEditing
		{
			get { return m_liveShaderEditing; }
			set
			{
				m_liveShaderEditing = value;
				m_innerEditorVariables.LiveMode = m_liveShaderEditing;
				UpdateLiveUI();
			}
		}

		public NodeAvailability CurrentNodeAvailability
		{
			get { return m_currentNodeAvailability; }
			set
			{
				NodeAvailability cache = m_currentNodeAvailability;
				m_currentNodeAvailability = value;

				if( cache != value )
					RefreshAvaibleNodes();
			}
		}
		public string GUID
		{
			get
			{
				if( m_isShaderFunctionWindow )
				{
					AmplifyShaderFunction openedShaderFunction = m_mainGraphInstance.CurrentShaderFunction;
					return openedShaderFunction != null ? AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( openedShaderFunction ) ) : string.Empty;
				}
				else
				{
					return m_mainGraphInstance.CurrentShader != null ? AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_mainGraphInstance.CurrentShader ) ) : string.Empty;
				}
			}
		}
		public List<Toast> Messages { get { return m_messages; } set { m_messages = value; } }
		public float MaxMsgWidth { get { return m_maxMsgWidth; } set { m_maxMsgWidth = value; } }
		public float MsgBoxHeight { get { return m_msgBoxHeight; } set { m_msgBoxHeight = value; } }
		public bool MsgBoxResized { get { return m_msgBoxResized; } set { m_msgBoxResized = value; } }
		public bool MaximizeMessages { get { return m_maximizeMessages; } set { m_maximizeMessages = value; } }
		public void InvalidateAlt() { m_altAvailable = false; }
		public PaletteWindow CurrentPaletteWindow { get { return m_paletteWindow; } }
		public PreMadeShaders PreMadeShadersInstance { get { return m_preMadeShaders; } }
		public Rect CameraInfo { get { return m_cameraInfo; } }
		public Vector2 TranformedMousePos { get { return m_currentMousePos2D * m_cameraZoom - m_cameraOffset; } }
		public Vector2 TranformedKeyEvtMousePos { get { return m_keyEvtMousePos2D * m_cameraZoom - m_cameraOffset; } }
		public PalettePopUp PalettePopUpInstance { get { return m_palettePopup; } }
		public DuplicatePreventionBuffer DuplicatePrevBufferInstance { get { return m_duplicatePreventionBuffer; } }
		public NodeParametersWindow ParametersWindow { get { return m_nodeParametersWindow; } }
		public NodeExporterUtils CurrentNodeExporterUtils { get { return m_nodeExporterUtils; } }
		public AmplifyShaderFunction OpenedShaderFunction { get { return m_mainGraphInstance.CurrentShaderFunction; } }
		public DrawInfo CameraDrawInfo { get { return m_drawInfo; } }
		public string Lastpath { get { return m_lastpath; } set { m_lastpath = value; } }
		public string LastOpenedLocation { get { return m_lastOpenedLocation; } set { m_lastOpenedLocation = value; } }
		public float AvailableCanvasWidth { get { return ( m_cameraInfo.width - m_paletteWindow.RealWidth - m_nodeParametersWindow.RealWidth ); } }
		public float AvailableCanvasHeight { get { return ( m_cameraInfo.height ); } }
		public float CameraZoom { get { return m_cameraZoom; } set { m_cameraZoom = value; m_zoomChanged = true; } }
		public int GraphCount { get { return m_graphCount; } set { m_graphCount = value; } }
		public bool ForceAutoPanDir { get { return m_forceAutoPanDir; } set { m_forceAutoPanDir = value; } }
		public bool LeftMouseButtonPressed { get { return m_lmbPressed; } }
		public bool ControlKeyPressed { get { return m_controlKeyPressed; } }
		public bool OpenedAssetFromNode { get { return m_openedAssetFromNode; } set { m_openedAssetFromNode = value; } }
		public bool IsShaderFunctionWindow
		{
			get { return m_isShaderFunctionWindow; }
			set
			{
				// @diogo: the palette list is filtered by availability, so switching window kind must rebuild it
				if ( m_isShaderFunctionWindow != value )
				{
					m_isShaderFunctionWindow = value;
					LateRefreshAvailableNodes();
				}
			}
		}
		public bool NodesLoadedCorrectly { get { return m_nodesLoadedCorrectly; } set { m_nodesLoadedCorrectly = value; } }
		public double CurrentInactiveTime { get { return m_currentInactiveTime; } }
		public string ReplaceMasterNodeData { get { return m_replaceMasterNodeData; } }
		public AvailableShaderTypes ReplaceMasterNodeType { get { return m_replaceMasterNodeType; } }
		public NodeWireReferencesUtils WireReferenceUtils { get { return m_wireReferenceUtils; } }
		public ContextPalette WindowContextPallete { get { return m_contextPalette; } }
		// This needs to go to UIUtils
		public Texture2D WireTexture { get { return m_wireTexture; } }
		public Event CurrentEvent { get { return m_currentEvent; } }
		public string CurrentCommandName { get { return m_currentCommandName; } }
		public InnerWindowEditorVariables InnerWindowVariables { get { return m_innerEditorVariables; } }
		public Material CurrentMaterial { get { return CurrentGraph.CurrentMaterial; } }
		public Shader CurrentShader { get { return CurrentGraph.CurrentShader; } }
		public Clipboard ClipboardInstance { get { return m_clipboard; } }
	}
}

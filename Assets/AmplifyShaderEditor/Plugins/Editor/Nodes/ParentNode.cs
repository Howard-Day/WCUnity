// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	public enum PreviewLocation
	{
		Auto,
		TopCenter,
		BottomCenter,
		Left,
		Right
	}

	public enum NodeMessageType
	{
		Error,
		Warning,
		Info,
		Legacy
	}

	// The far end of a dashed reference wire: the node the link points at, and the input port the
	// wire lands on. A PortId of -1 anchors it to the node's edge instead.
	public struct ReferenceWire
	{
		public int NodeId;
		public int PortId;

		public ReferenceWire( int nodeId, int portId = -1 )
		{
			NodeId = nodeId;
			PortId = portId;
		}
	}

	[Serializable]
	[NoAutoStaticsCleanup]
	public class ParentNode : UndoParentNode, ISerializationCallbackReceiver
	{

		protected readonly string[] PrecisionLabels = { "Float", "Half" };
		protected readonly string[] PrecisionLabelsExtra = { "Float", "Half", "Inherit" };

		private const double NodeClickTime = 0.2;
		protected GUIContent PrecisionContent = new GUIContent( "Precision", "Changes the precision of internal calculations, using lower types saves some performance\nDefault: Float" );
		private const int MoveCountBuffer = 3;// When testing for stopped movement we need to take Layout and Repaint into account for them not to interfere with tests
		private const float MinInsideBoxWidth = 20;
		private const float MinInsideBoxHeight = 10;

		private const string WikiLinkStr = "online reference";
		private const string ReferencesStr = "References";
		private const string ReferenceHolderLabel = "( {0} ) {1}";
		private const string BackStr = "Back";

		public delegate void OnNodeEvent( ParentNode node, bool testOnlySelected, InteractionMode interactionMode );
		public delegate void OnNodeGenericEvent( ParentNode node );
		public delegate void OnNodeReOrder( ParentNode node, int index );
		public delegate void DrawPropertySection();
		public delegate void OnSRPAction( int outputId, ref MasterNodeDataCollector dataCollector );

		[SerializeField]
		protected PrecisionType m_currentPrecisionType = PrecisionType.Inherit;

		[SerializeField]
		protected bool m_customPrecision = false;

		[SerializeField]
		protected InteractionMode m_defaultInteractionMode = InteractionMode.Other;

		public event OnNodeEvent OnNodeStoppedMovingEvent;
		public OnNodeGenericEvent OnNodeChangeSizeEvent;
		public OnNodeGenericEvent OnNodeDestroyedEvent;
		public event OnNodeReOrder OnNodeReOrderEvent;
		public OnSRPAction OnLightweightAction;
		public OnSRPAction OnHDAction;

		[SerializeField]
		private int m_uniqueId;

		[SerializeField]
		protected Rect m_position;

		[SerializeField]
		protected Rect m_unpreviewedPosition;

		[SerializeField]
		protected GUIContent m_content;

		[SerializeField]
		protected GUIContent m_additionalContent;

		[SerializeField]
		protected bool m_initialized;

		[SerializeField]
		protected NodeConnectionStatus m_connStatus;
		protected bool m_selfPowered = false;

		[SerializeField]
		protected int m_activeConnections;

		[SerializeField]
		protected System.Type m_activeType;

		[SerializeField]
		protected int m_activePort;

		[SerializeField]
		protected int m_activeNode;

		protected NodeRestrictions m_restrictions;

		[SerializeField]
		protected Color m_statusColor;

		[SerializeField]
		protected Rect m_propertyDrawPos;

		// Ports
		[SerializeField]
		protected List<InputPort> m_inputPorts = new List<InputPort>();
		protected Dictionary<int, InputPort> m_inputPortsDict = new Dictionary<int, InputPort>();

		[SerializeField]
		protected bool m_sortInputPorts = false;
		private List<InputPort> m_sortedInputPorts = new List<InputPort>();

		[SerializeField]
		protected List<OutputPort> m_outputPorts = new List<OutputPort>();
		protected Dictionary<int, OutputPort> m_outputPortsDict = new Dictionary<int, OutputPort>();

		[SerializeField]
		protected bool m_sortOutputPorts = false;
		private List<OutputPort> m_sortedOutputPorts = new List<OutputPort>();

		// Positions
		[SerializeField]
		protected Rect m_globalPosition;

		[SerializeField]
		protected Rect m_headerPosition;

		//private Vector2 m_tooltipOffset;

		[SerializeField]
		protected bool m_sizeIsDirty = false;

		[SerializeField]
		protected Vector2 m_extraSize;

		[SerializeField]
		protected Vector2 m_insideSize;

		[SerializeField]
		protected float m_fontHeight;

		// Editor State save on Play Button
		[SerializeField]
		protected bool m_isDirty;

		[SerializeField]
		private int m_isMoving = 0;
		[SerializeField]
		private Rect m_lastPosition;

		// Live Shader Gen
		[SerializeField]
		private bool m_saveIsDirty;

		[SerializeField]
		protected bool m_requireMaterialUpdate = false;

		[SerializeField]
		protected int m_commentaryParent = -1;

		[SerializeField]
		protected int m_depth = -1;

		[SerializeField]
		protected bool m_materialMode = false;

		[SerializeField]
		protected bool m_showPreview = false;

		// @diogo: true when this node's preview re-renders every pass ( continuous, or fed by a live
		// producer ). Only live SF-internal nodes are pooled - their RT is fresh each pass so it is safe to
		// borrow/return; static nodes keep a persistent RT as a cache (pooled SF previews, QA #5).
		protected bool m_previewLive = false;
		public bool PreviewLive { get { return m_previewLive; } }

		// @diogo: frontier flag (pooled SF previews, QA #5). Live consumers mark their producers during the
		// walk; the cached snapshot ( last pass, read-then-reset at this node's visit ) tells a STATIC node
		// whether a live consumer re-reads its RT every pass - if so it must keep a persistent RT ( frontier );
		// if not it is interior and renders through the pool only when dirtied, owning nothing between edits.
		protected bool m_feedsLiveConsumer = false;
		protected bool m_feedsLiveConsumerCached = false;

		// @diogo: re-entrancy guard for EnsurePreviewRendered ( graphs are acyclic, but never trust that ).
		private bool m_ensurePreviewGuard = false;

		// @diogo: pass in which this node last rendered. Time-driven nodes ( ContinuousPreviewRefresh ) never
		// clear PreviewIsDirty, so without this the pull path re-renders them once per consumer edge; a pass
		// runs at a single fixed time, so every repeat produces the same image.
		private int m_previewPassId = 0;

		[SerializeField]
		protected int m_previewMaterialPassId = -1;

		protected bool m_useSquareNodeTitle = false;

		[SerializeField]
		private bool m_continuousPreviewRefresh = false;
		private bool m_previewIsDirty = true;

		protected bool ContinuousPreviewRefresh
		{
			get { return m_continuousPreviewRefresh && !Preferences.User.DisablePreviews; }
			set { m_continuousPreviewRefresh = value; }
		}

		// Error Box Messages
		private Rect m_errorBox;
		private bool m_previousErrorMessage = false;
		protected bool m_showErrorMessage = false;
		protected NodeMessageType m_errorMessageTypeIsError = NodeMessageType.Error;
		protected string m_errorMessageTooltip = string.Empty;
		protected string m_warningPasses = string.Empty;

		private GUIContent m_errorIcon = new GUIContent();
		private GUIContent m_errorMessage = new GUIContent();
		private GUIStyle m_errorCurrentStyle;

		private const string ErrorTitle = "ERROR";
		private const string WarningTitle = "WARNING";
		private const string InfoTitle = "INFO";
		private const string LegacyTitle = "LEGACY";

		// Drawing Node
		protected PreviewLocation m_selectedLocation = PreviewLocation.Auto;
		private int m_extraHeaderHeight = 0;
		protected bool m_isVisible;
		protected bool m_selected = false;
		protected bool m_rmbIgnore;
		protected GUIContent m_sizeContentAux;

		protected uint m_currentReadParamIdx = 1;
		// @diogo: raised when a read runs out of params or swallows a failure; the clipboard version probe
		// rejects a candidate that leaves it set, since those reads produce no exception of their own
		protected bool m_silentReadFailure = false;
		protected bool m_reorderLocked = false;

		protected Rect m_cachedPos;
		protected Vector2 m_accumDelta = Vector2.zero;

		private bool m_isOnGrid = false;
		protected bool m_useInternalPortData = false;
		protected bool m_autoDrawInternalPortData = true;
		// @diogo: reference-style nodes override this to source inline port value labels from their
		// reference's matching port, or hide them entirely by returning null
		protected virtual InputPort GetPortInternalDataSource( int portIdx ) { return m_inputPorts[ portIdx ]; }
		protected DrawOrder m_drawOrder = DrawOrder.Default;

		protected bool m_movingInFrame = false;
		protected float m_anchorAdjust = -1;

		protected Color m_headerColor;

		[SerializeField] // needs to be serialized because of Undo
		protected Color m_headerColorModifier = Color.white;

		protected bool m_infiniteLoopDetected = false;
		protected int m_textLabelWidth = -1;

		private bool m_linkVisibility = false;
		[SerializeField]
		protected bool m_hasTooltipLink = true;

		protected int m_category = 0;

		protected double m_lastTimeSelected;
		private double m_tooltipTimestamp;
		private int m_tooltipPortId = -1;
		protected string m_tooltipText;

		protected Rect m_unscaledRemainingBox;
		protected Rect m_remainingBox;

		private int m_visibleInputs = 0;
		private int m_visibleOutputs = 0;

		private double m_doubleClickTimestamp;
		private const double DoubleClickTime = 0.25;

		protected bool m_canExpand = true;

		protected bool m_firstDraw = true;
		protected bool m_firstDrawTouch = false;

		protected int m_matrixId = -1;

		private float m_paddingTitleLeft = 0;
		private float m_paddingTitleRight = 0;

		// Preview Fields
		private Material m_previewMaterial = null;
		private Shader m_previewShader = null;
		protected string m_previewShaderGUID = string.Empty;
		protected float m_marginPreviewLeft = 0;
		protected bool m_globalShowPreview = false;
		protected Rect m_unscaledPreviewRect;
		protected Rect m_previewRect;
		protected bool m_drawPreviewMaskButtons = true;
		private int m_channelNumber = 0;
		protected bool m_firstPreviewDraw = true;
		[SerializeField]
		protected bool m_drawPreview = true;
		protected bool m_drawPreviewExpander = true;
		private bool m_spherePreview = false;
		protected bool m_drawPreviewAsSphere = false;
		protected bool m_forceDrawPreviewAsPlane = false;
		private bool m_finishPreviewRender = false;

		private int m_cachedMainTexId = -1;
		private int m_cachedMaskTexId = -1;
		private int m_cachedPortsId = -1;
		private int m_cachedPortId = -1;

		private int m_cachedDrawSphereId = -1;
		private int m_cachedInvertedZoomId = -1;
		//private int m_cachedIsLinearId = -1;

		private bool[] m_previewChannels = { true, true, true, false };

		// @diogo: set by DrawPreview, cleared on every layout pass, so the color picker only offers a readout
		// where a preview was actually drawn this repaint ( picker slots and collapsed nodes keep a stale rect )
		private bool m_previewWasDrawn = false;

		private static readonly string[] s_previewChannelNames = { "R", "G", "B", "A" };
		private static readonly GUIContent s_previewPickerContent = new GUIContent();
		private static Texture2D s_previewPickerTexel = null;

		// @diogo: color picker readback rate, decoupled from the repaint rate: the box follows the cursor every
		// frame while the values refresh at this interval, keeping the GPU sync off the cursor tracking path.
		private const double PreviewPickerReadInterval = 1.0 / 30.0;

		// @diogo: node the Ctrl picker is currently over. Nodes the preview pass does not otherwise root
		// ( the Color swatch ) opt in through this, so they only render while actually being read.
		private static int s_previewPickerRequestId = -1;

		private static int s_previewPickerNodeId = -1;
		private static double s_previewPickerReadTime = 0;
		private static Color s_previewPickerValue = Color.clear;

		// Others
		protected bool m_hasSubtitle = false;
		protected bool m_showSubtitle = true;
		protected bool m_hasLeftDropdown = false;
		protected bool m_autoWrapProperties = false;
		protected bool m_internalDataFoldout = true;
		protected bool m_propertiesFoldout = true;
		private bool m_referencesFoldout = false;
		private List<ParentNode> m_referenceHolders = null;
		protected bool m_repopulateDictionaries = true;

		protected Vector2 m_lastInputBottomRight = Vector2.zero;
		protected Vector2 m_lastOutputBottomLeft = Vector2.zero;

		private Vector4 m_portMask = Vector4.zero;

		private Vector2 m_auxVector2 = Vector4.zero;
		protected Rect m_auxRect;

		protected PreviewLocation m_autoLocation;
		protected Rect m_titlePos;
		protected Rect m_addTitlePos;
		protected Rect m_expandRect;
		protected Rect m_dropdownRect;
		protected Rect m_currInputPortPos;
		protected Rect m_currOutputPortPos;
		protected Color m_colorBuffer;

		[SerializeField]
		protected bool m_docking = false;

		[SerializeField]
		protected int m_visiblePorts = 0;

		protected int m_graphDepth = 0;

		protected int m_oldInputCount = -1;

		protected bool m_dropdownEditing = false;

		protected bool m_isNodeBeingCopied = false;

		protected string m_previousTitle = string.Empty;

		protected string m_previousAdditonalTitle = string.Empty;
		protected string m_previousAdditonalTitleFormat = string.Empty;

		private bool m_alive = true;

		// @diogo: tombstone set by Destroy(); with deferred destruction the shell stays != null, so signal entry points must check it
		private bool m_destroyed = false;

		private bool m_wasDeprecated = false;

		private double m_timedUpdateInitialValue;
		private double m_timedUpdateInterval;
		private bool m_fireTimedUpdateRequest = false;

		public ParentNode()
		{
			m_position = new Rect( 0, 0, 0, 0 );
			m_content = new GUIContent( GUIContent.none );
			m_additionalContent = new GUIContent( GUIContent.none );
			CommonInit( -1 );
		}

		public ParentNode( int uniqueId, float x, float y, float width, float height )
		{
			m_position = new Rect( x, y, width, height );
			m_content = new GUIContent( GUIContent.none );
			m_additionalContent = new GUIContent( GUIContent.none );
			CommonInit( uniqueId );
		}

		public virtual void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;

			RefreshUIHeaderColor();

			m_tooltipTimestamp = Time.realtimeSinceStartup;
			hideFlags = HideFlags.DontSave;
		}

		public void RefreshUIHeaderColor()
		{
			// @diogo: handle special shader function case; this will have to do for now...
			var functionNode = this as FunctionNode;
			if ( functionNode != null && functionNode.Function != null )
			{
				if ( functionNode.Function.Header == AmplifyShaderFunction.HeaderStyle.Custom )
				{
					m_headerColor = functionNode.Function.HeaderColor;
				}
				else if ( functionNode.Function.Header == AmplifyShaderFunction.HeaderStyle.Category )
				{
					m_headerColor = UIUtils.GetColorFromCategory( functionNode.Function.CustomNodeCategory );
				}
				else
				{
					m_headerColor = UIUtils.GetColorFromCategory( "Functions" );
				}
			}
			else if ( m_nodeAttribs != null )
			{
				if( UIUtils.HasColorCategory( m_nodeAttribs.Category ) )
				{
					m_headerColor = UIUtils.GetColorFromCategory( m_nodeAttribs.Category );
				}
				else
				{
					if( !string.IsNullOrEmpty( m_nodeAttribs.CustomCategoryColor ) )
					{
						m_headerColor = UIUtils.AddColorCategory( m_nodeAttribs.Category, m_nodeAttribs.CustomCategoryColor );
					}
				}
			}
		}

		// @diogo: reflected once per type; every node instance used to query its attributes on construction
		private static Dictionary<Type, NodeAttributes> s_nodeAttributesByType = new Dictionary<Type, NodeAttributes>();

		private static NodeAttributes GetNodeAttributes( Type type )
		{
			if ( !s_nodeAttributesByType.TryGetValue( type, out NodeAttributes attributes ) )
			{
				attributes = type.GetCustomAttributes( typeof( NodeAttributes ), true ).FirstOrDefault() as NodeAttributes;
				s_nodeAttributesByType.Add( type, attributes );
			}
			return attributes;
		}

		protected virtual void CommonInit( int uniqueId )
		{
			m_uniqueId = uniqueId;

			m_isOnGrid = false;
			ConnStatus = NodeConnectionStatus.Not_Connected;
			m_inputPorts = new List<InputPort>();
			m_inputPortsDict = new Dictionary<int, InputPort>();

			m_outputPorts = new List<OutputPort>();
			m_outputPortsDict = new Dictionary<int, OutputPort>();

			m_nodeAttribs = GetNodeAttributes( GetType() );
			if( m_nodeAttribs != null )
			{
				m_content.text = m_nodeAttribs.Name;
				//m_content.tooltip = m_nodeAttribs.Description;
				m_tooltipText = m_nodeAttribs.Description;
				m_selected = false;

				// @diogo: a deprecated node with no automatic conversion lives on in the graph, so it flags
				//         itself on the canvas instead of raising a one-off console warning on load
				if ( m_nodeAttribs.Deprecated )
				{
					ShowTab( NodeMessageType.Legacy, DeprecationMessage );
				}
			}

			m_sizeContentAux = new GUIContent();
			m_extraSize = new Vector2( 0, 0 );
			m_insideSize = new Vector2( 0, 0 );
			m_sizeIsDirty = true;
			m_initialized = true;
			m_restrictions = new NodeRestrictions();

			m_propertyDrawPos = new Rect();
		}

		public virtual void AfterCommonInit()
		{
			if( PreviewShader && !HasPreviewShader )
			{
				m_drawPreview = false;
				m_drawPreviewExpander = false;
				m_canExpand = false;
			}

			if( m_drawPreviewExpander || m_hasLeftDropdown )
			{
				m_paddingTitleRight += Constants.PreviewExpanderWidth + Constants.IconsLeftRightMargin;
				m_paddingTitleLeft = Constants.PreviewExpanderWidth + Constants.IconsLeftRightMargin;
			}
		}

		public virtual void Destroy()
		{
			m_alive = false;
			m_destroyed = true;
			if( OnNodeDestroyedEvent != null )
			{
				OnNodeDestroyedEvent( this );
				OnNodeDestroyedEvent = null;
			}

			OnLightweightAction = null;
			OnHDAction = null;

			OnNodeStoppedMovingEvent = null;
			OnNodeChangeSizeEvent = null;
			OnNodeReOrderEvent = null;
			if( m_restrictions != null )
				m_restrictions.Destroy();
			m_restrictions = null;

			if( m_inputPorts != null )
			{
				int inputCount = m_inputPorts.Count;
				for( int i = 0; i < inputCount; i++ )
				{
					m_inputPorts[ i ].Destroy();
				}
				m_inputPorts.Clear();
				m_inputPorts = null;
			}

			if( m_outputPorts != null )
			{
				int outputCount = m_outputPorts.Count;
				for( int i = 0; i < outputCount; i++ )
				{
					m_outputPorts[ i ].Destroy();
				}
				m_outputPorts.Clear();
				m_outputPorts = null;
			}

			if( m_inputPortsDict != null )
				m_inputPortsDict.Clear();

			m_inputPortsDict = null;

			if( m_outputPortsDict != null )
				m_outputPortsDict.Clear();

			m_outputPortsDict = null;

			if( m_previewMaterial != null )
				DestroyImmediate( m_previewMaterial );
			m_previewMaterial = null;

			m_previewShader = null;
			//m_containerGraph = null;
		}

		public virtual void Move( Vector2 delta )
		{
			if( m_docking )
				return;

			Move( delta, false );
		}

		public virtual void Move( Vector2 delta, bool snap )
		{
			if( m_docking )
				return;

			if( m_isMoving == 0 )
			{
				m_cachedPos = m_position;
				m_accumDelta = Vector2.zero;
			}

			m_isMoving = MoveCountBuffer;
			m_accumDelta += delta;

			if( snap )
			{
				m_position.x = Mathf.Round( ( m_cachedPos.x + m_accumDelta.x ) / Constants.GRID_SNAP_X ) * Constants.GRID_SNAP_X;
				m_position.y = Mathf.Round( ( m_cachedPos.y + m_accumDelta.y ) / Constants.GRID_SNAP_Y ) * Constants.GRID_SNAP_Y;
			}
			else
			{
				m_position.x += delta.x;
				m_position.y += delta.y;
			}
			//if(Event.current.type == EventType.Layout)
			m_movingInFrame = true;
		}

		public virtual void UpdateMaterial( Material mat )
		{
			m_requireMaterialUpdate = false;
		}

		public virtual void SetMaterialMode( Material mat, bool fetchMaterialValues )
		{
			m_materialMode = ( mat != null );
		}

		public virtual bool UpdateShaderDefaults( ref Shader shader, ref TextureDefaultsDataColector defaultCol ) { return false; }
		public virtual void ForceUpdateFromMaterial( Material material ) { }
		public void SetSaveIsDirty()
		{
			if( m_connStatus == NodeConnectionStatus.Connected )
			{
				SaveIsDirty = true;
			}
		}

		// @diogo: async popup callbacks (SearchableDropdown) fire outside the node's change-check scope,
		// so they must set the dirty flags that SafeDraw/SafeDrawProperties would otherwise infer
		public void SetDirtyFromAsyncEdit()
		{
			PreviewIsDirty = true;
			SetSaveIsDirty();
		}

		public void ActivateNodeReordering( int index )
		{
			if( OnNodeReOrderEvent != null )
				OnNodeReOrderEvent( this, index );
		}

		void RecalculateInputPortIdx()
		{
			m_inputPortsDict.Clear();
			int count = m_inputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_inputPorts[ i ].IsConnected )
				{
					int nodeId = m_inputPorts[ i ].ExternalReferences[ 0 ].NodeId;
					int portId = m_inputPorts[ i ].ExternalReferences[ 0 ].PortId;
					ParentNode node = UIUtils.GetNode( nodeId );
					// @diogo: the reference goes stale when the peer port is trimmed off an extensible node
					OutputPort outputPort = ( node != null ) ? node.GetOutputPortByUniqueId( portId ) : null;
					if( outputPort != null )
					{
						int outputCount = outputPort.ExternalReferences.Count;
						for( int j = 0; j < outputCount; j++ )
						{
							if( outputPort.ExternalReferences[ j ].NodeId == m_uniqueId &&
								outputPort.ExternalReferences[ j ].PortId == m_inputPorts[ i ].PortId )
							{
								outputPort.ExternalReferences[ j ].PortId = i;
							}
						}
					}
				}
				m_inputPorts[ i ].PortId = i;
				m_inputPortsDict.Add( i, m_inputPorts[ i ] );
			}
		}

		public void SwapInputPorts( int fromIdx, int toIdx )
		{
			InputPort port = m_inputPorts[ fromIdx ];
			//if( toIdx > fromIdx )
			//	toIdx--;
			m_inputPorts.Remove( port );
			m_inputPorts.Insert( toIdx, port );
			RecalculateInputPortIdx();
			SetSaveIsDirty();
		}

		public void RemoveInputPort( int idx )
		{
			if( idx < m_inputPorts.Count )
			{
				m_inputPortsDict.Remove( m_inputPorts[ idx ].PortId );
				m_inputPorts.RemoveAt( idx );
				SetSaveIsDirty();
				m_sizeIsDirty = true;
			}
		}

		public void RemoveOutputPort( string name )
		{
			int count = m_outputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_outputPorts[ i ].Name.Equals( name ) )
				{
					if( m_outputPorts[ i ].IsConnected )
					{
						m_containerGraph.DeleteConnection( false, m_uniqueId, m_outputPorts[ i ].PortId, false, true );
						m_outputPortsDict.Remove( m_outputPorts[ i ].PortId );
						m_outputPorts.RemoveAt( i );
						SetSaveIsDirty();
						m_sizeIsDirty = true;
					}
				}
			}
		}

		public void RemoveOutputPort( int idx, bool isArrayIndex = true )
		{
			if( isArrayIndex )
			{
				// idx represents a position on the output port array
				if( idx < m_outputPorts.Count )
				{
					if( m_outputPorts[ idx ].IsConnected )
					{
						m_containerGraph.DeleteConnection( false, m_uniqueId, m_outputPorts[ idx ].PortId, false, true );
					}

					m_outputPortsDict.Remove( m_outputPorts[ idx ].PortId );
					m_outputPorts.RemoveAt( idx );
					SetSaveIsDirty();
					m_sizeIsDirty = true;
				}
			}
			else
			{
				// idx represents a port unique id
				int count = m_outputPorts.Count;
				int arrIdx = -1;
				for( int i = 0; i < count; i++ )
				{
					if( m_outputPorts[ i ].PortId == idx )
					{
						arrIdx = i;
						break;
					}
				}

				if( arrIdx >= 0 )
				{
					if( m_outputPorts[ arrIdx ].IsConnected )
					{
						m_containerGraph.DeleteConnection( false, m_uniqueId, idx, false, true );
					}

					m_outputPortsDict.Remove( idx );
					m_outputPorts.RemoveAt( arrIdx );
					SetSaveIsDirty();
					m_sizeIsDirty = true;
				}
			}
		}

		// Manually add Ports
		public InputPort AddInputPort( WirePortDataType type, bool typeLocked, string name, int orderId = -1, MasterNodePortCategory category = MasterNodePortCategory.Fragment, int uniquePortId = -1 )
		{
			InputPort port = new InputPort( m_uniqueId, ( uniquePortId < 0 ? m_inputPorts.Count : uniquePortId ), type, name, typeLocked, ( orderId >= 0 ? orderId : m_inputPorts.Count ), category );
			m_inputPorts.Add( port );
			m_inputPortsDict.Add( port.PortId, port );
			SetSaveIsDirty();
			m_sizeIsDirty = true;
			return port;
		}

		public InputPort AddInputPort( WirePortDataType type, bool typeLocked, string name, string dataName, int orderId = -1, MasterNodePortCategory category = MasterNodePortCategory.Fragment, int uniquePortId = -1 )
		{
			InputPort port = new InputPort( m_uniqueId, ( uniquePortId < 0 ? m_inputPorts.Count : uniquePortId ), type, name, dataName, typeLocked, ( orderId >= 0 ? orderId : m_inputPorts.Count ), category );
			m_inputPorts.Add( port );
			m_inputPortsDict.Add( port.PortId, port );
			SetSaveIsDirty();
			m_sizeIsDirty = true;
			return port;
		}

		public InputPort AddInputPortAt( int idx, WirePortDataType type, bool typeLocked, string name, int orderId = -1, MasterNodePortCategory category = MasterNodePortCategory.Fragment, int uniquePortId = -1 )
		{
			InputPort port = new InputPort( m_uniqueId, ( uniquePortId < 0 ? m_inputPorts.Count : uniquePortId ), type, name, typeLocked, ( orderId >= 0 ? orderId : m_inputPorts.Count ), category );
			m_inputPorts.Insert( idx, port );
			m_inputPortsDict.Add( port.PortId, port );
			SetSaveIsDirty();
			m_sizeIsDirty = true;
			RecalculateInputPortIdx();
			return port;
		}

		public void AddOutputPort( WirePortDataType type, string name, int uniquePortId = -1 )
		{
			m_outputPorts.Add( new OutputPort( this, m_uniqueId, ( uniquePortId < 0 ? m_outputPorts.Count : uniquePortId ), type, name ) );
			m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
			SetSaveIsDirty();
			m_sizeIsDirty = true;
		}

		public void AddOutputPortAt( int idx, WirePortDataType type, string name, int uniquePortId = -1 )
		{
			OutputPort port = new OutputPort( this, m_uniqueId, ( uniquePortId < 0 ? m_outputPorts.Count : uniquePortId ), type, name );
			m_outputPorts.Insert( idx, port );
			m_outputPortsDict.Add( port.PortId, port );
			SetSaveIsDirty();
			m_sizeIsDirty = true;
		}

		public void AddOutputVectorPorts( WirePortDataType type, string name )
		{
			m_sizeIsDirty = true;
			m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, type, name ) );
			m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );

			switch( type )
			{
				case WirePortDataType.FLOAT2:
				{
					m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "X" ) );
					m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
					m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "Y" ) );
					m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
				}
				break;
				case WirePortDataType.FLOAT3:
				{
					m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "X" ) );
					m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
					m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "Y" ) );
					m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
					m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "Z" ) );
					m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
				}
				break;
				case WirePortDataType.FLOAT4:
				{
					m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "X" ) );
					m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
					m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "Y" ) );
					m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
					m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "Z" ) );
					m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
					m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "W" ) );
					m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
				}
				break;
			}
			SetSaveIsDirty();
		}

		public void SetPreviewDirtyFromOutputs()
		{
			PreviewIsDirty = true;
		}

		public string GetOutputVectorItem( int vectorPortId, int currentPortId, string result )
		{
			if( m_outputPorts[ 0 ].DataType == WirePortDataType.COLOR )
			{
				switch( currentPortId - vectorPortId )
				{
					case 1: result += ".r"; break;
					case 2: result += ".g"; break;
					case 3: result += ".b"; break;
					case 4: result += ".a"; break;
					case 5: result += ".rgb"; break;
				}
			}
			else
			{
				switch( currentPortId - vectorPortId )
				{
					case 1: result += ".x"; break;
					case 2: result += ".y"; break;
					case 3: result += ".z"; break;
					case 4: result += ".w"; break;
					case 5: result += ".xyz"; break;
				}
			}
			return result;
		}

		public void AddOutputColorPorts( string name = "RGBA", bool addAlpha = true, bool addRGB = false )
		{
			m_sizeIsDirty = true;

			// RGBA port
			int startOrderId = ( m_outputPorts.Count > 0 ) ? m_outputPorts[ m_outputPorts.Count - 1 ].OrderId : 0;

			m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, addAlpha ? WirePortDataType.COLOR : WirePortDataType.FLOAT3, name ) );
			m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
			m_outputPorts[ m_outputPorts.Count - 1 ].OrderId = startOrderId;

			// Red port
			m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "R" ) );
			m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
			m_outputPorts[ m_outputPorts.Count - 1 ].CustomColor = Color.red;
			m_outputPorts[ m_outputPorts.Count - 1 ].OrderId = startOrderId + 2;

			// Green port
			m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "G" ) );
			m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
			m_outputPorts[ m_outputPorts.Count - 1 ].CustomColor = Color.green;
			m_outputPorts[ m_outputPorts.Count - 1 ].OrderId = startOrderId + 3;

			// Blue port
			m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "B" ) );
			m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
			m_outputPorts[ m_outputPorts.Count - 1 ].CustomColor = Color.blue;
			m_outputPorts[ m_outputPorts.Count - 1 ].OrderId = startOrderId + 4;

			if ( addAlpha )
			{
				// Alpha port
				m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT, "A" ) );
				m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
				m_outputPorts[ m_outputPorts.Count - 1 ].CustomColor = Color.white;
				m_outputPorts[ m_outputPorts.Count - 1 ].OrderId = startOrderId + 5;
			}

			if ( addRGB && addAlpha )
			{
				// RGB port (placed below RGBA)
				m_outputPorts.Add( new OutputPort( this, m_uniqueId, m_outputPorts.Count, WirePortDataType.FLOAT3, "RGB" ) );
				m_outputPortsDict.Add( m_outputPorts[ m_outputPorts.Count - 1 ].PortId, m_outputPorts[ m_outputPorts.Count - 1 ] );
				m_outputPorts[ m_outputPorts.Count - 1 ].OrderId = startOrderId + 1;
			}
		}

		public void ConvertFromVectorToColorPorts()
		{
			m_outputPorts[ 0 ].ChangeType( WirePortDataType.COLOR, false );

			m_outputPorts[ 1 ].Name = "R";
			m_outputPorts[ 1 ].CustomColor = Color.red;

			m_outputPorts[ 2 ].Name = "G";
			m_outputPorts[ 2 ].CustomColor = Color.green;

			m_outputPorts[ 3 ].Name = "B";
			m_outputPorts[ 3 ].CustomColor = Color.blue;

			m_outputPorts[ 4 ].Name = "A";
			m_outputPorts[ 4 ].CustomColor = Color.white;
		}


		public string GetOutputColorItem( int vectorPortId, int currentPortId, string result )
		{
			switch( currentPortId - vectorPortId )
			{
				case 1: result += ".r"; break;
				case 2: result += ".g"; break;
				case 3: result += ".b"; break;
				case 4: result += ".a"; break;
				case 5: result += ".rgb"; break;
			}
			return result;
		}

		public void ChangeOutputType( WirePortDataType type, bool invalidateConnections )
		{
			int outputCount = m_outputPorts.Count;
			for( int i = 0; i < outputCount; i++ )
			{
				m_outputPorts[ i ].ChangeType( type, invalidateConnections );
			}
		}

		public void ChangeInputType( WirePortDataType type, bool invalidateConnections )
		{
			int inputCount = m_inputPorts.Count;
			for( int i = 0; i < inputCount; i++ )
			{
				m_inputPorts[ i ].ChangeType( type, invalidateConnections );
			}
		}

		// @diogo: as above, carrying the name a struct type needs alongside it
		public void ChangeOutputType( WirePortDataType type, string structName, bool invalidateConnections )
		{
			int outputCount = m_outputPorts.Count;
			for( int i = 0; i < outputCount; i++ )
			{
				m_outputPorts[ i ].ChangeType( type, structName, invalidateConnections );
			}
		}

		// @diogo: as above, carrying the name a struct type needs alongside it
		public void ChangeInputType( WirePortDataType type, string structName, bool invalidateConnections )
		{
			int inputCount = m_inputPorts.Count;
			for( int i = 0; i < inputCount; i++ )
			{
				m_inputPorts[ i ].ChangeType( type, structName, invalidateConnections );
			}
		}

		public void ChangeOutputProperties( int outputID, string newName, WirePortDataType newType, bool invalidateConnections = true )
		{
			if( outputID < m_outputPorts.Count )
			{
				m_outputPorts[ outputID ].ChangeProperties( newName, newType, invalidateConnections );
				IsDirty = true;
				m_sizeIsDirty = true;
				SetSaveIsDirty();
			}
		}

		public void ChangeOutputName( int outputArrayIdx, string newName )
		{
			if( outputArrayIdx < m_outputPorts.Count )
			{
				m_outputPorts[ outputArrayIdx ].Name = newName;
				IsDirty = true;
				m_sizeIsDirty = true;
			}
		}

		public InputPort CheckInputPortAt( Vector3 pos )
		{
			int count = m_inputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_inputPorts[ i ].InsideActiveArea( pos ) )
					return m_inputPorts[ i ];
			}
			return null;
		}

		public InputPort GetFirstInputPortOfType( WirePortDataType dataType, bool countObjectTypeAsValid )
		{
			int count = m_inputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( ( m_inputPorts[ i ].CheckValidType( dataType ) ) || ( countObjectTypeAsValid && m_inputPorts[ i ].DataType == WirePortDataType.OBJECT ) )
					return m_inputPorts[ i ];
			}
			return null;
		}

		public OutputPort CheckOutputPortAt( Vector3 pos )
		{
			int count = m_outputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_outputPorts[ i ].InsideActiveArea( pos ) )
					return m_outputPorts[ i ];
			}
			return null;
		}

		public OutputPort GetFirstOutputPortOfType( WirePortDataType dataType, bool checkForCasts )
		{
			int count = m_outputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( ( m_outputPorts[ i ].CheckValidType( dataType ) ) || ( checkForCasts && UIUtils.CanCast( dataType, m_outputPorts[ i ].DataType ) ) )
					return m_outputPorts[ i ];
			}
			return null;
		}

		virtual protected void ChangeSizeFinished() { m_firstPreviewDraw = true; /*MarkForPreviewUpdate();*/ }
		protected void ChangeSize()
		{
			// @diogo: keep the snap-drag anchor while a move gesture is active; re-anchoring mid-drag makes
			// every subsequent drag event re-apply the gesture's whole accumulated delta
			if ( m_isMoving == 0 )
			{
				m_cachedPos = m_position;
			}
			//UIUtils.ResetMainSkin();

			Vector2 inSize = Vector2.zero;
			int inputCount = 0;
			int inputSize = m_inputPorts.Count;
			for( int i = 0; i < inputSize; i++ )
			{
				if( m_inputPorts[ i ].Visible )
				{
					if( m_inputPorts[ i ].DirtyLabelSize || m_inputPorts[ i ].LabelSize == Vector2.zero )
					{
						m_inputPorts[ i ].DirtyLabelSize = false;
						m_sizeContentAux.text = m_inputPorts[ i ].Name;
						m_inputPorts[ i ].UnscaledLabelSize = UIUtils.UnZoomedInputPortStyle.CalcSize( m_sizeContentAux );
					}

					inSize.x = Mathf.Max( inSize.x, m_inputPorts[ i ].UnscaledLabelSize.x );
					inSize.y = Mathf.Max( inSize.y, m_inputPorts[ i ].UnscaledLabelSize.y );
					inputCount += 1;
				}
			}
			if( inSize.x > 0 )
				inSize.x += UIUtils.PortsSize.x + Constants.PORT_TO_LABEL_SPACE_X * 2;
			inSize.x += m_marginPreviewLeft;
			inSize.y = Mathf.Max( inSize.y, UIUtils.PortsSize.y );


			Vector2 outSize = Vector2.zero;
			int outputCount = 0;
			int outputSize = m_outputPorts.Count;
			for( int i = 0; i < outputSize; i++ )
			{
				if( m_outputPorts[ i ].Visible )
				{
					if( m_outputPorts[ i ].DirtyLabelSize || m_outputPorts[ i ].LabelSize == Vector2.zero )
					{
						m_outputPorts[ i ].DirtyLabelSize = false;
						m_sizeContentAux.text = m_outputPorts[ i ].Name;
						m_outputPorts[ i ].UnscaledLabelSize = UIUtils.UnZoomedOutputPortPortStyle.CalcSize( m_sizeContentAux );
					}

					outSize.x = Mathf.Max( outSize.x, m_outputPorts[ i ].UnscaledLabelSize.x );
					outSize.y = Mathf.Max( outSize.y, m_outputPorts[ i ].UnscaledLabelSize.y );
					outputCount += 1;
				}
			}
			if( outSize.x > 0 )
				outSize.x += UIUtils.PortsSize.x + Constants.PORT_TO_LABEL_SPACE_X * 2;
			outSize.y = Mathf.Max( outSize.y, UIUtils.PortsSize.y );

			if( m_additionalContent.text.Length > 0 )
			{
				m_extraHeaderHeight = (int)Constants.NODE_HEADER_EXTRA_HEIGHT;
				m_hasSubtitle = true && m_showSubtitle;
			}
			else
			{
				m_extraHeaderHeight = 0;
				m_hasSubtitle = false;
			}

			float headerWidth = Mathf.Max( UIUtils.UnZoomedNodeTitleStyle.CalcSize( m_content ).x + m_paddingTitleLeft + m_paddingTitleRight, UIUtils.UnZoomedPropertyValuesTitleStyle.CalcSize( m_additionalContent ).x + m_paddingTitleLeft + m_paddingTitleRight );
			m_position.width = Mathf.Max(headerWidth, Mathf.Max(MinInsideBoxWidth, m_insideSize.x) + inSize.x + outSize.x);/* + Constants.NODE_HEADER_LEFTRIGHT_MARGIN * 2;*/
			m_position.width = Mathf.Round(m_position.width / 32) * 32 + 32;
            //m_position.width += m_extraSize.x;

            m_fontHeight = Mathf.Max( inSize.y, outSize.y );

			m_position.height = Mathf.Max( inputCount, outputCount ) * Constants.PORT_ROW_HEIGHT_Y;// + Constants.INPUT_PORT_DELTA_Y;
			m_position.height = Mathf.Max( m_position.height, Mathf.Max( MinInsideBoxHeight, m_insideSize.y ) );
			m_position.height += UIUtils.HeaderMaxHeight + /*m_extraHeaderHeight +*/ Constants.INPUT_PORT_DELTA_Y;// + m_extraSize.y;
			if( m_showErrorMessage )
				m_position.height += 24;

			m_unpreviewedPosition = m_position;
			//UIUtils.CurrentWindow.CameraDrawInfo.InvertedZoom = cachedZoom;
			if( OnNodeChangeSizeEvent != null )
			{
				OnNodeChangeSizeEvent( this );
			}
			ChangeSizeFinished();
		}

		public virtual void Reset() { }
		public virtual void OnOutputPortConnected( int portId, int otherNodeId, int otherPortId ) { }

		public virtual void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			InputPort port = GetInputPortByUniqueId( portId );
			if( activateNode && m_connStatus == NodeConnectionStatus.Connected )
			{
				port.GetOutputNode().ActivateNode( m_activeNode, m_activePort, m_activeType );
			}

			PreviewIsDirty = true;
			OnNodeChange( new NodeUpdateCache() );
			SetSaveIsDirty();
		}

		public virtual void OnInputPortDisconnected( int portId ) { PreviewIsDirty = true; OnNodeChange( new NodeUpdateCache() ); }
		public virtual void OnOutputPortDisconnected( int portId ) { }

		public virtual void OnNodeChange( NodeUpdateCache cache )
		{
			bool touched = cache.Touch( this );
			bool wasSphere = SpherePreview;

			//Debug.Log( "Visited " + this.UniqueId + ", Cache: " + ( cache != null ) );

			CheckSpherePreview();

			// @diogo: a re-visit still has to propagate when the flag actually flipped - a consumer laid out
			// before its producer computes false first, and only the producer's later walk can correct it
			if ( touched && SpherePreview == wasSphere )
			{
				return;
			}

			int count = m_outputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_outputPorts[ i ].IsConnected )
				{
					for( int f = 0; f < m_outputPorts[ i ].ExternalReferences.Count; f++ )
					{
						ContainerGraph.GetNode( m_outputPorts[ i ].ExternalReferences[ f ].NodeId ).OnNodeChange( cache );
					}
				}
			}
		}

		public virtual void ActivateNode( int signalGenNodeId, int signalGenPortId, System.Type signalGenNodeType )
		{
			// @diogo: destroyed shells must ignore signals; destroy cascades and undo patching reach them via live neighbors
			if ( m_destroyed )
			{
				return;
			}

			if( m_selfPowered )
				return;

			ConnStatus = m_restrictions.GetRestiction( signalGenNodeType, signalGenPortId ) ? NodeConnectionStatus.Error : NodeConnectionStatus.Connected;
			m_activeConnections += 1;

			if( m_activeConnections == 1 )
			{
				m_activeType = signalGenNodeType;
				m_activeNode = signalGenNodeId;
				m_activePort = signalGenPortId;
				for( int i = 0; i < m_inputPorts.Count; i++ )
				{
					if( m_inputPorts[ i ].IsConnected )
					{
						m_inputPorts[ i ].GetOutputNode().ActivateNode( signalGenNodeId, signalGenPortId, signalGenNodeType );
					}
				}
			}
			// saveisdirty might be needed, gonna leave this here for now
			// SetSaveIsDirty();
		}

		public virtual void DeactivateInputPortNode( int deactivatedPort, bool forceComplete )
		{
			GetInputPortByUniqueId( deactivatedPort ).GetOutputNode().DeactivateNode( deactivatedPort, false );
		}

		public virtual void DeactivateNode( int deactivatedPort, bool forceComplete )
		{
			// @diogo: destroyed shells must ignore signals; destroy cascades and undo patching reach them via live neighbors
			if ( m_destroyed )
			{
				return;
			}

			if( m_selfPowered )
				return;

			// saveisdirty might be needed, gonna leave this here for now
			// SetSaveIsDirty();
			m_activeConnections -= 1;
			if( forceComplete || m_activeConnections <= 0 )
			{
				m_activeConnections = 0;
				ConnStatus = NodeConnectionStatus.Not_Connected;
				for( int i = 0; i < m_inputPorts.Count; i++ )
				{
					if( m_inputPorts[ i ].IsConnected )
					{
						ParentNode node = m_inputPorts[ i ].GetOutputNode();
						if( node != null )
							node.DeactivateNode( deactivatedPort == -1 ? m_inputPorts[ i ].PortId : deactivatedPort, false );
					}
				}
			}
		}

		// @diogo: incremental undo patching resets activation to the fresh-load precondition before re-propagating from the master node
		public void ResetActivationState()
		{
			m_activeConnections = 0;
			ConnStatus = NodeConnectionStatus.Not_Connected;
		}

		public Rect GlobalToLocalPosition( DrawInfo drawInfo )
		{
			float width = m_globalPosition.width / drawInfo.InvertedZoom;
			float height = m_globalPosition.height / drawInfo.InvertedZoom;

			float x = m_globalPosition.x / drawInfo.InvertedZoom - drawInfo.CameraOffset.x;
			float y = m_globalPosition.y / drawInfo.InvertedZoom - drawInfo.CameraOffset.y;
			return new Rect( x, y, width, height );
		}

		protected void CalculatePositionAndVisibility( DrawInfo drawInfo )
		{
			//m_movingInFrame = false;
			m_globalPosition = m_position;
			m_globalPosition.x = drawInfo.InvertedZoom * ( m_globalPosition.x + drawInfo.CameraOffset.x );
			m_globalPosition.y = drawInfo.InvertedZoom * ( m_globalPosition.y + drawInfo.CameraOffset.y );
			m_globalPosition.width *= drawInfo.InvertedZoom;
			m_globalPosition.height *= drawInfo.InvertedZoom;

			m_isVisible = ( m_globalPosition.x + m_globalPosition.width > 0 ) &&
							( m_globalPosition.x < drawInfo.CameraArea.width ) &&
							( m_globalPosition.y + m_globalPosition.height > 0 ) &&
							( m_globalPosition.y < drawInfo.CameraArea.height );

			// @diogo: a held button means the drag continues; a slow snapped drag leaves the position unchanged
			// across events, and expiring here mid-drag re-anchors Move and drops its sub-grid accumulation
			if( m_isMoving > 0 && drawInfo.CurrentEventType != EventType.MouseDrag && !m_containerGraph.ParentWindow.LeftMouseButtonPressed )
			{
				float deltaX = Mathf.Abs( m_lastPosition.x - m_position.x );
				float deltaY = Mathf.Abs( m_lastPosition.y - m_position.y );
				if( deltaX < 0.01f && deltaY < 0.01f )
				{
					m_isMoving -= 1;
					if( m_isMoving == 0 )
					{
						OnSelfStoppedMovingEvent();
					}
				}
				else
				{
					m_isMoving = MoveCountBuffer;
				}
				m_lastPosition = m_position;
			}
		}

		public void FireStoppedMovingEvent( bool testOnlySelected, InteractionMode interactionMode )
		{
			if( OnNodeStoppedMovingEvent != null )
				OnNodeStoppedMovingEvent( this, testOnlySelected, interactionMode );
		}

		public virtual void OnSelfStoppedMovingEvent()
		{
			FireStoppedMovingEvent( true, m_defaultInteractionMode );
		}

		protected void DrawPrecisionProperty( bool withInherit = true )
		{
			if( withInherit )
				m_currentPrecisionType = (PrecisionType)EditorGUILayoutPopup( PrecisionContent.text, (int)m_currentPrecisionType, PrecisionLabelsExtra );
			else
				m_currentPrecisionType = (PrecisionType)EditorGUILayoutPopup( PrecisionContent.text, (int)m_currentPrecisionType, PrecisionLabels );
		}

		public virtual void DrawTitle( Rect titlePos )
		{
			if( ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				GUI.Label( titlePos, m_content, UIUtils.GetCustomStyle( CustomStyle.NodeTitle ) );
			}
		}

		public virtual void DrawPreview( DrawInfo drawInfo, Rect rect )
		{
			//if ( !m_drawPreview )
			//	return;

			// @diogo: previews live in array slices and draw through the array material; the disabled image stays
			// a plain texture drawn with the 2D one. Nothing to draw before the first render, and allocating here
			// would place the slice before the node's inputs are known ( see ASEPreviewSlices.Allocate ).
			bool previewsDisabled = Preferences.User.DisablePreviews;
			if ( !previewsDisabled && ( m_outputPorts.Count == 0 || !m_outputPorts[ 0 ].HasPreviewTexture ) )
			{
				return;
			}
			ASEPreviewSlice slice = previewsDisabled ? default( ASEPreviewSlice ) : PreviewSlice;
			if ( !previewsDisabled && !slice.IsValid )
			{
				return;
			}

			m_previewWasDrawn = true;

			if( m_cachedDrawSphereId == -1 )
				m_cachedDrawSphereId = Shader.PropertyToID( "_DrawSphere" );

			if( m_cachedInvertedZoomId == -1 )
				m_cachedInvertedZoomId = Shader.PropertyToID( "_InvertedZoom" );

			m_channelNumber = 0;
			Vector4 mask = Vector4.one;
			if( m_outputPorts.Count > 0 )
			{
				switch( m_outputPorts[ 0 ].DataType )
				{
					case WirePortDataType.FLOAT:
					m_channelNumber = 1;
					mask.Set( 1, 1, 1, 0 );
					break;
					case WirePortDataType.FLOAT2:
					m_channelNumber = 2;
					mask.Set( m_previewChannels[ 0 ] ? 1 : 0, m_previewChannels[ 1 ] ? 1 : 0, 1, 0 );
					break;
					case WirePortDataType.COLOR:
					case WirePortDataType.FLOAT4:
					case WirePortDataType.SAMPLER1D:
					case WirePortDataType.SAMPLER2D:
					case WirePortDataType.SAMPLER3D:
					case WirePortDataType.SAMPLERCUBE:
					case WirePortDataType.SAMPLER2DARRAY:
					m_channelNumber = 4;
					mask.Set( m_previewChannels[ 0 ] ? 1 : 0, m_previewChannels[ 1 ] ? 1 : 0, m_previewChannels[ 2 ] ? 1 : 0, m_previewChannels[ 3 ] ? 1 : 0 );
					break;
					default:
					m_channelNumber = 3;
					mask.Set( m_previewChannels[ 0 ] ? 1 : 0, m_previewChannels[ 1 ] ? 1 : 0, m_previewChannels[ 2 ] ? 1 : 0, 0 );
					break;
				}
			}

			Material drawMaterial = previewsDisabled ? UIUtils.LinearMaterial : UIUtils.LinearArrayMaterial;
			drawMaterial.SetFloat( m_cachedDrawSphereId, ( SpherePreview ? 1 : 0 ) );
			drawMaterial.SetFloat( m_cachedInvertedZoomId, drawInfo.InvertedZoom );
			drawMaterial.SetVector( "_Mask", mask );

			bool cached = GL.sRGBWrite;
			GL.sRGBWrite = true;
			//EditorGUI.DrawPreviewTexture( rect, PreviewTexture, UIUtils.LinearMaterial );
			int pass = 0;
			if( SpherePreview )
			{
				if( mask.w == 1 )
					pass = 3;
				else
					pass = 1;
			}
			else if( mask.w == 1 )
				pass = 2;

			if( previewsDisabled )
			{
				Graphics.DrawTexture( rect, UIUtils.PreviewDisabledRT, drawMaterial, pass );
			}
			else
			{
				drawMaterial.SetFloat( s_previewSliceId, slice.Slice );
				Graphics.DrawTexture( rect, slice.Array, drawMaterial, pass );
			}
			GL.sRGBWrite = cached;
			//Preview buttons
			if( m_drawPreviewMaskButtons )
				DrawPreviewMaskButtonsRepaint( drawInfo, rect );
		}

		protected void DrawPreviewMaskButtonsLayout( DrawInfo drawInfo, Rect rect )
		{
			if( rect.Contains( drawInfo.MousePosition ) && m_channelNumber > 1 && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD2 )
			{
				Rect buttonRect = rect;
				buttonRect.height = 14 * drawInfo.InvertedZoom;
				buttonRect.y = rect.yMax - buttonRect.height;
				buttonRect.width = 14 * drawInfo.InvertedZoom;

				if( m_channelNumber == 2 )
				{
					m_previewChannels[ 0 ] = GUI.Toggle( buttonRect, m_previewChannels[ 0 ], string.Empty, GUIStyle.none );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					m_previewChannels[ 1 ] = GUI.Toggle( buttonRect, m_previewChannels[ 1 ], string.Empty, GUIStyle.none );
				}
				else if( m_channelNumber == 3 )
				{
					m_previewChannels[ 0 ] = GUI.Toggle( buttonRect, m_previewChannels[ 0 ], string.Empty, GUIStyle.none );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					m_previewChannels[ 1 ] = GUI.Toggle( buttonRect, m_previewChannels[ 1 ], string.Empty, GUIStyle.none );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					m_previewChannels[ 2 ] = GUI.Toggle( buttonRect, m_previewChannels[ 2 ], string.Empty, GUIStyle.none );
				}
				else if( m_channelNumber == 4 )
				{
					m_previewChannels[ 0 ] = GUI.Toggle( buttonRect, m_previewChannels[ 0 ], string.Empty, GUIStyle.none );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					m_previewChannels[ 1 ] = GUI.Toggle( buttonRect, m_previewChannels[ 1 ], string.Empty, GUIStyle.none );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					m_previewChannels[ 2 ] = GUI.Toggle( buttonRect, m_previewChannels[ 2 ], string.Empty, GUIStyle.none );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					m_previewChannels[ 3 ] = GUI.Toggle( buttonRect, m_previewChannels[ 3 ], string.Empty, GUIStyle.none );
				}
			}
		}

		protected void DrawPreviewMaskButtonsRepaint( DrawInfo drawInfo, Rect rect )
		{
			if( drawInfo.CurrentEventType == EventType.Repaint && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD2 && rect.Contains( drawInfo.MousePosition ) && m_channelNumber > 1 )
			{
				Rect buttonRect = rect;
				buttonRect.height = 14 * drawInfo.InvertedZoom;
				buttonRect.y = rect.yMax - buttonRect.height;
				buttonRect.width = 14 * drawInfo.InvertedZoom;

				if( m_channelNumber == 2 )
				{
					UIUtils.MiniButtonTopMid.Draw( buttonRect, "R", false, false, m_previewChannels[ 0 ], false );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					UIUtils.MiniButtonTopRight.Draw( buttonRect, "G", false, false, m_previewChannels[ 1 ], false );
				}
				else if( m_channelNumber == 3 )
				{
					UIUtils.MiniButtonTopMid.Draw( buttonRect, "R", false, false, m_previewChannels[ 0 ], false );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					UIUtils.MiniButtonTopMid.Draw( buttonRect, "G", false, false, m_previewChannels[ 1 ], false );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					UIUtils.MiniButtonTopRight.Draw( buttonRect, "B", false, false, m_previewChannels[ 2 ], false );
				}
				else if( m_channelNumber == 4 )
				{
					UIUtils.MiniButtonTopMid.Draw( buttonRect, "R", false, false, m_previewChannels[ 0 ], false );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					UIUtils.MiniButtonTopMid.Draw( buttonRect, "G", false, false, m_previewChannels[ 1 ], false );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					UIUtils.MiniButtonTopMid.Draw( buttonRect, "B", false, false, m_previewChannels[ 2 ], false );
					buttonRect.x += 14 * drawInfo.InvertedZoom;
					UIUtils.MiniButtonTopRight.Draw( buttonRect, "A", false, false, m_previewChannels[ 3 ], false );
				}
			}
		}

		protected bool PreviewPickerRequested { get { return s_previewPickerRequestId == UniqueId; } }

		// @diogo: nodes that stand a hand-drawn surface in for their preview ( the Color swatch ) register it
		// here so the Ctrl picker can still read the preview RT over it. They carry no channel mask UI, so
		// every channel the output has is listed.
		protected void MarkPreviewDrawn( Rect rect, int channelCount )
		{
			m_previewRect = rect;
			m_previewWasDrawn = true;
			m_channelNumber = channelCount;
			for ( int i = 0; i < m_previewChannels.Length; i++ )
			{
				m_previewChannels[ i ] = i < channelCount;
			}
		}

		// @diogo: Ctrl-hovering a node preview reads back the texel under the cursor and lists the channels the
		// preview mask has enabled. The readback is throttled, the box itself redraws with the cursor.
		public bool ShowPreviewColorPicker( DrawInfo drawInfo )
		{
			if ( !m_previewWasDrawn || m_channelNumber == 0 || ContainerGraph.LodLevel > ParentGraph.NodeLOD.LOD2 )
			{
				return false;
			}

			if ( !m_previewRect.Contains( drawInfo.MousePosition ) )
			{
				return false;
			}

			s_previewPickerRequestId = UniqueId;

			// @diogo: a hand-drawn surface only opts its node into the preview pass while the picker asks for it,
			// so for a tick or two nothing has rendered into the RT. The PreviewTexture getter allocates on read
			// and would hand back undefined contents, hence the non-allocating peek. Nodes that draw a real
			// preview are rooted by the pass already, and the aliasing overrides all sit on that side.
			if ( !m_drawPreview && ( m_outputPorts.Count == 0 || !m_outputPorts[ 0 ].HasPreviewTexture ) )
			{
				return false;
			}

			RenderTexture previewTexture = PreviewTexture2D( ASEPreviewSlices.PickerScratchIndex );
			if ( previewTexture == null )
			{
				return false;
			}

			float u = ( drawInfo.MousePosition.x - m_previewRect.xMin ) / m_previewRect.width;

			// @diogo: no Y flip here - the readback rows already come out in the preview's on-screen order
			float v = ( drawInfo.MousePosition.y - m_previewRect.yMin ) / m_previewRect.height;

			// @diogo: the sphere preview keeps a plane mapping and just masks out everything past its radius
			if ( SpherePreview && new Vector2( 2 * u - 1, 2 * v - 1 ).sqrMagnitude > 1 )
			{
				return false;
			}

			Color texel = SamplePreviewTexel( previewTexture, u, v );

			// @diogo: fixed notation so every channel lines up on the same number of decimals
			string format = "F" + Mathf.Clamp( Preferences.User.PreviewPickerDecimals, 0, 9 );

			string readout = string.Empty;
			for ( int i = 0; i < m_channelNumber; i++ )
			{
				if ( m_channelNumber > 1 && !m_previewChannels[ i ] )
				{
					continue;
				}

				if ( readout.Length > 0 )
				{
					readout += "\n";
				}
				readout += s_previewChannelNames[ i ] + ": " + FormatPreviewChannel( texel[ i ], format );
			}

			if ( readout.Length == 0 )
			{
				return false;
			}

			s_previewPickerContent.text = readout;
			Vector2 size = UIUtils.PreviewPickerBox.CalcSize( s_previewPickerContent );

			Rect pickerBox = new Rect( drawInfo.MousePosition.x + 16, drawInfo.MousePosition.y + 16, size.x, size.y );
			if ( pickerBox.xMax > Screen.width - 10 )
			{
				pickerBox.x = drawInfo.MousePosition.x - 16 - pickerBox.width;
			}

			if ( pickerBox.yMax > Screen.height - 10 )
			{
				pickerBox.y = drawInfo.MousePosition.y - 16 - pickerBox.height;
			}

			GUI.Label( pickerBox, s_previewPickerContent, UIUtils.PreviewPickerBox );
			return true;
		}

		// @diogo: reuses the last readback until the interval lapses; a different node always reads right away so
		// moving between previews never shows the neighbor's values.
		private Color SamplePreviewTexel( RenderTexture texture, float u, float v )
		{
			double now = EditorApplication.timeSinceStartup;
			if ( s_previewPickerNodeId != UniqueId || ( now - s_previewPickerReadTime ) >= PreviewPickerReadInterval )
			{
				s_previewPickerNodeId = UniqueId;
				s_previewPickerReadTime = now;
				s_previewPickerValue = ReadPreviewTexel( texture, u, v );
			}
			return s_previewPickerValue;
		}

		private static Color ReadPreviewTexel( RenderTexture texture, float u, float v )
		{
			if ( s_previewPickerTexel == null )
			{
				s_previewPickerTexel = new Texture2D( 1, 1, TextureFormat.RGBAFloat, false, true );
				s_previewPickerTexel.hideFlags = HideFlags.HideAndDontSave;
			}

			int x = Mathf.Clamp( (int)( u * texture.width ), 0, texture.width - 1 );
			int y = Mathf.Clamp( (int)( v * texture.height ), 0, texture.height - 1 );

			RenderTexture cached = RenderTexture.active;
			RenderTexture.active = texture;
			s_previewPickerTexel.ReadPixels( new Rect( x, y, 1, 1 ), 0, 0, false );
			RenderTexture.active = cached;

			return s_previewPickerTexel.GetPixel( 0, 0 );
		}

		private static string FormatPreviewChannel( float value, string format )
		{
			string formatted;
			if ( float.IsNaN( value ) )
			{
				formatted = "NaN";
			}
			else if ( float.IsInfinity( value ) )
			{
				formatted = value > 0 ? "Inf" : "-Inf";
			}
			else
			{
				formatted = value.ToString( format );
			}
			return formatted;
		}

		public void SetTimedUpdate( double timerInterval )
		{
			m_timedUpdateInitialValue = EditorApplication.timeSinceStartup;
			m_timedUpdateInterval = timerInterval;
			m_fireTimedUpdateRequest = true;
		}

		public virtual void FireTimedUpdate() { }
		/// <summary>
		///
		/// </summary>
		/// <param name="drawInfo"></param>
		public virtual void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			if( m_fireTimedUpdateRequest && ( EditorApplication.timeSinceStartup - m_timedUpdateInitialValue ) > m_timedUpdateInterval )
			{
				m_fireTimedUpdateRequest = false;
				FireTimedUpdate();
			}

			if( m_repopulateDictionaries )
			{
				m_repopulateDictionaries = false;

				m_inputPortsDict.Clear();
				int inputCount = m_inputPorts.Count;
				for( int i = 0; i < inputCount; i++ )
				{
					m_inputPortsDict.Add( m_inputPorts[ i ].PortId, m_inputPorts[ i ] );
				}

				m_outputPortsDict.Clear();
				int outputCount = m_outputPorts.Count;
				for( int i = 0; i < outputCount; i++ )
				{
					m_outputPortsDict.Add( m_outputPorts[ i ].PortId, m_outputPorts[ i ] );
				}
			}
		}

		private void UpdateSortedInputPorts()
		{
			if ( m_sortInputPorts )
			{
				m_sortedInputPorts.Clear();
				m_sortedInputPorts.AddRange( m_inputPorts );
				m_sortedInputPorts.Sort( ( x, y ) => x.OrderId.CompareTo( y.OrderId ) );
			}
			else
			{
				m_sortedInputPorts = m_inputPorts;
			}
		}

		private void UpdateSortedOutputPorts()
		{
			if ( m_sortOutputPorts )
			{
				m_sortedOutputPorts.Clear();
				m_sortedOutputPorts.AddRange( m_outputPorts );
				m_sortedOutputPorts.Sort( ( x, y ) => x.OrderId.CompareTo( y.OrderId ) );
			}
			else
			{
				m_sortedOutputPorts = m_outputPorts;
			}

		}

		/// <summary>
		/// This method should only be called to calculate layouts of elements to be draw later, only runs once per frame and before wires are drawn
		/// </summary>
		/// <param name="drawInfo"></param>
		public virtual void OnNodeLayout( DrawInfo drawInfo, NodeUpdateCache cache = null )
		{
			m_previewWasDrawn = false;

			if( ContainerGraph.ChangedLightingModel )
			{
				m_sizeIsDirty = true;
				m_firstPreviewDraw = true;
			}

			if( m_firstDraw )
			{
				m_firstDraw = false;
				AfterCommonInit();
				OnNodeChange( cache );
			}

			if( m_previousErrorMessage != m_showErrorMessage )
			{
				m_sizeIsDirty = true;
			}

			if( m_sizeIsDirty )
			{
				m_sizeIsDirty = false;
				ChangeSize();
				UpdateSortedInputPorts();
				UpdateSortedOutputPorts();
			}

			CalculatePositionAndVisibility( drawInfo );

			m_unscaledRemainingBox = m_position;
			m_remainingBox = m_globalPosition;

			m_lastInputBottomRight = m_position.position;
			m_lastOutputBottomLeft = m_position.position;
			m_lastOutputBottomLeft.x += m_position.width;

			m_visibleInputs = 0;
			m_visibleOutputs = 0;

			if( m_hasSubtitle )
				m_extraHeaderHeight = (int)Constants.NODE_HEADER_EXTRA_HEIGHT;
			else
				m_extraHeaderHeight = 0;

			m_lastInputBottomRight.y += UIUtils.HeaderMaxHeight + m_extraHeaderHeight;
			m_lastOutputBottomLeft.y += UIUtils.HeaderMaxHeight + m_extraHeaderHeight;
			m_unscaledRemainingBox.y += UIUtils.HeaderMaxHeight + m_extraHeaderHeight;

			if( m_isVisible )
			{
				// Header
				m_headerPosition = m_globalPosition;
				m_headerPosition.height = UIUtils.CurrentHeaderHeight + m_extraHeaderHeight * drawInfo.InvertedZoom;

				// Title
				m_titlePos = m_globalPosition;
				m_titlePos.height = m_headerPosition.height;
				if( m_hasSubtitle )
					m_titlePos.yMin += ( 2 * drawInfo.InvertedZoom );
				else
					m_titlePos.yMin += ( 8 * drawInfo.InvertedZoom );
				m_titlePos.width -= ( m_paddingTitleLeft + m_paddingTitleRight ) * drawInfo.InvertedZoom;
				m_titlePos.x += m_paddingTitleLeft * drawInfo.InvertedZoom;

				// Additional Title
				if( m_hasSubtitle )
				{
					m_addTitlePos = m_titlePos;
					m_addTitlePos.y = m_globalPosition.y;
					m_addTitlePos.yMin += ( 15 * drawInfo.InvertedZoom );
				}

				// Left Dropdown
				if( m_hasLeftDropdown && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD4 )
				{
					m_dropdownRect = m_headerPosition;
					m_dropdownRect.width = Constants.NodeButtonSizeX * drawInfo.InvertedZoom;
					m_dropdownRect.x = m_globalPosition.x + ( Constants.IconsLeftRightMargin + 1 ) * drawInfo.InvertedZoom;
					m_dropdownRect.height = Constants.NodeButtonSizeY * drawInfo.InvertedZoom;
					m_dropdownRect.y = m_globalPosition.y + m_headerPosition.height * 0.5f - 14 * drawInfo.InvertedZoom * 0.5f;
				}

				// Expander
				if( m_drawPreviewExpander && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD4 )
				{
					m_expandRect = m_globalPosition;
					m_expandRect.width = Constants.PreviewExpanderWidth * drawInfo.InvertedZoom;
					m_expandRect.x = m_globalPosition.x + m_globalPosition.width - ( Constants.IconsLeftRightMargin + Constants.PreviewExpanderWidth ) * drawInfo.InvertedZoom; //titlePos.x + titlePos.width;
					m_expandRect.height = Constants.PreviewExpanderHeight * drawInfo.InvertedZoom;
					m_expandRect.y = m_globalPosition.y + m_headerPosition.height * 0.5f - Constants.PreviewExpanderHeight * drawInfo.InvertedZoom * 0.5f;
				}
			}

			if( m_anchorAdjust < 0 )
			{
				m_anchorAdjust = UIUtils.GetCustomStyle( CustomStyle.PortEmptyIcon ).normal.background.width;
			}

			m_unscaledRemainingBox.y += Constants.INPUT_PORT_DELTA_Y;
			m_lastOutputBottomLeft.y += Constants.INPUT_PORT_DELTA_Y;
			m_lastInputBottomRight.y += Constants.INPUT_PORT_DELTA_Y;

			// Input Ports
			{
				m_currInputPortPos = m_globalPosition;
				m_currInputPortPos.width = drawInfo.InvertedZoom * UIUtils.PortsSize.x;
				m_currInputPortPos.height = drawInfo.InvertedZoom * UIUtils.PortsSize.y;

				m_currInputPortPos.x += drawInfo.InvertedZoom * Constants.PORT_INITIAL_X;
				m_currInputPortPos.y += drawInfo.InvertedZoom * Constants.PORT_INITIAL_Y + m_extraHeaderHeight * drawInfo.InvertedZoom;
				int inputCount = m_sortedInputPorts.Count;

				float initialX = m_lastInputBottomRight.x;

				for ( int i = 0; i < inputCount; i++ )
				{
					if( m_sortedInputPorts[ i ].Visible )
					{
						m_visibleInputs++;
						// Button
						m_sortedInputPorts[ i ].Position = m_currInputPortPos;

						// Label
						m_sortedInputPorts[ i ].LabelPosition = m_currInputPortPos;
						float deltaX = 1f * drawInfo.InvertedZoom * ( UIUtils.PortsSize.x + Constants.PORT_TO_LABEL_SPACE_X );
						m_auxRect = m_sortedInputPorts[ i ].LabelPosition;
						m_auxRect.x += deltaX;
						m_sortedInputPorts[ i ].LabelPosition = m_auxRect;

						//if( m_sortedInputPorts[ i ].DirtyLabelSize || m_sortedInputPorts[ i ].LabelSize == Vector2.zero )
						//{
						//	m_sortedInputPorts[ i ].DirtyLabelSize = false;
						//	m_sizeContentAux.text = m_sortedInputPorts[ i ].Name;
						//	m_sortedInputPorts[ i ].UnscaledLabelSize = UIUtils.UnZoomedInputPortStyle.CalcSize( m_sizeContentAux );
						//}

						m_sortedInputPorts[ i ].LabelSize = m_sortedInputPorts[ i ].UnscaledLabelSize * drawInfo.InvertedZoom;

						m_lastInputBottomRight.x = Mathf.Max( m_lastInputBottomRight.x, initialX + m_sortedInputPorts[ i ].UnscaledLabelSize.x + Constants.PORT_INITIAL_X + Constants.PORT_TO_LABEL_SPACE_X + UIUtils.PortsSize.x );

						if( !m_sortedInputPorts[ i ].Locked )
						{
							float overflow = 2;
							float scaledOverflow = 4 * drawInfo.InvertedZoom;
							m_auxRect = m_currInputPortPos;
							m_auxRect.yMin -= scaledOverflow + overflow;
							m_auxRect.yMax += scaledOverflow + overflow;
							m_auxRect.xMin -= Constants.PORT_INITIAL_X * drawInfo.InvertedZoom + scaledOverflow + overflow;
							if( m_containerGraph.ParentWindow.WireReferenceUtils.OutputPortReference.IsValid )
								m_auxRect.xMax += m_sortedInputPorts[ i ].LabelSize.x + Constants.PORT_TO_LABEL_SPACE_X * drawInfo.InvertedZoom + scaledOverflow + overflow;
							else
								m_auxRect.xMax += Constants.PORT_TO_LABEL_SPACE_X * drawInfo.InvertedZoom + scaledOverflow + overflow;
							m_sortedInputPorts[ i ].ActivePortArea = m_auxRect;
						}
						m_currInputPortPos.y += drawInfo.InvertedZoom * Constants.PORT_ROW_HEIGHT_Y;
						//GUI.Label( m_sortedInputPorts[ i ].ActivePortArea, string.Empty, UIUtils.Box );
					}
				}
				if( m_visibleInputs > 0 )
					m_lastInputBottomRight.y += Constants.PORT_ROW_HEIGHT_Y * ( m_visibleInputs - 1 ) + m_fontHeight;
			}

			// Output Ports
			{
				m_currOutputPortPos = m_globalPosition;
				m_currOutputPortPos.width = drawInfo.InvertedZoom * UIUtils.PortsSize.x;
				m_currOutputPortPos.height = drawInfo.InvertedZoom * UIUtils.PortsSize.y;

				m_currOutputPortPos.x += ( m_globalPosition.width - drawInfo.InvertedZoom * ( Constants.PORT_INITIAL_X + m_anchorAdjust ) );
				m_currOutputPortPos.y += drawInfo.InvertedZoom * Constants.PORT_INITIAL_Y + m_extraHeaderHeight * drawInfo.InvertedZoom;

				float initialX = m_lastOutputBottomLeft.x;

				int outputCount = m_sortedOutputPorts.Count;

				for ( int i = 0; i < outputCount; i++ )
				{
					if( m_sortedOutputPorts[ i ].Visible )
					{
						m_visibleOutputs++;
						//Button
						m_sortedOutputPorts[ i ].Position = m_currOutputPortPos;

						// Label
						m_sortedOutputPorts[ i ].LabelPosition = m_currOutputPortPos;
						float deltaX = 1f * drawInfo.InvertedZoom * ( UIUtils.PortsSize.x + Constants.PORT_TO_LABEL_SPACE_X );
						m_auxRect = m_sortedOutputPorts[ i ].LabelPosition;
						m_auxRect.x -= deltaX;
						m_sortedOutputPorts[ i ].LabelPosition = m_auxRect;

						m_sortedOutputPorts[ i ].LabelSize = m_sortedOutputPorts[ i ].UnscaledLabelSize * drawInfo.InvertedZoom;

						m_lastOutputBottomLeft.x = Mathf.Min( m_lastOutputBottomLeft.x, initialX - m_sortedOutputPorts[ i ].UnscaledLabelSize.x - Constants.PORT_INITIAL_X - Constants.PORT_TO_LABEL_SPACE_X - UIUtils.PortsSize.x );

						if( !m_sortedOutputPorts[ i ].Locked )
						{
							float overflow = 2;
							float scaledOverflow = 4 * drawInfo.InvertedZoom;
							m_auxRect = m_currOutputPortPos;
							m_auxRect.yMin -= scaledOverflow + overflow;
							m_auxRect.yMax += scaledOverflow + overflow;
							if( m_containerGraph.ParentWindow.WireReferenceUtils.InputPortReference.IsValid )
								m_auxRect.xMin -= m_sortedOutputPorts[ i ].LabelSize.x + Constants.PORT_TO_LABEL_SPACE_X * drawInfo.InvertedZoom + scaledOverflow + overflow;
							else
								m_auxRect.xMin -= Constants.PORT_TO_LABEL_SPACE_X * drawInfo.InvertedZoom + scaledOverflow + overflow;
							m_auxRect.xMax += Constants.PORT_INITIAL_X * drawInfo.InvertedZoom + scaledOverflow + overflow;
							m_sortedOutputPorts[ i ].ActivePortArea = m_auxRect;
						}
						m_currOutputPortPos.y += drawInfo.InvertedZoom * Constants.PORT_ROW_HEIGHT_Y;
						//GUI.Label( m_sortedOutputPorts[ i ].ActivePortArea, string.Empty, UIUtils.Box );
					}
				}
				if( m_visibleOutputs > 0 )
					m_lastOutputBottomLeft.y += Constants.PORT_ROW_HEIGHT_Y * ( m_visibleOutputs - 1 ) + m_fontHeight;
			}

			m_lastInputBottomRight.x += m_marginPreviewLeft;

			//Vector2 scaledLastOutputBottomLeft = ( m_lastOutputBottomLeft + drawInfo.CameraOffset ) * drawInfo.InvertedZoom;
			//GUI.Label( new Rect( scaledLastOutputBottomLeft, Vector2.one * 2 ), string.Empty, UIUtils.CurrentWindow.CustomStylesInstance.Box );

			m_unscaledRemainingBox.xMin = m_lastInputBottomRight.x;
			//m_unscaledRemainingBox.yMin = m_lastInputBottomRight.y;
			m_unscaledRemainingBox.xMax = m_lastOutputBottomLeft.x;
			m_unscaledRemainingBox.yMax = Mathf.Max( m_lastOutputBottomLeft.y, m_lastInputBottomRight.y );

			m_remainingBox.position = ( m_unscaledRemainingBox.position + drawInfo.CameraOffset ) * drawInfo.InvertedZoom;
			m_remainingBox.size = m_unscaledRemainingBox.size * drawInfo.InvertedZoom;

			//GUI.Label( m_remainingBox, string.Empty, UIUtils.Box );

			if( m_visibleInputs == 0 )
			{
				m_remainingBox.x += Constants.PORT_INITIAL_X * drawInfo.InvertedZoom;
				m_remainingBox.width -= Constants.PORT_INITIAL_X * drawInfo.InvertedZoom;
			}

			if( m_visibleOutputs == 0 )
			{
				m_remainingBox.width -= Constants.PORT_INITIAL_X * drawInfo.InvertedZoom;
			}

			if( ContainerGraph.ParentWindow.GlobalPreview != m_globalShowPreview )
			{
				m_globalShowPreview = ContainerGraph.ParentWindow.GlobalPreview;
				m_sizeIsDirty = true;
				ContainerGraph.ParentWindow.RequestRepaint();
			}

			// Generate Proper Preview Rect
			float marginAround = 10;
			float scaledMarginAround = marginAround * drawInfo.InvertedZoom;
			float previewSize = 128;
			PreviewLocation m_autoLocation = m_selectedLocation;
			if( m_selectedLocation == PreviewLocation.Auto )
			{
				if( m_visibleOutputs > m_visibleInputs )
				{
					m_autoLocation = PreviewLocation.Left;
				}
				else if( m_visibleOutputs < m_visibleInputs )
				{
					m_autoLocation = PreviewLocation.Right;
				}
				else if( m_unscaledRemainingBox.width > previewSize )
				{
					m_autoLocation = PreviewLocation.TopCenter;
				}
				else
				{
					m_autoLocation = PreviewLocation.BottomCenter;
				}
			}

			if( m_canExpand && ( m_showPreview || m_globalShowPreview ) )
			{
				if( m_autoLocation == PreviewLocation.TopCenter )
				{
					m_unscaledPreviewRect.y = m_unscaledRemainingBox.y;
					m_unscaledPreviewRect.x = m_unscaledRemainingBox.center.x - 0.5f * ( previewSize + 2 * marginAround );
				}
				else if( m_autoLocation == PreviewLocation.BottomCenter )
				{
					m_unscaledPreviewRect.y = Mathf.Max( m_lastOutputBottomLeft.y, m_lastInputBottomRight.y );
					m_unscaledPreviewRect.x = m_position.x + 0.5f * m_position.width - 0.5f * ( previewSize + 2 * marginAround );
				}
				else if( m_autoLocation == PreviewLocation.Left )
				{
					m_unscaledPreviewRect.y = m_lastInputBottomRight.y;
					m_unscaledPreviewRect.x = m_position.x;
				}
				else if( m_autoLocation == PreviewLocation.Right )
				{
					m_unscaledPreviewRect.y = m_lastOutputBottomLeft.y;
					m_unscaledPreviewRect.x = m_lastInputBottomRight.x;
				}
				if( m_autoLocation == PreviewLocation.BottomCenter )
					m_unscaledPreviewRect.height = previewSize + 2 * marginAround;
				else if( m_autoLocation == PreviewLocation.TopCenter )
					m_unscaledPreviewRect.height = previewSize + marginAround;
				else
					m_unscaledPreviewRect.height = previewSize + ( m_visibleInputs > 0 && m_visibleOutputs > 0 ? 2 * marginAround : marginAround );
				m_unscaledPreviewRect.width = previewSize + 2 * marginAround;

				m_previewRect = m_unscaledPreviewRect;
				m_previewRect.position = ( m_previewRect.position + drawInfo.CameraOffset ) * drawInfo.InvertedZoom;
				m_auxVector2.Set( previewSize * drawInfo.InvertedZoom, previewSize * drawInfo.InvertedZoom );
				m_previewRect.size = m_auxVector2;

				if( m_autoLocation == PreviewLocation.BottomCenter )
				{
					m_auxVector2.Set( m_previewRect.position.x + scaledMarginAround, m_previewRect.position.y + scaledMarginAround );
					m_previewRect.position = m_auxVector2;
				}
				else if( m_autoLocation == PreviewLocation.TopCenter )
				{
					m_auxVector2.Set( m_previewRect.position.x + scaledMarginAround, m_previewRect.position.y );
					m_previewRect.position = m_auxVector2;
				}
				else
				{
					m_previewRect.position += new Vector2( scaledMarginAround, ( m_visibleInputs > 0 && m_visibleOutputs > 0 ? scaledMarginAround : 0 ) );
				}
			}

			// Adjust node rect after preview
			if( m_firstPreviewDraw )
			{
				m_firstPreviewDraw = false;
				ContainerGraph.ParentWindow.RequestRepaint();
				if( m_canExpand && ( m_showPreview || m_globalShowPreview ) )
				{
					if( m_autoLocation == PreviewLocation.TopCenter )
					{
						float fillWidth = m_unscaledRemainingBox.width - m_unscaledPreviewRect.width;
						m_extraSize.x = Mathf.Max( -fillWidth, 0 );
						float fillHeight = m_position.yMax - m_unscaledPreviewRect.yMax;
						m_extraSize.y = Mathf.Max( -fillHeight, 0 );
					}
					if( m_autoLocation == PreviewLocation.BottomCenter )
					{
						float fillWidth = m_position.width - m_unscaledPreviewRect.width;
						m_extraSize.x = Mathf.Max( -fillWidth, 0 );
						float fillHeight = m_position.yMax - m_unscaledPreviewRect.yMax;
						m_extraSize.y = Mathf.Max( -fillHeight, 0 );
					}
					else if( m_autoLocation == PreviewLocation.Left )
					{
						float fillWidth = m_lastOutputBottomLeft.x - m_unscaledPreviewRect.xMax;
						m_extraSize.x = Mathf.Max( -fillWidth, 0 );
						float fillHeight = m_position.yMax - m_unscaledPreviewRect.yMax;
						m_extraSize.y = Mathf.Max( -fillHeight, 0 );
					}
					else if( m_autoLocation == PreviewLocation.Right )
					{
						float fillWidth = m_position.xMax - m_unscaledPreviewRect.xMax;
						m_extraSize.x = Mathf.Max( -fillWidth, 0 );
						float fillHeight = m_position.yMax - m_unscaledPreviewRect.yMax;
						m_extraSize.y = Mathf.Max( -fillHeight, 0 );
					}

					if( m_showErrorMessage )
						m_extraSize.y += 24;
				}
				else if( m_canExpand )
				{
					m_extraSize.y = 0;
					m_extraSize.x = 0;
				}

				m_position.width = m_unpreviewedPosition.width + m_extraSize.x;
				m_position.height = m_unpreviewedPosition.height + m_extraSize.y;
			}


			if( m_showErrorMessage )
			{
				m_errorBox = m_globalPosition;
				m_errorBox.y = ( m_globalPosition.yMax - 28 * drawInfo.InvertedZoom ) + 3 * drawInfo.InvertedZoom;
				m_errorBox.height = 25 * drawInfo.InvertedZoom;
			}

			m_previousErrorMessage = m_showErrorMessage;
		}

		/// <summary>
		/// This method should only be called to draw elements, runs once per frame and after wires are drawn
		/// </summary>
		/// <param name="drawInfo"></param>
		public virtual void OnNodeRepaint( DrawInfo drawInfo )
		{
			if( !m_isVisible )
				return;

			m_colorBuffer = GUI.color;
			// Background
			GUI.color = m_infiniteLoopDetected ? Constants.InfiniteLoopColor : Constants.NodeBodyColor;
			if( m_useSquareNodeTitle || ContainerGraph.LodLevel >= ParentGraph.NodeLOD.LOD2 )
				GUI.Label( m_globalPosition, string.Empty, UIUtils.NodeWindowOffSquare );
			else
				GUI.Label( m_globalPosition, string.Empty, UIUtils.GetCustomStyle( CustomStyle.NodeWindowOff ) );

			// Header
			//GUI
			GUI.color = m_headerColor * m_headerColorModifier;
			if( m_useSquareNodeTitle || ContainerGraph.LodLevel >= ParentGraph.NodeLOD.LOD2 )
				GUI.Label( m_headerPosition, string.Empty, UIUtils.NodeHeaderSquare );
			else
				GUI.Label( m_headerPosition, string.Empty, UIUtils.GetCustomStyle( CustomStyle.NodeHeader ) );
			GUI.color = m_colorBuffer;

			// @diogo: shader function icon
			var functionNode = this as FunctionNode;
			if ( functionNode != null )
			{
				float scaledSize = FunctionNode.HeaderIconSize * drawInfo.InvertedZoom;
				GUI.DrawTexture( new Rect( m_globalPosition.x, m_globalPosition.y, scaledSize, scaledSize ), functionNode.HeaderIcon, ScaleMode.ScaleAndCrop, true );
			}

			// Title
			DrawTitle( m_titlePos );

			// Additional Tile
			if( m_hasSubtitle && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
				GUI.Label( m_addTitlePos, m_additionalContent, UIUtils.GetCustomStyle( CustomStyle.PropertyValuesTitle ) );

			// Dropdown
			if( m_hasLeftDropdown && !m_dropdownEditing && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD4 )
				GUI.Label( m_dropdownRect, string.Empty, UIUtils.PropertyPopUp );

			// Expander
			if( m_drawPreviewExpander && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD4 )
				GUI.Label( m_expandRect, string.Empty, ( m_showPreview ? UIUtils.PreviewCollapser : UIUtils.PreviewExpander ) );

			// Input Ports
			int inputCount = m_inputPorts.Count;

			for( int i = 0; i < inputCount; i++ )
			{
				if( m_inputPorts[ i ].Visible )
				{
					// Input Port Icon
					if( ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD4 )
					{
						if( m_inputPorts[ i ].Locked )
							GUI.color = Constants.LockedPortColor;
						else if( ContainerGraph.ParentWindow.Options.ColoredPorts )
							GUI.color = ContainerGraph.GetPortColor( m_inputPorts[ i ].DataType, InputPortTintName( i ) );
						else
							GUI.color = m_inputPorts[ i ].HasCustomColor ? m_inputPorts[ i ].CustomColor : UIUtils.GetColorForDataType( m_inputPorts[ i ].DataType, true, true );

						GUIStyle style = m_inputPorts[ i ].IsConnected ? UIUtils.GetCustomStyle( CustomStyle.PortFullIcon ) : UIUtils.GetCustomStyle( CustomStyle.PortEmptyIcon );
						GUI.Label( m_inputPorts[ i ].Position, string.Empty, style );

						GUI.color = m_colorBuffer;
					}

					// Input Port Label
					if( ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
					{
						if( m_inputPorts[ i ].Locked )
						{
							GUI.color = Constants.PortLockedTextColor;
							GUI.Label( m_inputPorts[ i ].LabelPosition, m_inputPorts[ i ].Name, UIUtils.InputPortLabel );
							GUI.color = m_colorBuffer;
						}
						else
						{
							InputPort internalDataSource = GetPortInternalDataSource( i );
							if( m_containerGraph.ParentWindow.GlobalShowInternalData && internalDataSource != null && !m_inputPorts[ i ].IsConnected && UIUtils.InternalDataOnPort.fontSize > 1f && ( m_inputPorts[ i ].AutoDrawInternalData || ( m_autoDrawInternalPortData && m_useInternalPortData ) ) && internalDataSource.DisplayInternalData.Length > 4 && ( ( internalDataSource.DataType >= WirePortDataType.FLOAT && internalDataSource.DataType <= WirePortDataType.INT ) || internalDataSource.DataType == WirePortDataType.UINT ) )
							{
								GUI.color = Constants.NodeBodyColor/* * new Color( 1f, 1f, 1f, 0.75f )*/;
								Rect internalBox = m_inputPorts[ i ].LabelPosition;
								m_sizeContentAux.text = internalDataSource.DisplayInternalData;
								Vector2 portText = UIUtils.InternalDataOnPort.CalcSize( m_sizeContentAux );
								internalBox.width = portText.x;
								internalBox.height = portText.y;
								internalBox.y = m_inputPorts[ i ].LabelPosition.center.y - internalBox.height * 0.5f;
								internalBox.x = GlobalPosition.x - internalBox.width - 4 * drawInfo.InvertedZoom - 1;
								Rect backBox = new Rect( internalBox );
								backBox.xMin -= 4 * drawInfo.InvertedZoom;
								backBox.xMax += 4 * drawInfo.InvertedZoom;
								backBox.yMin -= 2 * drawInfo.InvertedZoom;
								backBox.yMax += 2 * drawInfo.InvertedZoom;
								GUI.Label( backBox, string.Empty, UIUtils.InternalDataBackground );
								GUI.color *= new Color( 1f, 1f, 1f, 0.5f );
								GUI.Label( internalBox, m_sizeContentAux, UIUtils.InternalDataOnPort );
								GUI.color = m_colorBuffer;
							}
							GUI.Label( m_inputPorts[ i ].LabelPosition, m_inputPorts[ i ].Name, UIUtils.InputPortLabel );
						}
					}
				}
			}

			// Output Ports
			int outputCount = m_outputPorts.Count;
			for( int i = 0; i < outputCount; i++ )
			{
				if( m_outputPorts[ i ].Visible )
				{
					// Output Port Icon
					if( ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD4 )
					{
						if( m_outputPorts[ i ].Locked )
							GUI.color = Constants.LockedPortColor;
						else if( ContainerGraph.ParentWindow.Options.ColoredPorts )
							GUI.color = ContainerGraph.GetPortColor( m_outputPorts[ i ].DataType, m_outputPorts[ i ].StructName );
						else
							GUI.color = m_outputPorts[ i ].HasCustomColor ? m_outputPorts[ i ].CustomColor : UIUtils.GetColorForDataType( m_outputPorts[ i ].DataType, true, false );

						GUIStyle style = m_outputPorts[ i ].IsConnected ? UIUtils.GetCustomStyle( CustomStyle.PortFullIcon ) : UIUtils.GetCustomStyle( CustomStyle.PortEmptyIcon );
						GUI.Label( m_outputPorts[ i ].Position, string.Empty, style );

						GUI.color = m_colorBuffer;
					}

					// Output Port Label
					if( ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
					{
						if( m_outputPorts[ i ].Locked )
						{
							GUI.color = Constants.PortLockedTextColor;
							GUI.Label( m_outputPorts[ i ].LabelPosition, m_outputPorts[ i ].Name, UIUtils.OutputPortLabel );
							GUI.color = m_colorBuffer;
						}
						else
						{
							GUI.Label( m_outputPorts[ i ].LabelPosition, m_outputPorts[ i ].Name, UIUtils.OutputPortLabel );
						}
					}
				}
			}

			// Preview
			if( ( m_showPreview || m_globalShowPreview ) && m_drawPreview )
				DrawPreview( drawInfo, m_previewRect );

			// Error and Warning bottom message
			if( m_showErrorMessage && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				GUI.color = new Color( 0.0f, 0.0f, 0.0f, 0.5f );
				GUI.Label( m_errorBox, string.Empty, UIUtils.Separator );
				GUI.color = m_colorBuffer;

				switch( m_errorMessageTypeIsError )
				{
					default:
					case NodeMessageType.Error:
					{
						m_errorMessage.text = ErrorTitle;
						m_errorIcon.image = UIUtils.SmallErrorIcon;
						m_errorCurrentStyle = UIUtils.BoldErrorStyle;
					}
					break;
					case NodeMessageType.Warning:
					{
						m_errorMessage.text = WarningTitle;
						m_errorIcon.image = UIUtils.SmallWarningIcon;
						m_errorCurrentStyle = UIUtils.BoldWarningStyle;
					}
					break;
					case NodeMessageType.Info:
					{
						m_errorMessage.text = InfoTitle;
						m_errorIcon.image = UIUtils.SmallInfoIcon;
						m_errorCurrentStyle = UIUtils.BoldInfoStyle;
					}
					break;
					case NodeMessageType.Legacy:
					{
						m_errorMessage.text = LegacyTitle;
						m_errorIcon.image = UIUtils.SmallInfoIcon;
						m_errorCurrentStyle = UIUtils.BoldInfoStyle;
					}
					break;
				}

				Rect textBox = m_errorBox;
				textBox.y += 1 * drawInfo.InvertedZoom;
				textBox.height = 24 * drawInfo.InvertedZoom;

				float textWidth = m_errorCurrentStyle.CalcSize( m_errorMessage ).x;

				// @diogo: icon and title are centered as a single group, so both are offset from the box
				//         center by half of what the other one occupies. The icon goes through DrawTexture
				//         rather than a label image, which draws unscaled ( and so unfiltered ) at any zoom.
				float iconSize = 12 * drawInfo.InvertedZoom;
				float iconGap = 4 * drawInfo.InvertedZoom;

				Rect iconBox = textBox;
				iconBox.x = textBox.center.x - ( textWidth + iconGap ) * 0.5f - iconSize * 0.5f;
				iconBox.y += ( textBox.height - iconSize ) * 0.5f;
				iconBox.width = iconSize;
				iconBox.height = iconSize;

				textBox.x += ( iconSize + iconGap ) * 0.5f;
				GUI.Label( textBox, m_errorMessage, m_errorCurrentStyle );

				if ( m_errorIcon.image != null )
				{
					GUI.DrawTexture( iconBox, m_errorIcon.image, ScaleMode.ScaleToFit, true );
				}
			}

			// Selection Box
			if( m_selected )
			{
				DrawNodeOutline( Constants.NodeSelectedColor );
			}
			else if ( HasMissingReference )
			{
				DrawNodeOutline( Constants.NodeMissingReferenceColor );
			}
			else
			{
				DrawReferenceSelectionHighlight();
			}

			// Debug Visualizers
			//if( FinishPreviewRender || m_continuousPreviewRefresh )
			//{
			//	GUI.color = new Color( 0, 1, 0.5f, 1f );
			//	Rect r = m_globalPosition;
			//	r.width = 8;
			//	r.height = 8;
			//	r.x -= 5 * drawInfo.InvertedZoom;
			//	r.y -= 5 * drawInfo.InvertedZoom;
			//	GUI.Label( r, string.Empty, UIUtils.GetCustomStyle( CustomStyle.PortFullIcon ) );
			//	GUI.color = m_colorBuffer;
			//	FinishPreviewRender = false;
			//}
			//GUI.Label( m_remainingBox, string.Empty, UIUtils.Box );
		}

		// @diogo: reference selection highlight, mirrors the Register/Get Local Var behavior for every
		// reference-capable node ( driven by ReferencedNodeId ): selecting a source tints its reference
		// nodes green, selecting a reference tints its source blue, so linked nodes are easy to spot
		private void DrawReferenceSelectionHighlight()
		{
			if( ContainerGraph.SelectedNodes.Count == 0 )
				return;

			Color highlight;
			int referencedId = ReferencedNodeId;
			if( referencedId > -1 )
			{
				ParentNode source = ContainerGraph.GetNode( referencedId );
				if( source == null || !source.Selected )
					return;
				highlight = Constants.SpecialGetLocalVarSelectionColor;
			}
			else
			{
				if( !IsReferencedBySelectedNode() )
					return;
				highlight = Constants.SpecialRegisterLocalVarSelectionColor;
			}

			DrawNodeOutline( highlight );
		}

		private void DrawNodeOutline( Color color )
		{
			GUI.color = color;
			if ( m_useSquareNodeTitle || ContainerGraph.LodLevel >= ParentGraph.NodeLOD.LOD2 )
			{
				GUI.Label( m_globalPosition, string.Empty, UIUtils.NodeWindowOnSquare );
			}
			else
			{
				GUI.Label( m_globalPosition, string.Empty, UIUtils.GetCustomStyle( CustomStyle.NodeWindowOn ) );
			}
			GUI.color = m_colorBuffer;
		}

		private bool IsReferencedBySelectedNode()
		{
			List<ParentNode> selectedNodes = ContainerGraph.SelectedNodes;
			int count = selectedNodes.Count;
			for( int i = 0; i < count; i++ )
			{
				if( selectedNodes[ i ].ReferencedNodeId == UniqueId )
					return true;
			}
			return false;
		}

		public bool DropdownEditing { get { return m_dropdownEditing; } set { m_dropdownEditing = value; PreviewIsDirty = true; } }
		public void DisablePreview()
		{
			m_showPreview = false;
			m_sizeIsDirty = true;
		}
		/// <summary>
		/// Handles gui controls, runs before node layout
		/// </summary>
		/// <param name="drawInfo"></param>
		public virtual void DrawGUIControls( DrawInfo drawInfo )
		{
			if( !m_initialized )
				return;

			if( !m_isVisible )
				return;

			if( drawInfo.CurrentEventType == EventType.MouseDown && drawInfo.LeftMouseButtonPressed )
			{
				if( m_expandRect.Contains( drawInfo.MousePosition ) )
				{
					m_showPreview = !m_showPreview;
					m_sizeIsDirty = true;
					ContainerGraph.ParentWindow.MouseInteracted = true;
				}

				if( m_hasLeftDropdown && m_dropdownRect.Contains( drawInfo.MousePosition ) )
				{
					m_dropdownEditing = true;
				}
				else if( m_dropdownEditing )
				{
					DropdownEditing = false;
				}
			}

			DrawGuiPorts( drawInfo );
		}

		//public static bool MyRepeatButton( DrawInfo drawInfo, Rect position, string text, GUIStyle style )
		//{
		//	if(/* drawInfo.CurrentEventType == EventType.MouseDown &&*/ position.Contains( drawInfo.MousePosition ) )
		//	{
		//		UIUtils.CurrentWindow.MouseInteracted = true;
		//		return true;
		//	}
		//	return false;
		//}

		public void DrawGuiPorts( DrawInfo drawInfo )
		{
			if( !m_initialized )
				return;

			if( !m_isVisible )
				return;

			if( drawInfo.CurrentEventType == EventType.MouseDown )
			{
				int inputCount = m_inputPorts.Count;
				int outputCount = m_outputPorts.Count;

				for( int i = 0; i < inputCount; i++ )
				{
					if( m_inputPorts[ i ].Visible && !m_inputPorts[ i ].Locked && m_isVisible && m_inputPorts[ i ].ActivePortArea.Contains( drawInfo.MousePosition ) && drawInfo.LeftMouseButtonPressed )
					{
						UIUtils.CurrentWindow.MouseInteracted = true;
						m_inputPorts[ i ].Click();
						// need to put the mouse button on a hot state so it will detect the Mouse Up event correctly on the Editor Window
						int controlID = GUIUtility.GetControlID( FocusType.Passive );
						//int controlID = GUIUtility.GetControlID( "repeatButton".GetHashCode(), FocusType.Passive, m_inputPorts[ i ].ActivePortArea );
						GUIUtility.hotControl = controlID;

						bool saveReference = true;
						if( m_inputPorts[ i ].IsConnected )
						{
							double doubleTapTime = EditorApplication.timeSinceStartup;
							bool doubleTap = ( doubleTapTime - m_doubleClickTimestamp ) < DoubleClickTime;
							m_doubleClickTimestamp = doubleTapTime;

							if( doubleTap )
							{
								m_containerGraph.DeleteConnection( true, UniqueId, m_inputPorts[ i ].PortId, true, true );
								Event.current.Use();
							}
							else
							//if ( AppyModifierToPort( _inputPorts[ i ], true ) )
							//{
							//saveReference = false;
							//}
							if( !ApplyModifierToPort( m_inputPorts[ i ], true ) )
							{
								UIUtils.ShowContextOnPick = false;
								PickInput( m_inputPorts[ i ] );
							}
							saveReference = false;
						}

						if( saveReference && !m_containerGraph.ParentWindow.WireReferenceUtils.InputPortReference.IsValid )
						//if ( !modifierApplied && !UIUtils.InputPortReference.IsValid )
						{
							m_containerGraph.ParentWindow.WireReferenceUtils.SetInputReference( m_uniqueId, m_inputPorts[ i ].PortId, m_inputPorts[ i ].DataType, m_inputPorts[ i ].TypeLocked );
						}

						IsDirty = true;
						inputCount = m_inputPorts.Count;
					}
				}

				// @diogo: an input click can connect or disconnect a wire, and a node whose outputs follow
				// its input ( Struct Split ) will have dropped ports since the count above was taken
				outputCount = m_outputPorts.Count;

				for( int i = 0; i < outputCount; i++ )
				{
					if( m_outputPorts[ i ].Visible && m_outputPorts[ i ].ActivePortArea.Contains( drawInfo.MousePosition ) && drawInfo.LeftMouseButtonPressed )
					{
						UIUtils.CurrentWindow.MouseInteracted = true;
						m_outputPorts[ i ].Click();
						// need to put the mouse button on a hot state so it will detect the Mouse Up event correctly on the Editor Window
						int controlID = GUIUtility.GetControlID( FocusType.Passive );
						//int controlID = GUIUtility.GetControlID( "aseRepeatButton".GetHashCode(), FocusType.Passive, m_outputPorts[ i ].ActivePortArea );
						GUIUtility.hotControl = controlID;

						bool saveReference = true;
						if( m_outputPorts[ i ].IsConnected )
						{
							if( ApplyModifierToPort( m_outputPorts[ i ], false ) )
							{
								saveReference = false;
							}
						}

						if( saveReference && !m_containerGraph.ParentWindow.WireReferenceUtils.OutputPortReference.IsValid )
						{
							m_containerGraph.ParentWindow.WireReferenceUtils.SetOutputReference( m_uniqueId, m_outputPorts[ i ].PortId, m_outputPorts[ i ].DataType, false );
						}

						IsDirty = true;
						outputCount = m_outputPorts.Count;
					}
				}
			}

			//Preview buttons
			// @diogo: allocated on every event: gating these controls by event type shifts the id of every
			// control drawn afterwards ( side panels included ) between press and release, dropping the click
			if( m_drawPreviewMaskButtons )
				DrawPreviewMaskButtonsLayout( drawInfo, m_previewRect );
		}

		/// <summary>
		/// Can be used to draw an entire node, runs after wires
		/// </summary>
		/// <param name="drawInfo"></param>
		public virtual void Draw( DrawInfo drawInfo )
		{
			if( !m_initialized )
				return;

			if( drawInfo.CurrentEventType == EventType.Repaint )
				OnNodeRepaint( drawInfo );
		}

		public virtual void SetPreviewInputs()
		{
			if( !HasPreviewShader || !m_initialized || Preferences.User.DisablePreviews )
				return;

			int count = m_inputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_inputPorts[ i ].IsConnected && m_inputPorts[ i ].InputNodeHasPreview( ContainerGraph ) )
				{
					m_inputPorts[ i ].SetPreviewInputTexture( ContainerGraph, i );
				}
				else
				{
					m_inputPorts[ i ].SetPreviewInputValue( ContainerGraph, i );
				}
			}

			// @diogo: ports past the input count ( first render, or a dynamic port removed ) keep their declared
			// default instead of whatever slice was last bound there; see ASEPreviewSlices.BindUnusedPorts.
			if( count != m_previewBoundInputCount )
			{
				ASEPreviewSlices.BindUnusedPorts( PreviewMaterial, count );
				m_previewBoundInputCount = count;
			}
		}


		public bool SafeDraw( DrawInfo drawInfo )
		{
			EditorGUI.BeginChangeCheck();
			Draw( drawInfo );
			if( EditorGUI.EndChangeCheck() )
			{
				SaveIsDirty = true;
				return true;
			}
			return false;
		}

		public bool ShowTooltip( DrawInfo drawInfo )
		{
			// @diogo: the link box lies outside the node, so no port can be under the cursor while it is reachable
			InputPort tooltipPort = m_linkVisibility ? null : GetTooltipPortAt( drawInfo.MousePosition );
			int tooltipPortId = ( tooltipPort != null ) ? tooltipPort.PortId : -1;

			// @diogo: moving between the node body and a port, or between ports, restarts the delay
			if ( tooltipPortId != m_tooltipPortId )
			{
				m_tooltipPortId = tooltipPortId;
				m_tooltipTimestamp = Time.realtimeSinceStartup;
			}

			if ( string.IsNullOrEmpty( m_tooltipText ) && tooltipPort == null )
			{
				return false;
			}

			// @diogo: any held-button gesture ( dragging nodes, box selecting ) hides the tooltip and restarts its
			// delay; the moving countdown alone flickers because it lapses whenever the pointer briefly holds still
			if ( ContainerGraph.ParentWindow.LeftMouseButtonPressed )
			{
				m_linkVisibility = false;
				m_tooltipTimestamp = Time.realtimeSinceStartup;
				return false;
			}

			if( m_globalPosition.Contains( drawInfo.MousePosition ) || m_linkVisibility )
			{
				if( m_tooltipTimestamp + 0.6f < Time.realtimeSinceStartup || m_linkVisibility )
				{
					bool errorTooltip = false;
					if( m_showErrorMessage && m_errorBox.Contains( drawInfo.MousePosition ) && !string.IsNullOrEmpty( m_errorMessageTooltip ) )
						errorTooltip = true;

					bool portTooltip = !errorTooltip && tooltipPort != null;
					string tooltipText = errorTooltip ? m_errorMessageTooltip : ( portTooltip ? tooltipPort.Tooltip : m_tooltipText );

					Rect globalTooltipPos = m_globalPosition;
					GUIContent temp = new GUIContent( tooltipText );
					UIUtils.TooltipBox.wordWrap = false;
					Vector2 optimal = UIUtils.TooltipBox.CalcSize( temp );
					if( optimal.x > 300f )
					{
						UIUtils.TooltipBox.wordWrap = true;
						optimal.x = 300f;
						optimal.y = UIUtils.TooltipBox.CalcHeight( temp, 300f );
					}

					globalTooltipPos.width = Mathf.Max( 120, optimal.x );
					globalTooltipPos.height = optimal.y;
					globalTooltipPos.center = m_globalPosition.center;

					if ( !errorTooltip && !portTooltip && m_hasTooltipLink )
						globalTooltipPos.height += 16;

					if ( errorTooltip )
					{
						globalTooltipPos.y = 10 + m_globalPosition.yMax;
					}
					else if ( portTooltip )
					{
						globalTooltipPos.x = tooltipPort.Position.x;
						globalTooltipPos.y = tooltipPort.Position.yMin - 10 - globalTooltipPos.height;
					}
					else
					{
						globalTooltipPos.y = m_globalPosition.yMin - 10 - globalTooltipPos.height;
					}

					if ( globalTooltipPos.x < 10 )
						globalTooltipPos.x = 10;

					if( globalTooltipPos.x + globalTooltipPos.width > Screen.width - 10 )
						globalTooltipPos.x = Screen.width - globalTooltipPos.width - 10;

					//UNCOMMENT this for auto adjust tooltip to the top window box
					//if( globalTooltipPos.y < 40 )
					//	globalTooltipPos.y = 40;

					if( errorTooltip && globalTooltipPos.y + globalTooltipPos.height > Screen.height - 32 )
						globalTooltipPos.y = Screen.height - 32 - globalTooltipPos.height;

					GUI.Label( globalTooltipPos, temp, UIUtils.TooltipBox );

					if ( !errorTooltip && !portTooltip && m_hasTooltipLink )
					{
						Rect link = globalTooltipPos;
						link.y = globalTooltipPos.yMax - 16;
						link.height = 16;
						link.width = 86;
						link.x = globalTooltipPos.center.x - 43;
						Rect hover = globalTooltipPos;
						hover.yMax += 15;// m_globalPosition.yMax;
						m_linkVisibility = hover.Contains( drawInfo.MousePosition );
						if( link.Contains( drawInfo.MousePosition ) )
						{
							if( drawInfo.CurrentEventType == EventType.MouseDown )
							{
								if( m_tooltipTimestamp + 1.25f < Time.realtimeSinceStartup )
								{
									Application.OpenURL( Attributes.NodeUrl );
								}
							}
							else
							{
								UIUtils.MainSkin.customStyles[ 52 ].Draw( link, WikiLinkStr, true, false, false, false );
							}
						}
						else
						{
							GUI.Label( link, WikiLinkStr, UIUtils.MainSkin.customStyles[ 52 ] );
						}
					}
					ContainerGraph.ParentWindow.RequestRepaint();
					return true;
				}
			}
			else
			{
				if( !m_linkVisibility )
					m_tooltipTimestamp = Time.realtimeSinceStartup;
			}

			return false;
		}

		// @diogo: ActivePortArea stops short of the label text, so hovering spans the icon through the label
		InputPort GetTooltipPortAt( Vector2 mousePosition )
		{
			if ( ContainerGraph.LodLevel > ParentGraph.NodeLOD.LOD3 || !m_globalPosition.Contains( mousePosition ) )
			{
				return null;
			}

			int count = m_inputPorts.Count;
			for ( int i = 0; i < count; i++ )
			{
				InputPort port = m_inputPorts[ i ];
				if ( port.Visible && !string.IsNullOrEmpty( port.Tooltip ) )
				{
					Rect hoverArea = port.Position;
					hoverArea.xMax = port.LabelPosition.x + port.LabelSize.x;
					if ( hoverArea.Contains( mousePosition ) )
					{
						return port;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Explains the deprecation on the node's LEGACY tab and on its properties help box. Nodes whose
		/// alternative comes with a caveat override this with their own text.
		/// </summary>
		public virtual string DeprecationMessage
		{
			get
			{
				return string.IsNullOrEmpty( m_nodeAttribs.DeprecatedAlternative ) ?
					Constants.DeprecatedNoAlternativeMessageStr :
					string.Format( Constants.DeprecatedMessageStr, m_nodeAttribs.DeprecatedAlternative );
			}
		}

		private void DrawDeprecationHelpBox()
		{
			if ( m_nodeAttribs != null && m_nodeAttribs.Deprecated )
			{
				EditorGUILayout.HelpBox( DeprecationMessage, MessageType.Info );
			}
		}

		public virtual bool SafeDrawProperties()
		{
			DrawDeprecationHelpBox();

			EditorGUI.BeginChangeCheck();
			PreDrawProperties();
			if( m_autoWrapProperties )
			{
				NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, Constants.ParameterLabelStr, DrawProperties );
			}
			else
			{
				DrawProperties();
			}
			bool changed = EditorGUI.EndChangeCheck();
			// @diogo: drawn outside the change check so browsing references never flags the shader as dirty
			DrawReferencesGroup();
			if ( changed )
			{
				PreviewIsDirty = true;
				//UIUtils.RecordObject(this);
				//MarkForPreviewUpdate();
				return true;
			}
			return false;
		}

		// @diogo: inverse of ReferencedNodeId, so any node pointed at by a reference picker lists its holders
		private void RefreshReferenceHolders()
		{
			if ( m_referenceHolders == null )
			{
				m_referenceHolders = new List<ParentNode>();
			}

			m_referenceHolders.Clear();

			// @diogo: an unassigned id would match every node not referencing anything, as those also report -1
			if ( UniqueId > -1 )
			{
				List<ParentNode> nodes = ContainerGraph.AllNodes;
				int count = nodes.Count;
				for ( int i = 0; i < count; i++ )
				{
					if ( nodes[ i ] != null && nodes[ i ] != this && nodes[ i ].ReferencesNode( UniqueId ) )
					{
						m_referenceHolders.Add( nodes[ i ] );
					}
				}
				// @diogo: AllNodes is in z-order, which shifts with clicks and save/load; sort so rows stay put
				m_referenceHolders.Sort( ( a, b ) => a.UniqueId.CompareTo( b.UniqueId ) );
			}
		}

		private void DrawReferencesGroup()
		{
			RefreshReferenceHolders();
			if ( m_referenceHolders.Count > 0 || UnreferencedMessage != null )
			{
				NodeUtils.DrawPropertyGroup( ref m_referencesFoldout, ReferencesStr, DrawReferences );
			}
		}

		// Shown in the References foldout when no node references this one; null hides the empty foldout.
		protected virtual string UnreferencedMessage { get { return null; } }

		private void DrawReferences()
		{
			int count = m_referenceHolders.Count;
			if ( count > 0 )
			{
				for ( int i = 0; i < count; i++ )
				{
					ParentNode holder = m_referenceHolders[ i ];
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField( string.Format( ReferenceHolderLabel, holder.UniqueId, holder.Attributes.Name ) );
					if ( GUILayout.Button( "\u25BA", "minibutton", GUILayout.Width( 17 ) ) )
					{
						ContainerGraph.ParentWindow.FocusOnNode( holder, 0, false );
					}
					EditorGUILayout.EndHorizontal();
				}

				if ( GUILayout.Button( BackStr ) )
				{
					ContainerGraph.ParentWindow.FocusOnNode( this, 0, false );
				}
			}
			else
			{
				EditorGUILayout.HelpBox( UnreferencedMessage, MessageType.Info );
			}
		}


		public virtual void PreDrawProperties()
		{
			if( m_useInternalPortData && m_autoDrawInternalPortData )
			{
				DrawInternalDataGroup();
			}
		}

		virtual public void DrawProperties() { }

		protected void DrawInternalDataGroup()
		{
			bool drawInternalDataUI = false;
			int inputCount = m_inputPorts.Count;
			if( inputCount > 0 )
			{
				for( int i = 0; i < inputCount; i++ )
				{
					if( m_inputPorts[ i ].Available && m_inputPorts[ i ].ValidInternalData && !m_inputPorts[ i ].IsConnected /*&& ( m_inputPorts[ i ].AutoDrawInternalData || ( m_autoDrawInternalPortData && m_useInternalPortData ) )*/  /*&& m_inputPorts[ i ].AutoDrawInternalData*/ )
					{
						drawInternalDataUI = true;
						break;
					}
				}
			}

			if( drawInternalDataUI )
				NodeUtils.DrawPropertyGroup( ref m_internalDataFoldout, Constants.InternalDataLabelStr, () =>
				{
					for( int i = 0; i < m_inputPorts.Count; i++ )
					{
						if( m_inputPorts[ i ].ValidInternalData && !m_inputPorts[ i ].IsConnected && m_inputPorts[ i ].Visible /*&& m_inputPorts[ i ].AutoDrawInternalData*/ )
						{
							m_inputPorts[ i ].ShowInternalData( this );
						}
					}
				} );
		}

		protected void PickInput( InputPort port )
		{
			WireReference connection = port.GetConnection( 0 );
			OutputPort from = port.GetOutputConnection( 0 );

			m_containerGraph.ParentWindow.WireReferenceUtils.OutputPortReference.SetReference( from.NodeId, from.PortId, from.DataType, connection.TypeLocked );
			m_containerGraph.DeleteConnection( true, UniqueId, port.PortId, true, true );
			//TODO: check if not necessary
			Event.current.Use();
			IsDirty = true;
			SetSaveIsDirty();
		}

		protected bool ApplyModifierToPort( WirePort port, bool isInput )
		{
			bool modifierApplied = false;
			switch( Event.current.modifiers )
			{
				case EventModifiers.Alt:
				{
					m_containerGraph.DeleteConnection( isInput, UniqueId, port.PortId, true, true );
					modifierApplied = true;
					m_containerGraph.ParentWindow.InvalidateAlt();
				}
				break;
				case EventModifiers.Control:
				{
					//WireReference connection = port.GetConnection( 0 );
					//if ( isInput )
					//{
					//	UIUtils.OutputPortReference.SetReference( connection.NodeId, connection.PortId, connection.DataType, connection.TypeLocked );
					//}
					//else
					//{
					//	UIUtils.InputPortReference.SetReference( connection.NodeId, connection.PortId, connection.DataType, connection.TypeLocked );
					//}

					//UIUtils.DeleteConnection( isInput, UniqueId, port.PortId, true );
					//modifierApplied = true;

					if( !isInput )
					{
						WireReference connection = port.GetConnection( 0 );
						m_containerGraph.ParentWindow.WireReferenceUtils.InputPortReference.SetReference( connection.NodeId, connection.PortId, connection.DataType, connection.TypeLocked );
						m_containerGraph.DeleteConnection( isInput, UniqueId, port.PortId, true, true );
						modifierApplied = true;
					}
				}
				break;
			}

			if( isInput )
				m_containerGraph.ParentWindow.WireReferenceUtils.SwitchPortReference.SetReference( port.NodeId, port.PortId, port.DataType, false ); //always save last connection
			else
				m_containerGraph.ParentWindow.WireReferenceUtils.SwitchPortReference.SetReference( -1, -1, WirePortDataType.OBJECT, false ); //invalidate connection

			if( modifierApplied )
			{
				Event.current.Use();
				IsDirty = true;
				SetSaveIsDirty();
			}
			return modifierApplied;
		}

		public void DeleteAllInputConnections( bool alsoDeletePorts , bool inhibitWireNodeAutoDel = false )
		{
			int count = m_inputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_inputPorts[ i ].IsConnected )
				{
					ParentNode connNode = null;
					if( inhibitWireNodeAutoDel )
					{
						connNode = m_inputPorts[ i ].GetOutputNode();
						connNode.Alive = false;
					}
					m_containerGraph.DeleteConnection( true, UniqueId, m_inputPorts[ i ].PortId, false, true );
					if( inhibitWireNodeAutoDel )
					{
						connNode.Alive = true;
					}
				}

			}
			if( alsoDeletePorts )
			{
				m_inputPorts.Clear();
				m_inputPortsDict.Clear();
			}
			SetSaveIsDirty();
		}

		public void DeleteAllOutputConnections( bool alsoDeletePorts )
		{
			int count = m_outputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				if( m_outputPorts[ i ].IsConnected )
					m_containerGraph.DeleteConnection( false, UniqueId, m_outputPorts[ i ].PortId, false, true );
			}

			if( alsoDeletePorts )
			{
				m_outputPorts.Clear();
				m_outputPortsDict.Clear();
			}
			SetSaveIsDirty();
		}

		public void DeleteInputPortByArrayIdx( int arrayIdx, bool registerUndo = true )
		{
			if( arrayIdx >= m_inputPorts.Count )
				return;

			m_containerGraph.DeleteConnection( true, UniqueId, m_inputPorts[ arrayIdx ].PortId, false, true, registerUndo );
			m_inputPortsDict.Remove( m_inputPorts[ arrayIdx ].PortId );
			m_inputPorts.RemoveAt( arrayIdx );

			m_sizeIsDirty = true;
			SetSaveIsDirty();
			RecalculateInputPortIdx();
		}

		public void DeleteOutputPortByArrayIdx( int portIdx )
		{
			if( portIdx >= m_outputPorts.Count )
				return;

			m_containerGraph.DeleteConnection( false, UniqueId, m_outputPorts[ portIdx ].PortId, false, true );
			m_outputPortsDict.Remove( m_outputPorts[ portIdx ].PortId );
			m_outputPorts.RemoveAt( portIdx );
			m_sizeIsDirty = true;
		}

		public InputPort GetInputPortByArrayId( int id )
		{
			if( id < m_inputPorts.Count )
				return m_inputPorts[ id ];

			return null;
		}

		public OutputPort GetOutputPortByArrayId( int id )
		{
			if( id < m_outputPorts.Count )
				return m_outputPorts[ id ];

			return null;
		}

		public InputPort GetInputPortByUniqueId( int id )
		{
			if( m_inputPortsDict.ContainsKey( id ) )
				return m_inputPortsDict[ id ];

			if( m_inputPortsDict.Count != m_inputPorts.Count )
				m_repopulateDictionaries = true;

			int inputCount = m_inputPorts.Count;
			for( int i = 0; i < inputCount; i++ )
			{
				if( m_inputPorts[ i ].PortId == id )
				{
					return m_inputPorts[ i ];
				}
			}
			return null;
		}

		public InputPort GetInputPortByName( string name )
		{
			return InputPorts.Find( x => x.Name.Equals( name ) );
		}

		public InputPort GetInputPortByExternalLinkId( string internalDataName )
		{
			return InputPorts.Find( x => x.ExternalLinkId.Equals( internalDataName ) );
		}

		public OutputPort GetOutputPortByUniqueId( int id )
		{
			if( m_outputPortsDict.ContainsKey( id ) )
				return m_outputPortsDict[ id ];

			if( m_outputPortsDict.Count != m_outputPorts.Count )
				m_repopulateDictionaries = true;

			int outputCount = m_outputPorts.Count;
			for( int i = 0; i < outputCount; i++ )
			{
				if( m_outputPorts[ i ].PortId == id )
					return m_outputPorts[ i ];
			}
			return null;
		}

		public virtual void AfterDuplication(){}

		public override string ToString()
		{
			string dump = "";
			dump += ( "Type: " + GetType() );
			dump += ( " Unique Id: " + UniqueId + "\n" );
			dump += ( " Inputs: \n" );

			int inputCount = m_inputPorts.Count;
			int outputCount = m_outputPorts.Count;

			for( int inputIdx = 0; inputIdx < inputCount; inputIdx++ )
			{
				dump += ( m_inputPorts[ inputIdx ] + "\n" );
			}
			dump += ( "Outputs: \n" );
			for( int outputIdx = 0; outputIdx < outputCount; outputIdx++ )
			{
				dump += ( m_outputPorts[ outputIdx ] + "\n" );
			}
			return dump;
		}

		public string GetValueFromOutputStr( int outputId, WirePortDataType inputPortType, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( ignoreLocalvar )
			{
				return GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			}
			OutputPort outPort = GetOutputPortByUniqueId( outputId );
			if( outPort.IsLocalValue( dataCollector.PortCategory ) )
			{
				if( outPort.DataType != WirePortDataType.OBJECT && outPort.DataType != inputPortType )
				{
					return UIUtils.CastPortType( ref dataCollector, CurrentPrecisionType, null, outPort.DataType, inputPortType, outPort.LocalValue( dataCollector.PortCategory ) );
				}
				else
				{
					return outPort.LocalValue( dataCollector.PortCategory );
				}
			}

			string result = GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			result = CreateOutputLocalVariable( outputId, result, ref dataCollector );

			if( outPort.DataType != WirePortDataType.OBJECT && outPort.DataType != inputPortType )
			{
				result = UIUtils.CastPortType( ref dataCollector, CurrentPrecisionType, null, outPort.DataType, inputPortType, result );
			}
			return result;
		}

		public virtual string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( dataCollector.IsSRP )
			{
				switch( dataCollector.CurrentSRPType )
				{
					case TemplateSRPType.HDRP: if(OnHDAction!=null) OnHDAction( outputId, ref dataCollector ); break;
					case TemplateSRPType.URP:if(OnLightweightAction != null) OnLightweightAction( outputId, ref dataCollector ); break;
				}
			}
			return string.Empty;
		}

		public string GenerateValueInVertex( ref MasterNodeDataCollector dataCollector, WirePortDataType dataType, string dataValue, string dataName, bool createInterpolator )
		{

			if( !dataCollector.IsFragmentCategory )
				return dataValue;

			//TEMPLATES
			if( dataCollector.IsTemplate )
			{
				if( createInterpolator && dataCollector.TemplateDataCollectorInstance.HasCustomInterpolatedData( dataName ) )
					return dataName;

				MasterNodePortCategory category = dataCollector.PortCategory;
				dataCollector.PortCategory = MasterNodePortCategory.Vertex;

				dataCollector.PortCategory = category;

				if( createInterpolator )
				{
					dataCollector.TemplateDataCollectorInstance.RegisterCustomInterpolatedData( dataName, dataType, CurrentPrecisionType, dataValue );
				}
				else
				{
					dataCollector.AddToVertexLocalVariables( -1, CurrentPrecisionType, dataType, dataName, dataValue );
				}

				return dataName;
			}

			//SURFACE
			{
				if( dataCollector.TesselationActive )
				{
					UIUtils.ShowMessage( UniqueId, "Unable to use Vertex to Frag when Tessellation is active" );
					return m_outputPorts[ 0 ].ErrorValue;
				}

				if( createInterpolator )
					dataCollector.AddToInput( UniqueId, dataName, dataType, CurrentPrecisionType );

				MasterNodePortCategory portCategory = dataCollector.PortCategory;
				dataCollector.PortCategory = MasterNodePortCategory.Vertex;
				if( createInterpolator )
				{
					dataCollector.AddLocalVariable( UniqueId, Constants.VertexShaderOutputStr + "." + dataName, dataValue + ";" );
				}
				else
				{
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, dataType, dataName, dataValue );
				}
				dataCollector.PortCategory = portCategory;
				return createInterpolator ? Constants.InputVarStr + "." + dataName : dataName;
			}
		}

		public string GenerateInputInVertex( ref MasterNodeDataCollector dataCollector , int inputPortUniqueId , string varName , bool createInterpolator , bool noInterpolationFlag = false , bool sampleFlag = false )
		{
			InputPort inputPort = GetInputPortByUniqueId( inputPortUniqueId );
			if( !dataCollector.IsFragmentCategory )
			{
				string value = inputPort.GeneratePortInstructions( ref dataCollector );
				dataCollector.AddLocalVariable( -1 , CurrentPrecisionType , inputPort.DataType , varName , value );
				return varName;
			}

			//TEMPLATES
			if( dataCollector.IsTemplate )
			{
				if( createInterpolator && dataCollector.TemplateDataCollectorInstance.HasCustomInterpolatedData( varName ) )
					return varName;

				MasterNodePortCategory category = dataCollector.PortCategory;
				dataCollector.PortCategory = MasterNodePortCategory.Vertex;
				//bool dirtyVertexVarsBefore = dataCollector.DirtyVertexVariables;
				//ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Vertex );

				string data = inputPort.GeneratePortInstructions( ref dataCollector );

				dataCollector.PortCategory = category;
				//if( !dirtyVertexVarsBefore && dataCollector.DirtyVertexVariables )
				//{
				//	dataCollector.AddVertexInstruction( dataCollector.VertexLocalVariablesFromList, UniqueId, false );
				//	dataCollector.ClearVertexLocalVariables();
				//	ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Vertex );
				//}

				//ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Fragment );

				if( createInterpolator )
				{
					dataCollector.TemplateDataCollectorInstance.RegisterCustomInterpolatedData( varName, inputPort.DataType, CurrentPrecisionType, data,true,MasterNodePortCategory.Fragment,noInterpolationFlag,sampleFlag );
				}
				else
				{
					dataCollector.AddToVertexLocalVariables( -1, CurrentPrecisionType, inputPort.DataType, varName, data );
				}

				return varName;
			}

			//SURFACE
			{
				if( dataCollector.TesselationActive )
				{
					UIUtils.ShowMessage( UniqueId, "Unable to use Vertex to Frag when Tessellation is active" );
					return m_outputPorts[ 0 ].ErrorValue;
				}

				if( createInterpolator )
					dataCollector.AddToInput( UniqueId, varName, inputPort.DataType, CurrentPrecisionType );

				MasterNodePortCategory portCategory = dataCollector.PortCategory;
				dataCollector.PortCategory = MasterNodePortCategory.Vertex;

				//bool dirtyVertexVarsBefore = dataCollector.DirtyVertexVariables;

				//ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Vertex );

				string vertexVarValue = inputPort.GeneratePortInstructions( ref dataCollector );
				if( createInterpolator )
				{
					dataCollector.AddLocalVariable( UniqueId, Constants.VertexShaderOutputStr + "." + varName, vertexVarValue + ";" );
				}
				else
				{
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, inputPort.DataType, varName, vertexVarValue );
				}

				dataCollector.PortCategory = portCategory;

				//if( !dirtyVertexVarsBefore && dataCollector.DirtyVertexVariables )
				//{
				//	dataCollector.AddVertexInstruction( dataCollector.VertexLocalVariables, UniqueId, false );
				//	dataCollector.ClearVertexLocalVariables();
				//	ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Vertex );
				//}

				//ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Fragment );

				return createInterpolator ? Constants.InputVarStr + "." + varName : varName;
			}
		}


		protected virtual void OnUniqueIDAssigned() { }

		// @diogo: a wire whose consumer emits nothing in this compile - a Register with no readers, a locked
		// master port - must not force a declared local; mirrors BranchScopeUtils.ConsumerEscapes (#365)
		protected int ActiveConnectionCount( OutputPort port )
		{
			int count = 0;
			List<WireReference> connections = port.ExternalReferences;
			for ( int i = 0; i < connections.Count; i++ )
			{
				ParentNode consumer = m_containerGraph.GetNode( connections[ i ].NodeId );
				if ( consumer is OutputNode )
				{
					InputPort consumerPort = consumer.GetInputPortByUniqueId( connections[ i ].PortId );
					if ( consumerPort == null || consumerPort.IsConnected )
					{
						count++;
					}
				}
				else if ( consumer != null && consumer.Category != 0 )
				{
					count++;
				}
			}
			return count;
		}

		public string CreateOutputLocalVariable( int outputArrayId, string value, ref MasterNodeDataCollector dataCollector )
		{
			OutputPort port = GetOutputPortByUniqueId( outputArrayId );

			if( port.IsLocalValue( dataCollector.PortCategory ) )
				return port.LocalValue( dataCollector.PortCategory );

			if( ActiveConnectionCount( port ) > 1 || dataCollector.RequiresHazardDeclaration( this ) )
			{
				RegisterLocalVariable( outputArrayId, value, ref dataCollector );
				return port.LocalValue( dataCollector.PortCategory );
			}
			else
			{
				// revisit later (break to components case)
				port.SetLocalValue( value, dataCollector.PortCategory );
			}

			return value;
		}

		public void RegisterLocalVariable( int outputArrayId, string value, ref MasterNodeDataCollector dataCollector, string customName = null )
		{
			OutputPort port = GetOutputPortByUniqueId( outputArrayId );

			// @diogo: a struct sits above the sampler flag but is a declarable type, provided the name
			// that declares it reached the port; without one there is nothing to write but the value
			bool declarableStruct = port.DataType == WirePortDataType.STRUCT && !string.IsNullOrEmpty( port.StructName );

			if( !declarableStruct && ( (int)port.DataType >= (int)( 1 << 10 ) || port.DataType == WirePortDataType.OBJECT ) ) //10 is the flag start of sampler types
			{
				port.SetLocalValue( value, dataCollector.PortCategory );
				return;
			}

			bool vertexMode = dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation;
			string localVar = port.ConfigOutputLocalValue( CurrentPrecisionType, value, customName, dataCollector.PortCategory );

			if( vertexMode )
			{
				dataCollector.AddToVertexLocalVariables( m_uniqueId, localVar );
			}
			else
			{
				dataCollector.AddToFragmentLocalVariables( m_uniqueId, localVar );
			}
		}

		public void InvalidateConnections()
		{
			int inputCount = m_inputPorts.Count;
			int outputCount = m_outputPorts.Count;

			for( int i = 0; i < inputCount; i++ )
			{
				m_inputPorts[ i ].InvalidateAllConnections();
			}

			for( int i = 0; i < outputCount; i++ )
			{
				m_outputPorts[ i ].InvalidateAllConnections();
			}
		}

		public virtual bool OnClick( Vector2 currentMousePos2D )
		{
			bool singleClick = true;
			if( ( EditorApplication.timeSinceStartup - m_lastTimeSelected ) < NodeClickTime )
			{
				// @diogo: on a node holding a reference, double-click goes to its source like the picker's focus button
				ParentNode referenced = ContainerGraph.GetNode( ReferencedNodeId );
				if ( referenced != null )
				{
					ContainerGraph.ParentWindow.FocusOnNode( referenced, 0, false );
				}
				else
				{
					OnNodeDoubleClicked( currentMousePos2D );
				}
				singleClick = false;
			}

			m_lastTimeSelected = EditorApplication.timeSinceStartup;
			return singleClick;
		}

		public virtual void OnNodeDoubleClicked( Vector2 currentMousePos2D )
		{
			ContainerGraph.ParentWindow.ParametersWindow.IsMaximized = !ContainerGraph.ParentWindow.ParametersWindow.IsMaximized;
		}

		public virtual void OnNodeSelected( bool value )
		{
			if( !value )
			{
				if( m_inputPorts != null )
				{
					int count = m_inputPorts.Count;
					for( int i = 0; i < count; i++ )
					{
						m_inputPorts[ i ].ResetEditing();
					}
				}

				if( m_outputPorts != null )
				{
					int count = m_outputPorts.Count;
					for( int i = 0; i < count; i++ )
					{
						m_outputPorts[ i ].ResetEditing();
					}
				}
			}
		}

		public void ResetOutputLocals()
		{
			int outputCount = m_outputPorts.Count;
			for( int i = 0; i < outputCount; i++ )
			{
				m_outputPorts[ i ].ResetLocalValue();
			}
		}


		public void ResetOutputLocalsIfNot( MasterNodePortCategory category )
		{
			int outputCount = m_outputPorts.Count;
			for( int i = 0; i < outputCount; i++ )
			{
				//if( !m_outputPorts[ i ].IsLocalOnCategory( category ) )
				//	m_outputPorts[ i ].ResetLocalValue();
				m_outputPorts[ i ].ResetLocalValueIfNot( category );
			}
		}

		public virtual void Rewire() { }

		//public virtual List<int> NodeReferences { get { return null; } }

		public int UniqueId
		{
			get { return m_uniqueId; }

			set
			{
				m_uniqueId = value;

				int inputCount = m_inputPorts.Count;
				int outputCount = m_outputPorts.Count;

				for( int inputIdx = 0; inputIdx < inputCount; inputIdx++ )
				{
					m_inputPorts[ inputIdx ].NodeId = value;
				}

				for( int outputIdx = 0; outputIdx < outputCount; outputIdx++ )
				{
					m_outputPorts[ outputIdx ].NodeId = value;
				}
				OnUniqueIDAssigned();
			}
		}
		public void SetBaseUniqueId( int uniqueId, bool setOnPorts = false )
		{
			m_uniqueId = uniqueId;
			if( setOnPorts )
			{
				int inputCount = m_inputPorts.Count;
				int outputCount = m_outputPorts.Count;

				for( int inputIdx = 0; inputIdx < inputCount; inputIdx++ )
				{
					m_inputPorts[ inputIdx ].NodeId = uniqueId;
				}

				for( int outputIdx = 0; outputIdx < outputCount; outputIdx++ )
				{
					m_outputPorts[ outputIdx ].NodeId = uniqueId;
				}
			}
		}

		public string OutputId
		{
			get
			{
				if( ContainerGraph.GraphId > 0 )
					return UniqueId + "_g" + ContainerGraph.GraphId;
				else
					return UniqueId.ToString();
			}
		}


		public virtual Rect Position { get { return m_position; } }
		public Rect TruePosition { get { return m_position; } }

		public Vector2 CenterPosition { get { return new Vector2( m_position.x + m_position.width * 0.5f, m_position.y + m_position.height * 0.5f ); ; } }

		public Rect GlobalPosition { get { return m_globalPosition; } }

		public Vector2 Corner { get { return new Vector2( m_position.x + m_position.width, m_position.y + m_position.height ); } }
		public Vector2 Vec2Position
		{
			get { return new Vector2( m_position.x, m_position.y ); }

			set
			{
				m_position.x = value.x;
				m_position.y = value.y;
			}
		}

		public Vector3 Vec3Position
		{
			get { return new Vector3( m_position.x, m_position.y, 0f ); }

			set
			{
				m_position.x = value.x;
				m_position.y = value.y;
			}
		}


		public bool Selected
		{
			get { return m_selected; }
			set
			{
				m_infiniteLoopDetected = false;
				m_selected = value;
				OnNodeSelected( value );
			}
		}

		public List<InputPort> InputPorts { get { return m_inputPorts; } }

		public List<OutputPort> OutputPorts
		{
			get { return m_outputPorts; }
		}

		public bool IsConnected { get { return m_connStatus == NodeConnectionStatus.Connected; } }
		public NodeConnectionStatus ConnStatus
		{
			get { return m_connStatus; }
			set
			{
				if( m_selfPowered )
				{
					m_connStatus = NodeConnectionStatus.Connected;
				}
				else
				{
					m_connStatus = value;
				}

				switch( m_connStatus )
				{
					case NodeConnectionStatus.Island:
					case NodeConnectionStatus.Not_Connected: m_statusColor = Constants.NodeDefaultColor; break;
					case NodeConnectionStatus.Connected: m_statusColor = Constants.NodeConnectedColor; break;
					case NodeConnectionStatus.Error: m_statusColor = Constants.NodeErrorColor; break;
				}

			}
		}

		public bool SelfPowered
		{
			set
			{
				m_selfPowered = value;
				if( value )
				{
					ConnStatus = NodeConnectionStatus.Connected;
				}
			}
		}

		// @diogo: id of the node this one soft-references through its Reference dropdown, or -1 when not
		// referencing; wire connectivity cannot see these links, so Clean Unused Nodes keeps targets via this
		public virtual int ReferencedNodeId { get { return -1; } }

		// @diogo: polled on every repaint, so overrides must stay pure field comparisons
		public virtual bool HasMissingReference { get { return false; } }

		// The struct name an input port is tinted by when ports are coloured; a node may show a type the port does not demand.
		protected virtual string InputPortTintName( int index ) { return m_inputPorts[ index ].StructName; }

		// @diogo: multi-valued version for the References foldout; overridden by nodes with soft links a
		// single id cannot express ( sampler donor, custom expression dependencies )
		public virtual bool ReferencesNode( int nodeId ) { return ReferencedNodeId == nodeId; }

		// @diogo: soft links drawn as dashed reference wires when either endpoint is selected; defaults
		// to the Reference dropdown target, extended by nodes with extra soft links
		public virtual void GetReferenceWires( List<ReferenceWire> wires )
		{
			if ( ReferencedNodeId > -1 )
			{
				wires.Add( new ReferenceWire( ReferencedNodeId ) );
			}
		}

		// This is also called when recording on Undo
		public virtual void OnBeforeSerialize() { }
		public virtual void OnAfterDeserialize()
		{
			m_selected = false;
			m_isOnGrid = false;
			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].ResetWireReferenceStatus();
			}
			m_repopulateDictionaries = true;
			m_sizeIsDirty = true;
			m_previewIsDirty = true;
		}

		public virtual int InputIdFromDeprecated( int oldInputId ) { return oldInputId; }

		public virtual int OutputIdFromDeprecated( int oldOutputId ) { return oldOutputId; }

		public virtual void ReadFromDeprecated( ref string[] nodeParams, Type oldType = null ) { }

		public void CopyBasePropertiesFrom( ParentNode other )
		{
			m_containerGraph = other.m_containerGraph;

			UniqueId = other.UniqueId;
			m_position = other.m_position;

			if ( UIUtils.CurrentShaderVersion() > 22 )
			{
				m_customPrecision = other.m_customPrecision;
				m_currentPrecisionType = other.m_currentPrecisionType;
			}

			if ( UIUtils.CurrentShaderVersion() > 5004 )
			{
				m_showPreview = other.m_showPreview;
			}
		}

		//Inherited classes must call this base method in order to setup id and position
		public virtual void ReadFromString( ref string[] nodeParams )
		{
			ParentReadFromString( ref nodeParams );
		}

		public void ParentReadFromString( ref string[] nodeParams )
		{
			m_currentReadParamIdx = IOUtils.NodeTypeId + 1;
			m_silentReadFailure = false;

			UniqueId = Convert.ToInt32( nodeParams[ m_currentReadParamIdx++ ] );

			string[] posCoordinates = nodeParams[ m_currentReadParamIdx++ ].Split( IOUtils.VECTOR_SEPARATOR );

			m_position.x = Convert.ToSingle( posCoordinates[ 0 ] );
			m_position.y = Convert.ToSingle( posCoordinates[ 1 ] );

			if( UIUtils.CurrentShaderVersion() > 22 )
			{
				string val = GetCurrentParam( ref nodeParams );
				if( m_customPrecision )
				{
					if( val.Equals("Fixed") )
						m_currentPrecisionType = PrecisionType.Half;
					else
					m_currentPrecisionType = (PrecisionType)Enum.Parse( typeof( PrecisionType ), val );
				}
				else
				{
					m_currentPrecisionType = PrecisionType.Inherit;
				}
			}

			if( UIUtils.CurrentShaderVersion() > 5004 )
				m_showPreview = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );

		}

		//should be called after ReadFromString
		public virtual void ReadInputDataFromString( ref string[] nodeParams )
		{
			int count = 0;
			if( UIUtils.CurrentShaderVersion() > 7003 )
			{
				try
				{
					count = Convert.ToInt32( nodeParams[ m_currentReadParamIdx++ ] );
				}
				catch( Exception e )
				{
					m_silentReadFailure = true;
					Debug.LogException( e );
				}
			}
			else
			{
				count = ( m_oldInputCount < 0 ) ? m_inputPorts.Count : m_oldInputCount;
			}

			int i = 0;
			for ( ; i < count && i < nodeParams.Length && m_currentReadParamIdx < nodeParams.Length; i++ )
			{
				if( UIUtils.CurrentShaderVersion() < 5003 )
				{
					int newId = VersionConvertInputPortId( i );
					if( UIUtils.CurrentShaderVersion() > 23 )
					{
						m_inputPorts[ newId ].DataType = (WirePortDataType)Enum.Parse( typeof( WirePortDataType ), nodeParams[ m_currentReadParamIdx++ ] );
					}

					m_inputPorts[ newId ].InternalData = nodeParams[ m_currentReadParamIdx++ ];
					if( m_inputPorts[ newId ].IsEditable && UIUtils.CurrentShaderVersion() >= 3100 && m_currentReadParamIdx < nodeParams.Length )
					{
						m_inputPorts[ newId ].Name = nodeParams[ m_currentReadParamIdx++ ];
					}
					m_inputPorts[ newId ].UpdatePreviewInternalData();
				}
				else
				{
					string portIdStr = nodeParams[ m_currentReadParamIdx++ ];
					int portId = -1;
					try
					{
						portId = Convert.ToInt32( portIdStr );
					}
					catch( Exception e )
					{
						m_silentReadFailure = true;
						Debug.LogException( e );
					}

					WirePortDataType DataType = (WirePortDataType)Enum.Parse( typeof( WirePortDataType ), nodeParams[ m_currentReadParamIdx++ ] );
					string InternalData = nodeParams[ m_currentReadParamIdx++ ];
					bool isEditable = Convert.ToBoolean( nodeParams[ m_currentReadParamIdx++ ] );
					string Name = string.Empty;
					if( isEditable && m_currentReadParamIdx < nodeParams.Length )
					{
						Name = nodeParams[ m_currentReadParamIdx++ ];
					}

					InputPort inputPort = GetInputPortByUniqueId( portId );
					if( inputPort != null )
					{
						if( UIUtils.IsValidType( DataType ) )
							inputPort.DataType = DataType;

						inputPort.InternalData = InternalData;
						if( !string.IsNullOrEmpty( Name ) )
						{
							inputPort.Name = Name;
						}
						inputPort.UpdatePreviewInternalData();
					}
				}
			}

			// @diogo: stopping short of the declared count means the payload ran out mid list, which the
			// loop bounds swallow silently by leaving the cursor exactly at the end
			if ( i < count )
			{
				m_silentReadFailure = true;
			}
		}

		public virtual void ReadOutputDataFromString( ref string[] nodeParams )
		{
			int count = 0;
			if( UIUtils.CurrentShaderVersion() > 7003 )
			{
				count = Convert.ToInt32( nodeParams[ m_currentReadParamIdx++ ] );
			}
			else
			{
				count = m_outputPorts.Count;
			}

			int i = 0;
			for ( ; i < count && i < nodeParams.Length && m_currentReadParamIdx < nodeParams.Length; i++ )
			{
				try
				{
					WirePortDataType dataType = (WirePortDataType)Enum.Parse( typeof( WirePortDataType ), nodeParams[ m_currentReadParamIdx++ ] );
					int portId = -1;
					if( UIUtils.CurrentShaderVersion() > 13903 )
					{
						portId = Convert.ToInt32( nodeParams[ m_currentReadParamIdx++ ] ); ;
					}
					else
					{
						portId = i;
					}

					OutputPort port = GetOutputPortByUniqueId( portId );
					if( port != null && UIUtils.IsValidType( dataType ) )
					{
						port.DataType = dataType;
					}

				}
				catch( Exception e )
				{
					m_silentReadFailure = true;
					Debug.LogException( e );
				}
			}

			// @diogo: see ReadInputDataFromString
			if ( i < count )
			{
				m_silentReadFailure = true;
			}
		}

		public virtual void ReadAdditionalClipboardData( ref string[] nodeParams ) { }

		public virtual void ReconnectClipboardReferences( Clipboard clipboard ) { }

		protected string GetCurrentParam( ref string[] nodeParams )
		{
			if( m_currentReadParamIdx < nodeParams.Length )
			{
				return nodeParams[ m_currentReadParamIdx++ ];
			}

			m_silentReadFailure = true;
			UIUtils.ShowMessage( UniqueId, "Invalid params number in node " + m_uniqueId + " of type " + GetType(), MessageSeverity.Error );
			return string.Empty;
		}

		protected string GetCurrentParam( int index, ref string[] nodeParams )
		{
			if( m_currentReadParamIdx < nodeParams.Length )
			{
				return nodeParams[ index ];
			}

			UIUtils.ShowMessage( UniqueId, "Invalid params number in node " + m_uniqueId + " of type " + GetType(), MessageSeverity.Error );
			return string.Empty;
		}


		public virtual void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			var type = GetType();
			var typeName = $"{ type.FullName}, {type.Assembly.GetName().Name}";

			JsonGraphFormat.BeginNodeLine( ref nodeInfo, typeName, m_uniqueId, m_position.position );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_currentPrecisionType );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_showPreview );
			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].WriteToString( ref connectionsInfo );
			}
		}

		// --- Snapshot-undo view state ---
		// Editor-only state ( foldout open/closed, etc. ) that is not part of the saved graph and so
		// is lost when the snapshot-undo reload recreates the node. The window captures this by node
		// id before a reload and reapplies it after, so undoing a value edit does not also collapse
		// foldouts the user had open. Subclasses append their own fields after calling base, and read
		// them back in the same order. m_showPreview is intentionally absent ( it is already part of
		// the serialized graph, so it survives the reload on its own ).
		public virtual void WriteUndoViewState( List<string> data )
		{
			data.Add( m_propertiesFoldout ? "1" : "0" );
		}

		public virtual void ReadUndoViewState( string[] data, ref int index )
		{
			m_propertiesFoldout = ReadUndoViewBool( data, ref index, m_propertiesFoldout );
		}

		// Bounds-checked positional read so a subclass that captures more fields than another reads
		// ( or vice versa ) degrades to the current value instead of throwing during an undo.
		protected static bool ReadUndoViewBool( string[] data, ref int index, bool fallback )
		{
			return ( index < data.Length ) ? ( data[ index++ ] == "1" ) : fallback;
		}

		public virtual void WriteInputDataToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_inputPorts.Count );
			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_inputPorts[ i ].PortId );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_inputPorts[ i ].DataType );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_inputPorts[ i ].InternalData );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_inputPorts[ i ].IsEditable );
				if( m_inputPorts[ i ].IsEditable )
				{
					IOUtils.AddFieldValueToString( ref nodeInfo, m_inputPorts[ i ].Name );
				}
			}
		}

		public void WriteOutputDataToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_outputPorts.Count );
			for( int i = 0; i < m_outputPorts.Count; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_outputPorts[ i ].DataType );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_outputPorts[ i ].PortId );
			}
		}

		public virtual void WriteAdditionalClipboardData( ref string nodeInfo ) { }

		public virtual string GetIncludes() { return string.Empty; }
		public virtual void OnObjectDropped( UnityEngine.Object obj ) { }
		public virtual void SetupFromCastObject( UnityEngine.Object obj ) { }
		public virtual bool OnNodeInteraction( ParentNode node ) { return false; }
		public virtual void OnConnectedOutputNodeChanges( int portId, int otherNodeId, int otherPortId, string name, WirePortDataType type ) { }
		public virtual void OnConnectedInputNodeChanges( int portId, int otherNodeId, int otherPortId, string name, WirePortDataType type ) { }

		public Rect CachedPos { get { return m_cachedPos; } }

		public bool IsOnGrid
		{
			set { m_isOnGrid = value; }
			get { return m_isOnGrid; }
		}

		public uint CurrentReadParamIdx
		{
			get { return m_currentReadParamIdx++; }
			set { m_currentReadParamIdx = value; }
		}

		// @diogo: CurrentReadParamIdx increments on read, so inspecting where a read landed needs its own way in
		public uint ReadParamIdx { get { return m_currentReadParamIdx; } }

		public bool SilentReadFailure { get { return m_silentReadFailure; } }

		public Dictionary<string, InputPort> InputPortsDict
		{
			get
			{
				Dictionary<string, InputPort> dict = new Dictionary<string, InputPort>();
				for( int i = 0; i < m_inputPorts.Count; i++ )
				{
					dict.Add( m_inputPorts[ i ].Name, m_inputPorts[ i ] );
				}
				return dict;
			}
		}

		public bool IsDirty
		{
			set { m_isDirty = value && UIUtils.DirtyMask; }
			get
			{
				bool value = m_isDirty;
				m_isDirty = false;
				return value;
			}
		}

		public virtual void ResetNodeData()
		{
			m_category = 0;
			m_graphDepth = 0;
		}

		// The field demand this node places on one input for the demand it received on its output; null skips the
		// edge, None walks it demanding nothing (non-struct wires always carry All)
		public virtual StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			return StructFieldMask.All;
		}

		public virtual void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			StructFieldMask received = nodeData.Fields;
			if ( !dataCollector.Touch( nodeData.Category, this, received ) )
			{
				// @diogo: first time touching this node during PropagateNodeData
				UIUtils.SetCategoryInBitArray( ref m_category, nodeData.Category );
				nodeData.GraphDepth += 1;
				if( nodeData.GraphDepth > m_graphDepth )
				{
					m_graphDepth = nodeData.GraphDepth;
				}
				int count = m_inputPorts.Count;
				for( int i = 0; i < count; i++ )
				{
					if( m_inputPorts[ i ].IsConnected )
					{
						StructFieldMask demand = FieldDemandForInput( i, received );
						if ( demand == null )
						{
							continue;
						}

						nodeData.OutputPortId = m_inputPorts[ i ].GetConnection( 0 ).PortId;
						nodeData.Fields = demand;
						m_inputPorts[ i ].GetOutputNode().PropagateNodeData( nodeData, ref dataCollector );
					}
				}
			}


		}

		public void SetTitleTextOnCallback( string compareTitle, Action<ParentNode, string> callback )
		{
			if( !m_previousTitle.Equals( compareTitle ) )
			{
				m_previousTitle = compareTitle;
				m_sizeIsDirty = true;
				callback( this, compareTitle );
			}
		}

		public void SetAdditonalTitleTextOnCallback( string compareTitle, Action<ParentNode, string> callback )
		{
			if( !m_previousAdditonalTitle.Equals( compareTitle ) )
			{
				m_previousAdditonalTitle = compareTitle;
				m_sizeIsDirty = true;
				callback( this, compareTitle );
			}
		}

		public void SetAdditonalTitleTextOnCallback( string compareTitle, string compareTitleFormat, Action<ParentNode, string> callback )
		{
			if ( !m_previousAdditonalTitle.Equals( compareTitle ) || !m_previousAdditonalTitleFormat.Equals( compareTitleFormat ))
			{
				m_previousAdditonalTitle = compareTitle;
				m_previousAdditonalTitleFormat = compareTitleFormat;
				m_sizeIsDirty = true;
				callback( this, compareTitle );
			}
		}

		public virtual void SetClippedTitle( string newText, int maxSize = 170, string endString = "..." )
		{
			m_content.text = GenerateClippedTitle( newText,maxSize,endString );
			m_sizeIsDirty = true;
		}

		public virtual void SetClippedAdditionalTitle( string newText, int maxSize = 170, string endString = "..." )
		{
			m_additionalContent.text = GenerateClippedTitle( newText, maxSize, endString );
			m_sizeIsDirty = true;
		}


		public void SetTitleText( string newText )
		{
			if( !newText.Equals( m_content.text ) )
			{
				m_content.text = newText;
				m_sizeIsDirty = true;
			}
		}

		public void SetAdditonalTitleText( string newText )
		{
			if( !newText.Equals( m_additionalContent.text ) )
			{
				m_additionalContent.text = newText;
				m_sizeIsDirty = true;
			}
		}

		public string GenerateErrorValue( int outputIdx = 0 )
		{
			switch( m_outputPorts[ outputIdx ].DataType )
			{
				case WirePortDataType.FLOAT2:
				{
					return "(0).xx";
				}
				case WirePortDataType.FLOAT3:
				{
					return "(0).xxx";
				}
				case WirePortDataType.FLOAT4:
				case WirePortDataType.COLOR:
				{
					return "(0).xxxx";
				}
			}
			return "0";
		}

		private const string StructMixErrorFormat = "{0} node cannot mix a struct with other types on its inputs.";

		// Shows an error and answers true when the wired inputs from first to last carry a struct beside any
		// other type. For nodes that assign every input into one result, where nothing converts between them.
		protected bool ReportStructMix( int first, int last )
		{
			bool hasStruct = false;
			bool hasOther = false;
			for ( int i = first; i <= last && i < m_inputPorts.Count; i++ )
			{
				// @diogo: the source's type, not the port's - some nodes re-type every input to the winning
				// type, wired or not. The wildcard is neither, since it may well be carrying a struct
				OutputPort source = m_inputPorts[ i ].IsConnected ? m_inputPorts[ i ].GetOutputConnection( 0 ) : null;
				if ( source != null && source.DataType != WirePortDataType.OBJECT )
				{
					if ( source.DataType == WirePortDataType.STRUCT )
					{
						hasStruct = true;
					}
					else
					{
						hasOther = true;
					}
				}
			}

			bool mixed = hasStruct && hasOther;
			if ( mixed )
			{
				UIUtils.ShowMessage( UniqueId, string.Format( StructMixErrorFormat, m_nodeAttribs.Name ), MessageSeverity.Error );
			}
			return mixed;
		}

		//Methods created to take into account new ports added on nodes newer versions
		//This way we can convert connections from previous versions to newer ones and not brake shader graph
		public virtual int VersionConvertInputPortId( int portId ) { return portId; }
		public virtual int VersionConvertOutputPortId( int portId ) { return portId; }

		public virtual string DataToArray { get { return string.Empty; } }

		public bool SaveIsDirty
		{
			set { m_saveIsDirty = value && UIUtils.DirtyMask; }
			get
			{
				bool value = m_saveIsDirty;
				m_saveIsDirty = false;
				return value;
			}
		}

		public GUIContent TitleContent { get { return m_content; } }
		public GUIContent AdditonalTitleContent { get { return m_additionalContent; } }
		public bool IsVisible { get { return m_isVisible; } }
		public NodeAttributes Attributes { get { return m_nodeAttribs; } }
		public bool ReorderLocked { get { return m_reorderLocked; } }
		public bool RequireMaterialUpdate { get { return m_requireMaterialUpdate; } }
		public bool RMBIgnore { get { return m_rmbIgnore; } }
		public float TextLabelWidth { get { return m_textLabelWidth; } }
		public bool IsMoving { get { return m_isMoving > 0; } }
		public bool MovingInFrame { get { return m_movingInFrame; } set { m_movingInFrame = value; } }
		public bool SizeIsDirty { get { return m_sizeIsDirty; } set { m_sizeIsDirty = value; } }
		public int Category { get { return m_category; } }
		public int CommentaryParent
		{
			get { return m_commentaryParent; }
			set { m_commentaryParent = value; }
		}

		public int Depth
		{
			get { return m_depth; }
			set { m_depth = value; }
		}

		public int MatrixId
		{
			get { return m_matrixId; }
			set { m_matrixId = value; }
		}

		public float PaddingTitleRight
		{
			get { return m_paddingTitleRight; }
			set { m_paddingTitleRight += value; }
		}

		public float PaddingTitleLeft
		{
			get { return m_paddingTitleLeft; }
			set { m_paddingTitleLeft += value; }
		}

		public int CachedPortsId
		{
			get
			{
				return m_cachedPortsId;
			}
		}

		public virtual bool RecursivePreviewUpdate( Dictionary<string,bool> duplicatesDict = null )
		{
			if( duplicatesDict == null )
			{
				duplicatesDict = ContainerGraph.ParentWindow.VisitedChanged;
			}

			// @diogo: snapshot last pass's frontier flag, then reset so this pass's live consumers ( visited
			// after their producers ) can re-mark it (pooled SF previews, QA #5).
			m_feedsLiveConsumerCached = m_feedsLiveConsumer;
			m_feedsLiveConsumer = false;

			// @diogo: accumulate preview liveness from producers (pooled SF previews, QA #5).
			bool live = ContinuousPreviewRefresh;

			for( int i = 0; i < InputPorts.Count; i++ )
			{
				ParentNode outNode = null;
				if( InputPorts[ i ].ExternalReferences.Count > 0 )
				{
					outNode = ContainerGraph.GetNode( InputPorts[ i ].ExternalReferences[ 0 ].NodeId );
				}
				if( outNode != null )
				{
					if( !duplicatesDict.ContainsKey( outNode.OutputId ) )
					{
						bool result = outNode.RecursivePreviewUpdate();
						if( result )
							PreviewIsDirty = true;
					} else if( duplicatesDict[ outNode.OutputId ] )
					{
						PreviewIsDirty = true;
					}
					live = live || outNode.PreviewLive;
				}
			}

			m_previewLive = live;

			// @diogo: pooled SF previews (QA #5). Push: a live node marks its producers as frontier so they
			// keep a persistent RT ( it re-reads them every pass ). Pull: about to render, so any producer
			// whose RT is missing ( pooled static, returned after its last render ) must render first - this
			// also self-heals liveness transitions where a stale flag let a frontier RT lapse.
			if( m_previewLive || PreviewIsDirty )
			{
				for( int i = 0; i < InputPorts.Count; i++ )
				{
					if( InputPorts[ i ].ExternalReferences.Count > 0 )
					{
						ParentNode outNode = ContainerGraph.GetNode( InputPorts[ i ].ExternalReferences[ 0 ].NodeId );
						if( outNode != null )
						{
							if( m_previewLive )
							{
								outNode.MarkFeedsLiveConsumer();
							}
							if( PreviewIsDirty )
							{
								outNode.EnsurePreviewRendered();
							}
						}
					}
				}
			}

			bool needsUpdate = PreviewIsDirty;
			RenderNodePreview();

			// @diogo: the walk's own render counts for the pass stamp, so consumers pulling this node right
			// after ( EnsurePreviewRendered ) do not re-render a time-driven node once per edge.
			m_previewPassId = s_previewPassId;

			if( !duplicatesDict.ContainsKey( OutputId ) )
				duplicatesDict.Add( OutputId, needsUpdate );
			return needsUpdate;
		}

		public void MarkFeedsLiveConsumer()
		{
			m_feedsLiveConsumer = true;
		}

		// @diogo: render this node now if any of its output RTs is missing (pooled SF previews, QA #5). A
		// pooled static's RT only exists during the pass that rendered it, so a consumer about to render must
		// pull its whole RT-less producer chain first ( recursion below, producers before this node ).
		public virtual void EnsurePreviewRendered()
		{
			if( !HasPreviewShader || !m_initialized || m_outputPorts == null || m_outputPorts.Count == 0 )
			{
				return;
			}

			bool missing = false;
			for( int i = 0; i < m_outputPorts.Count; i++ )
			{
				if( !m_outputPorts[ i ].HasPreviewTexture )
				{
					missing = true;
					break;
				}
			}

			// @diogo: also re-render when dirty, not just when the RT is missing. An out-of-band reference reader
			// ( e.g. Instance Sampler -> master ) may pull a producer that sits outside every visible cone and was
			// edited since its last render, so a present-but-stale RT must still be refreshed here. Static producers
			// render before their consumers and clear their dirty flag, so this is a no-op for them; time-driven ones
			// never clear it, hence the pass stamp. A MISSING RT bypasses the stamp, so pooled statics handed back at
			// SF end still self-heal.
			if( ( !missing && ( !PreviewIsDirty || m_previewPassId == s_previewPassId ) ) || m_ensurePreviewGuard )
			{
				return;
			}

			m_ensurePreviewGuard = true;
			EnsureProducersRendered();
			PreviewIsDirty = true;
			RenderNodePreview();
			m_previewPassId = s_previewPassId;
			m_ensurePreviewGuard = false;
		}

		// @diogo: recursion step of EnsurePreviewRendered; FunctionInput overrides it to resolve its real
		// producer through the outer graph (pooled SF previews, QA #5).
		protected virtual void EnsureProducersRendered()
		{
			for( int i = 0; i < InputPorts.Count; i++ )
			{
				if( InputPorts[ i ].ExternalReferences.Count > 0 )
				{
					ParentNode outNode = ContainerGraph.GetNode( InputPorts[ i ].ExternalReferences[ 0 ].NodeId );
					if( outNode != null )
					{
						outNode.EnsurePreviewRendered();
					}
				}
			}
		}

		public virtual void RenderNodePreview()
		{
			//Runs at least one time
			if( !HasPreviewShader || !m_initialized )
			{
				// nodes with no preview don't update at all
				PreviewIsDirty = false;
				return;
			}

			if( !PreviewIsDirty && !ContinuousPreviewRefresh )
				return;

			//Debug.Log( "PREVIEW " + this );

			SetPreviewInputs();

			if( m_cachedMainTexId == -1 )
				m_cachedMainTexId = Shader.PropertyToID( "_MainTex" );

			if( m_cachedMaskTexId == -1 )
				m_cachedMaskTexId = Shader.PropertyToID( "_MaskTex" );

			if( m_cachedPortsId == -1 )
				m_cachedPortsId = Shader.PropertyToID( "_Ports" );

			if( m_cachedPortId == -1 )
				m_cachedPortId = Shader.PropertyToID( "_Port" );

			if (!Preferences.User.DisablePreviews)
			{
				PreparePooledPreview();

				// @diogo: nothing sets RenderTexture.active before a Blit below - Blit binds its own destination, so
				// that assignment is a redundant SetRenderTarget command. Only the caller's target is saved/restored.
				RenderTexture temp = RenderTexture.active;

				int count = m_outputPorts.Count;
				for( int i = 0; i < count; i++ )
				{
					if ( i == 0 )
					{
						m_portMask.Set( 0, 0, 0, 0 );

						switch( m_outputPorts[ i ].DataType )
						{
							case WirePortDataType.INT:
							case WirePortDataType.FLOAT:
							m_portMask.Set( 1, 1, 1, 1 );
							break;
							case WirePortDataType.FLOAT2:
							m_portMask.Set( 1, 1, 0, 0 );
							break;
							case WirePortDataType.FLOAT3:
							m_portMask.Set( 1, 1, 1, 0 );
							break;
							case WirePortDataType.COLOR:
							case WirePortDataType.FLOAT4:
							m_portMask.Set( 1, 1, 1, 1 );
							break;
							default:
							m_portMask.Set( 1, 1, 1, 1 );
							break;
						}

						// @diogo: an all-ones mask makes the masking pass a plain copy, so render the preview straight
						// into the port slice and skip the scratch and its blit ( halves the blits of a single-output node ).
						if( !m_outputPorts[ i ].DataTypeIsMatrix && m_portMask == Vector4.one )
						{
							if ( m_previewMaterialPassId < PreviewMaterial.passCount )
							{
								m_outputPorts[ i ].BlitPreview( PreviewMaterial, m_previewMaterialPassId );
							}
						}
						else
						{
							// @diogo: the masking pass samples the unmasked preview, which a slice can't provide, so the
							// preview renders into a 2D scratch of the slice's size first.
							ASEPreviewSlice slice = m_outputPorts[ i ].PreviewSlice;
							RenderTexture beforeMask = ASEPreviewSlices.GetScratch( slice.Size, slice.Format, ASEPreviewSlices.MaskScratchIndex );

							if ( m_previewMaterialPassId < PreviewMaterial.passCount )
							{
								Graphics.Blit( null, beforeMask, PreviewMaterial, m_previewMaterialPassId );
							}

							if( m_outputPorts[ i ].DataTypeIsMatrix )
							{
								m_outputPorts[ i ].MaskingMaterial.SetTexture( m_cachedMainTexId, EditorGUIUtility.whiteTexture );
							}
							else
							{
								m_outputPorts[ i ].MaskingMaterial.SetTexture( m_cachedMainTexId, beforeMask );
							}
							m_outputPorts[ i ].MaskingMaterial.SetVector( m_cachedPortsId, m_portMask );

							m_outputPorts[ i ].BlitPreview( m_outputPorts[ i ].MaskingMaterial, 0 );
						}
					}
					else
					{
						m_outputPorts[ i ].MaskingMaterial.SetTexture( m_cachedMaskTexId , PreviewTexture2D( ASEPreviewSlices.MaskScratchIndex ) );
						m_outputPorts[ i ].MaskingMaterial.SetFloat( m_cachedPortId , i );

						m_outputPorts[ i ].BlitPreview( m_outputPorts[ i ].MaskingMaterial, 1 );
					}
				}

				RenderTexture.active = temp;
			}

			PreviewIsDirty = ContinuousPreviewRefresh;

			FinishPreviewRender = true;
		}

#if UNITY_6000_0_OR_NEWER
		// @diogo: blits issued since the last GPU sync this pass; see PreparePooledPreview and BeginPreviewPass.
		private static int s_previewBlitCount = 0;
#endif

		// @diogo: bumped once per preview pass and stamped into m_previewPassId by every render site.
		private static int s_previewPassId = 0;

		// @diogo: reset once at the start of each preview pass so a settled graph ( fewer blits than the sync
		// interval ) never triggers a sync - it only kicks in on genuinely large bursts.
		public static void BeginPreviewPass()
		{
#if UNITY_6000_0_OR_NEWER
			s_previewBlitCount = 0;
#endif
			s_previewPassId++;
		}

		private static RenderTexture s_previewSyncRT;

		// @diogo: Unity 6 only reclaims its graphics ring buffer once the GPU has consumed the submitted work.
		// GL.Flush submits without waiting and measured no different from no flush at all, while a 1x1
		// synchronous readback is the editor-side call that does wait ( no overflow at any graph size ).
		private static void WaitForPreviewGPU()
		{
			if ( s_previewSyncRT == null )
			{
				s_previewSyncRT = new RenderTexture( 1, 1, 0, RenderTextureFormat.ARGB32 );
				s_previewSyncRT.hideFlags = HideFlags.HideAndDontSave;
				s_previewSyncRT.Create();
			}
			UnityEngine.Rendering.AsyncGPUReadback.Request( s_previewSyncRT ).WaitForCompletion();
		}

		// @diogo: pooled SF previews (QA #5). Called by every RenderNodePreview ( base and overrides ) right
		// before blitting: when the gate passes, this render's output RTs are borrowed from the transient pool
		// and handed back when the owning SF finishes computing ( ReturnInternalPooledPreviews ), so the node
		// owns no VRAM between renders. Gate policy: SF-internal regular nodes pool when LIVE ( they re-render
		// every pass anyway ) or when static WITHOUT a live consumer ( interior - they render only when dirtied,
		// and any consumer that needs them re-rendered first pulls them via EnsurePreviewRendered ). Static
		// frontier nodes ( m_feedsLiveConsumerCached ) keep a persistent RT: a live consumer re-reads them every
		// pass without re-rendering them, so pooling those would force per-pass re-renders ( measured +45 ms ).
		// Correctness: producers render before consumers in post-order and the pool holds a whole SF's RTs until
		// SF-end, so every in-pass read hits a valid RT. Reference-registry reads ( Instance samplers, Texture
		// Transform/Texel Size/Texture Array ) bypass InputPorts, but their read sites pull the referenced node
		// via EnsurePreviewRendered and mark it as feeding a live consumer when the reader is live, so property
		// nodes pool like everything else (QA #5c). The same applies to Get Local Var's registry read of its
		// RegisterLocalVarNode. The gate also honors the raw ( un-snapshotted ) mark: a reference-only node
		// that no wire cone visits never snapshots, and the raw flag is what stops a live reader from
		// re-pulling it every pass. FunctionOutput is excluded because the owning FunctionNode's swatch
		// aliases its RT across ticks; FunctionNode ports only alias FunctionOutput RTs and own nothing
		// themselves.
		protected void PreparePooledPreview()
		{
			// @diogo: wait for the GPU every N blits so a big pass ( e.g. a fresh load of a huge graph ) can't
			// overflow Unity's graphics ring buffer. Runs once per actual render ( this method is the shared
			// pre-blit hook for the base path and all overrides ); ports + 1 bounds the blits of every render path.
			// Older editors have no such ring buffer, so they skip the stall entirely.
#if UNITY_6000_0_OR_NEWER
			s_previewBlitCount += m_outputPorts.Count + 1;
			if ( s_previewBlitCount >= Constants.PreviewGpuSyncInterval )
			{
				s_previewBlitCount = 0;
				WaitForPreviewGPU();
			}
#endif

			bool pooledPreview = Preferences.User.PooledPreviews && InsideShaderFunction && ( m_previewLive || ( !m_feedsLiveConsumerCached && !m_feedsLiveConsumer ) ) && !( this is FunctionOutput ) && !( this is FunctionNode );
			if( pooledPreview )
			{
				ASEPreviewRTPool pool = ContainerGraph.ParentWindow.PreviewRTPool;
				for( int pp = 0; pp < m_outputPorts.Count; pp++ )
				{
					// @diogo: reuse an RT already borrowed this pass - a re-render ( EnsurePreviewRendered on a
					// continuous node ) would otherwise overwrite it in AssignPooledPreview and orphan it in the
					// pool, growing VRAM unbounded on time-driven SF graphs.
					if( !m_outputPorts[ pp ].HasPooledPreviewTexture )
					{
						m_outputPorts[ pp ].AssignPooledPreview( pool.Checkout( this ) );
					}
				}
			}
			else
			{
				// @diogo: rendering into owned RTs - drop any whose size no longer matches the current
				// ( zoom-scaled ) preview size so they re-allocate for this blit (QA #5d). Safe here: FunctionNode
				// never reaches this ( own RenderNodePreview ), so no aliased RT is ever touched.
				for( int pp = 0; pp < m_outputPorts.Count; pp++ )
				{
					m_outputPorts[ pp ].EnsureOwnedPreviewSize();
				}
			}
		}

		protected void ShowTab( NodeMessageType type, string tooltip )
		{
			m_showErrorMessage = true;
			m_errorMessageTypeIsError = type;
			m_errorMessageTooltip = tooltip;
		}

		protected void ShowTab()
		{
			m_showErrorMessage = true;
		}

		protected void HideTab()
		{
			m_showErrorMessage = false;
		}

		protected const string AffectedPassesFormat = " Affected passes: {0}.";

		// Passes where the last build raised a deferred warning on this node, empty when it raised none.
		public string WarningPasses { get { return m_warningPasses; } set { m_warningPasses = value; } }

		// @diogo: a node message is composed at layout time, where no pass is being generated, so the passes
		// can only come from what the last build recorded
		protected string WithWarningPasses( string message )
		{
			return string.IsNullOrEmpty( m_warningPasses ) ? message : message + string.Format( AffectedPassesFormat, m_warningPasses );
		}

		private static readonly int s_previewSliceId = Shader.PropertyToID( "_Slice" );
		private int m_previewBoundInputCount = -1;

		// The slice holding this node's preview ( port 0 unless overridden ), invalid when it has none.
		public virtual ASEPreviewSlice PreviewSlice
		{
			get
			{
				if( m_outputPorts.Count > 0 )
					return m_outputPorts[ 0 ].PreviewSlice;
				else
					return default( ASEPreviewSlice );
			}
		}

		// A 2D copy of this node's preview ( see ASEPreviewSlices.GetScratch ), or the disabled image.
		public RenderTexture PreviewTexture2D( int scratchIndex )
		{
			if( Preferences.User.DisablePreviews )
			{
				return UIUtils.PreviewDisabledRT;
			}

			ASEPreviewSlice slice = PreviewSlice;
			return slice.IsValid ? slice.To2D( scratchIndex ) : null;
		}

		// Binds this node's preview to a port property of the consumer's material ( see ASEPreviewSlice.Bind ).
		public void BindPreview( Material material, int propertyId, int scratchIndex, ParentNode consumer )
		{
			ASEPreviewSlice slice = PreviewSlice;
			if( slice.IsValid )
			{
				slice.Bind( material, propertyId, scratchIndex, consumer );
			}
		}

		// True when any of this node's output slices lives in the given array ( see ASEPreviewSlices ).
		public bool UsesPreviewArray( RenderTexture array )
		{
			for( int i = 0; i < m_outputPorts.Count; i++ )
			{
				if( m_outputPorts[ i ].HasPreviewTexture && m_outputPorts[ i ].PreviewSlice.Array == array )
				{
					return true;
				}
			}
			return false;
		}

		// Adds the arrays this node's preview reads from, so its own slices avoid them ( see ASEPreviewSlices ).
		public void CollectInputPreviewArrays( List<RenderTexture> arrays )
		{
			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				if( m_inputPorts[ i ].ExternalReferences.Count > 0 )
				{
					ParentNode producer = ContainerGraph.GetNode( m_inputPorts[ i ].ExternalReferences[ 0 ].NodeId );
					OutputPort port = producer != null ? producer.GetOutputPortByUniqueId( m_inputPorts[ i ].ExternalReferences[ 0 ].PortId ) : null;
					if( port != null && port.HasPreviewTexture && !arrays.Contains( port.PreviewSlice.Array ) )
					{
						arrays.Add( port.PreviewSlice.Array );
					}
				}
			}

			// @diogo: reference reads ( Get Local Var, Static/Toggle Switch sources, Texture Array samplers,
			// Function Input's outer port ) bind past the input ports, so the material is the complete record
			if( m_previewMaterial != null )
			{
				ASEPreviewSlices.PortProperty[] ports = ASEPreviewSlices.GetShaderPorts( m_previewMaterial.shader );
				for( int i = 0; i < ports.Length; i++ )
				{
					if( ports[ i ].IsArray && m_previewMaterial.GetTexture( ports[ i ].Id ) is RenderTexture array && !arrays.Contains( array ) )
					{
						arrays.Add( array );
					}
				}
			}
		}

		public void FullWriteToString( ref string nodesInfo, ref string connectionsInfo )
		{
			WriteToString( ref nodesInfo, ref connectionsInfo );
			WriteInputDataToString( ref nodesInfo );
			WriteOutputDataToString( ref nodesInfo );
		}

		public void ClipboardFullWriteToString( ref string nodesInfo, ref string connectionsInfo )
		{
			FullWriteToString( ref nodesInfo, ref connectionsInfo );
			WriteAdditionalClipboardData( ref nodesInfo );
		}

		public bool FullReadFromString( ref string[] parameters )
		{
			try
			{
				ReadFromString( ref parameters );
				ReadInputDataFromString( ref parameters );
				ReadOutputDataFromString( ref parameters );
			}
			catch( Exception e )
			{
				Debug.LogException( e );
				return false;
			}
			return true;
		}

		public bool ClipboardFullReadFromString( ref string[] parameters )
		{
			try
			{
				if ( !FullReadFromString( ref parameters ) )
				{
					return false;
				}
				ReadAdditionalClipboardData( ref parameters );
			}
			catch( Exception e )
			{
				Debug.LogException( e );
				return false;
			}
			return true;
		}

		public string GenerateClippedTitle( string original , int maxSize = 170, string endString = "..." )
		{
			if( UIUtils.UnZoomedNodeTitleStyle == null )
				return original;

			GUIContent content = new GUIContent( original );

			string finalTitle = string.Empty;
			bool addEllipsis = false;
			for( int i = 1; i <= original.Length; i++ )
			{
				content.text = original.Substring( 0, i );
				Vector2 titleSize = UIUtils.UnZoomedNodeTitleStyle.CalcSize( content );
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
				finalTitle += endString;

			return finalTitle;
		}

		public virtual void CalculateCustomGraphDepth() { }
		public int GraphDepth { get { return m_graphDepth; } }

		public PrecisionType CurrentPrecisionType { get { return m_currentPrecisionType == PrecisionType.Inherit ? ContainerGraph.CurrentPrecision : m_currentPrecisionType; } }


		public Material PreviewMaterial
		{
			get
			{
				if( m_previewMaterial == null )
				{
					m_previewMaterial = new Material( PreviewShader );
				}
				return m_previewMaterial;
			}
		}

		public Shader PreviewShader
		{
			get
			{
				if( m_previewShader == null )
				{
					m_previewShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( m_previewShaderGUID ) );
				}

				if( m_previewShader == null )
				{
					m_previewShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( "d9ca47581ac157145bff6f72ac5dd73e" ) ); //ranged float guid
				}

				if( m_previewShader == null )
					m_previewShader = Shader.Find( "Unlit/Colored Transparent" );

				return m_previewShader;
			}
		}

		public bool HasPreviewShader
		{
			get { return !string.IsNullOrEmpty( m_previewShaderGUID ); }
		}

		// @diogo: nodes whose preview is spatially uniform ( the frag ignores UV and returns a single value )
		// override this to true so their output ports allocate a 1x1 preview RT instead of PreviewSize² - the
		// Repeat wrap makes it read identically to a full swatch for both display and downstream consumers, at a
		// fraction of the VRAM (QA #5).
		public virtual bool ConstantPreview { get { return false; } }

		public void CheckSpherePreview()
		{
			bool oneIsSphere = false;

			if( m_drawPreviewAsSphere )
				oneIsSphere = true;
			int count = m_inputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				ParentNode node = null;
				if( m_inputPorts[ i ].ExternalReferences.Count > 0)
				{
					node = ContainerGraph.GetNode( m_inputPorts[ i ].ExternalReferences[ 0 ].NodeId );
				}
				if( node != null && node.SpherePreview )
					oneIsSphere = true;
			}

			if( m_forceDrawPreviewAsPlane )
				oneIsSphere = false;

			SpherePreview = oneIsSphere;
		}

		public bool SpherePreview
		{
			get { return m_spherePreview; }
			set { m_spherePreview = value; }
		}

		public bool ShowPreview
		{
			get { return m_showPreview; }
			set { m_showPreview = value; }
		}

		// @diogo: true when this node's preview swatch is actually shown ( per-node expanded or global preview ),
		// matching the draw-side test at DrawPreview. The preview pass roots on visible nodes only, so it renders
		// just the shown previews and their upstream cones instead of the whole graph. Virtual because picker-style
		// nodes ( Texture Sample/Object/Array ) draw their preview RT through DrawTexturePicker with m_drawPreview off.
		public virtual bool IsPreviewVisible
		{
			get { return ( m_showPreview || ( ContainerGraph != null && ContainerGraph.ParentWindow != null && ContainerGraph.ParentWindow.GlobalPreview ) ) && m_drawPreview; }
		}

		public int VisiblePorts
		{
			get { return m_visiblePorts; }
			set { m_visiblePorts = value; }
		}

		public bool Docking
		{
			get { return m_docking; }
			set { m_docking = value; }
		}

		public bool UseSquareNodeTitle
		{
			get { return m_useSquareNodeTitle; }
			set { m_useSquareNodeTitle = value; }
		}

		public bool InsideShaderFunction
		{
			get { return ContainerGraph != ContainerGraph.ParentWindow.CurrentGraph; }
		}

		public virtual void SetContainerGraph( ParentGraph newgraph )
		{
			m_containerGraph = newgraph;
		}
		public virtual void OnMasterNodeReplaced( MasterNode newMasterNode ) { PreviewIsDirty = true;  }
		public virtual void RefreshExternalReferences() { }

		public Rect DropdownRect { get { return m_dropdownRect; } }

		public virtual bool Contains( Vector2 pos ) { return m_globalPosition.Contains( pos ); }
		public virtual bool Contains( Vector3 pos ) { return m_globalPosition.Contains( pos ); }
		public bool IsNodeBeingCopied { get { return m_isNodeBeingCopied; } set { m_isNodeBeingCopied = value; } }

		public virtual WirePortDataType GetInputPortVisualDataTypeByArrayIdx( int portArrayIdx )
		{
			return m_inputPorts[ portArrayIdx ].DataType;
		}

		public virtual WirePortDataType GetOutputPortVisualDataTypeById( int portId )
		{
			return GetOutputPortByUniqueId( portId ).DataType;
		}


		public virtual bool CheckFindText( string text )
		{
			// @diogo: also match the node's unique id, so an id from an error log can be searched directly
			return TitleContent.text.IndexOf( text, StringComparison.CurrentCultureIgnoreCase ) >= 0 ||
				( m_hasSubtitle && m_additionalContent.text.IndexOf( text, StringComparison.CurrentCultureIgnoreCase ) >= 0 ) ||
				( int.TryParse( text, out int id ) && id == m_uniqueId );
		}

		// @diogo: type search takes the full node attribute name, i.e. the name shown on the palette, since titles can be renamed
		public bool CheckFindType( string text )
		{
			return m_nodeAttribs != null && string.Equals( m_nodeAttribs.Name, text, StringComparison.OrdinalIgnoreCase );
		}

		public virtual ParentNode ExecuteStubCode(){ return this; }
		public virtual float HeightEstimate
		{
			get
			{
				float heightEstimate = 0;
				heightEstimate = 32 + Constants.INPUT_PORT_DELTA_Y;
				for( int i = 0; i < InputPorts.Count; i++ )
				{
					if( InputPorts[ i ].Visible )
						heightEstimate += Constants.PORT_ROW_HEIGHT_Y;
				}

				return heightEstimate;
				// Magic number 18 represents m_fontHeight that might not be set yet
				//return Constants.NODE_HEADER_EXTRA_HEIGHT + Mathf.Max( 18 + m_inputPorts.Count, m_outputPorts.Count ) * Constants.INPUT_PORT_DELTA_Y;
			}
		}
		public bool WasDeprecated { get { return m_wasDeprecated; } set { m_wasDeprecated = value; } }
		public bool Alive { get { return m_alive;} set { m_alive = value; } }
		public bool WasDestroyed { get { return m_destroyed; } }
		public string TypeName { get { if( m_nodeAttribs != null ) return m_nodeAttribs.Name;return GetType().ToString(); } }
		public bool PreviewIsDirty { set { m_previewIsDirty = value; } get { return m_previewIsDirty; } }
		protected bool FinishPreviewRender { get { return m_finishPreviewRender; } set { m_finishPreviewRender = value; } }
		public virtual bool IsStubNode { get { return false; } }
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

//#define ADD_SHADER_FUNCTION_HEADERS

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Function Node", "Functions", "Function Node", KeyCode.None, false, 0, int.MaxValue, typeof( AmplifyShaderFunction ) )]
	public class FunctionNode : ParentNode
	{
		[SerializeField]
		private AmplifyShaderFunction m_function;

		[SerializeField]
		private ParentGraph m_functionGraph;

		[SerializeField]
		private int m_functionGraphId = -1;

		[SerializeField]
		private List<FunctionInput> m_allFunctionInputs;
		private Dictionary<int, FunctionInput> m_allFunctionInputsDict = new Dictionary<int, FunctionInput>();

		[SerializeField]
		private List<FunctionOutput> m_allFunctionOutputs;
		private Dictionary<int, FunctionOutput> m_allFunctionOutputsDict = new Dictionary<int, FunctionOutput>();

		[SerializeField]
		private List<FunctionSwitch> m_allFunctionSwitches;
		private Dictionary<int, FunctionSwitch> m_allFunctionSwitchesDict = new Dictionary<int, FunctionSwitch>();

		[SerializeField]
		private ReordenatorNode m_reordenator;

		[SerializeField]
		private string m_filename;
		public string Filename => m_filename;

		[SerializeField]
		private string m_headerTitle = string.Empty;

		[SerializeField]
		private int m_orderIndex;

		[SerializeField]
		private string m_functionCheckSum;

		[SerializeField]
		private string m_functionGUID = string.Empty;
		public string FunctionGUID => m_functionGUID;

		// @diogo: see ShaderFunctionToNode.cs for more information
		private bool m_isEligibleForConversion = false;
		public bool IsEligibleForConversion => m_isEligibleForConversion;

		//[SerializeField]
		//private List<string> m_includes = new List<string>();

		//[SerializeField]
		//private List<string> m_pragmas = new List<string>();

		[SerializeField]
		private List<AdditionalDirectiveContainer> m_directives = new List<AdditionalDirectiveContainer>();

		private bool m_parametersFoldout = true;
		[SerializeField]
		private ParentGraph m_outsideGraph = null;

		[SerializeField]
		private FunctionOutput m_mainPreviewNode;

		bool m_portsChanged = false;
		//[SerializeField]
		bool m_initialGraphDraw = false;

		private bool m_refreshIdsRequired = false;

		public string[] ReadOptionsHelper = new string[] { };

		private bool m_lateRefresh = false;

		// The field demand already sent up each input during the current pass, keyed by category and input index
		private Dictionary<long, StructFieldMask> m_walkedDemand = new Dictionary<long, StructFieldMask>();
		private MasterNodeDataCollector m_walkedOwner;
		private static readonly MasterNodePortCategory[] s_walkCategories = { MasterNodePortCategory.Vertex, MasterNodePortCategory.Fragment, MasterNodePortCategory.Tessellation, MasterNodePortCategory.Debug };

		private Texture2D m_headerIcon = null;
		public Texture2D HeaderIcon
		{
			get
			{
				if ( m_headerIcon == null )
				{
					m_headerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( "5b59dcde73b12b746af7ff7c47a1195d" ) );
				}
				return m_headerIcon;
			}
		}
		public const int HeaderIconSize = 32;

		private Dictionary<int, bool> m_duplicatesBuffer = new Dictionary<int, bool>();
		string LastLine( string text )
		{
			string[] lines = text.Replace( "\r", "" ).Split( '\n' );
			return lines[ lines.Length - 1 ];
		}

		public void CommonInit( AmplifyShaderFunction function, int uniqueId )
		{
			SetBaseUniqueId( uniqueId );

			if( function == null )
				return;

			m_refreshIdsRequired = UIUtils.IsLoading && ( UIUtils.CurrentShaderVersion() < 14004 );

			m_function = function;

			if( Function.FunctionName.Length > 1 )
			{
				SetTitleText( GraphContextMenu.AddSpacesToSentence( Function.FunctionName ) );
			}
			else
			{
				SetTitleText( Function.FunctionName );
			}
			m_tooltipText = Function.Description;
			m_hasTooltipLink = false;
			if( m_functionGraph == null )
			{
				//m_functionGraph = new ParentGraph();
				m_functionGraph = CreateInstance<ParentGraph>();
				m_functionGraph.Init();
				m_functionGraph.ParentWindow = ContainerGraph.ParentWindow;
			}

			if( string.IsNullOrEmpty( m_functionGUID ) )
			{
				m_functionGUID = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_function ) );
			}

			m_functionGraphId = Mathf.Max( m_functionGraphId, ContainerGraph.ParentWindow.GraphCount );
			ContainerGraph.ParentWindow.GraphCount = m_functionGraphId + 1;
			m_functionGraph.SetGraphId( m_functionGraphId );

			// @diogo: set before the load so the inner nodes are built already knowing the pipeline;
			// dropping a function node fires no master node event, and generation can be a while away
			m_functionGraph.CurrentSRPType = ContainerGraph.CurrentSRPType;

			ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
			ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;

			AmplifyShaderEditorWindow.LoadFromMeta( ref m_functionGraph, ContainerGraph.ParentWindow.ContextMenuInstance, Function.FunctionInfo, useParsedCache: true );
			//m_functionCheckSum = LastLine( m_function.FunctionInfo );
			m_functionCheckSum = AssetDatabase.GetAssetDependencyHash( AssetDatabase.GetAssetPath( m_function ) ).ToString();
			List<PropertyNode> propertyList = UIUtils.PropertyNodesList();
			m_allFunctionInputs = UIUtils.FunctionInputList();
			m_allFunctionOutputs = UIUtils.FunctionOutputList();
			m_allFunctionSwitches = UIUtils.FunctionSwitchList();

			ContainerGraph.ParentWindow.CustomGraph = cachedGraph;

			m_allFunctionInputs.Sort( ( x, y ) => { return x.OrderIndex.CompareTo( y.OrderIndex ); } );
			m_allFunctionOutputs.Sort( ( x, y ) => { return x.OrderIndex.CompareTo( y.OrderIndex ); } );
			m_allFunctionSwitches.Sort( ( x, y ) => { return x.OrderIndex.CompareTo( y.OrderIndex ); } );

			int inputCount = m_allFunctionInputs.Count;
			for( int i = 0; i < inputCount; i++ )
			{
				if( m_refreshIdsRequired )
				{
					AddInputPort( m_allFunctionInputs[ i ].SelectedInputType, false, m_allFunctionInputs[ i ].InputName );
				}
				else
				{
					AddInputPort( m_allFunctionInputs[ i ].SelectedInputType, false, m_allFunctionInputs[ i ].InputName, -1, MasterNodePortCategory.Fragment, m_allFunctionInputs[ i ].UniqueId );
				}
				m_inputPorts[ i ].StructName = m_allFunctionInputs[ i ].SelectedInputStructName;
				InputPortSwitchRestriction( m_inputPorts[ i ] );

				if( !m_allFunctionInputs[ i ].InputPorts[ 0 ].IsConnected )
				{
					m_inputPorts[ i ].AutoDrawInternalData = true;
					m_inputPorts[ i ].InternalData = m_allFunctionInputs[ i ].InputPorts[ 0 ].InternalData;
				}
				m_allFunctionInputs[ i ].Fnode = this;
			}

			int outputCount = m_allFunctionOutputs.Count;
			FunctionOutput first = null;
			for( int i = 0; i < outputCount; i++ )
			{
				if( i == 0 )
					first = m_allFunctionOutputs[ i ];

				if( m_allFunctionOutputs[ i ].PreviewNode )
				{
					m_mainPreviewNode = m_allFunctionOutputs[ i ];
				}

				if( m_refreshIdsRequired )
				{
					AddOutputPort( m_allFunctionOutputs[ i ].AutoOutputType, m_allFunctionOutputs[ i ].OutputName );
				}
				else
				{
					AddOutputPort( m_allFunctionOutputs[ i ].AutoOutputType, m_allFunctionOutputs[ i ].OutputName, m_allFunctionOutputs[ i ].UniqueId );
				}
				m_outputPorts[ i ].StructName = m_allFunctionOutputs[ i ].AutoOutputStructName;
				OutputPortSwitchRestriction( m_outputPorts[ i ] );
			}

			// make sure to hide the ports properly
			CheckPortVisibility();

			if( m_mainPreviewNode == null )
				m_mainPreviewNode = first;

			//create reordenator to main graph
			bool inside = false;
			if( ContainerGraph.ParentWindow.CustomGraph != null )
				inside = true;

			if( /*hasConnectedProperties*/propertyList.Count > 0 )
			{
				m_reordenator = ScriptableObject.CreateInstance<ReordenatorNode>();
				m_reordenator.Init( "_" + Function.FunctionName, Function.FunctionName, propertyList, false );
				m_reordenator.OrderIndex = m_orderIndex;
				m_reordenator.HeaderTitle = Function.FunctionName;
				m_reordenator.IsInside = inside;
			}

			if( m_reordenator != null )
			{
				cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
				ContainerGraph.ParentWindow.CustomGraph = null;
				UIUtils.RegisterPropertyNode( m_reordenator );
				ContainerGraph.ParentWindow.CustomGraph = cachedGraph;

				if( inside )
				{
					UIUtils.RegisterPropertyNode( m_reordenator );
				}
			}

			UIUtils.RegisterFunctionNode( this );

			RefreshUIHeaderColor();

			m_previewShaderGUID = "aca70c900c50c004e8ef0b47c4fac4d4";
			m_useInternalPortData = false;
			m_selectedLocation = function.PreviewPosition;
			UIUtils.CurrentWindow.OutsideGraph.OnLODMasterNodesAddedEvent += OnLODMasterNodesAddedEvent;
		}

		public InputPort GetInput( FunctionInput input )
		{
			int index = m_allFunctionInputs.FindIndex( ( x ) => { return x.Equals( input ); } );
			if( index >= 0 )
				return InputPorts[ index ];
			else
				return null;
		}

		private void OnLODMasterNodesAddedEvent( int lod )
		{
			AddShaderFunctionDirectivesInternal( lod );
		}

		public void SetPreviewInput( InputPort input )
		{
			if( !HasPreviewShader || !m_initialized || Preferences.User.DisablePreviews )
				return;

			int scratchIndex = m_inputPorts.IndexOf( input );
			if( input.IsConnected && input.InputNodeHasPreview( ContainerGraph ) )
			{
				input.SetPreviewInputTexture( ContainerGraph, scratchIndex );
			}
			else
			{
				input.SetPreviewInputValue( ContainerGraph, scratchIndex );
			}
		}

		public override bool RecursivePreviewUpdate( Dictionary<string, bool> duplicatesDict = null )
		{
			if( duplicatesDict == null )
			{
				duplicatesDict = ContainerGraph.ParentWindow.VisitedChanged;
			}

			if( m_allFunctionOutputs == null || m_allFunctionOutputs.Count == 0 )
				return false;

			// @diogo: on-demand idle-skip (pooled SF previews, QA #5). When pooling is on, skip walking a static
			// SF's internal cone entirely - it only needs a recompute when this node is already dirty, when an
			// outer input changed this pass, or when the output cone holds a live/continuous node ( m_previewLive
			// from the last walk ). The FunctionOutput RTs are persistent so the visible swatch stays valid while
			// skipped; outer producers are still visited ( memoized ) so their dirtiness is observed cheaply.
			if( Preferences.User.PooledPreviews && !PreviewIsDirty && !m_previewLive )
			{
				bool anyInputDirty = false;
				for( int i = 0; i < InputPorts.Count; i++ )
				{
					if( !InputPorts[ i ].IsConnected || InputPorts[ i ].ExternalReferences.Count == 0 )
					{
						continue;
					}

					ParentNode producer = ContainerGraph.GetNode( InputPorts[ i ].ExternalReferences[ 0 ].NodeId );
					if( producer == null )
					{
						continue;
					}

					if( !duplicatesDict.ContainsKey( producer.OutputId ) )
					{
						if( producer.RecursivePreviewUpdate() )
						{
							anyInputDirty = true;
						}
					}
					else if( duplicatesDict[ producer.OutputId ] )
					{
						anyInputDirty = true;
					}
				}

				if( !anyInputDirty )
				{
					if( !duplicatesDict.ContainsKey( OutputId ) )
					{
						duplicatesDict.Add( OutputId, false );
					}
					return false;
				}
			}

			// @diogo: accumulate preview liveness from the SF's outputs (pooled SF previews, QA #5).
			bool live = false;
			for( int i = 0; i < m_allFunctionOutputs.Count; i++ )
			{
				ParentNode outNode = m_allFunctionOutputs[ i ];
				if( outNode != null )
				{
					if( !duplicatesDict.ContainsKey( outNode.OutputId ) )
					{
						bool result = outNode.RecursivePreviewUpdate();
						if( result )
							PreviewIsDirty = true;
					}
					else if( duplicatesDict[ outNode.OutputId ] )
					{
						PreviewIsDirty = true;
					}
					live = live || outNode.PreviewLive;
				}
			}
			m_previewLive = live;

			bool needsUpdate = PreviewIsDirty;
			RenderNodePreview();

			// @diogo: pooled SF previews (QA #5). The SF's compute is complete ( all internal reads happened
			// in post-order above ), so hand this SF's transient internal preview RTs back to the pool. The
			// FunctionOutput RTs are kept ( aliased by this node's visible swatch ); nested FunctionNodes
			// already returned their own sub-graph when their RecursivePreviewUpdate ran.
			if( Preferences.User.PooledPreviews && m_functionGraph != null )
			{
				ReturnInternalPooledPreviews( m_functionGraph );
			}

			if( !duplicatesDict.ContainsKey( OutputId ) )
				duplicatesDict.Add( OutputId, needsUpdate );
			return needsUpdate;
		}

		// @diogo: hand this SF's direct regular internal preview RTs back to the pool (pooled SF previews,
		// QA #5). Skips FunctionOutput nodes (aliased by this node's visible output ports) and nested
		// FunctionNodes (each returns its own sub-graph when its RecursivePreviewUpdate ran).
		private void ReturnInternalPooledPreviews( ParentGraph graph )
		{
			ASEPreviewRTPool pool = ContainerGraph.ParentWindow.PreviewRTPool;
			List<ParentNode> nodes = graph.AllNodes;
			for( int i = 0; i < nodes.Count; i++ )
			{
				ParentNode node = nodes[ i ];
				if( node == null || node is FunctionOutput || node is FunctionNode )
				{
					continue;
				}

				List<OutputPort> outputs = node.OutputPorts;
				for( int p = 0; p < outputs.Count; p++ )
				{
					pool.Return( outputs[ p ].DetachPooledPreview() );
				}
			}
		}

		public override void RenderNodePreview()
		{
			if( m_outputPorts == null )
				return;

			if( !PreviewIsDirty && !ContinuousPreviewRefresh )
				return;

			// this is in the wrong place??
			if( m_drawPreviewAsSphere != m_mainPreviewNode.SpherePreview )
			{
				m_drawPreviewAsSphere = m_mainPreviewNode.SpherePreview;
				OnNodeChange( new NodeUpdateCache() );
			}

			int count = m_outputPorts.Count;
			for( int i = 0; i < count; i++ )
			{
				m_outputPorts[ i ].AliasPreview( m_allFunctionOutputs[ i ].PreviewSlice );
			}

			if( PreviewIsDirty )
				FinishPreviewRender = true;

			PreviewIsDirty = false;
		}

		public override ASEPreviewSlice PreviewSlice
		{
			get
			{
				if( m_mainPreviewNode != null )
					return m_mainPreviewNode.PreviewSlice;
				else
					return base.PreviewSlice;
			}
		}

		private void AddShaderFunctionDirectivesInternal( int lod )
		{
			List<TemplateMultiPassMasterNode> nodes = ContainerGraph.ParentWindow.OutsideGraph.GetMultiPassMasterNodes( lod );
			int count = nodes.Count;
			for( int i = 0; i < count; i++ )
			{
				nodes[ i ].PassModule.AdditionalDirectives.AddShaderFunctionItems( OutputId, Function.AdditionalDirectives.DirectivesList );
			}
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			if( Function == null )
				return;

			//Debug.Log( "RefreshExternalReferences " + m_function.FunctionName + " " + UIUtils.CurrentWindow.IsShaderFunctionWindow );

			Function.UpdateDirectivesList();

			MasterNode masterNode = UIUtils.CurrentWindow.OutsideGraph.CurrentMasterNode;
			StandardSurfaceOutputNode surface = masterNode as StandardSurfaceOutputNode;



			if( surface != null )
			{
				//for( int i = 0; i < Function.AdditionalIncludes.IncludeList.Count; i++ )
				//{
				//	//ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalIncludes.OutsideList.Add( Function.AdditionalIncludes.IncludeList[ i ] );
				//	ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalDirectives.AddShaderFunctionItem( AdditionalLineType.Include, Function.AdditionalIncludes.IncludeList[ i ] );
				//	m_includes.Add( Function.AdditionalIncludes.IncludeList[ i ] );
				//}

				//for( int i = 0; i < Function.AdditionalPragmas.PragmaList.Count; i++ )
				//{
				//	//ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalPragmas.OutsideList.Add( Function.AdditionalPragmas.PragmaList[ i ] );
				//	ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalDirectives.AddShaderFunctionItem(AdditionalLineType.Pragma, Function.AdditionalPragmas.PragmaList[ i ] );
				//	m_pragmas.Add( Function.AdditionalPragmas.PragmaList[ i ] );
				//}
				surface.AdditionalDirectives.AddShaderFunctionItems( OutputId, Function.AdditionalDirectives.DirectivesList );
			}
			else
			{
				if( ContainerGraph.ParentWindow.OutsideGraph.MultiPassMasterNodes.Count > 0 )
				{
					for( int lod = -1; lod < ContainerGraph.ParentWindow.OutsideGraph.LodMultiPassMasternodes.Count; lod++ )
					{
						AddShaderFunctionDirectivesInternal( lod );
					}
				}
				else
				{
					// Assuring that we're not editing a Shader Function, as directives setup is not needed there
					if( !UIUtils.CurrentWindow.IsShaderFunctionWindow )
					{
						// This function is nested inside a shader function itself and this method
						// was called before the main output node was created.
						// This is possible since all nodes RefreshExternalReferences(...) are called at the end
						// of a LoadFromMeta
						// Need to delay this setup to after all nodes are loaded to then setup the directives
						m_lateRefresh = true;
						return;
					}
				}

			}
			m_directives.AddRange( Function.AdditionalDirectives.DirectivesList );

			if( m_refreshIdsRequired )
			{
				m_refreshIdsRequired = false;
				int inputCount = m_inputPorts.Count;
				for( int i = 0; i < inputCount; i++ )
				{
					m_inputPorts[ i ].ChangePortId( m_allFunctionInputs[ i ].UniqueId );
				}

				int outputCount = m_outputPorts.Count;
				for( int i = 0; i < outputCount; i++ )
				{
					m_outputPorts[ i ].ChangePortId( m_allFunctionOutputs[ i ].UniqueId );
				}
			}

			if( ContainerGraph.ParentWindow.CurrentGraph != m_functionGraph )
				ContainerGraph.ParentWindow.CurrentGraph.InstancePropertyCount += m_functionGraph.InstancePropertyCount;

			ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
			ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;

			if( ReadOptionsHelper.Length > 2 )
			{
				for( int i = 1; i < ReadOptionsHelper.Length; i += 2 )
				{
					int optionId = Convert.ToInt32( ReadOptionsHelper[ i ] );
					int optionValue = Convert.ToInt32( ReadOptionsHelper[ i + 1 ] );
					for( int j = 0; j < m_allFunctionSwitches.Count; j++ )
					{
						if( m_allFunctionSwitches[ j ].UniqueId == optionId )
						{
							m_allFunctionSwitches[ j ].SetCurrentSelectedInput( optionValue, m_allFunctionSwitches[ j ].GetCurrentSelectedInput() );
							break;
						}
					}
				}
			}

			ContainerGraph.ParentWindow.CustomGraph = cachedGraph;

			m_portsChanged = true;
		}

		void InputPortSwitchRestriction( WirePort port )
		{
			switch( port.DataType )
			{
				// @diogo: the wildcard still carries structs and every numeric type, but a texture object
				// is bound rather than assigned, so it cannot ride through as one
				case WirePortDataType.OBJECT:
				{
					port.AcceptAllTypes();
					port.AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
						WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
				}
				break;
				case WirePortDataType.FLOAT:
				case WirePortDataType.FLOAT2:
				case WirePortDataType.FLOAT3:
				case WirePortDataType.FLOAT4:
				case WirePortDataType.COLOR:
				case WirePortDataType.INT:
				{
					port.CreatePortRestrictions( WirePortDataType.FLOAT, WirePortDataType.FLOAT2, WirePortDataType.FLOAT3, WirePortDataType.FLOAT4, WirePortDataType.COLOR, WirePortDataType.INT, WirePortDataType.OBJECT );
				}
				break;
				case WirePortDataType.FLOAT2x2:
				case WirePortDataType.FLOAT3x3:
				case WirePortDataType.FLOAT4x4:
				{
					port.CreatePortRestrictions( WirePortDataType.FLOAT2x2, WirePortDataType.FLOAT3x3, WirePortDataType.FLOAT4x4, WirePortDataType.OBJECT );
				}
				break;
				case WirePortDataType.SAMPLER1D:
				case WirePortDataType.SAMPLER2D:
				case WirePortDataType.SAMPLER3D:
				case WirePortDataType.SAMPLERCUBE:
				case WirePortDataType.SAMPLER2DARRAY:
				{
					port.CreatePortRestrictions( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.OBJECT );
				}
				break;
				case WirePortDataType.SAMPLERSTATE:
				{
					port.CreatePortRestrictions( WirePortDataType.SAMPLERSTATE );
				}
				break;
				// @diogo: a struct only ever matches a struct, the wildcard is deliberately absent
				case WirePortDataType.STRUCT:
				{
					port.CreatePortRestrictions( WirePortDataType.STRUCT );
				}
				break;
				default:
				break;
			}
		}

		void OutputPortSwitchRestriction( WirePort port )
		{
			switch( port.DataType )
			{
				// @diogo: the wildcard still carries structs and every numeric type, but a texture object
				// is bound rather than assigned, so it cannot ride through as one
				case WirePortDataType.OBJECT:
				{
					port.AcceptAllTypes();
					port.AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
						WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
				}
				break;
				case WirePortDataType.FLOAT:
				case WirePortDataType.FLOAT2:
				case WirePortDataType.FLOAT3:
				case WirePortDataType.FLOAT4:
				case WirePortDataType.COLOR:
				case WirePortDataType.INT:
				case WirePortDataType.FLOAT2x2:
				case WirePortDataType.FLOAT3x3:
				case WirePortDataType.FLOAT4x4:
				{
					port.AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY );
				}
				break;
				case WirePortDataType.SAMPLER1D:
				case WirePortDataType.SAMPLER2D:
				case WirePortDataType.SAMPLER3D:
				case WirePortDataType.SAMPLERCUBE:
				case WirePortDataType.SAMPLER2DARRAY:
				{
					port.CreatePortRestrictions( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.OBJECT );
				}
				break;
				case WirePortDataType.SAMPLERSTATE:
				{
					port.CreatePortRestrictions( WirePortDataType.SAMPLERSTATE );
				}
				break;
				// @diogo: only the receiving port decides, and the wildcard says it carries structs
				case WirePortDataType.STRUCT:
				{
					port.CreatePortRestrictions( WirePortDataType.STRUCT, WirePortDataType.OBJECT );
				}
				break;
				default:
				break;
			}
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			int outputPortId = nodeData.OutputPortId;
			StructFieldMask received = nodeData.Fields;

			// @diogo: not a gate, the outer walk below runs on every visit; the touch only tells a struct upstream
			// that this reader was walked, so a whole-struct read of it can be served from the record
			dataCollector.Touch( nodeData.Category, this, received );
			ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
			m_outsideGraph = cachedGraph;
			ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;

			for( int i = 0; i < m_allFunctionOutputs.Count; i++ )
			{
				// @diogo: only the output the consumer came through carries its demand; the others are still walked for
				// reachability, as before, demanding nothing
				bool demanded = ( outputPortId < 0 ) || ( m_outputPorts[ i ].PortId == outputPortId );
				nodeData.Fields = demanded ? received : StructFieldMask.None;
				m_allFunctionOutputs[ i ].PropagateNodeData( nodeData, ref dataCollector );
			}

			ContainerGraph.ParentWindow.CustomGraph = cachedGraph;

			// @diogo: the outer walk is not gated on Touch: the outer demand is whatever the interior recorded on each Function Input,
			// and that can grow on every visit; an input is re-walked only when its demand grew, since re-sending a covered
			// demand adds nothing upstream and multiplies the visits exponentially along chains of function nodes
			UIUtils.SetCategoryInBitArray( ref m_category, nodeData.Category );
			nodeData.GraphDepth += 1;
			if ( nodeData.GraphDepth > m_graphDepth )
			{
				m_graphDepth = nodeData.GraphDepth;
			}

			if ( m_walkedOwner != dataCollector )
			{
				m_walkedOwner = dataCollector;
				m_walkedDemand.Clear();
			}

			// @diogo: the interior may read an input on another stage than the one this node was visited in (a Vertex To Fragment
			// inside the function), and that stage's demand has to reach the outer chain
			MasterNodePortCategory visitedCategory = nodeData.Category;
			int count = m_inputPorts.Count;
			for ( int i = 0; i < count; i++ )
			{
				if ( m_inputPorts[ i ].IsConnected )
				{
					bool anyRecorded = false;
					for ( int c = 0; c < s_walkCategories.Length && !anyRecorded; c++ )
					{
						anyRecorded = ( i < m_allFunctionInputs.Count ) && m_allFunctionInputs[ i ].DemandFor( s_walkCategories[ c ], dataCollector ) != null;
					}

					for ( int c = 0; c < s_walkCategories.Length; c++ )
					{
						MasterNodePortCategory category = s_walkCategories[ c ];
						StructFieldMask demand = ( i < m_allFunctionInputs.Count ) ? m_allFunctionInputs[ i ].DemandFor( category, dataCollector ) : null;

						// @diogo: only an input the interior never reached falls back to the visited stage; walking one read on
						// another stage in the visited one tags its chain with a stage it never generates in
						if ( demand == null && ( category != visitedCategory || anyRecorded ) )
						{
							continue;
						}

						StructFieldMask fields = demand ?? StructFieldMask.All;

						long key = ( ( long )category << 32 ) | ( uint )i;
						bool seen = m_walkedDemand.TryGetValue( key, out StructFieldMask walked );
						if ( seen && walked.Covers( fields ) )
						{
							continue;
						}
						m_walkedDemand[ key ] = seen ? walked.Union( fields ) : fields;

						nodeData.Category = category;
						nodeData.OutputPortId = m_inputPorts[ i ].GetConnection( 0 ).PortId;
						nodeData.Fields = fields;
						m_inputPorts[ i ].GetOutputNode().PropagateNodeData( nodeData, ref dataCollector );
					}
				}
			}
			nodeData.Category = visitedCategory;
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			UIUtils.RegisterFunctionNode( this );
		}

		public override void SetupFromCastObject( UnityEngine.Object obj )
		{
			base.SetupFromCastObject( obj );
			AmplifyShaderFunction function = obj as AmplifyShaderFunction;
			CommonInit( function, UniqueId );
			RefreshExternalReferences();
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			FunctionInput functionInput = m_refreshIdsRequired ? m_allFunctionInputs[ portId ] : GetFunctionInputByUniqueId( portId );
			functionInput.PreviewIsDirty = true;
			if( functionInput.AutoCast )
			{
				InputPort inputPort = m_refreshIdsRequired ? m_inputPorts[ portId ] : GetInputPortByUniqueId( portId );
				inputPort.MatchPortToConnection();

				ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
				ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;
				functionInput.ChangeOutputType( inputPort.DataType, inputPort.StructName, false );
				ContainerGraph.ParentWindow.CustomGraph = cachedGraph;
			}

			for( int i = 0; i < m_allFunctionOutputs.Count; i++ )
			{
				m_outputPorts[ i ].ChangeType( m_allFunctionOutputs[ i ].InputPorts[ 0 ].DataType, m_allFunctionOutputs[ i ].InputPorts[ 0 ].StructName, false );
			}
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );

			FunctionInput functionInput = m_refreshIdsRequired ? m_allFunctionInputs[ portId ] : GetFunctionInputByUniqueId( portId );
			functionInput.PreviewIsDirty = true;

			// @diogo: an auto-cast port keeps whatever it adopted, which for a struct leaves it naming one
			// nothing is feeding it - and a port that names a struct refuses every other struct offered to
			// it. Only the name is handed back, so every other type still keeps the shape it was cast to
			if ( functionInput.AutoCast )
			{
				InputPort inputPort = m_refreshIdsRequired ? m_inputPorts[ portId ] : GetInputPortByUniqueId( portId );
				inputPort.ChangeType( inputPort.DataType, functionInput.SelectedInputStructName, false );

				ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
				ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;
				functionInput.ChangeOutputType( inputPort.DataType, functionInput.SelectedInputStructName, false );
				ContainerGraph.ParentWindow.CustomGraph = cachedGraph;
			}
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			FunctionInput functionInput = m_refreshIdsRequired ? m_allFunctionInputs[ inputPortId ] : GetFunctionInputByUniqueId( inputPortId );
			functionInput.PreviewIsDirty = true;
			if( functionInput.AutoCast )
			{
				InputPort inputPort = m_refreshIdsRequired ? m_inputPorts[ inputPortId ] : GetInputPortByUniqueId( inputPortId );
				inputPort.MatchPortToConnection();

				ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
				ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;
				functionInput.ChangeOutputType( inputPort.DataType, inputPort.StructName, false );
				ContainerGraph.ParentWindow.CustomGraph = cachedGraph;
			}

			for( int i = 0; i < m_allFunctionOutputs.Count; i++ )
			{
				m_outputPorts[ i ].ChangeType( m_allFunctionOutputs[ i ].InputPorts[ 0 ].DataType, m_allFunctionOutputs[ i ].InputPorts[ 0 ].StructName, false );
			}
		}

		// The definition behind a struct this call hands out. The graph around it has no other way to
		// reach one - a registry never crosses the boundary, and the type is declared inside the function.
		public StructDefinitionData GetBoundaryStructDefinition( string typeName )
		{
			StructDefinitionData definition = null;
			int outputCount = ( m_functionGraph != null ) ? m_outputPorts.Count : 0;

			// @diogo: only a name this call actually puts on a wire is looked up, so a struct that stays
			// inside the function is never offered to the graph around it
			for ( int i = 0; i < outputCount && definition == null; i++ )
			{
				if ( m_outputPorts[ i ].DataType == WirePortDataType.STRUCT && m_outputPorts[ i ].StructName == typeName )
				{
					definition = m_functionGraph.GetStructDefinition( typeName );
				}
			}

			return definition;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			if( Function == null )
				return;

			if( Function.Description.Length > 0 || m_allFunctionSwitches.Count > 0 )
				NodeUtils.DrawPropertyGroup( ref m_parametersFoldout, "Parameters", DrawDescription );

			bool drawInternalDataUI = false;
			int inputCount = m_inputPorts.Count;
			if( inputCount > 0 )
			{
				for( int i = 0; i < inputCount; i++ )
				{
					if( m_inputPorts[ i ].Available && m_inputPorts[ i ].ValidInternalData && !m_inputPorts[ i ].IsConnected && m_inputPorts[ i ].AutoDrawInternalData /*&& ( m_inputPorts[ i ].AutoDrawInternalData || ( m_autoDrawInternalPortData && m_useInternalPortData ) )*/  /*&& m_inputPorts[ i ].AutoDrawInternalData*/ )
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
						if( m_inputPorts[ i ].ValidInternalData && !m_inputPorts[ i ].IsConnected && m_inputPorts[ i ].Visible && m_inputPorts[ i ].AutoDrawInternalData )
						{
							EditorGUI.BeginChangeCheck();
							m_inputPorts[ i ].ShowInternalData( this );
							if( EditorGUI.EndChangeCheck() )
							{
								m_allFunctionInputs[ i ].PreviewIsDirty = true;
							}
						}
					}
				} );
		}

		private void DrawDescription()
		{
			if( Function.Description.Length > 0 )
				EditorGUILayout.HelpBox( Function.Description, MessageType.Info );

			ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
			ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;
			for( int i = 0; i < m_allFunctionSwitches.Count; i++ )
			{
				m_allFunctionSwitches[ i ].AsDrawn = false;
			}

			for( int i = 0; i < m_allFunctionSwitches.Count; i++ )
			{
				if( m_allFunctionSwitches[ i ].DrawOption( this ) )
				{
					m_portsChanged = true;
				}
			}
			ContainerGraph.ParentWindow.CustomGraph = cachedGraph;
		}

		private void RemoveShaderFunctionDirectivesInternal( int lod )
		{
			List<TemplateMultiPassMasterNode> nodes = ContainerGraph.ParentWindow.OutsideGraph.GetMultiPassMasterNodes( lod );
			int count = nodes.Count;
			for( int i = 0; i < count; i++ )
			{
				nodes[ i ].PassModule.AdditionalDirectives.RemoveShaderFunctionItems( OutputId );
			}
		}

		public override void Destroy()
		{
			m_mainPreviewNode = null;
			base.Destroy();

			m_duplicatesBuffer.Clear();
			m_duplicatesBuffer = null;

			if( m_functionGraph != null && ContainerGraph.ParentWindow.CurrentGraph != m_functionGraph )
				ContainerGraph.ParentWindow.CurrentGraph.InstancePropertyCount -= m_functionGraph.InstancePropertyCount;

			// @diogo: skip on whole-graph teardown: master nodes may already be destroyed, and their directives die with them
			if ( !ParentGraph.IsBulkDestroying )
			{
				if( ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface != null )
				{
					//for( int i = 0; i < m_includes.Count; i++ )
					//{
					//	//if( ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalIncludes.OutsideList.Contains( m_includes[ i ] ) )
					//	//{
					//	//	ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalIncludes.OutsideList.Remove( m_includes[ i ] );
					//	//}
					//	ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalDirectives.RemoveShaderFunctionItem( AdditionalLineType.Include, m_includes[ i ] );
					//}

					//for( int i = 0; i < m_pragmas.Count; i++ )
					//{
					//	//if( ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalPragmas.OutsideList.Contains( m_pragmas[ i ] ) )
					//	//{
					//	//	ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalPragmas.OutsideList.Remove( m_pragmas[ i ] );
					//	//}
					//	ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalDirectives.RemoveShaderFunctionItem( AdditionalLineType.Pragma, m_pragmas[ i ] );
					//}
					ContainerGraph.ParentWindow.OutsideGraph.CurrentStandardSurface.AdditionalDirectives.RemoveShaderFunctionItems( OutputId/*, m_directives */);
				}
				else
				{
					if( ContainerGraph.ParentWindow.OutsideGraph.MultiPassMasterNodes.Count > 0 )
					{
						for( int lod = -1; lod < ContainerGraph.ParentWindow.OutsideGraph.LodMultiPassMasternodes.Count; lod++ )
						{
							RemoveShaderFunctionDirectivesInternal( lod );
						}
					}
				}
			}




			// Cannot GameObject.Destroy(m_directives[i]) since we would be removing them from
			// the shader function asset itself

			m_directives.Clear();
			m_directives = null;

			if( m_reordenator != null )
			{
				ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
				ContainerGraph.ParentWindow.CustomGraph = null;
				UIUtils.UnregisterPropertyNode( m_reordenator );
				ContainerGraph.ParentWindow.CustomGraph = cachedGraph;

				m_reordenator.Destroy();
				m_reordenator = null;
			}

			UIUtils.UnregisterFunctionNode( this );

			ParentGraph cachedGraph2 = ContainerGraph.ParentWindow.CustomGraph;
			ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;

			if( m_allFunctionInputs != null )
				m_allFunctionInputs.Clear();
			m_allFunctionInputs = null;

			if( m_allFunctionOutputs != null )
				m_allFunctionOutputs.Clear();
			m_allFunctionOutputs = null;

			if( m_functionGraph != null )
				m_functionGraph.SoftDestroy();
			m_functionGraph = null;

			ContainerGraph.ParentWindow.CustomGraph = cachedGraph2;
			m_function = null;

			m_allFunctionOutputsDict.Clear();
			m_allFunctionOutputsDict = null;

			m_allFunctionSwitchesDict.Clear();
			m_allFunctionSwitchesDict = null;

			m_allFunctionInputsDict.Clear();
			m_allFunctionInputsDict = null;

			UIUtils.CurrentWindow.OutsideGraph.OnLODMasterNodesAddedEvent -= OnLODMasterNodesAddedEvent;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			if( m_lateRefresh )
			{
				m_lateRefresh = false;
				RefreshExternalReferences();
			}

			CheckForChangesRecursively();

			base.OnNodeLogicUpdate( drawInfo );
			ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
			ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;

			if( m_functionGraph != null )
			{
				int nodeCount = m_functionGraph.AllNodes.Count;
				for( int i = 0; i < nodeCount; i++ )
				{
					m_functionGraph.AllNodes[ i ].OnNodeLogicUpdate( drawInfo );
				}

				//if( !string.IsNullOrEmpty( FunctionGraph.CurrentFunctionOutput.SubTitle ) )
				{
					SetAdditonalTitleText( FunctionGraph.CurrentFunctionOutput.SubTitle );
				}
			}

			ContainerGraph.ParentWindow.CustomGraph = cachedGraph;

			// @diogo: the output ports take their struct name when this node is built, which is before the
			// function graph has finished resolving what it holds - an asset reference in there is looked
			// up later, and nothing announces it. So the boundary is compared back against, the same way
			// the Struct node polls the definition it built its own ports from
			int boundaryCount = ( m_allFunctionOutputs != null ) ? m_allFunctionOutputs.Count : 0;
			for ( int i = 0; i < boundaryCount && !m_portsChanged; i++ )
			{
				m_portsChanged = m_outputPorts[ i ].StructName != m_allFunctionOutputs[ i ].InputPorts[ 0 ].StructName;
			}

			if( m_portsChanged )
			{
				m_portsChanged = false;
				// @diogo: a function-switch selection can route the SF output through a branch containing a
				// continuous node, changing its liveness; force a preview re-walk so the on-demand idle-skip
				// re-evaluates m_previewLive instead of staying stale-false (pooled SF previews, QA #5).
				PreviewIsDirty = true;
				for( int i = 0; i < m_allFunctionOutputs.Count; i++ )
				{
					m_outputPorts[ i ].ChangeType( m_allFunctionOutputs[ i ].InputPorts[ 0 ].DataType, m_allFunctionOutputs[ i ].InputPorts[ 0 ].StructName, false );
				}

				CheckPortVisibility();
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			//CheckForChangesRecursively();

			var nodeChangeCache = new NodeUpdateCache();

			if( !m_initialGraphDraw && drawInfo.CurrentEventType == EventType.Repaint )
			{
				m_initialGraphDraw = true;
				ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
				ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;
				if( m_functionGraph != null )
				{
					for( int i = 0; i < m_functionGraph.AllNodes.Count; i++ )
					{
						ParentNode node = m_functionGraph.AllNodes[ i ];
						if( node != null )
						{
							node.OnNodeLayout( drawInfo, nodeChangeCache );
						}
					}
				}
				ContainerGraph.ParentWindow.CustomGraph = cachedGraph;
			}

			base.Draw( drawInfo );
		}

		public bool CheckForChanges( bool forceCheck = false, bool forceChange = false )
		{
			if( ( ContainerGraph.ParentWindow.CheckFunctions || forceCheck || forceChange ) && m_function != null )
			{
				//string newCheckSum = LastLine( m_function.FunctionInfo );
				string newCheckSum = AssetDatabase.GetAssetDependencyHash( AssetDatabase.GetAssetPath( m_function ) ).ToString();
				if( !m_functionCheckSum.Equals( newCheckSum ) || forceChange )
				{
					m_functionCheckSum = newCheckSum;
					ContainerGraph.OnDuplicateEvent += DuplicateMe;
					return true;
				}
			}
			return false;
		}

		public bool CheckForChangesRecursively()
		{
			if( m_functionGraph == null )
				return false;

			bool result = false;
			for( int i = 0; i < m_functionGraph.FunctionNodes.NodesList.Count; i++ )
			{
				if( m_functionGraph.FunctionNodes.NodesList[ i ].CheckForChangesRecursively() )
					result = true;
			}
			if( CheckForChanges( false, result ) )
				result = true;

			return result;
		}

		public void DuplicateMe()
		{
			bool previewOpen = m_showPreview;

			string allOptions = m_allFunctionSwitches.Count.ToString();
			for( int i = 0; i < m_allFunctionSwitches.Count; i++ )
			{
				allOptions += "," + m_allFunctionSwitches[ i ].UniqueId + "," + m_allFunctionSwitches[ i ].GetCurrentSelectedInput();
			}

			ReadOptionsHelper = allOptions.Split( ',' );

			ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
			ContainerGraph.ParentWindow.CustomGraph = null;
			MasterNode masterNode = ContainerGraph.ParentWindow.CurrentGraph.CurrentMasterNode;
			if( masterNode != null )
				masterNode.InvalidateMaterialPropertyCount();

			ContainerGraph.ParentWindow.CustomGraph = cachedGraph;

			ParentNode newNode = ContainerGraph.CreateNode( m_function, false, Vec2Position );
			newNode.ShowPreview = previewOpen;
			( newNode as FunctionNode ).ReadOptionsHelper = ReadOptionsHelper;
			newNode.RefreshExternalReferences();
			if( ( newNode as FunctionNode ).m_reordenator && m_reordenator )
				( newNode as FunctionNode ).m_reordenator.OrderIndex = m_reordenator.OrderIndex;

			for( int i = 0; i < m_outputPorts.Count; i++ )
			{
				if( m_outputPorts[ i ].IsConnected )
				{
					OutputPort newOutputPort = newNode.GetOutputPortByUniqueId( m_outputPorts[ i ].PortId );
					if( newNode.OutputPorts != null && newOutputPort != null )
					{
						for( int j = m_outputPorts[ i ].ExternalReferences.Count - 1; j >= 0; j-- )
						{
							ContainerGraph.CreateConnection( m_outputPorts[ i ].ExternalReferences[ j ].NodeId, m_outputPorts[ i ].ExternalReferences[ j ].PortId, newOutputPort.NodeId, newOutputPort.PortId );
						}
					}
				}
				//else
				//{
				//if( newNode.OutputPorts != null && newNode.OutputPorts[ i ] != null )
				//{
				//    ContainerGraph.DeleteConnection( false, newNode.UniqueId, newNode.OutputPorts[ i ].PortId, false, false, false );
				//}
				//}
			}

			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				InputPort newInputPort = newNode.GetInputPortByUniqueId( m_inputPorts[ i ].PortId );
				if( newNode.InputPorts != null && newInputPort != null )
				{
					// @diogo: the rebuilt node starts from the function's own defaults, so the values
					// set on this instance have to be carried over or a function edit reverts them
					newInputPort.InternalData = m_inputPorts[ i ].InternalData;

					if( m_inputPorts[ i ].IsConnected )
					{
						// @diogo: a wildcard source, e.g. Get Local Var, may only resolve once the other rebuilt functions reconnect
						OutputPort sourcePort = m_inputPorts[ i ].GetOutputConnection( 0 );
						if ( sourcePort != null && sourcePort.DataType == WirePortDataType.OBJECT && !newInputPort.CheckValidType( sourcePort ) )
						{
							ContainerGraph.DeferWire( newInputPort, sourcePort );
						}
						else
						{
							ContainerGraph.CreateConnection( newInputPort.NodeId, newInputPort.PortId, m_inputPorts[ i ].ExternalReferences[ 0 ].NodeId, m_inputPorts[ i ].ExternalReferences[ 0 ].PortId );
						}
					}
				}
			}

			ContainerGraph.OnDuplicateEvent -= DuplicateMe;

			if( Selected )
			{
				ContainerGraph.DeselectNode( this );
				ContainerGraph.SelectNode( newNode, true, false );
			}

			// @diogo: a function rebuilt earlier in this pass may hold a wire to this instance
			ContainerGraph.RetargetDeferredWires( UniqueId, newNode.UniqueId );

			ContainerGraph.DestroyNode( this, false );
		}

		private FunctionOutput GetFunctionOutputByUniqueId( int uniqueId )
		{
			int listCount = m_allFunctionOutputs.Count;
			if( m_allFunctionOutputsDict.Count != m_allFunctionOutputs.Count )
			{
				m_allFunctionOutputsDict.Clear();
				for( int i = 0; i < listCount; i++ )
				{
					m_allFunctionOutputsDict.Add( m_allFunctionOutputs[ i ].UniqueId, m_allFunctionOutputs[ i ] );
				}
			}

			if( m_allFunctionOutputsDict.ContainsKey( uniqueId ) )
				return m_allFunctionOutputsDict[ uniqueId ];

			return null;
		}

		private FunctionInput GetFunctionInputByUniqueId( int uniqueId )
		{
			int listCount = m_allFunctionInputs.Count;
			if( m_allFunctionInputsDict.Count != m_allFunctionInputs.Count )
			{
				m_allFunctionInputsDict.Clear();
				for( int i = 0; i < listCount; i++ )
				{
					m_allFunctionInputsDict.Add( m_allFunctionInputs[ i ].UniqueId, m_allFunctionInputs[ i ] );
				}
			}

			if( m_allFunctionInputsDict.ContainsKey( uniqueId ) )
				return m_allFunctionInputsDict[ uniqueId ];

			return null;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			OutputPort outputPort = GetOutputPortByUniqueId( outputId );
			FunctionOutput functionOutput = GetFunctionOutputByUniqueId( outputId );

			if( outputPort.IsLocalValue( dataCollector.PortCategory ) )
				return outputPort.LocalValue( dataCollector.PortCategory );

			m_functionGraph.CurrentPrecision = ContainerGraph.ParentWindow.CurrentGraph.CurrentPrecision;
			m_functionGraph.CurrentSRPType = ContainerGraph.ParentWindow.CurrentGraph.CurrentSRPType;
			ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
			m_outsideGraph = cachedGraph;
			ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;
#if ADD_SHADER_FUNCTION_HEADERS
			if( m_reordenator != null && m_reordenator.RecursiveCount() > 0 && m_reordenator.HasTitle )
			{
				dataCollector.AddToProperties( UniqueId, "[Header(" + m_reordenator.HeaderTitle.Replace( "-", " " ) + ")]", m_reordenator.OrderIndex );
			}
#endif
			string result = string.Empty;
			for( int i = 0; i < m_allFunctionInputs.Count; i++ )
			{
				if ( UsesOuterPort( i ) )
					m_allFunctionInputs[ i ].OnPortGeneration += FunctionNodeOnPortGeneration;
			}

			result += functionOutput.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			for( int i = 0; i < m_allFunctionInputs.Count; i++ )
			{
				if ( UsesOuterPort( i ) )
					m_allFunctionInputs[ i ].OnPortGeneration -= FunctionNodeOnPortGeneration;
			}

			ContainerGraph.ParentWindow.CustomGraph = cachedGraph;

			if( outputPort.ConnectionCount > 1 )
				RegisterLocalVariable( outputId, result, ref dataCollector );
			else
				outputPort.SetLocalValue( result, dataCollector.PortCategory );

			return outputPort.LocalValue( dataCollector.PortCategory );
		}

		// True when the input generates through this node's port instead of its own default wire; the branch
		// analysis mirrors it to know which side of the boundary an input's chain lives on
		public bool UsesOuterPort( int index )
		{
			return !m_allFunctionInputs[ index ].InputPorts[ 0 ].IsConnected || m_inputPorts[ index ].IsConnected;
		}

		public bool UsesOuterPort( FunctionInput input )
		{
			int index = m_allFunctionInputs.FindIndex( ( x ) => { return x.Equals( input ); } );
			return index >= 0 && UsesOuterPort( index );
		}

		private string FunctionNodeOnPortGeneration( ref MasterNodeDataCollector dataCollector, int index, ParentGraph graph )
		{
			ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
			ContainerGraph.ParentWindow.CustomGraph = m_outsideGraph;
			string result = m_inputPorts[ index ].GeneratePortInstructions( ref dataCollector );
			ContainerGraph.ParentWindow.CustomGraph = cachedGraph;
			return result;
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );

			if( Function != null )
				IOUtils.AddFieldValueToString( ref nodeInfo, m_function.name );
			else
				IOUtils.AddFieldValueToString( ref nodeInfo, m_filename );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_reordenator != null ? m_reordenator.RawOrderIndex : -1 );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_headerTitle );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_functionGraphId );
			IOUtils.AddFieldValueToString( ref nodeInfo, AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( m_function ) ) );

			int functionSwitchCount = m_allFunctionSwitches != null ? m_allFunctionSwitches.Count : 0;
			string allOptions = functionSwitchCount.ToString();
			for( int i = 0; i < functionSwitchCount; i++ )
			{
				allOptions += "," + m_allFunctionSwitches[ i ].UniqueId + "," + m_allFunctionSwitches[ i ].GetCurrentSelectedInput();
			}
			IOUtils.AddFieldValueToString( ref nodeInfo, allOptions );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_filename = GetCurrentParam( ref nodeParams );
			m_orderIndex = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_headerTitle = GetCurrentParam( ref nodeParams );

			if( UIUtils.CurrentShaderVersion() > 7203 )
			{
				m_functionGraphId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 13704 )
			{
				m_functionGUID = GetCurrentParam( ref nodeParams );
			}

			bool loadFailed = false;

			AmplifyShaderFunction loaded = AssetDatabase.LoadAssetAtPath<AmplifyShaderFunction>( AssetDatabase.GUIDToAssetPath( m_functionGUID ) );
			if( loaded != null )
			{
				CommonInit( loaded, UniqueId );
			}
			else
			{
				string[] guids = AssetDatabase.FindAssets( "t:AmplifyShaderFunction " + m_filename );
				if( guids.Length > 0 )
				{
					string sfGuid = null;

					foreach( string guid in guids )
					{
						string assetPath = AssetDatabase.GUIDToAssetPath( guid );
						string name = System.IO.Path.GetFileNameWithoutExtension( assetPath );
						if( name.Equals( m_filename, StringComparison.OrdinalIgnoreCase ) )
						{
							sfGuid = guid;
							break;
						}
					}
					loaded = AssetDatabase.LoadAssetAtPath<AmplifyShaderFunction>( AssetDatabase.GUIDToAssetPath( sfGuid ) );

					if( loaded != null )
					{
						CommonInit( loaded, UniqueId );
					}
					else
					{
						loadFailed = true;
					}
				}
				else
				{
					loadFailed = true;
				}
			}

			if ( loadFailed )
			{
				if ( ShaderFunctionToNode.IsEligible( m_functionGUID ) )
				{
					// @diogo:
					// Function is missing but is eligible for conversion to scripted node.
					// Check ShaderFunctionToNode.cs for more information.
					m_isEligibleForConversion = true;
				}
				else
				{
					SetTitleText( "Missing Function" );
					UIUtils.ShowMessage( UniqueId, string.Format( "Missing {0} shader function on project folder", m_filename ), MessageSeverity.Error );
				}
			}

			if( UIUtils.CurrentShaderVersion() > 14203 )
			{
				ReadOptionsHelper = GetCurrentParam( ref nodeParams ).Split( ',' );
			}
		}

		public override void ReadOutputDataFromString( ref string[] nodeParams )
		{
			if( Function == null )
				return;

			base.ReadOutputDataFromString( ref nodeParams );

			ConfigureInputportsAfterRead();
		}

		public override void OnNodeDoubleClicked( Vector2 currentMousePos2D )
		{
			if( Function == null )
				return;

			ContainerGraph.DeSelectAll();
			this.Selected = true;

			ContainerGraph.ParentWindow.OnLeftMouseUp();
			AmplifyShaderEditorWindow.LoadShaderFunctionToASE( Function, true );
			this.Selected = false;
		}

		private void ConfigureInputportsAfterRead()
		{
			if( InputPorts != null )
			{
				int inputCount = InputPorts.Count;
				for( int i = 0; i < inputCount; i++ )
				{
					InputPorts[ i ].ChangeProperties( m_allFunctionInputs[ i ].InputName, m_allFunctionInputs[ i ].SelectedInputType, m_allFunctionInputs[ i ].SelectedInputStructName, false );
				}
			}

			if( OutputPorts != null )
			{
				int outputCount = OutputPorts.Count;
				for( int i = 0; i < outputCount; i++ )
				{
					OutputPorts[ i ].ChangeProperties( m_allFunctionOutputs[ i ].OutputName, m_allFunctionOutputs[ i ].AutoOutputType, m_allFunctionOutputs[ i ].AutoOutputStructName, false );
				}
			}
		}

		private void CheckPortVisibility()
		{
			bool changes = false;
			if( InputPorts != null )
			{
				for( int i = 0; i < m_allFunctionInputs.Count; i++ )
				{
					if( m_inputPorts[ i ].Visible != m_allFunctionInputs[ i ].IsConnected )
					{
						m_inputPorts[ i ].Visible = m_allFunctionInputs[ i ].IsConnected;
						changes = true;
					}
				}
			}

			if( changes )
				m_sizeIsDirty = true;
		}

		public bool HasProperties { get { return m_reordenator != null; } }

		public ParentGraph OutsideGraph { get { return m_outsideGraph; } }

		public ParentGraph FunctionGraph
		{
			get { return m_functionGraph; }
			set { m_functionGraph = value; }
		}

		public AmplifyShaderFunction Function
		{
			get { return m_function; }
			set { m_function = value; }
		}

		public override void SetContainerGraph( ParentGraph newgraph )
		{
			base.SetContainerGraph( newgraph );
			if( m_functionGraph == null )
				return;
			for( int i = 0; i < m_functionGraph.AllNodes.Count; i++ )
			{
				m_functionGraph.AllNodes[ i ].SetContainerGraph( m_functionGraph );
			}
		}

		public override void OnMasterNodeReplaced( MasterNode newMasterNode )
		{
			base.OnMasterNodeReplaced( newMasterNode );
			if( m_functionGraph == null )
				return;

			// @diogo: the function graph has no master node of its own, so the pipeline its nodes read
			// has to come from the graph owning the new master
			m_functionGraph.CurrentSRPType = ContainerGraph.ParentWindow.OutsideGraph.CurrentSRPType;

			ParentGraph cachedGraph = ContainerGraph.ParentWindow.CustomGraph;
			ContainerGraph.ParentWindow.CustomGraph = m_functionGraph;
			m_functionGraph.FireMasterNodeReplacedEvent( newMasterNode );
			ContainerGraph.ParentWindow.CustomGraph = cachedGraph;

			StandardSurfaceOutputNode surface = newMasterNode as StandardSurfaceOutputNode;
			if( surface != null )
			{
				surface.AdditionalDirectives.AddShaderFunctionItems( OutputId, Function.AdditionalDirectives.DirectivesList );
			}
			else
			{
				if( ContainerGraph.ParentWindow.OutsideGraph.MultiPassMasterNodes.Count > 0 )
				{
					for( int lod = -1; lod < ContainerGraph.ParentWindow.OutsideGraph.LodMultiPassMasternodes.Count; lod++ )
					{
						AddShaderFunctionDirectivesInternal( lod );
					}
				}
			}
		}
	}
}

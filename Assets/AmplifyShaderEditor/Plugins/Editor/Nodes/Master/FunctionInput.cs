// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Function Input", "Functions", "Function Input adds an input port to the shader function", NodeAvailabilityFlags = (int)NodeAvailability.ShaderFunction )]
	public sealed class FunctionInput : ParentNode
	{
		private const string InputTypeStr = "Input Type";
		private const string StructTypeStr = "Struct";
		private const int StructTypeIdx = 16;

		// @diogo: appended rather than inserted - the selected index is what gets serialized, so every
		// entry after an inserted one would change meaning in already saved graphs
		private readonly string[] m_inputValueTypes ={  "Int",
														"Float",
														"Vector2",
														"Vector3",
														"Vector4",
														"Color",
														"Matrix 2x2",
														"Matrix 3x3",
														"Matrix 4x4",
														"Sampler 1D",
														"Sampler 2D",
														"Sampler 3D",
														"Sampler Cube",
														"Sampler 2D Array",
														"Sampler State",
														"Custom",
														"Struct"};

		[SerializeField]
		private int m_selectedInputTypeInt = 1;

		// @diogo: a shader function is its own graph, so a struct crossing the boundary can never be
		// looked up in the caller's registry - the asset is the contract. Only its name is needed here,
		// which is why the name is kept beside the GUID and outlives an asset that moved or was deleted
		[SerializeField]
		private string m_structGUID = string.Empty;

		[SerializeField]
		private string m_structName = string.Empty;

		private AmplifyShaderStruct m_structAsset = null;
		private bool m_structAssetResolved = false;
		private string[] m_structOptions = null;

		private WirePortDataType m_selectedInputType = WirePortDataType.FLOAT;

		[SerializeField]
		private FunctionNode m_functionNode;

		[SerializeField]
		private string m_inputName = "Input";

		[SerializeField]
		private bool m_autoCast = false;

		[SerializeField]
		private int m_orderIndex = -1;

		private Dictionary<MasterNodePortCategory, StructFieldMask> m_demand = new Dictionary<MasterNodePortCategory, StructFieldMask>();
		private MasterNodeDataCollector m_demandOwner;

		private int m_typeId = -1;

		public bool m_ignoreConnection = false;

		public delegate string PortGeneration( ref MasterNodeDataCollector dataCollector, int index, ParentGraph graph );
		public PortGeneration OnPortGeneration = null;

		//Title editing
		[SerializeField]
		private string m_uniqueName;

		private bool m_isEditing;
		private bool m_stopEditing;
		private bool m_startEditing;
		private double m_clickTime;
		private double m_doubleClickTime = 0.3;
		private Rect m_titleClickArea;
		private bool m_showTitleWhenNotEditing = true;


		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue );
			m_inputPorts[ 0 ].AutoDrawInternalData = true;
			//m_inputPorts[ 0 ].Visible = false;
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_autoWrapProperties = true;
			m_textLabelWidth = 100;
			SetTitleText( m_inputName );
			UpdatePorts();
			SetAdditonalTitleText( "( " + m_inputValueTypes[ m_selectedInputTypeInt ] + " )" );
			m_previewShaderGUID = "04bc8e7b317dccb4d8da601680dd8140";
		}

		public override void SetPreviewInputs()
		{
			if( Fnode == null )
			{
				m_ignoreConnection = false;
				CheckSpherePreview();
			}
			else
			{
				var input = Fnode.GetInput( this );
				if( input != null && ( !InputPorts[ 0 ].IsConnected || input.IsConnected ) )
				{
					m_ignoreConnection = true;
					InputPorts[ 0 ].PreparePortCacheID();
					Fnode.SetPreviewInput( input );
					if( input.ExternalReferences.Count > 0 )
					{
						SpherePreview = Fnode.ContainerGraph.GetNode( input.ExternalReferences[ 0 ].NodeId ).SpherePreview;
					}
					else
					{
						SpherePreview = false;
					}
					input.BindInputPreview( Fnode.ContainerGraph, PreviewMaterial, InputPorts[ 0 ].CachedPropertyId, ASEPreviewSlices.ReferenceScratchIndex, this );
				}
				else
				{
					m_ignoreConnection = false;
					CheckSpherePreview();
				}
			}

			if( !m_ignoreConnection )
				base.SetPreviewInputs();

			for( int i = 0; i < OutputPorts[ 0 ].ExternalReferences.Count; i++ )
			{
				ContainerGraph.GetNode( OutputPorts[ 0 ].ExternalReferences[ i ].NodeId ).OnNodeChange( new NodeUpdateCache() );
			}

			if( m_typeId == -1 )
				m_typeId = Shader.PropertyToID( "_Type" );

			if( m_inputPorts[ 0 ].DataType == WirePortDataType.FLOAT || m_inputPorts[ 0 ].DataType == WirePortDataType.INT )
				PreviewMaterial.SetInt( m_typeId, 1 );
			else if( m_inputPorts[ 0 ].DataType == WirePortDataType.FLOAT2 )
				PreviewMaterial.SetInt( m_typeId, 2 );
			else if( m_inputPorts[ 0 ].DataType == WirePortDataType.FLOAT3 )
				PreviewMaterial.SetInt( m_typeId, 3 );
			else
				PreviewMaterial.SetInt( m_typeId, 0 );

		}

		public override bool RecursivePreviewUpdate( Dictionary<string, bool> duplicatesDict = null )
		{
			if( duplicatesDict == null )
			{
				duplicatesDict = ContainerGraph.ParentWindow.VisitedChanged;
			}

			// @diogo: accumulate preview liveness from the resolved ( outer or inner ) producer so an
			// externally time-driven input marks this SF cone live (pooled SF previews, QA #5).
			bool live = ContinuousPreviewRefresh;
			for( int i = 0; i < InputPorts.Count; i++ )
			{
				ParentNode outNode = null;
				if( Fnode != null )
				{
					var input = Fnode.GetInput( this );
					if( input.ExternalReferences.Count > 0 )
					{
						outNode = Fnode.ContainerGraph.GetNode( input.ExternalReferences[ 0 ].NodeId );
					}
					else if( InputPorts[ i ].ExternalReferences.Count > 0 )
					{
						outNode = ContainerGraph.GetNode( InputPorts[ i ].ExternalReferences[ 0 ].NodeId );
					}
				}
				else
				{
					if( InputPorts[ i ].ExternalReferences.Count > 0 )
					{
						outNode = ContainerGraph.GetNode( InputPorts[ i ].ExternalReferences[ 0 ].NodeId );
					}
				}
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

			// @diogo: same frontier push / RT pull as ParentNode.RecursivePreviewUpdate, on the resolved
			// ( outer or inner ) producer - this node reads its RT out-of-band on render (pooled SF previews,
			// QA #5).
			if( m_previewLive || PreviewIsDirty )
			{
				ParentNode resolvedProducer = ResolvePreviewProducer();
				if( resolvedProducer != null )
				{
					if( m_previewLive )
					{
						resolvedProducer.MarkFeedsLiveConsumer();
					}
					if( PreviewIsDirty )
					{
						resolvedProducer.EnsurePreviewRendered();
					}
				}
			}

			bool needsUpdate = PreviewIsDirty;
			RenderNodePreview();
			if( !duplicatesDict.ContainsKey( OutputId ) )
				duplicatesDict.Add( OutputId, needsUpdate );
			return needsUpdate;
		}

		// @diogo: the producer whose preview RT this node's render actually samples - the outer FunctionNode
		// input when available, the inner connection otherwise (pooled SF previews, QA #5).
		private ParentNode ResolvePreviewProducer()
		{
			if( Fnode != null )
			{
				var input = Fnode.GetInput( this );
				if( input != null && input.ExternalReferences.Count > 0 )
				{
					return Fnode.ContainerGraph.GetNode( input.ExternalReferences[ 0 ].NodeId );
				}
			}
			if( InputPorts.Count > 0 && InputPorts[ 0 ].ExternalReferences.Count > 0 )
			{
				return ContainerGraph.GetNode( InputPorts[ 0 ].ExternalReferences[ 0 ].NodeId );
			}
			return null;
		}

		// @diogo: pull through the resolved outer producer when available - SetPreviewInputs reads its RT
		// directly, bypassing this node's inner InputPorts (pooled SF previews, QA #5).
		protected override void EnsureProducersRendered()
		{
			ParentNode outNode = ResolvePreviewProducer();
			if( outNode != null )
			{
				outNode.EnsurePreviewRendered();
			}
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			UIUtils.RegisterFunctionInputNode( this );
			if( m_nodeAttribs != null )
				m_uniqueName = m_nodeAttribs.Name + UniqueId;
		}

		public override void Destroy()
		{
			base.Destroy();
			OnPortGeneration = null;
			UIUtils.UnregisterFunctionInputNode( this );
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			if( AutoCast )
			{
				m_inputPorts[ 0 ].MatchPortToConnection();
				SetIntTypeFromPort();
				UpdatePorts();
				SetAdditonalTitleText( "( " + m_inputValueTypes[ m_selectedInputTypeInt ] + " )" );
			}
		}

		public override void OnConnectedOutputNodeChanges( int portId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( portId, otherNodeId, otherPortId, name, type );
			if( AutoCast )
			{
				m_inputPorts[ 0 ].MatchPortToConnection();
				SetIntTypeFromPort();
				UpdatePorts();
				SetAdditonalTitleText( "( " + m_inputValueTypes[ m_selectedInputTypeInt ] + " )" );
			}
		}

		public void SetIntTypeFromPort()
		{
			switch( m_inputPorts[ 0 ].DataType )
			{
				case WirePortDataType.INT: m_selectedInputTypeInt = 0; break;
				default:
				case WirePortDataType.FLOAT: m_selectedInputTypeInt = 1; break;
				case WirePortDataType.FLOAT2: m_selectedInputTypeInt = 2; break;
				case WirePortDataType.FLOAT3: m_selectedInputTypeInt = 3; break;
				case WirePortDataType.FLOAT4: m_selectedInputTypeInt = 4; break;
				case WirePortDataType.COLOR: m_selectedInputTypeInt = 5; break;
				case WirePortDataType.FLOAT2x2: m_selectedInputTypeInt = 6; break;
				case WirePortDataType.FLOAT3x3: m_selectedInputTypeInt = 7; break;
				case WirePortDataType.FLOAT4x4: m_selectedInputTypeInt = 8; break;
				case WirePortDataType.SAMPLER1D: m_selectedInputTypeInt = 9; break;
				case WirePortDataType.SAMPLER2D: m_selectedInputTypeInt = 10; break;
				case WirePortDataType.SAMPLER3D: m_selectedInputTypeInt = 11; break;
				case WirePortDataType.SAMPLERCUBE: m_selectedInputTypeInt = 12; break;
				case WirePortDataType.SAMPLER2DARRAY: m_selectedInputTypeInt = 13; break;
				case WirePortDataType.SAMPLERSTATE: m_selectedInputTypeInt = 14; break;
				case WirePortDataType.OBJECT: m_selectedInputTypeInt = 15; break;
				case WirePortDataType.STRUCT: m_selectedInputTypeInt = StructTypeIdx; break;
			}
		}

		// @diogo: only the name is needed to declare the boundary, so a GUID that no longer resolves
		// falls back to the name saved beside it rather than to a project-wide search
		private AmplifyShaderStruct StructAsset
		{
			get
			{
				// @diogo: CommonInit runs as the ScriptableObject constructor, where Unity forbids the
				// AssetDatabase outright, so nothing on the construction path may reach past this guard
				if ( string.IsNullOrEmpty( m_structGUID ) )
				{
					return null;
				}

				if ( !m_structAssetResolved )
				{
					m_structAssetResolved = true;
					m_structAsset = AssetDatabase.LoadAssetAtPath<AmplifyShaderStruct>( AssetDatabase.GUIDToAssetPath( m_structGUID ) );
				}

				return m_structAsset;
			}
		}

		// A picked asset can be renamed from anywhere, including outside the graph, so the name is
		// compared against it rather than waiting to be told - the same poll Struct Create runs.
		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );

			if ( m_selectedInputType == WirePortDataType.STRUCT )
			{
				AmplifyShaderStruct asset = StructAsset;
				// @diogo: the compliant form, since this is what the boundary port declares
				if ( asset != null && m_structName != asset.Definition.CodeName )
				{
					m_structName = asset.Definition.CodeName;
					UpdatePorts();
				}
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			// Custom Editable Title
			if( ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				if( !m_isEditing && ( ( !ContainerGraph.ParentWindow.MouseInteracted && drawInfo.CurrentEventType == EventType.MouseDown && m_titleClickArea.Contains( drawInfo.MousePosition ) ) ) )
				{
					if( ( EditorApplication.timeSinceStartup - m_clickTime ) < m_doubleClickTime )
						m_startEditing = true;
					else
						GUI.FocusControl( null );
					m_clickTime = EditorApplication.timeSinceStartup;
				}
				else if( m_isEditing && ( ( drawInfo.CurrentEventType == EventType.MouseDown && !m_titleClickArea.Contains( drawInfo.MousePosition ) ) || !EditorGUIUtility.editingTextField ) )
				{
					m_stopEditing = true;
				}

				if( m_isEditing || m_startEditing )
				{
					EditorGUI.BeginChangeCheck();
					GUI.SetNextControlName( m_uniqueName );
					m_inputName = EditorGUITextField( m_titleClickArea, string.Empty, m_inputName, UIUtils.GetCustomStyle( CustomStyle.NodeTitle ) );
					if( EditorGUI.EndChangeCheck() )
					{
						SetTitleText( m_inputName );
						UIUtils.UpdateFunctionInputData( UniqueId, m_inputName );
					}

					if( m_startEditing )
						EditorGUI.FocusTextInControl( m_uniqueName );

				}

				if( drawInfo.CurrentEventType == EventType.Repaint )
				{
					if( m_startEditing )
					{
						m_startEditing = false;
						m_isEditing = true;
					}

					if( m_stopEditing )
					{
						m_stopEditing = false;
						m_isEditing = false;
						GUI.FocusControl( null );
					}
				}


			}
		}

		public override void OnNodeLayout( DrawInfo drawInfo, NodeUpdateCache cache )
		{
			// RUN LAYOUT CHANGES AFTER TITLES CHANGE
			base.OnNodeLayout( drawInfo, cache );
			m_titleClickArea = m_titlePos;
			m_titleClickArea.height = Constants.NODE_HEADER_HEIGHT;
		}

		public override void OnNodeRepaint( DrawInfo drawInfo )
		{
			base.OnNodeRepaint( drawInfo );

			if( !m_isVisible )
				return;

			// Fixed Title ( only renders when not editing )
			if( m_showTitleWhenNotEditing && !m_isEditing && !m_startEditing && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				GUI.Label( m_titleClickArea, m_content, UIUtils.GetCustomStyle( CustomStyle.NodeTitle ) );
			}
		}

		public override void OnNodeDoubleClicked( Vector2 currentMousePos2D )
		{
			if( currentMousePos2D.y - m_globalPosition.y > ( Constants.NODE_HEADER_HEIGHT + Constants.NODE_HEADER_EXTRA_HEIGHT ) * ContainerGraph.ParentWindow.CameraDrawInfo.InvertedZoom )
			{
				ContainerGraph.ParentWindow.ParametersWindow.IsMaximized = !ContainerGraph.ParentWindow.ParametersWindow.IsMaximized;
			}
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUILayout.BeginVertical();
			EditorGUI.BeginChangeCheck();
			m_inputName = EditorGUILayoutTextField( "Name", m_inputName );
			if( EditorGUI.EndChangeCheck() )
			{
				SetTitleText( m_inputName );
				UIUtils.UpdateFunctionInputData( UniqueId, m_inputName );
			}
			EditorGUI.BeginChangeCheck();
			m_selectedInputTypeInt = EditorGUILayoutPopup( InputTypeStr, m_selectedInputTypeInt, m_inputValueTypes );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdatePorts();
				SetAdditonalTitleText( "( " + m_inputValueTypes[ m_selectedInputTypeInt ] + " )" );
			}

			if( m_selectedInputTypeInt == StructTypeIdx )
			{
				DrawStructPicker();
			}

			m_autoCast = EditorGUILayoutToggle( "Auto Cast", m_autoCast );

			EditorGUILayout.Separator();
			if( !m_inputPorts[ 0 ].IsConnected && m_inputPorts[ 0 ].ValidInternalData )
			{
				m_inputPorts[ 0 ].ShowInternalData( this, true, "Default Value" );
			}


			EditorGUILayout.EndVertical();
		}

		private void DrawStructPicker()
		{
			// @diogo: the list reads as the assets are named; the corrected form is only what the port
			// declares, and is what gets stored
			List<string> assetNames = AmplifyShaderStructCache.AssetNames;
			List<string> guids = AmplifyShaderStructCache.GUIDs;

			if( m_structOptions == null || m_structOptions.Length != assetNames.Count )
			{
				m_structOptions = assetNames.ToArray();
			}
			else
			{
				assetNames.CopyTo( m_structOptions );
			}

			int current = guids.IndexOf( m_structGUID );

			EditorGUILayout.BeginHorizontal();
			SearchableDropdown.Draw( StructTypeStr, current, m_structOptions, ( idx ) =>
			{
				if( idx >= 0 && idx < guids.Count )
				{
					UndoRecordObject( "Selected struct definition" );
					m_structGUID = guids[ idx ];
					m_structName = StructDefinitionData.CleanTypeName( assetNames[ idx ] );
					m_structAsset = null;
					m_structAssetResolved = false;
					UpdatePorts();
					SetDirtyFromAsyncEdit();
				}
			} );

			UIUtils.DrawAssetPingButton( StructAsset );
			EditorGUILayout.EndHorizontal();
		}

		void UpdatePorts()
		{
			switch( m_selectedInputTypeInt )
			{
				case 0: m_selectedInputType = WirePortDataType.INT; break;
				default:
				case 1: m_selectedInputType = WirePortDataType.FLOAT; break;
				case 2: m_selectedInputType = WirePortDataType.FLOAT2; break;
				case 3: m_selectedInputType = WirePortDataType.FLOAT3; break;
				case 4: m_selectedInputType = WirePortDataType.FLOAT4; break;
				case 5: m_selectedInputType = WirePortDataType.COLOR; break;
				case 6: m_selectedInputType = WirePortDataType.FLOAT2x2; break;
				case 7: m_selectedInputType = WirePortDataType.FLOAT3x3; break;
				case 8: m_selectedInputType = WirePortDataType.FLOAT4x4; break;
				case 9: m_selectedInputType = WirePortDataType.SAMPLER1D; break;
				case 10: m_selectedInputType = WirePortDataType.SAMPLER2D; break;
				case 11: m_selectedInputType = WirePortDataType.SAMPLER3D; break;
				case 12: m_selectedInputType = WirePortDataType.SAMPLERCUBE; break;
				case 13: m_selectedInputType = WirePortDataType.SAMPLER2DARRAY; break;
				case 14: m_selectedInputType = WirePortDataType.SAMPLERSTATE; break;
				case 15: m_selectedInputType = WirePortDataType.OBJECT; break;
				case StructTypeIdx: m_selectedInputType = WirePortDataType.STRUCT; break;
			}

			string structName = SelectedInputStructName;

			ChangeInputType( m_selectedInputType, structName, false );

			//This node doesn't have any restrictions but changing types should be restricted to prevent invalid connections
			m_outputPorts[ 0 ].ChangeTypeWithRestrictions( m_selectedInputType, structName, PortCreateRestriction( m_selectedInputType ) );

			// @diogo: the call above only prunes wires the new type invalidates, it does not leave the mask
			// on the port, so the wildcard has to state what it refuses itself
			if ( m_selectedInputType == WirePortDataType.OBJECT )
			{
				m_outputPorts[ 0 ].AcceptAllTypes();
				m_outputPorts[ 0 ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
					WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
			}
			else
			{
				m_outputPorts[ 0 ].CreatePortRestrictions();
			}

			m_sizeIsDirty = true;
		}

		public static int PortCreateRestriction( WirePortDataType dataType )
		{
			int restrictions = 0;
			WirePortDataType[] types = null;
			switch( dataType )
			{
				// @diogo: the wildcard still carries structs and every numeric type, but a texture object
				// is bound rather than assigned, so it cannot ride through as one
				case WirePortDataType.OBJECT:
				{
					restrictions = int.MaxValue - ( int )WirePortDataType.SAMPLER1D - ( int )WirePortDataType.SAMPLER2D -
						( int )WirePortDataType.SAMPLER3D - ( int )WirePortDataType.SAMPLERCUBE -
						( int )WirePortDataType.SAMPLER2DARRAY - ( int )WirePortDataType.SAMPLERSTATE;
				}
				break;
				case WirePortDataType.FLOAT:
				case WirePortDataType.FLOAT2:
				case WirePortDataType.FLOAT3:
				case WirePortDataType.FLOAT4:
				case WirePortDataType.COLOR:
				case WirePortDataType.INT:
				{
					types = new WirePortDataType[] { WirePortDataType.FLOAT, WirePortDataType.FLOAT2, WirePortDataType.FLOAT3, WirePortDataType.FLOAT4, WirePortDataType.COLOR, WirePortDataType.INT, WirePortDataType.OBJECT };
				}
				break;
				case WirePortDataType.FLOAT2x2:
				case WirePortDataType.FLOAT3x3:
				case WirePortDataType.FLOAT4x4:
				{
					types = new WirePortDataType[] { WirePortDataType.FLOAT2x2, WirePortDataType.FLOAT3x3, WirePortDataType.FLOAT4x4, WirePortDataType.OBJECT };
				}
				break;
				case WirePortDataType.SAMPLER1D:
				case WirePortDataType.SAMPLER2D:
				case WirePortDataType.SAMPLER3D:
				case WirePortDataType.SAMPLERCUBE:
				case WirePortDataType.SAMPLER2DARRAY:
				{
					types = new WirePortDataType[] { WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.OBJECT };
				}
				break;
				case WirePortDataType.SAMPLERSTATE:
				{
					types = new WirePortDataType[] { WirePortDataType.SAMPLERSTATE };
				}
				break;
				// @diogo: this only prunes the sending side, and the wildcard says it carries structs
				case WirePortDataType.STRUCT:
				{
					types = new WirePortDataType[] { WirePortDataType.STRUCT, WirePortDataType.OBJECT };
				}
				break;
				default:
				break;
			}

			if( types != null )
			{
				for( int i = 0; i < types.Length; i++ )
				{
					restrictions = restrictions | (int)types[ i ];
				}
			}

			return restrictions;
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			if ( m_demandOwner != dataCollector )
			{
				m_demandOwner = dataCollector;
				m_demand.Clear();
			}

			StructFieldMask stored;
			m_demand[ nodeData.Category ] = m_demand.TryGetValue( nodeData.Category, out stored ) ? stored.Union( nodeData.Fields ) : nodeData.Fields;
			base.PropagateNodeData( nodeData, ref dataCollector );
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			return received;
		}

		// The field demand the function's interior placed on this input during this pass, per category; null when nothing reached it
		public StructFieldMask DemandFor( MasterNodePortCategory category, MasterNodeDataCollector collector )
		{
			StructFieldMask stored;
			return ( m_demandOwner == collector && m_demand.TryGetValue( category, out stored ) ) ? stored : null;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ outputId ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );

			string result = string.Empty;
			if( OnPortGeneration != null )
				result = OnPortGeneration( ref dataCollector, m_orderIndex, ContainerGraph.ParentWindow.CustomGraph );
			else
				result = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );

			if( m_outputPorts[ outputId ].ConnectionCount > 1 )
				RegisterLocalVariable( outputId, result, ref dataCollector );
			else
				m_outputPorts[ outputId ].SetLocalValue( result, dataCollector.PortCategory );

			return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_inputName );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_selectedInputTypeInt );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_orderIndex );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_autoCast );

			// @diogo: a pick left over from the Struct type is not saved, or it would read as a dependency
			bool isStruct = m_selectedInputTypeInt == StructTypeIdx;
			IOUtils.AddFieldValueToString( ref nodeInfo, isStruct ? m_structGUID : string.Empty );
			IOUtils.AddFieldValueToString( ref nodeInfo, isStruct ? m_structName : string.Empty );
		}

		public static int UpgradeTypeIdx( int typeIdx )
		{
			// 19901 => adds float2x2
			return typeIdx + ( ( UIUtils.CurrentShaderVersion() <= 19900 && typeIdx > 5 ) ? 1 : 0 );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_inputName = GetCurrentParam( ref nodeParams );
			m_selectedInputTypeInt = UpgradeTypeIdx( Convert.ToInt32( GetCurrentParam( ref nodeParams ) ) );
			m_orderIndex = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_autoCast = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );

			if( UIUtils.CurrentShaderVersion() >= 19913 )
			{
				m_structGUID = GetCurrentParam( ref nodeParams );
				m_structName = GetCurrentParam( ref nodeParams );
				m_structAsset = null;
				m_structAssetResolved = false;
			}

			SetTitleText( m_inputName );
			UpdatePorts();
			SetAdditonalTitleText( "( " + m_inputValueTypes[ m_selectedInputTypeInt ] + " )" );
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();

			SetTitleText( m_inputName );
			UpdatePorts();
			SetAdditonalTitleText( "( " + m_inputValueTypes[ m_selectedInputTypeInt ] + " )" );
		}

		public WirePortDataType SelectedInputType
		{
			get { return m_selectedInputType; }
		}

		// The struct this input declares. Empty for every other type, and for a Struct with nothing
		// picked yet - which the caller reports as an unnamed struct rather than inventing a name.
		public string SelectedInputStructName
		{
			get
			{
				if( m_selectedInputType != WirePortDataType.STRUCT )
				{
					return string.Empty;
				}

				// @diogo: falls back to whatever was wired in, so an auto-cast struct still names itself
				// when no asset was picked to declare it
				return !string.IsNullOrEmpty( m_structName ) ? m_structName : m_inputPorts[ 0 ].StructName;
			}
		}

		// The fields behind the name above, for the graph to resolve the type by. Null for every other
		// type, and for a Struct with no asset picked, which names nothing until the caller does.
		// @diogo: a function graph has no Struct node declaring what crosses its boundary, so a picked
		// asset is the only definition of it in there - without this the name arrives with no fields
		public StructDefinitionData DeclaredStructDefinition
		{
			get
			{
				AmplifyShaderStruct asset = ( m_selectedInputType == WirePortDataType.STRUCT ) ? StructAsset : null;
				return ( asset != null ) ? asset.Definition : null;
			}
		}

		public string InputName
		{
			get { return m_inputName; }
		}

		public int OrderIndex
		{
			get { return m_orderIndex; }
			set { m_orderIndex = value; }
		}

		public bool AutoCast
		{
			get { return m_autoCast; }
			set { m_autoCast = value; }
		}

		public FunctionNode Fnode
		{
			get { return m_functionNode; }
			set { m_functionNode = value; }
		}
	}
}

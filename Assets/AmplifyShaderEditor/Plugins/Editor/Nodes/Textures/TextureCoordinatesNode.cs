// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Texture Coordinates", "UV Coordinates", "Texture UV coordinates set, if <b>Tex</b> is connected to a texture object it will use that texture scale factors, otherwise uses <b>Tilling</b> and <b>Offset</b> port values", null, KeyCode.U, tags: "uv texcoord" )]
	public sealed class TextureCoordinatesNode : ParentNode
	{

		private const string DummyPropertyDec = "[HideInInspector] _DummyTex{0}( \"\", 2D ) = \"white\"";
		private const string DummyUniformDec = "uniform sampler2D _DummyTex{0};";
		private const string DummyTexCoordDef = "uv{0}_DummyTex{0}";
		private const string DummyTexCoordSurfDef = "float2 texCoordDummy{0} = {1}.uv{2}_DummyTex{2}*{3} + {4};";
		private const string DummyTexCoordSurfVar = "texCoordDummy{0}";

		private readonly string[] Dummy = { string.Empty };

		private const string TilingStr = "Tiling";
		private const string OffsetStr = "Offset";
		private const string TexCoordStr = "texcoord_";

		[SerializeField]
		private int m_referenceArrayId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		[SerializeField]
		private int m_textureCoordChannel = 0;

		//[SerializeField]
		//private int m_texcoordId = -1;

		[SerializeField]
		private int m_texcoordSize = 2;

		[SerializeField]
		private string m_surfaceTexcoordName = string.Empty;

		[SerializeField]
		private TexturePropertyNode m_inputReferenceNode = null;

		private Vector4Node m_texCoordsHelper;

		private TexturePropertyNode m_referenceNode = null;

		// @diogo: Triplanar's Top/Mid/Bot all carry the Triplanar's own id, so only the name tells them apart
		[SerializeField]
		private string m_referenceName = string.Empty;

		// @diogo: name last rendered in the subtitle; source renames don't notify us, so UpdateTitle polls it
		private string m_previousReferenceName = null;

		private InputPort m_texPort = null;
		private InputPort m_tilingPort = null;
		private InputPort m_offsetPort = null;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.SAMPLER2D, false, "Tex", -1, MasterNodePortCategory.Fragment, 2 );
			m_texPort = m_inputPorts[ m_inputPorts.Count - 1 ];
			m_texPort.CreatePortRestrictions( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.OBJECT );

			AddInputPort( WirePortDataType.FLOAT2, false, "Tiling", -1, MasterNodePortCategory.Fragment, 0 );
			m_tilingPort = m_inputPorts[ m_inputPorts.Count - 1 ];
			m_tilingPort.Vector2InternalData = new Vector2( 1, 1 );
			AddInputPort( WirePortDataType.FLOAT2, false, "Offset", -1, MasterNodePortCategory.Fragment, 1 );
			m_offsetPort = m_inputPorts[ m_inputPorts.Count - 1 ];


			AddOutputVectorPorts( WirePortDataType.FLOAT2, "UV" );
			m_outputPorts[ 1 ].Name = "U";
			m_outputPorts[ 2 ].Name = "V";
			AddOutputPort( WirePortDataType.FLOAT, "W" );
			AddOutputPort( WirePortDataType.FLOAT, "T" );
			m_textLabelWidth = 90;
			m_useInternalPortData = true;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			m_tilingPort.Category = MasterNodePortCategory.Vertex;
			m_offsetPort.Category = MasterNodePortCategory.Vertex;
			UpdateOutput();
			m_previewShaderGUID = "085e462b2de441a42949be0e666cf5d2";
		}

		public override void Reset()
		{
			m_surfaceTexcoordName = string.Empty;
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			if( portId == 2 )
			{
				m_texPort.MatchPortToConnection();
				m_inputReferenceNode = m_texPort.GetOutputNodeWhichIsNotRelay() as TexturePropertyNode;
				UpdatePorts();
			}
			UpdateTitle();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			if( portId == 2 )
			{
				m_inputReferenceNode = null;
				UpdatePorts();
			}
			UpdateTitle();
		}

		public override void OnConnectedOutputNodeChanges( int portId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( portId, otherNodeId, otherPortId, name, type );
			if( portId == 2 )
			{
				m_texPort.MatchPortToConnection();
				UpdateTitle();
			}
		}

		void UpdateTitle()
		{
			string referenceName;
			if ( m_inputReferenceNode != null )
			{
				referenceName = m_inputReferenceNode.PropertyInspectorName;
			}
			else if ( m_referenceArrayId > -1 && m_referenceNode != null )
			{
				referenceName = m_referenceNode.PropertyInspectorName;
			}
			else
			{
				referenceName = string.Empty;
			}

			// @diogo: diff the source name, not the formatted text, so polling never allocates or resizes
			if ( !string.Equals( m_previousReferenceName, referenceName ) )
			{
				m_previousReferenceName = referenceName;
				m_additionalContent.text = ( referenceName.Length > 0 ) ? string.Format( "Value( {0} )", referenceName ) : string.Empty;
				m_sizeIsDirty = true;
			}
		}

		void UpdatePorts()
		{
			if( m_inputReferenceNode != null || m_texPort.IsConnected )
			{
				m_tilingPort.Locked = true;
				m_offsetPort.Locked = true;
			}
			else if( m_referenceArrayId > -1 )
			{
				m_tilingPort.Locked = true;
				m_offsetPort.Locked = true;
			}
			else
			{
				m_tilingPort.Locked = false;
				m_offsetPort.Locked = false;
			}
		}

		public override void DrawProperties()
		{
			bool guiEnabledBuffer = GUI.enabled;

			List<string> arr = new List<string>( UIUtils.TexturePropertyNodeArr() );
			if( arr != null && arr.Count > 0 )
			{
				arr.Insert( 0, "None" );
				GUI.enabled = true && ( !m_texPort.IsConnected );
				EditorGUILayout.BeginHorizontal();
				SearchableDropdown.Draw( Constants.AvailableReferenceStr, m_referenceArrayId + 1, UIUtils.DisambiguateReferenceLabels( arr.ToArray(), ContainerGraph.TexturePropertyNodes.NodesList, 1 ), ( idx ) =>
				{
					// @diogo: idx counts the leading "None" entry, so the reference id is idx - 1
					int newReferenceArrayId = idx - 1;
					if( newReferenceArrayId != m_referenceArrayId )
					{
						UndoRecordObject( "Selected texture reference" );
						m_referenceArrayId = newReferenceArrayId;
						m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceArrayId );
						if( m_referenceNode != null )
						{
							m_referenceNodeId = m_referenceNode.UniqueId;
							m_referenceName = m_referenceNode.PropertyName;
						}
						else
						{
							m_referenceNodeId = -1;
							m_referenceArrayId = -1;
							m_referenceName = string.Empty;
						}

						UpdateTitle();
						UpdatePorts();
						SetDirtyFromAsyncEdit();
					}
				} );
				UIUtils.DrawReferenceFocusButton( m_referenceNode );
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				m_referenceArrayId = -1;
				GUI.enabled = false;
				SearchableDropdown.Draw( Constants.AvailableReferenceStr, m_referenceArrayId, Dummy, null );
			}

			GUI.enabled = guiEnabledBuffer;

			EditorGUI.BeginChangeCheck();
			m_texcoordSize = EditorGUILayoutIntPopup( Constants.AvailableUVSizesLabel, m_texcoordSize, Constants.AvailableUVSizesStr, Constants.AvailableUVSizes );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateOutput();
			}

			m_textureCoordChannel = EditorGUILayoutIntPopup( Constants.AvailableUVSetsLabel, m_textureCoordChannel, Constants.AvailableUVSetsStr, Constants.AvailableUVSets );


			if( m_referenceArrayId > -1 )
				GUI.enabled = false;

			base.DrawProperties();

			GUI.enabled = guiEnabledBuffer;
		}

		private void UpdateOutput()
		{
			if( m_texcoordSize == 3 )
			{
				m_outputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT3, false );
				m_outputPorts[ 0 ].Name = "UVW";
				m_outputPorts[ 3 ].Visible = true;
				m_outputPorts[ 4 ].Visible = false;
			}
			else if( m_texcoordSize == 4 )
			{
				m_outputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT4, false );
				m_outputPorts[ 0 ].Name = "UVWT";
				m_outputPorts[ 3 ].Visible = true;
				m_outputPorts[ 4 ].Visible = true;
			}
			else
			{
				m_outputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT2, false );
				m_outputPorts[ 0 ].Name = "UV";
				m_outputPorts[ 3 ].Visible = false;
				m_outputPorts[ 4 ].Visible = false;
			}
			m_sizeIsDirty = true;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			CheckReference();
			UpdateTitle();
		}

		//public override void Draw( DrawInfo drawInfo )
		//{
		//	base.Draw( drawInfo );
		//	//CheckReference();
		//}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			if( m_dropdownEditing )
			{
				EditorGUI.BeginChangeCheck();
				m_texcoordSize = EditorGUIIntPopup( m_dropdownRect, m_texcoordSize, Constants.AvailableUVSizesStr, Constants.AvailableUVSizes, UIUtils.PropertyPopUp );
				if( EditorGUI.EndChangeCheck() )
				{
					UpdateOutput();
					DropdownEditing = false;
				}
			}
		}

		// @diogo: the sorted register shifts indices on rename/add/remove; re-anchor the index to the property
		void CheckReference()
		{
			if ( m_referenceNodeId > -1 )
			{
				if ( m_referenceNode != null )
				{
					// @diogo: the live pointer wins, so mirror its name before any index driven re-resolve
					m_referenceName = m_referenceNode.PropertyName;
				}

				if ( m_referenceNode == null || UIUtils.GetTexturePropertyNode( m_referenceArrayId ) != m_referenceNode )
				{
					// @diogo: an id that outlived its target later resolves to whatever node inherits it;
					// Triplanar is an exception, it owns off-graph texture properties that carry its own id
					ParentNode owner = ContainerGraph.GetNode( m_referenceNodeId );
					if ( owner is TexturePropertyNode || owner is TriplanarNode )
					{
						m_referenceNode = UIUtils.GetTexturePropertyNodeByReference( m_referenceNodeId, m_referenceName );
						m_referenceArrayId = UIUtils.GetTexturePropertyNodeRegisterId( m_referenceNode );
					}
					else
					{
						m_referenceNodeId = -1;
						m_referenceArrayId = -1;
						m_referenceNode = null;
						m_referenceName = string.Empty;
						UpdateTitle();
					}
					UpdatePorts();
				}
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_textureCoordChannel = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 2402 )
			{
				if( UIUtils.CurrentShaderVersion() > 2404 )
				{
					m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}
				else
				{
					m_referenceArrayId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}
			}

			if( UIUtils.CurrentShaderVersion() > 5001 )
			{
				m_texcoordSize = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				UpdateOutput();
			}

			if ( UIUtils.CurrentShaderVersion() >= 19913 )
			{
				m_referenceName = GetCurrentParam( ref nodeParams );
			}
		}


		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			if( UIUtils.CurrentShaderVersion() > 2402 )
			{
				if( UIUtils.CurrentShaderVersion() > 2404 )
				{
					m_referenceNode = UIUtils.GetTexturePropertyNodeByReference( m_referenceNodeId, m_referenceName );
					if( m_referenceNodeId > -1 )
						m_referenceArrayId = UIUtils.GetTexturePropertyNodeRegisterId( m_referenceNode );
				}
				else
				{
					m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceArrayId );
					if( m_referenceNode != null )
					{
						m_referenceNodeId = m_referenceNode.UniqueId;
					}
				}
				UpdateTitle();
				UpdatePorts();
			}
		}
		public override int ReferencedNodeId { get { return m_referenceNodeId > -1 ? m_referenceNodeId : base.ReferencedNodeId; } }

		public override void ReconnectClipboardReferences( Clipboard clipboard )
		{
			// validate node first
			int newId = clipboard.GeNewNodeId( m_referenceNodeId );
			if ( ContainerGraph.GetNode( newId ) != null )
			{
				m_referenceNodeId = newId;
			}
			RefreshExternalReferences();
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			if( dataCollector.IsTemplate )
			{
				dataCollector.TemplateDataCollectorInstance.SetUVUsage( m_textureCoordChannel, m_texcoordSize, nodeData.Category );
			}
			else
			{
				dataCollector.SetTextureChannelSize( m_textureCoordChannel , m_outputPorts[0].DataType );

				if( m_textureCoordChannel > 3 )
				{
					dataCollector.AddCustomAppData( string.Format( TemplateHelperFunctions.TexUVFullSemantic , m_textureCoordChannel ) );
				}
			}
			UIUtils.SetCategoryInBitArray( ref m_category, nodeData.Category );

			MasterNodePortCategory propagateCategory = ( nodeData.Category != MasterNodePortCategory.Vertex && nodeData.Category != MasterNodePortCategory.Tessellation ) ? MasterNodePortCategory.Vertex : nodeData.Category;
			nodeData.Category = propagateCategory;
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
					//m_inputPorts[ i ].GetOutputNode().PropagateNodeCategory( category );
					nodeData.OutputPortId = m_inputPorts[ i ].GetConnection( 0 ).PortId;
					nodeData.Fields = StructFieldMask.All;
					m_inputPorts[ i ].GetOutputNode().PropagateNodeData( nodeData, ref dataCollector );
				}
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_textureCoordChannel );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceNodeId );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_texcoordSize );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceName );
		}

		string GetValidPropertyName()
		{
			string propertyName = string.Empty;
			if( m_inputReferenceNode != null )
			{
				propertyName = m_inputReferenceNode.PropertyName;
			}
			else if( m_referenceArrayId > -1 )
			{
				m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceArrayId );
				if( m_referenceNode != null )
				{
					propertyName = m_referenceNode.PropertyName;
				}
			}

			return propertyName;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			if( dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowMessage( UniqueId, m_nodeAttribs.Name + " cannot be used on Master Node Tessellation port" );
				return "-1";
			}

			//bool isVertex = ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation );

			string tiling = string.Empty;
			string offset = string.Empty;

			TexturePropertyNode textureProperty = null;
			string portProperty = string.Empty;
			if( m_texPort.IsConnected )
			{
				textureProperty = m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode;
				portProperty = m_texPort.GeneratePortInstructions( ref dataCollector );
			}
			else if( m_referenceArrayId > -1 )
			{
				textureProperty = UIUtils.GetTexturePropertyNode( m_referenceArrayId );
				if( textureProperty != null )
				{
					portProperty = textureProperty.BaseGenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalVar );
				}
			}

			//TEMPLATES
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
					return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );

				string uvName = string.Empty;
				string result = string.Empty;
				string indexStr = m_textureCoordChannel > 0 ? ( m_textureCoordChannel + 1 ).ToString() : "";
				string sizeDif = string.Empty;
				if( m_texcoordSize == 3 )
					sizeDif = "3";
				else if( m_texcoordSize == 4 )
					sizeDif = "4";

				if( dataCollector.TemplateDataCollectorInstance.GetCustomInterpolatedData( TemplateHelperFunctions.IntToUVChannelInfo[ m_textureCoordChannel ], m_outputPorts[ 0 ].DataType, PrecisionType.Float, ref result, false, dataCollector.PortCategory ) )
				{
					uvName = result;
				}
				else if( dataCollector.TemplateDataCollectorInstance.HasUV( m_textureCoordChannel ) )
				{
					uvName = dataCollector.TemplateDataCollectorInstance.GetUVName( m_textureCoordChannel, m_outputPorts[ 0 ].DataType );
				}
				else
				{
					uvName = dataCollector.TemplateDataCollectorInstance.RegisterUV( m_textureCoordChannel, m_outputPorts[ 0 ].DataType );
				}
				string currPropertyName = GetValidPropertyName();
				if( !string.IsNullOrEmpty( portProperty ) && portProperty != "0.0" )
				{
					currPropertyName = portProperty;
				}
				if( !string.IsNullOrEmpty( currPropertyName ) )
				{
					string finalTexCoordName = "uv" + indexStr + ( m_texcoordSize > 2 ? "s" + sizeDif : "" ) + currPropertyName;
					string dummyPropertyTexcoords = currPropertyName + "_ST";

					if( m_texCoordsHelper == null )
					{
						m_texCoordsHelper = CreateInstance<Vector4Node>();
						m_texCoordsHelper.ContainerGraph = ContainerGraph;
						m_texCoordsHelper.SetBaseUniqueId( UniqueId, true );
						m_texCoordsHelper.RegisterPropertyOnInstancing = false;
					}

					m_texCoordsHelper.AddGlobalToSRPBatcher = ( textureProperty != null ) ? ( textureProperty.CurrentParameterType != PropertyType.Global ) : true;
					m_texCoordsHelper.CurrentParameterType = PropertyType.Global;
					m_texCoordsHelper.SetRawVariableMode( ( textureProperty != null ) ? textureProperty.CurrentVariableMode : VariableMode.Create );

					m_texCoordsHelper.ResetOutputLocals();
					m_texCoordsHelper.SetRawPropertyName( dummyPropertyTexcoords );
					dummyPropertyTexcoords = m_texCoordsHelper.GenerateShaderForOutput( 0, ref dataCollector, false );

					if( m_texcoordSize > 2 )
					{
						dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, m_outputPorts[ 0 ].DataType, finalTexCoordName, uvName );
						dataCollector.AddLocalVariable( UniqueId, finalTexCoordName + ".xy", string.Format( Constants.TilingOffsetFormat, uvName + ".xy", dummyPropertyTexcoords + ".xy", dummyPropertyTexcoords + ".zw" ) + ";" );
						m_outputPorts[ 0 ].SetLocalValue( finalTexCoordName, dataCollector.PortCategory );
					}
					else
					{
						RegisterLocalVariable( 0, string.Format( Constants.TilingOffsetFormat, uvName, dummyPropertyTexcoords + ".xy", dummyPropertyTexcoords + ".zw" ), ref dataCollector, finalTexCoordName );
					}
					//RegisterLocalVariable( 0, string.Format( Constants.TilingOffsetFormat, uvName, dummyPropertyTexcoords+".xy", dummyPropertyTexcoords+".zw" ), ref dataCollector, finalTexCoordName );
				}
				else
				{
					string finalTexCoordName = "texCoord" + OutputId;
					tiling = m_tilingPort.GeneratePortInstructions( ref dataCollector );
					offset = m_offsetPort.GeneratePortInstructions( ref dataCollector );

					if( m_texcoordSize > 2 )
					{
						dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, m_outputPorts[ 0 ].DataType, finalTexCoordName, uvName );
						dataCollector.AddLocalVariable( UniqueId, finalTexCoordName + ".xy", string.Format( Constants.TilingOffsetFormat, uvName + ".xy", tiling, offset ) + ";" );
						m_outputPorts[ 0 ].SetLocalValue( finalTexCoordName, dataCollector.PortCategory );
					}
					else
					{
						RegisterLocalVariable( 0, string.Format( Constants.TilingOffsetFormat, uvName, tiling, offset ), ref dataCollector, finalTexCoordName );
					}
					//RegisterLocalVariable( 0, string.Format( Constants.TilingOffsetFormat, uvName, tiling, offset ), ref dataCollector, finalTexCoordName );
				}
				return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );
			}

			//SURFACE
			string propertyName = GetValidPropertyName();
			if( !string.IsNullOrEmpty( portProperty ) && portProperty != "0.0" )
			{
				propertyName = portProperty;
			}

			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );

			if( !m_tilingPort.IsConnected && m_tilingPort.Vector2InternalData == Vector2.one )
				tiling = null;
			else
				tiling = m_tilingPort.GeneratePortInstructions( ref dataCollector );

			if( !m_offsetPort.IsConnected && m_offsetPort.Vector2InternalData == Vector2.zero )
				offset = null;
			else
				offset = m_offsetPort.GeneratePortInstructions( ref dataCollector );

			if( !string.IsNullOrEmpty( propertyName ) /*m_referenceArrayId > -1*/ )
			{
				m_surfaceTexcoordName = GeneratorUtils.GenerateAutoUVs( ref dataCollector, UniqueId, m_textureCoordChannel, propertyName, m_outputPorts[ 0 ].DataType, tiling, offset, OutputId, ( textureProperty != null ) ? textureProperty.CurrentVariableMode : VariableMode.Create );
			}
			else
			{
				m_surfaceTexcoordName = GeneratorUtils.GenerateAutoUVs( ref dataCollector, UniqueId, m_textureCoordChannel, null, m_outputPorts[ 0 ].DataType, tiling, offset, OutputId );
			}

			m_outputPorts[ 0 ].SetLocalValue( m_surfaceTexcoordName, dataCollector.PortCategory );
			return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );
		}

		public override void ReadInputDataFromString( ref string[] nodeParams )
		{
			if( UIUtils.CurrentShaderVersion() > 7003 )
			{
				base.ReadInputDataFromString( ref nodeParams );
			}
			else
			{
				for( int i = 0; i < 2 && i < nodeParams.Length && m_currentReadParamIdx < nodeParams.Length; i++ )
				{
					if( UIUtils.CurrentShaderVersion() < 5003 )
					{
						int newId = VersionConvertInputPortId( i ) + 1;
						if( UIUtils.CurrentShaderVersion() > 23 )
						{
							m_inputPorts[ newId ].DataType = (WirePortDataType)Enum.Parse( typeof( WirePortDataType ), nodeParams[ m_currentReadParamIdx++ ] );
						}

						m_inputPorts[ newId ].InternalData = nodeParams[ m_currentReadParamIdx++ ];
						if( m_inputPorts[ newId ].IsEditable && UIUtils.CurrentShaderVersion() >= 3100 && m_currentReadParamIdx < nodeParams.Length )
						{
							m_inputPorts[ newId ].Name = nodeParams[ m_currentReadParamIdx++ ];
						}
					}
					else
					{
						int portId = Convert.ToInt32( nodeParams[ m_currentReadParamIdx++ ] );
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
							inputPort.DataType = DataType;
							inputPort.InternalData = InternalData;
							if( !string.IsNullOrEmpty( Name ) )
							{
								inputPort.Name = Name;
							}
						}
					}
				}
			}
		}

		public override void Destroy()
		{
			base.Destroy();
			m_referenceNode = null;

			if( m_texCoordsHelper != null )
			{
				//Not calling m_texCoordsHelper.Destroy() on purpose so UIUtils does not incorrectly unregister stuff
				DestroyImmediate( m_texCoordsHelper );
				m_texCoordsHelper = null;
			}
		}

	}
}

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Texture Transform", "Textures", "Gives access to texture tiling and offset as set on the material inspector" )]
	public sealed class TextureTransformNode : ParentNode
	{
		private readonly string[] Dummy = { string.Empty };
		private const string InstancedLabelStr = "Instanced";

		[SerializeField]
		private bool m_instanced = false;

		[SerializeField]
		private int m_referenceSamplerId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		[SerializeField]
		private TexturePropertyNode m_inputReferenceNode = null;

		private TexturePropertyNode m_referenceNode = null;

		// @diogo: Triplanar's Top/Mid/Bot all carry the Triplanar's own id, so only the name tells them apart
		[SerializeField]
		private string m_referenceName = string.Empty;

		// @diogo: name last rendered in the subtitle; source renames don't notify us, so UpdateTitle polls it
		private string m_previousReferenceName = null;

		private Vector4Node m_texCoordsHelper;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		private int m_cachedSamplerId = -1;
		private int m_cachedSamplerIdArray = -1;
		private int m_cachedSamplerIdCube = -1;
		private int m_cachedSamplerId3D = -1;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.SAMPLER2D, false, "Tex" );
			m_inputPorts[ 0 ].CreatePortRestrictions( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.OBJECT );
			AddOutputPort( WirePortDataType.FLOAT2, "Tiling" );
			AddOutputPort( WirePortDataType.FLOAT2, "Offset" );
			m_textLabelWidth = 80;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			m_previewShaderGUID = "25ba2903568b00343ae06788994cab54";
		}

		public override void AfterCommonInit()
		{
			base.AfterCommonInit();

			if( PaddingTitleLeft == 0 )
			{
				PaddingTitleLeft = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
				if( PaddingTitleRight == 0 )
					PaddingTitleRight = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
			}
		}

		public override void RenderNodePreview()
		{
			//Runs at least one time
			if( !m_initialized )
			{
				// nodes with no preview don't update at all
				PreviewIsDirty = false;
				return;
			}

			if( !PreviewIsDirty )
				return;

			SetPreviewInputs();

			if( !Preferences.User.DisablePreviews )
			{
				PreparePooledPreview();

				RenderTexture temp = RenderTexture.active;

				PreviewMaterial.SetInt( "_PreviewID" , 0 );
				m_outputPorts[ 0 ].BlitPreview( PreviewMaterial , m_previewMaterialPassId );

				PreviewMaterial.SetInt( "_PreviewID" , 1 );
				m_outputPorts[ 1 ].BlitPreview( PreviewMaterial , m_previewMaterialPassId );
				RenderTexture.active = temp;

			}

			PreviewIsDirty = ContinuousPreviewRefresh;
			FinishPreviewRender = true;
		}

		void SetPreviewTexture( Texture newValue )
		{
			if( newValue is Cubemap )
			{
				m_previewMaterialPassId = 3;
				if( m_cachedSamplerIdCube == -1 )
					m_cachedSamplerIdCube = Shader.PropertyToID( "_Cube" );

				PreviewMaterial.SetTexture( m_cachedSamplerIdCube, newValue as Cubemap );
			}
			else if( newValue is Texture2DArray )
			{

				m_previewMaterialPassId = 2;
				if( m_cachedSamplerIdArray == -1 )
					m_cachedSamplerIdArray = Shader.PropertyToID( "_Array" );

				PreviewMaterial.SetTexture( m_cachedSamplerIdArray, newValue as Texture2DArray );
			}
			else if( newValue is Texture3D )
			{
				m_previewMaterialPassId = 1;
				if( m_cachedSamplerId3D == -1 )
					m_cachedSamplerId3D = Shader.PropertyToID( "_Sampler3D" );

				PreviewMaterial.SetTexture( m_cachedSamplerId3D, newValue as Texture3D );
			}
			else
			{
				m_previewMaterialPassId = 0;
				if( m_cachedSamplerId == -1 )
					m_cachedSamplerId = Shader.PropertyToID( "_Sampler" );

				PreviewMaterial.SetTexture( m_cachedSamplerId, newValue );
			}
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();
			if( m_inputPorts[ 0 ].IsConnected )
			{
				SetPreviewTexture( m_inputPorts[ 0 ].InputPreviewTexture2D( ContainerGraph, ASEPreviewSlices.ReferenceScratchIndex ) );
			}
			else if( m_referenceNode != null )
			{
				if( m_referenceNode.Value != null )
				{
					SetPreviewTexture( m_referenceNode.Value );
				}
				else
				{
					// @diogo: reference read bypasses InputPorts; the referenced property may be pooled and RT-less,
					// so pull it and, when we are live, mark it so it settles as a persistent frontier RT (QA #5c).
					m_referenceNode.EnsurePreviewRendered();
					if( PreviewLive )
					{
						m_referenceNode.MarkFeedsLiveConsumer();
					}
					SetPreviewTexture( m_referenceNode.PreviewTexture2D( ASEPreviewSlices.ReferenceScratchIndex ) );
				}
			}
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			m_inputPorts[ 0 ].MatchPortToConnection();
			m_inputReferenceNode = m_inputPorts[ 0 ].GetOutputNodeWhichIsNotRelay() as TexturePropertyNode;
			UpdateTitle();

		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			m_inputReferenceNode = null;
			UpdateTitle();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			m_inputPorts[ 0 ].MatchPortToConnection();
			UpdateTitle();
		}

		void UpdateTitle()
		{
			string referenceName;
			if ( m_inputReferenceNode != null )
			{
				referenceName = m_inputReferenceNode.PropertyInspectorName;
			}
			else if ( m_referenceSamplerId > -1 && m_referenceNode != null )
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
				m_additionalContent.text = ( referenceName.Length > 0 ) ? string.Format( Constants.PropertyValueLabel, referenceName ) : string.Empty;
				m_sizeIsDirty = true;
			}
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			CheckReference();
			UpdateTitle();
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

				if ( m_referenceNode == null || UIUtils.GetTexturePropertyNode( m_referenceSamplerId ) != m_referenceNode )
				{
					// @diogo: an id that outlived its target later resolves to whatever node inherits it;
					// Triplanar is an exception, it owns off-graph texture properties that carry its own id
					ParentNode owner = ContainerGraph.GetNode( m_referenceNodeId );
					if ( owner is TexturePropertyNode || owner is TriplanarNode )
					{
						m_referenceNode = UIUtils.GetTexturePropertyNodeByReference( m_referenceNodeId, m_referenceName );
						m_referenceSamplerId = UIUtils.GetTexturePropertyNodeRegisterId( m_referenceNode );
					}
					else
					{
						m_referenceNodeId = -1;
						m_referenceSamplerId = -1;
						m_referenceNode = null;
						m_referenceName = string.Empty;
						UpdateTitle();
					}
				}
			}
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			bool guiEnabledBuffer = GUI.enabled;
			List<string> arr = new List<string>( UIUtils.TexturePropertyNodeArr() );

			if( arr != null && arr.Count > 0 )
			{
				arr.Insert( 0, "None" );
				GUI.enabled = true && ( !m_inputPorts[ 0 ].IsConnected );
				EditorGUILayout.BeginHorizontal();
				SearchableDropdown.Draw( Constants.AvailableReferenceStr, m_referenceSamplerId + 1, UIUtils.DisambiguateReferenceLabels( arr.ToArray(), ContainerGraph.TexturePropertyNodes.NodesList, 1 ), ( idx ) =>
				{
					if( idx - 1 != m_referenceSamplerId )
					{
						UndoRecordObject( "Selected texture reference" );
						m_referenceSamplerId = idx - 1;
						m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceSamplerId );
						if( m_referenceNode != null )
						{
							m_referenceNodeId = m_referenceNode.UniqueId;
							m_referenceName = m_referenceNode.PropertyName;
						}
						else
						{
							m_referenceNodeId = -1;
							m_referenceSamplerId = -1;
							m_referenceName = string.Empty;
						}
						UpdateTitle();
						SetDirtyFromAsyncEdit();
					}
				} );
				UIUtils.DrawReferenceFocusButton( m_referenceNode );
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				m_referenceSamplerId = -1;
				GUI.enabled = false;
				SearchableDropdown.Draw( Constants.AvailableReferenceStr, m_referenceSamplerId, Dummy, null );
			}

			GUI.enabled = guiEnabledBuffer;

			bool instanced = EditorGUILayoutToggle( InstancedLabelStr, m_instanced );
			if ( instanced != m_instanced )
			{
				m_instanced = instanced;
				UpdateInstancedCount( m_instanced );
			}
		}

		// @diogo: the helper property emits an instanced access macro, but the master node only writes the
		// matching UNITY_INSTANCING_BUFFER block ( and flags the material ) while the graph counts at least
		// one instanced property, so this node keeps that count like PropertyNode does ( #507 )
		void UpdateInstancedCount( bool instanced )
		{
			if ( instanced )
			{
				UIUtils.CurrentWindow.OutsideGraph.AddInstancePropertyCount();
			}
			else
			{
				UIUtils.CurrentWindow.OutsideGraph.RemoveInstancePropertyCount();
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( !m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
				string texTransform = string.Empty;
				VariableMode texVarMode = VariableMode.Create;

				if( m_inputPorts[ 0 ].IsConnected )
				{
					texTransform = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector ) + "_ST";
					if ( m_inputPorts[ 0 ].GetOutputNodeWhichIsNotRelay( 0 ) is TexturePropertyNode connectedTexProp )
					{
						texVarMode = connectedTexProp.CurrentVariableMode;
					}
				}
				else if( m_referenceNode != null )
				{
					m_referenceNode.BaseGenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
					texTransform = m_referenceNode.PropertyName + "_ST";
					texVarMode = m_referenceNode.CurrentVariableMode;
				}
				else
				{
					texTransform = "_ST";
					UIUtils.ShowMessage( UniqueId, "Please specify a texture sample on the Texture Transform Size node", MessageSeverity.Warning );
				}

				//bool excludeUniformKeyword = UIUtils.CurrentWindow.OutsideGraph.IsInstancedShader || UIUtils.CurrentWindow.OutsideGraph.IsSRP;
				//string uniformRegister = UIUtils.GenerateUniformName( excludeUniformKeyword, WirePortDataType.FLOAT4, texTransform );
				//dataCollector.AddToUniforms( UniqueId, uniformRegister, true );
				if( m_texCoordsHelper == null )
				{
					m_texCoordsHelper = CreateInstance<Vector4Node>();
					m_texCoordsHelper.ContainerGraph = ContainerGraph;
					m_texCoordsHelper.SetBaseUniqueId( UniqueId, true );
					m_texCoordsHelper.RegisterPropertyOnInstancing = false;

				}

				if ( m_instanced )
				{
					// TODO: Enable instanced graph
					m_texCoordsHelper.CurrentParameterType = PropertyType.InstancedProperty;
				}
				else
				{

					m_texCoordsHelper.AddGlobalToSRPBatcher = ( m_referenceNode != null ) ? ( m_referenceNode.CurrentParameterType != PropertyType.Global ) : true;
					m_texCoordsHelper.CurrentParameterType = PropertyType.Global;
				}

				m_texCoordsHelper.SetRawVariableMode( texVarMode );

				m_texCoordsHelper.ResetOutputLocals();
				m_texCoordsHelper.SetRawPropertyName( texTransform );
				texTransform = m_texCoordsHelper.GenerateShaderForOutput( 0, ref dataCollector, false );

				m_outputPorts[ 0 ].SetLocalValue( texTransform + ".xy", dataCollector.PortCategory );
				m_outputPorts[ 1 ].SetLocalValue( texTransform + ".zw", dataCollector.PortCategory );
			}

			return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			EditorGUI.BeginChangeCheck();
			{
				List<string> arr = new List<string>( UIUtils.TexturePropertyNodeArr() );
				bool guiEnabledBuffer = GUI.enabled;

				if( arr != null && arr.Count > 0 )
				{
					arr.Insert( 0, "None" );
					GUI.enabled = true && ( !m_inputPorts[ 0 ].IsConnected );
					m_referenceSamplerId = m_upperLeftWidget.DrawWidget( this, m_referenceSamplerId + 1, arr.ToArray() ) - 1;
				}
				else
				{
					m_referenceSamplerId = -1;
					GUI.enabled = false;
					m_upperLeftWidget.DrawWidget( this, m_referenceSamplerId, Dummy );
				}
				GUI.enabled = guiEnabledBuffer;
			}
			if( EditorGUI.EndChangeCheck() )
			{
				m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceSamplerId );
				if( m_referenceNode != null )
				{
					m_referenceNodeId = m_referenceNode.UniqueId;
					m_referenceName = m_referenceNode.PropertyName;
				}
				else
				{
					m_referenceNodeId = -1;
					m_referenceSamplerId = -1;
					m_referenceName = string.Empty;
				}
				UpdateTitle();
			}
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			m_referenceNode = UIUtils.GetTexturePropertyNodeByReference( m_referenceNodeId, m_referenceName );
			m_referenceSamplerId = UIUtils.GetTexturePropertyNodeRegisterId( m_referenceNode );
			UpdateTitle();
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

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 17200 )
			{
				m_instanced = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				if ( m_instanced )
				{
					UpdateInstancedCount( true );
				}
			}

			if ( UIUtils.CurrentShaderVersion() >= 19913 )
			{
				m_referenceName = GetCurrentParam( ref nodeParams );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceNodeId );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_instanced );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceName );
		}

		public override void Destroy()
		{
			base.Destroy();
			if ( m_instanced )
			{
				UpdateInstancedCount( false );
			}
			m_referenceNode = null;
			m_inputReferenceNode = null;
			m_upperLeftWidget = null;
			if( m_texCoordsHelper != null )
			{
				//Not calling m_texCoordsHelper.Destroy() on purpose so UIUtils does not incorrectly unregister stuff
				DestroyImmediate( m_texCoordsHelper );
				m_texCoordsHelper = null;
			}
		}
	}
}

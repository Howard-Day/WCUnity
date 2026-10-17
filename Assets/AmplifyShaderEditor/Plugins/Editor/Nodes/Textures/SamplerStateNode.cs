// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Sampler State", "Textures", "Creates a custom sampler state or returns the default one of a selected texture object" )]
	public class SamplerStateNode : ParentNode
	{
		private const string WrapModeStr = "Wrap Mode";
		private const string UAxisStr = "U axis";
		private const string VAxisStr = "V axis";
		private const string FilterModeStr = "Filter Mode";
		private const string AnisotropicFilteringStr = "Aniso. Filtering";
		private const string MessageMacrosOFF = "Sampling Macros option is turned OFF, this node will not generate any sampler state";
		public const string MessageCustomSamplerIgnored = "Sampling Macros option is turned OFF, so the custom sampler state will be ignored";
		private const string MessageTextureObject = "Only Texture Objects that are actually being sampled within the shader generate valid sampler states.\n\nPlease make sure the referenced Texture Object is being sampled otherwise the shader will fail to compile.";
		private const string MessageUnitSuppport = "Unity support for sampler states in versions below Unity 2018.1 is limited.\n\nNotably, only vertex/frag shaders support it and not surfaces shaders and sampler states can only be reused and not created if the version is below 2017.1";

		[SerializeField]
		protected int m_wrapMode = 0;

		[SerializeField]
		protected TextureWrapMode m_wrapModeU = TextureWrapMode.Repeat;

		[SerializeField]
		protected TextureWrapMode m_wrapModeV = TextureWrapMode.Repeat;

		[SerializeField]
		protected FilterMode m_filterMode = FilterMode.Bilinear;


		public enum AnisoModes
		{
			None,
			X2,
			X4,
			X8,
			X16
		}

		[SerializeField]
		protected AnisoModes m_anisoMode = AnisoModes.None;

		[SerializeField]
		private int m_referenceSamplerId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		[SerializeField]
		private TexturePropertyNode m_inputReferenceNode = null;

		private TexturePropertyNode m_referenceNode = null;

		// @diogo: resolved form of m_referenceNodeId when it points at a Sampler State rather than a
		// texture; the id itself is shared, so no second field is persisted
		private SamplerStateNode m_referenceStateNode = null;

		// @diogo: Triplanar's Top/Mid/Bot all carry the Triplanar's own id, so only the name tells them apart
		[SerializeField]
		private string m_referenceName = string.Empty;

		// @diogo: name last rendered in the subtitle; source renames don't notify us, so UpdateTitle polls it
		private string m_previousReferenceName = null;

		private string m_previousStateLabel = null;
		private int m_previousSettingsStamp = -1;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		private InputPort m_texPort;

		private readonly string[] m_wrapModeStr = {
			"Repeat",
			"Clamp",
			"Mirror",
			"Mirror Once",
			"Per-axis"
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.SAMPLER2D, false, "Tex" );
			m_inputPorts[ 0 ].CreatePortRestrictions( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY );
			AddOutputPort( WirePortDataType.SAMPLERSTATE, "Out" );
			m_texPort = m_inputPorts[ 0 ];
			m_hasLeftDropdown = true;
			m_autoWrapProperties = true;
			m_errorMessageTypeIsError = NodeMessageType.Warning;
			m_errorMessageTooltip = MessageMacrosOFF;
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			RefreshRegistration();
		}

		public override void Destroy()
		{
			base.Destroy();
			ContainerGraph.SamplerStateNodes.RemoveNode( this );
			m_referenceStateNode = null;
		}

		// @diogo: a texture source outranks it, so a state reference left over from before one was
		// wired or picked stays idle instead of shadowing it
		private bool UsesStateReference { get { return m_referenceStateNode != null && !m_texPort.IsConnected; } }

		// Label this node shows in reference pickers and in a referencing node's subtitle: the donor
		// property when it borrows one, otherwise its own settings.
		public string ReferenceLabel
		{
			get
			{
				if ( m_inputReferenceNode != null )
				{
					return m_inputReferenceNode.PropertyInspectorName;
				}
				if ( m_referenceNode != null )
				{
					return m_referenceNode.PropertyInspectorName;
				}
				return GenerateSamplerAttributes().Replace( '_', ' ' ).TrimStart();
			}
		}

		public override string DataToArray { get { return ReferenceLabel; } }

		// Only a node that does not itself reference another Sampler State registers, so a reference can
		// never resolve to another reference and no cycle is representable.
		private void RefreshRegistration()
		{
			if ( ContainerGraph.GetNode( m_referenceNodeId ) is SamplerStateNode )
			{
				ContainerGraph.SamplerStateNodes.RemoveNode( this );
			}
			else
			{
				ContainerGraph.SamplerStateNodes.AddNode( this );
				ContainerGraph.SamplerStateNodes.UpdateDataOnNode( UniqueId, ReferenceLabel );
			}
		}

		// Options for the Reference picker: None, every Texture Object, then every Sampler State but this
		// one, the two groups split by a divider. Outputs what a picked index maps back through.
		private string[] BuildReferenceOptions( out int textureCount, out int[] stateIds, out int[] separatorIdxs )
		{
			string[] textureLabels = UIUtils.DisambiguateReferenceLabels( UIUtils.TexturePropertyNodeArr(), ContainerGraph.TexturePropertyNodes.NodesList, 0 );
			textureCount = textureLabels.Length;

			// @diogo: this node is registered too, since it can source others, so filtering it out is what
			// keeps a self reference - the one cycle the register cannot rule out - off the list
			// @diogo: labels come off the register's cached array rather than ReferenceLabel, which builds
			// a string - the canvas widget rebuilds this list every frame for every Sampler State node
			List<SamplerStateNode> registered = ContainerGraph.SamplerStateNodes.NodesList;
			string[] registeredLabels = ContainerGraph.SamplerStateNodes.NodesArr;
			List<SamplerStateNode> stateNodes = new List<SamplerStateNode>( registered.Count );
			List<string> pickedLabels = new List<string>( registered.Count );
			for ( int i = 0; i < registered.Count; i++ )
			{
				if ( registered[ i ] != this )
				{
					stateNodes.Add( registered[ i ] );
					pickedLabels.Add( registeredLabels[ i ] );
				}
			}

			stateIds = new int[ stateNodes.Count ];
			for ( int i = 0; i < stateNodes.Count; i++ )
			{
				stateIds[ i ] = stateNodes[ i ].UniqueId;
			}
			string[] stateLabels = UIUtils.DisambiguateReferenceLabels( pickedLabels.ToArray(), stateNodes, 0 );

			// @diogo: a divider under None, and another opening the Sampler State group; with no Texture
			// Objects between them the two indices coincide and only one divider is drawn
			separatorIdxs = ( stateLabels.Length > 0 ) ? new int[] { 1, textureCount + 1 } : new int[] { 1 };

			string[] options = new string[ textureCount + stateLabels.Length + 1 ];
			options[ 0 ] = "None";
			textureLabels.CopyTo( options, 1 );
			stateLabels.CopyTo( options, textureCount + 1 );
			return options;
		}

		// Index of the current reference within the merged options, 0 when nothing is referenced.
		private int CurrentReferenceIndex( int textureCount, int[] stateIds )
		{
			if ( m_referenceStateNode != null )
			{
				for ( int i = 0; i < stateIds.Length; i++ )
				{
					if ( stateIds[ i ] == m_referenceNodeId )
					{
						return textureCount + 1 + i;
					}
				}
				return 0;
			}
			return m_referenceSamplerId + 1;
		}

		// Applies a pick from the merged options. The two reference kinds are exclusive, so taking one
		// clears the other.
		private void SelectReference( int idx, int textureCount, int[] stateIds )
		{
			UndoRecordObject( "Selected sampler state reference" );

			int stateIdx = idx - textureCount - 1;
			if ( stateIdx > -1 && stateIdx < stateIds.Length )
			{
				m_referenceNodeId = stateIds[ stateIdx ];
				m_referenceStateNode = ContainerGraph.SamplerStateNodes.GetNodeByUniqueId( m_referenceNodeId );
				m_referenceSamplerId = -1;
				m_referenceNode = null;
				m_referenceName = string.Empty;
			}
			else
			{
				m_referenceStateNode = null;
				m_referenceSamplerId = idx - 1;
				m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceSamplerId );
				if ( m_referenceNode != null )
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
			}

			RefreshRegistration();
			UpdateTitle();
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
			else if ( UsesStateReference )
			{
				referenceName = m_referenceStateNode.ReferenceLabel;
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

		public override void DrawProperties()
		{
			base.DrawProperties();
			bool guiEnabledBuffer = GUI.enabled;

			string[] options = BuildReferenceOptions( out int textureCount, out int[] stateIds, out int[] separatorIdxs );
			int currentIdx = CurrentReferenceIndex( textureCount, stateIds );

			GUI.enabled = !m_texPort.IsConnected;
			EditorGUILayout.BeginHorizontal();
			SearchableDropdown.Draw( Constants.AvailableReferenceStr, currentIdx, options, ( idx ) =>
			{
				if ( idx != currentIdx )
				{
					SelectReference( idx, textureCount, stateIds );
					SetDirtyFromAsyncEdit();
				}
			}, separatorIdxs );
			UIUtils.DrawReferenceFocusButton( ( m_referenceStateNode != null ) ? (ParentNode)m_referenceStateNode : m_referenceNode );
			EditorGUILayout.EndHorizontal();

			GUI.enabled = guiEnabledBuffer;

			// @diogo: gated on the sources that actually resolve, not on the stored ids: an id whose node
			// is gone shows as None in the picker, and a picker showing None has to stay editable
			EditorGUI.BeginDisabledGroup( m_texPort.IsConnected || m_referenceNode != null || m_referenceStateNode != null );
			EditorGUI.BeginChangeCheck();
			m_wrapMode = EditorGUILayoutPopup( WrapModeStr, m_wrapMode, m_wrapModeStr );
			if( EditorGUI.EndChangeCheck() )
			{
				switch( m_wrapMode )
				{
					case 0:
					m_wrapModeU = TextureWrapMode.Repeat;
					m_wrapModeV = TextureWrapMode.Repeat;
					break;
					case 1:
					m_wrapModeU = TextureWrapMode.Clamp;
					m_wrapModeV = TextureWrapMode.Clamp;
					break;
					case 2:
					m_wrapModeU = TextureWrapMode.Mirror;
					m_wrapModeV = TextureWrapMode.Mirror;
					break;
					case 3:
					m_wrapModeU = TextureWrapMode.MirrorOnce;
					m_wrapModeV = TextureWrapMode.MirrorOnce;
					break;
				}
			}

			if( m_wrapMode == 4 )
			{
				EditorGUI.indentLevel++;
				m_wrapModeU = (TextureWrapMode)EditorGUILayoutEnumPopup( UAxisStr, m_wrapModeU );
				m_wrapModeV = (TextureWrapMode)EditorGUILayoutEnumPopup( VAxisStr, m_wrapModeV );
				EditorGUI.indentLevel--;
			}

			m_filterMode = (FilterMode)EditorGUILayoutEnumPopup( FilterModeStr, m_filterMode );

			m_anisoMode = (AnisoModes)EditorGUILayoutEnumPopup( AnisotropicFilteringStr , m_anisoMode );

			EditorGUI.EndDisabledGroup();

			if( !UIUtils.CurrentWindow.OutsideGraph.SamplingMacros )
				EditorGUILayout.HelpBox( MessageMacrosOFF, MessageType.Warning );

			if( m_texPort.IsConnected || m_referenceNode != null )
				EditorGUILayout.HelpBox( MessageTextureObject, MessageType.Info );
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			CheckReference();
			UpdateTitle();

			// @diogo: settings and donor renames both change the advertised label, and neither notifies us;
			// stamping the settings keeps the poll from building a string every tick
			int settingsStamp = m_wrapMode | ( (int)m_wrapModeU << 4 ) | ( (int)m_wrapModeV << 8 ) | ( (int)m_filterMode << 12 ) | ( (int)m_anisoMode << 16 );
			if ( settingsStamp != m_previousSettingsStamp || !string.Equals( m_previousReferenceName, m_previousStateLabel ) )
			{
				m_previousSettingsStamp = settingsStamp;
				m_previousStateLabel = m_previousReferenceName;
				RefreshRegistration();
			}

			if( !UIUtils.CurrentWindow.OutsideGraph.SamplingMacros && ContainerGraph.CurrentShaderFunction == null )
			{
				m_errorMessageTooltip = MessageMacrosOFF;
				m_showErrorMessage = true;
			}
			else
			{
				CheckSamplerSourceUsable();
			}
		}

		// @diogo: warn in the node itself when the source texture isn't sampled and codegen will fall
		// back to an inline sampler state built from this node's own settings
		private void CheckSamplerSourceUsable()
		{
			TexturePropertyNode donor = null;
			if ( m_texPort.IsConnected )
			{
				donor = m_texPort.GetOutputNodeWhichIsNotRelay() as TexturePropertyNode;
			}
			else if ( m_referenceNodeId > -1 )
			{
				donor = m_referenceNode;
			}

			// @diogo: an unused node generates no code, so the fallback has no consequence to warn about
			if ( UIUtils.CurrentWindow.OutsideGraph.SamplingMacros && donor != null && !SamplerNode.CanBorrowSampler( donor ) && IsInUse() )
			{
				ShowTab( NodeMessageType.Warning, WithWarningPasses( string.Format( "Source texture {0} is not in use, so its sampler won't exist in the compiled shader. Falling back to an inline sampler state with this node's settings.", donor.PropertyName ) ) );
			}
			else
			{
				HideTab();
			}
		}

		// Whether this node generates code: something sampled consumes it, directly or through another
		// Sampler State that references it.
		private bool IsInUse()
		{
			bool inUse = SamplerNode.IsOutputSampled( ContainerGraph, m_outputPorts[ 0 ] );

			List<ParentNode> nodes = ContainerGraph.AllNodes;
			for ( int i = 0; i < nodes.Count && !inUse; i++ )
			{
				if ( nodes[ i ] is SamplerStateNode referrer && referrer.UsesStateReference && referrer.m_referenceStateNode == this )
				{
					inUse = referrer.IsInUse();
				}
			}
			return inUse;
		}

		// @diogo: the sorted register shifts indices on rename/add/remove; re-anchor the index to the property
		void CheckReference()
		{
			if ( m_referenceNodeId > -1 )
			{
				// @diogo: one id serves both kinds of source, so resolve the Sampler State form first; the
				// register drops a source that stops being one, which is what breaks the link
				m_referenceStateNode = ContainerGraph.SamplerStateNodes.GetNodeByUniqueId( m_referenceNodeId );

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
					else if ( !( owner is SamplerStateNode ) )
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

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			// @diogo: same merged list as the properties picker, so an index means the same thing in both;
			// the canvas widget is a plain popup and simply shows the two groups without the divider
			string[] options = BuildReferenceOptions( out int textureCount, out int[] stateIds, out _ );

			bool guiEnabledBuffer = GUI.enabled;
			GUI.enabled = !m_texPort.IsConnected;
			EditorGUI.BeginChangeCheck();
			int pickedIdx = m_upperLeftWidget.DrawWidget( this, CurrentReferenceIndex( textureCount, stateIds ), options );
			if( EditorGUI.EndChangeCheck() )
			{
				SelectReference( pickedIdx, textureCount, stateIds );
			}
			GUI.enabled = guiEnabledBuffer;
		}

		public string GenerateSamplerAttributes()
		{
			string result = string.Empty;
			switch( m_filterMode )
			{
				case FilterMode.Point:
				result += "_Point";
				break;
				default:
				case FilterMode.Bilinear:
				result += "_Linear";
				break;
				case FilterMode.Trilinear:
				result += "_Trilinear";
				break;
			}

			int finalWrap = m_wrapModeU == m_wrapModeV ? (int)m_wrapModeU : m_wrapMode;
			switch( finalWrap )
			{
				case 0:
				default:
				result += "_Repeat";
				break;
				case 1:
				result += "_Clamp";
				break;
				case 2:
				result += "_Mirror";
				break;
				case 3:
				result += "_MirrorOnce";
				break;
				case 4:
				{
					switch( m_wrapModeU )
					{
						default:
						case TextureWrapMode.Repeat:
						result += "_RepeatU";
						break;
						case TextureWrapMode.Clamp:
						result += "_ClampU";
						break;
						case TextureWrapMode.Mirror:
						result += "_MirrorU";
						break;
						case TextureWrapMode.MirrorOnce:
						result += "_MirrorOnceU";
						break;
					}
					switch( m_wrapModeV )
					{
						default:
						case TextureWrapMode.Repeat:
						result += "_RepeatV";
						break;
						case TextureWrapMode.Clamp:
						result += "_ClampV";
						break;
						case TextureWrapMode.Mirror:
						result += "_MirrorV";
						break;
						case TextureWrapMode.MirrorOnce:
						result += "_MirrorOnceV";
						break;
					}
				}
				break;
			}

			switch( m_anisoMode )
			{
				default:
				case AnisoModes.None:break;
				case AnisoModes.X2:	result += "_Aniso2";break;
				case AnisoModes.X4: result += "_Aniso4"; break;
				case AnisoModes.X8: result += "_Aniso8"; break;
				case AnisoModes.X16: result += "_Aniso16"; break;
			}
			return result;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( UsesStateReference )
			{
				return m_referenceStateNode.GenerateShaderForOutput( 0, ref dataCollector, ignoreLocalvar );
			}

			if( !m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				string propertyOrOptions = string.Empty;
				TexturePropertyNode samplerDonor = null;
				if( m_texPort.IsConnected )
				{
					// @diogo: guard like the Texture Sample borrow paths, skipping the donor's codegen on fallback
					TexturePropertyNode wireDonor = m_texPort.GetOutputNodeWhichIsNotRelay() as TexturePropertyNode;
					if ( wireDonor != null && !SamplerNode.CanBorrowSampler( wireDonor ) )
					{
						dataCollector.ShowPassWarning( UniqueId, string.Format( "Sampler State attempting to use sampler from unused {0} node. Falling back to an inline sampler state.", wireDonor.PropertyName ) );
						propertyOrOptions = GenerateSamplerAttributes();
					}
					else
					{
						samplerDonor = wireDonor;
						propertyOrOptions = m_texPort.GeneratePortInstructions( ref dataCollector );
					}
				}
				else if( m_referenceNode != null )
				{
					if ( SamplerNode.CanBorrowSampler( m_referenceNode ) )
					{
						m_referenceNode.BaseGenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
						samplerDonor = m_referenceNode;
						propertyOrOptions = m_referenceNode.PropertyName;
					}
					else
					{
						dataCollector.ShowPassWarning( UniqueId, string.Format( "Sampler State attempting to use sampler from unused {0} node. Falling back to an inline sampler state.", m_referenceNode.PropertyName ) );
						propertyOrOptions = GenerateSamplerAttributes();
					}
				}
				else
				{
					propertyOrOptions = GenerateSamplerAttributes();
				}

				string sampler = GeneratorUtils.GenerateSamplerState( ref dataCollector, UniqueId, propertyOrOptions , VariableMode.Create );

				// @diogo: the donor's sampler only binds on passes and programs that sample it, so let the pass
				// fall back to this node's inline settings where it doesn't; fetch-mode donors are sampled
				// outside ASE, so their pass usage can't be told from the graph and the borrow is trusted (#7, #549)
				if ( samplerDonor != null && samplerDonor.CurrentVariableMode != VariableMode.Fetch )
				{
					string fallbackSampler = string.Format( Constants.SamplerFormat, GenerateSamplerAttributes() );
					sampler = dataCollector.RegisterBorrowedSampler( UniqueId, propertyOrOptions, sampler, fallbackSampler, true );
				}

				m_outputPorts[ 0 ].SetLocalValue( sampler, dataCollector.PortCategory );
			}

			return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			m_referenceStateNode = ContainerGraph.SamplerStateNodes.GetNodeByUniqueId( m_referenceNodeId );
			if ( m_referenceStateNode == null )
			{
				m_referenceNode = UIUtils.GetTexturePropertyNodeByReference( m_referenceNodeId, m_referenceName );
				m_referenceSamplerId = UIUtils.GetTexturePropertyNodeRegisterId( m_referenceNode );
			}
			RefreshRegistration();
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
			m_wrapMode = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_wrapModeU = (TextureWrapMode)Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_wrapModeV = (TextureWrapMode)Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_filterMode = (FilterMode)Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 18926 )
			{
				m_anisoMode = (AnisoModes)Enum.Parse( typeof( AnisoModes ) , GetCurrentParam( ref nodeParams ) );
			}

			if ( UIUtils.CurrentShaderVersion() >= 19913 )
			{
				m_referenceName = GetCurrentParam( ref nodeParams );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_wrapMode );
			IOUtils.AddFieldValueToString( ref nodeInfo, (int)m_wrapModeU );
			IOUtils.AddFieldValueToString( ref nodeInfo, (int)m_wrapModeV );
			IOUtils.AddFieldValueToString( ref nodeInfo, (int)m_filterMode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceNodeId );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_anisoMode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceName );
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Additional Directives", "Functions", "Injects custom directives (includes, defines, pragmas) into the generated shader, with optional pass and UNITY_VERSION filters." )]
	public sealed class AdditionalDirectivesNode : ParentNode
	{
		private const string AutoRegisterStr = "Auto-Register";
		private const string NameStr = "Name";
		private const string NameTooltip = "Optional name shown below the node title.";
		private const string DirectivesModuleName = "Directives";
		private const string UnityVersionLabel = "Version";
		private const string UnityVersionTooltip =
			"Only emit this directive when the Unity version compiling the shader is within the given range; Min is inclusive while Max is NOT inclusive.\n" +
			"Versions are entered as major.minor.patch with minor and patch optional, e.g. 6000.2.3 or 2022.3.\n" +
			"Leave one side empty for an open-ended range or both empty to apply no version filter.";

		private const string UnityVersionInfo =
			"Version-filtered directives are wrapped in #if (UNITY_VERSION >= Min) && (UNITY_VERSION < Max) conditionals, " +
			"so filtering happens when the shader is compiled rather than when it is generated.\n\n" +
			"Versions are entered as major.minor.patch with minor and patch optional, e.g. a 2022.3 to 6000.3 range " +
			"emits the directive from Unity 2022.3.0 up to but not including 6000.3.\n\n" +
			"IMPORTANT: below Unity 6000.x, UNITY_VERSION only reserves a single digit for the minor and patch versions, " +
			"so version bounds with minor or patch above 9 are not representable and are rejected.";

		private const string AutoRegisterTooltip =
			"When off, directives only contribute while the node is connected to the Master Node; " +
			"when on, they always contribute, even with the node left unconnected.";

		private const string ConditionalsInfo = TemplateAdditionalDirectivesHelper.PassesFormatInfo + "\n\n" + UnityVersionInfo;

		[SerializeField]
		private TemplateAdditionalDirectivesHelper m_directives;

		[SerializeField]
		private bool m_autoRegister = false;

		[SerializeField]
		private string m_name = string.Empty;

		private bool m_registered = false;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_inputPorts[ 0 ].SetFreeForAll();
			m_textLabelWidth = 110;
			m_autoWrapProperties = true;

			if( m_directives == null )
				m_directives = new TemplateAdditionalDirectivesHelper( DirectivesModuleName );
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			// @diogo: outside graph so the master's auto-register pass reaches us from inside an SF subgraph too
			UIUtils.CurrentWindow.OutsideGraph.AdditionalDirectivesNodes.AddNode( this );
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			m_inputPorts[ 0 ].MatchPortToConnection();
			m_outputPorts[ 0 ].ChangeType( m_inputPorts[ 0 ].DataType, false );
			RegisterDirectives();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			RegisterDirectives();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			m_inputPorts[ 0 ].MatchPortToConnection();
			m_outputPorts[ 0 ].ChangeType( m_inputPorts[ 0 ].DataType, false );
		}

		public override void OnOutputPortConnected( int portId, int otherNodeId, int otherPortId )
		{
			base.OnOutputPortConnected( portId, otherNodeId, otherPortId );
			RegisterDirectives();
		}

		public override void OnOutputPortDisconnected( int portId )
		{
			base.OnOutputPortDisconnected( portId );
			RegisterDirectives();
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			RegisterDirectives();
		}

		// Connectivity to the master node can change through wires added/removed elsewhere in the
		// chain, which never fires this node's own port callbacks; the activation signal does reach
		// it, so re-register here to keep the connection-status result from going stale
		public override void ActivateNode( int signalGenNodeId, int signalGenPortId, Type signalGenNodeType )
		{
			// @diogo: destroyed shells must ignore signals; destroy cascades and undo patching reach them via live neighbors
			if ( WasDestroyed )
			{
				return;
			}

			base.ActivateNode( signalGenNodeId, signalGenPortId, signalGenNodeType );
			RegisterDirectives();
		}

		public override void DeactivateNode( int deactivatedPort, bool forceComplete )
		{
			// @diogo: destroyed shells must ignore signals; destroy cascades and undo patching reach them via live neighbors
			if ( WasDestroyed )
			{
				return;
			}

			base.DeactivateNode( deactivatedPort, forceComplete );
			RegisterDirectives();
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			if( m_directives == null )
				m_directives = new TemplateAdditionalDirectivesHelper( DirectivesModuleName );

			m_directives.ConditionalVersionLabel = UnityVersionLabel;
			m_directives.ConditionalVersionTooltip = UnityVersionTooltip;
			m_directives.ConditionalVersionIsUnityVersion = true;
			m_directives.ConditionalsInfo = ConditionalsInfo;

			EditorGUI.BeginChangeCheck();
			m_name = EditorGUILayoutTextField( new GUIContent( NameStr, NameTooltip ), m_name );
			if ( EditorGUI.EndChangeCheck() )
			{
				// @diogo: semicolon is the save format field separator
				m_name = m_name.Replace( ';', ' ' );
				SetAdditonalTitleText( m_name );
			}

			EditorGUI.BeginChangeCheck();
			m_autoRegister = EditorGUILayoutToggle( new GUIContent( AutoRegisterStr, AutoRegisterTooltip ), m_autoRegister );
			m_directives.Draw( this );
			if( EditorGUI.EndChangeCheck() )
				RegisterDirectives();
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			return ( inputIndex == 0 ) ? received : StructFieldMask.All;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			// @diogo: SF subgraph nodes contribute during the walk (edit-time path is unreliable inside an SF);
			// unconnected/auto-register go through CheckIfAutoRegister
			if( ContainerGraph.GraphId > 0 )
				ContributeDirectives( ref dataCollector );

			return m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
		}

		// @diogo: auto-register pass (like PropertyNode); InsideShaderFunction covers SF nodes whose conn
		// status is unreliable, walk overlap deduped downstream
		public void CheckIfAutoRegister( ref MasterNodeDataCollector dataCollector )
		{
			if( m_autoRegister && ( m_connStatus != NodeConnectionStatus.Connected || InsideShaderFunction ) )
				ContributeDirectives( ref dataCollector );
		}

		// @diogo: write built directives to the collector with the after-native order index; duplicates deduped downstream
		private void ContributeDirectives( ref MasterNodeDataCollector dataCollector )
		{
			if( m_directives == null )
				return;

			// @diogo: honor the per-directive pass filter; empty pass name (surface/BiRP) means no pass scoping, like TestConditionals
			string currentPass = dataCollector.CurrentPassName;
			// @diogo: resolve through the pass when there is one, so former names still match
			TemplateMultiPassMasterNode passMasterNode = dataCollector.MasterNode as TemplateMultiPassMasterNode;
			TemplatePass templatePass = ( passMasterNode != null ) ? passMasterNode.Pass : null;
			// @diogo: skip directives the current template already declares natively (see BuildDirectives)
			List<AdditionalDirectiveContainer> built = BuildDirectives( dataCollector );
			for( int i = 0; i < built.Count; i++ )
			{
				bool applies = string.IsNullOrEmpty( currentPass ) ||
					( ( templatePass != null ) ? built[ i ].AppliesToPass( templatePass ) : built[ i ].AppliesToPass( currentPass ) );
				if ( applies )
					dataCollector.AddToDirectives( built[ i ].FormattedValue, 1 );
				ScriptableObject.DestroyImmediate( built[ i ] );
			}
		}

		private void RegisterDirectives()
		{
			RemoveDirectives();

			// Directives setup is not needed while editing a Shader Function ( mirrors FunctionNode ).
			if( UIUtils.CurrentWindow != null && UIUtils.CurrentWindow.IsShaderFunctionWindow )
				return;

			if( ContainerGraph == null || ContainerGraph.ParentWindow == null )
				return;

			// @diogo: edit-time path is top-level-connected only; SF-connected uses the walk, auto-register uses CheckIfAutoRegister
			if( ContainerGraph.GraphId > 0 || m_connStatus != NodeConnectionStatus.Connected || m_directives == null )
				return;

			List<AdditionalDirectiveContainer> built = BuildDirectives();
			if( built.Count == 0 )
				return;

			MasterNode masterNode = ContainerGraph.ParentWindow.OutsideGraph.CurrentMasterNode;
			StandardSurfaceOutputNode surface = masterNode as StandardSurfaceOutputNode;
			if( surface != null )
			{
				surface.AdditionalDirectives.AddShaderFunctionItems( OutputId, built );
			}
			else
			{
				for( int lod = -1; lod < ContainerGraph.ParentWindow.OutsideGraph.LodMultiPassMasternodes.Count; lod++ )
				{
					List<TemplateMultiPassMasterNode> nodes = ContainerGraph.ParentWindow.OutsideGraph.GetMultiPassMasterNodes( lod );
					for( int i = 0; i < nodes.Count; i++ )
					{
						nodes[ i ].PassModule.AdditionalDirectives.AddShaderFunctionItems( OutputId, built );
					}
				}
			}

			for( int i = 0; i < built.Count; i++ )
			{
				ScriptableObject.DestroyImmediate( built[ i ] );
			}

			m_registered = true;
		}

		private void RemoveDirectives()
		{
			if( !m_registered )
				return;

			m_registered = false;

			if( ContainerGraph == null || ContainerGraph.ParentWindow == null )
				return;

			// @diogo: skip on whole-graph teardown: master nodes may already be destroyed, and their directives die with them
			if ( ParentGraph.IsBulkDestroying )
			{
				return;
			}

			MasterNode masterNode = ContainerGraph.ParentWindow.OutsideGraph.CurrentMasterNode;
			StandardSurfaceOutputNode surface = masterNode as StandardSurfaceOutputNode;
			if( surface != null )
			{
				surface.AdditionalDirectives.RemoveShaderFunctionItems( OutputId );
			}
			else
			{
				for( int lod = -1; lod < ContainerGraph.ParentWindow.OutsideGraph.LodMultiPassMasternodes.Count; lod++ )
				{
					List<TemplateMultiPassMasterNode> nodes = ContainerGraph.ParentWindow.OutsideGraph.GetMultiPassMasterNodes( lod );
					for( int i = 0; i < nodes.Count; i++ )
					{
						nodes[ i ].PassModule.AdditionalDirectives.RemoveShaderFunctionItems( OutputId );
					}
				}
			}
		}

		// Transforms the editable list into the lines pushed to the master node, wrapping version-filtered items in
		// #if UNITY_VERSION guards. The pushed items keep the SRP version fields at 0 so the master does not editor-filter them.
		// When a data collector is supplied (generation-time walk / auto-register), directives the current template already
		// declares natively are dropped, mirroring AddToPragmas/AddToDefines/AddToIncludes and staying version-adaptive.
		private List<AdditionalDirectiveContainer> BuildDirectives( MasterNodeDataCollector dataCollector = null )
		{
			List<AdditionalDirectiveContainer> result = new List<AdditionalDirectiveContainer>();
			List<AdditionalDirectiveContainer> source = m_directives.DirectivesList;
			for( int i = 0; i < source.Count; i++ )
			{
				AdditionalDirectiveContainer item = source[ i ];
				if( item == null || item.Origin == AdditionalContainerOrigin.Native )
					continue;

				if ( IsNativeDirective( dataCollector, item ) )
					continue;

				string condition = BuildVersionCondition( item.VersionMin, item.VersionMax );
				if( !string.IsNullOrEmpty( condition ) )
				{
					// @diogo: one entry per block — the pipeline dedups by full text, so a standalone #endif would collide and drop;
					// unindented since the template indents per line
					result.Add( MakeCustom( condition + "\n" + item.FormattedValue + "\n#endif", item.Passes ) );
				}
				else
				{
					result.Add( MakeDirective( item ) );
				}
			}
			return result;
		}

		// True when the current template already declares this directive natively, so contributing it again would emit a
		// duplicate line ( e.g. a "multi_compile" keyword the template gained in a newer SRP version ). Only include/define/pragma
		// are checked; custom lines have no native counterpart to match against. Surface/BiRP output has no native container.
		private static bool IsNativeDirective( MasterNodeDataCollector dataCollector, AdditionalDirectiveContainer item )
		{
			if ( dataCollector == null || !dataCollector.IsTemplate )
				return false;

			switch ( item.LineType )
			{
				case AdditionalLineType.Include:
				case AdditionalLineType.Define:
				case AdditionalLineType.Pragma:
				return dataCollector.TemplateDataCollectorInstance.HasDirective( item.LineType, item.Value );
			}

			return false;
		}

		// Min is inclusive while Max is exclusive, mirroring the "Switch by Unity Version" node ranges
		private static string BuildVersionCondition( int min, int max )
		{
			bool hasMin = min != 0;
			bool hasMax = max != 0;
			if( hasMin && hasMax )
				return string.Format( "#if (UNITY_VERSION >= {0}) && (UNITY_VERSION < {1})", min, max );
			if( hasMin )
				return string.Format( "#if (UNITY_VERSION >= {0})", min );
			if( hasMax )
				return string.Format( "#if (UNITY_VERSION < {0})", max );
			return string.Empty;
		}

		private static AdditionalDirectiveContainer MakeDirective( AdditionalDirectiveContainer item )
		{
			AdditionalDirectiveContainer copy = ScriptableObject.CreateInstance<AdditionalDirectiveContainer>();
			copy.hideFlags = HideFlags.HideAndDontSave;
			copy.LineType = item.LineType;
			copy.LineValue = item.LineValue;
			copy.GUIDToggle = item.GUIDToggle;
			copy.GUIDValue = item.GUIDValue;
			copy.LibObject = item.LibObject;
			copy.Passes = item.Passes;
			copy.VersionMin = 0;
			copy.VersionMax = 0;
			copy.Origin = AdditionalContainerOrigin.Custom;
			return copy;
		}

		private static AdditionalDirectiveContainer MakeCustom( string line, string passes )
		{
			AdditionalDirectiveContainer custom = ScriptableObject.CreateInstance<AdditionalDirectiveContainer>();
			custom.hideFlags = HideFlags.HideAndDontSave;
			custom.LineType = AdditionalLineType.Custom;
			custom.LineValue = line;
			custom.Passes = passes;
			custom.VersionMin = 0;
			custom.VersionMax = 0;
			custom.Origin = AdditionalContainerOrigin.Custom;
			return custom;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( m_directives == null )
				m_directives = new TemplateAdditionalDirectivesHelper( DirectivesModuleName );

			m_directives.ReadFromString( ref m_currentReadParamIdx, ref nodeParams );
			m_autoRegister = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			if ( UIUtils.CurrentShaderVersion() >= 19913 )
			{
				m_name = GetCurrentParam( ref nodeParams );
				SetAdditonalTitleText( m_name );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			m_directives.WriteToString( ref nodeInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_autoRegister );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_name );
		}

		public override void Destroy()
		{
			base.Destroy();
			UIUtils.CurrentWindow.OutsideGraph.AdditionalDirectivesNodes.RemoveNode( this );
			RemoveDirectives();
			if( m_directives != null )
			{
				m_directives.Destroy();
				m_directives = null;
			}
		}
	}
}

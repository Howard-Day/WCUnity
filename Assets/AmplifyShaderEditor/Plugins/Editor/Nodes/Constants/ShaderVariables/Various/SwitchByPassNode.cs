// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Switch by Pass", "Miscellaneous", "Switch between different inputs based on the shader pass being generated, evaluated once per pass at shader generation time" )]
	public class SwitchByPassNode : ParentNode
	{
		// Entry index i always maps to input port array index i + 1 ( 0 is the Default port );
		// port unique ids are not stored because ParentNode renumbers them to match array
		// indices on every insert/delete/swap ( see RecalculateInputPortIdx )
		[Serializable]
		private class PassEntry
		{
			public string Name;
			public string Passes; // comma-separated pass names to match, case-insensitive
			public bool FoldoutFlag = true;
		}

		private const float AddRemoveButtonLayoutWidth = 15;
		private const float LineAdjust = 1.15f;
		private const float IdentationAdjust = 5f;

		private const string DefaultPortName = "Default";
		private const string EntriesStr = "Pass Entries";
		private const string NameStr = "Name";
		private const string NameTooltip = "Name of the input port for this entry.";
		private const string PassesStr = "Passes";
		private const string PassesTooltip = "List of pass Name to match, or multiple separated by commas (case-insensitive)";
		private const string Info = "Outputs the input of the first entry matching the shader pass being generated, checked top to bottom. " +
			"The choice is made at shader generation time, once per pass, since pass names cannot be reliably queried from shader code.\n\n" +
			"Each entry may list multiple pass names separated by commas, e.g. \"DepthOnly, DepthNormals\" (semicolons are also accepted and converted to commas). Matching is case-insensitive.\n\n" +
			"The Default input is used when no entry matches, which is always the case outside templates. The node preview follows the graph's main pass.";

		private const string NamelessStructError = "Switch by Pass node cannot declare a struct whose name did not reach its ports.";

		private static readonly int m_conditionId = Shader.PropertyToID( "_Condition" );

		[SerializeField]
		private List<PassEntry> m_entries = new List<PassEntry>();

		private ReorderableList m_entryReordableList = null;
		private ReordableAction m_actionType = ReordableAction.None;
		private int m_actionIndex = 0;
		private int m_lastIndex = 0;

		private string m_lastEditorPassName = null;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			AddInputPort( WirePortDataType.FLOAT, false, DefaultPortName ).AcceptAllTypes();
			AddEntry( "ShadowCaster", "ShadowCaster" );

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_previewShaderGUID = "63c0b9ddc2c9d0c4b871af8347b2d5c9";

			UpdateConnections();
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnections();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateConnections();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			UpdateConnections();
		}

		public override void OnMasterNodeReplaced( MasterNode newMasterNode )
		{
			base.OnMasterNodeReplaced( newMasterNode );
			UpdateConnections();
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			// The graph's main pass can change after load or via template edits; keep the
			// preview in sync with it
			if ( m_lastEditorPassName != CurrentEditorPassName )
			{
				m_lastEditorPassName = CurrentEditorPassName;
				PreviewIsDirty = true;
			}
		}

		private TemplateMultiPassMasterNode CurrentEditorMasterNode
		{
			get
			{
				TemplateMultiPassMasterNode masterNode = ( ContainerGraph != null ) ? ContainerGraph.CurrentMasterNode as TemplateMultiPassMasterNode : null;
				// PassName is only valid once the master node has loaded its template
				return ( masterNode != null && masterNode.CurrentTemplate != null ) ? masterNode : null;
			}
		}

		private string CurrentEditorPassName
		{
			get
			{
				TemplateMultiPassMasterNode masterNode = CurrentEditorMasterNode;
				return ( masterNode != null ) ? masterNode.PassName : string.Empty;
			}
		}

		private void AddEntry( string name, string passes )
		{
			PassEntry entry = new PassEntry() { Name = name, Passes = passes };
			m_entries.Add( entry );
			AddInputPort( WirePortDataType.FLOAT, false, entry.Name ).AcceptAllTypes();
		}

		private void AddEntryAt( int index )
		{
			PassEntry entry = new PassEntry() { Name = "Pass", Passes = string.Empty };
			m_entries.Insert( index, entry );
			// Entry ports are laid out in order right after the Default port
			AddInputPortAt( index + 1, WirePortDataType.FLOAT, false, entry.Name ).AcceptAllTypes();
		}

		private void RemoveEntry( int index )
		{
			DeleteInputPortByArrayIdx( index + 1 );
			m_entries.RemoveAt( index );
		}

		private static readonly char[] PassSeparators = new char[] { ';', ',' };

		private static string[] SplitPassNames( string passNames )
		{
			return passNames.Split( PassSeparators, StringSplitOptions.RemoveEmptyEntries ).Select( p => p.Trim() ).Where( p => p.Length > 0 ).ToArray();
		}

		// @diogo: matching goes through the pass itself so a filter typed before a rename,
		// against a name the template still lists as former, keeps selecting the same input
		private bool MatchesPass( PassEntry entry, TemplateMultiPassMasterNode masterNode )
		{
			if ( string.IsNullOrEmpty( entry.Passes ) || masterNode == null )
			{
				return false;
			}

			foreach ( string token in SplitPassNames( entry.Passes ) )
			{
				if ( masterNode.PassNameMatches( token ) )
				{
					return true;
				}
			}
			return false;
		}

		private int GetPortArrayIdForPass( TemplateMultiPassMasterNode masterNode )
		{
			for ( int i = 0; i < m_entries.Count; i++ )
			{
				if ( MatchesPass( m_entries[ i ], masterNode ) )
				{
					return i + 1; // array id 0 is the Default port
				}
			}
			return 0;
		}

		private void UpdateConnections()
		{
			// Each pass may pick a different input at generation time, so every port shares the
			// highest priority connected type instead of following one active port
			WirePortDataType mainType = WirePortDataType.FLOAT;
			string structName = string.Empty;
			int highest = -1;
			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				if ( m_inputPorts[ i ].IsConnected )
				{
					WirePortDataType portType = m_inputPorts[ i ].GetOutputConnection().DataType;
					// @diogo: a struct ranks below every scalar, yet nothing casts to or from one, so a wired
					// struct decides the type outright rather than losing the priority race below
					if ( mainType != WirePortDataType.STRUCT && ( portType == WirePortDataType.STRUCT || UIUtils.GetPriority( portType ) > highest ) )
					{
						mainType = portType;
						structName = ( portType == WirePortDataType.STRUCT ) ? m_inputPorts[ i ].GetOutputConnection().StructName : string.Empty;
						highest = UIUtils.GetPriority( portType );
					}
				}
			}

			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].ChangeType( mainType, structName, false );
			}
			m_outputPorts[ 0 ].ChangeType( mainType, structName, false );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, EntriesStr, DrawReordableEntries, DrawEntryAddRemoveButtons );

			NodeUtils.DrawHelpBox( Info, MessageType.Info );
		}

		private void DrawReordableEntries()
		{
			if ( m_entryReordableList == null )
			{
				m_entryReordableList = new ReorderableList( m_entries, typeof( PassEntry ), true, false, false, false )
				{
					headerHeight = 0,
					footerHeight = 0,
					showDefaultBackground = false,
					elementHeightCallback = ( int index ) =>
					{
						float lineHeight = EditorGUIUtility.singleLineHeight * LineAdjust;
						return m_entries[ index ].FoldoutFlag ? 4 * lineHeight : lineHeight;
					},
					onSelectCallback = ( ReorderableList list ) =>
					{
						m_lastIndex = list.index;
					},
					onReorderCallback = ( ReorderableList list ) =>
					{
						// Entry ports are laid out in order right after the Default port
						SwapInputPorts( m_lastIndex + 1, list.index + 1 );
						UpdateConnections();
					},
					drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
					{
						PassEntry entry = m_entries[ index ];
						if ( entry != null )
						{
							float lineHeight = EditorGUIUtility.singleLineHeight;
							float lineSpacing = lineHeight * LineAdjust;

							float cacheLabelSize = EditorGUIUtility.labelWidth;
							EditorGUIUtility.labelWidth = 50;

							rect.x -= IdentationAdjust;
							rect.height = lineHeight;
							Rect foldoutRect = rect;
							if ( !entry.FoldoutFlag )
							{
								foldoutRect.width -= 2 * AddRemoveButtonLayoutWidth;
							}
							entry.FoldoutFlag = EditorGUIFoldout( foldoutRect, entry.FoldoutFlag, entry.Name );
							if ( entry.FoldoutFlag )
							{
								rect.x += IdentationAdjust;

								// Name
								rect.y += lineSpacing;
								EditorGUI.BeginChangeCheck();
								entry.Name = EditorGUITextField( rect, new GUIContent( NameStr, NameTooltip ), entry.Name );
								if ( EditorGUI.EndChangeCheck() )
								{
									entry.Name = entry.Name.Replace( ';', ' ' );
									if ( string.IsNullOrEmpty( entry.Name ) )
									{
										entry.Name = "Pass";
									}
									GetInputPortByArrayId( index + 1 ).Name = entry.Name;
									m_sizeIsDirty = true;
								}

								// Passes
								rect.y += lineSpacing;
								EditorGUI.BeginChangeCheck();
								entry.Passes = EditorGUI.TextField( rect, new GUIContent( PassesStr, PassesTooltip ), entry.Passes );
								if ( EditorGUI.EndChangeCheck() )
								{
									entry.Passes = entry.Passes.Replace( ';', ',' );
									UpdateConnections();
									PreviewIsDirty = true;
								}

								// Buttons
								rect.x += rect.width - 2 * AddRemoveButtonLayoutWidth;
								rect.y += lineSpacing;
								rect.width = AddRemoveButtonLayoutWidth;
								if ( GUI.Button( rect, string.Empty, UIUtils.PlusStyle ) )
								{
									m_actionType = ReordableAction.Add;
									m_actionIndex = index;
								}
								rect.x += AddRemoveButtonLayoutWidth;
								if ( GUI.Button( rect, string.Empty, UIUtils.MinusStyle ) )
								{
									m_actionType = ReordableAction.Remove;
									m_actionIndex = index;
								}
							}
							else
							{
								// Buttons
								rect.x += IdentationAdjust + rect.width - 2 * AddRemoveButtonLayoutWidth;
								rect.width = AddRemoveButtonLayoutWidth;
								if ( GUI.Button( rect, string.Empty, UIUtils.PlusStyle ) )
								{
									m_actionType = ReordableAction.Add;
									m_actionIndex = index;
								}
								rect.x += AddRemoveButtonLayoutWidth;
								if ( GUI.Button( rect, string.Empty, UIUtils.MinusStyle ) )
								{
									m_actionType = ReordableAction.Remove;
									m_actionIndex = index;
								}
							}

							EditorGUIUtility.labelWidth = cacheLabelSize;
						}
					}
				};
			}

			if ( m_entryReordableList != null )
			{
				EditorGUILayout.Space();
				if ( m_entries.Count == 0 )
				{
					EditorGUILayout.HelpBox( "Your list is Empty!\nUse the plus button to add one.", MessageType.Info );
				}
				else
				{
					m_entryReordableList.DoLayoutList();
				}
				EditorGUILayout.Space();
			}

			if ( m_actionType != ReordableAction.None )
			{
				switch ( m_actionType )
				{
					case ReordableAction.Add:
					AddEntryAt( m_actionIndex + 1 );
					break;
					case ReordableAction.Remove:
					RemoveEntry( m_actionIndex );
					break;
				}
				m_isDirty = true;
				m_actionType = ReordableAction.None;
				EditorGUI.FocusTextInControl( null );
				UpdateConnections();
			}
		}

		private void DrawEntryAddRemoveButtons()
		{
			if ( m_entries.Count == 0 )
				m_propertiesFoldout = false;

			// Add new entry
			if ( GUILayoutButton( string.Empty, UIUtils.PlusStyle, GUILayout.Width( AddRemoveButtonLayoutWidth ) ) )
			{
				AddEntryAt( m_entries.Count );
				m_propertiesFoldout = true;
				EditorGUI.FocusTextInControl( null );
				UpdateConnections();
			}

			// Remove last entry
			if ( GUILayoutButton( string.Empty, UIUtils.MinusStyle, GUILayout.Width( AddRemoveButtonLayoutWidth ) ) )
			{
				if ( m_entries.Count > 0 )
				{
					RemoveEntry( m_entries.Count - 1 );
					EditorGUI.FocusTextInControl( null );
					UpdateConnections();
				}
			}
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			return received;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			if ( ReportStructMix( 0, m_inputPorts.Count - 1 ) )
			{
				return m_outputPorts[ 0 ].ErrorValue;
			}

			// @diogo: a struct names itself on the port, since the wire type alone cannot spell it
			if ( m_outputPorts[ 0 ].DataType == WirePortDataType.STRUCT && string.IsNullOrEmpty( m_outputPorts[ 0 ].StructName ) )
			{
				UIUtils.ShowMessage( UniqueId, NamelessStructError, MessageSeverity.Error );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			TemplateMultiPassMasterNode masterNode = dataCollector.MasterNode as TemplateMultiPassMasterNode;

			InputPort port = GetInputPortByArrayId( GetPortArrayIdForPass( masterNode ) );
			return port.GeneratePortInstructions( ref dataCollector );
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();
			// Preview input textures are bound by port unique id ( _A = 0, _B = 1, ... )
			PreviewMaterial.SetInt( m_conditionId, GetInputPortByArrayId( GetPortArrayIdForPass( CurrentEditorMasterNode ) ).PortId );
		}

		private void RecreateEntryPorts()
		{
			// @diogo: a node being read has no wires yet, and a pasted one still carries the source's
			// id, so deleting through the graph would cut the source node's wires instead
			while ( m_inputPorts.Count > 1 )
			{
				RemoveInputPort( m_inputPorts.Count - 1 );
			}

			// Ports get sequential unique ids ( entry index + 1 ), which is also what saved wire
			// connections reference since ParentNode keeps ids matched to array indices
			foreach ( var entry in m_entries )
			{
				AddInputPort( WirePortDataType.FLOAT, false, entry.Name ).AcceptAllTypes();
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );

			m_entries.Clear();

			int count = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			for ( int i = 0; i < count; i++ )
			{
				PassEntry entry = new PassEntry();
				entry.Name = GetCurrentParam( ref nodeParams );
				entry.Passes = GetCurrentParam( ref nodeParams );
				Convert.ToInt32( GetCurrentParam( ref nodeParams ) ); // port id; redundant ( always index + 1 ) but kept in the format
				m_entries.Add( entry );
			}

			RecreateEntryPorts();
			UpdateConnections();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_entries.Count );
			for ( int i = 0; i < m_entries.Count; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_entries[ i ].Name );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_entries[ i ].Passes );
				IOUtils.AddFieldValueToString( ref nodeInfo, i + 1 ); // port id; redundant ( always index + 1 ) but kept in the format
			}
		}
	}
}

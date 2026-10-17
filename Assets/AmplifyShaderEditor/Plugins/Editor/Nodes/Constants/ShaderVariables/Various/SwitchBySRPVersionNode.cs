// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Switch by SRP Version", "Miscellaneous", "Switch between different inputs based on user-defined SRP version ranges" )]
	[NoAutoStaticsCleanup]
	public class SwitchBySRPVersionNode : ParentNode
	{
		// Range index i always maps to input port array index i + 1 ( 0 is the Default port );
		// port unique ids are not stored because ParentNode renumbers them to match array
		// indices on every insert/delete/swap ( see RecalculateInputPortIdx )
		[Serializable]
		private class VersionRange
		{
			public string Name;
			public int From; // packed version ( major * 10000 + minor * 100 ), inclusive
			public int To;   // packed version, exclusive
			public bool FoldoutFlag = true;
		}

		private const float AddRemoveButtonLayoutWidth = 15;
		private const float LineAdjust = 1.15f;
		private const float IdentationAdjust = 5f;

		private const int OpenEndedVersion = int.MaxValue; // empty To field; matches everything from From upward

		private const string DefaultPortName = "Default";
		private const string RangesStr = "Version Ranges";
		private const string NameStr = "Name";
		private const string NameTooltip = "Name of the input port for this entry.";
		private const string FromTooltip = "Minimum SRP version, inclusive ( major.minor )";
		private const string ToTooltip = "Maximum SRP version, exclusive ( major.minor ); leave empty for no upper limit";
		private const string Info = "Outputs the input of the first version range matching the currently installed SRP version, checked top to bottom.\n\n" +
			"From is inclusive while To is NOT inclusive, e.g. a 14.0 to 15.0 range matches every 14.x version but not 15.0. " +
			"Leaving To empty makes the range open-ended, matching everything from From upward.\n\n" +
			"The Default input is used when no range matches, which is always the case for Built-in.";

		private const string NamelessStructError = "Switch by SRP Version node cannot declare a struct whose name did not reach its ports.";

		// Version ranges matching the old fixed port layout; index + 1 = old port unique id; the last
		// entry is open-ended since the old node also matched it against anything above it
		private static readonly Tuple<string, int, int>[] LegacyVersionRanges = new Tuple<string, int, int>[]
		{
			new Tuple<string, int, int>( "10.x", 100000, 110000 ),
			new Tuple<string, int, int>( "11.x", 110000, 120000 ),
			new Tuple<string, int, int>( "12.x", 120000, 130000 ),
			new Tuple<string, int, int>( "13.x", 130000, 140000 ),
			new Tuple<string, int, int>( "14.x", 140000, 150000 ),
			new Tuple<string, int, int>( "15.x", 150000, 160000 ),
			new Tuple<string, int, int>( "16.x", 160000, 170000 ),
			new Tuple<string, int, int>( "17.0", 170000, 170100 ),
			new Tuple<string, int, int>( "17.1", 170100, 170200 ),
			new Tuple<string, int, int>( "17.2", 170200, 170300 ),
			new Tuple<string, int, int>( "17.3", 170300, 170400 ),
			new Tuple<string, int, int>( "17.4", 170400, 170500 ),
			new Tuple<string, int, int>( "17.5", 170500, OpenEndedVersion )
		};

		private static readonly string[] SRPTypeNames =
		{
			"Built-in",
			"High Definition",
			"Universal"
		};

		private static readonly int m_conditionId = Shader.PropertyToID( "_Condition" );

		[SerializeField]
		private List<VersionRange> m_ranges = new List<VersionRange>();

		private ReorderableList m_rangeReordableList = null;
		private ReordableAction m_actionType = ReordableAction.None;
		private int m_actionIndex = 0;
		private int m_lastIndex = 0;

		private string m_versionFieldInEdit = string.Empty;
		private string m_versionFieldBuffer = string.Empty;

		private bool m_legacyMigration = false;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			AddInputPort( WirePortDataType.FLOAT, false, DefaultPortName ).AcceptAllTypes();
			AddRange( "14.x", 140000, 150000 );

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_previewShaderGUID = "63c0b9ddc2c9d0c4b871af8347b2d5c9";

			UpdateConnections();
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			GetInputPortByUniqueId( portId ).MatchPortToConnection();
			UpdateConnections();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			InputPort port = GetInputPortByUniqueId( portId );
			port.MatchPortToConnection();

			// @diogo: an unwired arm keeps its last type, and a struct would be left with no name to declare
			if ( port.DataType == WirePortDataType.STRUCT )
			{
				port.ChangeType( WirePortDataType.FLOAT, string.Empty, false );
			}

			UpdateConnections();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			GetInputPortByUniqueId( outputPortId ).MatchPortToConnection();
			UpdateConnections();
		}

		public override void OnMasterNodeReplaced( MasterNode newMasterNode )
		{
			base.OnMasterNodeReplaced( newMasterNode );
			UpdateConnections();
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();

			if ( m_legacyMigration )
			{
				m_legacyMigration = false;

				// Drop migrated version ports without connections; keep only what was actually being used
				for ( int i = m_ranges.Count - 1; i >= 0; i-- )
				{
					if ( !GetInputPortByArrayId( i + 1 ).IsConnected )
					{
						DeleteInputPortByArrayIdx( i + 1 );
						m_ranges.RemoveAt( i );
					}
				}

				if ( m_ranges.Count == 0 )
				{
					AddRange( "14.x", 140000, 150000 );
				}
			}

			UpdateConnections();
		}

		private void AddRange( string name, int from, int to )
		{
			VersionRange range = new VersionRange() { Name = name, From = from, To = to };
			m_ranges.Add( range );
			AddInputPort( WirePortDataType.FLOAT, false, range.Name ).AcceptAllTypes();
		}

		private void AddRangeAt( int index )
		{
			int from = ASEPackageManagerHelper.MinimumSupportedSRPVersion;
			if ( index > 0 )
			{
				VersionRange previous = m_ranges[ index - 1 ];
				from = ( previous.To != OpenEndedVersion ) ? previous.To : previous.From + 10000;
			}
			VersionRange range = new VersionRange() { Name = ( from / 10000 ) + ".x", From = from, To = from + 10000 };
			m_ranges.Insert( index, range );
			// Range ports are laid out in order right after the Default port
			AddInputPortAt( index + 1, WirePortDataType.FLOAT, false, range.Name ).AcceptAllTypes();
		}

		private void RemoveRange( int index )
		{
			DeleteInputPortByArrayIdx( index + 1 );
			m_ranges.RemoveAt( index );
		}

		private static string VersionToString( int version )
		{
			return string.Format( "{0}.{1}", version / 10000, ( version % 10000 ) / 100 );
		}

		private static bool TryParseVersion( string str, out int version )
		{
			version = 0;
			if ( string.IsNullOrEmpty( str ) )
				return false;

			string[] parts = str.Trim().Split( '.' );
			if ( parts.Length > 2 )
				return false;

			int major;
			int minor = 0;
			if ( !int.TryParse( parts[ 0 ], out major ) )
				return false;

			if ( parts.Length > 1 && !int.TryParse( parts[ 1 ], out minor ) )
				return false;

			if ( major < 0 || minor < 0 || minor > 99 )
				return false;

			version = major * 10000 + minor * 100;
			return true;
		}

		// Immediate text field over a packed version value; edits apply on every keystroke so they
		// can't be lost by saving while the field still has focus ( a delayed field only commits on
		// Enter/focus change ). While focused, the raw text is kept so typing isn't reformatted
		// mid-edit; invalid text simply leaves the last valid value untouched.
		private int DrawVersionField( Rect rect, GUIContent label, string controlName, int version, bool allowOpenEnded )
		{
			string text;
			if ( m_versionFieldInEdit == controlName )
			{
				text = m_versionFieldBuffer;
			}
			else
			{
				text = ( allowOpenEnded && version == OpenEndedVersion ) ? string.Empty : VersionToString( version );
			}

			GUI.SetNextControlName( controlName );
			EditorGUI.BeginChangeCheck();
			text = EditorGUI.TextField( rect, label, text );
			if ( EditorGUI.EndChangeCheck() )
			{
				m_versionFieldInEdit = controlName;
				m_versionFieldBuffer = text;

				int parsed;
				if ( allowOpenEnded && string.IsNullOrEmpty( text.Trim() ) )
				{
					version = OpenEndedVersion;
					UpdateConnections();
					PreviewIsDirty = true;
				}
				else if ( TryParseVersion( text, out parsed ) )
				{
					version = parsed;
					UpdateConnections();
					PreviewIsDirty = true;
				}
			}
			else if ( m_versionFieldInEdit == controlName && GUI.GetNameOfFocusedControl() != controlName )
			{
				m_versionFieldInEdit = string.Empty;
			}

			return version;
		}

		private int GetActivePortArrayId()
		{
			if ( ContainerGraph != null )
			{
				int srpVersion = ASEPackageManagerHelper.CurrentSRPVersion;
				for ( int i = 0; i < m_ranges.Count; i++ )
				{
					if ( srpVersion >= m_ranges[ i ].From && srpVersion < m_ranges[ i ].To )
					{
						return i + 1; // array id 0 is the Default port
					}
				}
			}
			return 0;
		}

		private void UpdateConnections()
		{
			int activePortIndex = GetActivePortArrayId();
			InputPort activePort = GetInputPortByArrayId( activePortIndex );
			m_outputPorts[ 0 ].ChangeTypeWithRestrictions( activePort.DataType, activePort.StructName, FunctionInput.PortCreateRestriction( activePort.DataType ) );

			string activeName = ( activePortIndex > 0 ) ? m_ranges[ activePortIndex - 1 ].Name : DefaultPortName;
			string srpTypeName = ( ContainerGraph != null ) ? SRPTypeNames[ ( int )ContainerGraph.CurrentSRPType ] : "Unknown";
			SetAdditonalTitleText( string.Format( Constants.SubTitleCurrentFormatStr, srpTypeName + ", " + activeName ) );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, RangesStr, DrawReordableRanges, DrawRangeAddRemoveButtons );

			NodeUtils.DrawHelpBox( Info, MessageType.Info );
		}

		private void DrawReordableRanges()
		{
			if ( m_rangeReordableList == null )
			{
				m_rangeReordableList = new ReorderableList( m_ranges, typeof( VersionRange ), true, false, false, false )
				{
					headerHeight = 0,
					footerHeight = 0,
					showDefaultBackground = false,
					elementHeightCallback = ( int index ) =>
					{
						float lineHeight = EditorGUIUtility.singleLineHeight * LineAdjust;
						return m_ranges[ index ].FoldoutFlag ? 4 * lineHeight : lineHeight;
					},
					onSelectCallback = ( ReorderableList list ) =>
					{
						m_lastIndex = list.index;
					},
					onReorderCallback = ( ReorderableList list ) =>
					{
						// Range ports are laid out in order right after the Default port
						SwapInputPorts( m_lastIndex + 1, list.index + 1 );
						UpdateConnections();
					},
					drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
					{
						VersionRange range = m_ranges[ index ];
						if ( range != null )
						{
							float lineHeight = EditorGUIUtility.singleLineHeight;
							float lineSpacing = lineHeight * LineAdjust;

							float cacheLabelSize = EditorGUIUtility.labelWidth;
							EditorGUIUtility.labelWidth = 50;

							rect.x -= IdentationAdjust;
							rect.height = lineHeight;
							Rect foldoutRect = rect;
							if ( !range.FoldoutFlag )
							{
								foldoutRect.width -= 2 * AddRemoveButtonLayoutWidth;
							}
							range.FoldoutFlag = EditorGUIFoldout( foldoutRect, range.FoldoutFlag, range.Name );
							if ( range.FoldoutFlag )
							{
								rect.x += IdentationAdjust;

								// Name
								rect.y += lineSpacing;
								EditorGUI.BeginChangeCheck();
								range.Name = EditorGUITextField( rect, new GUIContent( NameStr, NameTooltip ), range.Name );
								if ( EditorGUI.EndChangeCheck() )
								{
									range.Name = range.Name.Replace( ';', ' ' );
									if ( string.IsNullOrEmpty( range.Name ) )
									{
										range.Name = ( range.From / 10000 ) + ".x";
									}
									GetInputPortByArrayId( index + 1 ).Name = range.Name;
									m_sizeIsDirty = true;
								}

								// From / To
								rect.y += lineSpacing;
								Rect fromRect = rect;
								fromRect.width = rect.width * 0.5f - 2;
								Rect toRect = fromRect;
								toRect.x += fromRect.width + 4;
								range.From = DrawVersionField( fromRect, new GUIContent( "From", FromTooltip ), "ASESRPRange" + UniqueId + "From" + index, range.From, false );
								range.To = DrawVersionField( toRect, new GUIContent( "To", ToTooltip ), "ASESRPRange" + UniqueId + "To" + index, range.To, true );

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

			if ( m_rangeReordableList != null )
			{
				EditorGUILayout.Space();
				if ( m_ranges.Count == 0 )
				{
					EditorGUILayout.HelpBox( "Your list is Empty!\nUse the plus button to add one.", MessageType.Info );
				}
				else
				{
					m_rangeReordableList.DoLayoutList();
				}
				EditorGUILayout.Space();
			}

			if ( m_actionType != ReordableAction.None )
			{
				switch ( m_actionType )
				{
					case ReordableAction.Add:
					AddRangeAt( m_actionIndex + 1 );
					break;
					case ReordableAction.Remove:
					RemoveRange( m_actionIndex );
					break;
				}
				m_isDirty = true;
				m_actionType = ReordableAction.None;
				EditorGUI.FocusTextInControl( null );
				UpdateConnections();
			}
		}

		private void DrawRangeAddRemoveButtons()
		{
			if ( m_ranges.Count == 0 )
				m_propertiesFoldout = false;

			// Add new range
			if ( GUILayoutButton( string.Empty, UIUtils.PlusStyle, GUILayout.Width( AddRemoveButtonLayoutWidth ) ) )
			{
				AddRangeAt( m_ranges.Count );
				m_propertiesFoldout = true;
				EditorGUI.FocusTextInControl( null );
				UpdateConnections();
			}

			// Remove last range
			if ( GUILayoutButton( string.Empty, UIUtils.MinusStyle, GUILayout.Width( AddRemoveButtonLayoutWidth ) ) )
			{
				if ( m_ranges.Count > 0 )
				{
					RemoveRange( m_ranges.Count - 1 );
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
			InputPort port = GetInputPortByArrayId( GetActivePortArrayId() );
			m_outputPorts[ 0 ].ChangeType( port.DataType, port.StructName, false );

			// @diogo: a struct names itself on the port, since the wire type alone cannot spell it
			if ( port.DataType == WirePortDataType.STRUCT && string.IsNullOrEmpty( port.StructName ) )
			{
				UIUtils.ShowMessage( UniqueId, NamelessStructError, MessageSeverity.Error );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			return port.GeneratePortInstructions( ref dataCollector );
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();
			// Preview input textures are bound by port unique id ( _A = 0, _B = 1, ... )
			PreviewMaterial.SetInt( m_conditionId, GetInputPortByArrayId( GetActivePortArrayId() ).PortId );
		}

		private void RecreateRangePorts()
		{
			// @diogo: a node being read has no wires yet, and a pasted one still carries the source's
			// id, so deleting through the graph would cut the source node's wires instead
			while ( m_inputPorts.Count > 1 )
			{
				RemoveInputPort( m_inputPorts.Count - 1 );
			}

			// Ports get sequential unique ids ( range index + 1 ), which is also what saved wire
			// connections reference since ParentNode keeps ids matched to array indices
			foreach ( var range in m_ranges )
			{
				AddInputPort( WirePortDataType.FLOAT, false, range.Name ).AcceptAllTypes();
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );

			m_ranges.Clear();

			bool legacyFormat = ( UIUtils.CurrentShaderVersion() <= 19909 );
			if ( !legacyFormat )
			{
				uint cachedReadParamIdx = m_currentReadParamIdx;
				bool cachedSilentReadFailure = m_silentReadFailure;
				try
				{
					int count = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
					for ( int i = 0; i < count; i++ )
					{
						VersionRange range = new VersionRange();
						range.Name = GetCurrentParam( ref nodeParams );
						range.From = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
						range.To = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
						Convert.ToInt32( GetCurrentParam( ref nodeParams ) ); // port id; redundant ( always index + 1 ) but kept in the format
						m_ranges.Add( range );
					}
				}
				catch ( Exception )
				{
					// Shaders saved with this same version but before the range rework have no range
					// data; what we just tried to read was the input port data block, so rewind and
					// treat the node as legacy format
					m_currentReadParamIdx = cachedReadParamIdx;
					// @diogo: the rewind is a planned recovery, so the read must not look like it failed
					m_silentReadFailure = cachedSilentReadFailure;
					m_ranges.Clear();
					legacyFormat = true;
				}
			}

			if ( legacyFormat )
			{
				// Old format had one fixed port per version; recreate them so connections still bind,
				// then prune unconnected ones on RefreshExternalReferences()
				for ( int i = 0; i < LegacyVersionRanges.Length; i++ )
				{
					VersionRange range = new VersionRange();
					range.Name = LegacyVersionRanges[ i ].Item1;
					range.From = LegacyVersionRanges[ i ].Item2;
					range.To = LegacyVersionRanges[ i ].Item3;
					m_ranges.Add( range );
				}
				m_legacyMigration = true;
			}

			RecreateRangePorts();
			UpdateConnections();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_ranges.Count );
			for ( int i = 0; i < m_ranges.Count; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_ranges[ i ].Name );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_ranges[ i ].From );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_ranges[ i ].To );
				IOUtils.AddFieldValueToString( ref nodeInfo, i + 1 ); // port id; redundant ( always index + 1 ) but kept in the format
			}
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Switch by Unity Version", "Miscellaneous", "Switch between different inputs based on user-defined Unity version ranges, evaluated at shader compile time via UNITY_VERSION" )]
	public class SwitchByUnityVersionNode : ParentNode
	{
		// Range index i always maps to input port array index i + 1 ( 0 is the Default port );
		// port unique ids are not stored because ParentNode renumbers them to match array
		// indices on every insert/delete/swap ( see RecalculateInputPortIdx )
		[Serializable]
		private class VersionRange
		{
			public string Name;
			public int From; // UNITY_VERSION macro value (see UnityVersionUtils), inclusive; 0 when open-ended
			public int To;   // UNITY_VERSION macro value, exclusive; int.MaxValue when open-ended
			public bool FoldoutFlag = true;
		}

		private const float AddRemoveButtonLayoutWidth = 15;
		private const float LineAdjust = 1.15f;
		private const float IdentationAdjust = 5f;

		private const int OpenEndedVersion = int.MaxValue;   // empty To field; matches everything from From upward
		private const int OpenEndedFromVersion = 0;          // empty From field; matches everything below To

		private const string DefaultPortName = "Default";
		private const string RangesStr = "Version Ranges";
		private const string NameStr = "Name";
		private const string NameTooltip = "Name of the input port for this entry.";
		private const string FromTooltip = "Minimum Unity version, inclusive (major.minor.patch; minor and patch are optional); leave empty for no lower limit";
		private const string ToTooltip = "Maximum Unity version, exclusive (major.minor.patch; minor and patch are optional); leave empty for no upper limit";
		private const string Info = "Generates UNITY_VERSION preprocessor conditionals so the input is picked by the Unity version compiling the shader, checked top to bottom; " +
			"unlike \"Switch by SRP Version\", all inputs are included in the generated code.\n\n" +
			"From is inclusive while To is NOT inclusive, e.g. a 6000.0 to 6000.2 range matches 6000.0 and 6000.1 but not 6000.2. " +
			"Leaving From or To empty makes that side of the range open-ended; leaving both empty is invalid since the range would match everything.\n\n" +
			"The Default input is used when no range matches. The node preview follows the Unity version running the editor.\n\n" +
			"IMPORTANT: below Unity 6000.x, UNITY_VERSION only reserves a single digit for the minor and patch versions, " +
			"so pre-6000 bounds with minor or patch above 9 are not representable and are rejected; " +
			"prefer whole-major boundaries for pre-6000 versions.";

		private const string NamelessStructError = "Switch by Unity Version node cannot declare a struct whose name did not reach its ports.";

		private static readonly int m_conditionId = Shader.PropertyToID( "_Condition" );

		[SerializeField]
		private List<VersionRange> m_ranges = new List<VersionRange>();

		private ReorderableList m_rangeReordableList = null;
		private ReordableAction m_actionType = ReordableAction.None;
		private int m_actionIndex = 0;
		private int m_lastIndex = 0;

		private UnityVersionField m_versionField = new UnityVersionField();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			ForbidSamplers( AddInputPort( WirePortDataType.FLOAT, false, DefaultPortName ) );
			int editorMajor = UnityVersionUtils.Major( UnityVersionUtils.EditorVersion );
			int nextMajor = ( editorMajor >= 6000 ) ? ( editorMajor + 1000 ) : ( editorMajor + 1 );
			AddRange( editorMajor + ".x", UnityVersionUtils.Pack( editorMajor, 0, 0 ), UnityVersionUtils.Pack( nextMajor, 0, 0 ) );

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_previewShaderGUID = "63c0b9ddc2c9d0c4b871af8347b2d5c9";

			UpdateConnections();
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			return received;
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

		// @diogo: a texture object is bound, not assigned, so no arm can carry one through the switch. A
		// struct is assigned like any value, so it is let in first - forbidding types seeds a mask without it
		private static void ForbidSamplers( InputPort port )
		{
			port.AcceptAllTypes();
			port.AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
				WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
		}

		private void AddRange( string name, int from, int to )
		{
			VersionRange range = new VersionRange() { Name = name, From = from, To = to };
			m_ranges.Add( range );
			ForbidSamplers( AddInputPort( WirePortDataType.FLOAT, false, range.Name ) );
		}

		private void AddRangeAt( int index )
		{
			// Editor version is truncated to major.minor since default ranges span whole minors
			int editorVersion = UnityVersionUtils.EditorVersion;
			int from = editorVersion - ( editorVersion % UnityVersionUtils.MinorStep( editorVersion ) );
			if ( index > 0 )
			{
				VersionRange previous = m_ranges[ index - 1 ];
				if ( previous.To != OpenEndedVersion )
				{
					from = previous.To;
				}
				else if ( previous.From != OpenEndedFromVersion )
				{
					from = previous.From + UnityVersionUtils.MinorStep( previous.From );
				}
			}
			// Unity version streams advance by minor (6000.0, 6000.1, ...), so new ranges span one minor
			VersionRange range = new VersionRange() { Name = UnityVersionUtils.VersionToString( from ), From = from, To = from + UnityVersionUtils.MinorStep( from ) };
			m_ranges.Insert( index, range );
			// Range ports are laid out in order right after the Default port
			ForbidSamplers( AddInputPortAt( index + 1, WirePortDataType.FLOAT, false, range.Name ) );
		}

		private void RemoveRange( int index )
		{
			DeleteInputPortByArrayIdx( index + 1 );
			m_ranges.RemoveAt( index );
		}

		private int DrawVersionField( Rect rect, GUIContent label, string controlName, int version, int emptyVersion, bool forceInvalid )
		{
			version = m_versionField.Draw( rect, label, controlName, version, true, emptyVersion, forceInvalid, out bool changed );
			if ( changed )
			{
				UpdateConnections();
				PreviewIsDirty = true;
			}
			return version;
		}

		private int GetActivePortArrayId()
		{
			int unityVersion = UnityVersionUtils.EditorVersion;
			for ( int i = 0; i < m_ranges.Count; i++ )
			{
				if ( unityVersion >= m_ranges[ i ].From && unityVersion < m_ranges[ i ].To )
				{
					return i + 1; // array id 0 is the Default port
				}
			}
			return 0;
		}

		private void UpdateConnections()
		{
			// All branches end up in the generated code, so every port shares the highest
			// priority connected type instead of following one active port
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

			int activePortIndex = GetActivePortArrayId();
			string activeName = ( activePortIndex > 0 ) ? m_ranges[ activePortIndex - 1 ].Name : DefaultPortName;
			SetAdditonalTitleText( string.Format( Constants.SubTitleCurrentFormatStr, activeName ) );
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
										range.Name = UnityVersionUtils.Major( range.From ) + ".x";
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
								// A range with both sides open-ended would match everything, which is what
								// the Default port is for; flag it on the From field
								bool bothOpenEnded = ( range.From == OpenEndedFromVersion ) && ( range.To == OpenEndedVersion );
								range.From = DrawVersionField( fromRect, new GUIContent( "From", FromTooltip ), "ASEUnityRange" + UniqueId + "From" + index, range.From, OpenEndedFromVersion, bothOpenEnded );
								range.To = DrawVersionField( toRect, new GUIContent( "To", ToTooltip ), "ASEUnityRange" + UniqueId + "To" + index, range.To, OpenEndedVersion, false );

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

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			if ( ReportStructMix( 0, m_inputPorts.Count - 1 ) )
			{
				return m_outputPorts[ 0 ].ErrorValue;
			}

			if ( m_ranges.Count == 0 )
			{
				return m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			}

			// @diogo: a struct names itself on the port, since the wire type alone cannot spell it
			bool isStruct = m_outputPorts[ 0 ].DataType == WirePortDataType.STRUCT;
			if ( isStruct && string.IsNullOrEmpty( m_outputPorts[ 0 ].StructName ) )
			{
				UIUtils.ShowMessage( UniqueId, NamelessStructError, MessageSeverity.Error );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			string outType = isStruct ? m_outputPorts[ 0 ].StructName : UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_outputPorts[ 0 ].DataType );

			BranchArmAnalysis defaultArm = BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ 0 ], ref dataCollector, divergentFlow: false );
			BranchArmAnalysis[] rangeArms = new BranchArmAnalysis[ m_ranges.Count ];
			bool anyScoped = defaultArm.Scoped;
			for ( int i = 0; i < m_ranges.Count; i++ )
			{
				rangeArms[ i ] = BranchScopeUtils.AnalyzeArm( this, GetInputPortByArrayId( i + 1 ), ref dataCollector, divergentFlow: false );
				anyScoped = anyScoped || rangeArms[ i ].Scoped;
			}

			// @diogo: a flat branch still generates up front so its dependencies land outside the
			// conditional block; a scoped one only hoists what it shares with the rest of the graph (#674)
			string defaultCode = null;
			if ( defaultArm.Scoped )
			{
				BranchScopeUtils.PreGenerateFrontier( defaultArm, ref dataCollector );
			}
			else
			{
				defaultCode = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			}

			string[] rangeCode = new string[ m_ranges.Count ];
			for ( int i = 0; i < m_ranges.Count; i++ )
			{
				if ( rangeArms[ i ].Scoped )
				{
					BranchScopeUtils.PreGenerateFrontier( rangeArms[ i ], ref dataCollector );
				}
				else
				{
					rangeCode[ i ] = GetInputPortByArrayId( i + 1 ).GeneratePortInstructions( ref dataCollector );
				}
			}

			string varName = "unityVersionSwitch" + OutputId;
			if ( anyScoped )
			{
				dataCollector.AddLocalVariable( UniqueId, outType + " " + varName + " = ( " + outType + " )0;", true );
			}

			// @diogo: the default arm runs when no range matched, which is the conjunction of their negations (#443)
			string defaultCondition = string.Empty;
			for ( int i = 0; i < m_ranges.Count; i++ )
			{
				string condition = string.Empty;
				if ( m_ranges[ i ].From != OpenEndedFromVersion )
				{
					condition = "UNITY_VERSION >= " + m_ranges[ i ].From;
				}
				if ( m_ranges[ i ].To != OpenEndedVersion )
				{
					condition += ( ( condition.Length > 0 ) ? " && " : string.Empty ) + "UNITY_VERSION < " + m_ranges[ i ].To;
				}
				// Both sides open-ended is flagged invalid in the UI but must still generate a
				// valid, always-true condition
				if ( condition.Length == 0 )
				{
					condition = "UNITY_VERSION >= 0";
				}
				defaultCondition += ( ( i == 0 ) ? string.Empty : " && " ) + "!( " + condition + " )";
				dataCollector.AddLocalVariable( UniqueId, ( ( i == 0 ) ? "#if ( " : "#elif ( " ) + condition + " )", true );
				if ( anyScoped )
				{
					BranchScopeUtils.EmitBracedArm( UniqueId, rangeArms[ i ], GetInputPortByArrayId( i + 1 ), varName, rangeCode[ i ], "( " + condition + " )", ref dataCollector );
				}
				else
				{
					dataCollector.AddLocalVariable( UniqueId, "\t" + outType + " " + varName + " = " + rangeCode[ i ] + ";", true );
				}
			}
			dataCollector.AddLocalVariable( UniqueId, "#else", true );
			if ( anyScoped )
			{
				BranchScopeUtils.EmitBracedArm( UniqueId, defaultArm, m_inputPorts[ 0 ], varName, defaultCode, defaultCondition, ref dataCollector );
			}
			else
			{
				dataCollector.AddLocalVariable( UniqueId, "\t" + outType + " " + varName + " = " + defaultCode + ";", true );
			}
			dataCollector.AddLocalVariable( UniqueId, "#endif", true );

			m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
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
				ForbidSamplers( AddInputPort( WirePortDataType.FLOAT, false, range.Name ) );
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );

			m_ranges.Clear();

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

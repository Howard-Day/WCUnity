// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Switch by Index", "Logical Operators", "Selects one of its input ports based on an integer index", tags: "select int option case mux branch" )]
	public sealed class SwitchByIndexNode : ParentNode
	{
		private const int IndexPortIdx = 0;
		private const int DefaultPortIdx = 1;
		private const int FirstValuePortIdx = 2;

		private const int MinAmountInputs = 2;
		private const int MaxAmountInputs = 8;

		private const string IndexPortName = "Index";
		private const string DefaultPortName = "Default";
		private const string CasePortName = "Case ";
		private const string AmountStr = "Case Amount";
		private const string DynamicBranchStr = "Dynamic Branching";
		private const string UnityBranchStr = "UNITY_BRANCH ";
		// @diogo: a blank label still reserves the prefix column; string.Empty makes it stretch full width
		private const string BlankLabelStr = " ";
		private const string DefaultInfo = "Outputs the input whose case value matches Index, or the Default input when none of them does.";
		private const string DuplicateInfo = "Repeated case values are ignored past their first input.";
		private const string CustomTypeError = "Switch by Index node does not support custom types on its inputs.";

		private static readonly int m_amountId = Shader.PropertyToID( "_Amount" );
		private static readonly int m_casesId = Shader.PropertyToID( "_Cases" );

		[SerializeField]
		private int m_amountInputs = MinAmountInputs;

		[SerializeField]
		private int[] m_caseValues = { 0, 1, 2, 3, 4, 5, 6, 7 };

		[SerializeField]
		private bool m_dynamicBranch = false;

		private float[] m_previewCases = new float[ MaxAmountInputs ];

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			AddInputPort( WirePortDataType.INT, false, IndexPortName );
			m_inputPorts[ IndexPortIdx ].AddPortRestrictions( WirePortDataType.FLOAT, WirePortDataType.INT );

			AddInputPort( WirePortDataType.FLOAT, false, DefaultPortName );

			for ( int i = 0; i < MaxAmountInputs; i++ )
			{
				AddInputPort( WirePortDataType.FLOAT, false, CasePortName + i );
			}

			// @diogo: a texture object is bound, not assigned, so no arm can carry one through a runtime switch
			for ( int i = DefaultPortIdx; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].AcceptAllTypes();
				m_inputPorts[ i ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
					WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
			}

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );

			m_useInternalPortData = true;
			m_previewShaderGUID = "8c9e885e97a4430ebd6447df0aaaf92f";

			UpdatePorts();
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			// @diogo: port 0 is the index; Default and every case are arms
			return ( inputIndex > 0 ) ? received : StructFieldMask.All;
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnections();
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdateConnections();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateConnections();
		}

		private void UpdatePorts()
		{
			for ( int i = 0; i < MaxAmountInputs; i++ )
			{
				m_inputPorts[ FirstValuePortIdx + i ].Visible = ( i < m_amountInputs );
				m_inputPorts[ FirstValuePortIdx + i ].Name = CasePortName + m_caseValues[ i ];
			}

			if ( HasDuplicateCases() )
			{
				ShowTab( NodeMessageType.Warning, DuplicateInfo );
			}
			else
			{
				HideTab();
			}

			m_sizeIsDirty = true;
		}

		// @diogo: a repeated value would emit a duplicate switch label, so only its first input is kept
		private bool IsDuplicateCase( int arm )
		{
			bool result = false;
			for ( int i = 0; i < arm && !result; i++ )
			{
				result = m_caseValues[ i ] == m_caseValues[ arm ];
			}
			return result;
		}

		private bool HasDuplicateCases()
		{
			bool result = false;
			for ( int i = 1; i < m_amountInputs && !result; i++ )
			{
				result = IsDuplicateCase( i );
			}
			return result;
		}

		// @diogo: Index stays INT so a wired float is cast; hidden value ports are kept in sync
		// with the visible ones so raising Amount never exposes a stale type
		private void UpdateConnections()
		{
			WirePortDataType mainType = WirePortDataType.FLOAT;
			string mainStructName = string.Empty;
			int highest = -1;

			for ( int i = DefaultPortIdx; i < m_inputPorts.Count; i++ )
			{
				if ( m_inputPorts[ i ].IsConnected )
				{
					WirePort source = m_inputPorts[ i ].GetOutputConnection();
					if ( UIUtils.GetPriority( source.DataType ) > highest )
					{
						mainType = source.DataType;
						// @diogo: a struct is named by the port, so the name has to ride along with the type
						// or nothing downstream can tell which struct came out of the switch
						mainStructName = source.StructName;
						highest = UIUtils.GetPriority( source.DataType );
					}
				}
			}

			for ( int i = DefaultPortIdx; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].ChangeType( mainType, mainStructName, false );
			}

			m_outputPorts[ 0 ].ChangeType( mainType, mainStructName, false );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, Constants.ParameterLabelStr, DrawParameters );

			NodeUtils.DrawHelpBox( DefaultInfo, MessageType.Info );

			if ( HasDuplicateCases() )
			{
				NodeUtils.DrawHelpBox( DuplicateInfo, MessageType.Warning );
			}
		}

		private void DrawParameters()
		{
			EditorGUI.BeginChangeCheck();
			m_amountInputs = EditorGUILayoutIntSlider( AmountStr, m_amountInputs, MinAmountInputs, MaxAmountInputs );

			for ( int i = 0; i < m_amountInputs; i++ )
			{
				m_caseValues[ i ] = EditorGUILayoutIntField( BlankLabelStr, m_caseValues[ i ] );
			}

			if ( EditorGUI.EndChangeCheck() )
			{
				UpdatePorts();
			}

			m_dynamicBranch = EditorGUILayoutToggle( DynamicBranchStr, m_dynamicBranch );
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			for ( int i = 0; i < MaxAmountInputs; i++ )
			{
				m_previewCases[ i ] = m_caseValues[ i ];
			}

			PreviewMaterial.SetInt( m_amountId, m_amountInputs );
			PreviewMaterial.SetFloatArray( m_casesId, m_previewCases );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			}

			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			if ( ReportStructMix( DefaultPortIdx, m_inputPorts.Count - 1 ) )
			{
				return m_outputPorts[ 0 ].ErrorValue;
			}

			WirePortDataType outputType = m_outputPorts[ 0 ].DataType;

			// @diogo: nothing names a custom type on the wire, so its accumulator cannot be declared. A
			// struct names itself on the port, and is only unnameable if that name failed to arrive
			bool namelessStruct = outputType == WirePortDataType.STRUCT && string.IsNullOrEmpty( m_outputPorts[ 0 ].StructName );
			if ( outputType == WirePortDataType.OBJECT || namelessStruct )
			{
				UIUtils.ShowMessage( UniqueId, CustomTypeError, MessageSeverity.Error );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			string index = m_inputPorts[ IndexPortIdx ].GeneratePortInstructions( ref dataCollector );

			// @diogo: every arm is laid out before any is generated, so each knows upfront if it owns its
			// value; anything it shares with the others resolves before the block opens (#365)
			BranchArmAnalysis[] arms = new BranchArmAnalysis[ m_amountInputs ];
			for ( int i = 0; i < m_amountInputs; i++ )
			{
				arms[ i ] = BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ FirstValuePortIdx + i ], ref dataCollector );
			}
			BranchArmAnalysis defaultArm = BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ DefaultPortIdx ], ref dataCollector );

			// @diogo: the scope carries the same verdict the hazard scan used, or the divergence net misfires (#365)
			bool uniformCondition = BranchScopeUtils.ConditionIsUniform( this );

			string[] values = new string[ m_amountInputs ];
			for ( int i = 0; i < m_amountInputs; i++ )
			{
				if ( arms[ i ].Scoped )
				{
					BranchScopeUtils.PreGenerateFrontier( arms[ i ], ref dataCollector );
				}
				else
				{
					values[ i ] = BranchScopeUtils.PreGenerateFallback( arms[ i ], m_inputPorts[ FirstValuePortIdx + i ], ref dataCollector, outputType, ignoreLocalvar, true );
				}
			}

			string defaultValue = null;
			if ( defaultArm.Scoped )
			{
				BranchScopeUtils.PreGenerateFrontier( defaultArm, ref dataCollector );
			}
			else
			{
				defaultValue = BranchScopeUtils.PreGenerateFallback( defaultArm, m_inputPorts[ DefaultPortIdx ], ref dataCollector, outputType, ignoreLocalvar, true );
			}

			string outType = ( outputType == WirePortDataType.STRUCT ) ? m_outputPorts[ 0 ].StructName : UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, outputType );
			string varName = "switchByIndex" + OutputId;
			string branchAttr = m_dynamicBranch ? UnityBranchStr : string.Empty;

			dataCollector.AddLocalVariable( UniqueId, outType + " " + varName + " = ( " + outType + " )0;", true );
			dataCollector.AddLocalVariable( UniqueId, branchAttr + "switch( " + index + " )", true );
			dataCollector.AddLocalVariable( UniqueId, "{", true );
			for ( int i = 0; i < m_amountInputs; i++ )
			{
				if ( !IsDuplicateCase( i ) )
				{
					dataCollector.AddLocalVariable( UniqueId, "\tcase " + m_caseValues[ i ] + ":", true );
					if ( arms[ i ].Scoped )
					{
						dataCollector.AddLocalVariable( UniqueId, "\t{", true );
						dataCollector.PushBranchScope( uniformCondition );
						values[ i ] = m_inputPorts[ FirstValuePortIdx + i ].GenerateShaderForOutput( ref dataCollector, outputType, ignoreLocalvar, true );
						dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + values[ i ] + ";", true );
						dataCollector.AddLocalVariable( UniqueId, "\tbreak;", true );
						dataCollector.PopBranchScope();
						dataCollector.AddLocalVariable( UniqueId, "\t}", true );
					}
					else
					{
						dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + values[ i ] + ";", true );
						dataCollector.AddLocalVariable( UniqueId, "\tbreak;", true );
					}
				}
			}
			dataCollector.AddLocalVariable( UniqueId, "\tdefault:", true );
			if ( defaultArm.Scoped )
			{
				dataCollector.AddLocalVariable( UniqueId, "\t{", true );
				dataCollector.PushBranchScope( uniformCondition );
				defaultValue = m_inputPorts[ DefaultPortIdx ].GenerateShaderForOutput( ref dataCollector, outputType, ignoreLocalvar, true );
				dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + defaultValue + ";", true );
				dataCollector.AddLocalVariable( UniqueId, "\tbreak;", true );
				dataCollector.PopBranchScope();
				dataCollector.AddLocalVariable( UniqueId, "\t}", true );
			}
			else
			{
				dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + defaultValue + ";", true );
				dataCollector.AddLocalVariable( UniqueId, "\tbreak;", true );
			}
			dataCollector.AddLocalVariable( UniqueId, "}", true );

			m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			// @diogo: metadata is plain text and can carry an amount that indexes past the ports
			m_amountInputs = Mathf.Clamp( Convert.ToInt32( GetCurrentParam( ref nodeParams ) ), MinAmountInputs, MaxAmountInputs );
			// @diogo: dev graphs written while the node still had a Clamp Input toggle carry it in this slot
			if ( m_currentReadParamIdx < nodeParams.Length && bool.TryParse( nodeParams[ m_currentReadParamIdx ], out _ ) )
			{
				GetCurrentParam( ref nodeParams );
			}
			// @diogo: graphs written before case values existed run out of params and keep the defaults
			for ( int i = 0; i < MaxAmountInputs && m_currentReadParamIdx < nodeParams.Length; i++ )
			{
				m_caseValues[ i ] = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			}
			if ( m_currentReadParamIdx < nodeParams.Length )
			{
				m_dynamicBranch = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			UpdatePorts();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_amountInputs );
			for ( int i = 0; i < MaxAmountInputs; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_caseValues[ i ] );
			}
			IOUtils.AddFieldValueToString( ref nodeInfo, m_dynamicBranch );
		}
	}
}

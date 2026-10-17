// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
//
// Custom Node Compare With Range
// Donated by The Four Headed Cat - @fourheadedcat

using UnityEngine;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Compare With Range", "Logical Operators", "Check if A is in the range between Range Min and Range Max. If true return value of True else return value of False", null, KeyCode.None, true, false, null, null, "The Four Headed Cat - @fourheadedcat" )]
	public sealed class TFHCCompareWithRange : DynamicTypeNode
	{
		private const string NamelessStructError = "Compare With Range node cannot declare a struct whose name did not reach its ports.";

		private WirePortDataType m_mainInputType = WirePortDataType.FLOAT;
		private WirePortDataType m_mainOutputType = WirePortDataType.FLOAT;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_inputPorts[ 0 ].Name = "Value";
			m_inputPorts[ 1 ].Name = "Range Min";
			AddInputPort( WirePortDataType.FLOAT, false, "Range Max" );
			AddInputPort( WirePortDataType.FLOAT, false, "True" );
			AddInputPort( WirePortDataType.FLOAT, false, "False" );

			// @diogo: a texture object is bound, not assigned, so no arm survives the ternary this emits. A
			// struct is let into True and False only, which take a branch form for one instead
			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				if ( i >= 3 )
				{
					m_inputPorts[ i ].AcceptAllTypes();
				}
				m_inputPorts[ i ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
					WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
			}

			m_textLabelWidth = 100;
			m_useInternalPortData = true;
			m_previewShaderGUID = "127d114eed178d7409f900134a6c00d1";
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			// @diogo: Value and the range are read whole; True and False are arms
			return ( inputIndex >= 3 ) ? received : StructFieldMask.All;
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			UpdateConnections( portId );
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			UpdateConnections( outputPortId );
		}

		public override void OnInputPortDisconnected( int portId )
		{
			if ( portId < 3 )
			{
				if ( portId > 0 )
				{
					m_inputPorts[ portId ].ChangeType( m_mainInputType, false );
				}
			}
			else
			{
				int otherPortId = ( portId == 3 ) ? 4 : 3;
				if ( m_inputPorts[ otherPortId ].IsConnected )
				{
					m_mainOutputType = m_inputPorts[ otherPortId ].DataType;
					m_inputPorts[ portId ].ChangeType( m_mainOutputType, m_inputPorts[ otherPortId ].StructName, false );
					m_outputPorts[ 0 ].ChangeType( m_mainOutputType, m_inputPorts[ otherPortId ].StructName, false );
				}
				// @diogo: a struct left on the free arms would go on refusing every other struct
				else if ( m_mainOutputType == WirePortDataType.STRUCT )
				{
					m_mainOutputType = WirePortDataType.FLOAT;
					m_inputPorts[ 3 ].ChangeType( m_mainOutputType, string.Empty, false );
					m_inputPorts[ 4 ].ChangeType( m_mainOutputType, string.Empty, false );
					m_outputPorts[ 0 ].ChangeType( m_mainOutputType, string.Empty, false );
				}
			}
		}

		void UpdateConnections( int portId )
		{
			m_inputPorts[ portId ].MatchPortToConnection();
			int otherPortId = 0;
			WirePortDataType otherPortType = WirePortDataType.FLOAT;
			if ( portId < 3 )
			{
				if ( portId == 0 )
				{
					m_mainInputType = m_inputPorts[ 0 ].DataType;
					for ( int i = 1; i < 3; i++ )
					{
						if ( !m_inputPorts[ i ].IsConnected )
						{
							m_inputPorts[ i ].ChangeType( m_mainInputType, false );
						}
					}
				}
			}
			else
			{
				otherPortId = ( portId == 3 ) ? 4 : 3;
				otherPortType = m_inputPorts[ otherPortId ].IsConnected ? m_inputPorts[ otherPortId ].DataType : WirePortDataType.FLOAT;
				m_mainOutputType = UIUtils.GetPriority( m_inputPorts[ portId ].DataType ) > UIUtils.GetPriority( otherPortType ) ? m_inputPorts[ portId ].DataType : otherPortType;

				// @diogo: a struct ranks below every scalar, yet nothing casts to or from one, so a wired
				// struct decides the type outright rather than losing to the float a free arm defaults to
				string structName = string.Empty;
				if ( m_inputPorts[ portId ].DataType == WirePortDataType.STRUCT )
				{
					m_mainOutputType = WirePortDataType.STRUCT;
					structName = m_inputPorts[ portId ].StructName;
				}
				else if ( otherPortType == WirePortDataType.STRUCT )
				{
					m_mainOutputType = WirePortDataType.STRUCT;
					structName = m_inputPorts[ otherPortId ].StructName;
				}

				m_outputPorts[ 0 ].ChangeType( m_mainOutputType, structName, false );

				if ( !m_inputPorts[ otherPortId ].IsConnected )
				{
					m_inputPorts[ otherPortId ].ChangeType( m_mainOutputType, structName, false );
				}
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( ReportStructMix( 3, 4 ) )
			{
				return m_outputPorts[ 0 ].ErrorValue;
			}

			//Conditional Operator ?: has some shenanigans
			//If the first operand is of type bool, one of the following must hold for the second and third operands:
			//Both operands have compatible structure types.
			//Both operands are scalars with numeric or bool type.
			//Both operands are vectors with numeric or bool type, where the two vectors are of the same size, which is less than or equal to four.
			//If the first operand is a packed vector of bool, then the conditional selection is performed on an elementwise basis.Both the second and third operands must be numeric vectors of the same size as the first operand.
			bool isStruct = m_outputPorts[ 0 ].DataType == WirePortDataType.STRUCT;
			WirePortDataType compatibleInputType = m_mainInputType;
			// @diogo: never cast to a struct - the value and range stay numeric whatever the arms carry
			if ( m_mainInputType != WirePortDataType.FLOAT && m_mainInputType != WirePortDataType.INT && m_mainInputType != m_mainOutputType && !isStruct )
			{
				compatibleInputType = m_mainOutputType;
			}

			//Check if VALUE is in range between MIN and MAX. If true return VALUE IF TRUE else VALUE IF FALSE"
			string a = m_inputPorts[ 0 ].GenerateShaderForOutput( ref dataCollector, compatibleInputType, ignoreLocalvar, true );
			string b = m_inputPorts[ 1 ].GenerateShaderForOutput( ref dataCollector, compatibleInputType, ignoreLocalvar, true );
			string c = m_inputPorts[ 2 ].GenerateShaderForOutput( ref dataCollector, compatibleInputType, ignoreLocalvar, true );
			string d = m_inputPorts[ 3 ].GenerateShaderForOutput( ref dataCollector, m_mainOutputType, ignoreLocalvar, true );
			string e = m_inputPorts[ 4 ].GenerateShaderForOutput( ref dataCollector, m_mainOutputType, ignoreLocalvar, true );

			// @diogo: HLSL has no ternary on a struct, so one is selected by if/else into a local instead,
			// under the same condition the ternary below tests
			if ( isStruct )
			{
				if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				{
					return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
				}

				string structName = m_outputPorts[ 0 ].StructName;
				if ( string.IsNullOrEmpty( structName ) )
				{
					UIUtils.ShowMessage( UniqueId, NamelessStructError, MessageSeverity.Error );
					return m_outputPorts[ 0 ].ErrorValue;
				}

				string varName = "compareRangeResult" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, structName + " " + varName + " = " + e + ";", true );
				dataCollector.AddLocalVariable( UniqueId, string.Format( "if( {0} >= {1} && {0} <= {2} )", a, b, c ), true );
				dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + d + ";", true );
				m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );
				return varName;
			}

			string strout = "(( " + a + " >= " + b + " && " + a + " <= " + c + " ) ? " + d + " :  " + e + " )";
			//Debug.Log(strout);
			return CreateOutputLocalVariable( 0, strout, ref dataCollector );
		}
	}
}

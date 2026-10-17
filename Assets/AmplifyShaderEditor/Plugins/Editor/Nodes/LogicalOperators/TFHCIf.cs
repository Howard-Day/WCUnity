// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
//
// Custom Node If
// Donated by The Four Headed Cat - @fourheadedcat

using UnityEngine;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "If [Community]", "Logical Operators", "Compare A with B. If A is greater than B output the value of A > B port. If A is equal to B output the value of A == B port. If A is lower than B output the value of A < B port. Equal Threshold parameter will be used to check A == B adding and subtracting this value to A.", null, KeyCode.None, true, false, null, null, "The Four Headed Cat - @fourheadedcat" )]
	public sealed class TFHCIf : ParentNode
	{
		private const string NamelessStructError = "If [Community] node cannot declare a struct whose name did not reach its ports.";

		private WirePortDataType m_inputMainDataType = WirePortDataType.FLOAT;
		private WirePortDataType m_outputMainDataType = WirePortDataType.FLOAT;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, "A" );
			AddInputPort( WirePortDataType.FLOAT, false, "B" );
			AddInputPort( WirePortDataType.FLOAT, false, "A > B" );
			AddInputPort( WirePortDataType.FLOAT, false, "A == B" );
			AddInputPort( WirePortDataType.FLOAT, false, "A < B" );
			AddInputPort( WirePortDataType.FLOAT, false, "Equal Threshold" );

			// @diogo: a texture object is bound, not assigned, so no arm survives the ternary this emits. A
			// struct is let into the three result arms only, which take a branch form for one instead
			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				if ( i >= 2 && i <= 4 )
				{
					m_inputPorts[ i ].AcceptAllTypes();
				}
				m_inputPorts[ i ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
					WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
			}

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_textLabelWidth = 110;
			m_useInternalPortData = true;
			m_previewShaderGUID = "5c7bc7e3cab81da499e4864ace0d86c5";
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			// @diogo: A, B and Equal Threshold are read whole; ports 2 to 4 are arms
			return ( inputIndex >= 2 && inputIndex <= 4 ) ? received : StructFieldMask.All;
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdateConnection( inputPortId );
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnection( portId );
		}

		public override void OnInputPortDisconnected( int portId )
		{
			UpdateConnection( portId );
		}

		void TestMainInputDataType()
		{
			WirePortDataType newType = WirePortDataType.FLOAT;
			if( m_inputPorts[ 0 ].IsConnected && UIUtils.GetPriority( m_inputPorts[ 0 ].DataType ) > UIUtils.GetPriority( newType ) )
			{
				newType = m_inputPorts[ 0 ].DataType;
			}

			if( m_inputPorts[ 1 ].IsConnected && ( UIUtils.GetPriority( m_inputPorts[ 1 ].DataType ) > UIUtils.GetPriority( newType ) ) )
			{
				newType = m_inputPorts[ 1 ].DataType;
			}

			if( m_inputPorts[ 5 ].IsConnected && ( UIUtils.GetPriority( m_inputPorts[ 5 ].DataType ) > UIUtils.GetPriority( newType ) ) )
			{
				newType = m_inputPorts[ 5 ].DataType;
			}

			m_inputMainDataType = newType;
		}

		void TestMainOutputDataType()
		{
			WirePortDataType newType = WirePortDataType.FLOAT;
			string structName = string.Empty;
			for( int i = 2; i < 5; i++ )
			{
				// @diogo: a struct ranks below every scalar, yet nothing casts to or from one, so a wired
				// struct decides the type outright rather than losing to the float this defaults to
				if ( m_inputPorts[ i ].IsConnected && newType != WirePortDataType.STRUCT &&
					( m_inputPorts[ i ].DataType == WirePortDataType.STRUCT || UIUtils.GetPriority( m_inputPorts[ i ].DataType ) > UIUtils.GetPriority( newType ) ) )
				{
					newType = m_inputPorts[ i ].DataType;
					structName = m_inputPorts[ i ].StructName;
				}
			}

			// @diogo: one struct becoming another keeps the wire type, so the name is compared too
			if ( newType != m_outputMainDataType || structName != m_outputPorts[ 0 ].StructName )
			{
				m_outputMainDataType = newType;
				m_outputPorts[ 0 ].ChangeType( m_outputMainDataType, structName, false );
			}

			// @diogo: a free arm carries the struct's name too, so a second one can only be wired the same
			// struct, and drops it again once no struct is left
			for ( int i = 2; i < 5; i++ )
			{
				if ( !m_inputPorts[ i ].IsConnected && ( newType == WirePortDataType.STRUCT || m_inputPorts[ i ].DataType == WirePortDataType.STRUCT ) )
				{
					m_inputPorts[ i ].ChangeType( ( newType == WirePortDataType.STRUCT ) ? WirePortDataType.STRUCT : WirePortDataType.FLOAT, structName, false );
				}
			}
		}

		public void UpdateConnection( int portId )
		{
			m_inputPorts[ portId ].MatchPortToConnection();
			switch( portId )
			{
				case 0:
				case 1:
				case 5:
				{
					TestMainInputDataType();
				}
				break;
				case 2:
				case 3:
				case 4:
				{
					TestMainOutputDataType();
				}
				break;
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( ReportStructMix( 2, 4 ) )
			{
				return m_outputPorts[ 0 ].ErrorValue;
			}

			string a = m_inputPorts[ 0 ].GenerateShaderForOutput( ref dataCollector, m_inputMainDataType, ignoreLocalvar, true );
			string b = m_inputPorts[ 1 ].GenerateShaderForOutput( ref dataCollector, m_inputMainDataType, ignoreLocalvar, true );
			string r1 = m_inputPorts[ 2 ].GenerateShaderForOutput( ref dataCollector, m_outputMainDataType, ignoreLocalvar, true );
			string r2 = m_inputPorts[ 3 ].GenerateShaderForOutput( ref dataCollector, m_outputMainDataType, ignoreLocalvar, true );
			string r3 = m_inputPorts[ 4 ].GenerateShaderForOutput( ref dataCollector, m_outputMainDataType, ignoreLocalvar, true );
			string tr = m_inputPorts[ 5 ].GenerateShaderForOutput( ref dataCollector, m_inputMainDataType, ignoreLocalvar, true );

			// @diogo: HLSL has no ternary on a struct, so one is selected by if/else into a local instead,
			// under the same conditions the ternary below tests
			if ( m_outputPorts[ 0 ].DataType == WirePortDataType.STRUCT )
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

				string varName = "ifResult" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, structName + " " + varName + " = " + r3 + ";", true );
				dataCollector.AddLocalVariable( UniqueId, string.Format( "if( {0} - {1} > {2} )", a, tr, b ), true );
				dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + r1 + ";", true );
				dataCollector.AddLocalVariable( UniqueId, string.Format( "else if( {0} - {1} <= {2} && {0} + {1} >= {2} )", a, tr, b ), true );
				dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + r2 + ";", true );
				m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );
				return varName;
			}

			// No Equal Threshold parameter
			//(a > b ? r1 : a == b ? r2 : r3 )      
			//string strout = " ( " + a + " > " + b  + " ? " + r1 + " : " + a + " == " + b + " ? " + r2 + " : " +  r3 + " ) ";

			// With Equal Threshold parameter
			// ( a - tr > b ? r1 : a - tr <= b && a + tr >= b ? r2 : r3 )
			string strout = " ( " + a + " - " + tr + " > " + b + " ? " + r1 + " : " + a + " - " + tr + " <= " + b + " && " + a + " + " + tr + " >= " + b + " ? " + r2 + " : " + r3 + " ) ";

			//Debug.Log( strout );
			return CreateOutputLocalVariable( 0, strout, ref dataCollector );
		}
	}
}

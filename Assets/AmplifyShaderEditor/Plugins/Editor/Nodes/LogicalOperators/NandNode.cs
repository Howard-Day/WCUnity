// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Nand", "Logical Operators", "Returns 1 if both the inputs A and B are 0.", tags: "not and" )]
	public sealed class NandNode : ParentNode
	{
		[UnityEngine.SerializeField]
		private WirePortDataType m_mainDataType = WirePortDataType.FLOAT;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, "A" );
			AddInputPort( WirePortDataType.FLOAT, false, "B" );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_useInternalPortData = true;
			m_previewShaderGUID = "04fb2bacb454a424d8cfeff7d95cca52";
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnection( portId );
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdateConnection( inputPortId );
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateConnection( portId );
		}

		void UpdateConnection( int portId )
		{
			WirePortDataType type1 = WirePortDataType.FLOAT;
			if ( m_inputPorts[ 0 ].IsConnected )
				type1 = m_inputPorts[ 0 ].GetOutputConnection( 0 ).DataType;

			WirePortDataType type2 = WirePortDataType.FLOAT;
			if ( m_inputPorts[ 1 ].IsConnected )
				type2 = m_inputPorts[ 1 ].GetOutputConnection( 0 ).DataType;
			
			m_mainDataType = UIUtils.GetPriority( type1 ) > UIUtils.GetPriority( type2 ) ? type1 : type2;

			if ( !m_inputPorts[ 0 ].IsConnected && !m_inputPorts[ 1 ].IsConnected && m_inputPorts[ 2 ].IsConnected )
				m_mainDataType = m_inputPorts[ 2 ].GetOutputConnection( 0 ).DataType;

			m_inputPorts[ 0 ].ChangeType( m_mainDataType, false );
			m_inputPorts[ 1 ].ChangeType( m_mainDataType, false );
			m_outputPorts[ 0 ].ChangeType( m_mainDataType, false );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			var A = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			var B = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
			var outputType = UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_outputPorts[ 0 ].DataType );
			
			string result = string.Format( "( ( {0} )( !{1} && !{2} ) )", outputType, A, B );
			return CreateOutputLocalVariable( 0, result, ref dataCollector );
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "And", "Logical Operators", "Returns 1 if both the inputs A and B are 1.", tags: "&& logical" )]
	public sealed class AndNode : ParentNode
	{
		private const int UpgradePortOffset = 2;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, "A" );
			AddInputPort( WirePortDataType.FLOAT, false, "B" );

			// @diogo: hidden ports are conversion targets for the old "And.asset" SF's A/B inputs;
			// they hand their connection over to the matching visible port above.
			AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue ).Visible = false;
			AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue ).Visible = false;

			// @diogo: a texture object is bound, not assigned, so it cannot be an operand of the and
			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
					WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
			}

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_useInternalPortData = true;
			m_previewShaderGUID = "b1edfc0d2d5e4aa19f256a68c95bf044";
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );

			if ( portId >= UpgradePortOffset )
			{
				// @diogo: SF upgrade path - forward the connection to the matching visible port.
				InputPort upgradePort = m_inputPorts[ portId ];
				m_inputPorts[ portId - UpgradePortOffset ].ConnectTo( otherNodeId, otherPortId, upgradePort.GetConnection( 0 ).DataType, false );
			}

			UpdateConnection();
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdateConnection();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateConnection();
		}

		private void UpdateConnection()
		{
			WirePortDataType type1 = WirePortDataType.FLOAT;
			if ( m_inputPorts[ 0 ].IsConnected )
			{
				type1 = m_inputPorts[ 0 ].GetOutputConnection( 0 ).DataType;
			}

			WirePortDataType type2 = WirePortDataType.FLOAT;
			if ( m_inputPorts[ 1 ].IsConnected )
			{
				type2 = m_inputPorts[ 1 ].GetOutputConnection( 0 ).DataType;
			}

			WirePortDataType mainDataType = UIUtils.GetPriority( type1 ) > UIUtils.GetPriority( type2 ) ? type1 : type2;

			m_inputPorts[ 0 ].ChangeType( mainDataType, false );
			m_inputPorts[ 1 ].ChangeType( mainDataType, false );
			m_outputPorts[ 0 ].ChangeType( mainDataType, false );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			}

			string A = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string B = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
			string outputType = UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_outputPorts[ 0 ].DataType );

			string result = string.Format( "( ( {0} )( {1} && {2} ) )", outputType, A, B );
			return CreateOutputLocalVariable( 0, result, ref dataCollector );
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Square", "Math Operators", "Returns the square of the input (A * A).", tags: "sqr pow2" )]
	public sealed class SquareNode : ParentNode
	{
		private InputPort m_mainInputPort;
		private InputPort m_upgradeInputPort;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_mainInputPort = AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );

			// @diogo:
			// This invisible additional port is being added because this node was created as a conversion target from "Square.asset" SF.
			// It allows the connection to work correctly between the old SF and this node.
			m_upgradeInputPort = AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue, uniquePortId: 2 );
			m_upgradeInputPort.Visible = false;

			m_useInternalPortData = true;
			m_previewShaderGUID = "5fb3507be6db4164a8dbb52901596a7c";
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );

			if ( portId == 2 )
			{
				// @diogo: this is the SF upgrade path
				m_mainInputPort.ConnectTo( otherNodeId, otherPortId, m_upgradeInputPort.GetConnection( 0 ).DataType, false );
			}

			m_mainInputPort.MatchPortToConnection();
			m_outputPorts[ 0 ].ChangeType( m_mainInputPort.DataType, false );
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			m_mainInputPort.MatchPortToConnection();
			m_outputPorts[ 0 ].ChangeType( m_mainInputPort.DataType, false );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			}

			string input = m_mainInputPort.GeneratePortInstructions( ref dataCollector );
			string result = string.Format( "( {0} * {0} )", input );
			return CreateOutputLocalVariable( 0, result, ref dataCollector );
		}
	}
}

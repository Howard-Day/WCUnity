// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;

namespace AmplifyShaderEditor
{
	// Reference:
	// https://www.shadertoy.com/view/4djSRW

	[Serializable]
	[NodeAttributes( "Hash 22", "Math Operators", "Portable hash function based on \"Hash without Sine\" shadertoy by Dave Hoskins.\n\nThis hash function guarantees deterministic results across all hardware.\n\nThe 22 corresponds to float2 input and float2 output.", tags: "hash22 22 h22" )]
	public sealed class Hash22Node : ParentNode
	{
		private const string FunctionHeader = "Hash22( {0} )";
		private string m_functionBody;

		private InputPort m_mainInputPort;
		private InputPort m_upgradeInputPort;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			IOUtils.AddFunctionHeader( ref m_functionBody, "float2 Hash22( float2 p )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float3 p3 = frac( p.xyx * float3( 0.1031, 0.1030, 0.0973 ) );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p3 += dot( p3, p3.yzx + 33.33 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "return frac( ( p3.xx + p3.yz ) * p3.zy );" );
			IOUtils.CloseFunctionBody( ref m_functionBody );

			m_mainInputPort = AddInputPort( WirePortDataType.FLOAT2, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT2, Constants.EmptyPortValue );

			// @diogo:
			// Note that this invisible additional port is being added because this node was created as a conversion target from "Hash 22.asset" SF.
			// This secondary port allows the connections to work correctly betweeen the old SF and the this node.
			m_upgradeInputPort = AddInputPort( WirePortDataType.FLOAT2, false, Constants.EmptyPortValue );
			m_upgradeInputPort.Visible = false;

			m_textLabelWidth = 50;
			m_useInternalPortData = true;
			m_autoWrapProperties = false;

			m_previewShaderGUID = "5beb527743214864a32c596b882299d9";
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );

			if ( portId == 1 )
			{
				// @diogo: If this connection happens, we're running an upgrade path: Connect to port 0 instead.
				m_mainInputPort.ConnectTo( otherNodeId, otherPortId );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			string input = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );

			string result = dataCollector.AddFunctions( FunctionHeader, m_functionBody, input );

			return CreateOutputLocalVariable( 0, result, ref dataCollector );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			var attr = ( NodeAttributes )Attribute.GetCustomAttribute( typeof( Hash22Node ), typeof( NodeAttributes ) );

			EditorGUILayout.HelpBox( attr.Description, MessageType.Info );
		}
	}
}

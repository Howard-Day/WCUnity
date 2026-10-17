// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;

namespace AmplifyShaderEditor
{
	// Reference:
	// https://www.shadertoy.com/view/4djSRW

	[Serializable]
	[NodeAttributes( "Hash 44", "Math Operators", "Portable hash function based on \"Hash without Sine\" shadertoy by Dave Hoskins.\n\nThis hash function guarantees deterministic results across all hardware.\n\nThe 44 corresponds to float4 input and float4 output.", tags: "hash44 44 h44" )]
	public sealed class Hash44Node : ParentNode
	{
		private const string FunctionHeader = "Hash44( {0} )";
		private string m_functionBody;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			IOUtils.AddFunctionHeader( ref m_functionBody, "float4 Hash44( float4 p4 )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p4 = frac( p4 * float4( 0.1031, 0.1030, 0.0973, 0.1099 ) );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p4 += dot( p4, p4.wzxy + 33.33 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "return frac( ( p4.xxyz + p4.yzzw ) * p4.zywx );" );
			IOUtils.CloseFunctionBody( ref m_functionBody );

			AddInputPort( WirePortDataType.FLOAT4, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT4, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_useInternalPortData = true;
			m_autoWrapProperties = false;

			m_previewShaderGUID = "12c2448960944d47a962bfcb9e4103e5";
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

			var attr = ( NodeAttributes )Attribute.GetCustomAttribute( typeof( Hash44Node ), typeof( NodeAttributes ) );

			EditorGUILayout.HelpBox( attr.Description, MessageType.Info );
		}
	}
}

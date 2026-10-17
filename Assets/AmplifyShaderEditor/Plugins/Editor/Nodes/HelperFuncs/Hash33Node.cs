// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;

namespace AmplifyShaderEditor
{
	// Reference:
	// https://www.shadertoy.com/view/4djSRW

	[Serializable]
	[NodeAttributes( "Hash 33", "Math Operators", "Portable hash function based on \"Hash without Sine\" shadertoy by Dave Hoskins.\n\nThis hash function guarantees deterministic results across all hardware.\n\nThe 33 corresponds to float3 input and float3 output.", tags: "hash33 33 h33" )]
	public sealed class Hash33Node : ParentNode
	{
		private const string FunctionHeader = "Hash33( {0} )";
		private string m_functionBody;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			IOUtils.AddFunctionHeader( ref m_functionBody, "float3 Hash33( float3 p3 )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p3 = frac( p3 * float3( 0.1031, 0.1030, 0.0973 ) );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p3 += dot( p3, p3.yxz + 33.33 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "return frac( ( p3.xxy + p3.yxx ) * p3.zyx );" );
			IOUtils.CloseFunctionBody( ref m_functionBody );

			AddInputPort( WirePortDataType.FLOAT3, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT3, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_useInternalPortData = true;
			m_autoWrapProperties = false;

			m_previewShaderGUID = "8dd06612f3494765b9bb0058a4831360";
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

			var attr = ( NodeAttributes )Attribute.GetCustomAttribute( typeof( Hash33Node ), typeof( NodeAttributes ) );

			EditorGUILayout.HelpBox( attr.Description, MessageType.Info );
		}
	}
}

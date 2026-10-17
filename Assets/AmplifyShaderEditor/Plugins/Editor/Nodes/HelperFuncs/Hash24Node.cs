// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;

namespace AmplifyShaderEditor
{
	// Reference:
	// https://www.shadertoy.com/view/4djSRW

	[Serializable]
	[NodeAttributes( "Hash 24", "Math Operators", "Portable hash function based on \"Hash without Sine\" shadertoy by Dave Hoskins.\n\nThis hash function guarantees deterministic results across all hardware.\n\nThe 24 corresponds to float2 input and float4 output.", tags: "hash24 24 h24" )]
	public sealed class Hash24Node : ParentNode
	{
		private const string FunctionHeader = "Hash24( {0} )";
		private string m_functionBody;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			IOUtils.AddFunctionHeader( ref m_functionBody, "float4 Hash24( float2 p )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float4 p4 = frac( p.xyxy * float4( 0.1031, 0.1030, 0.0973, 0.1099 ) );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p4 += dot( p4, p4.wzxy + 33.33 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "return frac( ( p4.xxyz + p4.yzzw ) * p4.zywx );" );
			IOUtils.CloseFunctionBody( ref m_functionBody );

			AddInputPort( WirePortDataType.FLOAT2, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT4, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_useInternalPortData = true;
			m_autoWrapProperties = false;

			m_previewShaderGUID = "27982962c83e4d469c7cfc9722843df6";
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

			var attr = ( NodeAttributes )Attribute.GetCustomAttribute( typeof( Hash24Node ), typeof( NodeAttributes ) );

			EditorGUILayout.HelpBox( attr.Description, MessageType.Info );
		}
	}
}

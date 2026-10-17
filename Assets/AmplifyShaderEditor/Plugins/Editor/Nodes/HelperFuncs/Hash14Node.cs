// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;

namespace AmplifyShaderEditor
{
	// Reference:
	// https://www.shadertoy.com/view/4djSRW

	[Serializable]
	[NodeAttributes( "Hash 14", "Math Operators", "Portable hash function based on \"Hash without Sine\" shadertoy by Dave Hoskins.\n\nThis hash function guarantees deterministic results across all hardware.\n\nThe 14 corresponds to float input and float4 output.", tags: "hash14 14 h14" )]
	public sealed class Hash14Node : ParentNode
	{
		private const string FunctionHeader = "Hash14( {0} )";
		private string m_functionBody;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			IOUtils.AddFunctionHeader( ref m_functionBody, "float4 Hash14( float p )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float4 p4 = frac( p.xxxx * float4( 0.1031, 0.1030, 0.0973, 0.1099 ) );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p4 += dot( p4, p4.wzxy + 33.33 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "return frac( ( p4.xxyz + p4.yzzw ) * p4.zywx );" );
			IOUtils.CloseFunctionBody( ref m_functionBody );

			AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT4, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_useInternalPortData = true;
			m_autoWrapProperties = false;

			m_previewShaderGUID = "59badd3adbd445d38b7eb82aeb229812";
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

			var attr = ( NodeAttributes )Attribute.GetCustomAttribute( typeof( Hash14Node ), typeof( NodeAttributes ) );

			EditorGUILayout.HelpBox( attr.Description, MessageType.Info );
		}
	}
}

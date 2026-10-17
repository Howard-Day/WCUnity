// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;

namespace AmplifyShaderEditor
{
	// Reference:
	// https://www.shadertoy.com/view/4djSRW

	[Serializable]
	[NodeAttributes( "Hash 13", "Math Operators", "Portable hash function based on \"Hash without Sine\" shadertoy by Dave Hoskins.\n\nThis hash function guarantees deterministic results across all hardware.\n\nThe 13 corresponds to float input and float3 output.", tags: "hash13 13 h13" )]
	public sealed class Hash13Node : ParentNode
	{
		private const string FunctionHeader = "Hash13( {0} )";
		private string m_functionBody;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			IOUtils.AddFunctionHeader( ref m_functionBody, "float3 Hash13( float p )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float3 p3 = frac( p.xxx * float3( 0.1031, 0.1030, 0.0973 ) );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p3 += dot( p3, p3.yzx + 33.33 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "return frac( ( p3.xxy + p3.yzz ) * p3.zyx );" );
			IOUtils.CloseFunctionBody( ref m_functionBody );

			AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT3, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_useInternalPortData = true;
			m_autoWrapProperties = false;

			m_previewShaderGUID = "e965b1996b0946fd8ddd990d840278cf";
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

			var attr = ( NodeAttributes )Attribute.GetCustomAttribute( typeof( Hash13Node ), typeof( NodeAttributes ) );

			EditorGUILayout.HelpBox( attr.Description, MessageType.Info );
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;

namespace AmplifyShaderEditor
{
	// Reference:
	// https://www.shadertoy.com/view/4djSRW

	[Serializable]
	[NodeAttributes( "Hash 31", "Math Operators", "Portable hash function based on \"Hash without Sine\" shadertoy by Dave Hoskins.\n\nThis hash function guarantees deterministic results across all hardware.\n\nThe 31 corresponds to float3 input and float output.", tags: "hash31 31 h31" )]
	public sealed class Hash31Node : ParentNode
	{
		private const string FunctionHeader = "Hash31( {0} )";
		private string m_functionBody;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			IOUtils.AddFunctionHeader( ref m_functionBody, "float Hash31( float3 p3 )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p3 = frac( p3 * 0.1031 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p3 += dot( p3, p3.zyx + 33.33 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "return frac( ( p3.x + p3.y ) * p3.z );" );
			IOUtils.CloseFunctionBody( ref m_functionBody );

			AddInputPort( WirePortDataType.FLOAT3, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_useInternalPortData = true;
			m_autoWrapProperties = false;

			m_previewShaderGUID = "d33248f2bfaa4443808d5fcf3436a70f";
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

			var attr = ( NodeAttributes )Attribute.GetCustomAttribute( typeof( Hash31Node ), typeof( NodeAttributes ) );

			EditorGUILayout.HelpBox( attr.Description, MessageType.Info );
		}
	}
}

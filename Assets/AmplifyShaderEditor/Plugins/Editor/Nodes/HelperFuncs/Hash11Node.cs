// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;

namespace AmplifyShaderEditor
{
	// Reference:
	// https://www.shadertoy.com/view/4djSRW

	[Serializable]
	[NodeAttributes( "Hash 11", "Math Operators", "Portable hash function based on \"Hash without Sine\" shadertoy by Dave Hoskins.\n\nThis hash function guarantees deterministic results across all hardware.\n\nThe 11 corresponds to float input and float output.", tags: "hash11 11 h11" )]
	public sealed class Hash11Node : ParentNode
	{
		private const string FunctionHeader = "Hash11( {0} )";
		private string m_functionBody;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			IOUtils.AddFunctionHeader( ref m_functionBody, "float Hash11( float p )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p = frac( p * 0.1031 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p *= p + 33.33;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "p *= p + p;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "return frac( p );" );
			IOUtils.CloseFunctionBody( ref m_functionBody );

			AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_useInternalPortData = true;
			m_autoWrapProperties = false;

			m_previewShaderGUID = "f8af8024c3584941bcb180ebeddc20b3";
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

			var attr = ( NodeAttributes )Attribute.GetCustomAttribute( typeof( Hash11Node ), typeof( NodeAttributes ) );

			EditorGUILayout.HelpBox( attr.Description, MessageType.Info );
		}
	}
}

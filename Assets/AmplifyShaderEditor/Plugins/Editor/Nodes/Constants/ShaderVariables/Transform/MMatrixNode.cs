// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

namespace AmplifyShaderEditor
{
	[System.Serializable]
	[NodeAttributes( "Model Matrix", "Matrix Transform", "Current model matrix" )]
	public sealed class MMatrixNode : ConstantShaderVariable
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputProperties( 0, "Out", WirePortDataType.FLOAT4x4 );
			m_value = "unity_ObjectToWorld";
			m_valueHDRP = "GetObjectToWorldMatrix()";
			m_valueURP = "GetObjectToWorldMatrix()";
			m_drawPreview = false;
		}
	}
}

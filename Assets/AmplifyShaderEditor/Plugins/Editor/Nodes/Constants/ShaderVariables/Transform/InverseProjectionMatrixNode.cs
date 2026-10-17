// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

namespace AmplifyShaderEditor
{
	[System.Serializable]
	[NodeAttributes( "Inverse Projection Matrix", "Matrix Transform", "Current inverse projection matrix" )]
	public sealed class InverseProjectionMatrixNode : ConstantShaderVariable
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputProperties( 0, "Out", WirePortDataType.FLOAT4x4 );
			m_value = "UNITY_MATRIX_I_P";
			m_drawPreview = false;
			m_matrixId = 1;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			return GeneratorUtils.GenerateInverseProjection( ref dataCollector, UniqueId, CurrentPrecisionType );
		}
	}
}

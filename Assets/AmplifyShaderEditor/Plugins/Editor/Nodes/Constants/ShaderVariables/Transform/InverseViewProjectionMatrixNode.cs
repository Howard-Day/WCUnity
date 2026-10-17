// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

namespace AmplifyShaderEditor
{
	[System.Serializable]
	[NodeAttributes( "Inverse View Projection Matrix", "Matrix Transform", "Current view inverse projection matrix" )]
	public sealed class InverseViewProjectionMatrixNode : ConstantShaderVariable
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputProperties( 0, "Out", WirePortDataType.FLOAT4x4 );
			m_value = "UNITY_MATRIX_I_VP";
			m_drawPreview = false;
			m_matrixId = 1;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			return GeneratorUtils.GenerateInverseViewProjection( ref dataCollector, UniqueId, CurrentPrecisionType );
		}
	}
}

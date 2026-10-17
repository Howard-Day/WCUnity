// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Camera Position", "Camera And Screen", "Camera position in World Space", tags: "World Space Camera Pos" )]
	public sealed class WorldSpaceCameraPos : ConstantShaderVariable
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputProperties( 0, "XYZ", WirePortDataType.FLOAT3 );
			AddOutputPort( WirePortDataType.FLOAT, "X" );
			AddOutputPort( WirePortDataType.FLOAT, "Y" );
			AddOutputPort( WirePortDataType.FLOAT, "Z" );

			m_value = "_WorldSpaceCameraPos";
			m_previewShaderGUID = "6b0c78411043dd24dac1152c84bb63ba";
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			return GetOutputVectorItem( 0, outputId, base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar ) );
		}
	}
}

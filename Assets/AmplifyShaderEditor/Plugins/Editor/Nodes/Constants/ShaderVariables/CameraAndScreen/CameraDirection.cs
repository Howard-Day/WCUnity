// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Camera Direction", "Camera And Screen", "" )]
	public sealed class CameraDirection : ConstantShaderVariable
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputProperties( 0, "Out", WirePortDataType.FLOAT3 );
			m_value = "-UNITY_MATRIX_V[ 2 ].xyz";
			m_previewShaderGUID = "61e2b74575969cb4c8746f6e41267b12";
		}
	}
}

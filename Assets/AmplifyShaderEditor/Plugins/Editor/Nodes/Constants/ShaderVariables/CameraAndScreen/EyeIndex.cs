// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Eye Index", "Camera And Screen", "Eye Index when stereo rendering is enabled, zero otherwise." )]
	public sealed class EyeIndex : ConstantShaderVariable
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputProperties( 0, "Out", WirePortDataType.INT );
			m_value = "unity_StereoEyeIndex";
			m_previewShaderGUID = "4ddc7827e3370c54b920471c2f47cdc9";
		}
	}
}

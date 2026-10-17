// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "NaN", "Constants And Properties", "NaN constant : asfloat( -1 ), also known as ( 0.0 / 0.0 ) or sqrt( -1 )" )]
	public sealed class NaNNode : ParentNode
	{
		public NaNNode() : base() { }
		public NaNNode( int uniqueId, float x, float y, float width, float height ) : base( uniqueId, x, y, width, height ) { }
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_textLabelWidth = 70;
			m_useInternalPortData = false;
			m_previewShaderGUID = "03fb3d95e1b55ab4cb38baad013bb0a7";
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			return "asfloat( -1 )";
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Object Position", "Object", "Object Position extracted directly from its transform matrix" )]
	public class ObjectPositionNode : ParentNode
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputVectorPorts( WirePortDataType.FLOAT3, "XYZ" );
			m_drawPreviewAsSphere = true;
			m_previewShaderGUID = "e95171394c12a0646b8e9ec9c3f87d56";
			m_textLabelWidth = 180;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			string objectPosition = GeneratorUtils.GenerateObjectPosition( ref dataCollector, UniqueId );
			return GetOutputVectorItem( 0, outputId, objectPosition );
		}
	}
}

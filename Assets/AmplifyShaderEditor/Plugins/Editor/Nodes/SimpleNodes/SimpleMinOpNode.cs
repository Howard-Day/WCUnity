// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Min", "Math Operators", "Minimum of two scalars or each respective component of two vectors" )]
	public sealed class SimpleMinOpNode : DynamicTypeNode
	{
		private int m_cachedPropertyId = -1;

		protected override void CommonInit( int uniqueId )
		{
			m_dynamicRestrictions = new WirePortDataType[]
			{
				WirePortDataType.OBJECT,
				WirePortDataType.FLOAT,
				WirePortDataType.FLOAT2,
				WirePortDataType.FLOAT3,
				WirePortDataType.FLOAT4,
				WirePortDataType.COLOR,
				WirePortDataType.FLOAT2x2,
				WirePortDataType.FLOAT3x3,
				WirePortDataType.FLOAT4x4,
				WirePortDataType.INT
			};

			base.CommonInit( uniqueId );
			m_extensibleInputPorts = true;
			m_previewShaderGUID = "d6033298044f0f14aa9932ca46e58ce6";
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if ( m_cachedPropertyId == -1 )
				m_cachedPropertyId = Shader.PropertyToID( "_Count" );

			PreviewMaterial.SetInt( m_cachedPropertyId, m_inputPorts.Count);
		}

		public override string BuildResults( int outputId,  ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.BuildResults( outputId,  ref dataCollector, ignoreLocalvar );

			string result = m_extensibleInputResults[ 0 ];
			for ( int i = 1; i < m_extensibleInputResults.Count; i++ )
			{
				result = "min( " + result + ", " + m_extensibleInputResults[ i ] + " )";
			}
			return result;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			// @diogo: handle backwards compatibility
			m_extensibleInputPorts = ( UIUtils.CurrentShaderVersion() >= 19906 );
			base.ReadFromString( ref nodeParams );
			m_extensibleInputPorts = true;
		}
	}
}

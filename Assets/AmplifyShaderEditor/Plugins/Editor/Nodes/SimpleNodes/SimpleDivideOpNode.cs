// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Divide", "Math Operators", "Division of two or more values ( A / B / .. )", null, KeyCode.D )]
	public sealed class SimpleDivideOpNode : DynamicTypeNode
	{
		private const string ChainingInfo = "Inputs are divided left to right, as ( A / B / C ). Unlike Multiply, division is neither commutative nor associative, so reordering or regrouping the inputs changes the result.";

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
			m_allowMatrixCheck = true;
			m_previewShaderGUID = "409f06d00d1094849b0834c52791fa72";
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			NodeUtils.DrawHelpBox( ChainingInfo, MessageType.Info );
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if ( m_cachedPropertyId == -1 )
				m_cachedPropertyId = Shader.PropertyToID( "_Count" );

			PreviewMaterial.SetInt( m_cachedPropertyId, m_inputPorts.Count );
		}

		public override string BuildResults( int outputId,  ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			SetExtensibleInputData( outputId, ref dataCollector, ignoreLocalvar );
			string result = "( " + m_extensibleInputResults[ 0 ];
			for ( int i = 1; i < m_extensibleInputResults.Count; i++ )
			{
				result += " / " + m_extensibleInputResults[ i ];
			}
			result += " )";
			return result;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			// @diogo: handle backwards compatibility
			m_extensibleInputPorts = ( UIUtils.CurrentShaderVersion() >= 19913 );
			base.ReadFromString( ref nodeParams );
			m_extensibleInputPorts = true;
		}
	}
}

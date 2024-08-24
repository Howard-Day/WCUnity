// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Exposure", "Lighting", "Get camera exposure value." )]
	public sealed class Exposure : ParentNode
	{		
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			
			bool isHDRP = ( dataCollector.CurrentSRPType == TemplateSRPType.HDRP );
			bool isURP17xOrAbove = ( dataCollector.CurrentSRPType == TemplateSRPType.URP && ASEPackageManagerHelper.CurrentSRPVersion >= ( int )ASESRPBaseline.ASE_SRP_17 );
			string result;

			if ( isHDRP || isURP17xOrAbove )
			{
				result = "GetCurrentExposureMultiplier()";
			}
			else
			{
				result = "( 1.0 )";
			}
			
			return CreateOutputLocalVariable( 0, result, ref dataCollector );
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Exposure", "Lighting", "Get camera exposure value. On URP 6000.7+ it is 1 in passes that don't apply pre-exposure." )]
	public sealed class Exposure : ParentNode
	{
		private readonly string[] URPExposureFunc =
		{
			"float ASEURPExposureMultiplier()\n",
			"{\n",
			"#if ( UNITY_VERSION >= 60070000 )\n",
			"\treturn GetPreExposureMultiplier();\n",
			"#else\n",
			"\treturn GetCurrentExposureMultiplier();\n",
			"#endif\n",
			"}\n"
		};

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
			bool isURP17xOrAbove = ( dataCollector.CurrentSRPType == TemplateSRPType.URP && ASEPackageManagerHelper.CurrentSRPVersion >= ( int )SRPBaseline.ASE_SRP_17_X );
			string result;

			if ( isHDRP )
			{
				result = "GetCurrentExposureMultiplier()";
			}
			else if ( isURP17xOrAbove )
			{
				// @diogo: URP 6000.7 left GetCurrentExposureMultiplier() a stub returning 1; its exposure is GetPreExposureMultiplier()
				dataCollector.AddFunction( URPExposureFunc[ 0 ], URPExposureFunc, false );
				result = "ASEURPExposureMultiplier()";
			}
			else
			{
				result = "( 1.0 )";
			}

			return CreateOutputLocalVariable( 0, result, ref dataCollector );
		}
	}
}

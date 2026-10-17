// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Diffuse And Specular From Metallic", "Miscellaneous", "Computes Diffuse and Specular color values from Base Color and Metallic." )]
	public class DiffuseAndSpecularFromMetallicNode : ParentNode
	{
		private const string FuncHeader = "ASEComputeDiffuseAndFresnel0( {0}, {1}, {2}, {3} )";
		private readonly string[] FuncBody =
		{
			"float3 ASEComputeDiffuseAndFresnel0( float3 baseColor, float metallic, out float3 specularColor, out float oneMinusReflectivity )\n",
			"{\n",
			"\t#ifdef UNITY_COLORSPACE_GAMMA\n",
			"\t\tconst float dielectricF0 = 0.220916301;\n",
			"\t#else\n",
			"\t\tconst float dielectricF0 = 0.04;\n",
			"\t#endif\n",
			"\tspecularColor = lerp( dielectricF0.xxx, baseColor, metallic );\n",
			"\toneMinusReflectivity = 1.0 - metallic;\n",
			"\treturn baseColor * oneMinusReflectivity;\n",
			"}\n"
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, "Base Color" );
			AddInputPort( WirePortDataType.FLOAT, false, "Metallic" );
			AddOutputPort( WirePortDataType.FLOAT3, "Diffuse Color" );
			AddOutputPort( WirePortDataType.FLOAT3, "Specular Color" );
			AddOutputPort( WirePortDataType.FLOAT, "One Minus Reflectivity" );
			m_previewShaderGUID = "c7c4485750948a045b5dab0985896e17";
		}
		
		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{			
			if ( m_outputPorts[ outputId ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
			}

			string baseColor = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string metallic = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );

			string specularColorVar = "specularColor" + OutputId;
			string oneMinusReflectivityVar = "oneMinusReflectivity" + OutputId;
			string diffuseColorVar = "diffuseColor" + OutputId;

			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, specularColorVar, "(0).xxx" );
			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, oneMinusReflectivityVar, "0" );

			dataCollector.AddFunction( FuncBody[ 0 ], FuncBody, false );
			RegisterLocalVariable( 0, string.Format( FuncHeader, baseColor, metallic, specularColorVar, oneMinusReflectivityVar ), ref dataCollector, diffuseColorVar );

			m_outputPorts[ 0 ].SetLocalValue( diffuseColorVar, dataCollector.PortCategory );
			m_outputPorts[ 1 ].SetLocalValue( specularColorVar, dataCollector.PortCategory );
			m_outputPorts[ 2 ].SetLocalValue( oneMinusReflectivityVar, dataCollector.PortCategory );

			return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
		}
	}
}

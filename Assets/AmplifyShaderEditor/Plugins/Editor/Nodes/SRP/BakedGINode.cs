// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "SRP Baked GI", "Miscellaneous", "Gets Baked GI info." )]
	public sealed class BakedGINode : ParentNode
	{
		private const string BakedGIHeaderHDRP = "ASEBakedGI( {0}, {1}, {2}, {3} )";
		private readonly string[] BakedGIBodyHDRP =
		{
			"float3 ASEBakedGI( float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap )\n",
			"{\n",
			"\tfloat3 positionRWS = GetCameraRelativePositionWS( positionWS );\n",
			"\treturn SampleBakedGI( positionRWS, normalWS, uvStaticLightmap, uvDynamicLightmap );\n",
			"}\n"
		};

		private const string BakedGIHeaderHDRP2 = "ASEBakedGI( {0}, {1}, {2}, {3}, {4} )";
		private readonly string[] BakedGIBodyHDRP2 =
		{
			"float3 ASEBakedGI( float3 positionWS, float3 normalWS, uint2 positionSS, float2 uvStaticLightmap, float2 uvDynamicLightmap )\n",
			"{\n",
			"\tfloat3 positionRWS = GetCameraRelativePositionWS( positionWS );\n",
			"\tbool needToIncludeAPV = true;\n",
			"\treturn SampleBakedGI( positionRWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, needToIncludeAPV );\n",
			"}\n"
		};

		private readonly string BakedGIHeaderURP = "ASEBakedGI( {0}, {1}, {2})";
		private readonly string[] BakedGIBodyURP =
		{
			"float3 ASEBakedGI( float3 normalWS, float2 uvStaticLightmap, bool applyScaling )\n",
			"{\n",
			"#ifdef LIGHTMAP_ON\n",
			"\tif( applyScaling )\n",
			"\t\tuvStaticLightmap = uvStaticLightmap * unity_LightmapST.xy + unity_LightmapST.zw;\n",
			"\treturn SampleLightmap( uvStaticLightmap, normalWS );\n",
			"#else\n",
			"\treturn SampleSH(normalWS);\n",
			"#endif\n",
			"}\n"
		};

		private readonly string BakedGIHeaderURP2 = "ASEBakedGI( {0}, {1}, {2}, {3}, {4}, {5})";
		private readonly string[] BakedGIBodyURP2 =
		{
            "float3 ASEBakedGI( float3 positionWS, float3 normalWS, uint2 positionSS, float2 uvStaticLightmap, float2 uvDynamicLightmap, bool applyScaling )\n",
            "{\n",
            "#ifdef LIGHTMAP_ON\n",
                    "\tif (applyScaling)\n",
                    "\t{\n",
                    "\t\tuvStaticLightmap = uvStaticLightmap * unity_LightmapST.xy + unity_LightmapST.zw;\n",
                    "\t\tuvDynamicLightmap = uvDynamicLightmap * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;\n",
                    "\t}\n",
            "#if defined(DYNAMICLIGHTMAP_ON)\n",
                "\treturn SampleLightmap(uvStaticLightmap, uvDynamicLightmap, normalWS);\n",
            "#else\n",
                    "\treturn SampleLightmap(uvStaticLightmap, normalWS);\n",
            "#endif\n",
            "#else\n",
            "#if (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))\n",
                "\tif (_EnableProbeVolumes)\n",
                "\t{\n",
                    "\t\tfloat3 bakeDiffuseLighting;\n",
                    "\t\tEvaluateAdaptiveProbeVolume(positionWS, normalWS, GetWorldSpaceNormalizeViewDir(positionWS), positionSS, bakeDiffuseLighting);\n",
                    "\t\treturn bakeDiffuseLighting;\n",
                "\t}\n",
                "\telse\n",
                    "\treturn SampleSH(normalWS);\n",
            "#else\n",
                    "\treturn SampleSH(normalWS);\n",
            "#endif\n",
            "#endif\n",
            "}\n"
		};

		private const string ApplyScalingStr = "Apply Scaling";

		[SerializeField]
		private bool m_applyScaling = true;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, "World Position" );
			AddInputPort( WirePortDataType.FLOAT3, false, "World Normal" );
			AddInputPort( WirePortDataType.FLOAT2, false, "Static UV" );
			AddInputPort( WirePortDataType.FLOAT2, false, "Dynamic UV" );
			AddInputPort( WirePortDataType.FLOAT4, false, "Screen Position" );
			AddOutputPort( WirePortDataType.FLOAT3, Constants.EmptyPortValue );
			m_textLabelWidth = 95;
			m_autoWrapProperties = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			EditorGUILayout.HelpBox( "Screen Position input must be Normalized (xyz/w), which is the default setting for Screen Position node.", MessageType.Info );

			m_applyScaling = EditorGUILayoutToggle( ApplyScalingStr, m_applyScaling );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( !dataCollector.IsSRP )
			{
				UIUtils.ShowMessage( "Node only intended to use on HDRP and URP rendering pipelines" );
				return GenerateErrorValue();
			}

			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			string positionWS;
			if ( m_inputPorts[ 0 ].IsConnected )
			{
				positionWS = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			}
			else
			{
				positionWS = GeneratorUtils.GenerateWorldPosition( ref dataCollector, UniqueId );
			}

			string normalWS;
			if ( m_inputPorts[ 1 ].IsConnected )
			{
				normalWS = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
			}
			else
			{
				normalWS = GeneratorUtils.GenerateWorldNormal( ref dataCollector, UniqueId );
			}

			string uvStaticLightmap = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
			string uvDynamicLightmap = m_inputPorts[ 3 ].GeneratePortInstructions( ref dataCollector );

			string screenPos;
			string positionSS;

			if ( m_inputPorts[ 4 ].IsConnected )
			{
				screenPos = m_inputPorts[ 4 ].GeneratePortInstructions( ref dataCollector );
				positionSS = string.Format( "( uint2 )( {0}.xy * _ScreenSize.xy )", screenPos );
			}
			else
			{
				screenPos = GeneratorUtils.GenerateScreenPosition( ref dataCollector, UniqueId, CurrentPrecisionType );
				positionSS = string.Format( "( uint2 )( {0}.xy / {0}.w * _ScreenSize.xy )", screenPos );
			}

			
			string localVarName = "bakedGI" + OutputId;

			if ( dataCollector.TemplateDataCollectorInstance.IsHDRP )
			{
				bool unityIsBeta = TemplateHelperFunctions.GetUnityBetaVersion( out int betaVersion );
				int unityVersion = TemplateHelperFunctions.GetUnityVersion();

				if ( ASEPackageManagerHelper.CurrentHDRPBaseline == ASESRPBaseline.ASE_SRP_14 && unityVersion >= 20220326 ||
					 ASEPackageManagerHelper.CurrentHDRPBaseline == ASESRPBaseline.ASE_SRP_16 && unityVersion >= 20230220 ||
					 ASEPackageManagerHelper.CurrentHDRPBaseline == ASESRPBaseline.ASE_SRP_17 && unityIsBeta && betaVersion >= 15 ||
					 ASEPackageManagerHelper.CurrentSRPVersion >= ( int )170003 )
				{
					dataCollector.AddToPragmas( UniqueId, "multi_compile_fragment _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2" );

					dataCollector.AddFunction( BakedGIBodyHDRP2[ 0 ], BakedGIBodyHDRP2, false );
					RegisterLocalVariable( 0, string.Format( BakedGIHeaderHDRP2, positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap ), ref dataCollector, localVarName );
				}
				else
				{
					dataCollector.AddFunction( BakedGIBodyHDRP[ 0 ], BakedGIBodyHDRP, false );
					RegisterLocalVariable( 0, string.Format( BakedGIHeaderHDRP, positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap ), ref dataCollector, localVarName );
				}
			}
			else
			{
				if ( ASEPackageManagerHelper.CurrentURPBaseline >= ASESRPBaseline.ASE_SRP_15 )
				{
					dataCollector.AddToPragmas( UniqueId, "multi_compile_fragment _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2" );

					dataCollector.AddFunction( BakedGIBodyURP2[ 0 ], BakedGIBodyURP2, false );
					RegisterLocalVariable( 0, string.Format( BakedGIHeaderURP2, positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, m_applyScaling ? "true" : "false" ), ref dataCollector, localVarName );
				}
				else
				{
					dataCollector.AddFunction( BakedGIBodyURP[ 0 ], BakedGIBodyURP, false );
					RegisterLocalVariable( 0, string.Format( BakedGIHeaderURP, normalWS, uvStaticLightmap, m_applyScaling ? "true" : "false" ), ref dataCollector, localVarName );
				}
			}
			return localVarName;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_applyScaling = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_applyScaling );
		}
	}
}

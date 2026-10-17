// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Indirect Diffuse Light", "Lighting", "Indirect Lighting" )]
	public sealed class IndirectDiffuseLighting : ParentNode
	{
		[SerializeField]
		private ViewSpace m_normalSpace = ViewSpace.Tangent;
		[SerializeField]
		private bool m_normalize = true;

		private int m_cachedIntensityId = -1;

		private const string FwdBasePragma = "#pragma multi_compile_fwdbase";

		private readonly string IndirectDiffuseHeaderURP14 = "ASEIndirectDiffuse( {0}, {1} )";
		private readonly string[] IndirectDiffuseBodyURP14 =
		{
			"half3 ASEIndirectDiffuse( PackedVaryings input, half3 normalWS, float3 positionWS = 0, half3 viewDirWS = 0 )\n",
			"{\n",
			"#if defined( DYNAMICLIGHTMAP_ON )\n",
			"\treturn SAMPLE_GI( input.lightmapUVOrVertexSH.xy, input.dynamicLightmapUV.xy, 0, normalWS );\n",
			"#elif defined( LIGHTMAP_ON )\n",
			"\treturn SAMPLE_GI( input.lightmapUVOrVertexSH.xy, 0, normalWS );\n",
			"#else\n",
			"\treturn SampleSH( normalWS );\n",
			"#endif\n",
			"}\n"
		};

		private readonly string IndirectDiffuseHeaderURP15 = "ASEIndirectDiffuse( {0}, {1}, {2}, {3} )";
		private readonly string[] IndirectDiffuseBodyURP15 =
		{
			"half3 ASEIndirectDiffuse( PackedVaryings input, half3 normalWS, float3 positionWS, half3 viewDirWS )\n",
			"{\n",
			"#if defined( DYNAMICLIGHTMAP_ON )\n",
			"\treturn SAMPLE_GI( input.lightmapUVOrVertexSH.xy, input.dynamicLightmapUV.xy, 0, normalWS );\n",
			"#elif defined( LIGHTMAP_ON )\n",
			"\treturn SAMPLE_GI( input.lightmapUVOrVertexSH.xy, 0, normalWS );\n",
			"#elif defined( PROBE_VOLUMES_L1 ) || defined( PROBE_VOLUMES_L2 )\n",
			"\treturn SAMPLE_GI( SampleSH( normalWS ), positionWS, normalWS, viewDirWS, input.positionCS.xy );\n",
			"#else\n",
			"\treturn SampleSH( normalWS );\n",
			"#endif\n",
			"}\n"
		};

		private readonly string IndirectDiffuseHeaderURP17 = "ASEIndirectDiffuse( {0}, {1}, {2}, {3} )";
		private readonly string[] IndirectDiffuseBodyURP17 =
		{
			"half3 ASEIndirectDiffuse( PackedVaryings input, half3 normalWS, float3 positionWS, half3 viewDirWS )\n",
			"{\n",
			"#if defined( DYNAMICLIGHTMAP_ON )\n",
			"\treturn SAMPLE_GI( input.lightmapUVOrVertexSH.xy, input.dynamicLightmapUV.xy, 0, normalWS );\n",
			"#elif defined( LIGHTMAP_ON )\n",
			"\treturn SAMPLE_GI( input.lightmapUVOrVertexSH.xy, 0, normalWS );\n",
			"#elif defined( PROBE_VOLUMES_L1 ) || defined( PROBE_VOLUMES_L2 )\n",
			"\treturn SampleProbeVolumePixel( SampleSH( normalWS ), positionWS, normalWS, viewDirWS, input.positionCS.xy );\n",
			"#else\n",
			"\treturn SampleSH( normalWS );\n",
			"#endif\n",
			"}\n"
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, "Normal" );
			AddOutputPort( WirePortDataType.FLOAT3, "RGB" );
			m_inputPorts[ 0 ].Vector3InternalData = Vector3.forward;
			m_autoWrapProperties = true;
			m_errorMessageTypeIsError = NodeMessageType.Warning;
			m_errorMessageTooltip = "This node only returns correct information using a custom light model, otherwise returns 0";
			m_previewShaderGUID = "b45d57fa606c1ea438fe9a2c08426bc7";
			m_drawPreviewAsSphere = true;
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if( m_inputPorts[ 0 ].IsConnected )
			{
				if( m_normalSpace == ViewSpace.Tangent )
					m_previewMaterialPassId = 1;
				else
					m_previewMaterialPassId = 2;
			}
			else
			{
				m_previewMaterialPassId = 0;
			}

			if( m_cachedIntensityId == -1 )
				m_cachedIntensityId = Shader.PropertyToID( "_Intensity" );

			PreviewMaterial.SetFloat( m_cachedIntensityId, RenderSettings.ambientIntensity );
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			base.PropagateNodeData( nodeData, ref dataCollector );
			// This needs to be rechecked
			//if( m_inputPorts[ 0 ].IsConnected )
			dataCollector.DirtyNormal = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			EditorGUI.BeginChangeCheck();
			m_normalSpace = (ViewSpace)EditorGUILayoutEnumPopup( "Normal Space", m_normalSpace );
			if( m_normalSpace != ViewSpace.World || !m_inputPorts[ 0 ].IsConnected )
			{
				m_normalize = EditorGUILayoutToggle("Normalize", m_normalize);
			}
			if( EditorGUI.EndChangeCheck() )
			{
				UpdatePort();
			}
		}

		private void UpdatePort()
		{
			if( m_normalSpace == ViewSpace.World )
				m_inputPorts[ 0 ].ChangeProperties( "World Normal", m_inputPorts[ 0 ].DataType, false );
			else
				m_inputPorts[ 0 ].ChangeProperties( "Normal", m_inputPorts[ 0 ].DataType, false );

			m_sizeIsDirty = true;
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			if( ( ContainerGraph.CurrentStandardSurface != null && ContainerGraph.CurrentStandardSurface.CurrentLightingModel != StandardShaderLightModel.CustomLighting ) )
				m_showErrorMessage = true;
			else
				m_showErrorMessage = false;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			string finalValue = string.Empty;

			if ( dataCollector.IsTemplate && dataCollector.IsFragmentCategory )
			{
				if ( !dataCollector.IsSRP )
				{

					dataCollector.AddToIncludes( UniqueId, Constants.UnityLightingLib );

					var multiPassMasterNode = dataCollector.MasterNode as TemplateMultiPassMasterNode;
					if ( multiPassMasterNode != null &&
						!multiPassMasterNode.Pass.Modules.IncludePragmaContainer.HasPragma( "multi_compile_fwdbase" ) &&
						!multiPassMasterNode.Pass.Modules.IncludePragmaContainer.HasPragma( "multi_compile_fwdadd" ) &&
						!multiPassMasterNode.Pass.Modules.IncludePragmaContainer.HasPragma( "multi_compile_fwdadd_fullshadows" ) )
					{
						dataCollector.AddToDirectives( FwdBasePragma );
					}

					string texcoord1 = string.Empty;
					string texcoord2 = string.Empty;

					if ( dataCollector.TemplateDataCollectorInstance.HasInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES1, false, MasterNodePortCategory.Vertex ) )
						texcoord1 = dataCollector.TemplateDataCollectorInstance.GetInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES1, false, MasterNodePortCategory.Vertex ).VarName;
					else
						texcoord1 = dataCollector.TemplateDataCollectorInstance.RegisterInfoOnSemantic( MasterNodePortCategory.Vertex, TemplateInfoOnSematics.TEXTURE_COORDINATES1, TemplateSemantics.TEXCOORD1, "texcoord1", WirePortDataType.FLOAT4, PrecisionType.Float, false );

					if ( dataCollector.TemplateDataCollectorInstance.HasInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES2, false, MasterNodePortCategory.Vertex ) )
						texcoord2 = dataCollector.TemplateDataCollectorInstance.GetInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES2, false, MasterNodePortCategory.Vertex ).VarName;
					else
						texcoord2 = dataCollector.TemplateDataCollectorInstance.RegisterInfoOnSemantic( MasterNodePortCategory.Vertex, TemplateInfoOnSematics.TEXTURE_COORDINATES2, TemplateSemantics.TEXCOORD2, "texcoord2", WirePortDataType.FLOAT4, PrecisionType.Float, false );

					string vOutName = dataCollector.TemplateDataCollectorInstance.CurrentTemplateData.VertexFunctionData.OutVarName;
					string fInName = dataCollector.TemplateDataCollectorInstance.CurrentTemplateData.FragmentFunctionData.InVarName;
					TemplateVertexData data = dataCollector.TemplateDataCollectorInstance.RequestNewInterpolator( WirePortDataType.FLOAT4, false, "ase_lmap" );

					string varName = "ase_lmap";
					if ( data != null )
						varName = data.VarName;

					dataCollector.AddToVertexLocalVariables( UniqueId, "#ifdef DYNAMICLIGHTMAP_ON //dynlm" );
					dataCollector.AddToVertexLocalVariables( UniqueId, vOutName + "." + varName + ".zw = " + texcoord2 + ".xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;" );
					dataCollector.AddToVertexLocalVariables( UniqueId, "#endif //dynlm" );
					dataCollector.AddToVertexLocalVariables( UniqueId, "#ifdef LIGHTMAP_ON //stalm" );
					dataCollector.AddToVertexLocalVariables( UniqueId, vOutName + "." + varName + ".xy = " + texcoord1 + ".xy * unity_LightmapST.xy + unity_LightmapST.zw;" );
					dataCollector.AddToVertexLocalVariables( UniqueId, "#endif //stalm" );

					TemplateVertexData shdata = dataCollector.TemplateDataCollectorInstance.RequestNewInterpolator( WirePortDataType.FLOAT3, false, "ase_sh" );
					string worldPos = dataCollector.TemplateDataCollectorInstance.GetWorldPos( false, MasterNodePortCategory.Vertex );
					string worldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( PrecisionType.Float, false, MasterNodePortCategory.Vertex );
					//Debug.Log( shdata );
					string shVarName = "ase_sh";
					if ( shdata != null )
						shVarName = shdata.VarName;
					string outSH = vOutName + "." + shVarName + ".xyz";
					dataCollector.AddToVertexLocalVariables( UniqueId, "#ifndef LIGHTMAP_ON //nstalm" );
					dataCollector.AddToVertexLocalVariables( UniqueId, "#if UNITY_SHOULD_SAMPLE_SH //sh" );
					dataCollector.AddToVertexLocalVariables( UniqueId, outSH + " = 0;" );
					dataCollector.AddToVertexLocalVariables( UniqueId, "#ifdef VERTEXLIGHT_ON //vl" );
					dataCollector.AddToVertexLocalVariables( UniqueId, outSH + " += Shade4PointLights (" );
					dataCollector.AddToVertexLocalVariables( UniqueId, "unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0," );
					dataCollector.AddToVertexLocalVariables( UniqueId, "unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb," );
					dataCollector.AddToVertexLocalVariables( UniqueId, "unity_4LightAtten0, " + worldPos + ", " + worldNormal + ");" );
					dataCollector.AddToVertexLocalVariables( UniqueId, "#endif //vl" );
					dataCollector.AddToVertexLocalVariables( UniqueId, outSH + " = ShadeSHPerVertex (" + worldNormal + ", " + outSH + ");" );
					dataCollector.AddToVertexLocalVariables( UniqueId, "#endif //sh" );
					dataCollector.AddToVertexLocalVariables( UniqueId, "#endif //nstalm" );

					string fragWorldNormal = string.Empty;
					if ( m_inputPorts[ 0 ].IsConnected )
					{
						if ( m_normalSpace == ViewSpace.Tangent )
							fragWorldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( UniqueId, CurrentPrecisionType, m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector ), OutputId );
						else
							fragWorldNormal = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					}
					else
					{
						fragWorldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( PrecisionType.Float, false, MasterNodePortCategory.Fragment );
					}

					dataCollector.AddLocalVariable( UniqueId, "UnityGIInput data" + OutputId + ";" );
					dataCollector.AddLocalVariable( UniqueId, "UNITY_INITIALIZE_OUTPUT( UnityGIInput, data" + OutputId + " );" );

					dataCollector.AddLocalVariable( UniqueId, "#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON) //dylm" + OutputId );
					dataCollector.AddLocalVariable( UniqueId, "data" + OutputId + ".lightmapUV = " + fInName + "." + varName + ";" );
					dataCollector.AddLocalVariable( UniqueId, "#endif //dylm" + OutputId );

					dataCollector.AddLocalVariable( UniqueId, "#if UNITY_SHOULD_SAMPLE_SH //fsh" + OutputId );
					dataCollector.AddLocalVariable( UniqueId, "data" + OutputId + ".ambient = " + fInName + "." + shVarName + ";" );
					dataCollector.AddLocalVariable( UniqueId, "#endif //fsh" + OutputId );

					dataCollector.AddToFragmentLocalVariables( UniqueId, "UnityGI gi" + OutputId + " = UnityGI_Base(data" + OutputId + ", 1, " + fragWorldNormal + ");" );

					finalValue = "gi" + OutputId + ".indirect.diffuse";
					m_outputPorts[ 0 ].SetLocalValue( finalValue, dataCollector.PortCategory );
					return finalValue;
				}
				else
				{
					if ( dataCollector.CurrentSRPType == TemplateSRPType.URP )
					{
						string texcoord1 = string.Empty;
						if ( dataCollector.TemplateDataCollectorInstance.HasInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES1, false, MasterNodePortCategory.Vertex ) )
							texcoord1 = dataCollector.TemplateDataCollectorInstance.GetInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES1, false, MasterNodePortCategory.Vertex ).VarName;
						else
							texcoord1 = dataCollector.TemplateDataCollectorInstance.RegisterInfoOnSemantic( MasterNodePortCategory.Vertex, TemplateInfoOnSematics.TEXTURE_COORDINATES1, TemplateSemantics.TEXCOORD1, "texcoord1", WirePortDataType.FLOAT4, PrecisionType.Float, false );

						string vOutName = dataCollector.TemplateDataCollectorInstance.CurrentTemplateData.VertexFunctionData.OutVarName;
						string fInName = dataCollector.TemplateDataCollectorInstance.CurrentTemplateData.FragmentFunctionData.InVarName;

						if ( !dataCollector.TemplateDataCollectorInstance.HasRawInterpolatorOfName( "lightmapUVOrVertexSH" ) )
						{
							string worldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( PrecisionType.Float, false, MasterNodePortCategory.Vertex );
							dataCollector.TemplateDataCollectorInstance.RequestNewInterpolator( WirePortDataType.FLOAT4, false, "lightmapUVOrVertexSH" );

							dataCollector.AddToVertexLocalVariables( UniqueId, "OUTPUT_LIGHTMAP_UV( " + texcoord1 + ", unity_LightmapST, " + vOutName + ".lightmapUVOrVertexSH.xy );", true );

							if ( ASEPackageManagerHelper.PackageSRPVersion >= ( int )SRPBaseline.ASE_SRP_15_X )
							{
								string worldPos = dataCollector.TemplateDataCollectorInstance.GetWorldPos( false, MasterNodePortCategory.Vertex );
								dataCollector.AddToVertexLocalVariables( UniqueId, "#if !defined( OUTPUT_SH4 )", true );
								dataCollector.AddToVertexLocalVariables( UniqueId, "OUTPUT_SH( " + worldPos + ", " + worldNormal + ", GetWorldSpaceNormalizeViewDir( " + worldPos + " ), " + vOutName + ".lightmapUVOrVertexSH.xyz );", true );
								dataCollector.AddToVertexLocalVariables( UniqueId, "#elif UNITY_VERSION > 60000009", true );
								dataCollector.AddToVertexLocalVariables( UniqueId, "OUTPUT_SH4( " + worldPos + ", " + worldNormal + ", GetWorldSpaceNormalizeViewDir( " + worldPos + " ), " + vOutName + ".lightmapUVOrVertexSH.xyz, " + vOutName + ".probeOcclusion );", true );
								dataCollector.AddToVertexLocalVariables( UniqueId, "#else", true );
								dataCollector.AddToVertexLocalVariables( UniqueId, "OUTPUT_SH4( " + worldPos + ", " + worldNormal + ", GetWorldSpaceNormalizeViewDir( " + worldPos + " ), " + vOutName + ".lightmapUVOrVertexSH.xyz );", true );
								dataCollector.AddToVertexLocalVariables( UniqueId, "#endif", true );
							}
							else
							{
								dataCollector.AddToVertexLocalVariables( UniqueId, "OUTPUT_SH( " + worldNormal + ", " + vOutName + ".lightmapUVOrVertexSH.xyz );", true );
							}

							dataCollector.AddToPragmas( UniqueId, "multi_compile _ LIGHTMAP_ON" );
							dataCollector.AddToPragmas( UniqueId, "multi_compile _ DIRLIGHTMAP_COMBINED" );
							dataCollector.AddToPragmas( UniqueId, "multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE" );

							if ( ASEPackageManagerHelper.CurrentSRPVersion >= ( int )SRPBaseline.ASE_SRP_17_X )
							{
								dataCollector.AddToPragmas( UniqueId, "multi_compile _ LIGHTMAP_BICUBIC_SAMPLING", "UNITY_VERSION >= 60010000" );
								dataCollector.AddToPragmas( UniqueId, "multi_compile_fragment _ _REFLECTION_PROBE_ATLAS", "UNITY_VERSION >= 60010000" );
							}
						}

						if ( !dataCollector.TemplateDataCollectorInstance.HasRawInterpolatorOfName( "dynamicLightmapUV" ) )
						{
							string texcoord2 = string.Empty;
							if ( dataCollector.TemplateDataCollectorInstance.HasInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES2, false, MasterNodePortCategory.Vertex ) )
								texcoord2 = dataCollector.TemplateDataCollectorInstance.GetInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES2, false, MasterNodePortCategory.Vertex ).VarName;
							else
								texcoord2 = dataCollector.TemplateDataCollectorInstance.RegisterInfoOnSemantic( MasterNodePortCategory.Vertex, TemplateInfoOnSematics.TEXTURE_COORDINATES2, TemplateSemantics.TEXCOORD2, "texcoord2", WirePortDataType.FLOAT4, PrecisionType.Float, false );

							dataCollector.TemplateDataCollectorInstance.RequestNewInterpolator( WirePortDataType.FLOAT4, false, "dynamicLightmapUV" );

							dataCollector.AddToVertexLocalVariables( UniqueId, "#if defined( DYNAMICLIGHTMAP_ON )", true );
							dataCollector.AddToVertexLocalVariables( UniqueId, string.Format( "{0}.dynamicLightmapUV.xy = {1}.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;", vOutName, texcoord2 ), true );
							dataCollector.AddToVertexLocalVariables( UniqueId, "#endif", true );

							dataCollector.AddToPragmas( UniqueId, "multi_compile _ DYNAMICLIGHTMAP_ON" );
						}

						string fragWorldNormal = string.Empty;
						if ( m_inputPorts[ 0 ].IsConnected )
						{
							if ( m_normalSpace == ViewSpace.Tangent )
								fragWorldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( UniqueId, CurrentPrecisionType, m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector ), OutputId );
							else
								fragWorldNormal = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
						}
						else
						{
							fragWorldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( PrecisionType.Float, false, MasterNodePortCategory.Fragment );
						}

						//SAMPLE_GI
						finalValue = "bakedGI" + OutputId;

						string result;
						if ( ASEPackageManagerHelper.CurrentSRPVersion >= ( int )SRPBaseline.ASE_SRP_15_X )
						{
							string positionWS = dataCollector.TemplateDataCollectorInstance.GetWorldPos();
							string viewDirWS = dataCollector.TemplateDataCollectorInstance.GetViewDir();

							if ( ASEPackageManagerHelper.CurrentSRPVersion >= ( int )SRPBaseline.ASE_SRP_17_X )
							{
								dataCollector.AddFunction( IndirectDiffuseBodyURP17[ 0 ], IndirectDiffuseBodyURP17, false );
								result = string.Format( IndirectDiffuseHeaderURP17, fInName, fragWorldNormal, positionWS, viewDirWS );
							}
							else
							{
								dataCollector.AddFunction( IndirectDiffuseBodyURP15[ 0 ], IndirectDiffuseBodyURP15, false );
								result = string.Format( IndirectDiffuseHeaderURP15, fInName, fragWorldNormal, positionWS, viewDirWS );
							}
						}
						else
						{
							dataCollector.AddFunction( IndirectDiffuseBodyURP14[ 0 ], IndirectDiffuseBodyURP14, false );
							result = string.Format( IndirectDiffuseHeaderURP14, fInName, fragWorldNormal );
						}

						dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, finalValue, result );

						string mainLight = dataCollector.TemplateDataCollectorInstance.GetURPMainLight( UniqueId );
						dataCollector.AddLocalVariable( UniqueId, string.Format( "MixRealtimeAndBakedGI( {0}, {1}, {2}, half4( 0, 0, 0, 0 ) );", mainLight, fragWorldNormal, finalValue ) );

						m_outputPorts[ 0 ].SetLocalValue( finalValue, dataCollector.PortCategory );
						return finalValue;
					}
					else if ( dataCollector.CurrentSRPType == TemplateSRPType.HDRP )
					{
						string texcoord1 = string.Empty;
						string texcoord2 = string.Empty;


						if ( dataCollector.TemplateDataCollectorInstance.HasInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES1, false, MasterNodePortCategory.Vertex ) )
							texcoord1 = dataCollector.TemplateDataCollectorInstance.GetInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES1, false, MasterNodePortCategory.Vertex ).VarName;
						else
							texcoord1 = dataCollector.TemplateDataCollectorInstance.RegisterInfoOnSemantic( MasterNodePortCategory.Vertex, TemplateInfoOnSematics.TEXTURE_COORDINATES1, TemplateSemantics.TEXCOORD1, "texcoord1", WirePortDataType.FLOAT4, PrecisionType.Float, false );

						if ( dataCollector.TemplateDataCollectorInstance.HasInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES2, false, MasterNodePortCategory.Vertex ) )
							texcoord2 = dataCollector.TemplateDataCollectorInstance.GetInfo( TemplateInfoOnSematics.TEXTURE_COORDINATES2, false, MasterNodePortCategory.Vertex ).VarName;
						else
							texcoord2 = dataCollector.TemplateDataCollectorInstance.RegisterInfoOnSemantic( MasterNodePortCategory.Vertex, TemplateInfoOnSematics.TEXTURE_COORDINATES2, TemplateSemantics.TEXCOORD2, "texcoord2", WirePortDataType.FLOAT4, PrecisionType.Float, false );

						dataCollector.TemplateDataCollectorInstance.RequestNewInterpolator( WirePortDataType.FLOAT4, false, "ase_lightmapUVs" );

						string vOutName = dataCollector.TemplateDataCollectorInstance.CurrentTemplateData.VertexFunctionData.OutVarName;
						string fInName = dataCollector.TemplateDataCollectorInstance.CurrentTemplateData.FragmentFunctionData.InVarName;

						dataCollector.AddToVertexLocalVariables( UniqueId, vOutName + ".ase_lightmapUVs.xy = " + texcoord1 + ".xy * unity_LightmapST.xy + unity_LightmapST.zw;" );
						dataCollector.AddToVertexLocalVariables( UniqueId, vOutName + ".ase_lightmapUVs.zw = " + texcoord2 + ".xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;" );

						string worldPos = dataCollector.TemplateDataCollectorInstance.GetWorldPos( false, MasterNodePortCategory.Fragment );

						dataCollector.AddToPragmas( UniqueId, "multi_compile _ LIGHTMAP_ON" );
						dataCollector.AddToPragmas( UniqueId, "multi_compile _ DIRLIGHTMAP_COMBINED" );
						dataCollector.AddToPragmas( UniqueId, "multi_compile _ DYNAMICLIGHTMAP_ON" );

						string fragWorldNormal = string.Empty;
						if ( m_inputPorts[ 0 ].IsConnected )
						{
							if ( m_normalSpace == ViewSpace.Tangent )
								fragWorldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( UniqueId, CurrentPrecisionType, m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector ), OutputId );
							else
								fragWorldNormal = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
						}
						else
						{
							fragWorldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( PrecisionType.Float, false, MasterNodePortCategory.Fragment );
						}

						//SAMPLE_GI
						if ( ASEPackageManagerHelper.CurrentSRPVersion >= ( int )SRPBaseline.ASE_SRP_17_X )
						{
							string screenPos = GeneratorUtils.GenerateScreenPositionRaw( ref dataCollector, UniqueId, CurrentPrecisionType );
							string positionSS = string.Format( "( uint2 )( {0}.xy / {0}.w * _ScreenSize.xy )", screenPos );

							dataCollector.AddLocalVariable( UniqueId, "float3 bakedGI" + OutputId + " = SampleBakedGI( " + worldPos + ", " + fragWorldNormal + ", " + positionSS + ", " + fInName + ".ase_lightmapUVs.xy, " + fInName + ".ase_lightmapUVs.zw );" );
						}
						else
						{
							dataCollector.AddLocalVariable( UniqueId, "float3 bakedGI" + OutputId + " = SampleBakedGI( " + worldPos + ", " + fragWorldNormal + ", " + fInName + ".ase_lightmapUVs.xy, " + fInName + ".ase_lightmapUVs.zw, false );" );
						}
						finalValue = "bakedGI" + OutputId;
						m_outputPorts[ 0 ].SetLocalValue( finalValue, dataCollector.PortCategory );
						return finalValue;
					}
				}
			}

			if ( !dataCollector.IsTemplate )
			{
				if ( dataCollector.GenType == PortGenType.NonCustomLighting || dataCollector.CurrentCanvasMode != NodeAvailability.CustomLighting )
				{
					UIUtils.ShowMessage( UniqueId, "Indirect Diffuse Light must be connected to Custom Lighting when using Surface shaders." );
					return m_outputPorts[0].ErrorValue;
				}
			}
			else if ( dataCollector.IsSRP )
			{
				UIUtils.ShowMessage( UniqueId, "Indirect Diffuse Light does not support URP/HDRP on Vertex or Tessellation stages." );
				return "float3( 0, 0, 0 )";
			}

			string normal = string.Empty;
			if( m_inputPorts[ 0 ].IsConnected )
			{
				if ( !dataCollector.IsSRP )
				{
					dataCollector.AddToInput( UniqueId, SurfaceInputs.WORLD_NORMAL, UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
					dataCollector.AddToInput( UniqueId, SurfaceInputs.INTERNALDATA, addSemiColon: false );
					dataCollector.ForceNormal = true;
				}

				normal = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
				normal = GeneratorUtils.GenerateWorldNormal( ref dataCollector, UniqueId, m_currentPrecisionType, normal, OutputId, m_normalSpace );

				if ( m_normalize )
				{
					normal = "normalize( " + normal + " )";
				}
			}
			else
			{
				if( dataCollector.IsFragmentCategory )
				{
					dataCollector.AddToInput( UniqueId, SurfaceInputs.WORLD_NORMAL, UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
					if( dataCollector.DirtyNormal )
					{
						dataCollector.AddToInput( UniqueId, SurfaceInputs.INTERNALDATA, addSemiColon: false );
						dataCollector.ForceNormal = true;
					}
				}

				normal = GeneratorUtils.GenerateWorldNormal( ref dataCollector, UniqueId, m_normalize );
			}


			if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, "indirectDiffuse" + OutputId, "ShadeSH9( float4( " + normal + ", 1 ) )" );
			}
			else
			{
				dataCollector.AddLocalVariable( UniqueId, "UnityGI gi" + OutputId + " = gi;" );
				dataCollector.AddLocalVariable( UniqueId, PrecisionType.Float, WirePortDataType.FLOAT3, "diffNorm" + OutputId, normal );
				dataCollector.AddLocalVariable( UniqueId, "gi" + OutputId + " = UnityGI_Base( data, 1, diffNorm" + OutputId + " );" );
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, "indirectDiffuse" + OutputId, "gi" + OutputId + ".indirect.diffuse + diffNorm" + OutputId + " * 0.0001" );
			}

			finalValue = "indirectDiffuse" + OutputId;
			m_outputPorts[ 0 ].SetLocalValue( finalValue, dataCollector.PortCategory );
			return finalValue;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 13002 )
				m_normalSpace = (ViewSpace)Enum.Parse( typeof( ViewSpace ), GetCurrentParam( ref nodeParams ) );

			UpdatePort();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_normalSpace );
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[System.Serializable]
	[NodeAttributes( "Main Light", "Lighting", "Light shading data of Main Directional light." )]
	[NoAutoStaticsCleanup]
	public sealed class MainLight : ParentNode
	{
		static readonly string SurfaceError = "This node only returns correct information using a custom light model.";

		static readonly string HelperText =
			"BiRP and URP\nMain Light is the Directional Light with highest intensity in the scene.\n\n" +
			"HDRP\nMain Light is the Directional Light allowed to cast shadows (only one allowed in HDRP) or, the Directional Light with highest intensity in the scene.";

		enum OutputPortID
		{
			DIRECTION = 0,
			COLOR_HDR = 1,
			COLOR_LDR = 2,
			INTENSITY = 3,
			SHADOW_ATTENUATION = 4
		}

		private struct OutputPortDescData
		{
			public string name;
			public WirePortDataType type;
			public string varName;
			public string formatBIRP;
			public string formatURP;
			public string formatHDRP;
		}

		private static readonly Dictionary<OutputPortID, OutputPortDescData> OutputPortDesc = new Dictionary<OutputPortID, OutputPortDescData>
		{
			{ OutputPortID.DIRECTION, new OutputPortDescData {
				name = "Direction",
				type = WirePortDataType.FLOAT3,
				varName = "ase_mainLightDirection",
				formatBIRP = "_WorldSpaceLightPos0.xyz",
				formatURP = "{0}.direction",
				formatHDRP = "-{0}.forward"
			} },

			{ OutputPortID.COLOR_HDR, new OutputPortDescData {
				name = "Raw Color",
				type = WirePortDataType.FLOAT3,
				varName = "ase_mainLightColorHDR",
				formatBIRP = "_LightColor0.rgb",
				formatURP = "{0}.color",
				formatHDRP = "{0}.color"
			} },

			{ OutputPortID.COLOR_LDR, new OutputPortDescData {
				name = "Color",
				type = WirePortDataType.FLOAT3,
				varName = "ase_mainLightColorLDR",
				formatBIRP = "_LightColor0.rgb / ( _LightColor0.a + 1e-7 )",
				formatURP = "{0}.color / ( max( max( {0}.color.r, {0}.color.g ), {0}.color.b ) + 1e-7 )",
				formatHDRP = "{0}.color / ( max( max( {0}.color.r, {0}.color.g ), {0}.color.b ) + 1e-7 )"
			} },

			{ OutputPortID.INTENSITY, new OutputPortDescData {
				name = "Intensity",
				type = WirePortDataType.FLOAT,
				varName = "ase_mainLightIntensity",
				formatBIRP = "_LightColor0.a",
				formatURP = "max( max( {0}.color.r, {0}.color.g ), {0}.color.b )",
				formatHDRP = "max( max( {0}.color.r, {0}.color.g ), {0}.color.b )"
			} },

			{ OutputPortID.SHADOW_ATTENUATION, new OutputPortDescData {
				name = "Shadow Attenuation",
				type = WirePortDataType.FLOAT,
				varName = "ase_mainLightShadowAtten",
				formatBIRP = "ase_lightShadowAtten",
				formatURP = "{0}.shadowAttenuation",
				formatHDRP = "1.0"
			} }
		};

		private static readonly string HDRPGetMainLightCall = "ASEGetMainLight( {0} );";
		private static readonly string[] HDRPGetMainLightFunction = {
			"inline void ASEGetMainLight( out DirectionalLightData light )\n",
			"{\n",
			"\tUNITY_BRANCH if ( _DirectionalShadowIndex >= 0 )\n",
			"\t{\n",
			"\t\tlight = _DirectionalLightDatas[ _DirectionalShadowIndex ];\n",
			"\t}\n",
			"\telse\n",
			"\t{\n",
			"\t\tif ( _DirectionalLightCount == 1 )\n",
			"\t\t{\n",
			"\t\t\tlight = _DirectionalLightDatas[ 0 ];\n",
			"\t\t}\n",
			"\t\telse if ( _DirectionalLightCount > 1 )\n",
			"\t\t{\n",
			"\t\t\tfloat highestIntensity = 0;\n",
			"\t\t\tuint highestIndex = 0;\n",
			"\t\t\tfor ( uint i = 0; i < _DirectionalLightCount; i++ )\n",
			"\t\t\t{\n",
			"\t\t\t\tfloat3 color = _DirectionalLightDatas[ i ].color;\n",
			"\t\t\t\tfloat intensity = max( max( color.r, color.g ), color.b );\n",
			"\t\t\t\tif ( intensity > highestIntensity )\n",
			"\t\t\t\t{\n",
			"\t\t\t\t\thighestIndex = i;\n",
			"\t\t\t\t\thighestIntensity = intensity;\n",
			"\t\t\t\t}\n",
			"\t\t\t}\n",
			"\t\t\tlight = _DirectionalLightDatas[ highestIndex ];\n",
			"\t\t}\n",
			"\t\telse\n",
			"\t\t{\n",
			"\t\t	light = ( DirectionalLightData )0;\n",
			"\t\t}\n",
			"\t}\n",
			"}\n"
		};

		private static readonly string HDRPGetDirectionalShadowAttenuationCall = "ASEGetDirectionalShadowAttenuation( {0}, {1}, {2} )";
		private static readonly string[] HDRPGetDirectionalShadowAttenuationFunction = {
			"inline float ASEGetDirectionalShadowAttenuation( DirectionalLightData light, uint2 positionSS, float3 positionRWS )\n",
			"{\n",
			"\tfloat shadowAttenuation = 1;\n",
			"\tif ( ( light.lightDimmer > 0 ) && ( light.shadowDimmer > 0 ) && _DirectionalShadowIndex >= 0 )\n",
			"\t{\n",
			"\t\tfloat3 L = -light.forward;\n",
			"\t\tHDShadowContext ctx = InitShadowContext();\n",
			"\t\tshadowAttenuation = GetDirectionalShadowAttenuation( ctx, positionSS, positionRWS, L, light.shadowIndex, L );\n",
			"\t}\n",
			"\treturn shadowAttenuation;\n",
			"}\n"
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			foreach ( var port in OutputPortDesc )
			{
				AddOutputPort( port.Value.type, port.Value.name );
			}

			m_errorMessageTypeIsError = NodeMessageType.Warning;
			m_errorMessageTooltip = SurfaceError;
			m_previewShaderGUID = "56862bb2992719b47b82efbf02fd11ff";
			m_drawPreviewAsSphere = true;

			m_drawPreview = false;
			m_drawPreviewExpander = false;
			m_canExpand = false;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUILayout.HelpBox( HelperText, MessageType.Info );
		}

		public override void RenderNodePreview()
		{
			// runs at least one time
			if ( !m_initialized )
			{
				// nodes with no preview don't update at all
				PreviewIsDirty = false;
				return;
			}

			if ( !PreviewIsDirty )
			{
				return;
			}

			if( !Preferences.User.DisablePreviews )
			{
				PreparePooledPreview();

				RenderTexture temp = RenderTexture.active;
				int count = m_outputPorts.Count;
				for( int i = 0 ; i < count ; i++ )
				{
					m_outputPorts[ i ].BlitPreview( PreviewMaterial , i );
				}
				RenderTexture.active = temp;
			}

			PreviewIsDirty = ContinuousPreviewRefresh;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			var outputPortID = ( OutputPortID )outputId;
			var port = OutputPortDesc[ outputPortID ];
			string varValue = string.Empty;

			if ( outputPortID == OutputPortID.SHADOW_ATTENUATION && !dataCollector.IsFragmentCategory )
			{
				UIUtils.ShowMessage( UniqueId, "Main Light node Shadow Attenuation output not supported on Vertex or Tessellation stages." );
				return m_outputPorts[0].ErrorValue;
			}

			if ( !dataCollector.HasLocalVariable( port.varName ) )
			{
				if ( !dataCollector.IsTemplate )
				{
					if ( dataCollector.CurrentCanvasMode != NodeAvailability.CustomLighting )
					{
						UIUtils.ShowMessage( UniqueId, "Main Light Attenuation node not supported on non-custom lighting Surface shaders." );
						return m_outputPorts[ 0 ].ErrorValue;
					}

					if ( dataCollector.GenType == PortGenType.NonCustomLighting )
					{
						UIUtils.ShowMessage( UniqueId, "Main Light Attenuation node must only be connected to Custom Lighting output on Surface shaders." );
						return m_outputPorts[ 0 ].ErrorValue;
					}

					// SURFACE
					if ( outputPortID == OutputPortID.SHADOW_ATTENUATION )
					{
						dataCollector.UsingLightAttenuation = true;
						varValue = GeneratorUtils.LightAttenuationStr;
					}
					else
					{
						varValue = string.IsNullOrEmpty( port.formatBIRP ) ? port.varName : port.formatBIRP;
					}
				}
				else
				{
					// TEMPLATE
					if ( !dataCollector.IsSRP )
					{
						// BIRP TEMPLATE
						dataCollector.AddToIncludes( -1, Constants.UnityLightingLib );

						if ( outputPortID == OutputPortID.SHADOW_ATTENUATION )
						{
							if ( !dataCollector.TemplateDataCollectorInstance.ContainsSpecialLocalFragVar( TemplateInfoOnSematics.LIGHT_ATTENUATION, WirePortDataType.FLOAT, ref varValue ) )
							{
								varValue = dataCollector.TemplateDataCollectorInstance.GetLightAtten( UniqueId );
							}
						}
						else
						{
							varValue = string.Format( port.formatBIRP );
						}
					}
					else if ( dataCollector.CurrentSRPType == TemplateSRPType.URP )
					{
						// URP TEMPLATE
						dataCollector.TemplateDataCollectorInstance.AddMainLightShadowAttenuationDependsURP( UniqueId );

						string mainLight = dataCollector.TemplateDataCollectorInstance.GetURPMainLight( UniqueId );

						varValue = string.Format( port.formatURP, mainLight );
					}
					else if ( dataCollector.CurrentSRPType == TemplateSRPType.HDRP )
					{
						// HDRP TEMPLATE
						dataCollector.TemplateDataCollectorInstance.AddMainLightShadowAttenuationDependsHDRP( UniqueId );

						dataCollector.AddFunction( HDRPGetMainLightFunction[ 0 ], HDRPGetMainLightFunction, false );

						const string mainLight = "ase_mainLight";
						dataCollector.AddLocalVariable( UniqueId, string.Format( "DirectionalLightData {0};", mainLight ) );
						dataCollector.AddLocalVariable( UniqueId, string.Format( HDRPGetMainLightCall, mainLight ) );

						if ( outputPortID == OutputPortID.SHADOW_ATTENUATION )
						{
							dataCollector.AddFunction( HDRPGetDirectionalShadowAttenuationFunction[ 0 ], HDRPGetDirectionalShadowAttenuationFunction, false );

							string positionWS = GeneratorUtils.GenerateRelativeWorldPosition( ref dataCollector, UniqueId );
							string positionSS = GeneratorUtils.GenerateScreenPositionPixelOnFrag( ref dataCollector, UniqueId, CurrentPrecisionType );
							string int2_positionSS = string.Format( "int2( {0}.xy )", positionSS );

							varValue = string.Format( HDRPGetDirectionalShadowAttenuationCall, mainLight, int2_positionSS, positionWS );
						}
						else
						{
							varValue = string.Format( port.formatHDRP, mainLight );
						}
					}
				}

				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, port.type, port.varName, varValue );
			}

			return port.varName;
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			m_errorMessageTypeIsError = NodeMessageType.Error;
			m_errorMessageTooltip = SurfaceError;
			m_showErrorMessage = ( ( ContainerGraph.CurrentStandardSurface != null && ContainerGraph.CurrentStandardSurface.CurrentLightingModel != StandardShaderLightModel.CustomLighting ) );
		}
	}
}

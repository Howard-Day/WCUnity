// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[System.Serializable]
	[NodeAttributes( "Main Light Attenuation", "Lighting", "Attenuation/shadow of main Directional light." )]
	public sealed class LightAttenuation : ParentNode
	{
		static readonly string SurfaceError = "This node only returns correct information using a custom light model, otherwise returns 1";
		static readonly string TemplateError = "This node will only produce proper attenuation if the template contains a shadow caster pass";

		private const string ASEAttenVarName = "ase_lightAtten";

		//private readonly string[] LightweightVertexInstructions =
		//{
		//	/*local vertex position*/"VertexPositionInputs ase_vertexInput = GetVertexPositionInputs ({0});",
		//	"#ifdef _MAIN_LIGHT_SHADOWS//ase_lightAtten_vert",
		//	/*available interpolator*/"{0} = GetShadowCoord( ase_vertexInput );",
		//	"#endif//ase_lightAtten_vert"
		//};
		private const string LightweightLightAttenDecl = "float ase_lightAtten = 0;";
		private readonly string[] LightweightFragmentInstructions =
		{
			/*shadow coords*/"Light ase_lightAtten_mainLight = GetMainLight( {0} );",
			//"ase_lightAtten = ase_lightAtten_mainLight.distanceAttenuation * ase_lightAtten_mainLight.shadowAttenuation;"
			"ase_lightAtten = {0}.distanceAttenuation * {0}.shadowAttenuation;"
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.FLOAT, "Out" );
			m_errorMessageTypeIsError = NodeMessageType.Warning;
			m_errorMessageTooltip = SurfaceError;
			m_previewShaderGUID = "4b12227498a5c8d46b6c44ea018e5b56";
			m_drawPreviewAsSphere = true;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( !dataCollector.IsFragmentCategory )
			{
				UIUtils.ShowMessage( UniqueId, "Main Light Attenuation node not supported on Vertex or Tessellation stages." );
				return m_outputPorts[0].ErrorValue;
			}

			if( dataCollector.IsTemplate  )
			{
				if( !dataCollector.IsSRP )
				{
					string result = string.Empty;
					if( dataCollector.TemplateDataCollectorInstance.ContainsSpecialLocalFragVar( TemplateInfoOnSematics.LIGHT_ATTENUATION, WirePortDataType.FLOAT, ref result ) )
					{
						return result;
					}

					return dataCollector.TemplateDataCollectorInstance.GetLightAtten( UniqueId );
				}
				else
				{
					if( dataCollector.CurrentSRPType == TemplateSRPType.URP )
					{
						if( dataCollector.HasLocalVariable( LightweightLightAttenDecl ))
							return ASEAttenVarName;

						dataCollector.TemplateDataCollectorInstance.AddMainLightShadowAttenuationDependsURP( UniqueId );

						dataCollector.AddLocalVariable( UniqueId, LightweightLightAttenDecl );
						string mainLight = dataCollector.TemplateDataCollectorInstance.GetURPMainLight( UniqueId );

						dataCollector.AddLocalVariable( UniqueId, string.Format( LightweightFragmentInstructions[ 1 ], mainLight) );
						return ASEAttenVarName;
					}
					else
					{
						UIUtils.ShowMessage( UniqueId, "Main Light Attenuation node currently not supported on HDRP" );
						return m_outputPorts[0].ErrorValue;
					}
				}
			}

			if ( dataCollector.CurrentCanvasMode != NodeAvailability.CustomLighting )
			{
				UIUtils.ShowMessage( UniqueId, "Main Light Attenuation node not supported on non-custom lighting Surface shaders." );
				return m_outputPorts[0].ErrorValue;
			}

			if ( dataCollector.GenType == PortGenType.NonCustomLighting )
			{
				UIUtils.ShowMessage( UniqueId, "Main Light Attenuation node must only be connected to Custom Lighting output on Surface shaders." );
				return m_outputPorts[0].ErrorValue;
			}

			dataCollector.UsingLightAttenuation = true;
			return ASEAttenVarName;
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			if( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader && ContainerGraph.CurrentSRPType != TemplateSRPType.URP )
			{
				m_showErrorMessage = true;
				m_errorMessageTypeIsError = NodeMessageType.Warning;
				m_errorMessageTooltip = TemplateError;
			} else
			{
				m_errorMessageTypeIsError = NodeMessageType.Error;
				m_errorMessageTooltip = SurfaceError;
				if ( ( ContainerGraph.CurrentStandardSurface != null && ContainerGraph.CurrentStandardSurface.CurrentLightingModel != StandardShaderLightModel.CustomLighting ) )
					m_showErrorMessage = true;
				else
					m_showErrorMessage = false;
			}


		}
	}
}

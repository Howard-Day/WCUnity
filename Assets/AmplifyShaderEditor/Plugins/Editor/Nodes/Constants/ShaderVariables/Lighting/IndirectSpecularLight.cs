// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Indirect Specular Light", "Lighting", "Indirect Specular Light" )]
	public sealed class IndirectSpecularLight : ParentNode
	{
		[SerializeField]
		private ViewSpace m_normalSpace = ViewSpace.Tangent;
		[SerializeField]
		private bool m_normalize = true;

		private const string DefaultErrorMessage = "This node only returns correct information using a custom light model, otherwise returns 0.";
		private const string UpgradeErrorMessage = "Smoothness port was previously being used as Roughness, please check if you are correctly using it and save to confirm.";
		private const string UnsupportedMessage = "Only valid on BiRP and URP templates.";
		private bool m_upgradeMessage = false;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, "Normal" );
			AddInputPort( WirePortDataType.FLOAT, false, "Smoothness" );
			AddInputPort( WirePortDataType.FLOAT, false, "Occlusion" );
			m_inputPorts[ 0 ].Vector3InternalData = Vector3.forward;
			m_inputPorts[ 1 ].FloatInternalData = 0.5f;
			m_inputPorts[ 2 ].FloatInternalData = 1;
			m_inputPorts[ 1 ].AutoDrawInternalData = true;
			m_inputPorts[ 2 ].AutoDrawInternalData = true;
			m_autoWrapProperties = true;
			AddOutputPort( WirePortDataType.FLOAT3, "RGB" );
			m_errorMessageTypeIsError = NodeMessageType.Warning;
			m_errorMessageTooltip = DefaultErrorMessage;
			m_previewShaderGUID = "d6e441d0a8608954c97fa347d3735e92";
			m_drawPreviewAsSphere = true;
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			base.PropagateNodeData( nodeData, ref dataCollector );
			if( m_inputPorts[ 0 ].IsConnected )
				dataCollector.DirtyNormal = true;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );

			if ( m_upgradeMessage || ( ContainerGraph.CurrentStandardSurface != null && ContainerGraph.CurrentStandardSurface.CurrentLightingModel != StandardShaderLightModel.CustomLighting ) )
			{
				m_errorMessageTypeIsError = m_upgradeMessage ? NodeMessageType.Warning : NodeMessageType.Error;
				m_errorMessageTooltip = m_upgradeMessage ? UpgradeErrorMessage : DefaultErrorMessage;
				m_showErrorMessage = true;
			}
			else if ( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader && ( ContainerGraph.CurrentSRPType != TemplateSRPType.URP && ContainerGraph.CurrentSRPType != TemplateSRPType.BiRP ) )
			{
				m_errorMessageTypeIsError = NodeMessageType.Error;
				m_errorMessageTooltip = UnsupportedMessage;
				m_showErrorMessage = true;
			}
			else
			{
				m_showErrorMessage = false;
			}
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
			if( !m_inputPorts[ 1 ].IsConnected )
				m_inputPorts[ 1 ].FloatInternalData = EditorGUILayout.FloatField( m_inputPorts[ 1 ].Name, m_inputPorts[ 1 ].FloatInternalData );
			if( !m_inputPorts[ 2 ].IsConnected )
				m_inputPorts[ 2 ].FloatInternalData = EditorGUILayout.FloatField( m_inputPorts[ 2 ].Name, m_inputPorts[ 2 ].FloatInternalData );

			if ( m_showErrorMessage )
			{
				EditorGUILayout.HelpBox( m_errorMessageTooltip, ( m_errorMessageTypeIsError == NodeMessageType.Error ) ? MessageType.Error : MessageType.Warning );
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

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( dataCollector.IsTemplate )
			{
				if( !dataCollector.IsSRP )
				{
					dataCollector.AddToIncludes( UniqueId, Constants.UnityLightingLib );
					string worldPos = dataCollector.TemplateDataCollectorInstance.GetWorldPos();
					string worldViewDir = dataCollector.TemplateDataCollectorInstance.GetViewDir( useMasterNodeCategory:false, customCategory:MasterNodePortCategory.Fragment );

					string worldNormal = string.Empty;
					if( m_inputPorts[ 0 ].IsConnected )
					{
						if( m_normalSpace == ViewSpace.Tangent )
							worldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( UniqueId, CurrentPrecisionType, m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector ), OutputId );
						else
							worldNormal = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					}
					else
					{
						worldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( PrecisionType.Float, false, MasterNodePortCategory.Fragment );
					}

					string tempsmoothness = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
					string tempocclusion = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );

					dataCollector.AddLocalVariable( UniqueId, "UnityGIInput data;" );
					dataCollector.AddLocalVariable( UniqueId, "UNITY_INITIALIZE_OUTPUT( UnityGIInput, data );" );
					dataCollector.AddLocalVariable( UniqueId, "data.worldPos = " + worldPos + ";" );
					dataCollector.AddLocalVariable( UniqueId, "data.worldViewDir = " + worldViewDir + ";" );
					dataCollector.AddLocalVariable( UniqueId, "data.probeHDR[0] = unity_SpecCube0_HDR;" );
					dataCollector.AddLocalVariable( UniqueId, "data.probeHDR[1] = unity_SpecCube1_HDR;" );
					dataCollector.AddLocalVariable( UniqueId, "#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION //specdataif0" );
					dataCollector.AddLocalVariable( UniqueId, "\tdata.boxMin[0] = unity_SpecCube0_BoxMin;" );
					dataCollector.AddLocalVariable( UniqueId, "#endif //specdataif0" );
					dataCollector.AddLocalVariable( UniqueId, "#if UNITY_SPECCUBE_BOX_PROJECTION //specdataif1" );
					dataCollector.AddLocalVariable( UniqueId, "\tdata.boxMax[0] = unity_SpecCube0_BoxMax;" );
					dataCollector.AddLocalVariable( UniqueId, "\tdata.probePosition[0] = unity_SpecCube0_ProbePosition;" );
					dataCollector.AddLocalVariable( UniqueId, "\tdata.boxMax[1] = unity_SpecCube1_BoxMax;" );
					dataCollector.AddLocalVariable( UniqueId, "\tdata.boxMin[1] = unity_SpecCube1_BoxMin;" );
					dataCollector.AddLocalVariable( UniqueId, "\tdata.probePosition[1] = unity_SpecCube1_ProbePosition;" );
					dataCollector.AddLocalVariable( UniqueId, "#endif //specdataif1" );

					dataCollector.AddLocalVariable( UniqueId, "Unity_GlossyEnvironmentData g" + OutputId + " = UnityGlossyEnvironmentSetup( " + tempsmoothness + ", " + worldViewDir + ", " + worldNormal + ", float3(0,0,0));" );
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, "indirectSpecular" + OutputId, "UnityGI_IndirectSpecular( data, " + tempocclusion + ", " + worldNormal + ", g" + OutputId + " )" );
					return "indirectSpecular" + OutputId;
				}
				else
				{
					if( dataCollector.CurrentSRPType == TemplateSRPType.URP )
					{
						string worldViewDir = dataCollector.TemplateDataCollectorInstance.GetViewDir( useMasterNodeCategory: false, customCategory: MasterNodePortCategory.Fragment );
						string worldNormal = string.Empty;
						if( m_inputPorts[ 0 ].IsConnected )
						{
							if( m_normalSpace == ViewSpace.Tangent )
								worldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( UniqueId, CurrentPrecisionType, m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector ), OutputId );
							else
								worldNormal = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
						}
						else
						{
							worldNormal = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( precisionType:PrecisionType.Float, useMasterNodeCategory: false, customCategory: MasterNodePortCategory.Fragment );
						}

						string tempsmoothness = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
						string tempocclusion = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );

						dataCollector.AddLocalVariable( UniqueId, "half3 reflectVector" + OutputId + " = reflect( -" + worldViewDir + ", " + worldNormal + " );" );
						if ( ASEPackageManagerHelper.PackageSRPVersion >= ( int )SRPBaseline.ASE_SRP_14_X )
						{
							if ( ASEPackageManagerHelper.PackageSRPVersion >= ( int )SRPBaseline.ASE_SRP_17_X )
							{
								// @diogo: note the conditions; this pragma is only valid for 6000.0
								dataCollector.AddToPragmas( UniqueId, "multi_compile _ _FORWARD_PLUS", "UNITY_VERSION < 60010000" );

								// @diogo: valid for 6000.1 and above, fragment-only from 6000.7; guards match "SRP Additional Light"
								// so the pragma merge collapses them instead of declaring the keyword twice
								dataCollector.AddToPragmas( UniqueId, "multi_compile _ _CLUSTER_LIGHT_LOOP", "(UNITY_VERSION >= 60010000) && (UNITY_VERSION < 60070000)" );
								dataCollector.AddToPragmas( UniqueId, "multi_compile_fragment _ _CLUSTER_LIGHT_LOOP", "(UNITY_VERSION >= 60070000)" );
								dataCollector.AddToPragmas( UniqueId, "multi_compile_fragment _ _REFLECTION_PROBE_ATLAS", "UNITY_VERSION >= 60010000" );
							}
							else
							{
								dataCollector.AddToPragmas( UniqueId, "multi_compile _ _FORWARD_PLUS" );
							}

							string worldPos = dataCollector.TemplateDataCollectorInstance.GetWorldPos( useMasterNodeCategory: false, customCategory: MasterNodePortCategory.Fragment );
							string normalizedScreenUV = dataCollector.TemplateDataCollectorInstance.GetScreenPosNormalized( CurrentPrecisionType, useMasterNodeCategory: false, customCategory: MasterNodePortCategory.Fragment );

							dataCollector.AddLocalVariable( UniqueId, string.Format( "float3 indirectSpecular{0} = GlossyEnvironmentReflection( reflectVector{0}, {1}, 1.0 - {2}, {3}, {4}.xy );",
								OutputId, worldPos, tempsmoothness, tempocclusion, normalizedScreenUV ) );
						}
						else
						{
							dataCollector.AddLocalVariable( UniqueId, "float3 indirectSpecular" + OutputId + " = GlossyEnvironmentReflection( reflectVector" + OutputId + ", 1.0 - " + tempsmoothness + ", " + tempocclusion + " );" );
						}
						return "indirectSpecular" + OutputId;
					}
					else if( dataCollector.CurrentSRPType == TemplateSRPType.HDRP )
					{
						UIUtils.ShowMessage( UniqueId, "Indirect Specular Light node currently not supported on HDRP" );
						return m_outputPorts[0].ErrorValue;
					}
				}
			}

			if( dataCollector.GenType == PortGenType.NonCustomLighting || dataCollector.CurrentCanvasMode != NodeAvailability.CustomLighting )
				return m_outputPorts[0].ErrorValue;

			string normal = string.Empty;
			if( m_inputPorts[ 0 ].IsConnected )
			{
				dataCollector.AddToInput( UniqueId, SurfaceInputs.WORLD_NORMAL, UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
				dataCollector.AddToInput( UniqueId, SurfaceInputs.INTERNALDATA, addSemiColon: false );
				dataCollector.ForceNormal = true;

				normal = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
				if( m_normalSpace == ViewSpace.Tangent )
				{
					normal = "WorldNormalVector( " + Constants.InputVarStr + " , " + normal + " )";
					if( m_normalize )
					{
						normal = "normalize( " + normal + " )";
					}
				}

				dataCollector.AddLocalVariable( UniqueId, "float3 indirectNormal" + OutputId + " = " + normal + ";" );
				normal = "indirectNormal" + OutputId;
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

			string smoothness = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
			string occlusion = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
			string viewDir = "data.worldViewDir";

			if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				string worldPos = GeneratorUtils.GenerateWorldPosition( ref dataCollector, UniqueId );
				viewDir = GeneratorUtils.GenerateViewDirection( ref dataCollector, UniqueId );

				dataCollector.AddLocalVariable( UniqueId, "UnityGIInput data;" );
				dataCollector.AddLocalVariable( UniqueId, "UNITY_INITIALIZE_OUTPUT( UnityGIInput, data );" );
				dataCollector.AddLocalVariable( UniqueId, "data.worldPos = " + worldPos + ";" );
				dataCollector.AddLocalVariable( UniqueId, "data.worldViewDir = " + viewDir + ";" );
				dataCollector.AddLocalVariable( UniqueId, "data.probeHDR[0] = unity_SpecCube0_HDR;" );
				dataCollector.AddLocalVariable( UniqueId, "data.probeHDR[1] = unity_SpecCube1_HDR;" );
				dataCollector.AddLocalVariable( UniqueId, "#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION //specdataif0" );
				dataCollector.AddLocalVariable( UniqueId, "data.boxMin[0] = unity_SpecCube0_BoxMin;" );
				dataCollector.AddLocalVariable( UniqueId, "#endif //specdataif0" );
				dataCollector.AddLocalVariable( UniqueId, "#if UNITY_SPECCUBE_BOX_PROJECTION //specdataif1" );
				dataCollector.AddLocalVariable( UniqueId, "data.boxMax[0] = unity_SpecCube0_BoxMax;" );
				dataCollector.AddLocalVariable( UniqueId, "data.probePosition[0] = unity_SpecCube0_ProbePosition;" );
				dataCollector.AddLocalVariable( UniqueId, "data.boxMax[1] = unity_SpecCube1_BoxMax;" );
				dataCollector.AddLocalVariable( UniqueId, "data.boxMin[1] = unity_SpecCube1_BoxMin;" );
				dataCollector.AddLocalVariable( UniqueId, "data.probePosition[1] = unity_SpecCube1_ProbePosition;" );
				dataCollector.AddLocalVariable( UniqueId, "#endif //specdataif1" );
			}

			dataCollector.AddLocalVariable( UniqueId, "Unity_GlossyEnvironmentData g" + OutputId + " = UnityGlossyEnvironmentSetup( " + smoothness + ", " + viewDir + ", " + normal + ", float3(0,0,0));" );
			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, "indirectSpecular" + OutputId, "UnityGI_IndirectSpecular( data, " + occlusion + ", " + normal + ", g" + OutputId + " )" );

			return "indirectSpecular" + OutputId;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 13002 )
				m_normalSpace = (ViewSpace)Enum.Parse( typeof( ViewSpace ), GetCurrentParam( ref nodeParams ) );

			if( UIUtils.CurrentShaderVersion() < 13804 )
			{
				m_upgradeMessage = true;
				UIUtils.ShowMessage( UniqueId, "Indirect Specular Light node: Smoothness port was previously being used as Roughness, please check if you are correctly using it and save to confirm." );
			}

			UpdatePort();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_normalSpace );
			m_upgradeMessage = false;
		}
	}
}

// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Reflection Probe", "Miscellaneous", "Provides access to the nearest Reflection Probe to the object. Only available on URP.\n\nView Dir OS: View Direction in Object-space\nNormal OS: Normal in Object-space\nLOD: Index of level-of-detail" )]
	public class ReflectionProbeNode : ParentNode
	{
		private const string ReflectionProbeStr = "SHADERGRAPH_REFLECTION_PROBE({0},{1},{2})";
		private const string InfoTransformSpace = "Both View Dir and Normal vectors are set in Object Space";
		public const string NodeErrorMsg = "Only valid on URP";
		public const string ErrorOnCompilationMsg = "Attempting to use URP specific node on incorrect SRP or Builtin RP.";
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, "View Dir OS" );
			AddInputPort( WirePortDataType.FLOAT3, false, "Normal OS" );
			AddInputPort( WirePortDataType.FLOAT, false, "LOD" );
			AddOutputPort( WirePortDataType.FLOAT3, "Out" );
			m_autoWrapProperties = true;
			m_errorMessageTooltip = NodeErrorMsg;
			m_errorMessageTypeIsError = NodeMessageType.Error;
			m_previewShaderGUID = "f7d3fa6f91f1f184f89060feb01051a1";
			m_drawPreviewAsSphere = true;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_showErrorMessage = ( ContainerGraph.CurrentCanvasMode == NodeAvailability.SurfaceShader ) ||
								 ( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader && ContainerGraph.CurrentSRPType != TemplateSRPType.URP );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUILayout.HelpBox( InfoTransformSpace, MessageType.Info );
			if ( m_showErrorMessage )
			{
				EditorGUILayout.HelpBox( NodeErrorMsg, MessageType.Error );
			}
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			PreviewMaterial.SetInt( "viewDirInput", m_inputPorts[ 0 ].IsConnected ? 1 : 0 );
			PreviewMaterial.SetInt( "normalInput", m_inputPorts[ 1 ].IsConnected ? 1 : 0 );
			PreviewMaterial.SetInt( "lodInput", m_inputPorts[ 2 ].IsConnected ? 1 : 0 );
		}

		uint viewDirInput;
		uint normalInput;
		uint lodInput;

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( !dataCollector.IsSRP || !dataCollector.TemplateDataCollectorInstance.IsURP )
			{
				UIUtils.ShowMessage( ErrorOnCompilationMsg, MessageSeverity.Error );
				return GenerateErrorValue();
			}

			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			if ( dataCollector.IsSRP && dataCollector.CurrentSRPType == TemplateSRPType.URP )
			{
				dataCollector.AddToPragmas( UniqueId, "multi_compile_fragment _ _REFLECTION_PROBE_BLENDING" );
				dataCollector.AddToPragmas( UniqueId, "multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION" );

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
				else if ( ASEPackageManagerHelper.PackageSRPVersion >= ( int )SRPBaseline.ASE_SRP_14_X )
				{
					dataCollector.AddToPragmas( UniqueId, "multi_compile _ _FORWARD_PLUS");
				}
			}

			string viewDirOS;
			if ( m_inputPorts[ 0 ].IsConnected )
			{
				viewDirOS = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			}
			else
			{
				viewDirOS = dataCollector.TemplateDataCollectorInstance.GetViewDir( CurrentPrecisionType, space: ViewSpace.Object );
			}

			string normalOS;
			if ( m_inputPorts[ 1 ].IsConnected )
			{
				normalOS = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
			}
			else
			{
				normalOS = dataCollector.TemplateDataCollectorInstance.GetVertexNormal( CurrentPrecisionType );
			}

			string lod = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );

			RegisterLocalVariable( outputId, string.Format( ReflectionProbeStr, viewDirOS, normalOS, lod ), ref dataCollector );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}
	}
}

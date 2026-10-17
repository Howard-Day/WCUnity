// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;
using UnityEngine;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Last Time", "Time", "Previous frame time parameters ( URP/HDRP only )" )]
	public sealed class LastTimeNode : ConstVecShaderVariable
	{
		private const string ErrorOnCompilationMsg = "Attempting to use a URP/HDRP specific node on Builtin RP.";
		private const string NodeErrorMsg = "Only valid on URP/HDRP";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputName( 1, "t" );
			ChangeOutputName( 2, "sin(t)" );
			ChangeOutputName( 3, "cos(t)" );
			m_value = "_LastTimeParameters";
			m_previewShaderGUID = "aec3b3d089294f8a8467ec0f4a0c0f63";
			m_errorMessageTooltip = NodeErrorMsg;
			m_errorMessageTypeIsError = NodeMessageType.Error;
			ContinuousPreviewRefresh = true;
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			// _LastTimeParameters only holds ( t, sin(t), cos(t) ); the w component is unused.
			m_outputPorts[ 4 ].Visible = false;
			if ( !m_outputPorts[ 0 ].IsConnected )
			{
				m_outputPorts[ 0 ].Visible = false;
			}
			m_sizeIsDirty = true;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_showErrorMessage = ( ContainerGraph.CurrentCanvasMode == NodeAvailability.SurfaceShader ) ||
									( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader &&
										ContainerGraph.CurrentSRPType != TemplateSRPType.HDRP &&
										ContainerGraph.CurrentSRPType != TemplateSRPType.URP );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			if ( m_showErrorMessage )
			{
				EditorGUILayout.HelpBox( NodeErrorMsg, MessageType.Error );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( !dataCollector.IsTemplate || !( dataCollector.TemplateDataCollectorInstance.IsHDRP || dataCollector.TemplateDataCollectorInstance.IsURP ) )
			{
				UIUtils.ShowMessage( ErrorOnCompilationMsg, MessageSeverity.Error );
				return GenerateErrorValue( outputId );
			}

			return base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
		}
	}
}

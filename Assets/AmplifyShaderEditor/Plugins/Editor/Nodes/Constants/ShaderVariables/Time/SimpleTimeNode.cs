// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Time", "Time", "Time in seconds with a scale multiplier" )]
	public sealed class SimpleTimeNode : ShaderVariablesNode
	{
		private const string TimeStandard = "_Time.y";
		private const string TimeSRP = "_TimeParameters.x";

		// Extra Shader Graph parity outputs: sin(t), cos(t), delta time, smooth delta time
		private readonly string[] StandardExtraTime =
		{
			"_SinTime.w",
			"_CosTime.w",
			"unity_DeltaTime.x",
			"unity_DeltaTime.z",
		};

		private readonly string[] SRPExtraTime =
		{
			"_TimeParameters.y",
			"_TimeParameters.z",
			"unity_DeltaTime.x",
			"unity_DeltaTime.z",
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputProperties( 0, "t", WirePortDataType.FLOAT );
			AddOutputPort( WirePortDataType.FLOAT, "sin(t)" );
			AddOutputPort( WirePortDataType.FLOAT, "cos(t)" );
			AddOutputPort( WirePortDataType.FLOAT, "dt" );
			AddOutputPort( WirePortDataType.FLOAT, "smoothDt" );
			AddInputPort( WirePortDataType.FLOAT, false, "Scale" );
			m_inputPorts[ 0 ].FloatInternalData = 1;
			m_useInternalPortData = true;
			m_previewShaderGUID = "45b7107d5d11f124fad92bcb1fa53661";
			ContinuousPreviewRefresh = true;
		}

		public override void RenderNodePreview()
		{
			if ( !m_initialized )
			{
				PreviewIsDirty = false;
				return;
			}

			if ( !PreviewIsDirty && !ContinuousPreviewRefresh )
			{
				return;
			}

			SetPreviewInputs();

			if ( !Preferences.User.DisablePreviews )
			{
				PreparePooledPreview();

				RenderTexture temp = RenderTexture.active;
				int count = m_outputPorts.Count;
				for ( int i = 0; i < count; i++ )
				{
					m_outputPorts[ i ].BlitPreview( PreviewMaterial, i );
				}
				RenderTexture.active = temp;
			}

			PreviewIsDirty = ContinuousPreviewRefresh;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			bool isSRP = dataCollector.IsTemplate && ( dataCollector.TemplateDataCollectorInstance.IsHDRP || dataCollector.TemplateDataCollectorInstance.IsURP );

			// sin(t), cos(t), delta time and smooth delta time outputs are not affected by the Scale input.
			if ( outputId > 0 )
			{
				return isSRP ? SRPExtraTime[ outputId - 1 ] : StandardExtraTime[ outputId - 1 ];
			}

			string multiplier = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string timeGlobalVar = isSRP ? TimeSRP : TimeStandard;

			if ( multiplier == "1.0" )
			{
				return timeGlobalVar;
			}

			string scaledVarName = "mulTime" + OutputId;
			string scaledVarValue = timeGlobalVar + " * " + multiplier;
			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, scaledVarName, scaledVarValue );
			return scaledVarName;
		}
	}
}

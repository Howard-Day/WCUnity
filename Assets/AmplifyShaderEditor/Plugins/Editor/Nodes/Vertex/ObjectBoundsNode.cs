// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Object Bounds", "Object", "Object Bounds extracted from SRP per-object data" )]
	public class ObjectBoundsNode : ParentNode
	{
		public const string NodeErrorMsg = "This node requires Universal or High-definition rendering pipeline version 14.0.4 or higher.";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.FLOAT3, "Min" );
			AddOutputPort( WirePortDataType.FLOAT3, "Max" );
			AddOutputPort( WirePortDataType.FLOAT3, "Size" );
			m_drawPreviewAsSphere = true;
			m_textLabelWidth = 180;
			m_errorMessageTooltip = NodeErrorMsg;
			m_errorMessageTypeIsError = NodeMessageType.Error;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );

			bool isTemplate = ( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader );
			bool isSRP = ( ContainerGraph.CurrentSRPType == TemplateSRPType.URP || ContainerGraph.CurrentSRPType == TemplateSRPType.HDRP );
			bool isSRPCompatible = ( ASEPackageManagerHelper.PackageSRPVersion >= 140004 );
			m_showErrorMessage = !isTemplate || !isSRP || !isSRPCompatible;
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
			if ( m_showErrorMessage )
			{
				UIUtils.ShowMessage( NodeErrorMsg );
				return GenerateErrorValue();
			}
			
			if ( outputId == 0 )
			{
				return dataCollector.TemplateDataCollectorInstance.GenerateObjectBoundsMin( ref dataCollector, UniqueId );
			}
			else if ( outputId == 1 )
			{
				return dataCollector.TemplateDataCollectorInstance.GenerateObjectBoundsMax( ref dataCollector, UniqueId );
			}
			else
			{
				return dataCollector.TemplateDataCollectorInstance.GenerateObjectBoundsSize( ref dataCollector, UniqueId );
			}
		}
	}
}

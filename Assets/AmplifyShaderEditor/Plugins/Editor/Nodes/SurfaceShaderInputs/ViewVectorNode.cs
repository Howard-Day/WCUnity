// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "View Vector", "Camera And Screen", "Unnormalized View Direction vector.", tags: "camera vector" )]
	public sealed class ViewVectorNode : ParentNode
	{
		private const string SpaceStr = "Space";

		[SerializeField]
		private ViewSpace m_viewDirSpace = ViewSpace.World;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			AddOutputVectorPorts( WirePortDataType.FLOAT3, "XYZ" );
			//m_outputPorts[ 4 ].Visible = false;

			m_textLabelWidth = 120;
			m_autoWrapProperties = true;
			m_drawPreviewAsSphere = true;
			m_hasLeftDropdown = true;
			UpdateTitle();
			m_previewShaderGUID = "30c17cf801c339f489ed656ff9d66a5b";
		}

		private void UpdateTitle()
		{
			m_additionalContent.text = string.Format( Constants.SubTitleSpaceFormatStr, m_viewDirSpace.ToString() );
			m_sizeIsDirty = true;
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			m_upperLeftWidget.DrawWidget<ViewSpace>( ref m_viewDirSpace, this, OnWidgetUpdate );
		}

		private readonly Action<ParentNode> OnWidgetUpdate = ( x ) =>
		{
			( x as ViewVectorNode ).UpdateTitle();
		};

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_viewDirSpace = ( ViewSpace )EditorGUILayoutEnumPopup( SpaceStr, m_viewDirSpace );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateTitle();
			}
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			m_previewMaterialPassId = ( int )m_viewDirSpace;
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			base.PropagateNodeData( nodeData, ref dataCollector );
			if ( m_viewDirSpace == ViewSpace.Tangent )
			{
				dataCollector.DirtyNormal = true;
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			if ( dataCollector.IsTemplate )
			{
				TemplateDataCollector inst = dataCollector.TemplateDataCollectorInstance;
				string varName = inst.GetViewVector( useMasterNodeCategory: true, customCategory: MasterNodePortCategory.Fragment, space: m_viewDirSpace );
				return GetOutputVectorItem( 0, outputId, varName );
			}

			if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				string result = GeneratorUtils.GenerateViewVector( ref dataCollector, UniqueId, space: m_viewDirSpace );
				return GetOutputVectorItem( 0, outputId, result );
			}
			else
			{
				if ( m_viewDirSpace == ViewSpace.Tangent )
				{
					dataCollector.AddToInput( UniqueId, SurfaceInputs.WORLD_NORMAL, UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
					dataCollector.AddToInput( UniqueId, SurfaceInputs.INTERNALDATA, addSemiColon: false );
					dataCollector.ForceNormal = true;
				}
				dataCollector.AddToInput( UniqueId, SurfaceInputs.WORLD_POS );
				string result = GeneratorUtils.GenerateViewVector( ref dataCollector, UniqueId, space: m_viewDirSpace );
				return GetOutputVectorItem( 0, outputId, result );
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_viewDirSpace = ( ViewSpace )Enum.Parse( typeof( ViewSpace ), GetCurrentParam( ref nodeParams ) );
			UpdateTitle();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_viewDirSpace );
		}
	}
}

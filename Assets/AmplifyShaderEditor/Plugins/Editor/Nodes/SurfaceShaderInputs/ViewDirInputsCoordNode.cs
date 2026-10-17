// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	public enum ViewSpace
	{
		Tangent,
		World,
		Object,
		View
	}

	[Serializable]
	[NodeAttributes( "View Direction", "Camera And Screen", "Normalized View Direction vector.", tags: "camera vector" )]
	public sealed class ViewDirInputsCoordNode : SurfaceShaderINParentNode
	{
		private const string SpaceStr = "Space";
		private const string NormalizeOptionStr = "Safe Normalize";

		[SerializeField]
		private bool m_safeNormalize = false;

		[SerializeField]
		private ViewSpace m_viewDirSpace = ViewSpace.World;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_currentInput = SurfaceInputs.VIEW_DIR;
			InitialSetup();
			m_textLabelWidth = 120;
			m_autoWrapProperties = true;
			m_drawPreviewAsSphere = true;
			m_hasLeftDropdown = true;
			UpdateTitle();
			m_previewShaderGUID = "07b57d9823df4bd4d8fe6dcb29fca36a";
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
			( x as ViewDirInputsCoordNode ).UpdateTitle();
		};

		public override void DrawProperties()
		{
			//base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_viewDirSpace = (ViewSpace)EditorGUILayoutEnumPopup( SpaceStr, m_viewDirSpace );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateTitle();
			}
			m_safeNormalize = EditorGUILayoutToggle( NormalizeOptionStr, m_safeNormalize );
			EditorGUILayout.HelpBox( "Having safe normalize ON makes sure your view vector is not zero even if you are using your shader with no cameras.", MessageType.None );
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			m_previewMaterialPassId = ( int )m_viewDirSpace;
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			base.PropagateNodeData( nodeData, ref dataCollector );
			if( m_viewDirSpace == ViewSpace.Tangent )
				dataCollector.DirtyNormal = true;

			if( m_safeNormalize )
				dataCollector.SafeNormalizeViewDir = true;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			if( dataCollector.IsTemplate )
			{
				TemplateDataCollector inst = dataCollector.TemplateDataCollectorInstance;
				string varName = inst.GetViewDir(useMasterNodeCategory: true, customCategory: MasterNodePortCategory.Fragment, normalizeType: m_safeNormalize ? NormalizeType.Safe : NormalizeType.Regular, space: m_viewDirSpace );
				return GetOutputVectorItem( 0, outputId, varName );
			}


			if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				string result = GeneratorUtils.GenerateViewDirection( ref dataCollector, UniqueId, normalizeType: m_safeNormalize ? NormalizeType.Safe : NormalizeType.Regular, space: m_viewDirSpace );
				return GetOutputVectorItem( 0, outputId, result );
			}
			else
			{
				if ( m_viewDirSpace == ViewSpace.Tangent )
				{
					dataCollector.ForceNormal = true;
					string result = base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalVar );
					if ( m_safeNormalize )
					{
						result = TemplateHelperFunctions.SafeNormalize( dataCollector, result );
					}
					return result;
				}
				else
				{
					dataCollector.AddToInput( UniqueId, SurfaceInputs.WORLD_POS );
					string result = GeneratorUtils.GenerateViewDirection( ref dataCollector, UniqueId, m_safeNormalize ? NormalizeType.Safe : NormalizeType.Regular, space: m_viewDirSpace );
					return GetOutputVectorItem( 0, outputId, result );
				}

			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 2402 )
			{
				m_viewDirSpace = ( ViewSpace )Enum.Parse( typeof( ViewSpace ), GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 15201 )
			{
				m_safeNormalize = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			UpdateTitle();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_viewDirSpace );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_safeNormalize );
		}
	}
}

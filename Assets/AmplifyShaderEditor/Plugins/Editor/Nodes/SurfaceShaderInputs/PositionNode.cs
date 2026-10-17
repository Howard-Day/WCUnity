// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Position", "Surface Data", "Surface position. Absolute World-space, by default.\n\n" +
		"Object: Object-space position, local to object.\n\n" +
		"World: Absolute world position, global to scene origin.\n\n" +
		"Relative World: Camera-relative world position.\n\n" +
		"View: View-space position.\n\n" )]
	public sealed class PositionNode : SurfaceShaderINParentNode
	{
		public enum Space
		{
			Object,
			World,
			RelativeWorld,
			View
		}

		private readonly string[] m_outputSpaceStr = { "Object", "World", "Relative World", "View" };

		[SerializeField]
		private int m_outputSpaceIndex = ( int )Space.World;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_currentInput = SurfaceInputs.WORLD_POS;
			m_drawPreviewAsSphere = true;
			m_previewShaderGUID = "62a28fcb1f1dd5640a572771eb121a3b";
			m_hasLeftDropdown = true;
			m_textLabelWidth = 65;
			m_autoWrapProperties = true;
			InitialSetup();

			m_previewMaterialPassId = ( int )m_outputSpaceIndex;
			ConfigureHeader();
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			EditorGUI.BeginChangeCheck();
			m_outputSpaceIndex = m_upperLeftWidget.DrawWidget( this, m_outputSpaceIndex, m_outputSpaceStr );
			if ( EditorGUI.EndChangeCheck() )
			{
				ConfigureHeader();
			}
		}

		public override void DrawProperties()
		{
			EditorGUI.BeginChangeCheck();
			m_outputSpaceIndex = EditorGUILayoutPopup( "Mode", m_outputSpaceIndex, m_outputSpaceStr );
			if ( EditorGUI.EndChangeCheck() )
			{
				ConfigureHeader();
			}

			if ( m_outputSpaceIndex == ( int )Space.World )
			{
				EditorGUILayout.HelpBox( "World refers to absolute World-Space coordinates, in full coordinates around the origin.", MessageType.Info );
			}
			else if ( m_outputSpaceIndex == ( int )Space.RelativeWorld )
			{
				EditorGUILayout.HelpBox( "Relative World refers to Camera-relative World-space, where values are kept relative to the Camera position, making them less prone to the numerical precision degradation of large floating point values.", MessageType.Info );
			}
		}

		void ConfigureHeader()
		{
			SetAdditonalTitleText( string.Format( Constants.SubTitleModeFormatStr, m_outputSpaceStr[ m_outputSpaceIndex ] ) );
			m_previewMaterialPassId = m_outputSpaceIndex;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			if ( dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowNoVertexModeNodeMessage( this );
				return string.Format( "{0}( 0, 0, 0 )", UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, WirePortDataType.FLOAT3 ) );
			}

			if ( dataCollector.IsTemplate )
			{
				string varName = dataCollector.TemplateDataCollectorInstance.GetPosition( ( Space )m_outputSpaceIndex );
				return GetOutputVectorItem( 0, outputId, varName );
			}
			else
			{
				string worldPosition = GeneratorUtils.GeneratePosition( ref dataCollector, UniqueId, ( Space )m_outputSpaceIndex );
				return GetOutputVectorItem( 0, outputId, worldPosition );
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_outputSpaceIndex = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			ConfigureHeader();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_outputSpaceIndex );
		}
	}
}

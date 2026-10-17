// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;

using System;
namespace AmplifyShaderEditor
{
	enum POMTexTypes
	{
		Texture2D,
		Texture3D,
		TextureArray
	};

	[Serializable]
	[NodeAttributes( "Parallax Occlusion Mapping", "UV Coordinates", "Calculates offseted UVs for parallax occlusion mapping" )]
	public sealed class ParallaxOcclusionMappingNode : ParentNode
	{
		private const string ArrayIndexStr = "Array Index";
		private const string Tex3DSliceStr = "Tex3D Slice";

		private readonly string[] m_channelTypeStr = { "Red Channel", "Green Channel", "Blue Channel", "Alpha Channel" };
		private readonly string[] m_channelTypeVal = { "r", "g", "b", "a" };

		[SerializeField]
		private int m_selectedChannelInt = 0;

		//[SerializeField]
		//private int m_minSamples = 8;

		//[SerializeField]
		//private int m_maxSamples = 16;
		[SerializeField]
		private InlineProperty m_inlineMinSamples = new InlineProperty( 8 );

		[SerializeField]
		private InlineProperty m_inlineMaxSamples = new InlineProperty( 16 );

		[SerializeField]
		private int m_sidewallSteps = 2;

		[SerializeField]
		private float m_defaultScale = 0.02f;

		[SerializeField]
		private float m_defaultRefPlane = 0f;

		[SerializeField]
		private bool m_clipEnds = false;

		[SerializeField]
		private Vector2 m_tilling = new Vector2( 1, 1 );

		[SerializeField]
		private bool m_useCurvature = false;

		[SerializeField]
		private Vector2 m_CurvatureVector = new Vector2( 0, 0 );

		private string m_functionHeader = "POM( {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14} )";
		private string m_functionBody = string.Empty;

		//private const string WorldDirVarStr = "worldViewDir";

		private InputPort m_uvPort;
		private InputPort m_texPort;
		private InputPort m_ssPort;
		private InputPort m_scalePort;
		private InputPort m_minSamplesPort;
		private InputPort m_maxSamplesPort;
		private InputPort m_sidewallStepsPort;
		private InputPort m_refPlanePort;
		private InputPort m_curvaturePort;
		private InputPort m_arrayIndexPort;

		private OutputPort m_pomUVPort;

		private Vector4Node m_texCoordsHelper;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT2, false, "UV",-1,MasterNodePortCategory.Fragment,0);
			AddInputPort( WirePortDataType.SAMPLER2D, false, "Tex", -1, MasterNodePortCategory.Fragment, 1 );
			AddInputPort( WirePortDataType.SAMPLERSTATE, false, "SS", -1, MasterNodePortCategory.Fragment, 7 );
			AddInputPort( WirePortDataType.FLOAT, false, "Scale", -1, MasterNodePortCategory.Fragment, 2 );
			AddInputPort( WirePortDataType.INT, false, "Min Samples", -1, MasterNodePortCategory.Fragment, 8 );
			AddInputPort( WirePortDataType.INT, false, "Max Samples", -1, MasterNodePortCategory.Fragment, 9 );
			AddInputPort( WirePortDataType.INT, false, "Sidewall Steps", -1, MasterNodePortCategory.Fragment, 10 );
			AddInputPort( WirePortDataType.FLOAT, false, "Ref Plane", -1, MasterNodePortCategory.Fragment, 4 );
			AddInputPort( WirePortDataType.FLOAT2, false, "Curvature", -1, MasterNodePortCategory.Fragment, 5 );
			AddInputPort( WirePortDataType.FLOAT, false, ArrayIndexStr, -1, MasterNodePortCategory.Fragment, 6 );

			AddOutputPort( WirePortDataType.FLOAT2, "Out" );
			AddOutputPort( WirePortDataType.FLOAT, "Depth" );

			m_uvPort = GetInputPortByUniqueId( 0 );
			m_texPort = GetInputPortByUniqueId( 1 );
			m_texPort.CreatePortRestrictions( WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLER2DARRAY );
			m_ssPort = GetInputPortByUniqueId( 7 );
			m_ssPort.CreatePortRestrictions( WirePortDataType.SAMPLERSTATE );
			m_scalePort = GetInputPortByUniqueId( 2 );
			m_refPlanePort = GetInputPortByUniqueId( 4 );
			m_pomUVPort = m_outputPorts[ 0 ];
			m_curvaturePort = GetInputPortByUniqueId( 5 );
			m_arrayIndexPort = GetInputPortByUniqueId( 6 );
			m_minSamplesPort = GetInputPortByUniqueId( 8 );
			m_maxSamplesPort = GetInputPortByUniqueId( 9 );
			m_sidewallStepsPort = GetInputPortByUniqueId( 10 );

			m_scalePort.FloatInternalData = 0.02f;
			m_useInternalPortData = false;
			m_textLabelWidth = 130;
			m_autoWrapProperties = true;
			m_curvaturePort.Visible = false;
			m_arrayIndexPort.Visible = false;
			UpdateSampler();
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			m_texPort.MatchPortToConnection();
			UpdateIndexPort();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			m_texPort.MatchPortToConnection();
			UpdateIndexPort();
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			EditorGUI.BeginChangeCheck();
			m_selectedChannelInt = EditorGUILayoutPopup( "Channel", m_selectedChannelInt, m_channelTypeStr );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateSampler();
			}
			//EditorGUIUtility.labelWidth = 105;

			//m_minSamples = EditorGUILayoutIntSlider( "Min Samples", m_minSamples, 1, 128 );
			UndoParentNode inst = this;
			EditorGUI.BeginDisabledGroup( m_minSamplesPort.IsConnected );
			m_inlineMinSamples.CustomDrawer( ref inst, ( x ) => { m_inlineMinSamples.IntValue = EditorGUILayoutIntSlider( "Min Samples", m_inlineMinSamples.IntValue, 1, 128 ); }, "Min Samples" );
			EditorGUI.EndDisabledGroup();

			//m_maxSamples = EditorGUILayoutIntSlider( "Max Samples", m_maxSamples, 1, 128 );
			EditorGUI.BeginDisabledGroup( m_maxSamplesPort.IsConnected );
			m_inlineMaxSamples.CustomDrawer( ref inst, ( x ) => { m_inlineMaxSamples.IntValue = EditorGUILayoutIntSlider( "Max Samples", m_inlineMaxSamples.IntValue, 1, 128 ); }, "Max Samples" );
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup( m_sidewallStepsPort.IsConnected );
			m_sidewallSteps = EditorGUILayoutIntSlider( "Sidewall Steps", m_sidewallSteps, 0, 10 );
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(m_scalePort.IsConnected );
			m_defaultScale = EditorGUILayoutSlider( "Default Scale", m_defaultScale, 0, 1 );
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup( m_refPlanePort.IsConnected );
			m_defaultRefPlane = EditorGUILayoutSlider( "Default Ref Plane", m_defaultRefPlane, 0, 1 );
			EditorGUI.EndDisabledGroup();
			//EditorGUIUtility.labelWidth = m_textLabelWidth;

			if( m_arrayIndexPort.Visible && !m_arrayIndexPort.IsConnected )
			{
				m_arrayIndexPort.FloatInternalData = EditorGUILayoutFloatField( "Array Index", m_arrayIndexPort.FloatInternalData );
			}

			//float cached = EditorGUIUtility.labelWidth;
			//EditorGUIUtility.labelWidth = 70;
			m_clipEnds = EditorGUILayoutToggle( "Clip Edges", m_clipEnds );
			//EditorGUIUtility.labelWidth = -1;
			//EditorGUIUtility.labelWidth = 100;
			//EditorGUILayout.BeginHorizontal();
			//EditorGUI.BeginDisabledGroup( !m_clipEnds );
			//m_tilling = EditorGUILayout.Vector2Field( string.Empty, m_tilling );
			//EditorGUI.EndDisabledGroup();
			//EditorGUILayout.EndHorizontal();
			//EditorGUIUtility.labelWidth = cached;

			EditorGUI.BeginChangeCheck();
			m_useCurvature = EditorGUILayoutToggle( "Clip Silhouette", m_useCurvature );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateCurvaturePort();
			}

			EditorGUI.BeginDisabledGroup( !(m_useCurvature && !m_curvaturePort.IsConnected) );
			m_CurvatureVector = EditorGUILayoutVector2Field( string.Empty, m_CurvatureVector );
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.HelpBox( "WARNING:\nTex must be connected to a Texture Object for this node to work\n\nMin and Max samples:\nControl the minimum and maximum number of layers extruded\n\nSidewall Steps:\nThe number of interpolations done to smooth the extrusion result on the side of the layer extrusions, min is used at steep angles while max is used at orthogonal angles\n\n"+
				"Ref Plane:\nReference plane lets you adjust the starting reference height, 0 = deepen ground, 1 = raise ground, any value above 0 might cause distortions at higher angles\n\n"+
				"Clip Edges:\nThis will clip the ends of your uvs to give a more 3D look at the edges. It'll use the tilling given by your Heightmap input.\n\n"+
				"Clip Silhouette:\nTurning this on allows you to use the UV coordinates to clip the effect curvature in U or V axis, useful for cylinders, works best with 'Clip Edges' turned OFF", MessageType.None );
		}

		private void UpdateIndexPort()
		{
			m_arrayIndexPort.Visible = m_texPort.DataType != WirePortDataType.SAMPLER2D;
			if( m_arrayIndexPort.Visible )
			{
				m_arrayIndexPort.Name = m_texPort.DataType == WirePortDataType.SAMPLER3D ? Tex3DSliceStr : ArrayIndexStr;
			}
			SizeIsDirty = true;
		}

		private void UpdateSampler()
		{
			m_texPort.Name = "Tex (" + m_channelTypeVal[ m_selectedChannelInt ].ToUpper() + ")";
		}

		private void UpdateCurvaturePort()
		{
			if ( m_useCurvature )
				m_curvaturePort.Visible = true;
			else
				m_curvaturePort.Visible = false;

			m_sizeIsDirty = true;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			CheckCustomSamplerUsable();
		}

		// @diogo: the wired sampler state only reaches codegen through the macro path, which
		// plain 2D textures skip entirely
		private void CheckCustomSamplerUsable()
		{
			bool usesMacros = ( UIUtils.CurrentWindow.OutsideGraph.SamplingMacros || m_texPort.DataType == WirePortDataType.SAMPLER2DARRAY ) && !GeneratorUtils.IsLegacyTemplateSampler( m_texPort );
			if ( !usesMacros && m_ssPort.IsConnected && ContainerGraph.CurrentShaderFunction == null )
			{
				ShowTab( NodeMessageType.Warning, SamplerStateNode.MessageCustomSamplerIgnored );
			}
			else
			{
				HideTab();
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( !m_texPort.IsConnected )
			{
				UIUtils.ShowMessage( UniqueId, "Parallax Occlusion Mapping node only works if a Texture Object is connected to its Tex (R) port" );
				return m_outputPorts[ outputId ].ErrorValue;
			}
			if ( m_outputPorts[ outputId ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
			}
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			// @diogo: a template-declared legacy sampler can only be sampled through the texXD intrinsics (#241)
			bool legacySampler = GeneratorUtils.IsLegacyTemplateSampler( m_texPort );

			string arrayIndex = m_arrayIndexPort.Visible?m_arrayIndexPort.GeneratePortInstructions( ref dataCollector ):"0";
			string textcoords = m_uvPort.GeneratePortInstructions( ref dataCollector );
			if( m_texPort.DataType == WirePortDataType.SAMPLER3D )
			{
				string texName = "pomTexCoord" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, texName, string.Format( "float3({0},{1})", textcoords, arrayIndex ) );
				textcoords = texName;
			}

			bool isHDRP = ( dataCollector.IsSRP && dataCollector.CurrentSRPType == TemplateSRPType.HDRP );
			bool isURP = ( dataCollector.IsSRP && dataCollector.CurrentSRPType == TemplateSRPType.URP );

			string texture = m_texPort.GeneratePortInstructions( ref dataCollector );
			GeneratePOMfunction( ref dataCollector );

			string scale = m_defaultScale.ToString();
			if( m_scalePort.IsConnected )
			{
				scale = m_scalePort.GeneratePortInstructions( ref dataCollector );
			}

			// positionCS is only consumed inside the depthUsed block below; acquire it there so the standard
			// surface (non-template) path never touches TemplateDataCollectorInstance, whose m_currentDataCollector
			// is null outside template generation ( GetClipPos would NRE ).
			string positionCS = string.Empty;
			string positionWS = string.Empty;

			bool depthUsed = ( outputId == 1 || m_outputPorts[ 1 ].IsConnected );
			if ( depthUsed )
			{
				if ( dataCollector.IsTemplate )
				{
					positionWS = dataCollector.TemplateDataCollectorInstance.GetPosition( isHDRP ? PositionNode.Space.RelativeWorld : PositionNode.Space.World );
					positionCS = dataCollector.TemplateDataCollectorInstance.GetClipPos();
				}
				else
				{
					positionWS = GeneratorUtils.GenerateWorldPosition( ref dataCollector, UniqueId );
					positionCS = "pomClipPos" + OutputId;
				}

				dataCollector.AddToDefines( UniqueId, "ASE_CHANGES_WORLD_POS" );
			}

			string viewDirTS = string.Empty;
			string viewDirWS = string.Empty;
			if ( depthUsed )
			{
				if ( dataCollector.IsSRP )
				{
					string camViewWS = dataCollector.TemplateDataCollectorInstance.GetViewDir( CurrentPrecisionType, space: ViewSpace.World );
					string camViewTS = dataCollector.TemplateDataCollectorInstance.GetViewDir( CurrentPrecisionType, space: ViewSpace.Tangent );

					if ( isURP )
					{
						// In the shadow caster UNITY_MATRIX_VP is rebound to the light, so the parallax must
						// march along the light, not the camera. _LightDirection (directional) / _LightPosition
						// (punctual) point surface->light, matching the surface->eye convention used below. The
						// pass is selected by the preprocessor so the same fragment code is valid in every pass.
						string viewBody = string.Empty;
						IOUtils.AddFunctionHeader( ref viewBody, "inline float3 POMViewDirWorld( float3 positionWS, float3 cameraViewWS )" );
						IOUtils.AddFunctionLine( ref viewBody, "#if ( SHADERPASS == SHADERPASS_SHADOWCASTER )" );
						IOUtils.AddFunctionLine( ref viewBody, "#if defined( _CASTING_PUNCTUAL_LIGHT_SHADOW )" );
						IOUtils.AddFunctionLine( ref viewBody, "return normalize( _LightPosition - positionWS );" );
						IOUtils.AddFunctionLine( ref viewBody, "#else" );
						IOUtils.AddFunctionLine( ref viewBody, "return _LightDirection;" );
						IOUtils.AddFunctionLine( ref viewBody, "#endif" );
						IOUtils.AddFunctionLine( ref viewBody, "#else" );
						IOUtils.AddFunctionLine( ref viewBody, "return cameraViewWS;" );
						IOUtils.AddFunctionLine( ref viewBody, "#endif" );
						IOUtils.CloseFunctionBody( ref viewBody );

						viewDirWS = "pomViewWorld" + OutputId;
						dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, viewDirWS,
							dataCollector.AddFunctions( "POMViewDirWorld( {0}, {1} )", viewBody, positionWS, camViewWS ) );

						// Camera tangent view dir is kept for non-shadow passes (forward depth) so their result
						// is byte-for-byte unchanged; the shadow caster rebuilds it from the light direction.
						string worldToTangent = dataCollector.TemplateDataCollectorInstance.GetWorldToTangentMatrix( CurrentPrecisionType );
						string tanBody = string.Empty;
						IOUtils.AddFunctionHeader( ref tanBody, "inline float3 POMViewDirTangent( float3 cameraViewTS, float3 viewDirWS, float3x3 worldToTangent )" );
						IOUtils.AddFunctionLine( ref tanBody, "#if ( SHADERPASS == SHADERPASS_SHADOWCASTER )" );
						IOUtils.AddFunctionLine( ref tanBody, "return mul( worldToTangent, viewDirWS );" );
						IOUtils.AddFunctionLine( ref tanBody, "#else" );
						IOUtils.AddFunctionLine( ref tanBody, "return cameraViewTS;" );
						IOUtils.AddFunctionLine( ref tanBody, "#endif" );
						IOUtils.CloseFunctionBody( ref tanBody );

						viewDirTS = "pomViewTan" + OutputId;
						dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, viewDirTS,
							dataCollector.AddFunctions( "POMViewDirTangent( {0}, {1}, {2} )", tanBody, camViewTS, viewDirWS, worldToTangent ) );
					}
					else if ( isHDRP )
					{
						// HDRP renders shadows in camera-relative space anchored to the MAIN camera, so GetViewDir
						// still points at the camera, not the light. The shadow view lives in UNITY_MATRIX_V, so
						// march along it: an orthographic shadow projection (directional light) uses the parallel
						// view forward, a perspective one (punctual) uses the direction to the shadow-view origin
						// from the inverse view matrix. Pass selected by the preprocessor.
						string viewBody = string.Empty;
						IOUtils.AddFunctionHeader( ref viewBody, "inline float3 POMViewDirWorld( float3 positionRWS, float3 cameraViewWS )" );
						IOUtils.AddFunctionLine( ref viewBody, "#if ( SHADERPASS == SHADERPASS_SHADOWS )" );
						IOUtils.AddFunctionLine( ref viewBody, "if ( UNITY_MATRIX_P._m33 > 0.5 )" );
						IOUtils.AddFunctionLine( ref viewBody, "return UNITY_MATRIX_V[ 2 ].xyz;" );
						IOUtils.AddFunctionLine( ref viewBody, "return normalize( UNITY_MATRIX_I_V._m03_m13_m23 - positionRWS );" );
						IOUtils.AddFunctionLine( ref viewBody, "#else" );
						IOUtils.AddFunctionLine( ref viewBody, "return cameraViewWS;" );
						IOUtils.AddFunctionLine( ref viewBody, "#endif" );
						IOUtils.CloseFunctionBody( ref viewBody );

						viewDirWS = "pomViewWorld" + OutputId;
						dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, viewDirWS,
							dataCollector.AddFunctions( "POMViewDirWorld( {0}, {1} )", viewBody, positionWS, camViewWS ) );

						string worldToTangent = dataCollector.TemplateDataCollectorInstance.GetWorldToTangentMatrix( CurrentPrecisionType );
						string tanBody = string.Empty;
						IOUtils.AddFunctionHeader( ref tanBody, "inline float3 POMViewDirTangent( float3 cameraViewTS, float3 viewDirWS, float3x3 worldToTangent )" );
						IOUtils.AddFunctionLine( ref tanBody, "#if ( SHADERPASS == SHADERPASS_SHADOWS )" );
						IOUtils.AddFunctionLine( ref tanBody, "return mul( worldToTangent, viewDirWS );" );
						IOUtils.AddFunctionLine( ref tanBody, "#else" );
						IOUtils.AddFunctionLine( ref tanBody, "return cameraViewTS;" );
						IOUtils.AddFunctionLine( ref tanBody, "#endif" );
						IOUtils.CloseFunctionBody( ref tanBody );

						viewDirTS = "pomViewTan" + OutputId;
						dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, viewDirTS,
							dataCollector.AddFunctions( "POMViewDirTangent( {0}, {1}, {2} )", tanBody, camViewTS, viewDirWS, worldToTangent ) );
					}
					else
					{
						viewDirWS = camViewWS;
						viewDirTS = camViewTS;
					}
				}
				else
				{
					// For correct shadows the parallax must march along the LIGHT in the shadow caster
					// pass (only UNITY_MATRIX_VP is rebound to the light there, not the view matrix).
					// Source the per-pass eye direction directly: light direction while casting shadows,
					// camera view direction otherwise.
					string viewBody = string.Empty;
					IOUtils.AddFunctionHeader( ref viewBody, "inline float3 POMViewDirWorld( float3 positionWS )" );
					IOUtils.AddFunctionLine( ref viewBody, "#if defined( UNITY_PASS_SHADOWCASTER )" );
					IOUtils.AddFunctionLine( ref viewBody, "if ( unity_LightShadowBias.x != 0.0 )" );
					IOUtils.AddFunctionLine( ref viewBody, "return normalize( UnityWorldSpaceLightDir( positionWS ) );" );
					IOUtils.AddFunctionLine( ref viewBody, "#endif" );
					IOUtils.AddFunctionLine( ref viewBody, "return normalize( UnityWorldSpaceViewDir( positionWS ) );" );
					IOUtils.CloseFunctionBody( ref viewBody );
					string viewCall = dataCollector.AddFunctions( "POMViewDirWorld( {0} )", viewBody, positionWS );

					viewDirWS = "pomViewWorld" + OutputId;
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, viewDirWS, viewCall );

					string worldToTangent = GeneratorUtils.GenerateWorldToTangentMatrix( ref dataCollector, UniqueId, CurrentPrecisionType );
					viewDirTS = "pomViewTan" + OutputId;
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, viewDirTS,
						"mul( " + worldToTangent + ", " + viewDirWS + " )" );
				}
			}
			else
			{
				if ( !dataCollector.DirtyNormal )
				{
					dataCollector.ForceNormal = true;
				}

				if ( dataCollector.IsTemplate )
				{
					viewDirWS = dataCollector.TemplateDataCollectorInstance.GetViewDir( CurrentPrecisionType, space: ViewSpace.World );
					viewDirTS = dataCollector.TemplateDataCollectorInstance.GetViewDir( CurrentPrecisionType, space: ViewSpace.Tangent );
				}
				else
				{
					viewDirWS = GeneratorUtils.GenerateViewDirection( ref dataCollector, UniqueId, space: ViewSpace.World );
					viewDirTS = GeneratorUtils.GenerateViewDirection( ref dataCollector, UniqueId, space: ViewSpace.Tangent );
				}
			}

			// min/max samples
			string minSamples = m_minSamplesPort.IsConnected ? m_minSamplesPort.GeneratePortInstructions( ref dataCollector ) : m_inlineMinSamples.GetValueOrProperty( false );
			string maxSamples = m_maxSamplesPort.IsConnected ? m_maxSamplesPort.GeneratePortInstructions( ref dataCollector ) : m_inlineMaxSamples.GetValueOrProperty( false );
			string sidewallSteps = m_sidewallStepsPort.IsConnected ? m_sidewallStepsPort.GeneratePortInstructions( ref dataCollector ) : m_sidewallSteps.ToString();

			//generate world normal
			string normalWS = string.Empty;
			if ( dataCollector.IsTemplate )
			{
				normalWS = dataCollector.TemplateDataCollectorInstance.GetWorldNormal( CurrentPrecisionType );
			}
			else
			{
				dataCollector.AddToInput( UniqueId, SurfaceInputs.WORLD_NORMAL, UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
				dataCollector.AddToInput( UniqueId, SurfaceInputs.INTERNALDATA, addSemiColon: false );
				normalWS = GeneratorUtils.GenerateWorldNormal( ref dataCollector, UniqueId );
			}

			string dx = "ddx( " + textcoords + " )";
			string dy = "ddy( " + textcoords + " )";
			dataCollector.NotifyDivergenceHazard( UniqueId );

			string refPlane = m_defaultRefPlane.ToString();
			if ( m_refPlanePort.IsConnected )
				refPlane = m_refPlanePort.GeneratePortInstructions( ref dataCollector );


			string curvature = "float2( "+ m_CurvatureVector.x + ", " + m_CurvatureVector.y + " )";
			if ( m_useCurvature )
			{
				dataCollector.AddToProperties( UniqueId, "[Header( Parallax Occlusion Mapping )]", 300 );
				dataCollector.AddToProperties( UniqueId, "_CurvFix(\"Curvature Bias\", Range( 0, 1 ) ) = 1", 301 );
				dataCollector.AddToUniforms( UniqueId, "float _CurvFix;", true );

				if ( m_curvaturePort.IsConnected )
					curvature = m_curvaturePort.GeneratePortInstructions( ref dataCollector );
			}


			string localVarName = "OffsetPOM" + OutputId;
			string textCoordsST = string.Empty;
			//string textureSTType = dataCollector.IsSRP ? "float4 " : "uniform float4 ";
			//dataCollector.AddToUniforms( UniqueId, textureSTType + texture +"_ST;");
			if( m_texCoordsHelper == null )
			{
				m_texCoordsHelper = CreateInstance<Vector4Node>();
				m_texCoordsHelper.ContainerGraph = ContainerGraph;
				m_texCoordsHelper.SetBaseUniqueId( UniqueId, true );
				m_texCoordsHelper.RegisterPropertyOnInstancing = false;
			}

			var textureProperty = m_texPort.GetOutputNodeWhichIsNotRelay( 0 ) as TexturePropertyNode;
			m_texCoordsHelper.AddGlobalToSRPBatcher = ( textureProperty != null ) ? ( textureProperty.CurrentParameterType != PropertyType.Global ) : true;
			m_texCoordsHelper.CurrentParameterType = PropertyType.Global;
			m_texCoordsHelper.SetRawVariableMode( ( textureProperty != null ) ? textureProperty.CurrentVariableMode : VariableMode.Create );

			m_texCoordsHelper.ResetOutputLocals();
			m_texCoordsHelper.SetRawPropertyName( texture + "_ST" );
			textCoordsST = m_texCoordsHelper.GenerateShaderForOutput( 0, ref dataCollector, false );
			//////

			string textureArgs = string.Empty;
			if( !legacySampler && ( outsideGraph.SamplingMacros || m_texPort.DataType == WirePortDataType.SAMPLER2DARRAY ) )
			{
				string sampler = string.Empty;
				if( m_ssPort.IsConnected )
				{
					sampler = m_ssPort.GeneratePortInstructions( ref dataCollector );
				}
				else
				{
					sampler = GeneratorUtils.GenerateSamplerState( ref dataCollector, UniqueId, texture, ( textureProperty != null ) ? textureProperty.CurrentVariableMode : VariableMode.Create );
				}
				if( outsideGraph.IsSRP )
				{
					textureArgs = texture + ", " + sampler;
				}
				else
				{
					textureArgs = texture + ", " + sampler;
				}
			}
			else
			{
				if ( m_ssPort.IsConnected )
				{
					dataCollector.ShowPassWarning( UniqueId, SamplerStateNode.MessageCustomSamplerIgnored );
				}
				textureArgs = texture;
			}
			//string functionResult = dataCollector.AddFunctions( m_functionHeader, m_functionBody, ( (m_pomTexType == POMTexTypes.TextureArray) ? "UNITY_PASS_TEX2DARRAY(" + texture + ")": texture), textcoords, dx, dy, normalWorld, worldViewDir, viewDirTan, m_minSamples, m_maxSamples, scale, refPlane, texture+"_ST.xy", curvature, arrayIndex );
			string functionResult = dataCollector.AddFunctions( m_functionHeader, m_functionBody, textureArgs, textcoords, dx, dy, normalWS, viewDirWS, viewDirTS, minSamples, maxSamples, sidewallSteps, scale, refPlane, textCoordsST + ".xy", curvature, arrayIndex );

			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, localVarName, functionResult );
			m_outputPorts[ 0 ].SetLocalValue( localVarName + ".xy", dataCollector.PortCategory );

			if ( depthUsed )
			{
				// Device depth (clipPos.z/clipPos.w) of the parallax-displaced surface point.
				// The hit lies along the (pass-correct) view ray, pushed into the surface by the parallax
				// height. The tangent-space height is converted to world units automatically from the
				// screen-space derivatives of the world position (auto via worldPos derivatives).
				string uvForDepth = "( " + textcoords + " ).xy";
				string worldPerUV = "pomWorldPerUV" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, worldPerUV,
					"length( ddx( " + positionWS + " ) + ddy( " + positionWS + " ) ) / max( length( ddx( " + uvForDepth + " ) + ddy( " + uvForDepth + " ) ), 1e-5 )" );

				string worldHeight = "pomWorldHeight" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, worldHeight,
					localVarName + ".z * " + scale + " * " + worldPerUV );

				// worldViewDir is the current pass's eye direction (camera or light), so the displacement
				// and the clip-space projection below stay consistent and shadows do not swim with the camera.
				string ndotv = "pomNdotV" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, ndotv,
					"dot( " + viewDirWS + ", " + normalWS + " )" );

				if ( isHDRP )
				{
					// POM self-shadow acne fix ( HDRP ). The shadow caster marches the height field along the
					// light while the receiver marches along the camera, so the two displaced surfaces disagree
					// by about one marching layer - a fixed WORLD-space gap. HDRP's normal bias ( receiver ) and
					// slope-scale depth bias ( caster ) both scale with the shadow texel size, so they shrink to
					// nothing at high shadow resolution and the gap shows up as acne. Push the caster hit half a
					// layer further from the light ( resolution-independent ) so the receiver's own surface stays
					// in front of the stored depth. Lower the 0.5 factor if it ever reads as peter-panning.
					string shadowSteps = "pomShadowSteps" + OutputId;
					dataCollector.AddLocalVariable( UniqueId, "#if ( SHADERPASS == SHADERPASS_SHADOWS )", true );
					dataCollector.AddLocalVariable( UniqueId, "float " + shadowSteps + " = floor( lerp( (float)" + maxSamples + ", (float)" + minSamples + ", saturate( " + ndotv + " ) ) );" );
					dataCollector.AddLocalVariable( UniqueId, worldHeight + " += 0.5 * " + scale + " * " + worldPerUV + " / max( " + shadowSteps + ", 1.0 );" );
					dataCollector.AddLocalVariable( UniqueId, "#endif", true );
				}

				// At grazing eye/light angles the 1/cos term explodes and the hit shoots sideways ( the old
				// max() clamped the denominator, keeping it huge instead of bounded ). Zero the displacement
				// past the horizon so the shadow stops drifting along the light.
				string rayDist = "pomRayDist" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, rayDist,
					"( " + ndotv + " > 1e-6 ) ? ( " + worldHeight + " / " + ndotv + " ) : 0" );

				string basePos = positionWS;
				if ( !dataCollector.IsSRP )
				{
					// Writing device depth in the shadow caster replaces the rasterized depth, so the vertex
					// normal-offset shadow bias is lost. Re-apply it here, scaled by the sine between normal
					// and light so it vanishes head-on and peaks at grazing ( Witness normal-offset technique ).
					string biasBody = string.Empty;
					IOUtils.AddFunctionHeader( ref biasBody, "inline float3 POMShadowBiasPos( float3 positionWS, float3 normalWS, float3 lightWS )" );
					IOUtils.AddFunctionLine( ref biasBody, "#if defined( UNITY_PASS_SHADOWCASTER )" );
					IOUtils.AddFunctionLine( ref biasBody, "if ( unity_LightShadowBias.x != 0.0 )" );
					IOUtils.AddFunctionLine( ref biasBody, "{" );
					IOUtils.AddFunctionLine( ref biasBody, "float shadowCos = dot( normalWS, lightWS );" );
					IOUtils.AddFunctionLine( ref biasBody, "float shadowSine = sqrt( saturate( 1.0 - shadowCos * shadowCos ) );" );
					IOUtils.AddFunctionLine( ref biasBody, "positionWS -= normalWS * shadowSine * abs( unity_LightShadowBias.x );" );
					IOUtils.AddFunctionLine( ref biasBody, "}" );
					IOUtils.AddFunctionLine( ref biasBody, "#endif" );
					IOUtils.AddFunctionLine( ref biasBody, "return positionWS;" );
					IOUtils.CloseFunctionBody( ref biasBody );
					basePos = "pomBiasedPos" + OutputId;
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, basePos,
						dataCollector.AddFunctions( "POMShadowBiasPos( {0}, {1}, {2} )", biasBody, positionWS, normalWS, viewDirWS ) );
				}
				dataCollector.AddLocalVariable( UniqueId, $"{positionWS} = {basePos} - {viewDirWS} * {rayDist};" );

				string clipExpr;
				if ( isURP )
				{
					// Writing device depth replaces the rasterized depth, which in the shadow caster was
					// biased in the vertex via ApplyShadowBias + the near-clip clamp. Re-apply both to the
					// displaced point so the depth lines up with the rest of the geometry's shadow map.
					string shadowClipBody = string.Empty;
					IOUtils.AddFunctionHeader( ref shadowClipBody, "inline float4 POMClipPos( float3 positionWS, float3 normalWS, float3 lightDir )" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "#if ( SHADERPASS == SHADERPASS_SHADOWCASTER )" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "float4 positionCS = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, lightDir ) );" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "#if UNITY_REVERSED_Z" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "positionCS.z = min( positionCS.z, UNITY_NEAR_CLIP_VALUE );" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "#else" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "positionCS.z = max( positionCS.z, UNITY_NEAR_CLIP_VALUE );" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "#endif" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "return positionCS;" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "#else" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "return TransformWorldToHClip( positionWS );" );
					IOUtils.AddFunctionLine( ref shadowClipBody, "#endif" );
					IOUtils.CloseFunctionBody( ref shadowClipBody );
					clipExpr = dataCollector.AddFunctions( "POMClipPos( {0}, {1}, {2} )", shadowClipBody, positionWS, normalWS, viewDirWS );
				}
				else if ( isHDRP )
				{
					clipExpr = "TransformWorldToHClip( " + positionWS + " )";
				}
				else
				{
					// UnityWorldToClipPos is the light's VP in the shadow caster pass; apply the same linear
					// shadow bias the geometry uses so the displaced depth lines up with the shadow map.
					string clipBody = string.Empty;
					IOUtils.AddFunctionHeader( ref clipBody, "inline float4 POMClipPos( float3 positionWS )" );
					IOUtils.AddFunctionLine( ref clipBody, "float4 positionCS = UnityWorldToClipPos( positionWS );" );
					IOUtils.AddFunctionLine( ref clipBody, "#if defined( UNITY_PASS_SHADOWCASTER )" );
					IOUtils.AddFunctionLine( ref clipBody, "positionCS = UnityApplyLinearShadowBias( positionCS );" );
					IOUtils.AddFunctionLine( ref clipBody, "#endif" );
					IOUtils.AddFunctionLine( ref clipBody, "return positionCS;" );
					IOUtils.CloseFunctionBody( ref clipBody );
					clipExpr = dataCollector.AddFunctions( "POMClipPos( {0} )", clipBody, positionWS );
				}
				if ( dataCollector.IsTemplate )
					dataCollector.AddLocalVariable( UniqueId, $"{positionCS} = {clipExpr};" );
				else
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT4, positionCS, clipExpr );

				string deviceDepth = "pomDeviceDepth" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, deviceDepth,
					positionCS + ".z / " + positionCS + ".w" );

				if ( isHDRP )
				{
					// HDRP applies the shadow slope-scale depth bias through the rasterizer ( SetGlobalDepthBias ),
					// which is bypassed when the fragment writes SV_Depth manually, so the displaced depth lands
					// unbiased and self-shadows ( acne that grows with the shadow texel footprint as the camera
					// pulls back ). Re-apply it exactly as HDRP's own ShaderPassDepthOnly does under _DEPTHOFFSET_ON.
					dataCollector.AddLocalVariable( UniqueId, "#if ( SHADERPASS == SHADERPASS_SHADOWS )", true );
					dataCollector.AddLocalVariable( UniqueId, deviceDepth + " += max( abs( ddx( " + deviceDepth + " ) ), abs( ddy( " + deviceDepth + " ) ) ) * _SlopeScaleDepthBias;" );
					dataCollector.AddLocalVariable( UniqueId, "#endif", true );
				}

				m_outputPorts[ 1 ].SetLocalValue( deviceDepth, dataCollector.PortCategory );
			}

			return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
		}

		private void GeneratePOMfunction( ref MasterNodeDataCollector dataCollector )
		{
			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			// @diogo: a template-declared legacy sampler can only be sampled through the texXD intrinsics (#241)
			bool legacySampler = GeneratorUtils.IsLegacyTemplateSampler( m_texPort );
			m_functionBody = string.Empty;
			switch( m_texPort.DataType )
			{
				default:
				case WirePortDataType.SAMPLER2D:
				{
					string sampleParam = string.Empty;
					sampleParam = GeneratorUtils.GetPropertyDeclaraction( "heightMap", TextureType.Texture2D, ", ", legacySampler ) + GeneratorUtils.GetSamplerDeclaraction( "samplerheightMap", TextureType.Texture2D, ", ", legacySampler );
					IOUtils.AddFunctionHeader( ref m_functionBody, string.Format("inline float3 POM( {0}float2 uvs, float2 dx, float2 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, int sidewallSteps, float parallax, float refPlane, float2 tilling, float2 curv, int index )", sampleParam ));
				}
				break;
				case WirePortDataType.SAMPLER3D:
				{
					string sampleParam = string.Empty;
					sampleParam = GeneratorUtils.GetPropertyDeclaraction( "heightMap", TextureType.Texture3D, ", ", legacySampler ) + GeneratorUtils.GetSamplerDeclaraction( "samplerheightMap", TextureType.Texture3D, ", ", legacySampler );
					IOUtils.AddFunctionHeader( ref m_functionBody, string.Format( "inline float3 POM( {0}float3 uvs, float3 dx, float3 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, int sidewallSteps, float parallax, float refPlane, float2 tilling, float2 curv, int index )", sampleParam ) );
				}
				break;
				case WirePortDataType.SAMPLER2DARRAY:
				if( outsideGraph.IsSRP )
					IOUtils.AddFunctionHeader( ref m_functionBody, "inline float3 POM( TEXTURE2D_ARRAY(heightMap), SAMPLER(samplerheightMap), float2 uvs, float2 dx, float2 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, int sidewallSteps, float parallax, float refPlane, float2 tilling, float2 curv, int index )" );
				else
					IOUtils.AddFunctionHeader( ref m_functionBody, "inline float3 POM( UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(heightMap), SamplerState samplerheightMap, float2 uvs, float2 dx, float2 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, int sidewallSteps, float parallax, float refPlane, float2 tilling, float2 curv, int index )" );
				break;
			}

			IOUtils.AddFunctionLine( ref m_functionBody, "float3 result = 0;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float stepIndex = 0;" );
			//IOUtils.AddFunctionLine( ref m_functionBody, "float numSteps = ( float )( minSamples + dot( viewWorld, normalWorld ) * ( maxSamples - minSamples ) );" );
			//IOUtils.AddFunctionLine( ref m_functionBody, "float numSteps = ( float )lerp( maxSamples, minSamples, length( fwidth( uvs ) ) * 10 );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float numSteps = floor( lerp( (float)maxSamples, (float)minSamples, saturate( dot( normalWorld, viewWorld ) ) ) );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float layerHeight = 1.0 / numSteps;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float2 plane = parallax * ( viewDirTan.xy / viewDirTan.z );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "uvs.xy += refPlane * plane;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float2 deltaTex = -plane * layerHeight;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float2 prevTexOffset = 0;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float prevRayZ = 1.0f;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float prevHeight = 0.0f;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float2 currTexOffset = deltaTex;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float currRayZ = 1.0f - layerHeight;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float currHeight = 0.0f;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float intersection = 0;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float2 finalTexOffset = 0;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "while ( stepIndex < numSteps + 1 )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "{" );

			string textureProp = "heightMap";
			string sampleState = "samplerheightMap";

			string uvs = "uvs + currTexOffset";
			if( m_texPort.DataType == WirePortDataType.SAMPLER3D )
				uvs = "float3(uvs.xy + currTexOffset, uvs.z)";
			else if( m_texPort.DataType == WirePortDataType.SAMPLER2DARRAY )
				uvs = outsideGraph.IsSRP ? uvs + ", index" : "float3(" + uvs + ", index)";

			string samplingCall = GeneratorUtils.GenerateSamplingCall( ref dataCollector, m_texPort.DataType, textureProp, sampleState, uvs, legacySampler, MipType.Derivative, "dx", "dy" );
			if( m_useCurvature )
			{
				IOUtils.AddFunctionLine( ref m_functionBody, " \tresult.z = dot( curv, currTexOffset * currTexOffset );" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tcurrHeight = " + samplingCall + "." + m_channelTypeVal[ m_selectedChannelInt ] + " * ( 1 - result.z );" );
			}
			else
			{
				IOUtils.AddFunctionLine( ref m_functionBody, " \tcurrHeight = " + samplingCall + "." + m_channelTypeVal[ m_selectedChannelInt ] + ";" );
			}
			// The top of the ray ( rayZ 1, offset 0 ) is never sampled, so prevHeight/prevRayZ stay sentinels
			// ( 0, 1 ) until the first advance. A feature tall enough to hit on step 0 would interpolate against
			// that sentinel and collapse to a near-constant shallow depth - a flat shelf near the ref plane,
			// worst at near-perpendicular views/lights where coarse layers make most texels hit on step 0. Seed
			// the bracket with the entry sample so a step-0 hit resolves to the real surface height ( 1 - H ).
			IOUtils.AddFunctionLine( ref m_functionBody, " \tif ( stepIndex == 0 ) prevHeight = currHeight;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \tif ( currHeight > currRayZ )" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t{" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tstepIndex = numSteps + 1;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t}" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \telse" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t{" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tstepIndex++;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevTexOffset = currTexOffset;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevRayZ = currRayZ;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevHeight = currHeight;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrTexOffset += deltaTex;" );
			if ( m_useCurvature )
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrRayZ -= layerHeight * ( 1 - result.z ) * (1+_CurvFix);" );
			else
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrRayZ -= layerHeight;" );
			IOUtils.AddFunctionLine( ref m_functionBody, " \t}" );
			IOUtils.AddFunctionLine( ref m_functionBody, "}" );

			if ( m_sidewallSteps > 0 || m_sidewallStepsPort.IsConnected )
			{
				IOUtils.AddFunctionLine( ref m_functionBody, "float sectionSteps = sidewallSteps;" );
				IOUtils.AddFunctionLine( ref m_functionBody, "float sectionIndex = 0;" );
				IOUtils.AddFunctionLine( ref m_functionBody, "float newZ = 0;" );
				IOUtils.AddFunctionLine( ref m_functionBody, "float newHeight = 0;" );
				IOUtils.AddFunctionLine( ref m_functionBody, "while ( sectionIndex < sectionSteps )" );
				IOUtils.AddFunctionLine( ref m_functionBody, "{" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tintersection = ( prevHeight - prevRayZ ) / ( prevHeight - currHeight + currRayZ - prevRayZ );" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tfinalTexOffset = prevTexOffset + intersection * deltaTex;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tnewZ = prevRayZ - intersection * layerHeight;" );

				string uvs2 = "uvs + finalTexOffset";
				if( m_texPort.DataType == WirePortDataType.SAMPLER3D )
					uvs2 = "float3(uvs.xy + finalTexOffset, uvs.z)";
				else if( m_texPort.DataType == WirePortDataType.SAMPLER2DARRAY )
					uvs2 = outsideGraph.IsSRP ? uvs2 + ", index" : "float3(" + uvs2 + ", index)";

				string samplingCall2 = GeneratorUtils.GenerateSamplingCall( ref dataCollector, m_texPort.DataType, textureProp, sampleState, uvs2, legacySampler, MipType.Derivative, "dx", "dy" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tnewHeight = " + samplingCall2 + "." + m_channelTypeVal[ m_selectedChannelInt ] + ";" );

				IOUtils.AddFunctionLine( ref m_functionBody, " \tif ( newHeight > newZ )" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t{" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrTexOffset = finalTexOffset;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrHeight = newHeight;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tcurrRayZ = newZ;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tdeltaTex = intersection * deltaTex;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tlayerHeight = intersection * layerHeight;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t}" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \telse" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t{" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevTexOffset = finalTexOffset;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevHeight = newHeight;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tprevRayZ = newZ;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tdeltaTex = ( 1 - intersection ) * deltaTex;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t \tlayerHeight = ( 1 - intersection ) * layerHeight;" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \t}" );
				IOUtils.AddFunctionLine( ref m_functionBody, " \tsectionIndex++;" );
				IOUtils.AddFunctionLine( ref m_functionBody, "}" );
			}
			else
			{
				IOUtils.AddFunctionLine( ref m_functionBody, "finalTexOffset = currTexOffset;" );
			}

			// Depth fraction (.z) of the displaced hit: linearly interpolate the crossing between the last
			// pair of samples that bracket the surface. currHeight is a filtered texture read, so this stays
			// smooth where the quantized ray height ( currRayZ ) would step/flicker - e.g. near-perpendicular
			// views/lights, where the march collapses to a single texel and resolves on coarse layer bounds.
			IOUtils.AddFunctionLine( ref m_functionBody, "float pomAfterH = currHeight - currRayZ;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float pomBeforeH = prevHeight - prevRayZ;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float pomCrossZ = currRayZ + ( pomAfterH / max( pomAfterH - pomBeforeH, 1e-5 ) ) * ( prevRayZ - currRayZ );" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float pomDepth = saturate( 1.0 - pomCrossZ );" );

			if ( m_useCurvature )
			{
				if ( !dataCollector.IsSRP )
				{
					IOUtils.AddFunctionLine( ref m_functionBody, "#ifdef UNITY_PASS_SHADOWCASTER" );
					IOUtils.AddFunctionLine( ref m_functionBody, "if ( unity_LightShadowBias.z == 0.0 )" );
					IOUtils.AddFunctionLine( ref m_functionBody, "{" );
					IOUtils.AddFunctionLine( ref m_functionBody, "#endif" );
				}
				IOUtils.AddFunctionLine( ref m_functionBody, " \tclip( 1 - result.z );" );
				if ( !dataCollector.IsSRP )
				{
					IOUtils.AddFunctionLine( ref m_functionBody, "#ifdef UNITY_PASS_SHADOWCASTER" );
					IOUtils.AddFunctionLine( ref m_functionBody, "}" );
					IOUtils.AddFunctionLine( ref m_functionBody, "#endif" );
				}
			}

			if ( m_clipEnds )
			{
				IOUtils.AddFunctionLine( ref m_functionBody, "result.xy = uvs.xy + finalTexOffset;" );
				if ( !dataCollector.IsSRP )
				{
					IOUtils.AddFunctionLine( ref m_functionBody, "#ifdef UNITY_PASS_SHADOWCASTER" );
					IOUtils.AddFunctionLine( ref m_functionBody, "if ( unity_LightShadowBias.z == 0.0 )" );
					IOUtils.AddFunctionLine( ref m_functionBody, "{" );
					IOUtils.AddFunctionLine( ref m_functionBody, "#endif" );
				}
				IOUtils.AddFunctionLine( ref m_functionBody, " \tclip( min( result, tilling - result ) );" );
				if ( !dataCollector.IsSRP )
				{
					IOUtils.AddFunctionLine( ref m_functionBody, "#ifdef UNITY_PASS_SHADOWCASTER" );
					IOUtils.AddFunctionLine( ref m_functionBody, "}" );
					IOUtils.AddFunctionLine( ref m_functionBody, "#endif" );
				}
				// .xy = parallax UV, .z = depth fraction (0 at ref plane, 1 deepest), see pomDepth above.
				IOUtils.AddFunctionLine( ref m_functionBody, "return float3( result.xy, pomDepth );" );
			}
			else
			{
				IOUtils.AddFunctionLine( ref m_functionBody, "return float3( uvs.xy + finalTexOffset, pomDepth );" );
			}
			IOUtils.CloseFunctionBody( ref m_functionBody );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_selectedChannelInt = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			//m_minSamples = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			//m_maxSamples = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() < 15406 )
			{
				m_inlineMinSamples.IntValue = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				m_inlineMaxSamples.IntValue = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			}
			else
			{
				m_inlineMinSamples.ReadFromString( ref m_currentReadParamIdx, ref nodeParams );
				m_inlineMaxSamples.ReadFromString( ref m_currentReadParamIdx, ref nodeParams );
			}
			m_sidewallSteps = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_defaultScale = Convert.ToSingle( GetCurrentParam( ref nodeParams ) );
			m_defaultRefPlane = Convert.ToSingle( GetCurrentParam( ref nodeParams ) );
			if ( UIUtils.CurrentShaderVersion() > 3001 )
			{
				m_clipEnds = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				string[] vector2Component = GetCurrentParam( ref nodeParams ).Split( IOUtils.VECTOR_SEPARATOR );
				if ( vector2Component.Length == 2 )
				{
					m_tilling.x = Convert.ToSingle( vector2Component[ 0 ] );
					m_tilling.y = Convert.ToSingle( vector2Component[ 1 ] );
				}
			}

			if ( UIUtils.CurrentShaderVersion() > 5005 )
			{
				m_useCurvature = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_CurvatureVector = IOUtils.StringToVector2( GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 13103 )
			{
				//if( UIUtils.CurrentShaderVersion() < 15307 )
				//{
				//	GetCurrentParam( ref nodeParams );
				//	//bool arrayIndexVisible = false;
				//	//arrayIndexVisible = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				//	//m_pomTexType = arrayIndexVisible ? POMTexTypes.TextureArray : POMTexTypes.Texture2D;
				//}
				//else
				//{
				//	GetCurrentParam( ref nodeParams );
				//	//m_pomTexType = (POMTexTypes)Enum.Parse( typeof(POMTexTypes), GetCurrentParam( ref nodeParams ) );
				//}
				if( UIUtils.CurrentShaderVersion() <= 18201 )
				{
					GetCurrentParam( ref nodeParams );
				}
				UpdateIndexPort();
			}

			UpdateSampler();
			//GeneratePOMfunction( string.Empty );
			UpdateCurvaturePort();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_selectedChannelInt );
			//IOUtils.AddFieldValueToString( ref nodeInfo, m_minSamples );
			//IOUtils.AddFieldValueToString( ref nodeInfo, m_maxSamples );
			m_inlineMinSamples.WriteToString( ref nodeInfo );
			m_inlineMaxSamples.WriteToString( ref nodeInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_sidewallSteps );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_defaultScale );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_defaultRefPlane );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_clipEnds );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_tilling.x.ToString() + IOUtils.VECTOR_SEPARATOR + m_tilling.y.ToString() );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_useCurvature );
			IOUtils.AddFieldValueToString( ref nodeInfo, IOUtils.Vector2ToString( m_CurvatureVector ) );
			//IOUtils.AddFieldValueToString( ref nodeInfo, m_useTextureArray );
			//IOUtils.AddFieldValueToString( ref nodeInfo, true );
		}

		public override void Destroy()
		{
			base.Destroy();
			//Not calling m_texCoordsHelper.Destroy() on purpose so UIUtils does not incorrectly unregister stuff
			if( m_texCoordsHelper != null )
			{
				DestroyImmediate( m_texCoordsHelper );
				m_texCoordsHelper = null;
			}


			m_uvPort = null;
			m_texPort = null;
			m_scalePort = null;
			m_pomUVPort = null;
		}
	}
}

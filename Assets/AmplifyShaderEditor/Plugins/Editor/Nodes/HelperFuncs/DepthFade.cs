// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Depth Fade", "Surface Data", "Outputs a linear gradient representing the distance between the surface of this object and geometry behind" )]
	public sealed class DepthFade : ParentNode
	{
		private const string ConvertToLinearStr = "Convert To Linear";
		private const string SaturateStr = "Saturate";
		private const string MirrorStr = "Mirror";

		private const string EyeDepthFuncHeader = "ASELinearEyeDepth( {0} )";

		[SerializeField]
		private bool m_convertToLinear = true;

		[SerializeField]
		private bool m_saturate = false;

		[SerializeField]
		private bool m_mirror = false;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, "Vertex Position", -1, MasterNodePortCategory.Fragment, 1 );
			AddInputPort( WirePortDataType.FLOAT, false, "Distance",-1,MasterNodePortCategory.Fragment,0 );
			GetInputPortByUniqueId(0).FloatInternalData = 1;
			AddOutputPort( WirePortDataType.FLOAT, "Out" );
			m_useInternalPortData = true;
			m_autoWrapProperties = true;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowNoVertexModeNodeMessage( this );
				return "0";
			}

			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return GetOutputColorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );

			if( !( dataCollector.IsTemplate && dataCollector.IsSRP ) )
				dataCollector.AddToIncludes( UniqueId, Constants.UnityCgLibFuncs );

			if( !dataCollector.IsTemplate || dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.HDRP )
			{
				if( dataCollector.IsTemplate && dataCollector.CurrentSRPType == TemplateSRPType.URP )
				{
					dataCollector.AddToDirectives( Constants.CameraDepthTextureURPEnabler, -1, AdditionalLineType.Define );
				}
				else
				{
					dataCollector.AddToUniforms( UniqueId, Constants.CameraDepthTextureValue );
					dataCollector.AddToUniforms( UniqueId, Constants.CameraDepthTextureTexelSize );
				}
			}

			string screenPosNorm;
			InputPort vertexPosPort = GetInputPortByUniqueId( 1 );
			if( vertexPosPort.IsConnected )
			{
				string vertexPosVar = "vertexPos" + OutputId;
				if ( dataCollector.IsTemplate || !dataCollector.TesselationActive )
				{
					GenerateInputInVertex( ref dataCollector, 1, vertexPosVar, false );
				}
				else
				{
					// @diogo: surface shader + tessellation? can't add interpolators, so generate everything in fragment
					vertexPosVar = vertexPosPort.GeneratePortInstructions( ref dataCollector );
				}
				screenPosNorm = GeneratorUtils.GenerateScreenPositionNormalizedForValue( vertexPosVar, OutputId, ref dataCollector, UniqueId, CurrentPrecisionType, !dataCollector.UsingCustomScreenPos );
			}
			else
			{
				if( dataCollector.IsTemplate )
				{
					string ppsScreenPos = string.Empty;
					if( !dataCollector.TemplateDataCollectorInstance.GetCustomInterpolatedData( TemplateInfoOnSematics.SCREEN_POSITION_NORMALIZED, WirePortDataType.FLOAT4, PrecisionType.Float, ref ppsScreenPos, true, MasterNodePortCategory.Fragment ) )
					{
						screenPosNorm = GeneratorUtils.GenerateScreenPositionNormalized( ref dataCollector, UniqueId, CurrentPrecisionType, !dataCollector.UsingCustomScreenPos );
					}
					else
					{
						screenPosNorm = ppsScreenPos;
					}
				}
				else
				{
					screenPosNorm = GeneratorUtils.GenerateScreenPositionNormalized( ref dataCollector, UniqueId, CurrentPrecisionType, !dataCollector.UsingCustomScreenPos );
				}
			}

			string screenDepth = TemplateHelperFunctions.CreateDepthFetch( dataCollector, screenPosNorm );
			if( m_convertToLinear )
			{
				screenDepth = GenerateEyeDepth( ref dataCollector, screenDepth );
			}
			else
			{
				screenDepth = string.Format( "({0}*( _ProjectionParams.z - _ProjectionParams.y ))", screenDepth );
			}

			string distance = GetInputPortByUniqueId( 0 ).GeneratePortInstructions( ref dataCollector );

			dataCollector.AddLocalVariable( UniqueId, "float screenDepth" + OutputId + " = " + screenDepth + ";" );

			string surfaceDepth = GenerateEyeDepth( ref dataCollector, screenPosNorm + ".z" );

			string finalVarName = "distanceDepth" + OutputId;
			string finalVarValue = "( screenDepth" + OutputId + " - " + surfaceDepth + " ) / ( " + distance + " )";

			if( m_mirror )
			{
				finalVarValue = string.Format( "abs( {0} )", finalVarValue );
			}

			if( m_saturate )
			{
				finalVarValue = string.Format( "saturate( {0} )", finalVarValue );
			}

			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, finalVarName, finalVarValue );
			m_outputPorts[ 0 ].SetLocalValue( finalVarName, dataCollector.PortCategory );
			return GetOutputColorItem( 0, outputId, finalVarName );
		}

		// Converts a raw depth buffer value into eye depth, on both perspective and orthographic cameras.
		private string GenerateEyeDepth( ref MasterNodeDataCollector dataCollector, string rawDepth )
		{
			string perspDepth = ( dataCollector.IsTemplate && dataCollector.IsSRP ) ? "LinearEyeDepth( rawDepth, _ZBufferParams )" : "LinearEyeDepth( rawDepth )";

			string body = string.Empty;
			IOUtils.AddFunctionHeader( ref body, "float ASELinearEyeDepth( float rawDepth )" );
			IOUtils.AddFunctionLine( ref body, "#if UNITY_REVERSED_Z" );
			IOUtils.AddFunctionLine( ref body, "float orthoDepth = _ProjectionParams.z - ( _ProjectionParams.z - _ProjectionParams.y ) * rawDepth;" );
			IOUtils.AddFunctionLine( ref body, "#else" );
			IOUtils.AddFunctionLine( ref body, "float orthoDepth = _ProjectionParams.y + ( _ProjectionParams.z - _ProjectionParams.y ) * rawDepth;" );
			IOUtils.AddFunctionLine( ref body, "#endif" );
			IOUtils.AddFunctionLine( ref body, string.Format( "return ( unity_OrthoParams.w == 0 ) ? {0} : orthoDepth;", perspDepth ) );
			IOUtils.CloseFunctionBody( ref body );

			return dataCollector.AddFunctions( EyeDepthFuncHeader, body, rawDepth );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			m_convertToLinear = EditorGUILayoutToggle( ConvertToLinearStr, m_convertToLinear );
			m_mirror = EditorGUILayoutToggle( MirrorStr, m_mirror );
			m_saturate = EditorGUILayoutToggle( SaturateStr, m_saturate );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() >= 13901 )
			{
				m_convertToLinear = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			if( UIUtils.CurrentShaderVersion() > 15607 )
			{
				m_saturate = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 15700 )
			{
				m_mirror = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_convertToLinear );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_saturate );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_mirror );
		}
	}
}

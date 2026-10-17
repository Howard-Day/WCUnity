// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[NoAutoStaticsCleanup]
	public static class GeneratorUtils
	{
		public const string VertexBlendWeightsStr = "ase_blendWeights";
		public const string VertexBlendIndicesStr = "ase_blendIndices";
		public const string ObjectPositionStr = "ase_objectPosition";
		public const string ObjectScaleStr = "ase_objectScale";
		public const string ParentObjectScaleStr = "ase_parentObjectScale";
		public const string ObjectBoundsMinStr = "ase_objectBoundsMin";
		public const string ObjectBoundsMaxStr = "ase_objectBoundsMax";
		public const string ObjectBoundsSizeStr = "ase_objectBoundsSize";
		public const string ScreenDepthStr = "ase_screenDepth";
		public const string DepthLinearEyeStr = "ase_depthLinearEye";
		public const string DepthLinear01Str = "ase_depthLinear01";
		public const string DepthEyeStr = "ase_depthEye";
		public const string DepthRawStr = "ase_depthRaw";
		public const string ViewPositionStr = "ase_positionVS";
		public const string ViewHPositionStr = "ase_hpositionVS";
		public const string WorldViewVectorStr = "ase_viewVectorWS";
		public const string WorldViewDirectionStr = "ase_viewDirWS";
		public const string WorldViewDirectionSafeStr = "ase_viewDirSafeWS";
		public const string ObjectViewVectorStr = "ase_viewVectorOS";
		public const string ObjectViewDirectionStr = "ase_viewDirOS";
		public const string ObjectViewDirectionSafeStr = "ase_viewDirSafeOS";
		public const string ViewViewVectorStr = "ase_viewVectorVS";
		public const string ViewViewDirectionStr = "ase_viewDirVS";
		public const string ViewViewDirectionSafeStr = "ase_viewDirSafeVS";
		public const string TangentViewVectorStr = "ase_viewVectorTS";
		public const string TangentViewDirectionStr = "ase_viewDirTS";
		public const string TangentViewDirectionSafeStr = "ase_viewDirSafeTS";
		public const string ClipPositionStr = "ase_positionCS";
		public const string VertexPosition3Str = "ase_positionOS";
		public const string VertexPosition4Str = "ase_positionOS4f";
		public const string VertexNormalStr = "ase_normalOS";
		public const string VertexTangentStr = "ase_tangentOS";
		public const string VertexTangentSignStr = "ase_tangentSign";
		public const string VertexBitangentStr = "ase_bitangentOS";
		public const string ScreenPosRawStr = "ase_positionSS";
		public const string ScreenPosNormStr = "ase_positionSSNorm";
		public const string ScreenPosCenterStr = "ase_positionSS_Center";
		public const string ScreenPosTiledStr = "ase_positionSS_Tiled";
		public const string ScreenPosPixelStr = "ase_positionSS_Pixel";
		public const string NormalizedScreenPosFormat = "{0} / {0}.w";
		public const string GrabScreenPositionStr = "ase_grabScreenPos";
		public const string GrabScreenPositionNormalizedStr = "ase_grabScreenPosNorm";
		public const string WorldPositionStr = "ase_positionWS";
		public const string RelativeWorldPositionStr = "ase_positionRWS";
		public const string VFaceStr = "ase_vface";
		public const string ShadowCoordsStr = "ase_shadowCoords";
		public const string LightAttenuationStr = "ase_lightAtten";
		public const string WorldLightDirStr = "ase_lightDirWS";
		public const string ObjectLightDirStr = "ase_lightDirOS";
		public const string WorldNormalStr = "ase_normalWS";
		public const string NormalizedWorldNormalStr = "ase_normalWSNorm";
		public const string WorldReflectionStr = "ase_reflectionWS";
		public const string WorldTangentStr = "ase_tangentWS";
		public const string WorldBitangentStr = "ase_bitangentWS";
		public const string WorldToTangentStr = "ase_worldToTangent";
		public const string ObjectToTangentStr = "ase_objectToTangent";
		public const string TangentToWorldPreciseStr = "ase_tangentToWorldPrecise";
		public const string TangentToWorldFastStr = "ase_tangentToWorldFast";
		public const string TangentToObjectStr = "ase_tangentToObject";
		public const string TangentToObjectFastStr = "ase_tangentToObjectFast";

		public static readonly string[] DepthModeStr = { "Linear Eye", "Linear 0-1", "Eye", "0-1" };
		public static readonly string[] DepthModeVarNameStr = { "depthLinearEye", "depthLinear01_", "depthEye", "depth01_" };

		private const string Float3Format = "float3 {0} = {1};";
		private const string Float4Format = "float4 {0} = {1};";
		private const string GrabFunctionHeader = "inline float4 ASE_ComputeGrabScreenPos( float4 pos )";
		private const string GrabFunctionCall = "ASE_ComputeGrabScreenPos( {0} )";
		private const string Identity4x4 = "ase_identity4x4";
		private const string FaceVertex = "ase_faceVertex";
		private const string ase_MatrixInvP = "ase_matrixInvP";
		private const string ase_MatrixInvVP = "ase_matrixInvVP";
		private const string ase_MatrixInvMVP = "ase_matrixInvMVP";

		private const string SurfaceScreenPosFormat = "{0}( {1}.screenPos.xyz , {1}.screenPos.w + 1e-7 )";

		private static readonly string[] GrabFunctionBody = {
			"#if UNITY_UV_STARTS_AT_TOP",
			"float scale = -1.0;",
			"#else",
			"float scale = 1.0;",
			"#endif",
			"float4 o = pos;",
			"o.y = pos.w * 0.5f;",
			"o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;",
			"return o;"
		};

		private static readonly string InverseProjectionMatrixFunctionHeader = "InverseProjectionMatrix()";
		private static readonly string[] InverseProjectionMatrixFunctionBody =
		{
			"float4x4 InverseProjectionMatrix()\n",
			"{\n",
			"\tfloat n11 = UNITY_MATRIX_P[ 0 ][ 0 ];\n",
			"\tfloat n22 = UNITY_MATRIX_P[ 1 ][ 1 ];\n",
			"\tfloat n33 = UNITY_MATRIX_P[ 2 ][ 2 ];\n",
			"\tfloat n43 = UNITY_MATRIX_P[ 3 ][ 2 ];\n",
			"\tfloat n34 = UNITY_MATRIX_P[ 2 ][ 3 ];\n",
			"\tfloat rcpN34 = rcp( n34 );\n",
			"\treturn float4x4( rcp( n11 ), 0, 0, 0, 0, rcp( n22 ), 0, 0, 0, 0, 0, n43, 0, 0, rcpN34, -n33 * rcpN34 * n43 );\n",
			"}\n"
		};

		private static readonly string ScreenPosNormToPixelFunctionHeader = "ASEScreenPositionNormalizedToPixel( {0}, {1} )";
		private static readonly string[] ScreenPosNormToPixelFunctionBody = {
			"float4 ASEScreenPositionNormalizedToPixel( float4 screenPosNorm, float4 screenParams )\n",
			"{\n",
			"\tfloat4 screenPosPixel = screenPosNorm * float4( screenParams.xy, 1, 1 );\n",
			"\t#if UNITY_UV_STARTS_AT_TOP\n",
			"\t\tscreenPosPixel.xy = float2( screenPosPixel.x, ( _ProjectionParams.x < 0 ) ? screenParams.y - screenPosPixel.y : screenPosPixel.y );\n",
			"\t#else\n",
			"\t\tscreenPosPixel.xy = float2( screenPosPixel.x, ( _ProjectionParams.x > 0 ) ? screenParams.y - screenPosPixel.y : screenPosPixel.y );\n",
			"\t#endif\n",
			"\treturn screenPosPixel;\n",
			"}\n"
		};

		// MATRIX IDENTITY
		static public string GenerateIdentity4x4( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			dataCollector.AddLocalVariable( uniqueId, "float4x4 ase_identity4x4 = float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1);" );
			return Identity4x4;
		}

		// OBJECT POSITION
		static public string GenerateObjectPosition( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GenerateObjectPosition( ref dataCollector, uniqueId );

			string value = "UNITY_MATRIX_M._m03_m13_m23";
			dataCollector.AddLocalVariable( uniqueId, PrecisionType.Float, WirePortDataType.FLOAT3, ObjectPositionStr, value );
			return ObjectPositionStr;
		}

		// OBJECT SCALE
		static public string GenerateObjectScale( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GenerateObjectScale( ref dataCollector, uniqueId );

			//string value= "1/float3( length( unity_WorldToObject[ 0 ].xyz ), length( unity_WorldToObject[ 1 ].xyz ), length( unity_WorldToObject[ 2 ].xyz ) );";
			string value = "float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) )";
			dataCollector.AddLocalVariable( uniqueId, PrecisionType.Float, WirePortDataType.FLOAT3, ObjectScaleStr, value );
			return ObjectScaleStr;
		}

		static public string GenerateRotationIndependentObjectScale( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GenerateRotationIndependentObjectScale( ref dataCollector, uniqueId );

			string value = "(1.0/float3( length( unity_WorldToObject[ 0 ].xyz ), length( unity_WorldToObject[ 1 ].xyz ), length( unity_WorldToObject[ 2 ].xyz ) ))";
			dataCollector.AddLocalVariable( uniqueId, PrecisionType.Float, WirePortDataType.FLOAT3, ParentObjectScaleStr, value );
			return ParentObjectScaleStr;
		}

		// POSITION
		static public string GeneratePosition( ref MasterNodeDataCollector dataCollector, int uniqueId, PositionNode.Space space )
		{
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetPosition( space );
			}

			string result;
			if ( space == PositionNode.Space.Object )
			{
				result = GenerateVertexPosition( ref dataCollector, uniqueId, WirePortDataType.FLOAT3 );
			}
			else if ( space == PositionNode.Space.RelativeWorld )
			{
				result = GenerateRelativeWorldPosition( ref dataCollector, uniqueId );
			}
			else if ( space == PositionNode.Space.View )
			{
				result = GenerateViewPosition( ref dataCollector, uniqueId );
			}
			else // PositionNode.Space.World
			{
				result = GenerateWorldPosition( ref dataCollector, uniqueId );
			}
			return result;
		}

		static public string GenerateWorldPosition( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			PrecisionType precision = PrecisionType.Float;
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetWorldPos();
			}

			if ( !dataCollector.HasLocalVariableByName( WorldPositionStr ) )
			{
				dataCollector.AddToInput( -1, SurfaceInputs.WORLD_POS, precision );

				string result;
				if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				{
					result = "mul( unity_ObjectToWorld, " + Constants.VertexShaderInputStr + ".vertex )";
				}
				else
				{
					result = Constants.InputVarStr + ".worldPos";
				}

				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, WorldPositionStr, result );
			}
			return WorldPositionStr;
		}

		static public string GenerateRelativeWorldPosition( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			PrecisionType precision = PrecisionType.Float;
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetRelativeWorldPos();
			}

			if ( !dataCollector.HasLocalVariableByName( RelativeWorldPositionStr ) )
			{
				if ( dataCollector.TesselationActive )
				{
					string positionWS = GenerateWorldPosition( ref dataCollector, uniqueId );

					string invProjMatrix = GeneratorUtils.GenerateInverseProjection( ref dataCollector, uniqueId, precision );

					if ( !dataCollector.HasLocalVariableByName( ClipPositionStr ) )
					{
						dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, ClipPositionStr,
							string.Format( "UnityWorldToClipPos( {0} )", positionWS ) );
					}

					if ( !dataCollector.HasLocalVariableByName( ViewHPositionStr ) )
					{
						dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, ViewHPositionStr,
							string.Format( "mul( {0}, {1} )", invProjMatrix, ClipPositionStr ) );
					}

					dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, RelativeWorldPositionStr,
						string.Format( "mul( ( float3x3 )UNITY_MATRIX_I_V, {0}.xyz / {0}.w )", ViewHPositionStr ) );
				}
				else
				{
					string positionOS = GenerateVertexPosition( ref dataCollector, uniqueId, WirePortDataType.FLOAT4, false, MasterNodePortCategory.Vertex );

					string positionRWSOutput = string.Format( "{0}.{1}", Constants.VertexShaderOutputStr, RelativeWorldPositionStr );
					string positionRWSInput = string.Format( "{0}.{1}", Constants.InputVarStr, RelativeWorldPositionStr );

					string invProjMatrix = GeneratorUtils.GenerateInverseProjection( ref dataCollector, uniqueId, precision, false, MasterNodePortCategory.Vertex );

					dataCollector.AddToInput( uniqueId, RelativeWorldPositionStr, WirePortDataType.FLOAT3 );

					if ( !dataCollector.HasLocalVariableByName( ClipPositionStr ) )
					{
						dataCollector.AddToVertexLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4, ClipPositionStr,
							string.Format( "UnityObjectToClipPos( {0} )", positionOS ) );
					}

					if ( !dataCollector.HasLocalVariableByName( ViewHPositionStr ) )
					{
						dataCollector.AddToVertexLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4, ViewHPositionStr,
							string.Format( "mul( {0}, {1} )", invProjMatrix, ClipPositionStr ) );
					}

					dataCollector.AddToVertexLocalVariables( uniqueId, precision, WirePortDataType.FLOAT3, RelativeWorldPositionStr,
						string.Format( "mul( ( float3x3 )UNITY_MATRIX_I_V, {0}.xyz / {0}.w )", ViewHPositionStr ) );

					dataCollector.AddToVertexLocalVariables( uniqueId, positionRWSOutput, RelativeWorldPositionStr );

					if ( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
					{
						dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, RelativeWorldPositionStr, positionRWSInput );
					}
				}
			}
			return RelativeWorldPositionStr;
		}

		static public string GenerateViewPosition( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			PrecisionType precision = PrecisionType.Float;
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetViewPos();
			}

			if ( !dataCollector.HasLocalVariableByName( ViewPositionStr ) )
			{
				string result;
				if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				{
					string positionOS = Constants.VertexShaderInputStr + ".vertex";
					result = string.Format( "UnityObjectToViewPos( {0} )", positionOS );
				}
				else
				{
					dataCollector.AddToInput( -1, SurfaceInputs.WORLD_POS, precision );

					string positionWS = Constants.InputVarStr + ".worldPos";
					result = string.Format( "UnityWorldToViewPos( {0} )", positionWS );
				}

				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, ViewPositionStr, result );
			}
			return ViewPositionStr;
		}

		// WORLD REFLECTION
		static public string GenerateWorldReflection( ref MasterNodeDataCollector dataCollector, int uniqueId, bool normalize = false )
		{
			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldReflection( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision, true, MasterNodePortCategory.Fragment, normalize );

			string precisionType = UIUtils.PrecisionWirePortToCgType( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision, WirePortDataType.FLOAT3 );
			string result = string.Empty;
			if ( !dataCollector.DirtyNormal )
				result = Constants.InputVarStr + ".worldRefl";
			else
				result = "WorldReflectionVector( " + Constants.InputVarStr + ", " + precisionType + "( 0, 0, 1 ) )";

			if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				result = "UnityObjectToWorldNormal( " + Constants.VertexShaderInputStr + ".normal )";
			if ( normalize )
			{
				result = string.Format( "normalize( {0} )", result );
			}

			dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, string.Concat( precisionType, " ", WorldReflectionStr, " = ", result, ";" ) );
			return WorldReflectionStr;
		}

		// WORLD NORMAL
		static public string GenerateWorldNormal( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precisionType, string normal, string outputId,
			ViewSpace normalSpace = ViewSpace.Tangent )
		{
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetWorldNormal( uniqueId, precisionType, normal, outputId, normalSpace );
			}

			if ( normalSpace == ViewSpace.Tangent )
			{
				string tanToWorld = GenerateTangentToWorldMatrixFast( ref dataCollector, uniqueId, precisionType );
				return string.Format( "mul( {0}, {1} )", tanToWorld, normal );
			}
			else if ( normalSpace == ViewSpace.World )
			{
				return normal;
			}
			else if ( normalSpace == ViewSpace.Object )
			{
				return string.Format( "UnityObjectToWorldNormal( {0} )", normal );
			}
			else if ( normalSpace == ViewSpace.View )
			{
				return string.Format( "mul( ( float3x3 )UNITY_MATRIX_I_V, {0} )", normal );
			}
			else
			{
				// unsupported
				return "float3( 0, 0, 0 )";
			}
		}
		static public string GenerateWorldNormal( ref MasterNodeDataCollector dataCollector, int uniqueId, bool normalize = false )
		{
			PrecisionType precision = UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision;

			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldNormal( precision, true, MasterNodePortCategory.Fragment, normalize );

			string precisionType = UIUtils.PrecisionWirePortToCgType( precision, WirePortDataType.FLOAT3 );
			string result = string.Empty;
			if ( !dataCollector.DirtyNormal )
				result = Constants.InputVarStr + ".worldNormal";
			else
				result = "WorldNormalVector( " + Constants.InputVarStr + ", " + precisionType + "( 0, 0, 1 ) )";

			if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				result = "UnityObjectToWorldNormal( " + Constants.VertexShaderInputStr + ".normal )";

			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, WorldNormalStr, result );
			if ( normalize )
			{
				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, NormalizedWorldNormalStr, "normalize( " + WorldNormalStr + " )" );
				return NormalizedWorldNormalStr;
			}
			return WorldNormalStr;
		}

		// WORLD TANGENT
		static public string GenerateWorldTangent( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldTangent( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );

			string precisionType = UIUtils.PrecisionWirePortToCgType( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision, WirePortDataType.FLOAT3 );
			string result = "WorldNormalVector( " + Constants.InputVarStr + ", " + precisionType + "( 1, 0, 0 ) )";

			if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				result = "UnityObjectToWorldDir( " + Constants.VertexShaderInputStr + ".tangent.xyz )";
			dataCollector.AddLocalVariable( uniqueId, UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision, WirePortDataType.FLOAT3, WorldTangentStr, result );
			//dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, string.Concat( precisionType, " ", WorldTangentStr, " = ", result, ";" ) );
			return WorldTangentStr;
		}

		// WORLD BITANGENT
		static public string GenerateWorldBitangent( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldBitangent( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );

			string precisionType = UIUtils.PrecisionWirePortToCgType( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision, WirePortDataType.FLOAT3 );
			string result = "WorldNormalVector( " + Constants.InputVarStr + ", " + precisionType + "( 0, 1, 0 ) )";

			if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				string worldNormal = GenerateWorldNormal( ref dataCollector, uniqueId );
				string worldTangent = GenerateWorldTangent( ref dataCollector, uniqueId );
				dataCollector.AddToVertexLocalVariables( uniqueId, string.Format( "half tangentSign = {0}.tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );", Constants.VertexShaderInputStr ) );
				result = "cross( " + worldNormal + ", " + worldTangent + " ) * tangentSign";
			}

			dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, string.Concat( precisionType, " ", WorldBitangentStr, " = ", result, ";" ) );
			return WorldBitangentStr;
		}

		// OBJECT TO TANGENT MATRIX
		static public string GenerateObjectToTangentMatrix( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			string normal = GenerateVertexNormal( ref dataCollector, uniqueId, precision );
			string tangent = GenerateVertexTangent( ref dataCollector, uniqueId, precision, WirePortDataType.FLOAT3 );
			string bitangen = GenerateVertexBitangent( ref dataCollector, uniqueId, precision );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3x3, ObjectToTangentStr, "float3x3( " + tangent + ", " + bitangen + ", " + normal + " )" );
			return ObjectToTangentStr;
		}

		// TANGENT TO OBJECT
		//static public string GenerateTangentToObjectMatrixFast( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		//{
		//	string normal = GenerateVertexNormal( ref dataCollector, uniqueId, precision );
		//	string tangent = GenerateVertexTangent( ref dataCollector, uniqueId, precision );
		//	string bitangent = GenerateVertexBitangent( ref dataCollector, uniqueId, precision );

		//	string result = string.Format( "float3x3({0}.x,{1}.x,{2}.x,{0}.y,{1}.y,{2}.y,{0}.z,{1}.z,{2}.z)",tangent,bitangent,normal );
		//	dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3x3, TangentToObjectFastStr, result );
		//	return TangentToObjectFastStr;
		//}

		//static public string GenerateTangentToObjectMatrixPrecise( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		//{
		//	string objectToTangent = GenerateObjectToTangentMatrix( ref dataCollector, uniqueId, precision );
		//	Add3x3InverseFunction( ref dataCollector, UIUtils.PrecisionWirePortToCgType( precision, WirePortDataType.FLOAT ) );
		//	dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3x3, TangentToObjectStr, string.Format( Inverse3x3Header, objectToTangent ) );
		//	return TangentToObjectStr;
		//}

		// WORLD TO TANGENT MATRIX
		static public string GenerateWorldToTangentMatrix( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldToTangentMatrix( precision );

			if ( dataCollector.IsFragmentCategory )
			{
				dataCollector.ForceNormal = true;

				dataCollector.AddToInput( -1, SurfaceInputs.WORLD_NORMAL, precision );
				dataCollector.AddToInput( -1, SurfaceInputs.INTERNALDATA, addSemiColon: false );
			}

			string worldNormal = GenerateWorldNormal( ref dataCollector, uniqueId );
			string worldTangent = GenerateWorldTangent( ref dataCollector, uniqueId );
			string worldBitangent = GenerateWorldBitangent( ref dataCollector, uniqueId );

			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3x3, WorldToTangentStr, "float3x3( " + worldTangent + ", " + worldBitangent + ", " + worldNormal + " )" );
			return WorldToTangentStr;
		}

		// TANGENT TO WORLD
		static public string GenerateTangentToWorldMatrixFast( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetTangentToWorldMatrixFast( precision );

			if ( dataCollector.IsFragmentCategory )
			{
				dataCollector.ForceNormal = true;

				dataCollector.AddToInput( -1, SurfaceInputs.WORLD_NORMAL, precision );
				dataCollector.AddToInput( -1, SurfaceInputs.INTERNALDATA, addSemiColon: false );
			}

			string worldNormal = GenerateWorldNormal( ref dataCollector, uniqueId );
			string worldTangent = GenerateWorldTangent( ref dataCollector, uniqueId );
			string worldBitangent = GenerateWorldBitangent( ref dataCollector, uniqueId );

			string result = string.Format( "float3x3( {0}.x, {1}.x, {2}.x, {0}.y, {1}.y, {2}.y, {0}.z, {1}.z, {2}.z )", worldTangent, worldBitangent, worldNormal );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3x3, TangentToWorldFastStr, result );
			return TangentToWorldFastStr;
		}

		static public string GenerateTangentToWorldMatrixPrecise( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetTangentToWorldMatrixPrecise( precision );

			if ( dataCollector.IsFragmentCategory )
			{
				dataCollector.ForceNormal = true;

				dataCollector.AddToInput( -1, SurfaceInputs.WORLD_NORMAL, precision );
				dataCollector.AddToInput( -1, SurfaceInputs.INTERNALDATA, addSemiColon: false );
			}

			string worldToTangent = GenerateWorldToTangentMatrix( ref dataCollector, uniqueId, precision );
			Add3x3InverseFunction( ref dataCollector, UIUtils.PrecisionWirePortToCgType( precision, WirePortDataType.FLOAT ) );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3x3, TangentToWorldPreciseStr, string.Format( Inverse3x3Header, worldToTangent ) );
			return TangentToWorldPreciseStr;
		}

		// MATRICES
		static public string GenerateInverseProjection( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string value;
			if ( dataCollector.IsSRP )
			{
				value = "UNITY_MATRIX_I_P";
			}
			else
			{
				dataCollector.AddFunction( InverseProjectionMatrixFunctionBody[ 0 ], InverseProjectionMatrixFunctionBody, false );
				value = InverseProjectionMatrixFunctionHeader;
			}

			if ( !useMasterNodeCategory && customCategory == MasterNodePortCategory.Vertex )
			{
				dataCollector.AddToVertexLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4x4, ase_MatrixInvP, value );
			}
			else
			{
				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4x4, ase_MatrixInvP, value );
			}
			return ase_MatrixInvP;
		}

		static public string GenerateInverseViewProjection( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string value;
			if ( dataCollector.IsSRP )
			{
				value = "mul( GetViewToWorldMatrix(), UNITY_MATRIX_I_P )";
			}
			else
			{
				value = string.Format( "mul( UNITY_MATRIX_I_V, {0} )", GenerateInverseProjection( ref dataCollector, uniqueId, precision ) );
			}
			if ( !useMasterNodeCategory && customCategory == MasterNodePortCategory.Vertex )
			{
				dataCollector.AddToVertexLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4x4, ase_MatrixInvVP, value );
			}
			else
			{
				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4x4, ase_MatrixInvVP, value );
			}
			return ase_MatrixInvVP;
		}

		static public string GenerateInverseModelViewProjection( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string value;
			if ( dataCollector.IsSRP )
			{
				value = "mul( GetWorldToObjectMatrix(), mul( GetViewToWorldMatrix(), UNITY_MATRIX_I_P ) )";
			}
			else
			{
				value = string.Format( "mul( unity_WorldToObject, {0} )", GenerateInverseViewProjection( ref dataCollector, uniqueId, precision ) );
			}
			if ( !useMasterNodeCategory && customCategory == MasterNodePortCategory.Vertex )
			{
				dataCollector.AddToVertexLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4x4, ase_MatrixInvMVP, value );
			}
			else
			{
				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4x4, ase_MatrixInvMVP, value );
			}
			return ase_MatrixInvMVP;
		}

		// SAMPLER STATES
		static public string GenerateSamplerState( ref MasterNodeDataCollector dataCollector, int uniqueId, string propertyName, VariableMode varMode, bool returnPropertyName = false )
		{

			string sampler = string.Format( Constants.SamplerFormat, propertyName );
			string samplerDecl = string.Empty;

			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			if ( outsideGraph.IsSRP )
				//if( dataCollector.IsSRP )
				samplerDecl = string.Format( Constants.SamplerDeclSRPFormat, sampler ) + ";";
			else
				samplerDecl = string.Format( Constants.SamplerDeclFormat, sampler ) + ";";
			// @diogo: the pipeline declares this texture and its sampler, so ours would redefine them
			bool pipelineDeclared = outsideGraph.IsSRP && propertyName == Constants.CameraDepthTextureName;
			if ( varMode == VariableMode.Create && !pipelineDeclared )
			{
				dataCollector.AddToUniforms( uniqueId, samplerDecl );
			}

			if ( returnPropertyName )
				return propertyName;
			else
				return sampler;
		}

		public static string GetPropertyFromSamplerState( string sampler )
		{
			if ( sampler.StartsWith( "sampler" ) )
				return sampler.Remove( 0, 7 );
			else
				return sampler;
		}

		// @diogo: a texture the template declares as a legacy sampler has no texture object behind it,
		// so the macro path's tex.Sample() form cannot compile against it (#241)
		public static bool IsLegacyTemplateSampler( InputPort texPort )
		{
			return texPort.IsConnected &&
				texPort.GetOutputNodeWhichIsNotRelay() is TemplateShaderPropertyNode templateProperty &&
				templateProperty.IsLegacySampler;
		}

		public static string GetSamplerDeclaraction( string texture, WirePortDataType type, string termination = "", bool legacySampler = false )
		{
			return GetSamplerDeclaraction( texture, Constants.WireToTexture[ type ], termination, legacySampler );
		}

		public static string GetSamplerDeclaraction( string sampler, TextureType type, string termination = "", bool legacySampler = false )
		{
			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			if ( !legacySampler && ( outsideGraph.SamplingMacros || type == TextureType.Texture2DArray ) )
			{
				if ( outsideGraph.IsSRP )
					return string.Format( Constants.SamplerDeclSRPFormat, sampler ) + termination;
				else
					return string.Format( Constants.SamplerDeclFormat, sampler ) + termination;
			}

			// we don't use sampler states when macros are not available
			return string.Empty;
		}

		// PROPERTY MACRO
		public static string GetPropertyDeclaraction( string texture, WirePortDataType type, string termination = "", bool legacySampler = false )
		{
			return GetPropertyDeclaraction( texture, Constants.WireToTexture[ type ], termination, legacySampler );
		}

		public static string GetPropertyDeclaraction( string texture, TextureType type, string termination = "", bool legacySampler = false )
		{
			if ( type == TextureType.Texture1D )
				return "sampler1D " + texture + termination;

			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			if ( !legacySampler && ( outsideGraph.SamplingMacros || type == TextureType.Texture2DArray ) )
			{
				if ( outsideGraph.IsSRP )
					return string.Format( Constants.TexDeclarationNoSamplerSRPMacros[ type ], texture ) + termination;
				else
					return string.Format( Constants.TexDeclarationNoSamplerStandardMacros[ type ], texture ) + termination;
			}

			return UIUtils.TextureTypeToCgType( type ) + " " + texture + termination;
		}

		// SAMPLING CALL
		// @diogo: kept for the deprecated Dithering node, which must stay on the pre-#241 call shape
		public static string GenerateSamplingCall( ref MasterNodeDataCollector dataCollector, WirePortDataType type, string property, string samplerState, string uv, MipType mip = MipType.Auto, params string[] mipData )
		{
			return GenerateSamplingCall( ref dataCollector, type, property, samplerState, uv, false, mip, mipData );
		}

		public static string GenerateSamplingCall( ref MasterNodeDataCollector dataCollector, WirePortDataType type, string property, string samplerState, string uv, bool legacySampler, MipType mip = MipType.Auto, params string[] mipData )
		{
			ParentGraph ousideGraph = UIUtils.CurrentWindow.OutsideGraph;
			string result = string.Empty;
			string mipSuffix = string.Empty;

			//samplerState = GetPropertyFromSamplerState( samplerState );
			TextureType textureType = Constants.WireToTexture[ type ];

			bool usingMacro = false;
			if ( !legacySampler && ( ousideGraph.SamplingMacros || textureType == TextureType.Texture2DArray ) )
				usingMacro = true;

			switch ( mip )
			{
				default:
				case MipType.Auto:
					break;
				case MipType.MipLevel:
					mipSuffix = usingMacro ? "_LOD" : "lod";
					break;
				case MipType.MipBias:
					mipSuffix = usingMacro ? "_BIAS" : "bias";
					break;
				case MipType.Derivative:
					mipSuffix = usingMacro ? "_GRAD" : "grad";
					break;
			}

			string mipParams = string.Empty;
			if ( mip != MipType.Auto )
			{
				for ( int i = 0; i < mipData.Length; i++ )
				{
					mipParams += ", " + mipData[ i ];
				}
			}

			if ( usingMacro )
			{
				if ( ousideGraph.IsSRP )
				{
					if ( textureType == TextureType.Texture3D && ( mip == MipType.MipBias || mip == MipType.Derivative ) )
						AddCustom3DSRPMacros( ref dataCollector );
					// srp macro
					result = string.Format( Constants.TexSampleSRPMacros[ textureType ], mipSuffix, property, samplerState, uv + mipParams );
				}
				else
				{
					AddCustomStandardSamplingMacros( ref dataCollector, type, mip );
					result = string.Format( Constants.TexSampleSamplerStandardMacros[ textureType ], mipSuffix, property, samplerState, uv + mipParams );

				}
			}
			else
			{
				//no macro : builtin and srp
				string uvs = uv + mipParams;
				string emptyParam = ", 0";
				if ( textureType == TextureType.Texture3D || textureType == TextureType.Cube )
					emptyParam = string.Empty;

				if ( ( mip == MipType.MipBias || mip == MipType.MipLevel ) )
					uvs = "float4( " + uv + emptyParam + mipParams + " )";

				result = string.Format( Constants.TexSampleStandard[ textureType ], mipSuffix, property, uvs );
			}

			// @diogo: no node id in scope here, the collector only needs to know a hazard was emitted
			if ( mip == MipType.Auto || mip == MipType.MipBias )
			{
				dataCollector.NotifyDivergenceHazard( -1 );
			}
			return result;
		}

		public static string GenerateScaleOffsettedUV( TextureType texType, string uvName, string propertyName, bool addST )
		{
			if ( addST )
				propertyName += "_ST";

			switch ( texType )
			{
				case TextureType.Texture1D: return uvName + " * " + propertyName + ".x + " + propertyName + ".z";
				case TextureType.Texture2D: return uvName + " * " + propertyName + ".xy + " + propertyName + ".zw";
				case TextureType.Texture3D:
				case TextureType.Cube: return uvName + " * " + propertyName + ".xy + " + propertyName + ".zw";
				default:
				case TextureType.Texture2DArray:
				case TextureType.ProceduralTexture: return uvName;
			}
		}
		// AUTOMATIC UVS - SURFACE ONLY
		static public string GenerateAutoUVs( ref MasterNodeDataCollector dataCollector, int uniqueId, int index, string propertyName = null, WirePortDataType size = WirePortDataType.FLOAT2, string scale = null, string offset = null, string outputId = null, VariableMode varMode = VariableMode.Create )
		{
			string result = string.Empty;
			string varName = string.Empty;
			string indexStr = index > 0 ? ( index + 1 ).ToString() : "";
			string sizeDif = string.Empty;
			WirePortDataType maxSize = dataCollector.GetMaxTextureChannelSize( index );

			//if( maxSize == WirePortDataType.FLOAT3 )
			//	sizeDif = "3";
			//else if( maxSize == WirePortDataType.FLOAT4 )
			//	sizeDif = "4";

			if ( !dataCollector.IsTemplate && ( size == WirePortDataType.FLOAT3 || size == WirePortDataType.FLOAT4 ) )
			{
				dataCollector.UsingHigherSizeTexcoords = true;
			}

			if ( !dataCollector.IsTemplate && index > 3 )
			{
				string texCoordNameIn = TemplateHelperFunctions.BaseInterpolatorName + index;
				string texCoordNameOut = TemplateHelperFunctions.BaseInterpolatorName + ( index + 1 ).ToString();
				if ( dataCollector.IsFragmentCategory )
				{
					GenerateValueInVertex( ref dataCollector, uniqueId, maxSize, PrecisionType.Float, Constants.VertexShaderInputStr + "." + texCoordNameIn, texCoordNameOut, true );
					result = Constants.InputVarStr + "." + texCoordNameOut;
				}
				else
				{
					result = Constants.VertexShaderInputStr + "." + texCoordNameIn;
				}

				if ( !string.IsNullOrEmpty( propertyName ) )
				{

					varName = "uv" + indexStr + ( maxSize != WirePortDataType.FLOAT2 ? "s" + sizeDif : "" ) + propertyName;
					if ( varMode == VariableMode.Create )
					{
						dataCollector.AddToUniforms( uniqueId, "uniform float4 " + propertyName + "_ST;" );
					}
					if ( maxSize > WirePortDataType.FLOAT2 )
					{
						dataCollector.UsingHigherSizeTexcoords = true;
						dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, PrecisionType.Float, maxSize, varName, result );
						dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, varName + ".xy = " + result + ".xy * " + propertyName + "_ST.xy + " + propertyName + "_ST.zw;" );
					}
					else
					{
						dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, PrecisionType.Float, maxSize, varName, result + " * " + propertyName + "_ST.xy + " + propertyName + "_ST.zw" );
					}

					result = varName;
				}

				switch ( maxSize )
				{
					default:
					case WirePortDataType.FLOAT2:
					{
						result += ".xy";
					}
					break;
					case WirePortDataType.FLOAT3:
					{
						result += ".xyz";
					}
					break;
					case WirePortDataType.FLOAT4: break;
				}

				if ( size < maxSize )
				{
					switch ( size )
					{
						case WirePortDataType.FLOAT2: result += ".xy"; break;
						case WirePortDataType.FLOAT3: result += ".xyz"; break;
					}
				}

				return result;
			}

			if ( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
			{
				string dummyPropUV = "_tex" + sizeDif + "coord" + indexStr;
				string dummyUV = "uv" + indexStr + dummyPropUV;

				dataCollector.AddToProperties( uniqueId, "[HideInInspector] " + dummyPropUV + "( \"\", 2D ) = \"white\" {}", 100 );
				dataCollector.AddToInput( uniqueId, dummyUV, maxSize );

				result = Constants.InputVarStr + "." + dummyUV;
			}
			else
			{
				result = Constants.VertexShaderInputStr + ".texcoord";
				if ( index > 0 )
				{
					result += index.ToString();
				}

				switch ( maxSize )
				{
					default:
					case WirePortDataType.FLOAT2:
					{
						result += ".xy";
					}
					break;
					case WirePortDataType.FLOAT3:
					{
						result += ".xyz";
					}
					break;
					case WirePortDataType.FLOAT4: break;
				}
			}

			varName = "uv" + indexStr + ( maxSize != WirePortDataType.FLOAT2 ? "s" + sizeDif : "" ) + "_TexCoord" + outputId;

			if ( !string.IsNullOrEmpty( propertyName ) )
			{
				string finalVarName = "uv" + indexStr + ( maxSize != WirePortDataType.FLOAT2 ? "s" + sizeDif : "" ) + propertyName;

				if ( varMode == VariableMode.Create )
				{
					dataCollector.AddToUniforms( uniqueId, "uniform float4 " + propertyName + "_ST;" );
				}
				if ( maxSize > WirePortDataType.FLOAT2 )
				{
					dataCollector.UsingHigherSizeTexcoords = true;
					dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, PrecisionType.Float, maxSize, finalVarName, result );
					dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, finalVarName + ".xy = " + result + ".xy * " + propertyName + "_ST.xy + " + propertyName + "_ST.zw;" );
				}
				else
				{
					dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, PrecisionType.Float, size, finalVarName, result + " * " + propertyName + "_ST.xy + " + propertyName + "_ST.zw" );
				}

				result = finalVarName;
			}
			else if ( !string.IsNullOrEmpty( scale ) || !string.IsNullOrEmpty( offset ) )
			{
				if ( maxSize > WirePortDataType.FLOAT2 )
				{
					dataCollector.UsingHigherSizeTexcoords = true;
					dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, PrecisionType.Float, size, varName, result );
					dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, varName + ".xy = " + result + ".xy" + ( string.IsNullOrEmpty( scale ) ? "" : " * " + scale ) + ( string.IsNullOrEmpty( offset ) ? "" : " + " + offset ) + ";" );
				}
				else
				{
					dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, PrecisionType.Float, size, varName, result + ( string.IsNullOrEmpty( scale ) ? "" : " * " + scale ) + ( string.IsNullOrEmpty( offset ) ? "" : " + " + offset ) );
				}

				result = varName;
			}
			else if ( dataCollector.PortCategory == MasterNodePortCategory.Fragment )
			{
				if ( maxSize > WirePortDataType.FLOAT2 )
					dataCollector.UsingHigherSizeTexcoords = true;
			}
			if ( size < maxSize )
			{
				switch ( size )
				{
					case WirePortDataType.FLOAT2: result += ".xy"; break;
					case WirePortDataType.FLOAT3: result += ".xyz"; break;
				}
			}
			return result;
		}

		// SCREEN POSITION
		static public string GenerateScreenPositionNormalizedForValue( string customVertexPos, string outputId, ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, bool addInput = true )
		{
			if ( !dataCollector.HasLocalVariableByName( ScreenPosNormStr ) )
			{
				string screenPos = GenerateScreenPositionRawForValue( customVertexPos, outputId, ref dataCollector, uniqueId, precision, addInput );

				// TODO: check later if precision can be half
				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, ScreenPosNormStr, string.Format( NormalizedScreenPosFormat, screenPos ) );
				dataCollector.AddLocalVariable( uniqueId, string.Format( "{0}.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? {0}.z : {0}.z * 0.5 + 0.5;", ScreenPosNormStr ) );
			}
			return ScreenPosNormStr;
		}

		static public string GenerateScreenPositionNormalized( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, bool addInput = true, string customScreenPos = null )
		{
			string screenPos;
			string varName;
			if ( string.IsNullOrEmpty( customScreenPos ) )
			{
				screenPos = GenerateScreenPositionRaw( ref dataCollector, uniqueId, precision, addInput );
				varName = ScreenPosNormStr;
			}
			else
			{
				screenPos = customScreenPos;
				varName = ScreenPosNormStr + uniqueId;
			}

			if ( !dataCollector.HasLocalVariableByName( varName ) )
			{
				// TODO: check later if precision can be half
				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, varName, string.Format( NormalizedScreenPosFormat, screenPos ) );
				dataCollector.AddLocalVariable( uniqueId, string.Format( "{0}.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? {0}.z : {0}.z * 0.5 + 0.5;", varName ) );
			}
			return varName;
		}

		static public string GenerateScreenPositionRawForValue( string customVertexPosition, string outputId, ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, bool addInput = true )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosForValue( precision, customVertexPosition, outputId );
			}

			string screenPosFrag = ScreenPosRawStr + outputId;

			if ( dataCollector.IsTemplate || !dataCollector.TesselationActive )
			{
				string value = GenerateVertexScreenPositionRawForValue( customVertexPosition, outputId, ref dataCollector, uniqueId, precision );

				if ( !dataCollector.IsFragmentCategory )
				{
					return value;
				}

				string screenPosVertex = "screenPosition" + outputId;
				dataCollector.AddToInput( uniqueId, screenPosVertex, WirePortDataType.FLOAT4, precision );
				dataCollector.AddToVertexLocalVariables( uniqueId, Constants.VertexShaderOutputStr + "." + screenPosVertex + " = " + value + ";" );


				string globalResult = Constants.InputVarStr + "." + screenPosVertex;
				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, screenPosFrag, globalResult );
			}
			else
			{
				string value = string.Format( "ComputeScreenPos( UnityObjectToClipPos( {0} ) )", customVertexPosition );
				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, screenPosFrag, value );
			}
			return screenPosFrag;
		}

		static public string GenerateScreenPositionRaw( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, bool addInput = true )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosRaw( precision );
			}

			if ( dataCollector.UsingCustomScreenPos && dataCollector.IsFragmentCategory )
			{
				string value = GenerateVertexScreenPositionRaw( ref dataCollector, uniqueId, precision );
				dataCollector.AddToInput( uniqueId, "screenPosition", WirePortDataType.FLOAT4, precision );
				dataCollector.AddToVertexLocalVariables( uniqueId, Constants.VertexShaderOutputStr + ".screenPosition = " + value + ";" );

				string globalResult = Constants.InputVarStr + ".screenPosition";
				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, ScreenPosRawStr, globalResult );
				return ScreenPosRawStr;
			}
			else if ( !dataCollector.IsFragmentCategory )
			{
				return GenerateVertexScreenPositionRaw( ref dataCollector, uniqueId, precision );
			}


			if ( !dataCollector.HasLocalVariableByName( ScreenPosRawStr ) )
			{
				if ( addInput )
				{
					dataCollector.AddToInput( uniqueId, SurfaceInputs.SCREEN_POS, precision );
					dataCollector.AddToIncludes( uniqueId, Constants.UnityShaderVariables );
				}

				string value = string.Format( SurfaceScreenPosFormat, UIUtils.PrecisionWirePortToCgType( precision, WirePortDataType.FLOAT4 ), Constants.InputVarStr );
				dataCollector.AddToLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4, ScreenPosRawStr, value );
			}
			return ScreenPosRawStr;
		}

		static public string GenerateScreenPositionCenter( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, bool addInput = true )
		{
			string screenPosNorm = GenerateScreenPositionNormalized( ref dataCollector, uniqueId, precision, addInput );
			string value = string.Format( "float4( {0}.xy * 2 - 1, 0, 0 )", screenPosNorm );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosCenterStr, value );
			return GeneratorUtils.ScreenPosCenterStr;
		}


		static public string GenerateScreenPositionTiled( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, bool addInput = true )
		{
			string screenPosNorm = GenerateScreenPositionNormalized( ref dataCollector, uniqueId, precision, addInput );
			string screenParams = dataCollector.IsURP ? "_ScaledScreenParams" : "_ScreenParams";
			string value = string.Format( "frac( float4( ( {0}.x * 2 - 1 ) * {1}.x / {1}.y, {0}.y * 2 - 1, 0, 0 ) )", screenPosNorm, screenParams );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosTiledStr, value );
			return GeneratorUtils.ScreenPosTiledStr;
		}

		static public void GenerateScreenPosNormToPixelFunction( ref MasterNodeDataCollector dataCollector )
		{
			dataCollector.AddFunction( ScreenPosNormToPixelFunctionBody[ 0 ], ScreenPosNormToPixelFunctionBody, false );
		}

		static public string GenerateScreenPosNormToPixelFunctionCall( string screenPosNorm, string screenParams )
		{
			return string.Format( ScreenPosNormToPixelFunctionHeader, screenPosNorm, screenParams );
		}

		static public string GenerateScreenPositionPixel( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, bool addInput = true )
		{
			string screenPosNorm = GenerateScreenPositionNormalized( ref dataCollector, uniqueId, precision, addInput );
			string screenParams = dataCollector.IsURP ? "_ScaledScreenParams" : "_ScreenParams";
			GenerateScreenPosNormToPixelFunction( ref dataCollector );
			string value = GenerateScreenPosNormToPixelFunctionCall( screenPosNorm, screenParams );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosPixelStr, value );
			return GeneratorUtils.ScreenPosPixelStr;
		}

		// DEPTH
		static public string ApplyLinearDepthModifier( ref MasterNodeDataCollector dataCollector, string instruction, DepthMode depthMode )
		{
			if ( depthMode == DepthMode.DepthLinearEye || depthMode == DepthMode.DepthLinear01 )
			{
				string space = ( depthMode == DepthMode.DepthLinearEye ) ? "LinearEyeDepth" : "Linear01Depth";

				if ( dataCollector.IsTemplate && dataCollector.IsSRP )
				{
					instruction = string.Format( "{0}( {1}, _ZBufferParams )", space, instruction );
				}
				else
				{
					instruction = string.Format( "{0}( {1} )", space, instruction );
				}
			}
			return instruction;
		}

		static public string GenerateSurfaceDepth( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, DepthMode depthMode )
		{
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetSurfaceDepth( depthMode, precision );
			}

			string screenPos = GeneratorUtils.GenerateScreenPositionRaw( ref dataCollector, uniqueId, precision );
			string instruction = string.Format( "{0}.z / {0}.w", screenPos );
			string varName;

			switch ( depthMode )
			{
				case DepthMode.DepthLinearEye:
				{
					instruction = ApplyLinearDepthModifier( ref dataCollector, instruction, depthMode );
					varName = GeneratorUtils.DepthLinearEyeStr;
					break;
				}
				case DepthMode.DepthLinear01:
				{
					instruction = ApplyLinearDepthModifier( ref dataCollector, instruction, depthMode );
					varName = GeneratorUtils.DepthLinear01Str;
					break;
				}
				case DepthMode.DepthEye:
				{
					instruction = string.Format( "( {0} ) * ( _ProjectionParams.z - _ProjectionParams.y )", instruction );
					varName = GeneratorUtils.DepthEyeStr;
					break;
				}
				case DepthMode.Depth01:
				default:
				{
					varName = GeneratorUtils.DepthRawStr;
					break;
				}
			}

			dataCollector.AddToLocalVariables( uniqueId, precision, WirePortDataType.FLOAT, varName, instruction );
			return varName;
		}

		// GRAB SCREEN POSITION
		static public string GenerateGrabScreenPosition( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision , bool addInput = true , string customScreenPos = null )
		{
			// overriding precision
			precision = PrecisionType.Float;

			string screenPos = string.Empty;
			if( string.IsNullOrEmpty( customScreenPos ) )
				screenPos = GenerateScreenPositionRaw( ref dataCollector , uniqueId , precision , addInput );
			else
				screenPos = customScreenPos;

			string computeBody = string.Empty;
			IOUtils.AddFunctionHeader( ref computeBody , GrabFunctionHeader );
			foreach( string line in GrabFunctionBody )
				IOUtils.AddFunctionLine( ref computeBody , line );
			IOUtils.CloseFunctionBody( ref computeBody );
			string functionResult = dataCollector.AddFunctions( GrabFunctionCall , computeBody , screenPos );

			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , GrabScreenPositionStr , functionResult );
			return GrabScreenPositionStr;
		}

		// GRAB SCREEN POSITION NORMALIZED
		static public string GenerateGrabScreenPositionNormalized( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision , bool addInput = true , string customScreenPos = null )
		{
			// @diogo: half screen UVs drift by about a pixel at 1080p, so pin float like the raw variant above
			precision = PrecisionType.Float;

			string stringPosVar = GenerateGrabScreenPosition( ref dataCollector, uniqueId, precision, addInput, customScreenPos );

			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, GrabScreenPositionNormalizedStr, string.Format( NormalizedScreenPosFormat, stringPosVar ) );
			return GrabScreenPositionNormalizedStr;
		}

		// SCREEN POSITION ON VERT
		static public string GenerateVertexScreenPositionRawForValue( string customVertexPosition , string outputId , ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosForValue( precision, customVertexPosition, outputId );
			}

			string screenPosVarName = ScreenPosRawStr + outputId;
			if ( !dataCollector.HasLocalVariableByName( screenPosVarName, MasterNodePortCategory.Vertex ) )
			{
				string value = string.Format( "ComputeScreenPos( UnityObjectToClipPos( {0} ) )", customVertexPosition );
				dataCollector.AddToVertexLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4, screenPosVarName, value );
			}
			return screenPosVarName;
		}

		static public string GenerateVertexScreenPositionRaw( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosRaw( precision );
			}

			if ( !dataCollector.HasLocalVariableByName( ScreenPosRawStr, MasterNodePortCategory.Vertex ) )
			{
				string value = string.Format( "ComputeScreenPos( UnityObjectToClipPos( {0}.vertex ) )", Constants.VertexShaderInputStr );
				dataCollector.AddToVertexLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4, ScreenPosRawStr, value );
			}
			return ScreenPosRawStr;
		}

		// VERTEX POSITION
		static public string GenerateVertexPosition( ref MasterNodeDataCollector dataCollector, int uniqueId, WirePortDataType type, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			// overriding precision
			var precision = PrecisionType.Float;

			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetVertexPosition( type, precision );
			}

			MasterNodePortCategory portCategory = useMasterNodeCategory ? dataCollector.PortCategory : customCategory;
			string varName = ( type == WirePortDataType.FLOAT3 ) ? VertexPosition3Str : VertexPosition4Str;
			string swizzle = ( type == WirePortDataType.FLOAT3 ) ? ".xyz" : string.Empty;

			if ( portCategory == MasterNodePortCategory.Vertex )
			{
				if ( !dataCollector.HasLocalVariableByName( varName, MasterNodePortCategory.Vertex ) )
				{
					string result = string.Format( "{0}.vertex{1}", Constants.VertexShaderInputStr, swizzle );
					dataCollector.AddToVertexLocalVariables( uniqueId, precision, type, varName, result );
				}
			}
			else if ( !dataCollector.HasLocalVariableByName( varName ) )
			{
				if ( dataCollector.TesselationActive )
				{
					dataCollector.AddToInput( -1, SurfaceInputs.WORLD_POS, precision );
					dataCollector.AddToIncludes( uniqueId, Constants.UnityShaderVariables );

					string positionWS = string.Format( "{0}.worldPos", Constants.InputVarStr );
					string result = string.Format( "mul( unity_WorldToObject, float4( {0}, 1 ) )", positionWS );
					dataCollector.AddLocalVariable( uniqueId, precision, type, varName, result );
				}
				else
				{
					string positionOS = GeneratorUtils.VertexPosition4Str;
					string positionOSOutput = string.Format( "{0}.{1}", Constants.VertexShaderOutputStr, positionOS );
					string positionOSInput = string.Format( "{0}.{1}", Constants.InputVarStr, positionOS );

					dataCollector.AddToInput( uniqueId, positionOS, WirePortDataType.FLOAT4 );

					if ( !dataCollector.HasLocalVariableByName( positionOS, MasterNodePortCategory.Vertex ) )
					{
						dataCollector.AddToVertexLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4, positionOS,
							string.Format( "{0}.vertex", Constants.VertexShaderInputStr ) );
					}
					dataCollector.AddToVertexLocalVariables( uniqueId, positionOSOutput, positionOS );

					string result = positionOSInput + swizzle;
					dataCollector.AddLocalVariable( uniqueId, precision, type, varName, result );
				}
			}
			return varName;
		}

		// VERTEX NORMAL
		static public string GenerateVertexNormal( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				return dataCollector.TemplateDataCollectorInstance.GetVertexNormal( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
			}

			string value = Constants.VertexShaderInputStr + ".normal.xyz";
			if( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
			{
				GenerateWorldNormal( ref dataCollector , uniqueId );
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , VertexNormalStr , "mul( unity_WorldToObject, float4( " + WorldNormalStr + ", 0 ) )" );
				dataCollector.AddLocalVariable( uniqueId , VertexNormalStr + " = normalize( " + VertexNormalStr + " );" );
				//dataCollector.AddToLocalVariables( uniqueId, precision, WirePortDataType.FLOAT3, VertexNormalStr, "mul( unity_WorldToObject, float4( " + WorldNormalStr + ", 0 ) )" );
			}
			else
			{
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , VertexNormalStr , value );
			}
			return VertexNormalStr;
		}

		// VERTEX TANGENT
		static public string GenerateVertexTangent( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision , WirePortDataType size )
		{
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				return dataCollector.TemplateDataCollectorInstance.GetVertexTangent( size , UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
			}

			if( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
			{
				GenerateWorldTangent( ref dataCollector , uniqueId );
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , VertexTangentStr , "mul( unity_WorldToObject, float4( " + WorldTangentStr + ", 0 ) )" );
				dataCollector.AddLocalVariable( uniqueId , VertexTangentStr +" = normalize( " + VertexTangentStr + " );");
			}
			else
			{
				string value = Constants.VertexShaderInputStr + ".tangent";
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , VertexTangentStr , value );
			}

			return ( size == WirePortDataType.FLOAT4 ) ? VertexTangentStr : VertexTangentStr + ".xyz";
		}

		// VERTEX TANGENT SIGN
		static public string GenerateVertexTangentSign( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				return dataCollector.TemplateDataCollectorInstance.GetTangentSign( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
			}

			string value = Constants.VertexShaderInputStr + ".tangent.w";
			if( dataCollector.IsFragmentCategory )
			{
				dataCollector.AddToInput( uniqueId , VertexTangentSignStr , WirePortDataType.FLOAT , PrecisionType.Half );
				dataCollector.AddToVertexLocalVariables( uniqueId , Constants.VertexShaderOutputStr + "." + VertexTangentSignStr + " = " + Constants.VertexShaderInputStr + ".tangent.w;" );
				return Constants.InputVarStr + "." + VertexTangentSignStr;
			}
			else
			{
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT , VertexTangentSignStr , value );
			}
			return VertexTangentSignStr;
		}

		// VERTEX BITANGENT
		static public string GenerateVertexBitangent( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				return dataCollector.TemplateDataCollectorInstance.GetVertexBitangent( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
			}

			if( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
			{
				GenerateWorldBitangent( ref dataCollector , uniqueId );
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , VertexBitangentStr , "mul( unity_WorldToObject, float4( " + WorldBitangentStr + ", 0 ) )" );
				dataCollector.AddLocalVariable( uniqueId , VertexBitangentStr + " = normalize( " + VertexBitangentStr + " );" );
			}
			else
			{
				GenerateVertexNormal( ref dataCollector , uniqueId , precision );
				GenerateVertexTangent( ref dataCollector , uniqueId , precision , WirePortDataType.FLOAT3 );
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , VertexBitangentStr , "cross( " + VertexNormalStr + ", " + VertexTangentStr + ") * " + Constants.VertexShaderInputStr + ".tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 )" );
			}
			return VertexBitangentStr;
		}

		// VERTEX POSITION ON FRAG
		static public string GenerateVertexPositionOnFrag( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if ( !dataCollector.HasLocalVariableByName( VertexPosition4Str ) )
			{
				dataCollector.AddToInput( uniqueId, SurfaceInputs.WORLD_POS );
				dataCollector.AddToIncludes( uniqueId, Constants.UnityShaderVariables );

				string value = "mul( unity_WorldToObject, float4( " + Constants.InputVarStr + ".worldPos , 1 ) )";

				dataCollector.AddToLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4, VertexPosition4Str, value );

			}
			return VertexPosition4Str;
		}

		// CLIP POSITION ON FRAG
		static public string GenerateScreenPositionRawOnFrag( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosRaw( precision );
			}

			if ( !dataCollector.HasLocalVariableByName( GeneratorUtils.ScreenPosRawStr ) )
			{
				dataCollector.AddToInput( uniqueId, SurfaceInputs.SCREEN_POS );
				dataCollector.AddToIncludes( uniqueId, Constants.UnityShaderVariables );

				string value = string.Format( SurfaceScreenPosFormat, UIUtils.PrecisionWirePortToCgType( precision, WirePortDataType.FLOAT4 ), Constants.InputVarStr );
				dataCollector.AddToLocalVariables( uniqueId, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosRawStr, value );
			}
			return GeneratorUtils.ScreenPosRawStr;
		}

		static public string GenerateScreenPositionNormalizedOnFrag( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosNormalized( precision );
			}

			if ( !dataCollector.HasLocalVariableByName( GeneratorUtils.ScreenPosNormStr ) )
			{
				string screenPosRaw = GenerateScreenPositionRawOnFrag( ref dataCollector, uniqueId, precision );
				string clipPlaneTestOp = string.Format( "{0}.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? {0}.z : {0}.z * 0.5 + 0.5;", GeneratorUtils.ScreenPosNormStr );
				dataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosNormStr, string.Format( GeneratorUtils.NormalizedScreenPosFormat, screenPosRaw ) );
				dataCollector.AddLocalVariable( -1, clipPlaneTestOp );
			}
			return GeneratorUtils.ScreenPosNormStr;
		}

		static public string GenerateScreenPositionCenterOnFrag( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosCenter( precision );
			}

			if ( !dataCollector.HasLocalVariableByName( GeneratorUtils.ScreenPosCenterStr ) )
			{
				string screenPosNorm = GenerateScreenPositionNormalizedOnFrag( ref dataCollector, uniqueId, precision );
				string value = string.Format( "float4( {0}.xy * 2 - 1, 0, 0 )", screenPosNorm );
				dataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosCenterStr, value );
			}
			return GeneratorUtils.ScreenPosCenterStr;
		}

		static public string GenerateScreenPositionTiledOnFrag( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosTiled( precision );
			}

			if ( !dataCollector.HasLocalVariableByName( GeneratorUtils.ScreenPosTiledStr ) )
			{
				string screenPosNorm = GenerateScreenPositionNormalizedOnFrag( ref dataCollector, uniqueId, precision );
				string screenParams = dataCollector.IsURP ? "_ScaledScreenParams" : "_ScreenParams";
				string value = string.Format( "frac( float4( ( {0}.x * 2 - 1 ) * {1}.x / {1}.y, {0}.y * 2 - 1, 0, 0 ) )", screenPosNorm, screenParams );
				dataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosTiledStr, value );
			}
			return GeneratorUtils.ScreenPosTiledStr;
		}

		static public string GenerateScreenPositionPixelOnFrag( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosPixel( precision );
			}

			if ( !dataCollector.HasLocalVariableByName( GeneratorUtils.ScreenPosPixelStr ) )
			{
				string screenPosNorm = GenerateScreenPositionNormalizedOnFrag( ref dataCollector, uniqueId, precision );
				string screenParams = dataCollector.IsURP ? "_ScaledScreenParams" : "_ScreenParams";
				GeneratorUtils.GenerateScreenPosNormToPixelFunction( ref dataCollector );
				dataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosPixelStr, GeneratorUtils.GenerateScreenPosNormToPixelFunctionCall( screenPosNorm, screenParams ) );
			}
			return GeneratorUtils.ScreenPosPixelStr;
		}

		// VIEW DIRECTION
		static public string GenerateViewVector( ref MasterNodeDataCollector dataCollector, int uniqueId, ViewSpace space = ViewSpace.World )
		{
			PrecisionType precision = UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision;
			if ( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetViewVector( precisionType: precision, space: space );
			}

			string varName;
			switch ( space )
			{
				case ViewSpace.Tangent: varName = GeneratorUtils.TangentViewVectorStr; break;
				case ViewSpace.Object: varName = GeneratorUtils.ObjectViewVectorStr; break;
				case ViewSpace.View: varName = GeneratorUtils.ViewViewVectorStr; break;
				case ViewSpace.World:
				default: varName = GeneratorUtils.WorldViewVectorStr; break;
			}

			if ( !dataCollector.HasLocalVariableByName( varName ) )
			{
				string worldPos = GenerateWorldPosition( ref dataCollector, uniqueId );
				string viewVectorWS = string.Format( "( ( unity_OrthoParams.w == 0 ) ? _WorldSpaceCameraPos - {0} : UNITY_MATRIX_V[ 2 ].xyz )", worldPos );

				string viewDir;
				if ( space == ViewSpace.Tangent )
				{
					viewDir = "mul( " + GenerateWorldToTangentMatrix( ref dataCollector, uniqueId, precision ) + ", " + viewVectorWS + " )";
				}
				else if ( space == ViewSpace.Object )
				{
					viewDir = "mul( ( float3x3 )unity_WorldToObject, " + viewVectorWS + " )";
				}
				else if ( space == ViewSpace.View )
				{
					viewDir = "mul( ( float3x3 )UNITY_MATRIX_V, " + viewVectorWS + " )";
				}
				else
				{
					viewDir = viewVectorWS;
				}

				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, varName, viewDir );
			}
			return varName;
		}

		static public string GenerateViewDirection( ref MasterNodeDataCollector dataCollector, int uniqueId, NormalizeType normalizeType = NormalizeType.Regular, ViewSpace space = ViewSpace.World )
		{
			PrecisionType precision = UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision;
			if( dataCollector.IsTemplate )
			{
				return dataCollector.TemplateDataCollectorInstance.GetViewDir( precisionType: precision, normalizeType: normalizeType, space: space );
			}

			string varName;
			switch ( space )
			{
				case ViewSpace.Tangent: varName = ( normalizeType == NormalizeType.Regular ) ? GeneratorUtils.TangentViewDirectionStr : GeneratorUtils.TangentViewDirectionSafeStr; break;
				case ViewSpace.Object: varName = ( normalizeType == NormalizeType.Regular ) ? GeneratorUtils.ObjectViewDirectionStr : GeneratorUtils.ObjectViewDirectionSafeStr; break;
				case ViewSpace.View: varName = ( normalizeType == NormalizeType.Regular ) ? GeneratorUtils.ViewViewDirectionStr : GeneratorUtils.ViewViewDirectionSafeStr; break;
				case ViewSpace.World:
				default: varName = ( normalizeType == NormalizeType.Regular ) ? GeneratorUtils.WorldViewDirectionStr : GeneratorUtils.WorldViewDirectionSafeStr; break;
			}

			if ( !dataCollector.HasLocalVariableByName( varName ) )
			{
				string viewVector = GenerateViewVector( ref dataCollector, uniqueId, space );

				if ( normalizeType == NormalizeType.Regular )
				{
					viewVector = "normalize( " + viewVector + " )";
				}
				else if ( normalizeType == NormalizeType.Safe )
				{
					viewVector = TemplateHelperFunctions.SafeNormalize( dataCollector, viewVector );
				}

				dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, varName, viewVector );
			}
			return varName;
		}

		const string FaceVertexInstr = "(dot({0},float3(0,0,1)))";
		static public string GenerateVertexFace( ref MasterNodeDataCollector dataCollector , int uniqueId )
		{
			string viewDir = GenerateViewDirection( ref dataCollector, uniqueId, space: ViewSpace.Tangent );
			dataCollector.AddLocalVariable( -1, PrecisionType.Float, WirePortDataType.FLOAT, FaceVertex, string.Format( FaceVertexInstr, viewDir ) );
			return FaceVertex;

		}
		// VIEW POS
		static public string GenerateViewPositionOnFrag( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if( dataCollector.IsTemplate )
				UnityEngine.Debug.LogWarning( "View Pos not implemented on Templates" );

			string vertexName = GenerateVertexPositionOnFrag( ref dataCollector, uniqueId, precision );
			string value = string.Format( "UnityObjectToViewPos( {0} )", vertexName );
			dataCollector.AddToLocalVariables( uniqueId, precision, WirePortDataType.FLOAT3, ViewPositionStr, value );
			return ViewPositionStr;
		}

		// SCREEN DEPTH
		static public string GenerateScreenDepthOnFrag( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if( dataCollector.IsTemplate )
				UnityEngine.Debug.LogWarning( "Screen Depth not implemented on Templates" );

			string viewPos = GenerateViewPositionOnFrag( ref dataCollector, uniqueId, precision );
			string value = string.Format( "-{0}.z", viewPos );
			dataCollector.AddToLocalVariables( uniqueId, precision, WirePortDataType.FLOAT, ScreenDepthStr, value );
			return ScreenDepthStr;
		}

		// LIGHT DIRECTION WORLD
		static public string GenerateWorldLightDirection( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			dataCollector.AddToIncludes( uniqueId, Constants.UnityCgLibFuncs );
			string worldPos = GeneratorUtils.GenerateWorldPosition( ref dataCollector, uniqueId );
			dataCollector.AddLocalVariable( uniqueId, "#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld" );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, WorldLightDirStr, "0" );
			dataCollector.AddLocalVariable( uniqueId, "#else //aseld" );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, WorldLightDirStr, ( dataCollector.SafeNormalizeLightDir ? "Unity_SafeNormalize" : "normalize" ) + "( UnityWorldSpaceLightDir( " + worldPos + " ) )" );
			dataCollector.AddLocalVariable( uniqueId, "#endif //aseld" );
			return WorldLightDirStr;
		}

		private static readonly string[] SafeNormalize =
		{
			"float{0} ASESafeNormalize(float{0} inVec)\n",
			"{\n",
			"\tfloat dp3 = max(1.175494351e-38, dot(inVec, inVec));\n",
			"\treturn inVec* rsqrt(dp3);\n",
			"}\n",
		};

		private static readonly string ASEUnpackNormalRGBCall = "ASEUnpackNormalRGB({0},{1})";
		private static readonly string[] ASEUnpackNormalRGB =
		{
			"float3 ASEUnpackNormalRGB(float4 PackedNormal, float Scale = 1.0 )\n",
			"{\n",
			"\tfloat3 normal;\n",
			"\tnormal.xyz = PackedNormal.rgb * 2.0 - 1.0;\n",
			"\tnormal.xy *= Scale;\n",
			"\treturn normal;\n",
			"}\n"
		};

		static public string NormalizeValue( ref MasterNodeDataCollector dataCollector , bool safeNormalize , WirePortDataType dataType, string value )
		{
			string normalizeInstruction = string.Empty;
			if( safeNormalize )
			{
				string[] finalFunction = null;
				string[] funcVersion = SafeNormalize;

				finalFunction = new string[ funcVersion.Length ];

				switch( dataType )
				{
					case WirePortDataType.FLOAT:
					finalFunction[0] = string.Format( funcVersion[ 0 ] , string.Empty );
					break;
					case WirePortDataType.FLOAT2:
					finalFunction[ 0 ] = string.Format( funcVersion[ 0 ] , "2" );
					break;
					case WirePortDataType.FLOAT3:
					finalFunction[ 0 ] = string.Format( funcVersion[ 0 ] , "3" );
					break;
					case WirePortDataType.FLOAT4:
					case WirePortDataType.COLOR:
					finalFunction[ 0 ] = string.Format( funcVersion[ 0 ] , "4" );
					break;
					default:
					case WirePortDataType.FLOAT2x2:
					case WirePortDataType.FLOAT3x3:
					case WirePortDataType.FLOAT4x4:
					case WirePortDataType.INT:
					case WirePortDataType.OBJECT:
					case WirePortDataType.SAMPLER1D:
					case WirePortDataType.SAMPLER2D:
					case WirePortDataType.SAMPLER3D:
					case WirePortDataType.SAMPLERCUBE:
					case WirePortDataType.UINT:
					case WirePortDataType.UINT4:
					case WirePortDataType.SAMPLER2DARRAY:
					case WirePortDataType.SAMPLERSTATE:return value;
				}

				for( int i = 1 ; i < funcVersion.Length ; i++ )
				{
					finalFunction[ i ] = funcVersion[ i ];
				}
				dataCollector.AddFunction( finalFunction[ 0 ] , finalFunction , false );
				normalizeInstruction = "ASESafeNormalize";
			}
			else
			{
				normalizeInstruction = "normalize";
			}

			return normalizeInstruction = normalizeInstruction + "( " + value + " )";

		}
		// LIGHT DIRECTION Object
		static public string GenerateObjectLightDirection( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, string vertexPos )
		{
			dataCollector.AddToIncludes( uniqueId, Constants.UnityCgLibFuncs );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, ObjectLightDirStr, "normalize( ObjSpaceLightDir( " + vertexPos + " ) )" );
			return ObjectLightDirStr;
		}

		// UNPACK NORMALS
		public static string GenerateUnpackNormalStr( ref MasterNodeDataCollector dataCollector, PrecisionType precision, int uniqueId, string outputId, string src, bool applyScale, string scale, UnpackInputMode inputMode )
		{
			string funcName;

			if( inputMode == UnpackInputMode.Object )
			{
				dataCollector.AddFunction( ASEUnpackNormalRGB[ 0 ] , ASEUnpackNormalRGB , false );
				return string.Format( ASEUnpackNormalRGBCall, src , scale );
			}

			if( dataCollector.IsTemplate && dataCollector.IsSRP )
			{
				if( applyScale )
				{
					dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, "unpack" + outputId, "UnpackNormalScale( " + src + ", " + scale + " )" );
					dataCollector.AddLocalVariable( uniqueId, "unpack" + outputId + ".z = lerp( 1, unpack" + outputId + ".z, saturate(" + scale + ") );" );
					funcName = "unpack" + outputId;
				}
				else
				{
					funcName = "UnpackNormalScale( " + src + ", " + scale + " )";
				}
			}
			else
			{
				funcName = applyScale ? "UnpackScaleNormal( " + src + ", " + scale + " )" : "UnpackNormal( " + src + " )";
			}
			return funcName;
		}

		//MATRIX INVERSE
		// 2x2
		public static string Inverse2x2Header = "Inverse2x2( {0} )";
		public static string[] Inverse2x2Function =
		{
			"{0}2x2 Inverse2x2({0}2x2 input)\n",
			"{\n",
			"\treturn {0}2x2( input._m11, -input._m01, -input._m10, input._m00 ) / determinant( input );\n",
			"}\n"
		};

		public static bool[] Inverse2x2FunctionFlags =
		{
			true,
			false,
			true,
			false
		};

		public static void Add2x2InverseFunction( ref MasterNodeDataCollector dataCollector, string precisionString )
		{
			if ( !dataCollector.HasFunction( Inverse2x2Header ) )
			{
				//Hack to be used util indent is properly used
				int currIndent = UIUtils.ShaderIndentLevel;
				if ( dataCollector.IsTemplate )
				{
					UIUtils.ShaderIndentLevel = 0;
				}
				else
				{
					UIUtils.ShaderIndentLevel = 1;
					UIUtils.ShaderIndentLevel++;
				}
				string finalFunction = string.Empty;
				for ( int i = 0; i < Inverse2x2Function.Length; i++ )
				{
					finalFunction += UIUtils.ShaderIndentTabs + ( Inverse2x2FunctionFlags[ i ] ? string.Format( Inverse2x2Function[ i ], precisionString ) : Inverse2x2Function[ i ] );
				}


				UIUtils.ShaderIndentLevel = currIndent;

				dataCollector.AddFunction( Inverse2x2Header, finalFunction );
			}
		}

		// 3x3
		public static string Inverse3x3Header = "Inverse3x3( {0} )";
		public static string[] Inverse3x3Function =
		{
			"{0}3x3 Inverse3x3({0}3x3 input)\n",
			"{\n",
			"\t{0}3 a = input._11_21_31;\n",
			"\t{0}3 b = input._12_22_32;\n",
			"\t{0}3 c = input._13_23_33;\n",
			"\treturn {0}3x3(cross(b,c), cross(c,a), cross(a,b)) * (1.0 / dot(a,cross(b,c)));\n",
			"}\n"
		};

		public static bool[] Inverse3x3FunctionFlags =
		{
			true,
			false,
			true,
			true,
			true,
			true,
			false
		};

		public static void Add3x3InverseFunction( ref MasterNodeDataCollector dataCollector, string precisionString )
		{
			if( !dataCollector.HasFunction( Inverse3x3Header ) )
			{
				//Hack to be used util indent is properly used
				int currIndent = UIUtils.ShaderIndentLevel;
				if( dataCollector.IsTemplate )
				{
					UIUtils.ShaderIndentLevel = 0;
				}
				else
				{
					UIUtils.ShaderIndentLevel = 1;
					UIUtils.ShaderIndentLevel++;
				}
				string finalFunction = string.Empty;
				for( int i = 0; i < Inverse3x3Function.Length; i++ )
				{
					finalFunction += UIUtils.ShaderIndentTabs + ( Inverse3x3FunctionFlags[ i ] ? string.Format( Inverse3x3Function[ i ], precisionString ) : Inverse3x3Function[ i ] );
				}


				UIUtils.ShaderIndentLevel = currIndent;

				dataCollector.AddFunction( Inverse3x3Header, finalFunction );
			}
		}

		public static string GenerateValueInVertex( ref MasterNodeDataCollector dataCollector, int uniqueId, WirePortDataType dataType, PrecisionType currentPrecisionType, string dataValue, string dataName, bool createInterpolator )
		{
			if( !dataCollector.IsFragmentCategory )
				return dataValue;

			//TEMPLATES
			if( dataCollector.IsTemplate )
			{
				if( createInterpolator && dataCollector.TemplateDataCollectorInstance.HasCustomInterpolatedData( dataName ) )
					return dataName;

				MasterNodePortCategory category = dataCollector.PortCategory;
				dataCollector.PortCategory = MasterNodePortCategory.Vertex;

				dataCollector.PortCategory = category;

				if( createInterpolator )
				{
					dataCollector.TemplateDataCollectorInstance.RegisterCustomInterpolatedData( dataName, dataType, currentPrecisionType, dataValue );
				}
				else
				{
					dataCollector.AddToVertexLocalVariables( -1, currentPrecisionType, dataType, dataName, dataValue );
				}

				return dataName;
			}

			//SURFACE
			{
				if( dataCollector.TesselationActive )
				{
					UIUtils.ShowMessage( "Unable to use Vertex to Frag when Tessellation is active" );
					switch( dataType )
					{
						case WirePortDataType.FLOAT2:
						{
							return "(0).xx";
						}
						case WirePortDataType.FLOAT3:
						{
							return "(0).xxx";
						}
						case WirePortDataType.FLOAT4:
						case WirePortDataType.COLOR:
						{
							return "(0).xxxx";
						}
					}
					return "0";
				}

				if( createInterpolator )
					dataCollector.AddToInput( uniqueId, dataName, dataType, currentPrecisionType );

				MasterNodePortCategory portCategory = dataCollector.PortCategory;
				dataCollector.PortCategory = MasterNodePortCategory.Vertex;
				if( createInterpolator )
				{
					dataCollector.AddLocalVariable( uniqueId, Constants.VertexShaderOutputStr + "." + dataName, dataValue + ";" );
				}
				else
				{
					dataCollector.AddLocalVariable( uniqueId, currentPrecisionType, dataType, dataName, dataValue );
				}
				dataCollector.PortCategory = portCategory;
				return createInterpolator ? Constants.InputVarStr + "." + dataName : dataName;
			}
		}

		public static void AddCustomStandardSamplingMacros( ref MasterNodeDataCollector dataCollector, TextureType type, MipType mip )
		{
			AddCustomStandardSamplingMacros( ref dataCollector, Constants.TextureToWire[ type ], mip );
		}

		public static void AddCustomStandardSamplingMacros( ref MasterNodeDataCollector dataCollector, WirePortDataType type, MipType mip )
		{
			MacrosMask result = MacrosMask.NONE;
			switch( mip )
			{
				default:
				case MipType.Auto:
				result |= MacrosMask.AUTO;
				break;
				case MipType.MipLevel:
				result |= MacrosMask.LOD;
				break;
				case MipType.MipBias:
				result |= MacrosMask.BIAS;
				break;
				case MipType.Derivative:
				result |= MacrosMask.GRAD;
				break;
			}

			switch( type )
			{
				default:
				case WirePortDataType.SAMPLER2D:
				dataCollector.Using2DMacrosMask |= result;
				break;
				case WirePortDataType.SAMPLER3D:
				dataCollector.Using3DMacrosMask |= result;
				break;
				case WirePortDataType.SAMPLERCUBE:
				dataCollector.UsingCUBEMacrosMask |= result;
				break;
				case WirePortDataType.SAMPLER2DARRAY:
				dataCollector.Using2DArrayMacrosMask |= result;
				break;
			}
		}

		public static void AddCustom3DSRPMacros( ref MasterNodeDataCollector dataCollector )
		{
			// add just once
			if( dataCollector.UsingExtra3DSRPMacros )
				return;

			dataCollector.UsingExtra3DSRPMacros = true;
			for( int i = 0; i < Constants.CustomSRPSamplingMacros.Length; i++ )
				dataCollector.AddToDirectives( Constants.CustomSRPSamplingMacros[ i ] );
		}

		public static void AddCustomArraySamplingMacros( ref MasterNodeDataCollector dataCollector )
		{
			// add just once
			if( dataCollector.UsingArrayDerivatives )
				return;

			dataCollector.UsingArrayDerivatives = true;
			for( int i = 0; i < Constants.CustomArraySamplingMacros.Length; i++ )
				dataCollector.AddToDirectives( Constants.CustomArraySamplingMacros[ i ] );
		}

		/*public static void AddCustomASEMacros( ref MasterNodeDataCollector dataCollector )
		{
			string varPrefix = dataCollector.IsSRP ? varPrefix = "TEXTURE" : "UNITY_DECLARE_TEX";

			if( dataCollector.IsSRP )
			{
				for( int i = 0; i < Constants.CustomASESRPArgsMacros.Length; i++ )
				{
					dataCollector.AddToDirectives( Constants.CustomASESRPArgsMacros[ i ] );
				}

				for( int i = 0; i < Constants.CustomSRPSamplingMacros.Length; i++ )
				{
					dataCollector.AddToDirectives( Constants.CustomSRPSamplingMacros[ i ] );
				}
			}
			else
			{

				for( int i = 0; i < Constants.CustomASEStandardArgsMacros.Length; i++ )
				{
					dataCollector.AddToDirectives( Constants.CustomASEStandardArgsMacros[ i ] );
				}

				for( int i = 0; i < Constants.CustomStandardSamplingMacros.Length; i++ )
				{
					dataCollector.AddToDirectives( Constants.CustomStandardSamplingMacros[ i ] );
				}
			}

			for( int i = 0; i < Constants.CustomASEDeclararionMacros.Length; i++ )
			{
				string value = string.Format( Constants.CustomASEDeclararionMacros[ i ], varPrefix );
				dataCollector.AddToDirectives( value );
			}

			string samplePrefix = string.Empty;
			string samplerArgs = string.Empty;
			string samplerDecl = string.Empty;

			if( dataCollector.IsSRP )
			{
				samplePrefix = "SAMPLE_TEXTURE";
				samplerArgs = "samplerName,";

				for( int i = 0; i < Constants.CustomASESamplingMacros.Length; i++ )
				{
					string value = string.Format( Constants.CustomASESamplingMacros[ i ], samplerArgs, samplePrefix, samplerDecl );
					dataCollector.AddToDirectives( value );
				}
			}
			else
			{
				samplePrefix = "UNITY_SAMPLE_TEX";
				samplerArgs = "samplerName,";
				samplerDecl = "_SAMPLER";
				dataCollector.AddToDirectives( Constants.CustomASEStandarSamplingMacrosHelper[ 0 ] );
				for( int i = 0; i < Constants.CustomASESamplingMacros.Length; i++ )
				{
					string value = string.Format( Constants.CustomASESamplingMacros[ i ], samplerArgs, samplePrefix, samplerDecl );
					dataCollector.AddToDirectives( value );
				}
				dataCollector.AddToDirectives( Constants.CustomASEStandarSamplingMacrosHelper[ 1 ] );
				samplerArgs = string.Empty;
				samplerDecl = string.Empty;
				for( int i = 0; i < Constants.CustomASESamplingMacros.Length; i++ )
				{
					string value = string.Format( Constants.CustomASESamplingMacros[ i ], samplerArgs, samplePrefix, samplerDecl );
					dataCollector.AddToDirectives( value );
				}
				dataCollector.AddToDirectives( Constants.CustomASEStandarSamplingMacrosHelper[ 2 ] );
			}
		}*/

		public static void RegisterUnity2019MatrixDefines( ref MasterNodeDataCollector dataCollector )
		{
			if( dataCollector.IsSRP && dataCollector.TemplateDataCollectorInstance.IsHDRP )
			{
				//dataCollector.AddToDefines( -1, "unity_CameraProjection UNITY_MATRIX_P" );
				//dataCollector.AddToDefines( -1, "unity_CameraInvProjection UNITY_MATRIX_I_P" );
				//dataCollector.AddToDefines( -1, "unity_WorldToCamera UNITY_MATRIX_V" );
				//dataCollector.AddToDefines( -1, "unity_CameraToWorld UNITY_MATRIX_I_V" );

				dataCollector.AddToUniforms( -1, "float4x4 unity_CameraProjection;" );
				dataCollector.AddToUniforms( -1, "float4x4 unity_CameraInvProjection;" );
				dataCollector.AddToUniforms( -1, "float4x4 unity_WorldToCamera;" );
				dataCollector.AddToUniforms( -1, "float4x4 unity_CameraToWorld;" );
			}
		}
	}
}

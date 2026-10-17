// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace AmplifyShaderEditor
{
	public enum NormalizeType
	{
		Off,
		Regular,
		Safe
	}

	public enum DepthMode
	{
		DepthLinearEye,
		DepthLinear01,
		DepthEye,
		Depth01,
	}

	public class InterpDataHelper
	{
		public string VarName;
		public WirePortDataType VarType;
		public bool IsSingleComponent;
		public bool SetAtCompileTime;
		public InterpDataHelper( WirePortDataType varType, string varName, bool isSingleComponent = true , bool setAtCompileTime = false )
		{
			VarName = varName;
			VarType = varType;
			IsSingleComponent = isSingleComponent;
			SetAtCompileTime = setAtCompileTime;
		}
	}

	public class TemplateCustomData
	{
		public WirePortDataType DataType;
		public string Name;
		public bool IsVertex;
		public bool IsFragment;
		// @diogo: fragment-side interpolator read, kept so a re-register from a disjoint branch scope can
		// re-declare the local without requesting a second interpolator (#365)
		public string FragmentValue;
		// OR of the conditions of every fragment read; null when read unconditionally
		public InterpolatorCondition Condition;
		public TemplateInterpElement InterpElement;
		// Vertex write held back until Condition is final, null when emitted eagerly
		public string DeferredWrite;
		public TemplateCustomData( string name, WirePortDataType dataType )
		{
			name = Name;
			DataType = dataType;
			IsVertex = false;
			IsFragment = false;
			FragmentValue = string.Empty;
		}
	}

	public class TemplateInputParameters
	{
		public WirePortDataType Type;
		public string Name;
		public string Declaration;
		public TemplateSemantics Semantic;
		public TemplateInputParameters( WirePortDataType type, PrecisionType precision, string name, TemplateSemantics semantic, string custom = null )
		{
			Type = type;
			Name = name;
			Semantic = semantic;
			Declaration = string.Format( "{0} {1} : {2}", UIUtils.PrecisionWirePortToCgType( precision, type ), Name, Semantic );
			if( !string.IsNullOrEmpty( custom ) )
				Declaration = custom;
		}
	}

	public class TemplateDataCollector
	{
		private const int MaxUV = 8;
		private int[] m_vertexUVUsage = { 0, 0, 0, 0, 0, 0, 0, 0 };
		private int[] m_fragmentUVUsage = { 0, 0, 0, 0, 0, 0, 0, 0 };

		private int m_multipassSubshaderIdx = 0;
		private int m_multipassPassIdx = 0;
		private TemplateMultiPass m_currentTemplate;
		private TemplateSRPType m_currentSRPType = TemplateSRPType.BiRP;

		private Dictionary<string, TemplateCustomData> m_customInterpolatedData;
		private Dictionary<string, TemplateVertexData> m_registeredVertexData;

		private Dictionary<TemplateInfoOnSematics, InterpDataHelper> m_availableFragData;
		private Dictionary<TemplateInfoOnSematics, InterpDataHelper> m_availableVertData;
		private TemplateInterpData m_interpolatorData;
		private Dictionary<TemplateSemantics, TemplateVertexData> m_vertexDataDict;
		private TemplateData m_currentTemplateData;
		private MasterNodeDataCollector m_currentDataCollector;
		public Dictionary<TemplateSemantics, TemplateInputParameters> m_vertexInputParams;
		public Dictionary<TemplateSemantics, TemplateInputParameters> m_fragmentInputParams;

		private Dictionary<TemplateInfoOnSematics, TemplateLocalVarData> m_specialVertexLocalVars;
		private Dictionary<TemplateInfoOnSematics, TemplateLocalVarData> m_specialFragmentLocalVars;

		private List<PropertyDataCollector> m_lateDirectivesList = new List<PropertyDataCollector>();
		private Dictionary<string, PropertyDataCollector> m_lateDirectivesDict = new Dictionary<string, PropertyDataCollector>();

		private List<PropertyDataCollector> m_srpBatcherPropertiesList = new List<PropertyDataCollector>();
		private List<PropertyDataCollector> m_fullSrpBatcherPropertiesList = new List<PropertyDataCollector>();
		private Dictionary<string, PropertyDataCollector> m_dotsPropertiesDict = new Dictionary<string, PropertyDataCollector>();
		private List<PropertyDataCollector> m_dotsPropertiesList = new List<PropertyDataCollector>();
		private List<PropertyDataCollector> m_dotsDefinesList = new List<PropertyDataCollector>();
		private List<PropertyDataCollector> m_fullDotsPropertiesList = new List<PropertyDataCollector>();
		private Dictionary<string, PropertyDataCollector> m_srpBatcherPropertiesDict = new Dictionary<string, PropertyDataCollector>();

		public void CopySRPPropertiesFromDataCollector( int nodeId, TemplateDataCollector dataCollector )
		{
			for( int i = 0; i < dataCollector.SrpBatcherPropertiesList.Count; i++ )
			{
				AddSRPBatcherProperty( nodeId, dataCollector.SrpBatcherPropertiesList[ i ].PropertyName );
			}
		}

		public void AddSRPBatcherProperty( int nodeID, string property )
		{
			if( !m_srpBatcherPropertiesDict.ContainsKey( property ) )
			{
				PropertyDataCollector newValue = new PropertyDataCollector( nodeID, property );
				m_srpBatcherPropertiesDict.Add( property, newValue );
				m_srpBatcherPropertiesList.Add( newValue );
			}
		}

		// @diogo: DOTS metadata constants are addressed by their position in the block, so every
		// pass has to declare the exact same set in the same order
		public void CopyDotsPropertiesFromDataCollector( int nodeId, TemplateDataCollector dataCollector )
		{
			for ( int i = 0; i < dataCollector.DotsPropertiesList.Count; i++ )
			{
				AddDotsProperty( nodeId, dataCollector.DotsPropertiesList[ i ].PropertyName, dataCollector.DotsDefinesList[ i ].PropertyName, dataCollector.DotsPropertiesList[ i ].OrderIndex );
			}
		}

		public void AddDotsProperty( int nodeId, string property, string define, int orderIndex )
		{
			if ( !m_dotsPropertiesDict.ContainsKey( property ) )
			{
				PropertyDataCollector newValue = new PropertyDataCollector( nodeId, property, orderIndex );
				m_dotsPropertiesDict.Add( property, newValue );
				m_dotsPropertiesList.Add( newValue );
				m_dotsDefinesList.Add( new PropertyDataCollector( nodeId, define, orderIndex ) );
			}
		}

		private int[] GetUVUsage( MasterNodePortCategory category )
		{
			return ( category == MasterNodePortCategory.Vertex ) ? m_vertexUVUsage : m_fragmentUVUsage;
		}

		public void SetUVUsage( int uv, WirePortDataType type, MasterNodePortCategory category )
		{
			SetUVUsage( uv, TemplateHelperFunctions.DataTypeChannelUsage[ type ], category );
		}

		public void SetUVUsage( int uv, int size, MasterNodePortCategory category )
		{
			if( uv >= 0 && uv < MaxUV )
			{
				int[] usage = GetUVUsage( category );
				usage[ uv ] = Mathf.Max( usage[ uv ], size );
			}
		}

		public void CloseLateDirectives()
		{
			if( m_lateDirectivesList.Count > 0 )
			{
				m_lateDirectivesList.Add( new PropertyDataCollector( -1, string.Empty ) );
			}
		}

		public void AddMainLightShadowAttenuationDependsURP( int UniqueId )
		{
			bool isForward = this.m_currentDataCollector.CurrentPassNameContains( "Forward" );
			bool isGBuffer = this.m_currentDataCollector.CurrentPassNameContains( "GBuffer" );

			// Pragmas
			var pragmas = new List<string>();
			if ( ASEPackageManagerHelper.CurrentSRPVersion >= 170100 )
			{
				if ( isForward || isGBuffer )
				{
					pragmas.Add( "multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN" );
					pragmas.Add( "multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH" );
				}
			}
			else if ( ASEPackageManagerHelper.CurrentSRPVersion >= 140009 )
			{
				if ( isForward || isGBuffer )
				{
					pragmas.Add( "multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN" );
					pragmas.Add( "multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH" );
				}
			}
			else
			{
				if ( isForward || isGBuffer )
				{
					pragmas.Add( "multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN" );
					pragmas.Add( "multi_compile_fragment _ _SHADOWS_SOFT" );
				}
			}

			for ( int i = 0; i < pragmas.Count; i++ )
			{
				m_currentDataCollector.AddToPragmas( UniqueId, pragmas[ i ] );
			}
		}

		public void AddMainLightShadowAttenuationDependsHDRP( int UniqueId )
		{
			m_currentDataCollector.AddToIncludes( UniqueId, "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl" );

			if ( ASEPackageManagerHelper.CurrentHDRPBaseline >= SRPBaseline.ASE_SRP_16_X )
			{
				m_currentDataCollector.AddToDirectives(
					"#if !defined( PUNCTUAL_SHADOW_LOW ) && !defined( PUNCTUAL_SHADOW_MEDIUM ) && !defined( PUNCTUAL_SHADOW_HIGH )\n" +
					"\t#define PUNCTUAL_SHADOW_LOW\n" +
					"#endif\n" );

				m_currentDataCollector.AddToPragmas( UniqueId, "multi_compile_fragment DIRECTIONAL_SHADOW_LOW DIRECTIONAL_SHADOW_MEDIUM DIRECTIONAL_SHADOW_HIGH" );
			}
			else
			{
				m_currentDataCollector.AddToDirectives(
					"#if !defined( SHADOW_LOW ) && !defined( SHADOW_MEDIUM ) && !defined( SHADOW_HIGH )\n" +
						"\t#define SHADOW_LOW\n" +
					"#endif\n" );

				m_currentDataCollector.AddToPragmas( UniqueId, "multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH" );
			}

			m_currentDataCollector.AddToDirectives(
				"#if !defined( AREA_SHADOW_MEDIUM ) && !defined( AREA_SHADOW_HIGH )\n" +
				"\t#define AREA_SHADOW_MEDIUM\n" +
				"#endif\n" +

				// @diogo: these are necessary here because HDRP defines a dummy one if these are not included earlier, causing a duplicated declaration
				"#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl\"\n" +
				"#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowContext.hlsl\"\n" );
		}

		public void AddLateDirective( AdditionalLineType type, string value )
		{

			if( !m_lateDirectivesDict.ContainsKey( value ) )
			{
				string formattedValue = string.Empty;
				switch( type )
				{
					case AdditionalLineType.Include: formattedValue = string.Format( Constants.IncludeFormat, value ); break;
					case AdditionalLineType.Define: formattedValue = string.Format( Constants.DefineFormat, value ); break;
					case AdditionalLineType.Pragma: formattedValue = string.Format( Constants.PragmaFormat, value ); break;
					case AdditionalLineType.Custom: formattedValue = value; break;
				}
				PropertyDataCollector property = new PropertyDataCollector( -1, formattedValue );
				m_lateDirectivesDict.Add( value, property );
				m_lateDirectivesList.Add( property );
			}
		}

		public void SetMultipassInfo( TemplateMultiPass currentTemplate, int subShaderIdx, int passIdx, TemplateSRPType currentSRPType )
		{
			m_currentTemplate = currentTemplate;
			m_multipassSubshaderIdx = subShaderIdx;
			m_multipassPassIdx = passIdx;
			m_currentSRPType = currentSRPType;
		}

		public bool HasDirective( AdditionalLineType type, string value )
		{
			switch( type )
			{
				case AdditionalLineType.Include:
				{
					return m_currentTemplate.SubShaders[ m_multipassSubshaderIdx ].Modules.IncludePragmaContainer.HasInclude( value ) ||
					m_currentTemplate.SubShaders[ m_multipassSubshaderIdx ].Passes[ m_multipassPassIdx ].Modules.IncludePragmaContainer.HasInclude( value );
				}
				case AdditionalLineType.Define:
				{
					return m_currentTemplate.SubShaders[ m_multipassSubshaderIdx ].Modules.IncludePragmaContainer.HasDefine( value ) ||
					m_currentTemplate.SubShaders[ m_multipassSubshaderIdx ].Passes[ m_multipassPassIdx ].Modules.IncludePragmaContainer.HasDefine( value );
				}
				case AdditionalLineType.Pragma:
				{
					return m_currentTemplate.SubShaders[ m_multipassSubshaderIdx ].Modules.IncludePragmaContainer.HasPragma( value ) ||
					m_currentTemplate.SubShaders[ m_multipassSubshaderIdx ].Passes[ m_multipassPassIdx ].Modules.IncludePragmaContainer.HasPragma( value );
				}
			}

			return false;
		}

		public void FillSpecialVariables( TemplatePass currentPass )
		{
			m_specialVertexLocalVars = new Dictionary<TemplateInfoOnSematics, TemplateLocalVarData>();
			m_specialFragmentLocalVars = new Dictionary<TemplateInfoOnSematics, TemplateLocalVarData>();
			int localVarAmount = currentPass.LocalVarsList.Count;
			for( int i = 0; i < localVarAmount; i++ )
			{
				if( currentPass.LocalVarsList[ i ].IsSpecialVar )
				{
					if( currentPass.LocalVarsList[ i ].Category == MasterNodePortCategory.Vertex )
					{
						m_specialVertexLocalVars.Add( currentPass.LocalVarsList[ i ].SpecialVarType, currentPass.LocalVarsList[ i ] );
					}
					else
					{
						m_specialFragmentLocalVars.Add( currentPass.LocalVarsList[ i ].SpecialVarType, currentPass.LocalVarsList[ i ] );
					}
				}
			}
		}

		public void BuildFromTemplateData( MasterNodeDataCollector dataCollector, TemplateData templateData )
		{
			m_registeredVertexData = new Dictionary<string, TemplateVertexData>();
			m_customInterpolatedData = new Dictionary<string, TemplateCustomData>();


			m_currentDataCollector = dataCollector;
			m_currentTemplateData = templateData;

			m_vertexDataDict = new Dictionary<TemplateSemantics, TemplateVertexData>();
			if( templateData.VertexDataList != null )
			{
				for( int i = 0; i < templateData.VertexDataList.Count; i++ )
				{
					m_vertexDataDict.Add( templateData.VertexDataList[ i ].Semantics, new TemplateVertexData( templateData.VertexDataList[ i ] ) );
				}
			}

			m_availableFragData = new Dictionary<TemplateInfoOnSematics, InterpDataHelper>();
			if( templateData.InterpolatorData != null && templateData.FragmentFunctionData != null )
			{
				m_interpolatorData = new TemplateInterpData( templateData.InterpolatorData );
				int fragCount = templateData.InterpolatorData.Interpolators.Count;
				for( int i = 0; i < fragCount; i++ )
				{
					string varName = string.Empty;
					if( templateData.InterpolatorData.Interpolators[ i ].ExcludeStructPrefix )
					{
						varName = templateData.InterpolatorData.Interpolators[ i ].VarName;
					}
					else if( templateData.InterpolatorData.Interpolators[ i ].IsSingleComponent )
					{
						varName = string.Format( TemplateHelperFunctions.TemplateVarFormat,
													templateData.FragmentFunctionData.InVarName,
													templateData.InterpolatorData.Interpolators[ i ].VarNameWithSwizzle );
					}
					else
					{
						varName = string.Format( templateData.InterpolatorData.Interpolators[ i ].VarNameWithSwizzle, templateData.FragmentFunctionData.InVarName );
					}

					m_availableFragData.Add( templateData.InterpolatorData.Interpolators[ i ].DataInfo,
					new InterpDataHelper( templateData.InterpolatorData.Interpolators[ i ].SwizzleType,
					varName,
					templateData.InterpolatorData.Interpolators[ i ].IsSingleComponent ) );
				}
			}


			m_availableVertData = new Dictionary<TemplateInfoOnSematics, InterpDataHelper>();
			if( templateData.VertexFunctionData != null && templateData.VertexDataList != null )
			{
				int vertCount = templateData.VertexDataList.Count;
				for( int i = 0; i < vertCount; i++ )
				{
					string varName = string.Empty;
					if( templateData.VertexDataList[ i ].ExcludeStructPrefix )
					{
						varName = templateData.VertexDataList[ i ].VarName;
					}
					else
					{
						varName = string.Format( TemplateHelperFunctions.TemplateVarFormat, templateData.VertexFunctionData.InVarName, templateData.VertexDataList[ i ].VarNameWithSwizzle );
					}

					m_availableVertData.Add( templateData.VertexDataList[ i ].DataInfo,
					new InterpDataHelper( templateData.VertexDataList[ i ].SwizzleType,
					varName,
					templateData.VertexDataList[ i ].IsSingleComponent ) );
				}
			}
		}

		public void RegisterFragInputParams( WirePortDataType type, PrecisionType precision, string name, TemplateSemantics semantic, string custom )
		{
			if( m_fragmentInputParams == null )
				m_fragmentInputParams = new Dictionary<TemplateSemantics, TemplateInputParameters>();

			m_fragmentInputParams.Add( semantic, new TemplateInputParameters( type, precision, name, semantic, custom ) );
		}

		public void RegisterFragInputParams( WirePortDataType type, PrecisionType precision, string name, TemplateSemantics semantic )
		{
			if( m_fragmentInputParams == null )
				m_fragmentInputParams = new Dictionary<TemplateSemantics, TemplateInputParameters>();

			m_fragmentInputParams.Add( semantic, new TemplateInputParameters( type, precision, name, semantic ) );
		}

		public void RegisterVertexInputParams( WirePortDataType type , PrecisionType precision , string name , TemplateSemantics semantic, string custom )
		{
			if( m_vertexInputParams == null )
				m_vertexInputParams = new Dictionary<TemplateSemantics , TemplateInputParameters>();

			m_vertexInputParams.Add( semantic , new TemplateInputParameters( type , precision , name , semantic, custom ) );
		}

		public void RegisterVertexInputParams( WirePortDataType type, PrecisionType precision, string name, TemplateSemantics semantic )
		{
			if( m_vertexInputParams == null )
				m_vertexInputParams = new Dictionary<TemplateSemantics, TemplateInputParameters>();

			m_vertexInputParams.Add( semantic, new TemplateInputParameters( type, precision, name, semantic ) );
		}

		public string GetInstanceId()
		{
			var precision = PrecisionType.Float;
			bool useMasterNodeCategory = true;
			MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment;

			m_currentDataCollector.AddToDirectives( "#define INSTANCEID_SEMANTIC SV_InstanceID" );

			if ( HasInfo( TemplateInfoOnSematics.INSTANCEID, useMasterNodeCategory, customCategory ) )
			{
				InterpDataHelper info = GetInfo( TemplateInfoOnSematics.INSTANCEID, useMasterNodeCategory, customCategory );
				return info.VarName;
			}
			else
			{
				MasterNodePortCategory category = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
				string name = TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_InstanceID ];
				string varName = RegisterInfoOnSemantic( category, TemplateInfoOnSematics.INSTANCEID, TemplateSemantics.INSTANCEID_SEMANTIC, name, WirePortDataType.UINT, precision, true );
				return varName;
			}
		}

		public string GetVertexId()
		{
			var precision = PrecisionType.Float;
			bool useMasterNodeCategory = true;
			MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment;

			m_currentDataCollector.AddToDirectives( "#define VERTEXID_SEMANTIC SV_VertexID" );

			WirePortDataType type = WirePortDataType.UINT;
			if( HasInfo( TemplateInfoOnSematics.VERTEXID, useMasterNodeCategory, customCategory ) )
			{
				InterpDataHelper info = GetInfo( TemplateInfoOnSematics.VERTEXID, useMasterNodeCategory, customCategory );
				return TemplateHelperFunctions.AutoSwizzleData( info.VarName, info.VarType, type, true );
			}
			else
			{
				MasterNodePortCategory portCategory = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
				string name = TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_VertexID ];
				return RegisterInfoOnSemantic( portCategory, TemplateInfoOnSematics.VERTEXID, TemplateSemantics.VERTEXID_SEMANTIC, name, WirePortDataType.UINT, precision, true );
			}
		}

		public string GetPrimitiveId()
		{
			if ( m_fragmentInputParams != null && m_fragmentInputParams.ContainsKey( TemplateSemantics.SV_PrimitiveID ) )
				return m_fragmentInputParams[ TemplateSemantics.SV_PrimitiveID ].Name;

			string name = TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_PrimitiveID ];
			RegisterFragInputParams( WirePortDataType.UINT, PrecisionType.Half, name, TemplateSemantics.SV_PrimitiveID );
			return m_fragmentInputParams[ TemplateSemantics.SV_PrimitiveID ].Name;
		}

		public string GetURPMainLight( int uniqueId, string shadowCoords = null )
		{
			if( string.IsNullOrEmpty( shadowCoords ) )
			{
				shadowCoords = GetShadowCoords( uniqueId );
			}
			m_currentDataCollector.AddLocalVariable( uniqueId , string.Format( "Light ase_mainLight = GetMainLight( {0} );",shadowCoords) );

			return "ase_mainLight";
		}

		public string GetVFace( int uniqueId )
		{
			if( IsSRP )
			{
				string result = string.Empty;
				if( GetCustomInterpolatedData( TemplateInfoOnSematics.VFACE, WirePortDataType.FLOAT, PrecisionType.Float, ref result, true, MasterNodePortCategory.Fragment ) )
				{
					m_currentDataCollector.AddToDirectives( "#if !defined(ASE_NEED_CULLFACE)" );
					m_currentDataCollector.AddToDirectives( "#define ASE_NEED_CULLFACE 1" );
					m_currentDataCollector.AddToDirectives( "#endif //ASE_NEED_CULLFACE" );
					return result;
				}
				else
				{
					if( m_fragmentInputParams != null && m_fragmentInputParams.ContainsKey( TemplateSemantics.SV_IsFrontFace ) )
						return m_fragmentInputParams[ TemplateSemantics.SV_IsFrontFace ].Name;

					string custom = "uint "+ TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_IsFrontFace ] + " : SV_IsFrontFace";
					RegisterFragInputParams( WirePortDataType.FLOAT, PrecisionType.Half, TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_IsFrontFace ], TemplateSemantics.SV_IsFrontFace, custom );
					return m_fragmentInputParams[ TemplateSemantics.SV_IsFrontFace ].Name;
				}
			}
			else
			{
				//if( m_fragmentInputParams != null && m_fragmentInputParams.ContainsKey( TemplateSemantics.VFACE ) )
				//	return m_fragmentInputParams[ TemplateSemantics.VFACE ].Name;

				//RegisterFragInputParams( WirePortDataType.FLOAT, PrecisionType.Half, TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.VFACE ], TemplateSemantics.VFACE );
				//return m_fragmentInputParams[ TemplateSemantics.VFACE ].Name;
				if( m_fragmentInputParams != null && m_fragmentInputParams.ContainsKey( TemplateSemantics.SV_IsFrontFace ) )
					return m_fragmentInputParams[ TemplateSemantics.SV_IsFrontFace ].Name;

				string custom = "uint " + TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_IsFrontFace ] + " : SV_IsFrontFace";
				RegisterFragInputParams( WirePortDataType.FLOAT , PrecisionType.Half , TemplateHelperFunctions.SemanticsDefaultName[ TemplateSemantics.SV_IsFrontFace ] , TemplateSemantics.SV_IsFrontFace , custom );
				return m_fragmentInputParams[ TemplateSemantics.SV_IsFrontFace ].Name;

			}
		}

		public string GetShadowCoords( int uniqueId, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			// overriding precision
			var precision = PrecisionType.Float;

			string worldPos = GetWorldPos( false, m_currentDataCollector.PortCategory );

			string result = string.Empty;
			if( GetCustomInterpolatedData( TemplateInfoOnSematics.SHADOWCOORDS, WirePortDataType.FLOAT4, precision, ref result, useMasterNodeCategory, customCategory ) )
			{
				return result;
			}

			string varName = GeneratorUtils.ShadowCoordsStr;
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;


			string shadowCoordsValue = string.Format( "TransformWorldToShadowCoord( {0} )", worldPos );
			if( m_currentDataCollector.PortCategory == MasterNodePortCategory.Fragment )
			{
				worldPos = GetWorldPos( false, MasterNodePortCategory.Vertex );
				m_currentDataCollector.AddLocalVariable( uniqueId, "#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) //la" );
				RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT4, precision, string.Format( "TransformWorldToShadowCoord( {0} )", worldPos ), false, MasterNodePortCategory.Fragment );
				m_currentDataCollector.AddLocalVariable( uniqueId, "#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS) //la" );
				m_currentDataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, varName, shadowCoordsValue );
				m_currentDataCollector.AddLocalVariable( uniqueId, "#else //la" );
				m_currentDataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, varName, "0" );
				m_currentDataCollector.AddLocalVariable( uniqueId, "#endif //la" );
			} else
			{
				m_currentDataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT4, varName, shadowCoordsValue );
			}
			return varName;
		}

		public bool HasUV( int uvChannel )
		{
			return ( m_currentDataCollector.PortCategory == MasterNodePortCategory.Fragment ) ? m_availableFragData.ContainsKey( TemplateHelperFunctions.IntToUVChannelInfo[ uvChannel ] ) : m_availableVertData.ContainsKey( TemplateHelperFunctions.IntToUVChannelInfo[ uvChannel ] );
		}

		public string GetUVName( int uvChannel, WirePortDataType dataType = WirePortDataType.FLOAT2 )
		{
			InterpDataHelper info = ( m_currentDataCollector.PortCategory == MasterNodePortCategory.Fragment ) ? m_availableFragData[ TemplateHelperFunctions.IntToUVChannelInfo[ uvChannel ] ] : m_availableVertData[ TemplateHelperFunctions.IntToUVChannelInfo[ uvChannel ] ];
			if( dataType != info.VarType )
				return info.VarName + UIUtils.GetAutoSwizzle( dataType );
			else
				return info.VarName;
		}

		public string GetTextureCoord( int uvChannel, string propertyName, int uniqueId, PrecisionType precisionType )
		{
			bool isVertex = ( m_currentDataCollector.PortCategory == MasterNodePortCategory.Vertex || m_currentDataCollector.PortCategory == MasterNodePortCategory.Tessellation );
			string uvChannelName = string.Empty;
			string propertyHelperVar = propertyName + "_ST";
			m_currentDataCollector.AddToUniforms( uniqueId, "float4", propertyHelperVar, IsSRP );
			string uvName = string.Empty;
			string result = string.Empty;
			if( GetCustomInterpolatedData( TemplateHelperFunctions.IntToUVChannelInfo[ uvChannel ], WirePortDataType.FLOAT2, PrecisionType.Float, ref result, false, m_currentDataCollector.PortCategory ) )
			{
				uvName = result;
			}
			else
			if( m_currentDataCollector.TemplateDataCollectorInstance.HasUV( uvChannel ) )
			{
				uvName = m_currentDataCollector.TemplateDataCollectorInstance.GetUVName( uvChannel );
			}
			else
			{
				uvName = m_currentDataCollector.TemplateDataCollectorInstance.RegisterUV( uvChannel );
			}

			uvChannelName = "uv" + propertyName;
			if( isVertex )
			{
				string value = string.Format( Constants.TilingOffsetFormat, uvName, propertyHelperVar + ".xy", propertyHelperVar + ".zw" );
				string lodLevel = "0";

				value = "float4( " + value + ", 0 , " + lodLevel + " )";
				m_currentDataCollector.AddLocalVariable( uniqueId, precisionType, WirePortDataType.FLOAT4, uvChannelName, value );
			}
			else
			{
				m_currentDataCollector.AddLocalVariable( uniqueId, precisionType, WirePortDataType.FLOAT2, uvChannelName, string.Format( Constants.TilingOffsetFormat, uvName, propertyHelperVar + ".xy", propertyHelperVar + ".zw" ) );
			}
			return uvChannelName;
		}

		public string GenerateAutoUVs( int uvChannel, WirePortDataType size = WirePortDataType.FLOAT2 )
		{
			string uvName = string.Empty;
			string result = string.Empty;
			if( GetCustomInterpolatedData( TemplateHelperFunctions.IntToUVChannelInfo[ uvChannel ], size, PrecisionType.Float, ref result, false, m_currentDataCollector.PortCategory ) )
			{
				uvName = result;
			}
			else
			if( HasUV( uvChannel ) )
			{
				uvName = GetUVName( uvChannel, size );
			}
			else
			{
				uvName = RegisterUV( uvChannel, size );
			}
			return uvName;
		}

		public string GetUV( int uvChannel, MasterNodePortCategory category = MasterNodePortCategory.Fragment, WirePortDataType size = WirePortDataType.FLOAT4 )
		{
			if( !HasUV( uvChannel ) )
			{
				RegisterUV( uvChannel, size );
			}

			InterpDataHelper info = ( category == MasterNodePortCategory.Fragment ) ? m_availableFragData[ TemplateHelperFunctions.IntToUVChannelInfo[ uvChannel ] ] : m_availableVertData[ TemplateHelperFunctions.IntToUVChannelInfo[ uvChannel ] ];
			return info.VarName;
		}

		public InterpDataHelper GetUVInfo( int uvChannel )
		{
			return ( m_currentDataCollector.PortCategory == MasterNodePortCategory.Fragment ) ? m_availableFragData[ TemplateHelperFunctions.IntToUVChannelInfo[ uvChannel ] ] : m_availableVertData[ TemplateHelperFunctions.IntToUVChannelInfo[ uvChannel ] ];
		}

		public string RegisterUV( int UVChannel, WirePortDataType size = WirePortDataType.FLOAT2 )
		{
			int channelsSize = TemplateHelperFunctions.DataTypeChannelUsage[ size ];
			WirePortDataType originalSize = size;
			int[] usage = GetUVUsage( m_currentDataCollector.PortCategory );
			if( usage[ UVChannel ] > channelsSize )
			{
				size = TemplateHelperFunctions.ChannelToDataType[ usage[ UVChannel ] ];
			}

			if ( TemplateHelperFunctions.InfoToDefine.TryGetValue( TemplateHelperFunctions.IntToUVChannelInfo[ UVChannel ], out string defineValue ) )
			{
				m_currentDataCollector.AddToDefines( -1, defineValue );
			}

			if ( m_currentDataCollector.PortCategory == MasterNodePortCategory.Vertex )
			{
				TemplateSemantics semantic = TemplateHelperFunctions.IntToSemantic[ UVChannel ];

				if( m_vertexDataDict.ContainsKey( semantic ) )
				{
					return m_vertexDataDict[ semantic ].VarName;
				}

				string varName = TemplateHelperFunctions.BaseInterpolatorName + ( ( UVChannel > 0 ) ? UVChannel.ToString() : string.Empty );
				m_availableVertData.Add( TemplateHelperFunctions.IntToUVChannelInfo[ UVChannel ],
				new InterpDataHelper( WirePortDataType.FLOAT4,
				string.Format( TemplateHelperFunctions.TemplateVarFormat,
				m_currentTemplateData.VertexFunctionData.InVarName,
				 varName ) ) );

				m_currentDataCollector.AddToVertexInput(
				string.Format( TemplateHelperFunctions.TexFullSemantic,
				varName,
				semantic ) );
				RegisterOnVertexData( semantic, size, varName );
				string finalVarName = m_availableVertData[ TemplateHelperFunctions.IntToUVChannelInfo[ UVChannel ] ].VarName;
				switch( size )
				{
					case WirePortDataType.FLOAT:
					case WirePortDataType.INT:
					case WirePortDataType.UINT:
					finalVarName += ".x";
					break;
					case WirePortDataType.FLOAT2:
					finalVarName += ".xy";
					break;
					case WirePortDataType.FLOAT3:
					finalVarName += ".xyz";
					break;
					case WirePortDataType.UINT4:
					case WirePortDataType.FLOAT4:
					case WirePortDataType.COLOR:
					case WirePortDataType.FLOAT2x2:
					case WirePortDataType.FLOAT3x3:
					case WirePortDataType.FLOAT4x4:
					case WirePortDataType.SAMPLER1D:
					case WirePortDataType.SAMPLER2D:
					case WirePortDataType.SAMPLER3D:
					case WirePortDataType.SAMPLERCUBE:
					case WirePortDataType.SAMPLER2DARRAY:
					case WirePortDataType.SAMPLERSTATE:
					case WirePortDataType.OBJECT:
					default:
					break;
				}
				return finalVarName;
			}
			else
			{
				//search if the correct vertex data is set ...
				TemplateInfoOnSematics info = TemplateHelperFunctions.IntToInfo[ UVChannel ];
				TemplateSemantics vertexSemantics = TemplateSemantics.NONE;
				foreach( KeyValuePair<TemplateSemantics, TemplateVertexData> kvp in m_vertexDataDict )
				{
					if( kvp.Value.DataInfo == info )
					{
						vertexSemantics = kvp.Key;
						break;
					}
				}

				// if not, add vertex data and create interpolator
				if( vertexSemantics == TemplateSemantics.NONE )
				{
					vertexSemantics = TemplateHelperFunctions.IntToSemantic[ UVChannel ];

					if( !m_vertexDataDict.ContainsKey( vertexSemantics ) )
					{
						string varName = TemplateHelperFunctions.BaseInterpolatorName + ( ( UVChannel > 0 ) ? UVChannel.ToString() : string.Empty );
						m_availableVertData.Add( TemplateHelperFunctions.IntToUVChannelInfo[ UVChannel ],
						new InterpDataHelper( WirePortDataType.FLOAT4,
						string.Format( TemplateHelperFunctions.TemplateVarFormat,
						m_currentTemplateData.VertexFunctionData.InVarName,
						 varName ) ) );

						m_currentDataCollector.AddToVertexInput(
						string.Format( TemplateHelperFunctions.TexFullSemantic,
						varName,
						vertexSemantics ) );
						RegisterOnVertexData( vertexSemantics, size, varName );
					}
				}

				// either way create interpolator
				TemplateVertexData availableInterp = RequestNewInterpolator( size, false );
				if( availableInterp != null )
				{
					bool isPosition = vertexSemantics == TemplateSemantics.POSITION || vertexSemantics == TemplateSemantics.POSITION;

					string interpVarName = m_currentTemplateData.VertexFunctionData.OutVarName + "." + availableInterp.VarNameWithSwizzle;
					InterpDataHelper vertInfo = m_availableVertData[ TemplateHelperFunctions.IntToUVChannelInfo[ UVChannel ] ];
					string interpDecl = string.Format( TemplateHelperFunctions.TemplateVariableDecl, interpVarName, TemplateHelperFunctions.AutoSwizzleData( vertInfo.VarName, vertInfo.VarType, size , isPosition ) );
					m_currentDataCollector.AddToVertexInterpolatorsDecl( interpDecl );
					string finalVarName = m_currentTemplateData.FragmentFunctionData.InVarName + "." + availableInterp.VarNameWithSwizzle;
					m_availableFragData.Add( TemplateHelperFunctions.IntToUVChannelInfo[ UVChannel ], new InterpDataHelper( size, finalVarName ) );
					if( size != originalSize )
					{
						//finalVarName = m_currentTemplateData.FragmentFunctionData.InVarName + "." + availableInterp.VarName + UIUtils.GetAutoSwizzle( originalSize );
						finalVarName = m_availableFragData[ TemplateHelperFunctions.IntToUVChannelInfo[ UVChannel ] ].VarName  + UIUtils.GetAutoSwizzle( originalSize );
					}
					return finalVarName;
				}
			}
			return string.Empty;
		}
		////////////////////////////////////////////////////////////////////////////////////////////////
		bool IsSemanticUsedOnInterpolator( TemplateSemantics semantics )
		{
			for( int i = 0; i < m_interpolatorData.Interpolators.Count; i++ )
			{
				if( m_interpolatorData.Interpolators[ i ].Semantics == semantics )
				{
					return true;
				}
			}
			return false;
		}

		public bool HasInfo( TemplateInfoOnSematics info, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			MasterNodePortCategory category = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
			return ( category == MasterNodePortCategory.Fragment ) ? m_availableFragData.ContainsKey( info ) : m_availableVertData.ContainsKey( info );
		}

		public InterpDataHelper GetInfo( TemplateInfoOnSematics info, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			MasterNodePortCategory category = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
			if( category == MasterNodePortCategory.Fragment )
			{
				if( !m_availableFragData[ info ].SetAtCompileTime )
				{
					string defineValue = string.Empty;
					if( TemplateHelperFunctions.InfoToDefineFrag.TryGetValue( info, out defineValue ) )
						m_currentDataCollector.AddToDefines( -1, defineValue );
				}
				return m_availableFragData[ info ];
			}
			else
			{
				if( !m_availableVertData[ info ].SetAtCompileTime )
				{
					string defineValue = string.Empty;
					if( TemplateHelperFunctions.InfoToDefineVertex.TryGetValue( info, out defineValue ) )
						m_currentDataCollector.AddToDefines( -1, defineValue );
				}
				return m_availableVertData[ info ];
			}
		}

		public string RegisterInfoOnSemantic( TemplateInfoOnSematics info, TemplateSemantics semantic, string name, WirePortDataType dataType, PrecisionType precisionType, bool requestNewInterpolator, string dataName = null )
		{
			return RegisterInfoOnSemantic( m_currentDataCollector.PortCategory, info, semantic, name, dataType, precisionType, requestNewInterpolator, dataName );
		}
		// This should only be used to semantics outside the text coord set
		public string RegisterInfoOnSemantic( MasterNodePortCategory portCategory, TemplateInfoOnSematics info, TemplateSemantics semantic, string name, WirePortDataType dataType, PrecisionType precisionType, bool requestNewInterpolator, string dataName = null )
		{
			if( portCategory == MasterNodePortCategory.Vertex )
			{
				if( m_vertexDataDict.ContainsKey( semantic ) )
				{
					return m_vertexDataDict[ semantic ].VarName;
				}

				m_availableVertData.Add( info,
					new InterpDataHelper( dataType,
						string.Format( TemplateHelperFunctions.TemplateVarFormat,
						m_currentTemplateData.VertexFunctionData.InVarName,
						name ),true,true ) );

				string vertInputVarType = UIUtils.PrecisionWirePortToCgType( precisionType, dataType );

				m_currentDataCollector.AddToVertexInput(
					string.Format( TemplateHelperFunctions.InterpFullSemantic,
						vertInputVarType,
						name,
						semantic ) );

				RegisterOnVertexData( semantic, dataType, name );
				return m_availableVertData[ info ].VarName;
			}
			else
			{
				//search if the correct vertex data is set ...
				TemplateSemantics vertexSemantics = TemplateSemantics.NONE;
				foreach( KeyValuePair<TemplateSemantics, TemplateVertexData> kvp in m_vertexDataDict )
				{
					if( kvp.Value.DataInfo == info )
					{
						vertexSemantics = kvp.Key;
						break;
					}
				}

				// if not, add vertex data and create interpolator
				if( vertexSemantics == TemplateSemantics.NONE )
				{
					vertexSemantics = semantic;

					if( !m_vertexDataDict.ContainsKey( vertexSemantics ) )
					{
						m_availableVertData.Add( info,
						new InterpDataHelper( dataType,
						string.Format( TemplateHelperFunctions.TemplateVarFormat,
						m_currentTemplateData.VertexFunctionData.InVarName,
						name ),true,true ) );

						string vertInputVarType = UIUtils.PrecisionWirePortToCgType( precisionType, dataType );
						m_currentDataCollector.AddToVertexInput(
						string.Format( TemplateHelperFunctions.InterpFullSemantic,
						vertInputVarType,
						name,
						vertexSemantics ) );
						RegisterOnVertexData( vertexSemantics, dataType, name );
					}
				}

				// either way create interpolator

				TemplateVertexData availableInterp = null;
				if( requestNewInterpolator || IsSemanticUsedOnInterpolator( semantic ) )
				{
					availableInterp = RequestNewInterpolator( dataType, false, dataName );
				}
				else
				{
					availableInterp = RegisterOnInterpolator( semantic, dataType, dataName );
				}

				if( availableInterp != null )
				{
					bool isPosition = vertexSemantics == TemplateSemantics.POSITION || vertexSemantics == TemplateSemantics.POSITION;

					string interpVarName = m_currentTemplateData.VertexFunctionData.OutVarName + "." + availableInterp.VarNameWithSwizzle;
					string interpDecl = string.Format( TemplateHelperFunctions.TemplateVariableDecl, interpVarName, TemplateHelperFunctions.AutoSwizzleData( m_availableVertData[ info ].VarName, m_availableVertData[ info ].VarType, dataType, isPosition ) );
					m_currentDataCollector.AddToVertexInterpolatorsDecl( interpDecl );
					string finalVarName = m_currentTemplateData.FragmentFunctionData.InVarName + "." + availableInterp.VarNameWithSwizzle;
					m_availableFragData.Add( info, new InterpDataHelper( dataType, finalVarName ) );
					return finalVarName;
				}
			}
			return string.Empty;
		}

		TemplateVertexData RegisterOnInterpolator( TemplateSemantics semantics, WirePortDataType dataType, string vertexDataName = null )
		{
			if( vertexDataName == null )
			{
				if( TemplateHelperFunctions.SemanticsDefaultName.ContainsKey( semantics ) )
				{
					vertexDataName = TemplateHelperFunctions.SemanticsDefaultName[ semantics ];
				}
				else
				{
					vertexDataName = string.Empty;
					Debug.LogError( "No valid name given to vertex data" );
				}
			}

			TemplateVertexData data = new TemplateVertexData( semantics, dataType, vertexDataName );
			m_interpolatorData.Interpolators.Add( data );
			string interpolator = string.Format( TemplateHelperFunctions.InterpFullSemantic, UIUtils.WirePortToCgType( dataType ), data.VarName, data.Semantics );
			m_currentDataCollector.AddToInterpolators( interpolator );
			return data;
		}

		public void RegisterOnVertexData( TemplateSemantics semantics, WirePortDataType dataType, string varName )
		{
			m_vertexDataDict.Add( semantics, new TemplateVertexData( semantics, dataType, varName ) );
		}

		public TemplateVertexData RequestMacroInterpolator( string varName )
		{
			if( varName != null && m_registeredVertexData.ContainsKey( varName ) )
			{
				return m_registeredVertexData[ varName ];
			}

			for( int i = 0; i < m_interpolatorData.AvailableInterpolators.Count; i++ )
			{
				if( !m_interpolatorData.AvailableInterpolators[ i ].IsFull )
				{
					TemplateVertexData data = m_interpolatorData.AvailableInterpolators[ i ].RequestChannels( WirePortDataType.FLOAT4, false, varName );
					if( data != null )
					{
						if( !m_registeredVertexData.ContainsKey( data.VarName ) )
						{
							m_registeredVertexData.Add( data.VarName, data );
						}
						if( m_interpolatorData.AvailableInterpolators[ i ].Usage == 1 )
						{
							string interpolator = string.Format( TemplateHelperFunctions.InterpMacro, varName, TemplateHelperFunctions.SemanticToInt[ data.Semantics ] );
							m_currentDataCollector.AddToInterpolators( interpolator );
						}
						return data;
					}
				}
			}
			return null;
		}

		public bool HasRawInterpolatorOfName( string name )
		{
			return m_interpolatorData.HasRawInterpolatorOfName( name );
		}

		public TemplateVertexData RequestNewInterpolator( WirePortDataType dataType, bool isColor, string varName = null , bool noInterpolationFlag = false, bool sampleFlag = false, InterpolatorCondition condition = null )
		{
			if( varName != null && m_registeredVertexData.ContainsKey( varName ) )
			{
				return m_registeredVertexData[ varName ];
			}

			// @diogo: packing ignores the condition on purpose, gating must never cost a slot; a slot is only gated
			// when everything first fit happens to put in it is (#443)
			for( int i = 0; i < m_interpolatorData.AvailableInterpolators.Count; i++ )
			{
				if( !m_interpolatorData.AvailableInterpolators[ i ].IsFull	)
				{
					if( m_interpolatorData.AvailableInterpolators[ i ].Usage != 0 &&
						(m_interpolatorData.AvailableInterpolators[ i ].NoInterpolation != noInterpolationFlag ||
						m_interpolatorData.AvailableInterpolators[ i ].Sample != sampleFlag ))
						continue;

					TemplateVertexData data = m_interpolatorData.AvailableInterpolators[ i ].RequestChannels( dataType, isColor, varName );
					if( data != null )
					{
						if( !m_registeredVertexData.ContainsKey( data.VarName ) )
						{
							m_registeredVertexData.Add( data.VarName, data );
						}

						if( m_interpolatorData.AvailableInterpolators[ i ].Usage == 1 )
						{
							m_interpolatorData.AvailableInterpolators[ i ].NoInterpolation = noInterpolationFlag;
							m_interpolatorData.AvailableInterpolators[ i ].Sample = sampleFlag;
							m_interpolatorData.AvailableInterpolators[ i ].Condition = condition;
							// First time using this interpolator, so we need to register it
							string interpolator = string.Format( TemplateHelperFunctions.TexFullSemantic,
																	data.VarName, data.Semantics );
							if( noInterpolationFlag )
								interpolator = "nointerpolation " + interpolator;

							if( sampleFlag)
								interpolator = "sample " + interpolator;

							if ( condition == null )
							{
								m_currentDataCollector.AddToInterpolators( interpolator );
							}
							else
							{
								m_interpolatorData.AvailableInterpolators[ i ].DeferredDeclaration = interpolator;
							}
						}
						else
						{
							m_interpolatorData.AvailableInterpolators[ i ].Condition = InterpolatorCondition.Or( m_interpolatorData.AvailableInterpolators[ i ].Condition, condition );
						}
						return data;
					}
				}
			}

			// This area is reached if max available interpolators from shader model is reached
			// Nevertheless, we register all new interpolators to that list so no imediate compilation errors are thrown
			// A warning message is then thrown to warn the user about this
			int newInterpId = 1 + TemplateHelperFunctions.SemanticToInt[ m_interpolatorData.AvailableInterpolators[ m_interpolatorData.AvailableInterpolators.Count - 1 ].Semantic ];
			if( TemplateHelperFunctions.IntToSemantic.ContainsKey( newInterpId ) )
			{
				TemplateInterpElement item = new TemplateInterpElement( TemplateHelperFunctions.IntToSemantic[ newInterpId ] );
				m_interpolatorData.AvailableInterpolators.Add( item );
				TemplateVertexData data = item.RequestChannels( dataType, isColor, varName );
				if( data != null )
				{
					if( !m_registeredVertexData.ContainsKey( data.VarName ) )
					{
						m_registeredVertexData.Add( data.VarName, data );
					}

					if( item.Usage == 1 )
					{
						string interpolator = string.Format( TemplateHelperFunctions.TexFullSemantic, data.VarName, data.Semantics );
						m_currentDataCollector.AddToInterpolators( interpolator );
					}
					return data;
				}
			}

			UIUtils.ShowMessage( "Maximum amount of interpolators exceeded", MessageSeverity.Error );
			return null;
		}

		// Unused channels in interpolators must be set to something so the compiler doesn't generate warnings
		public List<string> GetInterpUnusedChannels()
		{
			List<string> resetInstrucctions = new List<string>();

			if( m_interpolatorData != null )
			{
				for( int i = 0; i < m_interpolatorData.AvailableInterpolators.Count; i++ )
				{
					if( m_interpolatorData.AvailableInterpolators[ i ].Usage > 0 && !m_interpolatorData.AvailableInterpolators[ i ].IsFull )
					{
						string channels = string.Empty;
						bool[] availableChannels = m_interpolatorData.AvailableInterpolators[ i ].AvailableChannels;
						for( int j = 0; j < availableChannels.Length; j++ )
						{
							if( availableChannels[ j ] )
							{
								channels += TemplateHelperFunctions.VectorSwizzle[ j ];
							}
						}

						InterpolatorCondition condition = m_interpolatorData.AvailableInterpolators[ i ].Condition;
						if ( condition != null )
						{
							resetInstrucctions.Add( "#if " + condition.Expression );
						}
						resetInstrucctions.Add( string.Format( "{0}.{1}.{2} = 0;", m_currentTemplateData.VertexFunctionData.OutVarName, m_interpolatorData.AvailableInterpolators[ i ].Name, channels ) );
						if ( condition != null )
						{
							resetInstrucctions.Add( "#endif" );
						}
					}
				}
			}

			if( resetInstrucctions.Count > 0 )
			{
				resetInstrucctions.Insert( 0, "\n//setting value to unused interpolator channels and avoid initialization warnings" );
			}

			return resetInstrucctions;
		}

		// Emits the struct fields and vertex writes held back for keyword-gated interpolators now that their
		// conditions are final; returns the writes, to go after every other vertex instruction
		public List<string> FlushConditionalInterpolators()
		{
			List<string> writes = new List<string>();

			if ( m_interpolatorData != null )
			{
				for ( int i = 0; i < m_interpolatorData.AvailableInterpolators.Count; i++ )
				{
					TemplateInterpElement element = m_interpolatorData.AvailableInterpolators[ i ];
					if ( element.DeferredDeclaration != null )
					{
						// @diogo: one entry, the interpolator list drops duplicate lines and every slot's #endif is alike
						m_currentDataCollector.AddToInterpolators( ( element.Condition == null ) ? element.DeferredDeclaration : "#if " + element.Condition.Expression + "\n" + element.DeferredDeclaration + "\n#endif" );
						element.DeferredDeclaration = null;
					}
				}
			}

			if ( m_customInterpolatedData != null )
			{
				foreach ( KeyValuePair<string, TemplateCustomData> pair in m_customInterpolatedData )
				{
					TemplateCustomData customData = pair.Value;
					if ( customData.DeferredWrite != null )
					{
						string write = customData.DeferredWrite + " = " + pair.Key + ";";
						if ( customData.Condition == null )
						{
							writes.Add( write );
						}
						else
						{
							writes.Add( "#if " + customData.Condition.Expression );
							writes.Add( write );
							// @diogo: variants that declare the slot without taking this value still need its channels set
							InterpolatorCondition slotCondition = customData.InterpElement.Condition;
							if ( !InterpolatorCondition.AreEqual( slotCondition, customData.Condition ) )
							{
								writes.Add( ( slotCondition == null ) ? "#else" : "#elif " + slotCondition.Expression );
								writes.Add( customData.DeferredWrite + " = 0;" );
							}
							writes.Add( "#endif" );
						}
						customData.DeferredWrite = null;
					}
				}
			}

			return writes;
		}

		public bool ContainsSpecialLocalFragVar( TemplateInfoOnSematics info, WirePortDataType type, ref string result )
		{
			if( m_specialFragmentLocalVars.ContainsKey( info ) )
			{
				result = m_specialFragmentLocalVars[ info ].LocalVarName;
				if( m_specialFragmentLocalVars[ info ].DataType != type )
				{
					result = TemplateHelperFunctions.AutoSwizzleData( result, m_specialFragmentLocalVars[ info ].DataType, type, false );
				}
				return true;
			}
			return false;
		}

		public bool GetCustomInterpolatedData( TemplateInfoOnSematics info, WirePortDataType type, PrecisionType precisionType, ref string result, bool useMasterNodeCategory, MasterNodePortCategory customCategory )
		{
			bool isPosition =	info == TemplateInfoOnSematics.POSITION ||
								info == TemplateInfoOnSematics.CLIP_POS ||
								info == TemplateInfoOnSematics.SCREEN_POSITION ||
								info == TemplateInfoOnSematics.SCREEN_POSITION_NORMALIZED ||
								info == TemplateInfoOnSematics.WORLD_POSITION ||
								info == TemplateInfoOnSematics.RELATIVE_WORLD_POS ||
								info == TemplateInfoOnSematics.OBJECT_POSITION ||
								info == TemplateInfoOnSematics.VIEW_POSITION;

			MasterNodePortCategory category = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
			if( category == MasterNodePortCategory.Vertex )
			{

				if ( m_availableVertData.ContainsKey( info ) && !m_availableVertData[ info ].SetAtCompileTime )
				{
					string defineValue;
					if ( TemplateHelperFunctions.InfoToDefine.TryGetValue( info, out defineValue ) )
					{
						m_currentDataCollector.AddToDefines( -1, defineValue );
					}
					if ( TemplateHelperFunctions.InfoToDefineVertex.TryGetValue( info, out defineValue ) )
					{
						m_currentDataCollector.AddToDefines( -1, defineValue );
					}
				}

				if ( m_specialVertexLocalVars.ContainsKey( info ) )
				{
					result = m_specialVertexLocalVars[ info ].LocalVarName;
					if( m_specialVertexLocalVars[ info ].DataType != type )
					{
						result = TemplateHelperFunctions.AutoSwizzleData( result, m_specialVertexLocalVars[ info ].DataType, type , isPosition );
					}

					string defineValue = string.Empty;
					if( TemplateHelperFunctions.InfoToDefineVertex.TryGetValue( info, out defineValue ) )
						m_currentDataCollector.AddToDefines( -1, defineValue );

					return true;
				}
			}

			if( category == MasterNodePortCategory.Fragment )
			{
				if ( m_availableFragData.ContainsKey( info ) && !m_availableFragData[ info ].SetAtCompileTime )
				{
					string defineValue;
					if ( TemplateHelperFunctions.InfoToDefine.TryGetValue( info, out defineValue ) )
					{
						m_currentDataCollector.AddToDefines( -1, defineValue );
					}
					if ( TemplateHelperFunctions.InfoToDefineFrag.TryGetValue( info, out defineValue ) )
					{
						m_currentDataCollector.AddToDefines( -1, defineValue );
					}
				}

				if ( m_specialFragmentLocalVars.ContainsKey( info ) )
				{
					result = m_specialFragmentLocalVars[ info ].LocalVarName;
					if( m_specialFragmentLocalVars[ info ].DataType != type )
					{
						result = TemplateHelperFunctions.AutoSwizzleData( result, m_specialFragmentLocalVars[ info ].DataType, type, isPosition );
					}

					string defineValue = string.Empty;
					if( TemplateHelperFunctions.InfoToDefineFrag.TryGetValue( info, out defineValue ))
						m_currentDataCollector.AddToDefines( -1, defineValue );
					return true;
				}

				if( m_availableFragData.ContainsKey( info ) )
				{
					if( m_availableFragData[ info ].IsSingleComponent )
					{
						result = m_availableFragData[ info ].VarName;
						if( m_availableFragData[ info ].VarType != type )
						{
							result = TemplateHelperFunctions.AutoSwizzleData( result, m_availableFragData[ info ].VarType, type, isPosition );
						}
						return true;
					}
					else if( TemplateHelperFunctions.InfoToLocalVar.ContainsKey( info ) && TemplateHelperFunctions.InfoToWirePortType.ContainsKey( info ) )
					{
						result = TemplateHelperFunctions.InfoToLocalVar[ info ];
						m_currentDataCollector.AddLocalVariable( -1, precisionType, TemplateHelperFunctions.InfoToWirePortType[ info ], result, m_availableFragData[ info ].VarName );
						return true;
					}
				}
			}
			return false;
		}

		public WirePortDataType GetVertexPositionDataType()
		{
			InterpDataHelper info = GetInfo( TemplateInfoOnSematics.POSITION , false, MasterNodePortCategory.Vertex);
			return info.VarType;
		}

		public string GetVertexPosition( WirePortDataType type, PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			PrecisionType precision = PrecisionType.Float;

			string result = string.Empty;
			if ( GetCustomInterpolatedData( TemplateInfoOnSematics.OBJECT_POSITION, WirePortDataType.FLOAT3, precision, ref result, useMasterNodeCategory, customCategory ) )
			{
				return result;
			}

			if ( HasInfo( TemplateInfoOnSematics.POSITION, useMasterNodeCategory, customCategory ) )
			{
				InterpDataHelper info = GetInfo( TemplateInfoOnSematics.POSITION, useMasterNodeCategory, customCategory );
				if( type != WirePortDataType.OBJECT && type != info.VarType )
					return TemplateHelperFunctions.AutoSwizzleData( info.VarName, info.VarType, type,true );
				else
					return info.VarName;
			}
			else
			{
				MasterNodePortCategory portCategory = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
				string name = "ase_vertex_pos";
				string varName = RegisterInfoOnSemantic( portCategory, TemplateInfoOnSematics.POSITION, TemplateSemantics.POSITION, name, WirePortDataType.FLOAT4, precisionType, true );
				if( type != WirePortDataType.OBJECT && type != WirePortDataType.FLOAT4 )
					return TemplateHelperFunctions.AutoSwizzleData( varName, WirePortDataType.FLOAT4, type,true );
				else
					return varName;
			}
		}

		private const string InstancingLibStandard = "UnityInstancing.cginc";
		private const string InstancingLibSRP = "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl";

		public void SetupInstancing()
		{
			if( !HasInfo( TemplateInfoOnSematics.INSTANCE_ID ) )
			{
				m_currentDataCollector.AddToPragmas( -1, IOUtils.InstancedPropertiesHeader );
				m_currentDataCollector.AddToIncludes( -1, IsSRP ? InstancingLibSRP : InstancingLibStandard );
				m_currentDataCollector.AddToVertexInput( Constants.InstanceIdMacro );
				m_currentDataCollector.AddToInterpolators( Constants.InstanceIdMacro );
				m_currentDataCollector.AddToLocalVariables( MasterNodePortCategory.Vertex, -1, string.Format( "UNITY_SETUP_INSTANCE_ID( {0} );", m_currentTemplateData.VertexFunctionData.InVarName ) );
				m_currentDataCollector.AddToLocalVariables( MasterNodePortCategory.Vertex, -1, string.Format( "UNITY_TRANSFER_INSTANCE_ID( {0}, {1} );", m_currentTemplateData.VertexFunctionData.InVarName, m_currentTemplateData.VertexFunctionData.OutVarName ) );
				m_currentDataCollector.AddToLocalVariables( MasterNodePortCategory.Fragment, -1, string.Format( "UNITY_SETUP_INSTANCE_ID( {0} );", m_currentTemplateData.FragmentFunctionData.InVarName ) );
			}
		}

		public string GetVertexColor( PrecisionType precisionType )
		{
			if( HasInfo( TemplateInfoOnSematics.COLOR ) )
			{
				return GetInfo( TemplateInfoOnSematics.COLOR ).VarName;
			}
			else
			{
				string name = "ase_color";
				return RegisterInfoOnSemantic( TemplateInfoOnSematics.COLOR, TemplateSemantics.COLOR, name, WirePortDataType.FLOAT4, precisionType, false );
			}
		}

		public string GetVertexNormal( PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			if( HasInfo( TemplateInfoOnSematics.NORMAL, useMasterNodeCategory, customCategory ) )
			{
				InterpDataHelper info = GetInfo( TemplateInfoOnSematics.NORMAL, useMasterNodeCategory, customCategory );
				return TemplateHelperFunctions.AutoSwizzleData( info.VarName, info.VarType, WirePortDataType.FLOAT3 , false);
			}
			else
			{
				MasterNodePortCategory category = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
				string name = "ase_normal";
				return RegisterInfoOnSemantic( category, TemplateInfoOnSematics.NORMAL, TemplateSemantics.NORMAL, name, WirePortDataType.FLOAT3, precisionType, false );
			}
		}

		public string GetWorldNormal( PrecisionType precisionType = PrecisionType.Float, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment, bool normalize = false )
		{
			string result = string.Empty;
			if( GetCustomInterpolatedData( TemplateInfoOnSematics.WORLD_NORMAL, WirePortDataType.FLOAT3, precisionType, ref result, useMasterNodeCategory, customCategory ) )
			{
				if( normalize )
					return string.Format( "normalize( {0} )", result );
				else
					return result;
			}

			string varName = normalize ? "normalizeWorldNormal" : GeneratorUtils.WorldNormalStr;

			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			string worldNormalValue = string.Empty;

			if( !GetCustomInterpolatedData( TemplateInfoOnSematics.WORLD_NORMAL, WirePortDataType.FLOAT3, precisionType, ref worldNormalValue, false, MasterNodePortCategory.Vertex ) )
			{
				string vertexNormal = GetVertexNormal( precisionType, false, MasterNodePortCategory.Vertex );
				string formatStr = string.Empty;
				if( IsSRP )
					formatStr = "TransformObjectToWorldNormal( {0} )";
				else
					formatStr = "UnityObjectToWorldNormal( {0} )";
				worldNormalValue = string.Format( formatStr, vertexNormal );
			}

			if( normalize )
				worldNormalValue = string.Format( "normalize( {0} )", worldNormalValue );

			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT3, precisionType, worldNormalValue, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetWorldNormal( int uniqueId, PrecisionType precisionType, string normal, string outputId, ViewSpace normalSpace = ViewSpace.Tangent )
		{
			if ( normalSpace == ViewSpace.Tangent )
			{
				string tanToWorld0 = string.Empty;
				string tanToWorld1 = string.Empty;
				string tanToWorld2 = string.Empty;

				GetWorldTangentTf( precisionType, out tanToWorld0, out tanToWorld1, out tanToWorld2, true );

				string tanNormal = "tanNormal" + outputId;
				m_currentDataCollector.AddLocalVariable( uniqueId, "float3 " + tanNormal + " = " + normal + ";" );
				return string.Format( "float3( dot( {1}, {0} ), dot( {2}, {0} ), dot( {3}, {0} ) )", tanNormal, tanToWorld0, tanToWorld1, tanToWorld2 );
			}
			else if ( normalSpace == ViewSpace.World )
			{
				return normal;
			}
			else if ( normalSpace == ViewSpace.Object )
			{
				string formatStr = IsSRP ? "TransformObjectToWorldNormal( {0} )" : "UnityObjectToWorldNormal( {0} )";
				return string.Format( formatStr, normal );
			}
			else if ( normalSpace == ViewSpace.View )
			{
				string formatStr = IsSRP ? "TransformViewToWorldNormal( {0} )" : "mul( ( float3x3 )UNITY_MATRIX_I_V, {0} )";
				return string.Format( formatStr, normal );
			}
			else
			{
				// unsupported
				return "float3( 0, 0, 0 )";
			}
		}

		public string GetVertexTangent( WirePortDataType type, PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			if( HasInfo( TemplateInfoOnSematics.TANGENT, useMasterNodeCategory, customCategory ) )
			{
				InterpDataHelper info = GetInfo( TemplateInfoOnSematics.TANGENT, useMasterNodeCategory, customCategory );
				if( type != WirePortDataType.OBJECT && type != info.VarType )
					return TemplateHelperFunctions.AutoSwizzleData( info.VarName, info.VarType, type , false);
				else
					return info.VarName;
			}
			else
			{
				MasterNodePortCategory category = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
				string name = "ase_tangent";
				string varName = RegisterInfoOnSemantic( category, TemplateInfoOnSematics.TANGENT, TemplateSemantics.TANGENT, name, WirePortDataType.FLOAT4, precisionType, false );
				if( type != WirePortDataType.OBJECT && type != WirePortDataType.FLOAT4 )
					return TemplateHelperFunctions.AutoSwizzleData( varName, WirePortDataType.FLOAT4, type , false );
				else
					return varName;
			}
		}
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public string GetBlendWeights( bool useMasterNodeCategory = true , MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			if( HasInfo( TemplateInfoOnSematics.BLENDWEIGHTS , useMasterNodeCategory , customCategory ) )
			{
				InterpDataHelper info = GetInfo( TemplateInfoOnSematics.BLENDWEIGHTS , useMasterNodeCategory , customCategory );
				return info.VarName;
			}
			else
			{
				MasterNodePortCategory category = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
				string name = GeneratorUtils.VertexBlendWeightsStr;
				string varName = RegisterInfoOnSemantic( category , TemplateInfoOnSematics.BLENDWEIGHTS , TemplateSemantics.BLENDWEIGHTS , name , WirePortDataType.FLOAT4 ,PrecisionType.Float , false , name );
				return varName;
			}
		}

		public string GetBlendIndices( bool useMasterNodeCategory = true , MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			if( HasInfo( TemplateInfoOnSematics.BLENDINDICES , useMasterNodeCategory , customCategory ) )
			{
				InterpDataHelper info = GetInfo( TemplateInfoOnSematics.BLENDINDICES , useMasterNodeCategory , customCategory );
				return info.VarName;
			}
			else
			{
				MasterNodePortCategory category = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
				string name = GeneratorUtils.VertexBlendIndicesStr;
				string varName = RegisterInfoOnSemantic( category , TemplateInfoOnSematics.BLENDINDICES , TemplateSemantics.BLENDINDICES , name , WirePortDataType.UINT4 , PrecisionType.Float , false , name );
				return varName;
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public string GetVertexBitangent( PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string varName = GeneratorUtils.VertexBitangentStr;
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			string tangentValue = GetVertexTangent( WirePortDataType.FLOAT4, precisionType, false, MasterNodePortCategory.Vertex );
			string normalValue = GetVertexNormal( precisionType, false, MasterNodePortCategory.Vertex );

			string bitangentValue = string.Format( "cross( {0}, {1}.xyz ) * {1}.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 )", normalValue, tangentValue );
			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT3, precisionType, bitangentValue, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetWorldTangent( PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string result = string.Empty;
			if( GetCustomInterpolatedData( TemplateInfoOnSematics.WORLD_TANGENT, WirePortDataType.FLOAT3, precisionType, ref result, useMasterNodeCategory, customCategory ) )
			{
				return result;
			}

			string varName = GeneratorUtils.WorldTangentStr;
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			string worldTangentValue = string.Empty;
			if( !GetCustomInterpolatedData( TemplateInfoOnSematics.WORLD_TANGENT, WirePortDataType.FLOAT3, precisionType, ref worldTangentValue, false, MasterNodePortCategory.Vertex ) )
			{
				string vertexTangent = GetVertexTangent( WirePortDataType.FLOAT4, precisionType, false, MasterNodePortCategory.Vertex );
				string formatStr = string.Empty;

				if( IsSRP )
					formatStr = "TransformObjectToWorldDir( {0}.xyz )";
				else
					formatStr = "UnityObjectToWorldDir( {0} )";

				worldTangentValue = string.Format( formatStr, vertexTangent );
			}
			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT3, precisionType, worldTangentValue, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetTangentSign( PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string varName = GeneratorUtils.VertexTangentSignStr;
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			string tangentValue = GetVertexTangent( WirePortDataType.FLOAT4, precisionType, false, MasterNodePortCategory.Vertex );
			string tangentSignValue = string.Format( "{0}.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 )", tangentValue );
			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT, precisionType, tangentSignValue, useMasterNodeCategory, customCategory );
			return varName;
		}


		public string GetWorldBitangent( PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string result = string.Empty;
			if( GetCustomInterpolatedData( TemplateInfoOnSematics.WORLD_BITANGENT, WirePortDataType.FLOAT3, precisionType, ref result, useMasterNodeCategory, customCategory ) )
			{
				return result;
			}

			string varName = GeneratorUtils.WorldBitangentStr;
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			string worldBinormal = string.Empty;
			if( !GetCustomInterpolatedData( TemplateInfoOnSematics.WORLD_BITANGENT, WirePortDataType.FLOAT3, precisionType, ref worldBinormal, false, MasterNodePortCategory.Vertex ) )
			{
				string worldNormal = GetWorldNormal( precisionType, false, MasterNodePortCategory.Vertex );
				string worldtangent = GetWorldTangent( precisionType, false, MasterNodePortCategory.Vertex );
				string tangentSign = GetTangentSign( precisionType, false, MasterNodePortCategory.Vertex );
				worldBinormal = string.Format( "cross( {0}, {1} ) * {2}", worldNormal, worldtangent, tangentSign );
			}

			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT3, PrecisionType.Float, worldBinormal, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetWorldReflection( PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment, bool normalize = false )
		{
			string varName = GeneratorUtils.WorldReflectionStr;//UIUtils.GetInputValueFromType( SurfaceInputs.WORLD_REFL );
			if( normalize )
				varName = "normalized" + varName;

			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			string worldNormal = GetWorldNormal( precisionType );
			string worldViewDir = GetViewDir();
			string worldRefl = string.Format( "reflect( -{0}, {1} )", worldViewDir, worldNormal );

			if( normalize )
				worldRefl = string.Format( "normalize( {0} )", worldRefl );

			m_currentDataCollector.AddLocalVariable( -1, precisionType, WirePortDataType.FLOAT3, varName, worldRefl );
			return varName;
		}

		public string GetWorldReflection( PrecisionType precisionType, string normal )
		{
			string tanToWorld0 = string.Empty;
			string tanToWorld1 = string.Empty;
			string tanToWorld2 = string.Empty;

			GetWorldTangentTf( precisionType, out tanToWorld0, out tanToWorld1, out tanToWorld2 );
			string worldRefl = GetViewDir();

			return string.Format( "reflect( -{0}, float3( dot( {2}, {1} ), dot( {3}, {1} ), dot( {4}, {1} ) ) )", worldRefl, normal, tanToWorld0, tanToWorld1, tanToWorld2 );
		}

		public string GetLightAtten( int uniqueId, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			//string result = string.Empty;
			//if( GetCustomInterpolatedData( TemplateInfoOnSematics.WORLD_POSITION, PrecisionType.Float, ref result, useMasterNodeCategory, customCategory ) )
			//{
			//	return result;
			//}

			//string varName = GeneratorUtils.WorldPositionStr;//UIUtils.GetInputValueFromType( SurfaceInputs.WORLD_POS );
			//if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
			//	return varName;

			//if( !m_availableVertData.ContainsKey( TemplateInfoOnSematics.POSITION ) )
			//{
			//	UIUtils.ShowMessage( "Attempting to access inexisting vertex position to calculate world pos" );
			//	return "fixed3(0,0,0)";
			//}

			//string vertexPos = m_availableVertData[ TemplateInfoOnSematics.POSITION ].VarName;
			//string worldPosConversion = string.Format( "mul(unity_ObjectToWorld, {0}).xyz", vertexPos );

			//RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT3, PrecisionType.Float, worldPosConversion, useMasterNodeCategory, customCategory );
			//return varName;

			m_currentDataCollector.AddToIncludes( uniqueId, Constants.UnityAutoLightLib );
			m_currentDataCollector.AddToDefines( uniqueId, "ASE_SHADOWS" );
			RequestMacroInterpolator( "UNITY_SHADOW_COORDS" );
			m_currentDataCollector.AddToPragmas( uniqueId, "multi_compile_fwdbase" );

			//string vOutName = CurrentTemplateData.VertexFunctionData.OutVarName;
			string fInName = CurrentTemplateData.FragmentFunctionData.InVarName;
			string worldPos = GetWorldPos();
			m_currentDataCollector.AddLocalVariable( uniqueId, "UNITY_LIGHT_ATTENUATION( ase_lightAtten, " + fInName + ", " + worldPos + " )" );
			return "ase_lightAtten";

		}

		public string GenerateRotationIndependentObjectScale( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			string worldToObjectMatrix = ( m_currentSRPType == TemplateSRPType.BiRP ) ? "unity_WorldToObject" : "GetWorldToObjectMatrix()";
			string value = string.Format( "( 1.0 / float3( length( {0}[ 0 ].xyz ), length( {0}[ 1 ].xyz ), length( {0}[ 2 ].xyz ) ) )", worldToObjectMatrix );
			dataCollector.AddLocalVariable( uniqueId, PrecisionType.Float, WirePortDataType.FLOAT3, GeneratorUtils.ParentObjectScaleStr, value );
			return GeneratorUtils.ParentObjectScaleStr;
		}

		public string GenerateObjectScale( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			string worldToObjectMatrix = ( m_currentSRPType == TemplateSRPType.BiRP ) ? "unity_ObjectToWorld" : "GetObjectToWorldMatrix()";
			string value = string.Format( "float3( length( {0}[ 0 ].xyz ), length( {0}[ 1 ].xyz ), length( {0}[ 2 ].xyz ) )", worldToObjectMatrix );
			dataCollector.AddLocalVariable( uniqueId, PrecisionType.Float, WirePortDataType.FLOAT3, GeneratorUtils.ObjectScaleStr, value );
			return GeneratorUtils.ObjectScaleStr;
		}

		public string GenerateObjectPosition( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			string value = string.Empty;
			if ( m_currentSRPType != TemplateSRPType.BiRP )
			{
				value = "GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 )";
			}
			else
			{
				value = "UNITY_MATRIX_M._m03_m13_m23";
			}
			dataCollector.AddLocalVariable( uniqueId, PrecisionType.Float, WirePortDataType.FLOAT3, GeneratorUtils.ObjectPositionStr, value );
			return GeneratorUtils.ObjectPositionStr;
		}

		public string GenerateObjectBoundsMin( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			string value = string.Empty;
			if ( m_currentSRPType != TemplateSRPType.BiRP && ( ASEPackageManagerHelper.CurrentHDRPBaseline >= SRPBaseline.ASE_SRP_14_X ||
				ASEPackageManagerHelper.CurrentURPBaseline >= SRPBaseline.ASE_SRP_14_X  ) )
			{
				value = "unity_RendererBounds_Min.xyz";
			}
			else
			{
				value = "0"; // not supported
			}
			dataCollector.AddLocalVariable( uniqueId, PrecisionType.Float, WirePortDataType.FLOAT3, GeneratorUtils.ObjectBoundsMinStr, value );
			return GeneratorUtils.ObjectBoundsMinStr;
		}

		public string GenerateObjectBoundsMax( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			string value = string.Empty;
			if ( m_currentSRPType != TemplateSRPType.BiRP && ( ASEPackageManagerHelper.CurrentHDRPBaseline >= SRPBaseline.ASE_SRP_14_X ||
				ASEPackageManagerHelper.CurrentURPBaseline >= SRPBaseline.ASE_SRP_14_X ) )
			{
				value = "unity_RendererBounds_Max.xyz";
			}
			else
			{
				value = "0"; // not supported
			}
			dataCollector.AddLocalVariable( uniqueId, PrecisionType.Float, WirePortDataType.FLOAT3, GeneratorUtils.ObjectBoundsMaxStr, value );
			return GeneratorUtils.ObjectBoundsMaxStr;
		}

		public string GenerateObjectBoundsSize( ref MasterNodeDataCollector dataCollector, int uniqueId )
		{
			string value = string.Empty;
			if ( m_currentSRPType != TemplateSRPType.BiRP && ( ASEPackageManagerHelper.CurrentHDRPBaseline >= SRPBaseline.ASE_SRP_14_X ||
				ASEPackageManagerHelper.CurrentURPBaseline >= SRPBaseline.ASE_SRP_14_X ) )
			{
				value = "( unity_RendererBounds_Max.xyz - unity_RendererBounds_Min.xyz )";
			}
			else
			{
				value = "0"; // not supported
			}
			dataCollector.AddLocalVariable( uniqueId, PrecisionType.Float, WirePortDataType.FLOAT3, GeneratorUtils.ObjectBoundsSizeStr, value );
			return GeneratorUtils.ObjectBoundsSizeStr;
		}

		public string GetPosition( PositionNode.Space space, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			// overriding precision
			var precision = PrecisionType.Float;

			string result;
			if ( space == PositionNode.Space.Object )
			{
				result = GetVertexPosition( WirePortDataType.FLOAT3, precision, useMasterNodeCategory, customCategory );
			}
			else if ( space == PositionNode.Space.RelativeWorld )
			{

				result = GetRelativeWorldPos( useMasterNodeCategory, customCategory );
			}
			else if ( space == PositionNode.Space.View )
			{
				result = GetViewPos( useMasterNodeCategory, customCategory );
			}
			else // PositionNode.Space.World
			{
				result = GetWorldPos( useMasterNodeCategory, customCategory );
			}
			return result;
		}

		public string GetWorldPos( bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			// overriding precision
			var precision = PrecisionType.Float;

			string result = string.Empty;
			if( GetCustomInterpolatedData( TemplateInfoOnSematics.WORLD_POSITION, WirePortDataType.FLOAT3, precision, ref result, useMasterNodeCategory, customCategory ) )
			{
				return result;
			}
			else if( m_currentSRPType == TemplateSRPType.HDRP )
			{
				if( GetCustomInterpolatedData( TemplateInfoOnSematics.RELATIVE_WORLD_POS, WirePortDataType.FLOAT3, precision, ref result, useMasterNodeCategory, customCategory ) )
				{
					string worldPosVarName = GeneratorUtils.WorldPositionStr;
					string relWorldPosConversion = string.Format( "GetAbsolutePositionWS( {0} )", result );
					m_currentDataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT3, worldPosVarName, relWorldPosConversion );
					return worldPosVarName;
				}
			}

			string varName = GeneratorUtils.WorldPositionStr;//UIUtils.GetInputValueFromType( SurfaceInputs.WORLD_POS );
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			if( !m_availableVertData.ContainsKey( TemplateInfoOnSematics.POSITION ) )
			{
				UIUtils.ShowMessage( "Attempting to access inexisting vertex position to calculate world pos" );
				return "half3(0,0,0)";
			}

			string vertexPos = m_availableVertData[ TemplateInfoOnSematics.POSITION ].VarName;

			string worldPosConversion = string.Empty;

			//Check if world pos already defined in the vertex body
			if( !GetCustomInterpolatedData( TemplateInfoOnSematics.WORLD_POSITION, WirePortDataType.FLOAT3, precision, ref worldPosConversion, false, MasterNodePortCategory.Vertex ) )
			{
				if( m_currentSRPType == TemplateSRPType.HDRP )
				{
					worldPosConversion = string.Format( "GetAbsolutePositionWS( TransformObjectToWorld( ( {0} ).xyz ) )", vertexPos );
				}
				else if( m_currentSRPType == TemplateSRPType.URP )
				{
					worldPosConversion = string.Format( "TransformObjectToWorld( ( {0} ).xyz )", vertexPos );
				}
				else
				{
					worldPosConversion = string.Format( "mul( unity_ObjectToWorld, float4( ( {0} ).xyz, 1 ) ).xyz", vertexPos );
				}
			}
			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT3, precision, worldPosConversion, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetRelativeWorldPos( bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			PrecisionType precision = PrecisionType.Float;

			string result = string.Empty;
			if ( GetCustomInterpolatedData( TemplateInfoOnSematics.RELATIVE_WORLD_POS, WirePortDataType.FLOAT3, precision, ref result, useMasterNodeCategory, customCategory ) )
			{
				return result;
			}

			if ( !m_currentDataCollector.HasLocalVariableByName( GeneratorUtils.RelativeWorldPositionStr ) )
			{
				string positionOS = GetVertexPosition( WirePortDataType.FLOAT4, precision, false, MasterNodePortCategory.Vertex );
				string positionCS = GetClipPos( false, MasterNodePortCategory.Vertex );
				string invProjMatrix = GeneratorUtils.GenerateInverseProjection( ref m_currentDataCollector, -1, precision, false, MasterNodePortCategory.Vertex );

				if ( !m_currentDataCollector.HasLocalVariableByName( GeneratorUtils.ViewHPositionStr ) )
				{
					m_currentDataCollector.AddToVertexLocalVariables( -1, precision, WirePortDataType.FLOAT4, GeneratorUtils.ViewHPositionStr,
						string.Format( "mul( {0}, {1} )", invProjMatrix, GeneratorUtils.ClipPositionStr ) );
				}

				m_currentDataCollector.AddToVertexLocalVariables( -1, precision, WirePortDataType.FLOAT3, GeneratorUtils.RelativeWorldPositionStr,
					string.Format( "mul( ( float3x3 )UNITY_MATRIX_I_V, {0}.xyz / {0}.w )", GeneratorUtils.ViewHPositionStr ) );

				RegisterCustomInterpolatedData( GeneratorUtils.RelativeWorldPositionStr, WirePortDataType.FLOAT3, precision, GeneratorUtils.RelativeWorldPositionStr, useMasterNodeCategory, customCategory );
			}
			return GeneratorUtils.RelativeWorldPositionStr;
		}

		public string GetViewPos( bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			PrecisionType precision = PrecisionType.Float;

			string result = string.Empty;
			if ( GetCustomInterpolatedData( TemplateInfoOnSematics.VIEW_POSITION, WirePortDataType.FLOAT3, precision, ref result, useMasterNodeCategory, customCategory ) )
			{
				return result;
			}

			MasterNodePortCategory portCategory = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;

			if ( !m_currentDataCollector.HasLocalVariableByName( GeneratorUtils.ViewPositionStr ) )
			{
				if ( portCategory == MasterNodePortCategory.Vertex || portCategory == MasterNodePortCategory.Tessellation )
				{
					string positionOS = GetVertexPosition( WirePortDataType.FLOAT3, precision, useMasterNodeCategory, customCategory );
					result = string.Format( IsSRP ? "TransformWorldToView( TransformObjectToWorld( {0} ) )" : "UnityObjectToViewPos( {0} )", positionOS );
				}
				else
				{
					string positionWS = GetWorldPos( useMasterNodeCategory, customCategory );
					result = string.Format( IsSRP ? "TransformWorldToView( {0} )" : "UnityWorldToViewPos( {0} )", positionWS );
				}

				m_currentDataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT3, GeneratorUtils.ViewPositionStr, result );
			}
			return GeneratorUtils.ViewPositionStr;
		}

		public string GetClipPosForValue( string customVertexPos, string outputId, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string varName = GeneratorUtils.ClipPositionStr + outputId;
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			if( !m_availableVertData.ContainsKey( TemplateInfoOnSematics.POSITION ) )
			{
				UIUtils.ShowMessage( "Attempting to access inexisting vertex position to calculate clip pos" );
				return "half4(0,0,0,0)";
			}

			string formatStr = string.Empty;
			switch( m_currentSRPType )
			{
				default:
				case TemplateSRPType.BiRP:
				formatStr = "UnityObjectToClipPos( {0} )";
				break;
				case TemplateSRPType.HDRP:
				formatStr = "TransformWorldToHClip( TransformObjectToWorld( ( {0} ).xyz ) )";
				break;
				case TemplateSRPType.URP:
				formatStr = "TransformObjectToHClip( ( {0} ).xyz )";
				break;
			}

			string clipSpaceConversion = string.Format( formatStr, customVertexPos );
			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT4, PrecisionType.Float, clipSpaceConversion, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetClipPos( bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string result = string.Empty;
			if ( GetCustomInterpolatedData( TemplateInfoOnSematics.CLIP_POS, WirePortDataType.FLOAT4, PrecisionType.Float, ref result, useMasterNodeCategory, customCategory ) )
			{
				return result;
			}

			string varName = GeneratorUtils.ClipPositionStr;
			if ( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			if ( !m_availableVertData.ContainsKey( TemplateInfoOnSematics.POSITION ) )
			{
				UIUtils.ShowMessage( "Attempting to access inexisting vertex position to calculate clip pos" );
				return "half4(0,0,0,0)";
			}

			string vertexPos = m_availableVertData[ TemplateInfoOnSematics.POSITION ].VarName;

			string formatStr = string.Empty;
			switch( m_currentSRPType )
			{
				default:
				case TemplateSRPType.BiRP:
				formatStr = "UnityObjectToClipPos( {0} )";
				break;
				case TemplateSRPType.HDRP:
				formatStr = "TransformWorldToHClip( TransformObjectToWorld( ( {0} ).xyz ) )";
				break;
				case TemplateSRPType.URP:
				formatStr = "TransformObjectToHClip( ( {0} ).xyz )";
				break;
			}

			string clipSpaceConversion = string.Format( formatStr, vertexPos );
			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT4, PrecisionType.Float, clipSpaceConversion, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetScreenPosForValue( PrecisionType precision, string customVertexPos, string outputId, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			// overriding precision
			precision = PrecisionType.Float;

			string varName = UIUtils.GetInputValueFromType( SurfaceInputs.SCREEN_POS ) + outputId;
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			string clipSpacePos = GetClipPosForValue( customVertexPos, outputId, false, MasterNodePortCategory.Vertex );
			string screenPosConversion = string.Empty;
			if( m_currentSRPType == TemplateSRPType.HDRP )
			{
				screenPosConversion = string.Format( "ComputeScreenPos( {0}, _ProjectionParams.x )", clipSpacePos );
			}
			else
			{
				screenPosConversion = string.Format( "ComputeScreenPos( {0} )", clipSpacePos );
			}
			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT4, precision, screenPosConversion, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetScreenPosRaw( PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			// overriding precision
			precision = PrecisionType.Float;

			string result = string.Empty;
			if ( GetCustomInterpolatedData( TemplateInfoOnSematics.SCREEN_POSITION, WirePortDataType.FLOAT4, precision, ref result, useMasterNodeCategory, customCategory ) )
			{
				return result;
			}

			string varName = UIUtils.GetInputValueFromType( SurfaceInputs.SCREEN_POS );
			if ( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
			{
				return varName;
			}

			string clipSpacePos = GetClipPos( false, MasterNodePortCategory.Vertex );
			string screenPosConversion = string.Empty;
			if ( m_currentSRPType == TemplateSRPType.HDRP )
			{
				screenPosConversion = string.Format( "ComputeScreenPos( {0}, _ProjectionParams.x )", clipSpacePos );
			}
			else
			{
				screenPosConversion = string.Format( "ComputeScreenPos( {0} )", clipSpacePos );
			}
			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT4, precision, screenPosConversion, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetScreenPosNormalized( PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string result = string.Empty;
			if( GetCustomInterpolatedData( TemplateInfoOnSematics.SCREEN_POSITION_NORMALIZED, WirePortDataType.FLOAT4, precision, ref result, useMasterNodeCategory, customCategory ) )
			{
				return result;
			}

			if ( !m_currentDataCollector.HasLocalVariableByName( GeneratorUtils.ScreenPosNormStr ) )
			{
				string screenPos = GetScreenPosRaw( precision, useMasterNodeCategory, customCategory );
				string clipPlaneTestOp = string.Format( "{0}.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? {0}.z : {0}.z * 0.5 + 0.5;", GeneratorUtils.ScreenPosNormStr );
				m_currentDataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosNormStr, string.Format( GeneratorUtils.NormalizedScreenPosFormat, screenPos ) );
				m_currentDataCollector.AddLocalVariable( -1, clipPlaneTestOp );
			}
			return GeneratorUtils.ScreenPosNormStr;
		}

		public string GetScreenPosCenter( PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string screenPosNorm = GetScreenPosNormalized( precision, useMasterNodeCategory, customCategory );
			if ( !m_currentDataCollector.HasLocalVariableByName( GeneratorUtils.ScreenPosCenterStr ) )
			{
				string value = string.Format( "float4( {0}.xy * 2 - 1, 0, 0 )", screenPosNorm );
				m_currentDataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosCenterStr, value );
			}
			return GeneratorUtils.ScreenPosCenterStr;
		}

		public string GetScreenPosTiled( PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			if ( !m_currentDataCollector.HasLocalVariableByName( GeneratorUtils.ScreenPosTiledStr ) )
			{
				string screenPosNorm = GetScreenPosNormalized( precision, useMasterNodeCategory, customCategory );
				string screenParams = m_currentDataCollector.IsURP ? "_ScaledScreenParams" : "_ScreenParams";
				string value = string.Format( "frac( float4( ( {0}.x * 2 - 1 ) * {1}.x / {1}.y, {0}.y * 2 - 1, 0, 0 ) )", screenPosNorm, screenParams );
				m_currentDataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosTiledStr, value );
			}
			return GeneratorUtils.ScreenPosTiledStr;
		}

		public string GetScreenPosPixel( PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			if ( !m_currentDataCollector.HasLocalVariableByName( GeneratorUtils.ScreenPosPixelStr ) )
			{
				string screenPosNorm = GetScreenPosNormalized( precision, useMasterNodeCategory, customCategory );
				string screenParams = m_currentDataCollector.IsURP ? "_ScaledScreenParams" : "_ScreenParams";
				GeneratorUtils.GenerateScreenPosNormToPixelFunction( ref m_currentDataCollector );
				m_currentDataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT4, GeneratorUtils.ScreenPosPixelStr, GeneratorUtils.GenerateScreenPosNormToPixelFunctionCall( screenPosNorm, screenParams ) );
			}
			return GeneratorUtils.ScreenPosPixelStr;
		}

		public string GetSurfaceDepth( DepthMode depthMode, PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string screenPos = GetScreenPosRaw( precision, useMasterNodeCategory, customCategory );
			string instruction = string.Format( "{0}.z / {0}.w", screenPos );
			string varName;

			switch ( depthMode )
			{
				case DepthMode.DepthLinearEye:
				{
					instruction = GeneratorUtils.ApplyLinearDepthModifier( ref m_currentDataCollector, instruction, depthMode );
					varName = GeneratorUtils.DepthLinearEyeStr;
					break;
				}
				case DepthMode.DepthLinear01:
				{
					instruction = GeneratorUtils.ApplyLinearDepthModifier( ref m_currentDataCollector, instruction, depthMode );
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

			m_currentDataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT, varName, instruction );
			return varName;
		}

		public string GetViewVector( PrecisionType precisionType = PrecisionType.Float, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment, ViewSpace space = ViewSpace.World )
		{
			string varName;
			switch ( space )
			{
				case ViewSpace.Tangent: varName = GeneratorUtils.TangentViewVectorStr; break;
				case ViewSpace.Object: varName = GeneratorUtils.ObjectViewVectorStr; break;
				case ViewSpace.View: varName = GeneratorUtils.ViewViewVectorStr; break;
				case ViewSpace.World:
				default: varName = GeneratorUtils.WorldViewVectorStr; break;
			}
			if ( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
			{
				return varName;
			}

			string worldPos = GetWorldPos();
			string viewVectorWS = string.Format( "( ( unity_OrthoParams.w == 0 ) ? _WorldSpaceCameraPos - {0} : UNITY_MATRIX_V[ 2 ].xyz )", worldPos );

			string viewVector;
			if ( space == ViewSpace.Tangent )
			{
				string tanToWorld0, tanToWorld1, tanToWorld2;
				GetWorldTangentTf( precisionType, out tanToWorld0, out tanToWorld1, out tanToWorld2 );

				viewVector = string.Format( " {0} * {3}.x + {1} * {3}.y  + {2} * {3}.z", tanToWorld0, tanToWorld1, tanToWorld2, viewVectorWS );
			}
			else if ( space == ViewSpace.Object )
			{
				string worldToObjectMatrix = ( m_currentSRPType == TemplateSRPType.BiRP ) ? "unity_WorldToObject" : "GetWorldToObjectMatrix()";
				viewVector = string.Format( "mul( ( float3x3 ){0}, {1} )", worldToObjectMatrix, viewVectorWS );

			}
			else if ( space == ViewSpace.View )
			{
				viewVector = string.Format( "mul( ( float3x3 )UNITY_MATRIX_V, {0} )", viewVectorWS );
			}
			else
			{
				viewVector = viewVectorWS;
			}

			m_currentDataCollector.AddLocalVariable( -1, precisionType, WirePortDataType.FLOAT3, varName, viewVector );

			return varName;
		}

		public string GetViewDir( PrecisionType precisionType = PrecisionType.Float, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment, NormalizeType normalizeType = NormalizeType.Regular, ViewSpace space = ViewSpace.World )
		{
			if ( space == ViewSpace.World )
			{
				string result = string.Empty;
				if ( GetCustomInterpolatedData( TemplateInfoOnSematics.WORLD_VIEW_DIR, WirePortDataType.FLOAT3, precisionType, ref result, useMasterNodeCategory, customCategory ) )
				{
					return result;
				}
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

			if ( !m_currentDataCollector.HasLocalVariableByName( varName ) )
			{
				string viewVector = GetViewVector( precisionType, useMasterNodeCategory, customCategory, space );

				if ( normalizeType == NormalizeType.Regular )
				{
					viewVector = "normalize( " + viewVector + " )";
				}
				else if ( normalizeType == NormalizeType.Safe )
				{
					viewVector = TemplateHelperFunctions.SafeNormalize( m_currentDataCollector, viewVector );
				}

				m_currentDataCollector.AddLocalVariable( -1, precisionType, WirePortDataType.FLOAT3, varName, viewVector );
			}
			return varName;
		}

		public void GetWorldTangentTf( PrecisionType precisionType, out string tanToWorld0, out string tanToWorld1, out string tanToWorld2, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			tanToWorld0 = "tanToWorld0";
			tanToWorld1 = "tanToWorld1";
			tanToWorld2 = "tanToWorld2";

			if( HasCustomInterpolatedData( tanToWorld0, useMasterNodeCategory, customCategory ) ||
				 HasCustomInterpolatedData( tanToWorld1, useMasterNodeCategory, customCategory ) ||
				 HasCustomInterpolatedData( tanToWorld2, useMasterNodeCategory, customCategory ) )
				return;

			string worldTangent = GetWorldTangent( precisionType, useMasterNodeCategory, customCategory );
			string worldNormal = GetWorldNormal( precisionType, useMasterNodeCategory, customCategory );
			string worldBinormal = GetWorldBitangent( precisionType, useMasterNodeCategory, customCategory );

			string tanToWorldVar0 = string.Format( "float3( {0}.x, {1}.x, {2}.x )", worldTangent, worldBinormal, worldNormal );
			string tanToWorldVar1 = string.Format( "float3( {0}.y, {1}.y, {2}.y )", worldTangent, worldBinormal, worldNormal );
			string tanToWorldVar2 = string.Format( "float3( {0}.z, {1}.z, {2}.z )", worldTangent, worldBinormal, worldNormal );

			if( customCategory == MasterNodePortCategory.Vertex )
			{
				RegisterCustomInterpolatedData( tanToWorld0, WirePortDataType.FLOAT3, precisionType, tanToWorldVar0, useMasterNodeCategory, customCategory );
				RegisterCustomInterpolatedData( tanToWorld1, WirePortDataType.FLOAT3, precisionType, tanToWorldVar1, useMasterNodeCategory, customCategory );
				RegisterCustomInterpolatedData( tanToWorld2, WirePortDataType.FLOAT3, precisionType, tanToWorldVar2, useMasterNodeCategory, customCategory );
			}
			else
			{
				m_currentDataCollector.AddLocalVariable( -1, precisionType, WirePortDataType.FLOAT3, tanToWorld0, tanToWorldVar0 );
				m_currentDataCollector.AddLocalVariable( -1, precisionType, WirePortDataType.FLOAT3, tanToWorld1, tanToWorldVar1 );
				m_currentDataCollector.AddLocalVariable( -1, precisionType, WirePortDataType.FLOAT3, tanToWorld2, tanToWorldVar2 );
			}
		}

		public string GetTangentToWorldMatrixFast( PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string worldTangent = GetWorldTangent( precisionType );
			string worldNormal = GetWorldNormal( precisionType );
			string worldBinormal = GetWorldBitangent( precisionType );

			string varName = GeneratorUtils.TangentToWorldFastStr;
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			string result = string.Format( "float3x3( {0}.x, {1}.x, {2}.x, {0}.y, {1}.y, {2}.y, {0}.z, {1}.z, {2}.z )", worldTangent, worldBinormal, worldNormal );
			m_currentDataCollector.AddLocalVariable( -1, precisionType, WirePortDataType.FLOAT3x3, GeneratorUtils.TangentToWorldFastStr, result );
			return GeneratorUtils.TangentToWorldFastStr;
		}

		public string GetTangentToWorldMatrixPrecise( PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string worldToTangent = GetWorldToTangentMatrix( precisionType, useMasterNodeCategory, customCategory );
			GeneratorUtils.Add3x3InverseFunction( ref m_currentDataCollector, UIUtils.PrecisionWirePortToCgType( precisionType, WirePortDataType.FLOAT ) );
			m_currentDataCollector.AddLocalVariable( -1, precisionType, WirePortDataType.FLOAT3x3, GeneratorUtils.TangentToWorldPreciseStr, string.Format( GeneratorUtils.Inverse3x3Header, worldToTangent ) );
			return GeneratorUtils.TangentToWorldPreciseStr;
		}

		public string GetWorldToTangentMatrix( PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			string worldTangent = GetWorldTangent( precisionType );
			string worldNormal = GetWorldNormal( precisionType );
			string worldBinormal = GetWorldBitangent( precisionType );

			string varName = GeneratorUtils.WorldToTangentStr;// "worldToTanMat";
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;
			string worldTanMat = string.Format( "float3x3( {0}, {1}, {2} )", worldTangent, worldBinormal, worldNormal );

			m_currentDataCollector.AddLocalVariable( -1, precisionType, WirePortDataType.FLOAT3x3, varName, worldTanMat );
			return varName;
		}

		public string GetObjectToViewPos( PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			// overriding precision
			precision = PrecisionType.Float;

			string varName = "objectToViewPos";
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;
			string vertexPos = GetVertexPosition( WirePortDataType.FLOAT3, precision, false, MasterNodePortCategory.Vertex );

			string formatStr = string.Empty;
			if( IsSRP )
				formatStr = "TransformWorldToView( TransformObjectToWorld( {0} ) )";
			else
				formatStr = "UnityObjectToViewPos( {0} )";

			string objectToViewPosValue = string.Format( formatStr, vertexPos );
			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT3, precision, objectToViewPosValue, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetEyeDepth( PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment, bool linear01 = false )
		{
			// overriding precision
			precision = PrecisionType.Float;

			string varName = "eyeDepth";
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;
			string objectToView = GetObjectToViewPos( precision, false, MasterNodePortCategory.Vertex );
			string eyeDepthValue = string.Format( "-{0}.z", objectToView );
			if( linear01 )
			{
				eyeDepthValue += " * _ProjectionParams.w";
			}

			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT, precision, eyeDepthValue, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetObjectSpaceLightDir( PrecisionType precisionType, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			if( !IsSRP )
			{
				m_currentDataCollector.AddToIncludes( -1, Constants.UnityLightingLib );
				m_currentDataCollector.AddToIncludes( -1, Constants.UnityAutoLightLib );
			}

			string varName = "objectSpaceLightDir";

			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			string vertexPos = GetVertexPosition( WirePortDataType.FLOAT4, precisionType, false, MasterNodePortCategory.Vertex );

			string objectSpaceLightDir = string.Empty;
			switch( m_currentSRPType )
			{
				default:
				case TemplateSRPType.BiRP:
				objectSpaceLightDir = string.Format( "ObjSpaceLightDir( {0} )", vertexPos );
				break;
				case TemplateSRPType.HDRP:
				string worldSpaceLightDir = GetWorldSpaceLightDir( precisionType, useMasterNodeCategory, customCategory );
				objectSpaceLightDir = string.Format( "mul( GetWorldToObjectMatrix(), {0} ).xyz", worldSpaceLightDir );
				break;
				case TemplateSRPType.URP:
				objectSpaceLightDir = "mul( GetWorldToObjectMatrix(), _MainLightPosition ).xyz";
				break;
			}

			RegisterCustomInterpolatedData( varName, WirePortDataType.FLOAT3, precisionType, objectSpaceLightDir, useMasterNodeCategory, customCategory );
			return varName;
		}

		public string GetWorldSpaceLightDir( PrecisionType precision, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			if( !IsSRP )
			{
				m_currentDataCollector.AddToIncludes( -1, Constants.UnityLightingLib );
				m_currentDataCollector.AddToIncludes( -1, Constants.UnityAutoLightLib );
				AddLateDirective( AdditionalLineType.Custom, "//This is a late directive" );
			}
			else
			{

				string lightVar;
				if( m_currentSRPType == TemplateSRPType.HDRP )
				{
					lightVar = "-" + string.Format( TemplateHelperFunctions.LightDataFormatHDRP, "0", "forward" );
				}
				else
				{
					lightVar = "_MainLightPosition.xyz";
				}
				return m_currentDataCollector.SafeNormalizeLightDir ? string.Format( "SafeNormalize( {0} )", lightVar ) : lightVar;
			}

			string varName = "worldSpaceLightDir";
			if( HasCustomInterpolatedData( varName, useMasterNodeCategory, customCategory ) )
				return varName;

			string worldPos = GetWorldPos( useMasterNodeCategory, customCategory );
			string worldSpaceLightDir = string.Format( "UnityWorldSpaceLightDir( {0} )", worldPos );
			if( m_currentDataCollector.SafeNormalizeLightDir )
			{
				if( IsSRP )
				{
					worldSpaceLightDir = string.Format( "SafeNormalize( {0} )", worldSpaceLightDir );
				}
				else
				{
					m_currentDataCollector.AddToIncludes( -1, Constants.UnityBRDFLib );
					worldSpaceLightDir = string.Format( "Unity_SafeNormalize( {0} )", worldSpaceLightDir );
				}
			}

			m_currentDataCollector.AddLocalVariable( -1, precision, WirePortDataType.FLOAT3, varName, worldSpaceLightDir );
			return varName;
		}

		public void RegisterCustomInterpolatedData( string name, WirePortDataType dataType, PrecisionType precision, string vertexInstruction, bool useMasterNodeCategory = true,
													MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment, bool noInterpolationFlag = false, bool sampleFlag = false )
		{
			bool addLocalVariable = !name.Equals( vertexInstruction );

			MasterNodePortCategory category = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;

			if( !m_customInterpolatedData.ContainsKey( name ) )
			{
				m_customInterpolatedData.Add( name, new TemplateCustomData( name, dataType ) );
			}

			// @diogo: the vertex local is declared again when the open vertex scope cannot see the one already emitted,
			// otherwise a regenerated arm would read a name declared inside another arm's braces (#365)
			if ( !m_customInterpolatedData[ name ].IsVertex || ( addLocalVariable && !m_currentDataCollector.HasLocalVariableByName( name, MasterNodePortCategory.Vertex ) ) )
			{
				m_customInterpolatedData[ name ].IsVertex = true;
				if( addLocalVariable )
					m_currentDataCollector.AddToVertexLocalVariables( -1, precision, dataType, name, vertexInstruction );
			}

			if( category == MasterNodePortCategory.Fragment )
			{
				TemplateCustomData customData = m_customInterpolatedData[ name ];
				// @diogo: a read sits inside the #if arms it was emitted in, so their conjunction is when it runs; an open
				// vertex scope keeps it unconditional, the deferred write would land past that scope's braces (#443)
				InterpolatorCondition condition = null;
				if ( BranchScopeUtils.Enabled && m_currentDataCollector.CurrentBranchScopeOnCategory( MasterNodePortCategory.Vertex ) == 0 )
				{
					condition = InterpolatorCondition.FromConjunction( m_currentDataCollector.GetFragmentPreprocessorConditions() );
				}
				if( !customData.IsFragment )
				{
					customData.IsFragment = true;
					customData.Condition = condition;
					TemplateVertexData interpData = RequestNewInterpolator( dataType, false, null, noInterpolationFlag, sampleFlag, condition );
					if( interpData == null )
					{
						Debug.LogErrorFormat( "Could not assign interpolator of type {0} to variable {1}", dataType, name );
						return;
					}

					customData.FragmentValue = m_currentTemplateData.FragmentFunctionData.InVarName + "." + interpData.VarNameWithSwizzle;
					customData.InterpElement = m_interpolatorData.AvailableInterpolators.Find( element => element.Semantic == interpData.Semantics );
					if ( condition == null )
					{
						m_currentDataCollector.AddToVertexLocalVariables( -1, m_currentTemplateData.VertexFunctionData.OutVarName + "." + interpData.VarNameWithSwizzle, name );
					}
					else
					{
						customData.DeferredWrite = m_currentTemplateData.VertexFunctionData.OutVarName + "." + interpData.VarNameWithSwizzle;
					}
				}
				else if ( customData.InterpElement != null )
				{
					// @diogo: the slot widens from the member's whole condition, so the two can never collapse apart
					customData.Condition = InterpolatorCondition.Or( customData.Condition, condition );
					customData.InterpElement.Condition = InterpolatorCondition.Or( customData.InterpElement.Condition, customData.Condition );
				}

				// @diogo: outside the latch so a read from a disjoint branch scope re-declares; the collector's
				// scope-aware dedup no-ops when the existing declaration is still visible (#365)
				m_currentDataCollector.AddToLocalVariables( -1, precision, dataType, name, customData.FragmentValue );
			}
		}

		public bool HasCustomInterpolatedData( string name, bool useMasterNodeCategory = true, MasterNodePortCategory customCategory = MasterNodePortCategory.Fragment )
		{
			if( m_customInterpolatedData.ContainsKey( name ) )
			{
				MasterNodePortCategory category = useMasterNodeCategory ? m_currentDataCollector.PortCategory : customCategory;
				// @diogo: a declaration emitted inside a branch scope is invisible from a disjoint one, so report
				// it as missing and let the caller re-register instead of handing back an undeclared name (#365)
				bool registered = ( category == MasterNodePortCategory.Fragment ) ? m_customInterpolatedData[ name ].IsFragment : m_customInterpolatedData[ name ].IsVertex;
				return registered && ( !m_currentDataCollector.HasLocalVariableDeclared( name, category ) || m_currentDataCollector.HasLocalVariableByName( name, category ) );
			}
			return false;
		}

		public bool HasFragmentInputParams
		{
			get
			{
				if( m_fragmentInputParams != null )
					return m_fragmentInputParams.Count > 0;

				return false;
			}
		}

		public string FragInputParamsStr
		{
			get
			{
				string value = string.Empty;
				if( m_fragmentInputParams != null && m_fragmentInputParams.Count > 0 )
				{
					int count = m_fragmentInputParams.Count;
					if( count > 0 )
					{
						value = ", ";
						foreach( KeyValuePair<TemplateSemantics, TemplateInputParameters> kvp in m_fragmentInputParams )
						{
							value += kvp.Value.Declaration;

							if( --count > 0 )
							{
								value += " , ";
							}
						}
					}
				}
				return value;
			}
		}

		public string VertexInputParamsStr
		{
			get
			{
				string value = string.Empty;
				if( m_vertexInputParams != null && m_vertexInputParams.Count > 0 )
				{
					int count = m_vertexInputParams.Count;
					if( count > 0 )
					{
						value = ", ";
						foreach( KeyValuePair<TemplateSemantics, TemplateInputParameters> kvp in m_vertexInputParams )
						{
							value += kvp.Value.Declaration;

							if( --count > 0 )
							{
								value += " , ";
							}
						}
					}
				}
				return value;
			}
		}

		public void Destroy()
		{
			m_currentTemplate = null;

			m_currentTemplateData = null;

			m_currentDataCollector = null;

			if( m_fullSrpBatcherPropertiesList != null )
			{
				m_fullSrpBatcherPropertiesList.Clear();
				m_fullSrpBatcherPropertiesList = null;
			}

			if ( m_dotsPropertiesDict != null )
			{
				m_dotsPropertiesDict.Clear();
				m_dotsPropertiesDict = null;
			}

			if ( m_dotsPropertiesList != null )
			{
				m_dotsPropertiesList.Clear();
				m_dotsPropertiesList = null;
			}

			if ( m_dotsDefinesList != null )
			{
				m_dotsDefinesList.Clear();
				m_dotsDefinesList = null;
			}

			if ( m_fullDotsPropertiesList != null )
			{
				m_fullDotsPropertiesList.Clear();
				m_fullDotsPropertiesList = null;
			}

			if( m_srpBatcherPropertiesList != null )
			{
				m_srpBatcherPropertiesList.Clear();
				m_srpBatcherPropertiesList = null;
			}

			if( m_srpBatcherPropertiesDict != null )
			{
				m_srpBatcherPropertiesDict.Clear();
				m_srpBatcherPropertiesDict = null;
			}

			if( m_lateDirectivesList != null )
			{
				m_lateDirectivesList.Clear();
				m_lateDirectivesList = null;
			}

			if( m_lateDirectivesDict != null )
			{
				m_lateDirectivesDict.Clear();
				m_lateDirectivesDict = null;
			}

			if( m_registeredVertexData != null )
			{
				m_registeredVertexData.Clear();
				m_registeredVertexData = null;
			}

			if( m_vertexInputParams != null )
			{
				m_vertexInputParams.Clear();
				m_vertexInputParams = null;
			}

			if( m_fragmentInputParams != null )
			{
				m_fragmentInputParams.Clear();
				m_fragmentInputParams = null;
			}

			if( m_vertexDataDict != null )
			{
				m_vertexDataDict.Clear();
				m_vertexDataDict = null;
			}

			if( m_interpolatorData != null )
			{
				m_interpolatorData.Destroy();
				m_interpolatorData = null;
			}

			if( m_availableFragData != null )
			{
				m_availableFragData.Clear();
				m_availableFragData = null;
			}

			if( m_availableVertData != null )
			{
				m_availableVertData.Clear();
				m_availableVertData = null;
			}

			if( m_customInterpolatedData != null )
			{
				m_customInterpolatedData.Clear();
				m_customInterpolatedData = null;
			}

			if( m_specialVertexLocalVars != null )
			{
				m_specialVertexLocalVars.Clear();
				m_specialVertexLocalVars = null;
			}

			if( m_specialFragmentLocalVars != null )
			{
				m_specialFragmentLocalVars.Clear();
				m_specialFragmentLocalVars = null;
			}
		}

		public void BuildCBuffer( int nodeId )
		{
			m_fullSrpBatcherPropertiesList.Clear();
			if( m_srpBatcherPropertiesList.Count > 0 )
			{
				var regex = new Regex( @"(\d)\s+\b" );
				m_srpBatcherPropertiesList.Sort( ( a, b ) =>
				{
					var matchA = regex.Match( a.PropertyName );
					int sizeA = 0;
					if( matchA.Groups.Count > 1 && matchA.Groups[ 1 ].Value.Length > 0 )
						sizeA = Convert.ToInt32( matchA.Groups[ 1 ].Value, System.Globalization.CultureInfo.InvariantCulture );

					var matchB = regex.Match( b.PropertyName );
					int sizeB = 0;
					if( matchB.Groups.Count > 1 && matchB.Groups[ 1 ].Value.Length > 0 )
						sizeB = Convert.ToInt32( matchB.Groups[ 1 ].Value, System.Globalization.CultureInfo.InvariantCulture );

					return sizeB.CompareTo( sizeA );
				} );

				m_fullSrpBatcherPropertiesList.Insert(0, new PropertyDataCollector( nodeId, IOUtils.SRPCBufferPropertiesBegin ));
				m_fullSrpBatcherPropertiesList.AddRange( m_srpBatcherPropertiesList );
				m_fullSrpBatcherPropertiesList.Add( new PropertyDataCollector( nodeId, IOUtils.SRPCBufferPropertiesEnd ) );
			}
		}

		public void BuildDotsProperties( int nodeId )
		{
			m_fullDotsPropertiesList.Clear();
			if ( m_dotsPropertiesList.Count > 0 )
			{
				m_fullDotsPropertiesList.Add( new PropertyDataCollector( nodeId, string.Empty ) );
				m_fullDotsPropertiesList.Add( new PropertyDataCollector( nodeId, "#ifdef UNITY_DOTS_INSTANCING_ENABLED" ) );
				m_fullDotsPropertiesList.Add( new PropertyDataCollector( nodeId, "UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)" ) );
				m_fullDotsPropertiesList.AddRange( m_dotsPropertiesList );
				m_fullDotsPropertiesList.Add( new PropertyDataCollector( nodeId, "UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)" ) );
				m_fullDotsPropertiesList.AddRange( m_dotsDefinesList );
				m_fullDotsPropertiesList.Add( new PropertyDataCollector( nodeId, "#endif" ) );
			}
		}


		public void DumpSRPBatcher()
		{
			for( int i = 0; i < m_srpBatcherPropertiesList.Count; i++ )
			{
				Debug.Log( i + "::" + m_srpBatcherPropertiesList[ i ].PropertyName );
			}
		}

		public const string GlobalMaxInterpolatorReachedMsg = "Maximum amount of interpolators reached!\nPlease consider optmizing your shader!";
		public const string MaxInterpolatorSMReachedMsg = "Maximum amount of interpolators reached for current shader model on pass {0}! Please consider increasing the shader model to {1}!";
		public void CheckInterpolatorOverflow( string currShaderModel, string passName )
		{
			int maxInterpolatorAmount = TemplateHelperFunctions.AvailableInterpolators[ currShaderModel ];
			int currInterpolatorAmount = 1 + TemplateHelperFunctions.SemanticToInt[ InterpData.AvailableInterpolators[ InterpData.AvailableInterpolators.Count - 1 ].Semantic ];
			if( currInterpolatorAmount > maxInterpolatorAmount )
			{
				string shaderModel = string.Empty;
				if( TemplateHelperFunctions.GetShaderModelForInterpolatorAmount( currInterpolatorAmount, ref shaderModel ) )
				{
					UIUtils.ShowMessage( string.Format( MaxInterpolatorSMReachedMsg, passName, shaderModel ), MessageSeverity.Error );
				}
				else
				{
					UIUtils.ShowMessage( GlobalMaxInterpolatorReachedMsg, MessageSeverity.Error );
				}
			}
		}

		public Dictionary<TemplateSemantics, TemplateInputParameters> FragInputParameters { get { return m_fragmentInputParams; } }

		public bool HasVertexInputParams
		{
			get
			{
				if( m_vertexInputParams != null )
					return m_vertexInputParams.Count > 0;

				return false;
			}
		}

		public Dictionary<TemplateSemantics, TemplateInputParameters> VertexInputParameters { get { return m_vertexInputParams; } }
		public TemplateData CurrentTemplateData { get { return m_currentTemplateData; } }
		public int MultipassSubshaderIdx { get { return m_multipassSubshaderIdx; } }
		public int MultipassPassIdx { get { return m_multipassPassIdx; } }
		public TemplateSRPType CurrentSRPType { get { return m_currentSRPType; } set { m_currentSRPType = value; } }
		public bool IsHDRP { get { return m_currentSRPType == TemplateSRPType.HDRP; } }
		public bool IsURP { get { return m_currentSRPType == TemplateSRPType.URP; } }
		public bool IsSRP { get { return ( m_currentSRPType == TemplateSRPType.URP || m_currentSRPType == TemplateSRPType.HDRP ); } }
		public TemplateInterpData InterpData { get { return m_interpolatorData; } }
		public List<PropertyDataCollector> LateDirectivesList { get { return m_lateDirectivesList; } }
		public List<PropertyDataCollector> SrpBatcherPropertiesList { get { return m_srpBatcherPropertiesList; } }
		public List<PropertyDataCollector> FullSrpBatcherPropertiesList { get { return m_fullSrpBatcherPropertiesList; } }
		public List<PropertyDataCollector> DotsPropertiesList { get { return m_dotsPropertiesList; } }
		public List<PropertyDataCollector> DotsDefinesList { get { return m_dotsDefinesList; } }
		public List<PropertyDataCollector> FullDotsPropertiesList { get { return m_fullDotsPropertiesList; } }
		public Dictionary<TemplateSemantics, TemplateVertexData> VertexDataDict { get { return m_vertexDataDict; } }
	}
}

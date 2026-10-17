// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class PropertyDataCollector
	{
		public int NodeId;
		public int OrderIndex;
		public string PropertyName;
		public WirePortDataType DataType;
		public bool IsDirective;

		public PropertyDataCollector( int nodeId, string propertyName, int orderIndex = -1, bool isDirective = false )
		{
			NodeId = nodeId;
			PropertyName = propertyName;
			OrderIndex = orderIndex;
			IsDirective = isDirective;
		}
	}

	// @diogo: a sampler state borrowed from another texture only binds on programs where that texture
	// survives compilation, so its final name is decided per pass and program (#7, #549)
	public class BorrowedSamplerData
	{
		public int NodeId;
		public string DonorProperty;
		public string DonorSampler;
		public string FallbackSampler;
		public string Token;
		public bool DeclareFallback;
		public bool OnVertexProgram;

		public BorrowedSamplerData( int nodeId, string donorProperty, string donorSampler, string fallbackSampler, string token, bool declareFallback )
		{
			NodeId = nodeId;
			DonorProperty = donorProperty;
			DonorSampler = donorSampler;
			FallbackSampler = fallbackSampler;
			Token = token;
			DeclareFallback = declareFallback;
		}
	}

	public class InputCoordsCollector
	{
		public int NodeId;
		public string CoordName;
		public WirePortDataType DataType;
		public PrecisionType Precision;
		public int TextureSlot;
		public int TextureIndex;

		public InputCoordsCollector( int nodeId, string coordName, WirePortDataType dataType, PrecisionType precision, int textureSlot, int textureIndex )
		{
			NodeId = nodeId;
			CoordName = coordName;
			DataType = dataType;
			Precision = precision;
			TextureSlot = textureSlot;
			TextureIndex = textureIndex;
		}
	}

	public class TextureDefaultsDataColector
	{
		private List<string> m_names = new List<string>();
		private List<Texture> m_values = new List<Texture>();
		public void AddValue( string newName, Texture newValue )
		{
			m_names.Add( newName );
			m_values.Add( newValue );
		}

		public void Destroy()
		{
			m_names.Clear();
			m_names = null;

			m_values.Clear();
			m_values = null;
		}

		public string[] NamesArr { get { return m_names.ToArray(); } }
		public Texture[] ValuesArr { get { return m_values.ToArray(); } }
	}

	public enum TextureChannelUsage
	{
		Not_Used,
		Used,
		Required
	}

	[Flags]
	public enum MacrosMask
	{
		NONE = 0,
		AUTO = 1 << 1,
		LOD = 1 << 2,
		BIAS = 1 << 3,
		GRAD = 1 << 4,
	}

	[NoAutoStaticsCleanup]
	public class MasterNodeDataCollector
	{
		private bool m_showDebugMessages = false;
		private string m_input;
		private string m_customInput;
		private string m_properties;
		private string m_instancedProperties;
		private string m_instanceBlockName;
		private string m_uniforms;
		private string m_includes;
		private string m_pragmas;
		private string m_defines;
		private string m_instructions;
		private string m_localVariables;
		// @diogo: appended once per generated line, so strings here went quadratic on big graphs
		private StringBuilder m_vertexLocalVariables = new StringBuilder();
		private StringBuilder m_specialLocalVariables = new StringBuilder();
		private string m_vertexData;
		private StringBuilder m_customOutput = new StringBuilder();
		private string m_functions;
		private string m_structs;
		private string m_grabPass;

		private List<PropertyDataCollector> m_inputList;
		private List<PropertyDataCollector> m_customInputList;
		private List<PropertyDataCollector> m_propertiesList;
		private List<PropertyDataCollector> m_instancedPropertiesList;
		private List<PropertyDataCollector> m_uniformsList;
		private List<PropertyDataCollector> m_includesList;
		private List<PropertyDataCollector> m_additionalDirectivesList;
		//private List<PropertyDataCollector> m_tagsList;
		private List<PropertyDataCollector> m_pragmasList;
		private List<PropertyDataCollector> m_definesList;
		private List<PropertyDataCollector> m_instructionsList;
		private List<PropertyDataCollector> m_localVariablesList;
		private List<PropertyDataCollector> m_vertexLocalVariablesList;
		private List<PropertyDataCollector> m_specialLocalVariablesList;
		private List<PropertyDataCollector> m_vertexDataList;
		private List<PropertyDataCollector> m_customOutputList;
		private List<PropertyDataCollector> m_functionsList;
		private List<PropertyDataCollector> m_grabPassList;
		private List<PropertyDataCollector> m_aboveUsePassesList;
		private List<PropertyDataCollector> m_belowUsePassesList;

		private bool m_surfaceCustomShadowCaster = false;
		private List<InputCoordsCollector> m_customShadowCoordsList;
		private List<int> m_packSlotsList;
		private string m_customAppDataItems;

		private Dictionary<string, PropertyDataCollector> m_inputDict;
		private Dictionary<string, PropertyDataCollector> m_customInputDict;
		private Dictionary<string, PropertyDataCollector> m_propertiesDict;
		private Dictionary<string, PropertyDataCollector> m_instancedPropertiesDict;
		private Dictionary<string, PropertyDataCollector> m_uniformsDict;
		private Dictionary<string, PropertyDataCollector> m_softRegisteredUniformsDict;
		private Dictionary<string, PropertyDataCollector> m_includesDict;
		private Dictionary<string, PropertyDataCollector> m_additionalDirectivesDict;
		private Dictionary<string, string> m_includesExclusionDict;

		//private Dictionary<string, PropertyDataCollector> m_tagsDict;
		private Dictionary<string, PropertyDataCollector> m_pragmasDict;
		private Dictionary<string, PropertyDataCollector> m_definesDict;
		private Dictionary<string, int> m_virtualCoordinatesDict;
		private Dictionary<string, string> m_virtualVariablesDict;
		private Dictionary<string, PropertyDataCollector> m_localVariablesDict;
		private Dictionary<string, PropertyDataCollector> m_localVariablesByNameDict;
		private Dictionary<string, PropertyDataCollector> m_vertexLocalVariablesDict;
		private Dictionary<string, PropertyDataCollector> m_vertexLocalVariablesByNameDict;
		private Dictionary<string, PropertyDataCollector> m_specialLocalVariablesDict;
		private Dictionary<string, PropertyDataCollector> m_specialLocalVariablesByNameDict;
		private Dictionary<string, PropertyDataCollector> m_vertexDataDict;
		private Dictionary<string, PropertyDataCollector> m_customOutputDict;
		private Dictionary<string, PropertyDataCollector> m_customOutputByNameDict;
		private Dictionary<string, string> m_localFunctions;
		private Dictionary<string, string> m_localStructs;
		private Dictionary<string, string> m_grabPassDict;
		private Dictionary<string, string> m_usePassesDict;
		private Dictionary<string, string> m_customAppDataItemsDict;

		private Dictionary<string, InputCoordsCollector> m_customShadowCoordsDict;

		private TextureChannelUsage[] m_requireTextureProperty = { TextureChannelUsage.Not_Used, TextureChannelUsage.Not_Used, TextureChannelUsage.Not_Used, TextureChannelUsage.Not_Used };
		private WirePortDataType[] m_textureChannelSize =
		{
			WirePortDataType.FLOAT2,
			WirePortDataType.FLOAT2,
			WirePortDataType.FLOAT2,
			WirePortDataType.FLOAT2,
			WirePortDataType.FLOAT2,
			WirePortDataType.FLOAT2,
			WirePortDataType.FLOAT2,
			WirePortDataType.FLOAT2
		};

		private bool m_dirtyAppData;
		private bool m_dirtyInputs;
		private bool m_dirtyCustomInputs;
		private bool m_dirtyFunctions;
		private bool m_dirtyProperties;
		private bool m_dirtyInstancedProperties;
		private bool m_dirtyUniforms;
		private bool m_dirtyIncludes;
		private bool m_dirtyPragmas;
		private bool m_dirtyDefines;
		private bool m_dirtyAdditionalDirectives;
		private bool m_dirtyInstructions;
		private bool m_dirtyLocalVariables;
		private bool m_dirtyVertexLocalVariables;
		private bool m_dirtySpecialLocalVariables;
		private bool m_dirtyPerVertexData;
		private bool m_dirtyNormal;
		private bool m_forceNormal;

		private bool m_usingInternalData;
		private bool m_usingVertexColor;
		private bool m_usingWorldPosition;
		private bool m_usingWorldNormal;
		private bool m_usingScreenPos;
		private bool m_usingWorldReflection;
		private bool m_usingViewDirection;
		private bool m_usingLightAttenuation;
		private bool m_usingArrayDerivatives;
		private bool m_usingTextureArrays;
		private bool m_usingExtraStandardMacros;
		private MacrosMask m_using2DMacrosMask;
		private MacrosMask m_using3DMacrosMask;
		private MacrosMask m_usingCUBEMacrosMask;
		private MacrosMask m_using2DArrayMacrosMask;
		private bool m_usingExtra3DSRPMacros;

		private bool m_usingHigherSizeTexcoords;
		private bool m_usingCustomScreenPos;

		private bool m_usingCustomOutlineColor;
		private bool m_usingCustomOutlineWidth;
		private bool m_usingCustomOutlineAlpha;
		private int m_customOutlineSelectedAlpha = 0;
		private bool m_usingCustomOutput;

		private bool m_safeNormalizeLightDir;
		private bool m_safeNormalizeViewDir;

		private bool m_isOutlineDataCollector = false;

		private bool m_forceNormalIsDirty;
		private bool m_grabPassIsDirty;
		private bool m_tesselationActive;

		private Dictionary<int, PropertyNode> m_propertyNodes;
		private MasterNode m_masterNode;

		private int m_availableVertexTempId = 0;
		private int m_availableFragTempId = 0;

		private MasterNodePortCategory m_portCategory;
		private PortGenType m_genType;
		private RenderPath m_renderPath = RenderPath.All;
		private NodeAvailability m_currentCanvasMode = NodeAvailability.SurfaceShader;

		// @diogo: stage sides of the branch scope stacks, matching UIUtils.PortCategorytoAttayIdx (#365).
		private const int VertexScopeSide = 0;
		private const int FragmentScopeSide = 1;

		// @diogo: everything below is created on the first pushed scope, so a compile that never branches pays nothing (#365).
		private List<int>[] m_branchScopeStacks = null;
		private Dictionary<int, int> m_branchScopeParents = null;
		private Dictionary<string, List<int>> m_localVariablesScopes = null;
		private Dictionary<string, List<int>> m_specialLocalVariablesScopes = null;
		private Dictionary<string, List<int>> m_vertexLocalVariablesScopes = null;
		private Dictionary<string, List<int>> m_customOutputScopes = null;
		private HashSet<int> m_divergenceHazardNodes = null;
		private HashSet<int> m_outOfScopeValueNodes = null;
		private HashSet<int> m_uniformBranchScopes = null;
		private Dictionary<int, string> m_branchScopeConditions = null;
		private int m_branchScopeCounter = 0;
		private int m_hazardDeclarationDepth = 0;

		//Templates specific data
		private AvailableShaderTypes m_masterNodeCategory;
		private List<string> m_vertexInputList;
		private Dictionary<string, string> m_vertexInputDict;
		private List<string> m_interpolatorsList;
		private Dictionary<string, string> m_interpolatorsDict;
		private List<string> m_vertexInterpDeclList;
		private Dictionary<string, string> m_vertexInterpDeclDict;
		private TemplateDataCollector m_templateDataCollector;
		private Dictionary<MasterNodePortCategory, Dictionary<ParentNode, StructFieldMask>> m_propagateNodeCache;

		// @diogo: borrowed sampler states are emitted as tokens and only get their final name once the pass code
		// is known, so we can tell whether the donor texture is still there to bind them; the trailing separator
		// keeps a token from being a prefix of the next one, which string replacement would eat halfway (#7, #549)
		private const string BorrowedSamplerTokenFormat = "ASEBSAMPLER_{0}_{1}_{2}_{3}_";
		private Dictionary<string, BorrowedSamplerData> m_borrowedSamplers = new Dictionary<string, BorrowedSamplerData>();
		private Dictionary<string, string> m_borrowedSamplerValues = new Dictionary<string, string>();
		private List<BorrowedSamplerData> m_borrowedSamplerFallbacks = new List<BorrowedSamplerData>();
		private List<string> m_vertexGeneratedCode = new List<string>();
		private List<string> m_fragmentGeneratedCode = new List<string>();
		private Dictionary<string, int> m_passWarnings = new Dictionary<string, int>();

		public bool Touch( MasterNodePortCategory category, ParentNode node, StructFieldMask fields )
		{
			if ( !m_propagateNodeCache.TryGetValue( category, out Dictionary<ParentNode, StructFieldMask> propagateSet ) )
			{
				propagateSet = new Dictionary<ParentNode, StructFieldMask>();
				m_propagateNodeCache.Add( category, propagateSet );
			}

			if ( !propagateSet.TryGetValue( node, out StructFieldMask stored ) )
			{
				// @diogo: not touched yet; cache miss
				propagateSet.Add( node, fields );
				return false;
			}
			else if ( stored.Covers( fields ) )
			{
				// @diogo: already touched; cache hit
				return true;
			}
			else
			{
				// @diogo: a struct wire asked for fields this node has not forwarded yet
				propagateSet[ node ] = stored.Union( fields );
				return false;
			}
		}

		// Whether the propagation pre-pass reached this node in any category of the current pass
		public bool WasTouched( ParentNode node )
		{
			if ( m_propagateNodeCache == null )
			{
				return false;
			}

			foreach ( KeyValuePair<MasterNodePortCategory, Dictionary<ParentNode, StructFieldMask>> entry in m_propagateNodeCache )
			{
				if ( entry.Value.ContainsKey( node ) )
				{
					return true;
				}
			}
			return false;
		}

		public MasterNodeDataCollector( MasterNode masterNode ) : this()
		{
			m_masterNode = masterNode;
			m_masterNodeCategory = masterNode.CurrentMasterNodeCategory;
			m_currentCanvasMode = masterNode.ContainerGraph.CurrentCanvasMode;
		}

		public MasterNodeDataCollector()
		{
			//m_masterNode = masterNode;
			m_input = "struct Input\n\t\t{\n";
			m_customInput = "\t\tstruct SurfaceOutput{0}\n\t\t{\n";
			m_properties = IOUtils.PropertiesBegin;//"\tProperties\n\t{\n";
			m_uniforms = string.Empty;
			m_instructions = string.Empty;
			m_includes = string.Empty;
			m_pragmas = string.Empty;
			m_defines = string.Empty;
			m_localVariables = string.Empty;

			m_inputList = new List<PropertyDataCollector>();
			m_customInputList = new List<PropertyDataCollector>();
			m_propertiesList = new List<PropertyDataCollector>();
			m_instancedPropertiesList = new List<PropertyDataCollector>();
			m_uniformsList = new List<PropertyDataCollector>();
			m_includesList = new List<PropertyDataCollector>();
			m_additionalDirectivesList = new List<PropertyDataCollector>();
			//m_tagsList = new List<PropertyDataCollector>();
			m_pragmasList = new List<PropertyDataCollector>();
			m_definesList = new List<PropertyDataCollector>();
			m_instructionsList = new List<PropertyDataCollector>();
			m_localVariablesList = new List<PropertyDataCollector>();
			m_vertexLocalVariablesList = new List<PropertyDataCollector>();
			m_specialLocalVariablesList = new List<PropertyDataCollector>();
			m_vertexDataList = new List<PropertyDataCollector>();
			m_customOutputList = new List<PropertyDataCollector>();
			m_functionsList = new List<PropertyDataCollector>();
			m_grabPassList = new List<PropertyDataCollector>();
			m_aboveUsePassesList = new List<PropertyDataCollector>();
			m_belowUsePassesList = new List<PropertyDataCollector>();
			m_customAppDataItems = string.Empty;
			m_customAppDataItemsDict = new Dictionary<string, string>();
			m_customShadowCoordsList = new List<InputCoordsCollector>();
			m_packSlotsList = new List<int>();

			m_inputDict = new Dictionary<string, PropertyDataCollector>();
			m_customInputDict = new Dictionary<string, PropertyDataCollector>();

			m_propertiesDict = new Dictionary<string, PropertyDataCollector>();
			m_instancedPropertiesDict = new Dictionary<string, PropertyDataCollector>();
			m_uniformsDict = new Dictionary<string, PropertyDataCollector>();
			m_softRegisteredUniformsDict = new Dictionary<string, PropertyDataCollector>();
			m_includesDict = new Dictionary<string, PropertyDataCollector>();
			m_additionalDirectivesDict = new Dictionary<string, PropertyDataCollector>();
			m_includesExclusionDict = new Dictionary<string, string>();

			//m_tagsDict = new Dictionary<string, PropertyDataCollector>();
			m_pragmasDict = new Dictionary<string, PropertyDataCollector>();
			m_definesDict = new Dictionary<string, PropertyDataCollector>();
			m_virtualCoordinatesDict = new Dictionary<string, int>();
			m_localVariablesDict = new Dictionary<string, PropertyDataCollector>();
			m_localVariablesByNameDict = new Dictionary<string, PropertyDataCollector>();
			m_virtualVariablesDict = new Dictionary<string, string>();
			m_specialLocalVariablesDict = new Dictionary<string, PropertyDataCollector>();
			m_specialLocalVariablesByNameDict = new Dictionary<string, PropertyDataCollector>();
			m_vertexLocalVariablesDict = new Dictionary<string, PropertyDataCollector>();
			m_vertexLocalVariablesByNameDict = new Dictionary<string, PropertyDataCollector>();
			m_localFunctions = new Dictionary<string, string>();
			m_localStructs = new Dictionary<string, string>();
			m_vertexDataDict = new Dictionary<string, PropertyDataCollector>();
			m_customOutputDict = new Dictionary<string, PropertyDataCollector>();
			m_customOutputByNameDict = new Dictionary<string, PropertyDataCollector>();
			m_grabPassDict = new Dictionary<string, string>();
			m_usePassesDict = new Dictionary<string, string>();

			m_customShadowCoordsDict = new Dictionary<string, InputCoordsCollector>();

			m_dirtyAppData = false;
			m_dirtyInputs = false;
			m_dirtyCustomInputs = false;
			m_dirtyProperties = false;
			m_dirtyInstancedProperties = false;
			m_dirtyUniforms = false;
			m_dirtyInstructions = false;
			m_dirtyIncludes = false;
			m_dirtyPragmas = false;
			m_dirtyDefines = false;
			m_dirtyAdditionalDirectives = false;
			m_dirtyLocalVariables = false;
			m_dirtySpecialLocalVariables = false;
			m_grabPassIsDirty = false;

			m_portCategory = MasterNodePortCategory.Fragment;
			m_propertyNodes = new Dictionary<int, PropertyNode>();
			m_showDebugMessages = ( m_showDebugMessages && DebugConsoleWindow.DeveloperMode );

			//templates
			//m_masterNodeCategory = masterNode.CurrentMasterNodeCategory;

			m_vertexInputList = new List<string>();
			m_vertexInputDict = new Dictionary<string, string>();

			m_interpolatorsList = new List<string>();
			m_interpolatorsDict = new Dictionary<string, string>();

			m_vertexInterpDeclList = new List<string>();
			m_vertexInterpDeclDict = new Dictionary<string, string>();

			m_templateDataCollector = new TemplateDataCollector();

			m_propagateNodeCache = new Dictionary<MasterNodePortCategory, Dictionary<ParentNode, StructFieldMask>>();
		}

		public void CopyTextureChannelSizeFrom( ref MasterNodeDataCollector dataCollector )
		{
			for( int i = 0 ; i < m_textureChannelSize.Length ; i++ )
			{
				SetTextureChannelSize( i , dataCollector.GetMaxTextureChannelSize( i ) );
			}
		}

		public void SetTextureChannelSize( int channelIdx , WirePortDataType size )
		{
			if( size > m_textureChannelSize[ channelIdx ] )
			{
				m_textureChannelSize[ channelIdx ] = size;
			}
		}

		public WirePortDataType GetMaxTextureChannelSize( int channelIdx )
		{
			return m_textureChannelSize[ channelIdx ];
		}

		public void SetChannelUsage( int channelId, TextureChannelUsage usage )
		{
			if( channelId > -1 && channelId < 4 )
				m_requireTextureProperty[ channelId ] = usage;
		}

		public TextureChannelUsage GetChannelUsage( int channelId )
		{
			if( channelId > -1 && channelId < 4 )
				return m_requireTextureProperty[ channelId ];

			return TextureChannelUsage.Not_Used;
		}

		public void OpenPerVertexHeader( bool includeCustomData )
		{
			string appData = "inout " + ( m_dirtyAppData ? Constants.CustomAppDataFullName : Constants.AppDataFullName ) + " ";
			if( m_dirtyPerVertexData )
				return;

			m_dirtyPerVertexData = true;
			if( m_tesselationActive )
			{
				m_vertexData = "\t\tvoid " + Constants.VertexDataFunc + "( " + appData + Constants.VertexShaderInputStr + " )\n\t\t{\n";
			}
			else
			{
				m_vertexData = "\t\tvoid " + Constants.VertexDataFunc + "( " + appData + Constants.VertexShaderInputStr + ( includeCustomData ? ( string.Format( ", out Input {0}", Constants.VertexShaderOutputStr ) ) : string.Empty ) + " )\n\t\t{\n";
				if( includeCustomData )
					m_vertexData += string.Format( "\t\t\tUNITY_INITIALIZE_OUTPUT( Input, {0} );\n", Constants.VertexShaderOutputStr );
			}
		}

		public void ClosePerVertexHeader()
		{
			if( m_dirtyPerVertexData )
				m_vertexData += "\t\t}\n\n";
		}

		public void AddToVertexDisplacement( string value, VertexMode vertexMode )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			if( !m_dirtyPerVertexData )
			{
				OpenPerVertexHeader( true );
			}

			switch( vertexMode )
			{
				default:
				case VertexMode.Relative:
				{
					m_vertexData += "\t\t\t" + Constants.VertexShaderInputStr + ".vertex.xyz += " + value + ";\n";
				}
				break;
				case VertexMode.Absolute:
				{
					m_vertexData += "\t\t\t" + Constants.VertexShaderInputStr + ".vertex.xyz = " + value + ";\n";
				}
				break;
			}
			m_vertexData += "\t\t\t" + Constants.VertexShaderInputStr + ".vertex.w = 1;\n";
		}


		public void AddToVertexNormal( string value )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			if( !m_dirtyPerVertexData )
			{
				OpenPerVertexHeader( true );
			}

			m_vertexData += "\t\t\t" + Constants.VertexShaderInputStr + ".normal = " + value + ";\n";
		}

		public void AddToVertexTangent( string value )
		{
			if ( string.IsNullOrEmpty( value ) )
				return;

			if ( !m_dirtyPerVertexData )
			{
				OpenPerVertexHeader( true );
			}

			m_vertexData += "\t\t\t" + Constants.VertexShaderInputStr + ".tangent = " + value + ";\n";
		}

		public void AddVertexInstruction( string value, int nodeId = -1, bool addDelimiters = true )
		{
			if( !m_dirtyPerVertexData && !IsOutlineDataCollector/*&& !(m_usingCustomOutlineColor || m_usingCustomOutlineWidth)*/ )
			{
				OpenPerVertexHeader( true );
			}
			if( !m_vertexDataDict.ContainsKey( value ) )
			{
				m_vertexDataDict.Add( value, new PropertyDataCollector( nodeId, value ) );
				m_vertexDataList.Add( m_vertexDataDict[ value ] );
				m_vertexData += ( addDelimiters ? ( "\t\t\t" + value + ";\n" ) : value );
			}
		}

		public bool ContainsInput( string value )
		{
			return m_inputDict.ContainsKey( value );
		}

		public void AddToInput( int nodeId, string interpName, WirePortDataType dataType, PrecisionType precision = PrecisionType.Float, bool addSemiColon = true )
		{
			string value = UIUtils.PrecisionWirePortToCgType( precision, dataType ) + " " + interpName;
			AddToInput( nodeId, value, addSemiColon );

			if( !m_customShadowCoordsDict.ContainsKey( interpName ) )
			{
				int slot = 0;
				int index = 0;
				int size = UIUtils.GetChannelsAmount( dataType );

				if( m_packSlotsList.Count == 0 )
					m_packSlotsList.Add( 4 );

				for( int i = 0; i < m_packSlotsList.Count; i++ )
				{
					slot = i;
					if( m_packSlotsList[ i ] >= size )
					{
						index = 4 - m_packSlotsList[ i ];
						m_packSlotsList[ i ] -= size;
						break;
					}
					else if( i == m_packSlotsList.Count - 1 )
					{
						m_packSlotsList.Add( 4 );
					}
				}
				m_customShadowCoordsDict.Add( interpName, new InputCoordsCollector( nodeId, interpName, dataType, precision, slot, index ) );
				m_customShadowCoordsList.Add( m_customShadowCoordsDict[ interpName ] );
			}
		}

		public void AddToInput( int nodeId, SurfaceInputs surfaceInput, PrecisionType precision = PrecisionType.Float, bool addSemiColon = true )
		{
			switch( surfaceInput )
			{
				case SurfaceInputs.VIEW_DIR:
				UsingViewDirection = true;
				break;
				case SurfaceInputs.SCREEN_POS:
				UsingScreenPos = true;
				break;
				case SurfaceInputs.WORLD_POS:
				UsingWorldPosition = true;
				break;
				case SurfaceInputs.WORLD_REFL:
				UsingWorldReflection = true;
				break;
				case SurfaceInputs.WORLD_NORMAL:
				UsingWorldNormal = true;
				break;
				case SurfaceInputs.INTERNALDATA:
				UsingInternalData = true;
				break;
				case SurfaceInputs.COLOR:
				UsingVertexColor = true;
				break;
			}

			AddToInput( nodeId, UIUtils.GetInputDeclarationFromType( precision, surfaceInput ), addSemiColon );
		}

		/// <summary>
		/// Direct access to inputs, plese use another overload
		/// </summary>
		/// <param name="nodeId"></param>
		/// <param name="value"></param>
		/// <param name="addSemiColon"></param>
		public void AddToInput( int nodeId, string value, bool addSemiColon )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			if( !m_inputDict.ContainsKey( value ) )
			{
				m_inputDict.Add( value, new PropertyDataCollector( nodeId, value ,-1, !addSemiColon) );
				m_inputList.Add( m_inputDict[ value ] );

				m_input += "\t\t\t" + value + ( ( addSemiColon ) ? ( ";\n" ) : "\n" );
				m_dirtyInputs = true;
			}
		}

		public void CloseInputs()
		{
			m_input += "\t\t};";
		}

		public void ChangeCustomInputHeader( string value )
		{
			m_customInput = m_customInput.Replace( "{0}", value );
		}

		public void AddToCustomInput( int nodeId, string value, bool addSemiColon )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			if( !m_customInputDict.ContainsKey( value ) )
			{
				m_customInputDict.Add( value, new PropertyDataCollector( nodeId, value ) );
				m_customInputList.Add( m_customInputDict[ value ] );
				m_customInput += "\t\t\t" + value + ( ( addSemiColon ) ? ( ";\n" ) : "\n" );
				m_dirtyCustomInputs = true;
			}
		}

		public void CloseCustomInputs()
		{
			m_customInput += "\t\t};";
		}


		// Used by Template Master Node to add tabs into variable declaration
		public void TabifyInstancedVars()
		{
			for( int i = 0; i < m_instancedPropertiesList.Count; i++ )
			{
				m_instancedPropertiesList[ i ].PropertyName = '\t' + m_instancedPropertiesList[ i ].PropertyName;
			}
		}

		private int GetWeightForInstancedType( WirePortDataType type )
		{
			switch( type )
			{
				case WirePortDataType.INT:
				case WirePortDataType.FLOAT: return -1;
				case WirePortDataType.FLOAT2: return -2;
				case WirePortDataType.FLOAT3: return -3;
				case WirePortDataType.COLOR:
				case WirePortDataType.FLOAT4: return -4;
				case WirePortDataType.FLOAT2x2: return -8;
				case WirePortDataType.FLOAT3x3: return -9;
				case WirePortDataType.FLOAT4x4: return -16;
				default:
				case WirePortDataType.OBJECT:
				case WirePortDataType.SAMPLER1D:
				case WirePortDataType.SAMPLER2D:
				case WirePortDataType.SAMPLER3D:
				case WirePortDataType.SAMPLERCUBE:
				case WirePortDataType.SAMPLER2DARRAY:
				case WirePortDataType.SAMPLERSTATE:
				return 0;
			}
		}

		public void OptimizeInstancedProperties()
		{
			if( m_instancedPropertiesList.Count > 0 )
			{
				m_instancedProperties = string.Empty;
				m_instancedPropertiesList.Sort( ( x, y ) => { return GetWeightForInstancedType( x.DataType ).CompareTo( GetWeightForInstancedType( y.DataType ) ); } );
				int count = m_instancedPropertiesList.Count;
				for( int i = 0; i < count; i++ )
				{
					m_instancedProperties += m_instancedPropertiesList[ i ].PropertyName;
				}
			}
		}
		// Instanced properties
		public void SetupInstancePropertiesBlock( string blockName )
		{
			m_instanceBlockName = blockName;
			if( IsTemplate )
			{
				//if( DebugConsoleWindow.DeveloperMode )
				//	Debug.LogWarning( "SetupInstancePropertiesBlock should not be used during template mode" );

				return;
			}

			OptimizeInstancedProperties();

			if( m_dirtyInstancedProperties )
			{
				m_instancedProperties = string.Format( IOUtils.InstancedPropertiesBeginTabs, blockName ) + m_instancedProperties + IOUtils.InstancedPropertiesEndTabs;
			}
		}

		public void AddToDotsProperties( WirePortDataType dataType, int nodeId, string value, int orderIndex, PrecisionType precision )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			string prop = string.Format( IOUtils.DotsInstancedPropertiesData, UIUtils.PrecisionWirePortToCgType( precision, dataType ), value );
			string define = string.Format( IOUtils.DotsInstancedDefinesData, UIUtils.PrecisionWirePortToCgType( precision, dataType ), value );

			m_templateDataCollector.AddDotsProperty( nodeId, prop, define, orderIndex );
		}

		public void AddToInstancedProperties( WirePortDataType dataType, int nodeId, string value, int orderIndex )
		{
			if( string.IsNullOrEmpty( value ) )
				return;
			string uniformValue = value.Contains( "uniform" ) ? value : "uniform " + value;
			if( !m_instancedPropertiesDict.ContainsKey( value ) &&
				!m_uniformsDict.ContainsKey( value ) &&
				!m_uniformsDict.ContainsKey( uniformValue )
				)
			{
				PropertyDataCollector dataColl = new PropertyDataCollector( nodeId, value, orderIndex );
				dataColl.DataType = dataType;
				m_instancedPropertiesDict.Add( value, dataColl );
				m_instancedPropertiesList.Add( dataColl );
				m_instancedProperties += value;
				m_dirtyInstancedProperties = true;
			}
		}

		public void CloseInstancedProperties()
		{
			if( m_dirtyInstancedProperties )
			{
				m_instancedProperties += IOUtils.InstancedPropertiesEnd;
			}
		}

		// Properties
		public void CopyPropertiesFromDataCollector( MasterNodeDataCollector dataCollector )
		{
			if( dataCollector == null )
				return;

			int propertyCount = dataCollector.PropertiesList.Count;
			for( int i = 0; i < propertyCount; i++ )
			{
				AddToProperties( dataCollector.PropertiesList[ i ].NodeId,
								dataCollector.PropertiesList[ i ].PropertyName,
								dataCollector.PropertiesList[ i ].OrderIndex );
			}

			foreach( KeyValuePair<string, string> kvp in dataCollector.GrabPassDict )
			{
				AddGrabPass( kvp.Value );
			}

			m_templateDataCollector.CopySRPPropertiesFromDataCollector( -1, dataCollector.TemplateDataCollectorInstance );
			m_templateDataCollector.CopyDotsPropertiesFromDataCollector( -1, dataCollector.TemplateDataCollectorInstance );
		}

		public void AddToProperties( int nodeId, string value, int orderIndex )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			if( !m_propertiesDict.ContainsKey( value ) )
			{
				//Debug.Log( UIUtils );
				m_propertiesDict.Add( value, new PropertyDataCollector( nodeId, value, orderIndex ) );
				m_propertiesList.Add( m_propertiesDict[ value ] );
				m_properties += string.Format( IOUtils.PropertiesElement, value );
				m_dirtyProperties = true;
			}
		}

		public string BuildPropertiesString()
		{
			List<PropertyDataCollector> list = new List<PropertyDataCollector>( m_propertiesDict.Values );
			//for ( int i = 0; i < list.Count; i++ )
			//{
			//	Debug.Log( list[ i ].OrderIndex + " " + list[ i ].PropertyName );
			//}

			list.Sort( ( x, y ) => { return x.OrderIndex.CompareTo( y.OrderIndex ); } );
			CleanUpList( ref list );
			m_properties = IOUtils.PropertiesBegin;
			for( int i = 0; i < list.Count; i++ )
			{
				m_properties += string.Format( IOUtils.PropertiesElement, list[ i ].PropertyName );
				//Debug.Log()
			}
			m_properties += IOUtils.PropertiesEnd;
			return m_properties;
		}

		public bool ContainsProperty( string propertyName )
		{
			// TODO: this needs to change, find the property should be dependant of have a "("
			List<PropertyDataCollector> list = new List<PropertyDataCollector>( m_propertiesDict.Values );
			return list.Find( x => x.PropertyName.Contains( propertyName+"(" ) ) != null;
		}

		public string[] BuildUnformatedPropertiesStringArr()
		{
			List<PropertyDataCollector> list = new List<PropertyDataCollector>( m_propertiesDict.Values );
			list.Sort( ( x, y ) => { return x.OrderIndex.CompareTo( y.OrderIndex ); } );
			CleanUpList( ref list );
			string[] arr = new string[ list.Count ];
			for( int i = 0; i < list.Count; i++ )
			{
				arr[ i ] = list[ i ].PropertyName;
			}
			return arr;
		}
		//This clean up was set to remove Header attributes from shader functions which would be last on the property list
		//Thus creating a label on the inspector with no properties below
		public void CleanUpList( ref List<PropertyDataCollector> list )
		{
			if( list.Count == 0 )
				return;

			if( list[ list.Count - 1 ].PropertyName.Contains( "[Header(" ) )
			{
				//Check if this is a complete property or just a standalone header
				Match match = Regex.Match( list[ list.Count - 1 ].PropertyName, TemplateHelperFunctions.PropertiesPatternI );
				if( !match.Success )
				{
					list.RemoveAt( list.Count - 1 );
					CleanUpList( ref list );
				}
			}
		}

		public void CloseProperties()
		{
			if( m_dirtyProperties )
			{
				m_properties += IOUtils.PropertiesEnd;
			}
		}

		public void AddUsePass( string value, bool above )
		{
			if( m_usePassesDict.ContainsKey( value ) )
				return;
			m_usePassesDict.Add( value, value );
			if( above )
			{
				m_aboveUsePassesList.Add( new PropertyDataCollector( -1, value ) );
			}
			else
			{
				m_belowUsePassesList.Add( new PropertyDataCollector( -1, value ) );
			}
		}

		public void AddGrabPass( string value )
		{
			if( m_grabPassDict.ContainsKey( value ) )
				return;

			m_grabPassDict.Add( value, value );

			if( string.IsNullOrEmpty( value ) )
			{
				if( !m_grabPassIsDirty )
					m_grabPass += IOUtils.GrabPassEmpty;
			}
			else
			{
				m_grabPass += IOUtils.GrabPassBegin + value + IOUtils.GrabPassEnd;
			}
			m_grabPassList.Add( new PropertyDataCollector( -1, m_grabPass.Replace( "\t", string.Empty ).Replace( "\n", string.Empty ) ) );
			m_grabPassIsDirty = true;
		}

		// This is used by templates global variables to register already existing globals/properties
		//public void SoftRegisterUniform( string dataName )
		//{
		//	if( !m_uniformsDict.ContainsKey( dataName ) )
		//	{
		//		m_uniformsDict.Add( dataName, new PropertyDataCollector( -1, dataName ) );
		//	}
		//}

		public string GenerateInstanced(PrecisionType precisionType, WirePortDataType dataType,string propertyName )
		{
			if( IsSRP )
			{
				return string.Format( IOUtils.LWSRPInstancedPropertiesElement, UIUtils.PrecisionWirePortToCgType( precisionType, dataType ), propertyName );
			}
			else
			{
				return string.Format( IOUtils.InstancedPropertiesElement, UIUtils.PrecisionWirePortToCgType( precisionType, dataType ), propertyName );
			}
		}

		public bool CheckIfSoftRegistered( string name )
		{
			return m_softRegisteredUniformsDict.ContainsKey( name );
		}

		public void SoftRegisterUniform( TemplateShaderPropertyData data )
		{
			bool excludeUniformKeyword = ( data.PropertyType == PropertyType.InstancedProperty ) || IsSRP;

			string uniformName = data.IsMacro?	data.PropertyName:
												UIUtils.GenerateUniformName( excludeUniformKeyword, data.PropertyDataType, data.PropertyName );
			if( !m_uniformsDict.ContainsKey( uniformName ) )
			{
				PropertyDataCollector newEntry = new PropertyDataCollector( -1, uniformName );
				m_uniformsDict.Add( uniformName, newEntry );
				m_softRegisteredUniformsDict.Add( uniformName, newEntry );
			}

			// @diogo: the template owns this name whatever precision the graph is set to, so the half
			// spelling must be reserved too or a Half graph redeclares a Float template uniform (#241)
			if ( !data.IsMacro )
			{
				string halfUniformName = UIUtils.GenerateUniformName( excludeUniformKeyword, UIUtils.PrecisionWirePortToCgType( PrecisionType.Half, data.PropertyDataType ), data.PropertyName );
				if ( !m_uniformsDict.ContainsKey( halfUniformName ) )
				{
					PropertyDataCollector halfEntry = new PropertyDataCollector( -1, halfUniformName );
					m_uniformsDict.Add( halfUniformName, halfEntry );
					m_softRegisteredUniformsDict.Add( halfUniformName, halfEntry );
				}
			}

			string instancedUniform = GenerateInstanced( PrecisionType.Float, data.PropertyDataType, data.PropertyName );
			if( !m_uniformsDict.ContainsKey( instancedUniform ) )
			{
				PropertyDataCollector newEntry = new PropertyDataCollector( -1, instancedUniform );
				m_uniformsDict.Add( instancedUniform, newEntry );
				m_softRegisteredUniformsDict.Add( instancedUniform, newEntry );
			}

			instancedUniform = GenerateInstanced( PrecisionType.Half, data.PropertyDataType, data.PropertyName );
			if( !m_uniformsDict.ContainsKey( instancedUniform ) )
			{
				PropertyDataCollector newEntry = new PropertyDataCollector( -1, instancedUniform );
				m_uniformsDict.Add( instancedUniform, newEntry );
				m_softRegisteredUniformsDict.Add( instancedUniform, newEntry );
			}
		}

		public void AddToUniforms( int nodeId, string dataType, string dataName, bool checkSRPBatch = false, bool excludeUniform = false )
		{
			if( string.IsNullOrEmpty( dataName ) || string.IsNullOrEmpty( dataType ) )
				return;

			string value = UIUtils.GenerateUniformName( IsSRP || excludeUniform, dataType, dataName );
			if( !m_uniformsDict.ContainsKey( value ) && !m_uniformsDict.ContainsKey( dataName ) )
			{
				m_uniformsDict.Add( value, new PropertyDataCollector( nodeId, value ) );
				if( IsSRP && checkSRPBatch )
				{
					m_templateDataCollector.AddSRPBatcherProperty( nodeId, value );
				}
				else
				{
					m_uniforms += "\t\t" + value + '\n';
					m_uniformsList.Add( m_uniformsDict[ value ] );
				}
				m_dirtyUniforms = true;
			}
		}

		public void AddToUniforms( int nodeId, string value, bool checkSRPBatch = false )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			if( !m_uniformsDict.ContainsKey( value ) )
			{
				m_uniformsDict.Add( value, new PropertyDataCollector( nodeId, value ) );
				if( IsSRP && checkSRPBatch )
				{
					m_templateDataCollector.AddSRPBatcherProperty( nodeId, value );
				}
				else
				{
					m_uniforms += "\t\t" + value + '\n';
					m_uniformsList.Add( m_uniformsDict[ value ] );
				}
				m_dirtyUniforms = true;
			}
		}

		// @diogo: registers a sampler state taken from another texture, returning the token that stands in for it
		// until the pass code reveals if the donor texture is used by the same program (#7, #549)
		public string RegisterBorrowedSampler( int nodeId, string donorProperty, string donorSampler, string fallbackSampler, bool declareFallback )
		{
			if ( !( m_masterNode is TemplateMultiPassMasterNode passNode ) || donorSampler.Equals( fallbackSampler ) )
			{
				return donorSampler;
			}

			// @diogo: a node with no cached output regenerates once per consumer, so an indistinguishable borrow
			// must hand back the same token or its lines read as unique text and slip past the emission dedup (#7, #549)
			BorrowedSamplerData match = FindEquivalentBorrowedSampler( nodeId, donorProperty, donorSampler, fallbackSampler, declareFallback, null );
			if ( match != null )
			{
				return match.Token;
			}

			string token = string.Format( BorrowedSamplerTokenFormat, passNode.SubShaderIdx, passNode.PassIdx, nodeId, m_borrowedSamplers.Count );
			m_borrowedSamplers.Add( token, new BorrowedSamplerData( nodeId, donorProperty, donorSampler, fallbackSampler, token, declareFallback ) );
			return token;
		}

		// @diogo: the node taking the sampler knows a better fallback than the donor could, so it gets to aim the
		// pending decision at its own sampler instead of the donor's generic one, and to own the warning (#7, #549).
		// Returns the token to write in the code, or null when there is no borrow behind it
		public string RetargetBorrowedSampler( string token, int nodeId, string fallbackSampler, bool declareFallback )
		{
			if ( !m_borrowedSamplers.TryGetValue( token, out BorrowedSamplerData data ) )
			{
				return null;
			}

			data.NodeId = nodeId;
			data.FallbackSampler = fallbackSampler;
			data.DeclareFallback = declareFallback;

			// @diogo: the donor mints a token per read, so the retarget is where a repeated borrow collapses
			// back onto the one it already claimed (#7, #549)
			BorrowedSamplerData match = FindEquivalentBorrowedSampler( nodeId, data.DonorProperty, data.DonorSampler, fallbackSampler, declareFallback, token );
			if ( match != null )
			{
				m_borrowedSamplers.Remove( token );
				return match.Token;
			}

			return token;
		}

		private BorrowedSamplerData FindEquivalentBorrowedSampler( int nodeId, string donorProperty, string donorSampler, string fallbackSampler, bool declareFallback, string ignoredToken )
		{
			foreach ( BorrowedSamplerData data in m_borrowedSamplers.Values )
			{
				if ( data.NodeId == nodeId && data.DeclareFallback == declareFallback && !data.Token.Equals( ignoredToken ) &&
					data.DonorProperty.Equals( donorProperty ) && data.DonorSampler.Equals( donorSampler ) && data.FallbackSampler.Equals( fallbackSampler ) )
				{
					return data;
				}
			}
			return null;
		}

		public void AddGeneratedCode( MasterNodePortCategory category, string code )
		{
			if ( category == MasterNodePortCategory.Vertex || category == MasterNodePortCategory.Tessellation )
			{
				m_vertexGeneratedCode.Add( code );
			}
			else
			{
				m_fragmentGeneratedCode.Add( code );
			}
		}

		public void ResolveBorrowedSamplers( List<string> vertexInstructions, List<string> fragmentInstructions )
		{
			if ( m_borrowedSamplers.Count == 0 )
			{
				return;
			}

			string vertexCode = string.Join( "\n", m_vertexGeneratedCode ) + "\n" + string.Join( "\n", vertexInstructions );
			string fragmentCode = string.Join( "\n", m_fragmentGeneratedCode ) + "\n" + string.Join( "\n", fragmentInstructions );

			foreach ( BorrowedSamplerData data in m_borrowedSamplers.Values )
			{
				bool onVertex = vertexCode.Contains( data.Token );
				bool onFragment = fragmentCode.Contains( data.Token );
				if ( !onVertex && !onFragment )
				{
					continue;
				}

				bool donorSurvives = ( !onVertex || IsPropertyUsed( vertexCode, data.DonorProperty ) ) &&
									( !onFragment || IsPropertyUsed( fragmentCode, data.DonorProperty ) );
				if ( donorSurvives )
				{
					m_borrowedSamplerValues.Add( data.Token, data.DonorSampler );
				}
				else
				{
					data.OnVertexProgram = onVertex;
					m_borrowedSamplerValues.Add( data.Token, data.FallbackSampler );
					m_borrowedSamplerFallbacks.Add( data );

					if ( data.DeclareFallback )
					{
						string format = UIUtils.CurrentWindow.OutsideGraph.IsSRP ? Constants.SamplerDeclSRPFormat : Constants.SamplerDeclFormat;
						AddToUniforms( data.NodeId, string.Format( format, data.FallbackSampler ) + ";" );
					}
				}
			}
		}

		// @diogo: a texture only survives compilation when its name is read as a texture, which rules out both
		// its sampler and its companion uniforms since those keep the name glued to a longer identifier
		private static bool IsPropertyUsed( string code, string property )
		{
			return Regex.IsMatch( code, @"\b" + Regex.Escape( property ) + @"\b" );
		}

		// @diogo: a warning raised while a pass generates is held until every pass is done, so one message can
		// name all the passes it affects instead of the same text repeating once per pass. Keyed by text alone,
		// since two messages that read the same are a duplicate no matter which node raised them
		public void ShowPassWarning( int nodeId, string message )
		{
			if ( m_masterNode is TemplateMultiPassMasterNode )
			{
				m_passWarnings[ message ] = nodeId;
			}
			else
			{
				UIUtils.ShowMessage( nodeId, message, MessageSeverity.Warning );
			}
		}

		public void AddASEMacros()
		{
			if( Using2DMacrosMask == MacrosMask.NONE && Using3DMacrosMask == MacrosMask.NONE && UsingCUBEMacrosMask == MacrosMask.NONE && Using2DArrayMacrosMask == MacrosMask.NONE )
				return;

			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;

			//Debug.Log( UsingMacrosMask );
			AddToDirectives( Constants.CustomASEStandarSamplingMacrosHelper[ 0 ], 1 );

			if( ( Using2DMacrosMask & MacrosMask.AUTO ) == MacrosMask.AUTO )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 0 ], 1 );
			if( ( Using2DMacrosMask & MacrosMask.LOD ) == MacrosMask.LOD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 1 ], 1 );
			if( ( Using2DMacrosMask & MacrosMask.BIAS ) == MacrosMask.BIAS )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 2 ], 1 );
			if( ( Using2DMacrosMask & MacrosMask.GRAD ) == MacrosMask.GRAD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 3 ], 1 );

			if( ( Using3DMacrosMask & MacrosMask.AUTO ) == MacrosMask.AUTO )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 4 ], 1 );
			if( ( Using3DMacrosMask & MacrosMask.LOD ) == MacrosMask.LOD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 5 ], 1 );
			if( ( Using3DMacrosMask & MacrosMask.BIAS ) == MacrosMask.BIAS )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 6 ], 1 );
			if( ( Using3DMacrosMask & MacrosMask.GRAD ) == MacrosMask.GRAD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 7 ], 1 );

			if( ( UsingCUBEMacrosMask & MacrosMask.AUTO ) == MacrosMask.AUTO )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 8 ], 1 );
			if( ( UsingCUBEMacrosMask & MacrosMask.LOD ) == MacrosMask.LOD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 9 ], 1 );
			if( ( UsingCUBEMacrosMask & MacrosMask.BIAS ) == MacrosMask.BIAS )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 10 ], 1 );
			if( ( UsingCUBEMacrosMask & MacrosMask.GRAD ) == MacrosMask.GRAD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 11 ], 1 );

			if( ( Using2DArrayMacrosMask & MacrosMask.AUTO ) == MacrosMask.AUTO )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 12 ], 1 );
			if( ( Using2DArrayMacrosMask & MacrosMask.LOD ) == MacrosMask.LOD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 13 ], 1 );
			if( ( Using2DArrayMacrosMask & MacrosMask.BIAS ) == MacrosMask.BIAS )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 14 ], 1 );
			if( ( Using2DArrayMacrosMask & MacrosMask.GRAD ) == MacrosMask.GRAD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosRecent[ 15 ], 1 );

			AddToDirectives( Constants.CustomASEStandarSamplingMacrosHelper[ 1 ], 1 );

			if( ( Using2DMacrosMask & MacrosMask.AUTO ) == MacrosMask.AUTO )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 0 ], 1 );
			if( ( Using2DMacrosMask & MacrosMask.LOD ) == MacrosMask.LOD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 1 ], 1 );
			if( ( Using2DMacrosMask & MacrosMask.BIAS ) == MacrosMask.BIAS )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 2 ], 1 );
			if( ( Using2DMacrosMask & MacrosMask.GRAD ) == MacrosMask.GRAD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 3 ], 1 );

			if( ( Using3DMacrosMask & MacrosMask.AUTO ) == MacrosMask.AUTO )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 4 ], 1 );
			if( ( Using3DMacrosMask & MacrosMask.LOD ) == MacrosMask.LOD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 5 ], 1 );
			if( ( Using3DMacrosMask & MacrosMask.BIAS ) == MacrosMask.BIAS )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 6 ], 1 );
			if( ( Using3DMacrosMask & MacrosMask.GRAD ) == MacrosMask.GRAD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 7 ], 1 );

			if( ( UsingCUBEMacrosMask & MacrosMask.AUTO ) == MacrosMask.AUTO )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 8 ], 1 );
			if( ( UsingCUBEMacrosMask & MacrosMask.LOD ) == MacrosMask.LOD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 9 ], 1 );
			if( ( UsingCUBEMacrosMask & MacrosMask.BIAS ) == MacrosMask.BIAS )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 10 ], 1 );
			if( ( UsingCUBEMacrosMask & MacrosMask.GRAD ) == MacrosMask.GRAD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 11 ], 1 );

			if( ( Using2DArrayMacrosMask & MacrosMask.AUTO ) == MacrosMask.AUTO )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 12 ], 1 );
			if( ( Using2DArrayMacrosMask & MacrosMask.LOD ) == MacrosMask.LOD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 13 ], 1 );
			if( ( Using2DArrayMacrosMask & MacrosMask.BIAS ) == MacrosMask.BIAS )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 14 ], 1 );
			if( ( Using2DArrayMacrosMask & MacrosMask.GRAD ) == MacrosMask.GRAD )
				AddToDirectives( Constants.CustomASEStandarSamplingMacrosOlder[ 15 ], 1 );

			AddToDirectives( Constants.CustomASEStandarSamplingMacrosHelper[ 2 ], 1 );
		}

		public void AddToDirectives( string value, int orderIndex = -1 , AdditionalLineType type = AdditionalLineType.Custom )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			switch( type )
			{
				case AdditionalLineType.Include:value = "#include " + value;break;
				case AdditionalLineType.Define:value = "#define " + value; break;
				case AdditionalLineType.Pragma:value = "#pragma " + value; break;
			}
			if( !m_additionalDirectivesDict.ContainsKey( value ) )
			{
				PropertyDataCollector data = new PropertyDataCollector( -1, value, orderIndex );
				m_additionalDirectivesDict.Add( value, data );
				m_additionalDirectivesList.Add( data );
				m_dirtyAdditionalDirectives = true;
			}
		}

		public void AddToIncludes( int nodeId, string value )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			if( m_includesExclusionDict.ContainsKey( value ) )
			{
				return;
			}

			if( IsTemplate )
			{
				if( m_templateDataCollector.HasDirective( AdditionalLineType.Include, value ) )
					return;
			}

			if( !m_includesDict.ContainsKey( value ) )
			{
				PropertyDataCollector data = new PropertyDataCollector( nodeId, "#include \"" + value + "\"" );
				m_includesDict.Add( value, data );
				m_includesList.Add( data );
				m_includes += "\t\t#include \"" + value + "\"\n";
				m_dirtyIncludes = true;
			}
			else
			{
				if( m_showDebugMessages ) UIUtils.ShowMessage( "AddToIncludes:Attempting to add duplicate " + value, MessageSeverity.Warning );
			}
		}


		public void RemoveFromIncludes( string value )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			if( !m_includesExclusionDict.ContainsKey( value ) )
			{
				m_includesExclusionDict.Add( value, value );
			}

			if( m_includesDict.ContainsKey( value ) )
			{
				PropertyDataCollector data = m_includesDict[ value ];
				m_includesDict.Remove( value );
				m_includesList.Remove( data );
				m_dirtyIncludes = true;
				string finalValueName = "\t\t#include \"" + value + "\"\n";
				m_includes = m_includes.Replace( finalValueName, string.Empty );
			}
		}

		//public void AddToTags( int nodeId, string name, string value )
		//{
		//	if( string.IsNullOrEmpty( name ) || string.IsNullOrEmpty( value ) )
		//		return;

		//	if( !m_tagsDict.ContainsKey( name ) )
		//	{
		//		string finalResult = string.Format( "\"{0}\"=\"{1}\"", name, value );
		//		m_tagsDict.Add( name, new PropertyDataCollector( nodeId, finalResult ) );
		//		m_tagsList.Add( new PropertyDataCollector( nodeId, finalResult ) );
		//	}
		//}

		public bool ContainsPragma( string value )
		{
			return m_pragmasDict.ContainsKey( value );
		}

		// @diogo: condition, when set, is emitted verbatim as a "#if <condition>" guard around the pragma
		// (e.g. "UNITY_VERSION >= 60000100", "UNITY_VERSION >= 60000100 && UNITY_VERSION < 60010000", "defined( FOO )")
		// so a single generated shader stays cross-compatible across SRP sub-versions (e.g. all of 17.x).
		public void AddToPragmas( int nodeId, string value, string condition = null )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			if( IsTemplate )
			{
				if( m_templateDataCollector.HasDirective( AdditionalLineType.Pragma, value ) )
					return;
			}

			if( !m_pragmasDict.ContainsKey( value ) )
			{
				bool conditional = !string.IsNullOrEmpty( condition );
				// continuation lines are left unindented on purpose: the template's tag indentation is applied per-line on output
				string finalValue = conditional
					? "#if " + condition + "\n#pragma " + value + "\n#endif"
					: "#pragma " + value;
				PropertyDataCollector dataCollector = new PropertyDataCollector( nodeId , finalValue );

				//Adding both versions to dict so check can take both into account
				m_pragmasDict.Add( value, dataCollector );
				m_pragmasDict.Add( finalValue , dataCollector );

				m_pragmasList.Add( m_pragmasDict[ value ] );
				m_pragmas += conditional
					? "\t\t#if " + condition + "\n\t\t#pragma " + value + "\n\t\t#endif\n"
					: "\t\t#pragma " + value + "\n";
				m_dirtyPragmas = true;
			}
			else
			{
				if( m_showDebugMessages ) UIUtils.ShowMessage( "AddToPragmas:Attempting to add duplicate " + value, MessageSeverity.Warning );
			}
		}

		public bool ContainsDefine( string value )
		{
			return m_definesDict.ContainsKey( value );
		}

		public void AddToDefines( int nodeId, string value, bool define = true )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			if( IsTemplate )
			{
				if( m_templateDataCollector.HasDirective( AdditionalLineType.Define, value ) )
					return;
			}

			if( !m_definesDict.ContainsKey( value ) )
			{
				string defineValue = ( define ? "#define " : "#undef " ) + value;
				PropertyDataCollector dataCollector = new PropertyDataCollector( nodeId , defineValue );

				//Adding both versions to dict so check can take both into account
				m_definesDict.Add( value, dataCollector );
				m_definesDict.Add( defineValue , dataCollector );

				m_definesList.Add( m_definesDict[ value ] );
				m_defines += "\t\t" + defineValue + "\n";
				m_dirtyDefines = true;
			}
			else
			{
				if( m_showDebugMessages ) UIUtils.ShowMessage( "AddToDefines:Attempting to add duplicate " + value, MessageSeverity.Warning );
			}
		}

		public int GetVirtualCoordinatesId( int nodeId, string coord, string lodBias )
		{
			if( !m_virtualCoordinatesDict.ContainsKey( coord ) )
			{
				m_virtualCoordinatesDict.Add( coord, nodeId );
				AddLocalVariable( nodeId, "VirtualCoord " + Constants.VirtualCoordNameStr + nodeId + " = VTComputeVirtualCoord" + lodBias + "( " + coord + " );" );
				return nodeId;
			}
			else
			{
				int fetchedId = 0;
				m_virtualCoordinatesDict.TryGetValue( coord, out fetchedId );
				return fetchedId;
			}
		}

		public bool AddToLocalVariables( MasterNodePortCategory category, int nodeId, PrecisionType precisionType, WirePortDataType type, string varName, string varValue )
		{
			if( string.IsNullOrEmpty( varName ) || string.IsNullOrEmpty( varValue ) )
				return false;

			string value = UIUtils.PrecisionWirePortToCgType( precisionType, type ) + " " + varName + " = " + varValue + ";";
			return AddToLocalVariables( category, nodeId, value, varName: varName );
		}

		public bool AddToLocalVariables( int nodeId, PrecisionType precisionType, WirePortDataType type, string varName, string varValue )
		{
			if( string.IsNullOrEmpty( varName ) || string.IsNullOrEmpty( varValue ) )
				return false;

			string value = UIUtils.PrecisionWirePortToCgType( precisionType, type ) + " " + varName + " = " + varValue + ";";
			return AddToFragmentLocalVariables( nodeId, value, varName: varName );
		}

		public bool AddToLocalVariables( MasterNodePortCategory category, int nodeId, string value, bool ignoreDuplicates = false, string varName = "" )
		{
			if( string.IsNullOrEmpty( value ) )
				return false;

			switch( category )
			{
				case MasterNodePortCategory.Vertex:
				case MasterNodePortCategory.Tessellation:
				{
					return AddToVertexLocalVariables( nodeId, value, ignoreDuplicates, varName );
				}
				case MasterNodePortCategory.Fragment:
				case MasterNodePortCategory.Debug:
				{
					return AddToFragmentLocalVariables( nodeId, value, ignoreDuplicates, varName );
				}
			}

			return false;
		}

		public bool AddLocalVariable( int nodeId, string customType, string varName, string varValue )
		{
			if( string.IsNullOrEmpty( varName ) || string.IsNullOrEmpty( varValue ) )
				return false;

			string value = customType + " " + varName + " = " + varValue + ";";
			return AddLocalVariable( nodeId, value, varName: varName );
		}

		private bool ValidadeLocalVariable( PrecisionType precisionType, WirePortDataType type, string varName, string varValue, ref string result )
		{
			Array enumValues = Enum.GetValues( typeof( PrecisionType ) );
			foreach( PrecisionType currPrecision in enumValues)
			{
				string value = UIUtils.PrecisionWirePortToTypeValue( currPrecision, type, varName ) + " = " + varValue + ";";
				if( HasLocalVariable( value ) )
					return false;

				if( precisionType == currPrecision )
					result = value;
			}

			return true;
		}

		public bool AddLocalVariable( int nodeId, PrecisionType precisionType, WirePortDataType type, string varName, string varValue )
		{
			if ( string.IsNullOrEmpty( varName ) || string.IsNullOrEmpty( varValue ) )
			{
				return false;
			}

			string value = string.Empty;
			if ( ValidadeLocalVariable( precisionType, type, varName, varValue, ref value ) )
			{
				return AddLocalVariable( nodeId, value, varName: varName );
			}
			return false;
		}

		public bool AddLocalVariable( int nodeId, string varName, string varValue, bool ignoreDuplicates = false, bool addSemiColon = false )
		{
			string finalValue = addSemiColon ? varName + " = " + varValue + ";" : varName + " = " + varValue;
			return AddLocalVariable( nodeId, finalValue, ignoreDuplicates, varName: varName );
		}

		public bool AddLocalVariable( int nodeId, string value, bool ignoreDuplicates = false, string varName = "" )
		{
			if ( string.IsNullOrEmpty( value ) )
			{
				return false;
			}

			switch ( m_portCategory )
			{
				case MasterNodePortCategory.Vertex:
				case MasterNodePortCategory.Tessellation:
				{
					return AddToVertexLocalVariables( nodeId, value, ignoreDuplicates, varName );
				}
				case MasterNodePortCategory.Fragment:
				case MasterNodePortCategory.Debug:
				{
					return AddToFragmentLocalVariables( nodeId, value, ignoreDuplicates, varName );
				}
			}

			return false;
		}

		public string AddVirtualLocalVariable( int nodeId, string variable, string value )
		{
			if( string.IsNullOrEmpty( value ) )
				return string.Empty;

			string result = string.Empty;

			if( !m_virtualVariablesDict.ContainsKey( value ) )
			{
				m_virtualVariablesDict.Add( value, variable );
				result = variable;
			}
			else
			{
				m_virtualVariablesDict.TryGetValue( value, out result );
			}
			return result;
		}

		public void AddCodeComments( bool forceForwardSlash, params string[] comments )
		{
			if( m_portCategory == MasterNodePortCategory.Tessellation || m_portCategory == MasterNodePortCategory.Vertex )
			{
				AddToVertexLocalVariables( 0, IOUtils.CreateCodeComments( forceForwardSlash, comments ) );
			}
			else
			{
				AddToFragmentLocalVariables( 0, IOUtils.CreateCodeComments( forceForwardSlash, comments ) );
			}
		}

		// @diogo: collector currently emitting scoped code, so OutputPort can reach it without holding a reference (#365).
		public static MasterNodeDataCollector ActiveScopeCollector { get; private set; }

		public int PushBranchScope( bool uniformCondition = false, string preprocessorCondition = null )
		{
			if ( m_branchScopeStacks == null )
			{
				m_branchScopeStacks = new List<int>[] { new List<int>(), new List<int>() };
				m_branchScopeParents = new Dictionary<int, int>();
			}

			if ( ActiveScopeCollector != this )
			{
				if ( ActiveScopeCollector != null && ActiveScopeCollector.HasOpenBranchScope )
				{
					Debug.LogWarning( "[AmplifyShaderEditor] Opening a branch scope while another data collector is still active." );
				}
				ActiveScopeCollector = this;
			}

			List<int> stack = m_branchScopeStacks[ UIUtils.PortCategorytoAttayIdx( m_portCategory ) ];
			int scope = ++m_branchScopeCounter;
			m_branchScopeParents.Add( scope, stack.Count > 0 ? stack[ stack.Count - 1 ] : 0 );
			stack.Add( scope );

			if ( uniformCondition )
			{
				if ( m_uniformBranchScopes == null )
				{
					m_uniformBranchScopes = new HashSet<int>();
				}
				m_uniformBranchScopes.Add( scope );
			}

			if ( preprocessorCondition != null )
			{
				if ( m_branchScopeConditions == null )
				{
					m_branchScopeConditions = new Dictionary<int, string>();
				}
				m_branchScopeConditions.Add( scope, preprocessorCondition );
			}

			return scope;
		}

		// Conjunction of the open fragment scopes' preprocessor conditions, outermost first; empty when unconditional
		public List<string> GetFragmentPreprocessorConditions()
		{
			List<string> conditions = new List<string>();
			if ( m_branchScopeStacks != null && m_branchScopeConditions != null )
			{
				List<int> stack = m_branchScopeStacks[ FragmentScopeSide ];
				for ( int i = 0; i < stack.Count; i++ )
				{
					if ( m_branchScopeConditions.TryGetValue( stack[ i ], out string condition ) )
					{
						conditions.Add( condition );
					}
				}
			}
			return conditions;
		}

		// @diogo: a uniform scope keeps the quad together, but a divergent one anywhere in the open stack
		// still makes the flow divergent for everything nested inside it (#365)
		private bool InsideDivergentFragmentScope
		{
			get
			{
				bool divergent = false;
				if ( m_branchScopeStacks != null )
				{
					List<int> stack = m_branchScopeStacks[ FragmentScopeSide ];
					for ( int i = 0; i < stack.Count && !divergent; i++ )
					{
						divergent = m_uniformBranchScopes == null || !m_uniformBranchScopes.Contains( stack[ i ] );
					}
				}
				return divergent;
			}
		}

		public void PopBranchScope()
		{
			if ( m_branchScopeStacks == null )
			{
				return;
			}

			List<int> stack = m_branchScopeStacks[ UIUtils.PortCategorytoAttayIdx( m_portCategory ) ];
			if ( stack.Count > 0 )
			{
				stack.RemoveAt( stack.Count - 1 );
			}

			// @diogo: stays registered after the last pop so the port safety net covers post-branch reads; cleared on Destroy (#365).
		}

		public void PushHazardDeclarations()
		{
			m_hazardDeclarationDepth++;
		}

		public void PopHazardDeclarations()
		{
			m_hazardDeclarationDepth--;
		}

		// @diogo: while pre-generating above a conditional, a derivative-dependent value must declare even at
		// fan-out 1, otherwise it inlines and its text lands inside the branch braces instead (#365)
		public bool RequiresHazardDeclaration( ParentNode node )
		{
			return m_hazardDeclarationDepth > 0 && BranchScopeUtils.IsDerivativeDependent( node );
		}

		public int CurrentBranchScope { get { return CurrentBranchScopeOnSide( UIUtils.PortCategorytoAttayIdx( m_portCategory ) ); } }

		public bool InsideBranchScope { get { return CurrentBranchScope != 0; } }

		public int CurrentBranchScopeOnCategory( MasterNodePortCategory category )
		{
			return CurrentBranchScopeOnSide( UIUtils.PortCategorytoAttayIdx( category ) );
		}

		// @diogo: one extra tab per open branch scope; surface-path sinks keep it, template paths strip and recompute it (#365).
		private string BranchScopeIndentOnSide( int sideIdx )
		{
			if ( m_branchScopeStacks == null || m_branchScopeStacks[ sideIdx ].Count == 0 )
			{
				return string.Empty;
			}

			return new string( '\t', m_branchScopeStacks[ sideIdx ].Count );
		}

		private bool HasOpenBranchScope { get { return m_branchScopeStacks != null && ( m_branchScopeStacks[ VertexScopeSide ].Count > 0 || m_branchScopeStacks[ FragmentScopeSide ].Count > 0 ); } }

		private int CurrentBranchScopeOnSide( int sideIdx )
		{
			int scope = 0;
			if ( m_branchScopeStacks != null )
			{
				List<int> stack = m_branchScopeStacks[ sideIdx ];
				if ( stack.Count > 0 )
				{
					scope = stack[ stack.Count - 1 ];
				}
			}
			return scope;
		}

		internal bool IsBranchScopeVisibleFrom( int candidateScope, int fromScope )
		{
			bool visible = candidateScope == 0 || candidateScope == fromScope;
			if ( !visible && m_branchScopeParents != null )
			{
				int ancestor = fromScope;
				while ( !visible && m_branchScopeParents.TryGetValue( ancestor, out int parent ) && parent != 0 )
				{
					visible = parent == candidateScope;
					ancestor = parent;
				}
			}
			return visible;
		}

		public void NotifyDivergenceHazard( int nodeId )
		{
			if ( !InsideDivergentFragmentScope )
			{
				return;
			}

			if ( m_divergenceHazardNodes == null )
			{
				m_divergenceHazardNodes = new HashSet<int>();
			}

			if ( m_divergenceHazardNodes.Add( nodeId ) )
			{
				UIUtils.ShowMessage( nodeId, "A derivative-dependent instruction was emitted inside a runtime branch scope and the hazard scan missed it. The generated shader may fail to compile.", MessageSeverity.Warning );
			}
		}

		internal void NotifyOutOfScopeLocalValue( int nodeId )
		{
			if ( m_outOfScopeValueNodes == null )
			{
				m_outOfScopeValueNodes = new HashSet<int>();
			}

			if ( m_outOfScopeValueNodes.Add( nodeId ) )
			{
				UIUtils.ShowMessage( nodeId, "An output value was referenced outside the branch scope it was generated in and is being regenerated. The shader is still correct but code is duplicated.", MessageSeverity.Warning );
			}
		}

		// @diogo: a line without a scope record only ever landed at root, so it is visible from everywhere (#365).
		private bool IsLineVisibleFromScope( Dictionary<string, List<int>> lineScopes, string value, int sideIdx )
		{
			bool visible = true;
			if ( lineScopes != null && lineScopes.TryGetValue( value, out List<int> scopes ) )
			{
				visible = false;
				int fromScope = CurrentBranchScopeOnSide( sideIdx );
				for ( int i = 0; i < scopes.Count && !visible; i++ )
				{
					visible = IsBranchScopeVisibleFrom( scopes[ i ], fromScope );
				}
			}
			return visible;
		}

		// @diogo: seeds root for lines added before this sink had any record, otherwise they would read as scope-only (#365).
		private void RecordLineScope( ref Dictionary<string, List<int>> lineScopes, string value, int scope, bool wasAlreadyPresent )
		{
			if ( lineScopes == null )
			{
				if ( scope == 0 )
				{
					return;
				}
				lineScopes = new Dictionary<string, List<int>>();
			}

			if ( !lineScopes.TryGetValue( value, out List<int> scopes ) )
			{
				scopes = new List<int>();
				if ( wasAlreadyPresent )
				{
					scopes.Add( 0 );
				}
				lineScopes.Add( value, scopes );
			}

			if ( !scopes.Contains( scope ) )
			{
				scopes.Add( scope );
			}
		}

		public bool HasLocalVariable( string value )
		{
			return HasLocalVariable( value, m_portCategory );
		}

		public bool HasLocalVariable( string value, MasterNodePortCategory category )
		{
			switch( category )
			{
				case MasterNodePortCategory.Vertex:
				case MasterNodePortCategory.Tessellation:
				{
					return m_vertexLocalVariablesDict.ContainsKey( value ) && IsLineVisibleFromScope( m_vertexLocalVariablesScopes, value, VertexScopeSide );
				}
				case MasterNodePortCategory.Fragment:
				case MasterNodePortCategory.Debug:
				{
					if( m_usingCustomOutput )
					{
						return m_customOutputDict.ContainsKey( value ) && IsLineVisibleFromScope( m_customOutputScopes, value, FragmentScopeSide );
					}
					else
					{
						return m_localVariablesDict.ContainsKey( value ) && IsLineVisibleFromScope( m_localVariablesScopes, value, FragmentScopeSide );
					}
				}
			}
			return false;
		}

		// Whether a local of this name was declared on the side of the category, in any branch scope
		public bool HasLocalVariableDeclared( string varName, MasterNodePortCategory category )
		{
			switch ( category )
			{
				case MasterNodePortCategory.Vertex:
				case MasterNodePortCategory.Tessellation:
				{
					return m_vertexLocalVariablesByNameDict.ContainsKey( varName );
				}
				case MasterNodePortCategory.Fragment:
				case MasterNodePortCategory.Debug:
				{
					return m_usingCustomOutput ? m_customOutputByNameDict.ContainsKey( varName ) : m_localVariablesByNameDict.ContainsKey( varName );
				}
			}
			return false;
		}

		public bool HasLocalVariableByName( string varName )
		{
			return HasLocalVariableByName( varName, m_portCategory );
		}

		public bool HasLocalVariableByName( string varName, MasterNodePortCategory category )
		{
			switch ( category )
			{
				case MasterNodePortCategory.Vertex:
				case MasterNodePortCategory.Tessellation:
				{
					return m_vertexLocalVariablesByNameDict.TryGetValue( varName, out PropertyDataCollector vertexVar ) && IsLineVisibleFromScope( m_vertexLocalVariablesScopes, vertexVar.PropertyName, VertexScopeSide );
				}
				case MasterNodePortCategory.Fragment:
				case MasterNodePortCategory.Debug:
				{
					if ( m_usingCustomOutput )
					{
						return m_customOutputByNameDict.TryGetValue( varName, out PropertyDataCollector customVar ) && IsLineVisibleFromScope( m_customOutputScopes, customVar.PropertyName, FragmentScopeSide );
					}
					else
					{
						return m_localVariablesByNameDict.TryGetValue( varName, out PropertyDataCollector fragmentVar ) && IsLineVisibleFromScope( m_localVariablesScopes, fragmentVar.PropertyName, FragmentScopeSide );
					}
				}
			}
			return false;
		}

		public bool AddToFragmentLocalVariables( int nodeId, string value, bool ignoreDuplicates = false, string varName = "" )
		{
			if ( string.IsNullOrEmpty( value ) )
			{
				return false;
			}

			if ( m_usingCustomOutput )
			{
				if ( !m_customOutputDict.ContainsKey( value ) || ignoreDuplicates || !IsLineVisibleFromScope( m_customOutputScopes, value, FragmentScopeSide ) )
				{
					bool alreadyPresent = m_customOutputDict.TryGetValue( value, out PropertyDataCollector property );
					if ( !alreadyPresent )
					{
						property = new PropertyDataCollector( nodeId, value );
						m_customOutputDict.Add( value, property );
					}

					if ( !string.IsNullOrEmpty( varName ) && !m_customOutputByNameDict.ContainsKey( varName ) )
					{
						m_customOutputByNameDict.Add( varName, property );
					}

					m_customOutputList.Add( m_customOutputDict[ value ] );
					m_customOutput.Append( "\t\t\t" ).Append( BranchScopeIndentOnSide( FragmentScopeSide ) ).Append( value ).Append( '\n' );
					RecordLineScope( ref m_customOutputScopes, value, CurrentBranchScopeOnSide( FragmentScopeSide ), alreadyPresent );
					return true;
				}
				else
				{
					if ( m_showDebugMessages ) UIUtils.ShowMessage( "AddToLocalVariables:Attempting to add duplicate " + value, MessageSeverity.Warning );
				}
			}
			else
			{
				if ( !m_localVariablesDict.ContainsKey( value ) || ignoreDuplicates || !IsLineVisibleFromScope( m_localVariablesScopes, value, FragmentScopeSide ) )
				{
					bool alreadyPresent = m_localVariablesDict.TryGetValue( value, out PropertyDataCollector property );
					if ( !alreadyPresent )
					{
						property = new PropertyDataCollector( nodeId, value );
						m_localVariablesDict.Add( value, property );
					}

					if ( !string.IsNullOrEmpty( varName ) && !m_localVariablesByNameDict.ContainsKey( varName ) )
					{
						m_localVariablesByNameDict.Add( varName, property );
					}

					m_localVariablesList.Add( m_localVariablesDict[ value ] );
					RecordLineScope( ref m_localVariablesScopes, value, CurrentBranchScopeOnSide( FragmentScopeSide ), alreadyPresent );
					AddToSpecialLocalVariables( nodeId, value, ignoreDuplicates );
					return true;
				}
				else
				{
					if ( m_showDebugMessages ) UIUtils.ShowMessage( "AddToLocalVariables:Attempting to add duplicate " + value, MessageSeverity.Warning );
				}
			}
			return false;
		}

		public void AddToSpecialLocalVariables( int nodeId, string value, bool ignoreDuplicates = false, string varName = "" )
		{
			if ( string.IsNullOrEmpty( value ) )
				return;

			if ( m_usingCustomOutput )
			{
				if ( !m_customOutputDict.ContainsKey( value ) || ignoreDuplicates || !IsLineVisibleFromScope( m_customOutputScopes, value, FragmentScopeSide ) )
				{
					bool alreadyPresent = m_customOutputDict.TryGetValue( value, out PropertyDataCollector property );
					if ( !alreadyPresent )
					{
						property = new PropertyDataCollector( nodeId, value );
						m_customOutputDict.Add( value, property );
					}

					if ( !string.IsNullOrEmpty( varName ) && !m_customOutputByNameDict.ContainsKey( varName ) )
					{
						m_customOutputByNameDict.Add( varName, property );
					}

					m_customOutputList.Add( m_customOutputDict[ value ] );
					m_customOutput.Append( "\t\t\t" ).Append( BranchScopeIndentOnSide( FragmentScopeSide ) ).Append( value ).Append( '\n' );
					RecordLineScope( ref m_customOutputScopes, value, CurrentBranchScopeOnSide( FragmentScopeSide ), alreadyPresent );
					m_dirtySpecialLocalVariables = true;
				}
				else
				{
					if ( m_showDebugMessages ) UIUtils.ShowMessage( "AddToSpecialLocalVariables:Attempting to add duplicate " + value, MessageSeverity.Warning );
				}
			}
			else
			{
				if ( !m_specialLocalVariablesDict.ContainsKey( value ) || ignoreDuplicates || !IsLineVisibleFromScope( m_specialLocalVariablesScopes, value, FragmentScopeSide ) )
				{
					bool alreadyPresent = m_specialLocalVariablesDict.TryGetValue( value, out PropertyDataCollector property );
					if ( !alreadyPresent )
					{
						property = new PropertyDataCollector( nodeId, value );
						m_specialLocalVariablesDict.Add( value, property );
					}

					if ( !string.IsNullOrEmpty( varName ) && !m_specialLocalVariablesByNameDict.ContainsKey( varName ) )
					{
						m_specialLocalVariablesByNameDict.Add( varName, property );
					}

					m_specialLocalVariablesList.Add( m_specialLocalVariablesDict[ value ] );
					m_specialLocalVariables.Append( "\t\t\t" ).Append( BranchScopeIndentOnSide( FragmentScopeSide ) ).Append( value ).Append( '\n' );
					RecordLineScope( ref m_specialLocalVariablesScopes, value, CurrentBranchScopeOnSide( FragmentScopeSide ), alreadyPresent );
					m_dirtySpecialLocalVariables = true;
				}
				else
				{
					if ( m_showDebugMessages ) UIUtils.ShowMessage( "AddToSpecialLocalVariables:Attempting to add duplicate " + value, MessageSeverity.Warning );
				}
			}
		}

		public void ClearSpecialLocalVariables()
		{
			//m_specialLocalVariablesDict.Clear();
			m_specialLocalVariables.Length = 0;
			m_dirtySpecialLocalVariables = false;
		}

		public bool AddToVertexLocalVariables( int nodeId, string varName, string varValue )
		{
			if ( string.IsNullOrEmpty( varName ) || string.IsNullOrEmpty( varValue ) )
				return false;

			string value = varName + " = " + varValue + ";";
			return AddToVertexLocalVariables( nodeId, value, varName: varName );
		}

		public bool AddToVertexLocalVariables( int nodeId, PrecisionType precisionType, WirePortDataType type, string varName, string varValue )
		{
			if ( string.IsNullOrEmpty( varName ) || string.IsNullOrEmpty( varValue ) )
				return false;

			string value = UIUtils.PrecisionWirePortToCgType( precisionType, type ) + " " + varName + " = " + varValue + ";";
			return AddToVertexLocalVariables( nodeId, value, varName: varName );
		}

		public bool AddToVertexLocalVariables( int nodeId, string value, bool ignoreDuplicates = false, string varName = "" )
		{
			if ( string.IsNullOrEmpty( value ) )
				return false;

			if ( !m_vertexLocalVariablesDict.ContainsKey( value ) || ignoreDuplicates || !IsLineVisibleFromScope( m_vertexLocalVariablesScopes, value, VertexScopeSide ) )
			{
				bool alreadyPresent = m_vertexLocalVariablesDict.TryGetValue( value, out PropertyDataCollector property );
				if ( !alreadyPresent )
				{
					property = new PropertyDataCollector( nodeId, value );
					m_vertexLocalVariablesDict.Add( value, property  );
				}

				if ( !string.IsNullOrEmpty( varName ) && !m_vertexLocalVariablesByNameDict.ContainsKey( varName ) )
				{
					m_vertexLocalVariablesByNameDict.Add( varName, property );
				}

				m_vertexLocalVariablesList.Add( m_vertexLocalVariablesDict[ value ] );
				m_vertexLocalVariables.Append( "\t\t\t" ).Append( BranchScopeIndentOnSide( VertexScopeSide ) ).Append( value ).Append( '\n' );
				RecordLineScope( ref m_vertexLocalVariablesScopes, value, CurrentBranchScopeOnSide( VertexScopeSide ), alreadyPresent );
				m_dirtyVertexLocalVariables = true;
				return true;
			}
			else
			{
				if ( m_showDebugMessages ) UIUtils.ShowMessage( "AddToVertexLocalVariables:Attempting to add duplicate " + value, MessageSeverity.Warning );
			}

			return false;
		}

		public void ClearVertexLocalVariables()
		{
			//m_vertexLocalVariablesDict.Clear();
			m_vertexLocalVariables.Length = 0;
			m_dirtyVertexLocalVariables = false;
		}


		public bool CheckFunction( string header )
		{
			return m_localFunctions.ContainsKey( header );
		}

		public string AddFunctions( string header, string body, params object[] inParams )
		{
			if( !m_localFunctions.ContainsKey( header ) )
			{
				m_localFunctions.Add( header, body );
				// @diogo: FunctionsList feeds templates, which supply their own tag indent; the IOUtils
				// function builders hardcode the surface indent, so re-derive it from brace depth (#365).
				m_functionsList.Add( new PropertyDataCollector( -1, IOUtils.ReindentByBraceDepth( body ) ) );
				m_functions += "\n" + body + "\n";
				m_dirtyFunctions = true;
			}

			return String.Format( header, inParams );
		}

		public string AddFunctions( string header, string[] bodyLines, bool addNewLine, params object[] inParams )
		{
			if( !m_localFunctions.ContainsKey( header ) )
			{
				string body = string.Empty;
				for( int i = 0; i < bodyLines.Length; i++ )
				{
					body += ( m_masterNodeCategory == AvailableShaderTypes.Template ) ? bodyLines[ i ] : "\t\t" + bodyLines[ i ];
					if( addNewLine )
						body += '\n';
				}

				m_localFunctions.Add( header, body );
				m_functionsList.Add( new PropertyDataCollector( -1, body ) );
				m_functions += "\n" + body + "\n";
				m_dirtyFunctions = true;
			}

			return String.Format( header, inParams );
		}

		public bool HasFunction( string functionId )
		{
			return m_localFunctions.ContainsKey( functionId );
		}

		public bool HasStruct( string typeName )
		{
			return m_localStructs.ContainsKey( typeName );
		}

		// @diogo: a struct declaration rides the functions region: it is the last thing written before
		// the functions that would take the type, and every template already carries that tag. Inserted
		// ahead of the collected functions rather than appended, for the same reason
		public bool AddStruct( int nodeId, string typeName, string declaration )
		{
			if ( string.IsNullOrEmpty( typeName ) || string.IsNullOrEmpty( declaration ) )
			{
				return false;
			}

			string collected;
			if ( m_localStructs.TryGetValue( typeName, out collected ) )
			{
				// @diogo: one name cannot cover two bodies - both would be emitted and neither compiles
				return collected.Equals( declaration );
			}

			m_localStructs.Add( typeName, declaration );

			string body = IOUtils.ReindentByBraceDepth( declaration );
			m_functionsList.Insert( m_localStructs.Count - 1, new PropertyDataCollector( nodeId, body ) );
			m_structs += "\n\t\t" + body.Replace( "\n", "\n\t\t" ) + "\n";
			m_dirtyFunctions = true;
			return true;
		}

		public void AddFunction( string functionId, string body )
		{
			if( !m_localFunctions.ContainsKey( functionId ) )
			{
				// @diogo: see AddFunctions - templates need the hardcoded surface indent re-derived (#365).
				m_functionsList.Add( new PropertyDataCollector( -1, IOUtils.ReindentByBraceDepth( body ) ) );

				m_localFunctions.Add( functionId, body );
				m_functions += "\n" + body + "\n";
				m_dirtyFunctions = true;
			}
		}

		public void AddFunction( string functionId, string[] bodyLines, bool addNewline )
		{
			if( !m_localFunctions.ContainsKey( functionId ) )
			{
				string body = string.Empty;
				for( int i = 0; i < bodyLines.Length; i++ )
				{
					body += ( m_masterNodeCategory == AvailableShaderTypes.Template ) ? bodyLines[ i ] : "\t\t" + bodyLines[ i ];
					if( addNewline )
						body += '\n';

				}
				m_functionsList.Add( new PropertyDataCollector( -1, body ) );

				m_localFunctions.Add( functionId, body );
				m_functions += "\n" + body + "\n";
				m_dirtyFunctions = true;
			}
		}

		public void AddInstructions( string value, bool addTabs = false, bool addLineEnding = false )
		{
			m_instructionsList.Add( new PropertyDataCollector( -1, value ) );
			m_instructions += addTabs ? "\t\t\t" + value : value;
			if( addLineEnding )
			{
				m_instructions += '\n';
			}
			m_dirtyInstructions = true;
		}


		public void AddInstructions( bool addLineEnding, bool addTabs, params string[] values )
		{
			for( int i = 0; i < values.Length; i++ )
			{
				m_instructionsList.Add( new PropertyDataCollector( -1, values[ i ] ) );
				m_instructions += addTabs ? "\t\t\t" + values[ i ] : values[ i ];
				if( addLineEnding )
				{
					m_instructions += '\n';
				}
			}
			m_dirtyInstructions = true;
		}



		public void AddToStartInstructions( string value )
		{
			if( string.IsNullOrEmpty( value ) )
				return;

			m_instructions = value + m_instructions;
			m_dirtyInstructions = true;
		}

		public void ResetInstructions()
		{
			m_instructionsList.Clear();
			m_instructions = string.Empty;
			m_dirtyInstructions = false;
		}


		public void ResetVertexInstructions()
		{
			m_vertexDataList.Clear();
			m_vertexData = string.Empty;
			m_dirtyPerVertexData = false;
		}

		public void AddPropertyNode( PropertyNode node )
		{
			if( !m_propertyNodes.ContainsKey( node.UniqueId ) )
			{
				m_propertyNodes.Add( node.UniqueId, node );
			}
		}

		public void UpdateMaterialOnPropertyNodes( Material material )
		{
			m_masterNode.UpdateMaterial( material );
			foreach( KeyValuePair<int, PropertyNode> kvp in m_propertyNodes )
			{
				kvp.Value.UpdateMaterial( material );
			}
		}

		public void AddToVertexInput( string value )
		{
			if( !m_vertexInputDict.ContainsKey( value ) )
			{
				m_vertexInputDict.Add( value, value );
				m_vertexInputList.Add( value );
			}
		}

		public void AddToInterpolators( string value )
		{
			if( !m_interpolatorsDict.ContainsKey( value ) )
			{
				m_interpolatorsDict.Add( value, value );
				m_interpolatorsList.Add( value );
			}
		}

		public void AddToVertexInterpolatorsDecl( string value )
		{
			if( !m_vertexInterpDeclDict.ContainsKey( value ) )
			{
				m_vertexInterpDeclDict.Add( value, value );
				m_vertexInterpDeclList.Add( value );
			}
		}

		public void UpdateShaderImporter( ref Shader shader )
		{
			string path = AssetDatabase.GetAssetPath( shader );
			ShaderImporter importer = (ShaderImporter)ShaderImporter.GetAtPath( path );
			if( m_propertyNodes.Count > 0 )
			{
				try
				{
					bool hasContents = false;
					TextureDefaultsDataColector defaultCol = new TextureDefaultsDataColector();
					foreach( KeyValuePair<int, PropertyNode> kvp in m_propertyNodes )
					{
						hasContents = kvp.Value.UpdateShaderDefaults( ref shader, ref defaultCol ) || hasContents;
					}

					if( hasContents )
					{
						importer.SetDefaultTextures( defaultCol.NamesArr, defaultCol.ValuesArr );
						defaultCol.Destroy();
						defaultCol = null;
					}
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
			}
			if ( AmplifyShaderEditorWindow.IsBatchProcessing )
			{
				// During a batch resave, SaveAndReimport would force an immediate per-shader asset import,
				// flickering Unity's "Importing assets" dialog once per shader. Persist any importer change
				// ( default textures ) to the .meta without reimporting; the batch's StopAssetEditing imports
				// all the resaved shaders together in a single pass at the end.
				AssetDatabase.WriteImportSettingsIfDirty( path );
			}
			else
			{
				importer.SaveAndReimport();
			}
		}

		public void AddCustomAppData( string value )
		{
			if( m_customAppDataItemsDict.ContainsKey( value ) )
				return;

			m_customAppDataItemsDict.Add( value, value );
			m_customAppDataItems += "\t\t\t" + value + "\n";
			m_dirtyAppData = true;
		}

		public void ForceCustomAppDataUsage()
		{
			m_dirtyAppData = true;
		}

		//public string CustomAppDataName { get { return m_dirtyAppData ? Constants.CustomAppDataFullName : Constants.AppDataFullName; } }
		public string SurfaceVertexStructure { get { return ( m_dirtyAppData ? Constants.CustomAppDataFullName : Constants.AppDataFullName ); } }
		public string CustomAppData
		{
			get
			{
				if( m_dirtyPerVertexData )
				{
					return Constants.CustomAppDataFullBody + m_customAppDataItems + "\t\t};\n";
				}
				else if ( m_dirtyAppData )
				{
					return Constants.CustomAppDataFullBody + "\t\t};\n";
				}

				return string.Empty;
			}
		}

		public void Destroy()
		{
			m_masterNode = null;

			m_customAppDataItemsDict.Clear();
			m_customAppDataItemsDict = null;

			m_inputList.Clear();
			m_inputList = null;

			m_customInputList.Clear();
			m_customInputList = null;

			m_propertiesList.Clear();
			m_propertiesList = null;

			m_instancedPropertiesList.Clear();
			m_instancedPropertiesList = null;

			m_uniformsList.Clear();
			m_uniformsList = null;

			m_additionalDirectivesList.Clear();
			m_additionalDirectivesList = null;

			m_includesList.Clear();
			m_includesList = null;

			//m_tagsList.Clear();
			//m_tagsList = null;

			m_pragmasList.Clear();
			m_pragmasList = null;

			m_definesList.Clear();
			m_definesList = null;

			m_instructionsList.Clear();
			m_instructionsList = null;

			m_localVariablesList.Clear();
			m_localVariablesList = null;

			m_vertexLocalVariablesList.Clear();
			m_vertexLocalVariablesList = null;

			m_specialLocalVariablesList.Clear();
			m_specialLocalVariablesList = null;

			m_vertexDataList.Clear();
			m_vertexDataList = null;

			m_customOutputList.Clear();
			m_customOutputList = null;

			m_functionsList.Clear();
			m_functionsList = null;

			m_grabPassList.Clear();
			m_grabPassList = null;

			m_aboveUsePassesList.Clear();
			m_aboveUsePassesList = null;

			m_belowUsePassesList.Clear();
			m_belowUsePassesList = null;

			m_grabPassDict.Clear();
			m_grabPassDict = null;

			m_usePassesDict.Clear();
			m_usePassesDict = null;

			m_propertyNodes.Clear();
			m_propertyNodes = null;

			m_inputDict.Clear();
			m_inputDict = null;

			m_customInputDict.Clear();
			m_customInputDict = null;

			m_propertiesDict.Clear();
			m_propertiesDict = null;

			m_instancedPropertiesDict.Clear();
			m_instancedPropertiesDict = null;

			m_uniformsDict.Clear();
			m_uniformsDict = null;

			m_softRegisteredUniformsDict.Clear();
			m_softRegisteredUniformsDict = null;

			m_includesDict.Clear();
			m_includesDict = null;

			m_additionalDirectivesDict.Clear();
			m_additionalDirectivesDict = null;

			m_includesExclusionDict.Clear();
			m_includesExclusionDict = null;
			//m_tagsDict.Clear();
			//m_tagsDict = null;

			m_pragmasDict.Clear();
			m_pragmasDict = null;

			m_definesDict.Clear();
			m_definesDict = null;

			m_virtualCoordinatesDict.Clear();
			m_virtualCoordinatesDict = null;

			m_virtualVariablesDict.Clear();
			m_virtualVariablesDict = null;

			m_localVariablesDict.Clear();
			m_localVariablesDict = null;

			m_localVariablesByNameDict.Clear();
			m_localVariablesByNameDict = null;

			m_specialLocalVariablesDict.Clear();
			m_specialLocalVariablesDict = null;

			m_specialLocalVariablesByNameDict.Clear();
			m_specialLocalVariablesByNameDict = null;

			m_vertexLocalVariablesDict.Clear();
			m_vertexLocalVariablesDict = null;

			m_vertexLocalVariablesByNameDict.Clear();
			m_vertexLocalVariablesByNameDict = null;

			m_localFunctions.Clear();
			m_localFunctions = null;

			m_localStructs.Clear();
			m_localStructs = null;

			m_vertexDataDict.Clear();
			m_vertexDataDict = null;

			m_customOutputDict.Clear();
			m_customOutputDict = null;

			m_customOutputByNameDict.Clear();
			m_customOutputByNameDict = null;

			//templates
			m_vertexInputList.Clear();
			m_vertexInputList = null;

			m_vertexInputDict.Clear();
			m_vertexInputDict = null;

			m_interpolatorsList.Clear();
			m_interpolatorsList = null;

			m_interpolatorsDict.Clear();
			m_interpolatorsDict = null;

			m_vertexInterpDeclList.Clear();
			m_vertexInterpDeclList = null;

			m_vertexInterpDeclDict.Clear();
			m_vertexInterpDeclDict = null;

			m_templateDataCollector.Destroy();
			m_templateDataCollector = null;

			m_customShadowCoordsDict.Clear();
			m_customShadowCoordsDict = null;

			m_customShadowCoordsList.Clear();
			m_customShadowCoordsDict = null;

			m_packSlotsList.Clear();
			m_packSlotsList = null;

			// @diogo: a dangling ambient collector would have OutputPort validating against dead scopes (#365).
			if ( ActiveScopeCollector == this )
			{
				ActiveScopeCollector = null;
			}

			m_branchScopeStacks = null;
			m_branchScopeParents = null;
			m_uniformBranchScopes = null;
			m_branchScopeConditions = null;
			m_localVariablesScopes = null;
			m_specialLocalVariablesScopes = null;
			m_vertexLocalVariablesScopes = null;
			m_customOutputScopes = null;
			m_divergenceHazardNodes = null;
			m_outOfScopeValueNodes = null;
		}

		public string Inputs { get { return m_input; } }
		public string CustomInput { get { return m_customInput; } }
		public string Properties { get { return m_properties; } }
		public string InstanceBlockName { get { return m_instanceBlockName; } }
		public string InstancedProperties { get { return m_instancedProperties; } }
		public string Uniforms { get { return m_uniforms; } }
		public string Instructions { get { return m_instructions; } }
		public string Includes { get { return m_includes; } }
		public string Pragmas { get { return m_pragmas; } }
		public string Defines { get { return m_defines; } }
		public string LocalVariables { get { return m_localVariables; } }
		public string SpecialLocalVariables { get { return m_specialLocalVariables.ToString(); } }
		public string VertexLocalVariables { get { return m_vertexLocalVariables.ToString(); } }
		//public string VertexLocalVariablesFromList
		//{
		//	get
		//	{
		//		string result = string.Empty;
		//		int count = m_vertexLocalVariablesList.Count;
		//		for( int i = 0; i < count; i++ )
		//		{
		//			result += m_vertexLocalVariablesList[ i ].PropertyName + "\n";
		//		}
		//		return result;
		//	}
		//}
		public string VertexData { get { return m_vertexData; } }
		public string CustomOutput { get { return m_customOutput.ToString(); } }
		// @diogo: struct declarations lead, since a function that takes one is written after it
		public string Functions { get { return m_structs + m_functions; } }
		public string GrabPass { get { return m_grabPass; } }
		public bool DirtyAppData { get { return m_dirtyAppData; } }
		public bool DirtyInstructions { get { return m_dirtyInstructions; } }
		public bool DirtyUniforms { get { return m_dirtyUniforms; } }
		public bool DirtyProperties { get { return m_dirtyProperties; } }
		public bool DirtyInstancedProperties { get { return m_dirtyInstancedProperties; } }
		public bool DirtyInputs { get { return m_dirtyInputs; } }
		public bool DirtyCustomInput { get { return m_dirtyCustomInputs; } }
		public bool DirtyIncludes { get { return m_dirtyIncludes; } }
		public bool DirtyPragmas { get { return m_dirtyPragmas; } }
		public bool DirtyDefines { get { return m_dirtyDefines; } }
		public bool DirtyAdditionalDirectives { get { return m_dirtyAdditionalDirectives; } }
		public bool DirtyLocalVariables { get { return m_dirtyLocalVariables; } }
		public bool DirtyVertexVariables { get { return m_dirtyVertexLocalVariables; } }
		public bool DirtySpecialLocalVariables { get { return m_dirtySpecialLocalVariables; } }
		public bool DirtyPerVertexData { get { return m_dirtyPerVertexData; } }
		public bool DirtyFunctions { get { return m_dirtyFunctions; } }
		public bool DirtyGrabPass { get { return m_grabPassIsDirty; } }
		public int LocalVariablesAmount { get { return m_localVariablesDict.Count; } }
		public int SpecialLocalVariablesAmount { get { return m_specialLocalVariablesDict.Count; } }
		public int VertexLocalVariablesAmount { get { return m_vertexLocalVariablesDict.Count; } }
		public bool TesselationActive { set { m_tesselationActive = value; } get { return m_tesselationActive; } }


		public int AvailableVertexTempId { get { return m_availableVertexTempId++; } }
		public int AvailableFragTempId { get { return m_availableFragTempId++; } }

		/// <summary>
		/// Returns true if Normal output is being written by something else
		/// </summary>
		public bool DirtyNormal
		{
			get { return m_dirtyNormal; }
			set { m_dirtyNormal = value; }
		}

		public bool IsFragmentCategory
		{
			get { return m_portCategory == MasterNodePortCategory.Fragment || m_portCategory == MasterNodePortCategory.Debug; }
		}

		public MasterNodePortCategory PortCategory
		{
			get { return m_portCategory; }
			set { m_portCategory = value; }
		}

		public PortGenType GenType
		{
			get { return m_genType; }
			set { m_genType = value; }
		}

		public bool IsTemplate { get { return m_masterNodeCategory == AvailableShaderTypes.Template; } }

		public bool IsSRP { get { return ( TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.URP || TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP ); } }
		public bool IsURP { get { return ( TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.URP ); } }
		public bool IsHDRP { get { return ( TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP ); } }

		public AvailableShaderTypes MasterNodeCategory
		{
			get { return m_masterNodeCategory; }
			set { m_masterNodeCategory = value; }
		}

		public string CurrentPassName
		{
			get
			{
				var multiPassMasterNode = m_masterNode as TemplateMultiPassMasterNode;
				return ( multiPassMasterNode != null ) ? multiPassMasterNode.PassName : string.Empty;
			}
		}

		// Whether the current pass name contains a value, counting names it was renamed from.
		public bool CurrentPassNameContains( string value )
		{
			return m_masterNode is TemplateMultiPassMasterNode multiPassMasterNode && multiPassMasterNode.Pass != null && multiPassMasterNode.Pass.NameContains( value );
		}

		/// <summary>
		/// Forces write to Normal output when the output is not connected
		/// </summary>
		public bool ForceNormal
		{
			get { return m_forceNormal; }
			set
			{
				if( value )
				{
					if( !m_forceNormalIsDirty )
					{
						m_forceNormal = value;
						m_forceNormalIsDirty = value;
					}
				}
				else
				{
					m_forceNormal = value;
				}
			}
		}

		public bool UsingVertexColor
		{
			get { return m_usingVertexColor; }
			set { m_usingVertexColor = value; }
		}

		public bool UsingInternalData
		{
			get { return m_usingInternalData; }
			set { m_usingInternalData = value; }
		}

		public bool UsingScreenPos
		{
			get { return m_usingScreenPos; }
			set { m_usingScreenPos = value; }
		}

		public bool UsingCustomScreenPos
		{
			get { return m_usingCustomScreenPos; }
			set { m_usingCustomScreenPos = value; }
		}

		public bool UsingWorldNormal
		{
			get { return m_usingWorldNormal; }
			set { m_usingWorldNormal = value; }
		}

		public bool UsingWorldReflection
		{
			get { return m_usingWorldReflection; }
			set { m_usingWorldReflection = value; }
		}

		public bool UsingWorldPosition
		{
			get { return m_usingWorldPosition; }
			set { m_usingWorldPosition = value; }
		}

		public bool UsingViewDirection
		{
			get { return m_usingViewDirection; }
			set { m_usingViewDirection = value; }
		}

		public bool IsOutlineDataCollector
		{
			get { return m_isOutlineDataCollector; }
			set { m_isOutlineDataCollector = value; }
		}

		public bool UsingCustomOutlineColor
		{
			get { return m_usingCustomOutlineColor; }
			set { m_usingCustomOutlineColor = value; }
		}

		public bool UsingCustomOutlineWidth
		{
			get { return m_usingCustomOutlineWidth; }
			set { m_usingCustomOutlineWidth = value; }
		}

		public bool UsingCustomOutlineAlpha
		{
			get { return m_usingCustomOutlineAlpha; }
			set { m_usingCustomOutlineAlpha = value; }
		}

		public int CustomOutlineSelectedAlpha
		{
			get { return m_customOutlineSelectedAlpha; }
			set { m_customOutlineSelectedAlpha = value; }
		}

		public bool UsingCustomOutput
		{
			get { return m_usingCustomOutput; }
			set { m_usingCustomOutput = value; }
		}

		public bool UsingHigherSizeTexcoords
		{
			get { return m_usingHigherSizeTexcoords; }
			set { m_usingHigherSizeTexcoords = value; }
		}

		public bool UsingLightAttenuation
		{
			get { return m_usingLightAttenuation; }
			set { m_usingLightAttenuation = value; }
		}

		public bool UsingTextureArrays
		{
			get { return m_usingTextureArrays; }
			set { m_usingTextureArrays = value; }
		}

		public bool UsingExtraStandardMacros
		{
			get { return m_usingExtraStandardMacros; }
			set { m_usingExtraStandardMacros = value; }
		}

		public MacrosMask Using2DMacrosMask
		{
			get { return m_using2DMacrosMask; }
			set { m_using2DMacrosMask = value; }
		}

		public MacrosMask Using3DMacrosMask
		{
			get { return m_using3DMacrosMask; }
			set { m_using3DMacrosMask = value; }
		}

		public MacrosMask UsingCUBEMacrosMask
		{
			get { return m_usingCUBEMacrosMask; }
			set { m_usingCUBEMacrosMask = value; }
		}

		public MacrosMask Using2DArrayMacrosMask
		{
			get { return m_using2DArrayMacrosMask; }
			set { m_using2DArrayMacrosMask = value; }
		}

		public bool UsingExtra3DSRPMacros
		{
			get { return m_usingExtra3DSRPMacros; }
			set { m_usingExtra3DSRPMacros = value; }
		}

		public bool UsingArrayDerivatives
		{
			get { return m_usingArrayDerivatives; }
			set { m_usingArrayDerivatives = value; }
		}

		public bool SafeNormalizeLightDir
		{
			get { return m_safeNormalizeLightDir; }
			set { m_safeNormalizeLightDir = value; }
		}

		public bool SafeNormalizeViewDir
		{
			get { return m_safeNormalizeViewDir; }
			set { m_safeNormalizeViewDir = value; }
		}

		public string StandardAdditionalDirectives
		{
			get
			{
				string body = string.Empty;
				int count = m_additionalDirectivesList.Count;
				for( int i = 0; i < count; i++ )
				{
					body += "\t\t" + m_additionalDirectivesList[ i ].PropertyName + "\n";
				}
				return body;
			}
		}

		public bool SurfaceCustomShadowCaster
		{
			get { return m_surfaceCustomShadowCaster; }
			set { m_surfaceCustomShadowCaster = value; }
		}

		public bool CustomOutline { get { return UsingCustomOutlineColor || UsingCustomOutlineWidth || UsingCustomOutlineAlpha; } }
		public List<PropertyDataCollector> InputList { get { return m_inputList; } }
		public List<PropertyDataCollector> CustomInputList { get { return m_customInputList; } }
		public List<PropertyDataCollector> PropertiesList { get { return m_propertiesList; } }
		public List<PropertyDataCollector> InstancedPropertiesList { get { return m_instancedPropertiesList; } }
		public List<PropertyDataCollector> UniformsList { get { return m_uniformsList; } }
		public Dictionary<string, string> BorrowedSamplerValues { get { return m_borrowedSamplerValues; } }
		public List<BorrowedSamplerData> BorrowedSamplerFallbacks { get { return m_borrowedSamplerFallbacks; } }
		public Dictionary<string, int> PassWarnings { get { return m_passWarnings; } }
		public List<PropertyDataCollector> MiscList { get { return m_additionalDirectivesList; } }
		public List<PropertyDataCollector> BeforeNativeDirectivesList { get { return m_additionalDirectivesList.FindAll( obj => obj.OrderIndex < 0 ); } }
		public List<PropertyDataCollector> AfterNativeDirectivesList { get { return m_additionalDirectivesList.FindAll( obj => obj.OrderIndex > 0 ); } }
		public List<PropertyDataCollector> IncludesList { get { return m_includesList; } }
		//public List<PropertyDataCollector> TagsList { get { return m_tagsList; } }
		public List<PropertyDataCollector> PragmasList { get { return m_pragmasList; } }
		public List<PropertyDataCollector> DefinesList { get { return m_definesList; } }
		public List<PropertyDataCollector> InstructionsList { get { return m_instructionsList; } }
		public List<PropertyDataCollector> LocalVariablesList { get { return m_localVariablesList; } }
		public List<PropertyDataCollector> VertexLocalVariablesList { get { return m_vertexLocalVariablesList; } }
		public List<PropertyDataCollector> SpecialLocalVariablesList { get { return m_specialLocalVariablesList; } }
		public List<PropertyDataCollector> VertexDataList { get { return m_vertexDataList; } }
		public List<PropertyDataCollector> CustomOutputList { get { return m_customOutputList; } }
		public List<PropertyDataCollector> FunctionsList { get { return m_functionsList; } }
		public List<PropertyDataCollector> GrabPassList { get { return m_grabPassList; } }
		public Dictionary<string, string> GrabPassDict { get { return m_grabPassDict; } }
		public List<PropertyDataCollector> AboveUsePassesList { get { return m_aboveUsePassesList; } }
		public List<PropertyDataCollector> BelowUsePassesList { get { return m_belowUsePassesList; } }
		public Dictionary<string, string> AboveUsePassesDict { get { return m_usePassesDict; } }
		public List<InputCoordsCollector> CustomShadowCoordsList { get { return m_customShadowCoordsList; } }
		public List<int> PackSlotsList { get { return m_packSlotsList; } }
		public Dictionary<string, string> LocalFunctions { get { return m_localFunctions; } }
		//Templates
		public List<string> VertexInputList { get { return m_vertexInputList; } }
		public List<string> InterpolatorList { get { return m_interpolatorsList; } }
		public List<string> VertexInterpDeclList { get { return m_vertexInterpDeclList; } }
		public TemplateDataCollector TemplateDataCollectorInstance { get { return m_templateDataCollector; } }
		public MasterNode MasterNode { get { return m_masterNode; } }
		public RenderPath CurrentRenderPath
		{
			get { return m_renderPath; }
			set { m_renderPath = value; }
		}

		public NodeAvailability CurrentCanvasMode { get { return m_currentCanvasMode; } set { m_currentCanvasMode = value; } }
		public TemplateSRPType CurrentSRPType
		{
			get
			{
				if( IsTemplate )
					return m_templateDataCollector.CurrentSRPType;

				return TemplateSRPType.BiRP;
			}
		}

		public Dictionary<string, PropertyDataCollector> PropertiesDict { get { return m_propertiesDict; } }
	}
}

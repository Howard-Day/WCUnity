// Amplify Shader Editor - Visual Shader vEditing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Switch", "Logical Operators", "Creates a shader keyword toggle", tags: "Static Switch", Available = true )]
	public sealed class StaticSwitch : PropertyNode
	{
		public enum ShaderStage
		{
			All,
			Vertex,
			Fragment,
			Hull,
			Domain,
			Geometry,
			Raytracing
		};

		private float InstanceIconWidth = 19;
		private float InstanceIconHeight = 19;
		private readonly Color ReferenceHeaderColor = new Color( 0f, 0.5f, 0.585f, 1.0f );

		[SerializeField]
		private int m_defaultValue = 0;

		[SerializeField]
		private int m_materialValue = 0;

		[SerializeField]
		private int m_multiCompile = 0;

		[SerializeField]
		private int m_currentKeywordId = 0;

		[SerializeField]
		private string m_currentKeyword = string.Empty;

		[SerializeField]
		private bool m_materialToggle = true;

		[SerializeField]
		private bool m_lockKeyword = true;

		private const string IsLocalStr = "Is Local";
		private const string StageStr = "Stage";
		private static readonly GUIContent UnifiedBranchContent = new GUIContent( "Unified Branch", "Emits an HLSL if() on the keyword instead of #ifdef, so the generated code is identical whether the keyword is declared as shader_feature, multi_compile or dynamic_branch." );
		private static readonly GUIContent IncludeNoneContent = new GUIContent( "Include \"none\"", "Adds a trailing None entry that declares no keyword: the pragma gains the _ variant and the fallback branch reads the None input." );
		private static readonly GUIContent GenerateKeywordContent = new GUIContent( "Generate Keyword", "Emits the #pragma that declares this keyword. Disable it to generate only the branch code and declare the keyword elsewhere, e.g. through an Additional Directives node; Is Local and Stage no longer apply." );
		public const string UnifiedBranchInfoMsg = "Unified Branch is on, so the branch is emitted as if (KEYWORD) instead of #ifdef. Required for Unity 6.3 Shader Declaration Overrides to convert this keyword to dynamic branching. Shader stage is forced to All.";
		public const string UnifiedBranchSurfaceWarning = "Unified Branch is on, but Surface Shaders never resolve static keywords as identifiers, so the branch falls back to #ifdef. Use the Dynamic Branching keyword type instead.";
		private const string GenerateKeywordRuntimeWarning = "Generate Keyword is off while the branch is emitted as if (KEYWORD), so the manual declaration must give the keyword a value: use a keyword pragma or #define KEYWORD 1. A valueless #define only works with #ifdef.";

		[SerializeField]
		private bool m_isLocal = true;

		[SerializeField]
		private bool m_unifiedBranch = false;

		[SerializeField]
		private bool m_generateKeyword = true;

		[SerializeField]
		private ShaderStage m_shaderStage = ShaderStage.All;


		private GUIContent m_checkContent;
		private GUIContent m_popContent;

		private int m_conditionId = -1;

		private int m_lastSourceValue = -1;

		private const int MinComboSize = 50;
		private const int MaxComboSize = 105;

		private Rect m_varRect;
		private Rect m_imgRect;
		private bool m_editing;

		public enum KeywordModeType
		{
			Toggle = 0,
			ToggleOff = 1,
			KeywordEnum = 2,
		}

		public enum StaticSwitchVariableMode
		{
			Create = 0,
			Fetch = 1,
			Reference = 2
		}

		public enum KeywordType
		{
			ShaderFeature = 0,
			MultiCompile = 1,
			DynamicBranch = 2,
		}

		[SerializeField]
		private KeywordModeType m_keywordModeType = KeywordModeType.Toggle;

		[SerializeField]
		private StaticSwitch m_reference = null;

		private const string StaticSwitchStr = "Switch";
		private const string MaterialToggleStr = "Material Toggle";

		private const string ToggleMaterialValueStr = "Material Value";
		private const string ToggleDefaultValueStr = "Default Value";

		private const int MaxKeywordAmount = 9;
		private const string NoneEntryStr = "None";

		private const string AmountStr = "Amount";
		private const string KeywordStr = "Keyword";
		private const string CustomStr = "Custom";
		private const string ToggleTypeStr = "Toggle Type";
		private const string TypeStr = "Type";
		private const string ModeStr = "Mode";
		private const string KeywordTypeStr = "Keyword Type";

		private const string KeywordNameStr = "Keyword Name";
		public readonly static string[] KeywordTypeList = { "Shader Feature", "Multi Compile", "Dynamic Branch" };
		public readonly static int[] KeywordTypeInt = { ( int )KeywordType.ShaderFeature, ( int )KeywordType.MultiCompile, ( int )KeywordType.DynamicBranch };

		[SerializeField]
		private string[] m_defaultKeywordNames = { "Key0", "Key1", "Key2", "Key3", "Key4", "Key5", "Key6", "Key7", "Key8" };

		[SerializeField]
		private string[] m_keywordEnumList = { "Key0", "Key1" };

		[SerializeField]
		private bool m_includeNoneEntry = false;

		[SerializeField]
		private StaticSwitchVariableMode m_staticSwitchVarMode = StaticSwitchVariableMode.Create;

		[SerializeField]
		private int m_referenceArrayId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		private int m_keywordEnumAmount = 2;

		private bool m_isStaticSwitchDirty = false;

		private Rect m_iconPos;

		public const string KeywordModifierSurfaceWarning = "Stage is ignored in Surface Shaders due to a Unity bug.";
		private const string NamelessStructError = "Switch node cannot declare a struct whose name did not reach its ports.";
		private const string CustomTypeError = "Switch node does not support custom types on its inputs.";
		private const string RuntimeSamplerStateError = "Switch node cannot pick a Sampler State at runtime. Use Shader Feature or Multi Compile with Unified Branch off.";
		private const string RuntimeSamplerStateFunctionWarning = "Switch node cannot pick a Sampler State at runtime, which Unified Branch does when this function is used in a template shader.";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			AddInputPort( WirePortDataType.FLOAT, false, "False", -1, MasterNodePortCategory.Fragment, 1 );
			AddInputPort( WirePortDataType.FLOAT, false, "True", -1, MasterNodePortCategory.Fragment, 0 );
			for( int i = 2; i < 9; i++ )
			{
				AddInputPort( WirePortDataType.FLOAT, false, m_defaultKeywordNames[ i ] );
				m_inputPorts[ i ].Visible = false;
			}

			// @diogo: every arm takes its type from whatever is wired first, structs among them, and
			// says so rather than leaving it to the default that only covers what ASE can cast. A
			// texture object is bound, not assigned, so no arm can carry one either way
			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].AcceptAllTypes();
				// @diogo: a sampler state stays allowed: the #if form compiles, and generation reports the
				// runtime form, which cannot pick one
				m_inputPorts[ i ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
					WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY );
			}

			// @diogo: ports are laid out by order id so the none entry can lead without leaving
			// its trailing slot, which old graphs are still wired to
			m_sortInputPorts = true;

			m_headerColor = new Color( 0.0f, 0.55f, 0.45f, 1f );
			m_customPrefix = "Switch ";
			m_autoWrapProperties = false;
			m_freeType = false;
			m_useVarSubtitle = true;
			m_allowPropertyDuplicates = false;
			m_showTitleWhenNotEditing = false;
			m_currentParameterType = PropertyType.Property;

			m_checkContent = new GUIContent();
			m_checkContent.image = UIUtils.CheckmarkIcon;

			m_popContent = new GUIContent();
			m_popContent.image = UIUtils.PopupIcon;

			m_previewShaderGUID = "0b708c11c68e6a9478ac97fe3643eab1";
			m_showAutoRegisterUI = true;
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			return received;
		}

		// @diogo: a reference with no inputs of its own acts as a pure alias of its source
		private bool IsSourceAlias
		{
			get
			{
				if ( m_staticSwitchVarMode != StaticSwitchVariableMode.Reference || m_reference == null )
				{
					return false;
				}
				for ( int i = 0; i < m_inputPorts.Count; i++ )
				{
					if ( m_inputPorts[ i ].IsConnected )
					{
						return false;
					}
				}
				return true;
			}
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if( m_conditionId == -1 )
				m_conditionId = Shader.PropertyToID( "_Condition" );

			StaticSwitch node = ( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null ) ? m_reference : this;

			// @diogo: in reference mode the source's own material-toggle state decides which value drives it
			if ( node.MaterialToggleActive && node.m_materialMode )
			{
				PreviewMaterial.SetInt( m_conditionId, node.MaterialValue );
			}
			else
			{
				PreviewMaterial.SetInt( m_conditionId, node.DefaultValue );
			}

			if ( IsSourceAlias )
			{
				// @diogo: same pull-and-mark dance as SamplerNode references: the source may be pooled and RT-less
				node.EnsurePreviewRendered();
				if ( PreviewLive )
				{
					node.MarkFeedsLiveConsumer();
				}
				for ( int i = 0; i < m_inputPorts.Count; i++ )
				{
					m_inputPorts[ i ].PreparePortCacheID();
					node.BindPreview( PreviewMaterial, m_inputPorts[ i ].CachedPropertyId, i, this );
				}
			}
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();

			// @diogo: a reference borrows the source's property, so it must stay out of the property register;
			// paste reassigns the id after ReadFromString, making this the last gate to run
			if ( CurrentVarMode != StaticSwitchVariableMode.Reference && MaterialToggleActive )
			{
				UIUtils.RegisterPropertyNode( this );
			}
			else
			{
				UIUtils.UnregisterPropertyNode( this );
			}

			if( CurrentVarMode != StaticSwitchVariableMode.Reference )
			{
				ContainerGraph.StaticSwitchNodes.AddNode( this );
			}

			if ( UniqueId > -1 )
			{
				ContainerGraph.StaticSwitchNodes.OnReorderEventComplete -= OnReorderEventComplete;
				ContainerGraph.StaticSwitchNodes.OnReorderEventComplete += OnReorderEventComplete;
			}
		}

		public override void Destroy()
		{
			base.Destroy();
			UIUtils.UnregisterPropertyNode( this );
			if( CurrentVarMode != StaticSwitchVariableMode.Reference )
			{
				ContainerGraph.StaticSwitchNodes.RemoveNode( this );
			}

			if( UniqueId > -1 )
				ContainerGraph.StaticSwitchNodes.OnReorderEventComplete -= OnReorderEventComplete;
		}

		void OnReorderEventComplete()
		{
			if( CurrentVarMode == StaticSwitchVariableMode.Reference )
			{
				if( m_reference != null )
				{
					m_referenceArrayId = ContainerGraph.StaticSwitchNodes.GetNodeRegisterIdx( m_reference.UniqueId );
				}
			}
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnections();
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdateConnections();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateConnections();
		}

		private void UpdateConnections()
		{
			WirePortDataType mainType = WirePortDataType.FLOAT;
			string mainStructName = string.Empty;
			int highest = -1;

			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				if( m_inputPorts[ i ].IsConnected )
				{
					WirePort source = m_inputPorts[ i ].GetOutputConnection();
					if( UIUtils.GetPriority( source.DataType ) > highest )
					{
						mainType = source.DataType;
						// @diogo: a struct is named by the port, so the name has to ride along with the type
						// or nothing downstream can tell which struct came out of the switch
						mainStructName = source.StructName;
						highest = UIUtils.GetPriority( source.DataType );
					}
				}
			}

			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].ChangeType( mainType, mainStructName, false );
			}

			m_outputPorts[ 0 ].ChangeType( mainType, mainStructName, false );
		}

		public override string GetPropertyValue()
		{
			if ( MaterialToggleActive )
			{
				string value = UIUtils.PropertyFloatToString( m_defaultValue );
				if ( m_keywordModeType == KeywordModeType.KeywordEnum && m_keywordEnumAmount > 0 )
				{
					return PropertyAttributes + "[" + m_keywordModeType.ToString() + "( " + GetKeywordEnumPropertyList() + " )] " + m_propertyName + "( \"" + m_propertyInspectorName + "\", Float ) = " + value;
				}
				else
				{
					return PropertyAttributes + "[" + m_keywordModeType.ToString() + "( " + GetPropertyValStr() + " )] " + m_propertyName + "( \"" + m_propertyInspectorName + "\", Float ) = " + value;
				}
			}
			return string.Empty;
		}

		public string KeywordEnum( int index )
		{
			if ( MaterialToggleActive )
			{
				return string.IsNullOrEmpty( PropertyName ) ? KeywordEnumList( index ) : ( PropertyName + "_" + KeywordEnumList( index ) );
			}
			else
			{
				return string.IsNullOrEmpty( PropertyName ) ? KeywordEnumList( index ) : ( PropertyName + KeywordEnumList( index ) );
			}
		}

		public string KeywordEnumList( int index )
		{
			if ( CurrentVarMode == StaticSwitchVariableMode.Fetch )
			{
				return m_keywordEnumList[ index ];
			}
			else
			{
				return MaterialToggleActive ? m_keywordEnumList[ index ].ToUpper() : m_keywordEnumList[ index ];
			}

		}
		public override string PropertyName
		{
			get
			{
				if ( CurrentVarMode == StaticSwitchVariableMode.Fetch )
				{
					return m_currentKeyword;
				}
				else
				{
					return MaterialToggleActive ? base.PropertyName.ToUpper() : base.PropertyName;
				}
			}
		}

		public override string GetPropertyValStr()
		{
			if ( m_keywordModeType == KeywordModeType.KeywordEnum )
				return PropertyName;
			else if( !m_lockKeyword )
				return CurrentKeyword;
			else if( CurrentVarMode == StaticSwitchVariableMode.Fetch )
				return m_currentKeyword;
			else
				return PropertyName + OnOffStr;
		}

		public override string GetSubTitleVarNameFormatStr()
		{
			if ( m_multiCompile == ( int )KeywordType.DynamicBranch )
			{
				return "Dynamic( {0} )";
			}
			else
			{
				return "Static( {0} )";
			}
		}

		private string GetKeywordEnumPropertyList()
		{
			string result = string.Empty;
			for( int i = 0; i < m_keywordEnumList.Length; i++ )
			{
				if( i == 0 )
					result = m_keywordEnumList[ i ];
				else
					result += "," + m_keywordEnumList[ i ];
			}
			return result;
		}

		private string GetKeywordEnumPragmaList()
		{
			string result = string.Empty;
			for( int i = 0; i < KeywordEnumAmount; i++ )
			{
				if( i == 0 )
					result = KeywordEnum( i );
				else
					result += " " + KeywordEnum( i );
			}
			// @diogo: the none entry declares no keyword, it just enables the variant where none of them is set
			return HasNoneEntry ? ( "_ " + result ) : result;
		}

		public override string GetUniformValue()
		{
			return string.Empty;
		}

		public override bool GetUniformData( out string dataType, out string dataName, ref bool fullValue )
		{
			dataType = string.Empty;
			dataName = string.Empty;
			return false;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );

			m_allowPropertyDuplicates = ( m_staticSwitchVarMode == StaticSwitchVariableMode.Fetch );

			// @diogo: source edits only dirty the source's own preview; track its state so references re-render
			if ( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null )
			{
				int sourceValue = ( m_reference.MaterialToggleActive && m_reference.m_materialMode ) ? m_reference.MaterialValue : m_reference.DefaultValue;
				if ( sourceValue != m_lastSourceValue )
				{
					m_lastSourceValue = sourceValue;
					PreviewIsDirty = true;
				}

				// @diogo: the struct name is half the type here, so an alias tracks it as well
				if ( IsSourceAlias && ( m_outputPorts[ 0 ].DataType != m_reference.m_outputPorts[ 0 ].DataType ||
					m_outputPorts[ 0 ].StructName != m_reference.m_outputPorts[ 0 ].StructName ) )
				{
					m_outputPorts[ 0 ].ChangeType( m_reference.m_outputPorts[ 0 ].DataType, m_reference.m_outputPorts[ 0 ].StructName, false );
				}
			}

			MessageType samplerSeverity = RuntimeSamplerStateSeverity();
			if ( samplerSeverity == MessageType.Error )
			{
				ShowTab( NodeMessageType.Error, RuntimeSamplerStateError );
			}
			else if ( samplerSeverity == MessageType.Warning )
			{
				ShowTab( NodeMessageType.Warning, RuntimeSamplerStateFunctionWarning );
			}
			else
			{
				HideTab();
			}
		}

		public override bool RecursivePreviewUpdate( Dictionary<string, bool> duplicatesDict = null )
		{
			// @diogo: no wire links a reference to its source, so pull the source through the same visited-dict
			// walk a wired producer gets: it renders first and its dirtiness propagates here
			if ( IsSourceAlias )
			{
				if ( duplicatesDict == null )
				{
					duplicatesDict = ContainerGraph.ParentWindow.VisitedChanged;
				}
				if ( !duplicatesDict.ContainsKey( m_reference.OutputId ) )
				{
					if ( m_reference.RecursivePreviewUpdate( duplicatesDict ) )
					{
						PreviewIsDirty = true;
					}
				}
				else if ( duplicatesDict[ m_reference.OutputId ] )
				{
					PreviewIsDirty = true;
				}
			}
			return base.RecursivePreviewUpdate( duplicatesDict );
		}

		public override void DrawProperties()
		{
			//base.DrawProperties();
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, Constants.ParameterLabelStr, PropertyGroup );
			NodeUtils.DrawPropertyGroup( ref m_visibleCustomAttrFoldout, CustomAttrStr, DrawCustomAttributes, DrawCustomAttrAddRemoveButtons );
			DrawWarnings();
			CheckPropertyFromInspector();
		}

		// @diogo: warnings sit below every group, so each one names the control it refers to
		void DrawWarnings()
		{
			// @diogo: ahead of the Reference early-out, since a reference inherits its source's branch form
			MessageType samplerSeverity = RuntimeSamplerStateSeverity();
			if ( samplerSeverity != MessageType.None )
			{
				NodeUtils.DrawHelpBox( ( samplerSeverity == MessageType.Error ) ? RuntimeSamplerStateError : RuntimeSamplerStateFunctionWarning, samplerSeverity );
			}

			if ( CurrentVarMode == StaticSwitchVariableMode.Reference )
			{
				return;
			}

			bool isDynamicBranch = ( m_multiCompile == ( int )KeywordType.DynamicBranch );
			bool isSurface = ( m_containerGraph.CurrentCanvasMode == NodeAvailability.SurfaceShader );
			bool isFunction = ( m_containerGraph.CurrentCanvasMode == NodeAvailability.ShaderFunction );
			bool isTemplate = ( m_containerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader );

			// @diogo: a runtime branch reads the keyword as an identifier, which a bare #define doesn't provide;
			// unified branch falls back to #ifdef in Surface Shaders, where its own warning already covers it
			if ( CurrentVarMode == StaticSwitchVariableMode.Create && !m_generateKeyword && ( isDynamicBranch || ( m_unifiedBranch && !isSurface ) ) )
			{
				NodeUtils.DrawHelpBox( GenerateKeywordRuntimeWarning, MessageType.Warning );
			}

			if ( m_unifiedBranch && !isDynamicBranch )
			{
				if ( isSurface )
				{
					NodeUtils.DrawHelpBox( UnifiedBranchSurfaceWarning, MessageType.Warning );
				}
				else
				{
					NodeUtils.DrawHelpBox( UnifiedBranchInfoMsg, MessageType.Info );
				}
			}

			if ( ( !isTemplate || isFunction ) && ( m_keywordModeType == KeywordModeType.KeywordEnum ) && ( m_shaderStage != ShaderStage.All ) )
			{
				NodeUtils.DrawHelpBox( KeywordModifierSurfaceWarning, isFunction ? MessageType.Info : MessageType.Warning );
			}
		}

		void DrawEnumList()
		{
			EditorGUI.BeginChangeCheck();
			m_includeNoneEntry = EditorGUILayoutToggle( IncludeNoneContent, m_includeNoneEntry );
			if ( EditorGUI.EndChangeCheck() )
			{
				if ( m_includeNoneEntry )
				{
					KeywordEnumAmount = Mathf.Min( KeywordEnumAmount, MaxKeywordAmount - 1 );
				}
				UpdateLabels();
			}

			EditorGUI.BeginChangeCheck();
			// @diogo: a selection sitting on the none entry must follow it as the entry count shifts it around
			bool noneSelected = HasNoneEntry && ( CurrentSelectedInput == KeywordEnumAmount );
			KeywordEnumAmount = EditorGUILayoutIntSlider( AmountStr, KeywordEnumAmount, 2, m_includeNoneEntry ? MaxKeywordAmount - 1 : MaxKeywordAmount );
			if( EditorGUI.EndChangeCheck() )
			{
				CurrentSelectedInput = noneSelected ? KeywordEnumAmount : Mathf.Clamp( CurrentSelectedInput, 0, KeywordEnumAmount - 1 );
				UpdateLabels();
			}
			EditorGUI.indentLevel++;
			for( int i = 0; i < KeywordEnumAmount; i++ )
			{
				EditorGUI.BeginChangeCheck();
				m_keywordEnumList[ i ] = EditorGUILayoutTextField( "Item " + i, m_keywordEnumList[ i ] );
				if( EditorGUI.EndChangeCheck() )
				{
					m_keywordEnumList[ i ] = UIUtils.RemoveInvalidEnumCharacters( m_keywordEnumList[ i ] );
					m_keywordEnumList[ i ] = m_keywordEnumList[ i ].Replace( " ", "" ); // sad face :( does not support spaces
					m_inputPorts[ i ].Name = m_keywordEnumList[ i ];
					m_defaultKeywordNames[ i ] = m_inputPorts[ i ].Name;
				}
			}
			EditorGUI.indentLevel--;
		}

		public void UpdateLabels()
		{
			int keywordAmount = m_keywordModeType == KeywordModeType.KeywordEnum ? KeywordEnumAmount : 2;
			int maxinputs = keywordAmount + ( HasNoneEntry ? 1 : 0 );
			KeywordEnumAmount = Mathf.Clamp( KeywordEnumAmount, 0, keywordAmount );
			m_keywordEnumList = new string[ maxinputs ];

			for( int i = 0; i < keywordAmount; i++ )
			{
				m_keywordEnumList[ i ] = m_defaultKeywordNames[ i ];
				m_inputPorts[ i ].Name = m_keywordEnumList[ i ];
			}

			if ( HasNoneEntry )
			{
				m_keywordEnumList[ keywordAmount ] = NoneEntryStr;
				m_inputPorts[ keywordAmount ].Name = NoneEntryStr;
			}

			if( m_keywordModeType != KeywordModeType.KeywordEnum )
			{
				m_inputPorts[ 0 ].Name = "False";
				m_inputPorts[ 1 ].Name = "True";
			}

			// @diogo: the none entry keeps the trailing port that old graphs are wired to, and only
			// its order moves, putting it at the top of the node
			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].Visible = ( i < maxinputs );
				m_inputPorts[ i ].OrderId = HasNoneEntry ? ( i + 1 ) : i;
			}

			if ( HasNoneEntry )
			{
				m_inputPorts[ keywordAmount ].OrderId = 0;
			}

			m_sizeIsDirty = true;
			m_isStaticSwitchDirty = true;
		}

		void PropertyGroup()
		{
			EditorGUI.BeginChangeCheck();
			var prevVarMode = CurrentVarMode;
			CurrentVarMode = (StaticSwitchVariableMode)EditorGUILayoutEnumPopup( ModeStr, CurrentVarMode );
			if( EditorGUI.EndChangeCheck() )
			{
				if ( prevVarMode == StaticSwitchVariableMode.Fetch && m_currentKeywordId == 0 )
				{
					m_propertyName = m_currentKeyword;
					m_propertyInspectorName = m_currentKeyword;
				}

				if( CurrentVarMode == StaticSwitchVariableMode.Fetch )
				{
					m_keywordModeType = KeywordModeType.Toggle;
					UpdateLabels();
				}

				if( CurrentVarMode == StaticSwitchVariableMode.Reference )
				{
					UIUtils.UnregisterPropertyNode( this );
				}
				else
				{
					if ( MaterialToggleActive )
					{
						UIUtils.RegisterPropertyNode( this );
					}
					else
					{
						UIUtils.UnregisterPropertyNode( this );
					}
				}
			}

			UIUtils.DrawSeparator();

			if( CurrentVarMode == StaticSwitchVariableMode.Create )
			{
				EditorGUI.BeginChangeCheck();
				m_multiCompile = EditorGUILayoutIntPopup( KeywordTypeStr, m_multiCompile, KeywordTypeList, KeywordTypeInt );
				if( EditorGUI.EndChangeCheck() )
				{
					BeginPropertyFromInspectorCheck();
				}

				m_generateKeyword = EditorGUILayoutToggle( GenerateKeywordContent, m_generateKeyword );
			}
			else if( CurrentVarMode == StaticSwitchVariableMode.Reference )
			{
				string[] arr = ContainerGraph.StaticSwitchNodes.NodesArr;
				bool guiEnabledBuffer = GUI.enabled;
				if( arr != null && arr.Length > 0 )
				{
					GUI.enabled = true;
				}
				else
				{
					m_referenceArrayId = -1;
					GUI.enabled = false;
				}

				arr = UIUtils.DisambiguateReferenceLabels( arr, ContainerGraph.StaticSwitchNodes.NodesList, 0 );
				EditorGUILayout.BeginHorizontal();
				SearchableDropdown.Draw( Constants.AvailableReferenceStr, m_referenceArrayId, arr, ( idx ) =>
				{
					if( idx != m_referenceArrayId )
					{
						UndoRecordObject( "Selected static switch reference" );
						m_referenceArrayId = idx;
						m_reference = ContainerGraph.StaticSwitchNodes.GetNode( m_referenceArrayId );
						if( m_reference != null )
						{
							m_referenceNodeId = m_reference.UniqueId;
							CheckReferenceValues( true );
						}
						else
						{
							m_referenceArrayId = -1;
							m_referenceNodeId = -1;
						}
						SetDirtyFromAsyncEdit();
					}
				} );
				UIUtils.DrawReferenceFocusButton( m_reference );
				EditorGUILayout.EndHorizontal();
				GUI.enabled = guiEnabledBuffer;

				return;
			}

			if( CurrentVarMode == StaticSwitchVariableMode.Create || MaterialToggleActive )
			{
				EditorGUI.BeginChangeCheck();
				m_keywordModeType = (KeywordModeType)EditorGUILayoutEnumPopup( TypeStr, m_keywordModeType );
				if( EditorGUI.EndChangeCheck() )
				{
					UpdateLabels();
				}
			}

			if( m_keywordModeType != KeywordModeType.KeywordEnum )
			{
				if( CurrentVarMode == StaticSwitchVariableMode.Create || MaterialToggleActive )
				{
					ShowPropertyInspectorNameGUI();
					ShowPropertyNameGUI( true );
					if( CurrentVarMode == StaticSwitchVariableMode.Create )
					{
						EditorGUILayout.BeginHorizontal();
						bool guiEnabledBuffer = GUI.enabled;
						GUI.enabled = !m_lockKeyword;
						if( m_lockKeyword )
							EditorGUILayout.TextField( KeywordNameStr, GetPropertyValStr() );
						else
							m_currentKeyword = EditorGUILayoutTextField( KeywordNameStr, m_currentKeyword );
						GUI.enabled = guiEnabledBuffer;
						m_lockKeyword = GUILayout.Toggle( m_lockKeyword, ( m_lockKeyword ? UIUtils.LockIconOpen : UIUtils.LockIconClosed ), "minibutton", GUILayout.Width( 22 ) );
						EditorGUILayout.EndHorizontal();
					}
				}

			}
			else
			{
				if( CurrentVarMode == StaticSwitchVariableMode.Create || MaterialToggleActive )
				{
					ShowPropertyInspectorNameGUI();
					ShowPropertyNameGUI( true );
					DrawEnumList();
				}

			}

			if( CurrentVarMode == StaticSwitchVariableMode.Fetch )
			{
				//ShowPropertyInspectorNameGUI();
				string[] keywords = UIUtils.AvailableKeywords;
				SearchableDropdown.Draw( KeywordStr, m_currentKeywordId, keywords, ( idx ) =>
				{
					if( idx != m_currentKeywordId )
					{
						UndoRecordObject( "Selected keyword" );
						m_currentKeywordId = idx;
						if( idx != 0 && idx < keywords.Length )
						{
							m_currentKeyword = keywords[ idx ];
						}
						SetDirtyFromAsyncEdit();
					}
				} );

				if( m_currentKeywordId == 0 )
				{
					EditorGUI.BeginChangeCheck();
					m_currentKeyword = EditorGUILayoutTextField( CustomStr, m_currentKeyword );
					if( EditorGUI.EndChangeCheck() )
					{
						m_currentKeyword = UIUtils.RemoveInvalidCharacters( m_currentKeyword );
					}
				}
			}

			// @diogo: these only shape the keyword declaration, which the user takes over when generation is off
			bool keywordModifiersEnabled = m_generateKeyword || CurrentVarMode != StaticSwitchVariableMode.Create;

			bool isDynamicBranch = ( m_multiCompile == ( int )KeywordType.DynamicBranch );
			GUI.enabled = !isDynamicBranch;
			{
				bool newUnifiedBranch = EditorGUILayoutToggle( UnifiedBranchContent, UsesRuntimeBranch );
				// @diogo: Dynamic Branch forces this on, so only persist user input when it's actually editable
				if ( !isDynamicBranch )
				{
					m_unifiedBranch = newUnifiedBranch;
				}

				GUI.enabled = !isDynamicBranch && keywordModifiersEnabled;
				m_isLocal = EditorGUILayoutToggle( IsLocalStr, m_isLocal );

				GUI.enabled = !isDynamicBranch && !m_unifiedBranch && keywordModifiersEnabled;
				m_shaderStage = ( ShaderStage )EditorGUILayoutEnumPopup( StageStr, m_shaderStage );
				GUI.enabled = !isDynamicBranch;

				//if( CurrentVarMode == StaticSwitchVariableMode.Create )
				{
					ShowAutoRegister();
				}
			}
			GUI.enabled = true;

			if ( CurrentVarMode != StaticSwitchVariableMode.Fetch )
			{
				EditorGUI.BeginChangeCheck();
				m_materialToggle = EditorGUILayoutToggle( MaterialToggleStr, m_materialToggle );
				if ( EditorGUI.EndChangeCheck() )
				{
					if ( MaterialToggleActive )
					{
						UIUtils.RegisterPropertyNode( this );
					}
					else
					{
						UIUtils.UnregisterPropertyNode( this );
					}
				}
			}

			if ( MaterialToggleActive )
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space( 20 );
				m_propertyTab = GUILayout.Toolbar( m_propertyTab, LabelToolbarTitle );
				EditorGUILayout.EndHorizontal();
				switch( m_propertyTab )
				{
					default:
					case 0:
					{
						EditorGUI.BeginChangeCheck();
						if( m_keywordModeType != KeywordModeType.KeywordEnum )
							m_materialValue = EditorGUILayoutToggle( ToggleMaterialValueStr, m_materialValue == 1 ) ? 1 : 0;
						else
							m_materialValue = EditorGUILayoutPopup( ToggleMaterialValueStr, m_materialValue, m_keywordEnumList );
						if( EditorGUI.EndChangeCheck() )
							m_requireMaterialUpdate = true;
					}
					break;
					case 1:
					{
						if( m_keywordModeType != KeywordModeType.KeywordEnum )
							m_defaultValue = EditorGUILayoutToggle( ToggleDefaultValueStr, m_defaultValue == 1 ) ? 1 : 0;
						else
							m_defaultValue = EditorGUILayoutPopup( ToggleDefaultValueStr, m_defaultValue, m_keywordEnumList );
					}
					break;
				}
			}

			//EditorGUILayout.HelpBox( "Keyword Type:\n" +
			//	"The difference is that unused variants of \"Shader Feature\" shaders will not be included into game build while \"Multi Compile\" variants are included regardless of their usage.\n\n" +
			//	"So \"Shader Feature\" makes most sense for keywords that will be set on the materials, while \"Multi Compile\" for keywords that will be set from code globally.\n\n" +
			//	"You can set keywords using the material property using the \"Property Name\" or you can set the keyword directly using the \"Keyword Name\".", MessageType.None );
		}

		public override void CheckPropertyFromInspector( bool forceUpdate = false )
		{
			if( m_propertyFromInspector )
			{
				if( forceUpdate || ( EditorApplication.timeSinceStartup - m_propertyFromInspectorTimestamp ) > MaxTimestamp )
				{
					m_propertyFromInspector = false;
					RegisterPropertyName( true, m_propertyInspectorName, m_autoGlobalName, m_underscoredGlobal );
					m_propertyNameIsDirty = true;

					if( CurrentVarMode != StaticSwitchVariableMode.Reference )
					{
						ContainerGraph.StaticSwitchNodes.UpdateDataOnNode( UniqueId, DataToArray );
					}
				}
			}
		}

		// @diogo: renames also commit outside the inspector check ( duplicate dedup, panel renames ); keep
		// the sorted register in sync on every commit path or its order goes stale until the next reload
		public override void OnPropertyNameChanged()
		{
			base.OnPropertyNameChanged();
			if ( CurrentVarMode != StaticSwitchVariableMode.Reference )
			{
				ContainerGraph.StaticSwitchNodes.UpdateDataOnNode( UniqueId, DataToArray );
			}
		}

		public override void OnNodeLayout( DrawInfo drawInfo, NodeUpdateCache cache )
		{
			StaticSwitch node = ( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null ) ? m_reference : this;

			float finalSize = 0;
			if( node.m_keywordModeType == KeywordModeType.KeywordEnum )
			{
				if( node.m_inputPorts != null && CurrentSelectedInput >= 0 && CurrentSelectedInput < node.m_inputPorts.Count && node.m_inputPorts[ CurrentSelectedInput ] != null )
				{
					GUIContent dropdown = new GUIContent( node.m_inputPorts[ CurrentSelectedInput ].Name );
					int cacheSize = UIUtils.GraphDropDown.fontSize;
					UIUtils.GraphDropDown.fontSize = 10;
					Vector2 calcSize = UIUtils.GraphDropDown.CalcSize( dropdown );
					UIUtils.GraphDropDown.fontSize = cacheSize;
					finalSize = Mathf.Clamp( calcSize.x, MinComboSize, MaxComboSize );
					if( m_insideSize.x != finalSize )
					{
						m_insideSize.Set( finalSize, 25 );
						m_sizeIsDirty = true;
					}
				}
			}

			base.OnNodeLayout( drawInfo, cache );

			if( node.m_keywordModeType != KeywordModeType.KeywordEnum )
			{
				m_varRect = m_remainingBox;
				m_varRect.size = Vector2.one * 22 * drawInfo.InvertedZoom;
				m_varRect.center = m_remainingBox.center;
				if( m_showPreview )
					m_varRect.y = m_remainingBox.y;
			}
			else
			{
				m_varRect = m_remainingBox;
				m_varRect.width = finalSize * drawInfo.InvertedZoom;
				m_varRect.height = 16 * drawInfo.InvertedZoom;
				m_varRect.x = m_remainingBox.xMax - m_varRect.width;
				m_varRect.y += 1 * drawInfo.InvertedZoom;

				m_imgRect = m_varRect;
				m_imgRect.x = m_varRect.xMax - 16 * drawInfo.InvertedZoom;
				m_imgRect.width = 16 * drawInfo.InvertedZoom;
				m_imgRect.height = m_imgRect.width;
			}

			CheckReferenceValues( false );

			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference )
			{
				m_iconPos = m_globalPosition;
				m_iconPos.width = InstanceIconWidth * drawInfo.InvertedZoom;
				m_iconPos.height = InstanceIconHeight * drawInfo.InvertedZoom;

				m_iconPos.y = m_globalPosition.y + m_headerPosition.height * 0.5f - m_iconPos.height * 0.5f;
				m_iconPos.x += /*m_globalPosition.width - m_iconPos.width - */5 * drawInfo.InvertedZoom;
			}
		}

		// @diogo: a switch already in Reference mode borrows another node's keyword, so it is never a source
		public override bool CanBeReferenced { get { return m_staticSwitchVarMode != StaticSwitchVariableMode.Reference; } }

		protected override bool IsTitleEditable { get { return m_staticSwitchVarMode != StaticSwitchVariableMode.Reference; } }

		public override void SetReferenceTo( PropertyNode reference )
		{
			SetToReference( reference as StaticSwitch );
		}

		public void SetToReference( StaticSwitch reference )
		{
			if ( reference == null || reference == this )
			{
				return;
			}

			// @diogo: the setter drops this node from the register, so the source index is only stable after it
			CurrentVarMode = StaticSwitchVariableMode.Reference;
			UIUtils.UnregisterPropertyNode( this );

			m_referenceArrayId = ContainerGraph.StaticSwitchNodes.GetNodeRegisterIdx( reference.UniqueId );
			m_reference = ContainerGraph.StaticSwitchNodes.GetNode( m_referenceArrayId );
			if ( m_reference != null )
			{
				m_referenceNodeId = m_reference.UniqueId;
				CheckReferenceValues( true );
			}
			else
			{
				m_referenceArrayId = -1;
				m_referenceNodeId = -1;
			}
		}

		void CheckReferenceValues( bool forceUpdate )
		{
			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference )
			{
				if( m_reference == null && m_referenceNodeId > 0 )
				{
					m_reference = ContainerGraph.GetNode( m_referenceNodeId ) as StaticSwitch;
					m_referenceArrayId = ContainerGraph.StaticSwitchNodes.GetNodeRegisterIdx( m_referenceNodeId );
				}

				// @diogo: a node in Reference mode can't itself be a source ( no chaining ), so a source
				// switched into Reference mode invalidates the link
				if ( m_reference != null && !m_reference.CanBeReferenced )
				{
					m_reference = null;
				}

				if( m_reference != null )
				{
					if( forceUpdate || m_reference.IsStaticSwitchDirty )
					{
						int count = m_inputPorts.Count;
						for( int i = 0; i < count; i++ )
						{
							m_inputPorts[ i ].Name = m_reference.InputPorts[ i ].Name;
							m_inputPorts[ i ].Visible = m_reference.InputPorts[ i ].Visible;
							m_inputPorts[ i ].OrderId = m_reference.InputPorts[ i ].OrderId;
						}
						m_sizeIsDirty = true;
					}
				}
				else if ( m_referenceNodeId > -1 )
				{
					// @diogo: dangling link ( source gone, retyped, or now a reference ); drop it fully so
					// no stale name/outline/wire lingers while the picker already reads None
					m_referenceArrayId = -1;
					m_referenceNodeId = -1;
					m_sizeIsDirty = true;
				}
			}
			else
			{
				m_isStaticSwitchDirty = false;
			}
		}

		public override void DrawGUIControls( DrawInfo drawInfo )
		{
			base.DrawGUIControls( drawInfo );

			if( drawInfo.CurrentEventType != EventType.MouseDown || !MaterialToggleActive )
				return;

			if( m_varRect.Contains( drawInfo.MousePosition ) )
			{
				m_editing = true;
			}
			else if( m_editing )
			{
				m_editing = false;
			}
		}

		private int CurrentSelectedInput
		{
			get
			{
				return m_materialMode ? m_materialValue : m_defaultValue;
			}
			set
			{
				if( m_materialMode )
					m_materialValue = value;
				else
					m_defaultValue = value;
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference )
				return;

			if( m_editing )
			{
				if( m_keywordModeType != KeywordModeType.KeywordEnum )
				{
					if( GUI.Button( m_varRect, GUIContent.none, UIUtils.GraphButton ) )
					{
						UndoRecordObject( "Changing value Toggle on node Static Switch" );
						CurrentSelectedInput = CurrentSelectedInput == 1 ? 0 : 1;
						PreviewIsDirty = true;
						m_editing = false;
						if( m_materialMode )
							m_requireMaterialUpdate = true;
					}

					if( CurrentSelectedInput == 1 )
					{
						GUI.Label( m_varRect, m_checkContent, UIUtils.GraphButtonIcon );
					}
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					CurrentSelectedInput = EditorGUIPopup( m_varRect, CurrentSelectedInput, m_keywordEnumList, UIUtils.GraphDropDown );
					if( EditorGUI.EndChangeCheck() )
					{
						PreviewIsDirty = true;
						m_editing = false;
						if( m_materialMode )
							m_requireMaterialUpdate = true;
					}
				}
			}
		}

		public override void OnNodeRepaint( DrawInfo drawInfo )
		{
			base.OnNodeRepaint( drawInfo );

			if( !m_isVisible )
				return;

			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference )
			{
				GUI.Label( m_iconPos, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerTextureIcon ) );
				GUI.enabled = false;
			}

			StaticSwitch node = ( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null ) ? m_reference : this;

			if( !m_editing && MaterialToggleActive && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD2 )
			{
				if( node.m_keywordModeType != KeywordModeType.KeywordEnum )
				{
					GUI.Label( m_varRect, GUIContent.none, UIUtils.GraphButton );

					if( node.CurrentSelectedInput == 1 )
						GUI.Label( m_varRect, m_checkContent, UIUtils.GraphButtonIcon );
				}
				else
				{
					GUI.Label( m_varRect, node.m_keywordEnumList[ node.CurrentSelectedInput ], UIUtils.GraphDropDown );
					GUI.Label( m_imgRect, m_popContent, UIUtils.GraphButtonIcon );
				}
			}

			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference )
			{
				GUI.enabled = true;
			}
		}

		private string OnOffStr
		{
			get
			{
				if( !m_lockKeyword )
					return string.Empty;

				StaticSwitch node = null;
				switch( CurrentVarMode )
				{
					default:
					case StaticSwitchVariableMode.Create:
					case StaticSwitchVariableMode.Fetch:
					node = this;
					break;
					case StaticSwitchVariableMode.Reference:
					{
						node = ( m_reference != null ) ? m_reference : this;
					}
					break;
				}

				if ( !node.MaterialToggleActive )
				{
					return string.Empty;
				}

				switch( node.KeywordModeTypeValue )
				{
					default:
					case KeywordModeType.Toggle:
					return "_ON";
					case KeywordModeType.ToggleOff:
					return "_OFF";
				}
			}
		}

		// @diogo: dynamic_branch has no preprocessor form, so it always takes the runtime path
		private bool UsesRuntimeBranch { get { return m_multiCompile == ( int )KeywordType.DynamicBranch || m_unifiedBranch; } }

		// @diogo: the surface shader codegen step never resolves static keywords as identifiers, so
		// if (KEYWORD) cannot compile there and the branch falls back to #ifdef (#365)
		private bool EmitsRuntimeBranch( MasterNodeDataCollector dataCollector )
		{
			return m_multiCompile == ( int )KeywordType.DynamicBranch || ( m_unifiedBranch && dataCollector.IsTemplate );
		}

		// How a Sampler State on the output fares against the branch form: an error where the branch is always
		// emitted at runtime, a warning in a shader function, where that depends on the shader using it.
		private MessageType RuntimeSamplerStateSeverity()
		{
			MessageType severity = MessageType.None;
			if ( !IsSourceAlias && m_outputPorts[ 0 ].DataType == WirePortDataType.SAMPLERSTATE )
			{
				StaticSwitch node = ( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null ) ? m_reference : this;
				if ( node.m_multiCompile == ( int )KeywordType.DynamicBranch || ( node.m_unifiedBranch && m_containerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader ) )
				{
					severity = MessageType.Error;
				}
				else if ( node.m_unifiedBranch && m_containerGraph.CurrentCanvasMode == NodeAvailability.ShaderFunction )
				{
					severity = MessageType.Warning;
				}
			}
			return severity;
		}

		string GetStaticSwitchType( bool allowStages )
		{
			string staticSwitchType;
			bool allowModifiers = true;

			switch ( m_multiCompile )
			{
				case 1:
					staticSwitchType = "multi_compile";
					break;
				case 2:
					staticSwitchType = "dynamic_branch";
					allowModifiers = false;
					break;
				default:
					staticSwitchType = "shader_feature";
					break;
			}

			if ( allowModifiers )
			{
				if ( m_isLocal )
				{
					staticSwitchType += "_local";
				}

				if ( allowStages )
				{
					switch ( m_shaderStage )
					{
						default:
						case ShaderStage.All: break;
						case ShaderStage.Vertex: staticSwitchType += "_vertex"; break;
						case ShaderStage.Fragment: staticSwitchType += "_fragment"; break;
						case ShaderStage.Hull: staticSwitchType += "_hull"; break;
						case ShaderStage.Domain: staticSwitchType += "_domain"; break;
						case ShaderStage.Geometry: staticSwitchType += "_geometry"; break;
						case ShaderStage.Raytracing: staticSwitchType += "_raytracing"; break;
					}
				}
			}
			return staticSwitchType;
		}

		void RegisterPragmas( ref MasterNodeDataCollector dataCollector )
		{
			// @diogo: with generation off the keyword is declared elsewhere, e.g. an Additional Directives node
			if ( CurrentVarMode == StaticSwitchVariableMode.Create && m_generateKeyword )
			{
				string staticSwitchType = GetStaticSwitchType( allowStages: ( dataCollector.IsTemplate || ( m_keywordModeType != KeywordModeType.KeywordEnum ) ) && !m_unifiedBranch );

				if( m_keywordModeType == KeywordModeType.KeywordEnum )
				{
					dataCollector.AddToPragmas( UniqueId, staticSwitchType + " " + GetKeywordEnumPragmaList() );
				}
				else
				{
					if( m_multiCompile == 1 )
					{
						dataCollector.AddToPragmas( UniqueId, staticSwitchType + " _ " + CurrentKeyword );
					}
					else if( m_unifiedBranch && m_multiCompile == ( int )KeywordType.ShaderFeature )
					{
						// @diogo: off state must be explicit so the keyword set matches Shader Build Settings input
						dataCollector.AddToPragmas( UniqueId, staticSwitchType + " _ " + CurrentKeyword );
					}
					else
					{
						dataCollector.AddToPragmas( UniqueId, staticSwitchType + " " + CurrentKeyword );
					}
				}
			}
		}

		protected override void RegisterProperty( ref MasterNodeDataCollector dataCollector )
		{
			if( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null )
			{
				m_reference.RegisterProperty( ref dataCollector );
				m_reference.RegisterPragmas( ref dataCollector );
			}
			else
			{
				if ( MaterialToggleActive )
				{
					base.RegisterProperty( ref dataCollector );
				}

				RegisterPragmas( ref dataCollector );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			if ( ReportStructMix( 0, m_inputPorts.Count - 1 ) )
			{
				return m_outputPorts[ 0 ].ErrorValue;
			}

			StaticSwitch node = ( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference != null ) ? m_reference : this;

			this.OrderIndex = node.RawOrderIndex;
			this.OrderIndexOffset = node.OrderIndexOffset;

			// @diogo: with no inputs of its own, a reference outputs its source's result instead of switching defaults
			if ( IsSourceAlias )
			{
				return node.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			}

			// @diogo: nothing names the wildcard on the wire, so the local every form below declares
			// would come out with no type at all
			if ( m_outputPorts[ 0 ].DataType == WirePortDataType.OBJECT )
			{
				UIUtils.ShowMessage( UniqueId, CustomTypeError, MessageSeverity.Error );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			// @diogo: a sampler local must resolve to one global at compile time, so only the #if form can
			// carry it, and only unscoped, since scoping declares the local as ( SamplerState )0 first. The
			// runtime form is still generated, so the shader compiler's own error confirms the report
			bool isSamplerState = m_outputPorts[ 0 ].DataType == WirePortDataType.SAMPLERSTATE;
			if ( isSamplerState && node.EmitsRuntimeBranch( dataCollector ) )
			{
				UIUtils.ShowMessage( UniqueId, RuntimeSamplerStateError, MessageSeverity.Error );
			}

			// @diogo: every form below declares a local of the output type, so a struct only needs its name
			// spelled here - PrecisionWirePortToCgType has no answer for one and would emit a hole
			bool isStruct = m_outputPorts[ 0 ].DataType == WirePortDataType.STRUCT;
			if ( isStruct && string.IsNullOrEmpty( m_outputPorts[ 0 ].StructName ) )
			{
				UIUtils.ShowMessage( UniqueId, NamelessStructError, MessageSeverity.Error );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			string outType = isStruct ? m_outputPorts[ 0 ].StructName : UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_outputPorts[ 0 ].DataType );

			// @diogo: a sampler state has no zero value; every arm assigns it, so left bare the compiler
			// reports the runtime pick itself instead of the cast
			string runtimeDeclaration = outType + " dynamicSwitch" + OutputId + ( isSamplerState ? ";" : " = ( " + outType + " )0;" );

			if( node.KeywordModeTypeValue == KeywordModeType.KeywordEnum )
			{
				int keywordAmount = node.KeywordEnumAmount;
				int entryAmount = node.TotalEnumEntries;
				// @diogo: with a none entry the trailing else belongs to it, otherwise it repeats the default arm
				int elseIndex = node.HasNoneEntry ? keywordAmount : node.DefaultValue;

				string[] allOutputs = new string[ entryAmount ];

				if ( node.EmitsRuntimeBranch( dataCollector ) )
				{
					BranchArmAnalysis[] arms = new BranchArmAnalysis[ entryAmount ];
					for ( int i = 0; i < entryAmount; i++ )
					{
						// @diogo: the else arm doubles as the trailing else's value, so it can never live inside a scope
						arms[ i ] = ( i != elseIndex ) ? BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ i ], ref dataCollector ) : new BranchArmAnalysis();
					}

					for ( int i = 0; i < entryAmount; i++ )
					{
						if ( arms[ i ].Scoped )
						{
							BranchScopeUtils.PreGenerateFrontier( arms[ i ], ref dataCollector );
						}
						else
						{
							allOutputs[ i ] = BranchScopeUtils.PreGenerateFallback( arms[ i ], m_inputPorts[ i ], ref dataCollector );
						}
					}

					// @diogo: [branch] on a keyword that folds to a compile-time constant warns, so only dynamic_branch gets it
					string branchAttr = ( node.m_multiCompile == ( int )KeywordType.DynamicBranch ) ? "UNITY_BRANCH " : string.Empty;
					dataCollector.AddLocalVariable( UniqueId, runtimeDeclaration, true );
					for ( int i = 0; i < keywordAmount; i++ )
					{
						string keyword = node.KeywordEnum( i );
						if ( i == 0 )
							dataCollector.AddLocalVariable( UniqueId, branchAttr + "if ( " + keyword + " )", true );
						else
							dataCollector.AddLocalVariable( UniqueId, "else " + branchAttr + "if ( " + keyword + " )", true );

						dataCollector.AddLocalVariable( UniqueId, "{", true );

						if ( arms[ i ].Scoped )
						{
							// @diogo: an open scope indents everything it emits, so only a flat arm needs a literal tab
							dataCollector.PushBranchScope( uniformCondition: true );
							allOutputs[ i ] = m_inputPorts[ i ].GeneratePortInstructions( ref dataCollector );
							dataCollector.AddLocalVariable( UniqueId, "dynamicSwitch" + OutputId + " = " + allOutputs[ i ] + ";", true );
							dataCollector.PopBranchScope();
						}
						else
						{
							dataCollector.AddLocalVariable( UniqueId, "\tdynamicSwitch" + OutputId + " = " + allOutputs[ i ] + ";", true );
						}

						dataCollector.AddLocalVariable( UniqueId, "}", true );
					}
					dataCollector.AddLocalVariable( UniqueId, "else", true );
					dataCollector.AddLocalVariable( UniqueId, "{", true );
					dataCollector.AddLocalVariable( UniqueId, "\tdynamicSwitch" + OutputId + " = " + allOutputs[ elseIndex ] + ";", true );
					dataCollector.AddLocalVariable( UniqueId, "}", true );
				}
				else
				{
					BranchArmAnalysis[] arms = new BranchArmAnalysis[ entryAmount ];
					bool anyScoped = false;
					for ( int i = 0; i < entryAmount; i++ )
					{
						// @diogo: the else arm doubles as the trailing else's value, so it can never live inside a scope
						arms[ i ] = ( i != elseIndex && !isSamplerState ) ? BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ i ], ref dataCollector, divergentFlow: false ) : new BranchArmAnalysis();
						anyScoped = anyScoped || arms[ i ].Scoped;
					}

					for ( int i = 0; i < entryAmount; i++ )
					{
						if ( arms[ i ].Scoped )
						{
							BranchScopeUtils.PreGenerateFrontier( arms[ i ], ref dataCollector );
						}
						else
						{
							allOutputs[ i ] = m_inputPorts[ i ].GeneratePortInstructions( ref dataCollector );
						}
					}

					string varName = "staticSwitch" + OutputId;
					if ( anyScoped )
					{
						dataCollector.AddLocalVariable( UniqueId, outType + " " + varName + " = ( " + outType + " )0;", true );
					}

					for ( int i = 0; i < keywordAmount; i++ )
					{
						string keyword = node.KeywordEnum( i );
						if ( i == 0 )
							dataCollector.AddLocalVariable( UniqueId, "#if defined( " + keyword + " )", true );
						else
							dataCollector.AddLocalVariable( UniqueId, "#elif defined( " + keyword + " )", true );

						if ( anyScoped )
						{
							BranchScopeUtils.EmitBracedArm( UniqueId, arms[ i ], m_inputPorts[ i ], varName, allOutputs[ i ], node.ArmCondition( keyword, false ), ref dataCollector );
						}
						else
						{
							dataCollector.AddLocalVariable( UniqueId, "\t" + outType + " " + varName + " = " + allOutputs[ i ] + ";", true );
						}
					}
					dataCollector.AddLocalVariable( UniqueId, "#else", true );
					if ( anyScoped )
					{
						BranchScopeUtils.EmitBracedArm( UniqueId, arms[ elseIndex ], m_inputPorts[ elseIndex ], varName, allOutputs[ elseIndex ], null, ref dataCollector );
					}
					else
					{
						dataCollector.AddLocalVariable( UniqueId, "\t" + outType + " " + varName + " = " + allOutputs[ elseIndex ] + ";", true );
					}
					dataCollector.AddLocalVariable( UniqueId, "#endif", true );
				}
			}
			else
			{
				if ( node.EmitsRuntimeBranch( dataCollector ) )
				{
					BranchArmAnalysis falseArm = BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ 0 ], ref dataCollector );
					BranchArmAnalysis trueArm = BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ 1 ], ref dataCollector );

					string falseCode = null;
					string trueCode = null;

					if ( falseArm.Scoped )
					{
						BranchScopeUtils.PreGenerateFrontier( falseArm, ref dataCollector );
					}
					else
					{
						falseCode = BranchScopeUtils.PreGenerateFallback( falseArm, m_inputPorts[ 0 ], ref dataCollector );
					}

					if ( trueArm.Scoped )
					{
						BranchScopeUtils.PreGenerateFrontier( trueArm, ref dataCollector );
					}
					else
					{
						trueCode = BranchScopeUtils.PreGenerateFallback( trueArm, m_inputPorts[ 1 ], ref dataCollector );
					}

					string branchAttr = ( node.m_multiCompile == ( int )KeywordType.DynamicBranch ) ? "UNITY_BRANCH " : string.Empty;
					dataCollector.AddLocalVariable( UniqueId, runtimeDeclaration, true );
					dataCollector.AddLocalVariable( UniqueId, branchAttr + "if ( " + node.CurrentKeyword + " )", true );
					dataCollector.AddLocalVariable( UniqueId, "{", true );
					if ( trueArm.Scoped )
					{
						dataCollector.PushBranchScope( uniformCondition: true );
						trueCode = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
						dataCollector.AddLocalVariable( UniqueId, "dynamicSwitch" + OutputId + " = " + trueCode + ";", true );
						dataCollector.PopBranchScope();
					}
					else
					{
						dataCollector.AddLocalVariable( UniqueId, "\tdynamicSwitch" + OutputId + " = " + trueCode + ";", true );
					}
					dataCollector.AddLocalVariable( UniqueId, "}", true );
					dataCollector.AddLocalVariable( UniqueId, "else", true );
					dataCollector.AddLocalVariable( UniqueId, "{", true );
					if ( falseArm.Scoped )
					{
						dataCollector.PushBranchScope( uniformCondition: true );
						falseCode = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
						dataCollector.AddLocalVariable( UniqueId, "dynamicSwitch" + OutputId + " = " + falseCode + ";", true );
						dataCollector.PopBranchScope();
					}
					else
					{
						dataCollector.AddLocalVariable( UniqueId, "\tdynamicSwitch" + OutputId + " = " + falseCode + ";", true );
					}
					dataCollector.AddLocalVariable( UniqueId, "}", true );
					m_outputPorts[ 0 ].SetLocalValue( "dynamicSwitch" + OutputId, dataCollector.PortCategory );
				}
				else
				{
					BranchArmAnalysis falseArm = isSamplerState ? new BranchArmAnalysis() : BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ 0 ], ref dataCollector, divergentFlow: false );
					BranchArmAnalysis trueArm = isSamplerState ? new BranchArmAnalysis() : BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ 1 ], ref dataCollector, divergentFlow: false );
					bool anyScoped = falseArm.Scoped || trueArm.Scoped;

					string falseCode = null;
					string trueCode = null;

					if ( falseArm.Scoped )
					{
						BranchScopeUtils.PreGenerateFrontier( falseArm, ref dataCollector );
					}
					else
					{
						falseCode = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					}

					if ( trueArm.Scoped )
					{
						BranchScopeUtils.PreGenerateFrontier( trueArm, ref dataCollector );
					}
					else
					{
						trueCode = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
					}

					string varName = "staticSwitch" + OutputId;
					if ( anyScoped )
					{
						dataCollector.AddLocalVariable( UniqueId, outType + " " + varName + " = ( " + outType + " )0;", true );
					}

					//if( node.CurrentVarMode == StaticSwitchVariableMode.Fetch )
					dataCollector.AddLocalVariable( UniqueId, "#ifdef " + node.CurrentKeyword, true );
					//else
					//	dataCollector.AddLocalVariable( UniqueId, "#ifdef " + node.PropertyName + OnOffStr, true );
					if ( anyScoped )
					{
						BranchScopeUtils.EmitBracedArm( UniqueId, trueArm, m_inputPorts[ 1 ], varName, trueCode, node.ArmCondition( node.CurrentKeyword, false ), ref dataCollector );
					}
					else
					{
						dataCollector.AddLocalVariable( UniqueId, "\t" + outType + " " + varName + " = " + trueCode + ";", true );
					}
					dataCollector.AddLocalVariable( UniqueId, "#else", true );
					if ( anyScoped )
					{
						BranchScopeUtils.EmitBracedArm( UniqueId, falseArm, m_inputPorts[ 0 ], varName, falseCode, node.ArmCondition( node.CurrentKeyword, true ), ref dataCollector );
					}
					else
					{
						dataCollector.AddLocalVariable( UniqueId, "\t" + outType + " " + varName + " = " + falseCode + ";", true );
					}
					dataCollector.AddLocalVariable( UniqueId, "#endif", true );
					m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );
				}
			}

			if ( node.EmitsRuntimeBranch( dataCollector ) )
			{
				m_outputPorts[ 0 ].SetLocalValue( "dynamicSwitch" + OutputId, dataCollector.PortCategory );
			}
			else
			{
				m_outputPorts[ 0 ].SetLocalValue( "staticSwitch" + OutputId, dataCollector.PortCategory );
			}
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public override void DrawTitle( Rect titlePos )
		{
			bool referenceMode = m_staticSwitchVarMode == StaticSwitchVariableMode.Reference;
			string subTitle = string.Empty;
			string subTitleFormat = string.Empty;
			if( referenceMode )
			{
				// @diogo: an unassigned reference still reads as Ref( None ), not as this node's own Var( )
				subTitle = ( m_reference != null ) ? m_reference.GetPropertyValStr() : Constants.NoStringValue;
				subTitleFormat = Constants.SubTitleRefNameFormatStr;
			}
			else
			{
				subTitle = GetPropertyValStr();
				subTitleFormat = GetSubTitleVarNameFormatStr();
			}

			SetAdditonalTitleTextOnCallback( subTitle, subTitleFormat, ( instance, newSubTitle ) => instance.AdditonalTitleContent.text = string.Format( subTitleFormat, newSubTitle ) );

			if( !m_isEditing && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				// @diogo: only reference mode borrows the node type name; other modes show this node's own name
				GUI.Label( titlePos, referenceMode ? StaticSwitchStr : PropertyInspectorName, UIUtils.GetCustomStyle( CustomStyle.NodeTitle ) );
			}
		}

		public override void UpdateMaterial( Material mat )
		{
			base.UpdateMaterial( mat );
			if( UIUtils.IsProperty( m_currentParameterType ) && !InsideShaderFunction )
			{
				if( m_keywordModeType == KeywordModeType.KeywordEnum )
				{
					for( int i = 0; i < m_keywordEnumAmount; i++ )
					{
						string key = KeywordEnum( i );
						mat.DisableKeyword( key );
					}
					// @diogo: the none entry has no keyword of its own, it just leaves every other one disabled
					if ( m_materialValue < m_keywordEnumAmount )
					{
						mat.EnableKeyword( KeywordEnum( m_materialValue ) );
					}
					mat.SetFloat( m_propertyName, m_materialValue );
				}
				else
				{
					int final = m_materialValue;
					if( m_keywordModeType == KeywordModeType.ToggleOff )
						final = final == 1 ? 0 : 1;
					mat.SetFloat( m_propertyName, m_materialValue );
					if( final == 1 )
						mat.EnableKeyword( GetPropertyValStr() );
					else
						mat.DisableKeyword( GetPropertyValStr() );
				}
			}
		}

		public override void SetMaterialMode( Material mat, bool fetchMaterialValues )
		{
			base.SetMaterialMode( mat, fetchMaterialValues );
			if( fetchMaterialValues && m_materialMode && UIUtils.IsProperty( m_currentParameterType ) && mat.HasProperty( m_propertyName ) )
			{
				m_materialValue = mat.GetInt( m_propertyName );
			}
		}

		public override void ForceUpdateFromMaterial( Material material )
		{
			if( UIUtils.IsProperty( m_currentParameterType ) && material.HasProperty( m_propertyName ) )
			{
				m_materialValue = material.GetInt( m_propertyName );
				PreviewIsDirty = true;
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_multiCompile = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 14403 )
			{
				m_defaultValue = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				if( UIUtils.CurrentShaderVersion() > 14101 )
				{
					m_materialValue = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}
			}
			else
			{
				m_defaultValue = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) ) ? 1 : 0;
				if( UIUtils.CurrentShaderVersion() > 14101 )
				{
					m_materialValue = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) ) ? 1 : 0;
				}
			}

			if( UIUtils.CurrentShaderVersion() > 13104 )
			{
				m_materialToggle = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_currentKeyword = GetCurrentParam( ref nodeParams );
				m_currentKeywordId = UIUtils.GetKeywordId( m_currentKeyword );
			}
			if( UIUtils.CurrentShaderVersion() > 14001 )
			{
				m_keywordModeType = (KeywordModeType)Enum.Parse( typeof( KeywordModeType ), GetCurrentParam( ref nodeParams ) );
			}

			// @diogo: the none entry sits past the last keyword, but its flag only loads further down, so the
			// amount setter below clamps a selection pointing at it; keep the raw values around to restore it
			int loadedDefaultValue = m_defaultValue;
			int loadedMaterialValue = m_materialValue;

			if( UIUtils.CurrentShaderVersion() > 14403 )
			{
				KeywordEnumAmount = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				for( int i = 0; i < KeywordEnumAmount; i++ )
				{
					m_defaultKeywordNames[ i ] = GetCurrentParam( ref nodeParams );
				}

				UpdateLabels();
			}

			if( UIUtils.CurrentShaderVersion() > 16304 )
			{
				string currentVarMode = GetCurrentParam( ref nodeParams );
				CurrentVarMode = (StaticSwitchVariableMode)Enum.Parse( typeof( StaticSwitchVariableMode ), currentVarMode );
				if( CurrentVarMode == StaticSwitchVariableMode.Reference )
				{
					m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}
			}
			else
			{
				CurrentVarMode = (StaticSwitchVariableMode)m_variableMode;
				//Resetting m_variableMode to its default value since it will no longer be used and interfere released ransom properties behavior
				m_variableMode = VariableMode.Create;
			}

			if( CurrentVarMode == StaticSwitchVariableMode.Reference )
			{
				UIUtils.UnregisterPropertyNode( this );
			}
			else
			{
				if ( MaterialToggleActive )
				{
					UIUtils.RegisterPropertyNode( this );
				}
				else
				{
					UIUtils.UnregisterPropertyNode( this );
				}
			}

			if( UIUtils.CurrentShaderVersion() > 16700 )
			{
				m_isLocal = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 18401 )
				m_lockKeyword = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );

			if( UIUtils.CurrentShaderVersion() > 18928 )
				m_shaderStage = (ShaderStage)Enum.Parse( typeof(ShaderStage), GetCurrentParam( ref nodeParams ) );

			if ( UIUtils.CurrentShaderVersion() >= 19913 )
			{
				m_unifiedBranch = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_includeNoneEntry = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				m_generateKeyword = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
				if ( m_includeNoneEntry )
				{
					m_defaultValue = loadedDefaultValue;
					m_materialValue = loadedMaterialValue;
					UpdateLabels();
				}
			}

			SetMaterialToggleRetrocompatibility();

			if( !m_isNodeBeingCopied && CurrentVarMode != StaticSwitchVariableMode.Reference )
			{
				ContainerGraph.StaticSwitchNodes.UpdateDataOnNode( UniqueId, DataToArray );
			}
		}

		public override void ReleaseRansomedProperty()
		{
			//on old ASE, the property node m_variableMode was used on defining the static switch type, now we have a specific m_staticSwitchVarMode over here
			//the problem with this is the fix made to release ransomend property names( hash deb232819fff0f1aeaf029a21c55ef597b3424de ) uses m_variableMode and
			//makes old static switches to attempt and register an already registered name when doing this:
			//CurrentVariableMode = VariableMode.Create;
			//So we need to disable this release ransom property behavior as m_variableMode should never be on VariableMode.Create
			//The m_variableMode is set to its default value over the ReadFromString method after its value as been set over the new m_staticSwitchVarMode variable
		}

		void SetMaterialToggleRetrocompatibility()
		{
			if( UIUtils.CurrentShaderVersion() < 17108 )
			{
				if( !m_materialToggle && m_staticSwitchVarMode == StaticSwitchVariableMode.Create )
				{
					if( m_keywordModeType != KeywordModeType.KeywordEnum )
					{
						m_propertyName = m_propertyName.ToUpper() + "_ON";
					}
					else
					{
						m_propertyName = m_propertyName.ToUpper();
						for( int i = 0; i < m_keywordEnumList.Length; i++ )
						{
							m_keywordEnumList[ i ] = "_" + m_keywordEnumList[ i ].ToUpper();
						}
					}
					m_autoGlobalName = false;
				}
			}
		}

		public override void ReadFromDeprecated( ref string[] nodeParams, Type oldType = null )
		{
			base.ReadFromDeprecated( ref nodeParams, oldType );
			{
				m_currentKeyword = GetCurrentParam( ref nodeParams );
				m_currentKeywordId = UIUtils.GetKeywordId( m_currentKeyword );
				m_materialToggle = false;
				m_keywordModeType = KeywordModeType.Toggle;
				m_variableMode = VariableMode.Fetch;
				CurrentVarMode = StaticSwitchVariableMode.Fetch;
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_multiCompile );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_defaultValue );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_materialValue );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_materialToggle );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_currentKeyword );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_keywordModeType );
			IOUtils.AddFieldValueToString( ref nodeInfo, KeywordEnumAmount );
			for( int i = 0; i < KeywordEnumAmount; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_keywordEnumList[ i ] );
			}

			IOUtils.AddFieldValueToString( ref nodeInfo, CurrentVarMode );
			if( CurrentVarMode == StaticSwitchVariableMode.Reference )
			{
				int referenceId = ( m_reference != null ) ? m_reference.UniqueId : -1;
				IOUtils.AddFieldValueToString( ref nodeInfo, referenceId );
			}
			IOUtils.AddFieldValueToString( ref nodeInfo, m_isLocal );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_lockKeyword );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_shaderStage );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_unifiedBranch );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_includeNoneEntry );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_generateKeyword );
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			CheckReferenceValues( true );
		}

		public override void ReconnectClipboardReferences( Clipboard clipboard )
		{
			// validate node first
			int newId = clipboard.GeNewNodeId( m_referenceNodeId );
			if ( ContainerGraph.GetNode( newId ) != null )
			{
				m_referenceNodeId = newId;
			}
			RefreshExternalReferences();
		}

		StaticSwitchVariableMode CurrentVarMode
		{
			get { return m_staticSwitchVarMode; }
			set
			{
				if( m_staticSwitchVarMode != value )
				{
					if( value == StaticSwitchVariableMode.Reference )
					{
						ContainerGraph.StaticSwitchNodes.RemoveNode( this );
						m_referenceArrayId = -1;
						m_referenceNodeId = -1;
						m_reference = null;
						m_headerColorModifier = ReferenceHeaderColor;
					}
					else
					{
						m_headerColorModifier = Color.white;
						ContainerGraph.StaticSwitchNodes.AddNode( this );
						UpdateLabels();
					}
				}
				m_staticSwitchVarMode = value;
			}
		}
		public bool IsStaticSwitchDirty { get { return m_isStaticSwitchDirty; } }
		public StaticSwitch Reference { get { return m_reference; } }
		// @diogo: m_referenceNodeId is only cleared on entering Reference mode, not on leaving it, so also gate on mode
		public override int ReferencedNodeId { get { return ( m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_referenceNodeId > -1 ) ? m_referenceNodeId : base.ReferencedNodeId; } }
		public override bool HasMissingReference { get { return m_staticSwitchVarMode == StaticSwitchVariableMode.Reference && m_reference == null; } }
		public KeywordModeType KeywordModeTypeValue { get { return m_keywordModeType; } }
		public int DefaultValue { get { return m_defaultValue; } }
		public int MaterialValue { get { return m_materialValue; } }
		// @diogo: a stage-specific keyword is undefined in the other stages, so a condition built from it would
		// gate the interpolator struct differently in the vertex and fragment compiles of one variant (#443)
		private string ArmCondition( string keyword, bool negated )
		{
			string condition = null;
			if ( m_shaderStage == ShaderStage.All )
			{
				condition = ( negated ? "!" : string.Empty ) + "defined( " + keyword + " )";
			}
			return condition;
		}

		//public string CurrentKeyword { get { return m_currentKeyword; } }
		public string CurrentKeyword
		{
			get
			{
				if( CurrentVarMode == StaticSwitchVariableMode.Fetch )
					return m_currentKeyword;

				return ( m_lockKeyword || string.IsNullOrEmpty( m_currentKeyword ) ? PropertyName + OnOffStr : m_currentKeyword );
			}
		}
		public bool MaterialToggleActive { get { return m_materialToggle && m_staticSwitchVarMode != StaticSwitchVariableMode.Fetch; } }

		// @diogo: the none entry only exists in enum mode, taking the port right after the last keyword
		private bool HasNoneEntry { get { return m_includeNoneEntry && m_keywordModeType == KeywordModeType.KeywordEnum; } }

		private int TotalEnumEntries { get { return m_keywordEnumAmount + ( HasNoneEntry ? 1 : 0 ); } }

		public int KeywordEnumAmount
		{
			get
			{
				return m_keywordEnumAmount;
			}
			set
			{
				m_keywordEnumAmount = value;
				m_defaultValue = Mathf.Clamp( m_defaultValue, 0, TotalEnumEntries - 1 );
				m_materialValue = Mathf.Clamp( m_materialValue, 0, TotalEnumEntries - 1 );
			}
		}
	}
}

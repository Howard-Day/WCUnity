// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Toggle Switch", "Logical Operators", "Switch between any of its input ports" )]
	public class ToggleSwitchNode : PropertyNode
	{
		public enum ToggleSwitchVariableMode
		{
			Create = 0,
			Reference = 1
		}

		private float InstanceIconWidth = 19;
		private float InstanceIconHeight = 19;
		private readonly Color ReferenceHeaderColor = new Color( 0f, 0.5f, 0.585f, 1.0f );

		private const string ToggleSwitchStr = "Toggle Switch";
		private const string ModeStr = "Mode";

		private const string InputPortName = "In ";
		private const string CurrSelectedStr = "Toggle Value";
		private const string NamelessStructError = "Toggle Switch node cannot declare a struct whose name did not reach its ports.";
		private const string CustomTypeError = "Toggle Switch node does not support custom types on its inputs.";

		private const string AssignKeywordStr = "Assign Keyword";
		private const string AssignKeywordInfoStr = "The material inspector will assign a {0}_ON keyword when the toggle is turned ON. " +
			"To use it, you'll need a Switch node to create the keyword variants";

		//private const string LerpOp = "lerp({0},{1},{2})";
		private const string LerpOp = "(( {2} )?( {1} ):( {0} ))";

		[SerializeField]
		private string[] AvailableInputsLabels = { "In 0", "In 1" };

		[SerializeField]
		private int[] AvailableInputsValues = { 0, 1 };

		[SerializeField]
		private int m_currentSelectedInput = 0;

		[SerializeField]
		private WirePortDataType m_mainDataType = WirePortDataType.FLOAT;

		[SerializeField]
		private bool m_assignKeyword = true;

		[SerializeField]
		private ToggleSwitchVariableMode m_toggleSwitchVarMode = ToggleSwitchVariableMode.Create;

		[SerializeField]
		private ToggleSwitchNode m_reference = null;

		[SerializeField]
		private int m_referenceArrayId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		private bool m_isToggleSwitchDirty = false;

		private Rect m_iconPos;

		private int m_cachedPropertyId = -1;

		private int m_lastSourceSelected = -1;

		private GUIContent m_popContent;

		private Rect m_varRect;
		private Rect m_imgRect;
		private bool m_editing;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( m_mainDataType, false, InputPortName + "0" );
			AddInputPort( m_mainDataType, false, InputPortName + "1" );

			// @diogo: a texture object is bound, not assigned, so neither arm survives the lerp this always emits
			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].AcceptAllTypes();
				m_inputPorts[ i ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
					WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );
			}

			AddOutputPort( m_mainDataType, " " );
			m_insideSize.Set( 80, 25 );
			m_currentParameterType = PropertyType.Property;
			m_customPrefix = "Toggle Switch ";

			m_popContent = new GUIContent();
			m_popContent.image = UIUtils.PopupIcon;

			m_availableAttribs.Clear();
			//Need to maintain this because of retrocompatibility reasons
			m_availableAttribs.Add( new PropertyAttributes( "Toggle", "[Toggle]" ) );

			m_drawAttributes = false;
			m_freeType = false;
			m_useVarSubtitle = true;
			m_showTitleWhenNotEditing = false;
			m_useInternalPortData = true;
			m_previewShaderGUID = "beeb138daeb592a4887454f81dba2b3f";

			m_allowPropertyDuplicates = false;
			m_showAutoRegisterUI = false;

			m_srpBatcherCompatible = true;
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			return received;
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();

			// @diogo: a reference borrows the source's property, so it must stay out of the property register
			if ( CurrentVarMode != ToggleSwitchVariableMode.Reference )
			{
				UIUtils.RegisterPropertyNode( this );
			}
			else
			{
				UIUtils.UnregisterPropertyNode( this );
			}

			if( CurrentVarMode != ToggleSwitchVariableMode.Reference )
			{
				ContainerGraph.ToggleSwitchNodes.AddNode( this );
			}

			if ( UniqueId > -1 )
			{
				ContainerGraph.ToggleSwitchNodes.OnReorderEventComplete -= OnReorderEventComplete;
				ContainerGraph.ToggleSwitchNodes.OnReorderEventComplete += OnReorderEventComplete;
			}
		}

		public override void Destroy()
		{
			base.Destroy();
			UIUtils.UnregisterPropertyNode( this );
			if( CurrentVarMode != ToggleSwitchVariableMode.Reference )
			{
				ContainerGraph.ToggleSwitchNodes.RemoveNode( this );
			}

			if( UniqueId > -1 )
				ContainerGraph.ToggleSwitchNodes.OnReorderEventComplete -= OnReorderEventComplete;
		}

		void OnReorderEventComplete()
		{
			if( CurrentVarMode == ToggleSwitchVariableMode.Reference )
			{
				if( m_reference != null )
				{
					m_referenceArrayId = ContainerGraph.ToggleSwitchNodes.GetNodeRegisterIdx( m_reference.UniqueId );
				}
			}
		}

		// @diogo: a reference with no inputs of its own acts as a pure alias of its source
		private bool IsSourceAlias { get { return m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference && m_reference != null && !m_inputPorts[ 0 ].IsConnected && !m_inputPorts[ 1 ].IsConnected; } }

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if ( m_cachedPropertyId == -1 )
				m_cachedPropertyId = Shader.PropertyToID( "_Current" );

			ToggleSwitchNode node = ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference && m_reference != null ) ? m_reference : this;

			PreviewMaterial.SetInt( m_cachedPropertyId, node.m_currentSelectedInput );

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

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );

			// @diogo: source edits only dirty the source's own preview; track its state so references re-render
			if ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference && m_reference != null && m_reference.m_currentSelectedInput != m_lastSourceSelected )
			{
				m_lastSourceSelected = m_reference.m_currentSelectedInput;
				PreviewIsDirty = true;
			}

			if ( IsSourceAlias && m_outputPorts[ 0 ].DataType != m_reference.m_outputPorts[ 0 ].DataType )
			{
				m_outputPorts[ 0 ].ChangeType( m_reference.m_outputPorts[ 0 ].DataType, false );
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

		public override void OnConnectedOutputNodeChanges( int portId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( portId, otherNodeId, otherPortId, name, type );
			UpdateConnection();
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnection();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateConnection();
		}

		void UpdateConnection()
		{
			// @diogo: an unconnected arm counts as FLOAT, as it always has, since it carries no type of
			// its own and still has to default. A struct is the one type let past that floor: nothing
			// casts to a struct, so a phantom float would drag the node off the only real type on it
			bool bothConnected = m_inputPorts[ 0 ].IsConnected && m_inputPorts[ 1 ].IsConnected;

			m_mainDataType = WirePortDataType.FLOAT;
			string structName = string.Empty;
			int highest = bothConnected ? -1 : UIUtils.GetPriority( WirePortDataType.FLOAT );

			for ( int i = 0; i < 2; i++ )
			{
				if ( m_inputPorts[ i ].IsConnected )
				{
					WirePort source = m_inputPorts[ i ].GetOutputConnection( 0 );
					int priority = UIUtils.GetPriority( source.DataType );

					// @diogo: ties go to the second arm, as they did when this was a single comparison
					if ( priority >= highest || ( !bothConnected && source.DataType == WirePortDataType.STRUCT ) )
					{
						m_mainDataType = source.DataType;
						// @diogo: a struct is named by the port, so the name travels with the type it won on
						structName = source.StructName;
						highest = priority;
					}
				}
			}

			m_inputPorts[ 0 ].ChangeType( m_mainDataType, structName, false );
			m_inputPorts[ 1 ].ChangeType( m_mainDataType, structName, false );


			//m_outputPorts[ 0 ].ChangeProperties( m_out, m_mainDataType, false );
			m_outputPorts[ 0 ].ChangeType( m_mainDataType, structName, false );
		}

		public override void OnNodeLayout( DrawInfo drawInfo, NodeUpdateCache cache )
		{
			base.OnNodeLayout( drawInfo, cache );

			m_varRect = m_remainingBox;
			m_varRect.width = 50 * drawInfo.InvertedZoom;
			m_varRect.height = 16 * drawInfo.InvertedZoom;
			m_varRect.x = m_remainingBox.xMax - m_varRect.width;
			m_varRect.y += 1 * drawInfo.InvertedZoom;

			m_imgRect = m_varRect;
			m_imgRect.x = m_varRect.xMax - 16 * drawInfo.InvertedZoom;
			m_imgRect.width = 16 * drawInfo.InvertedZoom;
			m_imgRect.height = m_imgRect.width;

			CheckReferenceValues( false );

			if ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference )
			{
				m_iconPos = m_globalPosition;
				m_iconPos.width = InstanceIconWidth * drawInfo.InvertedZoom;
				m_iconPos.height = InstanceIconHeight * drawInfo.InvertedZoom;

				m_iconPos.y = m_globalPosition.y + m_headerPosition.height * 0.5f - m_iconPos.height * 0.5f;
				m_iconPos.x += /*m_globalPosition.width - m_iconPos.width - */5 * drawInfo.InvertedZoom;
			}
		}

		// @diogo: a switch already in Reference mode borrows another node's property, so it is never a source
		public override bool CanBeReferenced { get { return m_toggleSwitchVarMode != ToggleSwitchVariableMode.Reference; } }

		protected override bool IsTitleEditable { get { return m_toggleSwitchVarMode != ToggleSwitchVariableMode.Reference; } }

		// @diogo: an alias shows the source's port defaults - those drive its result; with any own input
		// connected the node lerps its own ports again, so their own defaults return
		protected override InputPort GetPortInternalDataSource( int portIdx )
		{
			if ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference )
			{
				if ( IsSourceAlias )
				{
					InputPort sourcePort = m_reference.m_inputPorts[ portIdx ];
					return sourcePort.IsConnected ? null : sourcePort;
				}
				if ( m_reference == null )
				{
					return null;
				}
			}
			return base.GetPortInternalDataSource( portIdx );
		}

		public override void SetReferenceTo( PropertyNode reference )
		{
			SetToReference( reference as ToggleSwitchNode );
		}

		public void SetToReference( ToggleSwitchNode reference )
		{
			if ( reference == null || reference == this )
			{
				return;
			}

			// @diogo: the setter drops this node from the register, so the source index is only stable after it
			CurrentVarMode = ToggleSwitchVariableMode.Reference;
			UIUtils.UnregisterPropertyNode( this );

			m_referenceArrayId = ContainerGraph.ToggleSwitchNodes.GetNodeRegisterIdx( reference.UniqueId );
			m_reference = ContainerGraph.ToggleSwitchNodes.GetNode( m_referenceArrayId );
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
			if ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference )
			{
				if ( m_reference == null && m_referenceNodeId > 0 )
				{
					m_reference = ContainerGraph.GetNode( m_referenceNodeId ) as ToggleSwitchNode;
					m_referenceArrayId = ContainerGraph.ToggleSwitchNodes.GetNodeRegisterIdx( m_referenceNodeId );
				}

				// @diogo: a node in Reference mode can't itself be a source ( no chaining ), so a source
				// switched into Reference mode invalidates the link
				if ( m_reference != null && !m_reference.CanBeReferenced )
				{
					m_reference = null;
				}

				if ( m_reference != null )
				{
					if ( forceUpdate || m_reference.IsToggleSwitchDirty )
					{
						int count = m_inputPorts.Count;
						for ( int i = 0; i < count; i++ )
						{
							m_inputPorts[ i ].Name = m_reference.InputPorts[ i ].Name;
							m_inputPorts[ i ].Visible = m_reference.InputPorts[ i ].Visible;
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
				m_isToggleSwitchDirty = false;
			}
		}


		public override void DrawGUIControls( DrawInfo drawInfo )
		{
			base.DrawGUIControls( drawInfo );

			if ( drawInfo.CurrentEventType != EventType.MouseDown )
				return;

			if ( m_varRect.Contains( drawInfo.MousePosition ) )
			{
				m_editing = true;
			}
			else if ( m_editing )
			{
				m_editing = false;
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			if ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference )
				return;

			if ( m_editing )
			{
				EditorGUI.BeginChangeCheck();
				m_currentSelectedInput = EditorGUIIntPopup( m_varRect, m_currentSelectedInput, AvailableInputsLabels, AvailableInputsValues, UIUtils.SwitchNodePopUp );
				if ( EditorGUI.EndChangeCheck() )
				{
					PreviewIsDirty = true;
					UpdateConnection();
					m_requireMaterialUpdate = true;
					m_editing = false;
				}
			}
		}

		public override void OnNodeRepaint( DrawInfo drawInfo )
		{
			base.OnNodeRepaint( drawInfo );

			if ( !m_isVisible )
				return;

			if ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference )
			{
				GUI.Label( m_iconPos, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerTextureIcon ) );
				GUI.enabled = false;
			}

			if ( !m_editing && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD2 )
			{
				ToggleSwitchNode node = ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference && m_reference != null ) ? m_reference : this;

				GUI.Label( m_varRect, node.AvailableInputsLabels[ node.m_currentSelectedInput ], UIUtils.GraphDropDown );
				GUI.Label( m_imgRect, m_popContent, UIUtils.GraphButtonIcon );
			}

			if ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference )
			{
				GUI.enabled = true;
			}
		}

		public override void DrawMainPropertyBlock()
		{
			EditorGUILayout.BeginVertical();
			{
				ShowPropertyInspectorNameGUI();
				ShowPropertyNameGUI( true );
				ShowVariableMode();
				ShowHybridInstanced();
				ShowAutoRegister();
				ShowPrecision();
				m_assignKeyword = EditorGUILayoutToggle( AssignKeywordStr, m_assignKeyword );
				if ( m_assignKeyword )
				{
					EditorGUILayout.HelpBox( string.Format( AssignKeywordInfoStr, PropertyName.ToUpper() ), MessageType.Info );
				}
				ShowToolbar();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Separator();
			EditorGUI.BeginChangeCheck();
			m_currentSelectedInput = EditorGUILayoutIntPopup( CurrSelectedStr, m_currentSelectedInput, AvailableInputsLabels, AvailableInputsValues );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateConnection();
				m_requireMaterialUpdate = true;
			}
		}

		void PropertyGroup()
		{
			EditorGUI.BeginChangeCheck();
			var prevVarMode = CurrentVarMode;
			CurrentVarMode = (ToggleSwitchVariableMode)EditorGUILayoutEnumPopup( ModeStr, CurrentVarMode );
			if( EditorGUI.EndChangeCheck() )
			{
				if( CurrentVarMode == ToggleSwitchVariableMode.Reference )
				{
					UIUtils.UnregisterPropertyNode( this );
				}
				else
				{
					UIUtils.RegisterPropertyNode( this );
				}
			}

			UIUtils.DrawSeparator();

			if( CurrentVarMode == ToggleSwitchVariableMode.Reference )
			{
				string[] arr = ContainerGraph.ToggleSwitchNodes.NodesArr;
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

				arr = UIUtils.DisambiguateReferenceLabels( arr, ContainerGraph.ToggleSwitchNodes.NodesList, 0 );
				EditorGUILayout.BeginHorizontal();
				SearchableDropdown.Draw( Constants.AvailableReferenceStr, m_referenceArrayId, arr, ( idx ) =>
				{
					if( idx != m_referenceArrayId )
					{
						UndoRecordObject( "Selected toggle switch reference" );
						m_referenceArrayId = idx;
						m_reference = ContainerGraph.ToggleSwitchNodes.GetNode( m_referenceArrayId );
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

			DrawMainPropertyBlock();
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

					if( CurrentVarMode != ToggleSwitchVariableMode.Reference )
					{
						ContainerGraph.ToggleSwitchNodes.UpdateDataOnNode( UniqueId, DataToArray );
					}
				}
			}
		}

		// @diogo: renames also commit outside the inspector check ( duplicate dedup, panel renames ); keep
		// the sorted register in sync on every commit path or its order goes stale until the next reload
		public override void OnPropertyNameChanged()
		{
			base.OnPropertyNameChanged();
			if ( CurrentVarMode != ToggleSwitchVariableMode.Reference )
			{
				ContainerGraph.ToggleSwitchNodes.UpdateDataOnNode( UniqueId, DataToArray );
			}
		}

		public override void DrawProperties()
		{
			//base.DrawProperties();
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, Constants.ParameterLabelStr, PropertyGroup );
			NodeUtils.DrawPropertyGroup( ref m_visibleCustomAttrFoldout, CustomAttrStr, DrawCustomAttributes, DrawCustomAttrAddRemoveButtons );
		}

		// @diogo: mirrors StaticSwitch: the lerp reads the source's property, so a reference must declare
		// the source's property instead of its own unused one — the source may not be connected at all
		protected override void RegisterProperty( ref MasterNodeDataCollector dataCollector )
		{
			if ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference && m_reference != null )
			{
				m_reference.RegisterProperty( ref dataCollector );
			}
			else
			{
				base.RegisterProperty( ref dataCollector );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			// @diogo: only the scoped form caches, the ternary one never sets a local value
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			}

			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			if ( ReportStructMix( 0, 1 ) )
			{
				return m_outputPorts[ 0 ].ErrorValue;
			}

			// @diogo: nothing names the wildcard on the wire, so the local every form below declares
			// would come out with no type at all
			if ( m_outputPorts[ 0 ].DataType == WirePortDataType.OBJECT )
			{
				UIUtils.ShowMessage( UniqueId, CustomTypeError, MessageSeverity.Error );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			// @diogo: a struct names itself on the port, since the wire type alone cannot spell it
			bool isStruct = m_outputPorts[ 0 ].DataType == WirePortDataType.STRUCT;
			if ( isStruct && string.IsNullOrEmpty( m_outputPorts[ 0 ].StructName ) )
			{
				UIUtils.ShowMessage( UniqueId, NamelessStructError, MessageSeverity.Error );
				return m_outputPorts[ 0 ].ErrorValue;
			}

			m_precisionString = isStruct ? m_outputPorts[ 0 ].StructName : UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_outputPorts[ 0 ].DataType );


			ToggleSwitchNode node = ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference && m_reference != null ) ? m_reference : this;

			// @diogo: with no inputs of its own, a reference outputs its source's result instead of lerping defaults
			if ( IsSourceAlias )
			{
				return node.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			}

			// @diogo: both arms are laid out before either is generated, so each knows upfront if it owns its value (#365)
			BranchArmAnalysis armA = BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ 0 ], ref dataCollector );
			BranchArmAnalysis armB = BranchScopeUtils.AnalyzeArm( this, m_inputPorts[ 1 ], ref dataCollector );

			string result = string.Empty;
			string resultA = null;
			string resultB = null;

			// @diogo: HLSL has no ternary on a struct type, so a struct always takes the branch form, which
			// assigns into a local instead of producing an expression
			if ( armA.Scoped || armB.Scoped || isStruct )
			{
				if ( armA.Scoped )
				{
					BranchScopeUtils.PreGenerateFrontier( armA, ref dataCollector );
				}
				else
				{
					resultA = BranchScopeUtils.PreGenerateFallback( armA, m_inputPorts[ 0 ], ref dataCollector, m_mainDataType, ignoreLocalvar, true );
				}

				if ( armB.Scoped )
				{
					BranchScopeUtils.PreGenerateFrontier( armB, ref dataCollector );
				}
				else
				{
					resultB = BranchScopeUtils.PreGenerateFallback( armB, m_inputPorts[ 1 ], ref dataCollector, m_mainDataType, ignoreLocalvar, true );
				}

				string varName = "toggleSwitch" + OutputId;

				dataCollector.AddLocalVariable( UniqueId, m_precisionString + " " + varName + " = ( " + m_precisionString + " )0;", true );
				dataCollector.AddLocalVariable( UniqueId, "if( " + node.m_propertyName + " )", true );
				dataCollector.AddLocalVariable( UniqueId, "{", true );
				if ( armB.Scoped )
				{
					// @diogo: an open scope indents everything it emits, so only a flat arm needs a literal tab
					dataCollector.PushBranchScope( uniformCondition: true );
					resultB = m_inputPorts[ 1 ].GenerateShaderForOutput( ref dataCollector, m_mainDataType, ignoreLocalvar, true );
					dataCollector.AddLocalVariable( UniqueId, varName + " = " + resultB + ";", true );
					dataCollector.PopBranchScope();
				}
				else
				{
					dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + resultB + ";", true );
				}
				dataCollector.AddLocalVariable( UniqueId, "}", true );
				dataCollector.AddLocalVariable( UniqueId, "else", true );
				dataCollector.AddLocalVariable( UniqueId, "{", true );
				if ( armA.Scoped )
				{
					dataCollector.PushBranchScope( uniformCondition: true );
					resultA = m_inputPorts[ 0 ].GenerateShaderForOutput( ref dataCollector, m_mainDataType, ignoreLocalvar, true );
					dataCollector.AddLocalVariable( UniqueId, varName + " = " + resultA + ";", true );
					dataCollector.PopBranchScope();
				}
				else
				{
					dataCollector.AddLocalVariable( UniqueId, "\t" + varName + " = " + resultA + ";", true );
				}
				dataCollector.AddLocalVariable( UniqueId, "}", true );

				m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );
				result = varName;
			}
			else
			{
				resultA = m_inputPorts[ 0 ].GenerateShaderForOutput( ref dataCollector, m_mainDataType, ignoreLocalvar, true );
				resultB = m_inputPorts[ 1 ].GenerateShaderForOutput( ref dataCollector, m_mainDataType, ignoreLocalvar, true );
				result = string.Format( LerpOp, resultA, resultB, node.m_propertyName );
			}
			return result;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_currentSelectedInput = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 18806 )
			{
				m_assignKeyword = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			if( UIUtils.CurrentShaderVersion() >= 19902 )
			{
				string currentVarMode = GetCurrentParam( ref nodeParams );
				CurrentVarMode = (  ToggleSwitchVariableMode )Enum.Parse( typeof( ToggleSwitchVariableMode ), currentVarMode );
				if ( CurrentVarMode == ToggleSwitchVariableMode.Reference )
				{
					m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}
			}
			else
			{
				CurrentVarMode = ToggleSwitchVariableMode.Create;
			}

			// @diogo: base.ReadFromString registers this as a property and the CurrentVarMode setter only
			// drops the toggle switch register, so realign the property register with the loaded mode
			if ( CurrentVarMode == ToggleSwitchVariableMode.Reference )
			{
				UIUtils.UnregisterPropertyNode( this );
			}
			else
			{
				UIUtils.RegisterPropertyNode( this );
			}

			// @diogo: register add ran before the name was read; re-push it like its sorted siblings do,
			// otherwise the last-loaded toggle stays sorted under its default name
			if ( !m_isNodeBeingCopied && CurrentVarMode != ToggleSwitchVariableMode.Reference )
			{
				ContainerGraph.ToggleSwitchNodes.UpdateDataOnNode( UniqueId, DataToArray );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_currentSelectedInput );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_assignKeyword );
			IOUtils.AddFieldValueToString( ref nodeInfo, CurrentVarMode );
			if ( CurrentVarMode == ToggleSwitchVariableMode.Reference )
			{
				int referenceId = ( m_reference != null ) ? m_reference.UniqueId : -1;
				IOUtils.AddFieldValueToString( ref nodeInfo, referenceId );
			}
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			m_selectedAttribs.Clear();
			UpdateConnection();
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

		public override string GetPropertyValue()
		{
			string toggleAttribute = ( m_assignKeyword ) ? "[Toggle]":"[ToggleUI]";
			return PropertyAttributes + toggleAttribute + " " + m_propertyName + "( \"" + m_propertyInspectorName + "\", Float ) = " + m_currentSelectedInput;
		}

		public override string GetUniformValue()
		{
			int index = m_containerGraph.IsSRP ? 1 : 0;
			return string.Format( Constants.UniformDec[ index ], UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, WirePortDataType.FLOAT ), m_propertyName );
		}

		public override bool GetUniformData( out string dataType, out string dataName, ref bool fullValue )
		{
			dataType = UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, WirePortDataType.FLOAT );
			dataName = m_propertyName;
			return true;
		}

		public override void DrawTitle( Rect titlePos )
		{
			bool referenceMode = m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference;
			string subTitle = string.Empty;
			string subTitleFormat = string.Empty;
			if( referenceMode )
			{
				// @diogo: an unassigned reference still reads as Ref( None ), not as this node's own Var( )
				subTitle = ( m_reference != null ) ? m_reference.PropertyName : Constants.NoStringValue;
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
				if ( CurrentVarMode == ToggleSwitchVariableMode.Create )
				{
					GUI.Label( titlePos, PropertyInspectorName, UIUtils.GetCustomStyle( CustomStyle.NodeTitle ) );
				}
				else
				{
					GUI.Label( titlePos, ToggleSwitchStr, UIUtils.GetCustomStyle( CustomStyle.NodeTitle ) );
				}
			}
		}

		public override void UpdateMaterial( Material mat )
		{
			base.UpdateMaterial( mat );
			if ( UIUtils.IsProperty( m_currentParameterType ) && !InsideShaderFunction )
			{
				mat.SetFloat( m_propertyName, ( float ) m_currentSelectedInput );
			}
		}

		public override void SetMaterialMode( Material mat , bool fetchMaterialValues )
		{
			base.SetMaterialMode( mat , fetchMaterialValues );
			if ( fetchMaterialValues && m_materialMode && UIUtils.IsProperty( m_currentParameterType ) && mat.HasProperty( m_propertyName ) )
			{
				m_currentSelectedInput = ( int ) mat.GetFloat( m_propertyName );
			}
		}

		public override void ForceUpdateFromMaterial( Material material )
		{
			if( UIUtils.IsProperty( m_currentParameterType ) && material.HasProperty( m_propertyName ) )
			{
				m_currentSelectedInput = (int)material.GetFloat( m_propertyName );
				PreviewIsDirty = true;
			}
		}

		public override string GetPropertyValStr()
		{
			return PropertyName;			//return m_currentSelectedInput.ToString();
		}

		public bool IsToggleSwitchDirty { get { return m_isToggleSwitchDirty; } }
		public ToggleSwitchNode Reference { get { return m_reference; } }
		// @diogo: m_referenceNodeId is only cleared on entering Reference mode, not on leaving it, so also gate on mode
		public override int ReferencedNodeId { get { return ( m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference && m_referenceNodeId > -1 ) ? m_referenceNodeId : base.ReferencedNodeId; } }
		public override bool HasMissingReference { get { return m_toggleSwitchVarMode == ToggleSwitchVariableMode.Reference && m_reference == null; } }
		ToggleSwitchVariableMode CurrentVarMode
		{
			get { return m_toggleSwitchVarMode; }
			set
			{
				if ( m_toggleSwitchVarMode != value )
				{
					if ( value == ToggleSwitchVariableMode.Reference )
					{
						ContainerGraph.ToggleSwitchNodes.RemoveNode( this );
						m_referenceArrayId = -1;
						m_referenceNodeId = -1;
						m_reference = null;
						m_headerColorModifier = ReferenceHeaderColor;
					}
					else
					{
						m_headerColorModifier = Color.white;
						ContainerGraph.ToggleSwitchNodes.AddNode( this );
					}
				}
				m_toggleSwitchVarMode = value;
			}
		}
	}
}

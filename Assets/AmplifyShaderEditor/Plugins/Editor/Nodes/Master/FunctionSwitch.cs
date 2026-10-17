// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Function Switch", "Functions", "Function Switch allows switching options at compile time for shader function", NodeAvailabilityFlags = (int)NodeAvailability.ShaderFunction )]
	public sealed class FunctionSwitch : ParentNode
	{
		private const string InputPortNameStr = "In ";

		private const string ToggleFalseStr = "False";
		private const string ToggleTrueStr = "True";

		private const string CurrSelectedStr = "Current";
		private const string MaxAmountStr = "Amount";
		private const int MaxAllowedAmount = 20;

		private const int MinComboSize = 50;
		private const int MaxComboSize = 105;

		[SerializeField]
		private string m_optionLabel = "Option";

		[SerializeField]
		private string[] AvailableInputsLabels = { "In 0", "In 1" };

		[SerializeField]
		private int[] AvailableInputsValues = { 0, 1 };

		[SerializeField]
		private int m_previousSelectedInput = 0;

		[SerializeField]
		private int m_currentSelectedInput = 0;

		[SerializeField]
		private int m_maxAmountInputs = 2;

		[SerializeField]
		private bool m_toggleMode = false;

		[SerializeField]
		private string[] m_optionNames = { "In 0", "In 1", "In 2", "In 3", "In 4", "In 5", "In 6", "In 7", "In 8", "In 9", "In 10", "In 11", "In 12", "In 13", "In 14", "In 15", "In 16", "In 17", "In 18", "In 19" };

		[SerializeField]
		private int m_orderIndex = -1;

		[SerializeField]
		private TexReferenceType m_referenceType = TexReferenceType.Object;

		[SerializeField]
		private FunctionSwitch m_functionSwitchReference = null;

		[SerializeField]
		private int m_referenceUniqueId = -1;

		[SerializeField]
		private bool m_validReference = false;

		private bool m_asDrawn = false;

		private GUIContent m_checkContent;
		private GUIContent m_popContent;

		private const double MaxTimestamp = 1;
		private bool m_nameModified = false;
		private double m_lastTimeNameModified = 0;

		private float InstanceIconWidth = 19;
		private float InstanceIconHeight = 19;
		// @diogo: neutral multiplier — the teal used by other reference nodes turns this orange header green;
		// a grey scale keeps the hue and just darkens, matching how the others read visually
		private readonly Color ReferenceHeaderColor = new Color( 0.5f, 0.5f, 0.5f, 1.0f );

		private Rect m_varRect;
		private Rect m_imgRect;
		private Rect m_iconPos;
		private bool m_editing;

		private int m_cachedPropertyId = -1;

		private bool m_dirtySettings = false;

		[SerializeField]
		private int m_refMaxInputs = -1;

		[SerializeField]
		private string m_refOptionLabel = string.Empty;

		[SerializeField]
		private int m_refSelectedInput = -1;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			for( int i = 0; i < MaxAllowedAmount; i++ )
			{
				AddInputPort( WirePortDataType.FLOAT, false, InputPortNameStr + i );
				m_inputPorts[ i ].Visible = ( i < 2 );
				m_inputPorts[ i ].SetFreeForAll();
			}
			AddOutputPort( WirePortDataType.FLOAT, " " );

			m_checkContent = new GUIContent();
			m_checkContent.image = UIUtils.CheckmarkIcon;

			m_popContent = new GUIContent();
			m_popContent.image = UIUtils.PopupIcon;

			m_textLabelWidth = 100;
			m_autoWrapProperties = true;
			m_insideSize.Set( 80, 25 );
			m_previewShaderGUID = "a58e46feaa5e3d14383bfeac24d008bc";
		}

		public void SetCurrentSelectedInput( int newValue, int prevValue )
		{
			m_previousSelectedInput = prevValue;
			if( m_validReference )
				m_currentSelectedInput = Mathf.Clamp( newValue, 0, m_refMaxInputs - 1 );
			else
				m_currentSelectedInput = Mathf.Clamp( newValue, 0, m_maxAmountInputs - 1 );
			m_outputPorts[ 0 ].ChangeType( m_inputPorts[ m_currentSelectedInput ].DataType, m_inputPorts[ m_currentSelectedInput ].StructName, false );
			PreviewIsDirty = true;
			ChangeSignalPropagation();
		}

		public int GetCurrentSelectedInput()
		{
			return m_currentSelectedInput;
		}


		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if( m_cachedPropertyId == -1 )
				m_cachedPropertyId = Shader.PropertyToID( "_Current" );

			PreviewMaterial.SetInt( m_cachedPropertyId, m_currentSelectedInput );
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			if( m_referenceType == TexReferenceType.Object )
			{
				UIUtils.RegisterFunctionSwitchNode( this );
			}
			else
			{
				if( ContainerGraph.ParentWindow.CustomGraph != null )
					UIUtils.RegisterFunctionSwitchNode( this );
				UIUtils.RegisterFunctionSwitchCopyNode( this );
			}
		}

		public override void Destroy()
		{
			base.Destroy();

			m_functionSwitchReference = null;
			m_referenceUniqueId = -1;

			if( m_referenceType == TexReferenceType.Object )
			{
				UIUtils.UnregisterFunctionSwitchNode( this );
			}
			else
			{
				UIUtils.UnregisterFunctionSwitchNode( this );
				UIUtils.UnregisterFunctionSwitchCopyNode( this );
			}
		}

		public override void OnConnectedOutputNodeChanges( int portId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( portId, otherNodeId, otherPortId, name, type );
			m_inputPorts[ portId ].MatchPortToConnection();
			if( portId == m_currentSelectedInput )
				m_outputPorts[ 0 ].ChangeType( m_inputPorts[ portId ].DataType, m_inputPorts[ portId ].StructName, false );
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			InputPort port = GetInputPortByUniqueId( portId );
			int arrayPos = m_inputPorts.IndexOf( port );
			if( activateNode && m_connStatus == NodeConnectionStatus.Connected && arrayPos == m_currentSelectedInput )
			{
				port.GetOutputNode().ActivateNode( m_activeNode, m_activePort, m_activeType );
			}

			OnNodeChange( new NodeUpdateCache() );
			SetSaveIsDirty();

			m_inputPorts[ portId ].MatchPortToConnection();
			if( arrayPos == m_currentSelectedInput )
				m_outputPorts[ 0 ].ChangeType( m_inputPorts[ portId ].DataType, m_inputPorts[ portId ].StructName, false );
		}

		public override void ActivateNode( int signalGenNodeId, int signalGenPortId, Type signalGenNodeType )
		{
			// @diogo: destroyed shells must ignore signals; destroy cascades and undo patching reach them via live neighbors
			if ( WasDestroyed )
			{
				return;
			}

			if( m_selfPowered )
				return;

			ConnStatus = m_restrictions.GetRestiction( signalGenNodeType, signalGenPortId ) ? NodeConnectionStatus.Error : NodeConnectionStatus.Connected;
			m_activeConnections += 1;

			m_activeType = signalGenNodeType;
			m_activeNode = signalGenNodeId;
			m_activePort = signalGenPortId;
			if( m_activeConnections == 1 )
				if( m_inputPorts[ m_currentSelectedInput ].IsConnected )
					m_inputPorts[ m_currentSelectedInput ].GetOutputNode().ActivateNode( signalGenNodeId, signalGenPortId, signalGenNodeType );

			// @diogo: activation is signal propagation ( re-runs on load / graph refresh ), not a user edit; dirtying
			// here flagged the graph modified on open. Base ParentNode omits it; real edits dirty via the change checks.
		}

		public override void DeactivateInputPortNode( int deactivatedPort, bool forceComplete )
		{
			InputPort port = GetInputPortByUniqueId( deactivatedPort );
			if( deactivatedPort == m_currentSelectedInput )
				port.GetOutputNode().DeactivateNode( deactivatedPort, false );
		}

		public override void DeactivateNode( int deactivatedPort, bool forceComplete )
		{
			// @diogo: destroyed shells must ignore signals; destroy cascades and undo patching reach them via live neighbors
			if ( WasDestroyed )
			{
				return;
			}

			if( m_selfPowered )
				return;

			// @diogo: see ActivateNode - no save-dirty on deactivation ( propagation, not a user edit )
			m_activeConnections -= 1;

			if( ( forceComplete || m_activeConnections <= 0 ) )
			{
				m_activeConnections = 0;
				ConnStatus = NodeConnectionStatus.Not_Connected;
				for( int i = 0; i < m_inputPorts.Count; i++ )
				{
					if( m_inputPorts[ i ].IsConnected && i == m_currentSelectedInput )
					{
						ParentNode node = m_inputPorts[ i ].GetOutputNode();
						if( node != null )
							node.DeactivateNode( deactivatedPort == -1 ? m_inputPorts[ i ].PortId : deactivatedPort, false );
					}
				}
			}
		}

		public void ChangeSignalPropagation()
		{
			if( m_previousSelectedInput != m_currentSelectedInput && ConnStatus == NodeConnectionStatus.Connected )
			{
				if( m_inputPorts[ m_previousSelectedInput ].IsConnected )
					m_inputPorts[ m_previousSelectedInput ].GetOutputNode().DeactivateNode( m_inputPorts[ m_previousSelectedInput ].PortId, false );

				if( m_inputPorts[ m_currentSelectedInput ].IsConnected )
					m_inputPorts[ m_currentSelectedInput ].GetOutputNode().ActivateNode( UniqueId, m_inputPorts[ m_currentSelectedInput ].PortId, m_activeType );
			}
		}

		public bool DrawOption( ParentNode owner, bool forceDraw = false )
		{
			if( !IsConnected && !forceDraw )
			{
				//EditorGUILayout.LabelField( "Not Connected" );
				return false;
			}

			if( m_asDrawn ) //used to prevent the same property to be drawn more than once
				return false;

			if( m_validReference )
			{
				return m_functionSwitchReference.DrawOption( owner, true );
			}

			int prev = m_currentSelectedInput;
			m_asDrawn = true;
			if( m_toggleMode )
			{
				m_currentSelectedInput = owner.EditorGUILayoutToggle( m_optionLabel, ( m_currentSelectedInput != 0 ? true : false ) ) ? 1 : 0;

				if( m_currentSelectedInput != prev )
				{
					SetCurrentSelectedInput( m_currentSelectedInput, prev );
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				m_currentSelectedInput = owner.EditorGUILayoutIntPopup( m_optionLabel, m_currentSelectedInput, AvailableInputsLabels, AvailableInputsValues );

				if( m_currentSelectedInput != prev )
				{
					SetCurrentSelectedInput( m_currentSelectedInput, prev );
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public void CheckReference()
		{
			if( m_referenceType != TexReferenceType.Instance )
			{
				m_validReference = false;
				return;
			}

			// @diogo: a node in Instance mode can't itself be a source ( no chaining ), so a source
			// switched into Instance mode ( or a missing one ) invalidates the link
			if ( m_functionSwitchReference == null || m_functionSwitchReference.m_referenceType == TexReferenceType.Instance )
			{
				m_validReference = false;
				ResetToSelf();
				return;
			}

			if( m_referenceUniqueId != m_functionSwitchReference.UniqueId )
			{
				UpdateFromSelected();
			}
			if( m_refSelectedInput != m_functionSwitchReference.GetCurrentSelectedInput() || m_refMaxInputs != m_functionSwitchReference.MaxAmountInputs || m_refOptionLabel != m_functionSwitchReference.OptionLabel )
			{
				UpdateFromSelected();
			}

			if( m_functionSwitchReference.DirtySettings )
			{
				UpdateFromSelected();
			}

			m_validReference = true;
		}

		void ResetToSelf()
		{
			m_functionSwitchReference = null;
			m_validReference = false;
			m_referenceUniqueId = -1;
			m_refMaxInputs = -1;
			m_refOptionLabel = string.Empty;
			m_refSelectedInput = -1;

			UpdateLabels();
			m_sizeIsDirty = true;
		}

		void UpdateFromSelected()
		{
			if( m_referenceUniqueId < 0 )
				return;

			m_functionSwitchReference = UIUtils.GetNode( m_referenceUniqueId ) as FunctionSwitch;
			if( m_functionSwitchReference != null )
			{
				m_validReference = true;
				for( int i = 0; i < MaxAllowedAmount; i++ )
				{
					m_inputPorts[ i ].Visible = ( i < m_functionSwitchReference.MaxAmountInputs );
					m_inputPorts[ i ].Name = m_functionSwitchReference.InputPorts[ i ].Name;
				}
				UpdateLabels();
				m_refMaxInputs = m_functionSwitchReference.m_maxAmountInputs;
				m_refOptionLabel = m_functionSwitchReference.OptionLabel;
				m_refSelectedInput = m_functionSwitchReference.GetCurrentSelectedInput();
				OrderIndex = m_functionSwitchReference.OrderIndex;

				SetCurrentSelectedInput( m_functionSwitchReference.GetCurrentSelectedInput(), m_currentSelectedInput );
			}

			m_sizeIsDirty = true;
			m_isDirty = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			{
				EditorGUI.BeginChangeCheck();
				m_referenceType = (TexReferenceType)EditorGUILayoutPopup( Constants.ReferenceTypeStr, (int)m_referenceType, Constants.ReferenceArrayLabels );
				if( EditorGUI.EndChangeCheck() )
				{
					if( m_referenceType == TexReferenceType.Object )
					{
						if( ContainerGraph.ParentWindow.CustomGraph == null )
							UIUtils.UnregisterFunctionSwitchCopyNode( this );
						UIUtils.RegisterFunctionSwitchNode( this );
						ResetToSelf();
						m_headerColorModifier = Color.white;
					}
					else
					{
						if( ContainerGraph.ParentWindow.CustomGraph == null )
							UIUtils.UnregisterFunctionSwitchNode( this );
						UIUtils.RegisterFunctionSwitchCopyNode( this );
						m_headerColorModifier = ReferenceHeaderColor;
					}
				}

				UIUtils.DrawSeparator();

				if( m_referenceType == TexReferenceType.Instance )
				{
					// @diogo: the register keeps registration order, which differs between a live session and a
					// reload; sort a copy like the other reference pickers so the list is stable across save/load
					List<FunctionSwitch> switches = new List<FunctionSwitch>( UIUtils.FunctionSwitchList() );
					switches.Sort( ( a, b ) =>
					{
						int result = a.OptionLabel.CompareTo( b.OptionLabel );
						return ( result != 0 ) ? result : a.UniqueId.CompareTo( b.UniqueId );
					} );
					string[] arr = new string[ switches.Count ];
					int[] ids = new int[ switches.Count ];
					for( int i = 0; i < arr.Length; i++ )
					{
						arr[ i ] = switches[ i ].OptionLabel;
						ids[ i ] = switches[ i ].UniqueId;
					}
					arr = UIUtils.DisambiguateReferenceLabels( arr, switches, 0 );
					EditorGUILayout.BeginHorizontal();
					SearchableDropdown.Draw( Constants.AvailableReferenceStr, Array.IndexOf( ids, m_referenceUniqueId ), arr, ( idx ) =>
					{
						if( idx > -1 && idx < ids.Length && ids[ idx ] != m_referenceUniqueId )
						{
							UndoRecordObject( "Selected function switch reference" );
							m_referenceUniqueId = ids[ idx ];
							UpdateFromSelected();
							SetDirtyFromAsyncEdit();
						}
					} );
					UIUtils.DrawReferenceFocusButton( UIUtils.GetNode( m_referenceUniqueId ) );
					EditorGUILayout.EndHorizontal();

					// @diogo: bailing without closing the outer change check leaks it onto the caller's
					// stack and flags the SF as modified just for drawing this panel
					if( EditorGUI.EndChangeCheck() )
					{
						m_dirtySettings = true;
					}
					return;
				}

				EditorGUI.BeginChangeCheck();
				m_optionLabel = EditorGUILayoutTextField( "Option Label", m_optionLabel );
				if( EditorGUI.EndChangeCheck() )
				{
					m_optionLabel = UIUtils.RemoveInvalidEnumCharacters( m_optionLabel );
					if( string.IsNullOrEmpty( m_optionLabel ) )
					{
						m_optionLabel = "Option";
					}

					UIUtils.UpdateFunctionSwitchData( UniqueId, m_optionLabel );
				}

				EditorGUI.BeginChangeCheck();
				m_toggleMode = EditorGUILayoutToggle( "Toggle Mode", m_toggleMode );
				if( EditorGUI.EndChangeCheck() )
				{
					UpdateLabels();
					m_sizeIsDirty = true;
				}

				if( !m_toggleMode )
				{
					EditorGUI.BeginChangeCheck();
					m_maxAmountInputs = EditorGUILayoutIntSlider( MaxAmountStr, m_maxAmountInputs, 2, MaxAllowedAmount );
					if( EditorGUI.EndChangeCheck() )
					{
						UpdateLabels();
						m_sizeIsDirty = true;
					}

					EditorGUI.indentLevel++;
					for( int i = 0; i < m_maxAmountInputs; i++ )
					{
						EditorGUI.BeginChangeCheck();
						m_inputPorts[ i ].Name = EditorGUILayoutTextField( "Item " + i, m_inputPorts[ i ].Name );
						if( EditorGUI.EndChangeCheck() )
						{
							m_nameModified = true;
							m_lastTimeNameModified = EditorApplication.timeSinceStartup;
							m_inputPorts[ i ].Name = UIUtils.RemoveInvalidEnumCharacters( m_inputPorts[ i ].Name );
							m_optionNames[ i ] = m_inputPorts[ i ].Name;
							if( string.IsNullOrEmpty( m_inputPorts[ i ].Name ) )
							{
								m_inputPorts[ i ].Name = InputPortNameStr + i;
							}
							m_sizeIsDirty = true;
						}
					}
					EditorGUI.indentLevel--;

					if( m_nameModified )
					{
						UpdateLabels();
					}
				}

				if( m_toggleMode )
				{
					EditorGUI.BeginChangeCheck();
					int prevVal = m_currentSelectedInput;
					m_currentSelectedInput = EditorGUILayoutToggle( CurrSelectedStr, ( m_currentSelectedInput != 0 ? true : false ) ) ? 1 : 0;
					if( EditorGUI.EndChangeCheck() )
						SetCurrentSelectedInput( m_currentSelectedInput, prevVal );
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					int prevVal = m_currentSelectedInput;
					m_currentSelectedInput = EditorGUILayoutIntPopup( CurrSelectedStr, m_currentSelectedInput, AvailableInputsLabels, AvailableInputsValues );
					if( EditorGUI.EndChangeCheck() )
					{
						SetCurrentSelectedInput( m_currentSelectedInput, prevVal );
					}
				}
			}
			if( EditorGUI.EndChangeCheck() )
			{
				m_dirtySettings = true;
			}
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			if( UIUtils.CurrentShaderVersion() > 14205 )
			{
				if( m_referenceType == TexReferenceType.Instance )
				{
					m_functionSwitchReference = UIUtils.GetNode( m_referenceUniqueId ) as FunctionSwitch;
					UpdateFromSelected();
				}
			}

			SetCurrentSelectedInput( m_currentSelectedInput, m_previousSelectedInput );
		}

		public override void ReconnectClipboardReferences( Clipboard clipboard )
		{
			// validate node first
			int newId = clipboard.GeNewNodeId( m_referenceUniqueId );
			if ( ContainerGraph.GetNode( newId ) != null )
			{
				m_referenceUniqueId = newId;
			}
			RefreshExternalReferences();
		}

		public void UpdateLabels()
		{
			if ( ToggleMode )
			{
				m_inputPorts[ 0 ].Name = ToggleFalseStr;
				m_inputPorts[ 1 ].Name = ToggleTrueStr;
			}
			else
			{
				for ( int i = 0; i < MaxAllowedAmount; i++ )
				{
					m_inputPorts[ i ].Name = OptionNames[ i ];
				}
			}

			for ( int i = 0; i < MaxAllowedAmount; i++ )
			{
				m_inputPorts[ i ].Visible = ( i < MaxAmountInputs );
			}

			if ( m_currentSelectedInput >= MaxAmountInputs )
			{
				m_currentSelectedInput = MaxAmountInputs - 1;
			}

			AvailableInputsLabels = new string[ MaxAmountInputs ];
			AvailableInputsValues = new int[ MaxAmountInputs ];

			for ( int i = 0; i < MaxAmountInputs; i++ )
			{
				AvailableInputsLabels[ i ] = OptionNames[ i ];
				AvailableInputsValues[ i ] = i;
			}
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			CheckReference();
			if( m_dirtySettings )
				m_dirtySettings = false;
		}

		public override void OnNodeLayout( DrawInfo drawInfo, NodeUpdateCache cache )
		{
			float finalSize = 0;
			if( !m_toggleMode )
			{
				GUIContent dropdown = new GUIContent( m_inputPorts[ m_currentSelectedInput ].Name );
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

			base.OnNodeLayout( drawInfo, cache );

			bool toggleMode = m_toggleMode;
			if( m_validReference )
			{
				toggleMode = m_functionSwitchReference.m_toggleMode;
			}

			if( toggleMode )
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

			if( m_referenceType == TexReferenceType.Instance )
			{
				m_iconPos = m_globalPosition;
				m_iconPos.width = InstanceIconWidth * drawInfo.InvertedZoom;
				m_iconPos.height = InstanceIconHeight * drawInfo.InvertedZoom;

				m_iconPos.y = m_globalPosition.y + m_headerPosition.height * 0.5f - m_iconPos.height * 0.5f;
				m_iconPos.x += 5 * drawInfo.InvertedZoom;
			}
		}

		public override void DrawGUIControls( DrawInfo drawInfo )
		{
			// @diogo: gate on mode, not on a resolved source — an unassigned reference must not enter value
			// editing either, or m_editing sticks and hides the widget
			if( m_referenceType == TexReferenceType.Instance )
			{
				base.DrawGUIControls( drawInfo );
			}
			else
			{
				base.DrawGUIControls( drawInfo );

				if( drawInfo.CurrentEventType != EventType.MouseDown )
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
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			if( m_nameModified )
			{
				if( ( EditorApplication.timeSinceStartup - m_lastTimeNameModified ) > MaxTimestamp )
				{
					m_nameModified = false;
				}
			}

			if( m_validReference )
			{
				// @diogo: key the cache on the format too, or a source sharing the label of whatever was shown
				// last (e.g. a duplicate's own value) never replaces the subtitle
				SetAdditonalTitleTextOnCallback( m_functionSwitchReference.OptionLabel, Constants.SubTitleRefNameFormatStr, ( instance, newSubTitle ) => instance.AdditonalTitleContent.text = string.Format( Constants.SubTitleRefNameFormatStr, newSubTitle ) );
			}
			else if( m_referenceType == TexReferenceType.Instance )
			{
				// @diogo: an unassigned reference still reads as Ref( None ), not as this node's own value
				SetAdditonalTitleTextOnCallback( Constants.NoStringValue, Constants.SubTitleRefNameFormatStr, ( instance, newSubTitle ) => instance.AdditonalTitleContent.text = string.Format( Constants.SubTitleRefNameFormatStr, newSubTitle ) );
			}
			else
			{
				SetAdditonalTitleTextOnCallback( m_optionLabel, Constants.SubTitleValueFormatStr, ( instance, newSubTitle ) => instance.AdditonalTitleContent.text = string.Format( Constants.SubTitleValueFormatStr, newSubTitle ) );

				if( m_editing )
				{
					if( m_toggleMode )
					{
						if( GUI.Button( m_varRect, GUIContent.none, UIUtils.GraphButton ) )
						{
							UndoRecordObject( "Changing value Option on node Function Switch" );
							PreviewIsDirty = true;
							int prevVal = m_currentSelectedInput;
							m_currentSelectedInput = m_currentSelectedInput == 1 ? 0 : 1;
							if( m_currentSelectedInput != prevVal )
								SetCurrentSelectedInput( m_currentSelectedInput, prevVal );
							m_editing = false;
						}

						if( m_currentSelectedInput == 1 )
						{
							GUI.Label( m_varRect, m_checkContent, UIUtils.GraphButtonIcon );
						}
					}
					else
					{
						EditorGUI.BeginChangeCheck();
						int prevVal = m_currentSelectedInput;
						m_currentSelectedInput = EditorGUIIntPopup( m_varRect, m_currentSelectedInput, AvailableInputsLabels, AvailableInputsValues, UIUtils.GraphDropDown );
						if( EditorGUI.EndChangeCheck() )
						{
							PreviewIsDirty = true;
							SetCurrentSelectedInput( m_currentSelectedInput, prevVal );
							m_editing = false;
						}
					}
				}
			}

		}

		public override void OnNodeRepaint( DrawInfo drawInfo )
		{
			base.OnNodeRepaint( drawInfo );

			if( !m_isVisible )
				return;

			if( m_referenceType == TexReferenceType.Instance )
			{
				GUI.Label( m_iconPos, string.Empty, UIUtils.GetCustomStyle( CustomStyle.SamplerTextureIcon ) );
			}

			if( ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD2 )
			{
				if( m_validReference )
				{
					bool cacheState = GUI.enabled;
					GUI.enabled = false;
					if( m_functionSwitchReference.m_toggleMode )
					{
						GUI.Label( m_varRect, GUIContent.none, UIUtils.GraphButton );
						if( m_functionSwitchReference.GetCurrentSelectedInput() == 1 )
						{
							GUI.Label( m_varRect, m_checkContent, UIUtils.GraphButtonIcon );
						}
					}
					else
					{
						GUI.Label( m_varRect, m_functionSwitchReference.AvailableInputsLabels[ m_currentSelectedInput ], UIUtils.GraphDropDown );
					}
					GUI.enabled = cacheState;
				}
				else
				{
					if( !m_editing )
					{
						if( m_toggleMode )
						{
							GUI.Label( m_varRect, GUIContent.none, UIUtils.GraphButton );

							if( m_currentSelectedInput == 1 )
							{
								GUI.Label( m_varRect, m_checkContent, UIUtils.GraphButtonIcon );
							}
						}
						else
						{
							GUI.Label( m_varRect, AvailableInputsLabels[ m_currentSelectedInput ], UIUtils.GraphDropDown );
							GUI.Label( m_imgRect, m_popContent, UIUtils.GraphButtonIcon );
						}
					}
				}
			}
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			// @diogo: only records the visit for a struct upstream; the selected input is walked on every visit, as before
			dataCollector.Touch( nodeData.Category, this, nodeData.Fields );
			if( m_inputPorts[ m_currentSelectedInput ].IsConnected )
			{
				nodeData.OutputPortId = m_inputPorts[ m_currentSelectedInput ].GetConnection( 0 ).PortId;
				m_inputPorts[ m_currentSelectedInput ].GetOutputNode().PropagateNodeData( nodeData, ref dataCollector );
			}
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			return received;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			return m_inputPorts[ m_currentSelectedInput ].GeneratePortInstructions( ref dataCollector );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_optionLabel = GetCurrentParam( ref nodeParams );
			m_toggleMode = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			m_currentSelectedInput = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_previousSelectedInput = m_currentSelectedInput;
			m_maxAmountInputs = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_orderIndex = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );

			for( int i = 0; i < m_maxAmountInputs; i++ )
			{
				m_optionNames[ i ] = GetCurrentParam( ref nodeParams );
			}

			UpdateLabels();
			m_sizeIsDirty = true;

			UIUtils.UpdateFunctionSwitchData( UniqueId, m_optionLabel );
			UIUtils.UpdateFunctionSwitchCopyData( UniqueId, m_optionLabel );
			if( UIUtils.CurrentShaderVersion() > 14205 )
			{
				m_referenceType = (TexReferenceType)Enum.Parse( typeof( TexReferenceType ), GetCurrentParam( ref nodeParams ) );
				m_referenceUniqueId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );

				if( m_referenceType == TexReferenceType.Object )
				{
					if( ContainerGraph.ParentWindow.CustomGraph == null )
						UIUtils.UnregisterFunctionSwitchCopyNode( this );
					UIUtils.RegisterFunctionSwitchNode( this );
					ResetToSelf();
				}
				else
				{
					if( ContainerGraph.ParentWindow.CustomGraph == null )
						UIUtils.UnregisterFunctionSwitchNode( this );
					UIUtils.RegisterFunctionSwitchCopyNode( this );
					m_headerColorModifier = ReferenceHeaderColor;
				}
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_optionLabel );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_toggleMode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_currentSelectedInput );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_maxAmountInputs );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_orderIndex );

			for( int i = 0; i < m_maxAmountInputs; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_optionNames[ i ] );
			}

			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceType );
			IOUtils.AddFieldValueToString( ref nodeInfo, ( m_functionSwitchReference != null ? m_functionSwitchReference.UniqueId : -1 ) );
		}

		public int OrderIndex
		{
			get { return m_orderIndex; }
			set { m_orderIndex = value; }
		}

		public string OptionLabel
		{
			get { return m_optionLabel; }
			set { m_optionLabel = value; }
		}

		public bool AsDrawn { get { return m_asDrawn; } set { m_asDrawn = value; } }

		public override string DataToArray { get { return m_optionLabel; } }
		public int MaxAmountInputs
		{
			get { return m_validReference ? m_functionSwitchReference.MaxAmountInputs : ( m_toggleMode ? 2 : m_maxAmountInputs ); }
		}
		public bool ToggleMode
		{
			get { return m_validReference ? m_functionSwitchReference.ToggleMode : m_toggleMode; }
		}
		public string[] OptionNames
		{
			get { return m_validReference ? m_functionSwitchReference.OptionNames : m_optionNames; }
		}
		public bool DirtySettings { get { return m_dirtySettings; } }
		public override int ReferencedNodeId { get { return ( m_referenceType == TexReferenceType.Instance && m_referenceUniqueId > -1 ) ? m_referenceUniqueId : base.ReferencedNodeId; } }
		public override bool HasMissingReference { get { return m_referenceType == TexReferenceType.Instance && m_functionSwitchReference == null; } }
	}
}

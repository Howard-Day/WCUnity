// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Get Local Var", "Miscellaneous", "Use a registered local variable", null, KeyCode.G )]
	public class GetLocalVarNode : ParentNode
	{
		[SerializeField]
		private int m_referenceId = -1;

		[SerializeField]
		private float m_referenceWidth = -1;

		[SerializeField]
		private int m_nodeId = -1;

		[SerializeField]
		private RegisterLocalVarNode m_currentSelected = null;

		[SerializeField]
		private string m_registerLocalVarName = string.Empty;

		private int m_cachedPropertyId = -1;

		private string m_previousLabel = string.Empty;

		private int m_prevReferenceId = -1;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.OBJECT, Constants.EmptyPortValue );

			// This is needed for infinite loop detection
			AddInputPort( WirePortDataType.OBJECT, false, Constants.EmptyPortValue );
			m_inputPorts[ 0 ].Visible = false;

			m_outputPorts[ 0 ].Locked = true;
			m_textLabelWidth = 80;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			m_previewShaderGUID = "f21a6e44c7d7b8543afacd19751d24c6";
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();

			if ( UniqueId > -1 )
			{
				m_containerGraph.LocalVarNodes.OnReorderEventComplete -= OnReorderEventComplete;
				m_containerGraph.LocalVarNodes.OnReorderEventComplete += OnReorderEventComplete;
			}
		}

		private void OnReorderEventComplete()
		{
			if( m_currentSelected != null )
			{
				m_referenceId = m_containerGraph.LocalVarNodes.GetNodeRegisterIdx( m_currentSelected.UniqueId );
			}
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if( m_currentSelected != null )
			{
				if( m_drawPreviewAsSphere != m_currentSelected.SpherePreview )
				{
					m_drawPreviewAsSphere = m_currentSelected.SpherePreview;
					OnNodeChange( new NodeUpdateCache() );
				}
				//CheckSpherePreview();

				if( m_cachedPropertyId == -1 )
					m_cachedPropertyId = Shader.PropertyToID( "_A" );

				// @diogo: registry read bypasses InputPorts; the register may be pooled and RT-less, so pull
				// it and, when we are live, mark it so it settles as a persistent frontier RT (QA #5c).
				m_currentSelected.EnsurePreviewRendered();
				if( PreviewLive )
				{
					m_currentSelected.MarkFeedsLiveConsumer();
				}
				m_currentSelected.OutputPorts[ 0 ].BindPreview( PreviewMaterial, m_cachedPropertyId, ASEPreviewSlices.ReferenceScratchIndex, this );
			}
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUILayout.BeginHorizontal();
			SearchableDropdown.Draw( Constants.AvailableReferenceStr, m_referenceId, UIUtils.LocalVarNodeArr(), ( idx ) =>
			{
				if( idx != m_referenceId )
				{
					UndoRecordObject( "Selected local var reference" );
					m_referenceId = idx;
					UpdateFromSelected();
					SetDirtyFromAsyncEdit();
				}
			} );

			UIUtils.DrawReferenceFocusButton( m_currentSelected );
			EditorGUILayout.EndHorizontal();
			//EditorGUILayout.LabelField( ConnStatus.ToString() + " " + m_activeConnections );
		}

		public override void Destroy()
		{
			base.Destroy();
			CurrentSelected = null;
			if( UniqueId > -1 )
				m_containerGraph.LocalVarNodes.OnReorderEventComplete -= OnReorderEventComplete;
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			if( m_dropdownEditing )
			{
				DropdownEditing = false;
				// @diogo: consume the chevron click so the canvas doesn't also select/drag under the popup
				if ( drawInfo.CurrentEventType == EventType.MouseDown )
				{
					ContainerGraph.ParentWindow.MouseInteracted = true;
				}
				SearchableDropdown.Open( m_dropdownRect, UIUtils.LocalVarNodeArr(), ( idx ) =>
				{
					if( idx != m_referenceId )
					{
						UndoRecordObject( "Selected local var reference" );
						m_referenceId = idx;
						UpdateFromSelected();
						SetDirtyFromAsyncEdit();
					}
				} );
			}
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			UpdateLocalVar();
		}

		void CheckForLoops()
		{
			if( CurrentSelected != null && UIUtils.DetectNodeLoopsFrom( CurrentSelected, new Dictionary<int, int>() ) )
			{
				// @diogo: mid-load the walk sees every settled getter dummy edge at once and false-positives on
				// evaluation-dead routes; report only - the destructive disable is interactive-edit protection
				if ( m_containerGraph.IsLoading )
				{
					UIUtils.ShowMessage( UniqueId, "Possible loop detected while loading, selection kept", MessageSeverity.Warning );
					return;
				}

				CurrentSelected = UIUtils.GetLocalVarNode( m_prevReferenceId );
				if( CurrentSelected == null || UIUtils.DetectNodeLoopsFrom( CurrentSelected, new Dictionary<int, int>() ) )
				{
					m_referenceId = -1;
					m_prevReferenceId = -1;
					CurrentSelected = null;
					m_outputPorts[ 0 ].Locked = true;
					SetAdditonalTitleText( "" );
					UIUtils.ShowMessage( "Infinite Loop detected, disabled selection" );
				}
				else
				{
					m_referenceId = m_prevReferenceId;
					UIUtils.ShowMessage( "Infinite Loop detected, reverted to previous selection" );
				}
			}
		}

		void UpdateFromSelected()
		{
			CurrentSelected = UIUtils.GetLocalVarNode( m_referenceId );
			CheckForLoops();

			if( m_currentSelected != null )
			{
				m_nodeId = m_currentSelected.UniqueId;
				m_outputPorts[ 0 ].Locked = false;
				m_outputPorts[ 0 ].ChangeType( m_currentSelected.OutputPorts[ 0 ].DataType, m_currentSelected.OutputPorts[ 0 ].StructName, false );
				m_drawPreviewAsSphere = m_currentSelected.SpherePreview;
				CheckSpherePreview();

				m_previousLabel = m_currentSelected.DataToArray;
				SetAdditonalTitleText( string.Format( Constants.SubTitleVarNameFormatStr, m_currentSelected.DataToArray ) );
				m_referenceWidth = m_currentSelected.Position.width;
			}

			m_sizeIsDirty = true;
			m_isDirty = true;
			m_prevReferenceId = m_referenceId;
		}

		public void UpdateLocalVar()
		{
			if( m_referenceId > -1 )
			{
				ParentNode newNode = UIUtils.GetLocalVarNode( m_referenceId );
				if( newNode != null )
				{
					if( newNode.UniqueId != m_nodeId )
					{
						CurrentSelected = null;
						int count = UIUtils.LocalVarNodeAmount();
						for( int i = 0; i < count; i++ )
						{
							ParentNode node = UIUtils.GetLocalVarNode( i );
							if( node.UniqueId == m_nodeId )
							{
								CurrentSelected = node as RegisterLocalVarNode;
								m_referenceId = i;
								break;
							}
						}
					}
				}

				if( m_currentSelected != null )
				{
					OutputPort sourcePort = m_currentSelected.OutputPorts[ 0 ];

					// @diogo: a struct can change name without changing type, which this guard would swallow
					if( sourcePort.DataType != m_outputPorts[ 0 ].DataType || sourcePort.StructName != m_outputPorts[ 0 ].StructName )
					{
						m_outputPorts[ 0 ].Locked = false;
						m_outputPorts[ 0 ].ChangeType( sourcePort.DataType, sourcePort.StructName, false );
					}

					if( !m_previousLabel.Equals( m_currentSelected.DataToArray ) )
					{
						m_previousLabel = m_currentSelected.DataToArray;
						SetAdditonalTitleText( string.Format( Constants.SubTitleVarNameFormatStr, m_currentSelected.DataToArray ) );
					}

					if( m_referenceWidth != m_currentSelected.Position.width )
					{
						m_referenceWidth = m_currentSelected.Position.width;
						m_sizeIsDirty = true;
					}
				}
				else
				{
					ResetReference();
				}
			}
		}

		public void ResetReference()
		{
			m_outputPorts[ 0 ].Locked = true;
			m_currentSelected = null;
			m_inputPorts[ 0 ].DummyClear();
			m_nodeId = -1;
			m_referenceId = -1;
			m_referenceWidth = -1;
			SetAdditonalTitleText( string.Empty );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_currentSelected != null )
			{
				return m_currentSelected.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			}
			else
			{
				UIUtils.ShowMessage( UniqueId, "Get Local Var node without reference. Attempting to access inexistant local variable.", MessageSeverity.Error );

				return m_outputPorts[ 0 ].ErrorValue;
			}
		}


		// @diogo: the hidden loop-detection reference on the input port counts as a connection, so the base walk already
		// crosses into the Register; this only forwards the demand that reached the Get instead of asking for everything
		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			return received;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 15 )
			{
				m_nodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				m_outputPorts[ 0 ].Locked = ( m_nodeId < 0 );
				if( UIUtils.CurrentShaderVersion() > 15500 )
				{
					m_registerLocalVarName = GetCurrentParam( ref nodeParams );
				}
			}
			else
			{
				m_referenceId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				m_outputPorts[ 0 ].Locked = ( m_referenceId < 0 );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			if( m_currentSelected != null )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_currentSelected.UniqueId );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_currentSelected.DataToArray );
			}
			else
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, -1 );
				IOUtils.AddFieldValueToString( ref nodeInfo, string.Empty );
			}

		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			if( UIUtils.CurrentShaderVersion() > 15 )
			{
				CurrentSelected = UIUtils.GetNode( m_nodeId ) as RegisterLocalVarNode;
				m_referenceId = UIUtils.GetLocalVarNodeRegisterId( m_nodeId );
				if( CurrentSelected == null && UIUtils.CurrentShaderVersion() > 15500 && !string.IsNullOrEmpty( m_registerLocalVarName ) )
				{
					CurrentSelected = m_containerGraph.LocalVarNodes.GetNodeByDataToArray( m_registerLocalVarName );
					if( CurrentSelected != null )
					{
						m_nodeId = CurrentSelected.UniqueId;
						m_referenceId = UIUtils.GetLocalVarNodeRegisterId( m_nodeId );
					}
				}
			}
			else
			{
				CurrentSelected = UIUtils.GetLocalVarNode( m_referenceId );
				if( m_currentSelected != null )
				{
					m_nodeId = m_currentSelected.UniqueId;
				}
			}

			CheckForLoops();

			if( m_currentSelected != null )
			{
				m_outputPorts[ 0 ].Locked = false;
				m_outputPorts[ 0 ].ChangeType( m_currentSelected.OutputPorts[ 0 ].DataType, m_currentSelected.OutputPorts[ 0 ].StructName, false );
			}
			else
			{
				m_outputPorts[ 0 ].Locked = true;
			}
		}

		public override void ActivateNode( int signalGenNodeId, int signalGenPortId, System.Type signalGenNodeType )
		{
			// @diogo: destroyed shells must ignore signals; destroy cascades and undo patching reach them via live neighbors
			if ( WasDestroyed )
			{
				return;
			}

			base.ActivateNode( signalGenNodeId, signalGenPortId, signalGenNodeType );
			if( m_activeConnections == 1 )
			{
				if( m_currentSelected != null )
				{
					m_currentSelected.ActivateNode( signalGenNodeId, signalGenPortId, signalGenNodeType );
				}
			}
		}

		public override void DeactivateNode( int deactivatedPort, bool forceComplete )
		{
			// @diogo: destroyed shells must ignore signals; destroy cascades and undo patching reach them via live neighbors
			if ( WasDestroyed )
			{
				return;
			}

			forceComplete = forceComplete || ( m_activeConnections == 1 );
			base.DeactivateNode( deactivatedPort, forceComplete );
			if( forceComplete && m_currentSelected != null )
			{
				m_currentSelected.DeactivateNode( deactivatedPort, false );
			}
		}

		public override int ReferencedNodeId { get { return ( m_currentSelected != null ) ? m_currentSelected.UniqueId : base.ReferencedNodeId; } }
		public override bool HasMissingReference { get { return m_currentSelected == null; } }

		public RegisterLocalVarNode CurrentSelected
		{
			get { return m_currentSelected; }
			set
			{
				// This is needed for infinite loop detection
				if( m_inputPorts != null )
					m_inputPorts[ 0 ].DummyClear();

				if( m_currentSelected != null )
				{
					m_currentSelected.UnregisterGetLocalVar( this );

					//if( m_currentSelected != value )
					m_currentSelected.DeactivateNode( 0, false );
				}

				if( value != null )
				{
					value.RegisterGetLocalVar( this );
					if( IsConnected && value != m_currentSelected )
						value.ActivateNode( UniqueId, 0, m_activeType );

					// This is needed for infinite loop detection
					m_inputPorts[ 0 ].DummyAdd( value.UniqueId, 0 ); ;
				}

				m_currentSelected = value;

			}
		}
	}
}

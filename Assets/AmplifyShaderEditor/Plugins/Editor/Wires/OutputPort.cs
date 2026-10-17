// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[System.Serializable]
	public sealed class OutputPort : WirePort
	{
		public delegate void OnNewPreviewRTCreated();
		public OnNewPreviewRTCreated OnNewPreviewRTCreatedEvent;

		[SerializeField]
		private bool m_connectedToMasterNode;

		[SerializeField]
		private bool[] m_isLocalValue = { false, false};

		[SerializeField]
		private string[] m_localOutputValue = { string.Empty,string.Empty};

		// @diogo: branch scope each cached value was generated in, 0 meaning root; serialized to stay in lockstep with m_isLocalValue (#365).
		[SerializeField]
		private int[] m_localValueScope = { 0, 0 };

		//[SerializeField]
		//private int m_isLocalWithPortType = 0;

		// @diogo: a slice of a shared preview array ( see ASEPreviewSlices ), not a RenderTexture of its own.
		private ASEPreviewSlice m_outputPreview;
		// @diogo: true when m_outputPreview is a borrowed pool slice (pooled SF previews, QA #5); the pool owns
		// it, so this port must never free it - only detach and hand it back.
		private bool m_pooledPreview = false;
		// @diogo: true when m_outputPreview aliases another port's slice ( FunctionNode ports mirror their
		// FunctionOutput ); the owner frees it, this port only forgets it.
		private bool m_aliasedPreview = false;
		// @diogo: owning node, used only to size the preview RT ( ConstantPreview -> 1x1 ). Runtime-only,
		// re-set every time the port is reconstructed through this ctor (QA #5).
		private ParentNode m_owner = null;
		private Material m_outputMaskMaterial = null;

		private int m_indexPreviewOffset = 0;

		public OutputPort( ParentNode owner, int nodeId, int portId, WirePortDataType dataType, string name ) : base( nodeId, portId, dataType, name )
		{
			LabelSize = Vector2.zero;
			m_owner = owner;
			OnNewPreviewRTCreatedEvent += owner.SetPreviewDirtyFromOutputs;
		}

		public string ErrorValue
		{
			get
			{
				string value = string.Empty;
				switch( m_dataType )
				{
					default:
					case WirePortDataType.OBJECT:
					case WirePortDataType.INT:
					case WirePortDataType.FLOAT: value = "(0)"; break;
					case WirePortDataType.FLOAT2: value = "half2(0,0)"; break;
					case WirePortDataType.FLOAT3: value = "half3(0,0,0)"; break;
					case WirePortDataType.COLOR:
					case WirePortDataType.FLOAT4: value = "half4(0,0,0,0)"; break;
					case WirePortDataType.FLOAT2x2: value = "half2x2(0,0,0,0)"; break;
					case WirePortDataType.FLOAT3x3: value = "half3x3(0,0,0,0,0,0,0,0,0)"; break;
					case WirePortDataType.FLOAT4x4: value = "half4x4(0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0)"; break;
				}
				return value;
			}
		}

		public bool ConnectedToMasterNode
		{
			get { return m_connectedToMasterNode; }
			set { m_connectedToMasterNode = value; }
		}

		public override void FullDeleteConnections()
		{
			UIUtils.DeleteConnection( false, m_nodeId, m_portId, true, true );
		}

		public bool HasConnectedNode
		{
			get
			{
				int count = m_externalReferences.Count;
				for( int i = 0; i < count; i++ )
				{
					if( UIUtils.GetNode( m_externalReferences[ i ].NodeId ).IsConnected )
						return true;
				}
				return false;
			}
		}
		public InputPort GetInputConnection( int connID = 0 )
		{
			if( connID < m_externalReferences.Count )
			{
				ParentNode node = UIUtils.GetNode( m_externalReferences[ connID ].NodeId );
				return ( node != null ) ? node.GetInputPortByUniqueId( m_externalReferences[ connID ].PortId ) : null;
			}
			return null;
		}

		public ParentNode GetInputNode( int connID = 0 )
		{
			if( connID < m_externalReferences.Count )
			{
				return UIUtils.GetNode( m_externalReferences[ connID ].NodeId );
			}
			return null;
		}

		public override void NotifyExternalRefencesOnChange()
		{
			for( int i = 0; i < m_externalReferences.Count; i++ )
			{
				ParentNode node = UIUtils.GetNode( m_externalReferences[ i ].NodeId );
				if( node )
				{
					node.CheckSpherePreview();
					// @diogo: the reference goes stale when the target port is trimmed off an extensible node
					InputPort port = node.GetInputPortByUniqueId( m_externalReferences[ i ].PortId );
					if ( port != null )
					{
						port.UpdateInfoOnExternalConn( m_nodeId, m_portId, m_dataType );
						node.OnConnectedOutputNodeChanges( m_externalReferences[ i ].PortId, m_nodeId, m_portId, m_name, m_dataType );
					}
				}
			}
		}

		public void ChangeTypeWithRestrictions( WirePortDataType newType, int restrictions )
		{
			if( m_dataType != newType )
			{
				DataType = newType;
				for( int i = 0; i < m_externalReferences.Count; i++ )
				{
					ParentNode inNode = UIUtils.GetNode( m_externalReferences[ i ].NodeId );
					InputPort inputPort = inNode.GetInputPortByUniqueId( m_externalReferences[ i ].PortId );

					bool valid = false;
					// @diogo: a free-for-all port retypes itself to what it is given, so its current type is no contract
					if ( restrictions == 0 || !inputPort.NotFreeForAllTypes )
					{
						valid = true;
					}
					else
					{
						valid = ( restrictions & (int)inputPort.DataType ) != 0;
					}

					if( valid )
					{
						inNode.CheckSpherePreview();
						inputPort.UpdateInfoOnExternalConn( m_nodeId, m_portId, m_dataType );
						inNode.OnConnectedOutputNodeChanges( m_externalReferences[ i ].PortId, m_nodeId, m_portId, m_name, m_dataType );
					}
					else
					{
						InvalidateConnection( m_externalReferences[ i ].NodeId, m_externalReferences[ i ].PortId );
						inputPort.InvalidateConnection( NodeId, PortId );
						i--;
					}
				}
			}
		}

		// @diogo: mirrors both halves of a struct type at once; the name is dropped by the plain
		// ChangeTypeWithRestrictions, which only knows about the wire type
		public void ChangeTypeWithRestrictions( WirePortDataType newType, string structName, int restrictions )
		{
			string newStructName = ( newType == WirePortDataType.STRUCT ) ? structName : string.Empty;
			bool nameChanged = m_structName != newStructName;
			bool typeChanged = m_dataType != newType;

			m_structName = newStructName;
			ChangeTypeWithRestrictions( newType, restrictions );

			// @diogo: the plain form returns early when the wire type is unchanged, and one struct
			// becoming another keeps the same type, so nothing downstream would ever hear the name move
			if ( nameChanged && !typeChanged )
			{
				NotifyExternalRefencesOnChange();
			}
		}

        public override void ChangePortId( int newPortId )
        {
            if( IsConnected )
            {
                int count = ExternalReferences.Count;
                for( int connIdx = 0; connIdx < count; connIdx++ )
                {
                    int nodeId = ExternalReferences[ connIdx ].NodeId;
                    int portId = ExternalReferences[ connIdx ].PortId;
                    ParentNode node = UIUtils.GetNode( nodeId );
                    if( node != null )
                    {
                        InputPort inputPort = node.GetInputPortByUniqueId( portId );
                        int inputCount = inputPort.ExternalReferences.Count;
                        for( int j = 0; j < inputCount; j++ )
                        {
                            if( inputPort.ExternalReferences[ j ].NodeId == NodeId &&
                                inputPort.ExternalReferences[ j ].PortId == PortId )
                            {
                                inputPort.ExternalReferences[ j ].PortId = newPortId;
                            }
                        }
                    }
                }
            }

            PortId = newPortId;
        }

        public string ConfigOutputLocalValue( PrecisionType precisionType, string value, string customName, MasterNodePortCategory category )
		{
			int idx = UIUtils.PortCategorytoAttayIdx( category );
			ParentGraph currentGraph = UIUtils.GetNode( NodeId ).ContainerGraph;
			string autoGraphId = currentGraph.GraphId > 0 ? "_g" + currentGraph.GraphId : string.Empty;
			m_localOutputValue[idx] = string.IsNullOrEmpty( customName ) ? ( "temp_output_" + m_nodeId + "_" + PortId + autoGraphId ) : customName;
			m_isLocalValue[idx] = true;
			m_localValueScope[ idx ] = CurrentBranchScope( category );
			//m_isLocalWithPortType |= (int)category;

			// @diogo: a struct is named by the port rather than by the type table, and takes no precision
			string typeName = ( DataType == WirePortDataType.STRUCT ) ? StructName : UIUtils.PrecisionWirePortToCgType( precisionType, DataType );

			return string.Format( Constants.LocalValueDecWithoutIdent, typeName, m_localOutputValue[idx], value );
		}

		// @diogo: scope the collector is emitting into right now, 0 whenever nothing is branching (#365).
		private static int CurrentBranchScope( MasterNodePortCategory category )
		{
			MasterNodeDataCollector collector = MasterNodeDataCollector.ActiveScopeCollector;
			return collector != null ? collector.CurrentBranchScopeOnCategory( category ) : 0;
		}

		public void SetLocalValue( string value, MasterNodePortCategory category )
		{
			int idx = UIUtils.PortCategorytoAttayIdx( category );
			m_isLocalValue[idx] = true;
			m_localOutputValue[ idx ] = value;
			m_localValueScope[ idx ] = CurrentBranchScope( category );
			//m_isLocalWithPortType |= (int)category;
		}

		public void ResetLocalValue()
		{
			for( int i = 0; i < m_localOutputValue.Length; i++ )
			{
				m_localOutputValue[ i ] = string.Empty;
				m_isLocalValue[i] = false;
				m_localValueScope[ i ] = 0;
			}
			//m_isLocalWithPortType = 0;
		}

		public void ResetLocalValueIfNot( MasterNodePortCategory category )
		{
			int idx = UIUtils.PortCategorytoAttayIdx( category );
			for( int i = 0; i < m_localOutputValue.Length; i++ )
			{
				if( i != idx )
				{
					m_localOutputValue[ i ] = string.Empty;
					m_isLocalValue[ i ] = false;
					m_localValueScope[ i ] = 0;
				}
			}
		}

		public void ResetLocalValueOnCategory( MasterNodePortCategory category )
		{
			int idx = UIUtils.PortCategorytoAttayIdx( category );
			m_localOutputValue[ idx ] = string.Empty;
			m_isLocalValue[ idx ] = false;
			m_localValueScope[ idx ] = 0;
		}

		public bool IsLocalOnCategory( MasterNodePortCategory category )
		{
			int idx = UIUtils.PortCategorytoAttayIdx( category );
			return m_isLocalValue[ idx ];
			//return ( m_isLocalWithPortType & (int)category ) != 0; ;
		}

		public override void ForceClearConnection()
		{
			UIUtils.DeleteConnection( false, m_nodeId, m_portId, false, true );
		}

		// @diogo: central guard for every cached output read; a value generated inside a branch arm is invisible from
		// outside it, so report the analysis miss and force a regeneration that re-stamps the slot (#365).
		public bool IsLocalValue( MasterNodePortCategory category )
		{
			int idx = UIUtils.PortCategorytoAttayIdx( category );
			bool isLocal = m_isLocalValue[ idx ];
			if ( isLocal && m_localValueScope[ idx ] != 0 )
			{
				MasterNodeDataCollector collector = MasterNodeDataCollector.ActiveScopeCollector;
				if ( collector != null && !collector.IsBranchScopeVisibleFrom( m_localValueScope[ idx ], collector.CurrentBranchScopeOnCategory( category ) ) )
				{
					collector.NotifyOutOfScopeLocalValue( m_nodeId );
					isLocal = false;
				}
			}
			return isLocal;
		}

		public string LocalValue(MasterNodePortCategory category)
		{
			int idx = UIUtils.PortCategorytoAttayIdx( category );
			return m_localOutputValue[idx];
		}

		// The slice holding this port's preview, allocated on first use. Invalid while previews are disabled.
		public ASEPreviewSlice PreviewSlice
		{
			get
			{
				if( Preferences.User.DisablePreviews )
				{
					return default( ASEPreviewSlice );
				}

				if( !m_outputPreview.IsValid )
				{
					// @diogo: 1x1 for spatially-uniform previews, current ( zoom-scaled ) size otherwise (QA #5).
					int size = ( m_owner != null && m_owner.ConstantPreview ) ? 1 : UIUtils.CurrentPreviewSize;
					m_outputPreview = ASEPreviewSlices.Allocate( size, Preferences.User.PreviewFormat, m_owner );
					m_pooledPreview = false;
					m_aliasedPreview = false;
					if( OnNewPreviewRTCreatedEvent != null )
						OnNewPreviewRTCreatedEvent();
				}

				return m_outputPreview;
			}
		}

		// Renders a material pass into this port's slice.
		public void BlitPreview( Material material, int pass )
		{
			PreviewSlice.Blit( material, pass );
		}

		// Binds this port's preview to a port property of the consumer's material ( see ASEPreviewSlice.Bind ).
		public void BindPreview( Material material, int propertyId, int scratchIndex, ParentNode consumer )
		{
			PreviewSlice.Bind( material, propertyId, scratchIndex, consumer );
		}

		// A 2D copy of this port's preview, for the GUI and readbacks that need a plain texture.
		public RenderTexture PreviewTexture2D( int scratchIndex )
		{
			if( Preferences.User.DisablePreviews )
			{
				return UIUtils.PreviewDisabledRT;
			}
			return PreviewSlice.To2D( scratchIndex );
		}

		// @diogo: FunctionNode ports mirror their FunctionOutput's slice; the alias is forgotten, never freed, here.
		public void AliasPreview( ASEPreviewSlice slice )
		{
			FreeOwnedPreview();
			m_outputPreview = slice;
			m_pooledPreview = false;
			m_aliasedPreview = true;
		}

		// @diogo: called right before a non-pooled render blits into this port (QA #5d). Drops an owned slice
		// whose size no longer matches the current ( zoom-scaled ) preview size, so the PreviewSlice getter
		// re-allocates it at the right size for this render. Safe ONLY on the render path - the slice's
		// content is rewritten immediately after - and never touches pooled or aliased slices ( callers must
		// not reach this through FunctionNode's aliasing ports ).
		public void EnsureOwnedPreviewSize()
		{
			if( HasOwnedPreviewTexture )
			{
				int size = ( m_owner != null && m_owner.ConstantPreview ) ? 1 : UIUtils.CurrentPreviewSize;
				if( m_outputPreview.Size != size )
				{
					FreeOwnedPreview();
				}
			}
		}

		private void FreeOwnedPreview()
		{
			if( HasOwnedPreviewTexture )
			{
				ASEPreviewSlices.Free( m_outputPreview );
			}
			m_outputPreview = default( ASEPreviewSlice );
			m_pooledPreview = false;
			m_aliasedPreview = false;
		}

		// @diogo: true when this port owns a persistent preview slice ( not borrowed or aliased ), for VRAM stats (QA #5).
		public bool HasOwnedPreviewTexture { get { return m_outputPreview.IsValid && !m_pooledPreview && !m_aliasedPreview; } }

		// @diogo: non-allocating peek - true when this port holds ANY preview slice ( owned, pooled or aliased ).
		// The PreviewSlice getter lazily allocates, so consumers that only need to know must use this (QA #5).
		public bool HasPreviewTexture { get { return m_outputPreview.IsValid; } }

		// @diogo: true when this port still holds a LIVE pool slice borrowed this pass; Unity-null aware so a
		// flushed array ( preview size/format change ) reads as not-held (QA #5).
		public bool HasPooledPreviewTexture { get { return m_pooledPreview && m_outputPreview.IsValid; } }

		// @diogo: the owned persistent preview slice ( invalid when none, borrowed or aliased ). Exposed so the
		// VRAM stats can read its ACTUAL dimensions - previews are not uniform ( 1x1 for constants ) (QA #5).
		public ASEPreviewSlice OwnedPreviewSlice { get { return HasOwnedPreviewTexture ? m_outputPreview : default( ASEPreviewSlice ); } }

		public int IndexPreviewOffset
		{
			get { return m_indexPreviewOffset; }
			set { m_indexPreviewOffset = value; }
		}

		public override void Destroy()
		{
			base.Destroy();
			// @diogo: never free a borrowed pool slice - the pool owns it (pooled SF previews, QA #5).
			FreeOwnedPreview();

			if( m_outputMaskMaterial != null )
				UnityEngine.ScriptableObject.DestroyImmediate( m_outputMaskMaterial );
			m_outputMaskMaterial = null;

			OnNewPreviewRTCreatedEvent = null;
		}

		// @diogo: borrow a pool slice as this port's preview target for the current pass (pooled SF previews,
		// QA #5). RenderNodePreview blits into it and consumers read it, exactly like an owned slice. Frees any
		// previously-owned persistent slice first, so switching a node to pooled ( or the first pooled render
		// after a load-time lazy allocation ) releases its old slice instead of orphaning it.
		public void AssignPooledPreview( ASEPreviewSlice slice )
		{
			FreeOwnedPreview();
			m_outputPreview = slice;
			m_pooledPreview = true;
		}

		// @diogo: hand the borrowed pool slice back (returns it so the caller can Return() it to the pool) and
		// clear this port so the next pass re-checks-out. No-op / returns an invalid slice for owned or empty ports.
		public ASEPreviewSlice DetachPooledPreview()
		{
			ASEPreviewSlice slice = m_pooledPreview ? m_outputPreview : default( ASEPreviewSlice );
			if( m_pooledPreview )
			{
				m_outputPreview = default( ASEPreviewSlice );
				m_pooledPreview = false;
			}
			return slice;
		}

		public Material MaskingMaterial
		{
			get
			{
				if( m_outputMaskMaterial == null )
				{
					//m_outputMaskMaterial = new Material( AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( "9c34f18ebe2be3e48b201b748c73dec0" ) ) );
					m_outputMaskMaterial = new Material( UIUtils.MaskingShader );
				}
				return m_outputMaskMaterial;
			}
			//set { m_outputMaskMaterial = value; }
		}
	}
}

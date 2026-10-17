// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Struct", "Struct Operators", "Build a struct, pass one through, or read its fields.", tags: "struct create split make break build set" )]
	public sealed class StructNode : ParentNode
	{
		private const string TypeNameStr = "Type";
		private const string DeclarationStr = "Declaration";
		private static readonly GUIContent TintContent = new GUIContent( "Tint", "What this struct is drawn with: the header of every Struct node carrying it, and every wire it travels on." );
		private const string FieldsStr = "Fields";
		private const string DefaultFieldName = "Field";
		private const string FieldIdsMarker = "FieldIds";
		private const string DefaultTypeName = "MyStruct";
		private const string LocalVarPrefix = "structNode";
		private const string DataStr = "Data";
		private const string NoTypeStr = "<None>";

		private const string OwnershipStr = "Definition";
		private const string ReferenceStr = "Reference";
		private const string PromoteStr = "Promote to Asset";
		private const string PromoteTitleStr = "Save Struct";
		private const string DemoteStr = "Copy to Local";
		private const string ExposureUndoStr = "Struct field exposure";
		private const string LocalSuffix = " (Local)";
		private const string AssetSuffix = " (Asset)";
		private const string NoFieldsStr = "No fields yet. Add one with the + button.";
		private const float FieldToggleWidth = 24;
		private const float FieldRowSpacing = 2;

		// @diogo: an empty label paints nothing but claims the rect for its tooltip, which is the only
		// way to put one on the Rect overloads below - none of them carry a GUIContent of their own
		private static readonly GUIContent InTooltip = new GUIContent( string.Empty, "Show an input port for this field. Wiring it sets the field, overriding whatever came in." );
		private static readonly GUIContent OutTooltip = new GUIContent( string.Empty, "Show an output port for this field, to read its value." );
		private static readonly GUIContent AllInputsTooltip = new GUIContent( string.Empty, "Show an input port on every field, or hide them all." );
		private static readonly GUIContent AllOutputsTooltip = new GUIContent( string.Empty, "Show an output port on every field, or hide them all." );
		private static readonly GUIContent DataPortsLabel = new GUIContent( "Data Ports",
			"The struct itself, in and out. The input takes one to pass through, which is also what tells " +
			"this node its type; the output carries the whole struct on to the next node." );
		private static readonly GUIContent DataInTooltip = new GUIContent( string.Empty, "Show the Data input, which takes a struct to pass through." );
		private static readonly GUIContent DataOutTooltip = new GUIContent( string.Empty, "Show the Data output, which carries the whole struct onward." );
		private static readonly GUIContent NameTooltip = new GUIContent( string.Empty, "Field name, exactly as it appears in the generated struct." );
		private static readonly GUIContent TypeTooltip = new GUIContent( string.Empty, "Field type." );
		private static readonly GUIContent AddTooltip = new GUIContent( string.Empty, "Duplicate this field below it." );
		private static readonly GUIContent RemoveTooltip = new GUIContent( string.Empty, "Remove this field." );
		private static readonly GUIContent AddFieldTooltip = new GUIContent( string.Empty, "Add a field at the end." );
		private static readonly GUIContent RemoveFieldTooltip = new GUIContent( string.Empty, "Remove the last field." );

		private const string NoTypeNameError = "Struct node needs a type name before it can build a value.";
		private const string NoFieldsError = "Struct node needs at least one field before it can build a value.";
		private const string TypeNameClashError = "Struct type {0} is already declared in this shader with a different field list.";
		private const string UnresolvedReferenceError = "Struct node references a definition that no longer exists.";
		private const string UnknownTypeError = "No definition for struct {0} in this graph. Add a Struct node carrying it.";
		private const string NoWireTypeError = "Nothing on Data names a struct type yet, so this node has no fields to offer.";
		private const string ClashWarning = "Another Struct node declares {0} with a different field list. Only one of them can be emitted.";
		private const string LateFieldWarning = "A struct field was read in a pass that did not ask for it during analysis and was generated late. The shader is still correct.";

		private const string NodeInfo =
			"Connect Data to pass a struct through: the type comes from the wire, and the struct is copied " +
			"whole first, so every field starts out as it arrived. Wired field inputs are then assigned " +
			"over that copy. An exposed but empty one changes nothing, and with none wired no copy is " +
			"emitted at all. Leave Data unconnected to build a struct instead, starting from zero.\n" +
			"\n" +
			"Definition says where the fields come from:\n" +
			"• Local: they belong to this node.\n" +
			"• Reference: borrowed from another Struct node or an Amplify Shader Struct asset, and edited there.\n" +
			"\n" +
			"Declaration says who owns the type:\n" +
			"• Create: ASE declares the struct in the generated shader.\n" +
			"• Fetch: the type comes from an include, so nothing is declared and the fields only drive the ports.";

		[SerializeField]
		private StructDefinitionData m_definition = new StructDefinitionData();

		[SerializeField]
		private StructOwnership m_ownership = StructOwnership.Local;

		// @diogo: the reference target is a type name when it is another node's Local definition, and a
		// GUID when it is an asset - the name is kept either way, since it is what recovers a broken GUID
		[SerializeField]
		private string m_referenceName = string.Empty;

		[SerializeField]
		private string m_referenceGUID = string.Empty;

		[SerializeField]
		private bool m_fieldsFoldout = true;

		// @diogo: which field ports are on show, one entry per field per direction. Ports are hidden
		// rather than removed so their ids - and the wires that reference them - survive the round trip
		[SerializeField]
		private List<bool> m_inputExposed = new List<bool>();

		[SerializeField]
		private List<bool> m_outputExposed = new List<bool>();

		// @diogo: the id of the field each port was last lined up with, one entry per field port, which is
		// what lets a port follow its field through a rename made while the graph was closed
		[SerializeField]
		private List<int> m_portFieldIds = new List<int>();

		// @diogo: the passthrough pair is on show independently of the fields. A new node offers only the
		// input, since a struct arriving is what tells an empty node what it is; a node built from a
		// dropped asset is the other way round ( ExposeAsBuilder )
		[SerializeField]
		private bool m_dataInputExposed = true;

		[SerializeField]
		private bool m_dataOutputExposed = false;

		// @diogo: the struct the field exposure was last set up for. A different type arriving is what
		// makes its fields come out on their own; the same one rewired leaves them as they were left
		[SerializeField]
		private string m_exposedForTypeName = string.Empty;

		private StructDefinitionData m_exposedForDefinition = null;

		private ReorderableList m_fieldsList = null;
		private int m_actionIndex = -1;
		private ReordableAction m_actionType = ReordableAction.None;

		private List<StructFieldData> m_fieldsListSource = null;
		private AmplifyShaderStruct m_referenceAsset = null;
		private bool m_referenceAssetResolved = false;
		private StructNode m_followedNode = null;
		private string[] m_referenceOptions = null;
		private List<string> m_referenceOptionNames = new List<string>();
		private List<string> m_referenceOptionDisplays = new List<string>();
		private List<string> m_referenceOptionGUIDs = new List<string>();

		// @diogo: what the ports were last built from, so a definition edited elsewhere is noticed by
		// comparison rather than by notification - it can change in an asset outside the graph entirely
		private StructDefinitionData m_portedDefinition = null;
		private string m_portedTypeName = string.Empty;

		// Set on a pasted node until it has settled whether the graph already holds its type name, which
		// it can only do once its wires are back.
		private bool m_pastedTypePending = false;

		private StructFieldMask[] m_demand = new StructFieldMask[ 2 ];
		private StructFieldMask[] m_emitted = new StructFieldMask[ 2 ];
		private MasterNodeDataCollector m_demandOwner;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			AddInputPort( WirePortDataType.STRUCT, false, Constants.EmptyPortValue );
			m_inputPorts[ 0 ].CreatePortRestrictions( WirePortDataType.STRUCT );
			AddOutputPort( WirePortDataType.STRUCT, Constants.EmptyPortValue );

			m_textLabelWidth = 90;
			m_customPrecision = true;

			// @diogo: gives every unconnected field an editable default, in the side panel and beside the
			// port. The defaults live on this node's ports rather than on the definition, so two nodes
			// building the same struct can start from different values
			m_useInternalPortData = true;

			UpdatePorts();
			UpdateSubtitle();
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();

			// @diogo: the container graph is not available in CommonInit, so a brand new node picks its
			// default name here; load and paste bring their own, and a reassigned id keeps the one it has
			if ( UniqueId > -1 && !m_containerGraph.IsLoading && !m_isNodeBeingCopied && string.IsNullOrEmpty( m_definition.TypeName ) )
			{
				m_definition.TypeName = FirstAvailableTypeName();
				UpdatePorts();
				UpdateSubtitle();
			}

			RefreshRegistration();
		}

		// The first default name no struct in this graph answers to yet; the first node takes the bare
		// name and the rest number up from 1.
		private string FirstAvailableTypeName()
		{
			string candidate = DefaultTypeName;
			int index = 0;
			while ( m_containerGraph.GetStructDefinition( candidate ) != null )
			{
				index++;
				candidate = DefaultTypeName + index;
			}
			return candidate;
		}

		// A type name no other Struct node's definition answers to: the name itself while it is free,
		// otherwise its trailing number counted up until one is.
		private string UniqueTypeName( string typeName )
		{
			string unique = typeName;
			if ( TypeNameHolder( unique ) != null )
			{
				string nameBase = StructDefinitionData.FieldNameBase( typeName );
				int index;
				int.TryParse( typeName.Substring( nameBase.Length ), out index );
				do
				{
					index++;
					unique = nameBase + index;
				} while ( TypeNameHolder( unique ) != null );
			}
			return unique;
		}

		// The other Struct node whose definition answers to a type name, or null while the name is free.
		private StructNode TypeNameHolder( string typeName )
		{
			// @diogo: only Struct nodes count, unlike for a default name, so a Local in Fetch mode can still
			// restate a type a function boundary carries; one borrowing this definition is no clash either
			List<ParentNode> nodes = m_containerGraph.AllNodes;
			for ( int i = 0; i < nodes.Count; i++ )
			{
				if ( nodes[ i ] is StructNode other && other != this && !other.IsPassthrough )
				{
					StructDefinitionData definition = other.ActiveDefinition;
					if ( definition != null && !ReferenceEquals( definition, m_definition ) && definition.CodeName.Equals( typeName ) )
					{
						return other;
					}
				}
			}
			return null;
		}

		// Names a definition this node has just come to own: afresh when it has no name, as a new node is
		// named, or numbered up when another Struct node already answers to the one it has - one pasted,
		// copied inline, or left dormant while the node borrowed or passed one through.
		private void ClaimUniqueTypeName()
		{
			// @diogo: load and undo put names back exactly as they were saved
			if ( OwnsDefinition && !m_containerGraph.IsLoading )
			{
				m_definition.TypeName = string.IsNullOrEmpty( m_definition.TypeName ) ? FirstAvailableTypeName() : UniqueTypeName( m_definition.CodeName );
			}
		}

		public override void AfterDuplication()
		{
			base.AfterDuplication();

			// @diogo: settled later, from ReconnectClipboardReferences or RefreshExternalReferences - a node
			// pasted with its Data wire is a passthrough, and only knows it once the wires are back
			m_pastedTypePending = true;

			// @diogo: a copy of a node that already stands for a struct is most likely there to take that
			// struct in, so the Data input a Reference hides on the way in comes back on the copy
			if ( m_ownership == StructOwnership.Reference || m_definition.Fields.Count > 0 )
			{
				m_dataInputExposed = true;
				m_inputPorts[ 0 ].Visible = true;
				m_sizeIsDirty = true;
			}
		}

		public override void ReconnectClipboardReferences( Clipboard clipboard )
		{
			base.ReconnectClipboardReferences( clipboard );
			ResolvePastedTypeName( clipboard );
		}

		// Settles a pasted definition that has no name, or one the graph already holds, by giving it a free
		// name. The clipboard is there when the paste carried wires, and lets the nodes pasted along follow
		// the rename.
		private void ResolvePastedTypeName( Clipboard clipboard )
		{
			if ( !m_pastedTypePending )
			{
				return;
			}
			m_pastedTypePending = false;

			if ( !OwnsDefinition )
			{
				return;
			}

			string pastedName = m_definition.CodeName;
			bool unnamed = string.IsNullOrEmpty( m_definition.TypeName );
			if ( !unnamed && TypeNameHolder( pastedName ) == null )
			{
				return;
			}

			// @diogo: even an exact copy of a type already here is numbered - a paste is asked for as a new
			// type to edit, and joining an existing one is what Reference is for
			ClaimUniqueTypeName();
			RefreshRegistration();

			if ( clipboard != null )
			{
				List<ClipboardData> pasted = clipboard.CurrentClipboardStrData;

				// @diogo: exposure first, since the references below notify what they feed - a passthrough
				// still set up for the old name would take the renamed copy for a new struct and reset its ports
				for ( int i = 0; i < pasted.Count; i++ )
				{
					if ( m_containerGraph.GetNode( pasted[ i ].NewNodeId ) is StructNode other && other.m_exposedForTypeName.Equals( pastedName ) )
					{
						other.m_exposedForTypeName = m_definition.CodeName;
					}
				}

				for ( int i = 0; i < pasted.Count; i++ )
				{
					if ( m_containerGraph.GetNode( pasted[ i ].NewNodeId ) is StructNode other && other.m_ownership == StructOwnership.Reference &&
						string.IsNullOrEmpty( other.m_referenceGUID ) && other.m_referenceName.Equals( pastedName ) )
					{
						other.FollowRename( m_definition.CodeName );
					}
				}
			}

			UpdatePorts();
			UpdateSubtitle();
		}

		public override void Destroy()
		{
			base.Destroy();
			m_containerGraph.StructNodes.RemoveNode( this );
			m_fieldsList = null;
			m_referenceAsset = null;
			m_followedNode = null;
			m_referenceOptions = null;
		}

		// A node whose Struct input is wired carries a value, not a definition - the type it passes
		// through is declared upstream, so it owns nothing and answers to the wire.
		// @diogo: the graph asks this of every struct node while resolving a type, and a destroyed node
		// stays in the node list with its ports already nulled, so the guard is not paranoia
		public bool IsPassthrough { get { return m_inputPorts != null && m_inputPorts.Count > 0 && m_inputPorts[ 0 ].IsConnected; } }

		public bool OwnsDefinition { get { return !IsPassthrough && m_ownership == StructOwnership.Local; } }

		// Only a node that owns its definition registers, so a reference can never resolve to another
		// reference and no cycle is representable.
		private void RefreshRegistration()
		{
			if ( OwnsDefinition )
			{
				m_containerGraph.StructNodes.AddNode( this );
				m_containerGraph.StructNodes.UpdateDataOnNode( UniqueId, m_definition.CodeName );
			}
			else
			{
				m_containerGraph.StructNodes.RemoveNode( this );
			}
		}

		// Makes this node borrow its definition rather than own it. The Data input goes out of sight on the
		// way in: a borrowing node is there to build that type, and a struct wired into Data would override it.
		private void EnterReferenceMode()
		{
			if ( m_ownership != StructOwnership.Reference )
			{
				m_ownership = StructOwnership.Reference;
				m_dataInputExposed = false;
			}
		}

		// Points this node at a definition it does not own. The GUID is empty for another node's Local
		// definition and set for an asset; the name resolves either one and recovers a GUID that broke.
		public void SetReferenceTo( string typeName, string guid )
		{
			EnterReferenceMode();
			m_referenceName = typeName;
			m_referenceGUID = guid;
			m_referenceAsset = null;
			m_referenceAssetResolved = false;
			m_followedNode = null;
			RefreshRegistration();
			UpdatePorts();
			UpdateSubtitle();
		}

		// Binds to an Amplify Shader Struct asset, as dropping one into the graph does.
		public void SetReferenceTo( AmplifyShaderStruct asset )
		{
			if ( asset == null )
			{
				return;
			}

			string guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( asset ) );
			SetReferenceTo( asset.Definition.CodeName, guid );
			m_referenceAsset = asset;
			m_referenceAssetResolved = true;
		}

		public StructDefinitionData LocalDefinition { get { return m_definition; } }

		// The registry is keyed by type name, which is what a reference stores and looks up.
		public override string DataToArray { get { return m_definition.CodeName; } }

		// The definition this node actually works from: the wire's when passing through, otherwise its
		// own or the one it borrows. Null when nothing resolves.
		public StructDefinitionData ActiveDefinition
		{
			get
			{
				if ( IsPassthrough )
				{
					// @diogo: asked of the graph, never of this node's own fields - a passthrough restates
					// nothing, so there is nothing here that could drift from the type on the wire
					StructNode followed = FollowedOwner( m_inputPorts[ 0 ].StructName );
					return ( followed != null ) ? followed.LocalDefinition : m_containerGraph.GetStructDefinition( m_inputPorts[ 0 ].StructName );
				}

				if ( m_ownership == StructOwnership.Local )
				{
					return m_definition;
				}

				if ( !string.IsNullOrEmpty( m_referenceGUID ) )
				{
					AmplifyShaderStruct asset = ReferenceAsset;
					return ( asset != null ) ? asset.Definition : null;
				}

				StructNode owner = ReferenceNode;
				return ( owner != null ) ? owner.LocalDefinition : null;
			}
		}

		// Why ActiveDefinition came back null, which is three separate things: a source that has yet to
		// name a type, a name this graph holds no definition for, or a reference whose target is gone.
		// @diogo: a struct wire is unnamed for as long as it is legitimately unresolved - a Function Input
		// with no asset picked declares one, and waits for the caller to say which - so the first of the
		// three is not a broken reference and must not be reported as one
		private string MissingDefinitionMessage()
		{
			string message = UnresolvedReferenceError;

			if ( IsPassthrough )
			{
				string typeName = m_inputPorts[ 0 ].StructName;
				message = string.IsNullOrEmpty( typeName ) ? NoWireTypeError : string.Format( UnknownTypeError, typeName );
			}

			return message;
		}

		private StructNode ReferenceNode
		{
			get
			{
				if ( m_containerGraph == null || string.IsNullOrEmpty( m_referenceName ) )
				{
					return null;
				}
				return FollowedOwner( m_referenceName );
			}
		}

		// The node whose Local definition this one borrows, which is what gives it the reference outline and
		// dashed wire. An asset has no node to point at, and a passthrough answers to its wire instead.
		// @diogo: the owner last resolved rather than a fresh lookup, since this is polled on every repaint;
		// ActiveDefinition resolves it again on each logic update
		public override int ReferencedNodeId
		{
			get
			{
				bool borrowsLocal = m_ownership == StructOwnership.Reference && string.IsNullOrEmpty( m_referenceGUID ) && !IsPassthrough;
				return ( borrowsLocal && m_followedNode != null ) ? m_followedNode.UniqueId : base.ReferencedNodeId;
			}
		}

		// Whether the definition this node borrows no longer resolves, which gives it the red outline other
		// reference nodes show when their source is gone.
		// @diogo: resolved state only, for the same reason; an asset counts as missing once its lookup has
		// failed, not before the first one has run
		public override bool HasMissingReference
		{
			get
			{
				bool missing = string.IsNullOrEmpty( m_referenceGUID ) ? m_followedNode == null : ( m_referenceAssetResolved && m_referenceAsset == null );
				return m_ownership == StructOwnership.Reference && !IsPassthrough && missing;
			}
		}

		// The Local owner answering to a type name. The one found last is kept while it still does, so an
		// owner renamed through a name another Local holds does not hand this node over to that one.
		private StructNode FollowedOwner( string typeName )
		{
			if ( m_followedNode == null || !m_containerGraph.StructNodes.NodesList.Contains( m_followedNode ) ||
				!m_followedNode.DataToArray.Equals( typeName ) )
			{
				m_followedNode = m_containerGraph.StructNodes.GetNodeByDataToArray( typeName );
			}
			return m_followedNode;
		}

		// @diogo: GUID resolves the asset and the name recovers it when that GUID no longer does, the same
		// two-step Function Node uses. The resolved flag stops a failed lookup from searching every frame
		private AmplifyShaderStruct ReferenceAsset
		{
			get
			{
				if ( !m_referenceAssetResolved )
				{
					m_referenceAssetResolved = true;
					m_referenceAsset = AssetDatabase.LoadAssetAtPath<AmplifyShaderStruct>( AssetDatabase.GUIDToAssetPath( m_referenceGUID ) );
					if ( m_referenceAsset == null && !string.IsNullOrEmpty( m_referenceName ) )
					{
						string[] guids = AssetDatabase.FindAssets( "t:AmplifyShaderStruct" );
						for ( int i = 0; i < guids.Length; i++ )
						{
							AmplifyShaderStruct candidate = AssetDatabase.LoadAssetAtPath<AmplifyShaderStruct>( AssetDatabase.GUIDToAssetPath( guids[ i ] ) );
							if ( candidate != null && candidate.Definition.CodeName.Equals( m_referenceName ) )
							{
								m_referenceAsset = candidate;
								m_referenceGUID = guids[ i ];
								break;
							}
						}
					}
				}

				return m_referenceAsset;
			}
		}

		private string ActiveTypeName
		{
			get
			{
				// @diogo: the subtitle names the type the shader will contain, which is the only name a
				// wire or a lookup ever uses - an asset's free-form name would not match either
				StructDefinitionData definition = ActiveDefinition;
				return ( definition != null ) ? definition.CodeName : string.Empty;
			}
		}

		// A definition can be edited from somewhere else entirely, including an asset changed outside the
		// graph, so the ports are compared against it rather than told when to change.
		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );

			StructDefinitionData definition = ActiveDefinition;
			m_headerColor = ( definition != null ) ? definition.Tint : StructDefinitionData.DefaultTint;
			if ( !ReferenceEquals( definition, m_portedDefinition ) || !PortsMatch( definition ) )
			{
				UpdatePorts();
				UpdateSubtitle();
			}
		}

		// @diogo: an unwired Data input names no struct so it can take any; it shows the one this node builds
		protected override string InputPortTintName( int index )
		{
			return ( index == 0 && !m_inputPorts[ 0 ].IsConnected ) ? m_outputPorts[ 0 ].StructName : base.InputPortTintName( index );
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );

			if ( portId == m_inputPorts[ 0 ].PortId )
			{
				m_inputPorts[ 0 ].MatchPortToConnection();
				OnPassthroughChanged();
			}
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );

			if ( m_inputPorts.Count > 0 && portId == m_inputPorts[ 0 ].PortId )
			{
				// @diogo: the port names the struct it was carrying, and it carries none now. Left set, it
				// would go on demanding that one and refuse every other struct offered to it
				m_inputPorts[ 0 ].ChangeType( WirePortDataType.STRUCT, string.Empty, false );
				OnPassthroughChanged();
			}
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );

			if ( inputPortId == m_inputPorts[ 0 ].PortId )
			{
				m_inputPorts[ 0 ].MatchPortToConnection();
				OnPassthroughChanged();
			}
		}

		// Wiring the Struct input is what turns this into a passthrough, so it changes what the node owns
		// as well as what it looks like.
		private void OnPassthroughChanged()
		{
			// @diogo: mid-load, the wires still to come address field ports by their saved ids, so re-syncing
			// to a since-reordered definition here would land them on the wrong fields; the first logic
			// update re-syncs by name once every wire is back
			if ( m_containerGraph.IsLoading )
			{
				RefreshRegistration();
				return;
			}

			// @diogo: reading a struct is the reason to pass one through, so the outputs come out on their
			// own; an override is always deliberate, so the inputs stay hidden until asked for
			if ( IsPassthrough )
			{
				StructDefinitionData definition = ActiveDefinition;
				string arrivingName = ( definition != null ) ? definition.CodeName : string.Empty;

				// @diogo: rewiring the same struct is not a new type, so it leaves the node as set up -
				// only a type this node was not already showing decides what comes out of it
				if ( !string.IsNullOrEmpty( arrivingName ) && arrivingName != m_exposedForTypeName )
				{
					// @diogo: nor is the same definition renamed upstream, which only the object can tell
					if ( !ReferenceEquals( definition, m_exposedForDefinition ) )
					{
						int fieldCount = ( definition != null ) ? definition.Fields.Count : 0;
						SetExposure( m_outputExposed, fieldCount, true );
						SetExposure( m_inputExposed, fieldCount, false );

						// @diogo: a wired override stays in sight; hidden, the hidden-input cleanup would drop
						// its wire. Flags still index the current ports, which the sync below carries them by
						for ( int k = 0; k + 1 < m_inputPorts.Count; k++ )
						{
							if ( FieldInput( k ).IsConnected )
							{
								Exposed( m_inputExposed, k, false );
								m_inputExposed[ k ] = true;
							}
						}
					}
					m_exposedForTypeName = arrivingName;
				}

				// @diogo: only replaced by a definition that resolved, so a moment with none does not forget it
				if ( definition != null )
				{
					m_exposedForDefinition = definition;
				}
			}

			ClaimUniqueTypeName();
			RefreshRegistration();
			UpdatePorts();
			UpdateSubtitle();
		}

		// The shape a node built from a dropped asset arrives in: every field takes an input, the struct
		// leaves through Data, and nothing is read back field by field. The reverse of an empty node,
		// which offers only the Data input, since a struct arriving is what tells it what it is.
		public void ExposeAsBuilder()
		{
			StructDefinitionData definition = ActiveDefinition;
			int fieldCount = ( definition != null ) ? definition.Fields.Count : 0;

			SetExposure( m_inputExposed, fieldCount, true );
			SetExposure( m_outputExposed, fieldCount, false );
			m_dataInputExposed = false;
			m_dataOutputExposed = true;

			UpdatePorts();
		}

		private static void SetExposure( List<bool> exposure, int fieldCount, bool exposed )
		{
			while ( exposure.Count < fieldCount )
			{
				exposure.Add( exposed );
			}

			// @diogo: every flag, not just one per field - the port sync hands them on by the port that held
			// them, so one past the field count can still land on a field
			for ( int i = 0; i < exposure.Count; i++ )
			{
				exposure[ i ] = exposed;
			}
		}

		private bool Exposed( List<bool> exposure, int index, bool fallback )
		{
			while ( exposure.Count <= index )
			{
				exposure.Add( fallback );
			}
			return exposure[ index ];
		}

		// A toggle by hand sets the exposure up for the type on the node, so that same type arriving over
		// Data afterwards leaves it as set - only a type never set up for comes out on its own.
		private void MarkExposureSetUp()
		{
			StructDefinitionData definition = ActiveDefinition;
			if ( definition != null && !string.IsNullOrEmpty( definition.TypeName ) )
			{
				m_exposedForTypeName = definition.CodeName;
				m_exposedForDefinition = definition;
			}
		}

		private void UpdateSubtitle()
		{
			// @diogo: the raw name decides whether there is one at all - CodeName answers "Struct" for an
			// empty one, which reads on the node as a type called Struct rather than as no type yet
			StructDefinitionData definition = ActiveDefinition;
			bool named = ( definition != null ) && !string.IsNullOrEmpty( definition.TypeName );
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, named ? definition.CodeName : NoTypeStr ) );
		}

		private InputPort FieldInput( int index ) { return ( index + 1 < m_inputPorts.Count ) ? m_inputPorts[ index + 1 ] : null; }

		private OutputPort FieldOutput( int index ) { return ( index + 1 < m_outputPorts.Count ) ? m_outputPorts[ index + 1 ] : null; }

		private int PortFieldId( int index ) { return ( index < m_portFieldIds.Count ) ? m_portFieldIds[ index ] : 0; }

		// One field port per direction per field, plus the passthrough pair that always leads.
		private void UpdatePorts()
		{
			StructDefinitionData definition = ActiveDefinition;

			// @diogo: a passthrough whose wire names no struct the graph knows has nothing to rebuild from,
			// and collapsing would drop the wires on its ports. The name is usually only in transit - an
			// upstream node still loading, or one that passes a rename on a frame late - and generation
			// already reports it when it is not
			if ( IsPassthrough && definition == null )
			{
				return;
			}

			int fieldCount = ( definition != null ) ? definition.Fields.Count : 0;
			// @diogo: CodeName answers "Struct" for a definition with no name, which is the right fallback
			// for generating legal HLSL and the wrong thing for identity - every unnamed node would claim
			// that same type, and one still being loaded would out-argue the real struct on the wire
			string codeName = ( definition != null && !string.IsNullOrEmpty( definition.TypeName ) ) ? definition.CodeName : string.Empty;

			// @diogo: a node just given fields of its own, with nothing wired in, exists to build one - so
			// the port carrying it out shows itself the first time that happens, and is the user's after
			if ( !IsPassthrough && fieldCount > 0 && m_inputPorts.Count == 1 )
			{
				m_dataOutputExposed = true;
			}

			SyncFieldPorts( definition, fieldCount );

			// @diogo: a fixed label - the type is already on the node subtitle, and repeating it on the
			// port only made the node wider as the name grew
			m_inputPorts[ 0 ].Name = DataStr;
			m_inputPorts[ 0 ].Visible = m_dataInputExposed;

			// @diogo: the name-carrying overload, not a bare assignment - one struct becoming another
			// keeps the wire type, so this is the only thing that tells anything downstream it moved
			m_outputPorts[ 0 ].ChangeProperties( DataStr, WirePortDataType.STRUCT, codeName, false );
			m_outputPorts[ 0 ].Visible = m_dataOutputExposed;

			for ( int i = 0; i < fieldCount; i++ )
			{
				StructFieldData field = definition.Fields[ i ];

				InputPort input = FieldInput( i );
				input.ChangeProperties( field.Name, field.DataType, false );
				input.Visible = Exposed( m_inputExposed, i, false );

				OutputPort output = FieldOutput( i );
				output.ChangeProperties( field.Name, field.DataType, false );
				output.Visible = Exposed( m_outputExposed, i, false );
			}

			m_portedDefinition = definition;
			m_portedTypeName = codeName;
			m_sizeIsDirty = true;
		}

		// Lines the field ports up with the definition, carrying their wires and exposure along, so a
		// field keeps its port wherever it moves in the list. A port finds its field by id first, which
		// is how a rename keeps its wires, and by name where either side has no id; the ports still
		// unclaimed are removed.
		private void SyncFieldPorts( StructDefinitionData definition, int fieldCount )
		{
			int portCount = m_inputPorts.Count - 1;
			int[] source = new int[ fieldCount ];
			bool[] claimed = new bool[ portCount ];

			for ( int i = 0; i < fieldCount; i++ )
			{
				source[ i ] = -1;
				int id = definition.Fields[ i ].Id;
				if ( id == 0 )
				{
					continue;
				}

				for ( int k = 0; k < portCount; k++ )
				{
					if ( !claimed[ k ] && PortFieldId( k ) == id )
					{
						source[ i ] = k;
						claimed[ k ] = true;
						break;
					}
				}
			}

			for ( int i = 0; i < fieldCount; i++ )
			{
				if ( source[ i ] >= 0 )
				{
					continue;
				}

				for ( int k = 0; k < portCount; k++ )
				{
					if ( !claimed[ k ] && FieldInput( k ).Name == definition.Fields[ i ].Name )
					{
						source[ i ] = k;
						claimed[ k ] = true;
						break;
					}
				}
			}

			for ( int k = 0; k < portCount; k++ )
			{
				if ( !claimed[ k ] )
				{
					if ( FieldInput( k ).IsConnected )
					{
						m_containerGraph.DeleteConnection( true, UniqueId, FieldInput( k ).PortId, false, true );
					}

					if ( FieldOutput( k ).IsConnected )
					{
						m_containerGraph.DeleteConnection( false, UniqueId, FieldOutput( k ).PortId, false, true );
					}
				}
			}

			List<InputPort> inputs = new List<InputPort>( fieldCount + 1 ) { m_inputPorts[ 0 ] };
			List<OutputPort> outputs = new List<OutputPort>( fieldCount + 1 ) { m_outputPorts[ 0 ] };
			List<bool> inputExposed = new List<bool>( fieldCount );
			List<bool> outputExposed = new List<bool>( fieldCount );
			List<int> fieldIds = new List<int>( fieldCount );

			// @diogo: a port created here takes the next flag no port holds - the ones a passthrough keeps
			// while its source is away, or the ones set for a type that has just arrived
			int nextFree = portCount;
			for ( int i = 0; i < fieldCount; i++ )
			{
				int k = source[ i ];
				if ( k < 0 )
				{
					inputs.Add( AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue ) );
					AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
					outputs.Add( m_outputPorts[ m_outputPorts.Count - 1 ] );
					k = nextFree++;
				}
				else
				{
					inputs.Add( FieldInput( k ) );
					outputs.Add( FieldOutput( k ) );
				}

				inputExposed.Add( Exposed( m_inputExposed, k, false ) );
				outputExposed.Add( Exposed( m_outputExposed, k, false ) );
				fieldIds.Add( definition.Fields[ i ].Id );
			}

			m_inputPorts.Clear();
			m_inputPorts.AddRange( inputs );
			m_outputPorts.Clear();
			m_outputPorts.AddRange( outputs );
			m_portFieldIds.Clear();
			m_portFieldIds.AddRange( fieldIds );

			// @diogo: through ids no port holds first - renumbered in one pass, two field inputs fed by the
			// same output would swap onto the same id on that output's side of the wire
			for ( int i = 1; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].ChangePortId( -i );
				m_outputPorts[ i ].ChangePortId( -i );
			}

			m_inputPortsDict.Clear();
			m_outputPortsDict.Clear();
			for ( int i = 0; i < m_inputPorts.Count; i++ )
			{
				m_inputPorts[ i ].ChangePortId( i );
				m_outputPorts[ i ].ChangePortId( i );
				m_inputPortsDict.Add( i, m_inputPorts[ i ] );
				m_outputPortsDict.Add( i, m_outputPorts[ i ] );
			}

			// @diogo: kept as they are without a definition. A passthrough whose source was pulled has no
			// fields for a moment, and dropping the flags there would lose what the user set up on a struct
			// that is about to be wired back
			if ( fieldCount > 0 )
			{
				m_inputExposed.Clear();
				m_inputExposed.AddRange( inputExposed );
				m_outputExposed.Clear();
				m_outputExposed.AddRange( outputExposed );
			}
		}

		private bool PortsMatch( StructDefinitionData definition )
		{
			int fieldCount = ( definition != null ) ? definition.Fields.Count : 0;

			if ( m_inputPorts.Count != fieldCount + 1 || m_outputPorts.Count != fieldCount + 1 )
			{
				return false;
			}

			if ( m_portedTypeName != ( ( definition != null ) ? definition.CodeName : string.Empty ) )
			{
				return false;
			}

			for ( int i = 0; i < fieldCount; i++ )
			{
				InputPort input = FieldInput( i );
				if ( input.Name != definition.Fields[ i ].Name || input.DataType != definition.Fields[ i ].DataType || PortFieldId( i ) != definition.Fields[ i ].Id )
				{
					return false;
				}
			}

			return true;
		}

		private string UniqueFieldName( string name, int ownIndex )
		{
			string candidate = UIUtils.RemoveInvalidCharacters( name );
			if ( string.IsNullOrEmpty( candidate ) )
			{
				candidate = DefaultFieldName;
			}

			string unique = candidate;
			int suffix = 0;
			bool taken = true;
			while ( taken )
			{
				taken = false;
				for ( int i = 0; i < m_definition.Fields.Count; i++ )
				{
					if ( i != ownIndex && m_definition.Fields[ i ].Name.Equals( unique ) )
					{
						taken = true;
						suffix++;
						unique = candidate + suffix;
						break;
					}
				}
			}

			return unique;
		}

		// @diogo: held back and drawn from DrawProperties below, since the group grows a row for every
		// field input exposed - from its usual place above, each click would shift the checkbox that
		// caused it out from under the pointer
		public override void PreDrawProperties() { }

		public override void DrawProperties()
		{
			base.DrawProperties();
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, Constants.ParameterLabelStr, DrawParameters );

			base.PreDrawProperties();

			NodeUtils.DrawHelpBox( NodeInfo, MessageType.Info );
		}

		private void DrawParameters()
		{
			if ( !IsPassthrough )
			{
				EditorGUI.BeginChangeCheck();
				StructOwnership ownership = ( StructOwnership )EditorGUILayoutEnumPopup( OwnershipStr, m_ownership );
				if ( EditorGUI.EndChangeCheck() )
				{
					if ( ownership == StructOwnership.Reference )
					{
						EnterReferenceMode();
					}
					else
					{
						m_ownership = ownership;
					}

					ClaimUniqueTypeName();
					RefreshRegistration();
					UpdatePorts();
					UpdateSubtitle();
				}

				if ( m_ownership == StructOwnership.Reference )
				{
					DrawReferencePicker();
				}
			}

			// @diogo: a borrowed or passed-through definition is edited where it lives, so it is shown
			// here but not editable
			StructDefinitionData definition = OwnsDefinition ? m_definition : ActiveDefinition;

			if ( definition == null )
			{
				NodeUtils.DrawHelpBox( MissingDefinitionMessage(), MessageType.Error );
			}
			else
			{
				if ( OwnsDefinition )
				{
					EditorGUI.BeginChangeCheck();
					string typeName = EditorGUILayoutTextField( TypeNameStr, definition.TypeName );
					if ( EditorGUI.EndChangeCheck() )
					{
						// @diogo: gathered before the rename, which is what stops them finding this node
						List<StructNode> referencing = ReferencingNodes();

						// @diogo: corrected as it is typed, so a definition this node owns can never hold a
						// name HLSL would refuse; an asset is the only place a free-form one survives
						definition.TypeName = UniqueTypeName( StructDefinitionData.CleanTypeName( typeName ) );
						m_containerGraph.StructNodes.UpdateDataOnNode( UniqueId, definition.CodeName );

						for ( int i = 0; i < referencing.Count; i++ )
						{
							referencing[ i ].FollowRename( definition.CodeName );
						}

						UpdatePorts();
						UpdateSubtitle();
					}
				}
				else
				{
					// @diogo: a borrowed name is not this node's to edit, and the asset behind it may be
					// called anything, so what is shown is the form the shader will actually use
					NodeUtils.DrawDisabledValue( TypeNameStr, definition.CodeName );
				}

				// @diogo: always the same control, so Create and Fetch are only ever met under this one
				// label; a definition that lives elsewhere decides its own, so the choice is disabled
				// rather than hidden and the panel keeps its shape
				if ( OwnsDefinition )
				{
					definition.DeclarationMode = ( VariableMode )EditorGUILayoutEnumPopup( DeclarationStr, definition.DeclarationMode );
				}
				else
				{
					NodeUtils.DrawDisabledEnum( DeclarationStr, definition.DeclarationMode );
				}

				if ( OwnsDefinition )
				{
					definition.Tint = EditorGUILayoutColorField( TintContent, definition.Tint, true, false, false );
				}
				else
				{
					EditorGUI.BeginDisabledGroup( true );
					EditorGUILayout.ColorField( TintContent, definition.Tint, true, false, false );
					EditorGUI.EndDisabledGroup();
				}

				DrawDataExposure();

				NodeUtils.DrawPropertyGroup( ref m_fieldsFoldout, FieldsStr, DrawFieldsList, DrawFieldsAddRemove );
			}

			if ( !IsPassthrough )
			{
				DrawOwnershipButton();
				DrawClashWarning();
			}

			if ( m_actionType != ReordableAction.None )
			{
				switch ( m_actionType )
				{
					case ReordableAction.Add:
					AddFieldAt( m_actionIndex + 1, true );
					break;
					case ReordableAction.Remove:
					RemoveFieldAt( m_actionIndex );
					break;
				}

				m_isDirty = true;
				m_actionType = ReordableAction.None;
				EditorGUI.FocusTextInControl( null );
			}
		}

		// Writes the inline definition out as an asset and follows it there. Reversible through the
		// button that replaces it once the node is borrowing.
		private void PromoteToAsset()
		{
			string path = EditorUtility.SaveFilePanelInProject( PromoteTitleStr, m_definition.TypeName, "asset", string.Empty );
			if ( !string.IsNullOrEmpty( path ) )
			{
				PromoteToAsset( path );
			}
		}

		private void PromoteToAsset( string path )
		{
			AmplifyShaderStruct asset = AssetDatabase.LoadMainAssetAtPath( path ) as AmplifyShaderStruct;
			bool created = ( asset == null );
			if ( created )
			{
				asset = ScriptableObject.CreateInstance<AmplifyShaderStruct>();
			}

			asset.Definition.CopyFrom( m_definition );

			if ( created )
			{
				AssetDatabase.CreateAsset( asset, path );
			}
			else
			{
				EditorUtility.SetDirty( asset );
			}
			AssetDatabase.SaveAssets();

			UndoRecordObject( "Promoted struct definition" );

			string typeName = m_definition.CodeName;
			string guid = AssetDatabase.AssetPathToGUID( path );

			// @diogo: an asset is named by the file it lives in, and the save dialog lets that differ from
			// the inline name, so the name to remember has to be read back off the asset
			string assetTypeName = asset.Definition.CodeName;

			// @diogo: every node pointing at this Local has to follow it to the asset, and before this one
			// stops owning the definition, since that is what they currently resolve through
			List<ParentNode> nodes = m_containerGraph.AllNodes;
			for ( int i = 0; i < nodes.Count; i++ )
			{
				StructNode other = nodes[ i ] as StructNode;
				if ( other != null && other != this )
				{
					other.RepointToAsset( typeName, assetTypeName, guid );
				}
			}

			EnterReferenceMode();
			m_referenceName = assetTypeName;
			m_referenceGUID = guid;
			m_referenceAsset = asset;
			m_referenceAssetResolved = true;
			RefreshRegistration();
			UpdatePorts();
			UpdateSubtitle();
			EditorGUIUtility.PingObject( asset );
		}

		// Follows a Local definition that has just become an asset. Only a node bound to it by name
		// moves; one already pointing at an asset keeps the link it has.
		public void RepointToAsset( string typeName, string assetTypeName, string guid )
		{
			if ( m_ownership == StructOwnership.Reference && string.IsNullOrEmpty( m_referenceGUID ) && m_referenceName.Equals( typeName ) )
			{
				UndoRecordObject( "Repointed struct definition reference" );
				m_referenceName = assetTypeName;
				m_referenceGUID = guid;
				m_referenceAsset = null;
				m_referenceAssetResolved = false;
				UpdatePorts();
				UpdateSubtitle();
			}
		}

		// The nodes borrowing this node's Local definition, found the way they find it themselves.
		private List<StructNode> ReferencingNodes()
		{
			List<StructNode> referencing = new List<StructNode>();
			List<ParentNode> nodes = m_containerGraph.AllNodes;
			for ( int i = 0; i < nodes.Count; i++ )
			{
				if ( nodes[ i ] is StructNode other && other != this && other.m_ownership == StructOwnership.Reference &&
					string.IsNullOrEmpty( other.m_referenceGUID ) && other.ReferenceNode == this )
				{
					referencing.Add( other );
				}
			}
			return referencing;
		}

		// Keeps following a Local whose type was just renamed, since the name is how a reference finds it.
		public void FollowRename( string typeName )
		{
			m_referenceName = typeName;
			UpdatePorts();
			UpdateSubtitle();
		}

		// Copies the borrowed definition back inline and drops the link, leaving the source untouched.
		private void DemoteToLocal()
		{
			StructDefinitionData source = ActiveDefinition;
			if ( source == null )
			{
				return;
			}

			UndoRecordObject( "Copied struct definition inline" );
			m_definition.CopyFrom( source );
			m_definition.EnsureFieldIds();
			m_ownership = StructOwnership.Local;
			m_referenceName = string.Empty;
			m_referenceGUID = string.Empty;
			m_referenceAsset = null;
			m_referenceAssetResolved = false;
			ClaimUniqueTypeName();
			RefreshRegistration();
			UpdatePorts();
			UpdateSubtitle();
		}

		// The passthrough pair is not a field, so it is settled here rather than in the list - and unlike
		// the fields, showing one is a statement about what this node is for, not about which parts of a
		// struct are interesting.
		private void DrawDataExposure()
		{
			Rect rect = NodeUtils.DrawPrefixedRow( DataPortsLabel, EditorStyles.toggle );

			// @diogo: the rect overloads indent what they are given, and this rect is already past the
			// prefix that spent the indent - left on, both boxes would sit a level right of the column
			int cachedIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			Rect inToggleRect = new Rect( rect.x, rect.y, FieldToggleWidth, EditorGUIUtility.singleLineHeight );
			Rect outToggleRect = new Rect( inToggleRect.xMax + FieldRowSpacing, rect.y, FieldToggleWidth, EditorGUIUtility.singleLineHeight );

			EditorGUI.BeginChangeCheck();
			bool dataInput = EditorGUI.Toggle( inToggleRect, m_dataInputExposed );
			bool dataOutput = EditorGUI.Toggle( outToggleRect, m_dataOutputExposed );
			if ( EditorGUI.EndChangeCheck() )
			{
				UndoRecordObject( ExposureUndoStr );
				m_dataInputExposed = dataInput;
				m_dataOutputExposed = dataOutput;
				UpdatePorts();
			}

			GUI.Label( inToggleRect, DataInTooltip );
			GUI.Label( outToggleRect, DataOutTooltip );

			EditorGUI.indentLevel = cachedIndentLevel;
		}

		// The two columns of field checkboxes summed up, above the ones they stand for: ticked when every
		// field shows that port, empty when none does, and a dash in between. Clicking sets them all.
		private void DrawExposureHeader( int fieldCount )
		{
			// @diogo: the list draws its rows with the indent zeroed and inset by the drag handle column
			// it reserves - only its padding when rows cannot be dragged - so this row is placed the same
			// way or the checkboxes would not line up
			int cachedIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			Rect rect = EditorGUILayout.GetControlRect( true, EditorGUIUtility.singleLineHeight, GUIStyle.none );
			rect.xMin += OwnsDefinition ? ReorderableList.Defaults.dragHandleWidth : ReorderableList.Defaults.padding;

			Rect inToggleRect = new Rect( rect.x, rect.y, FieldToggleWidth, EditorGUIUtility.singleLineHeight );
			Rect outToggleRect = new Rect( inToggleRect.xMax + FieldRowSpacing, rect.y, FieldToggleWidth, EditorGUIUtility.singleLineHeight );

			DrawExposureToggle( inToggleRect, m_inputExposed, true, fieldCount, AllInputsTooltip );
			DrawExposureToggle( outToggleRect, m_outputExposed, false, fieldCount, AllOutputsTooltip );

			EditorGUI.indentLevel = cachedIndentLevel;
		}

		private void DrawExposureToggle( Rect rect, List<bool> exposure, bool inputs, int fieldCount, GUIContent tooltip )
		{
			int exposedCount = 0;
			for ( int i = 0; i < fieldCount; i++ )
			{
				if ( Exposed( exposure, i, false ) )
				{
					exposedCount++;
				}
			}

			// @diogo: a partial column reads as unticked, so the click that follows the dash shows every
			// field rather than hiding the ones already on
			EditorGUI.showMixedValue = exposedCount > 0 && exposedCount < fieldCount;

			EditorGUI.BeginChangeCheck();
			bool exposed = EditorGUI.Toggle( rect, exposedCount == fieldCount );
			if ( EditorGUI.EndChangeCheck() )
			{
				UndoRecordObject( ExposureUndoStr );
				MarkExposureSetUp();
				SetExposure( exposure, fieldCount, exposed );

				// @diogo: hiding them all stops at a wired port, which would take its wire out of sight
				if ( !exposed )
				{
					for ( int i = 0; i < fieldCount; i++ )
					{
						WirePort port = inputs ? ( WirePort )FieldInput( i ) : FieldOutput( i );
						if ( port != null && port.IsConnected )
						{
							exposure[ i ] = true;
						}
					}
				}

				UpdatePorts();
			}
			EditorGUI.showMixedValue = false;

			GUI.Label( rect, tooltip );
		}

		private void DrawOwnershipButton()
		{
			if ( m_ownership == StructOwnership.Local )
			{
				EditorGUI.BeginDisabledGroup( string.IsNullOrEmpty( m_definition.TypeName ) || m_definition.Fields.Count == 0 );
				if ( GUILayoutButton( PromoteStr, EditorStyles.miniButton ) )
				{
					PromoteToAsset();
				}
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				EditorGUI.BeginDisabledGroup( ActiveDefinition == null );
				if ( GUILayoutButton( DemoteStr, EditorStyles.miniButton ) )
				{
					DemoteToLocal();
				}
				EditorGUI.EndDisabledGroup();
			}
		}

		// One picker for both kinds of target: the graph's Local definitions and the project's assets,
		// told apart by suffix rather than by living in separate dropdowns.
		private void DrawReferencePicker()
		{
			m_referenceOptionNames.Clear();
			m_referenceOptionDisplays.Clear();
			m_referenceOptionGUIDs.Clear();

			List<StructNode> locals = m_containerGraph.StructNodes.NodesList;
			if ( locals != null )
			{
				for ( int i = 0; i < locals.Count; i++ )
				{
					string typeName = locals[ i ].LocalDefinition.CodeName;
					if ( locals[ i ] != this && !string.IsNullOrEmpty( typeName ) )
					{
						// @diogo: a Local is only ever named by hand, so it has no separate asset name
						m_referenceOptionNames.Add( typeName );
						m_referenceOptionDisplays.Add( typeName );
						m_referenceOptionGUIDs.Add( string.Empty );
					}
				}
			}

			int localCount = m_referenceOptionNames.Count;
			List<string> assetNames = AmplifyShaderStructCache.AssetNames;
			List<string> assetGUIDs = AmplifyShaderStructCache.GUIDs;
			for ( int i = 0; i < assetNames.Count; i++ )
			{
				m_referenceOptionNames.Add( StructDefinitionData.CleanTypeName( assetNames[ i ] ) );
				m_referenceOptionDisplays.Add( assetNames[ i ] );
				m_referenceOptionGUIDs.Add( assetGUIDs[ i ] );
			}

			int count = m_referenceOptionNames.Count;
			if ( m_referenceOptions == null || m_referenceOptions.Length != count )
			{
				m_referenceOptions = new string[ count ];
			}

			int current = -1;
			for ( int i = 0; i < count; i++ )
			{
				bool isAsset = !string.IsNullOrEmpty( m_referenceOptionGUIDs[ i ] );
				m_referenceOptions[ i ] = m_referenceOptionDisplays[ i ] + ( isAsset ? AssetSuffix : LocalSuffix );

				if ( isAsset ? m_referenceOptionGUIDs[ i ].Equals( m_referenceGUID ) : ( string.IsNullOrEmpty( m_referenceGUID ) && m_referenceOptionNames[ i ].Equals( m_referenceName ) ) )
				{
					current = i;
				}
			}

			EditorGUILayout.BeginHorizontal();
			SearchableDropdown.Draw( ReferenceStr, current, m_referenceOptions, ( idx ) =>
			{
				if ( idx >= 0 && idx < m_referenceOptionNames.Count )
				{
					UndoRecordObject( "Selected struct definition reference" );
					SetReferenceTo( m_referenceOptionNames[ idx ], m_referenceOptionGUIDs[ idx ] );
					SetDirtyFromAsyncEdit();
				}
			}, localCount > 0 && localCount < count ? localCount : -1 );

			if ( string.IsNullOrEmpty( m_referenceGUID ) )
			{
				UIUtils.DrawReferenceFocusButton( ReferenceNode );
			}
			else
			{
				UIUtils.DrawAssetPingButton( ReferenceAsset );
			}
			EditorGUILayout.EndHorizontal();
		}

		// Only a graph saved with two Locals under one name reaches this, since a name is kept unique as it
		// is chosen. Unless both describe the same type, one of the two declarations is dropped at
		// generation and the shader silently builds against the wrong one.
		private void DrawClashWarning()
		{
			if ( !OwnsDefinition || string.IsNullOrEmpty( m_definition.TypeName ) )
			{
				return;
			}

			List<StructNode> locals = m_containerGraph.StructNodes.NodesList;
			if ( locals == null )
			{
				return;
			}

			for ( int i = 0; i < locals.Count; i++ )
			{
				// @diogo: the declarations are only built for a name that actually collides, so the common
				// case costs a string compare per node and no allocation
				if ( locals[ i ] != this && locals[ i ].LocalDefinition.CodeName.Equals( m_definition.CodeName ) &&
					!locals[ i ].LocalDefinition.BuildDeclaration( CurrentPrecisionType ).Equals( m_definition.BuildDeclaration( CurrentPrecisionType ) ) )
				{
					NodeUtils.DrawHelpBox( string.Format( ClashWarning, m_definition.TypeName ), MessageType.Error );
					return;
				}
			}
		}

		// @diogo: acts on the press instead of on GUI.Button's release. This panel's control ids are not
		// stable between the two passes of a click, and a button that loses hotControl in between never
		// reports the release - which dropped every one of these clicks that followed a click elsewhere
		private static bool PressButton( Rect rect, GUIContent content, GUIStyle style )
		{
			GUI.Label( rect, content, style );

			Event current = Event.current;
			if ( GUI.enabled && current.type == EventType.MouseDown && current.button == 0 && rect.Contains( current.mousePosition ) )
			{
				current.Use();
				return true;
			}

			return false;
		}

		// @diogo: the layout slot GUILayout.Button would have taken, so the row keeps its shape
		private static Rect PressButtonRect( GUIStyle style )
		{
			return GUILayoutUtility.GetRect( GUIContent.none, style, GUILayout.Width( Constants.PlusMinusButtonLayoutWidth ) );
		}

		private void DrawFieldsAddRemove()
		{
			using ( new EditorGUI.DisabledScope( !OwnsDefinition ) )
			{
				if ( PressButton( PressButtonRect( UIUtils.PlusStyle ), AddFieldTooltip, UIUtils.PlusStyle ) )
				{
					UndoRecordObject( "Added struct field" );
					AddFieldAt( m_definition.Fields.Count );
					m_fieldsFoldout = true;
					EditorGUI.FocusTextInControl( null );
				}

				if ( PressButton( PressButtonRect( UIUtils.MinusStyle ), RemoveFieldTooltip, UIUtils.MinusStyle ) )
				{
					UndoRecordObject( "Removed struct field" );
					RemoveFieldAt( m_definition.Fields.Count - 1 );
					EditorGUI.FocusTextInControl( null );
				}
			}
		}

		private void DrawFieldsList()
		{
			// @diogo: the list binds to one List instance, so switching to a borrowed definition rebuilds it
			StructDefinitionData shown = OwnsDefinition ? m_definition : ActiveDefinition;
			if ( shown == null )
			{
				return;
			}

			if ( m_fieldsListSource != shown.Fields )
			{
				m_fieldsListSource = shown.Fields;
				m_fieldsList = null;
			}

			if ( m_fieldsList == null )
			{
				m_fieldsList = new ReorderableList( m_fieldsListSource, typeof( StructFieldData ), true, false, false, false )
				{
					headerHeight = 0,
					footerHeight = 0,
					showDefaultBackground = false,
					elementHeight = EditorGUIUtility.singleLineHeight + 2,

					onReorderCallbackWithDetails = ( ReorderableList list, int oldIndex, int newIndex ) =>
					{
						// @diogo: the list has already moved the field; move it back so the snapshot holds the pre-drag order
						m_definition.MoveField( newIndex, oldIndex );
						UndoRecordObject( "Reordered struct field" );
						m_definition.MoveField( oldIndex, newIndex );
						UpdatePorts();
						m_isDirty = true;
					},

					drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
					{
						if ( index >= m_fieldsListSource.Count )
						{
							return;
						}

						StructFieldData field = m_fieldsListSource[ index ];
						float typeWidth = 70;
						float buttonWidth = Constants.PlusMinusButtonLayoutWidth;
						float nameWidth = rect.width - 2 * FieldToggleWidth - typeWidth - 2 * buttonWidth - 6 * FieldRowSpacing;

						Rect inToggleRect = new Rect( rect.x, rect.y, FieldToggleWidth, EditorGUIUtility.singleLineHeight );
						Rect outToggleRect = new Rect( inToggleRect.xMax + FieldRowSpacing, rect.y, FieldToggleWidth, EditorGUIUtility.singleLineHeight );
						Rect nameRect = new Rect( outToggleRect.xMax + FieldRowSpacing, rect.y, nameWidth, EditorGUIUtility.singleLineHeight );
						Rect typeRect = new Rect( nameRect.xMax + FieldRowSpacing, rect.y, typeWidth, EditorGUIUtility.singleLineHeight );
						Rect addRect = new Rect( typeRect.xMax + FieldRowSpacing, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight );
						Rect removeRect = new Rect( addRect.xMax + FieldRowSpacing, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight );

						// @diogo: exposing a port is never the definition's business, so these two stay
						// enabled even when the rest of the row - a borrowed or passed-through definition -
						// is not this node's to edit
						EditorGUI.BeginChangeCheck();
						bool inExposed = EditorGUI.Toggle( inToggleRect, Exposed( m_inputExposed, index, false ) );
						if ( EditorGUI.EndChangeCheck() )
						{
							UndoRecordObject( "Exposed struct field" );
							MarkExposureSetUp();
							m_inputExposed[ index ] = inExposed;
							FieldInput( index ).Visible = inExposed;
							m_sizeIsDirty = true;
						}

						EditorGUI.BeginChangeCheck();
						bool outExposed = EditorGUI.Toggle( outToggleRect, Exposed( m_outputExposed, index, false ) );
						if ( EditorGUI.EndChangeCheck() )
						{
							UndoRecordObject( "Exposed struct field" );
							MarkExposureSetUp();
							m_outputExposed[ index ] = outExposed;
							FieldOutput( index ).Visible = outExposed;
							m_sizeIsDirty = true;
						}

						using ( new EditorGUI.DisabledScope( !OwnsDefinition ) )
						{
							EditorGUI.BeginChangeCheck();
							field.Name = EditorGUITextField( nameRect, string.Empty, field.Name );
							if ( EditorGUI.EndChangeCheck() )
							{
								field.Name = UniqueFieldName( field.Name, index );
								UpdatePorts();
							}

							EditorGUI.BeginChangeCheck();
							int typeIdx = Mathf.Max( 0, Array.IndexOf( StructDefinitionData.AvailableFieldTypes, field.DataType ) );
							typeIdx = EditorGUIPopup( typeRect, typeIdx, StructDefinitionData.FieldTypeLabels, EditorStyles.popup );
							if ( EditorGUI.EndChangeCheck() )
							{
								field.DataType = StructDefinitionData.AvailableFieldTypes[ typeIdx ];
								UpdatePorts();
							}

							if ( PressButton( addRect, AddTooltip, UIUtils.PlusStyle ) )
							{
								m_actionType = ReordableAction.Add;
								m_actionIndex = index;
							}

							if ( PressButton( removeRect, RemoveTooltip, UIUtils.MinusStyle ) )
							{
								m_actionType = ReordableAction.Remove;
								m_actionIndex = index;
							}
						}

						// @diogo: drawn last and outside the disabled scope, so the tooltip wins the rect
						// and still appears on the cells a borrowed definition greys out
						GUI.Label( inToggleRect, InTooltip );
						GUI.Label( outToggleRect, OutTooltip );
						GUI.Label( nameRect, NameTooltip );
						GUI.Label( typeRect, TypeTooltip );
					}
				};
			}

			// @diogo: field order is part of the definition, so only the node that owns it may reorder it
			m_fieldsList.draggable = OwnsDefinition;

			if ( m_fieldsListSource.Count == 0 )
			{
				NodeUtils.DrawHelpBox( NoFieldsStr, MessageType.Info );
			}
			else
			{
				DrawExposureHeader( m_fieldsListSource.Count );
				m_fieldsList.DoLayoutList();
			}
		}

		private void AddFieldAt( int index, bool inheritSettings = false )
		{
			int at = Mathf.Clamp( index, 0, m_definition.Fields.Count );

			// @diogo: a field added from a row duplicates it - same type, same exposure, and the name it
			// was cloned from carried on to the next free index
			StructFieldData source = ( inheritSettings && at > 0 ) ? m_definition.Fields[ at - 1 ] : null;

			// @diogo: a field added by hand is one this node means to assign, so its input leads and the
			// output waits
			bool inExposed = ( source == null ) || Exposed( m_inputExposed, at - 1, true );
			bool outExposed = ( source != null ) && Exposed( m_outputExposed, at - 1, false );

			StructFieldData field = new StructFieldData();
			field.Id = m_definition.NewFieldId();
			if ( source != null )
			{
				field.DataType = source.DataType;
				field.TypeName = source.TypeName;
			}

			m_definition.Fields.Insert( at, field );
			field.Name = UniqueFieldName( ( source != null ) ? StructDefinitionData.FieldNameBase( source.Name ) : DefaultFieldName, at );

			UpdatePorts();

			// @diogo: set once the port exists, since the sync gives a port it creates the default exposure
			m_inputExposed[ at ] = inExposed;
			m_outputExposed[ at ] = outExposed;
			FieldInput( at ).Visible = inExposed;
			FieldOutput( at ).Visible = outExposed;
		}

		private void RemoveFieldAt( int index )
		{
			if ( index >= 0 && index < m_definition.Fields.Count )
			{
				m_definition.Fields.RemoveAt( index );
				UpdatePorts();
			}
		}

		public override void Reset()
		{
			base.Reset();
			m_emitted[ 0 ] = null;
			m_emitted[ 1 ] = null;
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			nodeData.Fields = DemandForOutput( nodeData.OutputPortId, nodeData.Fields );
			if ( m_demandOwner != dataCollector )
			{
				// @diogo: the record is per pass, but ResetNodesData never reaches a function's interior and making it
				// recurse changes the categories branch scoping reads there, so the pass's collector is the key instead
				m_demandOwner = dataCollector;
				m_demand[ 0 ] = null;
				m_demand[ 1 ] = null;
			}

			// @diogo: a walk that demands nothing says nothing about what generation may read
			if ( !nodeData.Fields.IsNone )
			{
				int slot = UIUtils.PortCategorytoAttayIdx( nodeData.Category );
				m_demand[ slot ] = ( m_demand[ slot ] == null ) ? nodeData.Fields : m_demand[ slot ].Union( nodeData.Fields );
			}

			base.PropagateNodeData( nodeData, ref dataCollector );
		}

		// Port 0 carries the whole value, so the demand that arrived on it stands; a field port names one field, unless nothing was demanded at all.
		private StructFieldMask DemandForOutput( int portId, StructFieldMask received )
		{
			OutputPort port = GetOutputPortByUniqueId( portId );
			int index = m_outputPorts.IndexOf( port );
			return ( index <= 0 || received.IsNone ) ? received : StructFieldMask.Field( index - 1 );
		}

		public override StructFieldMask FieldDemandForInput( int inputIndex, StructFieldMask received )
		{
			if ( inputIndex > 0 )
			{
				return received.Contains( inputIndex - 1 ) ? StructFieldMask.All : null;
			}

			// @diogo: input 0 is the passthrough Data input: forward only the demanded fields no wired input overrides
			if ( received.IsAll )
			{
				return StructFieldMask.All;
			}

			StructFieldMask forward = StructFieldMask.None;
			StructDefinitionData definition = ActiveDefinition;
			int count = ( definition != null ) ? definition.Fields.Count : 0;
			for ( int i = 0; i < count; i++ )
			{
				InputPort input = FieldInput( i );
				if ( received.Contains( i ) && ( input == null || !input.IsConnected ) )
				{
					forward = forward.Union( StructFieldMask.Field( i ) );
				}
			}
			return forward;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			StructDefinitionData definition = ActiveDefinition;
			if ( definition == null )
			{
				UIUtils.ShowMessage( UniqueId, MissingDefinitionMessage(), MessageSeverity.Error );
				return ErrorValueFor( outputId );
			}

			int slot = UIUtils.PortCategorytoAttayIdx( dataCollector.PortCategory );
			StructFieldMask demand = RecordedDemand( slot, dataCollector );
			StructFieldMask needed = NeededFor( outputId, demand, ref dataCollector );

			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				EnsureFieldsEmitted( needed, definition, slot, ref dataCollector );
				return ValueForOutput( outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ), definition );
			}

			string typeName = definition.CodeName;
			if ( string.IsNullOrEmpty( typeName ) )
			{
				UIUtils.ShowMessage( UniqueId, NoTypeNameError, MessageSeverity.Error );
				return ErrorValueFor( outputId );
			}

			if ( definition.Fields.Count == 0 )
			{
				UIUtils.ShowMessage( UniqueId, NoFieldsError, MessageSeverity.Error );
				return ErrorValueFor( outputId );
			}

			// @diogo: nothing to build when the value only flows through, so the fields are read straight
			// off the source and no copy is emitted at all - the same shape the old Struct Split had
			if ( IsPassthrough && !AnyFieldInputConnected() )
			{
				string source = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
				m_outputPorts[ 0 ].SetLocalValue( source, dataCollector.PortCategory );
				m_emitted[ slot ] = StructFieldMask.All;
				return ValueForOutput( outputId, source, definition );
			}

			string varName = LocalVarPrefix + OutputId;

			if ( IsPassthrough )
			{
				// @diogo: a struct assignment is a plain copy in HLSL, so the overrides below are all that
				// separates the value coming out from the one that came in
				string source = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
				dataCollector.AddLocalVariable( UniqueId, typeName + " " + varName + " = " + source + ";", true );
			}
			else
			{
				// @diogo: in Fetch mode the type comes from an include ASE never saw, so there is nothing to
				// declare and nothing to check the field list against
				if ( definition.DeclarationMode == VariableMode.Create &&
					!dataCollector.AddStruct( UniqueId, typeName, definition.BuildDeclaration( CurrentPrecisionType ) ) )
				{
					UIUtils.ShowMessage( UniqueId, string.Format( TypeNameClashError, typeName ), MessageSeverity.Error );
					return ErrorValueFor( outputId );
				}

				dataCollector.AddLocalVariable( UniqueId, typeName + " " + varName + " = ( " + typeName + " )0;", true );
			}

			// @diogo: assigned field by field rather than through an initializer list, which would depend
			// on field order and on every field being present in the type ASE may not have declared
			StructFieldMask emitted = StructFieldMask.None;
			for ( int i = 0; i < definition.Fields.Count; i++ )
			{
				InputPort input = FieldInput( i );
				if ( input == null )
				{
					continue;
				}

				// A passthrough already holds every field, so only a wired one is an override.
				if ( IsPassthrough && !input.IsConnected )
				{
					emitted = emitted.Union( StructFieldMask.Field( i ) );
					continue;
				}

				if ( !demand.Contains( i ) && !needed.Contains( i ) )
				{
					continue;
				}

				EmitFieldAssignment( i, varName, definition, ref dataCollector );
				emitted = emitted.Union( StructFieldMask.Field( i ) );
			}

			// @diogo: what a read may take off the local - the copied fields plus the overrides the pre-pass asked for
			m_emitted[ slot ] = ( demand.IsAll || needed.IsAll ) ? StructFieldMask.All : emitted;
			m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );
			return ValueForOutput( outputId, varName, definition );
		}

		private void EmitFieldAssignment( int fieldIndex, string varName, StructDefinitionData definition, ref MasterNodeDataCollector dataCollector )
		{
			string value = FieldInput( fieldIndex ).GeneratePortInstructions( ref dataCollector );
			dataCollector.AddLocalVariable( UniqueId, varName + "." + definition.Fields[ fieldIndex ].Name + " = " + value + ";", true );
		}

		// The field demand the pre-pass recorded for this stage slot in the current pass; every field when it recorded nothing
		private StructFieldMask RecordedDemand( int slot, MasterNodeDataCollector dataCollector )
		{
			return ( m_demandOwner == dataCollector && m_demand[ slot ] != null ) ? m_demand[ slot ] : StructFieldMask.All;
		}

		// Whether the pre-pass walked every wired reader of the whole value this pass; a reader it never saw may take any field off a copy
		private bool AllReadersWalked( ref MasterNodeDataCollector dataCollector )
		{
			int count = m_outputPorts[ 0 ].ConnectionCount;
			for ( int i = 0; i < count; i++ )
			{
				ParentNode reader = m_outputPorts[ 0 ].GetInputNode( i );
				if ( reader == null || !dataCollector.WasTouched( reader ) )
				{
					return false;
				}
			}
			return true;
		}

		// What a read through this port must find on the local: a field port names its field; the whole value needs
		// the pass's record when every wired reader of it was walked, otherwise every field, since a reader the
		// pre-pass never saw may take any of them off the copy
		private StructFieldMask NeededFor( int outputId, StructFieldMask demand, ref MasterNodeDataCollector dataCollector )
		{
			OutputPort port = GetOutputPortByUniqueId( outputId );
			int index = m_outputPorts.IndexOf( port );
			if ( index > 0 )
			{
				return StructFieldMask.Field( index - 1 );
			}

			return AllReadersWalked( ref dataCollector ) ? demand : StructFieldMask.All;
		}

		// Whether a wire into this input generates in the current pass: the Data input always does; a field input only
		// when its field is in the pass's record, or when a reader the pre-pass never walked forces every field
		public bool IsInputLive( int portId, MasterNodeDataCollector dataCollector )
		{
			InputPort port = GetInputPortByUniqueId( portId );
			int index = m_inputPorts.IndexOf( port );
			if ( index <= 0 )
			{
				return true;
			}

			int slot = UIUtils.PortCategorytoAttayIdx( dataCollector.PortCategory );
			return RecordedDemand( slot, dataCollector ).Contains( index - 1 ) || !AllReadersWalked( ref dataCollector );
		}

		// @diogo: a read the pre-pass never asked for - analysis miss; generate the field late and say so
		private void EnsureFieldsEmitted( StructFieldMask needed, StructDefinitionData definition, int slot, ref MasterNodeDataCollector dataCollector )
		{
			StructFieldMask emitted = m_emitted[ slot ];
			if ( emitted == null || emitted.IsAll )
			{
				return;
			}

			if ( emitted.Covers( needed ) )
			{
				return;
			}

			string varName = m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
			for ( int i = 0; i < definition.Fields.Count; i++ )
			{
				if ( needed.Contains( i ) && !emitted.Contains( i ) && FieldInput( i ) != null )
				{
					EmitFieldAssignment( i, varName, definition, ref dataCollector );
				}
			}
			m_emitted[ slot ] = emitted.Union( needed );
			UIUtils.ShowMessage( UniqueId, LateFieldWarning, MessageSeverity.Warning );
		}

		private bool AnyFieldInputConnected()
		{
			for ( int i = 1; i < m_inputPorts.Count; i++ )
			{
				if ( m_inputPorts[ i ].IsConnected )
				{
					return true;
				}
			}
			return false;
		}

		private string ValueForOutput( int outputId, string varName, StructDefinitionData definition )
		{
			OutputPort port = GetOutputPortByUniqueId( outputId );
			int index = m_outputPorts.IndexOf( port );

			if ( index <= 0 )
			{
				return varName;
			}

			int fieldIdx = index - 1;
			return ( fieldIdx < definition.Fields.Count ) ? varName + "." + definition.Fields[ fieldIdx ].Name : varName;
		}

		private string ErrorValueFor( int outputId )
		{
			OutputPort port = GetOutputPortByUniqueId( outputId );
			return ( port != null ) ? port.ErrorValue : string.Empty;
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );

			IOUtils.AddFieldValueToString( ref nodeInfo, ( int )m_ownership );
			IOUtils.AddFieldValueToString( ref nodeInfo, ( int )m_definition.DeclarationMode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_definition.TypeName );
			IOUtils.AddFieldValueToString( ref nodeInfo, IOUtils.ColorToString( m_definition.Tint ) );

			// @diogo: a pick left over from Reference mode is not saved, or it would read as a dependency
			bool isReference = m_ownership == StructOwnership.Reference;
			IOUtils.AddFieldValueToString( ref nodeInfo, isReference ? m_referenceName : string.Empty );
			IOUtils.AddFieldValueToString( ref nodeInfo, isReference ? m_referenceGUID : string.Empty );

			IOUtils.AddFieldValueToString( ref nodeInfo, m_definition.Fields.Count );
			for ( int i = 0; i < m_definition.Fields.Count; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_definition.Fields[ i ].Name );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_definition.Fields[ i ].DataType );
			}

			// @diogo: the ports that exist right now, whoever's definition they came from. Written so they
			// are back before the loader applies the wires - a port restored after its wire would drop it
			int portedCount = m_inputPorts.Count - 1;
			IOUtils.AddFieldValueToString( ref nodeInfo, portedCount );
			for ( int i = 0; i < portedCount; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_inputPorts[ i + 1 ].Name );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_inputPorts[ i + 1 ].DataType );
				IOUtils.AddFieldValueToString( ref nodeInfo, Exposed( m_inputExposed, i, false ) );
				IOUtils.AddFieldValueToString( ref nodeInfo, Exposed( m_outputExposed, i, false ) );
			}

			IOUtils.AddFieldValueToString( ref nodeInfo, m_dataInputExposed );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_dataOutputExposed );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_exposedForTypeName );

			// @diogo: the ids trail everything else behind a marker, so metadata from before them is read
			// up to here and the ports fall back to matching by name
			IOUtils.AddFieldValueToString( ref nodeInfo, FieldIdsMarker );
			for ( int i = 0; i < m_definition.Fields.Count; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_definition.Fields[ i ].Id );
			}

			for ( int i = 0; i < portedCount; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, PortFieldId( i ) );
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );

			m_ownership = ( StructOwnership )Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_definition.DeclarationMode = ( VariableMode )Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_definition.TypeName = GetCurrentParam( ref nodeParams );
			m_definition.Tint = IOUtils.StringToColor( GetCurrentParam( ref nodeParams ) );
			m_referenceName = GetCurrentParam( ref nodeParams );
			m_referenceGUID = GetCurrentParam( ref nodeParams );
			m_referenceAsset = null;
			m_referenceAssetResolved = false;

			int fieldCount = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_definition.Fields.Clear();
			for ( int i = 0; i < fieldCount; i++ )
			{
				StructFieldData field = new StructFieldData();
				field.Name = GetCurrentParam( ref nodeParams );
				field.DataType = ( WirePortDataType )Enum.Parse( typeof( WirePortDataType ), GetCurrentParam( ref nodeParams ) );
				m_definition.Fields.Add( field );
			}

			int portedCount = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_inputExposed.Clear();
			m_outputExposed.Clear();
			m_portFieldIds.Clear();

			while ( m_inputPorts.Count > 1 ) { DeleteInputPortByArrayIdx( m_inputPorts.Count - 1 ); }
			while ( m_outputPorts.Count > 1 ) { RemoveOutputPort( m_outputPorts.Count - 1 ); }

			for ( int i = 0; i < portedCount; i++ )
			{
				string name = GetCurrentParam( ref nodeParams );
				WirePortDataType type = ( WirePortDataType )Enum.Parse( typeof( WirePortDataType ), GetCurrentParam( ref nodeParams ) );
				m_inputExposed.Add( Convert.ToBoolean( GetCurrentParam( ref nodeParams ) ) );
				m_outputExposed.Add( Convert.ToBoolean( GetCurrentParam( ref nodeParams ) ) );

				AddInputPort( type, false, name );
				AddOutputPort( type, name );
				m_inputPorts[ m_inputPorts.Count - 1 ].Visible = m_inputExposed[ i ];
				m_outputPorts[ m_outputPorts.Count - 1 ].Visible = m_outputExposed[ i ];
			}

			m_dataInputExposed = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			m_dataOutputExposed = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			m_exposedForTypeName = GetCurrentParam( ref nodeParams );

			// @diogo: the loader reads the port data from this same array next, so "params left" cannot
			// tell the tail apart - only the marker can
			if ( m_currentReadParamIdx < nodeParams.Length && nodeParams[ m_currentReadParamIdx ] == FieldIdsMarker )
			{
				m_currentReadParamIdx++;
				for ( int i = 0; i < fieldCount; i++ )
				{
					m_definition.Fields[ i ].Id = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				}

				for ( int i = 0; i < portedCount; i++ )
				{
					m_portFieldIds.Add( Convert.ToInt32( GetCurrentParam( ref nodeParams ) ) );
				}
			}

			m_definition.EnsureFieldIds();

			m_inputPorts[ 0 ].Visible = m_dataInputExposed;
			m_outputPorts[ 0 ].Visible = m_dataOutputExposed;

			m_portedDefinition = null;
			m_portedTypeName = string.Empty;
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			RefreshRegistration();

			// @diogo: a paste without wires never reaches ReconnectClipboardReferences
			ResolvePastedTypeName( null );
		}
	}
}

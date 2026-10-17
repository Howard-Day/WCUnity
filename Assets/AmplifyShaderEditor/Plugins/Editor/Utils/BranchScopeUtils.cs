// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	public class BranchArmAnalysis
	{
		// @diogo: final verdict: generate this arm inside braces
		public bool Scoped;
		// @diogo: nodes (+ output port unique id) to pre-generate before the conditional
		public List<BranchFrontierEntry> Frontier = new List<BranchFrontierEntry>();
		// @diogo: derivative-dependent code found in the would-be-scoped set
		public bool Hazard;
	}

	public struct BranchFrontierEntry
	{
		public ParentNode Node;
		public int OutputPortId;
		// Function Node whose outside graph holds Node, or null when Node lives in the branch node's own graph
		public FunctionNode Portal;
	}

	[NoAutoStaticsCleanup]
	public static class BranchScopeUtils
	{
		// @diogo: exact-string-with-paren matching keeps the explicit-LOD/grad variants from tripping
		private static readonly string[] s_derivativeCalls =
		{
			"ddx",
			"ddy",
			"fwidth",
			"tex2D(",
			"tex2Dproj(",
			"tex2Dbias(",
			"tex3D(",
			"tex3Dbias(",
			"texCUBE(",
			"texCUBEbias(",
			"tex2DArray(",
			"SAMPLE_TEXTURE2D(",
			"SAMPLE_TEXTURE2D_X(",
			"SAMPLE_TEXTURE2D_BIAS(",
			"SAMPLE_TEXTURE3D(",
			"SAMPLE_TEXTURE3D_BIAS(",
			"SAMPLE_TEXTURECUBE(",
			"SAMPLE_TEXTURECUBE_BIAS(",
			"SAMPLE_TEXTURE2D_ARRAY(",
			"SAMPLE_TEXTURE2D_ARRAY_BIAS(",
			"SAMPLE_DEPTH_TEXTURE(",
			"UNITY_SAMPLE_TEX2D(",
			"UNITY_SAMPLE_TEX2D_SAMPLER(",
			"UNITY_SAMPLE_TEX3D(",
			"UNITY_SAMPLE_TEXCUBE(",
			"UNITY_SAMPLE_TEX2DARRAY(",
			"UNITY_SAMPLE_TEX2DARRAY_BIAS(",
			"UNITY_SAMPLE_SCREENSPACE_TEXTURE(",
			".Sample(",
			".SampleBias("
		};

		// @diogo: families whose value is either a draw call constant or a pure function of their own input
		// ports; a category left out only costs an optimization, so anything unlisted counts as varying (#365)
		private static readonly HashSet<string> s_uniformCategories = new HashSet<string>
		{
			"Constants And Properties",
			"Logical Operators",
			"Math Operators",
			"Matrix Operators",
			"Matrix Transform",
			"Object",
			"Object Transform",
			"Time",
			"Trigonometry Operators",
			"Vector Operators"
		};

		public static bool Enabled { get { return Preferences.User.OptimizedBranching; } }

		public static BranchArmAnalysis AnalyzeArm( ParentNode branchNode, InputPort armPort, ref MasterNodeDataCollector dataCollector, bool divergentFlow = true )
		{
			BranchArmAnalysis analysis = new BranchArmAnalysis();

			// @diogo: the preference gates the optimization only - a preprocessor branch is scoped for
			// correctness and must not be revertible by a switch meant for bisecting runtime branches (#674)

			// @diogo: linked ports indirect into another master node's port, out of this graph's reach
			if ( ( divergentFlow && !Enabled ) || !armPort.IsConnected || armPort.ExternalLink != null )
			{
				return analysis;
			}

			HashSet<ParentNode> exclusive = AnalyzeScope( branchNode, armPort, dataCollector, null, new Dictionary<ParentGraph, HashSet<ParentNode>>(), analysis.Frontier );
			if ( exclusive.Count == 0 )
			{
				return analysis;
			}

			// @diogo: a uniform condition keeps the whole quad on one path, so implicit derivatives stay
			// defined inside the branch; it does not excuse nodes that mutate shader-wide state (#365)

			// @diogo: a preprocessor branch is resolved before compilation, so nothing inside it ever sits
			// in divergent flow and no hazard can apply (#674)
			analysis.Hazard = divergentFlow && dataCollector.IsFragmentCategory &&
				( ContainsUnscopable( exclusive ) || ( ContainsHazard( exclusive ) && !ConditionIsUniform( branchNode ) ) );
			analysis.Scoped = !analysis.Hazard;
			return analysis;
		}

		// @diogo: Switch branches on a keyword and Toggle on its own material property, so both are uniform
		// whatever feeds their arms; the remaining ones are only uniform if their condition is (#365)
		public static bool ConditionIsUniform( ParentNode branchNode )
		{
			bool result = false;
			if ( branchNode is StaticSwitch || branchNode is ToggleSwitchNode )
			{
				result = true;
			}
			else if ( branchNode is Compare || branchNode is ConditionalIfNode )
			{
				result = ConditionPortIsUniform( branchNode.InputPorts[ 0 ] ) && ConditionPortIsUniform( branchNode.InputPorts[ 1 ] );
			}
			else if ( branchNode is SwitchByIndexNode )
			{
				// @diogo: port 0 is the index, everything past it is an arm
				result = ConditionPortIsUniform( branchNode.InputPorts[ 0 ] );
			}
			return result;
		}

		private static bool ConditionPortIsUniform( InputPort port )
		{
			bool result = false;
			// @diogo: a linked port indirects into another master node's port, out of this graph's reach
			if ( port.ExternalLink == null )
			{
				// @diogo: an unconnected port holds an inline constant
				result = !port.IsConnected || UpstreamIsUniform( port.GetOutputNode(), new HashSet<ParentNode>() );
			}
			return result;
		}

		private static bool UpstreamIsUniform( ParentNode node, HashSet<ParentNode> visited )
		{
			bool result = false;
			if ( node != null )
			{
				if ( !visited.Add( node ) )
				{
					// @diogo: the walk unwinds on the first rejection, so a node reached twice was accepted
					result = true;
				}
				else if ( IsUniformNode( node ) )
				{
					result = true;
					if ( node is GetLocalVarNode getLocalVarNode )
					{
						RegisterLocalVarNode source = getLocalVarNode.CurrentSelected;
						result = source != null && UpstreamIsUniform( source, visited );
					}
					else
					{
						List<InputPort> inputPorts = node.InputPorts;
						for ( int p = 0; p < inputPorts.Count && result; p++ )
						{
							if ( inputPorts[ p ].IsConnected )
							{
								result = UpstreamIsUniform( inputPorts[ p ].GetOutputNode(), visited );
							}
						}
					}
				}
			}
			return result;
		}

		private static bool IsUniformNode( ParentNode node )
		{
			bool result = false;
			// @diogo: these read a screen space derivative or the interpolated tangent frame despite sitting
			// in an otherwise uniform category
			if ( !( node is DdxOpNode || node is DdyOpNode || node is FWidthOpNode || node is WorldToTangentMatrix ||
				node is TransformPositionNode || node is TransformDirectionNode || node is TransformVectorOpNode ) )
			{
				// @diogo: relays and local vars are Miscellaneous, they only forward what feeds them
				result = node is RelayNode || node is WireNode || node is RegisterLocalVarNode || node is GetLocalVarNode ||
					( node.Attributes != null && s_uniformCategories.Contains( node.Attributes.Category ) );
			}
			return result;
		}

		public static void PreGenerateFrontier( BranchArmAnalysis arm, ref MasterNodeDataCollector dataCollector )
		{
			// @diogo: a frontier node that inlines emits no line to hoist and would regenerate inside the braces (#365)
			dataCollector.PushHazardDeclarations();

			for ( int i = 0; i < arm.Frontier.Count; i++ )
			{
				BranchFrontierEntry entry = arm.Frontier[ i ];
				ParentGraph cachedGraph = null;
				if ( entry.Portal != null )
				{
					cachedGraph = entry.Portal.ContainerGraph.ParentWindow.CustomGraph;
					entry.Portal.ContainerGraph.ParentWindow.CustomGraph = entry.Portal.OutsideGraph;
				}

				OutputPort outputPort = entry.Node.GetOutputPortByUniqueId( entry.OutputPortId );
				if ( outputPort == null || !outputPort.IsLocalValue( dataCollector.PortCategory ) )
				{
					entry.Node.GenerateShaderForOutput( entry.OutputPortId, ref dataCollector, false );
				}

				if ( entry.Portal != null )
				{
					entry.Portal.ContainerGraph.ParentWindow.CustomGraph = cachedGraph;
				}
			}

			dataCollector.PopHazardDeclarations();
		}

		// @diogo: braces every arm of a preprocessor conditional, scoped or not: without them a line emitted
		// inside one arm leaks into the enclosing scope of the variant that takes it, and a re-emission
		// outside the branch then redeclares it. Callers hoist the assigned local above the conditional (#674)

		// @diogo: condition must hold whenever the arm is taken, "!" prefix for a negation; null claims
		// nothing, which is always safe (#443)
		public static void EmitBracedArm( int nodeId, BranchArmAnalysis arm, InputPort armPort, string varName, string armCode, string condition, ref MasterNodeDataCollector dataCollector )
		{
			string value = armCode;
			dataCollector.AddLocalVariable( nodeId, "{", true );
			if ( arm.Scoped )
			{
				dataCollector.PushBranchScope( uniformCondition: true, condition );
				value = armPort.GeneratePortInstructions( ref dataCollector );
			}
			// @diogo: an open scope already indents everything it emits, so the literal tab is only
			// needed for the arms that stay flat
			dataCollector.AddLocalVariable( nodeId, ( arm.Scoped ? string.Empty : "\t" ) + varName + " = " + value + ";", true );
			if ( arm.Scoped )
			{
				dataCollector.PopBranchScope();
			}
			dataCollector.AddLocalVariable( nodeId, "}", true );
		}

		// @diogo: flat pre-generation of a hazard-refused arm, so its derivative work lands above the conditional (#365)
		public static string PreGenerateFallback( BranchArmAnalysis arm, InputPort armPort, ref MasterNodeDataCollector dataCollector )
		{
			BeginFallback( arm, ref dataCollector );
			string result = armPort.GeneratePortInstructions( ref dataCollector );
			EndFallback( arm, ref dataCollector );
			return result;
		}

		public static string PreGenerateFallback( BranchArmAnalysis arm, InputPort armPort, ref MasterNodeDataCollector dataCollector, WirePortDataType inputPortType, bool ignoreLocalVar, bool autoCast )
		{
			BeginFallback( arm, ref dataCollector );
			string result = armPort.GenerateShaderForOutput( ref dataCollector, inputPortType, ignoreLocalVar, autoCast );
			EndFallback( arm, ref dataCollector );
			return result;
		}

		private static void BeginFallback( BranchArmAnalysis arm, ref MasterNodeDataCollector dataCollector )
		{
			if ( arm.Hazard )
			{
				dataCollector.PushHazardDeclarations();
			}
		}

		private static void EndFallback( BranchArmAnalysis arm, ref MasterNodeDataCollector dataCollector )
		{
			if ( arm.Hazard )
			{
				dataCollector.PopHazardDeclarations();
			}
		}

		// @diogo: closure, exclusivity and frontier of one arm port in the graph that is current, then the
		// crossing into the parent graph; one pipeline for the arm itself and every Function Input it crosses
		private static HashSet<ParentNode> AnalyzeScope( ParentNode branchNode, InputPort armPort, MasterNodeDataCollector dataCollector, FunctionNode portal, Dictionary<ParentGraph, HashSet<ParentNode>> referenceSources, List<BranchFrontierEntry> frontier )
		{
			HashSet<ParentNode> exclusive = new HashSet<ParentNode>();
			ParentNode entryNode = armPort.GetOutputNode();
			if ( entryNode != null )
			{
				int entryPortId = armPort.ExternalReferences[ 0 ].PortId;
				List<ParentNode> closure = BuildClosure( entryNode, entryPortId, dataCollector );
				if ( closure.Count > 0 )
				{
					exclusive = BuildExclusiveSet( closure, branchNode, armPort, referenceSources, dataCollector );
					if ( exclusive.Count > 0 )
					{
						CollectFrontier( branchNode.ContainerGraph, closure, exclusive, entryNode, entryPortId, portal, frontier, dataCollector );
						CrossFunctionInputs( closure, exclusive, dataCollector, referenceSources, frontier );
					}
				}
			}
			return exclusive;
		}

		// @diogo: upstream closure of the arm, in discovery order; sets are keyed by node object since
		// unique ids collide across shader function graphs
		private static List<ParentNode> BuildClosure( ParentNode entryNode, int entryPortId, MasterNodeDataCollector dataCollector )
		{
			List<ParentNode> closure = new List<ParentNode>();
			HashSet<ParentNode> visited = new HashSet<ParentNode>();

			TryCollect( entryNode, entryPortId, dataCollector, closure, visited );

			for ( int i = 0; i < closure.Count; i++ )
			{
				ParentNode node = closure[ i ];
				if ( node is GetLocalVarNode getLocalVarNode )
				{
					RegisterLocalVarNode source = getLocalVarNode.CurrentSelected;
					if ( source != null )
					{
						TryCollect( source, source.OutputPorts[ 0 ].PortId, dataCollector, closure, visited );
					}
				}
				else if ( GetCrossingInput( node ) == null )
				{
					// @diogo: a function node is an atomic unit here, only its outer ports are walked
					List<InputPort> inputPorts = node.InputPorts;
					for ( int p = 0; p < inputPorts.Count; p++ )
					{
						if ( inputPorts[ p ].IsConnected && !IsDeadEdge( node, inputPorts[ p ].PortId, dataCollector ) && !IsVertexSideInput( node, inputPorts[ p ].PortId, dataCollector ) )
						{
							TryCollect( inputPorts[ p ].GetOutputNode(), inputPorts[ p ].ExternalReferences[ 0 ].PortId, dataCollector, closure, visited );
						}
					}
				}
			}
			return closure;
		}

		private static void TryCollect( ParentNode node, int outputPortId, MasterNodeDataCollector dataCollector, List<ParentNode> closure, HashSet<ParentNode> visited )
		{
			if ( node != null )
			{
				OutputPort outputPort = node.GetOutputPortByUniqueId( outputPortId );
				// @diogo: already emitted at a visible location, so neither it nor its upstream belong to the arm
				if ( outputPort == null || !outputPort.IsLocalValue( dataCollector.PortCategory ) )
				{
					if ( visited.Add( node ) )
					{
						closure.Add( node );
					}
				}
			}
		}

		private static HashSet<ParentNode> BuildExclusiveSet( List<ParentNode> closure, ParentNode branchNode, InputPort armPort, Dictionary<ParentGraph, HashSet<ParentNode>> referenceSourcesByGraph, MasterNodeDataCollector dataCollector )
		{
			HashSet<ParentNode> exclusive = new HashSet<ParentNode>( closure );
			Dictionary<ParentNode, bool> liveness = new Dictionary<ParentNode, bool>();

			// @diogo: one whole-graph scan per graph per arm, however many Function Inputs cross into it
			ParentGraph graph = branchNode.ContainerGraph;
			if ( !referenceSourcesByGraph.TryGetValue( graph, out HashSet<ParentNode> referenceSources ) )
			{
				referenceSources = CollectReferenceSources( graph );
				referenceSourcesByGraph.Add( graph, referenceSources );
			}

			bool changed = true;
			while ( changed )
			{
				changed = false;
				for ( int i = 0; i < closure.Count; i++ )
				{
					ParentNode node = closure[ i ];
					if ( exclusive.Contains( node ) && !StaysExclusive( node, exclusive, referenceSources, branchNode, armPort, liveness, dataCollector ) )
					{
						exclusive.Remove( node );
						changed = true;
					}
				}
			}
			return exclusive;
		}

		private static bool StaysExclusive( ParentNode node, HashSet<ParentNode> exclusive, HashSet<ParentNode> referenceSources, ParentNode branchNode, InputPort armPort, Dictionary<ParentNode, bool> liveness, MasterNodeDataCollector dataCollector )
		{
			bool result = true;

			if ( GetSwitchReference( node ) != null || referenceSources.Contains( node ) || SpansBothStages( node ) )
			{
				result = false;
			}
			else
			{
				// @diogo: wire ids index the graph the branch node lives in, not the window's ambient one
				ParentGraph graph = branchNode.ContainerGraph;
				List<OutputPort> outputPorts = node.OutputPorts;
				for ( int p = 0; p < outputPorts.Count && result; p++ )
				{
					List<WireReference> connections = outputPorts[ p ].ExternalReferences;
					for ( int c = 0; c < connections.Count && result; c++ )
					{
						result = !ConsumerEscapes( graph.GetNode( connections[ c ].NodeId ), connections[ c ].PortId, exclusive, branchNode, armPort, liveness, dataCollector );
					}
				}

				// @diogo: the Register -> Get link is not a wire, its readers are consumers all the same
				if ( result && node is RegisterLocalVarNode registerLocalVarNode )
				{
					List<GetLocalVarNode> readers = registerLocalVarNode.NodeReferences;
					for ( int r = 0; r < readers.Count && result; r++ )
					{
						result = !ConsumerEscapes( readers[ r ], -1, exclusive, branchNode, armPort, liveness, dataCollector );
					}
				}
			}

			return result;
		}

		private static bool ConsumerEscapes( ParentNode consumer, int consumerPortId, HashSet<ParentNode> exclusive, ParentNode branchNode, InputPort armPort, Dictionary<ParentNode, bool> liveness, MasterNodeDataCollector dataCollector )
		{
			bool result = false;
			// @diogo: the arm's own entry edge is the one consumer that lives inside the scope
			if ( consumer != null && !( consumer == branchNode && consumerPortId == armPort.PortId ) )
			{
				if ( consumer is OutputNode )
				{
					result = OutputPortIsLive( consumer, consumerPortId );
				}
				else if ( ReachesLiveCode( consumer, consumerPortId, branchNode.ContainerGraph, liveness, dataCollector ) )
				{
					result = !exclusive.Contains( consumer );
				}
			}
			return result;
		}

		// @diogo: master ports and shader function outputs are outside by definition, and never carry
		// category bits; a locked one keeps its wire but contributes nothing to this compile
		private static bool OutputPortIsLive( ParentNode outputNode, int portId )
		{
			InputPort port = outputNode.GetInputPortByUniqueId( portId );
			return port == null || port.IsConnected;
		}

		// @diogo: a consumer the pre-pass never categorized still counts as live when its downstream reaches
		// categorized code or a connected output, and as dead only when every path dangles
		private static bool ReachesLiveCode( ParentNode node, int portId, ParentGraph graph, Dictionary<ParentNode, bool> liveness, MasterNodeDataCollector dataCollector )
		{
			bool result;
			if ( node is OutputNode )
			{
				result = OutputPortIsLive( node, portId );
			}
			else if ( IsDeadEdge( node, portId, dataCollector ) )
			{
				result = false;
			}
			else if ( node.Category != 0 )
			{
				result = true;
			}
			else if ( !liveness.TryGetValue( node, out result ) )
			{
				// @diogo: seeded dead so a cycle back into this node reads as nothing new until the walk completes
				liveness.Add( node, false );

				List<OutputPort> outputPorts = node.OutputPorts;
				for ( int p = 0; p < outputPorts.Count && !result; p++ )
				{
					List<WireReference> connections = outputPorts[ p ].ExternalReferences;
					for ( int c = 0; c < connections.Count && !result; c++ )
					{
						ParentNode consumer = graph.GetNode( connections[ c ].NodeId );
						result = consumer != null && ReachesLiveCode( consumer, connections[ c ].PortId, graph, liveness, dataCollector );
					}
				}

				if ( !result && node is RegisterLocalVarNode registerLocalVarNode )
				{
					List<GetLocalVarNode> readers = registerLocalVarNode.NodeReferences;
					for ( int r = 0; r < readers.Count && !result; r++ )
					{
						result = readers[ r ] != null && ReachesLiveCode( readers[ r ], -1, graph, liveness, dataCollector );
					}
				}

				liveness[ node ] = result;
			}
			return result;
		}

		// @diogo: a wire into an input that will not generate in this pass: a Function Switch option that is not
		// selected, or a Struct node field the pre-pass did not ask for
		private static bool IsDeadEdge( ParentNode consumer, int portId, MasterNodeDataCollector dataCollector )
		{
			return IsUnselectedOption( consumer, portId ) || ( consumer is StructNode structNode && !structNode.IsInputLive( portId, dataCollector ) );
		}

		// @diogo: a fragment node that generates this input vertex-side (GenerateInputInVertex) takes it out of the
		// arm's code; walked, a chain also used elsewhere would be pre-generated in fragment above the branch (#443)
		private static bool IsVertexSideInput( ParentNode node, int portId, MasterNodeDataCollector dataCollector )
		{
			bool result = false;
			if ( dataCollector.IsFragmentCategory )
			{
				if ( node is VertexToFragmentNode )
				{
					result = portId == 0;
				}
				else if ( node is DepthFade )
				{
					result = portId == 1 && ( dataCollector.IsTemplate || !dataCollector.TesselationActive );
				}
				else if ( node is CameraDepthFade )
				{
					result = portId == 2;
				}
				else if ( node is SurfaceDepthNode )
				{
					result = portId == 0 && dataCollector.IsTemplate;
				}
			}
			return result;
		}

		// @diogo: a Function Switch only ever generates its selected option, so a wire into any other input is dead
		private static bool IsUnselectedOption( ParentNode consumer, int portId )
		{
			return consumer is FunctionSwitch functionSwitch && consumer.GetInputPortByUniqueId( portId ) != consumer.InputPorts[ functionSwitch.GetCurrentSelectedInput() ];
		}

		private static void CollectFrontier( ParentGraph graph, List<ParentNode> closure, HashSet<ParentNode> exclusive, ParentNode entryNode, int entryPortId, FunctionNode portal, List<BranchFrontierEntry> frontier, MasterNodeDataCollector dataCollector )
		{
			for ( int i = 0; i < closure.Count; i++ )
			{
				ParentNode node = closure[ i ];
				if ( !exclusive.Contains( node ) )
				{
					if ( node == entryNode )
					{
						AddFrontier( frontier, node, entryPortId, portal );
					}

					List<OutputPort> outputPorts = node.OutputPorts;
					for ( int p = 0; p < outputPorts.Count; p++ )
					{
						if ( FeedsExclusive( graph, outputPorts[ p ], exclusive, dataCollector ) )
						{
							AddFrontier( frontier, node, outputPorts[ p ].PortId, portal );
						}
					}

					// @diogo: a Register read from inside the scope must land above it, and it has no wire consumer to say so
					if ( node is RegisterLocalVarNode registerLocalVarNode && HasExclusiveReader( registerLocalVarNode, exclusive ) )
					{
						AddFrontier( frontier, node, node.OutputPorts[ 0 ].PortId, portal );
					}
				}
			}
		}

		// @diogo: an exclusive instanced Function Input generates its parent-graph chain inside the arm, so the
		// analysis follows it out; otherwise a chain the parent graph shares elsewhere is scoped unseen (#365)
		private static void CrossFunctionInputs( List<ParentNode> closure, HashSet<ParentNode> exclusive, MasterNodeDataCollector dataCollector, Dictionary<ParentGraph, HashSet<ParentNode>> referenceSources, List<BranchFrontierEntry> frontier )
		{
			for ( int i = 0; i < closure.Count; i++ )
			{
				FunctionInput functionInput = GetCrossingInput( closure[ i ] );
				if ( functionInput != null && exclusive.Contains( functionInput ) )
				{
					FunctionNode functionNode = functionInput.Fnode;
					InputPort outerPort = functionNode.GetInput( functionInput );
					if ( outerPort.ExternalLink != null )
					{
						// @diogo: a linked port indirects into another master node's port, out of reach of the
						// analysis, so the input is hoisted like any chain that cannot be proven exclusive
						exclusive.Remove( functionInput );
						AddFrontier( frontier, functionInput, functionInput.OutputPorts[ 0 ].PortId, null );
					}
					else if ( outerPort.IsConnected )
					{
						ParentGraph cachedGraph = functionNode.ContainerGraph.ParentWindow.CustomGraph;
						functionNode.ContainerGraph.ParentWindow.CustomGraph = functionNode.OutsideGraph;
						HashSet<ParentNode> outerExclusive = AnalyzeScope( functionNode, outerPort, dataCollector, functionNode, referenceSources, frontier );
						functionNode.ContainerGraph.ParentWindow.CustomGraph = cachedGraph;
						exclusive.UnionWith( outerExclusive );
					}
				}
			}
		}

		// The Function Input when it generates through its Function Node's outer port, so its chain lives in
		// the parent graph; null for any other node, or an input that takes its own default wire
		private static FunctionInput GetCrossingInput( ParentNode node )
		{
			FunctionInput result = null;
			if ( node is FunctionInput functionInput && functionInput.Fnode != null && functionInput.Fnode.UsesOuterPort( functionInput ) )
			{
				result = functionInput;
			}
			return result;
		}

		private static bool FeedsExclusive( ParentGraph graph, OutputPort outputPort, HashSet<ParentNode> exclusive, MasterNodeDataCollector dataCollector )
		{
			bool result = false;
			List<WireReference> connections = outputPort.ExternalReferences;
			for ( int c = 0; c < connections.Count && !result; c++ )
			{
				ParentNode consumer = graph.GetNode( connections[ c ].NodeId );
				result = consumer != null && exclusive.Contains( consumer ) && !IsDeadEdge( consumer, connections[ c ].PortId, dataCollector );
			}
			return result;
		}

		private static bool HasExclusiveReader( RegisterLocalVarNode registerLocalVarNode, HashSet<ParentNode> exclusive )
		{
			bool result = false;
			List<GetLocalVarNode> readers = registerLocalVarNode.NodeReferences;
			for ( int r = 0; r < readers.Count && !result; r++ )
			{
				result = readers[ r ] != null && exclusive.Contains( readers[ r ] );
			}
			return result;
		}

		private static void AddFrontier( List<BranchFrontierEntry> frontier, ParentNode node, int outputPortId, FunctionNode portal )
		{
			for ( int i = 0; i < frontier.Count; i++ )
			{
				if ( frontier[ i ].Node == node && frontier[ i ].OutputPortId == outputPortId )
				{
					return;
				}
			}
			frontier.Add( new BranchFrontierEntry { Node = node, OutputPortId = outputPortId, Portal = portal } );
		}

		private static HashSet<ParentNode> CollectReferenceSources( ParentGraph graph )
		{
			HashSet<ParentNode> sources = new HashSet<ParentNode>();
			List<ParentNode> nodes = graph.AllNodes;
			for ( int i = 0; i < nodes.Count; i++ )
			{
				ParentNode source = GetSwitchReference( nodes[ i ] );
				if ( source != null )
				{
					sources.Add( source );
				}
			}
			return sources;
		}

		private static ParentNode GetSwitchReference( ParentNode node )
		{
			ParentNode result = null;
			if ( node is StaticSwitch staticSwitch )
			{
				result = staticSwitch.Reference;
			}
			else if ( node is ToggleSwitchNode toggleSwitchNode )
			{
				result = toggleSwitchNode.Reference;
			}
			return result;
		}

		private static bool SpansBothStages( ParentNode node )
		{
			int category = node.Category;
			bool vertexSide = UIUtils.GetCategoryInBitArray( category, MasterNodePortCategory.Vertex ) || UIUtils.GetCategoryInBitArray( category, MasterNodePortCategory.Tessellation );
			bool fragmentSide = UIUtils.GetCategoryInBitArray( category, MasterNodePortCategory.Fragment ) || UIUtils.GetCategoryInBitArray( category, MasterNodePortCategory.Debug );
			return vertexSide && fragmentSide;
		}

		// @diogo: POM writes ASE_CHANGES_WORLD_POS through the non-scoped defines sink, so scoping it would
		// leave the define unconditional while the position write is not - wrong render, no compile error (#365)
		private static bool ContainsUnscopable( IEnumerable<ParentNode> nodes )
		{
			bool result = false;
			foreach ( ParentNode node in nodes )
			{
				if ( node is ParallaxOcclusionMappingNode )
				{
					result = true;
					break;
				}

				if ( node is FunctionNode functionNode && functionNode.FunctionGraph != null && ContainsUnscopable( functionNode.FunctionGraph.AllNodes ) )
				{
					result = true;
					break;
				}
			}
			return result;
		}

		private static bool ContainsHazard( IEnumerable<ParentNode> nodes )
		{
			bool result = false;
			foreach ( ParentNode node in nodes )
			{
				if ( IsDerivativeDependent( node ) )
				{
					result = true;
					break;
				}

				if ( node is FunctionNode functionNode && functionNode.FunctionGraph != null && ContainsHazard( functionNode.FunctionGraph.AllNodes ) )
				{
					result = true;
					break;
				}
			}
			return result;
		}

		public static bool IsDerivativeDependent( ParentNode node )
		{
			bool result = false;
			if ( node is SamplerNode samplerNode )
			{
				result = samplerNode.RequiresImplicitDerivatives( samplerNode.MipMode );
			}
			else if ( node is TriplanarNode || node is ParallaxOcclusionMappingNode || node is TextureArrayNode )
			{
				result = true;
			}
			else if ( node is ScreenColorNode || node is ScreenDepthNode || node is DepthFade )
			{
				// @diogo: their scene color/depth fetches sample with implicit mips outside HDRP
				result = true;
			}
			else if ( node is DdxOpNode || node is DdyOpNode || node is FWidthOpNode )
			{
				result = true;
			}
			else if ( node is CustomExpressionNode customExpressionNode )
			{
				result = ContainsDerivativeCall( customExpressionNode.Code );
			}
			return result;
		}

		private static bool ContainsDerivativeCall( string code )
		{
			bool result = false;
			if ( !string.IsNullOrEmpty( code ) )
			{
				for ( int i = 0; i < s_derivativeCalls.Length && !result; i++ )
				{
					result = code.IndexOf( s_derivativeCalls[ i ], StringComparison.Ordinal ) >= 0;
				}
			}
			return result;
		}
	}
}

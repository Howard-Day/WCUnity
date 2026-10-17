// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	// Parses and diffs two GenerateGraphSnapshot() meta strings ( current live graph vs undo/redo
	// target ) into a node/wire classification, without applying anything. Stage 1 runs this in
	// shadow mode ( parse + diff + log only ); the same parser/differ becomes Stage 2's patch input.
	[NoAutoStaticsCleanup]
	public static class UndoSnapshotDiff
	{
		// Result of diffing two graph snapshots ( see Diff ). Stage 1 ( shadow mode ) only classifies
		// and logs this; Stage 2 will drive the live patch from it.
		public sealed class SnapshotDiffResult
		{
			public bool Bailed;            // true when the diff cannot be trusted for patching
			public string BailReason;      // null when !Bailed
			public int UnchangedCount;
			public List<int> ChangedIds;   // same id, different line
			public List<int> PositionOnlyIds; // @diogo: same id, line differs only in the position field - patched in place, exempt from recreate policy
			public List<int> AddedIds;     // in target only
			public List<int> RemovedIds;   // in current only
			public int WireAdded;          // wire lines in target only
			public int WireRemoved;        // wire lines in current only
			public bool MasterNodeTouched; // any changed/added/removed id is a master-node line
			public bool FunctionNodeRecreated;        // @diogo: any changed/added line's type field is FunctionNode
			public string Version;                    // @diogo: the ( equal ) version field of both snapshots
			public Dictionary<int, string> TargetNodeLines;
			public Dictionary<int, string> CurrentNodeLines; // @diogo: for verify-failure line-pair diagnostics
			public List<int> TargetNodeOrder;         // @diogo: node ids in target line order, the patcher creates in this order
			public List<string> WiresToDisconnect;    // @diogo: current-only wires whose both endpoints are kept nodes, in current line order
			public List<string> WiresToConnect;       // @diogo: target wires absent from current, or touching a changed/added node, in
			                                          // target line order - adaptive-port cascades make wire connect order load-bearing
			public int CurrentNodeCount;
			public int TargetNodeCount;
		}

		// @diogo: built once from the concrete MasterNode subclasses plus FunctionOutput ( the function-window master
		// equivalent, a MasterNode sibling ); type strings are "FullName, AssemblyName" ( see ParentNode.WriteToString )
		private static readonly HashSet<string> s_masterNodeTypes = new HashSet<string>
		{
			typeof( TemplateMasterNode ).FullName + ", " + typeof( TemplateMasterNode ).Assembly.GetName().Name,
			typeof( TemplateMultiPassMasterNode ).FullName + ", " + typeof( TemplateMultiPassMasterNode ).Assembly.GetName().Name,
			typeof( StandardSurfaceOutputNode ).FullName + ", " + typeof( StandardSurfaceOutputNode ).Assembly.GetName().Name,
			typeof( LogNode ).FullName + ", " + typeof( LogNode ).Assembly.GetName().Name,
			typeof( FunctionOutput ).FullName + ", " + typeof( FunctionOutput ).Assembly.GetName().Name
		};

		// @diogo: FunctionNode type string, same construction pattern as s_masterNodeTypes
		private static readonly string s_functionNodeType = typeof( FunctionNode ).FullName + ", " + typeof( FunctionNode ).Assembly.GetName().Name;

		// @diogo: structural markers of the JSON graph line format (see JsonGraphFormat); the cheap
		// extraction helpers below read kind/type/id/pos/endpoints straight off the raw line, since
		// the differ touches every line of both snapshots on every patch and a full JSON parse per
		// line dominated the diff phase on large graphs
		private const string NodeLinePrefix = "{\"type\":\"";
		private const string NodeIdMarker = "\",\"id\":";
		private const string NodePosMarker = ",\"pos\":[";
		private const string WireLinePrefix = "{\"wire\":[";

		// @diogo: split-instruction node line layout: [0]=NodeParam, [1]=type, [2]=id, [3]="x,y" position
		private const int NodePositionFieldIndex = 3;

		// @diogo: MasterNode.WriteToString appends its fields right after ParentNode's two, so the
		// session-ordered m_masterNodeCategory lands at a fixed split index for every MasterNode subclass
		private const int MasterNodeCategoryFieldIndex = 9;

		// @diogo: MasterNode subclasses only - their lines carry m_masterNodeCategory, an index into a
		// session-phase-dependent template list re-derived from the template GUID on load, so it must
		// never count as a state difference (FunctionOutput is an OutputNode - no category field)
		private static readonly HashSet<string> s_masterCategoryTypes = new HashSet<string>
		{
			typeof( TemplateMasterNode ).FullName + ", " + typeof( TemplateMasterNode ).Assembly.GetName().Name,
			typeof( TemplateMultiPassMasterNode ).FullName + ", " + typeof( TemplateMultiPassMasterNode ).Assembly.GetName().Name,
			typeof( StandardSurfaceOutputNode ).FullName + ", " + typeof( StandardSurfaceOutputNode ).Assembly.GetName().Name,
			typeof( LogNode ).FullName + ", " + typeof( LogNode ).Assembly.GetName().Name
		};

		private class ParsedSnapshot
		{
			public string Version;
			public Dictionary<int, string> NodeLines = new Dictionary<int, string>();
			public HashSet<string> WireLines = new HashSet<string>();
			public List<int> NodeOrder = new List<int>();
			public List<string> WireOrder = new List<string>(); // @diogo: wire lines in serialization order
		}

		public static SnapshotDiffResult Diff( string currentMeta, string targetMeta )
		{
			SnapshotDiffResult result = new SnapshotDiffResult
			{
				ChangedIds = new List<int>(),
				PositionOnlyIds = new List<int>(),
				AddedIds = new List<int>(),
				RemovedIds = new List<int>(),
				TargetNodeLines = new Dictionary<int, string>(),
				CurrentNodeLines = new Dictionary<int, string>(),
				TargetNodeOrder = new List<int>(),
				WiresToDisconnect = new List<string>(),
				WiresToConnect = new List<string>()
			};

			try
			{
				if ( string.IsNullOrEmpty( currentMeta ) )
				{
					return Bail( result, "no current snapshot" );
				}
				if ( string.IsNullOrEmpty( targetMeta ) )
				{
					return Bail( result, "no target snapshot" );
				}

				ParsedSnapshot current = ParseSnapshot( currentMeta, result );
				if ( result.Bailed )
				{
					return result;
				}

				ParsedSnapshot target = ParseSnapshot( targetMeta, result );
				if ( result.Bailed )
				{
					return result;
				}

				if ( !string.Equals( current.Version, target.Version, StringComparison.Ordinal ) )
				{
					return Bail( result, "version mismatch" );
				}

				result.Version = target.Version;
				result.CurrentNodeCount = current.NodeLines.Count;
				result.TargetNodeCount = target.NodeLines.Count;
				result.TargetNodeLines = target.NodeLines;
				result.CurrentNodeLines = current.NodeLines;
				result.TargetNodeOrder = target.NodeOrder;

				foreach ( KeyValuePair<int, string> currentEntry in current.NodeLines )
				{
					if ( !target.NodeLines.TryGetValue( currentEntry.Key, out string targetLine ) )
					{
						result.RemovedIds.Add( currentEntry.Key );
						if ( IsMasterLine( currentEntry.Value ) )
						{
							result.MasterNodeTouched = true;
						}
						continue;
					}

					if ( string.Equals( currentEntry.Value, targetLine, StringComparison.Ordinal ) )
					{
						result.UnchangedCount++;
						continue;
					}

					if ( !string.Equals( NodeTypeField( currentEntry.Value ), NodeTypeField( targetLine ), StringComparison.Ordinal ) )
					{
						return Bail( result, "type change on id " + currentEntry.Key );
					}

					// @diogo: field-wise refinement: a line differing only in position is patched in place
					// (no recreate, no policy flags); one differing only in the session-ordered category
					// field is not a change at all - the oracle masks it too (see NormalizeForComparison)
					NodeLineDelta lineDelta = ClassifyNodeLineDelta( currentEntry.Value, targetLine );
					if ( lineDelta == NodeLineDelta.Equivalent )
					{
						result.UnchangedCount++;
						continue;
					}
					if ( lineDelta == NodeLineDelta.PositionOnly )
					{
						result.PositionOnlyIds.Add( currentEntry.Key );
						continue;
					}

					result.ChangedIds.Add( currentEntry.Key );
					if ( IsMasterLine( currentEntry.Value ) || IsMasterLine( targetLine ) )
					{
						result.MasterNodeTouched = true;
					}
					if ( string.Equals( NodeTypeField( targetLine ), s_functionNodeType, StringComparison.Ordinal ) )
					{
						result.FunctionNodeRecreated = true;
					}
				}

				foreach ( KeyValuePair<int, string> targetEntry in target.NodeLines )
				{
					if ( !current.NodeLines.ContainsKey( targetEntry.Key ) )
					{
						result.AddedIds.Add( targetEntry.Key );
						if ( IsMasterLine( targetEntry.Value ) )
						{
							result.MasterNodeTouched = true;
						}
						if ( string.Equals( NodeTypeField( targetEntry.Value ), s_functionNodeType, StringComparison.Ordinal ) )
						{
							result.FunctionNodeRecreated = true;
						}
					}
				}

				HashSet<int> changedOrRemoved = new HashSet<int>( result.ChangedIds );
				changedOrRemoved.UnionWith( result.RemovedIds );
				HashSet<int> changedOrAdded = new HashSet<int>( result.ChangedIds );
				changedOrAdded.UnionWith( result.AddedIds );

				// @diogo: both plans keep serialization order - adaptive-port nodes ( append/break style ) cascade
				// type changes per connection, so the patcher must re-apply wires in the same order a load would
				foreach ( string wireLine in current.WireOrder )
				{
					if ( !target.WireLines.Contains( wireLine ) )
					{
						result.WireRemoved++;
						if ( !TryGetWireEndpoints( wireLine, out int inNodeId, out int outNodeId ) )
						{
							return Bail( result, "bad wire line" );
						}
						if ( !changedOrRemoved.Contains( inNodeId ) && !changedOrRemoved.Contains( outNodeId ) )
						{
							result.WiresToDisconnect.Add( wireLine );
						}
					}
				}

				foreach ( string wireLine in target.WireOrder )
				{
					bool inCurrent = current.WireLines.Contains( wireLine );
					if ( !inCurrent )
					{
						result.WireAdded++;
					}

					if ( !TryGetWireEndpoints( wireLine, out int inNodeId, out int outNodeId ) )
					{
						return Bail( result, "bad wire line" );
					}
					if ( !inCurrent || changedOrAdded.Contains( inNodeId ) || changedOrAdded.Contains( outNodeId ) )
					{
						result.WiresToConnect.Add( wireLine );
					}
				}

				return result;
			}
			catch ( Exception e )
			{
				return Bail( result, "exception: " + e.GetType().Name );
			}
		}

		private static SnapshotDiffResult Bail( SnapshotDiffResult result, string reason )
		{
			result.Bailed = true;
			result.BailReason = reason;
			return result;
		}

		// Line taxonomy ( per GenerateGraphSnapshot ): copyright header lines before ShaderBodyBegin,
		// the ShaderBodyBegin marker line, the Version line, node lines, wire lines, the ShaderBodyEnd
		// marker line, the trailing CHECKSUM line, and empty lines. Anything else bails.
		private static ParsedSnapshot ParseSnapshot( string meta, SnapshotDiffResult result )
		{
			ParsedSnapshot parsed = new ParsedSnapshot();
			string[] lines = meta.Replace( "\r", string.Empty ).Split( IOUtils.LINE_TERMINATOR );

			bool inBody = false;
			bool sawVersion = false;
			for ( int i = 0; i < lines.Length; i++ )
			{
				string line = lines[ i ];
				if ( line.Length == 0 )
				{
					continue;
				}

				if ( !inBody )
				{
					if ( line.StartsWith( IOUtils.ShaderBodyBegin, StringComparison.Ordinal ) )
					{
						inBody = true;
					}
					// @diogo: copyright header lines before ShaderBodyBegin — nothing to classify
					continue;
				}

				if ( !sawVersion )
				{
					sawVersion = true;
					string[] versionParams = line.Split( IOUtils.VALUE_SEPARATOR );
					if ( versionParams.Length != 2 )
					{
						Bail( result, "bad version line" );
						return parsed;
					}
					parsed.Version = versionParams[ 1 ];
					continue;
				}

				if ( line.StartsWith( IOUtils.ShaderBodyEnd, StringComparison.Ordinal ) )
				{
					continue;
				}

				if ( line.StartsWith( IOUtils.CHECKSUM, StringComparison.Ordinal ) )
				{
					continue;
				}

				// @diogo: classify off the raw JSON markers - only the kind and node id matter here
				if ( line.StartsWith( NodeLinePrefix, StringComparison.Ordinal ) )
				{
					if ( !TryGetNodeId( line, out int id ) )
					{
						Bail( result, "bad node id" );
						return parsed;
					}
					if ( parsed.NodeLines.ContainsKey( id ) )
					{
						Bail( result, "duplicate node id" );
						return parsed;
					}
					parsed.NodeLines[ id ] = line;
					parsed.NodeOrder.Add( id );
					continue;
				}

				if ( line.StartsWith( WireLinePrefix, StringComparison.Ordinal ) )
				{
					if ( parsed.WireLines.Add( line ) )
					{
						parsed.WireOrder.Add( line );
					}
					continue;
				}

				Bail( result, "unclassified line: " + Truncate( line ) );
				return parsed;
			}

			return parsed;
		}

		private static string NodeTypeField( string nodeLine )
		{
			// @diogo: cheap read: type names carry no JSON-escapable characters, so the raw substring
			// up to the next quote is the type string (same reasoning as NormalizeForComparison)
			if ( nodeLine.StartsWith( NodeLinePrefix, StringComparison.Ordinal ) )
			{
				int typeEnd = nodeLine.IndexOf( '"', NodeLinePrefix.Length );
				if ( typeEnd >= 0 )
				{
					return nodeLine.Substring( NodeLinePrefix.Length, typeEnd - NodeLinePrefix.Length );
				}
			}
			string[] parameters = JsonGraphFormat.SplitInstruction( nodeLine );
			return parameters[ IOUtils.NodeTypeId ];
		}

		// @diogo: cheap node id read: an escaped type field cannot contain a raw quote, so the first
		// id marker occurrence after it is structural; assumes the line starts with NodeLinePrefix
		private static bool TryGetNodeId( string nodeLine, out int id )
		{
			id = 0;
			int markerStart = nodeLine.IndexOf( NodeIdMarker, NodeLinePrefix.Length, StringComparison.Ordinal );
			if ( markerStart < 0 )
			{
				return false;
			}
			int idStart = markerStart + NodeIdMarker.Length;
			int idEnd = nodeLine.IndexOf( ',', idStart );
			return idEnd > idStart && int.TryParse( nodeLine.Substring( idStart, idEnd - idStart ), NumberStyles.Integer, CultureInfo.InvariantCulture, out id );
		}

		// @diogo: cheap position read for the in-place patch fast path; the first pos marker is always
		// the structural one (only the type and id fields precede it)
		public static bool TryGetNodePosition( string nodeLine, out Vector2 position )
		{
			position = Vector2.zero;
			int posStart = nodeLine.IndexOf( NodePosMarker, StringComparison.Ordinal );
			if ( posStart < 0 )
			{
				return false;
			}
			int xStart = posStart + NodePosMarker.Length;
			int xEnd = nodeLine.IndexOf( ',', xStart );
			int yEnd = xEnd < 0 ? -1 : nodeLine.IndexOf( ']', xEnd + 1 );
			if ( yEnd < 0 )
			{
				return false;
			}
			if ( !float.TryParse( nodeLine.Substring( xStart, xEnd - xStart ), NumberStyles.Float, CultureInfo.InvariantCulture, out float x ) ||
				!float.TryParse( nodeLine.Substring( xEnd + 1, yEnd - xEnd - 1 ), NumberStyles.Float, CultureInfo.InvariantCulture, out float y ) )
			{
				return false;
			}
			position = new Vector2( x, y );
			return true;
		}

		// @diogo: refinement classes for two same-id node lines that differ as raw strings
		private enum NodeLineDelta
		{
			Changed,
			PositionOnly,
			Equivalent
		}

		// @diogo: field-wise comparison of two node lines: position differences are patchable in place,
		// session-ordered category differences are ignored, anything else is a real change
		private static NodeLineDelta ClassifyNodeLineDelta( string currentLine, string targetLine )
		{
			// @diogo: cheap structural check first - bulk moves classify thousands of pairs, so avoid the
			// full JSON split when the lines are identical outside the "pos":[...] segment; the first
			// pos marker is always the structural one (only the type and id fields precede it)
			int posStart = currentLine.IndexOf( NodePosMarker, StringComparison.Ordinal );
			if ( posStart >= 0 && posStart == targetLine.IndexOf( NodePosMarker, StringComparison.Ordinal ) &&
				string.CompareOrdinal( currentLine, 0, targetLine, 0, posStart ) == 0 )
			{
				int currentParamsStart = currentLine.IndexOf( "],\"params\":[", posStart, StringComparison.Ordinal );
				int targetParamsStart = targetLine.IndexOf( "],\"params\":[", posStart, StringComparison.Ordinal );
				if ( currentParamsStart >= 0 && targetParamsStart >= 0 &&
					currentLine.Length - currentParamsStart == targetLine.Length - targetParamsStart &&
					string.CompareOrdinal( currentLine, currentParamsStart, targetLine, targetParamsStart, currentLine.Length - currentParamsStart ) == 0 )
				{
					// @diogo: the caller only classifies raw-differing lines, so the difference is inside pos
					return NodeLineDelta.PositionOnly;
				}
			}

			string[] currentParams = JsonGraphFormat.SplitInstruction( currentLine );
			string[] targetParams = JsonGraphFormat.SplitInstruction( targetLine );
			if ( currentParams.Length != targetParams.Length )
			{
				return NodeLineDelta.Changed;
			}

			bool hasCategoryField = HasSessionOrderedCategory( currentParams );
			NodeLineDelta delta = NodeLineDelta.Equivalent;
			for ( int i = 0; i < currentParams.Length; i++ )
			{
				if ( string.Equals( currentParams[ i ], targetParams[ i ], StringComparison.Ordinal ) )
				{
					continue;
				}
				if ( hasCategoryField && i == MasterNodeCategoryFieldIndex )
				{
					continue;
				}
				if ( i == NodePositionFieldIndex )
				{
					delta = NodeLineDelta.PositionOnly;
					continue;
				}
				return NodeLineDelta.Changed;
			}
			return delta;
		}

		private static bool HasSessionOrderedCategory( string[] parameters )
		{
			return parameters.Length > MasterNodeCategoryFieldIndex && s_masterCategoryTypes.Contains( parameters[ IOUtils.NodeTypeId ] );
		}

		// @diogo: comparison-only form of a snapshot line: master-node lines get the session-ordered
		// category field masked, everything else passes through unchanged. The result is never a valid
		// instruction - it only feeds checksums/equality, with both sides through the same transform.
		public static string NormalizeForComparison( string line )
		{
			// @diogo: cheap gate: node lines start with the JSON type field; read it without a full parse
			// (type names carry no JSON-escapable characters, so the raw substring is the type string)
			if ( string.IsNullOrEmpty( line ) || !line.StartsWith( NodeLinePrefix, StringComparison.Ordinal ) )
			{
				return line;
			}
			int typeEnd = line.IndexOf( '"', NodeLinePrefix.Length );
			if ( typeEnd < 0 || !s_masterCategoryTypes.Contains( line.Substring( NodeLinePrefix.Length, typeEnd - NodeLinePrefix.Length ) ) )
			{
				return line;
			}

			string[] parameters = JsonGraphFormat.SplitInstruction( line );
			if ( !HasSessionOrderedCategory( parameters ) )
			{
				return line;
			}
			parameters[ MasterNodeCategoryFieldIndex ] = "*";

			// @diogo: re-escape each field so the joined form stays injective (mirrors the JSON encoding)
			StringBuilder builder = new StringBuilder( line.Length );
			for ( int i = 0; i < parameters.Length; i++ )
			{
				builder.Append( '"' ).Append( JsonGraphFormat.EscapeString( parameters[ i ] ) ).Append( '"' );
			}
			return builder.ToString();
		}

		private static bool IsMasterLine( string nodeLine )
		{
			return s_masterNodeTypes.Contains( NodeTypeField( nodeLine ) );
		}

		// @diogo: wire endpoint ids from the fixed wire shape {"wire":[in,inPort,out,outPort]}, read
		// straight off the raw line - the diff extracts endpoints from every wire line of both snapshots
		private static bool TryGetWireEndpoints( string wireLine, out int inNodeId, out int outNodeId )
		{
			inNodeId = 0;
			outNodeId = 0;
			if ( !wireLine.StartsWith( WireLinePrefix, StringComparison.Ordinal ) )
			{
				return false;
			}
			int inEnd = wireLine.IndexOf( ',', WireLinePrefix.Length );
			int inPortEnd = inEnd < 0 ? -1 : wireLine.IndexOf( ',', inEnd + 1 );
			int outEnd = inPortEnd < 0 ? -1 : wireLine.IndexOf( ',', inPortEnd + 1 );
			if ( outEnd < 0 )
			{
				return false;
			}
			return int.TryParse( wireLine.Substring( WireLinePrefix.Length, inEnd - WireLinePrefix.Length ), NumberStyles.Integer, CultureInfo.InvariantCulture, out inNodeId ) &&
				int.TryParse( wireLine.Substring( inPortEnd + 1, outEnd - inPortEnd - 1 ), NumberStyles.Integer, CultureInfo.InvariantCulture, out outNodeId );
		}

		private static string Truncate( string line )
		{
			return line.Length <= 40 ? line : line.Substring( 0, 40 );
		}
	}
}

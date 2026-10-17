// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	// Session counters for the snapshot-based undo system, surfaced in the Preferences Developer
	// section. Timing and size sampling is always collected ( negligible next to the work it measures );
	// Preferences.User.UndoProfiling only gates the extra per-event console logging. All counters live
	// for the editor session and reset on domain reload.
	[NoAutoStaticsCleanup]
	public static class UndoProfiler
	{
		public static int SerializeCount { get; private set; }
		public static double LastSerializeMs { get; private set; }
		public static double TotalSerializeMs { get; private set; }
		public static int LastSnapshotChars { get; private set; }
		public static long TotalSerializedChars { get; private set; }
		public static double LastCompressMs { get; private set; }
		public static double TotalCompressMs { get; private set; }
		public static int LastSnapshotCompressedBytes { get; private set; }
		public static long TotalCompressedBytes { get; private set; }

		public static int RestoreCount { get; private set; }
		public static double LastRestoreMs { get; private set; }
		public static double TotalRestoreMs { get; private set; }
		public static double LastRestoreLoadMs { get; private set; }
		public static double LastRestoreRefreshMs { get; private set; }
		public static double LastRestoreViewStateMs { get; private set; }
		public static int LastRestoreNodeCount { get; private set; }

		public static int StepsRecorded { get; private set; }

		public static int ShadowDiffCount { get; private set; }
		public static int ShadowBailCount { get; private set; }
		public static string LastShadowSummary { get; private set; } = "-";

		public static int PatchAttemptCount { get; private set; }
		public static int PatchSuccessCount { get; private set; }
		public static int PatchBailCount { get; private set; }
		public static int PatchVerifyFailCount { get; private set; }
		public static string LastPatchSummary { get; private set; } = "-";

		// @diogo: dev-only one-shots: the next incremental patch reports a verify failure / throws mid-patch, then falls back
		public static bool InjectVerifyFailure { get; set; }
		public static bool InjectPatchException { get; set; }

		public static double AverageSerializeMs { get { return SerializeCount > 0 ? TotalSerializeMs / SerializeCount : 0.0; } }
		public static double AverageRestoreMs { get { return RestoreCount > 0 ? TotalRestoreMs / RestoreCount : 0.0; } }
		public static double AverageCompressMs { get { return SerializeCount > 0 ? TotalCompressMs / SerializeCount : 0.0; } }

		public static double LastLoadTotalMs { get; private set; }
		public static double LastLoadCleanMs { get; private set; }
		public static double LastLoadTypeResolveMs { get; private set; }
		public static double LastLoadCreateMs { get; private set; }
		public static double LastLoadAddNodeMs { get; private set; }
		public static double LastLoadWireMs { get; private set; }
		public static double LastLoadPostMs { get; private set; }
		public static double LastLoadOtherMs { get; private set; }
		public static int LastLoadNodeCount { get; private set; }
		public static int LastLoadNestedCalls { get; private set; }

		// @diogo: LoadFromMeta is reentrant ( FunctionNode.ReadFromString recurses into it ), so bucket
		// accumulation spans all nesting depths and only publishes/logs when the top-level call exits.
		private static int s_loadDepth;
		private static double s_loadCleanMs;
		private static double s_loadTypeResolveMs;
		private static double s_loadCreateMs;
		private static double s_loadAddNodeMs;
		private static double s_loadWireMs;
		private static double s_loadPostMs;
		private static int s_loadNodeCount;
		private static int s_loadNestedCalls;
		private static readonly System.Diagnostics.Stopwatch s_loadTotalTimer = new System.Diagnostics.Stopwatch();

		// @diogo: temporary diagnostic for per-node-type Destroy() cost during CleanNodes/Destroy
		private static Dictionary<System.Type, double> s_destroyMsByType = new Dictionary<System.Type, double>();
		private static Dictionary<System.Type, int> s_destroyCountByType = new Dictionary<System.Type, int>();

		// One graph serialization for an undo snapshot ( proxy OnBeforeSerialize ).
		public static void RecordSerialize( double milliseconds, double compressMs, int chars, int compressedBytes )
		{
			SerializeCount++;
			LastSerializeMs = milliseconds;
			TotalSerializeMs += milliseconds;
			LastSnapshotChars = chars;
			TotalSerializedChars += chars;
			LastCompressMs = compressMs;
			TotalCompressMs += compressMs;
			LastSnapshotCompressedBytes = compressedBytes;
			TotalCompressedBytes += compressedBytes;

			if ( Preferences.User.UndoProfiling )
			{
				Debug.Log( string.Format( "[ASE Undo] snapshot #{0}: {1:0.00} ms + {2:0.00} ms compress, {3:0.0} KB -> {4:0.0} KB", SerializeCount, milliseconds, compressMs, chars / 1024.0, compressedBytes / 1024.0 ) );
			}
		}

		// One undo/redo reload ( RestoreFromUndoSnapshot ).
		public static void RecordRestore( double milliseconds, double loadMs, double refreshMs, double viewStateMs, int nodeCount )
		{
			RestoreCount++;
			LastRestoreMs = milliseconds;
			TotalRestoreMs += milliseconds;
			LastRestoreLoadMs = loadMs;
			LastRestoreRefreshMs = refreshMs;
			LastRestoreViewStateMs = viewStateMs;
			LastRestoreNodeCount = nodeCount;

			if ( Preferences.User.UndoProfiling )
			{
				Debug.Log( string.Format( "[ASE Undo] restore #{0}: {1:0.00} ms ( load {2:0.00} / refresh {3:0.00} / view {4:0.00} ), {5} nodes", RestoreCount, milliseconds, loadMs, refreshMs, viewStateMs, nodeCount ) );
			}
		}

		// One shadow-mode diff ( RestoreFromUndoSnapshot, Preferences.User.UndoProfiling only ). Stage 1:
		// classification is logged but never applied - the caller always still runs the full reload.
		public static void RecordShadowDiff( UndoSnapshotDiff.SnapshotDiffResult result, double ms )
		{
			ShadowDiffCount++;
			if ( result.Bailed )
			{
				ShadowBailCount++;
				LastShadowSummary = string.Format( "BAIL: {0}", result.BailReason );
			}
			else
			{
				LastShadowSummary = string.Format( "changed {0} pos {1} added {2} removed {3} wires +{4}/-{5}, unchanged {6}{7}{8}",
					result.ChangedIds.Count, result.PositionOnlyIds.Count, result.AddedIds.Count, result.RemovedIds.Count, result.WireAdded, result.WireRemoved, result.UnchangedCount,
					result.MasterNodeTouched ? ", MASTER TOUCHED" : string.Empty, result.FunctionNodeRecreated ? ", FN RECREATED" : string.Empty );
			}

			if ( Preferences.User.UndoProfiling )
			{
				if ( result.Bailed )
				{
					Debug.Log( string.Format( "[ASE Undo] shadow diff: {0:0.00} ms, BAIL: {1}", ms, result.BailReason ) );
				}
				else
				{
					Debug.Log( string.Format( "[ASE Undo] shadow diff: {0:0.00} ms, {1}", ms, LastShadowSummary ) );
					Debug.Log( string.Format( "[ASE Undo] shadow detail: changed [{0}] pos [{1}] added [{2}] removed [{3}] connect {4} disconnect {5}",
						FormatIdList( result.ChangedIds ), FormatIdList( result.PositionOnlyIds ), FormatIdList( result.AddedIds ), FormatIdList( result.RemovedIds ),
						FormatWireList( result.WiresToConnect ), FormatWireList( result.WiresToDisconnect ) ) );
				}
			}
		}

		// @diogo: fires when TryPatchGraphFromSnapshot begins, before any bail-out check
		public static void RecordPatchAttempt()
		{
			PatchAttemptCount++;
		}

		// @diogo: fires when a patch applies and passes the oracle checksum verify
		public static void RecordPatchSuccess( double totalMs, double serializeMs, double diffMs, double applyMs, double postMs, double verifyMs, double checksumMs, double selectMs, int changed, int positionOnly, int added, int removed, int wiresConnected, int wiresDisconnected )
		{
			PatchSuccessCount++;
			LastPatchSummary = string.Format( "patched: changed {0} pos {1} added {2} removed {3} wires +{4}/-{5}, {6:0.00} ms", changed, positionOnly, added, removed, wiresConnected, wiresDisconnected, totalMs );

			if ( Preferences.User.UndoProfiling )
			{
				Debug.Log( string.Format( "[ASE Undo] incremental patch #{0}: {1:0.00} ms ( serialize {2:0.00} / diff {3:0.00} / apply {4:0.00} / post {5:0.00} / verify {6:0.00} / cksum {7:0.00} / select {8:0.00} ), changed {9} pos {10} added {11} removed {12} wires +{13}/-{14}", PatchSuccessCount, totalMs, serializeMs, diffMs, applyMs, postMs, verifyMs, checksumMs, selectMs, changed, positionOnly, added, removed, wiresConnected, wiresDisconnected ) );
			}
		}

		// @diogo: fires when a pre-patch bail-out check rejects the diff, falling back to full reload;
		// diff is passed for policy bails so the log shows which ids drove the decision
		public static void RecordPatchBail( string reason, UndoSnapshotDiff.SnapshotDiffResult diff = null )
		{
			PatchBailCount++;
			LastPatchSummary = "BAIL: " + reason;

			if ( Preferences.User.UndoProfiling )
			{
				Debug.Log( string.Format( "[ASE Undo] incremental bail: {0} - falling back to full reload", reason ) );
				if ( diff != null && !diff.Bailed )
				{
					Debug.Log( string.Format( "[ASE Undo] bail detail: changed [{0}] pos [{1}] added [{2}] removed [{3}] connect {4} disconnect {5}",
						FormatIdList( diff.ChangedIds ), FormatIdList( diff.PositionOnlyIds ), FormatIdList( diff.AddedIds ), FormatIdList( diff.RemovedIds ),
						FormatWireList( diff.WiresToConnect ), FormatWireList( diff.WiresToDisconnect ) ) );
				}
			}
		}

		// @diogo: fires when a patch applied but the post-patch oracle checksum did not match the target;
		// verifyDiff is a diff of the patched serialize vs the target, used to log the exact drifting lines
		public static void RecordPatchVerifyFail( double totalMs, int patchedCount, UndoSnapshotDiff.SnapshotDiffResult verifyDiff = null )
		{
			PatchVerifyFailCount++;
			LastPatchSummary = string.Format( "VERIFY FAILED: {0} patched, {1:0.00} ms", patchedCount, totalMs );

			if ( Preferences.User.UndoProfiling )
			{
				Debug.Log( string.Format( "[ASE Undo] incremental verify FAILED ( {0} patched, {1:0.00} ms ) - falling back to full reload", patchedCount, totalMs ) );

				if ( verifyDiff != null )
				{
					if ( verifyDiff.Bailed )
					{
						Debug.Log( string.Format( "[ASE Undo] verify diff unavailable: {0}", verifyDiff.BailReason ) );
					}
					else
					{
						Debug.Log( string.Format( "[ASE Undo] verify diff: changed [{0}] pos [{1}] added [{2}] removed [{3}] connect {4} disconnect {5}",
							FormatIdList( verifyDiff.ChangedIds ), FormatIdList( verifyDiff.PositionOnlyIds ), FormatIdList( verifyDiff.AddedIds ), FormatIdList( verifyDiff.RemovedIds ),
							FormatWireList( verifyDiff.WiresToConnect ), FormatWireList( verifyDiff.WiresToDisconnect ) ) );

						// @diogo: position-only drift here means an in-place position write did not round-trip
						List<int> pairIds = new List<int>( verifyDiff.ChangedIds );
						pairIds.AddRange( verifyDiff.PositionOnlyIds );
						int pairCount = Mathf.Min( 3, pairIds.Count );
						for ( int i = 0; i < pairCount; i++ )
						{
							int id = pairIds[ i ];
							Debug.Log( string.Format( "[ASE Undo] verify mismatch id {0}:\nlive: {1}\nwant: {2}",
								id, FormatLineDelta( verifyDiff.CurrentNodeLines[ id ], verifyDiff.TargetNodeLines[ id ] ),
								FormatLineDelta( verifyDiff.TargetNodeLines[ id ], verifyDiff.CurrentNodeLines[ id ] ) ) );
						}
					}
				}
			}
		}

		// @diogo: fires when an undo/redo restore lands on a "modified" indicator verdict; diff of the
		// restored snapshot vs the saved baseline, so a wrong verdict names the exact drifting lines
		public static void RecordIndicatorMismatch( UndoSnapshotDiff.SnapshotDiffResult diff )
		{
			if ( Preferences.User.UndoProfiling && diff != null )
			{
				if ( diff.Bailed )
				{
					Debug.Log( string.Format( "[ASE Undo] indicator diff vs saved baseline unavailable: {0}", diff.BailReason ) );
				}
				else
				{
					Debug.Log( string.Format( "[ASE Undo] indicator diff vs saved baseline: changed [{0}] pos [{1}] added [{2}] removed [{3}] connect {4} disconnect {5}",
						FormatIdList( diff.ChangedIds ), FormatIdList( diff.PositionOnlyIds ), FormatIdList( diff.AddedIds ), FormatIdList( diff.RemovedIds ),
						FormatWireList( diff.WiresToConnect ), FormatWireList( diff.WiresToDisconnect ) ) );

					// @diogo: position-only entries are legitimate dirty verdicts (nodes moved since the save)
					List<int> pairIds = new List<int>( diff.ChangedIds );
					pairIds.AddRange( diff.PositionOnlyIds );
					int pairCount = Mathf.Min( 3, pairIds.Count );
					for ( int i = 0; i < pairCount; i++ )
					{
						int id = pairIds[ i ];
						Debug.Log( string.Format( "[ASE Undo] indicator mismatch id {0}:\nrestored: {1}\nsaved: {2}",
							id, FormatLineDelta( diff.CurrentNodeLines[ id ], diff.TargetNodeLines[ id ] ),
							FormatLineDelta( diff.TargetNodeLines[ id ], diff.CurrentNodeLines[ id ] ) ) );
					}
				}
			}
		}

		// @diogo: window of a line around its first difference against the other line, for mismatch logging
		private static string FormatLineDelta( string line, string other )
		{
			int firstDiff = 0;
			int minLength = Mathf.Min( line.Length, other.Length );
			while ( firstDiff < minLength && line[ firstDiff ] == other[ firstDiff ] )
			{
				firstDiff++;
			}

			int start = Mathf.Max( 0, firstDiff - 60 );
			int length = Mathf.Min( 160, line.Length - start );
			string window = line.Substring( start, length );
			return string.Format( "{0}{1}{2}", start > 0 ? "…" : string.Empty, window, start + length < line.Length ? "…" : string.Empty );
		}

		// One undo step pushed onto the stack ( RegisterUndo ). Approximates current history depth since
		// the last clear; Unity may merge same-frame steps and trim old ones, so treat it as an upper bound.
		public static void RecordStep( string name, ScriptableObject proxy )
		{
			StepsRecorded++;

			// @diogo: diagnostic: which action registered, under which undo group, triggered by which GUI event
			if ( Preferences.User.UndoProfiling )
			{
				Event evt = Event.current;
				Debug.Log( string.Format( "[ASE Undo] step #{0} registered: \"{1}\" ( group {2}, event {3}, proxy {4} )", StepsRecorded, name,
					UnityEditor.Undo.GetCurrentGroup(), evt != null ? evt.type + "/" + evt.rawType : "none", AssetUtils.GetEntityId( proxy ) ) );
			}
		}

		// Mirrors a full undo-stack clear so the step count tracks the live history.
		public static void ResetHistory()
		{
			StepsRecorded = 0;
		}

		// @diogo: entering LoadFromMeta ( possibly nested via FunctionNode ); resets accumulators on the outermost call
		public static void BeginLoadFromMeta()
		{
			s_loadDepth++;
			if ( s_loadDepth == 1 )
			{
				s_loadCleanMs = 0.0;
				s_loadTypeResolveMs = 0.0;
				s_loadCreateMs = 0.0;
				s_loadAddNodeMs = 0.0;
				s_loadWireMs = 0.0;
				s_loadPostMs = 0.0;
				s_loadNodeCount = 0;
				s_loadNestedCalls = 0;
				s_destroyMsByType.Clear();
				s_destroyCountByType.Clear();
				s_loadTotalTimer.Restart();
			}
			else
			{
				s_loadNestedCalls++;
			}
		}

		public static void AddLoadClean( double ms )
		{
			s_loadCleanMs += ms;
		}

		public static void AddLoadTypeResolve( double ms )
		{
			s_loadTypeResolveMs += ms;
		}

		public static void AddLoadCreate( double ms )
		{
			s_loadCreateMs += ms;
		}

		public static void AddLoadAddNode( double ms )
		{
			s_loadAddNodeMs += ms;
		}

		public static void AddLoadWire( double ms )
		{
			s_loadWireMs += ms;
		}

		public static void AddLoadPost( double ms )
		{
			s_loadPostMs += ms;
		}

		public static void AddLoadNodes( int count )
		{
			s_loadNodeCount += count;
		}

		public static void AddDestroyByType( System.Type type, double ms )
		{
			double existingMs;
			s_destroyMsByType.TryGetValue( type, out existingMs );
			s_destroyMsByType[ type ] = existingMs + ms;

			int existingCount;
			s_destroyCountByType.TryGetValue( type, out existingCount );
			s_destroyCountByType[ type ] = existingCount + 1;
		}

		// @diogo: leaving LoadFromMeta; publishes and logs the accumulated buckets once the outermost call returns
		public static void EndLoadFromMeta()
		{
			if ( s_loadDepth > 0 )
			{
				s_loadDepth--;
			}

			if ( s_loadDepth == 0 )
			{
				s_loadTotalTimer.Stop();
				double total = s_loadTotalTimer.Elapsed.TotalMilliseconds;
				double other = total - ( s_loadCleanMs + s_loadTypeResolveMs + s_loadCreateMs + s_loadAddNodeMs + s_loadWireMs + s_loadPostMs );
				if ( other < 0.0 )
				{
					other = 0.0;
				}

				LastLoadTotalMs = total;
				LastLoadCleanMs = s_loadCleanMs;
				LastLoadTypeResolveMs = s_loadTypeResolveMs;
				LastLoadCreateMs = s_loadCreateMs;
				LastLoadAddNodeMs = s_loadAddNodeMs;
				LastLoadWireMs = s_loadWireMs;
				LastLoadPostMs = s_loadPostMs;
				LastLoadOtherMs = other;
				LastLoadNodeCount = s_loadNodeCount;
				LastLoadNestedCalls = s_loadNestedCalls;

				if ( Preferences.User.UndoProfiling )
				{
					Debug.Log( string.Format( "[ASE Undo] LoadFromMeta: {0:0.00} ms ( clean {1:0.00} / type {2:0.00} / create {3:0.00} / add {4:0.00} / wire {5:0.00} / post {6:0.00} / other {7:0.00} ), {8} nodes, {9} nested", total, s_loadCleanMs, s_loadTypeResolveMs, s_loadCreateMs, s_loadAddNodeMs, s_loadWireMs, s_loadPostMs, other, s_loadNodeCount, s_loadNestedCalls ) );

					if ( s_destroyMsByType.Count > 0 )
					{
						List<KeyValuePair<System.Type, double>> sortedTypes = new List<KeyValuePair<System.Type, double>>( s_destroyMsByType );
						sortedTypes.Sort( ( a, b ) => b.Value.CompareTo( a.Value ) );

						StringBuilder sb = new StringBuilder();
						sb.Append( "[ASE Undo] Destroy by type: " );
						int entryCount = Mathf.Min( 8, sortedTypes.Count );
						for ( int i = 0; i < entryCount; i++ )
						{
							if ( i > 0 )
							{
								sb.Append( " | " );
							}
							System.Type type = sortedTypes[ i ].Key;
							sb.AppendFormat( "{0} {1:0.00} ms x{2}", type.Name, sortedTypes[ i ].Value, s_destroyCountByType[ type ] );
						}
						Debug.Log( sb.ToString() );
					}
				}
			}
		}

		// @diogo: comma-joined id list for shadow-detail logging, capped at 24 entries
		private static string FormatIdList( List<int> ids )
		{
			StringBuilder sb = new StringBuilder();
			int entryCount = Mathf.Min( 24, ids.Count );
			for ( int i = 0; i < entryCount; i++ )
			{
				if ( i > 0 )
				{
					sb.Append( ", " );
				}
				sb.Append( ids[ i ] );
			}
			if ( ids.Count > entryCount )
			{
				sb.Append( ", \u2026" );
			}
			return sb.ToString();
		}

		// @diogo: wire patch-plan size plus up to 6 semicolon-separated wire lines for shadow-detail logging
		private static string FormatWireList( List<string> wires )
		{
			StringBuilder sb = new StringBuilder();
			sb.Append( wires.Count );
			int entryCount = Mathf.Min( 6, wires.Count );
			int i = 0;
			foreach ( string wire in wires )
			{
				if ( i >= entryCount )
				{
					break;
				}
				sb.Append( i == 0 ? ": " : "; " );
				sb.Append( wire );
				i++;
			}
			if ( wires.Count > entryCount )
			{
				sb.Append( " \u2026" );
			}
			return sb.ToString();
		}

		public static void ResetStatistics()
		{
			SerializeCount = 0;
			LastSerializeMs = 0.0;
			TotalSerializeMs = 0.0;
			LastSnapshotChars = 0;
			TotalSerializedChars = 0;
			LastCompressMs = 0.0;
			TotalCompressMs = 0.0;
			LastSnapshotCompressedBytes = 0;
			TotalCompressedBytes = 0;
			RestoreCount = 0;
			LastRestoreMs = 0.0;
			TotalRestoreMs = 0.0;
			LastRestoreLoadMs = 0.0;
			LastRestoreRefreshMs = 0.0;
			LastRestoreViewStateMs = 0.0;
			LastRestoreNodeCount = 0;
			StepsRecorded = 0;
			LastLoadTotalMs = 0.0;
			LastLoadCleanMs = 0.0;
			LastLoadTypeResolveMs = 0.0;
			LastLoadCreateMs = 0.0;
			LastLoadAddNodeMs = 0.0;
			LastLoadWireMs = 0.0;
			LastLoadPostMs = 0.0;
			LastLoadOtherMs = 0.0;
			LastLoadNodeCount = 0;
			LastLoadNestedCalls = 0;
			ShadowDiffCount = 0;
			ShadowBailCount = 0;
			LastShadowSummary = "-";
			PatchAttemptCount = 0;
			PatchSuccessCount = 0;
			PatchBailCount = 0;
			PatchVerifyFailCount = 0;
			LastPatchSummary = "-";
		}
	}
}

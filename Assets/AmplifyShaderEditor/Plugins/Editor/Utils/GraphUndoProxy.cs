// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	// Snapshot-based undo proxy.
	//
	// The live graph ( ParentGraph + node ScriptableObjects ) is never recorded by Unity's undo
	// system. Instead this tiny object carries a single serialized meta string, stored
	// Deflate-compressed ( optional, see Preferences ). Whenever Unity needs to snapshot the
	// object's "current" state ( at registration, undo or redo ), OnBeforeSerialize re-serializes
	// the *live* graph into that string, so the snapshot always reflects the true current state.
	// On restore the owner rebuilds the graph from the string through the normal load path. This
	// sidesteps the per-object undo failure points entirely: if save/load is correct, undo is
	// correct.
	public sealed class GraphUndoProxy : ScriptableObject, ISerializationCallbackReceiver
	{
		// @diogo: exactly one of these two is populated per snapshot, per Preferences.User.CompressUndoSnapshots at capture time.
		[SerializeField]
		private string m_serializedGraph = string.Empty;

		[SerializeField]
		private byte[] m_compressedGraph = new byte[ 0 ];

		[SerializeField]
		private int[] m_selectedNodeIds = new int[ 0 ];

		[NonSerialized]
		private AmplifyShaderEditorWindow m_owner;

		[NonSerialized]
		private bool m_pendingRestore;

		// @diogo: diagnostic only ( UndoProfiling ): counts every OnAfterDeserialize to trace whether undo applies reach this instance
		[NonSerialized]
		private int m_deserializeCount;

		public void Init( AmplifyShaderEditorWindow owner )
		{
			m_owner = owner;
		}

		// Pushes the pre-edit state onto Unity's undo stack. Call this *before* a graph mutation.
		public void RegisterUndo( string name )
		{
			if ( m_owner == null )
			{
				return;
			}

			// Record this proxy directly with Unity's undo ( UndoUtils per-object methods are now
			// no-op shims ). OnBeforeSerialize captures the live graph into the snapshot string.
			Undo.RegisterCompleteObjectUndo( this, name );
			UndoProfiler.RecordStep( name, this );
		}

		// Initializes the serialized string right after a load so the field is valid before the
		// first edit. ( OnBeforeSerialize keeps it current from then on. )
		public void CaptureBaseline()
		{
			if ( m_owner != null )
			{
				string snapshot = m_owner.GenerateGraphSnapshot();
				if ( !string.IsNullOrEmpty( snapshot ) )
				{
					if ( Preferences.User.CompressUndoSnapshots )
					{
						m_compressedGraph = Compress( snapshot );
						m_serializedGraph = string.Empty;
					}
					else
					{
						m_serializedGraph = snapshot;
						m_compressedGraph = new byte[ 0 ];
					}
					m_selectedNodeIds = m_owner.GetSelectedNodeIds();
					m_owner.NotifyLiveGraphSerialized( snapshot );
				}
			}
		}

		public void OnBeforeSerialize()
		{
			// Crux of the design: always serialize the *live* graph here, never a cached value.
			// Unity invokes this both when recording an undo step and when capturing the opposite
			// direction for redo, so reading the live graph is what makes multi-level undo/redo
			// land on the correct state.
			if ( m_owner == null || !Preferences.User.EnableUndo )
			{
				return;
			}

			System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
			string snapshot = m_owner.GenerateGraphSnapshot();
			stopwatch.Stop();
			if ( !string.IsNullOrEmpty( snapshot ) )
			{
				double compressMs;
				int storedBytes;
				if ( Preferences.User.CompressUndoSnapshots )
				{
					System.Diagnostics.Stopwatch compressStopwatch = System.Diagnostics.Stopwatch.StartNew();
					m_compressedGraph = Compress( snapshot );
					compressStopwatch.Stop();
					m_serializedGraph = string.Empty;
					compressMs = compressStopwatch.Elapsed.TotalMilliseconds;
					storedBytes = m_compressedGraph.Length;
				}
				else
				{
					m_serializedGraph = snapshot;
					m_compressedGraph = new byte[ 0 ];
					compressMs = 0.0;
					storedBytes = snapshot.Length;
				}
				m_selectedNodeIds = m_owner.GetSelectedNodeIds();
				UndoProfiler.RecordSerialize( stopwatch.Elapsed.TotalMilliseconds, compressMs, snapshot.Length, storedBytes );

				// @diogo: Unity also invokes this capture inside every undo/redo, right before overwriting
				// the proxy with the target - hand the owner that fresh serialize so the patcher can reuse
				// it as its current-state capture instead of serializing the same state again
				m_owner.NotifyLiveGraphSerialized( snapshot );
			}
		}

		public void OnAfterDeserialize()
		{
			// The graph cannot be rebuilt here ( object creation/destruction is forbidden during
			// serialization callbacks ). The owner drives the actual reload from the
			// Undo.undoRedoPerformed callback; this flag is a guard for that path.
			m_pendingRestore = true;
			m_deserializeCount++;
		}

		public int DeserializeCount
		{
			get { return m_deserializeCount; }
		}

		public string SerializedGraph
		{
			get { return !string.IsNullOrEmpty( m_serializedGraph ) ? m_serializedGraph : Decompress( m_compressedGraph ); }
		}

		public int[] SelectedNodeIds
		{
			get { return m_selectedNodeIds; }
		}

		public bool PendingRestore
		{
			get { return m_pendingRestore; }
			set { m_pendingRestore = value; }
		}

		public int StoredSnapshotBytes { get { return !string.IsNullOrEmpty( m_serializedGraph ) ? m_serializedGraph.Length : ( m_compressedGraph != null ? m_compressedGraph.Length : 0 ); } }

		private static byte[] Compress( string text )
		{
			byte[] raw = Encoding.UTF8.GetBytes( text );
			using ( MemoryStream output = new MemoryStream() )
			{
				using ( DeflateStream deflate = new DeflateStream( output, System.IO.Compression.CompressionLevel.Fastest, true ) )
				{
					deflate.Write( raw, 0, raw.Length );
				}
				return output.ToArray();
			}
		}

		private static string Decompress( byte[] data )
		{
			if ( data == null || data.Length == 0 )
			{
				return string.Empty;
			}

			using ( MemoryStream input = new MemoryStream( data ) )
			using ( MemoryStream output = new MemoryStream() )
			{
				using ( DeflateStream deflate = new DeflateStream( input, CompressionMode.Decompress ) )
				{
					deflate.CopyTo( output );
				}
				return Encoding.UTF8.GetString( output.ToArray() );
			}
		}
	}
}

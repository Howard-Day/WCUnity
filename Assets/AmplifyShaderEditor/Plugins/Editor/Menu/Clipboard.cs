// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AmplifyShaderEditor
{

	public class ClipboardData
	{
		public string Data = string.Empty;
		public string Connections = string.Empty;
		public int OldNodeId = -1;
		public int NewNodeId = -1;
		
		public ClipboardData( string data, string connections, int oldNodeId )
		{
			Data = data;
			Connections = connections;
			OldNodeId = oldNodeId;
		}

		public override string ToString()
		{
			return Data + IOUtils.CLIPBOARD_DATA_SEPARATOR + Connections + IOUtils.CLIPBOARD_DATA_SEPARATOR + OldNodeId + IOUtils.CLIPBOARD_DATA_SEPARATOR + NewNodeId;
		}
	}

	public class Clipboard
	{
		public const string ClipboardId = "AMPLIFY_CLIPBOARD_ID";
		private readonly string[] ClipboardTagId = { "#CLIP_ITEM#" };
		private List<ClipboardData> m_clipboardStrData;
		private Dictionary<int, ClipboardData> m_clipboardAuxData;
		private Dictionary<string, ClipboardData> m_multiPassMasterNodeData;
		// @diogo: ASE version that wrote the clipboard data; 0 means the payload predates stamping
		private int m_clipboardVersion = 0;

		public Clipboard()
		{
			m_clipboardStrData = new List<ClipboardData>();
			m_clipboardAuxData = new Dictionary<int, ClipboardData>();
			m_multiPassMasterNodeData = new Dictionary<string, ClipboardData>();
		}

		public void ResetMultipassNodesData()
		{
			m_multiPassMasterNodeData.Clear();
		}

		public void AddMultiPassNodesToClipboard( List<TemplateMultiPassMasterNode> masterNodes, bool resetList, int lodId )
		{
			if( resetList )
				m_multiPassMasterNodeData.Clear();

			int templatesAmount = masterNodes.Count;
			for( int i = 0; i < templatesAmount; i++ )
			{
				if( !masterNodes[ i ].InvalidNode )
				{
					string data = string.Empty;
					string connection = string.Empty;
					System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
					masterNodes[ i ].FullWriteToString( ref data, ref connection );
					JsonGraphFormat.EndNodeLine( ref data );
					System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
					ClipboardData clipboardData = new ClipboardData( data, connection, masterNodes[ i ].UniqueId );
					m_multiPassMasterNodeData.Add( masterNodes[ i ].PassUniqueName + lodId, clipboardData );
				}
			}
		}

		public void GetMultiPassNodesFromClipboard( List<TemplateMultiPassMasterNode> masterNodes, int lodId )
		{
			int templatesAmount = masterNodes.Count;
			for( int i = 0; i < templatesAmount; i++ )
			{
				string clipboardDataId = masterNodes[ i ].PassUniqueName + lodId;
				if( m_multiPassMasterNodeData.ContainsKey( clipboardDataId ) )
				{
					ClipboardData nodeData = m_multiPassMasterNodeData[ clipboardDataId ];
					string[] nodeParams = JsonGraphFormat.SplitInstruction( nodeData.Data );
					System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
					masterNodes[ i ].FullReadFromString( ref nodeParams );
					System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
				}
			}

			for( int i = 0; i < templatesAmount; i++ )
			{
				string clipboardDataId = masterNodes[ i ].PassUniqueName + lodId;
				if( m_multiPassMasterNodeData.ContainsKey( clipboardDataId ) )
				{
					masterNodes[ i ].SetReadOptions();
					masterNodes[ i ].ForceOptionsRefresh();
				}
			}

		}

		// mirrorToSystem also leaves the payload in the OS clipboard, so a copy can travel between
		// projects. Only the Copy command asks for it: duplicate pastes straight back, and share
		// replaces the clipboard with its link.
		public void AddToClipboard( List<ParentNode> selectedNodes , Vector3 initialPosition, ParentGraph graph, bool mirrorToSystem )
		{
			//m_clipboardStrData.Clear();
			//m_clipboardAuxData.Clear();

			// @diogo: the version rides as a fourth component because StringToVector3 reads only the first
			// three, so older ASE builds still parse the position out of a stamped clipboard
			string clipboardData = IOUtils.Vector3ToString( initialPosition ) + IOUtils.VECTOR_SEPARATOR + VersionInfo.FullNumber + ClipboardTagId[ 0 ];
			int masterNodeId = UIUtils.CurrentWindow.CurrentGraph.CurrentMasterNodeId;
			int count = selectedNodes.Count;
			int writtenCount = 0;
			for ( int i = 0; i < count; i++ )
			{
				if ( UIUtils.CurrentWindow.IsShaderFunctionWindow || !graph.IsMasterNode( selectedNodes[ i ] ))
				{
					string nodeData = string.Empty;
					string connections = string.Empty;
					selectedNodes[ i ].ClipboardFullWriteToString( ref nodeData, ref connections );
					JsonGraphFormat.EndNodeLine( ref nodeData );
					clipboardData += nodeData;
					if ( !string.IsNullOrEmpty( connections ) )
					{
						connections = connections.Substring( 0, connections.Length - 1 );
						clipboardData += "\n" + connections;
					}
					if ( i < count - 1 )
						clipboardData += ClipboardTagId[ 0 ];

					writtenCount++;

					//ClipboardData data = new ClipboardData( nodeData, connections, selectedNodes[ i ].UniqueId );
					//m_clipboardStrData.Add( data );
					//m_clipboardAuxData.Add( selectedNodes[ i ].UniqueId, data );
				}
			}

			if ( !string.IsNullOrEmpty( clipboardData ) )
			{
				EditorPrefs.SetString( ClipboardId, clipboardData );
			}

			// @diogo: mirrored so a payload left in the OS clipboard, which PasteFromClipboard reads
			// first, can no longer outrank this copy; also lets copies travel between projects.
			// Skipped on an empty copy, which would otherwise wipe the OS clipboard for nothing
			if ( mirrorToSystem && writtenCount > 0 )
			{
				EditorGUIUtility.systemCopyBuffer = ClipboardId + ";" + clipboardData;
			}
			//for ( int i = 0; i < selectedNodes.Count; i++ )
			//{
			//	if ( selectedNodes[ i ].UniqueId != masterNodeId )
			//	{
			//		WireNode wireNode = selectedNodes[ i ] as WireNode;
			//		if ( wireNode != null )
			//		{
			//			if ( !IsNodeChainValid( selectedNodes[ i ], true ) || !IsNodeChainValid( selectedNodes[ i ], false ) )
			//			{
			//				UnityEngine.Debug.Log( "found invalid wire port" );
			//			}
			//		}
			//	}
			//}
		}

		public Vector3 GetDataFromEditorPrefs()
		{
			Vector3 initialPos = Vector3.zero;
			m_clipboardStrData.Clear();
			m_clipboardAuxData.Clear();
			m_clipboardVersion = 0;
			string clipboardData = EditorPrefs.GetString( ClipboardId, string.Empty );
			if ( !string.IsNullOrEmpty( clipboardData ) )
			{
				string[] clipboardDataArray = clipboardData.Split( ClipboardTagId, StringSplitOptions.None );
				initialPos = IOUtils.StringToVector3( clipboardDataArray[0] );
				// @diogo: 4th component, if present, is the version stamp added in AddToClipboard
				string[] initialPosComponents = clipboardDataArray[ 0 ].Split( IOUtils.VECTOR_SEPARATOR );
				if ( initialPosComponents.Length > 3 )
				{
					int.TryParse( initialPosComponents[ 3 ], out m_clipboardVersion );
				}
				for ( int i = 1; i < clipboardDataArray.Length; i++ )
				{
					if ( !string.IsNullOrEmpty( clipboardDataArray[ i ] ) )
					{
						int wiresIndex = clipboardDataArray[ i ].IndexOf( IOUtils.LINE_TERMINATOR );
						string nodeData = string.Empty;
						string connections = string.Empty;
						if ( wiresIndex < 0 )
						{
							nodeData = clipboardDataArray[ i ];
						}
						else
						{
							nodeData = clipboardDataArray[ i ].Substring( 0, wiresIndex );
							connections = clipboardDataArray[ i ].Substring( wiresIndex + 1 );
						}
						string[] nodeDataArr = JsonGraphFormat.SplitInstruction( nodeData );
						if ( nodeDataArr.Length > 2 )
						{
							int nodeId = Convert.ToInt32( nodeDataArr[ 2 ] );
							ClipboardData data = new ClipboardData( nodeData, connections, nodeId );
							m_clipboardStrData.Add( data );
							m_clipboardAuxData.Add( nodeId, data );
						}

					}
				}
			}
			return initialPos;
		}

		public bool IsNodeChainValid( ParentNode currentNode, bool forward )
		{
			WireNode wireNode = currentNode as WireNode;
			if ( wireNode == null )
			{
				return m_clipboardAuxData.ContainsKey( currentNode.UniqueId );
			}

			if ( forward )
			{
				if ( wireNode.InputPorts[ 0 ].ExternalReferences.Count > 0 )
				{
					int nodeId = wireNode.InputPorts[ 0 ].ExternalReferences[ 0 ].NodeId;
					if ( m_clipboardAuxData.ContainsKey( nodeId ) )
					{
						return IsNodeChainValid( UIUtils.GetNode( nodeId ), forward );
					}
				}
			}
			else
			{
				int nodeId = wireNode.OutputPorts[ 0 ].ExternalReferences[ 0 ].NodeId;
				if ( m_clipboardAuxData.ContainsKey( nodeId ) )
				{
					return IsNodeChainValid( UIUtils.GetNode( nodeId ), forward );
				}
			}
			return false;
		}

		public void GenerateFullString()
		{
			string data = string.Empty;
			for ( int i = 0; i < m_clipboardStrData.Count; i++ )
			{
				data += m_clipboardStrData[ i ].ToString();
				if ( i < m_clipboardStrData.Count - 1 )
				{
					data += IOUtils.LINE_TERMINATOR;
				}
			}
		}

		public void ClearClipboard()
		{
			m_clipboardStrData.Clear();
			m_clipboardAuxData.Clear();
			m_multiPassMasterNodeData.Clear();
			m_clipboardVersion = 0;
		}

		public ClipboardData GetClipboardData( int oldNodeId )
		{
			if ( m_clipboardAuxData.ContainsKey( oldNodeId ) )
				return m_clipboardAuxData[ oldNodeId ];
			return null;
		}

		public int GeNewNodeId( int oldNodeId )
		{
			if ( m_clipboardAuxData.ContainsKey( oldNodeId ) )
				return m_clipboardAuxData[ oldNodeId ].NewNodeId;
			return -1;
		}

		public List<ClipboardData> CurrentClipboardStrData
		{
			get { return m_clipboardStrData; }
		}

		public bool HasCachedMasterNodes { get { return m_multiPassMasterNodeData.Count > 0; } }

		public int ClipboardVersion { get { return m_clipboardVersion; } }
	}
}

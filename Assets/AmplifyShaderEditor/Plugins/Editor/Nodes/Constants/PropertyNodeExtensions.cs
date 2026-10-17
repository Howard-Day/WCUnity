// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	// Contract an external integration implements to extend property nodes, without ASE referencing
	// or knowing anything about that integration. Implementations are discovered automatically at
	// editor load (UnityEditor.TypeCache) — shipping a type that implements this interface is the
	// registration; there is no manual register call.
	//
	// See docs/property-node-extension-seam.md for how to build a bridge assembly against this.
	public interface IPropertyNodeExtension
	{
		// Stable, unique identifier used to namespace this extension's serialized per-node data.
		// Must not contain ':' and should match [A-Za-z0-9._-]+.
		string ExtensionId { get; }

		// Draws extra UI at the end of a property node's properties panel. Flag node.IsDirty (and
		// node.SetSaveIsDirty()) when a value changes so ASE regenerates and saves.
		void DrawUI( PropertyNode node );

		// Returns an extra shader-property attribute string (for example "[SomeDrawer]") appended to
		// the node's generated attributes. Return null or empty to contribute nothing.
		string BuildAttributes( PropertyNode node );

		// Serializes this extension's per-node data into one opaque token. Any character content is
		// allowed — since v1.9.9.10 the graph metadata is JSON-escaped, so human-readable tokens
		// ( e.g. plain JSON ) are preferred over Base64; they keep the saved shader diffable.
		// Return null or empty to persist nothing for this node.
		string WriteData( PropertyNode node );

		// Restores this extension's per-node data from a token previously produced by WriteData.
		void ReadData( PropertyNode node, string token );
	}

	// Registry and dispatch for IPropertyNodeExtension. Every method is a no-op when no implementation
	// is present, so ASE behaves exactly as before.
	[NoAutoStaticsCleanup]
	public static class PropertyNodeExtensions
	{
		private static List<IPropertyNodeExtension> m_extensions;

		// Pre-warm discovery on the main thread at editor load so later (possibly threaded)
		// serialization never touches TypeCache off-thread.
		[InitializeOnLoadMethod]
		private static void WarmUp()
		{
			EnsureExtensions();
		}

		private static List<IPropertyNodeExtension> EnsureExtensions()
		{
			if ( m_extensions == null )
			{
				m_extensions = new List<IPropertyNodeExtension>();
				foreach ( Type type in TypeCache.GetTypesDerivedFrom<IPropertyNodeExtension>() )
				{
					if ( type.IsAbstract || type.IsInterface )
					{
						continue;
					}

					try
					{
						m_extensions.Add( ( IPropertyNodeExtension )Activator.CreateInstance( type ) );
					}
					catch ( Exception e )
					{
						Debug.LogError( "[ASE] Failed to instantiate property node extension " + type.FullName + ": " + e.Message );
					}
				}
			}
			return m_extensions;
		}

		public static void DrawUI( PropertyNode node )
		{
			List<IPropertyNodeExtension> extensions = EnsureExtensions();
			for ( int i = 0; i < extensions.Count; i++ )
			{
				extensions[ i ].DrawUI( node );
			}
		}

		public static string BuildAttributes( PropertyNode node )
		{
			List<IPropertyNodeExtension> extensions = EnsureExtensions();
			if ( extensions.Count == 0 )
			{
				return string.Empty;
			}

			StringBuilder builder = new StringBuilder();
			for ( int i = 0; i < extensions.Count; i++ )
			{
				string attributes = extensions[ i ].BuildAttributes( node );
				if ( !string.IsNullOrEmpty( attributes ) )
				{
					builder.Append( attributes );
				}
			}
			return builder.ToString();
		}

		// Hands each registered extension its own token from the packed payload. Entries owned by
		// extensions that aren't currently present are simply ignored here and survive in the payload
		// (see WriteData), so a graph round-trips losslessly through a build that lacks them.
		public static void ReadData( PropertyNode node, string blob )
		{
			List<IPropertyNodeExtension> extensions = EnsureExtensions();
			if ( extensions.Count == 0 || string.IsNullOrEmpty( blob ) )
			{
				return;
			}

			Dictionary<string, string> map = Unpack( blob );
			for ( int i = 0; i < extensions.Count; i++ )
			{
				if ( map.TryGetValue( extensions[ i ].ExtensionId, out string token ) )
				{
					extensions[ i ].ReadData( node, token );
				}
			}
		}

		// Rebuilds the packed payload: refreshes every present extension's token while preserving
		// entries owned by extensions that aren't currently present (opaque passthrough).
		public static string WriteData( PropertyNode node, string previousBlob )
		{
			List<IPropertyNodeExtension> extensions = EnsureExtensions();
			if ( extensions.Count == 0 )
			{
				return previousBlob ?? string.Empty;
			}

			Dictionary<string, string> map = Unpack( previousBlob );
			for ( int i = 0; i < extensions.Count; i++ )
			{
				string token = extensions[ i ].WriteData( node );
				if ( string.IsNullOrEmpty( token ) )
				{
					map.Remove( extensions[ i ].ExtensionId );
				}
				else
				{
					map[ extensions[ i ].ExtensionId ] = token;
				}
			}
			return Pack( map );
		}

		// Returns the opaque token previously stored for 'extensionId' on this node, or null when there
		// is none. Lets an extension rebuild its own per-node cache after a domain reload: node.ExtensionData
		// is [SerializeField] and survives reloads, but the extension's in-memory cache does not, and the
		// raw token is otherwise hidden inside the packed payload.
		public static string GetToken( PropertyNode node, string extensionId )
		{
			if ( node == null || string.IsNullOrEmpty( node.ExtensionData ) )
			{
				return null;
			}

			Dictionary<string, string> map = Unpack( node.ExtensionData );
			return map.TryGetValue( extensionId, out string token ) ? token : null;
		}

		// Packs { id -> token } into one string as repeated "<id>:<len>:<token>".
		// Length-prefixing lets a token contain any character; only the id is restricted ( no ':' ).
		private static string Pack( Dictionary<string, string> map )
		{
			if ( map.Count == 0 )
			{
				return string.Empty;
			}

			StringBuilder builder = new StringBuilder();
			foreach ( KeyValuePair<string, string> entry in map )
			{
				builder.Append( entry.Key ).Append( ':' ).Append( entry.Value.Length ).Append( ':' ).Append( entry.Value );
			}
			return builder.ToString();
		}

		private static Dictionary<string, string> Unpack( string blob )
		{
			Dictionary<string, string> map = new Dictionary<string, string>();
			if ( string.IsNullOrEmpty( blob ) )
			{
				return map;
			}

			int index = 0;
			while ( index < blob.Length )
			{
				int idEnd = blob.IndexOf( ':', index );
				if ( idEnd < 0 )
				{
					break;
				}
				string id = blob.Substring( index, idEnd - index );

				int lenEnd = blob.IndexOf( ':', idEnd + 1 );
				if ( lenEnd < 0 )
				{
					break;
				}

				if ( !int.TryParse( blob.Substring( idEnd + 1, lenEnd - idEnd - 1 ), out int length ) || length < 0 || lenEnd + 1 + length > blob.Length )
				{
					break;
				}

				map[ id ] = blob.Substring( lenEnd + 1, length );
				index = lenEnd + 1 + length;
			}
			return map;
		}
	}
}

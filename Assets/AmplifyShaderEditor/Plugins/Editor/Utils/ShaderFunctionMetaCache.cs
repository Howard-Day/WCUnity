// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	// Everything LoadFromMeta derives from a graph's metadata text before it touches nodes: the
	// version it was saved with and one positional parameter array per node or wire instruction.
	public class ParsedGraphMeta
	{
		public bool HasVersion;
		public int Version;
		public List<string[]> Instructions = new List<string[]>();
		public List<bool> FromJson = new List<bool>();
	}

	// Parses a shader function's metadata once per distinct text, so the hundreds of instances of
	// the same function in a large graph replay the result instead of each re-parsing it
	// (docs/shader-function-load-cache.md).
	[NoAutoStaticsCleanup]
	public static class ShaderFunctionMetaCache
	{
		// @diogo: an edited asset is a new key, so stale entries only pile up; the clear bounds them
		private const int MaxEntries = 256;

		private static Dictionary<string, ParsedGraphMeta> s_cache = new Dictionary<string, ParsedGraphMeta>( StringComparer.Ordinal );

		public static ParsedGraphMeta Get( string meta )
		{
			if ( !s_cache.TryGetValue( meta, out ParsedGraphMeta parsed ) )
			{
				if ( s_cache.Count >= MaxEntries )
				{
					s_cache.Clear();
				}
				parsed = Parse( meta );
				s_cache.Add( meta, parsed );
			}
			return parsed;
		}

		// Null when the text carries no checksum or no node body, in which case there is nothing to load.
		public static ParsedGraphMeta Parse( string meta )
		{
			ParsedGraphMeta parsed = null;
			int checksumId = meta.IndexOf( IOUtils.CHECKSUM );
			if ( checksumId > -1 )
			{
				string checkSumStoredValue = meta.Substring( checksumId );
				string trimmedBuffer = meta.Remove( checksumId );

				string[] typeValuePair = checkSumStoredValue.Split( IOUtils.VALUE_SEPARATOR );
				if ( typeValuePair != null && typeValuePair.Length == 2 )
				{
					// Check read checksum and compare with the actual shader body to detect external changes
					string currentChecksumValue = IOUtils.CreateChecksum( trimmedBuffer );
					if ( DebugConsoleWindow.DeveloperMode && !currentChecksumValue.Equals( typeValuePair[ 1 ] ) )
					{
						//ShowMessage( "Wrong checksum" );
					}

					trimmedBuffer = trimmedBuffer.Replace( "\r", string.Empty );
					// find node info body
					int shaderBodyId = trimmedBuffer.IndexOf( IOUtils.ShaderBodyBegin );
					if ( shaderBodyId > -1 )
					{
						parsed = new ParsedGraphMeta();
						trimmedBuffer = trimmedBuffer.Substring( shaderBodyId );
						//Find set of instructions
						string[] instructions = trimmedBuffer.Split( IOUtils.LINE_TERMINATOR );
						// First line is to be ignored and second line contains version
						string[] versionParams = instructions[ 1 ].Split( IOUtils.VALUE_SEPARATOR );
						if ( versionParams.Length == 2 )
						{
							int version = 0;
							try
							{
								version = Convert.ToInt32( versionParams[ 1 ] );
							}
							catch ( Exception e )
							{
								Debug.LogException( e );
							}
							parsed.HasVersion = true;
							parsed.Version = version;
						}

						// valid instructions are only between the line after version and the line before the last one ( which contains ShaderBodyEnd )
						for ( int instructionIdx = 0; instructionIdx < instructions.Length - 1; instructionIdx++ )
						{
							string[] parameters = JsonGraphFormat.SplitInstruction( instructions[ instructionIdx ] );
							if ( parameters[ 0 ] == IOUtils.NodeParam || parameters[ 0 ] == IOUtils.WireConnectionParam )
							{
								parsed.Instructions.Add( parameters );
								parsed.FromJson.Add( JsonGraphFormat.LastInstructionFromJson );
							}
						}
					}
				}
			}
			return parsed;
		}
	}
}

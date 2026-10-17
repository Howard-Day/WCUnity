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
	// Graph metadata format used from v1.9.9.10 ( 19910 ) onwards: each instruction inside the
	// /*ASEBEGIN ... ASEEND*/ block is a single JSON object per line, replacing the legacy
	// ';'-separated positional lines:
	//
	//   Node: {"type":"<assembly qualified type>","id":<int>,"pos":[<x>,<y>],"params":["<param>",...]}
	//   Wire: {"wire":[<inNodeId>,<inPortId>,<outNodeId>,<outPortId>]}
	//
	// "params" carries the exact same ordered parameters the legacy format placed after the position
	// field, JSON-escaped so user text ( newlines, ';', '$', '@', '*' + '/' ) survives verbatim.
	// SplitInstruction() unpacks a JSON line back into the legacy parameter layout, so the
	// positional node readers consume both formats unchanged. Reading is sniffed per line and
	// never version-gated: legacy lines keep flowing through the frozen positional path.
	[NoAutoStaticsCleanup]
	public static class JsonGraphFormat
	{
		// True when the last instruction handed to SplitInstruction was in JSON format. Meant to
		// be consulted at the start of a node/module read to decide on legacy-only data healing
		// ( e.g. undoing the '$'/'@' escaping the legacy format applied to user text ). Calling
		// into SplitInstruction again invalidates it, so don't cache it across nested loads.
		// @diogo: settable so a cached parse can replay each line's origin without re-splitting it
		public static bool LastInstructionFromJson { get; set; }

		public static void BeginNodeLine( ref string buffer, string nodeTypeName, int uniqueId, Vector2 position )
		{
			buffer += "{\"type\":\"" + EscapeString( nodeTypeName ) +
				"\",\"id\":" + uniqueId.ToString( CultureInfo.InvariantCulture ) +
				",\"pos\":[" + position.x.ToString( CultureInfo.InvariantCulture ) +
				"," + position.y.ToString( CultureInfo.InvariantCulture ) + "],\"params\":[";
		}

		public static void EndNodeLine( ref string buffer )
		{
			buffer += "]}";
		}

		public static void AppendParam( ref string buffer, string value )
		{
			bool firstParam = ( buffer.Length > 0 && buffer[ buffer.Length - 1 ] == '[' );
			buffer += ( firstParam ? "\"" : ",\"" ) + EscapeString( value ) + "\"";
		}

		public static void AppendWireLine( ref string buffer, int inNodeId, int inPortId, int outNodeId, int outPortId )
		{
			buffer += "{\"wire\":[" + inNodeId.ToString( CultureInfo.InvariantCulture ) +
				"," + inPortId.ToString( CultureInfo.InvariantCulture ) +
				"," + outNodeId.ToString( CultureInfo.InvariantCulture ) +
				"," + outPortId.ToString( CultureInfo.InvariantCulture ) + "]}" + IOUtils.LINE_TERMINATOR;
		}

		// Single entry point for turning one metadata instruction line into the parameter array
		// the positional readers expect. JSON lines are unpacked into the legacy layout
		// ( [ "Node", type, id, "x,y", params... ] / [ "WireConnection", ids... ] ); anything
		// else falls back to the legacy ';' split.
		public static string[] SplitInstruction( string instruction )
		{
			LastInstructionFromJson = false;
			if ( string.IsNullOrEmpty( instruction ) )
			{
				return new string[] { string.Empty };
			}

			int position = 0;
			while ( position < instruction.Length && char.IsWhiteSpace( instruction[ position ] ) )
			{
				position++;
			}

			if ( position >= instruction.Length || instruction[ position ] != '{' )
			{
				return instruction.Split( IOUtils.FIELD_SEPARATOR );
			}

			LastInstructionFromJson = true;
			try
			{
				if ( ParseValue( instruction, ref position ) is Dictionary<string, object> fields )
				{
					if ( fields.TryGetValue( "wire", out object wire ) && wire is List<object> wireIds && wireIds.Count == 4 )
					{
						return new string[]
						{
							IOUtils.WireConnectionParam,
							wireIds[ 0 ].ToString(),
							wireIds[ 1 ].ToString(),
							wireIds[ 2 ].ToString(),
							wireIds[ 3 ].ToString()
						};
					}

					if ( fields.TryGetValue( "type", out object nodeType ) &&
						fields.TryGetValue( "id", out object uniqueId ) &&
						fields.TryGetValue( "pos", out object pos ) && pos is List<object> posCoords && posCoords.Count == 2 )
					{
						List<object> nodeParams = ( fields.TryGetValue( "params", out object p ) && p is List<object> paramList ) ?
							paramList : new List<object>();

						string[] parameters = new string[ 4 + nodeParams.Count ];
						parameters[ 0 ] = IOUtils.NodeParam;
						parameters[ 1 ] = nodeType.ToString();
						parameters[ 2 ] = uniqueId.ToString();
						parameters[ 3 ] = posCoords[ 0 ].ToString() + IOUtils.VECTOR_SEPARATOR + posCoords[ 1 ].ToString();
						for ( int i = 0; i < nodeParams.Count; i++ )
						{
							parameters[ 4 + i ] = nodeParams[ i ].ToString();
						}
						return parameters;
					}
				}
			}
			catch ( Exception e )
			{
				Debug.LogWarning( "[ASE] Failed to parse graph metadata line: " + instruction + "\n" + e.Message );
			}
			return new string[] { string.Empty };
		}

		// JSON strings may legally contain anything once escaped; '/' is additionally escaped when
		// preceded by '*' so user text can never terminate the /*ASEBEGIN comment block early.
		public static string EscapeString( string value )
		{
			if ( string.IsNullOrEmpty( value ) )
			{
				return string.Empty;
			}

			StringBuilder builder = null;
			int flushedUpTo = 0;
			for ( int i = 0; i < value.Length; i++ )
			{
				char current = value[ i ];
				string escaped = null;
				switch ( current )
				{
					case '"': escaped = "\\\""; break;
					case '\\': escaped = "\\\\"; break;
					case '\n': escaped = "\\n"; break;
					case '\r': escaped = "\\r"; break;
					case '\t': escaped = "\\t"; break;
					case '/':
					{
						if ( i > 0 && value[ i - 1 ] == '*' )
						{
							escaped = "\\/";
						}
					}
					break;
					default:
					{
						if ( current < ' ' )
						{
							escaped = "\\u" + ( ( int )current ).ToString( "x4" );
						}
					}
					break;
				}

				if ( escaped != null )
				{
					if ( builder == null )
					{
						builder = new StringBuilder( value.Length + 8 );
					}
					builder.Append( value, flushedUpTo, i - flushedUpTo );
					builder.Append( escaped );
					flushedUpTo = i + 1;
				}
			}

			if ( builder == null )
			{
				return value;
			}

			builder.Append( value, flushedUpTo, value.Length - flushedUpTo );
			return builder.ToString();
		}

		// Minimal JSON reader covering what the format emits: objects, arrays and strings, with
		// numbers/booleans/null captured as their raw token text since the positional readers
		// consume every parameter as a string anyway. Unknown keys are parsed and ignored so
		// future additions don't break older readers.
		private static object ParseValue( string text, ref int position )
		{
			SkipWhitespace( text, ref position );
			if ( position >= text.Length )
			{
				throw new FormatException( "Unexpected end of data" );
			}

			char current = text[ position ];
			if ( current == '{' )
			{
				return ParseObject( text, ref position );
			}
			if ( current == '[' )
			{
				return ParseArray( text, ref position );
			}
			if ( current == '"' )
			{
				return ParseString( text, ref position );
			}
			return ParseLiteral( text, ref position );
		}

		private static Dictionary<string, object> ParseObject( string text, ref int position )
		{
			Dictionary<string, object> fields = new Dictionary<string, object>();
			position++; // consume '{'
			SkipWhitespace( text, ref position );
			if ( position < text.Length && text[ position ] == '}' )
			{
				position++;
				return fields;
			}

			while ( true )
			{
				SkipWhitespace( text, ref position );
				string key = ParseString( text, ref position );
				SkipWhitespace( text, ref position );
				Expect( text, ref position, ':' );
				fields[ key ] = ParseValue( text, ref position );
				SkipWhitespace( text, ref position );
				if ( position >= text.Length )
				{
					throw new FormatException( "Unterminated object" );
				}
				if ( text[ position ] == ',' )
				{
					position++;
					continue;
				}
				Expect( text, ref position, '}' );
				return fields;
			}
		}

		private static List<object> ParseArray( string text, ref int position )
		{
			List<object> items = new List<object>();
			position++; // consume '['
			SkipWhitespace( text, ref position );
			if ( position < text.Length && text[ position ] == ']' )
			{
				position++;
				return items;
			}

			while ( true )
			{
				items.Add( ParseValue( text, ref position ) );
				SkipWhitespace( text, ref position );
				if ( position >= text.Length )
				{
					throw new FormatException( "Unterminated array" );
				}
				if ( text[ position ] == ',' )
				{
					position++;
					continue;
				}
				Expect( text, ref position, ']' );
				return items;
			}
		}

		private static string ParseString( string text, ref int position )
		{
			Expect( text, ref position, '"' );
			StringBuilder builder = new StringBuilder();
			while ( position < text.Length )
			{
				char current = text[ position++ ];
				if ( current == '"' )
				{
					return builder.ToString();
				}
				if ( current != '\\' )
				{
					builder.Append( current );
					continue;
				}
				if ( position >= text.Length )
				{
					break;
				}
				char escape = text[ position++ ];
				switch ( escape )
				{
					case '"': builder.Append( '"' ); break;
					case '\\': builder.Append( '\\' ); break;
					case '/': builder.Append( '/' ); break;
					case 'b': builder.Append( '\b' ); break;
					case 'f': builder.Append( '\f' ); break;
					case 'n': builder.Append( '\n' ); break;
					case 'r': builder.Append( '\r' ); break;
					case 't': builder.Append( '\t' ); break;
					case 'u':
					{
						if ( position + 4 > text.Length )
						{
							throw new FormatException( "Truncated unicode escape" );
						}
						builder.Append( ( char )int.Parse( text.Substring( position, 4 ), NumberStyles.HexNumber, CultureInfo.InvariantCulture ) );
						position += 4;
					}
					break;
					default: throw new FormatException( "Invalid escape '\\" + escape + "'" );
				}
			}
			throw new FormatException( "Unterminated string" );
		}

		private static string ParseLiteral( string text, ref int position )
		{
			int start = position;
			while ( position < text.Length )
			{
				char current = text[ position ];
				if ( current == ',' || current == '}' || current == ']' || char.IsWhiteSpace( current ) )
				{
					break;
				}
				position++;
			}
			if ( position == start )
			{
				throw new FormatException( "Unexpected character '" + text[ position ] + "'" );
			}
			return text.Substring( start, position - start );
		}

		private static void SkipWhitespace( string text, ref int position )
		{
			while ( position < text.Length && char.IsWhiteSpace( text[ position ] ) )
			{
				position++;
			}
		}

		private static void Expect( string text, ref int position, char expected )
		{
			if ( position >= text.Length || text[ position ] != expected )
			{
				throw new FormatException( "Expected '" + expected + "' at position " + position );
			}
			position++;
		}
	}
}

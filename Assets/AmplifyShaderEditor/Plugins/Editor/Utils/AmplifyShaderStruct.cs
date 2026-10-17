// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	// Whether a Struct Create node owns the definition it carries or borrows one.
	public enum StructOwnership
	{
		Local,
		Reference
	}

	// One field of a struct definition.
	[Serializable]
	public class StructFieldData
	{
		public string Name = string.Empty;

		public WirePortDataType DataType = WirePortDataType.FLOAT;

		// @diogo: spells the type when the wire type cannot. Empty for every built-in, and the slot a
		// nested struct fills later without changing the serialized shape
		public string TypeName = string.Empty;

		// Survives renames, retypes and reorders, which is how a Struct node's ports find their field
		// again once the definition was edited with the graph closed. Zero on data saved before ids.
		public int Id = 0;
	}

	[NoAutoStaticsCleanup]
	public sealed class StructFieldMask
	{
		public static readonly StructFieldMask All = new StructFieldMask( true, null );
		public static readonly StructFieldMask None = new StructFieldMask( false, null );

		private readonly bool m_all;
		private readonly HashSet<int> m_fields;

		private StructFieldMask( bool all, HashSet<int> fields )
		{
			m_all = all;
			m_fields = fields;
		}

		public static StructFieldMask Field( int index )
		{
			return new StructFieldMask( false, new HashSet<int> { index } );
		}

		public bool IsAll { get { return m_all; } }

		public bool IsNone { get { return !m_all && ( m_fields == null || m_fields.Count == 0 ); } }

		public bool Contains( int index )
		{
			return m_all || ( m_fields != null && m_fields.Contains( index ) );
		}

		public bool Covers( StructFieldMask other )
		{
			return IsAll || other.IsNone || ( !other.IsAll && m_fields != null && other.m_fields.IsSubsetOf( m_fields ) );
		}

		public StructFieldMask Union( StructFieldMask other )
		{
			if ( IsAll || other.IsAll )
			{
				return All;
			}

			if ( Covers( other ) )
			{
				return this;
			}

			if ( other.Covers( this ) )
			{
				return other;
			}

			HashSet<int> union = new HashSet<int>( m_fields );
			union.UnionWith( other.m_fields );
			return new StructFieldMask( false, union );
		}
	}

	// A struct type: its name, its fields, and whether ASE is the one declaring it. Shared by the
	// definition kept inline on a Struct Create node and the one kept in an Amplify Shader Struct asset.
	[Serializable]
	[NoAutoStaticsCleanup]
	public class StructDefinitionData
	{
		// The tint a definition starts from, and the one a struct shows when its definition cannot be found.
		public static readonly Color DefaultTint = new Color( 0.0f, 0.75f, 1.0f, 1.0f );

		[SerializeField]
		private string m_typeName = string.Empty;

		// @diogo: Fetch means the type already exists in an include, so the fields only drive ports and
		// nothing is emitted - the same meaning VariableMode carries on a property
		[SerializeField]
		private VariableMode m_declarationMode = VariableMode.Create;

		[SerializeField]
		private List<StructFieldData> m_fields = new List<StructFieldData>();

		[SerializeField]
		private Color m_tint = DefaultTint;

		// What the user called this type: whatever they typed, or whatever the asset is named. Kept
		// verbatim so it always reads back the way it was written.
		public string TypeName { get { return m_typeName; } set { m_typeName = value; } }

		// @diogo: the same name made legal for HLSL. Everything the shader sees uses this - the
		// declaration, the wire's identity, the registry key - and nothing stores it, so the name the
		// user owns and the name the compiler needs can never drift apart
		public string CodeName { get { return CleanTypeName( m_typeName ); } }

		public VariableMode DeclarationMode { get { return m_declarationMode; } set { m_declarationMode = value; } }

		public List<StructFieldData> Fields { get { return m_fields; } }

		// What the type is drawn with: the header of a Struct node carrying it, and every wire it travels on.
		public Color Tint { get { return m_tint; } set { m_tint = value; } }

		// The types a field may take, and the labels shown for them. Shared so the node's editor and the
		// asset's inspector cannot drift apart; the two arrays are index-matched.
		public static readonly WirePortDataType[] AvailableFieldTypes =
		{
			WirePortDataType.INT,
			WirePortDataType.FLOAT,
			WirePortDataType.FLOAT2,
			WirePortDataType.FLOAT3,
			WirePortDataType.FLOAT4,
			WirePortDataType.COLOR,
			WirePortDataType.FLOAT2x2,
			WirePortDataType.FLOAT3x3,
			WirePortDataType.FLOAT4x4
		};

		// @diogo: named after the property nodes that produce these values rather than after the wire
		// type, so the list reads the same as the palette
		public static readonly string[] FieldTypeLabels =
		{
			"Integer",
			"Float",
			"Vector2",
			"Vector3",
			"Vector4",
			"Color",
			"Matrix 2x2",
			"Matrix 3x3",
			"Matrix 4x4"
		};

		private const string FallbackTypeName = "Struct";

		// Turns arbitrary text into something HLSL will accept as a type name. File names are far more
		// permissive than identifiers, so an asset-named struct has to come through here.
		public static string CleanTypeName( string name )
		{
			if ( string.IsNullOrEmpty( name ) )
			{
				return FallbackTypeName;
			}

			// @diogo: whitespace closes up rather than becoming an underscore, so "TVE Visual Data" reads
			// as one identifier; every other stray character is kept as a separator instead
			System.Text.StringBuilder builder = new System.Text.StringBuilder( name.Length + 1 );
			for ( int i = 0; i < name.Length; i++ )
			{
				char c = name[ i ];
				if ( ( c >= 'a' && c <= 'z' ) || ( c >= 'A' && c <= 'Z' ) || ( c >= '0' && c <= '9' ) || c == '_' )
				{
					builder.Append( c );
				}
				else if ( !char.IsWhiteSpace( c ) && ( builder.Length == 0 || builder[ builder.Length - 1 ] != '_' ) )
				{
					builder.Append( '_' );
				}
			}

			while ( builder.Length > 0 && builder[ builder.Length - 1 ] == '_' )
			{
				builder.Length--;
			}

			if ( builder.Length == 0 )
			{
				return FallbackTypeName;
			}

			// @diogo: an identifier cannot open with a digit, and dropping it would collide two names that
			// differ only by their leading numbers
			if ( builder[ 0 ] >= '0' && builder[ 0 ] <= '9' )
			{
				builder.Insert( 0, '_' );
			}

			return builder.ToString();
		}

		// The name a duplicated field starts from: the source name with its trailing number dropped, so
		// the copy takes the next free index instead of stacking digits onto the one it came from.
		public static string FieldNameBase( string name )
		{
			int length = name.Length;
			while ( length > 0 && char.IsDigit( name[ length - 1 ] ) )
			{
				length--;
			}
			return ( length > 0 ) ? name.Substring( 0, length ) : name;
		}

		// Takes another definition's contents, fields included, so one can move between a node and an
		// asset without either ending up sharing the other's list.
		public void CopyFrom( StructDefinitionData other )
		{
			m_typeName = other.TypeName;
			m_declarationMode = other.DeclarationMode;
			m_tint = other.Tint;

			m_fields.Clear();
			for ( int i = 0; i < other.Fields.Count; i++ )
			{
				m_fields.Add( new StructFieldData()
				{
					Name = other.Fields[ i ].Name,
					Id = other.Fields[ i ].Id,
					DataType = other.Fields[ i ].DataType,
					TypeName = other.Fields[ i ].TypeName
				} );
			}
		}

		// Moves a field the way a ReorderableList drag does, shifting the ones in between.
		public void MoveField( int from, int to )
		{
			StructFieldData field = m_fields[ from ];
			m_fields.RemoveAt( from );
			m_fields.Insert( to, field );
		}

		// An id no other field of this definition holds. Random rather than counted, so a node repointed
		// from one asset to another cannot mistake a stranger's field for its own.
		public int NewFieldId()
		{
			int id;
			do
			{
				id = Guid.NewGuid().GetHashCode();
			}
			while ( id == 0 || m_fields.Exists( f => f.Id == id ) );
			return id;
		}

		// Gives an id to every field still without one. Returns whether any was handed out.
		public bool EnsureFieldIds()
		{
			bool changed = false;
			for ( int i = 0; i < m_fields.Count; i++ )
			{
				if ( m_fields[ i ].Id == 0 )
				{
					m_fields[ i ].Id = NewFieldId();
					changed = true;
				}
			}
			return changed;
		}

		// The HLSL type a field declares, which for a built-in comes from the wire type and otherwise
		// from the name the field carries.
		public static string FieldTypeName( StructFieldData field, PrecisionType precision )
		{
			return string.IsNullOrEmpty( field.TypeName ) ? UIUtils.PrecisionWirePortToCgType( precision, field.DataType ) : field.TypeName;
		}

		// The "struct Name { ... };" text. Empty in Fetch mode, where the type is not ASE's to declare.
		public string BuildDeclaration( PrecisionType precision )
		{
			string declaration = string.Empty;

			if ( m_declarationMode == VariableMode.Create && !string.IsNullOrEmpty( m_typeName ) )
			{
				declaration = "struct " + CodeName + "\n{\n";
				for ( int i = 0; i < m_fields.Count; i++ )
				{
					declaration += "\t" + FieldTypeName( m_fields[ i ], precision ) + " " + m_fields[ i ].Name + ";\n";
				}
				declaration += "};";
			}

			return declaration;
		}
	}

	// A struct definition shared across graphs. Struct Create either owns its definition inline or
	// references one of these; the asset is what lets a shader function and its caller agree on a type
	// without restating the field list, since registries never cross a function boundary.
	[CreateAssetMenu( menuName = "Amplify Shader Struct", fileName = "New Shader Struct", order = 86 )]
	public class AmplifyShaderStruct : ScriptableObject
	{
		[SerializeField]
		private StructDefinitionData m_definition = new StructDefinitionData();

		private string m_syncedFromName = null;

		// The definition this asset carries. Its type name is not stored data - it is derived from the
		// asset's own file name, the way a shader function takes its name from its file.
		public StructDefinitionData Definition
		{
			get
			{
				// @diogo: taken verbatim, the same as a name typed on a node - the asset may be called
				// anything, and CodeName is what makes it legal when the shader needs it
				if ( m_syncedFromName != name )
				{
					m_syncedFromName = name;
					m_definition.TypeName = name;
				}

				return m_definition;
			}
		}
	}

	// Every named AmplifyShaderStruct asset in the project, for the nodes that offer one to pick.
	[NoAutoStaticsCleanup]
	public static class AmplifyShaderStructCache
	{
		private static List<string> s_assetNames = new List<string>();
		private static List<string> s_guids = new List<string>();
		private static bool s_dirty = true;

		// @diogo: an inspector redraws every frame while its node is selected, so the project scan is
		// cached and only redone when Unity says the project actually changed
		[InitializeOnLoadMethod]
		private static void HookProjectChanges()
		{
			EditorApplication.projectChanged += () => s_dirty = true;
		}

		// What each asset is called, which is also its type name - the compliant form is derived from it
		// by StructDefinitionData.CodeName and is never stored.
		public static List<string> AssetNames { get { Refresh(); return s_assetNames; } }

		public static List<string> GUIDs { get { Refresh(); return s_guids; } }

		private static void Refresh()
		{
			if ( !s_dirty )
			{
				return;
			}

			s_dirty = false;
			s_assetNames.Clear();
			s_guids.Clear();

			string[] guids = AssetDatabase.FindAssets( "t:AmplifyShaderStruct" );
			for ( int i = 0; i < guids.Length; i++ )
			{
				AmplifyShaderStruct asset = AssetDatabase.LoadAssetAtPath<AmplifyShaderStruct>( AssetDatabase.GUIDToAssetPath( guids[ i ] ) );
				if ( asset != null )
				{
					s_assetNames.Add( asset.name );
					s_guids.Add( guids[ i ] );
				}
			}
		}
	}
}

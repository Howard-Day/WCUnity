// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;

namespace AmplifyShaderEditor
{
	public class ContextMenuItem
	{
		private const string PALETTE_NAME_MOD_STR = "   ";
		
		private string m_paletteName;
		private string m_name;
		private string m_tags;
		private string m_category;
		private string m_description;
		private System.Type m_type;
		private GUIContent m_guiContent;
		private string m_nameWithShortcut;
		private AmplifyShaderFunction m_function;
		private NodeAttributes m_nodeAttributes;
		private int m_usageCount;
		private int m_matchRank;
		private int m_matchLength;
		private string m_nameLower;
		private string[] m_tagList;

		public ContextMenuItem( NodeAttributes nodeAttributes, System.Type type, string name, string tags, string category, string description, AmplifyShaderFunction function, KeyCode shortcut )
		{
			m_nodeAttributes = nodeAttributes;
			m_name = name;
			m_nameLower = name.ToLower();
			m_tagList = string.IsNullOrEmpty( tags ) ? Array.Empty<string>() : tags.ToLower().Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );
			m_tags = name + ( string.IsNullOrEmpty( tags ) ? "" : " " + tags );
			m_tags = m_tags.ToLower();
			m_nameWithShortcut = shortcut != KeyCode.None ? ( name + " [ " + UIUtils.KeyCodeToString( shortcut ) + " ]" ) : name;
			m_paletteName = PALETTE_NAME_MOD_STR + m_name;
			m_type = type;
			m_category = category;
			m_description = description;
			m_function = function;
			m_guiContent = new GUIContent( m_nameWithShortcut, m_description );
		}

		public bool NameStartsWith( string lowercaseSearch )
		{
			return m_nameLower.StartsWith( lowercaseSearch, StringComparison.Ordinal );
		}

		// @diogo: length of the shortest tag opening with the searched text; -1 when no tag does
		public int GetTagPrefixMatchLength( string lowercaseSearch )
		{
			int matchLength = -1;
			for ( int i = 0; i < m_tagList.Length; i++ )
			{
				if ( m_tagList[ i ].StartsWith( lowercaseSearch, StringComparison.Ordinal ) && ( matchLength < 0 || m_tagList[ i ].Length < matchLength ) )
				{
					matchLength = m_tagList[ i ].Length;
				}
			}
			return matchLength;
		}

		public int CompareTo( ContextMenuItem item , bool useWeights )
		{
			// @diogo: names opening with the searched text rank above names matching it further in
			if ( m_matchRank != item.MatchRank )
			{
				return m_matchRank.CompareTo( item.MatchRank );
			}

			// @diogo: among names matching the same way, the shortest is the closest to what was typed
			if ( m_matchLength != item.MatchLength )
			{
				return m_matchLength.CompareTo( item.MatchLength );
			}

			// @diogo: nodes already present in the open graph rank first, most used before least
			if ( m_usageCount != item.UsageCount )
			{
				return item.UsageCount.CompareTo( m_usageCount );
			}

			if ( useWeights && NodeAttributes.SortOrderPriority > -1 && item.NodeAttributes.SortOrderPriority > -1 )
			{
				if ( NodeAttributes.SortOrderPriority > item.NodeAttributes.SortOrderPriority )
				{
					return 1;
				}
				else if ( NodeAttributes.SortOrderPriority == item.NodeAttributes.SortOrderPriority )
				{
					return m_name.CompareTo( item.Name );
				}
				else
				{
					return -1;
				}
			}
			return m_name.CompareTo( item.Name );
		}

		public string PaletteName { get { return m_paletteName; } }
		public string Name { get { return m_name; } }
		public string Tags { get { return m_tags; } }
		public string NameWithShortcut { get { return m_nameWithShortcut; } }
		public string Category { get { return m_category; } }
		public string Description { get { return m_description; } }
		public AmplifyShaderFunction Function { get { return m_function; } }
		public System.Type NodeType { get { return m_type; } }
		public GUIContent ItemUIContent { get { return m_guiContent; } }
		public NodeAttributes NodeAttributes { get { return m_nodeAttributes; } }
		public int UsageCount { get { return m_usageCount; } set { m_usageCount = value; } }
		public int MatchRank { get { return m_matchRank; } set { m_matchRank = value; } }
		public int MatchLength { get { return m_matchLength; } set { m_matchLength = value; } }
		public string NameLower { get { return m_nameLower; } }

		public override string ToString()
		{
			return m_name + ":" + m_category + ":" + m_description;
		}

		public void Destroy()
		{
			m_guiContent = null;
			m_nodeAttributes = null;
		}
	}
}

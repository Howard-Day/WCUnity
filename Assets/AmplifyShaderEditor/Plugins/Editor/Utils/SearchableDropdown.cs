// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace AmplifyShaderEditor
{
	// @diogo: reusable searchable picker (issue #474) for dropdowns whose lists can grow large. Selection is
	// asynchronous (a separate popup window), so callbacks must snapshot draw-time state they depend on,
	// record undo before mutating, and call SetDirtyFromAsyncEdit after (no change-check sees them).
	public class SearchableDropdown : AdvancedDropdown
	{
		private const string NoneStr = "None";
		private const string DefaultTitleStr = "Options";
		private const float MinHeight = 240f;
		private const float MinWidth = 180f;
		private const float MaxWidth = 320f;
		// @diogo: room for the row icon/indent, submenu arrow and scrollbar around the measured text
		private const float ItemPadding = 45f;
		private const char PathSeparator = '/';

		private class IndexedItem : AdvancedDropdownItem
		{
			public readonly int Index;

			public IndexedItem( string name, int index ) : base( name )
			{
				Index = index;
			}
		}

		private readonly string m_title;
		private readonly string[] m_options;
		private readonly Action<int> m_onSelected;
		private readonly int[] m_separatorIdxs;

		private SearchableDropdown( AdvancedDropdownState state, string title, string[] options, Action<int> onSelected, float minWidth, int[] separatorIdxs ) : base( state )
		{
			m_title = string.IsNullOrEmpty( title ) ? DefaultTitleStr : title;
			m_options = options;
			m_onSelected = onSelected;
			m_separatorIdxs = separatorIdxs;
			minimumSize = new Vector2( minWidth, MinHeight );
		}

		// @diogo: '/' nests an option under submenus, matching what the native popup did before
		protected override AdvancedDropdownItem BuildRoot()
		{
			AdvancedDropdownItem root = new AdvancedDropdownItem( m_title );
			Dictionary<string, AdvancedDropdownItem> groups = new Dictionary<string, AdvancedDropdownItem>();
			for( int i = 0; i < m_options.Length; i++ )
			{
				string option = m_options[ i ];
				int split = option.LastIndexOf( PathSeparator );
				AdvancedDropdownItem parent = ( split < 0 ) ? root : GetOrCreateGroup( root, groups, option.Substring( 0, split ) );
				if( i > 0 && m_separatorIdxs != null && Array.IndexOf( m_separatorIdxs, i ) > -1 )
				{
					parent.AddSeparator();
				}
				parent.AddChild( new IndexedItem( option.Substring( split + 1 ), i ) );
			}
			return root;
		}

		private static AdvancedDropdownItem GetOrCreateGroup( AdvancedDropdownItem root, Dictionary<string, AdvancedDropdownItem> groups, string path )
		{
			AdvancedDropdownItem group;
			if ( !groups.TryGetValue( path, out group ) )
			{
				int split = path.LastIndexOf( PathSeparator );
				AdvancedDropdownItem parent = ( split < 0 ) ? root : GetOrCreateGroup( root, groups, path.Substring( 0, split ) );
				group = new AdvancedDropdownItem( path.Substring( split + 1 ) );
				parent.AddChild( group );
				groups.Add( path, group );
			}
			return group;
		}

		protected override void ItemSelected( AdvancedDropdownItem item )
		{
			if( item is IndexedItem indexed && m_onSelected != null )
			{
				m_onSelected( indexed.Index );
			}
		}

		// @diogo: layout field (prefix label + dropdown button); opens the searchable list on click.
		// each separator index draws a divider above that option, to split the list into groups
		public static void Draw( string label, int current, string[] options, Action<int> onSelected, params int[] separatorIdxs )
		{
			Rect rect = EditorGUILayout.GetControlRect( true, EditorGUIUtility.singleLineHeight );
			Rect fieldRect = string.IsNullOrEmpty( label ) ? rect : EditorGUI.PrefixLabel( rect, new GUIContent( label ) );
			Draw( fieldRect, label, current, options, onSelected, separatorIdxs );
		}

		// @diogo: explicit-rect field for canvas / inline use
		public static void Draw( Rect fieldRect, string title, int current, string[] options, Action<int> onSelected, params int[] separatorIdxs )
		{
			string currentLabel = ( options != null && current >= 0 && current < options.Length ) ? options[ current ] : NoneStr;
			if( EditorGUI.DropdownButton( fieldRect, new GUIContent( currentLabel ), FocusType.Keyboard, EditorStyles.popup ) )
			{
				if( options != null && options.Length > 0 )
				{
					SearchableDropdown dropdown = new SearchableDropdown( new AdvancedDropdownState(), title, options, onSelected, 0, separatorIdxs );
					dropdown.Show( fieldRect );
				}
			}
		}

		// @diogo: opens the searchable list immediately anchored to a rect (no button); for in-canvas triggers,
		// where the anchor is a tiny zoomed chevron, so a minimum width keeps the popup usable
		public static void Open( Rect anchor, string[] options, Action<int> onSelected )
		{
			if( options != null && options.Length > 0 )
			{
				// @diogo: AdvancedDropdown never measures its content, so grow the floor to the longest visible
				// entry ourselves; rows display path segments, so measure those rather than the full option
				float width = MinWidth;
				for( int i = 0; i < options.Length; i++ )
				{
					string[] segments = options[ i ].Split( PathSeparator );
					for( int j = 0; j < segments.Length; j++ )
					{
						width = Mathf.Max( width, EditorStyles.label.CalcSize( new GUIContent( segments[ j ] ) ).x + ItemPadding );
					}
				}
				width = Mathf.Min( width, MaxWidth );

				// @diogo: showing mid-event fights the canvas for mouse capture/focus and the popup dies within
				// a frame; convert to screen space while the GUI context is live and open after the event ends
				Rect screenAnchor = GUIUtility.GUIToScreenRect( anchor );
				EditorApplication.delayCall += () =>
				{
					// @diogo: Show runs GUIToScreenRect on its rect again, which off-GUI still adds the focused
					// view's offset; measure and cancel it so the popup lands on the pre-converted anchor
					Rect stray = GUIUtility.GUIToScreenRect( new Rect( 0f, 0f, 0f, 0f ) );
					Rect finalAnchor = new Rect( screenAnchor.x - stray.x, screenAnchor.y - stray.y, screenAnchor.width, screenAnchor.height );
					SearchableDropdown dropdown = new SearchableDropdown( new AdvancedDropdownState(), DefaultTitleStr, options, onSelected, width, null );
					dropdown.Show( finalAnchor );
				};
			}
		}
	}
}

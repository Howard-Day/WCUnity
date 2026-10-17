// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	[NoAutoStaticsCleanup]
	public class NodeUtils
	{
		private const float HelpBoxIconWidth = 36;        // icon column inside the box
		private const float ParamsWindowWidthSlack = 36;  // scroll view margins + scrollbar allowance
		private const float PrefixPaddingRight = 2;       // EditorGUI's own gap between label and value

		private static GUIStyle m_helpBoxTextStyle = null;
		private static GUIContent m_tempTextContent = new GUIContent();
		private static int m_cachedIndentLevel = 0;

		public static float ParametersWindowWidth
		{
			get
			{
				return ( UIUtils.CurrentWindow != null && UIUtils.CurrentWindow.ParametersWindow != null ) ?
					UIUtils.CurrentWindow.ParametersWindow.TransformedArea.width : EditorGUIUtility.currentViewWidth;
			}
		}

		// Measures the height "text" needs when wrapped inside the node parameters panel. Word-wrapped
		// controls drawn in there must be given an explicit height; otherwise their reserved height
		// changes with scrollbar visibility, which can lock the scrollbar on even when content fits
		public static float MeasureTextHeight( string text, GUIStyle style, float occupiedWidth = 0 )
		{
			m_tempTextContent.text = text;
			return style.CalcHeight( m_tempTextContent, ParametersWindowWidth - ParamsWindowWidthSlack - occupiedWidth );
		}

		// Stable replacement for EditorGUILayout.HelpBox inside the node parameters scroll view ( see
		// MeasureTextHeight ); also draws the icon separately since including it in the content makes
		// CalcHeight undershoot
		public static void DrawHelpBox( string text, MessageType type )
		{
			if ( m_helpBoxTextStyle == null )
			{
				// Built from scratch instead of copying EditorStyles.helpBox; copies keep drawing
				// the box background through HiDPI scaled backgrounds, even when set to none
				m_helpBoxTextStyle = new GUIStyle();
				m_helpBoxTextStyle.fontSize = EditorStyles.helpBox.fontSize;
				m_helpBoxTextStyle.wordWrap = true;
				m_helpBoxTextStyle.padding = EditorStyles.helpBox.padding;
				m_helpBoxTextStyle.normal.textColor = EditorStyles.helpBox.normal.textColor;
			}

			float height = Mathf.Max( MeasureTextHeight( text, m_helpBoxTextStyle, HelpBoxIconWidth ), 40 );
			Rect rect = EditorGUILayout.GetControlRect( false, height );

			GUI.Label( rect, string.Empty, EditorStyles.helpBox );

			Texture icon = null;
			switch ( type )
			{
				case MessageType.Info: icon = EditorGUIUtility.IconContent( "console.infoicon" ).image; break;
				case MessageType.Warning: icon = EditorGUIUtility.IconContent( "console.warnicon" ).image; break;
				case MessageType.Error: icon = EditorGUIUtility.IconContent( "console.erroricon" ).image; break;
			}
			if ( icon != null )
			{
				Rect iconRect = new Rect( rect.x + 2, rect.y + 4, 32, 32 );
				GUI.DrawTexture( iconRect, icon, ScaleMode.ScaleToFit );
			}

			m_tempTextContent.text = text;
			Rect textRect = new Rect( rect.x + HelpBoxIconWidth, rect.y, rect.width - HelpBoxIconWidth, rect.height );
			GUI.Label( textRect, m_tempTextContent, m_helpBoxTextStyle );
		}

		// A labelled row whose value is fixed, drawn as a disabled control under an enabled label - a
		// greyed label reads as "this row does not apply", when in fact only its value is not editable
		public static void DrawDisabledValue( string label, string value )
		{
			Rect rect = BeginDisabledValue( label, EditorStyles.textField );
			EditorGUI.TextField( rect, value );
			EndDisabledValue();
		}

		public static void DrawDisabledEnum( string label, Enum value )
		{
			Rect rect = BeginDisabledValue( label, EditorStyles.popup );
			EditorGUI.EnumPopup( rect, value );
			EndDisabledValue();
		}

		// A labelled row whose value column the caller fills itself, for rows holding something other than
		// a single control. Returns the value half, already past the prefix.
		public static Rect DrawPrefixedRow( GUIContent label, GUIStyle style )
		{
			Rect rect = EditorGUILayout.GetControlRect( true, EditorGUIUtility.singleLineHeight, style );

			// @diogo: drawn into the label half rather than the whole row, so a tooltip on the label stays
			// over the label - the value column is left for whatever the caller puts there to answer for
			Rect labelRect = rect;
			labelRect.width = EditorGUIUtility.labelWidth;
			EditorGUI.LabelField( labelRect, label );

			rect.xMin += EditorGUIUtility.labelWidth + PrefixPaddingRight;
			return rect;
		}

		// @diogo: the row is measured with the style its editable counterpart uses and the value placed
		// on the column EditorGUI.PrefixLabel returns; the indent goes with the label it already moved,
		// or the rect overloads below - which indent whatever they are given - would apply it twice
		private static Rect BeginDisabledValue( string label, GUIStyle style )
		{
			Rect rect = EditorGUILayout.GetControlRect( true, EditorGUIUtility.singleLineHeight, style );

			GUI.enabled = true;
			EditorGUI.LabelField( rect, label );
			GUI.enabled = false;

			m_cachedIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			rect.xMin += EditorGUIUtility.labelWidth + PrefixPaddingRight;
			return rect;
		}

		private static void EndDisabledValue()
		{
			EditorGUI.indentLevel = m_cachedIndentLevel;
			GUI.enabled = true;
		}

		public delegate void DrawPropertySection();

		// Draws a foldout header toggle and returns its new state. Expanding or collapsing is view
		// state, so the toggle is kept out of the enclosing change checks and out of the undo stack.
		public static bool FoldoutToggle( bool foldoutValue, string sectionName, params GUILayoutOption[] options )
		{
			bool cachedChanged = GUI.changed;
			bool value = GUILayout.Toggle( foldoutValue, sectionName, UIUtils.MenuItemToggleStyle, options );
			GUI.changed = cachedChanged;
			return ( Event.current.button == Constants.FoldoutMouseId ) ? value : foldoutValue;
		}

		public static bool FoldoutToggle( bool foldoutValue, GUIContent sectionName, params GUILayoutOption[] options )
		{
			bool cachedChanged = GUI.changed;
			bool value = GUILayout.Toggle( foldoutValue, sectionName, UIUtils.MenuItemToggleStyle, options );
			GUI.changed = cachedChanged;
			return ( Event.current.button == Constants.FoldoutMouseId ) ? value : foldoutValue;
		}

		public static bool FoldoutToggle( bool foldoutValue, Rect rect, string sectionName )
		{
			bool cachedChanged = GUI.changed;
			bool value = GUI.Toggle( rect, foldoutValue, sectionName, UIUtils.MenuItemToggleStyle );
			GUI.changed = cachedChanged;
			return ( Event.current.button == Constants.FoldoutMouseId ) ? value : foldoutValue;
		}

		public static void DrawPropertyGroup( string sectionName, DrawPropertySection DrawSection )
		{
			Color cachedColor = GUI.color;
			GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, 0.5f );
			EditorGUILayout.BeginHorizontal( UIUtils.MenuItemToolbarStyle );
			GUI.color = cachedColor;

			GUILayout.Label( sectionName, UIUtils.MenuItemToggleStyle );

			EditorGUILayout.EndHorizontal();


			cachedColor = GUI.color;
			GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, ( EditorGUIUtility.isProSkin ? 0.5f : 0.25f ) );
			EditorGUILayout.BeginVertical( UIUtils.MenuItemBackgroundStyle );
			GUI.color = cachedColor;
			DrawSection();
			EditorGUILayout.Separator();
			EditorGUILayout.EndVertical();
		}


		public static void DrawNestedPropertyGroup( ref bool foldoutValue, string sectionName, DrawPropertySection DrawSection, int horizontalSpacing = 15 )
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space( horizontalSpacing );
				EditorGUILayout.BeginVertical( EditorStyles.helpBox );
				{
					Color cachedColor = GUI.color;
					GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, 0.5f );
					EditorGUILayout.BeginHorizontal();
					{
						GUI.color = cachedColor;
						foldoutValue = FoldoutToggle( foldoutValue, sectionName );
					}
					EditorGUILayout.EndHorizontal();
					EditorGUI.indentLevel--;
					if( foldoutValue )
					{
						cachedColor = GUI.color;
						GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, ( EditorGUIUtility.isProSkin ? 0.5f : 0.25f ) );
						{
							EditorGUILayout.BeginVertical( UIUtils.MenuItemBackgroundStyle );
							{
								GUI.color = cachedColor;
								DrawSection();
							}
							EditorGUILayout.EndVertical();
							EditorGUILayout.Separator();
						}
					}
					EditorGUI.indentLevel++;
				}
				EditorGUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
		}

		public static void DrawNestedPropertyGroup( ref bool foldoutValue, Rect rect, string sectionName, DrawPropertySection DrawSection, int horizontalSpacing = 15 )
		{
			var box = rect;
			box.height -= 2;
			GUI.Label( box, string.Empty, EditorStyles.helpBox );

			var tog = rect;
			tog.y -= ( tog.height - ( EditorGUIUtility.singleLineHeight + 5 ) ) * 0.5f;
			tog.xMin += 2;
			tog.xMax -= 2;
			tog.yMin += 2;
			foldoutValue = FoldoutToggle( foldoutValue, tog, sectionName );

			if( foldoutValue )
			{
				DrawSection();
			}
		}


		public static void DrawNestedPropertyGroup( ref bool foldoutValue, string sectionName, DrawPropertySection DrawSection, DrawPropertySection HeaderSection )
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space( 15 );
				EditorGUILayout.BeginVertical( EditorStyles.helpBox );
				Color cachedColor = GUI.color;
				GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, 0.5f );
				EditorGUILayout.BeginHorizontal();
				GUI.color = cachedColor;

				foldoutValue = FoldoutToggle( foldoutValue, sectionName );
				HeaderSection();
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
				if( foldoutValue )
				{
					cachedColor = GUI.color;
					GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, ( EditorGUIUtility.isProSkin ? 0.5f : 0.25f ) );
					EditorGUILayout.BeginVertical( UIUtils.MenuItemBackgroundStyle );
					GUI.color = cachedColor;
					DrawSection();
					EditorGUILayout.EndVertical();
					EditorGUILayout.Separator();
				}
				EditorGUI.indentLevel++;
				EditorGUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
		}

		public static void DrawNestedPropertyGroup( UndoParentNode owner, ref bool foldoutValue, ref bool enabledValue, string sectionName, DrawPropertySection DrawSection )
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space( 15 );
				EditorGUILayout.BeginVertical( EditorStyles.helpBox );
				Color cachedColor = GUI.color;
				GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, 0.5f );
				EditorGUILayout.BeginHorizontal();
				GUI.color = cachedColor;

				foldoutValue = FoldoutToggle( foldoutValue, sectionName );

				bool value = ( (object)owner != null ) ? owner.GUILayoutToggle( enabledValue, string.Empty,UIUtils.MenuItemEnableStyle, GUILayout.Width( 16 ) ) :
										GUILayout.Toggle( enabledValue, string.Empty, UIUtils.MenuItemEnableStyle, GUILayout.Width( 16 ) );
				if( Event.current.button == Constants.FoldoutMouseId )
				{
					enabledValue = value;
				}
				

				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
				if( foldoutValue )
				{
					cachedColor = GUI.color;
					GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, ( EditorGUIUtility.isProSkin ? 0.5f : 0.25f ) );
					EditorGUILayout.BeginVertical( UIUtils.MenuItemBackgroundStyle );
					GUI.color = cachedColor;
					DrawSection();
					EditorGUILayout.EndVertical();
					EditorGUILayout.Separator();
				}
				EditorGUI.indentLevel++;
				EditorGUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
		}


		public static void DrawPropertyGroup( ref bool foldoutValue, string sectionName, DrawPropertySection DrawSection )
		{
			Color cachedColor = GUI.color;
			GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, 0.5f );
			EditorGUILayout.BeginHorizontal( UIUtils.MenuItemToolbarStyle );
			GUI.color = cachedColor;

			foldoutValue = FoldoutToggle( foldoutValue, sectionName );
			EditorGUILayout.EndHorizontal();

			if( foldoutValue )
			{
				cachedColor = GUI.color;
				GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, ( EditorGUIUtility.isProSkin ? 0.5f : 0.25f ) );
				EditorGUILayout.BeginVertical( UIUtils.MenuItemBackgroundStyle );
				{
					GUI.color = cachedColor;
					EditorGUI.indentLevel++;
					DrawSection();
					EditorGUI.indentLevel--;
					EditorGUILayout.Separator();
				}
				EditorGUILayout.EndVertical();
			}
		}

		public static void DrawPropertyGroup( ref bool foldoutValue, string sectionName, DrawPropertySection DrawSection, DrawPropertySection HeaderSection )
		{
			Color cachedColor = GUI.color;
			GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, 0.5f );
			EditorGUILayout.BeginHorizontal( UIUtils.MenuItemToolbarStyle );
			GUI.color = cachedColor;

			foldoutValue = FoldoutToggle( foldoutValue, sectionName );
			HeaderSection();
			EditorGUILayout.EndHorizontal();

			if( foldoutValue )
			{
				cachedColor = GUI.color;
				GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, ( EditorGUIUtility.isProSkin ? 0.5f : 0.25f ) );
				EditorGUILayout.BeginVertical( UIUtils.MenuItemBackgroundStyle );
				{
					GUI.color = cachedColor;
					EditorGUI.indentLevel++;
					DrawSection();
					EditorGUI.indentLevel--;
					EditorGUILayout.Separator();
				}
				EditorGUILayout.EndVertical();
			}
		}


		public static bool DrawPropertyGroup( UndoParentNode owner, ref bool foldoutValue, ref bool enabledValue, string sectionName, DrawPropertySection DrawSection )
		{
			bool enableChanged = false;
			Color cachedColor = GUI.color;
			GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, 0.5f );
			EditorGUILayout.BeginHorizontal( UIUtils.MenuItemToolbarStyle );
			GUI.color = cachedColor;
			foldoutValue = FoldoutToggle( foldoutValue, sectionName, GUILayout.ExpandWidth( true ) );
			EditorGUI.BeginChangeCheck();
			bool value = ( (object)owner != null ) ? owner.EditorGUILayoutToggle( string.Empty, enabledValue, UIUtils.MenuItemEnableStyle, GUILayout.Width( 16 ) ) :
											EditorGUILayout.Toggle( string.Empty, enabledValue, UIUtils.MenuItemEnableStyle, GUILayout.Width( 16 ) );
			if( Event.current.button == Constants.FoldoutMouseId )
			{
				enabledValue = value;
			}
			if( EditorGUI.EndChangeCheck() )
			{
				enableChanged = true;
			}

			EditorGUILayout.EndHorizontal();

			if( foldoutValue )
			{
				cachedColor = GUI.color;
				GUI.color = new Color( cachedColor.r, cachedColor.g, cachedColor.b, ( EditorGUIUtility.isProSkin ? 0.5f : 0.25f ) );
				EditorGUILayout.BeginVertical( UIUtils.MenuItemBackgroundStyle );
				GUI.color = cachedColor;

				EditorGUILayout.Separator();
				EditorGUI.BeginDisabledGroup( !enabledValue );

				EditorGUI.indentLevel += 1;

				DrawSection();

				EditorGUI.indentLevel -= 1;
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.Separator();
				EditorGUILayout.EndVertical();
			}

			return enableChanged;
		}
	}
}

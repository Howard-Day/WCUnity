// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[System.Serializable]
	public class Toast
	{
		public MessageSeverity ItemType;
		public string ItemMessage;
		public double ItemTime;
		public int ItemOwnerId;
		public Toast( MessageSeverity itemType, string itemMessage, double itemTime,int itemOwnerId )
		{
			ItemType = itemType;
			ItemMessage = itemMessage;
			ItemTime = itemTime;
			ItemOwnerId = itemOwnerId;
		}
	}

	public class ConsoleLogWindow
	{
		public const int MAXWIDTH = 800;
		public const float FADETIME = 7;

		private const float ResizeBandSize = 5;
		private const float MinBoxWidth = 200;
		private const float MinBoxHeight = 66;
		private const float TopMargin = 44;

		private readonly GUIContent m_boxToggleContent = new GUIContent( "\u2261", "Toggle Message Box" );
		private readonly GUIContent m_clearContent = new GUIContent( "\u00D7", "Clear Messages" );

		protected AmplifyShaderEditorWindow m_parentWindow = null;

		// needs to be serialized
		private Vector2 m_currentScrollPos;

		private Rect m_boxArea = new Rect();
		private bool m_isResizing = false;
		private Vector2 m_resizeAxis = Vector2.zero;

		int lastCall = -1;
		
		public ConsoleLogWindow( AmplifyShaderEditorWindow parentWindow )
		{
			m_parentWindow = parentWindow;
		}

		public void AddMessage( MessageSeverity itemType, string itemMessage , int itemOwnerId )
		{
			var messages = m_parentWindow.Messages;

			// @diogo: a compile emits the same message once per node per pass and repeats interleave with
			// other nodes' messages, so the whole log is searched and the match reinserted as the newest
			for ( int i = 0; i < messages.Count; i++ )
			{
				Toast previous = messages[ i ];
				if ( previous.ItemType == itemType && previous.ItemOwnerId == itemOwnerId && previous.ItemMessage == itemMessage )
				{
					messages.RemoveAt( i );
					break;
				}
			}

			var toast = new Toast( itemType, itemMessage, Time.realtimeSinceStartup, itemOwnerId );
			messages.Insert( 0, toast );
			m_currentScrollPos.y = Mathf.Infinity;

			if( !m_parentWindow.MaximizeMessages )
				lastCall = Mathf.Max( (int)itemType, lastCall );
		}

		public bool IsInside( Vector2 position )
		{
			return m_parentWindow != null && m_parentWindow.MaximizeMessages && m_boxArea.Contains( position );
		}

		// @diogo: the box is anchored to the toggle button and grows left and up, so it resizes from its
		// left and top edges; grips sit astride the border, where the box has no content of its own.
		// Hit tested by hand instead of through GUI controls, which would eat the mouse up that ends
		// the drag and would add controls that come and go with the box
		private void HandleResize( Rect area, float leftBound, float topBound, Vector2 mousePosition )
		{
			Rect cornerGrip = new Rect( area.x - ResizeBandSize, area.y - ResizeBandSize, 2 * ResizeBandSize, 2 * ResizeBandSize );
			Rect leftGrip = new Rect( area.x - ResizeBandSize, cornerGrip.yMax, 2 * ResizeBandSize, area.yMax - cornerGrip.yMax );
			Rect topGrip = new Rect( cornerGrip.xMax, area.y - ResizeBandSize, area.xMax - cornerGrip.xMax, 2 * ResizeBandSize );

			EditorGUIUtility.AddCursorRect( cornerGrip, MouseCursor.ResizeUpLeft );
			EditorGUIUtility.AddCursorRect( leftGrip, MouseCursor.ResizeHorizontal );
			EditorGUIUtility.AddCursorRect( topGrip, MouseCursor.ResizeVertical );

			if ( !m_isResizing )
			{
				if ( Event.current.type == EventType.MouseDown && Event.current.button == ButtonClickId.LeftMouseButton )
				{
					bool onCorner = cornerGrip.Contains( mousePosition );
					bool onLeft = leftGrip.Contains( mousePosition );
					bool onTop = topGrip.Contains( mousePosition );
					if ( onCorner || onLeft || onTop )
					{
						m_isResizing = true;
						m_resizeAxis = new Vector2( ( onCorner || onLeft ) ? 1 : 0, ( onCorner || onTop ) ? 1 : 0 );
						Event.current.Use();
					}
				}
			}
			else if ( Event.current.type == EventType.MouseDrag )
			{
				if ( m_resizeAxis.x > 0 )
				{
					m_parentWindow.MaxMsgWidth = Mathf.Clamp( area.xMax - mousePosition.x, MinBoxWidth, area.xMax - leftBound );
				}

				if ( m_resizeAxis.y > 0 )
				{
					m_parentWindow.MsgBoxHeight = Mathf.Clamp( area.yMax - mousePosition.y, MinBoxHeight, area.yMax - topBound );
				}

				m_parentWindow.MsgBoxResized = true;
				m_parentWindow.MarkToRepaint();
				Event.current.Use();
			}
			else if ( Event.current.isMouse )
			{
				m_isResizing = false;

				// @diogo: stored once the drag ends instead of per delta, to keep EditorPrefs writes out of it
				if ( m_parentWindow.MsgBoxResized )
				{
					EditorVariablesManager.MsgBoxWidth.Value = m_parentWindow.MaxMsgWidth;
					EditorVariablesManager.MsgBoxHeight.Value = m_parentWindow.MsgBoxHeight;
					EditorVariablesManager.MsgBoxResized.Value = true;
				}

				Event.current.Use();
			}
		}

		public void Draw( Rect parentPosition, Vector2 mousePosition, int mouseButtonId, bool hasKeyboadFocus, float leftSide, float rightSide )
		{
			EventType currentEventType = Event.current.type;
			var messages = m_parentWindow.Messages;
			var maximize = m_parentWindow.MaximizeMessages;

			Rect button = parentPosition;
			button.width = 22;
			button.height = 22;
			button.x = parentPosition.x + parentPosition.width - button.width - rightSide - 8;
			button.y = parentPosition.y + parentPosition.height - button.height - ( m_parentWindow.CurrentSelection == ASESelectionMode.Material ? 52 : 8 );

			Rect toolbarArea = button;
			toolbarArea.y -= 5;
			toolbarArea.x -= 6;

			// @diogo: messages grow leftwards from the toggle button; the side panels draw before the
			// console and can't cover them, so stop them at the canvas edge instead. Both panels
			// together can take the whole window, hence the minimum width
			float leftBound = Mathf.Min( parentPosition.x + leftSide + 8, toolbarArea.xMax - 64 );
			float topBound = parentPosition.y + TopMargin;

			if ( maximize )
			{
				// @diogo: an old layout comes back without a height, and a box with none has no grip to grab
				float boxHeight = Mathf.Max( m_parentWindow.MsgBoxHeight, MinBoxHeight );
				float boxWidth = m_parentWindow.MsgBoxResized ? m_parentWindow.MaxMsgWidth : MAXWIDTH;

				toolbarArea.y -= 16 + 8;
				toolbarArea.xMin = Mathf.Max( toolbarArea.xMax - boxWidth, leftBound );
				toolbarArea.yMin = Mathf.Max( toolbarArea.yMax - boxHeight, topBound );

				m_boxArea = toolbarArea;
				m_boxArea.xMin -= ResizeBandSize;
				m_boxArea.yMin -= ResizeBandSize;
			}

			bool needsRepaint = false;
			if( maximize )
			{
				GUIStyle labelStyle = UIUtils.ConsoleLogLabel;
				HandleResize( toolbarArea, leftBound, topBound, mousePosition );
				GUILayout.BeginArea( toolbarArea, UIUtils.ConsoleLogMessage );
				EditorGUILayout.BeginVertical();
				m_currentScrollPos = EditorGUILayout.BeginScrollView( m_currentScrollPos );
				{
					int count = messages.Count;
					for( int i = count - 1; i >= 0; i-- )
					{
						switch( messages[ i ].ItemType )
						{
							case MessageSeverity.Error:
							labelStyle.normal.textColor = Color.red;
							break;
							case MessageSeverity.Warning:
							labelStyle.normal.textColor = Color.yellow;
							break;
							default:
							case MessageSeverity.Normal:
							labelStyle.normal.textColor = Color.white;
							break;
						}

						if( messages[ i ].ItemOwnerId < 0 )
						{
							if( Event.current.control && Event.current.shift )
							{
								if( GUILayout.Button( ( count - i ) + ": " + messages[ i ].ItemMessage, labelStyle ) )
								{
									if( Event.current.button == 1 )
									{
										EditorGUIUtility.systemCopyBuffer = messages[ i ].ItemMessage;
									}
								}
							}
							else
							{
								GUILayout.Label( ( count - i ) + ": " + messages[ i ].ItemMessage, labelStyle );
							}
						}
						else
						{
							if( GUILayout.Button( ( count - i ) + ": " + messages[ i ].ItemMessage, labelStyle ) )
							{
								UIUtils.CurrentWindow.FocusOnNode( messages[ i ].ItemOwnerId, 1, true );
								if( Event.current.button == 1 )
								{
									EditorGUIUtility.systemCopyBuffer = messages[ i ].ItemMessage;
								}
							}
						}
					}
				}
				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
				
				GUILayout.EndArea();
			} 
			else
			{
				// draw toaster
				int count = messages.Count;
				Rect rect = toolbarArea;

				// @diogo: toasts follow the box the user sized, so both presentations stay the same width
				float toastWidth = m_parentWindow.MsgBoxResized ? m_parentWindow.MaxMsgWidth : MAXWIDTH;
				float startFade = FADETIME - 1;
				for( int i = 0; i < count; i++ )
				{
					GUIStyle msgstyle = UIUtils.ConsoleLogMessage;
					float delta = (float)(Time.realtimeSinceStartup - messages[ i ].ItemTime);
					if( delta > FADETIME )
						continue;

					if( delta < 0.1f )
					{
						msgstyle.normal.textColor = Color.cyan;
					}
					else if( delta < startFade )
					{
						switch( messages[ i ].ItemType )
						{
							case MessageSeverity.Error:
							msgstyle.normal.textColor = Color.red;
							break;
							case MessageSeverity.Warning:
							msgstyle.normal.textColor = Color.yellow;
							break;
							default:
							case MessageSeverity.Normal:
							msgstyle.normal.textColor = Color.white;
							break;
						}
					}
					else
					{
						switch( messages[ i ].ItemType )
						{
							case MessageSeverity.Error:
							msgstyle.normal.textColor = new Color( 1, 0, 0, FADETIME - delta );
							break;
							case MessageSeverity.Warning:
							msgstyle.normal.textColor = new Color( 1, 1, 0, FADETIME - delta );
							break;
							default:
							case MessageSeverity.Normal:
							msgstyle.normal.textColor = new Color( 1, 1, 1, FADETIME - delta );
							break;
						}
					}

					needsRepaint = true;

					GUIContent gc = new GUIContent( messages[ i ].ItemMessage );
					var sizes = msgstyle.CalcSize( gc );

					// @diogo: CalcSize never wraps, so a long message would be laid out on a single line
					float boxWidth = Mathf.Min( Mathf.Min( sizes.x, toastWidth ), rect.xMax - leftBound );

					// @diogo: a box sized to the text sits right on the wrap threshold, where the renderer
					// still breaks its last word while the measured height stays a single line, so a message
					// only wraps when it genuinely does not fit
					msgstyle.wordWrap = boxWidth < sizes.x;

					rect.xMin = rect.xMax - boxWidth;
					rect.height = msgstyle.CalcHeight( gc, boxWidth );
					rect.y -= rect.height + 2;
					if( messages[ i ].ItemOwnerId < 0 )
					{
						GUI.Label( rect, gc, msgstyle );
					}
					else
					{
						if( GUI.Button( rect, gc, msgstyle ))
						{
							UIUtils.CurrentWindow.FocusOnNode( messages[ i ].ItemOwnerId, 1, true );
							if( Event.current.button == 1 )
							{
								EditorGUIUtility.systemCopyBuffer = messages[ i ].ItemMessage;
							}
						}
					}
				}
			}
			//GUI.color = cached;
			
			if( needsRepaint )
				m_parentWindow.MarkToRepaint();

			GUIStyle style = UIUtils.ConsoleLogCircle;

			button.size = Vector2.one * 16;
			
			switch( lastCall )
			{
				case 0: 
				style.normal.textColor = Color.cyan;
				break;
				case 1:
				style.normal.textColor = Color.yellow;
				break;
				case 2:
				style.normal.textColor = Color.red;
				break;
				default:
				style.normal.textColor = new Color( 1, 1, 1, 0.5f );
				break;
			}
			
			if( GUI.Button( button, m_boxToggleContent, style ) )
			{
				maximize = !maximize;
				m_parentWindow.MaximizeMessages = maximize;
				m_currentScrollPos.y = Mathf.Infinity;
				lastCall = -1;
			}

			style.normal.textColor = new Color( 1, 1, 1, 0.5f );
			//GUI.color = cached;
			button.x -= button.width + 2;

			if( maximize && GUI.Button( button, m_clearContent, style ) )
			{
				if( messages.Count == 0 )
				{
					maximize = false;
					m_parentWindow.MaximizeMessages = maximize;
				}
				ClearMessages();
			}

			button.width += button.width + 2;
			bool mouseOnTop = button.Contains( mousePosition );

			if( currentEventType == EventType.MouseMove && mouseOnTop )
				m_parentWindow.MarkToRepaint();

			if( DebugConsoleWindow.DeveloperMode )
			{
				if( Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha1 )
				{
					UIUtils.ShowMessage( "This is an info message\nwith two lines" );
				}

				if( Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha2 )
				{
					UIUtils.ShowMessage( "This is a warning message", MessageSeverity.Warning );
				}

				if( Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha3 )
				{
				
					UIUtils.ShowMessage( "THIS IS AN ERROR MESSAGE!!", MessageSeverity.Error );
				}
			}
		}

		public void ClearMessages()
		{
			m_parentWindow.Messages.Clear();
		}

		public void Toggle()
		{
			
		}

		public void Destroy()
		{
			m_parentWindow = null;
		}
	}
}

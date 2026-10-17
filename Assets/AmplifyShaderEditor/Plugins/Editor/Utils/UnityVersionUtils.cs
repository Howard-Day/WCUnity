// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace AmplifyShaderEditor
{
	// Unity versions are handled directly as their UNITY_VERSION macro value: 6MMMPPPP from
	// Unity 6000 on (single leading major digit, 3-digit minor, 4-digit patch, e.g. 6000.2.3
	// yields 60020003) and YYYYMP up to Unity 2023 (single-digit minor and patch, e.g. 2022.3.0
	// yields 202230); values from both formats stay ordered relative to each other, so mixed-era
	// comparisons still hold
	// https://docs.unity3d.com/6000.3/Documentation/Manual/shader-branching-unity-version.html
	[NoAutoStaticsCleanup]
	public static class UnityVersionUtils
	{
		private static int m_editorVersion = -1;

		// Unity version running the editor as a macro value, clamped to what its era can represent
		public static int EditorVersion
		{
			get
			{
				if ( m_editorVersion < 0 )
				{
					m_editorVersion = 0;
					string[] parts = Application.unityVersion.Split( '.' );
					if ( parts.Length >= 2 && int.TryParse( parts[ 0 ], out int major ) && int.TryParse( parts[ 1 ], out int minor ) )
					{
						// The patch part carries a release suffix, e.g. the 23f1 in 6000.0.23f1
						int patch = 0;
						if ( parts.Length >= 3 )
						{
							int digits = 0;
							while ( digits < parts[ 2 ].Length && char.IsDigit( parts[ 2 ][ digits ] ) )
							{
								digits++;
							}
							int.TryParse( parts[ 2 ].Substring( 0, digits ), out patch );
						}

						if ( major >= 6000 )
						{
							minor = Mathf.Min( minor, 999 );
							patch = Mathf.Min( patch, 9999 );
						}
						else
						{
							minor = Mathf.Min( minor, 9 );
							patch = Mathf.Min( patch, 9 );
						}
						m_editorVersion = Pack( major, minor, patch );
					}
				}
				return m_editorVersion;
			}
		}

		public static int Pack( int major, int minor, int patch )
		{
			return ( major >= 6000 ) ? ( ( major / 1000 ) * 10000000 + minor * 10000 + patch ) : ( major * 100 + minor * 10 + patch );
		}

		public static int Major( int version )
		{
			return ( version >= 60000000 ) ? ( ( version / 10000000 ) * 1000 ) : ( version / 100 );
		}

		// One minor version in macro encoding; the patch field is 4 digits from Unity 6000 on and
		// a single digit before
		public static int MinorStep( int version )
		{
			return ( version >= 60000000 ) ? 10000 : 10;
		}

		public static string VersionToString( int version )
		{
			int minor, patch;
			if ( version >= 60000000 )
			{
				minor = ( version / 10000 ) % 1000;
				patch = version % 10000;
			}
			else
			{
				minor = ( version / 10 ) % 10;
				patch = version % 10;
			}

			string str = Major( version ) + "." + minor;
			if ( patch != 0 )
			{
				str += "." + patch;
			}
			return str;
		}

		public static bool TryParseVersion( string str, out int version )
		{
			version = 0;
			if ( string.IsNullOrEmpty( str ) )
			{
				return false;
			}

			string[] parts = str.Trim().Split( '.' );
			if ( parts.Length > 3 )
			{
				return false;
			}

			int minor = 0;
			int patch = 0;
			if ( !int.TryParse( parts[ 0 ], out int major ) )
			{
				return false;
			}

			if ( parts.Length > 1 && !int.TryParse( parts[ 1 ], out minor ) )
			{
				return false;
			}

			if ( parts.Length > 2 && !int.TryParse( parts[ 2 ], out patch ) )
			{
				return false;
			}

			if ( major < 0 || minor < 0 || patch < 0 )
			{
				return false;
			}

			if ( major >= 6000 )
			{
				// 6MMMPPPP only fits 3-digit minors and 4-digit patches, and majors map to its
				// single leading digit (6000 yields 6), so only whole-thousand majors up to 9000
				// are representable
				if ( major > 9000 || ( major % 1000 ) != 0 || minor > 999 || patch > 9999 )
				{
					return false;
				}
			}
			else if ( minor > 9 || patch > 9 )
			{
				// YYYYMP only reserves a single digit for minor and patch
				return false;
			}

			version = Pack( major, minor, patch );
			return true;
		}
	}

	// Immediate text field over a packed UNITY_VERSION value; edits apply on every keystroke so
	// they can't be lost by saving while the field still has focus (a delayed field only commits
	// on Enter/focus change). While focused, the raw text is kept so typing isn't reformatted
	// mid-edit; invalid text tints the field red and leaves the last valid value untouched.
	public class UnityVersionField
	{
		private string m_inEditControl = string.Empty;
		private string m_buffer = string.Empty;

		// emptyVersion is the sentinel stored when allowEmpty and the field is left blank;
		// forceInvalid tints the field red regardless of content, for cross-field rules the
		// caller enforces; changed is only raised when a valid value is committed
		public int Draw( Rect rect, GUIContent label, string controlName, int version, bool allowEmpty, int emptyVersion, bool forceInvalid, out bool changed )
		{
			changed = false;

			string text;
			if ( m_inEditControl == controlName )
			{
				text = m_buffer;
			}
			else
			{
				text = ( allowEmpty && version == emptyVersion ) ? string.Empty : UnityVersionUtils.VersionToString( version );
			}

			bool valid = !forceInvalid && ( ( allowEmpty && string.IsNullOrEmpty( text.Trim() ) ) || UnityVersionUtils.TryParseVersion( text, out _ ) );
			Color cacheColor = GUI.backgroundColor;
			if ( !valid )
			{
				GUI.backgroundColor = Color.red;
			}

			GUI.SetNextControlName( controlName );
			EditorGUI.BeginChangeCheck();
			text = EditorGUI.TextField( rect, label, text );
			GUI.backgroundColor = cacheColor;
			if ( EditorGUI.EndChangeCheck() )
			{
				m_inEditControl = controlName;
				m_buffer = text;

				if ( allowEmpty && string.IsNullOrEmpty( text.Trim() ) )
				{
					version = emptyVersion;
					changed = true;
				}
				else if ( UnityVersionUtils.TryParseVersion( text, out int parsed ) )
				{
					version = parsed;
					changed = true;
				}
			}
			else if ( m_inEditControl == controlName && GUI.GetNameOfFocusedControl() != controlName )
			{
				m_inEditControl = string.Empty;
			}

			return version;
		}
	}
}
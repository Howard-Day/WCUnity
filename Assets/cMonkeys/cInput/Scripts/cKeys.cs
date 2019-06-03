using UnityEngine;

/// <summary>Easy access to KeyCode strings required by cInput</summary>
public class Keys {

	#region Keyboard input values

	#region Numbers

	public const string Alpha0 = "Alpha0";
	public const string Alpha1 = "Alpha1";
	public const string Alpha2 = "Alpha2";
	public const string Alpha3 = "Alpha3";
	public const string Alpha4 = "Alpha4";
	public const string Alpha5 = "Alpha5";
	public const string Alpha6 = "Alpha6";
	public const string Alpha7 = "Alpha7";
	public const string Alpha8 = "Alpha8";
	public const string Alpha9 = "Alpha9";

	#endregion //Numbers

	#region English alphabet

	public const string A = "A";
	public const string B = "B";
	public const string C = "C";
	public const string D = "D";
	public const string E = "E";
	public const string F = "F";
	public const string G = "G";
	public const string H = "H";
	public const string I = "I";
	public const string J = "J";
	public const string K = "K";
	public const string L = "L";
	public const string M = "M";
	public const string N = "N";
	public const string O = "O";
	public const string P = "P";
	public const string Q = "Q";
	public const string R = "R";
	public const string S = "S";
	public const string T = "T";
	public const string U = "U";
	public const string V = "V";
	public const string W = "W";
	public const string X = "X";
	public const string Y = "Y";
	public const string Z = "Z";

	#endregion //English alphabet

	#region Function keys

	public const string F1 = "F1";
	public const string F2 = "F2";
	public const string F3 = "F3";
	public const string F4 = "F4";
	public const string F5 = "F5";
	public const string F6 = "F6";
	public const string F7 = "F7";
	public const string F8 = "F8";
	public const string F9 = "F9";
	public const string F10 = "F10";
	public const string F11 = "F11";
	public const string F12 = "F12";
	public const string F13 = "F13";
	public const string F14 = "F14";
	public const string F15 = "F15";

	#endregion //Function keys

	#region Keypad (Numpad) keys

	public const string Keypad0 = "Keypad0";
	public const string Keypad1 = "Keypad1";
	public const string Keypad2 = "Keypad2";
	public const string Keypad3 = "Keypad3";
	public const string Keypad4 = "Keypad4";
	public const string Keypad5 = "Keypad5";
	public const string Keypad6 = "Keypad6";
	public const string Keypad7 = "Keypad7";
	public const string Keypad8 = "Keypad8";
	public const string Keypad9 = "Keypad9";
	/// <summary>Alternative for and equivalent to Keys.KeypadMultiply</summary>
	public const string KeypadAsterisk = "KeypadMultiply";
	public const string KeypadDivide = "KeypadDivide";
	public const string KeypadEnter = "KeypadEnter";
	public const string KeypadEquals = "KeypadEquals";
	public const string KeypadMinus = "KeypadMinus";
	public const string KeypadMultiply = "KeypadMultiply";
	public const string KeypadPeriod = "KeypadPeriod";
	public const string KeypadPlus = "KeypadPlus";
	/// <summary>Alternative for and equivalent to Keys.KeypadDivide</summary>
	public const string KeypadSlash = "KeypadDivide";

	#endregion //Keypad (Numpad) keys

	#region Other keys

	public const string None = "None";
	public const string AltGr = "AltGr";
	public const string Ampersand = "Ampersand";
	/// <summary>Alternative for and equivalent to Keys.Quote</summary>
	public const string Apostrophe = "Quote";
	/// <summary>Alternative for and equivalent to Keys.DownArrow</summary>
	public const string ArrowDown = "DownArrow";
	/// <summary>Alternative for and equivalent to Keys.LeftArrow</summary>
	public const string ArrowLeft = "LeftArrow";
	/// <summary>Alternative for and equivalent to Keys.RightArrow</summary>
	public const string ArrowRight = "RightArrow";
	/// <summary>Alternative for and equivalent to Keys.UpArrow</summary>
	public const string ArrowUp = "UpArrow";
	public const string Asterisk = "Asterisk";
	public const string AtSymbol = "At";
	public const string BackQuote = "BackQuote";
	public const string Backslash = "Backslash";
	public const string Backspace = "Backspace";
	public const string Break = "Break";
	public const string CapsLock = "CapsLock";
	public const string Caret = "Caret";
	public const string Clear = "Clear";
	public const string Colon = "Colon";
	public const string Comma = "Comma";
	public const string Delete = "Delete";
	public const string Dollar = "Dollar";
	public const string DoubleQuote = "DoubleQuote";
	public const string DownArrow = "DownArrow";
	public const string End = "End";
	/// <summary>Alternative for and equivalent to Keys.Return</summary>
	public const string Enter = "Return";
	public const string EqualSign = "Equals";
	public const string Escape = "Escape";
	public const string Exclaim = "Exclaim";
	/// <summary>Alternative for and equivalent to Keys.Exclaim</summary>
	public const string ExclamationMark = "Exclaim";
	/// <summary>Alternative for and equivalent to Keys.Slash</summary>
	public const string ForwardSlash = "Slash";
	/// <summary>Alternative for and equivalent to Keys.Greater</summary>
	public const string GreaterThan = "Greater";
	public const string Greater = "Greater";
	public const string Hash = "Hash";
	public const string Help = "Help";
	public const string Home = "Home";
	public const string Insert = "Insert";
	public const string LeftAlt = "LeftAlt";
	public const string LeftApple = "LeftApple";
	public const string LeftArrow = "LeftArrow";
	public const string LeftBracket = "LeftBracket";
	public const string LeftControl = "LeftControl";
	public const string LeftParen = "LeftParen";
	public const string LeftShift = "LeftShift";
	public const string LeftWindows = "LeftWindows";
	public const string Less = "Less";
	/// <summary>Alternative for and equivalent to Keys.Less</summary>
	public const string LessThan = "Less";
	public const string Menu = "Menu";
	public const string Minus = "Minus";
	/// <summary>Alternative for and equivalent to Keys.Greater</summary>
	public const string MoreThan = "Greater";
	/// <summary>Alternative for and equivalent to Keys.Hash</summary>
	public const string NumberSign = "Hash";
	public const string Numlock = "Numlock";
	public const string PageDown = "PageDown";
	public const string PageUp = "PageUp";
	public const string Pause = "Pause";
	public const string Period = "Period";
	public const string Plus = "Plus";
	/// <summary>Alternative for and equivalent to Keys.Hash</summary>
	public const string PoundSign = "Hash";
	public const string Print = "Print";
	/// <summary>Alternative for and equivalent to Keys.Question</summary>
	public const string QuestionMark = "Question";
	public const string Question = "Question";
	/// <summary>This is a single quote (apostrophe). For double quotes, use Keys.DoubleQuote.</summary>
	public const string Quote = "Quote";
	public const string Return = "Return";
	public const string RightAlt = "RightAlt";
	public const string RightApple = "RightApple";
	public const string RightArrow = "RightArrow";
	public const string RightBracket = "RightBracket";
	public const string RightControl = "RightControl";
	public const string RightParen = "RightParen";
	public const string RightShift = "RightShift";
	public const string RightWindows = "RightWindows";
	public const string ScrollLock = "ScrollLock";
	public const string Semicolon = "Semicolon";
	public const string Slash = "Slash";
	public const string Space = "Space";
	public const string SysReq = "SysReq";
	public const string Tab = "Tab";
	public const string Underscore = "Underscore";
	public const string UpArrow = "UpArrow";

	#endregion //Other keys

	#endregion //Keyboard input values

	#region Mouse input values

	public const string Mouse0 = "Mouse0";
	public const string Mouse1 = "Mouse1";
	public const string Mouse2 = "Mouse2";
	public const string Mouse3 = "Mouse3";
	public const string Mouse4 = "Mouse4";
	public const string Mouse5 = "Mouse5";
	public const string Mouse6 = "Mouse6";
	public const string MouseUp = "Mouse Up";
	public const string MouseDown = "Mouse Down";
	public const string MouseLeft = "Mouse Left";
	public const string MouseRight = "Mouse Right";
	public const string MouseWheelUp = "Mouse Wheel Up";
	public const string MouseWheelDown = "Mouse Wheel Down";

	#endregion //Mouse input values

	#region Gamepad values

	#region Gamepad buttons

	public const string JoystickButton0 = "JoystickButton0";
	public const string JoystickButton1 = "JoystickButton1";
	public const string JoystickButton2 = "JoystickButton2";
	public const string JoystickButton3 = "JoystickButton3";
	public const string JoystickButton4 = "JoystickButton4";
	public const string JoystickButton5 = "JoystickButton5";
	public const string JoystickButton6 = "JoystickButton6";
	public const string JoystickButton7 = "JoystickButton7";
	public const string JoystickButton8 = "JoystickButton8";
	public const string JoystickButton9 = "JoystickButton9";
	public const string JoystickButton10 = "JoystickButton10";
	public const string JoystickButton11 = "JoystickButton11";
	public const string JoystickButton12 = "JoystickButton12";
	public const string JoystickButton13 = "JoystickButton13";
	public const string JoystickButton14 = "JoystickButton14";
	public const string JoystickButton15 = "JoystickButton15";
	public const string JoystickButton16 = "JoystickButton16";
	public const string JoystickButton17 = "JoystickButton17";
	public const string JoystickButton18 = "JoystickButton18";
	public const string JoystickButton19 = "JoystickButton19";
	public const string Joystick1Button0 = "Joystick1Button0";
	public const string Joystick1Button1 = "Joystick1Button1";
	public const string Joystick1Button2 = "Joystick1Button2";
	public const string Joystick1Button3 = "Joystick1Button3";
	public const string Joystick1Button4 = "Joystick1Button4";
	public const string Joystick1Button5 = "Joystick1Button5";
	public const string Joystick1Button6 = "Joystick1Button6";
	public const string Joystick1Button7 = "Joystick1Button7";
	public const string Joystick1Button8 = "Joystick1Button8";
	public const string Joystick1Button9 = "Joystick1Button9";
	public const string Joystick1Button10 = "Joystick1Button10";
	public const string Joystick1Button11 = "Joystick1Button11";
	public const string Joystick1Button12 = "Joystick1Button12";
	public const string Joystick1Button13 = "Joystick1Button13";
	public const string Joystick1Button14 = "Joystick1Button14";
	public const string Joystick1Button15 = "Joystick1Button15";
	public const string Joystick1Button16 = "Joystick1Button16";
	public const string Joystick1Button17 = "Joystick1Button17";
	public const string Joystick1Button18 = "Joystick1Button18";
	public const string Joystick1Button19 = "Joystick1Button19";
	public const string Joystick2Button0 = "Joystick2Button0";
	public const string Joystick2Button1 = "Joystick2Button1";
	public const string Joystick2Button2 = "Joystick2Button2";
	public const string Joystick2Button3 = "Joystick2Button3";
	public const string Joystick2Button4 = "Joystick2Button4";
	public const string Joystick2Button5 = "Joystick2Button5";
	public const string Joystick2Button6 = "Joystick2Button6";
	public const string Joystick2Button7 = "Joystick2Button7";
	public const string Joystick2Button8 = "Joystick2Button8";
	public const string Joystick2Button9 = "Joystick2Button9";
	public const string Joystick2Button10 = "Joystick2Button10";
	public const string Joystick2Button11 = "Joystick2Button11";
	public const string Joystick2Button12 = "Joystick2Button12";
	public const string Joystick2Button13 = "Joystick2Button13";
	public const string Joystick2Button14 = "Joystick2Button14";
	public const string Joystick2Button15 = "Joystick2Button15";
	public const string Joystick2Button16 = "Joystick2Button16";
	public const string Joystick2Button17 = "Joystick2Button17";
	public const string Joystick2Button18 = "Joystick2Button18";
	public const string Joystick2Button19 = "Joystick2Button19";
	public const string Joystick3Button0 = "Joystick3Button0";
	public const string Joystick3Button1 = "Joystick3Button1";
	public const string Joystick3Button2 = "Joystick3Button2";
	public const string Joystick3Button3 = "Joystick3Button3";
	public const string Joystick3Button4 = "Joystick3Button4";
	public const string Joystick3Button5 = "Joystick3Button5";
	public const string Joystick3Button6 = "Joystick3Button6";
	public const string Joystick3Button7 = "Joystick3Button7";
	public const string Joystick3Button8 = "Joystick3Button8";
	public const string Joystick3Button9 = "Joystick3Button9";
	public const string Joystick3Button10 = "Joystick3Button10";
	public const string Joystick3Button11 = "Joystick3Button11";
	public const string Joystick3Button12 = "Joystick3Button12";
	public const string Joystick3Button13 = "Joystick3Button13";
	public const string Joystick3Button14 = "Joystick3Button14";
	public const string Joystick3Button15 = "Joystick3Button15";
	public const string Joystick3Button16 = "Joystick3Button16";
	public const string Joystick3Button17 = "Joystick3Button17";
	public const string Joystick3Button18 = "Joystick3Button18";
	public const string Joystick3Button19 = "Joystick3Button19";
	public const string Joystick4Button0 = "Joystick4Button0";
	public const string Joystick4Button1 = "Joystick4Button1";
	public const string Joystick4Button2 = "Joystick4Button2";
	public const string Joystick4Button3 = "Joystick4Button3";
	public const string Joystick4Button4 = "Joystick4Button4";
	public const string Joystick4Button5 = "Joystick4Button5";
	public const string Joystick4Button6 = "Joystick4Button6";
	public const string Joystick4Button7 = "Joystick4Button7";
	public const string Joystick4Button8 = "Joystick4Button8";
	public const string Joystick4Button9 = "Joystick4Button9";
	public const string Joystick4Button10 = "Joystick4Button10";
	public const string Joystick4Button11 = "Joystick4Button11";
	public const string Joystick4Button12 = "Joystick4Button12";
	public const string Joystick4Button13 = "Joystick4Button13";
	public const string Joystick4Button14 = "Joystick4Button14";
	public const string Joystick4Button15 = "Joystick4Button15";
	public const string Joystick4Button16 = "Joystick4Button16";
	public const string Joystick4Button17 = "Joystick4Button17";
	public const string Joystick4Button18 = "Joystick4Button18";
	public const string Joystick4Button19 = "Joystick4Button19";

	#endregion //Gamepad buttons

	#region Gamepad axes

	#region Any Gamepad Axes

	public const string JoyAxis1Negative = "Joy Axis 1-";
	public const string JoyAxis1Positive = "Joy Axis 1+";
	public const string JoyAxis2Negative = "Joy Axis 2-";
	public const string JoyAxis2Positive = "Joy Axis 2+";
	public const string JoyAxis3Negative = "Joy Axis 3-";
	public const string JoyAxis3Positive = "Joy Axis 3+";
	public const string JoyAxis4Negative = "Joy Axis 4-";
	public const string JoyAxis4Positive = "Joy Axis 4+";
	public const string JoyAxis5Negative = "Joy Axis 5-";
	public const string JoyAxis5Positive = "Joy Axis 5+";
	public const string JoyAxis6Negative = "Joy Axis 6-";
	public const string JoyAxis6Positive = "Joy Axis 6+";
	public const string JoyAxis7Negative = "Joy Axis 7-";
	public const string JoyAxis7Positive = "Joy Axis 7+";
	public const string JoyAxis8Negative = "Joy Axis 8-";
	public const string JoyAxis8Positive = "Joy Axis 8+";
	public const string JoyAxis9Negative = "Joy Axis 9-";
	public const string JoyAxis9Positive = "Joy Axis 9+";
	public const string JoyAxis10Negative = "Joy Axis 10-";
	public const string JoyAxis10Positive = "Joy Axis 10+";

	#endregion //Any Gamepad Axes

	#region Gamepad 1 Axes

	public const string Joy1Axis1Negative = "Joy1 Axis 1-";
	public const string Joy1Axis1Positive = "Joy1 Axis 1+";
	public const string Joy1Axis2Negative = "Joy1 Axis 2-";
	public const string Joy1Axis2Positive = "Joy1 Axis 2+";
	public const string Joy1Axis3Negative = "Joy1 Axis 3-";
	public const string Joy1Axis3Positive = "Joy1 Axis 3+";
	public const string Joy1Axis4Negative = "Joy1 Axis 4-";
	public const string Joy1Axis4Positive = "Joy1 Axis 4+";
	public const string Joy1Axis5Negative = "Joy1 Axis 5-";
	public const string Joy1Axis5Positive = "Joy1 Axis 5+";
	public const string Joy1Axis6Negative = "Joy1 Axis 6-";
	public const string Joy1Axis6Positive = "Joy1 Axis 6+";
	public const string Joy1Axis7Negative = "Joy1 Axis 7-";
	public const string Joy1Axis7Positive = "Joy1 Axis 7+";
	public const string Joy1Axis8Negative = "Joy1 Axis 8-";
	public const string Joy1Axis8Positive = "Joy1 Axis 8+";
	public const string Joy1Axis9Negative = "Joy1 Axis 9-";
	public const string Joy1Axis9Positive = "Joy1 Axis 9+";
	public const string Joy1Axis10Negative = "Joy1 Axis 10-";
	public const string Joy1Axis10Positive = "Joy1 Axis 10+";

	#endregion //Gamepad 1 Axes

	#region Gamepad 2 Axes

	public const string Joy2Axis1Negative = "Joy2 Axis 1-";
	public const string Joy2Axis1Positive = "Joy2 Axis 1+";
	public const string Joy2Axis2Negative = "Joy2 Axis 2-";
	public const string Joy2Axis2Positive = "Joy2 Axis 2+";
	public const string Joy2Axis3Negative = "Joy2 Axis 3-";
	public const string Joy2Axis3Positive = "Joy2 Axis 3+";
	public const string Joy2Axis4Negative = "Joy2 Axis 4-";
	public const string Joy2Axis4Positive = "Joy2 Axis 4+";
	public const string Joy2Axis5Negative = "Joy2 Axis 5-";
	public const string Joy2Axis5Positive = "Joy2 Axis 5+";
	public const string Joy2Axis6Negative = "Joy2 Axis 6-";
	public const string Joy2Axis6Positive = "Joy2 Axis 6+";
	public const string Joy2Axis7Negative = "Joy2 Axis 7-";
	public const string Joy2Axis7Positive = "Joy2 Axis 7+";
	public const string Joy2Axis8Negative = "Joy2 Axis 8-";
	public const string Joy2Axis8Positive = "Joy2 Axis 8+";
	public const string Joy2Axis9Negative = "Joy2 Axis 9-";
	public const string Joy2Axis9Positive = "Joy2 Axis 9+";
	public const string Joy2Axis10Negative = "Joy2 Axis 10-";
	public const string Joy2Axis10Positive = "Joy2 Axis 10+";

	#endregion //Gamepad 2 Axes

	#region Gamepad 3 Axes

	public const string Joy3Axis1Negative = "Joy3 Axis 1-";
	public const string Joy3Axis1Positive = "Joy3 Axis 1+";
	public const string Joy3Axis2Negative = "Joy3 Axis 2-";
	public const string Joy3Axis2Positive = "Joy3 Axis 2+";
	public const string Joy3Axis3Negative = "Joy3 Axis 3-";
	public const string Joy3Axis3Positive = "Joy3 Axis 3+";
	public const string Joy3Axis4Negative = "Joy3 Axis 4-";
	public const string Joy3Axis4Positive = "Joy3 Axis 4+";
	public const string Joy3Axis5Negative = "Joy3 Axis 5-";
	public const string Joy3Axis5Positive = "Joy3 Axis 5+";
	public const string Joy3Axis6Negative = "Joy3 Axis 6-";
	public const string Joy3Axis6Positive = "Joy3 Axis 6+";
	public const string Joy3Axis7Negative = "Joy3 Axis 7-";
	public const string Joy3Axis7Positive = "Joy3 Axis 7+";
	public const string Joy3Axis8Negative = "Joy3 Axis 8-";
	public const string Joy3Axis8Positive = "Joy3 Axis 8+";
	public const string Joy3Axis9Negative = "Joy3 Axis 9-";
	public const string Joy3Axis9Positive = "Joy3 Axis 9+";
	public const string Joy3Axis10Negative = "Joy3 Axis 10-";
	public const string Joy3Axis10Positive = "Joy3 Axis 10+";

	#endregion //Gamepad 3 Axes

	#region Gamepad 4 Axes

	public const string Joy4Axis1Negative = "Joy4 Axis 1-";
	public const string Joy4Axis1Positive = "Joy4 Axis 1+";
	public const string Joy4Axis2Negative = "Joy4 Axis 2-";
	public const string Joy4Axis2Positive = "Joy4 Axis 2+";
	public const string Joy4Axis3Negative = "Joy4 Axis 3-";
	public const string Joy4Axis3Positive = "Joy4 Axis 3+";
	public const string Joy4Axis4Negative = "Joy4 Axis 4-";
	public const string Joy4Axis4Positive = "Joy4 Axis 4+";
	public const string Joy4Axis5Negative = "Joy4 Axis 5-";
	public const string Joy4Axis5Positive = "Joy4 Axis 5+";
	public const string Joy4Axis6Negative = "Joy4 Axis 6-";
	public const string Joy4Axis6Positive = "Joy4 Axis 6+";
	public const string Joy4Axis7Negative = "Joy4 Axis 7-";
	public const string Joy4Axis7Positive = "Joy4 Axis 7+";
	public const string Joy4Axis8Negative = "Joy4 Axis 8-";
	public const string Joy4Axis8Positive = "Joy4 Axis 8+";
	public const string Joy4Axis9Negative = "Joy4 Axis 9-";
	public const string Joy4Axis9Positive = "Joy4 Axis 9+";
	public const string Joy4Axis10Negative = "Joy4 Axis 10-";
	public const string Joy4Axis10Positive = "Joy4 Axis 10+";

	#endregion //Gamepad 4 Axes

	#endregion //Gamepad axes

	#endregion //Gamepad values

	#region Xbox Controls

	// this is so ugly
	private static bool isOSX {
		get {
			return (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer);
		}
	}
	private static bool isLinux {
		get {
			return (Application.platform == RuntimePlatform.LinuxPlayer);
		}
	}
	public static bool isWebGL {
		get {
#if UNITY_WEBGL
			return true;
#else
			return false;
#endif
		}
	}

	private static string[] _jNamez { get { return Input.GetJoystickNames(); } }
	//private static string _xboxOneMac = "Microsoft Xbox One Wired Controller";
	private static string _xboxOneWin = "Controller (Xbox One For Windows)";

	#region Any Xbox Controller

	public static string XboxA {
		get {
			if (isOSX) {
				return JoystickButton16;
			} else {
				return JoystickButton0;
			}
		}
	}
	public static string XboxB {
		get {
			if (isOSX) {
				return JoystickButton17;
			} else {
				return JoystickButton1;
			}
		}
	}
	public static string XboxX {
		get {
			if (isOSX) {
				return JoystickButton18;
			} else {
				return JoystickButton2;
			}
		}
	}
	public static string XboxY {
		get {
			if (isOSX) {
				return JoystickButton19;
			} else {
				return JoystickButton3;
			}
		}
	}
	public static string XboxBack {
		get {
			if (isOSX) {
				return JoystickButton10;
			} else if (isWebGL) {
				return JoystickButton8;
			} else {
				return JoystickButton6;
			}
		}
	}
	public static string XboxView {
		get { return XboxBack; }
	}
	public static string XboxStart {
		get {
			if (isOSX || isWebGL) {
				return JoystickButton9;
			} else {
				return JoystickButton7;
			}
		}
	}
	public static string XboxMenu {
		get { return XboxStart; }
	}
	public static string XboxBumperLeft {
		get {
			if (isOSX) {
				return JoystickButton13;
			} else {
				return JoystickButton4;
			}
		}
	}
	public static string XboxBumperRight {
		get {
			if (isOSX) {
				return JoystickButton14;
			} else {
				return JoystickButton5;
			}
		}
	}
	public static string XboxLStickButton {
		get {
			if (isLinux) {
				return JoystickButton9;
			} else if (isOSX) {
				return JoystickButton11;
			} else if (isWebGL) {
				return JoystickButton10;
			} else {
				return JoystickButton8;
			}
		}
	}
	public static string XboxRStickButton {
		get {
			if (isLinux) {
				return JoystickButton10;
			} else if (isOSX) {
				return JoystickButton12;
			} else if (isWebGL) {
				return JoystickButton11;
			} else {
				return JoystickButton9;
			}
		}
	}
	public static string XboxLStickLeft {
		get {
			return JoyAxis1Negative;
		}
	}
	public static string XboxLStickRight {
		get {
			return JoyAxis1Positive;
		}
	}
	public static string XboxLStickUp {
		get {
			return JoyAxis2Negative;
		}
	}
	public static string XboxLStickDown {
		get {
			return JoyAxis2Positive;
		}
	}
	public static string XboxRStickLeft {
		get {
			if (isOSX || isWebGL) {
				return JoyAxis3Negative;
			} else {
				return JoyAxis4Negative;
			}
		}
	}
	public static string XboxRStickRight {
		get {
			if (isOSX || isWebGL) {
				return JoyAxis3Positive;
			} else {
				return JoyAxis4Positive;
			}
		}
	}
	public static string XboxRStickUp {
		get {
			if (isOSX || isWebGL) {
				return JoyAxis4Negative;
			} else {
				return JoyAxis5Negative;
			}
		}
	}
	public static string XboxRStickDown {
		get {
			if (isOSX || isWebGL) {
				return JoyAxis4Positive;
			} else {
				return JoyAxis5Positive;
			}
		}
	}
	public static string XboxDPadLeft {
		get {
			if (isLinux) {
				return JoyAxis7Negative;
			} else if (isOSX) {
				return JoystickButton7;
			} else if (isWebGL) {
				return JoystickButton14;
			} else {
				return JoyAxis6Negative;
			}
		}
	}
	public static string XboxDPadRight {
		get {
			if (isLinux) {
				return JoyAxis7Positive;
			} else if (isOSX) {
				return JoystickButton8;
			} else if (isWebGL) {
				return JoystickButton15;
			} else {
				return JoyAxis6Positive;
			}
		}
	}
	public static string XboxDPadUp {
		get {
			if (isLinux) {
				return JoyAxis8Positive;
			} else if (isOSX) {
				return JoystickButton5;
			} else if (isWebGL) {
				return JoystickButton12;
			} else {
				return JoyAxis7Positive;
			}
		}
	}
	public static string XboxDPadDown {
		get {
			if (isLinux) {
				return JoyAxis8Negative;
			} else if (isOSX) {
				return JoystickButton6;
			} else if (isWebGL) {
				return JoystickButton13;
			} else {
				return JoyAxis7Negative;
			}
		}
	}
	public static string XboxTriggerLeft {
		get {
			if (isLinux) {
				return JoyAxis3Positive;
			} else if (isOSX) {
				return JoyAxis5Positive;
			} else if (isWebGL) {
				return JoystickButton6;
			} else {
				return JoyAxis9Positive;
			}
		}
	}
	public static string XboxTriggerRight {
		get {
			if (isOSX || isLinux) {
				return JoyAxis6Positive;
			} else if (isWebGL) {
				return JoystickButton7;
			} else {
				return JoyAxis10Positive;
			}
		}
	}

	#endregion //Any Xbox Controller

	#region Xbox Controller 1

	public static string Xbox1A {
		get {
			if (isOSX) {
				return Joystick1Button16;
			} else {
				return Joystick1Button0;
			}
		}
	}
	public static string Xbox1B {
		get {
			if (isOSX) {
				return Joystick1Button17;
			} else {
				return Joystick1Button1;
			}
		}
	}
	public static string Xbox1X {
		get {
			if (isOSX) {
				return Joystick1Button18;
			} else {
				return Joystick1Button2;
			}
		}
	}
	public static string Xbox1Y {
		get {
			if (isOSX) {
				return Joystick1Button19;
			} else {
				return Joystick1Button3;
			}
		}
	}
	public static string Xbox1Back {
		get {
			if (isOSX) {
				return Joystick1Button10;
			} else if (isWebGL) {
				return Joystick1Button8;
			} else {
				return Joystick1Button6;
			}
		}
	}
	public static string Xbox1View {
		get { return Xbox1Back; }
	}
	public static string Xbox1Start {
		get {
			if (isOSX || isWebGL) {
				return Joystick1Button9;
			} else {
				return Joystick1Button7;
			}
		}
	}
	public static string Xbox1Menu {
		get { return Xbox1Start; }
	}
	public static string Xbox1BumperLeft {
		get {
			if (isOSX) {
				return Joystick1Button13;
			} else {
				return Joystick1Button4;
			}
		}
	}
	public static string Xbox1BumperRight {
		get {
			if (isOSX) {
				return Joystick1Button14;
			} else {
				return Joystick1Button5;
			}
		}
	}
	public static string Xbox1LStickButton {
		get {
			if (isLinux) {
				return Joystick1Button9;
			} else if (isOSX) {
				return Joystick1Button11;
			} else if (isWebGL) {
				return Joystick1Button10;
			} else {
				return Joystick1Button8;
			}
		}
	}
	public static string Xbox1RStickButton {
		get {
			if (isLinux) {
				return Joystick1Button10;
			} else if (isOSX) {
				return Joystick1Button12;
			} else if (isWebGL) {
				return Joystick1Button11;
			} else {
				return Joystick1Button9;
			}
		}
	}
	public static string Xbox1LStickLeft {
		get {
			return Joy1Axis1Negative;
		}
	}
	public static string Xbox1LStickRight {
		get {
			return Joy1Axis1Positive;
		}
	}
	public static string Xbox1LStickUp {
		get {
			return Joy1Axis2Negative;
		}
	}
	public static string Xbox1LStickDown {
		get {
			return Joy1Axis2Positive;
		}
	}
	public static string Xbox1RStickLeft {
		get {
			if (isOSX || isWebGL) {
				return Joy1Axis3Negative;
			} else {
				return Joy1Axis4Negative;
			}
		}
	}
	public static string Xbox1RStickRight {
		get {
			if (isOSX || isWebGL) {
				return Joy1Axis3Positive;
			} else {
				return Joy1Axis4Positive;
			}
		}
	}
	public static string Xbox1RStickUp {
		get {
			if (isOSX || isWebGL) {
				return Joy1Axis4Negative;
			} else {
				return Joy1Axis5Negative;
			}
		}
	}
	public static string Xbox1RStickDown {
		get {
			if (isOSX || isWebGL) {
				return Joy1Axis4Positive;
			} else {
				return Joy1Axis5Positive;
			}
		}
	}
	public static string Xbox1DPadLeft {
		get {
			if (isLinux) {
				return Joy1Axis7Negative;
			} else if (isOSX) {
				return Joystick1Button7;
			} else if (isWebGL) {
				return Joystick1Button14;
			} else {
				// Windows
				if (_jNamez.Length >= 1) {
					if (_jNamez[0].ToLower() == _xboxOneWin.ToLower()) {
						return Joy1Axis7Negative;
					}
				}

				return Joy1Axis6Negative;
			}
		}
	}
	public static string Xbox1DPadRight {
		get {
			if (isLinux) {
				return Joy1Axis7Positive;
			} else if (isOSX) {
				return Joystick1Button8;
			} else if (isWebGL) {
				return Joystick1Button15;
			} else {
				// Windows
				if (_jNamez.Length >= 1) {
					if (_jNamez[0].ToLower() == _xboxOneWin.ToLower()) {
						return Joy1Axis7Positive;
					}
				}

				return Joy1Axis6Positive;
			}
		}
	}
	public static string Xbox1DPadUp {
		get {
			if (isLinux) {
				return Joy1Axis8Positive;
			} else if (isOSX) {
				return Joystick1Button5;
			} else if (isWebGL) {
				return Joystick1Button12;
			} else {
				// Windows
				if (_jNamez.Length >= 1) {
					if (_jNamez[0].ToLower() == _xboxOneWin.ToLower()) {
						return Joy1Axis8Positive;
					}
				}

				return Joy1Axis7Positive;
			}
		}
	}
	public static string Xbox1DPadDown {
		get {
			if (isLinux) {
				return Joy1Axis8Negative;
			} else if (isOSX) {
				return Joystick1Button6;
			} else if (isWebGL) {
				return Joystick1Button13;
			} else {
				// Windows
				if (_jNamez.Length >= 1) {
					if (_jNamez[0].ToLower() == _xboxOneWin.ToLower()) {
						return Joy1Axis8Negative;
					}
				}

				return Joy1Axis7Negative;
			}
		}
	}
	public static string Xbox1TriggerLeft {
		get {
			if (isLinux) {
				return Joy1Axis3Positive;
			} else if (isOSX) {
				return Joy1Axis5Positive;
			} else if (isWebGL) {
				return Joystick1Button6;
			} else {
				return Joy1Axis9Positive;
			}
		}
	}
	public static string Xbox1TriggerRight {
		get {
			if (isOSX || isLinux) {
				return Joy1Axis6Positive;
			} else if (isWebGL) {
				return Joystick1Button7;
			} else {
				return Joy1Axis10Positive;
			}
		}
	}

	#endregion //Xbox Controller 1

	#region Xbox Controller 2

	public static string Xbox2A {
		get {
			if (isOSX) {
				return Joystick2Button16;
			} else {
				return Joystick2Button0;
			}
		}
	}
	public static string Xbox2B {
		get {
			if (isOSX) {
				return Joystick2Button17;
			} else {
				return Joystick2Button1;
			}
		}
	}
	public static string Xbox2X {
		get {
			if (isOSX) {
				return Joystick2Button18;
			} else {
				return Joystick2Button2;
			}
		}
	}
	public static string Xbox2Y {
		get {
			if (isOSX) {
				return Joystick2Button19;
			} else {
				return Joystick2Button3;
			}
		}
	}
	public static string Xbox2Back {
		get {
			if (isOSX) {
				return Joystick2Button10;
			} else if (isWebGL) {
				return Joystick2Button8;
			} else {
				return Joystick2Button6;
			}
		}
	}
	public static string Xbox2View {
		get { return Xbox2Back; }
	}
	public static string Xbox2Start {
		get {
			if (isOSX || isWebGL) {
				return Joystick2Button9;
			} else {
				return Joystick2Button7;
			}
		}
	}
	public static string Xbox2Menu {
		get { return Xbox2Start; }
	}
	public static string Xbox2BumperLeft {
		get {
			if (isOSX) {
				return Joystick2Button13;
			} else {
				return Joystick2Button4;
			}
		}
	}
	public static string Xbox2BumperRight {
		get {
			if (isOSX) {
				return Joystick2Button14;
			} else {
				return Joystick2Button5;
			}
		}
	}
	public static string Xbox2LStickButton {
		get {
			if (isLinux) {
				return Joystick2Button9;
			} else if (isOSX) {
				return Joystick2Button11;
			} else if (isWebGL) {
				return Joystick2Button10;
			} else {
				return Joystick2Button8;
			}
		}
	}
	public static string Xbox2RStickButton {
		get {
			if (isLinux) {
				return Joystick2Button10;
			} else if (isOSX) {
				return Joystick2Button12;
			} else if (isWebGL) {
				return Joystick2Button11;
			} else {
				return Joystick2Button9;
			}
		}
	}
	public static string Xbox2LStickLeft {
		get {
			return Joy2Axis1Negative;
		}
	}
	public static string Xbox2LStickRight {
		get {
			return Joy2Axis1Positive;
		}
	}
	public static string Xbox2LStickUp {
		get {
			return Joy2Axis2Negative;
		}
	}
	public static string Xbox2LStickDown {
		get {
			return Joy2Axis2Positive;
		}
	}
	public static string Xbox2RStickLeft {
		get {
			if (isOSX || isWebGL) {
				return Joy2Axis3Negative;
			} else {
				return Joy2Axis4Negative;
			}
		}
	}
	public static string Xbox2RStickRight {
		get {
			if (isOSX || isWebGL) {
				return Joy2Axis3Positive;
			} else {
				return Joy2Axis4Positive;
			}
		}
	}
	public static string Xbox2RStickUp {
		get {
			if (isOSX || isWebGL) {
				return Joy2Axis4Negative;
			} else {
				return Joy2Axis5Negative;
			}
		}
	}
	public static string Xbox2RStickDown {
		get {
			if (isOSX || isWebGL) {
				return Joy2Axis4Positive;
			} else {
				return Joy2Axis5Positive;
			}
		}
	}
	public static string Xbox2DPadLeft {
		get {
			if (isLinux) {
				return Joy2Axis7Negative;
			} else if (isOSX) {
				return Joystick2Button7;
			} else if (isWebGL) {
				return Joystick2Button14;
			} else {
				// Windows
				if (_jNamez.Length >= 2) {
					if (_jNamez[1].ToLower() == _xboxOneWin.ToLower()) {
						return Joy2Axis7Negative;
					}
				}

				return Joy2Axis6Negative;
			}
		}
	}
	public static string Xbox2DPadRight {
		get {
			if (isLinux) {
				return Joy2Axis7Positive;
			} else if (isOSX) {
				return Joystick2Button8;
			} else if (isWebGL) {
				return Joystick2Button15;
			} else {
				// Windows
				if (_jNamez.Length >= 2) {
					if (_jNamez[1].ToLower() == _xboxOneWin.ToLower()) {
						return Joy2Axis7Positive;
					}
				}

				return Joy2Axis6Positive;
			}
		}
	}
	public static string Xbox2DPadUp {
		get {
			if (isLinux) {
				return Joy2Axis8Positive;
			} else if (isOSX) {
				return Joystick2Button5;
			} else if (isWebGL) {
				return Joystick2Button12;
			} else {
				// Windows
				if (_jNamez.Length >= 2) {
					if (_jNamez[1].ToLower() == _xboxOneWin.ToLower()) {
						return Joy2Axis8Positive;
					}
				}

				return Joy2Axis7Positive;
			}
		}
	}
	public static string Xbox2DPadDown {
		get {
			if (isLinux) {
				return Joy2Axis8Negative;
			} else if (isOSX) {
				return Joystick2Button6;
			} else if (isWebGL) {
				return Joystick2Button13;
			} else {
				// Windows
				if (_jNamez.Length >= 2) {
					if (_jNamez[1].ToLower() == _xboxOneWin.ToLower()) {
						return Joy2Axis8Negative;
					}
				}

				return Joy2Axis7Negative;
			}
		}
	}
	public static string Xbox2TriggerLeft {
		get {
			if (isLinux) {
				return Joy2Axis3Positive;
			} else if (isOSX) {
				return Joy2Axis5Positive;
			} else if (isWebGL) {
				return Joystick2Button6;
			} else {
				return Joy2Axis9Positive;
			}
		}
	}
	public static string Xbox2TriggerRight {
		get {
			if (isOSX || isLinux) {
				return Joy2Axis6Positive;
			} else if (isWebGL) {
				return Joystick2Button7;
			} else {
				return Joy2Axis10Positive;
			}
		}
	}

	#endregion //Xbox Controller 2

	#region Xbox Controller 3

	public static string Xbox3A {
		get {
			if (isOSX) {
				return Joystick3Button16;
			} else {
				return Joystick3Button0;
			}
		}
	}
	public static string Xbox3B {
		get {
			if (isOSX) {
				return Joystick3Button17;
			} else {
				return Joystick3Button1;
			}
		}
	}
	public static string Xbox3X {
		get {
			if (isOSX) {
				return Joystick3Button18;
			} else {
				return Joystick3Button2;
			}
		}
	}
	public static string Xbox3Y {
		get {
			if (isOSX) {
				return Joystick3Button19;
			} else {
				return Joystick3Button3;
			}
		}
	}
	public static string Xbox3Back {
		get {
			if (isOSX) {
				return Joystick3Button10;
			} else if (isWebGL) {
				return Joystick3Button8;
			} else {
				return Joystick3Button6;
			}
		}
	}
	public static string Xbox3View {
		get { return Xbox3Back; }
	}
	public static string Xbox3Start {
		get {
			if (isOSX || isWebGL) {
				return Joystick3Button9;
			} else {
				return Joystick3Button7;
			}
		}
	}
	public static string Xbox3Menu {
		get { return Xbox3Start; }
	}
	public static string Xbox3BumperLeft {
		get {
			if (isOSX) {
				return Joystick3Button13;
			} else {
				return Joystick3Button4;
			}
		}
	}
	public static string Xbox3BumperRight {
		get {
			if (isOSX) {
				return Joystick3Button14;
			} else {
				return Joystick3Button5;
			}
		}
	}
	public static string Xbox3LStickButton {
		get {
			if (isLinux) {
				return Joystick3Button9;
			} else if (isOSX) {
				return Joystick3Button11;
			} else if (isWebGL) {
				return Joystick3Button10;
			} else {
				return Joystick3Button8;
			}
		}
	}
	public static string Xbox3RStickButton {
		get {
			if (isLinux) {
				return Joystick3Button10;
			} else if (isOSX) {
				return Joystick3Button12;
			} else if (isWebGL) {
				return Joystick3Button11;
			} else {
				return Joystick3Button9;
			}
		}
	}
	public static string Xbox3LStickLeft {
		get {
			return Joy3Axis1Negative;
		}
	}
	public static string Xbox3LStickRight {
		get {
			return Joy3Axis1Positive;
		}
	}
	public static string Xbox3LStickUp {
		get {
			return Joy3Axis2Negative;
		}
	}
	public static string Xbox3LStickDown {
		get {
			return Joy3Axis2Positive;
		}
	}
	public static string Xbox3RStickLeft {
		get {
			if (isOSX || isWebGL) {
				return Joy3Axis3Negative;
			} else {
				return Joy3Axis4Negative;
			}
		}
	}
	public static string Xbox3RStickRight {
		get {
			if (isOSX || isWebGL) {
				return Joy3Axis3Positive;
			} else {
				return Joy3Axis4Positive;
			}
		}
	}
	public static string Xbox3RStickUp {
		get {
			if (isOSX || isWebGL) {
				return Joy3Axis4Negative;
			} else {
				return Joy3Axis5Negative;
			}
		}
	}
	public static string Xbox3RStickDown {
		get {
			if (isOSX || isWebGL) {
				return Joy3Axis4Positive;
			} else {
				return Joy3Axis5Positive;
			}
		}
	}
	public static string Xbox3DPadLeft {
		get {
			if (isLinux) {
				return Joy3Axis7Negative;
			} else if (isOSX) {
				return Joystick3Button7;
			} else if (isWebGL) {
				return Joystick3Button14;
			} else {
				// Windows
				if (_jNamez.Length >= 3) {
					if (_jNamez[2].ToLower() == _xboxOneWin.ToLower()) {
						return Joy3Axis7Negative;
					}
				}

				return Joy3Axis6Negative;
			}
		}
	}
	public static string Xbox3DPadRight {
		get {
			if (isLinux) {
				return Joy3Axis7Positive;
			} else if (isOSX) {
				return Joystick3Button8;
			} else if (isWebGL) {
				return Joystick3Button15;
			} else {
				// Windows
				if (_jNamez.Length >= 3) {
					if (_jNamez[2].ToLower() == _xboxOneWin.ToLower()) {
						return Joy3Axis7Positive;
					}
				}

				return Joy3Axis6Positive;
			}
		}
	}
	public static string Xbox3DPadUp {
		get {
			if (isLinux) {
				return Joy3Axis8Positive;
			} else if (isOSX) {
				return Joystick3Button5;
			} else if (isWebGL) {
				return Joystick3Button12;
			} else {
				// Windows
				if (_jNamez.Length >= 3) {
					if (_jNamez[2].ToLower() == _xboxOneWin.ToLower()) {
						return Joy3Axis8Positive;
					}
				}

				return Joy3Axis7Positive;
			}
		}
	}
	public static string Xbox3DPadDown {
		get {
			if (isLinux) {
				return Joy3Axis8Negative;
			} else if (isOSX) {
				return Joystick3Button6;
			} else if (isWebGL) {
				return Joystick3Button13;
			} else {
				// Windows
				if (_jNamez.Length >= 3) {
					if (_jNamez[2].ToLower() == _xboxOneWin.ToLower()) {
						return Joy3Axis8Negative;
					}
				}

				return Joy3Axis7Negative;
			}
		}
	}
	public static string Xbox3TriggerLeft {
		get {
			if (isLinux) {
				return Joy3Axis3Positive;
			} else if (isOSX) {
				return Joy3Axis5Positive;
			} else if (isWebGL) {
				return Joystick3Button6;
			} else {
				return Joy3Axis9Positive;
			}
		}
	}
	public static string Xbox3TriggerRight {
		get {
			if (isOSX || isLinux) {
				return Joy3Axis6Positive;
			} else if (isWebGL) {
				return Joystick3Button7;
			} else {
				return Joy3Axis10Positive;
			}
		}
	}

	#endregion //Xbox Controller 3

	#region Xbox Controller 4

	public static string Xbox4A {
		get {
			if (isOSX) {
				return Joystick4Button16;
			} else {
				return Joystick4Button0;
			}
		}
	}
	public static string Xbox4B {
		get {
			if (isOSX) {
				return Joystick4Button17;
			} else {
				return Joystick4Button1;
			}
		}
	}
	public static string Xbox4X {
		get {
			if (isOSX) {
				return Joystick4Button18;
			} else {
				return Joystick4Button2;
			}
		}
	}
	public static string Xbox4Y {
		get {
			if (isOSX) {
				return Joystick4Button19;
			} else {
				return Joystick4Button3;
			}
		}
	}
	public static string Xbox4Back {
		get {
			if (isOSX) {
				return Joystick4Button10;
			} else if (isWebGL) {
				return Joystick4Button8;
			} else {
				return Joystick4Button6;
			}
		}
	}
	public static string Xbox4View {
		get { return Xbox4Back; }
	}
	public static string Xbox4Start {
		get {
			if (isOSX || isWebGL) {
				return Joystick4Button9;
			} else {
				return Joystick4Button7;
			}
		}
	}
	public static string Xbox4Menu {
		get { return Xbox4Start; }
	}
	public static string Xbox4BumperLeft {
		get {
			if (isOSX) {
				return Joystick4Button13;
			} else {
				return Joystick4Button4;
			}
		}
	}
	public static string Xbox4BumperRight {
		get {
			if (isOSX) {
				return Joystick4Button14;
			} else {
				return Joystick4Button5;
			}
		}
	}
	public static string Xbox4LStickButton {
		get {
			if (isLinux) {
				return Joystick4Button9;
			} else if (isOSX) {
				return Joystick4Button11;
			} else if (isWebGL) {
				return Joystick4Button10;
			} else {
				return Joystick4Button8;
			}
		}
	}
	public static string Xbox4RStickButton {
		get {
			if (isLinux) {
				return Joystick4Button10;
			} else if (isOSX) {
				return Joystick4Button12;
			} else if (isWebGL) {
				return Joystick4Button11;
			} else {
				return Joystick4Button9;
			}
		}
	}
	public static string Xbox4LStickLeft {
		get {
			return Joy4Axis1Negative;
		}
	}
	public static string Xbox4LStickRight {
		get {
			return Joy4Axis1Positive;
		}
	}
	public static string Xbox4LStickUp {
		get {
			return Joy4Axis2Negative;
		}
	}
	public static string Xbox4LStickDown {
		get {
			return Joy4Axis2Positive;
		}
	}
	public static string Xbox4RStickLeft {
		get {
			if (isOSX || isWebGL) {
				return Joy4Axis3Negative;
			} else {
				return Joy4Axis4Negative;
			}
		}
	}
	public static string Xbox4RStickRight {
		get {
			if (isOSX || isWebGL) {
				return Joy4Axis3Positive;
			} else {
				return Joy4Axis4Positive;
			}
		}
	}
	public static string Xbox4RStickUp {
		get {
			if (isOSX || isWebGL) {
				return Joy4Axis4Negative;
			} else {
				return Joy4Axis5Negative;
			}
		}
	}
	public static string Xbox4RStickDown {
		get {
			if (isOSX || isWebGL) {
				return Joy4Axis4Positive;
			} else {
				return Joy4Axis5Positive;
			}
		}
	}
	public static string Xbox4DPadLeft {
		get {
			if (isLinux) {
				return Joy4Axis7Negative;
			} else if (isOSX) {
				return Joystick4Button7;
			} else if (isWebGL) {
				return Joystick4Button14;
			} else {
				// Windows
				if (_jNamez.Length >= 4) {
					if (_jNamez[3].ToLower() == _xboxOneWin.ToLower()) {
						return Joy4Axis7Negative;
					}
				}

				return Joy4Axis6Negative;
			}
		}
	}
	public static string Xbox4DPadRight {
		get {
			if (isLinux) {
				return Joy4Axis7Positive;
			} else if (isOSX) {
				return Joystick4Button8;
			} else if (isWebGL) {
				return Joystick4Button15;
			} else {
				// Windows
				if (_jNamez.Length >= 4) {
					if (_jNamez[3].ToLower() == _xboxOneWin.ToLower()) {
						return Joy4Axis7Positive;
					}
				}

				return Joy4Axis6Positive;
			}
		}
	}
	public static string Xbox4DPadUp {
		get {
			if (isLinux) {
				return Joy4Axis8Positive;
			} else if (isOSX) {
				return Joystick4Button5;
			} else if (isWebGL) {
				return Joystick4Button12;
			} else {
				// Windows
				if (_jNamez.Length >= 4) {
					if (_jNamez[3].ToLower() == _xboxOneWin.ToLower()) {
						return Joy4Axis8Positive;
					}
				}

				return Joy4Axis7Positive;
			}
		}
	}
	public static string Xbox4DPadDown {
		get {
			if (isLinux) {
				return Joy4Axis8Negative;
			} else if (isOSX) {
				return Joystick4Button6;
			} else if (isWebGL) {
				return Joystick4Button13;
			} else {
				// Windows
				if (_jNamez.Length >= 4) {
					if (_jNamez[3].ToLower() == _xboxOneWin.ToLower()) {
						return Joy4Axis8Negative;
					}
				}

				return Joy4Axis7Negative;
			}
		}
	}
	public static string Xbox4TriggerLeft {
		get {
			if (isLinux) {
				return Joy4Axis3Positive;
			} else if (isOSX) {
				return Joy4Axis5Positive;
			} else if (isWebGL) {
				return Joystick4Button6;
			} else {
				return Joy4Axis9Positive;
			}
		}
	}
	public static string Xbox4TriggerRight {
		get {
			if (isOSX || isLinux) {
				return Joy4Axis6Positive;
			} else if (isWebGL) {
				return Joystick4Button7;
			} else {
				return Joy4Axis10Positive;
			}
		}
	}

	#endregion //Xbox Controller 4

	#endregion //Xbox Controls

}

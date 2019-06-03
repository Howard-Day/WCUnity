#define Use_cInputGUI // Comment out this line to use your own GUI instead of cInput's built-in GUI.

#region Namespaces

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

#endregion

/***********************************************************************
 *  cInput 2.9.1 by cMonkeys
 *  This script is NOT free, unlike Custom Inputmanager 1.x.
 *  Therefore the use of this script is strictly personal and 
 *  may not be spread without permission.
 *  
 *  Any technical or license questions can be mailed
 *  to support@cMonkeys.com, but please read the 
 *  included help documents first.
 ***********************************************************************/

public class cInput : MonoBehaviour {

	#region cInput Variables and Properties

	#region Public Variables/Properties

#if Use_cInputGUI

	[Obsolete("Please use cGUI.cSkin.")]
	public static GUISkin cSkin {
		get {
			Debug.LogWarning("cInput.cSkin has been deprecated. Please use cGUI.cSkin.");
			return cGUI.cSkin;
		}
		set {
			Debug.LogWarning("cInput.cSkin has been deprecated. Please use cGUI.cSkin.");
			cGUI.cSkin = value;
		}
	} // cSkin is DEPRECATED!

#endif

	public static float gravity = 3;
	public static float sensitivity = 3;
	public static float deadzone = 0.001f;

	/// <summary>An event that fires whenever a key is changed with ChangeKey.</summary>
	public static event System.Action OnKeyChanged;
	/// <summary>Whether or not cInput is waiting for an input to be pressed.</summary>
	public static bool scanning { get { return _scanning; } set { _scanning = value; } }
	/// <summary>How many inputs have been defined in cInput. Read-only.</summary>
	public static int length {
		get {
			_cInputInit(); // if cInput doesn't exist, create it
			return _inputLength + 1;
		}
	} // length is read-only
	/// <summary>Should we allow the same input to be used for multiple cInput actions?</summary>
	public static bool allowDuplicates {
		get {
			_cInputInit(); // if cInput doesn't exist, create it
			return _allowDuplicates;
		}
		set {
			_allowDuplicates = value;
			if (_usePlayerPrefs) { PlayerPrefs.SetString("cInput_dubl", value.ToString()); }
			_exAllowDuplicates = value.ToString();
		}
	}

	/// <summary>Should cInput automatically save and load to/from PlayerPrefs?</summary>
	public static bool usePlayerPrefs { get { return _usePlayerPrefs; } set { _usePlayerPrefs = value; } }

	#region anyKey and anyKeyDown properties

	/// <summary>Is any key, mouse button, or key defined with cInput.SetKey currently held down? (Read Only)</summary>
	public static bool anyKey {
		get {
			_cInputInit();
			if (Input.anyKey) {
				return true;
			} else {
				foreach (KeyValuePair<int, int> kvp in _inputNameHash) {
					if (cInput.GetKey(kvp.Key)) {
						return true;
					}
				}
			}

			return false;
		}
	}

	/// <summary>Returns true the first frame the user hits any key, mouse button, or key defined with cInput.SetKey. (Read Only)</summary>
	public static bool anyKeyDown {
		get {
			_cInputInit();
			if (Input.anyKeyDown) {
				return true;
			} else {
				foreach (KeyValuePair<int, int> kvp in _inputNameHash) {
					if (cInput.GetKeyDown(kvp.Key)) {
						return true;
					}
				}
			}

			return false;
		}
	}

	#endregion //AnyKey and AnyKeyDown functions

	#endregion //Public Variables/Properties

	#region Private Variables

	private const int MAX_INPUTS = 250; // max number of custom inputs (SetKey)
	private static bool _allowDuplicates = false;
	private static string[,] _defaultStrings = new string[MAX_INPUTS, 5];
	private static Dictionary<int, int> _inputNameHash = new Dictionary<int, int>(); // a hash reference to the array index
	private static string[] _inputName = new string[MAX_INPUTS]; // name of the input action (e.g., "Jump")
	private static KeyCode[] _inputPrimary = new KeyCode[MAX_INPUTS]; // primary input assigned to action (e.g., "Space")
	private static KeyCode[] _modifierUsedPrimary = new KeyCode[MAX_INPUTS]; // modfier used on primary input
	private static KeyCode[] _inputSecondary = new KeyCode[MAX_INPUTS]; // secondary input assigned to action
	private static KeyCode[] _modifierUsedSecondary = new KeyCode[MAX_INPUTS]; // modfier used on secondary input
	private static List<KeyCode> _modifiers = new List<KeyCode>(); // list that holds the allowed modifiers
	private static List<int> _markedAsAxis = new List<int>(); // list that keeps track of which actions are used to make axis
	private static Dictionary<int, int> _axisNameHash = new Dictionary<int, int>(); // a hash reference to the array index
	private static string[] _axisName = new string[MAX_INPUTS];
	private static string[] _axisPrimary = new string[MAX_INPUTS];
	private static string[] _axisSecondary = new string[MAX_INPUTS];
	private static float[] _individualAxisSens = new float[MAX_INPUTS]; // individual axis sensitivity settings
	private static float[] _individualAxisGrav = new float[MAX_INPUTS]; // individual axis gravity settings
	private static float[] _individualAxisDead = new float[MAX_INPUTS]; // individual axis gravity settings
	private static bool[] _invertAxis = new bool[MAX_INPUTS];
	private static int[,] _makeAxis = new int[MAX_INPUTS, 2]; // stores the index of Keys used in the axis
	private static int _inputLength = -1;
	private static int _axisLength = -1;
	private static List<KeyCode> _forbiddenKeys = new List<KeyCode>();
	private static List<string> _forbiddenAxes = new List<string>();

	private static bool[] _getKeyArray = new bool[MAX_INPUTS]; // values stored for GetKey function
	private static bool[] _getKeyDownArray = new bool[MAX_INPUTS]; // values stored for GetKeyDown
	private static bool[] _getKeyUpArray = new bool[MAX_INPUTS]; // values stored for GetKeyUp
	private static bool[] _axisTriggerArrayPrimary = new bool[MAX_INPUTS]; // values that help to check if an axis is up or down
	private static bool[] _axisTriggerArraySecondary = new bool[MAX_INPUTS]; // values that help to check if secondary input for an axis is up or down
	private static float[] _getAxis = new float[MAX_INPUTS];
	private static float[] _getAxisRaw = new float[MAX_INPUTS];
	private static float[] _getAxisArray = new float[MAX_INPUTS];
	private static float[] _getAxisArrayRaw = new float[MAX_INPUTS];
	private static int[] _tmpCalibratedAxisIndices = new int[] { }; // used to avoid creating a new int[] in _GetCalibratedAxisInput each frame (no GC!)

	// which types of inputs to allow when assigning inputs to actions
	private static bool _allowMouseAxis = false;
	private static bool _allowMouseButtons = true;
	private static bool _allowJoystickButtons = true;
	private static bool _allowJoystickAxis = true;
	private static bool _allowKeyboard = true;

	private static int _numGamepads = 4; // number of gamepads supported by built-in Input Manager settings

	/// <summary>Are we scanning inputs to make a new assignment?</summary>
	private static bool _scanning;
	/// <summary>Which index number of the array for inputs to scan for</summary>
	private static int _cScanIndex;
	/// <summary>Which input are we scanning for (primary (1) or secondary (2))?</summary>
	private static int _cScanInput;
	/// <summary>A reference to the cInput instance.</summary>
	private static cInput _cObject;
#if Use_cInputGUI
	/// <summary>A reference to the cInputGUI instance.</summary>
	private static cInputGUI _cInputGUIObject;
#endif
	private static bool _useGUI = true;
	private static bool _cKeysLoaded;
	/// <summary>This is used to store all axis raw values so we can see if they changed since scanning began</summary>
	private static Dictionary<string, float> _axisRawValues = new Dictionary<string, float>();

	// External saving related variables
	private static string _exAllowDuplicates;
	private static string _exAxis;
	private static string _exAxisSensitivity;
	private static string _exAxisGravity;
	private static string _exAxisDeadzone;
	private static string _exAxisInverted;
	private static string _exDefaults;
	private static string _exInputs;
	private static string _exCalibrations;
	private static string _exCalibrationValues;
	private static bool _externalSaving = false;
	// Save to PlayerPrefs?
	private static bool _usePlayerPrefs = true;

	private static Dictionary<string, KeyCode> _string2Key = new Dictionary<string, KeyCode>();

	private static int[] _axisType = new int[11 * (_numGamepads + 1)];
	/// <summary>The value that should be considered 0 for this axis</summary>
	private static Dictionary<string, float> _axisCalibrationOffset = new Dictionary<string, float>();
	// "Gamepad 0" is input from any gamepad
	private static string[,] _joyStrings = new string[_numGamepads + 1, 11];
	private static string[,] _joyStringsPos = new string[_numGamepads + 1, 11];
	private static string[,] _joyStringsNeg = new string[_numGamepads + 1, 11];
	// these three below dictionaries are used to get quick references to the indices to the above arrays
	private static Dictionary<string, int[]> _joyStringsIndices = new Dictionary<string, int[]>();
	private static Dictionary<string, int[]> _joyStringsPosIndices = new Dictionary<string, int[]>();
	private static Dictionary<string, int[]> _joyStringsNegIndices = new Dictionary<string, int[]>();

	#endregion //Private Variables

	#endregion // cInput Variables and Properties

	#region Awake/Start/Update functions

	void Awake() {
		DontDestroyOnLoad(gameObject); // Keep this thing from getting destroyed if we change levels.

		// set values to global values
		for (int n = 0; n < MAX_INPUTS; n++) {
			_individualAxisSens[n] = -99;
			_individualAxisGrav[n] = -99;
			_individualAxisDead[n] = -99;
		}
	}

	void Start() {
		_CreateDictionary();
		if (!_cKeysLoaded) {
			if (_externalSaving) {
				_LoadExternalInputs();
				//Debug.Log("cInput loaded inputs from external source.");
			} else {
				_LoadInputs();
				//Debug.Log("cInput settings loaded inputs from PlayerPrefs.");
			}
		}

		AddModifier(KeyCode.None); // we need to initialize the modifiers with this one
	}

	void Update() {
		if (_scanning) {
			if (_cScanInput != 0) {
				// scan for a button press to assign a key (using the GUI)
				_InputScans();
			} else {
				// this is the part where a button is actually assigned after scanning is complete
				string _prim;
				string _sec;

				if (string.IsNullOrEmpty(_axisPrimary[_cScanIndex])) {
					_prim = _inputPrimary[_cScanIndex].ToString();
				} else {
					_prim = _axisPrimary[_cScanIndex];
				}

				if (string.IsNullOrEmpty(_axisSecondary[_cScanIndex])) {
					_sec = _inputSecondary[_cScanIndex].ToString();
				} else {
					_sec = _axisSecondary[_cScanIndex];
				}

				_ChangeKey(_cScanIndex, _inputName[_cScanIndex], _prim, _sec);
				_scanning = false;
				if (OnKeyChanged != null) { OnKeyChanged(); }
			}
		}

		// update values for all inputs
		_CheckInputs();
	}

	#endregion

	#region Init Related and CreateDictionary functions

	/// <summary>Initializes the cInput object.</summary>
	public static void Init() {
		_cInputInit(); // if cInput doesn't exist, create it
	}

	public static void Init(bool useGUI) {
		cInput._useGUI = false;
		_cInputInit();
	}

	private static void _cInputInit() {
		if (_cObject == null) {
			GameObject cObject = GameObject.Find("cObject");
			if (!cObject) {
				// We need to create a GameObject named cObject
				cObject = new GameObject();
				cObject.name = "cObject";
			}

			// make sure the GameObject also has the cInput component attached
			if (cObject.GetComponent<cInput>() == null) {
				_cObject = cObject.AddComponent<cInput>();
			}
		}

#if Use_cInputGUI
		if (_useGUI) {
			// make sure the GameObject also has the cInputGUI component attached
			if (!_cInputGUIObject) {
				// get a reference to the component if it already exists
				_cInputGUIObject = _cObject.GetComponent<cInputGUI>();
				if (!_cInputGUIObject) {
					// if there's still no reference, the component needs to be added
					_cObject.gameObject.AddComponent<cInputGUI>();
				}
			}
		}
#endif

	}

	private static void _CreateDictionary() {
		if (_string2Key.Count == 0) { // don't create the dictionary more than once
			for (int i = (int)KeyCode.None; i <= (int)KeyCode.Joystick4Button19; i++) {
				KeyCode key = (KeyCode)i;
				_string2Key.Add(key.ToString(), key);
			}

			// Create joystrings dictionaries
			for (int gamepad = 0; gamepad <= _numGamepads; gamepad++) {
				for (int axis = 1; axis <= 10; axis++) {
					StringBuilder joyString;
					if (gamepad == 0) {
						// "Gamepad 0" is input from any gamepad
						joyString = new StringBuilder("Joy Axis " + axis);
					} else {
						joyString = new StringBuilder("Joy" + gamepad + " Axis " + axis);
					}

					_joyStrings[gamepad, axis] = joyString.ToString();
					_joyStringsIndices.Add(joyString.ToString(), new[] { gamepad, axis });
					joyString.Append("+");
					_joyStringsPos[gamepad, axis] = joyString.ToString();
					_joyStringsPosIndices.Add(joyString.ToString(), new[] { gamepad, axis });
					joyString.Replace('+', '-');
					_joyStringsNeg[gamepad, axis] = joyString.ToString();
					_joyStringsNegIndices.Add(joyString.ToString(), new[] { gamepad, axis });
				}
			}
		}
	}

	#endregion //Init Related and CreateDictionary functions

	#region ForbidKey & ForbidAxis functions

	/// <summary>Forbids a key from being bound using the GUI.</summary>
	/// <param name="key">The KeyCode of the key to forbid.</param>
	public static void ForbidKey(KeyCode key) {
		_cInputInit(); // if cInput doesn't exist, create it
		if (!_forbiddenKeys.Contains(key)) {
			if (key != KeyCode.None) {
				_forbiddenKeys.Add(key);
			}
		}
	}

	/// <summary>Forbids a key from being bound using the GUI.</summary>
	/// <param name="keyString">The string name of the key to forbid.</param>
	public static void ForbidKey(string keyString) {
		_cInputInit(); // if cInput doesn't exist, create it
		ForbidKey(_String2KeyCode(keyString));
	}

	public static void ForbidAxis(string axis) {
		_cInputInit(); // if cInput doesn't exist, create it

		if (_IsAxisValid(axis)) {
			if (!_forbiddenAxes.Contains(axis)) {
				_forbiddenAxes.Add(axis);
			}
		} else {
			Debug.LogWarning("Couldn't forbid axis " + axis + ". Not a valid axis.");
		}
	}

	#endregion //ForbidKey & ForbidAxis functions

	#region AddModifier and RemoveModifier functions

	/// <summary>Designates a key for use as a modifier.</summary>
	/// <param name="modifierKey">The KeyCode for the key to be used as a modifier.</param>
	public static void AddModifier(KeyCode modifierKey) {
		_cInputInit(); // if cInput doesn't exist, create it
		if (!_modifiers.Contains(modifierKey)) {
			_modifiers.Add(modifierKey);
		}
	}

	/// <summary>Designates a key for use as a modifier.</summary>
	/// <param name="modifier">The string name of the key to be used as a modifier.</param>
	public static void AddModifier(string modifier) {
		_cInputInit(); // if cInput doesn't exist, create it
		AddModifier(_String2KeyCode(modifier));
	}

	/// <summary>Removes a key for use as a modifier.</summary>
	/// <param name="modifierKey">The KeyCode for the key which should no longer be used as a modifier.</param>
	public static void RemoveModifier(KeyCode modifierKey) {
		_cInputInit(); // if cInput doesn't exist, create it
		if (_modifiers.Contains(modifierKey)) {
			_modifiers.Remove(modifierKey);
		}
	}

	/// <summary>Removes a key for use as a modifier.</summary>
	/// <param name="modifier">The string name of the key which should no longer be used as a modifier.</param>
	public static void RemoveModifier(string modifier) {
		_cInputInit(); // if cInput doesn't exist, create it
		RemoveModifier(_String2KeyCode(modifier));
	}

	#endregion

	#region SetKey functions

	#region SetKey Overloaded Functions

	/// <summary>Defines a cInput action.</summary>
	/// <param name="description">The name of the cInput action.</param>
	/// <param name="primary">The input to use as the primary source.</param>
	/// <returns>Returns the HashCode (int) of the cInput action name.</returns>
	public static int SetKey(string description, string primary) {
		// note that Keys.None is used here for secondary and secondaryModifier because there is no secondary input
		return SetKey(description, primary, Keys.None, primary, Keys.None);
	}

	/// <summary>Defines a cInput action with a secondary input.</summary>
	/// <param name="description">The name of the cInput action.</param>
	/// <param name="primary">The input to use as the primary source.</param>
	/// <param name="secondary">The input to use as the secondary source.</param>
	/// <returns>Returns the HashCode (int) of the cInput action name.</returns>
	public static int SetKey(string description, string primary, string secondary) {
		return SetKey(description, primary, secondary, primary, secondary);
	}

	/// <summary>Defines a cInput action with a modifier on the primary input.</summary>
	/// <param name="description">The name of the cInput action.</param>
	/// <param name="primary">The input to use as the primary source.</param>
	/// <param name="secondary">The input to use as the secondary source.</param>
	/// <param name="primaryModifier">Require this modifier to be pressed for the primary input.</param>
	/// <returns>Returns the HashCode (int) of the cInput action name.</returns>
	public static int SetKey(string description, string primary, string secondary, string primaryModifier) {
		return SetKey(description, primary, secondary, primaryModifier, secondary);
	}

	#endregion //SetKey Overloaded Functions

	/// <summary>Defines a cInput action with modifiers.</summary>
	/// <param name="description">The name of the cInput action.</param>
	/// <param name="primary">The input to use as the primary source.</param>
	/// <param name="secondary">The input to use as the secondary source.</param>
	/// <param name="primaryModifier">Require this modifier to be pressed for the primary input.</param>
	/// <param name="secondaryModifier">Require this modifier to be pressed for the secondary input.</param>
	/// <returns>Returns the HashCode (int) of the cInput action name.</returns>
	public static int SetKey(string description, string primary, string secondary, string primaryModifier, string secondaryModifier) {
		_cInputInit(); // if cInput doesn't exist, create it

		// make sure we pass valid values for the modifiers
		if (String.IsNullOrEmpty(primaryModifier) || primaryModifier == Keys.None) { primaryModifier = primary; }
		if (String.IsNullOrEmpty(secondaryModifier) || secondaryModifier == Keys.None) { secondaryModifier = secondary; }

		int index = _FindKeyByDescription(description);

		// make sure this key hasn't already been set
		if (index == -1) {
			int _num = _inputLength + 1;
			// actually set the key
			_SetDefaultKey(_num, description, primary, secondary, primaryModifier, secondaryModifier);
		} else {
#if UNITY_EDITOR
			// skip this warning if an input with the same settings already exists
			int pStringHash = (primaryModifier != primary) ? (primaryModifier + " + " + primary).GetHashCode() : primary.GetHashCode();
			int sStringHash = (secondaryModifier != secondary) ? (secondaryModifier + " + " + secondary).GetHashCode() : secondary.GetHashCode();
			if (!(pStringHash == GetText(index, 1).GetHashCode() && sStringHash == GetText(index, 2).GetHashCode())) {
				// also skip this warning if we loaded from an external source or we already created the cInput object
				if (_externalSaving == false || _cObject == null) {
					// Whoops! Key with this name already exists!
					Debug.LogWarning("A key with the name of " + description + " already exists. You should use ChangeKey() if you want to change an existing key!\n" +
						"This message will only be shown in the editor and is safe to ignore if you're reloading a scene/script that initializes the " + description + " input.");
				}
			}
#endif
		}

		return description.GetHashCode();
	}

	private static void _SetDefaultKey(int _num, string _name, string _input1, string _input2, string pMod, string sMod) {
		_defaultStrings[_num, 0] = _name;
		_defaultStrings[_num, 1] = _input1;
		_defaultStrings[_num, 2] = (string.IsNullOrEmpty(_input2)) ? KeyCode.None.ToString() : _input2;
		_defaultStrings[_num, 3] = string.IsNullOrEmpty(pMod) ? _input1 : pMod;
		_defaultStrings[_num, 4] = string.IsNullOrEmpty(sMod) ? _input2 : sMod;

		// store a hash of the key name
		int hashCode = _name.GetHashCode();
		if (!_inputNameHash.ContainsKey(hashCode)) {
			_inputNameHash.Add(hashCode, _num);
		}

		if (_num > _inputLength) { _inputLength = _num; }

		_modifierUsedPrimary[_num] = _String2KeyCode(_defaultStrings[_num, 3]);
		_modifierUsedSecondary[_num] = _String2KeyCode(_defaultStrings[_num, 4]);
		_SetKey(_num, _name, _input1, _input2);
		_SaveDefaults();
	}

	private static void _SetKey(int _num, string _name, string _input1, string _input2) {
		// input description
		_inputName[_num] = _name;

		_axisPrimary[_num] = "";

		if (_string2Key.Count == 0) { return; }

		if (!string.IsNullOrEmpty(_input1)) {
			// enter keyboard input in the input array
			KeyCode _keyCode1 = _String2KeyCode(_input1);
			_inputPrimary[_num] = _keyCode1;

			// enter mouse and gamepad axis inputs in the inputstring array
			string axisName = _ChangeStringToAxisName(_input1);
			if (_input1 != axisName) {
				_axisPrimary[_num] = _input1;
			}
		}

		_axisSecondary[_num] = "";

		if (!string.IsNullOrEmpty(_input2)) {
			// enter input in the alt input array
			KeyCode _keyCode2 = _String2KeyCode(_input2);
			_inputSecondary[_num] = _keyCode2;

			// enter mouse and gamepad axis inputs in the inputstring array
			string axisName = _ChangeStringToAxisName(_input2);
			if (_input2 != axisName) {
				_axisSecondary[_num] = _input2;
			}
		}
	}

	#endregion //SetKey functions

	#region SetAxis and SetAxisDeadzone/Gravity/Sensitivity functions

	#region Overloaded SetAxis Functions

	/// <summary>Define a cInput axis with only one input.</summary>
	/// <param name="description">The name of the axis.</param>
	/// <param name="action">The cInput key (action) to use as the value for this axis.</param>
	/// <returns>Returns the HashCode (int) of the cInput axis.</returns>
	public static int SetAxis(string description, string action) {
		return SetAxis(description, action, "-1", sensitivity, gravity, deadzone);
	}

	/// <summary>Define a cInput axis with only one input, and set sensitivity.</summary>
	/// <param name="description">The name of the axis.</param>
	/// <param name="action">The cInput key to use as the value for this axis.</param>
	/// <param name="axisSensitivity">The sensitivity of the axis (how fast it goes to 1).</param>
	/// <returns>Returns the HashCode (int) of the cInput axis.</returns>
	public static int SetAxis(string description, string action, float axisSensitivity) {
		return SetAxis(description, action, "-1", axisSensitivity, gravity, deadzone);
	}

	/// <summary>Define a cInput axis with only one input, and set sensitivity and gravity.</summary>
	/// <param name="description">The name of the axis.</param>
	/// <param name="action">The cInput key to use as the value for this axis.</param>
	/// <param name="axisSensitivity">The sensitivity of the axis (how fast it goes to 1).</param>
	/// <param name="axisGravity">The gravity of the axis (how fast it returns to 0).</param>
	/// <returns>Returns the HashCode (int) of the cInput axis.</returns>
	public static int SetAxis(string description, string action, float axisSensitivity, float axisGravity) {
		return SetAxis(description, action, "-1", axisSensitivity, axisGravity, deadzone);
	}

	/// <summary>Define a cInput axis with only one input, and set sensitivity, gravity, and deadzone.</summary>
	/// <param name="description">The name of the axis.</param>
	/// <param name="action">The cInput key to use as the value for this axis.</param>
	/// <param name="axisSensitivity">The sensitivity of the axis (how fast it goes to 1).</param>
	/// <param name="axisGravity">The gravity of the axis (how fast it returns to 0).</param>
	/// <param name="axisDeadzone">The deadzone of the axis (ignore values less than this value).</param>
	/// <returns>Returns the HashCode (int) of the cInput axis.</returns>
	public static int SetAxis(string description, string action, float axisSensitivity, float axisGravity, float axisDeadzone) {
		return SetAxis(description, action, "-1", axisSensitivity, axisGravity, axisDeadzone);
	}

	/// <summary>Define a cInput axis with negative and positive inputs.</summary>
	/// <param name="description">The name of the axis.</param>
	/// <param name="negativeInput">The cInput key to use as the negative value for this axis.</param>
	/// <param name="positiveInput">The cInput key to use as the positive value for this axis.</param>
	/// <returns>Returns the HashCode (int) of the cInput axis.</returns>
	public static int SetAxis(string description, string negativeInput, string positiveInput) {
		return SetAxis(description, negativeInput, positiveInput, sensitivity, gravity, deadzone);
	}

	/// <summary>Define a cInput axis with negative and positive inputs, also setting sensitivity.</summary>
	/// <param name="description">The name of the axis.</param>
	/// <param name="negativeInput">The cInput key to use as the negative value for this axis.</param>
	/// <param name="positiveInput">The cInput key to use as the positive value for this axis.</param>
	/// <param name="axisSensitivity">The sensitivity of the axis (how fast it goes to 1).</param>
	/// <returns>Returns the HashCode (int) of the cInput axis.</returns>
	public static int SetAxis(string description, string negativeInput, string positiveInput, float axisSensitivity) {
		return SetAxis(description, negativeInput, positiveInput, axisSensitivity, gravity, deadzone);
	}

	/// <summary>Define a cInput axis with negative and positive inputs, also setting sensitivity and gravity.</summary>
	/// <param name="description">The name of the axis.</param>
	/// <param name="negativeInput">The cInput key to use as the negative value for this axis.</param>
	/// <param name="positiveInput">The cInput key to use as the positive value for this axis.</param>
	/// <param name="axisSensitivity">The sensitivity of the axis (how fast it goes to 1).</param>
	/// <param name="axisGravity">The gravity of the axis (how fast it returns to 0).</param>
	/// <returns>Returns the HashCode (int) of the cInput axis.</returns>
	public static int SetAxis(string description, string negativeInput, string positiveInput, float axisSensitivity, float axisGravity) {
		return SetAxis(description, negativeInput, positiveInput, axisSensitivity, axisGravity, deadzone);
	}

	#endregion //Overloaded SetAxis Functions

	/// <summary>Define a cInput axis with negative and positive inputs, also setting sensitivity, gravity, and deadzone.</summary>
	/// <param name="description">The name of the axis.</param>
	/// <param name="negativeInput">The cInput key to use as the negative value for this axis.</param>
	/// <param name="positiveInput">The cInput key to use as the positive value for this axis.</param>
	/// <param name="axisSensitivity">The sensitivity of the axis (how fast it goes to 1).</param>
	/// <param name="axisGravity">The gravity of the axis (how fast it returns to 0).</param>
	/// <param name="axisDeadzone">The deadzone of the axis (ignore values less than this value).</param>
	/// <returns>Returns the HashCode (int) of the cInput axis.</returns>
	public static int SetAxis(string description, string negativeInput, string positiveInput, float axisSensitivity, float axisGravity, float axisDeadzone) {
		_cInputInit(); // if cInput doesn't exist, create it
		if (IsKeyDefined(negativeInput)) {
			int _num = _FindAxisByDescription(description); // overwrite existing axis of same name
			if (_num == -1) {
				// this axis doesn't exist, so make a new one
				_num = _axisLength + 1;
			}

			int posInput = -1; // -1 by default, which means no input.
			int negInput = _FindKeyByDescription(negativeInput);

			if (IsKeyDefined(positiveInput)) {
				posInput = _FindKeyByDescription(positiveInput);
				if (!_markedAsAxis.Contains(posInput)) {
					// mark this posInput as used for Axis
					_markedAsAxis.Add(posInput);
				}
			} else if (positiveInput != "-1") {
				// the key isn't defined and we're not passing in -1 as a value, so there's a problem
				Debug.LogError("Can't define Axis named: " + description + ". Please define '" + positiveInput + "' with SetKey() first.");
				return description.GetHashCode(); // break out of this function without trying to assign the axis
			}

			if (!_markedAsAxis.Contains(negInput)) {
				// mark this negInput as used for Axis
				_markedAsAxis.Add(negInput);
			}

			_SetAxis(_num, description, negInput, posInput);
			_individualAxisSens[negInput] = axisSensitivity;
			_individualAxisGrav[negInput] = axisGravity;
			_individualAxisDead[negInput] = axisDeadzone;
			if (posInput >= 0) {
				_individualAxisSens[posInput] = axisSensitivity;
				_individualAxisGrav[posInput] = axisGravity;
				_individualAxisDead[posInput] = axisDeadzone;
			}
		} else {
			Debug.LogError("Can't define Axis named: " + description + ". Please define '" + negativeInput + "' with SetKey() first.");
		}

		return description.GetHashCode();
	}

	private static void _SetAxis(int _num, string _description, int _negative, int _positive) {
		if (_num > _axisLength) {
			_axisLength = _num;
		}

		// store a hash of the key name
		int hashCode = _description.GetHashCode();
		if (!_axisNameHash.ContainsKey(hashCode)) {
			_axisNameHash.Add(hashCode, _num);
		}

		_axisName[_num] = _description;
		_makeAxis[_num, 0] = _negative;
		_makeAxis[_num, 1] = _positive;
		_SaveAxis();
	}

	#region Deadzone, Gravity, and Sensitivity functions

	#region Public functions which just call the private functions

	/// <summary>Sets the sensitivity of an axis directly (after the axis has been defined).</summary>
	/// <param name="axisName">The name of the axis.</param>
	/// <param name="sensitivity">The value to set the sensitivity to.</param>
	public static void SetAxisSensitivity(string axisName, float sensitivity) {
		_SetAxisSensitivity(axisName.GetHashCode(), sensitivity, axisName);
	}

	/// <summary>Sets the sensitivity of an axis directly (after the axis has been defined).</summary>
	/// <param name="axisHash">The hashcode of the name of the axis.</param>
	/// <param name="sensitivity">The value to set the sensitivity to.</param>
	public static void SetAxisSensitivity(int axisHash, float sensitivity) {
		_SetAxisSensitivity(axisHash, sensitivity);
	}

	/// <summary>Sets the gravity of an axis directly (after the axis has been defined).</summary>
	/// <param name="axisHash">The name of the axis.</param>
	/// <param name="gravity">The value to set the gravity to.</param>
	public static void SetAxisGravity(string axisName, float gravity) {
		_SetAxisGravity(axisName.GetHashCode(), gravity, axisName);
	}

	/// <summary>Sets the gravity of an axis directly (after the axis has been defined).</summary>
	/// <param name="axisHash">The hashcode of the name of the axis.</param>
	/// <param name="gravity">The value to set the gravity to.</param>
	public static void SetAxisGravity(int axisHash, float gravity) {
		_SetAxisGravity(axisHash, gravity);
	}

	/// <summary>Sets the deadzone of an axis directly (after the axis has been defined).</summary>
	/// <param name="axisName">The name of the axis.</param>
	/// <param name="deadzone">The value to set deadzone to.</param>
	public static void SetAxisDeadzone(string axisName, float deadzone) {
		_SetAxisDeadzone(axisName.GetHashCode(), deadzone, axisName);
	}

	/// <summary>Sets the deadzone of an axis directly (after the axis has been defined).</summary>
	/// <param name="axisHash">The hashcode of the name of the axis.</param>
	/// <param name="deadzone">The value to set deadzone to.</param>
	public static void SetAxisDeadzone(int axisHash, float deadzone) {
		_SetAxisDeadzone(axisHash, deadzone);
	}

	#endregion //Public functions which just call the private functions

	#region Private functions which actually do the work

	private static void _SetAxisSensitivity(int hash, float sensitivity, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		int axis = _FindAxisByHash(hash);
		if (axis == -1) {
			// axis not defined!
			string errorText = (!string.IsNullOrEmpty(description)) ? description : "axis matching hashcode of " + hash;
			Debug.LogError("Cannot set sensitivity of " + errorText + ". Have you defined this axis with SetAxis() yet?");
		} else {
			// axis has been defined
			_individualAxisSens[_makeAxis[axis, 0]] = sensitivity;
			// don't try to set value for positive input if this axis only has negative input
			if (_makeAxis[axis, 1] >= 0) {
				if (_FindKeyByHash(_inputName[_makeAxis[axis, 1]].GetHashCode()) >= 0) {
					_individualAxisSens[_makeAxis[axis, 1]] = sensitivity;
				}
			}

			_SaveAxisSensitivity();
		}
	}

	private static void _SetAxisGravity(int hash, float gravity, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		int axis = _FindAxisByHash(hash);
		if (axis == -1) {
			// axis not defined!
			string errorText = (!string.IsNullOrEmpty(description)) ? description : "axis matching hashcode of " + hash;
			Debug.LogError("Cannot set gravity of " + errorText + ". Have you defined this axis with SetAxis() yet?");
		} else {
			// axis has been defined
			_individualAxisGrav[_makeAxis[axis, 0]] = gravity;
			// don't try to set value for positive input if this axis only has negative input
			if (_makeAxis[axis, 1] >= 0) {
				if (_FindKeyByHash(_inputName[_makeAxis[axis, 1]].GetHashCode()) >= 0) {
					_individualAxisGrav[_makeAxis[axis, 1]] = gravity;
				}
			}

			_SaveAxisGravity();
		}
	}

	private static void _SetAxisDeadzone(int hash, float deadzone, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		int axis = _FindAxisByHash(hash);
		if (axis == -1) {
			// axis not defined!
			string errorText = (!string.IsNullOrEmpty(description)) ? description : "axis matching hashcode of " + hash;
			Debug.LogError("Cannot set deadzone of " + errorText + ". Have you defined this axis with SetAxis() yet?");
		} else {
			// axis has been defined
			_individualAxisDead[_makeAxis[axis, 0]] = deadzone;
			// don't try to set value for positive input if this axis only has negative input
			if (_makeAxis[axis, 1] >= 0) {
				if (_FindKeyByHash(_inputName[_makeAxis[axis, 1]].GetHashCode()) >= 0) {
					_individualAxisDead[_makeAxis[axis, 1]] = deadzone;
				}
			}

			_SaveAxisDeadzone();
		}
	}

	#endregion //Private functions which actually do the work

	#endregion //Deadzone, Gravity, and Sensitivity functions

	#endregion //SetAxis and SetAxisSensitivity & related functions

	#region GetKey, GetAxis, GetText, and related functions

	#region GetKey/GetButton & related functions

	// returns -1 if there was an error, otherwise returns the array index of the key
	private static int _FindKeyByHash(int hash) {
		return (_inputNameHash.ContainsKey(hash)) ? _inputNameHash[hash] : -1;
	}

	private static int _FindKeyByDescription(string description) {
		return _FindKeyByHash(description.GetHashCode());
	}

	private static bool _GetKey(int hash, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		if (!_DefaultsExist()) {
			Debug.LogError("No default inputs found. Please setup your default inputs with SetKey first.");
			return false;
		}

		if (!_cKeysLoaded) { return false; } // make sure we've saved/loaded keys before trying to access them.
		int _index = _FindKeyByHash(hash);

		if (_index > -1) {
			return _getKeyArray[_index];
		} else {
			// if we got this far then the string didn't match and there's a problem
			string errorText = (!string.IsNullOrEmpty(description)) ? description : "hash " + hash;
			Debug.LogError("Couldn't find a key match for " + errorText + ". Is it possible you typed it wrong or forgot to setup your defaults after making changes?");
			return false;
		}
	}

	/// <summary>Returns true every frame the key is being pressed.</summary>
	/// <param name="description">The name of the key.</param>
	/// <returns>A boolean. True every frame the key is still pressed.</returns>
	public static bool GetKey(string description) {
		return _GetKey(description.GetHashCode(), description);
	}

	/// <summary>Returns true every frame the key is being pressed.</summary>
	/// <param name="descriptionHash">The hashcode of the name of the key.</param>
	/// <returns>A boolean. True every frame the key is still pressed.</returns>
	public static bool GetKey(int descriptionHash) {
		return _GetKey(descriptionHash);
	}

	private static bool _GetKeyDown(int hash, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		if (!_DefaultsExist()) {
			Debug.LogError("No default inputs found. Please setup your default inputs with SetKey first.");
			return false;
		}

		if (!_cKeysLoaded) { return false; } // make sure we've saved/loaded keys before trying to access them.
		int _index = _FindKeyByHash(hash);

		if (_index > -1) {
			return _getKeyDownArray[_index];
		} else {
			// if we got this far then the string didn't match and there's a problem
			string errorText = (!string.IsNullOrEmpty(description)) ? description : "hash " + hash;
			Debug.LogError("Couldn't find a key match for " + errorText + ". Is it possible you typed it wrong or forgot to setup your defaults after making changes?");
			return false;
		}
	}

	/// <summary>Returns true only on the one frame the key is pressed down.</summary>
	/// <param name="description">The name of the key.</param>
	/// <returns>A boolean. True on the frame the key is pressed down.</returns>
	public static bool GetKeyDown(string description) {
		return _GetKeyDown(description.GetHashCode(), description);
	}

	/// <summary>Returns true only on the one frame the key is pressed down.</summary>
	/// <param name="descriptionHash">A hashcode of the name of the key.</param>
	/// <returns>A boolean. True on the frame the key is pressed down.</returns>
	public static bool GetKeyDown(int descriptionHash) {
		return _GetKeyDown(descriptionHash);
	}

	private static bool _GetKeyUp(int hash, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		if (!_DefaultsExist()) {
			Debug.LogError("No default inputs found. Please setup your default inputs with SetKey first.");
			return false;
		}

		if (!_cKeysLoaded) { return false; } // make sure we've saved/loaded keys before trying to access them.
		int _index = _FindKeyByHash(hash);

		if (_index > -1) {
			return _getKeyUpArray[_index];
		} else {
			// if we got this far then the string didn't match and there's a problem
			string errorText = (!string.IsNullOrEmpty(description)) ? description : "hash " + hash;
			Debug.LogError("Couldn't find a key match for " + errorText + ". Is it possible you typed it wrong or forgot to setup your defaults after making changes?");
			return false;
		}
	}

	/// <summary>Returns true only on the one frame the key is released.</summary>
	/// <param name="description">The name of the key.</param>
	/// <returns>A boolean. True only for one frame when the key is first released.</returns>
	public static bool GetKeyUp(string description) {
		return _GetKeyUp(description.GetHashCode(), description);
	}

	/// <summary>Returns true only on the one frame the key is released.</summary>
	/// <param name="descriptionHash">The hashcode of the name of the key.</param>
	/// <returns>A boolean. True only for one frame when the key is first released.</returns>
	public static bool GetKeyUp(int descriptionHash) {
		return _GetKeyUp(descriptionHash);
	}

	#region GetButton functions -- they just call GetKey functions

	/// <summary>Returns true every frame the input is being pressed</summary>
	public static bool GetButton(string description) {
		return GetKey(description);
	}

	/// <summary>Returns true every frame the input is being pressed</summary>
	public static bool GetButton(int descriptionHash) {
		return GetKey(descriptionHash);
	}

	/// <summary>Returns true just once when the input is first pressed down</summary>
	public static bool GetButtonDown(string description) {
		return GetKeyDown(description);
	}

	/// <summary>Returns true just once when the input is first pressed down</summary>
	public static bool GetButtonDown(int descriptionHash) {
		return GetKeyDown(descriptionHash);
	}

	/// <summary>Returns true just once when the input is released</summary>
	public static bool GetButtonUp(string description) {
		return GetKeyUp(description);
	}

	/// <summary>Returns true just once when the input is released</summary>
	public static bool GetButtonUp(int descriptionHash) {
		return GetKeyUp(descriptionHash);
	}

	#endregion //GetButton functions -- they just call GetKey functions

	#endregion //GetKey functions

	#region GetAxis and related functions

	private static int _FindAxisByHash(int hash) {
		return (_axisNameHash.ContainsKey(hash)) ? _axisNameHash[hash] : -1;
	}

	private static int _FindAxisByDescription(string description) {
		return _FindAxisByHash(description.GetHashCode());
	}

	private static float _GetAxis(int hash, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		if (!_DefaultsExist()) {
			Debug.LogError("No default inputs found. Please setup your default inputs with SetKey first.");
			return 0;
		}

		int index = _FindAxisByHash(hash);
		if (index > -1) {
			if (_invertAxis[index]) {
				// this axis should be inverted, so invert the value!
				return _getAxisArray[index] * -1;
			} else {
				// this axis is normal, return the normal value
				return _getAxisArray[index];
			}
		}

		// if we got this far then the string didn't match and there's a problem
		string errorText = (!string.IsNullOrEmpty(description)) ? description : "axis with hashcode of " + hash;
		Debug.LogError("Couldn't find an axis match for " + errorText + ". Is it possible you typed it wrong?");
		return 0;
	}

	/// <summary>Gets the value (float) of a cInput axis.</summary>
	/// <param name="description">The name of the axis.</param>
	public static float GetAxis(string description) {
		return _GetAxis(description.GetHashCode(), description);
	}

	/// <summary>Gets the value (float) of a cInput axis.</summary>
	/// <param name="descriptionHash">The HashCode (int) of the cInput axis name.</param>
	public static float GetAxis(int descriptionHash) {
		return _GetAxis(descriptionHash);
	}

	private static float _GetAxisRaw(int hash, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		if (!_DefaultsExist()) {
			Debug.LogError("No default inputs found. Please setup your default inputs with SetKey first.");
			return 0;
		}

		int index = _FindAxisByHash(hash);
		if (index > -1) {
			if (_invertAxis[index]) {
				// this axis should be inverted, so invert the value!
				return _getAxisArrayRaw[index] * -1;
			} else {
				// this axis is normal, return the normal value
				return _getAxisArrayRaw[index];
			}
		}

		// if we got this far then the string didn't match and there's a problem
		string errorText = (!string.IsNullOrEmpty(description)) ? description : "axis with hashcode of " + hash;
		Debug.LogError("Couldn't find an axis match for " + errorText + ". Is it possible you typed it wrong?");
		return 0;
	}

	/// <summary>Gets the raw value (float) of a cInput axis, unmodified by sensitivity or gravity settings.</summary>
	/// <param name="description">The name of the axis.</param>
	public static float GetAxisRaw(string description) {
		return _GetAxisRaw(description.GetHashCode(), description);
	}

	/// <summary>Gets the raw value (float) of a cInput axis, unmodified by sensitivity or gravity settings.</summary>
	/// <param name="descriptionHash">The HashCode (int) of the cInput axis name.</param>
	public static float GetAxisRaw(int descriptionHash) {
		return _GetAxisRaw(descriptionHash);
	}

	#region GetAxisSensitivity/Gravity/Deadzone functions

	// The public GetAxisSensitivity functions just call this one.
	private static float _GetAxisSensitivity(int hash, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		int axis = _FindAxisByHash(hash);
		if (axis == -1) {
			// axis not defined!
			string errorText = (!string.IsNullOrEmpty(description)) ? description : "axis with hashcode of " + hash;
			Debug.LogError("Cannot get sensitivity of " + errorText + ". Have you defined this axis with SetAxis() yet?");
			return -1;
		} else {
			// axis has been defined
			return _individualAxisSens[_makeAxis[axis, 0]];
		}
	}

	/// <summary>Retrieve the sensitivity value of the axis.</summary>
	/// <param name="description">The axis you want the sensitivity value for.</param>
	public static float GetAxisSensitivity(string description) {
		return _GetAxisSensitivity(description.GetHashCode(), description);
	}

	/// <summary>Retrieve the sensitivity value of the axis.</summary>
	/// <param name="descriptionHash">The hashcode of the axis you want the sensitivity value for.</param>
	public static float GetAxisSensitivity(int descriptionHash) {
		return _GetAxisSensitivity(descriptionHash);
	}

	// The public GetAxisGravity functions just call this one.
	private static float _GetAxisGravity(int hash, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		int axis = _FindAxisByHash(hash);
		if (axis == -1) {
			// axis not defined!
			string errorText = (!string.IsNullOrEmpty(description)) ? description : "axis with hashcode of " + hash;
			Debug.LogError("Cannot get gravity of " + errorText + ". Have you defined this axis with SetAxis() yet?");
			return -1;
		} else {
			// axis has been defined
			return _individualAxisGrav[_makeAxis[axis, 0]];
		}
	}

	/// <summary>Retrieve the gravity value of the axis.</summary>
	/// <param name="description">The axis you want the gravity value for.</param>
	public static float GetAxisGravity(string description) {
		return _GetAxisGravity(description.GetHashCode(), description);
	}

	/// <summary>Retrieve the gravity value of the axis.</summary>
	/// <param name="descriptionHash">The hashcode of the axis you want the gravity value for.</param>
	public static float GetAxisGravity(int descriptionHash) {
		return _GetAxisGravity(descriptionHash);
	}

	// The public GetAxisDeadzone functions just call this one.
	private static float _GetAxisDeadzone(int hash, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		int axis = _FindAxisByHash(hash);
		if (axis == -1) {
			// axis not defined!
			string errorText = (!string.IsNullOrEmpty(description)) ? description : "axis with hashcode of " + hash;
			Debug.LogError("Cannot get deadzone of " + errorText + ". Have you defined this axis with SetAxis() yet?");
			return -1;
		} else {
			// axis has been defined
			return _individualAxisDead[_makeAxis[axis, 0]];
		}
	}

	/// <summary>Retrieve the deadzone value of the axis.</summary>
	/// <param name="description">The axis you want the deadzone value for.</param>
	public static float GetAxisDeadzone(string description) {
		return _GetAxisDeadzone(description.GetHashCode(), description);
	}

	/// <summary>Retrieve the deadzone value of the axis.</summary>
	/// <param name="descriptionHash">The hashcode of the axis you want the deadzone value for.</param>
	public static float GetAxisDeadzone(int descriptionHash) {
		return _GetAxisDeadzone(descriptionHash);
	}

	#endregion //GetAxisSensitivity/Gravity/Deadzone functions

	#endregion //GetAxis and related functions

	#region GetText, _ChangeStringToAxisName, _PosOrNeg functions

	#region Overloaded GetText(string) and GetText(int) functions for UnityScript compatibility

	/// <summary>Get the string name of the primary input for a cInput action.</summary>
	/// <param name="action">The name of the cInput action.</param>
	public static string GetText(string action) {
		return GetText(action, 1);
	}

	/// <summary>Get the string name of a cInput action.</summary>
	/// <param name="index">The array index number to get the name for.</param>
	public static string GetText(int index) {
		return GetText(index, 0);
	}

	#endregion //Overloaded GetText(string) and GetText(int) functions for UnityScript compatibility

	/// <summary>Get the name of one of the inputs of a cInput action.</summary>
	/// <param name="action">The name of the action.</param>
	/// <param name="input">1 for primary input. 2 for secondary input.</param>
	public static string GetText(string action, int input) {
		int index = _FindKeyByDescription(action);
		if (index == -1) {
			Debug.LogWarning("Could not get text for action " + action + ". It doesn't exist.");
			return "Invalid Input";
		} else {
			return GetText(index, input);
		}
	}

	#region Overloads for the return string to turn blank instead of "None"
	/// <summary>Get the name of one of the inputs of a cInput action.</summary>
	/// <param name="action">The name of the action.</param>
	/// <param name="input">1 for primary input. 2 for secondary input.</param>
	/// <param name="returnBlank">If true, returns an empty string instead of "None" when no input is assigned.</param>
	public static string GetText(string action, int input, bool returnBlank) {
		string returnString = GetText(action, input);
		if (returnBlank && returnString == "None") { returnString = ""; }
		return returnString;
	}

	/// <summary>Get the string name of a cInput action.</summary>
	/// <param name="index">The array index number to get the name for.</param>
	/// <param name="returnBlank">If true, returns an empty string instead of "None" when no input is assigned.</param>
	public static string GetText(int index, int input, bool returnBlank) {
		string returnString = GetText(index, input);
		if (returnBlank && returnString == "None") { returnString = ""; }
		return returnString;
	}
	#endregion //Overloads for the return string to turn blank instead of "None"

	/// <summary>Get the name of an input using an int. Useful in for loops for GUIs.</summary>
	/// <param name="index">The index of the input.</param>
	/// <param name="input">Label, Primary, or Secondary. (0, 1, 2)</param>
	public static string GetText(int index, int input) {
		_cInputInit(); // if cInput doesn't exist, create it
		// make sure a valid input value is passed in
		if (input < 0 || input > 2) {
			Debug.LogWarning("Can't look for text #" + input + " for " + _inputName[index] + " input. Only 0, 1, or 2 is acceptable. Clamping to this range.");
			input = Mathf.Clamp(input, 0, 2);
		}

		// make sure the index is within a valid range
		if (index < 0 || index >= _inputName.Length || index >= _axisPrimary.Length || index >= _axisSecondary.Length) {
			// index out of range exception!
			Debug.LogWarning("Index out of range when calling GetText for input at index " + index + ". Are you sure you've set up cInput properly?");
			return "Invalid Input";
		}

		// another check to make sure index is within valid range
		if (index >= length) {
			Debug.LogWarning("Index out of range when calling GetText for input at index " + index + ". Index should be less than " + length + ".");
			return "Invalid Input";
		}

		string name;

		if (input == 1) {
			if (!string.IsNullOrEmpty(_axisPrimary[index])) {
				name = _axisPrimary[index];
			} else {
				string _prefix = "";
				// if modifier is not empty and isn't the same as the key, and the key isn't empty
				if (_modifierUsedPrimary[index] != KeyCode.None && _modifierUsedPrimary[index] != _inputPrimary[index] && _inputPrimary[index] != KeyCode.None) {
					_prefix = _modifierUsedPrimary[index].ToString() + " + ";
				}
				name = _prefix + _inputPrimary[index].ToString();
			}
		} else if (input == 2) {
			if (!string.IsNullOrEmpty(_axisSecondary[index])) {
				name = _axisSecondary[index];
			} else {
				string _prefix = "";
				// if modifier is not empty and isn't the same as the key, and the key isn't empty
				if (_modifierUsedSecondary[index] != KeyCode.None && _modifierUsedSecondary[index] != _inputSecondary[index] && _inputSecondary[index] != KeyCode.None) {
					_prefix = _modifierUsedSecondary[index].ToString() + " + ";
				}
				name = _prefix + _inputSecondary[index].ToString();
			}
		} else {
			name = _inputName[index];
			return name;
		}

		// check to see if this key is currently waiting to be reassigned
		if (_scanning && (index == _cScanIndex) && (input == _cScanInput)) {
			name = ". . .";
		}

		return name;
	}

	private static string _ChangeStringToAxisName(string description) {
		// First we need to change the name of some of these things. . .
		switch (description) {
			case "Mouse Left": { return "Mouse Horizontal"; }
			case "Mouse Right": { return "Mouse Horizontal"; }
			case "Mouse Up": { return "Mouse Vertical"; }
			case "Mouse Down": { return "Mouse Vertical"; }
			case "Mouse Wheel Up": { return "Mouse Wheel"; }
			case "Mouse Wheel Down": { return "Mouse Wheel"; }
		}

		string joystring = _FindJoystringByDescription(description);
		if (!String.IsNullOrEmpty(joystring)) {
			return joystring;
		}

		return description;
	}

	private static string _FindJoystringByDescription(string description) {
		int[] index;

		if (_joyStringsPosIndices.ContainsKey(description)) {
			index = _joyStringsPosIndices[description];
			return _joyStrings[index[0], index[1]];
		} else if (_joyStringsNegIndices.ContainsKey(description)) {
			index = _joyStringsNegIndices[description];
			return _joyStrings[index[0], index[1]];
		}

		return null;
	}

	private static bool _IsAxisValid(string axis) {
		switch (axis) {
			case "Mouse Left":
			case "Mouse Right":
			case "Mouse Up":
			case "Mouse Down":
			case "Mouse Wheel Up":
			case "Mouse Wheel Down": { return true; }
		}

		return (_joyStringsPosIndices.ContainsKey(axis) || _joyStringsNegIndices.ContainsKey(axis));
	}

	// This function returns -1 for negative axes
	private static int _PosOrNeg(string description) {
		int posneg = 1;

		switch (description) {
			case "Mouse Left": { return -1; }
			//case "Mouse Right": { return 1; }
			//case "Mouse Up": { return 1; }
			case "Mouse Down": { return -1; }
			//case "Mouse Wheel Up": { return 1; }
			case "Mouse Wheel Down": { return -1; }
		}

		//if (_joyStringsPosIndices.ContainsKey(description)) {
		//	return 1;
		//} else 
		if (_joyStringsNegIndices.ContainsKey(description)) {
			return -1;
		}

		return posneg;
	}

	#endregion //GetText, _ChangeStringToAxisName, _PosOrNeg functions

	#endregion //GetKey, GetAxis, GetText, and related functions

	#region ChangeKey functions

	#region ChangeKey (wait for input) functions

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="action">The string name of the key/action you want to change.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	/// <param name="allowMouseAxis">Allow a mouse axis to be bound? Default is false.</param>
	/// <param name="allowMouseButtons">Allow a mouse button to be bound? Default is true.</param>
	/// <param name="allowGamepadAxis">Allow a gamepad axis to be bound? Default is true.</param>
	/// <param name="allowGamepadButtons">Allow a gamepad button to be bound? Default is true.</param>
	/// <param name="allowKeyboard">Allow keyboard keys to be bound? Default is true.</param>
	public static void ChangeKey(string action, int input, bool allowMouseAxis, bool allowMouseButtons, bool allowGamepadAxis, bool allowGamepadButtons, bool allowKeyboard) {
		_cInputInit(); // if cInput doesn't exist, create it
		int _index = _FindKeyByDescription(action);
		if (_index < 0) {
			Debug.LogError("Trying to change cInput action named " + action + ", but couldn't find it. Did you create it first with SetKey()?");
			return; // don't try to change key or else we'll get an error
		}

		ChangeKey(_index, input, allowMouseAxis, allowMouseButtons, allowGamepadAxis, allowGamepadButtons, allowKeyboard);
	}

	#region overloaded ChangeKey(string) functions for UnityScript compatibility

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="action">The string name of the key/action you want to change.</param>
	public static void ChangeKey(string action) {
		ChangeKey(action, 1, _allowMouseAxis, _allowMouseButtons, _allowJoystickAxis, _allowJoystickButtons, _allowKeyboard);
	}

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="action">The string name of the key/action you want to change.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	public static void ChangeKey(string action, int input) {
		ChangeKey(action, input, _allowMouseAxis, _allowMouseButtons, _allowJoystickAxis, _allowJoystickButtons, _allowKeyboard);
	}

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="action">The string name of the key/action you want to change.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	/// <param name="allowMouseAxis">Allow a mouse axis to be bound? Default is false.</param>
	public static void ChangeKey(string action, int input, bool allowMouseAxis) {
		ChangeKey(action, input, allowMouseAxis, _allowMouseButtons, _allowJoystickAxis, _allowJoystickButtons, _allowKeyboard);
	}

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="action">The string name of the key/action you want to change.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	/// <param name="allowMouseAxis">Allow a mouse axis to be bound? Default is false.</param>
	/// <param name="allowMouseButtons">Allow a mouse button to be bound? Default is true.</param>
	public static void ChangeKey(string action, int input, bool allowMouseAxis, bool allowMouseButtons) {
		ChangeKey(action, input, allowMouseAxis, allowMouseButtons, _allowJoystickAxis, _allowJoystickButtons, _allowKeyboard);
	}

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="action">The string name of the key/action you want to change.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	/// <param name="allowMouseAxis">Allow a mouse axis to be bound? Default is false.</param>
	/// <param name="allowMouseButtons">Allow a mouse button to be bound? Default is true.</param>
	/// <param name="allowGamepadAxis">Allow a gamepad axis to be bound? Default is true.</param>
	public static void ChangeKey(string action, int input, bool allowMouseAxis, bool allowMouseButtons, bool allowGamepadAxis) {
		ChangeKey(action, input, allowMouseAxis, allowMouseButtons, allowGamepadAxis, _allowJoystickButtons, _allowKeyboard);
	}

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="action">The string name of the key/action you want to change.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	/// <param name="allowMouseAxis">Allow a mouse axis to be bound? Default is false.</param>
	/// <param name="allowMouseButtons">Allow a mouse button to be bound? Default is true.</param>
	/// <param name="allowGamepadAxis">Allow a gamepad axis to be bound? Default is true.</param>
	/// <param name="allowGamepadButtons">Allow a gamepad button to be bound? Default is true.</param>
	public static void ChangeKey(string action, int input, bool allowMouseAxis, bool allowMouseButtons, bool allowGamepadAxis, bool allowGamepadButtons) {
		ChangeKey(action, input, allowMouseAxis, allowMouseButtons, allowGamepadAxis, allowGamepadButtons, _allowKeyboard);
	}

	#endregion //overloaded ChangeKey(string) functions for UnityScript compatibility

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="index">The input array index of the key you want to change. Useful in for loops for GUI.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	/// <param name="allowMouseAxis">Allow a mouse axis to be bound? Default is false.</param>
	/// <param name="allowMouseButtons">Allow a mouse button to be bound? Default is true.</param>
	/// <param name="allowGamepadAxis">Allow a gamepad axis to be bound? Default is true.</param>
	/// <param name="allowGamepadButtons">Allow a gamepad button to be bound? Default is true.</param>
	/// <param name="allowKeyboard">Allow keyboard keys to be bound? Default is true.</param>
	public static void ChangeKey(int index, int input, bool allowMouseAxis, bool allowMouseButtons, bool allowGamepadAxis, bool allowGamepadButtons, bool allowKeyboard) {
		_cInputInit(); // if cInput doesn't exist, create it
		if (input != 1 && input != 2) {
			Debug.LogWarning("ChangeKey can only change primary (1) or secondary (2) inputs. You're trying to change: " + input);
			return; // no need to do more since it won't work
		}

		_ScanForNewKey(index, input, allowMouseAxis, allowMouseButtons, allowGamepadAxis, allowGamepadButtons, allowKeyboard);
	}

	#region overloaded ChangeKey(int) functions for UnityScript compatibility

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="index">The input array index of the key you want to change. Useful in for loops for GUI.</param>
	public static void ChangeKey(int index) {
		ChangeKey(index, 1, _allowMouseAxis, _allowMouseButtons, _allowJoystickAxis, _allowJoystickButtons, _allowKeyboard);
	}

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="index">The input array index of the key you want to change. Useful in for loops for GUI.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	public static void ChangeKey(int index, int input) {
		ChangeKey(index, input, _allowMouseAxis, _allowMouseButtons, _allowJoystickAxis, _allowJoystickButtons, _allowKeyboard);
	}

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="index">The input array index of the key you want to change. Useful in for loops for GUI.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	/// <param name="allowMouseAxis">Allow a mouse axis to be bound? Default is false.</param>
	public static void ChangeKey(int index, int input, bool allowMouseAxis) {
		ChangeKey(index, input, allowMouseAxis, _allowMouseButtons, _allowJoystickAxis, _allowJoystickButtons, _allowKeyboard);
	}

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="index">The input array index of the key you want to change. Useful in for loops for GUI.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	/// <param name="allowMouseAxis">Allow a mouse axis to be bound? Default is false.</param>
	/// <param name="allowMouseButtons">Allow a mouse button to be bound? Default is true.</param>
	public static void ChangeKey(int index, int input, bool allowMouseAxis, bool allowMouseButtons) {
		ChangeKey(index, input, allowMouseAxis, allowMouseButtons, _allowJoystickAxis, _allowJoystickButtons, _allowKeyboard);
	}

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="index">The input array index of the key you want to change. Useful in for loops for GUI.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	/// <param name="allowMouseAxis">Allow a mouse axis to be bound? Default is false.</param>
	/// <param name="allowMouseButtons">Allow a mouse button to be bound? Default is true.</param>
	/// <param name="allowGamepadAxis">Allow a gamepad axis to be bound? Default is true.</param>
	public static void ChangeKey(int index, int input, bool allowMouseAxis, bool allowMouseButtons, bool allowGamepadAxis) {
		ChangeKey(index, input, allowMouseAxis, allowMouseButtons, allowGamepadAxis, _allowJoystickButtons, _allowKeyboard);
	}

	/// <summary>cInput will wait for the user to press a button, then bind that button to this key. Also see cInput.scanning.</summary>
	/// <param name="index">The input array index of the key you want to change. Useful in for loops for GUI.</param>
	/// <param name="input">Primary = 1, Secondary = 2</param>
	/// <param name="allowMouseAxis">Allow a mouse axis to be bound? Default is false.</param>
	/// <param name="allowMouseButtons">Allow a mouse button to be bound? Default is true.</param>
	/// <param name="allowGamepadAxis">Allow a gamepad axis to be bound? Default is true.</param>
	/// <param name="allowGamepadButtons">Allow a gamepad button to be bound? Default is true.</param>
	public static void ChangeKey(int index, int input, bool allowMouseAxis, bool allowMouseButtons, bool allowGamepadAxis, bool allowGamepadButtons) {
		ChangeKey(index, input, allowMouseAxis, allowMouseButtons, allowGamepadAxis, allowGamepadButtons, _allowKeyboard);
	}

	#endregion //overloaded ChangeKey(int) functions for UnityScript compatibility

	#endregion //ChangeKey (wait for input) functions

	#region ChangeKey set via script (don't wait for input)

	/// <summary>Change a cInput action via script, including modifiers.</summary>
	/// <param name="action">The name of the cInput action to change.</param>
	/// <param name="primary">The name of the primary input to use for this action.</param>
	/// <param name="secondary">The name of the secondary input to use for this action.</param>
	/// <param name="primaryModifier">The name of the modifier to use for the primary input.</param>
	/// <param name="secondaryModifier">The name of the modifier to use for the secondary input.</param>
	public static void ChangeKey(string action, string primary, string secondary, string primaryModifier, string secondaryModifier) {
		_cInputInit(); // if cInput doesn't exist, create it
		int _num = _FindKeyByDescription(action);

		// set modifiers
		if (String.IsNullOrEmpty(primaryModifier)) {
			primaryModifier = primary;
		}

		if (String.IsNullOrEmpty(secondaryModifier)) {
			secondaryModifier = secondary;
		}

		_modifierUsedPrimary[_num] = _String2KeyCode(primaryModifier);
		_modifierUsedSecondary[_num] = _String2KeyCode(secondaryModifier);

		_ChangeKey(_num, action, primary, secondary);
	}

	#region overloaded ChangeKey(string, primary, secondary) function for UnityScript compatibility)

	/// <summary>Change a cInput action via script.</summary>
	/// <param name="action">The name of the cInput action to change.</param>
	/// <param name="primary">The name of the primary input to use for this action.</param>
	public static void ChangeKey(string action, string primary) {
		int _num = _FindKeyByDescription(action);
		ChangeKey(action, primary, "", primary, _modifierUsedSecondary[_num].ToString());
	}

	/// <summary>Change a cInput action via script.</summary>
	/// <param name="action">The name of the cInput action to change.</param>
	/// <param name="primary">The name of the primary input to use for this action.</param>
	/// <param name="secondary">The name of the secondary input to use for this action.</param>
	public static void ChangeKey(string action, string primary, string secondary) {
		ChangeKey(action, primary, secondary, primary, secondary);
	}

	/// <summary>Change a cInput action via script, including a primary modifier.</summary>
	/// <param name="action">The name of the cInput action to change.</param>
	/// <param name="primary">The name of the primary input to use for this action.</param>
	/// <param name="secondary">The name of the secondary input to use for this action.</param>
	/// <param name="primaryModifier">The name of the modifier to use for the primary input.</param>
	public static void ChangeKey(string action, string primary, string secondary, string primaryModifier) {
		ChangeKey(action, primary, secondary, primaryModifier, secondary);
	}

	#endregion

	#endregion //ChangeKey set via script (don't wait for input)

	/// <summary>This starts the process of scanning for a new key (using the GUI to assign an input).</summary>
	/// <param name="index">The index of the input array</param>
	/// <param name="input">Primary or secondary input</param>
	/// <param name="mouseAx">Allow mouse axis (mouse ball) to be bound?</param>
	/// <param name="mouseBut">Allow mouse buttons to be bound?</param>
	/// <param name="joyAx">Allow joystick axes to be bound?</param>
	/// <param name="joyBut">Allow joystick buttons to be bound?</param>
	/// <param name="keyb">Allow keys from the keyboard to be bound?</param>
	private static void _ScanForNewKey(int index, int input, bool mouseAx, bool mouseBut, bool joyAx, bool joyBut, bool keyb) {
		_allowMouseAxis = mouseAx;
		_allowMouseButtons = mouseBut;
		_allowJoystickButtons = joyBut;
		_allowJoystickAxis = joyAx;
		_allowKeyboard = keyb;

		_cScanInput = input;
		_cScanIndex = index;
		_scanning = true;

		_axisRawValues = _GetAxisRawValues(); // get current axis values to make sure they change while scanning
	}

	/// <summary>Iterates through all of the axes in the Input Manager and saves the values in a dictionary.</summary>
	private static Dictionary<string, float> _GetAxisRawValues() {
		Dictionary<string, float> arv = new Dictionary<string, float>(); // arv means Axis Raw Values

		// these are all manually taken from the Axes that cInput installs in the Input Manager
		arv.Add("Horizontal", Input.GetAxisRaw("Horizontal"));
		arv.Add("Vertical", Input.GetAxisRaw("Vertical"));
		arv.Add("Fire1", Input.GetAxisRaw("Fire1"));
		arv.Add("Fire2", Input.GetAxisRaw("Fire2"));
		arv.Add("Fire3", Input.GetAxisRaw("Fire3"));
		arv.Add("Jump", Input.GetAxisRaw("Jump"));
		arv.Add("Mouse X", Input.GetAxisRaw("Mouse X"));
		arv.Add("Mouse Y", Input.GetAxisRaw("Mouse Y"));
		arv.Add("Mouse Horizontal", Input.GetAxisRaw("Mouse Horizontal"));
		arv.Add("Mouse Vertical", Input.GetAxisRaw("Mouse Vertical"));
		arv.Add("Mouse ScrollWheel", Input.GetAxisRaw("Mouse ScrollWheel"));
		arv.Add("Mouse Wheel", Input.GetAxisRaw("Mouse Wheel"));
		arv.Add("Window Shake X", Input.GetAxisRaw("Window Shake X"));
		arv.Add("Window Shake Y", Input.GetAxisRaw("Window Shake Y"));
		arv.Add("Shift", Input.GetAxisRaw("Shift"));

		string gpString = "";
		for (int gamePad = 1; gamePad <= _numGamepads; gamePad++) {
			for (int axis = 1; axis <= 10; axis++) {
				gpString = "Joy" + gamePad + " Axis " + axis;
				arv.Add(gpString, Input.GetAxis(gpString));
			}
		}

		return arv;
	}

	private static void _ChangeKey(int num, string action, string primary, string secondary) {
		_SetKey(num, action, primary, secondary);
		_SaveInputs();
	}

	#endregion //ChangeKey functions

	#region Save, Load, Reset & Clear functions

	#region Saving

	private static void _SaveAxis() {
		int _num = _axisLength + 1;
		string _axName = "";
		string _axNeg = "";
		string _axPos = "";
		for (int n = 0; n < _num; n++) {
			_axName += _axisName[n] + "*";
			_axNeg += _makeAxis[n, 0] + "*";
			_axPos += _makeAxis[n, 1] + "*";
		}

		string _axis = _axName + "#" + _axNeg + "#" + _axPos + "#" + _num;
		if (_usePlayerPrefs) {
			PlayerPrefs.SetString("cInput_axis", _axis);
		}

		_exAxis = _axis;
	}

	private static void _SaveAxisSensitivity() {
		int _num = _inputLength + 1;

		string _indAxSens = "";
		for (int n = 0; n < _num; n++) {
			_indAxSens += _individualAxisSens[n] + "*";
		}

		if (_usePlayerPrefs) {
			PlayerPrefs.SetString("cInput_indAxSens", _indAxSens);
		}

		_exAxisSensitivity = _indAxSens;
	}

	private static void _SaveAxisGravity() {
		int _num = _inputLength + 1;

		string _indAxGrav = "";
		for (int n = 0; n < _num; n++) {
			_indAxGrav += _individualAxisGrav[n] + "*";
		}

		if (_usePlayerPrefs) {
			PlayerPrefs.SetString("cInput_indAxGrav", _indAxGrav);
		}

		_exAxisGravity = _indAxGrav;
	}

	private static void _SaveAxisDeadzone() {
		int _num = _inputLength + 1;

		string _indAxDead = "";
		for (int n = 0; n < _num; n++) {
			_indAxDead += _individualAxisDead[n] + "*";
		}

		if (_usePlayerPrefs) {
			PlayerPrefs.SetString("cInput_indAxDead", _indAxDead);
		}

		_exAxisDeadzone = _indAxDead;
	}

	private static void _SaveAxInverted() {
		int _num = _axisLength + 1;
		string _axInv = "";

		for (int n = 0; n < _num; n++) {
			_axInv += _invertAxis[n] + "*";
		}

		if (_usePlayerPrefs) {
			PlayerPrefs.SetString("cInput_axInv", _axInv);
		}

		_exAxisInverted = _axInv;
	}

	private static void _SaveDefaults() {
		// saving default inputs
		int _num = _inputLength + 1;
		string _defName = "";
		string _def1 = "";
		string _def2 = "";
		string _defmod1 = "";
		string _defmod2 = "";
		for (int n = 0; n < _num; n++) {

			_defName += _defaultStrings[n, 0] + "*";
			_def1 += _defaultStrings[n, 1] + "*";
			_def2 += _defaultStrings[n, 2] + "*";
			_defmod1 += _defaultStrings[n, 3] + "*";
			_defmod2 += _defaultStrings[n, 4] + "*";
		}

		string _Default = _defName + "#" + _def1 + "#" + _def2 + "#" + _defmod1 + "#" + _defmod2;
		if (_usePlayerPrefs) {
			PlayerPrefs.SetInt("cInput_count", _num);
			PlayerPrefs.SetString("cInput_defaults", _Default);
		}
		_exDefaults = _num + "¿" + _Default;
	}

	private static void _SaveInputs() {
		int _num = _inputLength + 1;
		// *** save input configuration ***
		string _descr = "";
		string _inp = "";
		string _alt_inp = "";
		string _inpStr = "";
		string _alt_inpStr = "";
		string _modifierStr = "";
		string _alt_modifierStr = "";

		for (int n = 0; n < _num; n++) {
			// make the strings
			_descr += _inputName[n] + "*";
			_inp += _inputPrimary[n] + "*";
			_alt_inp += _inputSecondary[n] + "*";
			_inpStr += _axisPrimary[n] + "*";
			_alt_inpStr += _axisSecondary[n] + "*";
			_modifierStr += _modifierUsedPrimary[n] + "*";
			_alt_modifierStr += _modifierUsedSecondary[n] + "*";
		}

		// save the strings to the PlayerPrefs
		if (_usePlayerPrefs) {
			PlayerPrefs.SetString("cInput_descr", _descr);
			PlayerPrefs.SetString("cInput_inp", _inp);
			PlayerPrefs.SetString("cInput_alt_inp", _alt_inp);
			PlayerPrefs.SetString("cInput_inpStr", _inpStr);
			PlayerPrefs.SetString("cInput_alt_inpStr", _alt_inpStr);
			PlayerPrefs.SetString("cInput_modifierStr", _modifierStr);
			PlayerPrefs.SetString("cInput_alt_modifierStr", _alt_modifierStr);
		}

		_exInputs = _descr + "¿" + _inp + "¿" + _alt_inp + "¿" + _inpStr + "¿" + _alt_inpStr + "¿" + _modifierStr + "¿" + _alt_modifierStr;
	}

	/// <summary>A string containing all current cInput settings.</summary>
	public static string externalInputs {
		get {
			return _exAllowDuplicates + "æ" + _exAxis + "æ" + _exAxisInverted + "æ" + _exDefaults + "æ" + _exInputs +
					"æ" + _exCalibrations + "æ" + _exCalibrationValues + "æ" + _exAxisSensitivity + "æ" + _exAxisGravity + "æ" + _exAxisDeadzone;
		}
	}

	#endregion //Saving

	#region Loading

	/// <summary>Load all cInput settings from a string.</summary>
	/// <param name="externString">The string containing the settings to load.</param>
	public static void LoadExternal(string externString) {
		_cInputInit(); // if cInput doesn't exist, create it
		string[] tmpExternalStrings = externString.Split('æ');
		_exAllowDuplicates = tmpExternalStrings[0];
		_exAxis = tmpExternalStrings[1];
		_exAxisInverted = tmpExternalStrings[2];
		_exDefaults = tmpExternalStrings[3];
		_exInputs = tmpExternalStrings[4];
		_exCalibrations = tmpExternalStrings[5];
		_exCalibrationValues = tmpExternalStrings[6];
		// saving method was changed, so check that new save method was used before trying to load it
		if (tmpExternalStrings.Length > 7) {
			_exAxisSensitivity = tmpExternalStrings[7];
			_exAxisGravity = tmpExternalStrings[8];
			_exAxisDeadzone = tmpExternalStrings[9];
		}

		_LoadExternalInputs();
	}

	private static void _LoadInputs() {
		// don't attempt to load from PlayerPrefs is PlayerPrefs is disabled
		// and make sure we have data saved to PlayerPrefs
		if (!_usePlayerPrefs || !PlayerPrefs.HasKey("cInput_count")) {
			// there is nothing to load
			_cKeysLoaded = true;
			return;
		}

		if (PlayerPrefs.HasKey("cInput_dubl")) {
			if (PlayerPrefs.GetString("cInput_dubl") == "True") {
				allowDuplicates = true;
			} else {
				allowDuplicates = false;
			}
		}

		int _count = PlayerPrefs.GetInt("cInput_count");
		_inputLength = _count - 1;

		string _defaults = PlayerPrefs.GetString("cInput_defaults");
		string[] ar_defs = _defaults.Split('#');
		string[] ar_defName = ar_defs[0].Split('*');
		string[] ar_defPrime = ar_defs[1].Split('*');
		string[] ar_defSec = ar_defs[2].Split('*');
		string[] ar_modPrime = ar_defs[3].Split('*');
		string[] ar_modSec = ar_defs[4].Split('*');

		for (int n = 0; n < ar_defName.Length - 1; n++) {
			_SetDefaultKey(n, ar_defName[n], ar_defPrime[n], ar_defSec[n], ar_modPrime[n], ar_modSec[n]);
		}

		if (PlayerPrefs.HasKey("cInput_inp")) {
			string _descr = PlayerPrefs.GetString("cInput_descr");
			string _inp = PlayerPrefs.GetString("cInput_inp");
			string _alt_inp = PlayerPrefs.GetString("cInput_alt_inp");
			string _inpStr = PlayerPrefs.GetString("cInput_inpStr");
			string _alt_inpStr = PlayerPrefs.GetString("cInput_alt_inpStr");
			string _modifierStr = PlayerPrefs.GetString("cInput_modifierStr");
			string _alt_modifierStr = PlayerPrefs.GetString("cInput_alt_modifierStr");

			string[] ar_descr = _descr.Split('*');
			string[] ar_inp = _inp.Split('*');
			string[] ar_alt_inp = _alt_inp.Split('*');
			string[] ar_inpStr = _inpStr.Split('*');
			string[] ar_alt_inpStr = _alt_inpStr.Split('*');
			string[] ar_modifierStr = _modifierStr.Split('*');
			string[] ar_alt_modifierStr = _alt_modifierStr.Split('*');

			for (int n = 0; n < ar_descr.Length - 1; n++) {
				if (ar_descr[n] == _defaultStrings[n, 0]) {
					_inputName[n] = ar_descr[n];
					_inputPrimary[n] = _String2KeyCode(ar_inp[n]);
					_inputSecondary[n] = _String2KeyCode(ar_alt_inp[n]);
					_axisPrimary[n] = ar_inpStr[n];
					_axisSecondary[n] = ar_alt_inpStr[n];
					_modifierUsedPrimary[n] = _String2KeyCode(ar_modifierStr[n]);
					_modifierUsedSecondary[n] = _String2KeyCode(ar_alt_modifierStr[n]);
				}
			}

			// fixes inputs when defaults are being changed
			for (int m = 0; m < ar_defName.Length - 1; m++) {
				for (int n = 0; n < ar_descr.Length - 1; n++) {
					if (ar_descr[n] == _defaultStrings[m, 0]) {
						_inputName[m] = ar_descr[n];
						_inputPrimary[m] = _String2KeyCode(ar_inp[n]);
						_inputSecondary[m] = _String2KeyCode(ar_alt_inp[n]);
						_axisPrimary[m] = ar_inpStr[n];
						_axisSecondary[m] = ar_alt_inpStr[n];
						_modifierUsedPrimary[n] = _String2KeyCode(ar_modifierStr[n]);
						_modifierUsedSecondary[n] = _String2KeyCode(ar_alt_modifierStr[n]);
					}
				}
			}
		}

		if (PlayerPrefs.HasKey("cInput_axis")) {
			string _ax = PlayerPrefs.GetString("cInput_axis");

			string[] _axis = _ax.Split('#');
			string[] _axName = _axis[0].Split('*');
			string[] _axNeg = _axis[1].Split('*');
			string[] _axPos = _axis[2].Split('*');

			int _axCount = int.Parse(_axis[3]);
			for (int n = 0; n < _axCount; n++) {
				int _neg = int.Parse(_axNeg[n]);
				int _pos = int.Parse(_axPos[n]);
				_SetAxis(n, _axName[n], _neg, _pos);
			}
		}

		if (PlayerPrefs.HasKey("cInput_axInv")) {
			string _invAx = PlayerPrefs.GetString("cInput_axInv");
			string[] _axInv = _invAx.Split('*');

			for (int n = 0; n < _axInv.Length; n++) {
				if (_axInv[n] == "True") {
					_invertAxis[n] = true;
				} else {
					_invertAxis[n] = false;
				}
			}
		}

		if (PlayerPrefs.HasKey("cInput_indAxSens")) {
			string _tmpAxisSens = PlayerPrefs.GetString("cInput_indAxSens");
			string[] _arrAxisSens = _tmpAxisSens.Split('*');
			for (int n = 0; n < _arrAxisSens.Length - 1; n++) {
				_individualAxisSens[n] = float.Parse(_arrAxisSens[n]);
			}
		}

		if (PlayerPrefs.HasKey("cInput_indAxGrav")) {
			string _tmpAxisGrav = PlayerPrefs.GetString("cInput_indAxGrav");
			string[] _arrAxisGrav = _tmpAxisGrav.Split('*');
			for (int n = 0; n < _arrAxisGrav.Length - 1; n++) {
				_individualAxisGrav[n] = float.Parse(_arrAxisGrav[n]);
			}
		}

		if (PlayerPrefs.HasKey("cInput_indAxDead")) {
			string _tmpAxisDead = PlayerPrefs.GetString("cInput_indAxDead");
			string[] _arrAxisDead = _tmpAxisDead.Split('*');
			for (int n = 0; n < _arrAxisDead.Length - 1; n++) {
				_individualAxisDead[n] = float.Parse(_arrAxisDead[n]);
			}
		}

		// calibration loading
		if (PlayerPrefs.HasKey("cInput_saveCals")) {
			string _saveCals = PlayerPrefs.GetString("cInput_saveCals");
			string[] _saveCalsArr = _saveCals.Split('*');
			for (int n = 0; n < _saveCalsArr.Length - 1; n++) {
				_axisType[n] = int.Parse(_saveCalsArr[n]);
			}
		}

		if (PlayerPrefs.HasKey("cInput_calsVals")) {
			string _calsVals = PlayerPrefs.GetString("cInput_calsVals");
			_CalibrationValuesFromString(_calsVals);
		}

		_cKeysLoaded = true;
	}

	private static void _LoadExternalInputs() {
		_externalSaving = true;
		// splitting the external strings
		string[] externalStringDefaults = _exDefaults.Split('¿');
		string[] externalStringInputs = _exInputs.Split('¿');

		allowDuplicates = (_exAllowDuplicates == "True") ? true : false;

		int _count = int.Parse(externalStringDefaults[0]);
		_inputLength = _count - 1;

		string _defaults = externalStringDefaults[1];
		string[] ar_defs = _defaults.Split('#');
		string[] ar_defName = ar_defs[0].Split('*');
		string[] ar_defPrime = ar_defs[1].Split('*');
		string[] ar_defSec = ar_defs[2].Split('*');
		string[] ar_modPrime = ar_defs[3].Split('*');
		string[] ar_modSec = ar_defs[4].Split('*');

		for (int n = 0; n < ar_defName.Length - 1; n++) {
			_SetDefaultKey(n, ar_defName[n], ar_defPrime[n], ar_defSec[n], ar_modPrime[n], ar_modSec[n]);
		}

		if (!string.IsNullOrEmpty(externalStringInputs[0])) {
			string _descr = externalStringInputs[0];
			string _inp = externalStringInputs[1];
			string _alt_inp = externalStringInputs[2];
			string _inpStr = externalStringInputs[3];
			string _alt_inpStr = externalStringInputs[4];
			string _modifierStr = externalStringInputs[5];
			string _alt_modifierStr = externalStringInputs[6];

			string[] ar_descr = _descr.Split('*');
			string[] ar_inp = _inp.Split('*');
			string[] ar_alt_inp = _alt_inp.Split('*');
			string[] ar_inpStr = _inpStr.Split('*');
			string[] ar_alt_inpStr = _alt_inpStr.Split('*');
			string[] ar_modifierStr = _modifierStr.Split('*');
			string[] ar_alt_modifierStr = _alt_modifierStr.Split('*');

			for (int n = 0; n < ar_descr.Length - 1; n++) {
				if (ar_descr[n] == _defaultStrings[n, 0]) {
					_inputName[n] = ar_descr[n];
					_inputPrimary[n] = _String2KeyCode(ar_inp[n]);
					_inputSecondary[n] = _String2KeyCode(ar_alt_inp[n]);
					_axisPrimary[n] = ar_inpStr[n];
					_axisSecondary[n] = ar_alt_inpStr[n];
					_modifierUsedPrimary[n] = _String2KeyCode(ar_modifierStr[n]);
					_modifierUsedSecondary[n] = _String2KeyCode(ar_alt_modifierStr[n]);
				}
			}

			// fixes inputs when defaults are being changed
			for (int m = 0; m < ar_defName.Length - 1; m++) {
				for (int n = 0; n < ar_descr.Length - 1; n++) {
					if (ar_descr[n] == _defaultStrings[m, 0]) {
						_inputName[m] = ar_descr[n];
						_inputPrimary[m] = _String2KeyCode(ar_inp[n]);
						_inputSecondary[m] = _String2KeyCode(ar_alt_inp[n]);
						_axisPrimary[m] = ar_inpStr[n];
						_axisSecondary[m] = ar_alt_inpStr[n];
						_modifierUsedPrimary[n] = _String2KeyCode(ar_modifierStr[n]);
						_modifierUsedSecondary[n] = _String2KeyCode(ar_alt_modifierStr[n]);
					}
				}
			}
		}

		string[] externalStringAxes = _exAxis.Split('¿');

		if (externalStringAxes.Length > 1) {
			#region Load Strings saved from cInput 2.8.6 or below
			//Debug.Log("Loading old school style.");
			if (!string.IsNullOrEmpty(externalStringAxes[0])) {
				string _invAx = _exAxisInverted;
				string[] _axInv = (!string.IsNullOrEmpty(_invAx)) ? _invAx.Split('*') : null;
				string _ax = externalStringAxes[0];

				string[] _axis = _ax.Split('#');
				string[] _axName = _axis[0].Split('*');
				string[] _axNeg = _axis[1].Split('*');
				string[] _axPos = _axis[2].Split('*');

				int _axCount = int.Parse(_axis[3]);
				for (int n = 0; n < _axCount; n++) {
					int _neg = int.Parse(_axNeg[n]);
					int _pos = int.Parse(_axPos[n]);
					_SetAxis(n, _axName[n], _neg, _pos);

					if (!string.IsNullOrEmpty(_invAx)) {
						if (_axInv[n] == "True") {
							_invertAxis[n] = true;
						} else {
							_invertAxis[n] = false;
						}
					}
				}
			}

			if (!string.IsNullOrEmpty(externalStringAxes[1])) {
				string _tmpAxisSens = externalStringAxes[1];
				string[] _arrAxisSens = _tmpAxisSens.Split('*');
				for (int n = 0; n < _arrAxisSens.Length - 1; n++) {
					_individualAxisSens[n] = float.Parse(_arrAxisSens[n]);
				}
			}


			if (externalStringAxes.Length > 2) {
				if (!string.IsNullOrEmpty(externalStringAxes[2])) {
					string _tmpAxisGrav = externalStringAxes[2];
					string[] _arrAxisGrav = _tmpAxisGrav.Split('*');
					for (int n = 0; n < _arrAxisGrav.Length - 1; n++) {
						_individualAxisGrav[n] = float.Parse(_arrAxisGrav[n]);
					}
				}
			}

			if (externalStringAxes.Length > 3) {
				if (!string.IsNullOrEmpty(externalStringAxes[3])) {
					string _tmpAxisDead = externalStringAxes[3];
					string[] _arrAxisDead = _tmpAxisDead.Split('*');
					for (int n = 0; n < _arrAxisDead.Length - 1; n++) {
						_individualAxisDead[n] = float.Parse(_arrAxisDead[n]);
					}
				}
			}
			#endregion //Load Strings saved from cInput 2.8.6 or below
		} else {
			#region Load external strings saved from 2.8.7 or above
			//Debug.Log("Loading new school style");
			if (!string.IsNullOrEmpty(_exAxis)) {
				string _invAx = _exAxisInverted;
				string[] _axInv = (!string.IsNullOrEmpty(_invAx)) ? _invAx.Split('*') : null;

				string[] _axis = _exAxis.Split('#');
				string[] _axName = _axis[0].Split('*');
				string[] _axNeg = _axis[1].Split('*');
				string[] _axPos = _axis[2].Split('*');

				int _axCount = int.Parse(_axis[3]);
				for (int n = 0; n < _axCount; n++) {
					int _neg = int.Parse(_axNeg[n]);
					int _pos = int.Parse(_axPos[n]);
					_SetAxis(n, _axName[n], _neg, _pos);

					if (!string.IsNullOrEmpty(_invAx)) {
						if (_axInv[n] == "True") {
							_invertAxis[n] = true;
						} else {
							_invertAxis[n] = false;
						}
					}
				}
			}

			if (!string.IsNullOrEmpty(_exAxisSensitivity)) {
				string[] _arrAxisSens = _exAxisSensitivity.Split('*');
				for (int n = 0; n < _arrAxisSens.Length - 1; n++) {
					_individualAxisSens[n] = float.Parse(_arrAxisSens[n]);
				}
			}

			if (!string.IsNullOrEmpty(_exAxisGravity)) {
				string _tmpAxisGrav = _exAxisGravity;
				string[] _arrAxisGrav = _tmpAxisGrav.Split('*');
				for (int n = 0; n < _arrAxisGrav.Length - 1; n++) {
					_individualAxisGrav[n] = float.Parse(_arrAxisGrav[n]);
				}
			}

			if (!string.IsNullOrEmpty(_exAxisDeadzone)) {
				string _tmpAxisDead = _exAxisDeadzone;
				string[] _arrAxisDead = _tmpAxisDead.Split('*');
				for (int n = 0; n < _arrAxisDead.Length - 1; n++) {
					_individualAxisDead[n] = float.Parse(_arrAxisDead[n]);
				}
			}
			#endregion //Load external strings saved from 2.8.7 or above
		}

		// calibration loading
		if (!string.IsNullOrEmpty(_exCalibrations)) {
			string _saveCals = _exCalibrations;
			string[] _saveCalsArr = _saveCals.Split('*');
			for (int n = 1; n <= _saveCalsArr.Length - 2; n++) {
				_axisType[n] = int.Parse(_saveCalsArr[n]);
			}
		}

		if (!string.IsNullOrEmpty(_exCalibrationValues)) {
			_CalibrationValuesFromString(_exCalibrationValues);
		}

		_cKeysLoaded = true;
	}

	#endregion //Loading

	/// <summary>Clears all user-modified cInput settings and resets them to their defaults.</summary>
	public static void ResetInputs() {
		_cInputInit(); // if cInput doesn't exist, create it
		// reset inputs to default values
		for (int n = 0; n < _inputLength + 1; n++) {
			_SetKey(n, _defaultStrings[n, 0], _defaultStrings[n, 1], _defaultStrings[n, 2]);
			_modifierUsedPrimary[n] = _String2KeyCode(_defaultStrings[n, 3]);
			_modifierUsedSecondary[n] = _String2KeyCode(_defaultStrings[n, 4]);
		}

		for (int n = 0; n < _axisLength; n++) {
			_invertAxis[n] = false;
		}

		Clear();
		_SaveDefaults();
		_SaveInputs();
		_SaveAxInverted();

		if (OnKeyChanged != null) { OnKeyChanged(); }
	}

	/// <summary>Clears all cInput settings from PlayerPrefs.</summary>
	public static void Clear() {
		_cInputInit(); // if cInput doesn't exist, create it
		Debug.LogWarning("Clearing out all cInput related values from PlayerPrefs");
		PlayerPrefs.DeleteKey("cInput_axInv");
		PlayerPrefs.DeleteKey("cInput_axis");
		PlayerPrefs.DeleteKey("cInput_indAxSens");
		PlayerPrefs.DeleteKey("cInput_indAxGrav");
		PlayerPrefs.DeleteKey("cInput_indAxDead");
		PlayerPrefs.DeleteKey("cInput_count");
		PlayerPrefs.DeleteKey("cInput_defaults");
		PlayerPrefs.DeleteKey("cInput_descr");
		PlayerPrefs.DeleteKey("cInput_inp");
		PlayerPrefs.DeleteKey("cInput_alt_inp");
		PlayerPrefs.DeleteKey("cInput_inpStr");
		PlayerPrefs.DeleteKey("cInput_alt_inpStr");
		PlayerPrefs.DeleteKey("cInput_dubl");
		PlayerPrefs.DeleteKey("cInput_saveCals");
		PlayerPrefs.DeleteKey("cInput_calsVals");
		PlayerPrefs.DeleteKey("cInput_modifierStr");
		PlayerPrefs.DeleteKey("cInput_alt_modifierStr");
	}

	#endregion //Save, Load, Reset & Clear functions

	#region Miscellaneous functions

	#region AxisInverted functions

	// this sets the inversion of axisName to invertedStatus
	private static bool _AxisInverted(int hash, bool invertedStatus, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		int index = _FindAxisByHash(hash);
		if (index > -1) {
			_invertAxis[index] = invertedStatus;
			_SaveAxInverted();
			return invertedStatus;
		}

		// if we got this far then the string didn't match and there's a problem.
		string errorText = (!string.IsNullOrEmpty(description)) ? description : "axis with hashcode of " + hash;
		Debug.LogWarning("Couldn't find an axis match for " + errorText + " while trying to set inversion status. Is it possible you typed it wrong?");
		return false;
	}

	public static bool AxisInverted(string description, bool invertedStatus) {
		return _AxisInverted(description.GetHashCode(), invertedStatus, description);
	}

	public static bool AxisInverted(int descriptionHash, bool invertedStatus) {
		return _AxisInverted(descriptionHash, invertedStatus);
	}

	// this just returns inversion status of axisName
	private static bool _AxisInverted(int hash, string description = "") {
		_cInputInit(); // if cInput doesn't exist, create it
		int index = _FindAxisByHash(hash);
		if (index > -1) {
			return _invertAxis[index];
		}

		// if we got this far then the string didn't match and there's a problem.
		string errorText = (!string.IsNullOrEmpty(description)) ? description : "axis with hashcode of " + hash;
		Debug.LogWarning("Couldn't find an axis match for " + errorText + " while trying to get inversion status. Is it possible you typed it wrong?");
		return false;
	}

	public static bool AxisInverted(string description) {
		return _AxisInverted(description.GetHashCode(), description);
	}

	public static bool AxisInverted(int descriptionHash) {
		return _AxisInverted(descriptionHash);
	}

	#endregion //AxisInverted functions

	#region Calibration functions

	public static void Calibrate() {
		_cInputInit(); // if cInput doesn't exist, create it
		string _saveCals = "";
		_axisCalibrationOffset = _GetAxisRawValues();
		if (_usePlayerPrefs) {
			PlayerPrefs.SetString("cInput_calsVals", _CalibrationValuesToString());
		}

		for (int gamepad = 1; gamepad <= _numGamepads; gamepad++) {
			for (int axis = 1; axis <= 10; axis++) {
				int index = 10 * (gamepad - 1) + (axis - 1);
				string _joystring = _joyStrings[gamepad, axis];
				float axisRaw = Input.GetAxisRaw(_joystring);
				_axisType[index] = (axisRaw < -deadzone) ? 1 : // axis is negative by default
					(axisRaw > deadzone) ? -1 : // axis is positive by default
					0; // axis is 0 by default
				_saveCals += _axisType[index] + "*";
				if (_usePlayerPrefs) {
					PlayerPrefs.SetString("cInput_saveCals", _saveCals);
				}

				_exCalibrations = _saveCals;
			}
		}
	}

	private static string _CalibrationValuesToString() {
		string calVals = "";
		foreach (KeyValuePair<string, float> kvp in _axisCalibrationOffset) {
			calVals += kvp.Key + "*" + kvp.Value.ToString() + "#";
		}

		return calVals;
	}

	private static void _CalibrationValuesFromString(string calVals) {
		_axisCalibrationOffset.Clear(); // start with a clean slate
		string[] kvps = calVals.Split('#');
		for (int i = 0; i < kvps.Length - 1; i++) {
			string[] kvp = kvps[i].Split('*');
			_axisCalibrationOffset.Add(kvp[0], float.Parse(kvp[1]));
		}
	}

	private static float _GetCalibratedAxisInput(string description) {
		float rawValue = Input.GetAxisRaw(_ChangeStringToAxisName(description));

		switch (description) {
			case "Mouse Left":
			case "Mouse Right":
			case "Mouse Up":
			case "Mouse Down":
			case "Mouse Wheel Up":
			case "Mouse Wheel Down": { return rawValue; }
		}

		if (_joyStringsPosIndices.ContainsKey(description)) {
			_tmpCalibratedAxisIndices = _joyStringsPosIndices[description];
		} else if (_joyStringsNegIndices.ContainsKey(description)) {
			_tmpCalibratedAxisIndices = _joyStringsNegIndices[description];
		} else {
			Debug.LogWarning("No match found for " + description + " (" + _ChangeStringToAxisName(description) +
						"). This should never happen, in theory. Returning value of " + rawValue);
			return rawValue;
		}

		int index = 10 * _tmpCalibratedAxisIndices[0] + _tmpCalibratedAxisIndices[1];
		switch (_axisType[index]) {
			default:
			case 0: {
					// axis returns 0 by default
					return rawValue;
				}
			case 1: {
					// axis returns -1 by default
					return (rawValue + 1) / 2;
				}
			case -1: {
					//axis returns 1 by default
					return (rawValue - 1) / 2;
				}
		}
	}

	#endregion //Calibration functions

	#region _String2KeyCode function

	private static KeyCode _String2KeyCode(string str) {
		if (String.IsNullOrEmpty(str)) { return KeyCode.None; }
		if (_string2Key.Count == 0) { _CreateDictionary(); }

		if (_string2Key.ContainsKey(str)) {
			KeyCode _key = _string2Key[str];
			return _key;
		} else {
			if (!_IsAxisValid(str)) {
				Debug.Log("cInput error: " + str + " is not a valid input.");
			}

			return KeyCode.None;
		}
	}

	#endregion //_String2KeyCode function

	#region _DefaultsExist, IsKeyDefined, and IsAxisDefined functions

	private static bool _DefaultsExist() {
		return (_defaultStrings.Length > 0) ? true : false;
	}

	private static bool _IsKeyDefined(int hash) {
		_cInputInit(); // if cInput doesn't exist, create it
		return _inputNameHash.ContainsKey(hash);
	}

	/// <summary>Checks to see if a cInput action exists.</summary>
	/// <param name="keyName">The name of the action to check.</param>
	public static bool IsKeyDefined(string keyName) {
		return _IsKeyDefined(keyName.GetHashCode());
	}

	/// <summary>Checks to see if a cInput action exists.</summary>
	/// <param name="keyHash">The HashCode of the name of the action to check.</param>
	public static bool IsKeyDefined(int keyHash) {
		return _IsKeyDefined(keyHash);
	}

	private static bool _IsAxisDefined(int hash) {
		_cInputInit(); // if cInput doesn't exist, create it
		return _axisNameHash.ContainsKey(hash);
	}

	/// <summary>Checks to see if a cInput axis exists.</summary>
	/// <param name="axisName">The name of the axis to check for.</param>
	public static bool IsAxisDefined(string axisName) {
		return _IsAxisDefined(axisName.GetHashCode());
	}

	/// <summary>Checks to see if a cInput axis exists.</summary>
	/// <param name="axisHash">The HashCode of the name of the axis to check for.</param>
	public static bool IsAxisDefined(int axisHash) {
		return _IsAxisDefined(axisHash);
	}

	#endregion //_DefaultsExist, IsKeyDefined, and IsAxisDefined functions

	#region Duplicate Prevention functions

	private void _CheckDuplicates(int _num, int _count) {
		if (allowDuplicates) { return; }

		for (int n = 0; n < length; n++) {
			if (_count == 1) {
				if (_num != n && _inputPrimary[_num] == _inputPrimary[n] && _modifierUsedPrimary[_num] == _modifierUsedPrimary[n]) {
					_inputPrimary[n] = KeyCode.None;
				}

				if (_inputPrimary[_num] == _inputSecondary[n] && _modifierUsedPrimary[_num] == _modifierUsedSecondary[n]) {
					_inputSecondary[n] = KeyCode.None;
				}
			}

			if (_count == 2) {
				if (_inputSecondary[_num] == _inputPrimary[n] && _modifierUsedSecondary[_num] == _modifierUsedPrimary[n]) {
					_inputPrimary[n] = KeyCode.None;
				}

				if (_num != n && _inputSecondary[_num] == _inputSecondary[n] && _modifierUsedSecondary[_num] == _modifierUsedSecondary[n]) {
					_inputSecondary[n] = KeyCode.None;
				}
			}
		}
	}

	private void _CheckDuplicateStrings(int _num, int _count) {
		if (allowDuplicates) { return; }

		for (int n = 0; n < length; n++) {
			if (_count == 1) {
				if (_num != n && _axisPrimary[_num] == _axisPrimary[n]) {
					_axisPrimary[n] = "";
					_inputPrimary[n] = KeyCode.None;
				}

				if (_axisPrimary[_num] == _axisSecondary[n]) {
					_axisSecondary[n] = "";
					_inputSecondary[n] = KeyCode.None;
				}
			}

			if (_count == 2) {
				if (_axisSecondary[_num] == _axisPrimary[n]) {
					_axisPrimary[n] = "";
					_inputPrimary[n] = KeyCode.None;
				}

				if (_num != n && _axisSecondary[_num] == _axisSecondary[n]) {
					_axisSecondary[n] = "";
					_inputSecondary[n] = KeyCode.None;
				}
			}
		}
	}

	#endregion //Duplicate Prevention functions

	#endregion //Miscellaneous functions

	#region CheckInputs & _InputScans functions

	/// <summary>This is the magic that updates the values for all the inputs in cInput</summary>
	private void _CheckInputs() {
		bool inputPrimary = false; // a digital button/key; true if it's currently being pushed down
		bool inputSecondary = false; // a digital button/key; true if it's currently being pushed down
		bool axisPrimaryDefined = false; // whether or not an axis has a primary input defined for this input
		bool axisSecondaryDefined = false; // whether or not an axis has a secondary input defined for this input
		float axisPrimaryValue = 0f; // the value of the primary input for this element
		float axisSecondaryValue = 0f; // the value of the secondary input for this element

		#region Update input values

		for (int n = 0; n < _inputLength + 1; n++) {

			#region Handle cInput Keys/Buttons

			inputPrimary = Input.GetKey(_inputPrimary[n]);
			inputSecondary = Input.GetKey(_inputSecondary[n]);

			#region Check Modifiers
			bool _pModPressed = false; // is the primary modifier currently being pressed?
			bool _sModPressed = false; // is the secondary modifier currently being pressed?
			bool _modifierPressed = false; // is any modifier currently being pressed?

			for (int i = 0; i < _modifiers.Count; i++) {
				if (Input.GetKey(_modifiers[i])) {
					_modifierPressed = true; // at least one modifier is active
					if (!_pModPressed && _modifiers[i] == _modifierUsedPrimary[n]) { _pModPressed = true; }
					if (!_sModPressed && _modifiers[i] == _modifierUsedSecondary[n]) { _sModPressed = true; }
				}
			}

			/* These next two lines are really ugly, so here's an explanation of the parts:
			 * (_modifierUsedPrimary[n] == _inputPrimary[n]) <-- means there is NO modifier for this input
			 * (!_modifierPressed) <-- means there was NO modifier key pushed
			 * (_modifierUsedPrimary[n] != _inputPrimary[n]) <-- means there IS a modifier for this input
			 * (_pModPressed) <-- means the modifier for this input HAS been pushed.
			 * 
			 * So what this does is checks two things:
			 * If there's no modifier AND no modifier keys are being pressed, we're good to go.
			 * OR
			 * If there is a modifier AND the modifier key is being pressed, we're good to go.
			 * */
			// These bools are used to determine if this key's modifier (if any) is being pushed.
			bool _primaryModifierPassed = (((_modifierUsedPrimary[n] == _inputPrimary[n]) && !_modifierPressed) || ((_modifierUsedPrimary[n] != _inputPrimary[n]) && _pModPressed));
			bool _secondaryModifierPassed = (((_modifierUsedSecondary[n] == _inputSecondary[n]) && !_modifierPressed) || (_modifierUsedSecondary[n] != _inputSecondary[n] && _sModPressed));
			#endregion //Check Modifiers

			if (!string.IsNullOrEmpty(_axisPrimary[n])) {
				axisPrimaryDefined = true; // this is an axis
				axisPrimaryValue = _GetCalibratedAxisInput(_axisPrimary[n]) * _PosOrNeg(_axisPrimary[n]);
			} else {
				axisPrimaryDefined = false; // this isn't an axis
				// set the value to 1 if the key is being pushed down, otherwise it's zero
				axisPrimaryValue = inputPrimary ? 1f : 0f;
			}

			if (!string.IsNullOrEmpty(_axisSecondary[n])) {
				axisSecondaryDefined = true;
				axisSecondaryValue = _GetCalibratedAxisInput(_axisSecondary[n]) * _PosOrNeg(_axisSecondary[n]);
			} else {
				axisSecondaryDefined = false; // this isn't an axis
				// set the value to 1 if the key is being pushed down, otherwise it's zero
				axisSecondaryValue = inputSecondary ? 1f : 0f;
			}

			#region GetKey
			if ((inputPrimary && _primaryModifierPassed) || (inputSecondary && _secondaryModifierPassed) || (axisPrimaryDefined && axisPrimaryValue > deadzone) || (axisSecondaryDefined && axisSecondaryValue > deadzone)) {
				_getKeyArray[n] = true;
			} else {
				_getKeyArray[n] = false;
			}
			#endregion //GetKey

			#region GetKeyDown
			if ((_primaryModifierPassed && Input.GetKeyDown(_inputPrimary[n])) || (_secondaryModifierPassed && Input.GetKeyDown(_inputSecondary[n]))) {
				_getKeyDownArray[n] = true;
			} else {
				bool doOnce = false;
				if (axisPrimaryDefined && axisPrimaryValue > deadzone && !_axisTriggerArrayPrimary[n]) {
					_axisTriggerArrayPrimary[n] = true;
					doOnce = true;
				}
				if (axisSecondaryDefined && axisSecondaryValue > deadzone && !_axisTriggerArraySecondary[n]) {
					_axisTriggerArraySecondary[n] = true;
					doOnce = true;
				}

				_getKeyDownArray[n] = ((_axisTriggerArrayPrimary[n] || _axisTriggerArraySecondary[n]) && doOnce);
			}
			#endregion //GetKeyDown

			#region GetKeyUp
			if ((Input.GetKeyUp(_inputPrimary[n]) && _primaryModifierPassed) || (Input.GetKeyUp(_inputSecondary[n]) && _secondaryModifierPassed)) {
				_getKeyUpArray[n] = true;
			} else {
				bool doOnce = false;
				if (axisPrimaryDefined && axisPrimaryValue <= deadzone && _axisTriggerArrayPrimary[n]) {
					_axisTriggerArrayPrimary[n] = false;
					doOnce = true;
				}

				if (axisSecondaryDefined && axisSecondaryValue <= deadzone && _axisTriggerArraySecondary[n]) {
					_axisTriggerArraySecondary[n] = false;
					doOnce = true;
				}

				_getKeyUpArray[n] = ((!_axisTriggerArrayPrimary[n] || !_axisTriggerArraySecondary[n]) && doOnce);
			}
			#endregion //GetKeyUp

			#endregion //Handle cInput Keys/Buttons

			#region Handle cInput Axes

			// Store global sensitivity, gravity and deadzone so we can change them and restore them later.
			// I know it seems silly to do this every iteration of the loop, but for some reason if we don't, it breaks things.
			float defaultSens = sensitivity;
			float defaultGrav = gravity;
			float defaultDead = deadzone;
			// Set individual sensitivity, gravity and deadzone
			sensitivity = (_individualAxisSens[n] != -99) ? _individualAxisSens[n] : defaultSens;
			gravity = (_individualAxisGrav[n] != -99) ? _individualAxisGrav[n] : defaultGrav;
			deadzone = (_individualAxisDead[n] != -99) ? _individualAxisDead[n] : defaultDead;

			// this keeps input working even if Time.deltaTime is 0
			float fauxDeltaTime = (Time.deltaTime == 0) ? 0.012f : Time.deltaTime;

			// gets the axis value(s) and apply smoothing (sensitivity/gravity) for non-raw value
			if (axisPrimaryValue > deadzone || axisSecondaryValue > deadzone) {
				// for the raw value, just take the highest value from the primary or secondary input
				_getAxisRaw[n] = Mathf.Max(axisPrimaryValue, axisSecondaryValue);

				// use sensitivity settings to gradually bring the non-raw value up to the raw value if not already there
				if (_getAxis[n] < _getAxisRaw[n]) { _getAxis[n] = Mathf.Min(_getAxis[n] + sensitivity * fauxDeltaTime, _getAxisRaw[n]); }
				// use gravity settings to gradually bring the non-raw value back down to the raw value if analog input decreases
				if (_getAxis[n] > _getAxisRaw[n]) { _getAxis[n] = Mathf.Max(_getAxisRaw[n], _getAxis[n] - gravity * fauxDeltaTime); }
			} else {
				// both inputs are less than or equal to deadzone cutoff
				_getAxisRaw[n] = 0; //pretend you're not getting any value at all on the raw axis

				// use gravity settings to gradually bring the non-raw value back down to zero if not already there
				if (_getAxis[n] > 0) { _getAxis[n] = Mathf.Max(0, _getAxis[n] - gravity * fauxDeltaTime); }
			}

			// Restore global sensitivity, gravity and deadzone.
			// I know it seems silly to do this every iteration of the loop, but for some reason if we don't, it breaks things.
			sensitivity = defaultSens;
			gravity = defaultGrav;
			deadzone = defaultDead;

			#endregion //Handle cInput Axes

		}

		#endregion //Update input values

		/*
		 * NO LONGER IN THE FOR LOOP ABOVE WHICH GETS THE VALUES OF THE INPUTS!
		*/

		// compile the virtual axes (negative and positive)
		for (int n = 0; n <= _axisLength; n++) {
			int neg = _makeAxis[n, 0];
			int pos = _makeAxis[n, 1];
			if (_makeAxis[n, 1] == -1) {
				// This axis has no "positive" input defined, so use the "negative" axis as the default value
				_getAxisArray[n] = _getAxis[neg];
				_getAxisArrayRaw[n] = _getAxisRaw[neg];
			} else {
				// This axis has both a negative and positive input defined, so combine them for the result
				_getAxisArray[n] = _getAxis[pos] - _getAxis[neg];
				_getAxisArrayRaw[n] = _getAxisRaw[pos] - _getAxisRaw[neg];
			}
		}
	}

	/// <summary>This is where we detect what input is being pressed to assign inputs using the GUI</summary>
	private void _InputScans() {
		KeyCode _tmpModifier = KeyCode.None;
		if (Input.GetKey(KeyCode.Escape)) {
			if (_cScanInput == 1) {
				_inputPrimary[_cScanIndex] = KeyCode.None;
				_axisPrimary[_cScanIndex] = "";
				_cScanInput = 0;
			}

			if (_cScanInput == 2) {
				_inputSecondary[_cScanIndex] = KeyCode.None;
				_axisSecondary[_cScanIndex] = "";
				_cScanInput = 0;
			}
		}

		#region keyboard + mouse + joystick button scanning

		if (_scanning && Input.anyKeyDown && !Input.GetKey(KeyCode.Escape)) {
			KeyCode _key = KeyCode.None;

			for (int i = (int)KeyCode.None; i < 450; i++) {
				KeyCode _ckey = (KeyCode)i;
				if (_ckey.ToString().StartsWith("Mouse")) {
					if (!_allowMouseButtons) {
						continue;
					}
				} else if (_ckey.ToString().StartsWith("Joystick")) {
					if (!_allowJoystickButtons) {
						continue;
					}
				} else if (!_allowKeyboard) {
					continue;
				}

				// loop through modifier list and set the input key
				for (int n = 0; n < _modifiers.Count; n++) {
					if (Input.GetKeyDown(_modifiers[n])) {
						return;
					}

					if (Input.GetKeyDown(_ckey)) {
						_key = _ckey;
						_tmpModifier = _ckey; // if this doesn't change it means there is no modifier used to set this input
						bool markedAsAxis = false; // has this key been marked as an axis?
						for (int m = 0; m < _markedAsAxis.Count; m++) {
							if (_cScanIndex == _markedAsAxis[m]) {
								markedAsAxis = true;
								break; // no need to loop through the rest
							}
						}

						// check if modifier is been pressed and that the inputs aren't part of an axis
						if (Input.GetKey(_modifiers[n]) && !markedAsAxis) {
							_tmpModifier = _modifiers[n]; // if this is being set here it means we have a modifier being pressed down
							break;
						}
					}
				}
			}

			if (_key != KeyCode.None) {
				bool _keyCleared = true;
				// check if the entered key is forbidden
				for (int b = 0; b < _forbiddenKeys.Count; b++) {
					if (_key == _forbiddenKeys[b]) {
						_keyCleared = false;
						break;
					}
				}

				if (_keyCleared) {
					if (_cScanInput == 1) {
						_inputPrimary[_cScanIndex] = _key;
						_modifierUsedPrimary[_cScanIndex] = _tmpModifier; // set the modifier being used 
						_axisPrimary[_cScanIndex] = "";
						_CheckDuplicates(_cScanIndex, _cScanInput);
					}

					if (_cScanInput == 2) {
						_inputSecondary[_cScanIndex] = _key;
						_modifierUsedSecondary[_cScanIndex] = _tmpModifier; // set the modifier being used
						_axisSecondary[_cScanIndex] = "";
						_CheckDuplicates(_cScanIndex, _cScanInput);
					}

					_cScanInput = 0;
				}
			}
		}

		#region mouse scroll wheel scanning (considered to be a mousebutton)

		if (_allowMouseButtons) {
			//if (!Mathf.Approximately(_axisRawValues["Mouse Wheel"], Input.GetAxisRaw("Mouse Wheel"))) {
			if (Input.GetAxis("Mouse Wheel") > 0 && !Input.GetKey(KeyCode.Escape)) {
				if (!_forbiddenAxes.Contains("Mouse Wheel Up")) {
					if (_cScanInput == 1) {
						_axisPrimary[_cScanIndex] = "Mouse Wheel Up";
					}

					if (_cScanInput == 2) {
						_axisSecondary[_cScanIndex] = "Mouse Wheel Up";
					}

					_CheckDuplicateStrings(_cScanIndex, _cScanInput);
					_cScanInput = 0;
				}
			} else if (Input.GetAxis("Mouse Wheel") < 0 && !Input.GetKey(KeyCode.Escape)) {
				if (!_forbiddenAxes.Contains("Mouse Wheel Down")) {
					if (_cScanInput == 1) {
						_axisPrimary[_cScanIndex] = "Mouse Wheel Down";
					}

					if (_cScanInput == 2) {
						_axisSecondary[_cScanIndex] = "Mouse Wheel Down";
					}

					_CheckDuplicateStrings(_cScanIndex, _cScanInput);
					_cScanInput = 0;
				}
			}
			//}
		}

		#endregion //mouse scroll wheel scanning (considered to be a mousebutton)

		#endregion // keyboard + mouse + joystick button scanning

		#region mouse axis scanning

		if (_allowMouseAxis) {
			//if (!Mathf.Approximately(_axisRawValues["Mouse Horizontal"], Input.GetAxisRaw("Mouse Horizontal"))) {
			if (Input.GetAxis("Mouse Horizontal") < -deadzone && !Input.GetKey(KeyCode.Escape)) {
				if (!_forbiddenAxes.Contains("Mouse Left")) {
					if (_cScanInput == 1) {
						_axisPrimary[_cScanIndex] = "Mouse Left";
					}

					if (_cScanInput == 2) {
						_axisSecondary[_cScanIndex] = "Mouse Left";
					}

					_CheckDuplicateStrings(_cScanIndex, _cScanInput);
					_cScanInput = 0;
				}
			} else if (Input.GetAxis("Mouse Horizontal") > deadzone && !Input.GetKey(KeyCode.Escape)) {
				if (!_forbiddenAxes.Contains("Mouse Right")) {
					if (_cScanInput == 1) {
						_axisPrimary[_cScanIndex] = "Mouse Right";
					}

					if (_cScanInput == 2) {
						_axisSecondary[_cScanIndex] = "Mouse Right";
					}

					_CheckDuplicateStrings(_cScanIndex, _cScanInput);
					_cScanInput = 0;
				}
			}
			//}

			//if (!Mathf.Approximately(_axisRawValues["Mouse Vertical"], Input.GetAxisRaw("Mouse Vertical"))) {
			if (Input.GetAxis("Mouse Vertical") > deadzone && !Input.GetKey(KeyCode.Escape)) {
				if (!_forbiddenAxes.Contains("Mouse Up")) {
					if (_cScanInput == 1) {
						_axisPrimary[_cScanIndex] = "Mouse Up";
					}

					if (_cScanInput == 2) {
						_axisSecondary[_cScanIndex] = "Mouse Up";
					}

					_CheckDuplicateStrings(_cScanIndex, _cScanInput);
					_cScanInput = 0;
				}
			} else if (Input.GetAxis("Mouse Vertical") < -deadzone && !Input.GetKey(KeyCode.Escape)) {
				if (!_forbiddenAxes.Contains("Mouse Down")) {
					if (_cScanInput == 1) {
						_axisPrimary[_cScanIndex] = "Mouse Down";
					}

					if (_cScanInput == 2) {
						_axisSecondary[_cScanIndex] = "Mouse Down";
					}

					_CheckDuplicateStrings(_cScanIndex, _cScanInput);
					_cScanInput = 0;
				}
			}
			//}
		}

		#endregion // mouse axis scanning

		#region joystick axis scanning

		if (_allowJoystickAxis) {
			float scanningDeadzone = 0.25f;
			for (int gamepad = 1; gamepad <= _numGamepads; gamepad++) {
				for (int axis = 1; axis <= 10; axis++) {
					string _joystring = _joyStrings[gamepad, axis];
					string _joystringPos = _joyStringsPos[gamepad, axis];
					string _joystringNeg = _joyStringsNeg[gamepad, axis];

					float axisRaw = Input.GetAxisRaw(_joystring);
					bool axisChanged = false; // whether or not the axis' value has changed during scanning

					// we do it this way to avoid exception if key is not found in dictionary
					if (_axisRawValues.ContainsKey(_joystring)) {
						if (!Mathf.Approximately(_axisRawValues[_joystring], axisRaw)) { axisChanged = true; }
					}

					/*if (!axisChanged) {
						if (_axisRawValues.ContainsKey(_joystringPos)) {
							if (!Mathf.Approximately(_axisRawValues[_joystringPos], axisRaw)) {
								axisChanged = true;
							}
						}
					}

					if (!axisChanged) {
						if (_axisRawValues.ContainsKey(_joystringNeg)) {
							if (!Mathf.Approximately(_axisRawValues[_joystringNeg], axisRaw)) {
								axisChanged = true;
							}
						}
					}*/

					if (axisChanged) {

						#region Special Xbox gamepad trigger handling

						// Why does this check for OSX have to be so ugly?
						if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer) {
							// On OSX, Xbox triggers are bound to axes 5 (-/+) and 6 (-/+).
							// The problem with this is that they return a value between -1 and 1,
							// defaulting to -1 when they aren't pressed, 0 when they're pressed half way,
							// and 1 when fully pressed.
							// This code changes the value to be 0 by default and 1 when fully pressed.

							if (axis == 5 || axis == 6) {
								string gamepadName = Input.GetJoystickNames()[gamepad - 1];
								// possible Mac gamepad names for Xbox gamepad from the very useful InControl
								string[] JoystickNames = new[] {
									"", // Yes, really.
									"Microsoft Wireless 360 Controller",
									"Mad Catz, Inc. Mad Catz FPS Pro GamePad",
									"\u00A9Microsoft Corporation Controller"
								};

								for (int z = 0; z < JoystickNames.Length; z++) {
									if (gamepadName == JoystickNames[z]) {
										axisRaw = (axisRaw + 1) / 2;
										break; // finished iterating OSX gamepad names
									}
								}
							}
						} else {
							string gamepadName = Input.GetJoystickNames()[gamepad - 1];
							string _xboxOneWin = "Controller (Xbox One For Windows)";

							if (gamepadName.ToLower() == _xboxOneWin.ToLower()) {
								// On Windows, Xbox One triggers are bound to axes 3 (-/+) and 6 (-/+)
								// as well as axes 9(+) and 10(+). This mirrors the OSX problem with Xbox 360 gamepads.
								// So just use axes 9 and 10.
								if (axis == 3) {
									_joystringPos = _joystringNeg = _joyStringsPos[gamepad, 9];
								} else if (axis == 6) {
									_joystringPos = _joystringNeg = _joyStringsPos[gamepad, 10];
								}
							} else if (axis == 3) {
								// On Windows, Xbox 360 triggers are bound both to axis 3 (+/-) and axes 9 (+) and 10 (+).
								// The problem with letting them bind to axis 3 is if both are pressed,
								// it returns -1 + 1 which is 0, which is the same as neither of them being pressed.
								// This code tries to prevent them from being bound to the same axis, so they can both be
								// pressed without interfering with each other.

								// if this is the gamepad's 3rd axis we want to check if either
								// axis 9 or 10 is also returning a value

								string lTrigger = _joyStringsPos[gamepad, 9];
								string rTrigger = _joyStringsPos[gamepad, 10];

								// if axis 9 or 10 has a positive value above scanningDeadzone, use that axis instead of axis 3
								if (_GetCalibratedAxisInput(lTrigger) > scanningDeadzone) {
									_joystringPos = lTrigger;
									_joystringNeg = lTrigger;
								} else if (_GetCalibratedAxisInput(rTrigger) > scanningDeadzone) {
									_joystringPos = rTrigger;
									_joystringNeg = rTrigger;
								}
							}
						}

						#endregion //Special Xbox gamepad trigger handling

						float axisVal;

						if (axisRaw < 0) {
							// if the raw value is negative
							// ignore this axis if it has been forbidden
							if (_forbiddenAxes.Contains(_joystringNeg)) { continue; }
							// otherwise the axis is the calibrated input of the negative axis
							axisVal = _GetCalibratedAxisInput(_joystringNeg);
						} else {
							// else raw value is positive
							// ignore this axis if it has been forbidden
							if (_forbiddenAxes.Contains(_joystringPos)) { continue; }
							// otherwise it's the calibrated input of the positive axis
							axisVal = _GetCalibratedAxisInput(_joystringPos);
						}

						if (_scanning && Mathf.Abs(axisVal) > scanningDeadzone && !Input.GetKey(KeyCode.Escape)) {
							//Debug.Log("Calibrated value: " + axis + ". Raw value: " + Input.GetAxisRaw(_joystring));
							if (_cScanInput == 1) {
								if (axisVal > scanningDeadzone) {
									_axisPrimary[_cScanIndex] = _joystringPos;
								} else if (axisVal < -scanningDeadzone) {
									_axisPrimary[_cScanIndex] = _joystringNeg;
								}

								_CheckDuplicateStrings(_cScanIndex, _cScanInput);
								_cScanInput = 0;
								return; // found our input, so don't iterate anymore
							} else if (_cScanInput == 2) {
								if (axisVal > scanningDeadzone) {
									_axisSecondary[_cScanIndex] = _joystringPos;
								} else if (axisVal < -scanningDeadzone) {
									_axisSecondary[_cScanIndex] = _joystringNeg;
								}

								_CheckDuplicateStrings(_cScanIndex, _cScanInput);
								_cScanInput = 0;
								return; // found our input, so don't iterate anymore
							}
						}
					}
				}
			}
		}

		#endregion // joystick axis scanning

	}

	#endregion //CheckInputs & _InputScans functions
}

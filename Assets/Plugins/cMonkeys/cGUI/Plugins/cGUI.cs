using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class cGUI : MonoBehaviour {

	public static event System.Action OnGUIToggled;

	#region Variables/Properties

	#region Public Variables/Properties

	/// <summary>Whether or not the cAudio Plugin is present. You should never set this manually.</summary>
	public static bool cAudioExists = false;
	/// <summary>Whether or not the cInput Plugin is present. You should never set this manually.</summary>
	public static bool cInputExists = false;
	/// <summary>Whether or not the cVideo Plugin is present. You should never set this manually.</summary>
	public static bool cVideoExists = false;

	/// <summary>The custom GUISkin to use for the GUI.</summary>
	public static GUISkin cSkin;

	/// <summary>The max size of the menu window. If the menu window is bigger than the screen then the menu window will be fullscreen.</summary>
	public static Vector2 windowMaxSize {
		get { return _windowSize; }
		set {
			if (value.x < 800) {
				value.x = 800;
				Debug.LogWarning("cGUI.windowSize error: " + value.x + " width is too low. Minimum width is 800 for this menu. Defaulting the width to 800.");
			}

			if (value.y < 600) {
				value.y = 600;
				Debug.LogWarning("cGUI.windowSize error: " + value.y + " height is too low. Minimum height is 600 for this menu. Defaulting the height to 600.");
			}

			_windowSize = value;
		}
	}

	/// <summary>The background color of the GUI.</summary>
	public static Color bgColor {
		get { return _bgColor; }
		set {
			_bgColor = value;
			UpdateGUIColors();
		}
	}

	/// <summary>Is cGUI showing the cAudio GUI?</summary>
	public static bool showingAudioGUI { get { return _showAudioGUI; } }
	/// <summary>Is cGUI showing the cInput GUI?</summary>
	public static bool showingInputGUI { get { return _showInputGUI; } }
	/// <summary>Is cGUI showing the cVideo GUI?</summary>
	public static bool showingVideoGUI { get { return _showVideoGUI; } }
	/// <summary>Is cGUI showing any GUI?</summary>
	public static bool showingAnyGUI { get { return (showingAudioGUI || showingInputGUI || showingVideoGUI); } }

	#endregion // Public Variables/Properties

	#region Private Variables

	private static bool _showAudioGUI = false;
	private static bool _showInputGUI = false;
	private static bool _showVideoGUI = false;

	private static bool _cSkinWarned = false;

	private static Color _bgColor;
	private static Vector2 _windowSize = new Vector2(1024, 600);

	// droplist vars
	private static Vector2 resScroll;

	#endregion // Private Variables

	#endregion // Variables/Properties

	#region Public Functions

	/// <summary>Toggles the GUI on or off.</summary>
	public static void ToggleGUI() {
		if (showingAnyGUI) {
			_HideGUI();
		} else {
			_ShowGUI();
		}
	}

	/// <summary>Displays the cAudio GUI if cAudio is in the project.</summary>
	public static void ShowAudioGUI() {
		if (!cAudioExists) { return; }
		_showAudioGUI = true;
		_showInputGUI = false;
		_showVideoGUI = false;
		if (OnGUIToggled != null) { OnGUIToggled(); }
	}

	/// <summary>Displays the cInput GUI if cInput is in the project.</summary>
	public static void ShowInputGUI() {
		if (!cInputExists) { return; }
		_showAudioGUI = false;
		_showInputGUI = true;
		_showVideoGUI = false;
		if (OnGUIToggled != null) { OnGUIToggled(); }
	}

	/// <summary>Displays the cVideo GUI if cVideo is in the project.</summary>
	public static void ShowVideoGUI() {
		if (!cVideoExists) { return; }
		_showAudioGUI = false;
		_showInputGUI = false;
		_showVideoGUI = true;
		if (OnGUIToggled != null) { OnGUIToggled(); }
	}

	/// <summary>Used to display a dropdown selection box. Both displayList and index are references so make sure to use ref!</summary>
	/// <param name="rect">The Rect within which to display the selection box.</param>
	/// <param name="stringArray">A string array of all the options that will be shown in the dropdown box.</param>
	/// <param name="maxShown">The maximum number of options shown in the dropdown list at once.</param>
	/// <param name="displayList">Whether or not the box is dropped down. False means the dropdown box is "collapsed."</param>
	/// <param name="index">The index of the currently selected option of the stringArray.</param>
	/// <returns>True if the dropdown box is expanded. False if it is collapsed.</returns>
	public static bool SelectBox(Rect rect, string[] stringArray, int maxShown, ref bool displayList, ref int index) {
		/* You can use the selectbox as you would use a button.
		 *          
		 * Example:         
		 * if (cGUI.SelectBox(myRect, myStringArray, 5, ref myBool, ref myInt)) {
		 *		Debug.Log(myStringArray[myInt]); 
		 * }
		 */

		// droplist private vars 
		int bLength = Mathf.Clamp(stringArray.Length, 1, maxShown);
		Rect scrollRect1 = new Rect(rect.x, rect.y + rect.height, rect.width, rect.height * bLength);
		Rect scrollRect2 = new Rect(rect.x, rect.y, rect.width - 15, rect.height * stringArray.Length);
		Rect checkRect = new Rect(rect.x, rect.y, rect.width, rect.height * (bLength + 1));
		bool sBox = false;

		string dropdownbox = "dropdownbox";
		string selectionbox = "selectionbox";

		if (!cSkin) {
			dropdownbox = "button";
			selectionbox = "button";
		}

		if (GUI.Button(rect, stringArray[index], dropdownbox)) {
			displayList = !displayList; // open or close the droplist by changing the droplist bool
			resScroll = Vector2.zero;
		}

		if (displayList) {
			// we want to show the droplist...
			resScroll = GUI.BeginScrollView(scrollRect1, resScroll, scrollRect2); // set the scroll list size
			for (int n = 0; n < stringArray.Length; n++) {
				Rect _r = new Rect(rect.x, rect.y + (rect.height * (n)), rect.width, rect.height); // make rects for the droplist selections
				if (GUI.Button(_r, stringArray[n].ToString(), selectionbox)) {
					index = n; // set the array value
					displayList = false;
					sBox = true;
				}
			}

			GUI.EndScrollView();
		}

		// close selectbox if clicked outside the selectbox
		if (!checkRect.Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0)) {
			displayList = false;
		}

		return sBox;
	}

	/// <summary>GUI Menu Colors</summary>
	public static void UpdateGUIColors() {
		GUI.backgroundColor = bgColor;
		Color _c = cGUI.bgColor * 0.75f;
		if (cSkin) {
			cSkin.button.normal.textColor = _c;
			cSkin.label.normal.textColor = _c;
			cSkin.textField.normal.textColor = _c;
			cSkin.textArea.normal.textColor = _c;

			for (int n = 0; n < 6; n++) { cSkin.customStyles[n].normal.textColor = _c; }
			cSkin.customStyles[1].normal.textColor = Color.white;
			cSkin.customStyles[7].normal.textColor = Color.white;
			cSkin.customStyles[6].normal.textColor = Color.white;
		}
	}

	#endregion // Public Functions

	#region Private Functions

	private void Awake() {
		_bgColor = GUI.color;
	}

	private void Start() {
		// make sure alpha isn't turned completely off
		if (_bgColor.a == 0) {
			_bgColor.a = 1;
		}
	}

	/// <summary>Attempts to display the GUI for cInput, cAudio, or cVideo, if they exist.</summary>
	private static void _ShowGUI() {
		if (!cSkin && !_cSkinWarned) {
			Debug.Log("No cGUI skin loaded. Please use cGUI.cSkin to assign a GUI skin.");
			_cSkinWarned = true; // don't warn about this again
		}

		if (cInputExists) {
			ShowInputGUI();
		} else if (cAudioExists) {
			ShowAudioGUI();
		} else if (cVideoExists) {
			ShowVideoGUI();
		}
	}

	/// <summary>Hides all GUIs.</summary>
	private static void _HideGUI() {
		_showAudioGUI = false;
		_showInputGUI = false;
		_showVideoGUI = false;
		if (OnGUIToggled != null) { OnGUIToggled(); }
	}

	#endregion // Private Functions

}

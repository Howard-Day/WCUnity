using UnityEngine;

[RequireComponent(typeof(cGUI))]
public class cInputGUI : MonoBehaviour {
	Rect windowRect;
	float _wHeight = cGUI.windowMaxSize.y;
	float _wWidth = cGUI.windowMaxSize.x;
	bool showPopUp;
	private Vector2 _scrollPosition;
	private float _clickDelay = 0f;

	private string label2, box2, popwindow, smallbutton;

	void Start() {
		cGUI.cInputExists = true;
	}

	#region OnGUI

	void OnGUI() {
		if (cGUI.showingInputGUI) {

			if (cInput.scanning) {
				_clickDelay = Time.realtimeSinceStartup + 0.15f;
			}

			GUI.skin = cGUI.cSkin;
			cGUI.UpdateGUIColors();

			if (Screen.height - cGUI.windowMaxSize.y < 0) { _wHeight = Screen.height; } else { _wHeight = cGUI.windowMaxSize.y; }
			if (Screen.width - cGUI.windowMaxSize.x < 0) { _wWidth = Screen.width; } else { _wWidth = cGUI.windowMaxSize.x; }
			windowRect = new Rect((Screen.width - _wWidth) / 2, (Screen.height - _wHeight) / 2, _wWidth, _wHeight);
			//windowRect = GUILayout.Window(0, windowRect, MenuWindow, "");
			GUI.Window(0, windowRect, MenuWindow, "");
			if (showPopUp) {
				GUI.Window(1, new Rect((Screen.width - 512) / 2, (Screen.height - 350) / 2, 512, 350), popUp, "", popwindow);
				GUI.BringWindowToFront(1);
				GUI.FocusWindow(1);
			}
		}
	}

	void popUp(int windowID) {
		GUI.FocusWindow(1);
		GUI.TextField(new Rect(40, 100, 450, 95), "Please leave all analog inputs in their neutral positions.\n\nClick on OK when ready.");

		Rect _buttonRect = new Rect(150, 284, 200, 35);
		GUI.Button(_buttonRect, "OK", smallbutton);
		if (_buttonRect.Contains(Event.current.mousePosition) && Input.GetMouseButtonUp(0) && showPopUp) {
			cInput.Calibrate();
			showPopUp = false;
		}
	}

	void MenuWindow(int windowID) {
		if (!showPopUp) { GUI.FocusWindow(0); }
		GUI.backgroundColor = cGUI.bgColor;
		#region left menu ---------------------------------------------

		float _buttonWidth = 128;
		float _buttonHeight = 35;
		float _lbuttonStartH = _wWidth / 26.5f;
		int _strvert = 7;
		if (cGUI.cSkin && cGUI.cSkin.name == "cGUISkin Dark") { _strvert = 6; }
		float _lbuttonStartV = _wHeight / _strvert;

		float _lbuttonEnd = _wHeight - (_wHeight / 6);
		float _lbuttonSpace = 50;
		int _showInt = 0;

		GUI.SetNextControlName("textarea"); // set button to active mode
		if (GUI.Button(new Rect(_lbuttonStartH, _lbuttonStartV + (_lbuttonSpace * _showInt++), _buttonWidth, _buttonHeight), "  INPUTS")) {
			// don't actually do anything because this one is already activated
		}

		GUI.FocusControl("textarea");

		if (cGUI.cAudioExists) {
			if (GUI.Button(new Rect(_lbuttonStartH, _lbuttonStartV + (_lbuttonSpace * _showInt++), _buttonWidth, _buttonHeight), "  AUDIO")) {
				cGUI.ShowAudioGUI();
			}
		}

		if (cGUI.cVideoExists) {
			if (GUI.Button(new Rect(_lbuttonStartH, _lbuttonStartV + (_lbuttonSpace * _showInt++), _buttonWidth, _buttonHeight), "  VIDEO")) {
				cGUI.ShowVideoGUI();
			}
		}


		if (GUI.Button(new Rect(_lbuttonStartH, _lbuttonEnd - (_lbuttonSpace * 2), _buttonWidth, _buttonHeight), "  CALIBRATE")) {
			showPopUp = true;
		}

		if (GUI.Button(new Rect(_lbuttonStartH, _lbuttonEnd - (_lbuttonSpace), _buttonWidth, _buttonHeight), "  DEFAULTS")) {
			cInput.ResetInputs(); // reset cInput to defaults
		}

		if (GUI.Button(new Rect(_lbuttonStartH, _lbuttonEnd, _buttonWidth, _buttonHeight), "  EXIT")) {
			cGUI.ToggleGUI();
		}

		#endregion // left menu ---------------------------------------

		#region right menu (cInput) -----------------------------------

		float v_widthStartH1 = windowRect.width / 3;
		float v_widthStartH3 = windowRect.width / 1.8f;
		float v_widthStartV2 = windowRect.height / 9f;
		float v_space = 60;

		if (cGUI.cSkin) {
			label2 = "label";
			box2 = "box2";
			popwindow = "popwindow";
			smallbutton = "smallbutton";
		} else {
			label2 = "label";
			box2 = "box";
			popwindow = "window";
			smallbutton = "button";
		}

		Rect rightSideRect = new Rect(windowRect.width / 3.6f, v_widthStartV2 * 2.2f, windowRect.width * 0.67f, windowRect.height);

		// input settings
		GUI.Label(new Rect(rightSideRect.x + (rightSideRect.width / 2) - (_buttonWidth / 2), windowRect.height / 6.5f, _buttonWidth, _buttonHeight), "INPUT SETTINGS", label2);
		// GUI.Label(new Rect(windowRect.width * 0.57f, v_widthStartV1, _buttonWidth, _buttonHeight), "INPUT SETTINGS", label2);

		GUI.Label(new Rect(rightSideRect.x + (rightSideRect.width * 0.2f) - (_buttonWidth * 1.2f / 2), rightSideRect.y, _buttonWidth * 1.2f, _buttonHeight), "ACTION", label2);
		GUI.Label(new Rect(rightSideRect.x + (rightSideRect.width * 0.5f) - (_buttonWidth * 1.2f / 2), rightSideRect.y, _buttonWidth * 1.2f, _buttonHeight), "PRIMARY", label2);
		GUI.Label(new Rect(rightSideRect.x + (rightSideRect.width * 0.8f) - (_buttonWidth * 1.2f / 2), rightSideRect.y, _buttonWidth * 1.2f, _buttonHeight), "SECONDARY", label2);

		// scroll 
		_scrollPosition = GUI.BeginScrollView(new Rect(v_widthStartH1, v_widthStartV2 * 3.0f, v_widthStartH3 * 1.1f, windowRect.height * 0.58f), _scrollPosition, new Rect(v_widthStartH1, 0, v_widthStartH3, v_space * (cInput.length - 5)));

		for (int n = 0; n < cInput.length; n++) {
			GUI.Label(new Rect(rightSideRect.x + (rightSideRect.width * 0.2f) - (_buttonWidth * 1.2f / 2), 0 + (_buttonHeight * n), _buttonWidth * 1.2f, _buttonHeight), cInput.GetText(n, 0), "label"); // name of input
			if (GUI.Button(new Rect(rightSideRect.x + (rightSideRect.width * 0.5f) - (_buttonWidth * 1.2f / 2), 0 + (_buttonHeight * n), _buttonWidth * 1.2f, _buttonHeight), cInput.GetText(n, 1), box2) && Input.GetMouseButtonUp(0)) {
				if (Time.realtimeSinceStartup > _clickDelay) {
					cInput.ChangeKey(n, 1);
				}
			}

			if (GUI.Button(new Rect(rightSideRect.x + (rightSideRect.width * 0.8f) - (_buttonWidth * 1.2f / 2), 0 + (_buttonHeight * n), _buttonWidth * 1.2f, _buttonHeight), cInput.GetText(n, 2), box2)) {
				if (Time.realtimeSinceStartup > _clickDelay) {
					cInput.ChangeKey(n, 2);
				}
			}
		}

		GUI.EndScrollView();

		#endregion //right menu ---------------------------------------
	}

	#endregion //OnGUI

}

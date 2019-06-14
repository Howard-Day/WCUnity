using UnityEngine;
using System.Collections;

public class cInputDemoSetup : MonoBehaviour {
	public GUISkin guiSkin;
	public Color menuColor = new Color(0.09f, 0.77f, 1, 1);

	void Start() {
		// initialize cInput
		cInput.Init();
		// cGUI SETUP
		// set the guiskin for cGUI
		cGUI.cSkin = guiSkin;
		cGUI.bgColor = menuColor;
		// set the maxsize of the menu window. If this is greater then the screen size it will scaled to fullscreen.
		// the menu size will be clamped to the max screen size, so setting the size really high will garantee you 
		// that the menu will be filling the screen. If you don't set the window size it will default to 1024X600.
		cGUI.windowMaxSize = new Vector2(1024, 600);

		// cINPUT SETUP
		// first we setup the allowed modifier keys, by default there will be no modifiers. If you don't want modifier keys skip this step
		// keep in mind that if a key is set as modifier it can't be used as a normal input anymore!
		cInput.AddModifier(KeyCode.LeftShift);
		cInput.AddModifier(KeyCode.RightShift);
		cInput.AddModifier(KeyCode.LeftAlt);
		cInput.AddModifier(KeyCode.RightAlt);
		cInput.AddModifier(KeyCode.LeftControl);
		cInput.AddModifier(KeyCode.RightControl);

		// setting up the default inputkeys...
		cInput.SetKey("Pause", "P"); // sets the 'Pause' input to "P" - notice we didn't set up a secondary input-this will be defaulted to 'None'
		cInput.SetKey("Left", "A", "LeftArrow"); // sets the 'Left' primary input to 'A' and the secondary input to 'LeftArrow'
		cInput.SetKey("Right", "D", Keys.RightArrow); // inputs can be set as string or as Key, using the Keys class
		cInput.SetKey("Up", "W", Keys.UpArrow); // using the Keys class allows you to autocomplete the inputs
		cInput.SetKey("Down", "S", Keys.DownArrow);
		cInput.SetKey("Shoot", Keys.Space, Keys.X, Keys.None, Keys.LeftShift); // here we set up a default modifier key for "X" so ACTION "Shoot" will default to 'SPACE' & 'LeftShift+X' as default inputs 

		// The Keys class can be very helpful in getting the correct name.
		cInput.SetKey("Weapon 1", Keys.Joy1Axis1Negative);
		cInput.SetKey("Weapon 2", Keys.Joy1Axis1Positive);
		cInput.SetKey("Weapon 3", Keys.Joy1Axis2Negative);
		cInput.SetKey("Weapon 4", Keys.Joy1Axis2Positive);
		cInput.SetKey("Weapon 5", Keys.Joy1Axis3Negative);
		cInput.SetKey("Weapon 6", Keys.Joy1Axis3Positive);
		// Note that the above inputs aren't actually used in this demo.
		// They're just defined here to show you how it's done.

		// we define an axis like this:
		cInput.SetAxis("Horizontal", "Left", "Right"); // we set up the 'Horizontal' axis with 'Left' and 'Right'as inputs
		cInput.SetAxis("Vertical", "Up", "Down"); // we set up 'Vertical' axis with 'Up' and 'Down' as inputs. 
		// Notice we don't use the 'Vertical' axis in our control code in plane.cs but we don't want to allow modifier keys for inputs UP and DOWN. Any inputs that are part of an axis are ignoring modifiers
	}
}

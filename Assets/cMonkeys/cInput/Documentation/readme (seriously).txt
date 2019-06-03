How to test the Demo
--------------------

- Use the "Edit -> Project Settings -> cInput -> Setup InputManager Asset" menu command in the Unity Editor to create an InputManager.asset file designed to work with cInput.
- Open the cInput Demo Scene in the "cMonkeys/cInput/Example/" folder.
- Play around with it.



How to use cInput in your own project
-------------------------------------

You only need 2 files for cInput to work, these are "InputManager.asset" (created using the "Edit -> Project Settings -> cInput -> Setup InputManager Asset" menu option in the editor) & either "cInput.cs" or "cInput.dll" (depending on if you have cInput or cInput Pro). Once these two files are in the appropriate locations, all other files included in this package can safely be removed from your project (unless you want the optional features mentioned below).

- Use the "Edit -> Project Settings -> cInput -> Setup InputManager Asset" menu command in the Unity Editor to create an InputManager.asset file designed to work with cInput.
- If you have cInput Pro, you may need to place cInput.cs in the "[your project folder]/Assets/Plugins" folder if you want to access it using UnityScript (JavaScript).
- OPTIONAL: Again, if you have cInput Pro and if you want to use the Keys class or the included (Legacy) GUI, you may need to place the cKeys.cs, cInputGUI.cs, and cGUI.cs files in the "[your project folder]/Assets/Plugins" folder.



Please read the reference manual if you don't know what to do. :)
http://cinput2.weebly.com/reference-manual.html

We also have video tutorials to help you get started on our website:
http://cinput2.weebly.com/video-tutorials.html



Questions?

Contact Us: http://cinput2.weebly.com/contact.html
Forum Post: http://forum.unity3d.com/threads/cinput-2-0-unitys-custom-inputmanager-got-improved.130730/

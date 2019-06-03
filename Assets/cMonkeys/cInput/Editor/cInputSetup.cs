#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class cInputSetup : EditorWindow {
	// creates a menu entry that replaces a file.
	[MenuItem("Edit/Project Settings/cInput/Setup InputManager Asset")]
	static void ReplaceInputManagerAssetFile() {
		string path = Application.dataPath;
		path = path.Remove(path.Length - 6);

		string destPath = path + "ProjectSettings/";
		string sourcePath = path = "Assets/cMonkeys/cInput/Editor/";
		bool exit = false;

		if (UnityEditor.EditorUtility.DisplayDialog("Replace InputManager.asset file", "Replace InputManager.asset file with one designed to work with cInput?"
													+ " (A backup will be made.)"
													+ "\n\nMake sure the InputManager settings are NOT open in the inspector or this won't work!", "OK", "Cancel")) {
			if (!System.IO.File.Exists(destPath + "InputManager.backup")) {
				// backup the old intputmanager.asset file
				FileUtil.CopyFileOrDirectory(destPath + "InputManager.asset", destPath + "InputManager.backup");
			}

			// copy the new inputmanager.asset file over the old version
			FileUtil.ReplaceFile(sourcePath + "cInput.dat", destPath + "InputManager.asset");
			exit = true;
		}

		if (exit) {
			// load the changes we just made
			AssetDatabase.Refresh();
		}
	}
}
#endif

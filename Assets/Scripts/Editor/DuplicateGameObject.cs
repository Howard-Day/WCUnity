using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

public class DuplicateGameObject : MonoBehaviour
{
    [MenuItem("MoonBeast/Duplicate %d")]
    static void CustomDuplicate()
    {
        //Ignore things not in the scene and not Prefabs
        if (Selection.activeGameObject is GameObject && Selection.activeGameObject.activeInHierarchy && PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject) == PrefabInstanceStatus.NotAPrefab)
        {
            //Duplicate object
            GameObject duplicate = Instantiate(Selection.activeGameObject,
            Selection.activeGameObject.transform.position,
            Selection.activeGameObject.transform.rotation,
            Selection.activeGameObject.transform.parent);

            //Move directly underneath original
            duplicate.transform.SetSiblingIndex(
            Selection.activeGameObject.transform.GetSiblingIndex() + 1);

            //Rename and increment
            duplicate.name = IncrementName(Selection.activeGameObject.name);

            //Select new object
            Selection.activeGameObject = duplicate;

            //Register Undo
            Undo.RegisterCreatedObjectUndo(duplicate, "Duplicated GameObject");
        }
        //Keep prefab linkages
        else if ((Selection.activeGameObject is GameObject && Selection.activeGameObject.activeInHierarchy && PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject) == PrefabInstanceStatus.Connected))
        {
            GameObject original = Selection.activeGameObject;
            //Normal Dupe
            EditorApplication.ExecuteMenuItem("Edit/Duplicate");
            //Set the new object into cache
            GameObject duplicate = Selection.activeGameObject;
            //Move directly underneath original
            duplicate.transform.SetSiblingIndex(
            original.transform.GetSiblingIndex() + 1);
            //Rename and increment
            duplicate.name = IncrementName(original.name);
            //Register Undo
            Undo.RegisterCreatedObjectUndo(duplicate, "Duplicated GameObject");
        }
        //Handle all other cases
        else 
        {
            //Don't break default behaviour elsewhere
            EditorApplication.ExecuteMenuItem("Edit/Duplicate");
        }
    }


    private static string IncrementName(string input)
    {
        var dupNumberRegex = new Regex("\\d*$");

        // Extract the copy number string
        var dupNumberMatch = dupNumberRegex.Match(input).Value;

        // Remove the extracted number to get a clean string
        var inputWithoutPadding = dupNumberRegex.Replace(input, "");

        var padding = dupNumberMatch.Length;
        if (padding <= 0)
        {
            return $"{inputWithoutPadding}";
        }

        var parsedNumber = int.Parse(dupNumberMatch);
        var incrementedNumber = parsedNumber + 1;

        // Correctly pad the string and concatenate everything
        var newName = $"{inputWithoutPadding}{incrementedNumber.ToString().PadLeft(padding, '0')}";
        return newName;
    }

}

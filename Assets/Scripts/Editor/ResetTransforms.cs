using UnityEditor;
using UnityEngine;

public class ResetTransforms : MonoBehaviour
{
    [MenuItem("MoonBeast/Reset Local Transforms #r")]
    static void ResetXforms()
    {
        foreach (GameObject xformObject in Selection.gameObjects)
        {
            xformObject.transform.localScale = Vector3.one;
            xformObject.transform.localPosition = Vector3.zero;
            xformObject.transform.localEulerAngles = Vector3.zero;
        }
    }
}

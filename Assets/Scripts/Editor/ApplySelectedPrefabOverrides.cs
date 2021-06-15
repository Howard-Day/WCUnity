using UnityEditor;
using UnityEngine;

public class ApplySelectedPrefabOverrides : MonoBehaviour
{
    [MenuItem("MoonBeast/Apply Prefab Overrides %#x")]
    static void ApplyOverrides()
    {
        foreach (GameObject prefabObject in Selection.gameObjects)
        {
            //if ( PrefabStageUtility.GetPrefabStage(Selection.activeGameObject) != null)        
            if (PrefabUtility.GetPrefabInstanceStatus(prefabObject) != PrefabInstanceStatus.NotAPrefab)
            {
                PrefabUtility.ApplyPrefabInstance(prefabObject, InteractionMode.UserAction);
            }
        }
    }
}

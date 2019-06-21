using UnityEngine;

public class SnapToPixelGrid : MonoBehaviour 
{
    [SerializeField]
    private int pixelsPerUnit = 50;

    private Transform parent;

    private void Start()
    {
        parent = transform.parent;
    }

    /// <summary>
    /// Snap the object to the pixel grid determined by the given pixelsPerUnit.
    /// Using the parent's world position, this moves to the nearest pixel grid location by 
    /// offseting this GameObject by the difference between the parent position and pixel grid.
    /// </summary>
    private void LateUpdate() 
    {
        Vector3 newLocalPosition = Vector3.zero;

        newLocalPosition.x = (Mathf.Round(parent.position.x * pixelsPerUnit) / pixelsPerUnit) - parent.position.x;
        newLocalPosition.y = (Mathf.Round(parent.position.y * pixelsPerUnit) / pixelsPerUnit) - parent.position.y;

        transform.localPosition = newLocalPosition;
    }
}
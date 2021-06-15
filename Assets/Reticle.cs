using UnityEngine;

public class Reticle : MonoBehaviour
{
    public float xDrift;
    public float yDrift;
    public float zDrift;
    public GameObject rollReticle;

    ShipSettings shipMain;

    Vector3 initPos;
    // Start is called before the first frame update
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        initPos = transform.localPosition;

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 clampedDrift = Vector3.ClampMagnitude(shipMain.rotDelta, 4f);

        Vector3 shiftOffset = new Vector3(xDrift * -clampedDrift.y, yDrift * -clampedDrift.x, 0);

        transform.localPosition = initPos + shiftOffset;
        if (rollReticle)
        {
            Vector3 rollOffset = new Vector3(0,0,zDrift * clampedDrift.y);
            rollReticle.transform.localRotation = Quaternion.Euler(rollOffset);
        }

    }
}


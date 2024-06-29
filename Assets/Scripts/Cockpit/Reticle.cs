using UnityEngine;

public class Reticle : MonoBehaviour
{    
    public float chaseYOffset = 0f;
    public float xDrift;
    public float yDrift;    
    public float zDrift;
    
    public GameObject rollReticle;

    public bool chaseCamMode = false;

    ShipSettings shipMain;

    Vector3 initPos;
    Vector3 offsetPos;


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

        if (chaseCamMode)
        {
            offsetPos = initPos + new Vector3(0,chaseYOffset,0);
        }
        else
        {
            offsetPos = initPos;
        }

        transform.localPosition = offsetPos + shiftOffset;
        if (rollReticle)
        {
            Vector3 rollOffset = new Vector3(0, 0, zDrift * clampedDrift.z);
            rollReticle.transform.localRotation = Quaternion.Euler(rollOffset);
        }

    }
}


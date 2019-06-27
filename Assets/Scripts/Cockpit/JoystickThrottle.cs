using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class JoystickThrottle : MonoBehaviour
{
    ShipSettings shipMain;
    public Sprite[] joystickSprites;
    public Sprite[] firingJoystickSprites;
    public Sprite[] throttleSprites;
    public Sprite[] feetSprites;
    public SpriteRenderer Joystick;
    public SpriteRenderer Throttle;
    public SpriteRenderer Feet;
    Vector2 RefShift;
    float refSpin;
    [HideInInspector] public Vector2 SmoothShift;
    [HideInInspector] public float SmoothSpin;
    public Vector2 TargetShift;
    public float ShiftSmoothness = .4f;

    float smoothThrottle;
    float smoothBurn;
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
    }
    void DoThrottle(){
        smoothThrottle = Mathf.SmoothStep(smoothThrottle,shipMain.targetSpeed/shipMain.topSpeed,0.2f);
        
        if(!shipMain.isAfterburning)
        {
            smoothBurn = Mathf.SmoothStep(smoothBurn,smoothThrottle*.75f,0.3f);
        }
        else 
        {
            smoothBurn = Mathf.SmoothStep(smoothBurn,1,0.3f);
        }
        smoothBurn = Mathf.Clamp01(smoothBurn);
        Throttle.sprite = throttleSprites[Mathf.FloorToInt(smoothBurn*(throttleSprites.Length-1))];
        //throttlestickAnim.SetCurrentFrame((int)((currentForwardAcceleration / forwardTopSpeed) * throttlestickAnim.totalCells));
    }
    float refSteerX;
    float refSteerY;
    float refSteerZ;
    void DoJoystick()
    {
        
        float xShift =  shipMain.deltaRot.y;
        float yShift =  shipMain.deltaRot.x;
        
        //print (xShift);
        TargetShift = new Vector2(Mathf.Clamp(xShift,-1f,1f),-Mathf.Clamp(yShift,-1f,1f));
        SmoothShift = Vector2.SmoothDamp(SmoothShift,TargetShift,ref RefShift, ShiftSmoothness);

        refSteerX = (SmoothShift.x+1)/2;
        refSteerY = (SmoothShift.y+1)/2;

        float steerX = 1-Mathf.Clamp01(refSteerX);
        float steerY = 1-Mathf.Clamp01(refSteerY);        

        int currentjoyFrame = (int)(steerY * 7f) * 8 + (int)(steerX*8f);
        //print("current joystick frame is" + currentjoyFrame);
        //Debug.Assert(currentjoyFrame > 63," OUT OF RANGE JOYSTICK");
        if(shipMain.isFiring)
        {
            Joystick.sprite = firingJoystickSprites[currentjoyFrame];
        }
        else
        {
            Joystick.sprite = joystickSprites[currentjoyFrame];
        }

    }
    void DoFeet()
    {
        float zShift =  shipMain.deltaRot.z;
        float TargetSpin = Mathf.Clamp(zShift,-1f,1f);
        SmoothSpin = Mathf.SmoothDamp(SmoothSpin,TargetSpin,ref refSpin,ShiftSmoothness);
        refSteerZ = (SmoothSpin+1)/2;
        float steerZ = Mathf.Clamp01(refSteerZ);
        //Also do Feet Spinning/steer
        int currentFeetFrame = (int)(steerZ*16f);
        Feet.sprite = feetSprites[currentFeetFrame];
    }

    // Update is called once per frame
    void Update()
    {
        DoThrottle();
        DoJoystick();
        DoFeet();
    }
}

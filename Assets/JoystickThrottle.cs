using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class JoystickThrottle : MonoBehaviour
{
    public ShipSettings shipMain;
    public Sprite[] joystickSprites;
    public Sprite[] firingJoystickSprites;
    public Sprite[] throttleSprites;
    public SpriteRenderer Joystick;
    public SpriteRenderer Throttle;
    Vector2 RefShift;
    [HideInInspector] public Vector2 SmoothShift;
    public Vector2 TargetShift;
    public float ShiftSmoothness = .2f;

    float smoothThrottle;
    float smoothBurn;
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
    Vector2 expandedRefTurn;
    float refSteerX;
    float refSteerY;
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

    // Update is called once per frame
    void Update()
    {
        DoThrottle();
        DoJoystick();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DualJoystickThrottle : MonoBehaviour
{
    ShipSettings shipMain;
    public Sprite[] joystickSprites;
    public Sprite[] firingJoystickSprites;
    public Sprite[] feetSprites;
    public SpriteRenderer JoystickLeft;
    public SpriteRenderer JoystickRight;
    public SpriteRenderer Feet;
    Vector3 RefShift;
    float refSpin;
    [HideInInspector] public Vector3 SmoothShift;
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
    float refSteerX;
    float refSteerY;
    float refSteerZ;
    void DoJoysticks()
    {

        float xShift = shipMain.rotDelta.y;
        float yShift = shipMain.rotDelta.x;
        float zShift = shipMain.rotDelta.z;

        //print (xShift);
        TargetShift = new Vector3(Mathf.Clamp(xShift, -1f, 1f), -Mathf.Clamp(yShift, -1f, 1f), Mathf.Clamp(zShift, -1f, 1f));
        SmoothShift = Vector3.SmoothDamp(SmoothShift, TargetShift, ref RefShift, ShiftSmoothness);

        refSteerX = (SmoothShift.x + 1) / 2;
        refSteerY = (SmoothShift.y + 1) / 2;
        refSteerZ = Mathf.Clamp01((Mathf.Lerp(refSteerZ, zShift, .5f) + 1) / 2);



        float steerX =  1 - Mathf.Clamp01(refSteerX);
        float steerY =  1 - Mathf.Clamp01(refSteerY);
        float steerZ = refSteerZ - .5f;

        float leftSteerY = Mathf.Clamp01(steerY - steerZ);
        //print("Current Z Delta is " + zShift +" and current smoothed delta is " + refSteerZ);
        float rightSteerY = Mathf.Clamp01(steerY + steerZ);

        int currentLeftjoyFrame = (int)(leftSteerY * 7f) * 8 + (int)(steerX * 8f);
        int currentRightjoyFrame = (int)(rightSteerY * 7f) * 8 + (int)( (1-steerX) * 8f);
        //print("current joystick frame is" + currentjoyFrame);
        //Debug.Assert(currentjoyFrame > 63," OUT OF RANGE JOYSTICK");
        if (shipMain.isFiring)
        {
            JoystickLeft.sprite = firingJoystickSprites[Mathf.Clamp(currentLeftjoyFrame, 0, 63)];
            JoystickRight.sprite = firingJoystickSprites[Mathf.Clamp(currentRightjoyFrame, 0, 63)];
        }
        else
        {
            JoystickLeft.sprite = joystickSprites[Mathf.Clamp(currentLeftjoyFrame, 0, 63)];
            JoystickRight.sprite = joystickSprites[Mathf.Clamp(currentRightjoyFrame, 0, 63)];
        }
    }


    void DoFeet()
    {
        smoothThrottle = Mathf.SmoothStep(smoothThrottle, shipMain.targetSpeed / shipMain.topSpeed, 0.2f);

        if (!shipMain.isAfterburning)
        {
            smoothBurn = Mathf.Lerp(smoothBurn, smoothThrottle * .75f, 0.3f);
        }
        else
        {
            smoothBurn = Mathf.Lerp(smoothBurn, 1, 0.1f);
        }
        smoothBurn = Mathf.Clamp01(smoothBurn);

        //Also do Feet Throttle
        int currentFeetFrame = Mathf.FloorToInt(smoothBurn * (feetSprites.Length - 1));
        Feet.sprite = feetSprites[Mathf.Clamp(currentFeetFrame, 0, 15)];
        if (shipMain.isAfterburning)
        {
            Feet.sprite = feetSprites[15];
        }
    }

    // Update is called once per frame
    void Update()
    {

        DoJoysticks();
        DoFeet();
    }
}

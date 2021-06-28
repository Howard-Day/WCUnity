using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class BracketController : MonoBehaviour
{
    public HUDRoot HUDRoot;
    public ShipSettings ship;
    public ShipSettings shipMain;
    public Camera hudCamera;
    public Color Color;
    public Vector2 clipDist;
    public float clipAngle;
    private int pixelsPerUnit = 100;
    Image bracketSprite;
    Vector3 newLocalPosition;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    /// 
    int blink = 0;
    int locked = 0;
    Rect targetRect;
    RectTransform bracketRect;
    float angleTo;
    float distTo;
    Vector2 screenRes;

    void Start()
    {
        bracketSprite = (Image)gameObject.AddComponent<Image>();
        bracketSprite.material = HUDRoot.bracketMat;
        //transform.localScale = Vector3.one*.00075f;

       // mainCamera = (Camera)GameObject.FindGameObjectWithTag("MainCamera").GetComponent("Camera");
        bracketRect = gameObject.GetComponent<RectTransform>();
        bracketRect.anchorMin = Vector2.zero;
        bracketRect.anchorMax = Vector2.one;
        bracketRect.anchoredPosition = Vector2.one/2;
        screenRes.y = Screen.height;
        screenRes.x = Screen.width;
    }

    public static Vector3 GetScreenPosition(Camera mainCamera, Vector3 targetPosition, float depth, Vector2 screenRes)
    {
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetPosition);
        screenPosition.x -= screenRes.x / 2;
        screenPosition.x /= 100;
        screenPosition.y -= screenRes.y / 2;
        screenPosition.y /= 100;


        screenPosition.z = depth;
        return screenPosition;
    }


    // Update is called once per frame
    void Update()
    {
        if(!ship)
        {
            GameObjTracker.RegisterAllShips();
            GameObjTracker.RegisterTeams();
            //Destroy(this.gameObject);
            return;
        }
        //use default bracket color and sprite   
        bracketSprite.color = Color;
        bracketSprite.sprite = HUDRoot.defaultBracket;
        if (shipMain.AITeam == ShipSettings.TEAM.CONFED)
        {
            bracketSprite.type = Image.Type.Sliced;
        }
        if (shipMain.AITeam == ShipSettings.TEAM.KILRATHI)
        {
            bracketSprite.type = Image.Type.Tiled;
            
        }
        bracketSprite.pixelsPerUnitMultiplier = 50;
        angleTo = Vector3.Angle(hudCamera.transform.forward, ship.transform.position - hudCamera.transform.position);
        distTo = Vector3.Distance(hudCamera.transform.position, ship.transform.position);

        //unless the target is the current target!
        if (ship == shipMain.currentTarget)
        {
            bracketSprite.sprite = HUDRoot.targetBracket;
            if (GameObjTracker.frames % 5 == 0)
            {
                if(blink == 0)
                {   blink = 1;
                    bracketSprite.color = Color;
                    bracketSprite.enabled = true;
                }
                else
                {
                    blink = 0;
                    bracketSprite.enabled = false;
                }
                if(shipMain.currentLocked)
                {
                        locked = 1;
                }
                else
                {
                    locked = 0;
                }
            }

            if (locked > 0 )
            {
                bracketSprite.sprite = HUDRoot.lockedBracket;
                bracketSprite.color = Color;
                bracketSprite.enabled = true;
            }
            
        }

        if (distTo > clipDist.y*.75f && ship != shipMain.currentTarget)
        {
            bracketSprite.enabled = false;
        }
        if (distTo > clipDist.y)
        {
            bracketSprite.enabled = false;
        }
        if (distTo < clipDist.x)
        {
            bracketSprite.enabled = false;
        }
        if (angleTo >= clipAngle)
        {
            bracketSprite.enabled = false;
        }

        bracketSprite.transform.localEulerAngles = Vector3.zero;
        var collider = ship.GetComponent<Collider>();
        Vector3 cen = collider.bounds.center;
        Vector3 ext = collider.bounds.extents;
        Vector2[] extentPoints = new Vector2[8]
        {
         GetScreenPosition(hudCamera, new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z-ext.z),1.8f,screenRes),
         GetScreenPosition(hudCamera, new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z-ext.z),1.8f,screenRes),
         GetScreenPosition(hudCamera, new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z+ext.z),1.8f,screenRes),
         GetScreenPosition(hudCamera, new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z+ext.z),1.8f,screenRes),
         GetScreenPosition(hudCamera, new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z-ext.z),1.8f,screenRes),
         GetScreenPosition(hudCamera, new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z-ext.z),1.8f,screenRes),
         GetScreenPosition(hudCamera, new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z+ext.z),1.8f,screenRes),
         GetScreenPosition(hudCamera, new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z+ext.z),1.8f,screenRes)
        };
        Vector2 min = extentPoints[0];
        Vector2 max = extentPoints[0];
        foreach (Vector2 v in extentPoints)
        {
            min = new Vector2(Mathf.Min(min.x, v.x), Mathf.Min(min.y, v.y));
            max = new Vector2(Mathf.Max(max.x, v.x), Mathf.Max(max.y, v.y));
        }
        Vector2 posSize = new Vector2((max.x-min.x), (max.y-min.y));        
        RectTransform rectTrans = gameObject.transform as RectTransform;
        rectTrans.localPosition = GetScreenPosition(hudCamera,ship.gameObject.transform.position, 1.8f,new Vector2(640,400) );
        rectTrans.sizeDelta = Vector2.Min(Vector2.Max(Vector2.one*.085f, posSize*.75f), Vector2.one);    

    }
}

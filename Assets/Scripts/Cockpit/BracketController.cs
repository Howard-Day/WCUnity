using OneManEscapePlan.SpaceRailShooter.Scripts.Effects;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class BracketController : MonoBehaviour
{
    public HUDRoot HUDRoot;
    public ShipSettings ship;
    public ShipSettings shipMain;
    public PixelCamera pixelCamera;
    public Color Color;
    public Vector2 clipDist;
    public float clipAngle;
    
    Image bracketSprite;

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

    void Start()
    {
        bracketSprite = (Image)gameObject.AddComponent<Image>();
        bracketSprite.material = HUDRoot.bracketMat;
        bracketSprite.color = new Color(0, 0, 0, 1);
        //transform.localScale = Vector3.one*.00075f;

        // mainCamera = (Camera)GameObject.FindGameObjectWithTag("MainCamera").GetComponent("Camera");
        bracketRect = gameObject.GetComponent<RectTransform>();
        bracketRect.anchorMin = Vector2.zero;
        bracketRect.anchorMax = Vector2.zero;
        bracketRect.anchoredPosition = Vector2.one / 2;
        bracketSprite.color = Color;
        bracketRect.sizeDelta = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (ship == null || pixelCamera == null) return;

        if (shipMain.Team == TEAM.CONFED)
        {
            bracketSprite.type = Image.Type.Sliced;
        }
        if (shipMain.Team == TEAM.KILRATHI)
        {
            bracketSprite.type = Image.Type.Tiled;
        }
        bracketSprite.pixelsPerUnitMultiplier = 50;
        angleTo = Vector3.Angle(this.pixelCamera.transform.forward, ship.transform.position - this.pixelCamera.transform.position);
        distTo = Vector3.Distance(this.pixelCamera.transform.position, ship.transform.position);

        if (ship == shipMain.CurrentTarget)
        {
            bracketSprite.sprite = HUDRoot.targetBracket;
            if (GameObjTracker.Instance.CurrentFrame % 15 == 0)
            {
                if (blink == 0)
                {
                    blink = 1;
                    bracketSprite.color = Color;
                    bracketSprite.enabled = true;
                }
                else
                {
                    blink = 0;
                    bracketSprite.enabled = false;
                }
                if (shipMain.currentLocked)
                {
                    locked = 1;
                }
                else
                {
                    locked = 0;
                }
            }

            if (locked > 0)
            {
                bracketSprite.sprite = HUDRoot.lockedBracket;
                bracketSprite.color = Color;
                bracketSprite.enabled = true;
            }
        } 
        else
        {
            //use default bracket color and sprite   
            bracketSprite.color = Color;
            bracketSprite.sprite = HUDRoot.defaultBracket;
        }

        //handle hiding brackets when a ship is cloaking and cloaked! 
        if (ship.isCloaking)
        {
            bracketSprite.sprite = HUDRoot.targetBracket;
            if (GameObjTracker.Instance.CurrentFrame % 15 == 0)
            {
                if (blink == 0)
                {
                    blink = 1;
                    bracketSprite.color = Color;
                    bracketSprite.enabled = true;
                }
                else
                {
                    blink = 0;
                    bracketSprite.enabled = false;
                }
            }
        }
        if (ship.IsCloaked)
        {
            bracketSprite.sprite = HUDRoot.targetBracket;
            bracketSprite.color = Color;
            bracketSprite.enabled = false;
        }


        if (distTo > clipDist.y * .75f && ship != shipMain.CurrentTarget)
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
        var screenRes = new Vector2(Screen.width, Screen.height);
        Vector2[] extentPoints = new Vector2[8]
        {
            GetScreenPosition(pixelCamera, new Vector3(cen.x- ext.x, cen.y- ext.y, cen.z- ext.z), 1.8f, screenRes),
            GetScreenPosition(pixelCamera, new Vector3(cen.x+ ext.x, cen.y- ext.y, cen.z- ext.z), 1.8f, screenRes),
            GetScreenPosition(pixelCamera, new Vector3(cen.x- ext.x, cen.y- ext.y, cen.z+ ext.z), 1.8f, screenRes),
            GetScreenPosition(pixelCamera, new Vector3(cen.x+ ext.x, cen.y- ext.y, cen.z+ ext.z), 1.8f, screenRes),
            GetScreenPosition(pixelCamera, new Vector3(cen.x- ext.x, cen.y+ ext.y, cen.z- ext.z), 1.8f, screenRes),
            GetScreenPosition(pixelCamera, new Vector3(cen.x+ ext.x, cen.y+ ext.y, cen.z- ext.z), 1.8f, screenRes),
            GetScreenPosition(pixelCamera, new Vector3(cen.x- ext.x, cen.y+ ext.y, cen.z+ ext.z), 1.8f, screenRes),
            GetScreenPosition(pixelCamera, new Vector3(cen.x+ ext.x, cen.y+ ext.y, cen.z+ ext.z), 1.8f, screenRes)
        };

        Vector2 min = extentPoints[0];
        Vector2 max = extentPoints[0];
        foreach (Vector2 v in extentPoints)
        {
            min = new Vector2(Mathf.Min(min.x, v.x), Mathf.Min(min.y, v.y));
            max = new Vector2(Mathf.Max(max.x, v.x), Mathf.Max(max.y, v.y));

        }

        RectTransform rectTrans = gameObject.transform as RectTransform;
        var screenPos = GetScreenPosition(pixelCamera, ship.gameObject.transform.position, 1.8f, screenRes);
        var renderTextureRes = new Vector2(pixelCamera.RenderSettings.RenderTexture.width, pixelCamera.RenderSettings.RenderTexture.height);
        Vector2 scaleFactor = new Vector2(renderTextureRes.x / screenRes.x, renderTextureRes.y / screenRes.y);
        var finalScreenPos = screenPos;
        finalScreenPos.x *= scaleFactor.x;
        finalScreenPos.y *= scaleFactor.y;

        Vector2 posSize = new Vector2((max.x - min.x), (max.y - min.y));
        posSize.x *= scaleFactor.x;
        posSize.y *= scaleFactor.y;
        rectTrans.sizeDelta = Vector2.Min(Vector2.Max(Vector2.one * .085f, posSize * .75f), Vector2.one);

        rectTrans.localPosition = finalScreenPos;
    }

    public static Vector3 GetScreenPosition(PixelCamera mainCamera, Vector3 targetPosition, float depth, Vector2 screenRes)
    {
        Vector3 screenPosition = mainCamera.Camera.WorldToScreenPoint(targetPosition);
        screenPosition.x -= screenRes.x / 2;
        screenPosition.x /= 100;
        screenPosition.y -= screenRes.y / 2;
        screenPosition.y /= 100;

        screenPosition.z = depth;

        return screenPosition;
    }
}

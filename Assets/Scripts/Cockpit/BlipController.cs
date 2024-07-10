using UnityEngine;
using UnityEngine.UI;

public class BlipController : MonoBehaviour
{
    public Radar radarRoot;
    public ShipSettings ship;
    public ShipSettings shipMain;
    public Color Near;
    public Color Far;
    public Vector2 clipDist;
    private int pixelsPerUnit = 100;
    Image blipSprite;
    Vector3 newLocalPosition;
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        blipSprite = (Image)gameObject.AddComponent<Image>();
        blipSprite.material = radarRoot.blipMat;
        transform.localScale = Vector3.one * .00075f;
        blipSprite.color = new Color(0, 0, 0, 1);
    }
    int blink = 0;
    int locked = 0;

    // Update is called once per frame
    void Update()
    {
        if (!ship || ship.isDead)
        {
            GameObjTracker.RegisterAllShips();
            GameObjTracker.RegisterTeams();       
        }
        if (ship)
        {
            Vector3 blipLoc = ship.transform.position;
            Vector3 blipAngle = blipLoc - shipMain.transform.position;
            float blipDist = blipAngle.magnitude;
            blipAngle.Normalize();
            float x = Vector3.Dot(blipAngle, shipMain.transform.right);
            float y = Vector3.Dot(blipAngle, shipMain.transform.up);
            float z = Vector3.Dot(blipAngle, shipMain.transform.forward);
            //x = //Mathf.Clamp01(x);
            //y = //Mathf.Clamp01(y);

            float normalizedDist = Mathf.Clamp01((clipDist.x + blipDist) / clipDist.y);
            blipSprite.color = Color.Lerp(Near, Far, normalizedDist);
            //blipSprite.sprite = radarRoot.fighterBlips[0];
            if (ship.shipRadius >= 40)//Big contact! 
            { //use the last 3 sprites as normalized distance falloffs
                blipSprite.sprite = radarRoot.capitalBlips[Mathf.CeilToInt(normalizedDist * 3)];
            }
            else//Fighter contact! 
            { //use the last 3 sprites as normalized distance falloffs
                blipSprite.sprite = radarRoot.fighterBlips[Mathf.CeilToInt(normalizedDist * 3)];
                //unless the target is the current target!
                if (ship == shipMain.currentTarget)
                {
                    if (GameObjTracker.frames % 10 == 0)
                    {
                        if (blink == 0)
                        {
                            blink = 1;
                            blipSprite.color = Near;
                            blipSprite.enabled = true;
                        }
                        else
                        {
                            blink = 0;
                            blipSprite.enabled = false;
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

                    blipSprite.sprite = radarRoot.fighterBlips[Mathf.CeilToInt(normalizedDist * 3)];
                    if (locked > 0)
                    {
                        blipSprite.sprite = radarRoot.fighterBlips[0];
                    }
                }
            }
            //handle if the ship is cloaked
            //handle flashing hiding brackets when a ship is cloaking and cloaked! 
            if (ship.isCloaking)
            {
                
                if (GameObjTracker.frames % 5 == 0)
                {
                    if (blink == 0)
                    {
                        blink = 1;
                        blipSprite.color = Near;
                        blipSprite.enabled = true;
                    }
                    else
                    {
                        blink = 0;
                        blipSprite.enabled = false;
                    }
                }
            }
            if (ship.isCloaked && !ship.isCloaking)
            {
                blipSprite.color = Near;
                blipSprite.enabled = false;
            }
            

            blipSprite.transform.localEulerAngles = Vector3.zero;

            Vector3 losePos = new Vector3(x * radarRoot.radarMapXScale, y * radarRoot.radarMapYScale, -.0005f);
            if (z < 0)
            {
                losePos = Vector3.Scale(new Vector3(x, y, 0).normalized, new Vector3(radarRoot.radarMapXScale, radarRoot.radarMapYScale, 1f));
            }
            newLocalPosition.x = (Mathf.Round(losePos.x * pixelsPerUnit) / pixelsPerUnit);
            newLocalPosition.y = (Mathf.Round(losePos.y * pixelsPerUnit) / pixelsPerUnit);
            newLocalPosition.z = losePos.z;


            blipSprite.transform.localPosition = newLocalPosition;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TargetComsNav : MonoBehaviour
{
    [Header("System Roots")]
    public GameObject TargetDispRoot;
    public GameObject NavDispRoot;
    public GameObject CommsDispRoot;
    public enum VDUMode { Target, Comms, Nav };
    public VDUMode Mode;
    [Header("Target Display Settings")]
    public Text TargetText;
    public Color TextColor;
    public Color TextColorLocked;
    public Text TargetName;
    public Text TargetDist;
    public bool inBase8 = false;
    public Sprite GenericVDU;
    public GameObject TargetBase;
    public GameObject TargetDamaged;
    public GameObject DamagedLeft;
    public GameObject DamagedRight;
    public GameObject DamagedFront;
    public GameObject DamagedBack;
    public GameObject ShieldFront;
    public GameObject ShieldBack;
    public Sprite[] shieldSprites;
    public Sprite shieldNull;
    public GameObject subtext;
    public GameObject subtextNull;
    public GameObject subtextDamaged;


    ShipSettings shipMain;

    ShipSettings currentTarget = null;

    Color textColor;
    GameObject mfdSubtext;
    Vector3 offset = Vector3.back * .101f;
    bool subtextDamgedMode = false;
    bool subtextNullMode = false;
    bool subtextSwitchNeeded = false;
    // Start is called before the first frame update
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        DamagedBack.SetActive(false);
        DamagedRight.SetActive(false);
        DamagedLeft.SetActive(false);
        DamagedFront.SetActive(false);
        textColor = TargetText.color;
    }
    public static string Int32ToString(int value, int toBase)
    {
        string result = string.Empty;
        do
        {
            result = "0123456789ABCDEF"[value % toBase] + result;
            value /= toBase;
        }
        while (value > 0);

        return result;
    }

    void DoTarget()
    {
        if (shipMain.currentTarget == null)
        {
            TargetName.text = "NO TARGET";
            TargetName.color = TextColor;
            TargetDist.text = "Range: NONE";
            TargetDist.color = TextColor;
            TargetBase.GetComponent<SpriteRenderer>().sprite = GenericVDU;
            TargetDamaged.GetComponent<SpriteRenderer>().sprite = GenericVDU;
            DamagedBack.SetActive(false);
            DamagedRight.SetActive(false);
            DamagedLeft.SetActive(false);
            DamagedFront.SetActive(false);
            ShieldFront.GetComponent<SpriteRenderer>().sprite = shieldNull;
            ShieldBack.GetComponent<SpriteRenderer>().sprite = shieldNull;
            //remove any previous MFD subtexts
            if (mfdSubtext && !subtextNullMode)
            {
                Destroy(mfdSubtext);
                subtextSwitchNeeded = true;
                subtextDamgedMode = false;
                
            }
            //check if there are no subtexts!
            if (!mfdSubtext && !subtextNullMode)
            {                
                subtextSwitchNeeded = true;
            }
            //Create a null subtext
            if (!mfdSubtext && !subtextNullMode && subtextSwitchNeeded)
            {
                mfdSubtext = Instantiate(subtextNull, TargetBase.transform.position + offset, TargetBase.transform.rotation, TargetBase.transform);
                subtextSwitchNeeded = true;
                subtextNullMode = true;
            }
            return;
        }

        if (shipMain.currentTarget != null && currentTarget != shipMain)
        {
            if (currentTarget == null) //No VDU target assigned
            {
                currentTarget = shipMain.currentTarget;

                TargetName.text = "Target: " + currentTarget.gameObject.name;
                if (currentTarget.VDUImage != null)
                {
                    TargetBase.GetComponent<SpriteRenderer>().sprite = currentTarget.VDUImage;
                    TargetDamaged.GetComponent<SpriteRenderer>().sprite = currentTarget.VDUImage;
                }
                else
                {
                    TargetBase.GetComponent<SpriteRenderer>().sprite = GenericVDU;
                    TargetDamaged.GetComponent<SpriteRenderer>().sprite = GenericVDU;
                }
                DamagedBack.SetActive(false);
                DamagedRight.SetActive(false);
                DamagedLeft.SetActive(false);
                DamagedFront.SetActive(false);
            }
            if (currentTarget != null) //We have a target! 
            {
                float tarDist = Vector3.Distance(shipMain.transform.position, currentTarget.transform.position);
                tarDist = Mathf.FloorToInt(tarDist * 10) / 10f;

                TargetName.text = "Target: " + currentTarget.DisplayName;

                if (!inBase8)
                {
                    TargetDist.text = "Range: " + tarDist * 2 + "m";
                }
                else 
                {
                    TargetDist.text = "Range: " + Int32ToString(Mathf.FloorToInt(tarDist) * 2,8) + "m";
                }

                if (currentTarget.Armor.x < currentTarget._ArmorMax.x / 2)
                {
                    DamagedFront.SetActive(true);
                }
                if (currentTarget.Armor.y < currentTarget._ArmorMax.y / 2)
                {
                    DamagedBack.SetActive(true);
                }
                if (currentTarget.Armor.z < currentTarget._ArmorMax.z / 2)
                {
                    DamagedLeft.SetActive(true);
                }
                if (currentTarget.Armor.w < currentTarget._ArmorMax.w / 2)
                {
                    DamagedRight.SetActive(true);
                }
                int SFront = Mathf.FloorToInt((currentTarget.Shield.x / currentTarget._ShieldMax.x) * 4) - 1;
                int SRear = Mathf.FloorToInt((currentTarget.Shield.y / currentTarget._ShieldMax.y) * 4) - 1;

                SFront = Mathf.Clamp(SFront,0,3);
                SRear = Mathf.Clamp(SRear, 0, 3);
                ///print(currentTarget.Shield.x/currentTarget._ShieldMax.x +" "+ currentTarget.Shield.y/currentTarget._ShieldMax.y );

                if (SFront == -1)
                {
                    ShieldFront.SetActive(false);

                }
                else
                {
                    ShieldFront.GetComponent<SpriteRenderer>().sprite = shieldSprites[SFront];
                    ShieldFront.SetActive(true);
                }
                if (SRear == -1)
                {
                    ShieldBack.SetActive(false);

                }
                else
                {
                    ShieldBack.GetComponent<SpriteRenderer>().sprite = shieldSprites[SRear];
                    ShieldBack.SetActive(true);
                }
                if (currentTarget.isLocked)
                {
                    TargetText.text = "LOCKED TARGET";
                    TargetText.color = TextColorLocked;
                }
                else
                {
                    TargetText.text = "AUTO TARGETING";
                    TargetText.color = TextColor;
                }
                if (tarDist > shipMain.GetComponentInChildren<ProjectileWeapon>().gunRange)
                {
                    TargetDist.color = TextColorLocked;
                }
                else
                {
                    TargetDist.color = TextColor;
                }
                //handle little animated MFD target texts! 
                //check for null subtext existence
                if (mfdSubtext && subtextNullMode)
                {
                    subtextNullMode = false;
                    Destroy(mfdSubtext);
                }
                //Set update flag
                if (!mfdSubtext)
                {
                    subtextSwitchNeeded = true;
                }

                if (!subtextDamgedMode && subtextSwitchNeeded)
                {
                    mfdSubtext = Instantiate(subtext, TargetBase.transform.position + offset, TargetBase.transform.rotation, TargetBase.transform);
                    subtextSwitchNeeded = false;
                }
                if (subtextDamgedMode && subtextSwitchNeeded)
                {
                    mfdSubtext = Instantiate(subtextDamaged, TargetBase.transform.position + offset, TargetBase.transform.rotation, TargetBase.transform);
                    subtextSwitchNeeded = false;
                }
                
                
                //Engage ship systems damage mode! Remove old subtext...
                if (currentTarget._CoreStrength < currentTarget.CoreMax && !subtextDamgedMode)
                {
                    subtextDamgedMode = true;
                    Destroy(mfdSubtext);
                }
                //Back to standard mode! Remove old subtext...
                if (currentTarget._CoreStrength >= currentTarget.CoreMax && subtextDamgedMode)
                {
                    subtextDamgedMode = false;
                    Destroy(mfdSubtext);
                }
            }
        }
        //our target cloaked!
        if (currentTarget && currentTarget.isCloaked)
        {
            currentTarget = null;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        switch (Mode)
        {
            case (VDUMode.Target):
                {
                    DoTarget();
                }
                break;


        }
    }
}

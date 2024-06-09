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

    ShipSettings shipMain;

    ShipSettings currentTarget = null;

    Color textColor;

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
    void DoTarget()
    {
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
                TargetName.text = "Target: " + currentTarget.gameObject.name;
                TargetDist.text = "Range: " + tarDist + "m";

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

            }
        }
        else
        {

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

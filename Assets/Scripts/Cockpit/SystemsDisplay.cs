using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
public class SystemsDisplay : MonoBehaviour
{
    public enum MFDMode { Weapon, Damge };

    public MFDMode currentMode = MFDMode.Weapon;

    public Text mode;
    public Text currentWeapon;
    public Text currentGun;
    public Text damage;

    public GameObject shipBase;

    public Material ActiveWeapon;
    public Material Weapon;
    public Material DamagedComponent;

    [System.Serializable]
    public class WeaponIcons
    {
        public GameObject Laser;
        public GameObject Neutron;
        public GameObject MassDriver;
        public GameObject DF;
        public GameObject HS;
        public GameObject IR;
        public GameObject FF;
        public GameObject Mine;
    }
    [System.Serializable]
    public class DamageIcons
    {
        public GameObject IonDrive;
        public GameObject PowerPlant;
        public GameObject ShieldGen;
        public GameObject CompSys;
        public GameObject ComUnit;
        public GameObject Track;
        public GameObject AccelAbs;
        public GameObject EjectSys;
        public GameObject RepairSys;
        public GameObject Jets;
    }

    public WeaponIcons WIcon;
    public DamageIcons DIcon;


    ShipSettings shipMain;
    ProjectileWeapon[] guns;
    List<GameObject> gunIcons;

    // Start is called before the first frame update
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        guns = shipMain.gameObject.GetComponentsInChildren<ProjectileWeapon>();
        gunIcons = new List<GameObject>();

    }
    bool wRegSuccess = false;

    public void RegisterWeapons()
    {
        if (gunIcons.Count > 0)
        {
            foreach (GameObject icon in gunIcons)
            {
                Destroy(icon);
            }
        }
        gunIcons = new List<GameObject>();
        foreach (ProjectileWeapon gun in guns)
        {
            Vector3 localPos = gun.gameObject.transform.localPosition / shipMain.shipRadius * 1.5f;
            if (localPos.magnitude >= Mathf.Infinity)
            {
                wRegSuccess = false;
                return;
            }
            bool xFlip = false;
            localPos.y = localPos.z;
            if (localPos.x > 0)
            {
                xFlip = true;
            }
            localPos.z = 0f;
            localPos *= .15f;
            localPos += shipBase.transform.position;
            //print(localPos);
            if (gun.Type == ProjectileWeapon.GunType.Laser)
            {
                GameObject gunIcon = (GameObject)Instantiate(WIcon.Laser, localPos, Quaternion.identity, shipBase.transform);
                gunIcon.GetComponent<SpriteRenderer>().flipX = xFlip;
                gunIcon.GetComponent<SpriteRenderer>().material = ActiveWeapon;
                gunIcons.Add(gunIcon);
            }
        }
        if (gunIcons.Count > 0)
        {
            wRegSuccess = true;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        switch (currentMode)
        {
            case MFDMode.Weapon:
                {
                    if (gunIcons.Count == 0 || !wRegSuccess)
                    {
                        RegisterWeapons();
                    }

                }
                break;



        }

    }
}

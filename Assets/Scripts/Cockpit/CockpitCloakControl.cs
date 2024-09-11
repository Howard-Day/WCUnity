using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockpitCloakControl : MonoBehaviour
{

    public GameObject CockpitArt;
    public GameObject CockpitDamageArt;
    public GameObject[] CockpitContorlArt;
    Material Scenebuffer;
    ShipSettings shipMain;
    CockpitViewSwitcher Cam;
    float smoothedCloakAmount = 0f;
    // Start is called before the first frame update
    void Start()
    {
        shipMain = gameObject.GetComponent<CockpitViewSwitcher>().shipMain;
        GameObject[] Scenebuffers = GameObject.FindGameObjectsWithTag("SceneBuffer");
        foreach (GameObject bufferobj in Scenebuffers)
        {
            if (bufferobj.GetComponent<MeshRenderer>() != null)
            {
                Scenebuffer = bufferobj.GetComponent<MeshRenderer>().material;
            }
        }
        Cam = gameObject.GetComponentInParent<CockpitViewSwitcher>();

    }

        // Update is called once per frame
    void Update()
    {
        if (!shipMain)
        {
            shipMain = gameObject.GetComponent<CockpitViewSwitcher>().shipMain;
        }

        smoothedCloakAmount = Mathf.Lerp(smoothedCloakAmount, shipMain.cloakedAmount, .125f) *1.05f;

        float clampedCloakAmount = Mathf.Clamp01(smoothedCloakAmount);

        CockpitArt.GetComponent<SpriteRenderer>().material.SetFloat("_CloakBlend", clampedCloakAmount);
        CockpitDamageArt.GetComponent<SpriteRenderer>().material.SetFloat("_CloakBlend", clampedCloakAmount);

        foreach(GameObject control in CockpitContorlArt)
        {
            control.GetComponent<SpriteRenderer>().material.SetFloat("_CloakBlend", clampedCloakAmount);
        }

        float steppedCloakAmount = Mathf.FloorToInt(clampedCloakAmount * 8f) / 8f;

        Scenebuffer.SetFloat("_AltBlend", steppedCloakAmount);
        Scenebuffer.SetFloat("_LUTBlend", Mathf.Lerp(.5f,1f, steppedCloakAmount));

        //Overwrite for external views! 
        if (Cam.activeView == CockpitViewSwitcher.View.Chase)
        {
            CockpitArt.GetComponent<SpriteRenderer>().material.SetFloat("_CloakBlend", 0);
            CockpitDamageArt.GetComponent<SpriteRenderer>().material.SetFloat("_CloakBlend", 0);
            Scenebuffer.SetFloat("_AltBlend", 0);
            Scenebuffer.SetFloat("_LUTBlend", 0);
        }

    }
}

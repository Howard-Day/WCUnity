using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockpitDamage : MonoBehaviour
{
    public GameObject DamageVFX;
    public GameObject ShatterVFX;
    public GameObject[] SmallMasks;
    public GameObject[] LargeMasks;
    public float ZoneAreaSize;
    public Transform FrontDamageZone;
    public Transform BackDamageZone;
    public Transform RightDamageZone;
    public Transform LeftDamageZone;
    public Transform UpDamageZone;
    public Transform DownDamageZone;

    public Transform VDUDamageZone1;
    public Transform VDUDamageZone2;
    public Transform RadarDamageZone;
    public Transform ShieldDamageZone;
    
    ShipSettings shipMain;

    Vector3 flattenVec = new Vector3(1,.5f,0);
    // Start is called before the first frame update
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
    }

    void SpawnDamage(Transform Zone, GameObject Spawn, Vector2 randCutoff, float randZone, GameObject VFX)
    {
        GameObject Damage = (GameObject)Instantiate(Spawn,
        Zone.position+
        Vector3.Scale(Random.insideUnitSphere,flattenVec*randZone),
        transform.rotation,transform);            
        Damage.transform.localEulerAngles = Vector3.zero; 
        Damage.transform.localEulerAngles = new Vector3(0,0,Random.Range(0,4)*90);
        Damage.GetComponent<SpriteMask>().alphaCutoff = Random.Range(randCutoff.x,randCutoff.y); 

        Instantiate(VFX,Damage.transform.position,Quaternion.identity, transform);

    }
    void DoDamage()
    {   
        
        if(shipMain.lastHit == ShipSettings.HitLoc.F)
        {
            SpawnDamage(FrontDamageZone,SmallMasks[Random.Range(0,SmallMasks.Length-1)],new Vector2(.3f,.995f), ZoneAreaSize*2,ShatterVFX);
            SpawnDamage(FrontDamageZone,SmallMasks[Random.Range(0,SmallMasks.Length-1)],new Vector2(.3f,.995f), ZoneAreaSize*2,ShatterVFX);
        }
        if(shipMain.lastHit == ShipSettings.HitLoc.B)
        {
            SpawnDamage(BackDamageZone,LargeMasks[Random.Range(0,LargeMasks.Length-1)],new Vector2(.2f,.95f), ZoneAreaSize,ShatterVFX);
        }
        if(shipMain.lastHit == ShipSettings.HitLoc.R)
        {
            SpawnDamage(RightDamageZone,LargeMasks[Random.Range(0,LargeMasks.Length-1)],new Vector2(.2f,.95f), ZoneAreaSize,ShatterVFX);
        }
        if(shipMain.lastHit == ShipSettings.HitLoc.L)
        {
            SpawnDamage(LeftDamageZone,LargeMasks[Random.Range(0,LargeMasks.Length-1)],new Vector2(.2f,.95f), ZoneAreaSize,ShatterVFX);
        }
        if(shipMain.lastHit == ShipSettings.HitLoc.U)
        {
            SpawnDamage(UpDamageZone,SmallMasks[Random.Range(0,SmallMasks.Length-1)],new Vector2(.2f,.95f), ZoneAreaSize,ShatterVFX);
        }      
        if(shipMain.lastHit == ShipSettings.HitLoc.D)
        {
            SpawnDamage(DownDamageZone,SmallMasks[Random.Range(0,SmallMasks.Length-1)],new Vector2(.2f,.95f), ZoneAreaSize,ShatterVFX);
        }          
        if(shipMain.componentDamage.Track >= .25f)
        {
            SpawnDamage(RadarDamageZone,SmallMasks[Random.Range(0,SmallMasks.Length-1)],new Vector2(.2f,.95f), 0f,DamageVFX);
        }                     
        if(shipMain.componentDamage.ShieldGen >= .5f)
        {
            SpawnDamage(ShieldDamageZone,SmallMasks[Random.Range(0,SmallMasks.Length-1)],new Vector2(.2f,.95f), 0f,DamageVFX);
        }     

        if(shipMain.componentDamage.CompSys >= .5f)
        {
            SpawnDamage(VDUDamageZone2,LargeMasks[Random.Range(0,LargeMasks.Length-1)],new Vector2(.2f,.95f), 0f,DamageVFX);
        }     

        if(shipMain.componentDamage.Track >= .5f)
        {
            SpawnDamage(VDUDamageZone1,LargeMasks[Random.Range(0,LargeMasks.Length-1)],new Vector2(.2f,.95f), 0f,DamageVFX);
        }   
        shipMain.hitInternal = false;  
    }
    // Update is called once per frame
    void Update()
    {
        if(shipMain.hitInternal == true)
        {
            //print("COCKPIT DAMAGE DETECTED In Zone " + shipMain.lastHit);
            DoDamage();
        }
    }
}

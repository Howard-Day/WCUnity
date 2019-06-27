using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
public class KPSSpeed : MonoBehaviour
{
    public Text Speed;
    ShipSettings shipMain;
    // Start is called before the first frame update
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
    }

    // Update is called once per frame
    void Update()
    {
       float setSpeed = shipMain.speed;
       
         
       int speedDisp = Mathf.FloorToInt(setSpeed*10);

       Speed.text = speedDisp.ToString(); 
    }
}

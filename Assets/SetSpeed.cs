using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
public class SetSpeed : MonoBehaviour
{
    public Text Speed;
    ShipSettings mainShip;
    // Start is called before the first frame update
    void Start()
    {
        mainShip = GameObject.FindGameObjectWithTag("PlayerShip").GetComponent<ShipSettings>();
    }

    // Update is called once per frame
    void Update()
    {
       float setSpeed = mainShip.targetSpeed;
       if (setSpeed >= mainShip.topSpeed)
        setSpeed = mainShip.topSpeed;
       if(mainShip.isAfterburning)
        setSpeed = mainShip.burnSpeed;
         
       int speedDisp = Mathf.FloorToInt(setSpeed*10);

       Speed.text = speedDisp.ToString(); 
    }
}

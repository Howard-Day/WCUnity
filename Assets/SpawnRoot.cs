using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnRoot : MonoBehaviour
{
    public GameObject SpawnParent;
    public bool isPlayer;
    public AIPlayer.AILevel aiLevel;
    // Start is called before the first frame update
    void Start()
    {
        GameObject root = Instantiate(SpawnParent,transform.position, transform.rotation);
        transform.parent = root.transform;
        root.name = SpawnParent.name;
        root.GetComponent<AIPlayer>().AISkillLevel = aiLevel;
        if(isPlayer)
        {
            root.GetComponent<AIPlayer>().enabled = false;
            root.GetComponent<KeyPlayer>().enabled = false;
        }
       
    }
}

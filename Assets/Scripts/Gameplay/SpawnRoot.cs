using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnRoot : MonoBehaviour
{
    public GameObject SpawnParent;
    public bool isPlayer;
    public AIShipSkillSettings aiSkillSettings;
    public MessageHandler messageOverride;
    // Start is called before the first frame update
    void Start()
    {
        GameObject root = Instantiate(SpawnParent,transform.position, transform.rotation);
        transform.parent = root.transform;
        root.name = SpawnParent.name;

        var aiPlayer = root.GetComponent<AIPlayer>();
        var shipSettings = root.GetComponent<ShipSettings>();

        aiPlayer.SkillSettings = aiSkillSettings;
        shipSettings.isPlayer = true;
        shipSettings.playerUI = this.gameObject;
        if (isPlayer)
        {
            aiPlayer.enabled = false;
            root.GetComponent<KeyPlayer>().enabled = false;
        }
       
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMainTex : MonoBehaviour
{
    public Texture2D[] RandomTextures;
    bool randomSwitchDone = false;
    // Start is called before the first frame update
    void Update()
    {
        MeshRenderer r = gameObject.GetComponent<MeshRenderer>();
        if (r != null && !randomSwitchDone)
        {         
            r.material.mainTexture = RandomTextures[Random.Range(0, RandomTextures.Length - 1)];
            randomSwitchDone = true;
        }

        ParticleSystemRenderer p = gameObject.GetComponent<ParticleSystemRenderer>();
        if (p != null && !randomSwitchDone)
        {
            p.material.mainTexture = RandomTextures[Random.Range(0, RandomTextures.Length - 1)];
            randomSwitchDone = true;
        }
        if (gameObject.GetComponent<ParticleSystem>().time <= .01f)
        {
            randomSwitchDone = false;
        }
    }
}

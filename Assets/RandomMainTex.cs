using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMainTex : MonoBehaviour
{
    public Texture2D[] RandomTextures; 
    // Start is called before the first frame update
    void Start()
    {
        MeshRenderer r = gameObject.GetComponent<MeshRenderer>();
        if (r != null)
        {         
            r.material.mainTexture = RandomTextures[Random.Range(0, RandomTextures.Length - 1)];
        }

        ParticleSystemRenderer p = gameObject.GetComponent<ParticleSystemRenderer>();
        if (p != null)
        {
            p.material.mainTexture = RandomTextures[Random.Range(0, RandomTextures.Length - 1)];
        }
    }


}

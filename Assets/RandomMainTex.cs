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
        if (r == null)
        {
            ParticleSystemRenderer p = gameObject.GetComponent<ParticleSystemRenderer>();
            p.material.mainTexture = RandomTextures[Random.Range(0, RandomTextures.Length - 1)];
        }
        else
        {
            r.material.mainTexture = RandomTextures[Random.Range(0, RandomTextures.Length - 1)];
        }
    }


}

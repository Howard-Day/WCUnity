using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAudioSelector : MonoBehaviour
{
    public AudioClip[] RandomAudio;
    public float audioRange = 50f;
    public float audioVolume = 1f;
    public Vector2 audioPitchMinMax = new Vector2(.8f,1f);
    public float dopplerLevel = 1f;
    [HideInInspector] public AudioSource SFX;

    // Start is called before the first frame update
    void Start()
    {
        SFX = gameObject.AddComponent<AudioSource>();
        if (RandomAudio.Length > 0)
        {
            int randomClip = Random.Range(0, RandomAudio.Length-1);

            SFX.clip = RandomAudio[randomClip];

        }
        SFX.playOnAwake = false;
        SFX.volume = audioVolume;
        SFX.pitch = Random.Range(audioPitchMinMax.x,audioPitchMinMax.y);
        SFX.spatialBlend = 1f;
        SFX.dopplerLevel = dopplerLevel;
        SFX.maxDistance = audioRange;
        SFX.minDistance = audioRange/10;
        SFX.rolloffMode = AudioRolloffMode.Linear;
        SFX.playOnAwake = true;
        SFX.Play();
    }

}

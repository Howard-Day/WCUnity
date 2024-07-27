using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicController : MonoBehaviour
{
    public float MusicVolume = 1f;
    public enum MissionStatus { LAUNCH, START, COMBAT, SUCCESS, FAIL, LANDING };
    public MissionStatus Status = MissionStatus.START;

    [HideInInspector] public AudioListener Listener;
    [HideInInspector] public GameObjTracker Tracker;


    public AudioClip MissionStart;
    public AudioClip MissionStrike;
    public AudioClip Combat;
    public AudioClip IntenseCombat;
    public AudioClip TailingEnemy;
    public AudioClip Tailed;
    public AudioClip WingmanHit;
    public AudioClip WingmanDeath;
    public AudioClip TargetDeath;

    public float combatDist;
    public float intenseDist;
    public int intenseCount;
    public float huntingAngle;
    public float huntedAngle;



    AudioSource Music1;
    AudioSource Music2;
    AudioSource NextMusicBlend;

    // Start is called before the first frame update
    void Start()
    {
        Listener = Camera.main.GetComponent<AudioListener>();
        Tracker = GameObject.FindGameObjectWithTag("GamePlayObjs").GetComponent<GameObjTracker>();
        Music1 = gameObject.AddComponent<AudioSource>();
        Music2 = gameObject.AddComponent<AudioSource>();
        Music1.Stop();
        Music2.Stop();
        Music1.spatialBlend = 0f;
        Music2.spatialBlend = 0f;
        Music1.rolloffMode = AudioRolloffMode.Linear;
        Music2.rolloffMode = AudioRolloffMode.Linear;
        Music1.bypassEffects = true;
        Music2.bypassEffects = true;
        Music1.volume = MusicVolume;
        Music2.volume = MusicVolume;
        NextMusicBlend = Music1;
    }
    public void FadeMusic(AudioClip musicIn, float fadeTime, int beatFrame)
    {

    }
    public void DynamicMusic()
    {
        switch (Status)
        {
            case (MissionStatus.START):
                {

                }
                break;
            case (MissionStatus.COMBAT):
                {
                    Music1.clip = Combat;
                    if (!Music1.isPlaying)
                    {
                        Music1.Play();
                    }
                    Music1.loop = true;
                }
                break;

        }
    }

    // Update is called once per frame
    void Update()
    {
        DynamicMusic();
    }
}





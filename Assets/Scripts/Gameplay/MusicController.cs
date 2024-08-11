using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicController : MonoBehaviour
{
    public float MusicVolume = 1f;
    public enum MissionStatus { LAUNCH, START, COMBAT, SUCCESS, FAIL, LANDING };
    public MissionStatus Status = MissionStatus.START;
    public static bool ExteriorCam = false;
    public static bool SpeechDuck = false;
    public static bool MusicEmphasis = false;
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
    float duckingMultiplier = 1f;
    float duckingTarget = 1f;
    float duckingPitchMultiplier = 1f;
    float duckingPitchTarget = 1f;
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
    public void MusicDucking()
    {
        if (ExteriorCam)
        {
            duckingTarget = .666f;
            duckingPitchTarget = 1f;
        }
        if (SpeechDuck)
        {
            duckingTarget = .666f;
            duckingPitchTarget = 1f;
        }
        if (MusicEmphasis)
        {
            duckingTarget = 2f;
            duckingPitchTarget = 1f;
        }
        if (!ExteriorCam && !SpeechDuck && !MusicEmphasis)
        {
            duckingTarget = 1f;
            duckingPitchTarget = 1f;
        }
        duckingMultiplier = Mathf.Lerp(duckingMultiplier, duckingTarget, .1f);
        duckingPitchMultiplier = Mathf.Lerp(duckingPitchMultiplier, duckingPitchTarget, .1f);
        Music1.volume = MusicVolume * duckingMultiplier;
        Music2.volume = MusicVolume * duckingMultiplier;
        Music1.pitch = duckingPitchMultiplier;
        Music2.pitch = duckingPitchMultiplier;
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
        MusicDucking();
    }
}





using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedSpriteList : MonoBehaviour
{
    public Sprite[] Frames;
    public float[] FrameDelays;
    [HideInInspector] public static int frames = 0;
    SpriteRenderer sprite;
    int CurrentFrame = 0;
    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        sprite.sprite = Frames[0];
    }

    // Update is called once per frame
    void Update()
    {
        sprite.sprite = Frames[CurrentFrame];
        if (CurrentFrame < Frames.Length - 1 && frames % FrameDelays[CurrentFrame] == 0)
        {
            CurrentFrame++;
        }
        frames++;
    }
}

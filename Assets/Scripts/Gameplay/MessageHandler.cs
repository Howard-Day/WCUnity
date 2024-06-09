using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MessageHandler", order = 1)]

public class MessageHandler : ScriptableObject
{
    public GameObject MFDAvatar;
    public string Name;
    public Message[] greetings;
    public Message[] agrees;
    public Message[] disagrees;
    public Message[] warnings;
    public Message[] insults;
    public Message[] cinematicMessages;
    public Message[] deaths;
    public Message[] landings;
    public Message[] fails;
    public Message[] retreats;
}


[System.Serializable]
public class Message
{
    public enum TYPE { GREETING, AGREE, DISAGREE, WARNING, INSULT, CINEMATIC, DEATH, LANDING, FAIL, RETREAT };
    public TYPE messageType = TYPE.GREETING;
    public string message;
    public AudioClip messageAudio;
    public int messageID = 0;
}
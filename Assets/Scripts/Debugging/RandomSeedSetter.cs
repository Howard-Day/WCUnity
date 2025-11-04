using UnityEngine;

[DefaultExecutionOrder(-9999)]
public class RandomSeedSetter : MonoBehaviour
{
    [SerializeField] private int seed = 101;
    [Min(30)]
    [SerializeField] private int framerate = 60;

    void Awake()
    {
        Random.InitState(seed);
        Time.captureFramerate = 60;
    }
}

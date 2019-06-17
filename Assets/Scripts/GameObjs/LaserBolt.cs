using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBolt : MonoBehaviour
{
  [SerializeField] float speed = 100f;
  [SerializeField] float range = 1000f;

  float distanceTraveled = 0f;
  /// <summary>
  /// Start is called on the frame when a script is enabled just before
  /// any of the Update methods is called the first time.
  /// </summary>
  void Start()
  {    
    gameObject.transform.SetParent(GameObject.FindWithTag("Projectiles").transform);   
    
  }
  void Update()
  {
    transform.position += transform.forward * speed * Time.deltaTime;

    distanceTraveled += speed * Time.deltaTime;
    if (distanceTraveled > range)
    {
      Destroy(gameObject);
    }
  }
}

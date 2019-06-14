using UnityEngine;
using System.Collections;

public class cInputDemoRocket : MonoBehaviour {
	public GameObject explosionPrefab;

	GameObject target;
	float _startTime;
	float _speed;

	void Start() {
		_startTime = Time.time;
		target = GameObject.FindWithTag("Player");
		gameObject.name = "Missile";
		Destroy(gameObject, 5);
	}

	void OnTriggerEnter(Collider col) {
		if (col.tag == "Player") {
			Destroy(gameObject);
			Destroy(target.gameObject);
			Instantiate(explosionPrefab, target.transform.position, Quaternion.identity);
		}
	}

	void Update() {
		if (target) {
			_speed = Mathf.Clamp(_speed, Time.time - _startTime + 6, 10);
			transform.LookAt(target.transform);
			transform.Translate(Vector3.forward * _speed * Time.deltaTime);
			transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
		} else { Destroy(gameObject); }
	}
}

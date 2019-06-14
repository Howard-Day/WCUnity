using UnityEngine;
using System.Collections;

public class cInputDemoTurret : MonoBehaviour {
	public GameObject rocketPrefab;
	public Transform shootPoint;
	public Transform target;
	float damping = 20;
	float shootDelay = 3;
	private float shootTime;
	private Renderer _renderer;

	void Start() {
		shootDelay = Random.Range(3, 7);
		shootTime = shootDelay;
		_renderer = transform.Find("Box10").GetComponent<Renderer>();
	}

	void Update() {
		if (target && _renderer.isVisible) {
			Quaternion rotate = Quaternion.LookRotation(target.position - transform.position);
			transform.rotation = Quaternion.Slerp(transform.rotation, rotate, Time.deltaTime * damping);
			transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
			if (Time.time > shootTime) {
				Shoot();
				shootTime = Time.time + shootDelay;
			}
		}
	}

	void Shoot() {
		GameObject _bullet = (GameObject)Instantiate(rocketPrefab, shootPoint.position, Quaternion.identity);
		_bullet.transform.LookAt(target);
		_bullet.tag = "Enemy";
	}
}

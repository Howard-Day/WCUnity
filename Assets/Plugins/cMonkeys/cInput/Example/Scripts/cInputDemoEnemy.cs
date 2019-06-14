using UnityEngine;
using System.Collections;

public class cInputDemoEnemy : MonoBehaviour {
	public GameObject bulletPrefab;
	public Transform playerTransform;

	float bulletTimer;

	private Transform _mesh;
	private Transform _turret;
	private Renderer _renderer;

	void Start() {
		_mesh = transform.Find("Mesh");
		_turret = transform.Find("Turret");
		_renderer = _mesh.GetComponent<Renderer>();
	}

	void Update() {
		if (!_renderer.isVisible) { // make sure the enemy can't shoot before it's fully on the screen
			bulletTimer = Time.time - 1;
		}

		transform.Translate(Vector3.forward * 5f * Time.deltaTime);
		if (playerTransform && _renderer.isVisible && Time.time > bulletTimer + 1.5f) {
			GameObject _bullet = (GameObject)Instantiate(bulletPrefab, _turret.position, Quaternion.identity);
			_bullet.transform.LookAt(playerTransform);
			_bullet.tag = "Enemy";
			bulletTimer = Time.time;
		}
	}
}

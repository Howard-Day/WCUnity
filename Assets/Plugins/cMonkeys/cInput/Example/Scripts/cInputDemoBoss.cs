using UnityEngine;
using System.Collections;

public class cInputDemoBoss : MonoBehaviour {
	public GameObject bulletPrefab;
	public Renderer renderedMesh;
	public Transform playerTransform;
	float bulletTimer;

	void Start() {
			renderedMesh = GetComponent<Renderer>();
	}

	void Update() {
		if (!renderedMesh) { Destroy(gameObject); return; }

		transform.Translate(Vector3.forward * 5f * Time.deltaTime);
		if (playerTransform && renderedMesh.isVisible && Time.time > bulletTimer + 1.5f) {
			// bullet 1
			GameObject _bullet = (GameObject)Instantiate(bulletPrefab, transform.position, Quaternion.identity);
			_bullet.transform.LookAt(playerTransform);
			_bullet.tag = "Enemy";
			// bullet 2
			_bullet = (GameObject)Instantiate(bulletPrefab, transform.position, Quaternion.identity);
			Vector3 offset = new Vector3(playerTransform.position.x - 10, playerTransform.position.y, playerTransform.position.z);
			_bullet.transform.LookAt(offset);
			_bullet.tag = "Enemy";
			// bullet 3
			_bullet = (GameObject)Instantiate(bulletPrefab, transform.position, Quaternion.identity);
			offset = new Vector3(playerTransform.position.x + 10, playerTransform.position.y, playerTransform.position.z);
			_bullet.transform.LookAt(offset);
			_bullet.tag = "Enemy";
			// update bulletTimer
			bulletTimer = Time.time;
		}
	}
}

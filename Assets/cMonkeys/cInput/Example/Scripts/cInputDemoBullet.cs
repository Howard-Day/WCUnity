using UnityEngine;
using System.Collections;

public class cInputDemoBullet : MonoBehaviour {
	public GameObject explosionPrefab;
	public AudioClip pewpew;

	void Start() {
		gameObject.name = "Bullet";
		GameObject bulletContainer;

		if (GameObject.Find("Bullets") == null) {
			bulletContainer = new GameObject();
			bulletContainer.name = "Bullets";
		} else {
			bulletContainer = GameObject.Find("Bullets");
		}

		transform.parent = bulletContainer.transform;
		Destroy(gameObject, 3);
	}

	void Update() {
		transform.Translate(Vector3.forward * 20f * Time.deltaTime);
		RaycastHit hit;
		if (Physics.Raycast(transform.position, Vector3.forward, out hit, 0.5f)) {
			string hitTag = hit.collider.tag;
			if (hitTag != tag) {
				if (hitTag == "Enemy") {
					Destroy(gameObject);
					Transform destroyMe;
					switch (hit.transform.name) {
						case "Bullet": {
								destroyMe = hit.collider.transform;
								break;
							}
						case "Missile": {
								destroyMe = hit.collider.transform;
								break;
							}
						case "anti Air Block": {
								destroyMe = hit.collider.transform;
								break;
							}
						case "enemy boss": {
								destroyMe = hit.collider.transform;
								break;
							}
						default: {
								destroyMe = hit.collider.transform.parent;
								break;
							}
					}

					Destroy(destroyMe.gameObject);
					Instantiate(explosionPrefab, destroyMe.position, Quaternion.identity);
				} else if (hitTag == "Player") {
					GameObject player = GameObject.FindGameObjectWithTag("Player");
					Destroy(gameObject);
					Destroy(player);
					Instantiate(explosionPrefab, player.transform.position, Quaternion.identity);
				}
			}
		}
	}

	void OnBecameInvisible() {
		Destroy(gameObject);
	}
}

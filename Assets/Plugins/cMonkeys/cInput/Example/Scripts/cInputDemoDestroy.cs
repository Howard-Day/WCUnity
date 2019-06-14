using UnityEngine;
using System.Collections;

public class cInputDemoDestroy : MonoBehaviour {

	IEnumerator Start() {
		yield return new WaitForSeconds(1.5f);
		Destroy(gameObject);
	}
}

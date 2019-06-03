/*
 * Standard Assets SmoothFollow.js translated to C#
 * 
This camera smooths out rotation around the y-axis and height.
Horizontal Distance to the target is always fixed.

There are many different ways to smooth the rotation but doing it this way gives you a lot of control over how the camera behaves.

For every of those smoothed values we calculate the wanted value and the current value.
Then we smooth it using the Lerp function.
Then we apply the smoothed values to the transform's position.
*/

using UnityEngine;

// adding cInputScript namespace in an attempt to prevent editor conflicts with UnityScript Standard Assets SmoothFollow
namespace cInputScript {
	public class SmoothFollow : MonoBehaviour {
		// the target we are following
		public Transform target;
		// the distance in the x-z plane to the target
		public float distance = 10f;
		// the heigh we want the camera to be above the target
		public float height = 5f;

		// how much we (???)
		public float heightDamping = 2f;
		public float rotationDamping = 3f;

		void LateUpdate() {
			// early out if we don't have a target
			if (!target) { return; }
			// calculate the current rotation angles
			var wantedRotationAngle = target.eulerAngles.y;
			var wantedHeight = target.position.y + height;
			
			var currentRotationAngle = transform.eulerAngles.y;
			var currentHeight = transform.position.y;
			// damp the rotation around the y-axis
			currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
			// damp the height
			currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);
			// convert the angle (quaternion) into a rotation (euler)
			var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);
			// set the position of the camera on the x-z plane to:
			// distance units behind the target
			transform.position = target.position;
			transform.position -= currentRotation * Vector3.forward * distance;
			// set the height of the camera
			transform.position = new Vector3(transform.position.x, currentHeight,transform.position.z);
			// always look at the target
			transform.LookAt(target);
		}
	}
}
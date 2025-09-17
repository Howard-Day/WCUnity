using NUnit.Framework;
using UnityEngine;

[RequireComponent (typeof(Camera))]
public class CameraFix : MonoBehaviour
{
	new private Camera camera;
	public RenderTexture rt;

	private void Awake() {
		camera = GetComponent<Camera>();
		Assert.IsNotNull(camera);

		camera.forceIntoRenderTexture = true;
	}

	private void LateUpdate() {
		camera.Render();
	}

	private void OnPreRender() {
		camera.targetTexture = rt;
	}

	//see https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnRenderImage.html
	private void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (camera == null) return;
		camera.targetTexture = null; //this is necessary so that our rendered image actually appears onscreen
	}
}

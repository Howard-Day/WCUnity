using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Image Effects/CRT/Ultimate CRT (standalone)")]
public class StandaloneCRTEffect : BaseCRTEffect {
    void OnPreCull() {
        InternalPreRender();
    }

    protected override RenderTexture CreateCameraTexture(RenderTexture currentCameraTexture) {
		var newCameraTexture = base.CreateCameraTexture(currentCameraTexture);
	
		if(newCameraTexture != null)
			return newCameraTexture;

		return new RenderTexture(Screen.width, Screen.height, 0);
	}

	override protected void OnCameraPostRender(Texture texture) {
		ProcessEffect(texture, (RenderTexture) null);
	}
}

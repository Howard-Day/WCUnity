using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
public class ImageSwitcher : MonoBehaviour {
	private int textureIndex;
	private Texture2D[] textures;

	private BaseCRTEffect.Preset[] presets;
	private int presetIndex;

	private SpriteRenderer spriteRenderer;
	private BaseCRTEffect effect;
	private Text text;

	private float textVisibleDuration = 0.0f;

	void Awake() {
		BaseCRTEffect.Preset[] allPresets = (BaseCRTEffect.Preset[]) System.Enum.GetValues(typeof(BaseCRTEffect.Preset));
		presets = new BaseCRTEffect.Preset[allPresets.Length - 1];

		var size = 0;
		foreach(var p in allPresets)
			if(p != BaseCRTEffect.Preset.Custom)
				presets[size++] = p;

		textures = Resources.LoadAll<Texture2D>("");

		spriteRenderer 	= GetComponent<SpriteRenderer>();

		foreach(Camera camera in Camera.allCameras) {
			effect = camera.GetComponentInChildren<BaseCRTEffect>();

			if(effect != null)
				break;
		}
			
		var texts = FindObjectsOfType<Text>();
		foreach(var t in texts) {
			if(t.name != "Preset Name")
				continue;

			text = t;
			break;
		}

		text.enabled = false;

		textureIndex = System.Array.IndexOf(textures, spriteRenderer.sprite.texture);
		presetIndex = System.Array.IndexOf(presets, effect.predefinedModel);

		if(presetIndex < 0) {
			effect.predefinedModel = presets[0];
			presetIndex = 0;
		}

		ShowPresetName(presets[presetIndex]);
	}
	
	void Update () {
		if(textVisibleDuration != 0.0f) {
			textVisibleDuration -= Time.deltaTime;

			if(textVisibleDuration <= 0.0f) {
				textVisibleDuration = 0.0f;
				text.enabled = false;
			}
		}

		var x = Input.GetKeyDown("a") ? -1 : Input.GetKeyDown("d") ? 1 : 0;
		var y = Input.GetKeyDown("w") ? -1 : Input.GetKeyDown("s") ? 1 : 0;
		var onOff = Input.GetKeyDown(KeyCode.Space);

		if(x == 0 && y == 0 && ! onOff)
			return;

		var texInd = (textureIndex + x + textures.Length) % textures.Length;
		var preInd = (presetIndex + y + presets.Length) % presets.Length;

		if(textureIndex != texInd) {
			spriteRenderer.sprite = Sprite.Create(textures[texInd], new Rect(0.0f, 0.0f, textures[texInd].width, textures[texInd].height), new Vector2(0.5f, 0.5f));
			textureIndex = texInd;
		}

		if(presetIndex != preInd) {
			presetIndex = preInd;
			effect.predefinedModel = presets[preInd];

			ShowPresetName(presets[preInd]);
		}

		if(onOff) {
			effect.enabled = ! effect.enabled;

			ShowOnOff(effect.enabled);
		}
	}

	public void OnUpClicked() {
		var preInd = (presetIndex - 1 + presets.Length) % presets.Length;

		presetIndex = preInd;
		effect.predefinedModel = presets[preInd];

		ShowPresetName(presets[preInd]);
	}

	public void OnDownClicked() {
		var preInd = (presetIndex + 1 + presets.Length) % presets.Length;

		presetIndex = preInd;
		effect.predefinedModel = presets[preInd];

		ShowPresetName(presets[preInd]);
	}

	public void OnLeftClicked() {
		var texInd = (textureIndex - 1 + textures.Length) % textures.Length;

		spriteRenderer.sprite = Sprite.Create(textures[texInd], new Rect(0.0f, 0.0f, textures[texInd].width, textures[texInd].height), new Vector2(0.5f, 0.5f));
		textureIndex = texInd;
	}

	public void OnRightClicked() {
		var texInd = (textureIndex + 1 + textures.Length) % textures.Length;

		spriteRenderer.sprite = Sprite.Create(textures[texInd], new Rect(0.0f, 0.0f, textures[texInd].width, textures[texInd].height), new Vector2(0.5f, 0.5f));
		textureIndex = texInd;
	}

	public void OnCenterClicked() {
		effect.enabled = ! effect.enabled;

		ShowOnOff(effect.enabled);
	}

	private void ShowPresetName(BaseCRTEffect.Preset preset) {
		text.text = preset.ToString();
		text.enabled = true;

		textVisibleDuration = 2.0f;
	}

	private void ShowOnOff(bool on) {
		text.text = on ? "[postprocess: on]" : "[postprocess: off]";
		text.enabled = true;

		textVisibleDuration = 2.0f;
	}
}
